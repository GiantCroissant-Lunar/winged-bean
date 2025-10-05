using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using ConsoleDungeon.Host;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Scrutor;
using WingedBean.Contracts;
using WingedBean.Contracts.Core;
using WingedBean.Contracts.Game;
using WingedBean.Registry;
using WingedBean.PluginLoader;
using WingedBean.Providers.AssemblyContext;
using WingedBean.PluginSystem;
using WingedBean.Host.Console;

// Console host entry point with dynamic plugin loading.
// Initializes Registry, loads plugins from configuration, and launches ConsoleDungeon app.

// Configure logging
using var loggerFactory = LoggerFactory.Create(builder =>
{
    builder.AddConsole();
    builder.SetMinimumLevel(LogLevel.Information);
});
var logger = loggerFactory.CreateLogger("ConsoleDungeon.Host");

logger.LogInformation("========================================");
logger.LogInformation("ConsoleDungeon.Host - Dynamic Plugin Mode");
logger.LogInformation("========================================");
logger.LogInformation("");

try
{
    // Step 1: Create foundation services
    logger.LogInformation("[1/5] Initializing foundation services...");
    var registry = new ActualRegistry();
    var contextProvider = new AssemblyContextProvider();
    var pluginLoader = new ActualPluginLoader(contextProvider);

    registry.Register<IRegistry>(registry);
    registry.Register<WingedBean.Contracts.Core.IPluginLoader>(pluginLoader);
    logger.LogInformation("✓ Foundation services initialized");
    logger.LogInformation("");

    // Step 2: Discover plugins (hybrid: new manifests + legacy fallback)
    logger.LogInformation("[2/5] Loading plugin configuration...");
    var exeDirectory = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location) ?? System.Environment.CurrentDirectory;
    var pluginsDir = System.IO.Path.Combine(exeDirectory, "plugins");

    var loadedById = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
    var loadedPlugins = new Dictionary<string, WingedBean.Contracts.Core.ILoadedPlugin>();

    // New path (default): discover .plugin.json manifests and load via ALC loader
    var discovery = new PluginDiscovery(pluginsDir);
    var alcLoader = new AlcPluginLoader();

    // Host services for DI-based plugin activators
    var hostServicesCollection = new ServiceCollection();
    hostServicesCollection.AddLogging(b =>
    {
        b.AddConsole();
        b.SetMinimumLevel(LogLevel.Information);
    });
    hostServicesCollection.AddSingleton<IRegistry>(registry);
    var hostServicesProvider = hostServicesCollection.BuildServiceProvider();

    var discovered = (await discovery.DiscoverPluginsAsync()).ToList();
    logger.LogInformation("  → Discovered {Count} plugin manifest(s) in {PluginsDir}", discovered.Count, pluginsDir);

    foreach (var manifest in discovered)
    {
        try
        {
            var plugin = await alcLoader.LoadPluginAsync(manifest);
            await plugin.ActivateAsync(hostServicesProvider);

            // Scrutor: auto-wire any concrete classes that implement WingedBean.Contracts.*
            plugin.Services
                .Scan(scan => scan
                    .FromAssemblies(((WingedBean.Host.Console.LoadedPlugin)plugin).Assembly)
                    .AddClasses(c => c.Where(t =>
                        t.IsClass && !t.IsAbstract &&
                        t.GetInterfaces().Any(i => i.Namespace?.StartsWith("WingedBean.Contracts") == true)))
                    .AsImplementedInterfaces()
                    .WithSingletonLifetime());

            RegisterServicesFromServiceCollection(registry, plugin.Services, manifest.Id, logger);
            loadedPlugins[manifest.Id] = new LegacyLoadedAdapter(manifest.Id);
            loadedById.Add(manifest.Id);
            logger.LogInformation("    ✓ ALC-loaded: {PluginId} v{Version}", manifest.Id, manifest.Version);
        }
        catch (System.Exception ex)
        {
            logger.LogError("    ✗ Failed ALC load {PluginId}: {Message}", manifest.Id, ex.Message);
        }
    }

    // Legacy path deprecated: only load if explicitly enabled
    var useLegacy = (Environment.GetEnvironmentVariable("ENABLE_LEGACY_PLUGINS_JSON") ?? "").ToLowerInvariant();
    List<PluginDescriptor> enabledPlugins = new();
    if (useLegacy == "1" || useLegacy == "true")
    {
        var configPath = System.IO.Path.Combine(exeDirectory, "plugins.json");
        var config = await LoadPluginConfigurationAsync(configPath);
        enabledPlugins = config.Plugins
            .Where(p => p.Enabled && !loadedById.Contains(p.Id))
            .OrderByDescending(p => p.Priority)
            .ToList();
        logger.LogInformation("  → Legacy config entries: {Count}", enabledPlugins.Count);
    }
    else
    {
        logger.LogInformation("  → Legacy plugins.json disabled (set ENABLE_LEGACY_PLUGINS_JSON=1 to enable)");
    }

    // Step 3: Load plugins dynamically
    logger.LogInformation("[3/5] Loading plugins...");
    foreach (var descriptor in enabledPlugins)
    {
        if (descriptor.LoadStrategy != ConsoleDungeon.Host.LoadStrategy.Eager)
        {
            logger.LogInformation("  ⊘ Skipping {PluginId} (strategy: {LoadStrategy})", descriptor.Id, descriptor.LoadStrategy);
            continue;
        }

        logger.LogInformation("  → Loading: {PluginId} (priority: {Priority})", descriptor.Id, descriptor.Priority);

        try
        {
            // Resolve plugin path relative to executable directory
            var pluginPath = System.IO.Path.IsPathRooted(descriptor.Path)
                ? descriptor.Path
                : System.IO.Path.Combine(exeDirectory, descriptor.Path);
            var plugin = await pluginLoader.LoadAsync(pluginPath);
            loadedPlugins[descriptor.Id] = plugin;

            logger.LogInformation("    ✓ Loaded: {PluginId} v{Version}", plugin.Manifest.Id, plugin.Manifest.Version);

            // Auto-register services from plugin
            await RegisterPluginServicesAsync(registry, plugin, descriptor.Priority, logger);
        }
        catch (System.Exception ex)
        {
            logger.LogError("    ✗ Failed to load {PluginId}: {Message}", descriptor.Id, ex.Message);

            // Check if plugin is critical
            if (descriptor.Priority >= 1000)
            {
                logger.LogCritical("    CRITICAL: Plugin {PluginId} failed to load. Aborting.", descriptor.Id);
                return;
            }
        }
    }

    logger.LogInformation("✓ {Count} plugins loaded successfully", loadedPlugins.Count);
    logger.LogInformation("");

    // Step 4: Verify required services
    logger.LogInformation("[4/5] Verifying service registry...");
    VerifyRequiredServices(registry, logger);
    logger.LogInformation("✓ All required services registered");
    logger.LogInformation("");

    // Step 5: Launch Terminal App from plugins (RFC-0017)
    logger.LogInformation("[5/5] Launching Terminal App from plugins...");
    logger.LogInformation("");

    if (!registry.IsRegistered<ITerminalApp>())
    {
        throw new System.InvalidOperationException("No ITerminalApp registered. Ensure a UI plugin (e.g., ConsoleDungeonApp) is loaded.");
    }

    var terminalApp = registry.Get<ITerminalApp>();
    var appConfig = new TerminalAppConfig
    {
        Name = "Console Dungeon",
        Cols = 80,
        Rows = 24
    };

    // Provide registry and gameplay service to UI (RFC-0018)
    appConfig.Parameters["registry"] = registry;

    if (registry.IsRegistered<IDungeonGameService>())
    {
        var gameService = registry.Get<IDungeonGameService>();
        appConfig.Parameters["gameService"] = gameService;
    }
    await terminalApp.StartAsync(appConfig);
}
catch (System.Exception ex)
{
    logger.LogCritical(ex, "FATAL ERROR: {Message}", ex.Message);
    System.Environment.Exit(1);
}

static async System.Threading.Tasks.Task<PluginConfiguration> LoadPluginConfigurationAsync(string path)
{
    if (!System.IO.File.Exists(path))
    {
        throw new System.IO.FileNotFoundException($"Plugin configuration file not found: {path}");
    }

    var json = await System.IO.File.ReadAllTextAsync(path);
    var config = JsonSerializer.Deserialize<PluginConfiguration>(json, new JsonSerializerOptions
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip
    });

    return config ?? throw new System.InvalidOperationException("Failed to parse plugin configuration");
}

static async System.Threading.Tasks.Task RegisterPluginServicesAsync(
    IRegistry registry,
    WingedBean.Contracts.Core.ILoadedPlugin plugin,
    int pluginPriority,
    ILogger logger)
{
    // Set registry on plugin BEFORE activation (required for OnActivateAsync)
    var setRegistryMethod = plugin.GetType().GetMethod(
        "SetRegistry",
        System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public);
    if (setRegistryMethod != null)
    {
        setRegistryMethod.Invoke(plugin, new object[] { registry });
    }

    // Activate plugin (if it implements IPlugin)
    await plugin.ActivateAsync();

    // Build candidate registrations, honoring [Plugin] attribute metadata when present
    var candidates = new List<(Type contractType, object instance, int priority)>();
    foreach (var service in plugin.GetServices())
    {
        var implType = service.GetType();

        // Read optional [Plugin] attribute from the implementation class
        var pluginAttr = implType
            .GetCustomAttributes(typeof(WingedBean.Contracts.Core.PluginAttribute), inherit: true)
            .Cast<WingedBean.Contracts.Core.PluginAttribute>()
            .FirstOrDefault();

        // Determine contract interfaces this instance actually implements (WingedBean.Contracts.*)
        var implementedContracts = implType.GetInterfaces()
            .Where(i => i.Namespace?.StartsWith("WingedBean.Contracts") == true)
            .ToList();

        // If [Plugin].Provides is specified, restrict to those that are actually implemented
        IEnumerable<Type> contractInterfaces = implementedContracts;
        if (pluginAttr?.Provides != null && pluginAttr.Provides.Length > 0)
        {
            var provided = new HashSet<Type>(pluginAttr.Provides);
            contractInterfaces = implementedContracts.Where(i => provided.Contains(i));
        }

        // Choose priority: service-level [Plugin].Priority (if > 0) overrides plugin descriptor priority
        var effectivePriority = (pluginAttr != null && pluginAttr.Priority > 0)
            ? pluginAttr.Priority
            : pluginPriority;

        foreach (var contract in contractInterfaces)
        {
            candidates.Add((contract, service, effectivePriority));
        }
    }

    // Deduplicate: register only the highest-priority implementation per contract type
    var byContract = candidates
        .GroupBy(c => c.contractType)
        .Select(g => g.OrderByDescending(c => c.priority).First());

    foreach (var entry in byContract)
    {
        var contractType = entry.contractType;
        var instance = entry.instance;
        var priority = entry.priority;

        // Use reflection to call IRegistry.Register<T>(T implementation, int priority)
        var registerMethod = typeof(IRegistry).GetMethods()
            .Where(m => m.Name == "Register" && m.IsGenericMethod)
            .Where(m => m.GetParameters().Length == 2)
            .Where(m => m.GetParameters()[0].ParameterType.IsGenericParameter)
            .Where(m => m.GetParameters()[1].ParameterType == typeof(int))
            .FirstOrDefault()
            ?.MakeGenericMethod(contractType);

        registerMethod?.Invoke(registry, new object[] { instance, priority });
        logger.LogDebug("      → Registered: {ContractType} (priority: {Priority})", contractType.Name, priority);
    }
}

static void RegisterServicesFromServiceCollection(
    IRegistry registry,
    Microsoft.Extensions.DependencyInjection.IServiceCollection services,
    string pluginId,
    ILogger logger)
{
    // Build a temporary provider to resolve instances
    // Ensure basic logging and host registry so ctor injection can resolve
    services.AddLogging();
    services.AddSingleton<IRegistry>(registry);
    var provider = services.BuildServiceProvider();

    foreach (var sd in services)
    {
        var serviceType = sd.ServiceType;
        if (serviceType?.Namespace?.StartsWith("WingedBean.Contracts") != true) continue;
        // Skip foundation services we already hold
        if (serviceType == typeof(IRegistry) || serviceType == typeof(WingedBean.Contracts.Core.IPluginLoader))
            continue;

        try
        {
            var instance = provider.GetService(serviceType);
            if (instance == null) continue;

            var registerMethod = typeof(IRegistry).GetMethods()
                .Where(m => m.Name == "Register" && m.IsGenericMethod)
                .Where(m => m.GetParameters().Length == 2)
                .Where(m => m.GetParameters()[0].ParameterType.IsGenericParameter)
                .Where(m => m.GetParameters()[1].ParameterType == typeof(int))
                .FirstOrDefault()
                ?.MakeGenericMethod(serviceType);

            // Default priority for DI-registered services (can be refined later)
            registerMethod?.Invoke(registry, new object[] { instance, 100 });
            logger.LogDebug("      → Registered: {ServiceType} from {PluginId} (priority: 100)", serviceType.Name, pluginId);
        }
        catch (Exception ex)
        {
            logger.LogWarning("      ⚠ Failed to register {ServiceType} from {PluginId}: {Message}", serviceType?.Name, pluginId, ex.Message);
        }
    }
}

static void VerifyRequiredServices(IRegistry registry, ILogger logger)
{
    // Verify foundation services
    if (!registry.IsRegistered<IRegistry>())
    {
        throw new System.InvalidOperationException("IRegistry is not registered");
    }
    if (!registry.IsRegistered<WingedBean.Contracts.Core.IPluginLoader>())
    {
        throw new System.InvalidOperationException("IPluginLoader is not registered");
    }

    logger.LogInformation("  ✓ IRegistry registered");
    logger.LogInformation("  ✓ IPluginLoader registered");
}

namespace ConsoleDungeon.Host
{
    // Adapter to satisfy existing loadedPlugins dictionary (we only need Id for counts)
    sealed class LegacyLoadedAdapter : WingedBean.Contracts.Core.ILoadedPlugin
    {
        public LegacyLoadedAdapter(string id) { Id = id; }
        public string Id { get; }
        public string Version => "";
        public WingedBean.Contracts.Core.PluginManifest Manifest => new WingedBean.Contracts.Core.PluginManifest();
        public WingedBean.Contracts.Core.PluginState State => WingedBean.Contracts.Core.PluginState.Activated;
        public TService? GetService<TService>() where TService : class => null;
        public IEnumerable<object> GetServices() => Array.Empty<object>();
        public System.Threading.Tasks.Task ActivateAsync(System.Threading.CancellationToken cancellationToken = default) => System.Threading.Tasks.Task.CompletedTask;
        public System.Threading.Tasks.Task DeactivateAsync(System.Threading.CancellationToken cancellationToken = default) => System.Threading.Tasks.Task.CompletedTask;
    }
}

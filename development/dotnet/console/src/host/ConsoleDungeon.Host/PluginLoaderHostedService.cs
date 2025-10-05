using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using WingedBean.Contracts.Core;
using WingedBean.PluginSystem;
using WingedBean.Registry;
using WingedBean.PluginLoader;
using ConsoleDungeon.Host;
using WingedBean.Contracts.Terminal;

namespace WingedBean.Host.Console;

/// <summary>
/// Hosted service that loads plugins and initializes the application.
/// Runs after DI container is built but before the terminal app starts.
/// </summary>
public class PluginLoaderHostedService : IHostedService
{
    private readonly IRegistry _registry;
    private readonly WingedBean.Contracts.Core.IPluginLoader _pluginLoader;
    private readonly ILogger<PluginLoaderHostedService> _logger;

    public PluginLoaderHostedService(
        IRegistry registry,
        WingedBean.Contracts.Core.IPluginLoader pluginLoader,
        ILogger<PluginLoaderHostedService> logger)
    {
        _registry = registry;
        _pluginLoader = pluginLoader;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await LoadPluginsAsync(cancellationToken);
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    private async Task LoadPluginsAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("========================================");
        _logger.LogInformation("ConsoleDungeon.Host - Dynamic Plugin Mode");
        _logger.LogInformation("========================================");

        try
        {
            // Step 1: Create foundation services
            _logger.LogInformation("[1/3] Initializing foundation services...");
            _registry.Register<IRegistry>(_registry);
            _registry.Register<WingedBean.Contracts.Core.IPluginLoader>(_pluginLoader);
            _logger.LogInformation("✓ Foundation services initialized");

            // Step 2: Discover and load plugins
            _logger.LogInformation("[2/3] Loading plugins...");
            var exeDirectory = Path.GetDirectoryName(typeof(PluginLoaderHostedService).Assembly.Location) ?? Environment.CurrentDirectory;
            var pluginsDir = Path.Combine(exeDirectory, "plugins");

            var loadedPlugins = new Dictionary<string, WingedBean.Contracts.Core.ILoadedPlugin>();
            var loadedById = new HashSet<string>();

            // Load manifest-based plugins
            if (Directory.Exists(pluginsDir))
            {
                var manifestPaths = Directory.GetFiles(pluginsDir, ".plugin.json", SearchOption.AllDirectories);
                foreach (var manifestPath in manifestPaths)
                {
                    try
                    {
                        var pluginDir = Path.GetDirectoryName(manifestPath)!;
                        var manifest = await LoadPluginManifestAsync(manifestPath);
                        var pluginId = manifest.Id;

                        if (loadedById.Contains(pluginId))
                        {
                            _logger.LogWarning("  ⊘ Skipping duplicate plugin: {PluginId}", pluginId);
                            continue;
                        }

                        _logger.LogInformation("  → Loading manifest plugin: {PluginId}", pluginId);
                        var plugin = await _pluginLoader.LoadAsync(pluginDir);
                        loadedPlugins[pluginId] = plugin;
                        loadedById.Add(pluginId);

                        // Auto-register services from plugin
                        await RegisterPluginServicesAsync(plugin, 100, cancellationToken); // Default priority for manifests
                        _logger.LogInformation("    ✓ Loaded: {PluginId} v{Version}", plugin.Manifest.Id, plugin.Manifest.Version);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "    ✗ Failed to load manifest plugin from {ManifestPath}: {Message}", manifestPath, ex.Message);
                    }
                }
            }

            // Legacy path: prefer manifests, but fall back to plugins.json when none are present or explicitly enabled
            var useLegacyEnv = (Environment.GetEnvironmentVariable("ENABLE_LEGACY_PLUGINS_JSON") ?? "").ToLowerInvariant();
            var enableLegacy = useLegacyEnv == "1" || useLegacyEnv == "true" || loadedById.Count == 0;
            List<PluginDescriptor> enabledPlugins = new();
            if (enableLegacy)
            {
                var configPath = Path.Combine(exeDirectory, "plugins.json");
                if (File.Exists(configPath))
                {
                    var config = await LoadPluginConfigurationAsync(configPath);
                    enabledPlugins = config.Plugins
                        .Where(p => p.Enabled && !loadedById.Contains(p.Id))
                        .OrderByDescending(p => p.Priority)
                        .ToList();
                    var reason = loadedById.Count == 0 && string.IsNullOrEmpty(useLegacyEnv)
                        ? "(fallback: no manifests found)"
                        : (string.IsNullOrEmpty(useLegacyEnv) ? "(env enabled)" : "(env enabled)");
                    _logger.LogInformation("  → Legacy plugins.json entries: {Count} {Reason}", enabledPlugins.Count, reason);
                }
                else
                {
                    _logger.LogInformation("  → No plugins.json found at {Path}", configPath);
                }
            }
            else
            {
                _logger.LogInformation("  → Legacy plugins.json disabled (set ENABLE_LEGACY_PLUGINS_JSON=1 to force)");
            }

            // Load legacy plugins
            foreach (var descriptor in enabledPlugins)
            {
                if (descriptor.LoadStrategy != ConsoleDungeon.Host.LoadStrategy.Eager)
                {
                    _logger.LogInformation("  ⊘ Skipping {PluginId} (strategy: {LoadStrategy})", descriptor.Id, descriptor.LoadStrategy);
                    continue;
                }

                _logger.LogInformation("  → Loading: {PluginId} (priority: {Priority})", descriptor.Id, descriptor.Priority);

                try
                {
                    // Resolve plugin path relative to executable directory
                    var pluginPath = Path.IsPathRooted(descriptor.Path)
                        ? descriptor.Path
                        : Path.Combine(exeDirectory, descriptor.Path);
                    var plugin = await _pluginLoader.LoadAsync(pluginPath);
                    loadedPlugins[descriptor.Id] = plugin;

                    _logger.LogInformation("    ✓ Loaded: {PluginId} v{Version}", plugin.Manifest.Id, plugin.Manifest.Version);

                    // Auto-register services from plugin
                    await RegisterPluginServicesAsync(plugin, descriptor.Priority, cancellationToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError("    ✗ Failed to load {PluginId}: {Message}", descriptor.Id, ex.Message);

                    // Check if plugin is critical
                    if (descriptor.Priority >= 1000)
                    {
                        _logger.LogCritical("    CRITICAL: Plugin {PluginId} failed to load. Aborting.", descriptor.Id);
                        throw;
                    }
                }
            }

            _logger.LogInformation("✓ {Count} plugins loaded successfully", loadedPlugins.Count);

            // Step 3: Verify required services
            _logger.LogInformation("[3/3] Verifying service registry...");
            VerifyRequiredServices();
            _logger.LogInformation("✓ All required services registered");
        }
        catch (Exception ex)
        {
            _logger.LogCritical(ex, "FATAL ERROR during plugin loading: {Message}", ex.Message);
            throw;
        }
    }

    private async Task<WingedBean.PluginSystem.PluginManifest> LoadPluginManifestAsync(string path)
    {
        var json = await File.ReadAllTextAsync(path);
        var manifest = JsonSerializer.Deserialize<WingedBean.PluginSystem.PluginManifest>(json, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            ReadCommentHandling = JsonCommentHandling.Skip
        });
        return manifest ?? throw new InvalidOperationException("Failed to parse plugin manifest");
    }

    private async Task<PluginConfiguration> LoadPluginConfigurationAsync(string path)
    {
        if (!File.Exists(path))
        {
            throw new FileNotFoundException($"Plugin configuration file not found: {path}");
        }

        var json = await File.ReadAllTextAsync(path);
        var config = JsonSerializer.Deserialize<PluginConfiguration>(json, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            ReadCommentHandling = JsonCommentHandling.Skip
        });

        return config ?? throw new InvalidOperationException("Failed to parse plugin configuration");
    }

    private async Task RegisterPluginServicesAsync(
        WingedBean.Contracts.Core.ILoadedPlugin plugin,
        int pluginPriority,
        CancellationToken cancellationToken)
    {
        // Set registry on plugin BEFORE activation (required for OnActivateAsync)
        var setRegistryMethod = plugin.GetType().GetMethod(
            "SetRegistry",
            BindingFlags.Instance | BindingFlags.Public);
        if (setRegistryMethod != null)
        {
            setRegistryMethod.Invoke(plugin, new object[] { _registry });
        }

        // Activate plugin (if it implements IPlugin)
        await plugin.ActivateAsync();

        // Build candidate registrations, honoring [Plugin] attribute metadata when present
        var candidates = new List<(Type contractType, object instance, int priority)>();
        foreach (var service in plugin.GetServices())
        {
            var implType = service.GetType();

            // If service has SetRegistry(IRegistry), inject the runtime registry
            try
            {
                var setReg = implType.GetMethod("SetRegistry", BindingFlags.Instance | BindingFlags.Public);
                if (setReg != null && setReg.GetParameters().Length == 1 && setReg.GetParameters()[0].ParameterType == typeof(IRegistry))
                {
                    setReg.Invoke(service, new object[] { _registry });
                }
            }
            catch { }

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

            registerMethod?.Invoke(_registry, new object[] { instance, priority });
            _logger.LogDebug("      → Registered: {ContractType} (priority: {Priority})", contractType.Name, priority);
        }
    }

    private void VerifyRequiredServices()
    {
        // Verify foundation services
        if (!_registry.IsRegistered<IRegistry>())
        {
            throw new InvalidOperationException("IRegistry is not registered");
        }
        if (!_registry.IsRegistered<WingedBean.Contracts.Core.IPluginLoader>())
        {
            throw new InvalidOperationException("IPluginLoader is not registered");
        }

        _logger.LogInformation("  ✓ IRegistry registered");
        _logger.LogInformation("  ✓ IPluginLoader registered");

        // Check for terminal app
        try
        {
            var terminalApp = _registry.Get<ITerminalApp>();
            _logger.LogInformation("  ✓ ITerminalApp registered: {Type}", terminalApp.GetType().Name);
        }
        catch (Exception ex)
        {
            _logger.LogWarning("  ⚠ ITerminalApp not registered: {Message}", ex.Message);
        }

        // Check for render service
        try
        {
            var renderService = _registry.Get<WingedBean.Contracts.Game.IRenderService>();
            _logger.LogInformation("  ✓ IRenderService registered: {Type}", renderService.GetType().Name);
        }
        catch (Exception ex)
        {
            _logger.LogWarning("  ⚠ IRenderService not registered: {Message}", ex.Message);
        }

        // Check for game service
        try
        {
            var gameService = _registry.Get<WingedBean.Contracts.Game.IDungeonGameService>();
            _logger.LogInformation("  ✓ IDungeonGameService registered: {Type}", gameService.GetType().Name);
        }
        catch (Exception ex)
        {
            _logger.LogWarning("  ⚠ IDungeonGameService not registered: {Message}", ex.Message);
        }
    }
}

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
using Plate.PluginManoi.Contracts;
using Plate.PluginManoi.Core;
using Plate.PluginManoi.Registry;
using Plate.PluginManoi.Loader;
using ConsoleDungeon.Host;

// Terminal and UI contracts now in Plate.CrossMilo.Contracts namespace
using ITerminalApp = Plate.CrossMilo.Contracts.TerminalUI.ITerminalApp;
// Game contracts are now in Plate.CrossMilo.Contracts.Game.*
using IDungeonGameService = Plate.CrossMilo.Contracts.Game.Dungeon.IService;
using IRenderService = Plate.CrossMilo.Contracts.Game.Render.IService;
// Resource service for NuGet loading
using IResourceService = Plate.CrossMilo.Contracts.Resource.Services.IService;

// Type aliases for old references that haven't been migrated yet
using IPluginLoader = Plate.PluginManoi.Contracts.IPluginLoader;
using ILoadedPlugin = Plate.PluginManoi.Contracts.ILoadedPlugin;
using PluginAttribute = Plate.PluginManoi.Contracts.PluginAttribute;
using PluginManifest = Plate.PluginManoi.Core.PluginManifest;

namespace WingedBean.Host.Console;

/// <summary>
/// Hosted service that loads plugins and initializes the application.
/// Runs after DI container is built but before the terminal app starts.
/// </summary>
public class PluginLoaderHostedService : IHostedService
{
    private readonly IRegistry _registry;
    private readonly IPluginLoader _pluginLoader;
    private readonly ILogger<PluginLoaderHostedService> _logger;

    public PluginLoaderHostedService(
        IRegistry registry,
        IPluginLoader pluginLoader,
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
            _registry.Register<IPluginLoader>(_pluginLoader);
            _logger.LogInformation("✓ Foundation services initialized");

            // Step 2: Discover and load plugins
            _logger.LogInformation("[2/3] Loading plugins...");
            var exeDirectory = Path.GetDirectoryName(typeof(PluginLoaderHostedService).Assembly.Location) ?? Environment.CurrentDirectory;
            var pluginsDir = Path.Combine(exeDirectory, "plugins");

            var loadedPlugins = new Dictionary<string, ILoadedPlugin>();
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

                        // Load NuGet dependencies BEFORE loading plugin assembly
                        await LoadNuGetDependenciesAsync(manifestPath, pluginId, cancellationToken);

                        // Resolve entry point path relative to manifest directory
                        var entryPoint = manifest.EntryPoint?.Dotnet ?? $"./{manifest.Id}.dll";
                        var assemblyPath = Path.GetFullPath(Path.Combine(pluginDir, entryPoint));
                        
                        _logger.LogInformation("  → Loading manifest plugin: {PluginId} from {AssemblyPath}", pluginId, assemblyPath);
                        
                        // Load plugin using resolved assembly path
                        var plugin = await _pluginLoader.LoadAsync(assemblyPath);
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

    private async Task<PluginManifest> LoadPluginManifestAsync(string path)
    {
        var json = await File.ReadAllTextAsync(path);
        var manifest = JsonSerializer.Deserialize<PluginManifest>(json, new JsonSerializerOptions
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
        ILoadedPlugin plugin,
        int pluginPriority,
        CancellationToken cancellationToken)
    {
        // Set registry on plugin BEFORE activation - RFC-0038: Use IRegistryAware
        if (plugin is IRegistryAware registryAware)
        {
            registryAware.SetRegistry(_registry);
        }
        else
        {
            // Fallback to reflection for plugins not yet updated
            var setRegistryMethod = plugin.GetType().GetMethod(
                "SetRegistry",
                BindingFlags.Instance | BindingFlags.Public);
            if (setRegistryMethod != null)
            {
                setRegistryMethod.Invoke(plugin, new object[] { _registry });
            }
        }

        // Activate plugin (if it implements IPlugin)
        await plugin.ActivateAsync();

        // Build candidate registrations, honoring [Plugin] attribute metadata when present
        var candidates = new List<(Type contractType, object instance, int priority)>();
        foreach (var service in plugin.GetServices())
        {
            var implType = service.GetType();

            // If service has SetRegistry(IRegistry), inject the runtime registry - RFC-0038
            if (service is IRegistryAware serviceRegistryAware)
            {
                serviceRegistryAware.SetRegistry(_registry);
            }
            else
            {
                // Fallback to reflection for services not yet updated
                try
                {
                    var setReg = implType.GetMethod("SetRegistry", BindingFlags.Instance | BindingFlags.Public);
                    if (setReg != null && setReg.GetParameters().Length == 1 && setReg.GetParameters()[0].ParameterType == typeof(IRegistry))
                    {
                        setReg.Invoke(service, new object[] { _registry });
                    }
                }
                catch { }
            }

            // Read optional [Plugin] attribute from the implementation class
            var pluginAttr = implType
                .GetCustomAttributes(typeof(PluginAttribute), inherit: true)
                .Cast<PluginAttribute>()
                .FirstOrDefault();

            // Determine contract interfaces this instance actually implements
            // Support both legacy (WingedBean.Contracts.*) and new (Plate.CrossMilo.Contracts.*) namespaces
            var implementedContracts = implType.GetInterfaces()
                .Where(i => i.Namespace?.StartsWith("WingedBean.Contracts") == true ||
                           i.Namespace?.StartsWith("Plate.CrossMilo.Contracts") == true)
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

            // Use RegistryHelper for type-safe registration (RFC-0038 Phase 2)
            _registry.RegisterDynamic(contractType, instance, priority);
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
        if (!_registry.IsRegistered<IPluginLoader>())
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
            var renderService = _registry.Get<IRenderService>();
            _logger.LogInformation("  ✓ IRenderService registered: {Type}", renderService.GetType().Name);
        }
        catch (Exception ex)
        {
            _logger.LogWarning("  ⚠ IRenderService not registered: {Message}", ex.Message);
        }

        // Check for game service
        try
        {
            var gameService = _registry.Get<IDungeonGameService>();
            _logger.LogInformation("  ✓ IDungeonGameService registered: {Type}", gameService.GetType().Name);
        }
        catch (Exception ex)
        {
            _logger.LogWarning("  ⚠ IDungeonGameService not registered: {Message}", ex.Message);
        }
    }
    
    /// <summary>
    /// Load NuGet package dependencies for a plugin before the plugin assembly is loaded.
    /// </summary>
    private async Task LoadNuGetDependenciesAsync(
        string manifestPath,
        string pluginId,
        CancellationToken cancellationToken)
    {
        try
        {
            // Read raw JSON to extract dependencies
            var json = await File.ReadAllTextAsync(manifestPath, cancellationToken);
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;
            
            // Check if dependencies.nuget exists
            if (!root.TryGetProperty("dependencies", out var depsElement))
            {
                return; // No dependencies at all
            }
            
            if (!depsElement.TryGetProperty("nuget", out var nugetElement))
            {
                return; // No NuGet dependencies
            }
            
            // Try to get Resource service from registry
            IResourceService? resourceService = null;
            try
            {
                resourceService = _registry.Get<IResourceService>();
            }
            catch
            {
                // Resource service not available yet - this is expected if Resource plugin
                // hasn't loaded yet. We'll skip NuGet loading for now.
                _logger.LogDebug("    → Resource service not available for NuGet dependency loading (will be available after Resource plugin loads)");
                return;
            }
            
            if (resourceService == null)
            {
                _logger.LogWarning("    → Resource service not available, skipping NuGet dependencies for {PluginId}", pluginId);
                return;
            }
            
            // Parse NuGet dependencies array
            var nugetDeps = new List<NuGetDependency>();
            foreach (var element in nugetElement.EnumerateArray())
            {
                var dep = new NuGetDependency
                {
                    PackageId = element.GetProperty("packageId").GetString() ?? "",
                    Version = element.TryGetProperty("version", out var verElem) ? verElem.GetString() : null,
                    Feed = element.TryGetProperty("feed", out var feedElem) ? feedElem.GetString() : null,
                    Optional = element.TryGetProperty("optional", out var optElem) && optElem.GetBoolean(),
                    Reason = element.TryGetProperty("reason", out var reasonElem) ? reasonElem.GetString() : null
                };
                
                if (!string.IsNullOrWhiteSpace(dep.PackageId))
                {
                    nugetDeps.Add(dep);
                }
            }
            
            if (nugetDeps.Count == 0)
            {
                return; // No valid NuGet dependencies
            }
            
            _logger.LogInformation("    → Loading {Count} NuGet dependencies for {PluginId}...", nugetDeps.Count, pluginId);
            
            // Load each NuGet package
            foreach (var dep in nugetDeps)
            {
                try
                {
                    var versionStr = dep.Version != null ? $"/{dep.Version}" : "";
                    var feedStr = dep.Feed != null ? $"@{dep.Feed}" : "";
                    var nugetUri = $"nuget:{dep.PackageId}{versionStr}{feedStr}";
                    
                    _logger.LogInformation("      → Loading NuGet: {PackageId} {Version}", dep.PackageId, dep.Version ?? "latest");
                    
                    // Dynamically load NuGetPackageResource type
                    var nugetResourceType = Type.GetType(
                        "WingedBean.Plugins.Resource.NuGet.NuGetPackageResource, WingedBean.Plugins.Resource.NuGet"
                    );
                    
                    if (nugetResourceType == null)
                    {
                        _logger.LogWarning("        ⊘ NuGet provider not available, skipping package: {PackageId}", dep.PackageId);
                        if (!dep.Optional)
                        {
                            throw new InvalidOperationException(
                                $"Required NuGet package '{dep.PackageId}' cannot be loaded: NuGet provider not available"
                            );
                        }
                        continue;
                    }
                    
                    // Call LoadAsync via reflection
                    var loadAsyncMethod = typeof(IResourceService)
                        .GetMethod(nameof(IResourceService.LoadAsync))!
                        .MakeGenericMethod(nugetResourceType);
                    
                    var loadTask = (Task)loadAsyncMethod.Invoke(
                        resourceService,
                        new object[] { nugetUri, cancellationToken }
                    )!;
                    
                    await loadTask.ConfigureAwait(false);
                    
                    var package = loadTask.GetType().GetProperty("Result")!.GetValue(loadTask);
                    
                    if (package == null && !dep.Optional)
                    {
                        throw new InvalidOperationException(
                            $"Required NuGet package '{dep.PackageId}' version '{dep.Version ?? "latest"}' not found"
                        );
                    }
                    
                    if (package != null)
                    {
                        // Get version from loaded package
                        var versionProp = nugetResourceType.GetProperty("Version");
                        var loadedVersion = versionProp?.GetValue(package)?.ToString() ?? "unknown";
                        
                        _logger.LogInformation("        ✓ Loaded: {PackageId} v{Version}", dep.PackageId, loadedVersion);
                        
                        if (!string.IsNullOrWhiteSpace(dep.Reason))
                        {
                            _logger.LogDebug("          Reason: {Reason}", dep.Reason);
                        }
                    }
                    else if (dep.Optional)
                    {
                        _logger.LogInformation("        ⊘ Optional package not found: {PackageId}", dep.PackageId);
                    }
                }
                catch (Exception ex)
                {
                    if (dep.Optional)
                    {
                        _logger.LogWarning(ex, "        ⊘ Failed to load optional NuGet package '{PackageId}': {Message}", dep.PackageId, ex.Message);
                    }
                    else
                    {
                        _logger.LogError(ex, "        ✗ Failed to load required NuGet package '{PackageId}': {Message}", dep.PackageId, ex.Message);
                        throw new InvalidOperationException(
                            $"Failed to load required NuGet package '{dep.PackageId}' for plugin '{pluginId}'",
                            ex
                        );
                    }
                }
            }
            
            _logger.LogInformation("    ✓ NuGet dependencies loaded for {PluginId}", pluginId);
        }
        catch (Exception ex) when (ex is not InvalidOperationException)
        {
            _logger.LogError(ex, "    ✗ Error loading NuGet dependencies for {PluginId}: {Message}", pluginId, ex.Message);
            // Don't throw - let plugin loading continue
        }
    }
}

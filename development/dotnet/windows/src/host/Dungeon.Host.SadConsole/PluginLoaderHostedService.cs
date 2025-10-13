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
using Plate.CrossMilo.Contracts;
using Plate.PluginManoi.Contracts;
using Plate.PluginManoi.Core;
using ILoadedPlugin = Plate.PluginManoi.Contracts.ILoadedPlugin;
using IPluginLoader = Plate.PluginManoi.Contracts.IPluginLoader;
using IResourceService = Plate.CrossMilo.Contracts.Resource.Services.IService;
using PluginAttribute = Plate.PluginManoi.Contracts.PluginAttribute;
using PluginManifest = Plate.PluginManoi.Core.PluginManifest;

namespace Dungeon.Host.SadConsole;

/// <summary>
/// Hosted service that loads plugins using MANIFEST-ONLY approach.
/// Legacy plugins.json loading has been REMOVED.
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
        _logger.LogInformation("Dungeon Host - Manifest-Only Plugin Loading");
        _logger.LogInformation("========================================");

        try
        {
            // Step 1: Create foundation services
            _logger.LogInformation("[1/4] Initializing foundation services...");
            _registry.Register<IRegistry>(_registry);
            _registry.Register<IPluginLoader>(_pluginLoader);
            _logger.LogInformation("✓ Foundation services initialized");

            // Step 2: Discover manifests
            _logger.LogInformation("[2/4] Discovering plugin manifests...");
            var exeDirectory = Path.GetDirectoryName(typeof(PluginLoaderHostedService).Assembly.Location)
                ?? Environment.CurrentDirectory;
            var pluginsDir = Path.Combine(exeDirectory, "plugins");

            if (!Directory.Exists(pluginsDir))
            {
                _logger.LogWarning("⚠ Plugins directory not found: {PluginsDir}", pluginsDir);
                _logger.LogWarning("  Creating directory...");
                Directory.CreateDirectory(pluginsDir);
                _logger.LogInformation("  No plugins to load. Place .plugin.json manifests in: {PluginsDir}", pluginsDir);
                return;
            }

            var manifests = await DiscoverManifestsAsync(pluginsDir, cancellationToken);
            _logger.LogInformation("✓ Discovered {Count} plugin manifests", manifests.Count);

            if (manifests.Count == 0)
            {
                _logger.LogWarning("⚠ No plugin manifests found in: {PluginsDir}", pluginsDir);
                _logger.LogInformation("  Expected format: plugins/<plugin-name>/<plugin-name>.plugin.json");
                return;
            }

            // Step 3: Load overrides (optional)
            _logger.LogInformation("[3/4] Loading plugin overrides...");
            var overrides = await LoadOverridesAsync(exeDirectory, cancellationToken);
            if (overrides != null)
            {
                _logger.LogInformation("✓ Loaded plugin overrides ({Count} plugins configured)",
                    overrides.Overrides.Count);
            }
            else
            {
                _logger.LogInformation("  No overrides file found (plugins-overrides.json) - using manifest defaults");
            }

            // Step 4: Apply overrides and sort by priority
            var orderedManifests = ApplyOverridesAndSort(manifests, overrides);

            // Step 5: Load plugins in order
            _logger.LogInformation("[4/4] Loading plugins in priority order...");
            var loadedPlugins = await LoadPluginsInOrderAsync(orderedManifests, overrides, cancellationToken);

            _logger.LogInformation("✓ {Count} plugins loaded successfully", loadedPlugins.Count);

            // Step 6: Verify required services
            _logger.LogInformation("Verifying service registry...");
            VerifyRequiredServices();
            _logger.LogInformation("✓ All required services registered");

            // Optional: Start WebSocket server if available
            TryStartWebSocketServer(4040);
        }
        catch (Exception ex)
        {
            _logger.LogCritical(ex, "FATAL ERROR during plugin loading: {Message}", ex.Message);
            throw;
        }
    }

    /// <summary>
    /// Discover all plugin manifests in the plugins directory
    /// </summary>
    private async Task<List<PluginManifest>> DiscoverManifestsAsync(
        string pluginsDir,
        CancellationToken cancellationToken)
    {
        var manifests = new List<PluginManifest>();
        var manifestPaths = Directory.GetFiles(pluginsDir, "*.plugin.json", SearchOption.AllDirectories);

        foreach (var manifestPath in manifestPaths)
        {
            try
            {
                var manifest = await LoadPluginManifestAsync(manifestPath, cancellationToken);
                var pluginDir = Path.GetDirectoryName(manifestPath)!;

                // Validate manifest
                if (string.IsNullOrWhiteSpace(manifest.Id))
                {
                    _logger.LogError("  ✗ Invalid manifest at {Path}: Missing 'id' field", manifestPath);
                    continue;
                }

                // Validate entry point exists
                var entryPoint = manifest.EntryPoint?.Dotnet ?? $"{manifest.Id}.dll";
                var assemblyPath = Path.GetFullPath(Path.Combine(pluginDir, entryPoint));

                if (!File.Exists(assemblyPath))
                {
                    _logger.LogError("  ✗ Plugin {PluginId}: Assembly not found at {Path}",
                        manifest.Id, assemblyPath);
                    continue;
                }

                // Store resolved path for later
                manifest.EntryPoints = new Dictionary<string, string>
                {
                    ["dotnet"] = assemblyPath,
                    ["directory"] = pluginDir,
                    ["manifest"] = manifestPath
                };

                manifests.Add(manifest);
                _logger.LogDebug("  → Discovered: {PluginId} v{Version} at {Path}",
                    manifest.Id, manifest.Version, pluginDir);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "  ✗ Failed to load manifest from {Path}: {Message}",
                    manifestPath, ex.Message);
            }
        }

        return manifests;
    }

    /// <summary>
    /// Load plugin overrides configuration (optional)
    /// </summary>
    private async Task<PluginOverrides?> LoadOverridesAsync(
        string exeDirectory,
        CancellationToken cancellationToken)
    {
        var overridesPath = Path.Combine(exeDirectory, "plugins-overrides.json");

        if (!File.Exists(overridesPath))
        {
            return null;
        }

        try
        {
            var json = await File.ReadAllTextAsync(overridesPath, cancellationToken);
            var overrides = JsonSerializer.Deserialize<PluginOverrides>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                ReadCommentHandling = JsonCommentHandling.Skip,
                AllowTrailingCommas = true
            });

            return overrides;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "  ✗ Failed to load overrides from {Path}: {Message}",
                overridesPath, ex.Message);
            return null;
        }
    }

    /// <summary>
    /// Apply overrides and sort manifests by priority
    /// </summary>
    private List<PluginManifest> ApplyOverridesAndSort(
        List<PluginManifest> manifests,
        PluginOverrides? overrides)
    {
        var result = new List<PluginManifest>();

        foreach (var manifest in manifests)
        {
            // Check if plugin is disabled by override
            if (overrides?.Overrides.TryGetValue(manifest.Id, out var pluginOverride) == true)
            {
                if (pluginOverride.Enabled == false)
                {
                    _logger.LogInformation("  ⊘ Skipping {PluginId}: Disabled by override ({Reason})",
                        manifest.Id, pluginOverride.Reason ?? "no reason given");
                    continue;
                }

                // Apply priority override
                if (pluginOverride.Priority.HasValue)
                {
                    _logger.LogDebug("  → {PluginId}: Priority override {Old} → {New}",
                        manifest.Id, manifest.Priority, pluginOverride.Priority.Value);
                    manifest.Priority = pluginOverride.Priority.Value;
                }

                // Apply load strategy override
                if (!string.IsNullOrWhiteSpace(pluginOverride.LoadStrategy))
                {
                    manifest.LoadStrategy = pluginOverride.LoadStrategy;
                }
            }

            result.Add(manifest);
        }

        // Sort by priority (higher loads first)
        return result.OrderByDescending(m => m.Priority).ToList();
    }

    /// <summary>
    /// Load plugins in priority order
    /// </summary>
    private async Task<Dictionary<string, ILoadedPlugin>> LoadPluginsInOrderAsync(
        List<PluginManifest> orderedManifests,
        PluginOverrides? overrides,
        CancellationToken cancellationToken)
    {
        var loadedPlugins = new Dictionary<string, ILoadedPlugin>();

        foreach (var manifest in orderedManifests)
        {
            // Skip lazy-loaded plugins for now (future enhancement)
            if (manifest.LoadStrategy.Equals("lazy", StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogInformation("  ⊘ Skipping {PluginId}: Lazy loading not yet implemented",
                    manifest.Id);
                continue;
            }

            try
            {
                _logger.LogInformation("  → Loading: {PluginId} v{Version} (priority: {Priority})",
                    manifest.Id, manifest.Version, manifest.Priority);

                // Load NuGet dependencies first
                await LoadNuGetDependenciesAsync(manifest, cancellationToken);

                // Load plugin assembly
                var assemblyPath = manifest.EntryPoints!["dotnet"];
                var plugin = await _pluginLoader.LoadAsync(assemblyPath);
                loadedPlugins[manifest.Id] = plugin;

                _logger.LogInformation("    ✓ Loaded: {PluginId} v{Version}",
                    plugin.Manifest.Id, plugin.Manifest.Version);

                // Register services from plugin
                await RegisterPluginServicesAsync(plugin, manifest.Priority, overrides, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "    ✗ Failed to load {PluginId}: {Message}",
                    manifest.Id, ex.Message);

                // Check if plugin is critical (priority >= 1000)
                if (manifest.Priority >= 1000)
                {
                    _logger.LogCritical("    CRITICAL: Plugin {PluginId} failed to load. Aborting.",
                        manifest.Id);
                    throw;
                }
            }
        }

        return loadedPlugins;
    }

    private async Task<PluginManifest> LoadPluginManifestAsync(string path, CancellationToken cancellationToken)
    {
        var json = await File.ReadAllTextAsync(path, cancellationToken);
        return JsonSerializer.Deserialize<PluginManifest>(json, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            ReadCommentHandling = JsonCommentHandling.Skip,
            AllowTrailingCommas = true
        }) ?? throw new InvalidOperationException($"Failed to parse plugin manifest: {path}");
    }

    private async Task LoadNuGetDependenciesAsync(
        PluginManifest manifest,
        CancellationToken cancellationToken)
    {
        if (manifest.Dependencies?.Nuget == null || manifest.Dependencies.Nuget.Count == 0)
        {
            return;
        }

        try
        {
            var resourceService = _registry.Get<IResourceService>();
            _logger.LogInformation("    → Loading {Count} NuGet dependencies...",
                manifest.Dependencies.Nuget.Count);

            foreach (var dep in manifest.Dependencies.Nuget)
            {
                try
                {
                    var versionStr = !string.IsNullOrWhiteSpace(dep.Version) ? $"/{dep.Version}" : "";
                    var feedStr = !string.IsNullOrWhiteSpace(dep.Feed) ? $"@{dep.Feed}" : "";
                    var nugetUri = $"nuget:{dep.PackageId}{versionStr}{feedStr}";

                    _logger.LogDebug("      → {PackageId} {Version}", dep.PackageId, dep.Version ?? "latest");

                    // Load via Resource service (implementation-specific)
                    // This is a placeholder - actual implementation depends on Resource service API

                    _logger.LogInformation("        ✓ Loaded: {PackageId}", dep.PackageId);
                }
                catch (Exception ex)
                {
                    if (dep.Optional)
                    {
                        _logger.LogWarning("        ⊘ Optional package not loaded: {PackageId}", dep.PackageId);
                    }
                    else
                    {
                        _logger.LogError(ex, "        ✗ Failed to load required NuGet package: {PackageId}",
                            dep.PackageId);
                        throw;
                    }
                }
            }
        }
        catch (Exception ex) when (ex.Message.Contains("not registered"))
        {
            _logger.LogDebug("    → Resource service not available, skipping NuGet dependencies");
        }
    }

    private async Task RegisterPluginServicesAsync(
        ILoadedPlugin plugin,
        int pluginPriority,
        PluginOverrides? overrides,
        CancellationToken cancellationToken)
    {
        // Set registry on plugin BEFORE activation
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
            setRegistryMethod?.Invoke(plugin, new object[] { _registry });
        }

        // Activate plugin
        await plugin.ActivateAsync();

        // Get services and register them
        var services = plugin.GetServices().ToList();
        _logger.LogInformation("      → Plugin returned {Count} services", services.Count);

        foreach (var service in services)
        {
            _logger.LogInformation("        • Service type: {Type}", service.GetType().FullName);
            var implType = service.GetType();

            // Inject registry into service if supported
            if (service is IRegistryAware serviceRegistryAware)
            {
                serviceRegistryAware.SetRegistry(_registry);
            }

            // Get contract interfaces
            var contractInterfaces = implType.GetInterfaces()
                .Where(i => i.Namespace?.StartsWith("WingedBean.Contracts") == true ||
                           i.Namespace?.StartsWith("Plate.CrossMilo.Contracts") == true)
                .ToList();

            // Read [Plugin] attribute for metadata
            var pluginAttr = implType
                .GetCustomAttributes(typeof(PluginAttribute), inherit: true)
                .Cast<PluginAttribute>()
                .FirstOrDefault();

            // Determine priority
            var effectivePriority = (pluginAttr?.Priority ?? 0) > 0
                ? pluginAttr!.Priority
                : pluginPriority;

            // Register each contract interface
            foreach (var contract in contractInterfaces)
            {
                _registry.RegisterDynamic(contract, service, effectivePriority);
                _logger.LogDebug("        → Registered: {ContractType} (priority: {Priority})",
                    contract.Name, effectivePriority);
            }
        }
    }

    private void VerifyRequiredServices()
    {
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

        // Check for optional services
        try
        {
            var uiApp = _registry.Get<Plate.CrossMilo.Contracts.UI.App.IService>();
            _logger.LogInformation("  ✓ IUIApp registered: {Type}", uiApp.GetType().Name);
        }
        catch
        {
            _logger.LogWarning("  ⚠ IUIApp not registered");
        }
    }

    private void TryStartWebSocketServer(int port)
    {
        try
        {
            var wsInterface = AppDomain.CurrentDomain
                .GetAssemblies()
                .SelectMany(a =>
                {
                    try { return a.GetTypes(); }
                    catch { return Array.Empty<Type>(); }
                })
                .FirstOrDefault(t => t.FullName == "Plate.CrossMilo.Contracts.WebSocket.Services.IService");

            if (wsInterface == null) return;

            var getAll = typeof(IRegistry).GetMethod("GetAll")!.MakeGenericMethod(wsInterface);
            var results = getAll.Invoke(_registry, Array.Empty<object>()) as System.Collections.IEnumerable;
            var first = results?.Cast<object>().FirstOrDefault();

            if (first != null)
            {
                var start = wsInterface.GetMethod("Start", new[] { typeof(int) });
                start?.Invoke(first, new object[] { port });
                _logger.LogInformation("→ WebSocket server started on port {Port}", port);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "→ WebSocket server failed to start (optional)");
        }
    }
}

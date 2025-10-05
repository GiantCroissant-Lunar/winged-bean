using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using NuGet.Versioning;

namespace WingedBean.PluginSystem;

/// <summary>
/// Orchestrates the plugin system startup: discovery, loading, and activation with advanced features
/// </summary>
public class HostBootstrap
{
    private readonly IPluginLoader _pluginLoader;
    private readonly PluginDiscovery _discovery;
    private readonly PluginDependencyResolver _resolver;
    private readonly ServiceCollection _services;
    private readonly List<ILoadedPlugin> _loadedPlugins;
    private readonly ILogger<HostBootstrap>? _logger;
    private readonly NuGetVersion _hostVersion;
    private IPluginRegistry? _pluginRegistry;
    private IPluginSignatureVerifier? _signatureVerifier;
    private IPluginUpdateManager? _updateManager;
    private IPluginPermissionEnforcer? _permissionEnforcer;

    /// <summary>
    /// Initialize host bootstrap
    /// </summary>
    /// <param name="pluginLoader">Plugin loader implementation</param>
    /// <param name="hostVersion">Host version for compatibility checking</param>
    /// <param name="pluginDirectories">Directories to scan for plugins</param>
    public HostBootstrap(IPluginLoader pluginLoader, string hostVersion = "1.0.0", params string[] pluginDirectories)
    {
        _pluginLoader = pluginLoader;
        _hostVersion = VersionExtensions.ParseVersion(hostVersion);
        _discovery = new PluginDiscovery(pluginDirectories);
        _resolver = new PluginDependencyResolver();
        _services = new ServiceCollection();
        _loadedPlugins = new List<ILoadedPlugin>();
    }

    /// <summary>
    /// Initialize host bootstrap with logger
    /// </summary>
    /// <param name="pluginLoader">Plugin loader implementation</param>
    /// <param name="logger">Logger instance</param>
    /// <param name="hostVersion">Host version for compatibility checking</param>
    /// <param name="pluginDirectories">Directories to scan for plugins</param>
    public HostBootstrap(IPluginLoader pluginLoader, ILogger<HostBootstrap> logger, string hostVersion = "1.0.0", params string[] pluginDirectories)
    {
        _pluginLoader = pluginLoader;
        _logger = logger;
        _hostVersion = VersionExtensions.ParseVersion(hostVersion);
        // Note: We'll create separate loggers later when we have access to ILoggerFactory
        _discovery = new PluginDiscovery(pluginDirectories);
        _resolver = new PluginDependencyResolver();
        _services = new ServiceCollection();
        _loadedPlugins = new List<ILoadedPlugin>();
    }

    /// <summary>
    /// Boot the host with plugin system
    /// </summary>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Final service provider with all plugin services</returns>
    public async Task<IServiceProvider> BootAsync(CancellationToken ct = default)
    {
        _logger?.LogInformation("Starting host bootstrap");

        try
        {
            // 1. Register core host services
            RegisterHostServices();

            // 2. Discover plugins
            _logger?.LogInformation("Discovering plugins");
            var manifests = await _discovery.DiscoverPluginsAsync(ct);

            if (!manifests.Any())
            {
                _logger?.LogWarning("No plugins discovered");
                return _services.BuildServiceProvider();
            }

            // 3. Validate dependencies
            if (!_resolver.ValidateDependencies(manifests))
            {
                throw new InvalidOperationException("Plugin dependency validation failed");
            }

            // 4. Resolve dependency order with version compatibility
            _logger?.LogInformation("Resolving plugin load order with host version {HostVersion}", _hostVersion);
            var orderedManifests = _resolver.ResolveLoadOrder(manifests, _hostVersion);

            // 5. Verify plugin signatures and permissions
            _logger?.LogInformation("Verifying plugin security");
            await VerifyPluginSecurityAsync(orderedManifests, ct);

            // 6. Load plugins in dependency order
            _logger?.LogInformation("Loading plugins");
            foreach (var manifest in orderedManifests)
            {
                if (ct.IsCancellationRequested)
                    break;

                // Register plugin in registry
                if (_pluginRegistry != null)
                {
                    await _pluginRegistry.RegisterPluginAsync(manifest, ct);
                }

                var plugin = await _pluginLoader.LoadPluginAsync(manifest, ct);
                _loadedPlugins.Add(plugin);

                _logger?.LogInformation("Loaded plugin {PluginId} v{Version}", manifest.Id, manifest.Version);
            }

            // 7. Build intermediate service provider for plugin activation
            var hostServiceProvider = _services.BuildServiceProvider();

            // 8. Activate plugins in dependency order
            _logger?.LogInformation("Activating plugins");
            foreach (var plugin in _loadedPlugins)
            {
                if (ct.IsCancellationRequested)
                    break;

                // Register plugin permissions
                if (_permissionEnforcer != null && plugin.Manifest.Security?.Permissions != null)
                {
                    _permissionEnforcer.RegisterPermissions(plugin.Id, plugin.Manifest.Security.Permissions);
                }

                await plugin.ActivateAsync(hostServiceProvider, ct);

                // Register plugin services in main service collection
                foreach (var serviceDescriptor in plugin.Services)
                {
                    ((IList<ServiceDescriptor>)_services).Add(serviceDescriptor);
                }

                _logger?.LogInformation("Activated plugin {PluginId} v{Version}", plugin.Id, plugin.Manifest.Version);
            }

            // 9. Build final service provider with all plugin services
            var finalServiceProvider = _services.BuildServiceProvider();

            // 10. Initialize update manager
            _updateManager = finalServiceProvider.GetService<IPluginUpdateManager>();
            if (_updateManager != null)
            {
                // Subscribe to update events
                _updateManager.UpdateAvailable += OnPluginUpdateAvailable;
                _updateManager.UpdateCompleted += OnPluginUpdateCompleted;
                _updateManager.UpdateFailed += OnPluginUpdateFailed;
            }

            _logger?.LogInformation("Host initialized with {Count} plugins", _loadedPlugins.Count);

            return finalServiceProvider;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Host bootstrap failed");
            throw;
        }
    }

    /// <summary>
    /// Register core host services with advanced plugin features
    /// </summary>
    private void RegisterHostServices()
    {
        _logger?.LogDebug("Registering host services");

        // Register logging if not already registered
        if (!_services.Any(s => s.ServiceType == typeof(ILoggerFactory)))
        {
            _services.AddLogging(builder =>
            {
                builder.AddConsole();
                builder.SetMinimumLevel(LogLevel.Information);
            });
        }

        // Register plugin management services
        _services.AddSingleton(_pluginLoader);
        _services.AddSingleton<PluginRegistry>();

        // Register advanced plugin features
        var registryPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "WingedBean", "plugin-registry.json");
        _pluginRegistry = new FilePluginRegistry(registryPath);
        _services.AddSingleton(_pluginRegistry);

        _signatureVerifier = new RsaPluginSignatureVerifier();
        _services.AddSingleton(_signatureVerifier);

        _permissionEnforcer = new DefaultPluginPermissionEnforcer();
        _services.AddSingleton(_permissionEnforcer);

        // Update manager needs to be created after other services
        _services.AddSingleton<IPluginUpdateManager>(provider =>
        {
            var logger = provider.GetService<ILogger<PluginUpdateManager>>();
            return new PluginUpdateManager(_pluginLoader, _pluginRegistry, _signatureVerifier, logger);
        });

        // Register event bus for inter-plugin communication
        _services.AddSingleton<IEventBus, EventBus>();

        // Register host version for compatibility checks
        _services.AddSingleton(_hostVersion);

        _logger?.LogDebug("Host services registered with advanced features");
    }

    /// <summary>
    /// Get all loaded plugins
    /// </summary>
    public IReadOnlyList<ILoadedPlugin> LoadedPlugins => _loadedPlugins.AsReadOnly();

    /// <summary>
    /// Shutdown the host and unload all plugins
    /// </summary>
    /// <param name="ct">Cancellation token</param>
    public async Task ShutdownAsync(CancellationToken ct = default)
    {
        _logger?.LogInformation("Shutting down host");

        // Deactivate plugins in reverse order
        foreach (var plugin in _loadedPlugins.AsEnumerable().Reverse())
        {
            try
            {
                if (plugin.State == PluginState.Activated)
                {
                    await plugin.DeactivateAsync(ct);
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error deactivating plugin: {PluginId}", plugin.Id);
            }
        }

        // Unload all plugins
        foreach (var plugin in _loadedPlugins.ToList())
        {
            try
            {
                await _pluginLoader.UnloadPluginAsync(plugin, ct);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error unloading plugin: {PluginId}", plugin.Id);
            }
        }

        _loadedPlugins.Clear();
        _logger?.LogInformation("Host shutdown complete");
    }

    /// <summary>
    /// Verify plugin signatures and security permissions
    /// </summary>
    private async Task VerifyPluginSecurityAsync(IEnumerable<PluginManifest> manifests, CancellationToken ct)
    {
        if (_signatureVerifier == null)
            return;

        foreach (var manifest in manifests)
        {
            if (manifest.Security?.Signature != null)
            {
                var pluginPath = GetPluginPath(manifest);
                var isSignatureValid = await _signatureVerifier.VerifySignatureAsync(manifest, pluginPath, ct);

                if (!isSignatureValid)
                {
                    var securityLevel = manifest.Security.SecurityLevel;
                    if (securityLevel == SecurityLevel.Restricted || securityLevel == SecurityLevel.Isolated)
                    {
                        throw new InvalidOperationException($"Plugin {manifest.Id} signature verification failed and security level requires valid signature");
                    }

                    _logger?.LogWarning("Plugin {PluginId} signature verification failed, but security level allows loading", manifest.Id);
                }
                else
                {
                    _logger?.LogInformation("Plugin {PluginId} signature verified successfully", manifest.Id);
                }
            }
            else if (manifest.Security?.SecurityLevel == SecurityLevel.Restricted || manifest.Security?.SecurityLevel == SecurityLevel.Isolated)
            {
                throw new InvalidOperationException($"Plugin {manifest.Id} requires signature but none provided");
            }
        }
    }

    /// <summary>
    /// Get plugin path for a manifest
    /// </summary>
    private string GetPluginPath(PluginManifest manifest)
    {
        // Simple implementation - in production this would be more sophisticated
        return Path.Combine("plugins", manifest.Id);
    }

    /// <summary>
    /// Handle plugin update available event
    /// </summary>
    private void OnPluginUpdateAvailable(object? sender, PluginUpdateAvailableEventArgs e)
    {
        _logger?.LogInformation("Update available for plugin {PluginId}: {CurrentVersion} -> {AvailableVersion}",
            e.PluginId, e.CurrentVersion, e.AvailableVersion);

        if (e.IsAutoUpdateEnabled)
        {
            _logger?.LogInformation("Auto-update enabled for plugin {PluginId}, scheduling update", e.PluginId);
            // In a real implementation, this would schedule the update
        }
    }

    /// <summary>
    /// Handle plugin update completed event
    /// </summary>
    private void OnPluginUpdateCompleted(object? sender, PluginUpdateEventArgs e)
    {
        _logger?.LogInformation("Plugin {PluginId} successfully updated from {FromVersion} to {ToVersion}",
            e.PluginId, e.FromVersion, e.ToVersion);
    }

    /// <summary>
    /// Handle plugin update failed event
    /// </summary>
    private void OnPluginUpdateFailed(object? sender, PluginUpdateErrorEventArgs e)
    {
        _logger?.LogError(e.Exception, "Plugin {PluginId} update failed: {FromVersion} -> {ToVersion}: {ErrorMessage}",
            e.PluginId, e.FromVersion, e.ToVersion, e.ErrorMessage);
    }

    /// <summary>
    /// Get plugin statistics
    /// </summary>
    public async Task<PluginStatistics?> GetPluginStatisticsAsync(CancellationToken ct = default)
    {
        return _pluginRegistry != null ? await _pluginRegistry.GetStatisticsAsync(ct) : null;
    }

    /// <summary>
    /// Check for plugin updates
    /// </summary>
    public async Task CheckForUpdatesAsync(CancellationToken ct = default)
    {
        if (_updateManager == null)
            return;

        foreach (var plugin in _loadedPlugins)
        {
            try
            {
                await _updateManager.CheckForUpdatesAsync(plugin.Id, ct);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Failed to check for updates for plugin {PluginId}", plugin.Id);
            }
        }
    }
}

/// <summary>
/// Simple event bus for inter-plugin communication
/// </summary>
public interface IEventBus
{
    /// <summary>
    /// Publish an event to all subscribers
    /// </summary>
    /// <typeparam name="T">Event type</typeparam>
    /// <param name="eventData">Event data</param>
    Task PublishAsync<T>(T eventData) where T : class;

    /// <summary>
    /// Subscribe to events of a specific type
    /// </summary>
    /// <typeparam name="T">Event type</typeparam>
    /// <param name="handler">Event handler</param>
    void Subscribe<T>(Func<T, Task> handler) where T : class;
}

/// <summary>
/// Simple in-memory event bus implementation
/// </summary>
public class EventBus : IEventBus
{
    private readonly Dictionary<Type, List<Func<object, Task>>> _handlers = new();
    private readonly ILogger<EventBus>? _logger;

    public EventBus(ILogger<EventBus>? logger = null)
    {
        _logger = logger;
    }

    public async Task PublishAsync<T>(T eventData) where T : class
    {
        if (_handlers.TryGetValue(typeof(T), out var handlers))
        {
            _logger?.LogDebug("Publishing event {EventType} to {HandlerCount} handlers", typeof(T).Name, handlers.Count);

            var tasks = handlers.Select(handler => handler(eventData));
            await Task.WhenAll(tasks);
        }
    }

    public void Subscribe<T>(Func<T, Task> handler) where T : class
    {
        if (!_handlers.ContainsKey(typeof(T)))
        {
            _handlers[typeof(T)] = new List<Func<object, Task>>();
        }

        _handlers[typeof(T)].Add(async obj => await handler((T)obj));
        _logger?.LogDebug("Subscribed handler for event type {EventType}", typeof(T).Name);
    }
}

/// <summary>
/// Plugin registry for managing loaded plugin metadata
/// </summary>
public class PluginRegistry
{
    private readonly Dictionary<string, PluginManifest> _manifests = new();
    private readonly ILogger<PluginRegistry>? _logger;

    public PluginRegistry(ILogger<PluginRegistry>? logger = null)
    {
        _logger = logger;
    }

    /// <summary>
    /// Register a plugin manifest
    /// </summary>
    /// <param name="manifest">Plugin manifest</param>
    public void Register(PluginManifest manifest)
    {
        _manifests[manifest.Id] = manifest;
        _logger?.LogDebug("Registered plugin manifest: {PluginId}", manifest.Id);
    }

    /// <summary>
    /// Get a plugin manifest by ID
    /// </summary>
    /// <param name="pluginId">Plugin ID</param>
    /// <returns>Plugin manifest or null if not found</returns>
    public PluginManifest? GetManifest(string pluginId)
    {
        return _manifests.TryGetValue(pluginId, out var manifest) ? manifest : null;
    }

    /// <summary>
    /// Get all registered plugin manifests
    /// </summary>
    /// <returns>Collection of plugin manifests</returns>
    public IEnumerable<PluginManifest> GetAllManifests()
    {
        return _manifests.Values;
    }
}

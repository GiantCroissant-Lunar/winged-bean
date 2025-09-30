using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;

namespace WingedBean.Host;

/// <summary>
/// Orchestrates the plugin system startup: discovery, loading, and activation
/// </summary>
public class HostBootstrap
{
    private readonly IPluginLoader _pluginLoader;
    private readonly PluginDiscovery _discovery;
    private readonly PluginDependencyResolver _resolver;
    private readonly ServiceCollection _services;
    private readonly List<ILoadedPlugin> _loadedPlugins;
    private readonly ILogger<HostBootstrap>? _logger;

    /// <summary>
    /// Initialize host bootstrap
    /// </summary>
    /// <param name="pluginLoader">Plugin loader implementation</param>
    /// <param name="pluginDirectories">Directories to scan for plugins</param>
    public HostBootstrap(IPluginLoader pluginLoader, params string[] pluginDirectories)
    {
        _pluginLoader = pluginLoader;
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
    /// <param name="pluginDirectories">Directories to scan for plugins</param>
    public HostBootstrap(IPluginLoader pluginLoader, ILogger<HostBootstrap> logger, params string[] pluginDirectories)
    {
        _pluginLoader = pluginLoader;
        _logger = logger;
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

            // 4. Resolve dependency order
            _logger?.LogInformation("Resolving plugin load order");
            var orderedManifests = _resolver.ResolveLoadOrder(manifests);

            // 5. Load plugins in dependency order
            _logger?.LogInformation("Loading plugins");
            foreach (var manifest in orderedManifests)
            {
                if (ct.IsCancellationRequested)
                    break;

                var plugin = await _pluginLoader.LoadPluginAsync(manifest, ct);
                _loadedPlugins.Add(plugin);
            }

            // 6. Build intermediate service provider for plugin activation
            var hostServiceProvider = _services.BuildServiceProvider();

            // 7. Activate plugins in dependency order
            _logger?.LogInformation("Activating plugins");
            foreach (var plugin in _loadedPlugins)
            {
                if (ct.IsCancellationRequested)
                    break;

                await plugin.ActivateAsync(hostServiceProvider, ct);
                
                // Register plugin services in main service collection
                foreach (var serviceDescriptor in plugin.Services)
                {
                    ((IList<ServiceDescriptor>)_services).Add(serviceDescriptor);
                }
            }

            // 8. Build final service provider with all plugin services
            var finalServiceProvider = _services.BuildServiceProvider();

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
    /// Register core host services
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

        // Register event bus for inter-plugin communication
        _services.AddSingleton<IEventBus, EventBus>();

        _logger?.LogDebug("Host services registered");
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

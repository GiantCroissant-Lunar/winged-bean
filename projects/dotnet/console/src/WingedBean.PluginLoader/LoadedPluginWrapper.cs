using System.Reflection;
using WingedBean.Contracts.Core;
using WingedBean.Providers.AssemblyContext;

namespace WingedBean.PluginLoader;

/// <summary>
/// Wrapper for a loaded plugin that implements ILoadedPlugin from WingedBean.Contracts.Core.
/// Tracks plugin state and manages lifecycle.
/// </summary>
internal class LoadedPluginWrapper : ILoadedPlugin
{
    private readonly Assembly _assembly;
    private readonly IPlugin? _pluginInstance;
    private readonly AssemblyContextProvider _contextProvider;
    private PluginState _state;
    private readonly Dictionary<Type, object> _services = new();
    private IRegistry? _registry;

    /// <summary>
    /// Unique plugin identifier.
    /// </summary>
    public string Id => Manifest.Id;

    /// <summary>
    /// Plugin version.
    /// </summary>
    public string Version => Manifest.Version;

    /// <summary>
    /// Plugin manifest metadata.
    /// </summary>
    public PluginManifest Manifest { get; }

    /// <summary>
    /// Current state of the plugin.
    /// </summary>
    public PluginState State => _state;

    /// <summary>
    /// Assembly context name for this plugin.
    /// </summary>
    internal string ContextName { get; }

    /// <summary>
    /// Initialize LoadedPluginWrapper.
    /// </summary>
    /// <param name="manifest">Plugin manifest</param>
    /// <param name="assembly">Loaded assembly</param>
    /// <param name="contextName">Assembly context name</param>
    /// <param name="pluginInstance">Plugin instance (if implements IPlugin)</param>
    /// <param name="contextProvider">Context provider for unloading</param>
    public LoadedPluginWrapper(
        PluginManifest manifest,
        Assembly assembly,
        string contextName,
        IPlugin? pluginInstance,
        AssemblyContextProvider contextProvider)
    {
        Manifest = manifest ?? throw new ArgumentNullException(nameof(manifest));
        _assembly = assembly ?? throw new ArgumentNullException(nameof(assembly));
        ContextName = contextName ?? throw new ArgumentNullException(nameof(contextName));
        _pluginInstance = pluginInstance;
        _contextProvider = contextProvider ?? throw new ArgumentNullException(nameof(contextProvider));
        _state = PluginState.Loaded;
    }

    /// <summary>
    /// Get a service provided by this plugin.
    /// </summary>
    /// <typeparam name="TService">Service type</typeparam>
    /// <returns>Service instance, or null if not found</returns>
    public TService? GetService<TService>() where TService : class
    {
        lock (_services)
        {
            if (_services.TryGetValue(typeof(TService), out var service))
            {
                return service as TService;
            }

            // Try to find and instantiate service from assembly
            var serviceType = _assembly.GetTypes()
                .FirstOrDefault(t => typeof(TService).IsAssignableFrom(t) && !t.IsInterface && !t.IsAbstract);

            if (serviceType != null)
            {
                var instance = Activator.CreateInstance(serviceType) as TService;
                if (instance != null)
                {
                    _services[typeof(TService)] = instance;
                    return instance;
                }
            }

            return null;
        }
    }

    /// <summary>
    /// Get all services provided by this plugin.
    /// </summary>
    /// <returns>Collection of service instances</returns>
    public IEnumerable<object> GetServices()
    {
        lock (_services)
        {
            return _services.Values.ToList();
        }
    }

    /// <summary>
    /// Activate the plugin (register services, initialize resources).
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    public async Task ActivateAsync(CancellationToken cancellationToken = default)
    {
        if (_state != PluginState.Loaded && _state != PluginState.Deactivated)
        {
            throw new InvalidOperationException($"Cannot activate plugin {Id} in state {_state}");
        }

        _state = PluginState.Activating;

        try
        {
            // If plugin implements IPlugin, call its OnActivateAsync
            if (_pluginInstance != null && _registry != null)
            {
                await _pluginInstance.OnActivateAsync(_registry, cancellationToken);
            }

            _state = PluginState.Activated;
        }
        catch
        {
            _state = PluginState.Failed;
            throw;
        }
    }

    /// <summary>
    /// Deactivate the plugin (cleanup, prepare for unload).
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    public async Task DeactivateAsync(CancellationToken cancellationToken = default)
    {
        if (_state != PluginState.Activated)
        {
            return; // Already deactivated or not activated
        }

        _state = PluginState.Deactivating;

        try
        {
            // If plugin implements IPlugin, call its OnDeactivateAsync
            if (_pluginInstance != null)
            {
                await _pluginInstance.OnDeactivateAsync(cancellationToken);
            }

            // Clear services
            lock (_services)
            {
                _services.Clear();
            }

            _state = PluginState.Deactivated;
        }
        catch
        {
            _state = PluginState.Failed;
            throw;
        }
    }

    /// <summary>
    /// Set the registry for plugin activation.
    /// </summary>
    /// <param name="registry">Service registry</param>
    internal void SetRegistry(IRegistry registry)
    {
        _registry = registry;
    }
}

using System.Runtime.Loader;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using WingedBean.PluginSystem;
using System.Reflection;

namespace WingedBean.Host.Console;

/// <summary>
/// Concrete implementation of ILoadedPlugin for .NET ALC-based loading
/// </summary>
public class LoadedPlugin : ILoadedPlugin
{
    private readonly IPluginActivator _activator;
    private readonly AssemblyLoadContext? _loadContext;
    private readonly Assembly _assembly;
    private readonly IServiceCollection _services;
    private PluginState _state;

    public string Id => Manifest.Id;
    public Version Version => System.Version.Parse(Manifest.Version);
    public PluginManifest Manifest { get; }
    public PluginState State => _state;
    public IServiceCollection Services => _services;

    public LoadedPlugin(PluginManifest manifest, IPluginActivator activator, AssemblyLoadContext? loadContext, Assembly assembly)
    {
        Manifest = manifest;
        _activator = activator;
        _loadContext = loadContext;
        _assembly = assembly;
        _services = new ServiceCollection();
        _state = PluginState.Loaded;
    }

    public async Task ActivateAsync(IServiceProvider hostServices, CancellationToken ct = default)
    {
        if (_state != PluginState.Loaded && _state != PluginState.Deactivated)
        {
            throw new InvalidOperationException($"Cannot activate plugin {Id} in state {_state}");
        }

        _state = PluginState.Activating;

        try
        {
            await _activator.ActivateAsync(_services, hostServices, ct);
            _state = PluginState.Activated;
        }
        catch
        {
            _state = PluginState.Failed;
            throw;
        }
    }

    public async Task DeactivateAsync(CancellationToken ct = default)
    {
        if (_state != PluginState.Activated)
        {
            return; // Already deactivated or not activated
        }

        _state = PluginState.Deactivating;

        try
        {
            await _activator.DeactivateAsync(ct);
            _state = PluginState.Deactivated;
        }
        catch
        {
            _state = PluginState.Failed;
            throw;
        }
    }

    /// <summary>
    /// Get the load context for this plugin (for unloading)
    /// </summary>
    internal AssemblyLoadContext? LoadContext => _loadContext;

    /// <summary>
    /// The plugin's main assembly (for scanning/DI helpers).
    /// </summary>
    public Assembly Assembly => _assembly;
}

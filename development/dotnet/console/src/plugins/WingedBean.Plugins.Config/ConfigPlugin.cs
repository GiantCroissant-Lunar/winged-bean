using Plate.PluginManoi.Contracts;
using Plate.CrossMilo.Contracts.Config.Services;

namespace WingedBean.Plugins.Config;

/// <summary>
/// Legacy plugin interface implementation for backward compatibility with the current host.
/// This bridges the gap between the new IPluginActivator pattern and the old IPlugin pattern.
/// </summary>
public class ConfigPlugin : IPlugin
{
    private ConfigService? _serviceInstance;

    public string Id => "wingedbean.plugins.config";
    public string Version => "1.0.0";

    public Task OnActivateAsync(IRegistry registry, CancellationToken ct = default)
    {
        // Create the config service instance using parameterless constructor
        _serviceInstance = new ConfigService();
        
        // Register the service directly with the registry (priority 100 from Plugin attribute)
        registry.Register<IService>(_serviceInstance, priority: 100);
        
        return Task.CompletedTask;
    }

    public Task OnDeactivateAsync(CancellationToken ct = default)
    {
        // Config service doesn't have explicit cleanup
        return Task.CompletedTask;
    }

    public IEnumerable<object> GetServices()
    {
        if (_serviceInstance != null)
        {
            yield return _serviceInstance;
        }
    }
}

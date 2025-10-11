using Plate.PluginManoi.Contracts;
using Plate.CrossMilo.Contracts;
using Plate.CrossMilo.Contracts.ECS.Services;

namespace WingedBean.Plugins.ArchECS;

/// <summary>
/// Legacy plugin interface implementation for backward compatibility with the current host.
/// This bridges the gap between the new IPluginActivator pattern and the old IPlugin pattern.
/// </summary>
public class ArchECSPlugin : IPlugin
{
    private ArchECSService? _serviceInstance;

    public string Id => "wingedbean.plugins.archecs";
    public string Version => "1.0.0";

    public Task OnActivateAsync(IRegistry registry, CancellationToken ct = default)
    {
        // Create the ECS service instance
        _serviceInstance = new ArchECSService();
        
        // Register the service directly with the registry (priority 100 from Plugin attribute)
        registry.Register<IService>(_serviceInstance, priority: 100);
        
        return Task.CompletedTask;
    }

    public Task OnDeactivateAsync(CancellationToken ct = default)
    {
        // ECS service doesn't have explicit cleanup
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

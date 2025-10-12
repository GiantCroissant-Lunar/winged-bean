using Plate.CrossMilo.Contracts;
using Plate.PluginManoi.Contracts;

namespace WingedBean.Plugins.ArchECS;

/// <summary>
/// Legacy plugin interface implementation for backward compatibility with the current host.
/// This bridges the gap between the new IPluginActivator pattern and the old IPlugin pattern.
/// </summary>
[Plugin(
    Name = "ArchECSService",
    Provides = new[] { typeof(ArchECSService) },
    Priority = 100
)]
public class ArchECSPlugin : IPlugin
{
    private ArchECSService? _serviceInstance;

    public string Id => "wingedbean.plugins.archecs";
    public string Version => "1.0.0";

    public Task OnActivateAsync(IRegistry registry, CancellationToken ct = default)
    {
        Console.WriteLine("DEBUG: ArchECSPlugin.OnActivateAsync called!");

        // Create the ECS service instance
        _serviceInstance = new ArchECSService();
        Console.WriteLine("DEBUG: Created ArchECSService instance");

        // Register the service directly with the registry
        registry.Register<ArchECSService>(_serviceInstance, priority: 100);
        Console.WriteLine("DEBUG: Registered ArchECSService in registry");

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

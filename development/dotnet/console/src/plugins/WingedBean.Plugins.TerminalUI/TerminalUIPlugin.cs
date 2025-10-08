using Plate.PluginManoi.Contracts;
using Plate.CrossMilo.Contracts.TerminalUI.Services;

namespace WingedBean.Plugins.TerminalUI;

/// <summary>
/// Legacy plugin interface implementation for backward compatibility with the current host.
/// This bridges the gap between the new IPluginActivator pattern and the old IPlugin pattern.
/// </summary>
public class TerminalUIPlugin : IPlugin
{
    private TerminalGuiService? _serviceInstance;

    public string Id => "winged-bean.terminal-ui";
    public string Version => "1.0.0";

    public Task OnActivateAsync(IRegistry registry, CancellationToken ct = default)
    {
        // Create the terminal UI service instance
        _serviceInstance = new TerminalGuiService();
        
        // Register the service directly with the registry (priority 100 from Plugin attribute)
        registry.Register<IService>(_serviceInstance, priority: 100);
        
        return Task.CompletedTask;
    }

    public Task OnDeactivateAsync(CancellationToken ct = default)
    {
        _serviceInstance?.Shutdown();
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

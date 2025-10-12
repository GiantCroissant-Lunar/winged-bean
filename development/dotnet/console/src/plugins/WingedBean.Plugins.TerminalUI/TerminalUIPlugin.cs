using Plate.CrossMilo.Contracts;
using Plate.PluginManoi.Contracts;
using ConsoleDungeon.Contracts;
using WingedBeanGameplay = WingedBean.Contracts.Gameplay.IService;
using WingedBean.Contracts.Gameplay;

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
        IDungeonService? dungeon = null;
        WingedBeanGameplay? gameplay = null;
        IWorldSnapshotProvider? provider = null;
        try { dungeon = registry.Get<IDungeonService>(); } catch { }
        try { gameplay = registry.Get<WingedBeanGameplay>(); } catch { }
        try { provider = registry.Get<IWorldSnapshotProvider>(); } catch { }
        _serviceInstance = new TerminalGuiService(dungeon, gameplay, provider);

        // Register the service directly with the registry (priority 100 from Plugin attribute)
        registry.Register<ITerminalUIService>(_serviceInstance, priority: 100);

        return Task.CompletedTask;
    }

    public Task OnDeactivateAsync(CancellationToken ct = default)
    {
        // Shutdown is handled by Application.RequestStop() in UI menu
        // _serviceInstance?.Shutdown(); // Method removed from IService interface
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

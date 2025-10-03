using WingedBean.Contracts.Core;
using WingedBean.Contracts.Game;

namespace WingedBean.Plugins.DungeonGame;

/// <summary>
/// Plugin that provides dungeon crawler game logic with ECS and reactive patterns.
/// Implements IPlugin for the Registry-based plugin system.
/// </summary>
[Plugin(
    Name = "DungeonGameService",
    Provides = new[] { typeof(IDungeonGameService) },
    Priority = 100
)]
public class DungeonGamePlugin : IPlugin
{
    private DungeonGameService? _service;

    public string Id => "wingedbean.plugins.dungeongame";
    public string Version => "1.0.0";

    public async Task OnActivateAsync(IRegistry registry, CancellationToken ct = default)
    {
        System.Console.WriteLine($"      [DungeonGamePlugin] OnActivateAsync called");
        _service = new DungeonGameService(registry);
        System.Console.WriteLine($"      [DungeonGamePlugin] Created DungeonGameService instance");
        registry.Register<IDungeonGameService>(_service);
        System.Console.WriteLine($"      [DungeonGamePlugin] Registered IDungeonGameService");
        await Task.CompletedTask;
    }

    public async Task OnDeactivateAsync(CancellationToken ct = default)
    {
        _service?.Shutdown();
        await Task.CompletedTask;
    }

    public IEnumerable<object> GetServices()
    {
        if (_service != null)
        {
            yield return _service;
        }
    }
}

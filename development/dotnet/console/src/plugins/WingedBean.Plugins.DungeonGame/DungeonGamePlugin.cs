using Plate.PluginManoi.Contracts;
using Plate.CrossMilo.Contracts.Game.Dungeon;
using Plate.CrossMilo.Contracts.Game.Render;
using WingedBean.Plugins.DungeonGame.Services;

namespace WingedBean.Plugins.DungeonGame;

/// <summary>
/// Plugin that provides dungeon crawler game logic with ECS and reactive patterns.
/// Implements IPlugin for the Registry-based plugin system.
/// </summary>
[Plugin(
    Name = "DungeonGameService",
    Provides = new[] { typeof(Plate.CrossMilo.Contracts.Game.Dungeon.IService), typeof(Plate.CrossMilo.Contracts.Game.Render.IService) },
    Priority = 100
)]
public class DungeonGamePlugin : IPlugin
{
    private DungeonGameService? _dungeonService;
    private RenderServiceProvider? _renderService;

    public string Id => "wingedbean.plugins.dungeongame";
    public string Version => "1.0.0";

    public async Task OnActivateAsync(IRegistry registry, CancellationToken ct = default)
    {
        System.Console.WriteLine($"      [DungeonGamePlugin] OnActivateAsync called");
        
        // Register Dungeon Game service
        _dungeonService = new DungeonGameService(registry);
        System.Console.WriteLine($"      [DungeonGamePlugin] Created DungeonGameService instance");
        registry.Register<Plate.CrossMilo.Contracts.Game.Dungeon.IService>(_dungeonService, priority: 100);
        System.Console.WriteLine($"      [DungeonGamePlugin] Registered IService (Game.Dungeon)");
        
        // Register Render service
        _renderService = new RenderServiceProvider();
        System.Console.WriteLine($"      [DungeonGamePlugin] Created RenderServiceProvider instance");
        registry.Register<Plate.CrossMilo.Contracts.Game.Render.IService>(_renderService, priority: 100);
        System.Console.WriteLine($"      [DungeonGamePlugin] Registered IService (Game.Render)");
        
        await Task.CompletedTask;
    }

    public async Task OnDeactivateAsync(CancellationToken ct = default)
    {
        _dungeonService?.Shutdown();
        await Task.CompletedTask;
    }

    public IEnumerable<object> GetServices()
    {
        if (_dungeonService != null)
        {
            yield return _dungeonService;
        }
        if (_renderService != null)
        {
            yield return _renderService;
        }
    }
}

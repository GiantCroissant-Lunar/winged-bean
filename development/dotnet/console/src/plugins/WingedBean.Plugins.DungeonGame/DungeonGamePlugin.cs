using Microsoft.Extensions.Logging;
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
    private ILogger? _logger;

    public string Id => "wingedbean.plugins.dungeongame";
    public string Version => "1.0.0";

    public async Task OnActivateAsync(IRegistry registry, CancellationToken ct = default)
    {
        // Try to get logger (optional)
        try
        {
            _logger = registry.Get<ILogger<DungeonGamePlugin>>();
        }
        catch
        {
            _logger = null;
        }

        _logger?.LogInformation("DungeonGamePlugin OnActivateAsync called");
        
        // Register Dungeon Game service
        _dungeonService = new DungeonGameService(registry);
        _logger?.LogDebug("Created DungeonGameService instance");
        registry.Register<Plate.CrossMilo.Contracts.Game.Dungeon.IService>(_dungeonService, priority: 100);
        _logger?.LogDebug("Registered IService (Game.Dungeon)");
        
        // Register Render service
        _renderService = new RenderServiceProvider();
        _logger?.LogDebug("Created RenderServiceProvider instance");
        registry.Register<Plate.CrossMilo.Contracts.Game.Render.IService>(_renderService, priority: 100);
        _logger?.LogDebug("Registered IService (Game.Render)");
        
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

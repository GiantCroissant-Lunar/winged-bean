using Microsoft.Extensions.Logging;
using Plate.PluginManoi.Contracts;
using Plate.CrossMilo.Contracts;
using ConsoleDungeon.Contracts;

namespace WingedBean.Plugins.DungeonGame;

/// <summary>
/// Plugin that provides dungeon crawler game logic with ECS and reactive patterns.
/// Implements IPlugin for the Registry-based plugin system.
/// </summary>
[Plugin(
    Name = "DungeonGameService",
    Provides = new[] { typeof(IDungeonService) },
    Priority = 100
)]
public class DungeonGamePlugin : IPlugin
{
    private DungeonGameService? _dungeonService;
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
        registry.Register<IDungeonService>(_dungeonService, priority: 100);
        _logger?.LogDebug("Registered IDungeonService");
        
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
    }
}

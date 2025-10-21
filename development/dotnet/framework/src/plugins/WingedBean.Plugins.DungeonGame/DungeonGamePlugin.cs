using ConsoleDungeon.Contracts;
using Microsoft.Extensions.Logging;
using Plate.CrossMilo.Contracts;
using Plate.PluginManoi.Contracts;
using WingedBean.Contracts.Gameplay;

namespace WingedBean.Plugins.DungeonGame;

/// <summary>
/// Plugin that provides dungeon crawler game logic with ECS and reactive patterns.
/// Implements IPlugin for the Registry-based plugin system.
/// </summary>
[Plugin(
    Name = "DungeonGameService",
    Provides = new[] { typeof(IDungeonService), typeof(IService), typeof(IWorldSnapshotProvider) },
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
        Console.WriteLine("DEBUG: DungeonGamePlugin.OnActivateAsync CALLED!");

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
        Console.WriteLine("DEBUG: Got logger");

        // WORKAROUND: Ensure ArchECSService is registered before we need it
        // This is needed because ArchECSPlugin's OnActivateAsync isn't being called by the manifest loader
        try
        {
            var existingEcs = registry.Get<WingedBean.Plugins.ArchECS.ArchECSService>();
            _logger?.LogDebug("ArchECSService already registered");
            Console.WriteLine("DEBUG: ArchECSService found");
        }
        catch
        {
            Console.WriteLine("DEBUG: ArchECSService NOT found, registering now");
            _logger?.LogWarning("ArchECSService not found in registry, registering it now");
            var ecsService = new WingedBean.Plugins.ArchECS.ArchECSService();
            registry.Register<WingedBean.Plugins.ArchECS.ArchECSService>(ecsService, priority: 100);
            _logger?.LogInformation("Registered ArchECSService");
            Console.WriteLine("DEBUG: ArchECSService registered");
        }

        // Register Dungeon Game service
        Console.WriteLine("DEBUG: Creating DungeonGameService");
        _dungeonService = new DungeonGameService(registry);
        _logger?.LogDebug("Created DungeonGameService instance");
        Console.WriteLine("DEBUG: Registering IDungeonService + Gameplay IService + WorldSnapshotProvider");
        registry.Register<IDungeonService>(_dungeonService, priority: 100);
        registry.Register<IService>(_dungeonService, priority: 100);
        registry.Register<IWorldSnapshotProvider>(_dungeonService, priority: 100);
        _logger?.LogDebug("Registered IDungeonService, Gameplay.IService and IWorldSnapshotProvider");
        Console.WriteLine("DEBUG: DungeonGamePlugin.OnActivateAsync COMPLETE");

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

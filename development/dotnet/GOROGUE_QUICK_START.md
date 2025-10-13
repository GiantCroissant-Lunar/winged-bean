# GoRogue Integration - Quick Start Guide

## Setup MessagePipe (5 minutes)

### 1. Update `DungeonGamePluginActivator.cs`

```csharp
using MessagePipe;
using WingedBean.Plugins.DungeonGame.ViewModels;

public class DungeonGamePluginActivator : IPluginActivator
{
    public Task ActivateAsync(IServiceCollection services, IServiceProvider host, CancellationToken ct)
    {
        // Add MessagePipe
        services.AddMessagePipe();
        
        // Register ViewModels
        services.AddSingleton<DungeonViewModel>();
        
        // Register systems with MessagePipe publishers
        services.AddSingleton<GoRogueFOVSystem>();
        services.AddSingleton<GoRoguePathfindingSystem>();
        
        return Task.CompletedTask;
    }
}
```

---

## Initialize Player with FOV (2 minutes)

### 2. Update `DungeonGame.cs` - Add FOV to Player

```csharp
private void CreatePlayerLegacy()
{
    var player = _world.CreateEntity();
    
    _world.AttachComponent(player, new Player());
    _world.AttachComponent(player, new Position(40, 12, 1));
    _world.AttachComponent(player, new Stats { /* ... */ });
    _world.AttachComponent(player, new Renderable { /* ... */ });
    
    // ✨ NEW: Add FOV component
    _world.AttachComponent(player, new FOVComponent
    {
        Range = 10,
        DistanceCalculation = Distance.Chebyshev,
        IsDirty = true  // Calculate on first frame
    });
    
    // ✨ NEW: Add map memory
    _world.AttachComponent(player, new MapMemoryComponent
    {
        ExploredTiles = new HashSet<Point>(),
        VisibleTiles = new HashSet<Point>()
    });
}
```

---

## Create and Initialize Map (5 minutes)

### 3. Create DungeonMap in `DungeonGame.cs`

```csharp
private DungeonMap? _currentMap;

private void InitializeWorldLegacy()
{
    // Create map
    _currentMap = new DungeonMap(80, 24, 1);
    
    // Fill with floor tiles
    for (int x = 0; x < _currentMap.Width; x++)
    {
        for (int y = 0; y < _currentMap.Height; y++)
        {
            _currentMap.Tiles[x, y] = new Tile
            {
                Type = TileType.Floor,
                IsWalkable = true,
                IsTransparent = true,
                IsExplored = false,
                IsVisible = false,
                Glyph = '.',
                Foreground = Color.Gray,
                Background = Color.Black
            };
        }
    }
    
    // Add some walls (example)
    for (int x = 0; x < _currentMap.Width; x++)
    {
        _currentMap.Tiles[x, 0] = CreateWallTile();
        _currentMap.Tiles[x, _currentMap.Height - 1] = CreateWallTile();
    }
    for (int y = 0; y < _currentMap.Height; y++)
    {
        _currentMap.Tiles[0, y] = CreateWallTile();
        _currentMap.Tiles[_currentMap.Width - 1, y] = CreateWallTile();
    }
    
    // Create entities...
    CreatePlayerLegacy();
    CreateEnemiesLegacy(5);
}

private Tile CreateWallTile() => new Tile
{
    Type = TileType.Wall,
    IsWalkable = false,
    IsTransparent = false,
    IsExplored = false,
    IsVisible = false,
    Glyph = '#',
    Foreground = Color.White,
    Background = Color.Black
};
```

---

## Register GoRogue Systems (3 minutes)

### 4. Update `RegisterSystems()` in `DungeonGame.cs`

```csharp
private void RegisterSystems()
{
    // Get MessagePipe publishers from DI
    var fovPublisher = _registry.Get<IPublisher<FOVCalculatedEvent>>();
    var pathPublisher = _registry.Get<IPublisher<PathCalculatedEvent>>();
    
    // Create GoRogue systems
    var fovSystem = new GoRogueFOVSystem(fovPublisher);
    var pathSystem = new GoRoguePathfindingSystem(pathPublisher);
    
    // Set the map
    fovSystem.SetMap(_currentMap!);
    pathSystem.SetMap(_currentMap!);
    
    // Register systems in order
    _systems.Add(fovSystem);              // FOV first
    _systems.Add(new AISystem());         // AI
    _systems.Add(pathSystem);             // Pathfinding
    _systems.Add(new MovementSystem());   // Movement
    _systems.Add(new CombatSystem());     // Combat
    _systems.Add(new RenderSystem());     // Render
}
```

---

## Trigger FOV Recalculation on Movement (2 minutes)

### 5. Update Movement to Mark FOV Dirty

```csharp
// In your input handler or movement system
public void MovePlayer(int dx, int dy)
{
    foreach (var entity in _world.CreateQuery<Player, Position, FOVComponent>())
    {
        ref var pos = ref _world.GetComponent<Position>(entity);
        ref var fov = ref _world.GetComponent<FOVComponent>(entity);
        
        // Move player
        pos.X += dx;
        pos.Y += dy;
        
        // Mark FOV for recalculation
        fov.IsDirty = true;
    }
}
```

---

## Use ViewModel in UI (Terminal.Gui Example)

### 6. Update `TerminalGuiService.cs`

```csharp
public class TerminalGuiDungeonView : View
{
    private readonly DungeonViewModel _viewModel;
    private IDisposable? _tilesSubscription;
    
    public TerminalGuiDungeonView(DungeonViewModel viewModel)
    {
        _viewModel = viewModel;
        
        // Subscribe to tile changes
        _tilesSubscription = _viewModel.Tiles.ObserveChanged()
            .Subscribe(e =>
            {
                // Redraw changed tiles
                foreach (var item in e.NewItems)
                {
                    DrawTile(item.Key, item.Value);
                }
            });
        
        // Subscribe to player HP changes
        _viewModel.WhenAnyValue(x => x.PlayerHP)
            .Subscribe(hp => UpdateHPDisplay(hp));
    }
    
    private void DrawTile(Point pos, TileViewModel tile)
    {
        if (!tile.IsExplored) return;
        
        var glyph = tile.Glyph;
        var color = tile.IsVisible ? tile.Foreground : DimColor(tile.Foreground);
        
        // Draw to Terminal.Gui
        AddRune(pos.X, pos.Y, new Rune(glyph));
    }
}
```

---

## Add Pathfinding to Enemy AI (5 minutes)

### 7. Update `AISystem.cs` to Use Pathfinding

```csharp
public class AISystem : IECSSystem
{
    public void Execute(IWorld world, float deltaTime)
    {
        // Find player
        Position? playerPos = null;
        foreach (var p in world.CreateQuery<Player, Position>())
        {
            playerPos = world.GetComponent<Position>(p);
            break;
        }
        
        if (!playerPos.HasValue) return;
        
        // Process enemies with pathfinding
        foreach (var enemy in world.CreateQuery<Enemy, Position, PathfindingComponent>())
        {
            ref var enemyPos = ref world.GetComponent<Position>(enemy);
            ref var pathComp = ref world.GetComponent<PathfindingComponent>(enemy);
            ref var ai = ref world.GetComponent<Enemy>(enemy);
            
            var distance = CalculateDistance(enemyPos, playerPos.Value);
            
            if (ai.State == AIState.Chase)
            {
                // Set pathfinding target
                pathComp.TargetPosition = new Point(playerPos.Value.X, playerPos.Value.Y);
                
                // Follow path if available
                if (pathComp.CurrentPath != null)
                {
                    bool hasMoreSteps = GoRoguePathfindingSystem.FollowPath(
                        ref enemyPos, 
                        ref pathComp
                    );
                    
                    if (!hasMoreSteps)
                    {
                        // Path completed, recalculate
                        pathComp.TargetPosition = new Point(playerPos.Value.X, playerPos.Value.Y);
                    }
                }
            }
        }
    }
}
```

---

## Publish Custom Events (Optional)

### 8. Publish Events from Your Systems

```csharp
public class CombatSystem : IECSSystem
{
    private readonly IPublisher<CombatEvent>? _combatPublisher;
    
    public CombatSystem(IPublisher<CombatEvent>? combatPublisher = null)
    {
        _combatPublisher = combatPublisher;
    }
    
    public void Execute(IWorld world, float deltaTime)
    {
        // ... combat logic ...
        
        // Publish combat event
        _combatPublisher?.Publish(new CombatEvent(
            AttackerId: attackerId,
            DefenderId: defenderId,
            DamageDealt: damage,
            IsKillingBlow: defenderHP <= 0
        ));
    }
}
```

---

## Subscribe to Events in Plugins (Optional)

### 9. Analytics Plugin Example

```csharp
public class AnalyticsPlugin : IPluginActivator
{
    public Task ActivateAsync(IServiceCollection services, IServiceProvider host, CancellationToken ct)
    {
        var combatSubscriber = host.GetRequiredService<ISubscriber<CombatEvent>>();
        var fovSubscriber = host.GetRequiredService<ISubscriber<FOVCalculatedEvent>>();
        
        combatSubscriber.Subscribe(evt =>
        {
            Console.WriteLine($"Combat: {evt.AttackerId} hit {evt.DefenderId} for {evt.DamageDealt} damage");
        });
        
        fovSubscriber.Subscribe(evt =>
        {
            Console.WriteLine($"FOV calculated: {evt.VisibleTiles.Count} tiles visible");
        });
        
        return Task.CompletedTask;
    }
}
```

---

## Testing Checklist

- [ ] Player has FOVComponent and MapMemoryComponent
- [ ] DungeonMap is created and initialized
- [ ] GoRogueFOVSystem is registered and has map set
- [ ] GoRoguePathfindingSystem is registered and has map set
- [ ] MessagePipe is added to DI
- [ ] DungeonViewModel is registered
- [ ] FOV recalculates when player moves
- [ ] Tiles update visibility correctly
- [ ] UI subscribes to ViewModel changes

---

## Common Issues

### FOV Not Calculating?
- Check `FOVComponent.IsDirty` is set to `true`
- Verify `GoRogueFOVSystem.SetMap()` was called
- Ensure tiles have `IsTransparent` set correctly

### Pathfinding Not Working?
- Check `PathfindingComponent.TargetPosition` is set
- Verify `GoRoguePathfindingSystem.SetMap()` was called
- Ensure tiles have `IsWalkable` set correctly

### Events Not Firing?
- Verify MessagePipe is registered: `services.AddMessagePipe()`
- Check publishers are injected into systems
- Ensure subscribers are registered before events are published

---

## Performance Tips

1. **Throttle FOV** - Only recalculate when player moves
2. **Throttle Pathfinding** - Use `RecalculationInterval` (e.g., 0.5 seconds)
3. **Limit FOV Range** - Use 8-12 tiles for good performance
4. **Use ObservableCollections** - They minimize UI updates
5. **Profile** - Use BenchmarkDotNet for hotspots

---

## Next Steps

1. ✅ Follow this quick start
2. 📖 Read `GOROGUE_INTEGRATION_SUMMARY.md` for architecture details
3. 🎮 Test in both Terminal.Gui and SadConsole
4. 🗺️ Add procedural map generation (see GoRogue docs)
5. 💡 Add dynamic lighting with `LightSourceComponent`
6. 🎯 Enhance AI with more sophisticated behaviors

---

**Ready to build a proper dungeon crawler! 🎮**

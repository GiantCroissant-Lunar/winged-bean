---
id: RFC-0043
title: Tile-Based Map System for Dungeon Game
status: Proposed
category: game-engine, ecs, gameplay
created: 2025-10-14
updated: 2025-10-14
priority: P1
effort: 4-6 hours
---

# RFC-0043: Tile-Based Map System for Dungeon Game

**Status:** Proposed  
**Date:** 2025-10-14  
**Author:** Development Team  
**Category:** game-engine, ecs, gameplay  
**Priority:** HIGH (P1)  
**Estimated Effort:** 4-6 hours  
**Depends On:** 
- RFC-0007: Arch ECS Integration (Implemented)
- WingedBean.Plugins.DungeonGame (Existing)

---

## Summary

Introduce a tile-based map system (`DungeonMap`, `MapTile`, `TileType`) to the ECS-based dungeon game, enabling spatial representation of walkable/blocking terrain. This provides the foundation for procedural dungeon generation (RFC-0044) and advanced pathfinding (future). The map will be stored as an ECS component and checked by movement/rendering systems.

---

## Motivation

### Current Problem

The dungeon game currently has **no spatial map representation**:

1. **No Walls or Terrain** - Entities can move anywhere without collision
2. **No Spatial Structure** - No concept of rooms, corridors, or obstacles
3. **Manual Spawn Positioning** - Entity positions are hardcoded or random without context
4. **Cannot Support Procedural Generation** - No data structure to hold generated layouts

**Current State:**
```csharp
// DungeonGame.cs - InitializeWorldLegacy()
var player = _world.Create();
player.Add(new Position { X = 40, Y = 20 }); // Arbitrary center position
```

**Issues:**
- Player spawns in the void (no validation)
- Enemies can spawn on top of the player
- No concept of "walkable" vs "blocked" tiles
- Rendering only shows entities, not environment

### Why Now?

This is **foundational** for:
1. ✅ **RFC-0044**: Procedural dungeon generation (needs map to populate)
2. ✅ **Future AI**: Pathfinding requires spatial graph
3. ✅ **Gameplay**: Walls, doors, obstacles create tactical depth
4. ✅ **Visual Feedback**: Render environment alongside entities

### Success Criteria

✅ Define tile types (Floor, Wall, Door, Empty)  
✅ Store 2D map as ECS component  
✅ Movement system validates walkability  
✅ Entities cannot walk through walls  
✅ Test with hardcoded sample map  
✅ No breaking changes to existing gameplay

---

## Proposal

### 1. Core Data Structures

#### 1.1 TileType Enum

```csharp
namespace WingedBean.Plugins.DungeonGame.Components;

/// <summary>
/// Types of tiles that can exist in a dungeon map.
/// </summary>
public enum TileType
{
    /// <summary>Empty/void space (unpassable, no rendering)</summary>
    Empty = 0,
    
    /// <summary>Walkable floor tile</summary>
    Floor = 1,
    
    /// <summary>Blocking wall tile</summary>
    Wall = 2,
    
    /// <summary>Passable door tile (can open/close in future)</summary>
    Door = 3,
    
    /// <summary>Corridor connecting rooms</summary>
    Corridor = 4
}
```

#### 1.2 MapTile Struct

```csharp
namespace WingedBean.Plugins.DungeonGame.Components;

/// <summary>
/// Represents a single tile in the dungeon map.
/// Immutable value type for performance and safety.
/// </summary>
public readonly struct MapTile
{
    public TileType Type { get; init; }
    public bool IsWalkable { get; init; }
    public bool BlocksVision { get; init; }
    public char Symbol { get; init; }
    
    // Factory methods for common tiles
    public static MapTile Empty => new() 
    { 
        Type = TileType.Empty, 
        IsWalkable = false, 
        BlocksVision = true, 
        Symbol = ' ' 
    };
    
    public static MapTile Floor => new() 
    { 
        Type = TileType.Floor, 
        IsWalkable = true, 
        BlocksVision = false, 
        Symbol = '.' 
    };
    
    public static MapTile Wall => new() 
    { 
        Type = TileType.Wall, 
        IsWalkable = false, 
        BlocksVision = true, 
        Symbol = '#' 
    };
    
    public static MapTile Door => new() 
    { 
        Type = TileType.Door, 
        IsWalkable = true, 
        BlocksVision = false, 
        Symbol = '+' 
    };
}
```

**Design Decisions:**
- `readonly struct` for value semantics and performance (no GC pressure)
- `IsWalkable` separate from `TileType` for future extensibility (locked doors, traps)
- `BlocksVision` for future line-of-sight/fog-of-war features
- `Symbol` stored with tile for rendering consistency

#### 1.3 DungeonMap Component

```csharp
namespace WingedBean.Plugins.DungeonGame.Components;

/// <summary>
/// 2D grid map of dungeon tiles.
/// Attached to the World as a singleton component.
/// </summary>
public class DungeonMap
{
    private readonly MapTile[,] _tiles;
    
    public int Width { get; }
    public int Height { get; }
    
    public DungeonMap(int width, int height)
    {
        Width = width;
        Height = height;
        _tiles = new MapTile[width, height];
        
        // Initialize with empty tiles
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                _tiles[x, y] = MapTile.Empty;
            }
        }
    }
    
    /// <summary>Get tile at position (throws if out of bounds)</summary>
    public MapTile GetTile(int x, int y)
    {
        if (!IsInBounds(x, y))
            throw new ArgumentOutOfRangeException($"Position ({x}, {y}) is out of bounds");
        return _tiles[x, y];
    }
    
    /// <summary>Set tile at position (throws if out of bounds)</summary>
    public void SetTile(int x, int y, MapTile tile)
    {
        if (!IsInBounds(x, y))
            throw new ArgumentOutOfRangeException($"Position ({x}, {y}) is out of bounds");
        _tiles[x, y] = tile;
    }
    
    /// <summary>Check if position is within map bounds</summary>
    public bool IsInBounds(int x, int y)
    {
        return x >= 0 && x < Width && y >= 0 && y < Height;
    }
    
    /// <summary>Check if position is walkable (in bounds and tile allows movement)</summary>
    public bool IsWalkable(int x, int y)
    {
        return IsInBounds(x, y) && _tiles[x, y].IsWalkable;
    }
    
    /// <summary>Find all walkable positions (for spawn point selection)</summary>
    public IEnumerable<(int X, int Y)> GetWalkablePositions()
    {
        for (int x = 0; x < Width; x++)
        {
            for (int y = 0; y < Height; y++)
            {
                if (_tiles[x, y].IsWalkable)
                {
                    yield return (x, y);
                }
            }
        }
    }
}
```

**Design Decisions:**
- Array-based storage (`MapTile[,]`) for cache-friendly access
- Bounds checking in accessors for safety
- `GetWalkablePositions()` for spawn point selection
- No serialization logic (generated at runtime, not saved)

---

### 2. System Integration

#### 2.1 DungeonGame Changes

```csharp
// DungeonGame.cs
public class DungeonGame
{
    private DungeonMap? _dungeonMap;
    
    private void InitializeWorldLegacy()
    {
        // NEW: Create test map
        _dungeonMap = CreateTestMap();
        _logger?.LogInformation("Created test map: {Width}x{Height}", 
            _dungeonMap.Width, _dungeonMap.Height);
        
        // Existing: Create player at valid position
        CreatePlayerAt(40, 20);
        CreateEnemiesLegacy(5);
    }
    
    private DungeonMap CreateTestMap()
    {
        var map = new DungeonMap(80, 40);
        
        // Simple room with walls
        // ########
        // #......#
        // #......#
        // ########
        for (int x = 35; x <= 45; x++)
        {
            for (int y = 15; y <= 25; y++)
            {
                if (x == 35 || x == 45 || y == 15 || y == 25)
                {
                    map.SetTile(x, y, MapTile.Wall);
                }
                else
                {
                    map.SetTile(x, y, MapTile.Floor);
                }
            }
        }
        
        // Add door
        map.SetTile(40, 15, MapTile.Door);
        
        return map;
    }
}
```

#### 2.2 MovementSystem Changes

```csharp
// Systems/MovementSystem.cs
public class MovementSystem : IECSSystem
{
    public void Update(IWorld world, float deltaTime)
    {
        // NEW: Get map from world
        var map = GetDungeonMap(world);
        
        var query = world.Query<Position, Movement>();
        foreach (var entity in query)
        {
            var pos = entity.Get<Position>();
            var movement = entity.Get<Movement>();
            
            int newX = pos.X + movement.DeltaX;
            int newY = pos.Y + movement.DeltaY;
            
            // NEW: Validate movement against map
            if (map != null && !map.IsWalkable(newX, newY))
            {
                // Movement blocked by wall
                _logger?.LogDebug("Movement blocked at ({X}, {Y})", newX, newY);
                continue;
            }
            
            // Apply movement
            pos.X = newX;
            pos.Y = newY;
            
            // Clear movement delta
            movement.DeltaX = 0;
            movement.DeltaY = 0;
        }
    }
    
    private DungeonMap? GetDungeonMap(IWorld world)
    {
        // Map stored as singleton component
        // Implementation depends on ECS component storage
        return world.GetSingleton<DungeonMap>();
    }
}
```

---

### 3. Rendering Integration

```csharp
// Systems/RenderSystem.cs
public class RenderSystem : IECSSystem
{
    public void Update(IWorld world, float deltaTime)
    {
        // NEW: Render map first (background layer)
        RenderMap(world);
        
        // Existing: Render entities (foreground layer)
        RenderEntities(world);
    }
    
    private void RenderMap(IWorld world)
    {
        var map = world.GetSingleton<DungeonMap>();
        if (map == null) return;
        
        for (int y = 0; y < map.Height; y++)
        {
            for (int x = 0; x < map.Width; x++)
            {
                var tile = map.GetTile(x, y);
                if (tile.Type == TileType.Empty) continue;
                
                // Emit render event
                EmitTileRender(x, y, tile.Symbol, GetTileColor(tile.Type));
            }
        }
    }
    
    private string GetTileColor(TileType type)
    {
        return type switch
        {
            TileType.Floor => "Gray",
            TileType.Wall => "White",
            TileType.Door => "Brown",
            _ => "Black"
        };
    }
}
```

---

## Implementation Plan

### Phase 1: Core Components (2 hours)
- [ ] Create `Components/TileType.cs`
- [ ] Create `Components/MapTile.cs` with factory methods
- [ ] Create `Components/DungeonMap.cs` with tests
- [ ] Unit tests for boundary checking, walkability

### Phase 2: System Integration (1.5 hours)
- [ ] Update `DungeonGame.cs` with `_dungeonMap` field
- [ ] Implement `CreateTestMap()` helper
- [ ] Update `MovementSystem.cs` with collision detection
- [ ] Update spawn logic to use walkable positions

### Phase 3: Rendering (1 hour)
- [ ] Update `RenderSystem.cs` to render map tiles
- [ ] Test rendering in console UI
- [ ] Test rendering in desktop UI (SadConsole)

### Phase 4: Testing & Polish (1 hour)
- [ ] Integration tests with ArchECS
- [ ] Verify player collision in test map
- [ ] Verify enemy pathfinding respects walls
- [ ] Documentation updates

---

## Testing Strategy

### Unit Tests
```csharp
[Fact]
public void DungeonMap_IsWalkable_ReturnsFalse_ForWalls()
{
    var map = new DungeonMap(10, 10);
    map.SetTile(5, 5, MapTile.Wall);
    
    Assert.False(map.IsWalkable(5, 5));
}

[Fact]
public void DungeonMap_IsWalkable_ReturnsFalse_ForOutOfBounds()
{
    var map = new DungeonMap(10, 10);
    
    Assert.False(map.IsWalkable(-1, 5));
    Assert.False(map.IsWalkable(15, 5));
}

[Fact]
public void MovementSystem_BlocksMovement_IntoWalls()
{
    // Arrange: Create world with map and entity
    var world = CreateTestWorld();
    var map = CreateMapWithWall(5, 5);
    var player = CreateEntityAt(4, 5);
    
    // Act: Try to move into wall
    player.Get<Movement>().DeltaX = 1;
    movementSystem.Update(world, 0.016f);
    
    // Assert: Position unchanged
    Assert.Equal(4, player.Get<Position>().X);
}
```

### Integration Tests
- Player spawns in walkable area
- Player cannot walk through walls
- Enemies respect map boundaries
- Map renders correctly in both UIs

---

## Migration Path

### Backward Compatibility
✅ **No breaking changes** - Map is optional:
```csharp
// If no map exists, movement works as before
if (map == null || !map.IsInBounds(newX, newY))
{
    // Old behavior: allow movement
    pos.X = newX;
    pos.Y = newY;
}
else if (map.IsWalkable(newX, newY))
{
    // New behavior: check walkability
    pos.X = newX;
    pos.Y = newY;
}
```

### Rollout Plan
1. **Phase 1**: Add map structures (no behavior changes)
2. **Phase 2**: Enable collision in dev builds
3. **Phase 3**: Create test maps for validation
4. **Phase 4**: Enable by default in all builds

---

## Risks & Mitigations

| Risk | Impact | Mitigation |
|------|--------|------------|
| **Performance**: Large maps (1000x1000) | Medium | Use chunking/spatial hashing (future) |
| **Memory**: Map stored in memory | Low | 80x40 map = ~13KB (negligible) |
| **ECS Integration**: Singleton component pattern | Medium | Document pattern, add helper methods |
| **Testing**: Hard to visualize failures | Low | Add debug render methods |

---

## Alternatives Considered

### 1. Tile Entity per Position
**Approach:** Create ECS entity for each tile
```csharp
for each tile:
    world.Create()
        .Add(new Position(x, y))
        .Add(new TileComponent(type))
```

**Pros:** Pure ECS approach, can query tiles  
**Cons:** 3200 entities for 80x40 map, slow queries, memory overhead  
**Verdict:** ❌ Rejected - Not performant for static data

### 2. Octree/Quadtree Spatial Structure
**Approach:** Store tiles in hierarchical spatial tree

**Pros:** Fast collision queries, efficient for sparse maps  
**Cons:** Complex implementation, overkill for small dungeons  
**Verdict:** ❌ Deferred - Consider for future large maps

### 3. External Map Service
**Approach:** Store map outside ECS in separate service

**Pros:** Clear separation of concerns  
**Cons:** Systems need reference to external service, breaks ECS patterns  
**Verdict:** ❌ Rejected - Violates ECS architecture

---

## Success Metrics

### Functional
- ✅ 100% test coverage for map operations
- ✅ Player collision detection works in 100% of test cases
- ✅ No movement into walls in any scenario
- ✅ Map rendering visible in both console and desktop

### Performance
- ✅ Map initialization < 10ms
- ✅ Collision check < 0.01ms per entity
- ✅ Render overhead < 5% of frame time
- ✅ Memory usage < 50KB for typical map

### Code Quality
- ✅ All public APIs documented
- ✅ Unit tests pass
- ✅ Integration tests pass
- ✅ No warnings or code smells

---

## Future Enhancements

Enabled by this RFC (not included in initial implementation):

1. **Doors & Interactions** (RFC-TBD)
   - Add `IsLocked`, `IsOpen` states
   - Player interaction to open/close

2. **Fog of War** (RFC-TBD)
   - Track `IsExplored`, `IsVisible` per tile
   - Render based on player vision

3. **Destructible Terrain** (RFC-TBD)
   - Allow runtime tile modification
   - Emit events for map changes

4. **Multi-Layer Maps** (RFC-TBD)
   - Ground layer, object layer, ceiling layer
   - Support for bridges, pits, etc.

---

## Related RFCs

- **RFC-0007**: Arch ECS Integration ✅ Implemented
- **RFC-0044**: Procedural Dungeon Generation ⏭️ Depends on this
- **RFC-TBD**: Pathfinding with ModernSatsuma 🔮 Future

---

## References

- ArchECS Documentation: [Arch GitHub](https://github.com/genaray/Arch)
- Roguelike Map Design: [RogueBasin](http://www.roguebasin.com/index.php?title=Cellular_Automata_Method_for_Generating_Random_Cave-Like_Levels)
- Tile-Based Games: [Game Programming Patterns](https://gameprogrammingpatterns.com/spatial-partition.html)

---

**Decision:** Proposed - Awaiting Review  
**Next Steps:** 
1. Review with team
2. Create implementation branch
3. Begin Phase 1 (Core Components)

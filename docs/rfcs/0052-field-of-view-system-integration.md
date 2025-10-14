---
id: RFC-0052
title: Field of View System Integration
status: Proposed
category: game-engine, rendering, gameplay
created: 2025-10-14
updated: 2025-10-14
priority: P1
effort: 2-4 hours
---

# RFC-0052: Field of View System Integration

**Status:** Proposed  
**Date:** 2025-10-14  
**Author:** Development Team  
**Category:** game-engine, rendering, gameplay  
**Priority:** HIGH (P1)  
**Estimated Effort:** 2-4 hours  
**Depends On:**
- RFC-0043: Tile-Based Map System (✅ Implemented)
- RFC-0044: Procedural Dungeon Generation (✅ Implemented)
- GoRogue library (✅ Available in DungeonGame plugin)

---

## Summary

Integrate GoRogue's Field of View (FOV) system with the newly implemented tile-based map system to provide:
- **Visibility tracking** (explored vs visible tiles)
- **Dynamic fog of war** rendering
- **Shadowcasting** for realistic line-of-sight
- **Map memory** (remember explored areas)

This bridges the gap between the generic map system (WingedBean.Gameplay.Map) and the game-specific FOV implementation (WingedBean.Plugins.DungeonGame), creating a reusable FOV system in the framework layer.

---

## Motivation

### Current State

✅ **What Exists:**
- Tile-based map system with visibility flags (`IsVisible`, `IsExplored`)
- GoRogue FOV implementation in DungeonGame plugin
- Pathfinding using GoRogue's A* algorithm
- Map memory components for tracking explored tiles

❌ **What's Missing:**
- No connection between `DungeonMap` and GoRogue FOV
- FOV logic tied to specific DungeonGame plugin (not reusable)
- No generic FOV system at framework level
- MapRenderSystem doesn't respect visibility

### Why This Matters

**Player Experience:**
- 🌫️ **Fog of War** - Classic roguelike visibility
- 🔦 **Exploration** - Sense of discovery as areas reveal
- 👁️ **Strategic Gameplay** - Plan around what's visible
- 🗺️ **Map Memory** - Remember where you've been

**Technical Benefits:**
- 🧩 **Reusability** - FOV system usable by any game mode
- 🎯 **Performance** - Efficient shadowcasting algorithm
- 🔧 **Extensibility** - Support different vision types (darkvision, blind sight)
- ✅ **Tested** - GoRogue's battle-tested algorithms

---

## Proposal

### 1. Architecture

Create a framework-level FOV system that bridges `WingedBean.Gameplay.Map` and GoRogue:

```
WingedBean.Gameplay.Map (Framework)
├── Data/
│   ├── DungeonMap.cs                 (✅ Exists)
│   └── MapTile.cs                    (✅ Exists)
├── Components/
│   ├── FOVComponent.cs               (🆕 NEW - Framework level)
│   └── MapMemoryComponent.cs         (🆕 NEW - Framework level)
└── Systems/
    ├── FOVSystem.cs                  (🆕 NEW - Framework level)
    └── MapRenderSystem.cs            (🔄 UPDATE - Respect visibility)

WingedBean.Plugins.DungeonGame (Game-specific)
├── Components/
│   └── GoRogueComponents.cs          (✅ Keep for game-specific features)
└── Systems/
    └── GoRogueFOVSystem.cs           (🔄 REFACTOR - Use framework FOV)
```

### 2. Component Design

#### FOVComponent (Framework Level)

```csharp
namespace WingedBean.Gameplay.Map.Components;

/// <summary>
/// Field of View component for entities with vision.
/// </summary>
public struct FOVComponent
{
    /// <summary>
    /// Vision range in tiles.
    /// </summary>
    public int Range { get; set; }

    /// <summary>
    /// Whether FOV needs recalculation.
    /// </summary>
    public bool IsDirty { get; set; }

    /// <summary>
    /// Cached visible tile positions.
    /// </summary>
    public HashSet<(int x, int y)>? VisibleTiles { get; set; }

    public FOVComponent(int range)
    {
        Range = range;
        IsDirty = true;
        VisibleTiles = null;
    }
}
```

#### MapMemoryComponent (Framework Level)

```csharp
namespace WingedBean.Gameplay.Map.Components;

/// <summary>
/// Tracks which tiles an entity has explored.
/// </summary>
public struct MapMemoryComponent
{
    /// <summary>
    /// Set of explored tile coordinates.
    /// </summary>
    public HashSet<(int x, int y)>? ExploredTiles { get; set; }

    public static MapMemoryComponent Create()
    {
        return new MapMemoryComponent
        {
            ExploredTiles = new HashSet<(int x, int y)>()
        };
    }
}
```

### 3. FOV System Implementation

```csharp
namespace WingedBean.Gameplay.Map.Systems;

/// <summary>
/// Field of View system using recursive shadowcasting.
/// </summary>
public class FOVSystem
{
    private readonly World _world;

    public FOVSystem(World world)
    {
        _world = world;
    }

    /// <summary>
    /// Calculates FOV for all entities with FOV components.
    /// </summary>
    public void UpdateFOV()
    {
        var query = new QueryDescription()
            .WithAll<PositionComponent, FOVComponent, MapMemoryComponent>();

        _world.Query(in query, (Entity entity, 
            ref PositionComponent pos,
            ref FOVComponent fov,
            ref MapMemoryComponent memory) =>
        {
            if (!fov.IsDirty)
                return;

            // Get the map
            var mapQuery = new QueryDescription().WithAll<MapComponent>();
            _world.Query(in mapQuery, (ref MapComponent mapComp) =>
            {
                var map = mapComp.Map;
                
                // Clear previous visibility
                map.ClearVisibility();
                
                // Calculate new visible tiles using shadowcasting
                var newVisible = CalculateFOV(map, pos.X, pos.Y, fov.Range);
                
                // Update map tiles
                foreach (var (x, y) in newVisible)
                {
                    map.SetVisible(x, y, true);
                    map.SetExplored(x, y, true);
                    
                    // Update memory
                    memory.ExploredTiles?.Add((x, y));
                }
                
                fov.VisibleTiles = newVisible;
                fov.IsDirty = false;
            });
        });
    }

    /// <summary>
    /// Recursive shadowcasting FOV algorithm.
    /// </summary>
    private HashSet<(int x, int y)> CalculateFOV(
        DungeonMap map, 
        int originX, 
        int originY, 
        int range)
    {
        var visible = new HashSet<(int x, int y)> { (originX, originY) };

        // Cast shadows in 8 octants
        for (int octant = 0; octant < 8; octant++)
        {
            CastLight(map, originX, originY, range, 1, 1.0, 0.0, 
                octant, visible);
        }

        return visible;
    }

    private void CastLight(DungeonMap map, int cx, int cy, 
        int range, int row, double start, double end, 
        int octant, HashSet<(int x, int y)> visible)
    {
        if (start < end)
            return;

        for (int j = row; j <= range; j++)
        {
            int dx = -j - 1;
            int dy = -j;
            bool blocked = false;

            while (dx <= 0)
            {
                dx++;
                int X, Y;
                
                // Transform coordinates based on octant
                (X, Y) = TransformOctant(cx, cy, dx, dy, octant);

                double l_slope = (dx - 0.5) / (dy + 0.5);
                double r_slope = (dx + 0.5) / (dy - 0.5);

                if (start < r_slope)
                    continue;
                else if (end > l_slope)
                    break;

                if (!map.IsInBounds(X, Y))
                    continue;

                int distance = Math.Abs(dx) + Math.Abs(dy);
                if (distance <= range)
                {
                    visible.Add((X, Y));
                }

                if (blocked)
                {
                    if (map.BlocksSight(X, Y))
                    {
                        start = r_slope;
                        continue;
                    }
                    else
                    {
                        blocked = false;
                    }
                }
                else
                {
                    if (map.BlocksSight(X, Y) && j < range)
                    {
                        blocked = true;
                        CastLight(map, cx, cy, range, j + 1, start, l_slope, 
                            octant, visible);
                    }
                }
            }

            if (blocked)
                break;
        }
    }

    private (int X, int Y) TransformOctant(int cx, int cy, int dx, int dy, int octant)
    {
        return octant switch
        {
            0 => (cx + dx, cy - dy),
            1 => (cx + dy, cy - dx),
            2 => (cx + dy, cy + dx),
            3 => (cx + dx, cy + dy),
            4 => (cx - dx, cy + dy),
            5 => (cx - dy, cy + dx),
            6 => (cx - dy, cy - dx),
            7 => (cx - dx, cy - dy),
            _ => (cx, cy)
        };
    }
}
```

### 4. Updated MapRenderSystem

```csharp
// Update existing RenderMap method
public string RenderMap(bool showOnlyVisible = true, int? playerX = null, int? playerY = null)
{
    var mapQuery = new QueryDescription().WithAll<MapComponent>();
    var result = new System.Text.StringBuilder();

    _world.Query(in mapQuery, (ref MapComponent mapComp) =>
    {
        var map = mapComp.Map;

        for (int y = 0; y < map.Height; y++)
        {
            for (int x = 0; x < map.Width; x++)
            {
                var tile = map.GetTile(x, y);

                // Check if this is player position
                if (playerX == x && playerY == y)
                {
                    result.Append('@');
                    continue;
                }

                if (showOnlyVisible)
                {
                    if (tile.IsVisible)
                    {
                        // Visible tiles - full brightness
                        result.Append(GetTileChar(tile.Type));
                    }
                    else if (tile.IsExplored)
                    {
                        // Explored but not visible - dim
                        result.Append('·');
                    }
                    else
                    {
                        // Unknown - empty
                        result.Append(' ');
                    }
                }
                else
                {
                    result.Append(GetTileChar(tile.Type));
                }
            }
            result.AppendLine();
        }
    });

    return result.ToString();
}
```

---

## Implementation Plan

### Phase 1: Framework FOV Components (1 hour)

1. **Create FOVComponent** in `WingedBean.Gameplay.Map/Components/`
2. **Create MapMemoryComponent** in `WingedBean.Gameplay.Map/Components/`
3. **Add unit tests** for components

### Phase 2: FOV System Implementation (2 hours)

1. **Implement FOVSystem** with shadowcasting algorithm
2. **Update MapRenderSystem** to respect visibility
3. **Add comprehensive unit tests**
   - Test FOV calculation
   - Test visibility updates
   - Test memory persistence
4. **Update demo** to showcase FOV

### Phase 3: Integration & Testing (1 hour)

1. **Test with generated dungeons**
2. **Performance profiling** (target <5ms for typical FOV)
3. **Update documentation**
4. **Add example usage** to demo

---

## Testing Strategy

### Unit Tests

```csharp
[Fact]
public void FOVSystem_CalculatesVisibilityCorrectly()
{
    // Arrange
    var world = World.Create();
    var map = new DungeonMap(20, 20);
    
    // Create a room
    for (int y = 5; y < 15; y++)
        for (int x = 5; x < 15; x++)
            map.SetTile(x, y, MapTile.Floor());
    
    var mapEntity = world.Create(new MapComponent(map, 1));
    var player = world.Create(
        new PositionComponent(10, 10),
        new FOVComponent(5),
        MapMemoryComponent.Create()
    );
    
    var fovSystem = new FOVSystem(world);
    
    // Act
    fovSystem.UpdateFOV();
    
    // Assert
    Assert.True(map.GetTile(10, 10).IsVisible); // Origin
    Assert.True(map.GetTile(11, 10).IsVisible); // Adjacent
    Assert.False(map.GetTile(20, 20).IsVisible); // Out of range
}

[Fact]
public void FOVSystem_RespectsSightBlocking()
{
    // Test that walls block FOV
}

[Fact]
public void MapMemory_RetainsExploredTiles()
{
    // Test that explored tiles are remembered
}
```

### Performance Tests

- FOV calculation < 5ms for 80x45 map with range 10
- Memory usage < 1MB for typical FOV data
- No allocations in hot path after warm-up

---

## Benefits

### Immediate Value

✅ **Working Fog of War** - Classic roguelike visibility  
✅ **Exploration Feel** - Discovery as you move  
✅ **Framework-Level** - Reusable across game modes  
✅ **Tested Algorithm** - Proven shadowcasting implementation  

### Future Enhancements

🔮 **Light Sources** - Dynamic lighting from torches, spells  
🔮 **Vision Types** - Darkvision, tremorsense, blind sight  
🔮 **Stealth System** - Hide when not in enemy FOV  
🔮 **Fog Effects** - Partial visibility, darkness levels  

---

## Alternatives Considered

### 1. Use GoRogue Directly in Framework

**Pros:** Battle-tested, feature-rich  
**Cons:** Tight coupling, heavy dependency  
**Decision:** Extract algorithm, keep minimal dependencies

### 2. Simple Circle-Based FOV

**Pros:** Very fast, simple to implement  
**Cons:** Unrealistic (see through walls), poor gameplay  
**Decision:** Rejected - shadowcasting is essential for roguelikes

### 3. Defer FOV to Game Layer

**Pros:** Less framework complexity  
**Cons:** Code duplication across game modes  
**Decision:** Rejected - FOV is fundamental enough for framework

---

## Migration Path

Existing `WingedBean.Plugins.DungeonGame.Systems.GoRogueFOVSystem` can:

1. **Option A:** Migrate to use framework FOVSystem
2. **Option B:** Keep for game-specific features (light sources, etc.)
3. **Option C:** Deprecate and remove after migration

**Recommended:** Option B - keep game plugin FOV for advanced features, use framework FOV as foundation.

---

## Success Criteria

✅ FOV system integrated with DungeonMap  
✅ Visibility correctly tracked and rendered  
✅ Map memory persists explored tiles  
✅ Performance < 5ms for typical scenarios  
✅ Unit tests > 90% coverage  
✅ Demo showcases fog of war  
✅ Documentation complete  

---

## References

- [GoRogue FOV Documentation](https://github.com/Chris3606/GoRogue)
- [Recursive Shadowcasting in Python](http://www.roguebasin.com/index.php?title=FOV_using_recursive_shadowcasting)
- RFC-0043: Tile-Based Map System
- RFC-0044: Procedural Dungeon Generation
- Existing: `WingedBean.Plugins.DungeonGame.Systems.GoRogueFOVSystem`

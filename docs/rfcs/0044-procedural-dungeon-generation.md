---
id: RFC-0044
title: Procedural Dungeon Generation with Plate.EdgRogqd
status: Proposed
category: game-engine, procedural-generation, gameplay
created: 2025-10-14
updated: 2025-10-14
priority: P1
effort: 8-12 hours
---

# RFC-0044: Procedural Dungeon Generation with Plate.EdgRogqd

**Status:** Proposed  
**Date:** 2025-10-14  
**Author:** Development Team  
**Category:** game-engine, procedural-generation, gameplay  
**Priority:** HIGH (P1)  
**Estimated Effort:** 8-12 hours  
**Depends On:** 
- RFC-0043: Tile-Based Map System (Required)
- Plate.EdgRogqd library (Available)
- Plate.ModernSatsuma library (Available)

---

## Summary

Integrate **Plate.EdgRogqd** graph-based dungeon generator to procedurally create dungeon layouts for the ECS-based dungeon game. Generated dungeons will populate the `DungeonMap` (RFC-0043) with rooms, corridors, and doors, creating unique, playable levels each time. This replaces static/hardcoded maps with algorithmic generation, increasing replayability and content variety.

---

## Motivation

### Current Problem

1. **Static Maps Only** - Dungeons are hardcoded or loaded from JSON
2. **No Variety** - Same layout every playthrough (boring)
3. **Manual Design Required** - Content creation bottleneck
4. **No Replayability** - Players memorize layouts
5. **Limited Scale** - Hard to create large, interconnected dungeons

### Why Procedural Generation?

**Player Benefits:**
- 🎲 **Infinite Variety** - Every dungeon is unique
- 🔄 **Replayability** - Fresh experience each time
- 🎯 **Adaptive Difficulty** - Generate appropriate complexity
- 🌍 **Scale** - Large dungeons without manual design

**Developer Benefits:**
- ⚡ **Faster Content Creation** - Algorithm generates layouts
- 🎨 **Design Flexibility** - Tweak parameters, not tiles
- 🐛 **Easier Testing** - Seed-based reproducibility
- 📈 **Scalability** - Add features to generator once

### Why Plate.EdgRogqd?

**Advantages:**
1. ✅ **Graph-Based** - Uses proven Edgar algorithm (research-backed)
2. ✅ **Room Templates** - Guarantees interesting room shapes
3. ✅ **Connectivity** - Ensures all rooms reachable
4. ✅ **Configurable** - Control room count, size, corridor width
5. ✅ **Already Integrated** - Part of plate-projects stack
6. ✅ **Uses ModernSatsuma** - Leverages graph algorithms

**Alternatives Considered:**
- **BSP (Binary Space Partitioning)**: Simpler but less flexible
- **Cellular Automata**: Good for caves, bad for structured dungeons
- **Wave Function Collapse**: Powerful but complex, overkill
- **Custom Algorithm**: Reinventing wheel, time-consuming

### Success Criteria

✅ Generate dungeons with 5-10 interconnected rooms  
✅ All rooms reachable from starting room  
✅ Corridors connect rooms with doors  
✅ Seed-based reproducibility (same seed = same dungeon)  
✅ Generation time < 100ms for typical dungeon  
✅ No overlapping rooms or impossible geometry  
✅ Player spawns in valid walkable position  

---

## Proposal

### 1. Architecture Overview

```
┌─────────────────────────────────────────────────┐
│         DungeonGame.Initialize()                 │
│                                                  │
│  1. Load DungeonLevelData (JSON)                 │
│     - Room count, size constraints               │
│     - Seed (optional)                            │
│                                                  │
│  2. DungeonGenerator.Generate()                  │
│     ├─> Build LevelDescription (graph)           │
│     ├─> EdgRogqd: Generate Layout                │
│     ├─> Convert to DungeonMap                    │
│     └─> Return populated map                     │
│                                                  │
│  3. Spawn Entities                               │
│     - Find walkable positions                    │
│     - Place player, enemies, items               │
└─────────────────────────────────────────────────┘
```

---

### 2. Core Components

#### 2.1 DungeonGeneratorOptions

```csharp
namespace WingedBean.Plugins.DungeonGame.Generation;

/// <summary>
/// Configuration for dungeon generation.
/// Maps to JSON level data for data-driven generation.
/// </summary>
public class DungeonGeneratorOptions
{
    /// <summary>Target map width (actual may vary slightly)</summary>
    public int Width { get; set; } = 80;
    
    /// <summary>Target map height (actual may vary slightly)</summary>
    public int Height { get; set; } = 40;
    
    /// <summary>Random seed (null = random seed)</summary>
    public int? Seed { get; set; }
    
    /// <summary>Minimum number of rooms</summary>
    public int MinRooms { get; set; } = 5;
    
    /// <summary>Maximum number of rooms</summary>
    public int MaxRooms { get; set; } = 10;
    
    /// <summary>Minimum room dimension (width/height)</summary>
    public int MinRoomSize { get; set; } = 4;
    
    /// <summary>Maximum room dimension (width/height)</summary>
    public int MaxRoomSize { get; set; } = 12;
    
    /// <summary>Corridor width (1-3 typical)</summary>
    public int CorridorWidth { get; set; } = 1;
    
    /// <summary>Allow cycles in room graph (multiple paths)</summary>
    public bool AllowCycles { get; set; } = false;
    
    /// <summary>Percentage of doors vs open connections (0.0-1.0)</summary>
    public float DoorProbability { get; set; } = 0.3f;
}
```

#### 2.2 Room Templates

```csharp
namespace WingedBean.Plugins.DungeonGame.Generation;

using Plate.EdgRogqd;
using Plate.EdgRogqd.Generation.Graph;

/// <summary>
/// Defines room shapes for dungeon generation.
/// Templates are patterns of tiles that define room layouts.
/// </summary>
public static class RoomTemplates
{
    /// <summary>Simple rectangular room (5x5)</summary>
    public static RoomTemplateGrid2D SmallRectangular()
    {
        // #####
        // #...#
        // #...#
        // #...#
        // #####
        var template = new RoomTemplateGrid2D(5, 5);
        
        for (int x = 0; x < 5; x++)
        {
            for (int y = 0; y < 5; y++)
            {
                bool isEdge = x == 0 || x == 4 || y == 0 || y == 4;
                template.SetTile(x, y, isEdge ? '#' : '.');
            }
        }
        
        return template;
    }
    
    /// <summary>Medium rectangular room (8x8)</summary>
    public static RoomTemplateGrid2D MediumRectangular()
    {
        var template = new RoomTemplateGrid2D(8, 8);
        
        for (int x = 0; x < 8; x++)
        {
            for (int y = 0; y < 8; y++)
            {
                bool isEdge = x == 0 || x == 7 || y == 0 || y == 7;
                template.SetTile(x, y, isEdge ? '#' : '.');
            }
        }
        
        return template;
    }
    
    /// <summary>Large rectangular room (12x12)</summary>
    public static RoomTemplateGrid2D LargeRectangular()
    {
        var template = new RoomTemplateGrid2D(12, 12);
        
        for (int x = 0; x < 12; x++)
        {
            for (int y = 0; y < 12; y++)
            {
                bool isEdge = x == 0 || x == 11 || y == 0 || y == 11;
                template.SetTile(x, y, isEdge ? '#' : '.');
            }
        }
        
        return template;
    }
    
    /// <summary>Horizontal corridor (1xN)</summary>
    public static RoomTemplateGrid2D HorizontalCorridor(int length)
    {
        var template = new RoomTemplateGrid2D(length, 1);
        
        for (int x = 0; x < length; x++)
        {
            template.SetTile(x, 0, '#'); // All corridor tiles
        }
        
        return template;
    }
    
    /// <summary>Vertical corridor (Nx1)</summary>
    public static RoomTemplateGrid2D VerticalCorridor(int length)
    {
        var template = new RoomTemplateGrid2D(1, length);
        
        for (int y = 0; y < length; y++)
        {
            template.SetTile(0, y, '#'); // All corridor tiles
        }
        
        return template;
    }
    
    /// <summary>Get random room template based on size constraint</summary>
    public static RoomTemplateGrid2D GetRandomTemplate(Random random, int minSize, int maxSize)
    {
        var templates = new List<Func<RoomTemplateGrid2D>>
        {
            SmallRectangular,
            MediumRectangular,
            LargeRectangular
        };
        
        // Filter by size constraint
        var validTemplates = templates
            .Where(t => 
            {
                var temp = t();
                return temp.Width >= minSize && temp.Width <= maxSize;
            })
            .ToList();
        
        if (validTemplates.Count == 0)
            return SmallRectangular(); // Fallback
        
        int index = random.Next(validTemplates.Count);
        return validTemplates[index]();
    }
}
```

#### 2.3 DungeonGenerator (Core)

```csharp
namespace WingedBean.Plugins.DungeonGame.Generation;

using Microsoft.Extensions.Logging;
using Plate.EdgRogqd;
using Plate.EdgRogqd.Generation.Graph;
using WingedBean.Plugins.DungeonGame.Components;

/// <summary>
/// Procedural dungeon generator using Plate.EdgRogqd.
/// Converts graph-based layouts to tile-based DungeonMap.
/// </summary>
public class DungeonGenerator
{
    private readonly ILogger<DungeonGenerator>? _logger;
    
    public DungeonGenerator(ILogger<DungeonGenerator>? logger = null)
    {
        _logger = logger;
    }
    
    /// <summary>
    /// Generate a dungeon map based on options.
    /// </summary>
    public DungeonMap Generate(DungeonGeneratorOptions options)
    {
        _logger?.LogInformation("Generating dungeon with seed: {Seed}", 
            options.Seed ?? -1);
        
        var random = options.Seed.HasValue 
            ? new Random(options.Seed.Value) 
            : new Random();
        
        // Step 1: Build level description (room graph)
        var levelDesc = BuildLevelDescription(options, random);
        
        // Step 2: Generate layout using EdgRogqd
        var generator = new GraphBasedGeneratorGrid2D<int>(levelDesc);
        var layout = generator.GenerateLayout();
        
        _logger?.LogInformation("Generated layout with {RoomCount} rooms", 
            layout.Rooms.Count);
        
        // Step 3: Convert to DungeonMap
        var map = ConvertLayoutToMap(layout, options);
        
        _logger?.LogInformation("Created dungeon map: {Width}x{Height}", 
            map.Width, map.Height);
        
        return map;
    }
    
    /// <summary>
    /// Build room graph for EdgRogqd.
    /// Each node = room, each edge = corridor connection.
    /// </summary>
    private LevelDescriptionGrid2D<int> BuildLevelDescription(
        DungeonGeneratorOptions options, 
        Random random)
    {
        var levelDesc = new LevelDescriptionGrid2D<int>();
        
        // Determine number of rooms
        int roomCount = random.Next(options.MinRooms, options.MaxRooms + 1);
        
        // Add rooms to graph
        for (int i = 0; i < roomCount; i++)
        {
            // Assign template to room
            var template = RoomTemplates.GetRandomTemplate(
                random, 
                options.MinRoomSize, 
                options.MaxRoomSize);
            
            levelDesc.AddRoom(i, template);
        }
        
        // Connect rooms (linear chain for simplicity, can be enhanced)
        for (int i = 0; i < roomCount - 1; i++)
        {
            levelDesc.AddConnection(i, i + 1);
        }
        
        // Optional: Add cycles (extra connections)
        if (options.AllowCycles && roomCount > 3)
        {
            int cycleCount = random.Next(1, 3);
            for (int i = 0; i < cycleCount; i++)
            {
                int room1 = random.Next(roomCount);
                int room2 = random.Next(roomCount);
                if (room1 != room2)
                {
                    levelDesc.AddConnection(room1, room2);
                }
            }
        }
        
        return levelDesc;
    }
    
    /// <summary>
    /// Convert EdgRogqd layout to DungeonMap.
    /// </summary>
    private DungeonMap ConvertLayoutToMap(
        LayoutGrid2D<int> layout, 
        DungeonGeneratorOptions options)
    {
        var map = new DungeonMap(options.Width, options.Height);
        
        // Place rooms from layout
        foreach (var room in layout.Rooms)
        {
            PlaceRoom(map, room);
        }
        
        // Place corridors (connections between rooms)
        foreach (var corridor in layout.Corridors)
        {
            PlaceCorridor(map, corridor);
        }
        
        // Add doors at room entrances
        AddDoors(map, layout, options.DoorProbability);
        
        return map;
    }
    
    private void PlaceRoom(DungeonMap map, LayoutRoomGrid2D<int> room)
    {
        var position = room.Position;
        var template = room.RoomTemplate;
        
        for (int x = 0; x < template.Width; x++)
        {
            for (int y = 0; y < template.Height; y++)
            {
                int mapX = position.X + x;
                int mapY = position.Y + y;
                
                if (!map.IsInBounds(mapX, mapY)) continue;
                
                char tile = template.GetTile(x, y);
                map.SetTile(mapX, mapY, ConvertCharToTile(tile));
            }
        }
    }
    
    private void PlaceCorridor(DungeonMap map, Corridor corridor)
    {
        // Simple straight corridor implementation
        // Can be enhanced with L-shaped corridors
        
        int x1 = corridor.Start.X;
        int y1 = corridor.Start.Y;
        int x2 = corridor.End.X;
        int y2 = corridor.End.Y;
        
        // Horizontal corridor
        if (y1 == y2)
        {
            int startX = Math.Min(x1, x2);
            int endX = Math.Max(x1, x2);
            
            for (int x = startX; x <= endX; x++)
            {
                if (map.IsInBounds(x, y1))
                {
                    map.SetTile(x, y1, MapTile.Floor);
                }
            }
        }
        // Vertical corridor
        else if (x1 == x2)
        {
            int startY = Math.Min(y1, y2);
            int endY = Math.Max(y1, y2);
            
            for (int y = startY; y <= endY; y++)
            {
                if (map.IsInBounds(x1, y))
                {
                    map.SetTile(x1, y, MapTile.Floor);
                }
            }
        }
        // L-shaped corridor (go horizontal then vertical)
        else
        {
            // Horizontal segment
            for (int x = Math.Min(x1, x2); x <= Math.Max(x1, x2); x++)
            {
                if (map.IsInBounds(x, y1))
                {
                    map.SetTile(x, y1, MapTile.Floor);
                }
            }
            
            // Vertical segment
            for (int y = Math.Min(y1, y2); y <= Math.Max(y1, y2); y++)
            {
                if (map.IsInBounds(x2, y))
                {
                    map.SetTile(x2, y, MapTile.Floor);
                }
            }
        }
    }
    
    private void AddDoors(DungeonMap map, LayoutGrid2D<int> layout, float doorProb)
    {
        var random = new Random();
        
        // Find wall tiles adjacent to floor tiles (potential door locations)
        for (int x = 1; x < map.Width - 1; x++)
        {
            for (int y = 1; y < map.Height - 1; y++)
            {
                var tile = map.GetTile(x, y);
                if (tile.Type != TileType.Wall) continue;
                
                // Check if this wall separates two floor tiles
                bool doorCandidate = IsDoorCandidate(map, x, y);
                
                if (doorCandidate && random.NextDouble() < doorProb)
                {
                    map.SetTile(x, y, MapTile.Door);
                }
            }
        }
    }
    
    private bool IsDoorCandidate(DungeonMap map, int x, int y)
    {
        // Check horizontal door (floor left and right)
        bool horizontal = 
            map.GetTile(x - 1, y).Type == TileType.Floor &&
            map.GetTile(x + 1, y).Type == TileType.Floor;
        
        // Check vertical door (floor above and below)
        bool vertical = 
            map.GetTile(x, y - 1).Type == TileType.Floor &&
            map.GetTile(x, y + 1).Type == TileType.Floor;
        
        return horizontal || vertical;
    }
    
    private MapTile ConvertCharToTile(char c)
    {
        return c switch
        {
            '#' => MapTile.Wall,
            '.' => MapTile.Floor,
            '+' => MapTile.Door,
            ' ' => MapTile.Empty,
            _ => MapTile.Empty
        };
    }
}
```

---

### 3. Data Integration

#### 3.1 Update GameResourceData

```csharp
// Data/GameResourceData.cs

public class DungeonLevelData
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public string? Description { get; init; }
    public int Floor { get; init; }
    public required DimensionsData Dimensions { get; init; }
    
    // NEW: Generation configuration
    public GenerationConfigData? Generation { get; init; }
    
    public EnemySpawnData[]? EnemySpawns { get; init; }
    public ItemSpawnData[]? ItemSpawns { get; init; }
}

// NEW: Generation configuration
public class GenerationConfigData
{
    public int? Seed { get; init; }
    public RoomConfigData? Rooms { get; init; }
    public int CorridorWidth { get; init; } = 1;
    public bool AllowCycles { get; init; } = false;
    public float DoorProbability { get; init; } = 0.3f;
}

public class RoomConfigData
{
    public int Min { get; init; } = 5;
    public int Max { get; init; } = 10;
    public int MinSize { get; init; } = 4;
    public int MaxSize { get; init; } = 12;
}
```

#### 3.2 Example Level JSON

```json
{
  "id": "dungeon-level-01",
  "name": "The Dark Caves",
  "description": "A procedurally generated cave system",
  "floor": 1,
  "dimensions": {
    "width": 80,
    "height": 40
  },
  "generation": {
    "seed": 12345,
    "rooms": {
      "min": 5,
      "max": 10,
      "minSize": 5,
      "maxSize": 12
    },
    "corridorWidth": 1,
    "allowCycles": false,
    "doorProbability": 0.3
  },
  "enemySpawns": [
    {
      "enemyId": "goblin",
      "count": { "min": 3, "max": 5 },
      "spawnType": "random-rooms"
    }
  ]
}
```

---

### 4. DungeonGame Integration

```csharp
// DungeonGame.cs

private async Task InitializeWorldFromResourcesAsync()
{
    var levelData = await _resourceLoader!.LoadDungeonLevelAsync("dungeon-level-01");
    if (levelData == null)
    {
        InitializeWorldLegacy();
        return;
    }
    
    // NEW: Generate dungeon map
    if (levelData.Generation != null)
    {
        _dungeonMap = GenerateDungeon(levelData);
    }
    else
    {
        _dungeonMap = CreateTestMap(); // Fallback
    }
    
    // Find valid spawn position
    var spawnPos = FindWalkablePosition(_dungeonMap);
    
    // Create player
    var playerData = await _resourceLoader.LoadPlayerAsync("default-player");
    if (playerData != null)
    {
        playerData.StartingPosition.X = spawnPos.X;
        playerData.StartingPosition.Y = spawnPos.Y;
        EntityFactory.CreatePlayer(_world, playerData);
    }
    
    // Spawn enemies in walkable areas
    await EntityFactory.SpawnEnemiesFromLevelAsync(
        _world, levelData, _resourceLoader, _dungeonMap, Random.Shared, _logger);
}

private DungeonMap GenerateDungeon(DungeonLevelData levelData)
{
    var options = new DungeonGeneratorOptions
    {
        Width = levelData.Dimensions.Width,
        Height = levelData.Dimensions.Height,
        Seed = levelData.Generation?.Seed,
        MinRooms = levelData.Generation?.Rooms?.Min ?? 5,
        MaxRooms = levelData.Generation?.Rooms?.Max ?? 10,
        MinRoomSize = levelData.Generation?.Rooms?.MinSize ?? 4,
        MaxRoomSize = levelData.Generation?.Rooms?.MaxSize ?? 12,
        CorridorWidth = levelData.Generation?.CorridorWidth ?? 1,
        AllowCycles = levelData.Generation?.AllowCycles ?? false,
        DoorProbability = levelData.Generation?.DoorProbability ?? 0.3f
    };
    
    var generator = new DungeonGenerator(_logger);
    return generator.Generate(options);
}

private (int X, int Y) FindWalkablePosition(DungeonMap map)
{
    var walkable = map.GetWalkablePositions().ToList();
    if (walkable.Count == 0)
    {
        // Fallback to center
        return (map.Width / 2, map.Height / 2);
    }
    
    // Return first walkable position (or random)
    return walkable[0];
}
```

---

## Implementation Plan

### Phase 1: Core Generation (4 hours)
- [ ] Create `Generation/DungeonGeneratorOptions.cs`
- [ ] Create `Generation/RoomTemplates.cs`
- [ ] Create `Generation/DungeonGenerator.cs`
- [ ] Unit tests for generator

### Phase 2: Data Integration (2 hours)
- [ ] Update `GameResourceData.cs` with generation config
- [ ] Create sample level JSON with generation section
- [ ] Update `ResourceLoader.cs` to parse generation config

### Phase 3: Game Integration (3 hours)
- [ ] Update `DungeonGame.cs` to call generator
- [ ] Update spawn logic to use walkable positions
- [ ] Test with multiple seeds

### Phase 4: Testing & Tuning (3 hours)
- [ ] Visual tests (render generated dungeons)
- [ ] Connectivity tests (all rooms reachable)
- [ ] Performance tests (generation time)
- [ ] Parameter tuning (room count, size)

---

## Testing Strategy

### Unit Tests
```csharp
[Fact]
public void DungeonGenerator_GeneratesConnectedDungeon()
{
    var options = new DungeonGeneratorOptions 
    { 
        Seed = 42, 
        MinRooms = 5, 
        MaxRooms = 5 
    };
    
    var generator = new DungeonGenerator();
    var map = generator.Generate(options);
    
    // Verify connectivity using flood fill
    Assert.True(IsAllRoomsConnected(map));
}

[Fact]
public void DungeonGenerator_SameSeed_GeneratesSameMap()
{
    var options = new DungeonGeneratorOptions { Seed = 12345 };
    
    var map1 = new DungeonGenerator().Generate(options);
    var map2 = new DungeonGenerator().Generate(options);
    
    Assert.True(MapsAreEqual(map1, map2));
}
```

### Integration Tests
- Generated dungeons load in game
- Player spawns in walkable area
- Enemies spawn in valid rooms
- All UI rendering works

---

## Performance Considerations

| Operation | Target | Typical |
|-----------|--------|---------|
| **Generation** | < 100ms | ~50ms |
| **Map Size** | 80x40 | 3,200 tiles |
| **Room Count** | 5-10 | 7 avg |
| **Memory** | < 1MB | ~500KB |

**Optimization Strategies:**
- Cache generated maps by seed
- Lazy evaluation of walkable positions
- Pre-allocate tile arrays

---

## Risks & Mitigations

| Risk | Impact | Mitigation |
|------|--------|------------|
| **EdgRogqd incomplete** | High | Implement fallback BSP generator |
| **Generation failures** | Medium | Retry with different seed, fallback to static |
| **Performance issues** | Low | Profile and optimize hot paths |
| **Poor layouts** | Medium | Parameter tuning, template variety |

---

## Future Enhancements

1. **Special Room Types** (RFC-TBD)
   - Boss rooms, treasure rooms, shops
   - Custom templates per room type

2. **Multi-Floor Dungeons** (RFC-TBD)
   - Stairs/portals between floors
   - Connected floor generation

3. **Themed Dungeons** (RFC-TBD)
   - Cave themes, castle themes, etc.
   - Template sets per theme

4. **Advanced Algorithms** (RFC-TBD)
   - BSP + cellular automata hybrid
   - Voronoi diagram rooms
   - Grammar-based generation

---

## Related RFCs

- **RFC-0043**: Tile-Based Map System ✅ Required Dependency
- **RFC-TBD**: Pathfinding with ModernSatsuma 🔮 Future
- **RFC-TBD**: GOAP AI for Dungeon Exploration 🔮 Future

---

## References

- Edgar Algorithm: [Edgar-Unity Paper](https://dspace.cvut.cz/handle/10467/76816)
- Plate.EdgRogqd: `/plate-projects/edg-rogqd`
- Roguelike Generation: [RogueBasin](http://www.roguebasin.com/index.php?title=Dungeon_generator)

---

**Decision:** Proposed - Awaiting Review  
**Next Steps:**
1. Review with team
2. Complete RFC-0043 (prerequisite)
3. Create implementation branch
4. Begin Phase 1 (Core Generation)

# Dungeon Generation Integration Plan

**Date**: 2025-10-14  
**Goal**: Integrate Plate.EdgRogqd for procedural dungeon generation so players can walk around generated dungeons

---

## Current State

### Existing Components

1. **WingedBean.Plugins.DungeonGame** - Core game plugin
   - ECS-based architecture using ArchECS
   - Player movement, combat, AI systems
   - Resource-driven data loading (JSON)
   - Components: Position, Health, Renderable, AI, Movement

2. **Current Map System** - Static/Data-Driven
   - `DungeonLevelData` defines static level properties
   - Dimensions, enemy spawns, item spawns defined in JSON
   - No actual tile/wall generation - just spawn areas

3. **Available Libraries**
   - ✅ **Plate.EdgRogqd** - Dungeon generation (graph-based)
   - ✅ **Plate.ModernSatsuma** - Graph algorithms (pathfinding, connectivity)
   - ✅ **Plate.GoAP-CKPor** - AI planning (for smarter NPCs)
   - ✅ **Plate.Capability-Orlia** - Capability evaluation (for actions)

---

## Integration Plan

### Phase 1: Add Map/Tile System (Foundation)

**Goal**: Add tile-based map representation to support generated dungeons

#### 1.1 Create Tile Types
```csharp
// Components/TileType.cs
public enum TileType
{
    Empty,      // Void/unpassable
    Floor,      // Walkable floor
    Wall,       // Blocking wall
    Door,       // Passable door
    Corridor    // Connecting passage
}

// Components/MapTile.cs
public struct MapTile
{
    public TileType Type { get; set; }
    public bool IsWalkable { get; set; }
    public bool BlocksVision { get; set; }
    public char Symbol { get; set; }
}
```

#### 1.2 Create Map Component
```csharp
// Components/DungeonMap.cs
public class DungeonMap
{
    public int Width { get; }
    public int Height { get; }
    private MapTile[,] _tiles;
    
    public MapTile GetTile(int x, int y);
    public void SetTile(int x, int y, MapTile tile);
    public bool IsWalkable(int x, int y);
    public bool IsInBounds(int x, int y);
}
```

#### 1.3 Update Movement System
- Check map tiles before allowing movement
- Validate walkability from `DungeonMap`
- Update collision detection

**Files to Modify**:
- `Systems/MovementSystem.cs` - Add map collision checks
- `DungeonGame.cs` - Create and attach map component

---

### Phase 2: Integrate EdgRogqd Generator

**Goal**: Use EdgRogqd to procedurally generate dungeon layouts

#### 2.1 Create Generator Wrapper
```csharp
// Generation/DungeonGenerator.cs
public class DungeonGenerator
{
    private readonly ILogger? _logger;
    
    public DungeonMap Generate(DungeonGeneratorOptions options)
    {
        // Use Plate.EdgRogqd.GraphBasedGeneratorGrid2D
        // Convert LayoutGrid2D to our DungeonMap format
    }
}
```

#### 2.2 Define Room Templates
```csharp
// Generation/RoomTemplates.cs
public static class RoomTemplates
{
    // Simple rectangular rooms
    public static RoomTemplateGrid2D SmallRoom => /* 5x5 */;
    public static RoomTemplateGrid2D MediumRoom => /* 8x8 */;
    public static RoomTemplateGrid2D LargeRoom => /* 12x12 */;
    
    // Corridor templates
    public static RoomTemplateGrid2D HorizontalCorridor => /* 1xN */;
    public static RoomTemplateGrid2D VerticalCorridor => /* Nx1 */;
}
```

#### 2.3 Create Level Description
```csharp
// Generation/DungeonLevelDescription.cs
public class DungeonLevelDescription
{
    public int NumberOfRooms { get; set; } = 8;
    public bool AllowCycles { get; set; } = false;
    
    public LevelDescriptionGrid2D<int> ToEdgRogqdDescription()
    {
        // Build graph of rooms
        // Assign room templates
        // Return EdgRogqd LevelDescription
    }
}
```

#### 2.4 Update DungeonGame Initialization
```csharp
// In DungeonGame.cs - InitializeWorldFromResourcesAsync
private async Task InitializeWorldFromResourcesAsync()
{
    // NEW: Generate dungeon map
    var generator = new DungeonGenerator(_logger);
    var options = new DungeonGeneratorOptions 
    {
        Width = levelData.Dimensions.Width,
        Height = levelData.Dimensions.Height,
        Seed = levelData.Seed ?? Random.Shared.Next()
    };
    
    _dungeonMap = generator.Generate(options);
    _logger?.LogInformation("Generated dungeon map: {Width}x{Height}", 
        _dungeonMap.Width, _dungeonMap.Height);
    
    // Existing: Create player, enemies, items
    // NEW: Use generated walkable tiles for spawn positions
}
```

**Files to Create**:
- `Generation/DungeonGenerator.cs`
- `Generation/RoomTemplates.cs`
- `Generation/DungeonLevelDescription.cs`

**Files to Modify**:
- `DungeonGame.cs` - Add map generation
- `Data/GameResourceData.cs` - Add `Seed` property to DungeonLevelData

---

### Phase 3: Rendering & Visualization

**Goal**: Display generated dungeon tiles in UI

#### 3.1 Update Render System
```csharp
// Systems/RenderSystem.cs
public override void Update(IWorld world, float deltaTime)
{
    // NEW: First render map tiles
    RenderMap(world);
    
    // Existing: Render entities
    RenderEntities(world);
}

private void RenderMap(IWorld world)
{
    var map = world.Get<DungeonMap>();
    for (int y = 0; y < map.Height; y++)
    {
        for (int x = 0; x < map.Width; x++)
        {
            var tile = map.GetTile(x, y);
            // Emit render event with tile symbol
        }
    }
}
```

#### 3.2 Update Console UI Plugin
```csharp
// WingedBean.Plugins.ConsoleDungeon/ConsoleDungeonAppRefactored.cs
// Subscribe to map tile render events
// Draw tiles with appropriate symbols:
//   '#' for walls
//   '.' for floors  
//   '+' for doors
```

#### 3.3 Update Desktop UI Plugin (SadConsole)
```csharp
// WingedBean.Plugins.SadConsole/DungeonGameScreen.cs
// Render map tiles to console surface
// Use colored glyphs for different tile types
```

**Files to Modify**:
- `Systems/RenderSystem.cs`
- `../WingedBean.Plugins.ConsoleDungeon/ConsoleDungeonAppRefactored.cs`
- `../../windows/src/plugins/WingedBean.Plugins.SadConsole/DungeonGameScreen.cs`

---

### Phase 4: Enhanced Features (Future)

#### 4.1 Pathfinding with ModernSatsuma
- Use Dijkstra/A* for enemy pathfinding
- Create navigation graph from walkable tiles
- Update AI system to use pathfinding

#### 4.2 Room Types & Themes
- Special rooms: boss, treasure, shop
- Room decorations/features
- Environmental hazards

#### 4.3 Multi-Floor Dungeons
- Stairs/portals between floors
- Generate connected floor layouts
- Persistent floor state

#### 4.4 GOAP AI Integration
- Enemies use GOAP for decision making
- Goals: patrol, chase, flee, investigate
- Actions: move, attack, use item
- Sensors: detect player, check health

---

## Implementation Order

### Sprint 1: Foundation (This Session)
1. ✅ Integrate plate projects (DONE)
2. ✅ Plan dungeon generation (THIS DOCUMENT)
3. ⏭️ Add MapTile and DungeonMap components
4. ⏭️ Update MovementSystem for collision

### Sprint 2: Generation
1. Create DungeonGenerator wrapper
2. Define basic room templates
3. Integrate EdgRogqd generation
4. Test generation with different seeds

### Sprint 3: Rendering
1. Update RenderSystem for map tiles
2. Update console UI for tile rendering
3. Update desktop UI for tile rendering
4. Visual polish (colors, symbols)

### Sprint 4: Enhancement
1. Add pathfinding using ModernSatsuma
2. Smarter spawn positions (not in walls!)
3. Special rooms and features
4. Save/load generated maps

---

## Data Structure Changes

### Add to DungeonLevelData
```json
{
  "id": "dungeon-level-01",
  "name": "The Dark Caves",
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
    "allowCycles": false
  },
  "enemySpawns": [ /* ... */ ],
  "itemSpawns": [ /* ... */ ]
}
```

---

## Technical Considerations

### Performance
- Generate dungeon once at level load
- Cache generated map for level restart
- Consider chunking for very large dungeons

### Testing
- Unit tests for tile operations
- Integration tests for generation
- Visual tests for rendering
- Seed-based reproducibility

### Compatibility
- Works with both console and desktop hosts
- Maintains existing ECS architecture
- Backward compatible with static levels (if needed)

---

## Next Steps

**Immediate**:
1. Create `Components/MapTile.cs` and `Components/DungeonMap.cs`
2. Add map field to `DungeonGame.cs`
3. Update `MovementSystem.cs` to check walkability
4. Test movement with hardcoded map

**After Foundation**:
1. Implement `DungeonGenerator` using EdgRogqd
2. Create basic room templates
3. Generate first procedural dungeon
4. Update rendering to show tiles

---

## Questions to Resolve

1. **Map Storage**: Store as component or separate service?
   - **Decision**: Component attached to World for ECS consistency

2. **Generation Timing**: On load or async background?
   - **Decision**: On level load (synchronous) - fast enough for small dungeons

3. **Room Template Format**: Hardcoded or data-driven (JSON)?
   - **Decision**: Start hardcoded, move to JSON if needed

4. **Coordinate System**: Top-left (0,0) or bottom-left (0,0)?
   - **Decision**: Top-left to match screen rendering conventions

---

## Success Criteria

- ✅ Player can walk around generated dungeon
- ✅ Walls block movement
- ✅ Enemies spawn in walkable areas only
- ✅ Map is rendered correctly in both console and desktop
- ✅ Different seeds produce different layouts
- ✅ No entities spawn inside walls
- ✅ All rooms are connected and reachable

---

**Status**: 📋 PLANNED  
**Next Action**: Implement Phase 1 - Map/Tile System Foundation

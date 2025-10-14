# Dungeon Generation Architecture

## System Overview

```
┌─────────────────────────────────────────────────────────────────────┐
│                          WingedBean Game                              │
├─────────────────────────────────────────────────────────────────────┤
│                                                                       │
│  ┌─────────────────┐         ┌──────────────────┐                  │
│  │  DungeonGame    │────────▶│  ArchECS World   │                  │
│  │  (Main Logic)   │         │  (Entities)      │                  │
│  └────────┬────────┘         └──────────────────┘                  │
│           │                                                          │
│           │ owns                                                     │
│           │                                                          │
│  ┌────────▼───────────────────────────────────────────┐            │
│  │              DungeonMap Component                   │            │
│  │  ┌──────────────────────────────────────────────┐  │            │
│  │  │  MapTile[,] _tiles (2D Array)                 │  │            │
│  │  │  - TileType (Floor, Wall, Door, etc)          │  │            │
│  │  │  - IsWalkable, BlocksVision                   │  │            │
│  │  │  - Visual Properties (symbol, color)          │  │            │
│  │  └──────────────────────────────────────────────┘  │            │
│  │                                                      │            │
│  │  Methods:                                            │            │
│  │  - GetTile(x, y) → MapTile                          │            │
│  │  - IsWalkable(x, y) → bool                          │            │
│  │  - IsInBounds(x, y) → bool                          │            │
│  └──────────────────────────────────────────────────────┘            │
│                                                                       │
└─────────────────────────────────────────────────────────────────────┘
```

## Generation Pipeline

```
┌──────────────────────────────────────────────────────────────────┐
│ 1. Load Level Data (JSON)                                         │
│    ┌─────────────────────────────────────────────┐               │
│    │  DungeonLevelData                            │               │
│    │  {                                           │               │
│    │    "dimensions": { width: 80, height: 40 }   │               │
│    │    "generation": {                           │               │
│    │      "seed": 12345,                          │               │
│    │      "rooms": { min: 5, max: 10 }            │               │
│    │    }                                          │               │
│    │  }                                           │               │
│    └─────────────────────────────────────────────┘               │
└────────────────────┬─────────────────────────────────────────────┘
                     │
                     ▼
┌──────────────────────────────────────────────────────────────────┐
│ 2. Create Level Description                                       │
│    ┌─────────────────────────────────────────────┐               │
│    │  DungeonLevelDescription                     │               │
│    │  - Build room graph (nodes & edges)          │               │
│    │  - Assign room templates                     │               │
│    │  - Configure generation parameters           │               │
│    └─────────────────────────────────────────────┘               │
└────────────────────┬─────────────────────────────────────────────┘
                     │
                     ▼
┌──────────────────────────────────────────────────────────────────┐
│ 3. Generate Layout (Plate.EdgRogqd)                               │
│    ┌─────────────────────────────────────────────┐               │
│    │  GraphBasedGeneratorGrid2D                   │               │
│    │  1. Create room positions                    │               │
│    │  2. Connect rooms with corridors             │               │
│    │  3. Resolve overlaps                         │               │
│    │  4. Place doors                              │               │
│    │                                              │               │
│    │  Output: LayoutGrid2D<Room>                  │               │
│    └─────────────────────────────────────────────┘               │
└────────────────────┬─────────────────────────────────────────────┘
                     │
                     ▼
┌──────────────────────────────────────────────────────────────────┐
│ 4. Convert to DungeonMap                                          │
│    ┌─────────────────────────────────────────────┐               │
│    │  DungeonGenerator.Generate()                 │               │
│    │  - Convert LayoutGrid2D to MapTile[,]        │               │
│    │  - Set tile types (floor, wall, door)        │               │
│    │  - Mark walkable areas                       │               │
│    │  - Assign visual properties                  │               │
│    │                                              │               │
│    │  Output: DungeonMap                          │               │
│    └─────────────────────────────────────────────┘               │
└────────────────────┬─────────────────────────────────────────────┘
                     │
                     ▼
┌──────────────────────────────────────────────────────────────────┐
│ 5. Spawn Entities                                                 │
│    ┌─────────────────────────────────────────────┐               │
│    │  - Find walkable positions                   │               │
│    │  - Spawn player at start position            │               │
│    │  - Spawn enemies in random rooms             │               │
│    │  - Spawn items in random rooms               │               │
│    │  - Verify no spawns in walls                 │               │
│    └─────────────────────────────────────────────┘               │
└────────────────────┬─────────────────────────────────────────────┘
                     │
                     ▼
┌──────────────────────────────────────────────────────────────────┐
│ 6. Game Running                                                   │
│    - Movement System checks map.IsWalkable()                      │
│    - Render System draws tiles then entities                      │
│    - AI System uses pathfinding on walkable tiles                 │
└──────────────────────────────────────────────────────────────────┘
```

## Room Template System

```
┌────────────────────────────────────────────────────────┐
│                   Room Templates                        │
├────────────────────────────────────────────────────────┤
│                                                         │
│  Small Room (5x5)      Medium Room (8x8)               │
│  #####                 ########                         │
│  #...#                 #......#                         │
│  #...#                 #......#                         │
│  #...#                 #......#                         │
│  #####                 #......#                         │
│                        #......#                         │
│                        #......#                         │
│                        ########                         │
│                                                         │
│  Large Room (12x12)    Corridor (1xN)                  │
│  ############          #                                │
│  #..........#          #                                │
│  #..........#          #                                │
│  #..........#          #                                │
│  #..........#          #                                │
│  #..........#                                           │
│  #..........#                                           │
│  #..........#                                           │
│  #..........#                                           │
│  #..........#                                           │
│  #..........#                                           │
│  ############                                           │
│                                                         │
│  Legend:                                                │
│  #  = Wall tile (not walkable)                          │
│  .  = Floor tile (walkable)                             │
│  +  = Door tile (walkable, connects rooms)              │
│                                                         │
└────────────────────────────────────────────────────────┘
```

## Component Relationships

```
┌─────────────────────────────────────────────────────────┐
│                    ECS Systems Layer                     │
├─────────────────────────────────────────────────────────┤
│                                                          │
│  ┌──────────────┐    ┌──────────────┐                  │
│  │ Movement     │    │ Render       │                  │
│  │ System       │    │ System       │                  │
│  └──────┬───────┘    └──────┬───────┘                  │
│         │                   │                           │
│         │ reads             │ reads                     │
│         │                   │                           │
│  ┌──────▼───────────────────▼──────────┐               │
│  │       DungeonMap Component           │               │
│  │  (Attached to World)                 │               │
│  └──────────────────────────────────────┘               │
│                                                          │
│  ┌──────────────┐    ┌──────────────┐                  │
│  │ Entity       │    │ Entity       │                  │
│  │ (Player)     │    │ (Enemy)      │                  │
│  │ ┌──────────┐ │    │ ┌──────────┐ │                  │
│  │ │Position  │ │    │ │Position  │ │                  │
│  │ │x, y      │ │    │ │x, y      │ │                  │
│  │ └──────────┘ │    │ └──────────┘ │                  │
│  └──────────────┘    └──────────────┘                  │
│         ▲                   ▲                           │
│         │                   │                           │
│         └───────────────────┘                           │
│              Must be on                                 │
│            walkable tiles                               │
│                                                          │
└─────────────────────────────────────────────────────────┘
```

## Integration with Plate Libraries

```
┌────────────────────────────────────────────────────────────┐
│              Plate Library Stack                            │
├────────────────────────────────────────────────────────────┤
│                                                             │
│  ┌──────────────────────────────────────────────────────┐ │
│  │  Plate.EdgRogqd (Dungeon Generation)                  │ │
│  │  - GraphBasedGeneratorGrid2D                          │ │
│  │  - LevelDescriptionGrid2D                             │ │
│  │  - RoomTemplateGrid2D                                 │ │
│  └─────────────────┬────────────────────────────────────┘ │
│                    │ uses                                  │
│  ┌─────────────────▼────────────────────────────────────┐ │
│  │  Plate.ModernSatsuma (Graph Algorithms)               │ │
│  │  - Graph data structures                              │ │
│  │  - Dijkstra pathfinding (future)                      │ │
│  │  - Connectivity checks                                │ │
│  └───────────────────────────────────────────────────────┘ │
│                                                             │
│  ┌──────────────────────────────────────────────────────┐ │
│  │  Plate.GoAP-CKPor (AI Planning) - FUTURE              │ │
│  │  - Enemy decision making                              │ │
│  │  - Goal: Patrol rooms, Chase player                   │ │
│  │  - Actions: Move to room, Attack                      │ │
│  └───────────────────────────────────────────────────────┘ │
│                                                             │
│  ┌──────────────────────────────────────────────────────┐ │
│  │  Plate.Capability-Orlia (Capabilities) - FUTURE       │ │
│  │  - Check if enemy can attack                          │ │
│  │  - Check if player can use item                       │ │
│  │  - Check if door can be opened                        │ │
│  └───────────────────────────────────────────────────────┘ │
│                                                             │
└────────────────────────────────────────────────────────────┘
```

## File Structure (After Implementation)

```
WingedBean.Plugins.DungeonGame/
├── Components/
│   ├── TileType.cs              ← NEW: Enum for tile types
│   ├── MapTile.cs               ← NEW: Tile data structure
│   ├── DungeonMap.cs            ← NEW: 2D map container
│   ├── Position.cs              (existing)
│   ├── Health.cs                (existing)
│   └── ...
├── Generation/                  ← NEW: Generation logic
│   ├── DungeonGenerator.cs      ← NEW: Main generator
│   ├── RoomTemplates.cs         ← NEW: Template definitions
│   ├── DungeonLevelDescription.cs ← NEW: Level config
│   └── DungeonGeneratorOptions.cs ← NEW: Gen options
├── Systems/
│   ├── MovementSystem.cs        ← MODIFY: Add collision
│   ├── RenderSystem.cs          ← MODIFY: Add map rendering
│   ├── AISystem.cs              (existing)
│   └── CombatSystem.cs          (existing)
├── Data/
│   ├── GameResourceData.cs      ← MODIFY: Add seed field
│   ├── EntityFactory.cs         (existing)
│   └── ResourceLoader.cs        (existing)
├── DungeonGame.cs               ← MODIFY: Add map generation
└── DungeonGamePlugin.cs         (existing)
```

## Data Flow Example

```
Player presses 'W' (move up)
    │
    ▼
Input Handler captures key
    │
    ▼
MovementSystem.Update()
    │
    ├─ Get entity's current position (x, y)
    │
    ├─ Calculate new position (x, y-1)
    │
    ├─ Check: map.IsInBounds(x, y-1)?
    │      ├─ No → Cancel movement
    │      └─ Yes → Continue
    │
    ├─ Check: map.IsWalkable(x, y-1)?
    │      ├─ No → Cancel movement (hit wall)
    │      └─ Yes → Update entity position
    │
    └─ Emit PositionChanged event
           │
           ▼
    RenderSystem updates display
```

---

**Architecture**: Modular, ECS-based, Data-driven  
**Performance**: Pre-generated maps, cached tiles  
**Extensibility**: Template-based rooms, pluggable generators  
**Status**: 📐 DESIGNED - Ready for implementation

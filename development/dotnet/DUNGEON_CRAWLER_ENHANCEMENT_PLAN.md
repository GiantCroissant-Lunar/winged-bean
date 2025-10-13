# Dungeon Crawler Enhancement Plan

## Executive Summary

This document outlines the missing features and proposed architecture for transforming the current basic ECS game into a full-featured dungeon crawler.

---

## Current State Analysis

### ✅ Existing Components

**Location:** `console/src/plugins/WingedBean.Plugins.DungeonGame/`

#### Components (`Components/`)
- **EntityComponents.cs**
  - `Player` - Tag component for player entity
  - `Enemy` - AI component (Type, State, AggroRange, Target)
  - `EnemyType` enum - Goblin, Orc, Skeleton, Troll, Dragon
  - `AIState` enum - Idle, Patrol, Chase, Attack, Flee
  - `Item` - Item component with type and value

- **CoreComponents.cs**
  - `Position` - X, Y, Floor coordinates
  - `Stats` - HP, Mana, Strength, Dexterity, Intelligence, Defense, Level, Experience
  - `Renderable` - Symbol, Colors, RenderLayer
  - `Blocking` - BlocksMovement, BlocksLight (partially implemented)

#### Systems (`Systems/`)
- **AISystem.cs** - Basic enemy AI (aggro detection, chase, flee)
- **CombatSystem.cs** - Adjacent melee combat
- **MovementSystem.cs** - Position validation and bounds checking
- **RenderSystem.cs** - Simple text buffer rendering

#### Game Logic
- **DungeonGame.cs** - ECS initialization, world management, game loop
- **EntityFactory.cs** - Data-driven entity creation
- **ResourceLoader.cs** - JSON-based resource loading

---

## Missing Critical Features

### 1. ❌ Environment/Tilemap System

**Current State:** No dungeon structure exists. Entities float in empty space.

**Required:**
- Dungeon map data structure (2D grid per floor)
- Tile types (floor, wall, door, stairs, trap, etc.)
- Collision detection against tiles
- Door mechanics (open/closed/locked)
- Trap mechanics
- Environmental effects (lava, water, ice)

**Proposed Components:**
```csharp
// New file: Components/EnvironmentComponents.cs

public struct Tile
{
    public TileType Type;
    public bool IsWalkable;
    public bool IsTransparent;  // For FOV
    public bool IsExplored;     // For fog of war
    public bool IsVisible;      // Current visibility
}

public enum TileType
{
    Floor,
    Wall,
    Door,
    LockedDoor,
    StairsUp,
    StairsDown,
    Trap,
    Water,
    Lava,
    Chasm
}

public struct DungeonMap
{
    public int Width;
    public int Height;
    public int Floor;
    public Tile[,] Tiles;
}

public struct Door
{
    public bool IsOpen;
    public bool IsLocked;
    public string? RequiredKeyId;
}

public struct Trap
{
    public TrapType Type;
    public int Damage;
    public bool IsTriggered;
    public bool IsVisible;
}

public enum TrapType
{
    Spike,
    Arrow,
    Poison,
    Teleport,
    Alarm
}
```

**Proposed System:**
```csharp
// New file: Systems/EnvironmentSystem.cs

public class EnvironmentSystem : IECSSystem
{
    private Dictionary<int, DungeonMap> _maps = new();
    
    public void Execute(IWorld world, float deltaTime)
    {
        // Handle door interactions
        // Trigger traps
        // Apply environmental effects
        // Check tile-based collisions
    }
}
```

---

### 2. ❌ Camera/Viewport System

**Current State:** RenderSystem renders a fixed 40x15 area with no camera tracking.

**Required:**
- Camera position following player
- Viewport boundaries
- Smooth scrolling
- Zoom levels (optional)
- Minimap rendering

**Proposed Components:**
```csharp
// New file: Components/CameraComponents.cs

public struct Camera
{
    public int X;
    public int Y;
    public int Floor;
    public int ViewportWidth;
    public int ViewportHeight;
    public EntityHandle? FollowTarget;  // Usually the player
    public CameraMode Mode;
}

public enum CameraMode
{
    FollowPlayer,
    Free,
    Cinematic
}

public struct Viewport
{
    public int ScreenX;
    public int ScreenY;
    public int Width;
    public int Height;
}
```

**Proposed System:**
```csharp
// New file: Systems/CameraSystem.cs

public class CameraSystem : IECSSystem
{
    public void Execute(IWorld world, float deltaTime)
    {
        // Find camera entity
        // Update camera position to follow target
        // Clamp camera to dungeon bounds
        // Calculate viewport boundaries
    }
}
```

**Required Changes to RenderSystem:**
- Accept camera position
- Render only tiles within viewport
- Transform world coordinates to screen coordinates

---

### 3. ❌ Field of View (FOV) / Line of Sight System

**Current State:** No visibility system. All entities always visible.

**Required:**
- Fog of war (explored vs visible)
- Vision radius based on light sources
- Shadowcasting algorithm
- Light sources (torches, spells, etc.)
- Dynamic lighting

**Proposed Components:**
```csharp
// New file: Components/VisionComponents.cs

public struct Vision
{
    public int Range;           // How far entity can see
    public float Angle;         // Field of view angle (360 = omnidirectional)
    public VisionType Type;
}

public enum VisionType
{
    Normal,
    Darkvision,
    Blindsight,
    Tremorsense
}

public struct LightSource
{
    public int Radius;
    public float Intensity;
    public ConsoleColor Color;
    public bool IsActive;
}

public struct Visibility
{
    public bool IsVisible;      // Currently visible to player
    public bool IsExplored;     // Has been seen before
    public float LightLevel;    // 0.0 to 1.0
}
```

**Proposed System:**
```csharp
// New file: Systems/FOVSystem.cs

public class FOVSystem : IECSSystem
{
    public void Execute(IWorld world, float deltaTime)
    {
        // Find player position
        // Calculate visible tiles using shadowcasting
        // Update tile visibility
        // Update entity visibility
        // Apply light sources
    }
    
    private void ComputeFOV(Position origin, int range, DungeonMap map)
    {
        // Implement shadowcasting algorithm
        // Recursive shadowcasting or permissive FOV
    }
}
```

**Algorithm Recommendation:** Recursive Shadowcasting (fast, accurate)

---

### 4. ⚠️ Advanced AI System (Enhancement)

**Current State:** Basic chase/flee AI exists in AISystem.cs

**Required Enhancements:**
- **Pathfinding** - A* algorithm for intelligent navigation
- **Group tactics** - Coordinated attacks, flanking
- **AI personalities** - Aggressive, defensive, cowardly, etc.
- **Ranged combat** - Maintain distance, use cover
- **Patrol routes** - Predefined paths
- **Memory system** - Remember last seen player position
- **State machine** - More complex behavior trees

**Proposed Components:**
```csharp
// Add to Components/EntityComponents.cs

public struct AIMemory
{
    public Position? LastSeenPlayerPosition;
    public float TimeSinceLastSeen;
    public List<EntityHandle> AlliedEntities;
    public List<EntityHandle> HostileEntities;
}

public struct Pathfinding
{
    public List<Position> CurrentPath;
    public Position? TargetPosition;
    public float RecalculateTimer;
}

public struct AIPersonality
{
    public float Aggression;      // 0.0 to 1.0
    public float Caution;         // 0.0 to 1.0
    public float Cooperation;     // 0.0 to 1.0
    public float PreferredRange;  // Melee vs ranged preference
}
```

**Proposed System Enhancement:**
```csharp
// New file: Systems/PathfindingSystem.cs

public class PathfindingSystem : IECSSystem
{
    public void Execute(IWorld world, float deltaTime)
    {
        // For each enemy with Pathfinding component
        // Calculate A* path to target
        // Update path if target moved
        // Handle path following
    }
    
    private List<Position> CalculateAStarPath(Position start, Position goal, DungeonMap map)
    {
        // A* implementation
    }
}
```

**Required Changes to AISystem:**
- Use pathfinding results instead of direct movement
- Implement group coordination
- Add personality-based decision making
- Implement ranged combat logic

---

### 5. ❌ Procedural Content Generation (PCG)

**Current State:** Hardcoded entity spawning. No dungeon generation.

**Required:**
- **Dungeon layout generation** - BSP, Cellular Automata, or Drunkard's Walk
- **Room placement** - Various room types and sizes
- **Corridor generation** - Connect rooms
- **Loot distribution** - Balanced item placement
- **Enemy spawning** - Difficulty scaling
- **Floor progression** - Increasing difficulty per floor

**Proposed Components:**
```csharp
// New file: Components/GenerationComponents.cs

public struct DungeonGenerator
{
    public GenerationAlgorithm Algorithm;
    public int Seed;
    public DungeonParameters Parameters;
}

public enum GenerationAlgorithm
{
    BinarySpacePartitioning,
    CellularAutomata,
    DrunkardsWalk,
    RoomAndCorridor,
    Maze
}

public struct DungeonParameters
{
    public int MinRoomSize;
    public int MaxRoomSize;
    public int RoomAttempts;
    public float CorridorWindiness;
    public int EnemyDensity;
    public int ItemDensity;
    public int TrapDensity;
}

public struct Room
{
    public int X;
    public int Y;
    public int Width;
    public int Height;
    public RoomType Type;
}

public enum RoomType
{
    Normal,
    Treasure,
    Boss,
    Shop,
    Shrine,
    Trap
}
```

**Proposed System:**
```csharp
// New file: Systems/DungeonGenerationSystem.cs

public class DungeonGenerationSystem
{
    public DungeonMap GenerateDungeon(int floor, DungeonParameters parameters, int seed)
    {
        // Choose algorithm based on parameters
        // Generate rooms
        // Connect rooms with corridors
        // Place doors
        // Place stairs
        // Place traps
        // Return completed map
    }
    
    private List<Room> GenerateRoomsBSP(int width, int height, DungeonParameters parameters)
    {
        // Binary Space Partitioning implementation
    }
    
    private void ConnectRooms(DungeonMap map, List<Room> rooms)
    {
        // Generate corridors between rooms
    }
}
```

**Proposed System:**
```csharp
// New file: Systems/SpawnSystem.cs

public class SpawnSystem : IECSSystem
{
    public void SpawnEnemies(IWorld world, DungeonMap map, int floor, ResourceLoader loader)
    {
        // Calculate enemy budget based on floor
        // Choose enemy types based on difficulty
        // Place enemies in valid locations
        // Ensure no spawning near player start
    }
    
    public void SpawnItems(IWorld world, DungeonMap map, int floor, ResourceLoader loader)
    {
        // Calculate loot budget
        // Place items in rooms and corridors
        // Bias treasure rooms
    }
}
```

---

## Implementation Priority

### Phase 1: Foundation (Week 1-2)
1. **Environment System** - Tilemap, collision, doors
2. **Camera System** - Viewport tracking
3. **Update RenderSystem** - Camera-aware rendering

### Phase 2: Visibility (Week 3)
4. **FOV System** - Shadowcasting, fog of war
5. **Light Sources** - Dynamic lighting

### Phase 3: Intelligence (Week 4)
6. **Pathfinding System** - A* implementation
7. **Enhanced AI** - Personalities, tactics

### Phase 4: Generation (Week 5-6)
8. **Dungeon Generation** - BSP or Room+Corridor algorithm
9. **Spawn System** - Enemy and item placement
10. **Floor Progression** - Difficulty scaling

---

## File Structure Proposal

```
WingedBean.Plugins.DungeonGame/
├── Components/
│   ├── CoreComponents.cs           (existing)
│   ├── EntityComponents.cs         (existing)
│   ├── EnvironmentComponents.cs    (NEW)
│   ├── CameraComponents.cs         (NEW)
│   ├── VisionComponents.cs         (NEW)
│   └── GenerationComponents.cs     (NEW)
├── Systems/
│   ├── AISystem.cs                 (existing - enhance)
│   ├── CombatSystem.cs             (existing)
│   ├── MovementSystem.cs           (existing - enhance)
│   ├── RenderSystem.cs             (existing - enhance)
│   ├── EnvironmentSystem.cs        (NEW)
│   ├── CameraSystem.cs             (NEW)
│   ├── FOVSystem.cs                (NEW)
│   ├── PathfindingSystem.cs        (NEW)
│   └── SpawnSystem.cs              (NEW)
├── Generation/                     (NEW)
│   ├── DungeonGenerator.cs
│   ├── BSPGenerator.cs
│   ├── CellularAutomataGenerator.cs
│   └── RoomConnector.cs
├── Algorithms/                     (NEW)
│   ├── AStar.cs
│   ├── Shadowcasting.cs
│   └── LineOfSight.cs
├── Data/
│   ├── EntityFactory.cs            (existing)
│   ├── GameResourceData.cs         (existing)
│   └── ResourceLoader.cs           (existing)
└── DungeonGame.cs                  (existing - update system registration)
```

---

## Testing Strategy

### Unit Tests
- Pathfinding algorithm correctness
- FOV shadowcasting accuracy
- Dungeon generation validity (all rooms connected)
- Collision detection

### Integration Tests
- Camera follows player correctly
- Enemies pathfind around obstacles
- Fog of war updates properly
- Dungeon generation produces playable levels

### Performance Tests
- FOV calculation < 16ms (60 FPS)
- Pathfinding < 5ms per enemy
- Dungeon generation < 1 second per floor

---

## Dependencies

### Existing
- ✅ ArchECS - Entity Component System
- ✅ ResourceLoader - JSON data loading

### New (Optional)
- Priority Queue (for A*) - Can implement custom or use built-in
- Random Number Generator - Use `System.Random` with seed

---

## Next Steps

1. **Review this plan** - Confirm architecture and priorities
2. **Create feature branches** - One per phase
3. **Implement Phase 1** - Environment and Camera systems
4. **Write tests** - TDD approach for algorithms
5. **Iterate** - Playtest and refine

---

## References

### Algorithms
- **Shadowcasting FOV**: http://www.roguebasin.com/index.php?title=FOV_using_recursive_shadowcasting
- **A* Pathfinding**: http://theory.stanford.edu/~amitp/GameProgramming/AStarComparison.html
- **BSP Dungeon Generation**: http://www.roguebasin.com/index.php?title=Basic_BSP_Dungeon_generation
- **Cellular Automata**: http://www.roguebasin.com/index.php?title=Cellular_Automata_Method_for_Generating_Random_Cave-Like_Levels

### Roguelike Development
- RogueBasin: http://www.roguebasin.com/
- /r/roguelikedev: https://www.reddit.com/r/roguelikedev/

---

**Document Version:** 1.0  
**Date:** 2025-10-13  
**Author:** Cascade AI Assistant

# RFC-0004: Dungeon Generation Integration

**Status:** Proposed
**Priority:** P0 - Critical (Blocking)
**Effort:** 2 weeks
**Phase:** 1 - Foundation
**Dependencies:** RFC-0001 (Environment/Tilemap)
**Plate Layer:** edg-rogqd (exists)

---

## Problem Statement

WingedBean has edg-rogqd (Plate layer) for procedural dungeon generation using graph-based algorithms, and WingedBean.Gameplay.Map with partial map management, but they are not integrated. Currently:

- No active dungeon generation
- No procedural content
- Maps must be manually created
- No floor progression

### Current State
- ✅ **edg-rogqd** exists (Plate layer) - Graph-based dungeon generation
- ✅ **modern-satsuma** exists (graph algorithms used by edg-rogqd)
- ✅ **WingedBean.Gameplay.Map** exists - Partial structure
- ❌ No integration between edg-rogqd and WingedBean
- ❌ No dungeon generation in gameplay loop
- ❌ Hardcoded entity spawning (via EntityFactory)

### Impact
- **Blocks:** Procedural content, replayability, floor progression
- **Affects:** Spawn system, player progression, game balance
- **Without this:** Game has no procedurally generated dungeons

---

## Proposed Solution

Integrate edg-rogqd's dungeon generation into WingedBean by:

1. Creating **DungeonGenerationSystem** in WingedBean.Gameplay.Map
2. Converting edg-rogqd output to WingedBean tile format
3. Integrating with MapService (from RFC-0001)
4. Adding floor generation on game start and stair traversal

### Architecture

```
WingedBean.Gameplay.Map/
├── Generation/                        # Dungeon generation (NEW)
│   ├── DungeonGenerationService.cs   # Main generation service
│   ├── EdgRogqdAdapter.cs            # Adapter for edg-rogqd
│   └── LayoutConverter.cs            # Convert edg-rogqd → WingedBean tiles
├── Systems/
│   ├── DungeonGenerationSystem.cs    # ECS system for generation (NEW)
│   └── FloorTransitionSystem.cs      # Handle stair traversal (NEW)
├── Data/
│   ├── FloorParameters.cs            # Per-floor generation params (NEW)
│   └── DungeonSeed.cs                # Seed management (NEW)
└── Components/
    └── FloorMetadata.cs               # Floor info component (NEW)
```

---

## Data Structures

### FloorParameters

```csharp
/// <summary>
/// Parameters for generating a specific dungeon floor
/// </summary>
public class FloorParameters
{
    /// <summary>
    /// Floor number (0 = ground, 1+ = deeper)
    /// </summary>
    public int FloorNumber { get; set; }

    /// <summary>
    /// Map width in tiles
    /// </summary>
    public int Width { get; set; } = 80;

    /// <summary>
    /// Map height in tiles
    /// </summary>
    public int Height { get; set; } = 50;

    /// <summary>
    /// Minimum room size
    /// </summary>
    public int MinRoomSize { get; set; } = 4;

    /// <summary>
    /// Maximum room size
    /// </summary>
    public int MaxRoomSize { get; set; } = 12;

    /// <summary>
    /// Number of room attempts
    /// </summary>
    public int RoomAttempts { get; set; } = 30;

    /// <summary>
    /// Corridor windiness (0.0 to 1.0)
    /// </summary>
    public float CorridorWindiness { get; set; } = 0.5f;

    /// <summary>
    /// Enemy density (enemies per 100 tiles)
    /// </summary>
    public int EnemyDensity { get; set; } = 3;

    /// <summary>
    /// Item density (items per 100 tiles)
    /// </summary>
    public int ItemDensity { get; set; } = 2;

    /// <summary>
    /// Trap density (traps per 100 tiles)
    /// </summary>
    public int TrapDensity { get; set; } = 1;

    /// <summary>
    /// Difficulty multiplier (increases with depth)
    /// </summary>
    public float DifficultyMultiplier => 1.0f + (FloorNumber * 0.1f);
}
```

### DungeonSeed

```csharp
/// <summary>
/// Seed for procedural generation (for reproducibility)
/// </summary>
public class DungeonSeed
{
    /// <summary>
    /// Master seed
    /// </summary>
    public int MasterSeed { get; set; }

    /// <summary>
    /// Get seed for specific floor
    /// </summary>
    public int GetFloorSeed(int floorNumber)
    {
        // Derive floor-specific seed from master seed
        return HashCode.Combine(MasterSeed, floorNumber);
    }
}
```

### FloorMetadata Component

```csharp
/// <summary>
/// Metadata about a dungeon floor (ECS component)
/// </summary>
public struct FloorMetadata
{
    /// <summary>
    /// Floor number
    /// </summary>
    public int FloorNumber;

    /// <summary>
    /// Seed used for generation
    /// </summary>
    public int Seed;

    /// <summary>
    /// Number of rooms generated
    /// </summary>
    public int RoomCount;

    /// <summary>
    /// Player spawn position on this floor
    /// </summary>
    public (int X, int Y) PlayerSpawnPosition;

    /// <summary>
    /// Stairs up position (null if ground floor)
    /// </summary>
    public (int X, int Y)? StairsUpPosition;

    /// <summary>
    /// Stairs down position (null if deepest floor)
    /// </summary>
    public (int X, int Y)? StairsDownPosition;

    /// <summary>
    /// Has this floor been generated?
    /// </summary>
    public bool IsGenerated;
}
```

---

## Services

### IDungeonGenerationService

```csharp
/// <summary>
/// Service for generating dungeon floors
/// </summary>
public interface IDungeonGenerationService
{
    /// <summary>
    /// Generate a dungeon floor
    /// </summary>
    DungeonMap GenerateFloor(FloorParameters parameters, int seed);

    /// <summary>
    /// Get default parameters for a floor
    /// </summary>
    FloorParameters GetDefaultParameters(int floorNumber);
}
```

### DungeonGenerationService Implementation

```csharp
public class DungeonGenerationService : IDungeonGenerationService
{
    private readonly EdgRogqdAdapter _adapter;
    private readonly LayoutConverter _converter;

    public DungeonGenerationService()
    {
        _adapter = new EdgRogqdAdapter();
        _converter = new LayoutConverter();
    }

    public DungeonMap GenerateFloor(FloorParameters parameters, int seed)
    {
        // 1. Create edg-rogqd level description
        var levelDescription = _adapter.CreateLevelDescription(parameters);

        // 2. Generate dungeon using edg-rogqd
        var generator = _adapter.CreateGenerator(levelDescription, seed);
        var edgLayout = generator.Generate(new DungeonGeneratorOptions());

        // 3. Convert edg-rogqd layout to WingedBean DungeonMap
        var dungeonMap = _converter.ConvertToWingedBeanMap(
            edgLayout,
            parameters.FloorNumber
        );

        // 4. Post-process (add doors, traps, etc.)
        PostProcessMap(dungeonMap, parameters);

        return dungeonMap;
    }

    public FloorParameters GetDefaultParameters(int floorNumber)
    {
        return new FloorParameters
        {
            FloorNumber = floorNumber,
            Width = 80,
            Height = 50,
            MinRoomSize = 4 + (floorNumber / 5), // Larger rooms deeper
            MaxRoomSize = 12 + (floorNumber / 3),
            RoomAttempts = 30 + (floorNumber * 2),
            CorridorWindiness = 0.5f + (floorNumber * 0.05f),
            EnemyDensity = 3 + floorNumber, // More enemies deeper
            ItemDensity = 2 + (floorNumber / 2),
            TrapDensity = 1 + (floorNumber / 3)
        };
    }

    private void PostProcessMap(DungeonMap map, FloorParameters parameters)
    {
        // Add doors to room entrances
        PlaceDoors(map);

        // Add traps based on density
        PlaceTraps(map, parameters.TrapDensity);

        // Mark special rooms (treasure, boss, etc.)
        MarkSpecialRooms(map, parameters.FloorNumber);
    }
}
```

---

## Adapters

### EdgRogqdAdapter

```csharp
/// <summary>
/// Adapter for edg-rogqd dungeon generation
/// </summary>
public class EdgRogqdAdapter
{
    public LevelDescriptionGrid2D<RoomDescriptionGrid2D> CreateLevelDescription(
        FloorParameters parameters)
    {
        // Create edg-rogqd level description from parameters
        var levelDesc = new LevelDescriptionGrid2D<RoomDescriptionGrid2D>();

        // Configure room sizes, counts, etc.
        levelDesc.MinRoomSize = parameters.MinRoomSize;
        levelDesc.MaxRoomSize = parameters.MaxRoomSize;
        levelDesc.RoomAttempts = parameters.RoomAttempts;
        // ... configure other edg-rogqd parameters

        return levelDesc;
    }

    public GraphBasedGeneratorGrid2D<RoomDescriptionGrid2D> CreateGenerator(
        LevelDescriptionGrid2D<RoomDescriptionGrid2D> levelDescription,
        int seed)
    {
        // Create edg-rogqd generator with seed
        return new GraphBasedGeneratorGrid2D<RoomDescriptionGrid2D>(
            levelDescription,
            new Random(seed)
        );
    }
}
```

### LayoutConverter

```csharp
/// <summary>
/// Converts edg-rogqd layouts to WingedBean DungeonMap
/// </summary>
public class LayoutConverter
{
    public DungeonMap ConvertToWingedBeanMap(
        IDungeonLayout edgLayout,
        int floorNumber)
    {
        // Extract room/corridor data from edg-rogqd layout
        var rooms = ExtractRooms(edgLayout);
        var corridors = ExtractCorridors(edgLayout);

        // Determine map size
        int width = CalculateWidth(rooms, corridors);
        int height = CalculateHeight(rooms, corridors);

        // Create WingedBean DungeonMap
        var map = new DungeonMap(width, height, floorNumber);

        // Initialize all tiles as walls
        FillWithWalls(map);

        // Carve out rooms
        foreach (var room in rooms)
        {
            CarveRoom(map, room);
        }

        // Carve out corridors
        foreach (var corridor in corridors)
        {
            CarveCorridor(map, corridor);
        }

        // Place stairs
        PlaceStairs(map, rooms);

        // Determine spawn positions
        DetermineSpawnPositions(map, rooms);

        return map;
    }

    private void CarveRoom(DungeonMap map, RoomData room)
    {
        for (int y = room.Y; y < room.Y + room.Height; y++)
        {
            for (int x = room.X; x < room.X + room.Width; x++)
            {
                var tile = map.GetTile(x, y);
                tile.Type = TileType.Floor;
                tile.IsWalkable = true;
                tile.IsTransparent = true;
                tile.Symbol = '.';
                map.SetTile(x, y, tile);
            }
        }

        // Add walls around room perimeter
        AddRoomWalls(map, room);
    }

    private void CarveCorridor(DungeonMap map, CorridorData corridor)
    {
        foreach (var (x, y) in corridor.Path)
        {
            var tile = map.GetTile(x, y);
            tile.Type = TileType.Floor;
            tile.IsWalkable = true;
            tile.IsTransparent = true;
            tile.Symbol = '.';
            map.SetTile(x, y, tile);
        }
    }

    private void PlaceStairs(DungeonMap map, List<RoomData> rooms)
    {
        // Stairs up in first room (if not ground floor)
        if (map.Floor > 0)
        {
            var firstRoom = rooms.First();
            var stairPos = GetRandomFloorPosition(map, firstRoom);
            var tile = map.GetTile(stairPos.X, stairPos.Y);
            tile.Type = TileType.StairsUp;
            tile.Symbol = '<';
            map.SetTile(stairPos.X, stairPos.Y, tile);
        }

        // Stairs down in last room
        var lastRoom = rooms.Last();
        var downPos = GetRandomFloorPosition(map, lastRoom);
        var downTile = map.GetTile(downPos.X, downPos.Y);
        downTile.Type = TileType.StairsDown;
        downTile.Symbol = '>';
        map.SetTile(downPos.X, downPos.Y, downTile);
    }
}
```

---

## Systems

### DungeonGenerationSystem

```csharp
/// <summary>
/// ECS system for dungeon generation
/// </summary>
public class DungeonGenerationSystem : IECSSystem
{
    private readonly IDungeonGenerationService _generationService;
    private readonly IMapService _mapService;
    private readonly DungeonSeed _seed;

    public DungeonGenerationSystem(
        IDungeonGenerationService generationService,
        IMapService mapService,
        DungeonSeed seed)
    {
        _generationService = generationService;
        _mapService = mapService;
        _seed = seed;
    }

    public void Execute(IWorld world, float deltaTime)
    {
        // This system doesn't run every frame
        // It's triggered on:
        // 1. Game start (generate floor 0)
        // 2. Stair traversal (generate new floor)
    }

    /// <summary>
    /// Generate a specific floor
    /// </summary>
    public void GenerateFloor(int floorNumber)
    {
        // Get parameters for this floor
        var parameters = _generationService.GetDefaultParameters(floorNumber);

        // Get seed for this floor
        int floorSeed = _seed.GetFloorSeed(floorNumber);

        // Generate dungeon map
        var dungeonMap = _generationService.GenerateFloor(parameters, floorSeed);

        // Store in MapService
        _mapService.LoadMap(floorNumber, dungeonMap);

        // Create floor metadata entity
        CreateFloorMetadataEntity(world, floorNumber, floorSeed, dungeonMap);
    }

    private void CreateFloorMetadataEntity(
        IWorld world,
        int floorNumber,
        int seed,
        DungeonMap map)
    {
        var entity = world.CreateEntity();
        entity.Add(new FloorMetadata
        {
            FloorNumber = floorNumber,
            Seed = seed,
            RoomCount = CountRooms(map),
            PlayerSpawnPosition = FindPlayerSpawnPosition(map),
            StairsUpPosition = FindStairsPosition(map, TileType.StairsUp),
            StairsDownPosition = FindStairsPosition(map, TileType.StairsDown),
            IsGenerated = true
        });
    }
}
```

### FloorTransitionSystem

```csharp
/// <summary>
/// Handles stair traversal and floor transitions
/// </summary>
public class FloorTransitionSystem : IECSSystem
{
    private readonly IMapService _mapService;
    private readonly DungeonGenerationSystem _generationSystem;

    public void Execute(IWorld world, float deltaTime)
    {
        // Check if player is on stairs
        var playerQuery = world.Query<Player, Position>();
        var player = playerQuery.FirstOrDefault();
        if (player == null) return;

        var playerPos = player.Get<Position>();
        var currentFloor = _mapService.GetCurrentFloor();
        var map = _mapService.GetMap(currentFloor);
        var tile = map.GetTile(playerPos.X, playerPos.Y);

        // Check for stair input (e.g., '>' or '<' key)
        if (tile.Type == TileType.StairsDown && InputPressed("DescendStairs"))
        {
            DescendFloor(world, player);
        }
        else if (tile.Type == TileType.StairsUp && InputPressed("AscendStairs"))
        {
            AscendFloor(world, player);
        }
    }

    private void DescendFloor(IWorld world, EntityHandle player)
    {
        int currentFloor = _mapService.GetCurrentFloor();
        int nextFloor = currentFloor + 1;

        // Check if next floor exists
        var floorMetadataQuery = world.Query<FloorMetadata>();
        var nextFloorMetadata = floorMetadataQuery.FirstOrDefault(e =>
            e.Get<FloorMetadata>().FloorNumber == nextFloor);

        if (nextFloorMetadata == null)
        {
            // Generate next floor
            _generationSystem.GenerateFloor(nextFloor);
        }

        // Move player to next floor
        var position = player.Get<Position>();
        position.Floor = nextFloor;

        // Get stairs up position on next floor
        var metadata = GetFloorMetadata(world, nextFloor);
        if (metadata.StairsUpPosition.HasValue)
        {
            position.X = metadata.StairsUpPosition.Value.X;
            position.Y = metadata.StairsUpPosition.Value.Y;
        }
        else
        {
            // Use spawn position
            position.X = metadata.PlayerSpawnPosition.X;
            position.Y = metadata.PlayerSpawnPosition.Y;
        }

        player.Set(position);

        // Update current floor
        _mapService.SetCurrentFloor(nextFloor);

        // Log transition
        LogFloorTransition(currentFloor, nextFloor);
    }

    private void AscendFloor(IWorld world, EntityHandle player)
    {
        int currentFloor = _mapService.GetCurrentFloor();
        if (currentFloor == 0) return; // Can't go up from ground floor

        int previousFloor = currentFloor - 1;

        // Move player to previous floor
        var position = player.Get<Position>();
        position.Floor = previousFloor;

        // Get stairs down position on previous floor
        var metadata = GetFloorMetadata(world, previousFloor);
        if (metadata.StairsDownPosition.HasValue)
        {
            position.X = metadata.StairsDownPosition.Value.X;
            position.Y = metadata.StairsDownPosition.Value.Y;
        }

        player.Set(position);

        // Update current floor
        _mapService.SetCurrentFloor(previousFloor);

        // Log transition
        LogFloorTransition(currentFloor, previousFloor);
    }
}
```

---

## Integration with Game Initialization

```csharp
// In DungeonGame.cs or similar startup code

public class DungeonGame
{
    private DungeonGenerationSystem _generationSystem;
    private FloorTransitionSystem _transitionSystem;

    public void Initialize()
    {
        // ... other initialization ...

        // Create dungeon seed (from player input or random)
        var seed = new DungeonSeed { MasterSeed = GetSeedFromPlayerOrRandom() };

        // Create generation system
        _generationSystem = new DungeonGenerationSystem(
            new DungeonGenerationService(),
            _mapService,
            seed
        );

        // Create transition system
        _transitionSystem = new FloorTransitionSystem(
            _mapService,
            _generationSystem
        );

        // Generate initial floor (ground floor, floor 0)
        _generationSystem.GenerateFloor(0);
        _mapService.SetCurrentFloor(0);

        // Spawn player at floor 0 spawn position
        var floorMetadata = GetFloorMetadata(0);
        SpawnPlayer(floorMetadata.PlayerSpawnPosition);

        // Register systems
        RegisterSystem(_generationSystem);
        RegisterSystem(_transitionSystem);
    }
}
```

---

## Implementation Steps

### Step 1: Create Services (3-4 days)
1. Create `IDungeonGenerationService` and implementation
2. Create `EdgRogqdAdapter` to wrap edg-rogqd
3. Create `LayoutConverter` to convert layouts
4. Write unit tests for conversion logic

### Step 2: Create Systems (2-3 days)
1. Implement `DungeonGenerationSystem`
2. Implement `FloorTransitionSystem`
3. Add floor metadata tracking
4. Write integration tests

### Step 3: Integration (2-3 days)
1. Integrate with game initialization
2. Add stair traversal input handling
3. Test floor generation
4. Test floor transitions

### Step 4: Polish (2-3 days)
1. Add seed input UI
2. Test procedural generation consistency (same seed = same dungeon)
3. Balance floor parameters
4. Test difficulty scaling

---

## Success Criteria

- ✅ edg-rogqd generates dungeons on game start
- ✅ Dungeons converted to WingedBean tile format correctly
- ✅ Rooms, corridors, stairs, doors placed correctly
- ✅ Player spawns at valid position
- ✅ Stair traversal works (ascend/descend)
- ✅ Floor transitions preserve player state
- ✅ Same seed generates same dungeon
- ✅ Difficulty scales with floor depth
- ✅ All tests pass

---

## Performance Targets

- Floor generation: < 1 second for 80x50 map
- Floor transition: < 100ms
- Memory: < 5MB per floor

---

## Dependencies

- **RFC-0001:** Environment/Tilemap (target format for conversion)
- **Plate Project:** edg-rogqd (exists, graph-based generation)
- **Plate Project:** modern-satsuma (used by edg-rogqd)

---

## Follow-Up

- **RFC-0006:** Spawn System (uses generated rooms for spawn placement)
- **RFC-0011:** Progression System (floor depth affects difficulty)

---

## Open Questions

1. **Floor Storage:** Keep all generated floors in memory, or serialize and load on-demand?
   - **Recommendation:** Keep current + adjacent floors, serialize others

2. **Seed Input:** Allow player to specify seed, or always random?
   - **Recommendation:** Both - random by default, optional seed input

3. **Floor Limit:** Maximum dungeon depth?
   - **Recommendation:** 20-30 floors initially, configurable

4. **Special Floors:** Boss floors, treasure floors, rest floors?
   - **Recommendation:** Every 5th floor is special (configurable in FloorParameters)

---

**Author:** WingedBean Team
**Date:** 2025-10-15
**Version:** 1.0

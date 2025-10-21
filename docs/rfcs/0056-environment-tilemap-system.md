# RFC-0001: Environment/Tilemap System

**Status:** Proposed
**Priority:** P0 - Critical (Blocking)
**Effort:** 2-3 weeks
**Phase:** 1 - Foundation
**Dependencies:** None (foundational)

---

## Problem Statement

WingedBean currently has entities (player, enemies, items) but no environment for them to exist in. Entities float in empty space with no walls, floors, doors, or obstacles. This prevents spatial gameplay, collision detection, line-of-sight, and pathfinding from functioning.

### Current State
- ✅ `Position` component exists (X, Y, Floor)
- ✅ `Blocking` component exists (BlocksMovement, BlocksLight) but unused
- ✅ `MovementSystem` exists but no collision detection
- ❌ No dungeon map data structure
- ❌ No tiles (walls, floors, doors)
- ❌ No collision system
- ❌ No environmental mechanics (doors, traps, hazards)

### Impact
- **Blocks:** All spatial gameplay, rendering, collision, FOV, pathfinding
- **Affects:** MovementSystem, RenderSystem, FOVSystem (future), PathfindingSystem (future)
- **Without this:** Game cannot function as a dungeon crawler

---

## Proposed Solution

Create `WingedBean.Gameplay.Environment` project to manage dungeon maps, tiles, and environmental mechanics.

### Architecture

```
WingedBean.Gameplay.Environment/
├── Components/
│   ├── DungeonMap.cs              # Map data structure
│   ├── Tile.cs                    # Tile properties
│   ├── Door.cs                    # Door state
│   ├── Trap.cs                    # Trap mechanics
│   └── EnvironmentalEffect.cs     # Hazards (lava, water, etc.)
├── Systems/
│   ├── EnvironmentSystem.cs       # Main environment logic
│   ├── DoorSystem.cs              # Door interactions
│   ├── TrapSystem.cs              # Trap triggers
│   └── HazardSystem.cs            # Environmental damage
├── Data/
│   └── TileDefinitions.json       # Tile type definitions
└── Services/
    ├── IMapService.cs             # Map access interface
    └── MapService.cs              # Map management
```

---

## Data Structures

### Tile Structure

```csharp
/// <summary>
/// Represents a single tile in the dungeon.
/// </summary>
public struct Tile
{
    /// <summary>
    /// The type of tile (floor, wall, door, etc.)
    /// </summary>
    public TileType Type;

    /// <summary>
    /// Can entities walk on this tile?
    /// </summary>
    public bool IsWalkable;

    /// <summary>
    /// Does this tile block line of sight?
    /// </summary>
    public bool IsTransparent;

    /// <summary>
    /// Has the player explored this tile? (for fog of war)
    /// </summary>
    public bool IsExplored;

    /// <summary>
    /// Is this tile currently visible to the player?
    /// </summary>
    public bool IsVisible;

    /// <summary>
    /// Visual representation
    /// </summary>
    public char Symbol;

    /// <summary>
    /// Colors for rendering
    /// </summary>
    public (ConsoleColor Foreground, ConsoleColor Background) Colors;

    /// <summary>
    /// Entity on this tile (if any) - optimization for collision
    /// </summary>
    public EntityHandle? Entity;
}

public enum TileType
{
    Floor,          // Walkable ground
    Wall,           // Solid wall
    Door,           // Door (open/closed/locked)
    StairsUp,       // Stairs to previous floor
    StairsDown,     // Stairs to next floor
    Trap,           // Trap tile
    Water,          // Shallow water (walkable, slows movement)
    Lava,           // Lava (walkable, damages)
    Chasm,          // Pit (not walkable, instant death)
    Ice,            // Ice (walkable, slippery)
    Grass,          // Grass (walkable, normal)
    Stone,          // Stone floor (walkable, normal)
}
```

### DungeonMap Structure

```csharp
/// <summary>
/// Represents a single floor of the dungeon.
/// </summary>
public class DungeonMap
{
    /// <summary>
    /// Map width in tiles
    /// </summary>
    public int Width { get; private set; }

    /// <summary>
    /// Map height in tiles
    /// </summary>
    public int Height { get; private set; }

    /// <summary>
    /// Floor number (0 = ground, 1+ = deeper)
    /// </summary>
    public int Floor { get; private set; }

    /// <summary>
    /// Tile data (2D array)
    /// </summary>
    private Tile[,] _tiles;

    /// <summary>
    /// Get tile at position
    /// </summary>
    public Tile GetTile(int x, int y);

    /// <summary>
    /// Set tile at position
    /// </summary>
    public void SetTile(int x, int y, Tile tile);

    /// <summary>
    /// Check if position is within map bounds
    /// </summary>
    public bool IsInBounds(int x, int y);

    /// <summary>
    /// Check if position is walkable
    /// </summary>
    public bool IsWalkable(int x, int y);

    /// <summary>
    /// Check if position is transparent (for FOV)
    /// </summary>
    public bool IsTransparent(int x, int y);

    /// <summary>
    /// Get all tiles in rectangular area
    /// </summary>
    public IEnumerable<(int X, int Y, Tile Tile)> GetTilesInArea(int x, int y, int width, int height);

    /// <summary>
    /// Set entity reference on tile (for collision optimization)
    /// </summary>
    public void SetEntityOnTile(int x, int y, EntityHandle? entity);
}
```

### Door Component

```csharp
/// <summary>
/// Door entity component
/// </summary>
public struct Door
{
    /// <summary>
    /// Is the door open?
    /// </summary>
    public bool IsOpen;

    /// <summary>
    /// Is the door locked?
    /// </summary>
    public bool IsLocked;

    /// <summary>
    /// Key required to unlock (null if no key required)
    /// </summary>
    public string? RequiredKeyId;

    /// <summary>
    /// Door's tile position
    /// </summary>
    public (int X, int Y) Position;
}
```

### Trap Component

```csharp
/// <summary>
/// Trap entity component
/// </summary>
public struct Trap
{
    /// <summary>
    /// Type of trap
    /// </summary>
    public TrapType Type;

    /// <summary>
    /// Damage dealt by trap
    /// </summary>
    public int Damage;

    /// <summary>
    /// Has the trap been triggered?
    /// </summary>
    public bool IsTriggered;

    /// <summary>
    /// Is the trap visible to the player?
    /// </summary>
    public bool IsVisible;

    /// <summary>
    /// Trap's tile position
    /// </summary>
    public (int X, int Y) Position;
}

public enum TrapType
{
    Spike,      // Deals physical damage
    Arrow,      // Deals physical damage, ranged
    Poison,     // Deals poison damage over time
    Teleport,   // Teleports entity to random location
    Alarm,      // Alerts nearby enemies
    Pit,        // Entity falls to lower floor
}
```

### Environmental Effect Component

```csharp
/// <summary>
/// Environmental hazard or effect on a tile
/// </summary>
public struct EnvironmentalEffect
{
    /// <summary>
    /// Effect type
    /// </summary>
    public EnvironmentType Type;

    /// <summary>
    /// Damage per turn (if applicable)
    /// </summary>
    public int DamagePerTurn;

    /// <summary>
    /// Movement cost multiplier (1.0 = normal, 2.0 = half speed)
    /// </summary>
    public float MovementCostMultiplier;

    /// <summary>
    /// Position of effect
    /// </summary>
    public (int X, int Y) Position;
}

public enum EnvironmentType
{
    Lava,       // Damages entities standing on it
    Water,      // Slows movement
    Ice,        // Slippery, may cause slide
    Fire,       // Burning tile, damages and spreads
    Poison,     // Poison gas, damages over time
}
```

---

## Systems

### EnvironmentSystem

```csharp
/// <summary>
/// Main system for environment management
/// </summary>
public class EnvironmentSystem : IECSSystem
{
    private IMapService _mapService;

    public void Execute(IWorld world, float deltaTime)
    {
        // 1. Handle door interactions (player opens/closes doors)
        HandleDoorInteractions(world);

        // 2. Trigger traps when entities step on them
        TriggerTraps(world);

        // 3. Apply environmental effects (lava damage, water slow, etc.)
        ApplyEnvironmentalEffects(world, deltaTime);

        // 4. Update tile entity references (for collision optimization)
        UpdateTileEntityReferences(world);
    }

    private void HandleDoorInteractions(IWorld world)
    {
        // Query for player + Door entities at same position
        // Open/close door based on input
        // Handle locked doors (check for key in inventory)
    }

    private void TriggerTraps(IWorld world)
    {
        // Query for entities with Position component
        // Check if standing on trap tile
        // Trigger trap if not already triggered
        // Apply trap effects (damage, teleport, alarm, etc.)
    }

    private void ApplyEnvironmentalEffects(IWorld world, float deltaTime)
    {
        // Query for entities with Position component
        // Check tile type at position
        // Apply environmental effects (lava damage, water slow, etc.)
    }

    private void UpdateTileEntityReferences(IWorld world)
    {
        // Clear all tile entity references
        // Query for entities with Position component
        // Set entity reference on their current tile
        // Used for fast collision detection
    }
}
```

### DoorSystem

```csharp
/// <summary>
/// Handles door-specific logic
/// </summary>
public class DoorSystem : IECSSystem
{
    private IMapService _mapService;
    private IInputService _inputService;

    public void Execute(IWorld world, float deltaTime)
    {
        // Check for door interaction input (e.g., 'o' key)
        if (_inputService.IsActionPressed("OpenDoor"))
        {
            OpenDoorAtPlayerPosition(world);
        }

        if (_inputService.IsActionPressed("CloseDoor"))
        {
            CloseDoorAtPlayerPosition(world);
        }
    }

    private void OpenDoorAtPlayerPosition(IWorld world)
    {
        // Get player position
        // Check adjacent tiles for doors
        // If door is locked, check for key in inventory
        // Open door (update Door component, update Tile walkability)
    }

    private void CloseDoorAtPlayerPosition(IWorld world)
    {
        // Get player position
        // Check adjacent tiles for doors
        // Close door (update Door component, update Tile walkability)
    }
}
```

### TrapSystem

```csharp
/// <summary>
/// Handles trap triggers and effects
/// </summary>
public class TrapSystem : IECSSystem
{
    private IMapService _mapService;

    public void Execute(IWorld world, float deltaTime)
    {
        // Query for entities with Position component
        var query = world.Query<Position>();

        foreach (var entity in query)
        {
            var position = entity.Get<Position>();

            // Check if tile has a trap
            var trap = GetTrapAtPosition(world, position.X, position.Y);
            if (trap != null && !trap.Value.IsTriggered)
            {
                TriggerTrap(world, entity, trap.Value);
            }
        }
    }

    private void TriggerTrap(IWorld world, EntityHandle entity, Trap trap)
    {
        // Mark trap as triggered
        // Apply trap effect based on type
        switch (trap.Type)
        {
            case TrapType.Spike:
                ApplyDamage(entity, trap.Damage);
                break;
            case TrapType.Poison:
                ApplyPoisonEffect(entity, trap.Damage);
                break;
            case TrapType.Teleport:
                TeleportEntity(entity);
                break;
            case TrapType.Alarm:
                AlertNearbyEnemies(entity.Get<Position>());
                break;
            // ... other trap types
        }
    }
}
```

### HazardSystem

```csharp
/// <summary>
/// Handles environmental hazards (lava, water, ice, etc.)
/// </summary>
public class HazardSystem : IECSSystem
{
    private IMapService _mapService;

    public void Execute(IWorld world, float deltaTime)
    {
        // Query for entities with Position component
        var query = world.Query<Position, Stats>();

        foreach (var entity in query)
        {
            var position = entity.Get<Position>();
            var tile = _mapService.GetTile(position.Floor, position.X, position.Y);

            ApplyTileEffect(entity, tile);
        }
    }

    private void ApplyTileEffect(EntityHandle entity, Tile tile)
    {
        switch (tile.Type)
        {
            case TileType.Lava:
                ApplyDamage(entity, 5); // 5 damage per turn
                break;
            case TileType.Water:
                // Slow movement (handled in MovementSystem)
                break;
            case TileType.Ice:
                // Slippery (may slide, handled in MovementSystem)
                break;
            case TileType.Chasm:
                // Instant death or fall to lower floor
                KillOrFallEntity(entity);
                break;
            // ... other tile types
        }
    }
}
```

---

## Services

### IMapService

```csharp
/// <summary>
/// Service for accessing dungeon maps
/// </summary>
public interface IMapService
{
    /// <summary>
    /// Get map for a specific floor
    /// </summary>
    DungeonMap GetMap(int floor);

    /// <summary>
    /// Get current active floor
    /// </summary>
    int GetCurrentFloor();

    /// <summary>
    /// Set current active floor
    /// </summary>
    void SetCurrentFloor(int floor);

    /// <summary>
    /// Get tile at position
    /// </summary>
    Tile GetTile(int floor, int x, int y);

    /// <summary>
    /// Set tile at position
    /// </summary>
    void SetTile(int floor, int x, int y, Tile tile);

    /// <summary>
    /// Check if position is walkable
    /// </summary>
    bool IsWalkable(int floor, int x, int y);

    /// <summary>
    /// Check if position is transparent (for FOV)
    /// </summary>
    bool IsTransparent(int floor, int x, int y);

    /// <summary>
    /// Create a new floor
    /// </summary>
    DungeonMap CreateFloor(int floor, int width, int height);

    /// <summary>
    /// Load map from data (for generated dungeons)
    /// </summary>
    void LoadMap(int floor, DungeonMap map);
}
```

---

## Integration Points

### MovementSystem Integration

```csharp
// BEFORE (current)
public void Execute(IWorld world, float deltaTime)
{
    var query = world.Query<Position, Velocity>();
    foreach (var entity in query)
    {
        var position = entity.Get<Position>();
        var velocity = entity.Get<Velocity>();

        position.X += velocity.DX;
        position.Y += velocity.DY;

        // No collision detection!
    }
}

// AFTER (with EnvironmentSystem)
public void Execute(IWorld world, float deltaTime)
{
    var query = world.Query<Position, Velocity>();
    foreach (var entity in query)
    {
        var position = entity.Get<Position>();
        var velocity = entity.Get<Velocity>();

        int newX = position.X + velocity.DX;
        int newY = position.Y + velocity.DY;

        // Check if new position is walkable
        if (_mapService.IsWalkable(position.Floor, newX, newY))
        {
            // Check if tile has entity blocking it
            var tile = _mapService.GetTile(position.Floor, newX, newY);
            if (tile.Entity == null || !EntityBlocksMovement(tile.Entity.Value))
            {
                position.X = newX;
                position.Y = newY;
            }
        }
    }
}
```

### RenderSystem Integration

```csharp
// BEFORE (current)
public void Execute(IWorld world, float deltaTime)
{
    // Render entities only
    var query = world.Query<Position, Renderable>();
    foreach (var entity in query)
    {
        // Render entity
    }
}

// AFTER (with EnvironmentSystem)
public void Execute(IWorld world, float deltaTime)
{
    // 1. Render tiles
    var map = _mapService.GetMap(_mapService.GetCurrentFloor());
    for (int y = 0; y < map.Height; y++)
    {
        for (int x = 0; x < map.Width; x++)
        {
            var tile = map.GetTile(x, y);
            if (tile.IsVisible || tile.IsExplored)
            {
                RenderTile(x, y, tile);
            }
        }
    }

    // 2. Render entities on top
    var query = world.Query<Position, Renderable>();
    foreach (var entity in query)
    {
        var position = entity.Get<Position>();
        var tile = map.GetTile(position.X, position.Y);
        if (tile.IsVisible) // Only render visible entities
        {
            RenderEntity(entity);
        }
    }
}
```

---

## Data Files

### TileDefinitions.json

```json
{
  "tiles": [
    {
      "type": "Floor",
      "symbol": ".",
      "foreground": "Gray",
      "background": "Black",
      "walkable": true,
      "transparent": true
    },
    {
      "type": "Wall",
      "symbol": "#",
      "foreground": "White",
      "background": "DarkGray",
      "walkable": false,
      "transparent": false
    },
    {
      "type": "Door",
      "symbol": "+",
      "foreground": "Yellow",
      "background": "Black",
      "walkable": false,
      "transparent": false,
      "note": "Walkable and transparent when open"
    },
    {
      "type": "StairsDown",
      "symbol": ">",
      "foreground": "Cyan",
      "background": "Black",
      "walkable": true,
      "transparent": true
    },
    {
      "type": "StairsUp",
      "symbol": "<",
      "foreground": "Cyan",
      "background": "Black",
      "walkable": true,
      "transparent": true
    },
    {
      "type": "Lava",
      "symbol": "~",
      "foreground": "Red",
      "background": "DarkRed",
      "walkable": true,
      "transparent": true,
      "damagePerTurn": 5
    },
    {
      "type": "Water",
      "symbol": "≈",
      "foreground": "Blue",
      "background": "DarkBlue",
      "walkable": true,
      "transparent": true,
      "movementCostMultiplier": 2.0
    }
  ]
}
```

---

## Testing Strategy

### Unit Tests
- Tile property getters/setters
- DungeonMap bounds checking
- DungeonMap tile access
- IsWalkable/IsTransparent logic

### Integration Tests
- MovementSystem with collision detection
- Door opening/closing
- Trap triggering
- Environmental damage application
- Tile entity reference updates

### Performance Tests
- Map tile access < 1ms for 100x100 map
- Collision checking < 0.1ms per entity
- Tile entity reference updates < 5ms for 100 entities

---

## Implementation Steps

### Step 1: Data Structures (2-3 days)
1. Create `WingedBean.Gameplay.Environment` project
2. Define `Tile` struct
3. Define `DungeonMap` class
4. Define component structs (Door, Trap, EnvironmentalEffect)
5. Write unit tests for data structures

### Step 2: Map Service (2-3 days)
1. Create `IMapService` interface
2. Implement `MapService` class
3. Add map storage (Dictionary<int, DungeonMap>)
4. Write unit tests for map service

### Step 3: Systems (1 week)
1. Implement `EnvironmentSystem`
2. Implement `DoorSystem`
3. Implement `TrapSystem`
4. Implement `HazardSystem`
5. Write integration tests for systems

### Step 4: Integration (3-5 days)
1. Update `MovementSystem` with collision detection
2. Update `RenderSystem` to render tiles
3. Register systems in DungeonGame
4. Test full integration

### Step 5: Data & Polish (2-3 days)
1. Create `TileDefinitions.json`
2. Add tile loading in ResourceLoader
3. Test all tile types
4. Add door/trap/hazard entities to test maps

---

## Success Criteria

- ✅ Dungeon maps with multiple floors can be created
- ✅ Tiles have walkability and transparency properties
- ✅ Entities collide with walls and each other
- ✅ Doors can be opened/closed/locked
- ✅ Traps trigger when entities step on them
- ✅ Environmental hazards (lava, water) affect entities
- ✅ RenderSystem displays tiles and entities
- ✅ MovementSystem respects collision
- ✅ All tests pass
- ✅ Performance metrics met

---

## Follow-Up RFCs

After this RFC is implemented:
- **RFC-0002: Camera Integration** - Requires map bounds
- **RFC-0003: FOV System** - Requires tile transparency
- **RFC-0004: Dungeon Generation Integration** - Requires map data structure
- **RFC-0005: Pathfinding System** - Requires tile walkability

---

## Open Questions

1. **Tile Entity References:** Should we store entity references on tiles for O(1) collision, or query ECS each time for O(n) collision?
   - **Recommendation:** Store references, update in EnvironmentSystem. Faster collision detection.

2. **Door Representation:** Should doors be tiles or entities?
   - **Recommendation:** Hybrid - door tiles + optional door entity for state (locked, key requirement).

3. **Environmental Effects:** Should effects be tiles or separate entities?
   - **Recommendation:** Tiles for static effects (lava), entities for dynamic effects (fire spreading).

4. **Map Storage:** How many floors should be kept in memory?
   - **Recommendation:** Current floor + adjacent floors (for stairs). Serialize others.

---

## References

- **RogueBasin Tile Design:** http://www.roguebasin.com/index.php?title=Tile-based_games
- **Existing Code:** `WingedBean.Plugins.DungeonGame/Components/CoreComponents.cs` (Position, Blocking)
- **Related:** `DUNGEON_CRAWLER_ENHANCEMENT_PLAN.md` - Section "Missing Critical Features #1"

---

**Author:** WingedBean Team
**Date:** 2025-10-15
**Version:** 1.0

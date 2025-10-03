# ECS Architecture Guide

**Version**: 1.0.0
**Last Updated**: 2025-10-02
**Related RFC**: [RFC-0007: Arch ECS Integration](../rfcs/0007-arch-ecs-integration.md)

## Overview

This document describes the Entity Component System (ECS) architecture used in Winged Bean, specifically for the ConsoleDungeon game. The architecture follows RFC-0007 and implements a layered abstraction pattern that allows swapping ECS implementations.

## Architecture Layers

### Layer 1: ECS Contracts (Platform-Agnostic)

Location: `framework/src/WingedBean.Contracts.ECS/`

**Purpose**: Define platform-agnostic interfaces that any ECS library can implement.

**Key Interfaces**:

- **`IECSService`**: Top-level service for creating/destroying worlds
- **`IWorld`**: Represents an isolated ECS world with entities and components
- **`IEntity`**: Wrapper around entity handles for component operations
- **`IQuery`**: Abstraction for querying entities by component types
- **`IECSSystem`**: Base interface for systems that process entities
- **`EntityHandle`**: Value type representing an entity reference

**Target Framework**: `netstandard2.1` (for maximum compatibility)

### Layer 2: Arch ECS Plugin (Tier 3 Plugin)

Location: `console/src/plugins/WingedBean.Plugins.ArchECS/`

**Purpose**: Concrete implementation using the Arch ECS library.

**Key Classes**:

- **`ArchECSService`**: Implements `IECSService`, manages Arch worlds
- **`ArchWorld`**: Implements `IWorld`, wraps `Arch.Core.World`
- **`ArchEntity`**: Implements `IEntity`, wraps `Arch.Core.Entity`
- **`ArchQuery`**: Implements `IQuery`, wraps `Arch.Core.QueryDescription`

**Dependencies**:
- `Arch` v1.3.0 (NuGet)
- `Arch.SourceGen` v1.0.0 (for compile-time query optimization)

**Plugin Configuration** (`.plugin.json`):
```json
{
  "id": "wingedbean.plugins.archecs",
  "version": "1.0.0",
  "provides": ["WingedBean.Contracts.ECS.IECSService"],
  "loadStrategy": "Eager"
}
```

### Layer 3: Game Logic (ConsoleDungeon)

Location: `console/src/plugins/WingedBean.Plugins.DungeonGame/`

**Components**: Define data structures for game entities
**Systems**: Define behavior logic that processes components
**DungeonGame**: Orchestrates systems and initializes the game world

## Component Design

Components are **pure data** structures (no logic):

```csharp
// Example: Position component
public struct Position
{
    public int X;
    public int Y;
    public int Floor;
}
```

**Rules**:
- All components are `struct` (value types)
- Components contain only data fields
- No methods or business logic
- Serializable by default (easy save/load)

**Core Components**:

| Component | Purpose | Fields |
|-----------|---------|--------|
| `Position` | Spatial location | X, Y, Floor |
| `Stats` | Character stats | HP, Mana, Strength, Defense, etc. |
| `Renderable` | Visual representation | Symbol, ForegroundColor, RenderLayer |
| `Player` | Tag component | (none - marker) |
| `Enemy` | AI behavior | Type, State, AggroRange, Target |
| `Blocking` | Collision data | BlocksMovement, BlocksLight |

## System Design

Systems are **pure logic** that processes components:

```csharp
public class MovementSystem : IECSSystem
{
    public void Execute(IECSService ecs, float deltaTime)
    {
        var world = ecs.GetWorld(0);
        foreach (var entity in world.CreateQuery<Position>())
        {
            ref var pos = ref world.GetComponent<Position>(entity);
            // Apply movement logic
        }
    }
}
```

**Rules**:
- Systems implement `IECSSystem`
- Systems are stateless (or minimal state)
- Systems query entities by components
- Systems execute in deterministic order

**Core Systems**:

| System | Priority | Purpose |
|--------|----------|---------|
| `AISystem` | 100 | Update enemy AI states and pathfinding |
| `MovementSystem` | 90 | Apply movement and collision detection |
| `CombatSystem` | 80 | Process combat damage and death |
| `RenderSystem` | 10 | Render entities to console |

**Execution Order**: Higher priority → Lower priority

## Entity Composition

Entities are composed from components:

```csharp
// Player entity
var player = world.CreateEntity();
world.AttachComponent(player, new Player());
world.AttachComponent(player, new Position(40, 12, 1));
world.AttachComponent(player, new Stats { MaxHP = 100, ... });
world.AttachComponent(player, new Renderable { Symbol = '@', ... });

// Enemy entity
var goblin = world.CreateEntity();
world.AttachComponent(goblin, new Enemy { Type = EnemyType.Goblin, ... });
world.AttachComponent(goblin, new Position(10, 5, 1));
world.AttachComponent(goblin, new Stats { MaxHP = 20, ... });
world.AttachComponent(goblin, new Renderable { Symbol = 'g', ... });
```

**Composition Pattern**:
- Flexible: Add/remove components at runtime
- Reusable: Same components for different entity types
- Queryable: Find entities by component combinations

## Query Patterns

Systems query entities by component types:

```csharp
// Single component query
foreach (var entity in world.CreateQuery<Position>())
{
    var pos = world.GetComponent<Position>(entity);
}

// Two component query
foreach (var entity in world.CreateQuery<Position, Renderable>())
{
    ref var pos = ref world.GetComponent<Position>(entity);
    var render = world.GetComponent<Renderable>(entity);
}

// Three component query
foreach (var entity in world.CreateQuery<Player, Position, Stats>())
{
    // Only entities with ALL three components
}
```

**Performance**:
- Arch uses chunk iteration (cache-friendly)
- Queries return `ref` for zero-copy access
- No allocations per query (reuses memory)

## Game Loop Integration

`DungeonGame` orchestrates the ECS:

```csharp
public class DungeonGame
{
    private readonly IECSService _ecs;
    private IWorld _world;
    private readonly List<IECSSystem> _systems = new();

    public void Initialize()
    {
        _world = _ecs.CreateWorld();
        _systems.Add(new AISystem());
        _systems.Add(new MovementSystem());
        _systems.Add(new CombatSystem());
        _systems.Add(new RenderSystem());

        CreatePlayer();
        CreateEnemies(5);
    }

    public void Update()
    {
        var deltaTime = CalculateDeltaTime();
        foreach (var system in _systems)
        {
            system.Execute(_ecs, deltaTime);
        }
    }
}
```

**60 FPS Target**: `Update()` called every 16ms

## Performance Characteristics

Based on Arch ECS benchmarks:

| Operation | Performance | Allocation |
|-----------|-------------|------------|
| Create 1M entities | ~139ms | 0 B |
| Add 1M components | ~157ms | 0 B |
| Query 1M entities | ~42ms | 0 B |

**ConsoleDungeon Target**: 1000+ entities at 60 FPS

## Testing Strategy

### Unit Tests

**Adapter Tests** (`WingedBean.Plugins.ArchECS.Tests`):
- `ArchWorldTests`: 15 tests for world operations
- `ArchEntityTests`: 13 tests for entity operations
- `ArchQueryTests`: 11 tests for query operations

**System Tests** (`WingedBean.Plugins.DungeonGame.Tests`):
- `MovementSystemTests`: 6 tests for movement logic
- `CombatSystemTests`: 6 tests for combat mechanics
- `AISystemTests`: 6 tests for AI behavior
- `RenderSystemTests`: 5 tests for rendering

**Total**: 62 tests, all passing

### Integration Tests

- Create world with 100+ entities
- Run all systems for 1000 frames
- Verify no crashes, memory leaks, or performance degradation

## Best Practices

### Component Design
✅ **DO**: Keep components small and focused
✅ **DO**: Use `struct` for components
❌ **DON'T**: Add methods to components
❌ **DON'T**: Store references in components (use EntityHandle)

### System Design
✅ **DO**: Make systems stateless when possible
✅ **DO**: Use `ref` returns to modify components
✅ **DO**: Query for minimal component sets
❌ **DON'T**: Store entity handles across frames (check `IsAlive`)
❌ **DON'T**: Modify entity composition during iteration

### Performance
✅ **DO**: Use specific queries (fewer components = faster)
✅ **DO**: Batch entity creation/destruction
✅ **DO**: Profile with real workloads
❌ **DON'T**: Create/destroy entities in hot loops
❌ **DON'T**: Query entities you don't need

## Future Enhancements

1. **Multi-threading**: Arch supports parallel system execution
2. **Serialization**: Add save/load for game state
3. **Entity Prefabs**: Template system for common entity types
4. **Component Pooling**: Reuse destroyed entity slots
5. **Debug Visualization**: Entity inspector UI

## References

- [RFC-0007: Arch ECS Integration](../rfcs/0007-arch-ecs-integration.md)
- [Arch ECS GitHub](https://github.com/genaray/Arch)
- [ECS Pattern Overview](https://en.wikipedia.org/wiki/Entity_component_system)

---

**Maintained by**: Winged Bean Team
**Questions**: See `docs/rfcs/0007-arch-ecs-integration.md`

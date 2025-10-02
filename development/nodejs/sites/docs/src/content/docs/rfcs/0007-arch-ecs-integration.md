---
title: "RFC-0007: Arch ECS Integration for Dungeon Crawler Gameplay"
---

# RFC-0007: Arch ECS Integration for Dungeon Crawler Gameplay

## Status

**Proposed** - Ready for Implementation

## Date

2025-10-01

## Summary

Integrate **Arch ECS** (Entity Component System) as the core gameplay implementation layer for the ConsoleDungeon dungeon crawler game, while maintaining the service-oriented architecture at the application level. This provides high-performance entity management (1M+ entities) for game logic while keeping cross-cutting concerns (networking, UI, config) in service plugins.

## Motivation

### Current Problem

ConsoleDungeon currently has:
- ✅ Service-oriented architecture working
- ✅ Terminal.Gui v2 TUI rendering
- ✅ WebSocket/xterm.js integration
- ❌ **No gameplay systems** (no dungeon, no combat, no entities)
- ❌ **No game rules** (no stats, inventory, progression)
- ❌ **No scalable entity management** (would need manual entity tracking)

**Challenges for game development:**
1. Managing thousands of entities (enemies, items, particles) manually is error-prone
2. Update loops with manual entity lists have poor cache performance
3. No standard patterns for gameplay features (combat, AI, movement)
4. Difficult to serialize/deserialize game state for save/load

### Why ECS?

**Entity Component System** solves these problems:

1. **Performance**: Data-oriented design, cache-friendly memory layout
2. **Scalability**: Handle 10,000+ entities at 60 FPS
3. **Flexibility**: Compose entity behaviors from components
4. **Maintainability**: Systems separate concerns cleanly
5. **Serialization**: Components are data, easy to save/load

### Why Arch ECS?

**Arch** is a high-performance ECS library for .NET:

- ✅ **Performance**: >1M entities/second creation
- ✅ **Zero allocation**: Queries don't allocate memory
- ✅ **Source generators**: Compile-time query optimization
- ✅ **.NET 8 native**: Modern C# 12 features
- ✅ **Small footprint**: ~100KB library
- ✅ **Active development**: Well-maintained by genaray
- ✅ **Good documentation**: Examples and benchmarks

**Alternatives considered:**
- **DefaultEcs**: Good, but less performant
- **Entitas**: Unity-focused, complex code generation
- **Custom ECS**: Reinventing the wheel

**Benchmark (from Arch repo):**
```
| Method          | EntityCount | Mean       | Allocated |
|---------------- |------------ |-----------:|----------:|
| CreateEntity    | 1000000     | 139.2 ms   | 0 B       |
| AddComponent    | 1000000     | 156.8 ms   | 0 B       |
| Query           | 1000000     | 42.1 ms    | 0 B       |
```

## Proposal

### Architecture: Service Layer + ECS Layer

```
┌─────────────────────────────────────────────────┐
│     Service Layer (WingedBean Architecture)     │
│                                                 │
│  IConfigService  IWebSocketService  ITerminalUI │
│  IECSService  ← New abstraction                │
└─────────────────────────────────────────────────┘
                        ↓
┌─────────────────────────────────────────────────┐
│        ECS Abstraction (Tier 1 Contract)        │
│                                                 │
│  IECSService - Platform-agnostic ECS interface │
└─────────────────────────────────────────────────┘
                        ↓
┌─────────────────────────────────────────────────┐
│      Arch ECS Implementation (Tier 3 Plugin)    │
│                                                 │
│  ArchECSService - Concrete implementation      │
│  Uses Arch.Core under the hood                 │
└─────────────────────────────────────────────────┘
                        ↓
┌─────────────────────────────────────────────────┐
│           Game Logic (Systems)                  │
│                                                 │
│  MovementSystem  CombatSystem  AISystem  etc.  │
└─────────────────────────────────────────────────┘
                        ↓
┌─────────────────────────────────────────────────┐
│            Game Data (Components)               │
│                                                 │
│  Position  Stats  Renderable  Inventory  etc.  │
└─────────────────────────────────────────────────┘
```

**Key Principle:** Services handle cross-cutting concerns (networking, rendering, config), ECS handles entity behaviors (movement, combat, AI).

### Tier 1: ECS Contract

Create: `framework/src/WingedBean.Contracts.ECS/`

**IECSService.cs:**
```csharp
namespace WingedBean.Contracts.ECS;

/// <summary>
/// Platform-agnostic ECS service interface.
/// Abstracts the underlying ECS implementation (Arch, EnTT, Unity ECS, etc.)
/// </summary>
public interface IECSService
{
    /// <summary>
    /// Create a new entity in the world.
    /// </summary>
    EntityHandle CreateEntity();

    /// <summary>
    /// Create multiple entities efficiently.
    /// </summary>
    EntityHandle[] CreateEntities(int count);

    /// <summary>
    /// Add a component to an entity.
    /// </summary>
    void AddComponent<T>(EntityHandle entity, T component) where T : struct;

    /// <summary>
    /// Get a component from an entity.
    /// </summary>
    ref T GetComponent<T>(EntityHandle entity) where T : struct;

    /// <summary>
    /// Check if entity has a component.
    /// </summary>
    bool HasComponent<T>(EntityHandle entity) where T : struct;

    /// <summary>
    /// Remove a component from an entity.
    /// </summary>
    void RemoveComponent<T>(EntityHandle entity) where T : struct;

    /// <summary>
    /// Destroy an entity and all its components.
    /// </summary>
    void DestroyEntity(EntityHandle entity);

    /// <summary>
    /// Check if entity exists and is valid.
    /// </summary>
    bool IsAlive(EntityHandle entity);

    /// <summary>
    /// Query entities with specific components.
    /// </summary>
    IEnumerable<EntityHandle> Query<T1>() where T1 : struct;
    IEnumerable<EntityHandle> Query<T1, T2>() where T1 : struct where T2 : struct;
    IEnumerable<EntityHandle> Query<T1, T2, T3>()
        where T1 : struct where T2 : struct where T3 : struct;

    /// <summary>
    /// Execute all registered systems for this frame.
    /// </summary>
    void Update(float deltaTime);

    /// <summary>
    /// Register a system to execute each update.
    /// </summary>
    void RegisterSystem(IECSSystem system, int priority = 0);

    /// <summary>
    /// Unregister a system.
    /// </summary>
    void UnregisterSystem(IECSSystem system);

    /// <summary>
    /// Get total entity count.
    /// </summary>
    int EntityCount { get; }
}

/// <summary>
/// Opaque handle to an entity. Internal representation varies by ECS implementation.
/// </summary>
public readonly struct EntityHandle : IEquatable<EntityHandle>
{
    internal readonly int Id;
    internal readonly int WorldId;

    internal EntityHandle(int id, int worldId)
    {
        Id = id;
        WorldId = worldId;
    }

    public bool Equals(EntityHandle other) =>
        Id == other.Id && WorldId == other.WorldId;

    public override bool Equals(object? obj) =>
        obj is EntityHandle handle && Equals(handle);

    public override int GetHashCode() =>
        HashCode.Combine(Id, WorldId);

    public static bool operator ==(EntityHandle left, EntityHandle right) =>
        left.Equals(right);

    public static bool operator !=(EntityHandle left, EntityHandle right) =>
        !left.Equals(right);
}

/// <summary>
/// Base interface for ECS systems.
/// </summary>
public interface IECSSystem
{
    /// <summary>
    /// Execute system logic for this frame.
    /// </summary>
    void Execute(IECSService ecs, float deltaTime);
}
```

**Project file:**
```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>netstandard2.1</TargetFramework>
    <LangVersion>8.0</LangVersion>
    <Nullable>enable</Nullable>
  </PropertyGroup>

  <ItemGroup>
    <ProjectReference Include="..\WingedBean.Contracts.Core\WingedBean.Contracts.Core.csproj" />
  </ItemGroup>
</Project>
```

### Tier 3: Arch ECS Plugin

Create: `console/src/plugins/WingedBean.Plugins.ArchECS/`

**ArchECSService.cs:**
```csharp
using Arch.Core;
using Arch.Core.Extensions;
using WingedBean.Contracts.Core;
using WingedBean.Contracts.ECS;

namespace WingedBean.Plugins.ArchECS;

[Plugin(
    Name = "Arch.ECS",
    Provides = new[] { typeof(IECSService) },
    Priority = 100
)]
public class ArchECSService : IECSService
{
    private readonly World _world;
    private readonly List<SystemEntry> _systems = new();

    private record SystemEntry(IECSSystem System, int Priority);

    public ArchECSService()
    {
        _world = World.Create();
    }

    public EntityHandle CreateEntity()
    {
        var entity = _world.Create();
        return new EntityHandle(entity.Id, _world.Id);
    }

    public EntityHandle[] CreateEntities(int count)
    {
        var handles = new EntityHandle[count];
        for (int i = 0; i < count; i++)
        {
            handles[i] = CreateEntity();
        }
        return handles;
    }

    public void AddComponent<T>(EntityHandle handle, T component) where T : struct
    {
        var entity = ToArchEntity(handle);
        _world.Add(entity, component);
    }

    public ref T GetComponent<T>(EntityHandle handle) where T : struct
    {
        var entity = ToArchEntity(handle);
        return ref _world.Get<T>(entity);
    }

    public bool HasComponent<T>(EntityHandle handle) where T : struct
    {
        var entity = ToArchEntity(handle);
        return _world.Has<T>(entity);
    }

    public void RemoveComponent<T>(EntityHandle handle) where T : struct
    {
        var entity = ToArchEntity(handle);
        _world.Remove<T>(entity);
    }

    public void DestroyEntity(EntityHandle handle)
    {
        var entity = ToArchEntity(handle);
        _world.Destroy(entity);
    }

    public bool IsAlive(EntityHandle handle)
    {
        var entity = ToArchEntity(handle);
        return _world.IsAlive(entity);
    }

    public IEnumerable<EntityHandle> Query<T1>() where T1 : struct
    {
        var query = _world.Query(in new QueryDescription().WithAll<T1>());
        var results = new List<EntityHandle>();

        query.ForEach((Entity entity) =>
        {
            results.Add(new EntityHandle(entity.Id, _world.Id));
        });

        return results;
    }

    public IEnumerable<EntityHandle> Query<T1, T2>()
        where T1 : struct where T2 : struct
    {
        var query = _world.Query(in new QueryDescription().WithAll<T1, T2>());
        var results = new List<EntityHandle>();

        query.ForEach((Entity entity) =>
        {
            results.Add(new EntityHandle(entity.Id, _world.Id));
        });

        return results;
    }

    public IEnumerable<EntityHandle> Query<T1, T2, T3>()
        where T1 : struct where T2 : struct where T3 : struct
    {
        var query = _world.Query(in new QueryDescription().WithAll<T1, T2, T3>());
        var results = new List<EntityHandle>();

        query.ForEach((Entity entity) =>
        {
            results.Add(new EntityHandle(entity.Id, _world.Id));
        });

        return results;
    }

    public void Update(float deltaTime)
    {
        // Execute systems in priority order (highest first)
        foreach (var entry in _systems.OrderByDescending(s => s.Priority))
        {
            entry.System.Execute(this, deltaTime);
        }
    }

    public void RegisterSystem(IECSSystem system, int priority = 0)
    {
        _systems.Add(new SystemEntry(system, priority));
    }

    public void UnregisterSystem(IECSSystem system)
    {
        _systems.RemoveAll(s => s.System == system);
    }

    public int EntityCount => _world.Size;

    private Entity ToArchEntity(EntityHandle handle)
    {
        return new Entity(handle.Id, handle.WorldId);
    }
}
```

**Project file:**
```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <Nullable>enable</Nullable>
    <LangVersion>12.0</LangVersion>
  </PropertyGroup>

  <ItemGroup>
    <!-- Arch ECS -->
    <PackageReference Include="Arch" Version="1.3.0" />
    <PackageReference Include="Arch.SourceGen" Version="1.0.0" />

    <!-- WingedBean Contracts -->
    <ProjectReference Include="../../../framework/src/WingedBean.Contracts.Core/WingedBean.Contracts.Core.csproj" />
    <ProjectReference Include="../../../framework/src/WingedBean.Contracts.ECS/WingedBean.Contracts.ECS.csproj" />
  </ItemGroup>

  <ItemGroup>
    <None Include=".plugin.json">
      <CopyToOutputDirectory>Always</CopyToOutputDirectory>
    </None>
  </ItemGroup>
</Project>
```

**.plugin.json:**
```json
{
  "id": "wingedbean.plugins.archecs",
  "version": "1.0.0",
  "name": "Arch ECS Service",
  "description": "High-performance Entity Component System using Arch",
  "author": "WingedBean Team",
  "provides": [
    "WingedBean.Contracts.ECS.IECSService"
  ],
  "dependencies": [],
  "loadStrategy": "Lazy",
  "entryPoint": "WingedBean.Plugins.ArchECS.ArchECSService"
}
```

### Game Components

Create: `console/src/host/ConsoleDungeon/Components/`

**CoreComponents.cs:**
```csharp
namespace ConsoleDungeon.Components;

/// <summary>
/// Position in the dungeon (tile-based).
/// </summary>
public struct Position
{
    public int X;
    public int Y;
    public int Floor;

    public Position(int x, int y, int floor = 1)
    {
        X = x;
        Y = y;
        Floor = floor;
    }
}

/// <summary>
/// Character statistics.
/// </summary>
public struct Stats
{
    public int MaxHP;
    public int CurrentHP;
    public int MaxMana;
    public int CurrentMana;
    public int Strength;
    public int Dexterity;
    public int Intelligence;
    public int Defense;
    public int Level;
    public int Experience;
}

/// <summary>
/// Visual representation in terminal.
/// </summary>
public struct Renderable
{
    public char Symbol;
    public ConsoleColor ForegroundColor;
    public ConsoleColor BackgroundColor;
    public int RenderLayer; // 0=floor, 1=items, 2=creatures, 3=effects
}

/// <summary>
/// Player-controlled entity marker (tag component).
/// </summary>
public struct Player { }

/// <summary>
/// Name component.
/// </summary>
public struct Name
{
    public string Value;

    public Name(string value) => Value = value;
}

/// <summary>
/// Enemy AI component.
/// </summary>
public struct Enemy
{
    public EnemyType Type;
    public AIState State;
    public float AggroRange;
    public EntityHandle? Target;
}

public enum EnemyType
{
    Goblin,
    Orc,
    Skeleton,
    Troll,
    Dragon
}

public enum AIState
{
    Idle,
    Patrol,
    Chase,
    Attack,
    Flee
}

/// <summary>
/// Movement velocity (for smooth animations).
/// </summary>
public struct Velocity
{
    public float X;
    public float Y;
}

/// <summary>
/// Collision/blocking component.
/// </summary>
public struct Blocking
{
    public bool BlocksMovement;
    public bool BlocksLight;
}

/// <summary>
/// Item component.
/// </summary>
public struct Item
{
    public ItemType Type;
    public int Value;
    public bool Stackable;
    public int StackCount;
}

public enum ItemType
{
    Gold,
    HealthPotion,
    ManaPotion,
    Sword,
    Shield,
    Armor,
    Helmet,
    Boots,
    Key
}

/// <summary>
/// Inventory component (stores entity handles to item entities).
/// </summary>
public struct Inventory
{
    public List<EntityHandle> Items;
    public int MaxSlots;

    public Inventory(int maxSlots)
    {
        Items = new List<EntityHandle>();
        MaxSlots = maxSlots;
    }
}
```

### Game Systems

Create: `console/src/host/ConsoleDungeon/Systems/`

**MovementSystem.cs:**
```csharp
using WingedBean.Contracts.ECS;
using ConsoleDungeon.Components;

namespace ConsoleDungeon.Systems;

public class MovementSystem : IECSSystem
{
    private const int DungeonWidth = 80;
    private const int DungeonHeight = 24;

    public void Execute(IECSService ecs, float deltaTime)
    {
        // Query all entities with Position + Velocity
        foreach (var entity in ecs.Query<Position, Velocity>())
        {
            ref var pos = ref ecs.GetComponent<Position>(entity);
            ref var vel = ref ecs.GetComponent<Velocity>(entity);

            // Apply velocity
            pos.X += (int)Math.Round(vel.X * deltaTime);
            pos.Y += (int)Math.Round(vel.Y * deltaTime);

            // Clamp to dungeon bounds
            pos.X = Math.Clamp(pos.X, 0, DungeonWidth - 1);
            pos.Y = Math.Clamp(pos.Y, 0, DungeonHeight - 1);

            // Reset velocity (for discrete movement)
            vel.X = 0;
            vel.Y = 0;
        }
    }
}
```

**CombatSystem.cs:**
```csharp
using WingedBean.Contracts.ECS;
using ConsoleDungeon.Components;

namespace ConsoleDungeon.Systems;

public class CombatSystem : IECSSystem
{
    public void Execute(IECSService ecs, float deltaTime)
    {
        // Find player
        EntityHandle? playerHandle = null;
        foreach (var entity in ecs.Query<Player, Position, Stats>())
        {
            playerHandle = entity;
            break;
        }

        if (!playerHandle.HasValue)
            return;

        ref var playerPos = ref ecs.GetComponent<Position>(playerHandle.Value);
        ref var playerStats = ref ecs.GetComponent<Stats>(playerHandle.Value);

        // Check all enemies
        foreach (var enemyEntity in ecs.Query<Enemy, Position, Stats>())
        {
            ref var enemyPos = ref ecs.GetComponent<Position>(enemyEntity);
            ref var enemyStats = ref ecs.GetComponent<Stats>(enemyEntity);
            ref var enemy = ref ecs.GetComponent<Enemy>(enemyEntity);

            // If adjacent, combat happens
            if (IsAdjacent(playerPos, enemyPos))
            {
                // Simple combat: both deal damage
                enemyStats.CurrentHP -= playerStats.Strength - enemyStats.Defense / 2;
                playerStats.CurrentHP -= enemyStats.Strength - playerStats.Defense / 2;

                // Check for death
                if (enemyStats.CurrentHP <= 0)
                {
                    // Enemy died - destroy entity
                    ecs.DestroyEntity(enemyEntity);

                    // Award XP
                    playerStats.Experience += 10;
                }

                if (playerStats.CurrentHP <= 0)
                {
                    // Player died - game over
                    playerStats.CurrentHP = 0;
                }
            }
        }
    }

    private static bool IsAdjacent(Position a, Position b)
    {
        return Math.Abs(a.X - b.X) <= 1 &&
               Math.Abs(a.Y - b.Y) <= 1 &&
               a.Floor == b.Floor;
    }
}
```

**RenderSystem.cs:**
```csharp
using WingedBean.Contracts.ECS;
using WingedBean.Contracts.TerminalUI;
using ConsoleDungeon.Components;

namespace ConsoleDungeon.Systems;

public class RenderSystem : IECSSystem
{
    private readonly ITerminalUIService _terminal;

    public RenderSystem(ITerminalUIService terminal)
    {
        _terminal = terminal;
    }

    public void Execute(IECSService ecs, float deltaTime)
    {
        // Clear screen
        _terminal.Clear();

        // Get all renderable entities
        var renderables = new List<(Position pos, Renderable render)>();

        foreach (var entity in ecs.Query<Position, Renderable>())
        {
            var pos = ecs.GetComponent<Position>(entity);
            var render = ecs.GetComponent<Renderable>(entity);
            renderables.Add((pos, render));
        }

        // Sort by render layer (floor first, then items, then creatures, then effects)
        var sorted = renderables.OrderBy(r => r.render.RenderLayer);

        // Render each entity
        foreach (var (pos, render) in sorted)
        {
            _terminal.DrawChar(pos.X, pos.Y, render.Symbol, render.ForegroundColor);
        }

        // Render UI (HP, stats, etc.)
        RenderUI(ecs);

        // Present to screen
        _terminal.Present();
    }

    private void RenderUI(IECSService ecs)
    {
        // Find player
        foreach (var entity in ecs.Query<Player, Stats>())
        {
            var stats = ecs.GetComponent<Stats>(entity);

            // Render HP bar at bottom
            _terminal.DrawText(0, 23,
                $"HP: {stats.CurrentHP}/{stats.MaxHP}  " +
                $"MP: {stats.CurrentMana}/{stats.MaxMana}  " +
                $"Level: {stats.Level}  " +
                $"XP: {stats.Experience}");

            break;
        }
    }
}
```

### Integration with DungeonGame

**DungeonGame.cs:**
```csharp
using WingedBean.Contracts.Core;
using WingedBean.Contracts.ECS;
using WingedBean.Contracts.TerminalUI;
using ConsoleDungeon.Systems;
using ConsoleDungeon.Components;

namespace ConsoleDungeon;

public class DungeonGame
{
    private readonly IRegistry _registry;
    private readonly IECSService _ecs;
    private readonly ITerminalUIService _terminal;

    public DungeonGame(IRegistry registry)
    {
        _registry = registry;
        _ecs = registry.Get<IECSService>();
        _terminal = registry.Get<ITerminalUIService>();

        InitializeSystems();
        InitializeWorld();
    }

    private void InitializeSystems()
    {
        // Register systems in execution order
        _ecs.RegisterSystem(new MovementSystem(), priority: 100);
        _ecs.RegisterSystem(new AISystem(), priority: 90);
        _ecs.RegisterSystem(new CombatSystem(), priority: 80);
        _ecs.RegisterSystem(new RenderSystem(_terminal), priority: 10);
    }

    private void InitializeWorld()
    {
        // Create player
        var player = _ecs.CreateEntity();
        _ecs.AddComponent(player, new Player());
        _ecs.AddComponent(player, new Position(40, 12, 1));
        _ecs.AddComponent(player, new Name("Hero"));
        _ecs.AddComponent(player, new Stats
        {
            MaxHP = 100,
            CurrentHP = 100,
            MaxMana = 50,
            CurrentMana = 50,
            Strength = 10,
            Dexterity = 10,
            Intelligence = 10,
            Defense = 5,
            Level = 1,
            Experience = 0
        });
        _ecs.AddComponent(player, new Renderable
        {
            Symbol = '@',
            ForegroundColor = ConsoleColor.White,
            RenderLayer = 2
        });
        _ecs.AddComponent(player, new Inventory(20));
        _ecs.AddComponent(player, new Velocity());

        // Create some goblins
        for (int i = 0; i < 5; i++)
        {
            var goblin = _ecs.CreateEntity();
            _ecs.AddComponent(goblin, new Enemy
            {
                Type = EnemyType.Goblin,
                State = AIState.Idle,
                AggroRange = 5.0f
            });
            _ecs.AddComponent(goblin, new Position(
                Random.Shared.Next(10, 70),
                Random.Shared.Next(2, 22),
                1
            ));
            _ecs.AddComponent(goblin, new Name("Goblin"));
            _ecs.AddComponent(goblin, new Stats
            {
                MaxHP = 20,
                CurrentHP = 20,
                Strength = 5,
                Defense = 2
            });
            _ecs.AddComponent(goblin, new Renderable
            {
                Symbol = 'g',
                ForegroundColor = ConsoleColor.Green,
                RenderLayer = 2
            });
            _ecs.AddComponent(goblin, new Velocity());
        }
    }

    public async Task RunAsync()
    {
        var lastTime = DateTime.UtcNow;

        while (true)
        {
            var currentTime = DateTime.UtcNow;
            var deltaTime = (float)(currentTime - lastTime).TotalSeconds;
            lastTime = currentTime;

            // Handle input (keyboard events)
            HandleInput();

            // Update ECS (all systems execute)
            _ecs.Update(deltaTime);

            // Target 60 FPS
            await Task.Delay(16);
        }
    }

    private void HandleInput()
    {
        // TODO: Get input from Terminal UI service
        // Apply velocity to player for movement
    }
}
```

## Migration Plan

### Phase 1: Create Contract (Day 1)

1. Create `framework/src/WingedBean.Contracts.ECS/` project
2. Implement `IECSService` interface
3. Add to `Framework.sln`
4. Build and verify

### Phase 2: Create Arch Plugin (Day 2)

1. Create `console/src/plugins/WingedBean.Plugins.ArchECS/` project
2. Add Arch NuGet package
3. Implement `ArchECSService`
4. Create `.plugin.json` manifest
5. Build and verify

### Phase 3: Define Components (Day 3)

1. Create `console/src/host/ConsoleDungeon/Components/` directory
2. Implement all game components
3. Build and verify

### Phase 4: Implement Systems (Day 4-5)

1. Create `console/src/host/ConsoleDungeon/Systems/` directory
2. Implement `MovementSystem`
3. Implement `CombatSystem`
4. Implement `RenderSystem`
5. Test each system individually

### Phase 5: Integration (Day 6)

1. Update `plugins.json` to include Arch ECS
2. Create `DungeonGame` class
3. Wire up systems and world initialization
4. Test end-to-end

### Phase 6: Testing (Day 7)

1. Verify entities create correctly
2. Verify movement works
3. Verify combat triggers
4. Verify rendering correct
5. Performance test (10,000+ entities)

## Benefits

### Performance
- ✅ Handle 10,000+ entities at 60 FPS
- ✅ Cache-friendly memory layout
- ✅ Zero-allocation queries

### Scalability
- ✅ Add new components without modifying existing code
- ✅ Systems are independent, easy to test
- ✅ Easy to add new gameplay features

### Maintainability
- ✅ Clear separation of concerns (data vs. behavior)
- ✅ Components are pure data (easy to serialize)
- ✅ Systems are pure logic (easy to test)

### Flexibility
- ✅ Compose entities from components
- ✅ Swap ECS implementations (Arch → DefaultEcs)
- ✅ Unity can use Unity ECS, Console uses Arch

## Definition of Done

### Contracts
- [ ] `WingedBean.Contracts.ECS` project created
- [ ] `IECSService` interface complete
- [ ] Added to Framework.sln
- [ ] Builds successfully

### Plugin
- [ ] `WingedBean.Plugins.ArchECS` project created
- [ ] Arch NuGet package added
- [ ] `ArchECSService` implementation complete
- [ ] `.plugin.json` manifest created
- [ ] Builds successfully

### Components
- [ ] All core components defined
- [ ] Components are structs (value types)
- [ ] Components compile successfully

### Systems
- [ ] Movement system implemented
- [ ] Combat system implemented
- [ ] Render system implemented
- [ ] Systems tested individually

### Integration
- [ ] Arch ECS in `plugins.json`
- [ ] `DungeonGame` class created
- [ ] Systems registered
- [ ] World initialization works

### Testing
- [ ] Player entity creates
- [ ] Enemy entities create
- [ ] Movement works
- [ ] Combat works
- [ ] Rendering works
- [ ] Performance acceptable (60 FPS with 1000+ entities)

## Dependencies

- RFC-0005: Target Framework Compliance
- RFC-0006: Dynamic Plugin Loading
- Arch ECS library (NuGet)

## References

- [Arch ECS GitHub](https://github.com/genaray/Arch)
- [Arch ECS Wiki](https://github.com/genaray/Arch/wiki)
- [ECS Pattern Overview](https://en.wikipedia.org/wiki/Entity_component_system)

---

**Author:** System Analysis
**Reviewers:** [Pending]
**Status:** Proposed - Awaiting approval
**Priority:** HIGH (P1)
**Estimated Effort:** 7 days
**Target Date:** 2025-10-13
**Dependencies:** RFC-0005, RFC-0006

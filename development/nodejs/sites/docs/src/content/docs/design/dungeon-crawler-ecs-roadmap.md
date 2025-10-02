# Dungeon Crawler ECS Enhancement Roadmap

## Overview

This document outlines the next steps for enhancing the WingedBean console project to become a fully-featured dungeon crawler game with **Arch ECS** as the gameplay implementation layer, while maintaining the **service-oriented architecture** at the top level.

**Date:** 2025-10-01
**Status:** Planning Phase
**Target:** Q4 2025 - Q1 2026

---

## Current State Assessment

### ✅ What We Have

**Architecture Foundation:**
- ✅ 4-tiered architecture implemented
- ✅ Service registry pattern working
- ✅ Plugin system functional
- ✅ Terminal.Gui v2 TUI running
- ✅ WebSocket/xterm.js integration working
- ✅ PTY service available

**Technical Stack:**
- Framework: `.NET 9.0` (needs downgrade to `.NET 8.0` for Tier 3/4)
- Contracts: `net9.0` (needs change to `.NET Standard 2.1`)
- Terminal UI: Terminal.Gui v2
- Communication: SuperSocket WebSocket

### ❌ What We Need

**Compliance Issues:**
1. **Target Frameworks:** Contracts using `net9.0` instead of `netstandard2.1`
2. **Plugin Loading:** Currently static references, need dynamic runtime loading
3. **Gameplay Systems:** No ECS implementation
4. **Game Rules:** No dungeon crawler mechanics
5. **PTY Integration:** Not integrated with main game loop
6. **Source Gen:** Missing analyzer/source generator project

**Game Features Missing:**
- Entity Component System (Arch ECS)
- Dungeon generation
- Character systems (stats, inventory, etc.)
- Combat mechanics
- AI/enemy behaviors
- Progression systems
- Save/Load functionality

---

## Phase 1: Framework Compliance (Week 1-2)

### Priority: CRITICAL - Foundation Must Be Correct

### 1.1 Fix Target Frameworks

**Goal:** Ensure Tier 1 contracts are portable, Tier 3/4 use modern .NET

#### Actions:

**A. Update All Tier 1 Contract Projects → `netstandard2.1`**

Projects to update:
```
framework/src/WingedBean.Contracts.Core
framework/src/WingedBean.Contracts.Config
framework/src/WingedBean.Contracts.Audio
framework/src/WingedBean.Contracts.Resource
framework/src/WingedBean.Contracts.WebSocket
framework/src/WingedBean.Contracts.TerminalUI
framework/src/WingedBean.Contracts.Pty
```

Change in each `.csproj`:
```xml
<TargetFramework>netstandard2.1</TargetFramework>
```

**Reason:** Allows contracts to be used in Unity (via .NET Standard 2.1 support), Godot, and other platforms.

**B. Update Tier 2 Registry → `netstandard2.1`**

Project: `framework/src/WingedBean.Registry`

**Reason:** Registry is shared infrastructure, must be portable.

**C. Keep Tier 3/4 Console Projects → `net8.0`**

Projects to update from `net9.0` → `net8.0`:
```
console/src/shared/WingedBean.PluginLoader
console/src/providers/WingedBean.Providers.AssemblyContext
console/src/plugins/WingedBean.Plugins.*
console/src/host/ConsoleDungeon
console/src/host/ConsoleDungeon.Host
```

**Reason:** `.NET 8.0` is LTS (Long Term Support), more stable for production.

**D. Create Source Generator Project → `netstandard2.0`**

New project:
```
framework/src/WingedBean.Contracts.SourceGen/
```

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>netstandard2.0</TargetFramework>
    <EnforceExtendedAnalyzerRules>true</EnforceExtendedAnalyzerRules>
    <IsRoslynComponent>true</IsRoslynComponent>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Microsoft.CodeAnalysis.CSharp" Version="4.3.0" PrivateAssets="all" />
    <PackageReference Include="Microsoft.CodeAnalysis.Analyzers" Version="3.3.4" PrivateAssets="all" />
  </ItemGroup>
</Project>
```

**Reason:** Roslyn analyzers/source generators require `netstandard2.0` for widest compatibility.

#### Verification:

```bash
# Build framework (should work with .NET Standard 2.1)
cd framework
dotnet build Framework.sln

# Build console (should work with .NET 8.0)
cd ../console
dotnet build Console.sln
```

---

### 1.2 Implement Dynamic Plugin Loading

**Goal:** Load plugins at runtime via configuration, not static references

#### Current Problem:

`ConsoleDungeon.Host.csproj` has static references:
```xml
<ProjectReference Include="../WingedBean.Plugins.WebSocket/..." />
<ProjectReference Include="../WingedBean.Plugins.TerminalUI/..." />
<ProjectReference Include="../WingedBean.Plugins.Config/..." />
```

#### Solution:

**A. Create Plugin Configuration File**

`console/src/host/ConsoleDungeon.Host/plugins.json`:
```json
{
  "plugins": [
    {
      "id": "wingedbean.plugins.config",
      "path": "plugins/WingedBean.Plugins.Config.dll",
      "priority": 1000,
      "loadStrategy": "Eager"
    },
    {
      "id": "wingedbean.plugins.websocket",
      "path": "plugins/WingedBean.Plugins.WebSocket.dll",
      "priority": 100,
      "loadStrategy": "Eager"
    },
    {
      "id": "wingedbean.plugins.terminalui",
      "path": "plugins/WingedBean.Plugins.TerminalUI.dll",
      "priority": 100,
      "loadStrategy": "Eager"
    },
    {
      "id": "wingedbean.plugins.ecs",
      "path": "plugins/WingedBean.Plugins.ArchECS.dll",
      "priority": 50,
      "loadStrategy": "Lazy"
    }
  ]
}
```

**B. Update ConsoleDungeon.Host to Use ActualPluginLoader**

Remove static `ProjectReference` entries, use dynamic loading:

```csharp
// ConsoleDungeon.Host/Program.cs
public static async Task Main(string[] args)
{
    Console.WriteLine("ConsoleDungeon.Host - Dynamic Plugin Loading");

    // Step 1: Create foundation services
    var registry = new ActualRegistry();
    var contextProvider = new AssemblyContextProvider();
    var pluginLoader = new ActualPluginLoader(contextProvider);

    registry.Register<IRegistry>(registry);
    registry.Register<IPluginLoader>(pluginLoader);

    // Step 2: Load plugins from configuration
    var pluginConfig = LoadPluginConfiguration("plugins.json");

    foreach (var plugin in pluginConfig.Plugins.OrderBy(p => p.Priority))
    {
        Console.WriteLine($"Loading plugin: {plugin.Id}...");
        var loadedPlugin = await pluginLoader.LoadAsync(plugin.Path);

        // Auto-register services from plugin
        foreach (var service in loadedPlugin.GetServices())
        {
            var serviceType = service.GetType().GetInterfaces()
                .FirstOrDefault(i => i.Namespace?.StartsWith("WingedBean.Contracts") == true);

            if (serviceType != null)
            {
                registry.Register(serviceType, service, plugin.Priority);
            }
        }
    }

    // Step 3: Launch game
    var app = new ConsoleDungeon.Program(registry);
    await app.RunAsync();
}
```

**C. Build Script to Copy Plugins**

Create `console/src/host/ConsoleDungeon.Host/copy-plugins.targets`:
```xml
<Project>
  <Target Name="CopyPlugins" AfterTargets="Build">
    <ItemGroup>
      <PluginFiles Include="../../plugins/**/bin/$(Configuration)/$(TargetFramework)/*.dll" />
    </ItemGroup>
    <Copy SourceFiles="@(PluginFiles)" DestinationFolder="$(OutDir)plugins/" />
  </Target>
</Project>
```

Import in `.csproj`:
```xml
<Import Project="copy-plugins.targets" />
```

#### Verification:

```bash
dotnet run --project console/src/host/ConsoleDungeon.Host/ConsoleDungeon.Host.csproj
# Should see: "Loading plugin: wingedbean.plugins.config..."
```

---

## Phase 2: Arch ECS Integration (Week 3-4)

### Priority: HIGH - Core Gameplay Foundation

### 2.1 Add Arch ECS NuGet Package

**Documentation:** https://github.com/genaray/Arch

**Arch ECS Benefits:**
- ✅ High performance (>1M entities/sec)
- ✅ Zero allocation queries
- ✅ Source generators for query optimization
- ✅ .NET 8 native
- ✅ Small memory footprint

#### Actions:

**A. Create Arch ECS Plugin Project**

New project structure:
```
console/src/plugins/WingedBean.Plugins.ArchECS/
├── WingedBean.Plugins.ArchECS.csproj
├── ArchECSPlugin.cs                  # Plugin entry point
├── Systems/
│   ├── MovementSystem.cs             # Entity movement
│   ├── CombatSystem.cs               # Combat resolution
│   ├── AISystem.cs                   # Enemy AI
│   └── RenderSystem.cs               # Terminal rendering
├── Components/
│   ├── TransformComponent.cs         # Position, rotation
│   ├── StatsComponent.cs             # HP, Mana, Stats
│   ├── InventoryComponent.cs         # Items
│   ├── AIComponent.cs                # AI state
│   └── RenderComponent.cs            # Visual representation
└── Queries/
    ├── PlayerQuery.cs                # Player entities
    ├── EnemyQuery.cs                 # Enemy entities
    └── ItemQuery.cs                  # Item entities
```

**B. Add Arch Package**

`WingedBean.Plugins.ArchECS.csproj`:
```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <Nullable>enable</Nullable>
  </PropertyGroup>

  <ItemGroup>
    <!-- Arch ECS Core -->
    <PackageReference Include="Arch" Version="1.3.0" />
    <PackageReference Include="Arch.SourceGen" Version="1.0.0" />

    <!-- WingedBean Contracts -->
    <ProjectReference Include="../../../framework/src/WingedBean.Contracts.Core/WingedBean.Contracts.Core.csproj" />
  </ItemGroup>
</Project>
```

**C. Create ECS Service Contract (Tier 1)**

New contract: `framework/src/WingedBean.Contracts.ECS/`

```csharp
// WingedBean.Contracts.ECS/IECSService.cs
namespace WingedBean.Contracts.ECS;

/// <summary>
/// ECS service for managing game entities and systems.
/// Abstracts the underlying ECS implementation (Arch, EnTT, etc.)
/// </summary>
public interface IECSService
{
    /// <summary>
    /// Create a new entity in the world.
    /// </summary>
    EntityHandle CreateEntity();

    /// <summary>
    /// Add a component to an entity.
    /// </summary>
    void AddComponent<T>(EntityHandle entity, T component) where T : struct;

    /// <summary>
    /// Get a component from an entity.
    /// </summary>
    ref T GetComponent<T>(EntityHandle entity) where T : struct;

    /// <summary>
    /// Remove a component from an entity.
    /// </summary>
    void RemoveComponent<T>(EntityHandle entity) where T : struct;

    /// <summary>
    /// Destroy an entity.
    /// </summary>
    void DestroyEntity(EntityHandle entity);

    /// <summary>
    /// Execute all registered systems for this frame.
    /// </summary>
    void Update(float deltaTime);

    /// <summary>
    /// Register a system to run each frame.
    /// </summary>
    void RegisterSystem(IECSSystem system);
}

public readonly struct EntityHandle
{
    public readonly int Id;
    public EntityHandle(int id) => Id = id;
}

public interface IECSSystem
{
    void Execute(IECSService ecs, float deltaTime);
}
```

**D. Implement Arch-based ECS Service**

```csharp
// WingedBean.Plugins.ArchECS/ArchECSService.cs
using Arch.Core;
using Arch.Core.Extensions;
using WingedBean.Contracts.ECS;
using WingedBean.Contracts.Core;

namespace WingedBean.Plugins.ArchECS;

[Plugin(
    Name = "Arch.ECS",
    Provides = new[] { typeof(IECSService) },
    Priority = 100
)]
public class ArchECSService : IECSService
{
    private readonly World _world;
    private readonly List<IECSSystem> _systems = new();

    public ArchECSService()
    {
        _world = World.Create();
    }

    public EntityHandle CreateEntity()
    {
        var entity = _world.Create();
        return new EntityHandle(entity.Id);
    }

    public void AddComponent<T>(EntityHandle handle, T component) where T : struct
    {
        var entity = new Entity(handle.Id, _world.Id);
        _world.Add(entity, component);
    }

    public ref T GetComponent<T>(EntityHandle handle) where T : struct
    {
        var entity = new Entity(handle.Id, _world.Id);
        return ref _world.Get<T>(entity);
    }

    public void RemoveComponent<T>(EntityHandle handle) where T : struct
    {
        var entity = new Entity(handle.Id, _world.Id);
        _world.Remove<T>(entity);
    }

    public void DestroyEntity(EntityHandle handle)
    {
        var entity = new Entity(handle.Id, _world.Id);
        _world.Destroy(entity);
    }

    public void Update(float deltaTime)
    {
        foreach (var system in _systems)
        {
            system.Execute(this, deltaTime);
        }
    }

    public void RegisterSystem(IECSSystem system)
    {
        _systems.Add(system);
    }
}
```

---

### 2.2 Define Game Components

**Goal:** Create reusable ECS components for dungeon crawler mechanics

```csharp
// WingedBean.Plugins.ArchECS/Components/CoreComponents.cs
namespace WingedBean.Plugins.ArchECS.Components;

/// <summary>
/// Position in the dungeon (tile-based).
/// </summary>
public struct Position
{
    public int X;
    public int Y;
    public int Floor;
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
/// Player-controlled entity marker.
/// </summary>
public struct Player { }

/// <summary>
/// Enemy AI component.
/// </summary>
public struct Enemy
{
    public EnemyType Type;
    public AIState State;
    public float AggroRange;
}

public enum EnemyType
{
    Goblin,
    Orc,
    Skeleton,
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
    Key
}

/// <summary>
/// Inventory component (list of entity handles to items).
/// </summary>
public struct Inventory
{
    public List<EntityHandle> Items;
    public int MaxSlots;
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
```

---

### 2.3 Implement Core Systems

**Goal:** Create systems for gameplay logic

```csharp
// WingedBean.Plugins.ArchECS/Systems/MovementSystem.cs
using Arch.Core;
using Arch.Core.Extensions;
using WingedBean.Contracts.ECS;
using WingedBean.Plugins.ArchECS.Components;

namespace WingedBean.Plugins.ArchECS.Systems;

/// <summary>
/// Handles entity movement based on velocity.
/// </summary>
public class MovementSystem : IECSSystem
{
    public void Execute(IECSService ecs, float deltaTime)
    {
        // Arch query: all entities with Position + Velocity
        var query = Query<Position, Velocity>();

        query.ForEach((ref Position pos, ref Velocity vel) =>
        {
            // Apply velocity
            pos.X += (int)(vel.X * deltaTime);
            pos.Y += (int)(vel.Y * deltaTime);

            // Clamp to dungeon bounds
            pos.X = Math.Clamp(pos.X, 0, DungeonWidth - 1);
            pos.Y = Math.Clamp(pos.Y, 0, DungeonHeight - 1);
        });
    }

    private const int DungeonWidth = 80;
    private const int DungeonHeight = 24;
}

// WingedBean.Plugins.ArchECS/Systems/CombatSystem.cs
public class CombatSystem : IECSSystem
{
    public void Execute(IECSService ecs, float deltaTime)
    {
        // Query: Player + Position + Stats
        var playerQuery = Query<Player, Position, Stats>();

        // Query: Enemy + Position + Stats
        var enemyQuery = Query<Enemy, Position, Stats>();

        // Check for combat interactions
        playerQuery.ForEach((ref Player player, ref Position playerPos, ref Stats playerStats) =>
        {
            enemyQuery.ForEach((ref Enemy enemy, ref Position enemyPos, ref Stats enemyStats) =>
            {
                // If adjacent, resolve combat
                if (IsAdjacent(playerPos, enemyPos))
                {
                    // Simple combat: reduce enemy HP
                    enemyStats.CurrentHP -= playerStats.Strength;

                    if (enemyStats.CurrentHP <= 0)
                    {
                        // Destroy enemy entity
                        // (handle in separate cleanup system)
                    }
                }
            });
        });
    }

    private bool IsAdjacent(Position a, Position b)
    {
        return Math.Abs(a.X - b.X) <= 1 && Math.Abs(a.Y - b.Y) <= 1;
    }
}

// WingedBean.Plugins.ArchECS/Systems/RenderSystem.cs
public class RenderSystem : IECSSystem
{
    private readonly ITerminalUIService _terminalUI;

    public RenderSystem(ITerminalUIService terminalUI)
    {
        _terminalUI = terminalUI;
    }

    public void Execute(IECSService ecs, float deltaTime)
    {
        // Clear screen
        _terminalUI.Clear();

        // Query all renderable entities
        var query = Query<Position, Renderable>();

        // Sort by render layer
        var sorted = query.OrderBy(e => e.GetComponent<Renderable>().RenderLayer);

        foreach (var entity in sorted)
        {
            var pos = entity.GetComponent<Position>();
            var render = entity.GetComponent<Renderable>();

            _terminalUI.DrawChar(pos.X, pos.Y, render.Symbol, render.ForegroundColor);
        }

        // Present to screen
        _terminalUI.Present();
    }
}
```

---

### 2.4 Integrate ECS with Game Loop

**Goal:** Connect ECS systems to the main game loop

```csharp
// ConsoleDungeon/DungeonGame.cs
using WingedBean.Contracts.Core;
using WingedBean.Contracts.ECS;
using WingedBean.Contracts.TerminalUI;
using WingedBean.Plugins.ArchECS.Systems;
using WingedBean.Plugins.ArchECS.Components;

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
        _ecs.RegisterSystem(new MovementSystem());
        _ecs.RegisterSystem(new AISystem());
        _ecs.RegisterSystem(new CombatSystem());
        _ecs.RegisterSystem(new RenderSystem(_terminal));
    }

    private void InitializeWorld()
    {
        // Create player entity
        var player = _ecs.CreateEntity();
        _ecs.AddComponent(player, new Player());
        _ecs.AddComponent(player, new Position { X = 10, Y = 10, Floor = 1 });
        _ecs.AddComponent(player, new Stats
        {
            MaxHP = 100,
            CurrentHP = 100,
            Strength = 10,
            Defense = 5
        });
        _ecs.AddComponent(player, new Renderable
        {
            Symbol = '@',
            ForegroundColor = ConsoleColor.White,
            RenderLayer = 2
        });
        _ecs.AddComponent(player, new Inventory
        {
            Items = new List<EntityHandle>(),
            MaxSlots = 20
        });

        // Create some enemies
        for (int i = 0; i < 5; i++)
        {
            var enemy = _ecs.CreateEntity();
            _ecs.AddComponent(enemy, new Enemy
            {
                Type = EnemyType.Goblin,
                State = AIState.Idle
            });
            _ecs.AddComponent(enemy, new Position
            {
                X = Random.Shared.Next(80),
                Y = Random.Shared.Next(24),
                Floor = 1
            });
            _ecs.AddComponent(enemy, new Stats { MaxHP = 20, CurrentHP = 20 });
            _ecs.AddComponent(enemy, new Renderable
            {
                Symbol = 'g',
                ForegroundColor = ConsoleColor.Green,
                RenderLayer = 2
            });
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

            // Process input (from TerminalUI service)
            // ...

            // Update ECS systems
            _ecs.Update(deltaTime);

            await Task.Delay(16); // ~60 FPS
        }
    }
}
```

---

## Phase 3: Dungeon Generation (Week 5-6)

### 3.1 Procedural Dungeon Generator

**Goal:** Generate random dungeons using BSP or cellular automata

**Options:**
1. **Binary Space Partitioning (BSP)** - Clean rectangular rooms
2. **Cellular Automata** - Organic cave-like dungeons
3. **Wave Function Collapse** - Complex tile-based generation

**Recommended:** Start with BSP (simpler, faster)

```csharp
// WingedBean.Plugins.DungeonGen/BSPGenerator.cs
public class BSPDungeonGenerator
{
    public Dungeon Generate(int width, int height)
    {
        var dungeon = new Dungeon(width, height);
        var root = new BSPNode(0, 0, width, height);

        // Split recursively
        root.Split(minRoomSize: 6, maxDepth: 4);

        // Create rooms in leaf nodes
        CreateRooms(root, dungeon);

        // Connect rooms with corridors
        ConnectRooms(root, dungeon);

        return dungeon;
    }
}
```

### 3.2 Dungeon Service Contract

```csharp
// WingedBean.Contracts.Dungeon/IDungeonService.cs
public interface IDungeonService
{
    Dungeon GenerateDungeon(int floor, int width, int height);
    Tile GetTile(int x, int y, int floor);
    void SetTile(int x, int y, int floor, Tile tile);
    List<Room> GetRooms(int floor);
}
```

---

## Phase 4: PTY & xterm.js Integration (Week 7)

### 4.1 Dual Launch Mode

**Goal:** Support both standalone Terminal.Gui and PTY/xterm.js modes

**Implementation:**

```csharp
// ConsoleDungeon.Host/Program.cs
public static async Task Main(string[] args)
{
    var launchMode = args.Contains("--pty") ? LaunchMode.PTY : LaunchMode.Standalone;

    // ... registry setup ...

    if (launchMode == LaunchMode.PTY)
    {
        // Load PTY service plugin
        var ptyPlugin = await pluginLoader.LoadAsync("plugins/WingedBean.Plugins.PtyService.dll");
        var ptyService = ptyPlugin.GetService<IPtyService>();
        registry.Register<IPtyService>(ptyService);

        // Start PTY host (outputs to stdout/stderr)
        var ptyHost = new PtyTerminalHost(registry);
        await ptyHost.RunAsync();
    }
    else
    {
        // Standard Terminal.Gui mode
        var app = new DungeonGame(registry);
        await app.RunAsync();
    }
}
```

**Usage:**
```bash
# Standalone mode
dotnet run

# PTY mode (for xterm.js)
dotnet run -- --pty
```

**PTY Service wraps the process:**
```javascript
// Node.js PTY wrapper
const pty = require('node-pty');
const process = pty.spawn('dotnet',
  ['run', '--project', 'ConsoleDungeon.Host.csproj', '--', '--pty'],
  { cwd: './console/src/host/ConsoleDungeon.Host' }
);

// Stream to WebSocket → xterm.js
process.onData(data => wsServer.broadcast(data));
```

---

## Phase 5: Game Rules & Content (Week 8-10)

### 5.1 Core Gameplay Loop

1. **Exploration:** Navigate dungeon rooms
2. **Combat:** Turn-based or real-time combat
3. **Loot:** Find items, gold, equipment
4. **Progression:** Level up, gain stats
5. **Objectives:** Find stairs, defeat bosses, escape dungeon

### 5.2 Content Systems

**A. Item Database**
```csharp
// Data-driven item definitions
public class ItemDatabase
{
    public Dictionary<string, ItemDefinition> Items { get; set; }
}

public class ItemDefinition
{
    public string Id { get; set; }
    public string Name { get; set; }
    public ItemType Type { get; set; }
    public int Value { get; set; }
    public Dictionary<string, int> Stats { get; set; } // "Strength": +5
}
```

Load from JSON:
```json
{
  "items": {
    "sword_iron": {
      "name": "Iron Sword",
      "type": "Weapon",
      "value": 50,
      "stats": { "Strength": 5 }
    }
  }
}
```

**B. Enemy Templates**
```json
{
  "enemies": {
    "goblin": {
      "name": "Goblin",
      "hp": 20,
      "strength": 5,
      "defense": 2,
      "xp": 10,
      "lootTable": ["gold_small", "health_potion"]
    }
  }
}
```

**C. Progression System**
```csharp
public class LevelSystem
{
    public int CalculateRequiredXP(int level) => level * 100;

    public void LevelUp(ref Stats stats)
    {
        stats.MaxHP += 10;
        stats.Strength += 2;
        stats.Defense += 1;
    }
}
```

---

## Phase 6: Save/Load & Persistence (Week 11)

### 6.1 Save System

**Goal:** Serialize ECS world state to disk

**Approach:**
- Save entity components as JSON
- Use MessagePack or Protobuf for performance
- Support multiple save slots

```csharp
// WingedBean.Plugins.SaveSystem/SaveService.cs
public interface ISaveService
{
    Task SaveGameAsync(string slotName, IECSService ecs);
    Task<bool> LoadGameAsync(string slotName, IECSService ecs);
    List<SaveSlot> GetSaveSlots();
}
```

**Arch Serialization:**
```csharp
public class ArchSaveSerializer
{
    public SaveData Serialize(World world)
    {
        var data = new SaveData();

        // Query all entities
        var query = world.Query(in new QueryDescription().WithAll<Position, Stats>());

        query.ForEach((Entity entity, ref Position pos, ref Stats stats) =>
        {
            data.Entities.Add(new SavedEntity
            {
                Id = entity.Id,
                Components = new object[] { pos, stats }
            });
        });

        return data;
    }
}
```

---

## Recommended Project Structure (Final)

```
development/dotnet/
├── framework/
│   ├── src/
│   │   ├── WingedBean.Contracts.Core/           # netstandard2.1
│   │   ├── WingedBean.Contracts.Config/         # netstandard2.1
│   │   ├── WingedBean.Contracts.ECS/            # netstandard2.1 (NEW)
│   │   ├── WingedBean.Contracts.Dungeon/        # netstandard2.1 (NEW)
│   │   ├── WingedBean.Contracts.SaveSystem/     # netstandard2.1 (NEW)
│   │   ├── WingedBean.Contracts.SourceGen/      # netstandard2.0 (NEW)
│   │   └── WingedBean.Registry/                 # netstandard2.1
│   └── tests/
│
├── console/
│   ├── src/
│   │   ├── host/
│   │   │   └── ConsoleDungeon.Host/             # net8.0 - Entry point
│   │   ├── shared/
│   │   │   └── WingedBean.PluginLoader/         # net8.0
│   │   ├── providers/
│   │   │   └── WingedBean.Providers.AssemblyContext/ # net8.0
│   │   ├── plugins/
│   │   │   ├── WingedBean.Plugins.Config/       # net8.0
│   │   │   ├── WingedBean.Plugins.WebSocket/    # net8.0
│   │   │   ├── WingedBean.Plugins.TerminalUI/   # net8.0
│   │   │   ├── WingedBean.Plugins.ArchECS/      # net8.0 (NEW)
│   │   │   ├── WingedBean.Plugins.DungeonGen/   # net8.0 (NEW)
│   │   │   └── WingedBean.Plugins.SaveSystem/   # net8.0 (NEW)
│   │   └── game/
│   │       └── ConsoleDungeon/                  # net8.0 - Game logic
│   │           ├── DungeonGame.cs               # Main game class
│   │           ├── Systems/                     # Game-specific systems
│   │           ├── Components/                  # Game-specific components
│   │           └── Data/
│   │               ├── items.json
│   │               ├── enemies.json
│   │               └── dungeons.json
│   └── tests/
```

---

## Implementation Priority Matrix

| Phase | Priority | Effort | Impact | Dependencies |
|-------|----------|--------|--------|--------------|
| 1.1 Framework Compliance | CRITICAL | 2 days | High | None |
| 1.2 Dynamic Plugin Loading | CRITICAL | 3 days | High | 1.1 |
| 2.1 Arch ECS Integration | HIGH | 3 days | High | 1.1, 1.2 |
| 2.2 Game Components | HIGH | 2 days | Medium | 2.1 |
| 2.3 Core Systems | HIGH | 4 days | High | 2.1, 2.2 |
| 2.4 Game Loop Integration | HIGH | 2 days | High | 2.3 |
| 3.1 Dungeon Generation | MEDIUM | 5 days | High | 2.4 |
| 4.1 PTY Integration | MEDIUM | 3 days | Medium | 2.4 |
| 5.1 Game Rules | LOW | 7 days | High | 3.1 |
| 6.1 Save System | LOW | 4 days | Medium | 5.1 |

---

## Success Criteria

### Phase 1 Complete When:
- ✅ All Tier 1 contracts use `netstandard2.1`
- ✅ All Tier 3/4 use `net8.0`
- ✅ Plugins load dynamically from `plugins.json`
- ✅ Source generator project exists

### Phase 2 Complete When:
- ✅ Arch ECS plugin loads successfully
- ✅ Player entity can move on screen
- ✅ Enemy entities exist and render
- ✅ Combat system resolves damage
- ✅ 60 FPS maintained with 1000+ entities

### Phase 3 Complete When:
- ✅ Dungeon generates procedurally
- ✅ Rooms connected by corridors
- ✅ Player can navigate entire dungeon
- ✅ Multiple floors supported

### Phase 4 Complete When:
- ✅ Game runs in both standalone and PTY modes
- ✅ xterm.js displays game correctly
- ✅ Input works from both modes
- ✅ No rendering artifacts

### Phase 5 Complete When:
- ✅ 10+ item types exist
- ✅ 5+ enemy types exist
- ✅ Combat feels balanced
- ✅ Progression system works
- ✅ Win/lose conditions exist

### Phase 6 Complete When:
- ✅ Game state saves to disk
- ✅ Game state loads correctly
- ✅ Multiple save slots work
- ✅ No data loss on crash

---

## Next Immediate Actions (Week 1)

### Day 1-2: Framework Fixes
1. Update all contract projects to `netstandard2.1`
2. Update Tier 2 Registry to `netstandard2.1`
3. Update Tier 3/4 console projects to `net8.0`
4. Create source generator project skeleton

### Day 3-4: Dynamic Loading
1. Create `plugins.json` configuration
2. Remove static plugin references from Host
3. Implement plugin discovery and loading
4. Test plugin loading from config

### Day 5: Verify & Document
1. Build and test all projects
2. Verify tier dependency rules
3. Update architecture documentation
4. Create migration notes

---

## Resources & References

**Arch ECS:**
- GitHub: https://github.com/genaray/Arch
- Wiki: https://github.com/genaray/Arch/wiki
- Performance: https://github.com/genaray/Arch/wiki/Benchmarks

**Dungeon Generation:**
- BSP Tutorial: http://www.roguebasin.com/index.php?title=Basic_BSP_Dungeon_generation
- Cellular Automata: http://www.roguebasin.com/index.php?title=Cellular_Automata_Method_for_Generating_Random_Cave-Like_Levels

**Terminal.Gui v2:**
- Docs: https://gui-cs.github.io/Terminal.Gui/
- Examples: https://github.com/gui-cs/Terminal.Gui/tree/v2_develop/UICatalog

**Roguelike Development:**
- RogueBasin: http://www.roguebasin.com/
- /r/roguelikedev: https://www.reddit.com/r/roguelikedev/

---

**Status:** Ready for Implementation
**Next Review:** After Phase 1 completion
**Author:** Architecture Analysis + Enhancement Roadmap
**Date:** 2025-10-01

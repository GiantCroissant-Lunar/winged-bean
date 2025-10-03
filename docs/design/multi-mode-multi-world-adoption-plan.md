# Multi-Mode and Multi-World Adoption Plan for Winged Bean

**Date**: 2025-10-02  
**Status**: Planning (Revised after architecture review)  
**References**:
- craft-sim: `ref-projects/craft-sim/projects/craft-sim/docs/Design-PlayEdit-Modes.md`
- craft-sim: `ref-projects/craft-sim/projects/craft-sim/docs/Unity-Profile.md`
- winged-bean: `docs/rfcs/0002-service-platform-core-4-tier-architecture.md`
- winged-bean: `docs/rfcs/0003-plugin-architecture-foundation.md`
- winged-bean: `docs/rfcs/0005-target-framework-compliance.md`
- winged-bean: `docs/rfcs/0006-dynamic-plugin-loading.md`
- winged-bean: `docs/rfcs/0007-arch-ecs-integration.md`
- winged-bean: `docs/rfcs/0014-reactive-plugin-architecture-for-dungeon-game.md`

---

## Executive Summary

**winged-bean** is a **4-tier service-oriented platform** with plugin architecture and multi-profile support. We should adopt craft-sim's **multi-mode** (Play/EditOverlay/EditPaused) and **multi-world** (Authoring → Runtime pipeline) concepts by **extending the existing IECSService** interface, NOT by replacing the service architecture. The multi-world system integrates as an ECS plugin feature within the established service platform.

## Vision Confirmed (from Chat History)

From the 2025-10-02 conversation:

> **Engine Strategy**: Yes - Console, Unity, Godot, custom engines
> **Editor Vision**: In-game editor  
> **Multi-World Use Cases**: Yes - especially authoring → runtime pipeline  
> **Approach**: winged-bean is a service platform with tiered architecture, starting with concrete dungeon crawler TUI console app

---

## Architecture Understanding (Critical Context)

### winged-bean IS NOT craft-sim

**craft-sim**: Engine-first architecture
- Core abstraction: Spaces (authoring) → Worlds (runtime)
- Profile = IEngineProfile abstraction
- Package manager with JSON manifests
- Emulates Unity workflows

**winged-bean**: Service-first platform
- Core abstraction: 4-tier service platform (Contracts → Façades → Adapters → Providers)
- Profile = Tier 3/4 separation (Console adapters/providers, Unity adapters/providers)
- Plugin system with [Plugin] attributes and .plugin.json
- Multi-profile service orchestration

### Key Insight: Integration, Not Replacement

Multi-world and mode systems should **extend IECSService**, not replace the service architecture.

```
Service Layer (Cross-Cutting)          ECS Layer (Gameplay)
- IConfigService                       - IECSService ← Extend with multi-world
- IWebSocketService                      ↓
- ITerminalUIService                   - Multi-world support
- IRecorder                            - Mode service
                                       - Bake pipeline
```

### What winged-bean HAS ✅

1. **4-Tier Service Architecture** (RFC-0002)
   - **Tier 1**: Contracts (netstandard2.1, profile-agnostic interfaces)
   - **Tier 2**: Source-Generated Façades ([RealizeService] attribute)
   - **Tier 3**: Profile-Aware Adapters (Resilience, LoadContext, Telemetry, Schedulers)
   - **Tier 4**: Profile-Specific Providers (Terminal.Gui+node-pty, Unity APIs, Godot APIs)

2. **Multi-Profile Support** (RFC-0005)
   - Console: .NET 8.0 (LTS)
   - Unity: .NET Standard 2.1 (Unity 2021.2+)
   - Godot: .NET Standard 2.1 (Godot 4.0+)
   - Clear target framework strategy

3. **Plugin Architecture** (RFC-0003, RFC-0006)
   - **Everything is a plugin** (except minimal host)
   - Dynamic loading via `IPluginLoader` and `AssemblyLoadContext`
   - Plugin manifests (`.plugin.json`)
   - [Plugin] attribute for metadata
   - Hot-swap ready (ALC, HybridCLR)

4. **ECS Integration** (RFC-0007)
   - `IECSService` abstraction (Tier 1 contract)
   - `ArchECSService` implementation (Tier 3 plugin)
   - Service layer for cross-cutting concerns
   - ECS layer for entity behaviors
   - **Single runtime world** (current)

5. **Reactive Patterns** (RFC-0017)
   - System.Reactive (Rx.NET)
   - ReactiveUI for UI state
   - MessagePipe for pub/sub
   - ObservableCollections

### What winged-bean LACKS ❌

1. **Multi-World Architecture**
   - No authoring/runtime separation
   - No bake/build pipeline  
   - No stable ID mapping (AuthoringNode ↔ RuntimeEntity)
   - IECSService exposes single world only

2. **Multi-Mode System**
   - No Play/EditOverlay/EditPaused modes
   - No mode service to gate system execution
   - No editor overlay concept

3. **Authoring Data Model**
   - No stable IDs for authoring entities
   - No authoring → runtime transformation
   - Components created directly at runtime

**NOTE**: We DO have profile abstraction (Tier 3/4), we DO have plugin manifests (.plugin.json). We should NOT create IEngineProfile or separate package manifests.

---

## craft-sim Architecture (Key Concepts)

### 1. Multi-World Model

```
┌─────────────────────────────────┐
│     Authoring World             │
│  - Stable IDs (SpaceNodeId)     │
│  - Serializable (Spaces/Assets) │
│  - Source of truth              │
└─────────────────────────────────┘
            ↓ Bake/Build Pipeline
┌─────────────────────────────────┐
│    Runtime World(s)             │
│  - Compiled from authoring      │
│  - Ephemeral (gameplay ECS)     │
│  - Multiple instances possible  │
└─────────────────────────────────┘
```

**Benefits**:
- **Clean save/load**: Persist only authoring data
- **Determinism**: Rebuild runtime from authoring for clean resets
- **Multiple sandboxes**: A/B testing, different seeds, network clients
- **Undo/redo**: Track changes on authoring; runtime is disposable

### 2. Play/Edit Modes

**Mode Taxonomy**:

1. **Play**
   - Runtime simulation ticking
   - Minimal/no editor systems
   - Gameplay-focused

2. **EditOverlay** (In-Game Editor)
   - Runtime continues ticking
   - Editor panels active (Hierarchy, Inspector, Console)
   - Live-edit policies for light changes (transforms, parameters)

3. **EditPaused**
   - Simulation halted
   - Editor systems active
   - Structural edits allowed (hierarchy, prefab swap)
   - Re-bake on resume

**System Groups**:
- **RuntimeSystems**: Run in Play or EditOverlay
- **EditorSystems**: Run in EditOverlay and EditPaused
- **ModeService**: Gates execution based on current mode

### 3. Bake/Build Pipeline

```csharp
interface IRuntimeBuilder
{
    void Build(
        World authoring,      // Input: authoring world
        World runtime,        // Output: runtime world
        AssetDatabase assets, // Asset references
        Relations relations,  // Node → Asset relations
        Mapping mapping       // Bidirectional ID mapping
    );
}
```

**Policies**:
- **Live-apply** (EditOverlay): Small changes update both authoring and runtime
- **Structural changes** (EditPaused): Queue rebuild, re-bake on resume

### 4. Engine Profile Abstraction

```json
{
  "name": "com.example.rendering.basic",
  "version": "0.1.0",
  "dependencies": { "com.example.core": ">=0.1.0" },
  "extensionPoints": {
    "systems": {
      "runtime": ["RenderingSystem"],
      "editor": ["GizmoSystem"]
    },
    "panels": ["InspectorPanel"],
    "importers": [
      { "ext": ".mat", "type": "MaterialImporter" }
    ],
    "buildSteps": {
      "pre": ["ValidateMaterialsStep"],
      "transform": ["MaterialBindingStep"]
    }
  }
}
```

**Key Components**:
- **Relation Kinds**: `prefab-of`, `uses-material`, `uses-mesh`, `uses-texture`
- **Asset Conventions**: Assets folder, .meta files with GUID/importer settings
- **BuildPipeline Profile**: Authoring → runtime transformation steps
- **WorldHost Preset**: Editor/runtime system groups, play controls
- **Package Manager**: Extensible plugin system with manifest schema

---

## Adoption Roadmap

### Phase 1: Extend IECSService (Immediate) 🎯

**Goal**: Add multi-world and mode support to existing IECSService WITHOUT breaking changes

**Per R-CODE-010**: Prefer editing existing files over creating new ones. We extend `IECSService`, not create new abstractions.

#### 1.1 Extend ECS Contracts (Tier 1)

**Update**: `framework/src/WingedBean.Contracts.ECS/IECSService.cs`

```csharp
namespace WingedBean.Contracts.ECS;

// NEW: Mode enumeration
public enum GameMode
{
    Play,
    EditOverlay,
    EditPaused
}

// NEW: Authoring node ID (stable across rebuilds)
public readonly struct AuthoringNodeId : IEquatable<AuthoringNodeId>
{
    public Guid Value { get; }
    public AuthoringNodeId(Guid value) => Value = value;
    public static AuthoringNodeId NewId() => new(Guid.NewGuid());
    // ... IEquatable implementation
}

// NEW: World handle (opaque reference to Arch world)
public readonly struct WorldHandle : IEquatable<WorldHandle>
{
    internal readonly int Id;
    internal WorldHandle(int id) => Id = id;
    // ... IEquatable implementation
}

// EXISTING: IECSService (extend with multi-world API)
public interface IECSService
{
    // ===== EXISTING API (backward compatible) =====
    EntityHandle CreateEntity();
    EntityHandle[] CreateEntities(int count);
    void AddComponent<T>(EntityHandle entity, T component) where T : struct;
    ref T GetComponent<T>(EntityHandle entity) where T : struct;
    bool HasComponent<T>(EntityHandle entity) where T : struct;
    void RemoveComponent<T>(EntityHandle entity) where T : struct;
    void DestroyEntity(EntityHandle entity);
    bool IsAlive(EntityHandle entity);
    IEnumerable<EntityHandle> Query<T1>() where T1 : struct;
    IEnumerable<EntityHandle> Query<T1, T2>() where T1 : struct where T2 : struct;
    void Update(float deltaTime);
    void RegisterSystem(IECSSystem system, int priority = 0);
    void UnregisterSystem(IECSSystem system);
    int EntityCount { get; }
    
    // ===== NEW: Multi-World API =====
    
    /// <summary>
    /// Get the default authoring world. Always exists.
    /// </summary>
    WorldHandle AuthoringWorld { get; }
    
    /// <summary>
    /// Create a new runtime world with the given name.
    /// </summary>
    WorldHandle CreateRuntimeWorld(string name);
    
    /// <summary>
    /// Destroy a runtime world and all its entities.
    /// </summary>
    void DestroyWorld(WorldHandle world);
    
    /// <summary>
    /// Get all runtime worlds.
    /// </summary>
    IReadOnlyList<WorldHandle> GetRuntimeWorlds();
    
    /// <summary>
    /// Create entity in specific world (authoring or runtime).
    /// </summary>
    EntityHandle CreateEntity(WorldHandle world);
    
    /// <summary>
    /// Get which world an entity belongs to.
    /// </summary>
    WorldHandle GetEntityWorld(EntityHandle entity);
    
    // ===== NEW: Mode Service Integration =====
    
    /// <summary>
    /// Current game mode (Play/EditOverlay/EditPaused).
    /// </summary>
    GameMode CurrentMode { get; }
    
    /// <summary>
    /// Change game mode. Fires ModeChanged event.
    /// </summary>
    void SetMode(GameMode mode);
    
    /// <summary>
    /// Raised when mode changes.
    /// </summary>
    event EventHandler<GameMode>? ModeChanged;
    
    // ===== NEW: Authoring → Runtime Pipeline =====
    
    /// <summary>
    /// Map authoring node to runtime entity (for editor selection, inspection).
    /// </summary>
    void MapAuthoringToRuntime(AuthoringNodeId authoringId, EntityHandle runtimeEntity);
    
    /// <summary>
    /// Get runtime entity for an authoring node.
    /// </summary>
    EntityHandle? GetRuntimeEntity(AuthoringNodeId authoringId);
    
    /// <summary>
    /// Get authoring node for a runtime entity.
    /// </summary>
    AuthoringNodeId? GetAuthoringNode(EntityHandle runtimeEntity);
    
    /// <summary>
    /// Clear all authoring→runtime mappings.
    /// </summary>
    void ClearMappings();
    
    // ===== NEW: System Gating by Mode =====
    
    /// <summary>
    /// Query if a system should run in current mode.
    /// Override in system to customize behavior.
    /// </summary>
    bool ShouldSystemRun(IECSSystem system);
}

// EXTEND: IECSSystem with mode awareness
public interface IECSSystem
{
    void Execute(IECSService ecs, float deltaTime);
    
    /// <summary>
    /// Override to control when system runs (default: Run in all modes).
    /// </summary>
    bool ShouldRun(GameMode mode) => true;
}
```

**Action Items**:
- [ ] Update `WingedBean.Contracts.ECS/IECSService.cs` with new API
- [ ] Add `GameMode`, `AuthoringNodeId`, `WorldHandle` types
- [ ] Add mode service properties and events
- [ ] Add multi-world methods
- [ ] Add authoring mapping methods
- [ ] **NO new files created** - extend existing contract

#### 1.2 Implement in Arch ECS Plugin (Tier 3)

**Update**: `console/src/plugins/WingedBean.Plugins.ArchECS/ArchECSService.cs`

```csharp
using Arch.Core;
using WingedBean.Contracts.ECS;

namespace WingedBean.Plugins.ArchECS;

[Plugin(
    Name = "Arch.ECS",
    Provides = new[] { typeof(IECSService) },
    Priority = 100
)]
public class ArchECSService : IECSService
{
    // Existing single-world (now treated as default runtime world)
    private readonly World _defaultWorld;
    
    // NEW: Authoring world (always exists)
    private readonly World _authoringWorld;
    
    // NEW: Additional runtime worlds
    private readonly Dictionary<int, World> _runtimeWorlds = new();
    private int _nextWorldId = 1;
    
    // NEW: Mode service
    private GameMode _currentMode = GameMode.Play;
    
    // NEW: Authoring→Runtime mapping
    private readonly Dictionary<AuthoringNodeId, EntityHandle> _authoringToRuntime = new();
    private readonly Dictionary<EntityHandle, AuthoringNodeId> _runtimeToAuthoring = new();
    
    private readonly List<SystemEntry> _systems = new();
    private record SystemEntry(IECSSystem System, int Priority);
    
    public ArchECSService()
    {
        // Create authoring world (stable, persistent)
        _authoringWorld = World.Create();
        
        // Create default runtime world (ephemeral, rebuilt from authoring)
        _defaultWorld = World.Create();
        _runtimeWorlds[0] = _defaultWorld;
    }
    
    // ===== EXISTING API (uses default world for backward compatibility) =====
    
    public EntityHandle CreateEntity()
    {
        return CreateEntity(new WorldHandle(0)); // Use default runtime world
    }
    
    // ... other existing methods delegate to default world
    
    // ===== NEW: Multi-World API =====
    
    public WorldHandle AuthoringWorld => new WorldHandle(-1);
    
    public WorldHandle CreateRuntimeWorld(string name)
    {
        var world = World.Create();
        var handle = new WorldHandle(_nextWorldId++);
        _runtimeWorlds[handle.Id] = world;
        return handle;
    }
    
    public void DestroyWorld(WorldHandle worldHandle)
    {
        if (worldHandle.Id == -1)
            throw new InvalidOperationException("Cannot destroy authoring world");
        if (worldHandle.Id == 0)
            throw new InvalidOperationException("Cannot destroy default world");
            
        if (_runtimeWorlds.TryGetValue(worldHandle.Id, out var world))
        {
            world.Destroy();
            _runtimeWorlds.Remove(worldHandle.Id);
        }
    }
    
    public IReadOnlyList<WorldHandle> GetRuntimeWorlds()
    {
        return _runtimeWorlds.Keys.Select(id => new WorldHandle(id)).ToList();
    }
    
    public EntityHandle CreateEntity(WorldHandle worldHandle)
    {
        var world = GetWorld(worldHandle);
        var entity = world.Create();
        return new EntityHandle(entity.Id, world.Id);
    }
    
    public WorldHandle GetEntityWorld(EntityHandle entity)
    {
        return new WorldHandle(entity.WorldId);
    }
    
    // ===== NEW: Mode Service =====
    
    public GameMode CurrentMode
    {
        get => _currentMode;
    }
    
    public void SetMode(GameMode mode)
    {
        if (_currentMode == mode) return;
        
        var oldMode = _currentMode;
        _currentMode = mode;
        ModeChanged?.Invoke(this, mode);
        
        Console.WriteLine($"[ECS] Mode changed: {oldMode} → {mode}");
    }
    
    public event EventHandler<GameMode>? ModeChanged;
    
    // ===== NEW: Authoring Mapping =====
    
    public void MapAuthoringToRuntime(AuthoringNodeId authoringId, EntityHandle runtimeEntity)
    {
        _authoringToRuntime[authoringId] = runtimeEntity;
        _runtimeToAuthoring[runtimeEntity] = authoringId;
    }
    
    public EntityHandle? GetRuntimeEntity(AuthoringNodeId authoringId)
    {
        return _authoringToRuntime.TryGetValue(authoringId, out var entity) ? entity : null;
    }
    
    public AuthoringNodeId? GetAuthoringNode(EntityHandle runtimeEntity)
    {
        return _runtimeToAuthoring.TryGetValue(runtimeEntity, out var nodeId) ? nodeId : null;
    }
    
    public void ClearMappings()
    {
        _authoringToRuntime.Clear();
        _runtimeToAuthoring.Clear();
    }
    
    // ===== NEW: System Gating =====
    
    public bool ShouldSystemRun(IECSSystem system)
    {
        return system.ShouldRun(_currentMode);
    }
    
    public void Update(float deltaTime)
    {
        // Execute systems in priority order, respecting mode
        foreach (var entry in _systems.OrderByDescending(s => s.Priority))
        {
            if (ShouldSystemRun(entry.System))
            {
                entry.System.Execute(this, deltaTime);
            }
        }
    }
    
    // Helper to get Arch.Core.World from WorldHandle
    private World GetWorld(WorldHandle handle)
    {
        if (handle.Id == -1)
            return _authoringWorld;
        if (_runtimeWorlds.TryGetValue(handle.Id, out var world))
            return world;
        throw new InvalidOperationException($"World {handle.Id} not found");
    }
    
    // ... rest of existing implementation
}
```

**Action Items**:
- [ ] Update `ArchECSService.cs` to support multi-world
- [ ] Add authoring world creation
- [ ] Implement mode service properties
- [ ] Implement authoring mapping methods
- [ ] Update `Update()` to gate systems by mode
- [ ] **Maintain backward compatibility** with single-world API

---

### Phase 2: Documentation (Immediate) 📄

**Per R-DOC-050**: Only create docs when explicitly requested.

#### 2.1 Update Existing RFCs

- [ ] **Update RFC-0007**: Add multi-world and mode system sections
- [ ] Document backward compatibility strategy
- [ ] Add migration guide for single-world → multi-world

#### 2.2 Create Design Documents (if requested)

- [ ] `docs/design/multi-world-architecture.md` - Authoring/runtime separation patterns
- [ ] `docs/design/mode-system-integration.md` - Mode service integration with services
- [ ] `docs/design/profile-conventions.md` - Console/Unity/Godot conventions (NOT code interfaces)

**NOTE**: Do NOT create RFC-0017, RFC-0018, RFC-0019 yet. Wait until Phase 1 implementation validates the approach.

### Phase 2: ConsoleDungeon Prototype (Near-Term) 🎮

**Goal**: Validate multi-world architecture with concrete TUI dungeon crawler

#### 2.1 Implement Minimal Multi-World

**ConsoleDungeon.Host** changes:

```csharp
// Create authoring and runtime worlds
var worldHost = new WorldHost();
var authoring = worldHost.AuthoringWorld;
var runtime = worldHost.CreateRuntimeWorld("main");

// Initialize authoring data
var dungeonAuthoring = new DungeonAuthoringService();
dungeonAuthoring.CreateDungeon(authoring, width: 80, height: 24);

// Build runtime world
var builder = new ConsoleRuntimeBuilder();
var mapping = new IdMapping();
builder.Build(authoring, runtime, assetDb, mapping);

// Start game loop
var modeService = new ModeService();
modeService.SetMode(GameMode.Play);

while (running)
{
    if (modeService.CurrentMode == GameMode.Play)
    {
        // Tick runtime systems
        dungeonSystems.Update(runtime, deltaTime);
    }
    
    // Render based on mode
    renderer.Render(runtime, modeService.CurrentMode);
}
```

**Key Changes**:
- Separate authoring and runtime worlds
- Authoring data uses stable IDs (`AuthoringNodeId`)
- Runtime world built from authoring via builder
- Mode service gates system execution

#### 2.2 Implement Mode Switching

**Keyboard Shortcuts** (Terminal.Gui):

- `Tab` or `~`: Toggle EditOverlay (show/hide editor panels)
- `Ctrl+P`: Pause/Resume (Play ↔ EditPaused)
- `Ctrl+S`: Save authoring data
- `Ctrl+R`: Rebuild runtime from authoring

**Editor Panels** (EditOverlay mode):

```
┌─────────────────────────────────────────────────────────────┐
│ Hierarchy         │ Game View                │ Inspector    │
│                   │                          │              │
│ - Dungeon         │   ####                   │ Selected:    │
│   - Player        │   #..#                   │ - Player     │
│   - Enemies (5)   │   #@E#    ← Runtime      │              │
│   - Items (3)     │   ####                   │ Position:    │
│                   │                          │   X: 5       │
│ [Authoring View]  │                          │   Y: 3       │
│                   │ [EditOverlay Active]     │              │
└─────────────────────────────────────────────────────────────┘
```

**Action Items**:
- [ ] Implement `WorldHost` with authoring/runtime separation
- [ ] Implement `ModeService` with Play/EditOverlay/EditPaused
- [ ] Create `ConsoleRuntimeBuilder` for dungeon baking
- [ ] Add keyboard shortcuts for mode switching
- [ ] Create minimal editor panels (Hierarchy, Inspector)
- [ ] Implement authoring data persistence (save/load)

### Phase 3: Unity Profile Implementation (Medium-Term) 🎨

**Goal**: Extend multi-world architecture to Unity profile

#### 3.1 Unity Profile Components

**Unity Profile Manifest** (`unity.profile.json`):

```json
{
  "name": "unity",
  "displayName": "Unity Profile",
  "version": "1.0.0",
  "conventions": {
    "assetFolder": "Assets/",
    "metaFileExtension": ".meta",
    "relationKinds": [
      "prefab-of",
      "uses-material",
      "uses-mesh",
      "uses-texture"
    ]
  },
  "buildPipeline": {
    "steps": [
      "CollectSceneRoots",
      "ResolvePrefabs",
      "MapComponents",
      "RegisterSystems"
    ]
  },
  "worldHost": {
    "systemGroups": {
      "runtime": ["PhysicsSystem", "RenderingSystem"],
      "editor": ["GizmoSystem", "InspectorSystem"]
    }
  }
}
```

**Unity-Specific Systems**:

```csharp
// WingedBean.Host.Unity/Systems/UnityRuntimeBuilder.cs
public class UnityRuntimeBuilder : IRuntimeBuilder
{
    public void Build(World authoring, World runtime, IAssetDatabase assets, IIdMapping mapping)
    {
        // 1. Collect scene roots
        var roots = authoring.Query<AuthoringNode, Transform>();
        
        // 2. Resolve prefabs and relations
        var prefabs = ResolvePrefabs(roots, assets);
        
        // 3. Map components (Transform → UnityEngine.Transform)
        MapComponents(roots, runtime, mapping);
        
        // 4. Register Unity-specific systems
        RegisterRenderingSystems(runtime);
        RegisterPhysicsSystems(runtime);
    }
}
```

**Action Items**:
- [ ] Create `unity.profile.json` specification
- [ ] Implement `UnityEngineProfile`
- [ ] Implement `UnityRuntimeBuilder`
- [ ] Create Unity editor panels integration
- [ ] Test multi-world with Unity scene authoring

### Phase 4: Multi-Profile Support (Long-Term) 🚀

**Goal**: Support multiple engine profiles (Unity, Godot, Console, Custom)

#### 4.1 Profile Registry

```csharp
// WingedBean.Contracts.Core/Engine/IProfileRegistry.cs
public interface IProfileRegistry
{
    void Register(IEngineProfile profile);
    IEngineProfile? GetProfile(string name);
    IReadOnlyList<IEngineProfile> GetAllProfiles();
}

// Usage in host
var registry = new ProfileRegistry();
registry.Register(new ConsoleProfile());
registry.Register(new UnityProfile());
registry.Register(new GodotProfile());

var profile = registry.GetProfile("console");
var builder = profile.BuildPipeline.CreateBuilder();
```

#### 4.2 Godot Profile

**Godot Profile Manifest** (`godot.profile.json`):

```json
{
  "name": "godot",
  "displayName": "Godot Profile",
  "conventions": {
    "sceneExtension": ".tscn",
    "resourceExtension": ".tres",
    "relationKinds": [
      "instance-of",
      "uses-material",
      "uses-mesh"
    ]
  }
}
```

**Action Items**:
- [ ] Create `IProfileRegistry` abstraction
- [ ] Implement profile discovery and loading
- [ ] Create Godot profile specification
- [ ] Implement `GodotRuntimeBuilder`
- [ ] Document multi-profile architecture

---

## Key Design Decisions

### 1. Authoring Data Model

**Stable IDs for authoring nodes**:

```csharp
public readonly struct AuthoringNodeId
{
    public Guid Value { get; }
    
    public static AuthoringNodeId NewId() => new(Guid.NewGuid());
}

public struct AuthoringNode
{
    public AuthoringNodeId Id;
    public string Name;
    public AuthoringNodeId? Parent;
}
```

**Benefits**:
- Survives runtime world rebuilds
- Enables save/load
- Supports prefab overrides
- Enables editor selection tracking

### 2. System Gating

**Mode-aware system execution**:

```csharp
public abstract class ManagedSystem
{
    public virtual bool ShouldRun(GameMode mode)
    {
        return mode == GameMode.Play;
    }
}

public class MovementSystem : ManagedSystem
{
    public override bool ShouldRun(GameMode mode)
    {
        // Run in Play and EditOverlay, not EditPaused
        return mode == GameMode.Play || mode == GameMode.EditOverlay;
    }
}

public class InspectorSystem : ManagedSystem
{
    public override bool ShouldRun(GameMode mode)
    {
        // Run in EditOverlay and EditPaused, not Play
        return mode != GameMode.Play;
    }
}
```

### 3. Live-Edit Policies

**Define which changes can be live-applied**:

```csharp
public interface ILiveEditPolicy
{
    bool CanLiveApply(ComponentType type);
}

public class ConsoleLiveEditPolicy : ILiveEditPolicy
{
    public bool CanLiveApply(ComponentType type)
    {
        // Allow live-editing position and simple properties
        return type == typeof(Position) ||
               type == typeof(Health) ||
               type == typeof(Name);
        
        // Structural changes (add/remove entities) require rebuild
    }
}
```

### 4. Mapping Strategies

**Bidirectional ID mapping**:

```csharp
public class IdMapping : IIdMapping
{
    private readonly Dictionary<AuthoringNodeId, Entity> _authoringToRuntime = new();
    private readonly Dictionary<Entity, AuthoringNodeId> _runtimeToAuthoring = new();
    
    public void Map(AuthoringNodeId nodeId, Entity entity)
    {
        _authoringToRuntime[nodeId] = entity;
        _runtimeToAuthoring[entity] = nodeId;
    }
    
    public Entity? GetRuntimeEntity(AuthoringNodeId nodeId)
    {
        return _authoringToRuntime.TryGetValue(nodeId, out var entity) 
            ? entity 
            : null;
    }
}
```

---

## Migration Strategy

### Gradual Adoption

**Current ConsoleDungeon** (RFC-0007):
- Single runtime world
- Direct component creation
- No authoring/runtime separation

**Step 1**: Add authoring world (non-breaking)
- Keep existing runtime world
- Create parallel authoring world
- Start populating authoring data

**Step 2**: Implement builder (feature flag)
- Add `IRuntimeBuilder` implementation
- Toggle between direct creation and baked creation
- Validate parity

**Step 3**: Enable multi-mode (opt-in)
- Add mode service
- Implement keyboard shortcuts
- Toggle editor overlay

**Step 4**: Deprecate direct creation
- Remove old code paths
- Fully commit to authoring → runtime pipeline

### Backward Compatibility

**Ensure existing code continues to work**:

```csharp
// Old way (still works)
var entity = world.Create<Position, Health>();

// New way (preferred)
var authoring = new AuthoringNode { Name = "Player" };
authoringWorld.Create(authoring);
builder.Build(authoringWorld, runtimeWorld, assetDb, mapping);
```

---

## Success Criteria

### Phase 1 (Foundation)
- [ ] Core abstractions defined and documented
- [ ] RFCs approved and merged
- [ ] No breaking changes to existing code

### Phase 2 (ConsoleDungeon)
- [ ] Authoring and runtime worlds functional
- [ ] Mode switching works (Play/EditOverlay/EditPaused)
- [ ] Save/load authoring data
- [ ] Editor panels show authoring state
- [ ] Runtime rebuilds from authoring without data loss

### Phase 3 (Unity Profile)
- [ ] Unity profile manifest defined
- [ ] UnityRuntimeBuilder implemented
- [ ] Unity editor integration working
- [ ] Asset pipeline (prefabs/materials) supported

### Phase 4 (Multi-Profile)
- [ ] Profile registry working
- [ ] Multiple profiles loaded simultaneously
- [ ] Godot profile prototype
- [ ] Documentation complete

---

## Related Documents

### Existing RFCs
- RFC-0002: Service Platform Core 4-Tier Architecture
- RFC-0003: Plugin Architecture Foundation
- RFC-0004: Project Organization and Folder Structure
- RFC-0006: Dynamic Plugin Loading
- RFC-0007: Arch ECS Integration
- RFC-0017: Reactive Plugin Architecture for Dungeon Game

### New RFCs (To Create)
- RFC-0017: Multi-World Architecture (Authoring → Runtime)
- RFC-0018: Play/Edit Mode System
- RFC-0019: Engine Profile Abstraction (expanded)

### Design Documents
- docs/ARCHITECTURE.md (to create)
- docs/PROCESS.md (to create)
- docs/design/unity-profile-specification.md (to create)
- docs/design/console-profile-specification.md (to create)

### Reference Documents
- craft-sim: `docs/Design-PlayEdit-Modes.md`
- craft-sim: `docs/Unity-Profile.md`
- craft-sim: `docs/ARCHITECTURE.md`

---

## Open Questions

1. **Authoring Data Persistence Format**
   - JSON? Binary? Custom format?
   - How to handle large scenes efficiently?

2. **Multi-World Performance**
   - Cost of maintaining multiple Arch worlds?
   - Memory overhead of authoring + runtime?

3. **Asset Pipeline**
   - How to handle asset imports (textures, models)?
   - Unity AssetDatabase equivalent?

4. **Networking Implications**
   - How does multi-world work with multiplayer?
   - Client-side prediction with authoring/runtime separation?

5. **Hot-Reload Strategy**
   - Can we hot-reload systems without rebuilding worlds?
   - Live-reload C# code while in EditOverlay?

6. **Editor UX in Terminal.Gui**
   - How to fit editor panels in 80x24 terminal?
   - Keyboard shortcuts vs mouse interaction?

---

## Conclusion

Adopting craft-sim's multi-mode and multi-world architecture will transform winged-bean from a service-oriented framework into a true game development platform. The gradual adoption strategy ensures we can validate concepts with ConsoleDungeon before expanding to Unity and Godot profiles.

**Next Steps**:
1. Review this plan with stakeholders
2. Create RFC-0017, RFC-0018, RFC-0019
3. Begin Phase 1 implementation (core abstractions)
4. Prototype multi-world in ConsoleDungeon

---

**Version**: 1.0.0  
**Author**: GitHub Copilot (via chat analysis)  
**Date**: 2025-10-02

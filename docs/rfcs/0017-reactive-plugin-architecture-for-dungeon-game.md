---
id: RFC-0017
title: Reactive Plugin Architecture for Dungeon Game
status: Proposed
category: architecture, plugins
created: 2025-10-03
updated: 2025-10-03
author: Claude Code
---

# RFC-0017: Reactive Plugin Architecture for Dungeon Game

## Summary

Refactor the ConsoleDungeon architecture to properly separate game logic and UI rendering into distinct plugins using reactive patterns (System.Reactive, ReactiveUI, MessagePipe, ObservableCollections). This establishes clean separation of concerns where game state changes flow reactively to UI updates, following the established WingedBean plugin architecture.

## Motivation

### Current Problems

The current ConsoleDungeon implementation violates several architectural principles:

1. **❌ Game logic in host project** (`src/host/ConsoleDungeon/`)
   - Game logic (DungeonGame, Systems, Components) should be in a plugin
   - Host projects should only orchestrate plugin loading
   - Violates R-CODE-050 (respect project structure)

2. **❌ Direct coupling between game and UI** (`Program.cs`)
   - `Program.RunAsync()` directly instantiates `DungeonGame` and renders it
   - UI code directly queries ECS world in render loops
   - No abstraction layer for game state access
   - Makes it impossible to swap UI implementations

3. **❌ Missing reactive patterns**
   - No observables for game state changes
   - UI polls game state instead of reacting to changes
   - No pub/sub for game events (combat, movement, etc.)
   - Difficult to add features like replays, networking, or alternative UIs

4. **❌ Wrong Terminal.Gui dependency placement**
   - `ConsoleDungeon.csproj` (game logic) depends on Terminal.Gui
   - Should only be in UI plugin
   - Created `DungeonView` in wrong project

### Why Reactive Patterns?

**Benefits of reactive architecture:**

1. **Separation of Concerns**: Game logic publishes events, UI subscribes
2. **Decoupling**: Game doesn't know about UI, UI doesn't modify game directly
3. **Testability**: Can test game logic without UI, UI without game
4. **Extensibility**: Easy to add new subscribers (networking, AI, logging)
5. **Performance**: Update only what changed, not entire screen
6. **State Management**: Single source of truth with observable streams

### Why These Libraries?

**System.Reactive (Rx.NET)**
- Industry-standard reactive extensions for .NET
- LINQ-style query operators for event streams
- Powerful composition and transformation

**ReactiveUI**
- MVVM framework built on Rx.NET
- Observable properties and collections
- Perfect for UI state management

**MessagePipe**
- High-performance pub/sub messaging
- Zero-allocation message passing
- Filter/intercept messages easily

**ObservableCollections (Cysharp)**
- High-performance observable collections
- Optimized for game scenarios
- Works seamlessly with ReactiveUI

## Proposal

### New Architecture

```
┌──────────────────────────────────────────────────────────────┐
│ ConsoleDungeon.Host (Minimal Orchestrator)                   │
│ - Loads plugins from plugins.json                            │
│ - Initializes Registry                                       │
│ - No game logic, no UI code                                  │
│ - Starts ITerminalApp from plugin                            │
└──────────────────────────────────────────────────────────────┘
                             │
                             │ loads plugins
         ┌───────────────────┴──────────────┐
         │                                  │
┌────────▼──────────────────┐   ┌──────────▼───────────────────┐
│ WingedBean.Plugins        │   │ WingedBean.Plugins           │
│ .DungeonGame              │   │ .DungeonUI                   │
│                           │   │                              │
│ Provides:                 │   │ Provides:                    │
│ - IDungeonGameService     │   │ - ITerminalApp               │
│                           │   │                              │
│ Contains:                 │   │ Contains:                    │
│ - DungeonGame class       │   │ - DungeonView (Terminal.Gui) │
│ - ECS Systems             │   │ - DungeonTerminalApp         │
│ - Components              │   │ - Input handling             │
│ - Game state logic        │   │                              │
│                           │   │ Depends on:                  │
│ Publishes (MessagePipe):  │   │ - IDungeonGameService        │
│ - EntityMovedEvent        │◄──┤                              │
│ - CombatEvent             │   │ Subscribes (MessagePipe):    │
│ - GameStateChangedEvent   │   │ - EntityMovedEvent           │
│ - EntitySpawnedEvent      │   │ - CombatEvent                │
│ - EntityDiedEvent         │   │ - GameStateChangedEvent      │
│                           │   │                              │
│ Exposes (ReactiveUI):     │   │ Uses (System.Reactive):      │
│ - IObservable<GameState>  │   │ - Subscribe to observables   │
│ - ObservableCollection    │   │ - Render on state change     │
│   <EntitySnapshot>        │   │ - Throttle/debounce updates  │
└───────────────────────────┘   └──────────────────────────────┘
```

### Plugin Breakdown

#### 1. WingedBean.Plugins.DungeonGame
**Purpose**: Pure game logic plugin (no UI dependencies)

**Provides:**
- `IDungeonGameService` contract

**Contains:**
- All game logic from `src/host/ConsoleDungeon/` moved here
- `DungeonGame` class
- ECS Systems (AISystem, MovementSystem, CombatSystem, RenderSystem)
- Components (Player, Enemy, Position, Stats, etc.)
- Game state management

**Dependencies:**
- `IECSService` (Arch ECS plugin)
- System.Reactive
- ReactiveUI
- MessagePipe
- ObservableCollections

**Publishes via MessagePipe:**
```csharp
// Movement events
public record EntityMovedEvent(EntityId Id, Position OldPos, Position NewPos);

// Combat events
public record CombatEvent(EntityId Attacker, EntityId Defender, int Damage, bool Killed);

// State events
public record GameStateChangedEvent(GameState OldState, GameState NewState);

// Entity lifecycle
public record EntitySpawnedEvent(EntityId Id, EntityType Type);
public record EntityDiedEvent(EntityId Id, EntityType Type);
```

**Exposes via ReactiveUI:**
```csharp
public interface IDungeonGameService
{
    // Lifecycle
    void Initialize();
    void Update(float deltaTime);

    // Input (commands, not direct state mutation)
    void HandleInput(GameInput input);

    // Reactive state
    IObservable<GameState> GameStateObservable { get; }
    IObservableCollection<EntitySnapshot> Entities { get; }
    IObservable<PlayerStats> PlayerStatsObservable { get; }

    // Direct queries (for non-reactive scenarios)
    IWorld World { get; }
    GameState CurrentState { get; }
}
```

#### 2. WingedBean.Plugins.DungeonUI
**Purpose**: Terminal.Gui rendering plugin

**Provides:**
- `ITerminalApp` implementation

**Contains:**
- `DungeonTerminalApp` (implements ITerminalApp)
- `DungeonView` (Terminal.Gui custom view)
- Input handling → converts to `GameInput` commands
- Rendering logic (subscribes to game state)

**Dependencies:**
- `IDungeonGameService`
- Terminal.Gui v2
- System.Reactive

**Example Implementation:**
```csharp
public class DungeonTerminalApp : ITerminalApp
{
    private readonly IDungeonGameService _gameService;
    private readonly IDisposable _subscriptions;

    public DungeonTerminalApp(IDungeonGameService gameService)
    {
        _gameService = gameService;

        // Subscribe to game state changes
        _subscriptions = new CompositeDisposable(
            _gameService.GameStateObservable
                .Subscribe(state => OnGameStateChanged(state)),

            _gameService.Entities
                .ObserveCollectionChanged()
                .Throttle(TimeSpan.FromMilliseconds(16)) // 60 FPS max
                .Subscribe(_ => RefreshView()),

            _gameService.PlayerStatsObservable
                .Subscribe(stats => UpdateStatsDisplay(stats))
        );
    }

    public async Task StartAsync(TerminalAppConfig config, CancellationToken ct)
    {
        Application.Init();

        var dungeonView = new DungeonView(_gameService);
        var statsView = new StatsView(_gameService);

        // Setup Terminal.Gui application
        // ...
    }
}
```

### New Contracts

#### WingedBean.Contracts.Game/IDungeonGameService.cs
```csharp
namespace WingedBean.Contracts.Game;

public interface IDungeonGameService
{
    // Lifecycle
    void Initialize();
    void Update(float deltaTime);
    void Shutdown();

    // Input commands (not direct state mutation)
    void HandleInput(GameInput input);

    // Reactive state (UI subscribes)
    IObservable<GameState> GameStateObservable { get; }
    IObservableCollection<EntitySnapshot> Entities { get; }
    IObservable<PlayerStats> PlayerStatsObservable { get; }

    // Direct queries (fallback for non-reactive scenarios)
    IWorld World { get; }
    GameState CurrentState { get; }
    PlayerStats CurrentPlayerStats { get; }
}

// Input commands
public record GameInput(InputType Type, object? Data = null);

public enum InputType
{
    MoveUp, MoveDown, MoveLeft, MoveRight,
    Attack, UseItem, Inventory, Quit
}

// Game state
public enum GameState
{
    NotStarted, Running, Paused, GameOver, Victory
}

// Snapshots (immutable views for UI)
public record EntitySnapshot(
    EntityId Id,
    Position Position,
    char Symbol,
    ConsoleColor ForegroundColor,
    ConsoleColor BackgroundColor,
    int RenderLayer
);

public record PlayerStats(
    int CurrentHP, int MaxHP,
    int CurrentMana, int MaxMana,
    int Level, int Experience,
    int Strength, int Dexterity, int Intelligence, int Defense
);
```

#### WingedBean.Contracts.Game/GameEvents.cs
```csharp
namespace WingedBean.Contracts.Game;

// MessagePipe events for pub/sub
public record EntityMovedEvent(EntityId Id, Position OldPos, Position NewPos);
public record CombatEvent(EntityId Attacker, EntityId Defender, int Damage, bool Killed);
public record GameStateChangedEvent(GameState OldState, GameState NewState);
public record EntitySpawnedEvent(EntityId Id, EntityType Type);
public record EntityDiedEvent(EntityId Id, EntityType Type);
public record ItemCollectedEvent(EntityId CollectorId, EntityId ItemId);
public record LevelUpEvent(EntityId EntityId, int NewLevel);
```

### Migration Plan

#### Phase 1: Create Contracts
- [ ] Create `WingedBean.Contracts.Game` project
- [ ] Define `IDungeonGameService` interface
- [ ] Define `GameEvents.cs` (MessagePipe events)
- [ ] Define DTOs (GameInput, EntitySnapshot, PlayerStats)

#### Phase 2: Create DungeonGame Plugin
- [ ] Create `WingedBean.Plugins.DungeonGame` project
- [ ] Add dependencies: System.Reactive, ReactiveUI, MessagePipe, ObservableCollections
- [ ] Move game logic from `src/host/ConsoleDungeon/` to plugin
- [ ] Implement `IDungeonGameService`
- [ ] Add reactive state (observables, collections)
- [ ] Implement MessagePipe publishers

#### Phase 3: Refactor DungeonUI Plugin
- [ ] Rename `WingedBean.Plugins.ConsoleDungeon` → `WingedBean.Plugins.DungeonUI`
- [ ] Add Terminal.Gui v2 dependency
- [ ] Move `DungeonView` from host to plugin
- [ ] Implement reactive subscriptions
- [ ] Convert input handling to command pattern
- [ ] Update `ITerminalApp` implementation

#### Phase 4: Update Host
- [ ] Remove all game logic from `src/host/ConsoleDungeon/`
- [ ] Remove Terminal.Gui dependency from ConsoleDungeon.csproj
- [ ] Convert to pure library (if needed) or remove entirely
- [ ] Update `ConsoleDungeon.Host/plugins.json`
- [ ] Verify plugin loading order

#### Phase 5: Testing
- [ ] Unit tests for game logic (without UI)
- [ ] Integration tests for reactive subscriptions
- [ ] Verify MessagePipe events flow correctly
- [ ] Test UI updates on state changes

### Dependencies to Add

**NuGet Packages:**
```xml
<!-- WingedBean.Contracts.Game -->
<PackageReference Include="System.Reactive" Version="6.0.0" />

<!-- WingedBean.Plugins.DungeonGame -->
<PackageReference Include="System.Reactive" Version="6.0.0" />
<PackageReference Include="ReactiveUI" Version="20.1.1" />
<PackageReference Include="MessagePipe" Version="1.8.1" />
<PackageReference Include="ObservableCollections" Version="3.1.0" />

<!-- WingedBean.Plugins.DungeonUI -->
<PackageReference Include="System.Reactive" Version="6.0.0" />
<PackageReference Include="Terminal.Gui" Version="1.17.1" />
```

### Example: Reactive Flow

**Game publishes event:**
```csharp
// In MovementSystem.Execute()
if (moved)
{
    _publisher.Publish(new EntityMovedEvent(entity, oldPos, newPos));
    _entitiesSubject.OnNext(CreateSnapshot(entity)); // Update observable
}
```

**UI subscribes and renders:**
```csharp
// In DungeonView constructor
_gameService.Entities
    .ObserveCollectionChanged()
    .Throttle(TimeSpan.FromMilliseconds(16)) // 60 FPS
    .ObserveOn(RxApp.MainThreadScheduler)
    .Subscribe(_ => {
        SetNeedsDisplay(); // Trigger Terminal.Gui redraw
    });
```

## Benefits

### Architectural Benefits
1. ✅ **Proper separation of concerns** (R-PRC-010)
2. ✅ **Plugin-based game logic** (follows established patterns)
3. ✅ **Reactive state management** (industry best practice)
4. ✅ **Testable in isolation** (game logic without UI)
5. ✅ **Extensible** (easy to add new UIs, networking, AI)

### Performance Benefits
1. ✅ **Update only changed entities** (not full screen redraws)
2. ✅ **Throttle/debounce updates** (control render frequency)
3. ✅ **Zero-allocation pub/sub** (MessagePipe optimization)
4. ✅ **Efficient collection updates** (ObservableCollections)

### Developer Experience
1. ✅ **Hot-reloadable plugins** (game + UI separately)
2. ✅ **Easy to add features** (subscribe to new events)
3. ✅ **LINQ-style event queries** (Rx operators)
4. ✅ **Type-safe messaging** (MessagePipe)

## Alternatives Considered

### Alternative 1: Keep current architecture, just move code
**Rejected** - Doesn't solve coupling issues, misses reactive benefits

### Alternative 2: Use only MessagePipe (no ReactiveUI)
**Rejected** - Misses state management benefits, observable collections

### Alternative 3: Use only ReactiveUI (no MessagePipe)
**Rejected** - Rx event handling less efficient than MessagePipe pub/sub

### Alternative 4: Custom event system
**Rejected** - Reinventing the wheel, worse performance

## Migration Strategy

### Breaking Changes
- `src/host/ConsoleDungeon/Program.cs` will be removed/refactored
- Current direct game instantiation will be replaced with plugin loading
- UI code must use `IDungeonGameService` instead of direct `DungeonGame` access

### Backwards Compatibility
- Host project still loads plugins the same way
- `ITerminalApp` contract unchanged
- ECS systems internally unchanged (just wrapped in reactive layer)

### Rollout Plan
1. Create contracts (non-breaking addition)
2. Create game plugin (parallel to existing code)
3. Create UI plugin (refactor existing plugin)
4. Update host to use new plugins
5. Remove old code once verified

## Open Questions

1. **Should we make RenderSystem reactive too?**
   - Currently it's an ECS system
   - Could convert to pure subscriber of movement/spawn events
   - **Decision needed**: Keep as system or convert to subscriber?

2. **How to handle save/load with reactive state?**
   - Serialize ECS world (already planned)
   - Replay events from MessagePipe?
   - **Decision needed**: Snapshot or event sourcing?

3. **Should DungeonGame plugin expose IECSWorld directly?**
   - Currently planned: `IDungeonGameService.World` exposes it
   - Alternative: Hide ECS completely behind service methods
   - **Decision needed**: Expose or hide ECS?

4. **MessagePipe scope: global or per-plugin?**
   - Global: All plugins share message bus
   - Per-plugin: Isolated messaging
   - **Decision needed**: Scope strategy?

## References

- [RFC-0003: Plugin Architecture Foundation](./0003-plugin-architecture-foundation.md)
- [RFC-0006: Dynamic Plugin Loading](./0006-dynamic-plugin-loading.md)
- [RFC-0007: Arch ECS Integration](./0007-arch-ecs-integration.md)
- [System.Reactive Documentation](https://github.com/dotnet/reactive)
- [ReactiveUI Documentation](https://www.reactiveui.net/)
- [MessagePipe Documentation](https://github.com/Cysharp/MessagePipe)
- [ObservableCollections Documentation](https://github.com/Cysharp/ObservableCollections)

## Success Criteria

- [ ] Game logic runs as plugin (loaded from plugins.json)
- [ ] UI plugin successfully subscribes to game state changes
- [ ] Entity movements trigger UI updates reactively
- [ ] No direct coupling between game and UI code
- [ ] Terminal.Gui only in UI plugin (not in game logic)
- [ ] All tests pass (game logic tests without UI)
- [ ] Performance: 60 FPS with 100+ entities
- [ ] Hot-reload works for both plugins independently

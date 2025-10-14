---
id: RFC-0046
title: Turn-Based Game Loop with Energy System
status: Proposed
category: game-engine, ecs, gameplay
created: 2025-10-14
updated: 2025-10-14
priority: P1
effort: 6-8 hours
---

# RFC-0046: Turn-Based Game Loop with Energy System

**Status:** Proposed  
**Date:** 2025-10-14  
**Author:** Development Team  
**Category:** game-engine, ecs, gameplay  
**Priority:** HIGH (P1)  
**Estimated Effort:** 6-8 hours  
**Depends On:** 
- RFC-0007: Arch ECS Integration (Implemented)
- RFC-0043: Tile-Based Map System (In Progress)
- WingedBean.Plugins.DungeonGame (Existing)

---

## Summary

Implement a **turn-based game loop** using an energy/action point system for the dungeon crawler. This replaces the current real-time update model with discrete turns, where entities (player, enemies, NPCs) take actions in order based on their speed/energy. The system ensures fair turn ordering, supports variable action costs, and integrates with the tile-based map system (RFC-0043) for movement validation.

---

## Motivation

### Current Problem

The dungeon game currently uses a **real-time update loop**:

```csharp
// DungeonGame.cs - Update()
public void Update(float deltaTime)
{
    _movementSystem.Execute(_world, deltaTime);
    _aiSystem.Execute(_world, deltaTime);
    _combatSystem.Execute(_world, deltaTime);
    _renderSystem.Execute(_world, deltaTime);
}
```

**Issues:**
1. **No Turn Order** - All entities act simultaneously, causing race conditions
2. **Timing-Dependent** - Behavior varies with frame rate (not deterministic)
3. **Unfair AI** - Enemies can act multiple times before player responds
4. **No Action Costs** - Moving and attacking take the same "time"
5. **Difficult to Balance** - Speed/agility stats have no clear effect
6. **Poor UX** - Player can't plan ahead, feels chaotic

### Why Turn-Based?

**Gameplay Benefits:**
- 🎯 **Strategic Depth** - Player can think and plan each move
- ⚖️ **Fair Combat** - Clear action order, no frame-rate advantages
- 🎲 **Predictable** - Deterministic outcomes, easier to balance
- 🧠 **Tactical Decisions** - Action costs matter (move vs attack vs use item)
- 📊 **Clear Feedback** - "It's your turn" vs "Enemy's turn"

**Technical Benefits:**
- ✅ **Deterministic** - Same inputs = same outputs (testable, reproducible)
- ✅ **Replay Support** - Can record/replay entire game from action log
- ✅ **Network-Friendly** - Easy to synchronize (future multiplayer)
- ✅ **AI-Friendly** - AI gets full world state to make decisions

### Success Criteria

✅ Player and enemies take turns in energy-based order  
✅ Actions consume energy (move = 100, attack = 100, wait = 50)  
✅ Fast entities act more frequently than slow entities  
✅ Turn order displayed in UI  
✅ Player input blocks until their turn  
✅ AI acts automatically on their turn  
✅ Game state is deterministic and reproducible

---

## Proposal

### 1. Core Architecture

#### 1.1 Energy System

**Concept:** Each entity has an **energy pool** that fills over time based on their speed. When energy reaches a threshold (e.g., 100), the entity gets a turn.

```
Entity Speed: 10
Energy: 0 → 10 → 20 → ... → 100 (TURN!)
Action Cost: -100
Energy: 0 → 10 → 20 → ...
```

**Advantages:**
- Fast entities (speed 15) act more often than slow entities (speed 5)
- Fractional speeds work naturally (speed 7.5 = 7.5 energy/tick)
- Variable action costs (quick attack = 80, heavy attack = 120)
- Simple to implement and understand

#### 1.2 Turn Manager

Central system that:
1. Advances energy for all entities
2. Determines whose turn it is
3. Waits for action (player input or AI decision)
4. Processes action
5. Repeats

---

### 2. Component Definitions

#### 2.1 Energy Component

```csharp
namespace WingedBean.Plugins.DungeonGame.Components;

/// <summary>
/// Energy/action point component for turn-based gameplay.
/// Entities gain energy based on speed and act when energy >= threshold.
/// </summary>
public struct Energy
{
    /// <summary>Current energy accumulated</summary>
    public float Current;
    
    /// <summary>Energy threshold to take a turn (typically 100)</summary>
    public float Threshold;
    
    /// <summary>Energy gained per tick (derived from speed stat)</summary>
    public float GainRate;
    
    /// <summary>Is this entity currently taking their turn?</summary>
    public bool IsActive;
    
    /// <summary>Has this entity's turn been processed this cycle?</summary>
    public bool TurnProcessed;
}
```

#### 2.2 TurnAction Component

```csharp
namespace WingedBean.Plugins.DungeonGame.Components;

/// <summary>
/// Pending action for an entity's turn.
/// Set by input handler (player) or AI system (enemies).
/// Consumed by action processing system.
/// </summary>
public struct TurnAction
{
    public ActionType Type;
    public int TargetX;
    public int TargetY;
    public EntityHandle? TargetEntity;
    public string? ItemId;
    public int EnergyCost;
}

public enum ActionType
{
    None,
    Move,
    MeleeAttack,
    RangedAttack,
    UseItem,
    PickupItem,
    DropItem,
    OpenDoor,
    Wait,
    Interact
}
```

#### 2.3 TurnOrder Component (Singleton)

```csharp
namespace WingedBean.Plugins.DungeonGame.Components;

/// <summary>
/// Global turn state and ordering.
/// Singleton component attached to world.
/// </summary>
public class TurnOrder
{
    /// <summary>Current turn number (increments each full cycle)</summary>
    public int TurnNumber { get; set; }
    
    /// <summary>Entity currently taking their turn</summary>
    public EntityHandle? ActiveEntity { get; set; }
    
    /// <summary>Queue of entities ready to act (energy >= threshold)</summary>
    public Queue<EntityHandle> ReadyQueue { get; set; } = new();
    
    /// <summary>Is the game waiting for player input?</summary>
    public bool WaitingForPlayerInput { get; set; }
    
    /// <summary>Game state (player turn, enemy turn, processing)</summary>
    public TurnState State { get; set; }
}

public enum TurnState
{
    PlayerTurn,
    EnemyTurn,
    ProcessingActions,
    GameOver
}
```

---

### 3. System Implementation

#### 3.1 TurnManagerSystem

```csharp
namespace WingedBean.Plugins.DungeonGame.Systems;

using Microsoft.Extensions.Logging;
using WingedBean.Plugins.ArchECS.Internal;
using WingedBean.Plugins.DungeonGame.Components;

/// <summary>
/// Core turn-based game loop system.
/// Manages energy accumulation, turn order, and action processing.
/// </summary>
public class TurnManagerSystem : IECSSystem
{
    private readonly ILogger<TurnManagerSystem>? _logger;
    
    public TurnManagerSystem(ILogger<TurnManagerSystem>? logger = null)
    {
        _logger = logger;
    }
    
    public void Execute(IWorld world, float deltaTime)
    {
        // Get or create turn order singleton
        var turnOrder = world.GetSingleton<TurnOrder>() ?? InitializeTurnOrder(world);
        
        switch (turnOrder.State)
        {
            case TurnState.PlayerTurn:
                ProcessPlayerTurn(world, turnOrder);
                break;
                
            case TurnState.EnemyTurn:
                ProcessEnemyTurn(world, turnOrder);
                break;
                
            case TurnState.ProcessingActions:
                ProcessActions(world, turnOrder);
                break;
                
            case TurnState.GameOver:
                // Do nothing, game ended
                break;
        }
    }
    
    private TurnOrder InitializeTurnOrder(IWorld world)
    {
        var turnOrder = new TurnOrder
        {
            TurnNumber = 1,
            State = TurnState.PlayerTurn
        };
        
        world.SetSingleton(turnOrder);
        
        // Initialize energy for all entities
        foreach (var entity in world.CreateQuery<Stats>())
        {
            if (!world.HasComponent<Energy>(entity))
            {
                var stats = world.GetComponent<Stats>(entity);
                world.AddComponent(entity, new Energy
                {
                    Current = 0,
                    Threshold = 100,
                    GainRate = CalculateEnergyGainRate(stats),
                    IsActive = false,
                    TurnProcessed = false
                });
            }
        }
        
        _logger?.LogInformation("Turn-based system initialized");
        return turnOrder;
    }
    
    private void ProcessPlayerTurn(IWorld world, TurnOrder turnOrder)
    {
        // Find player entity
        var playerQuery = world.CreateQuery<Player, Energy, TurnAction>();
        var playerEntity = playerQuery.FirstOrDefault();
        
        if (playerEntity == default)
        {
            _logger?.LogWarning("No player entity found");
            turnOrder.State = TurnState.GameOver;
            return;
        }
        
        ref var energy = ref world.GetComponent<Energy>(playerEntity);
        ref var action = ref world.GetComponent<TurnAction>(playerEntity);
        
        // Accumulate energy if below threshold
        if (energy.Current < energy.Threshold)
        {
            energy.Current += energy.GainRate;
            
            // Check if any enemies are ready to act first
            if (HasEnemiesReady(world))
            {
                turnOrder.State = TurnState.EnemyTurn;
                return;
            }
            
            return; // Still accumulating energy
        }
        
        // Player has enough energy for a turn
        if (action.Type == ActionType.None)
        {
            // Wait for player input
            turnOrder.WaitingForPlayerInput = true;
            turnOrder.ActiveEntity = playerEntity;
            return;
        }
        
        // Player has chosen an action
        turnOrder.WaitingForPlayerInput = false;
        energy.Current -= action.EnergyCost;
        energy.TurnProcessed = true;
        
        _logger?.LogDebug("Player action: {Action} (cost: {Cost})", 
            action.Type, action.EnergyCost);
        
        // Process action immediately
        ProcessEntityAction(world, playerEntity, action);
        
        // Clear action
        action.Type = ActionType.None;
        
        // Check for enemies ready to act
        turnOrder.State = HasEnemiesReady(world) 
            ? TurnState.EnemyTurn 
            : TurnState.PlayerTurn;
    }
    
    private void ProcessEnemyTurn(IWorld world, TurnOrder turnOrder)
    {
        // Find enemies with enough energy
        var enemyQuery = world.CreateQuery<Enemy, Energy>();
        
        foreach (var entity in enemyQuery)
        {
            ref var energy = ref world.GetComponent<Energy>(entity);
            
            if (energy.Current < energy.Threshold)
            {
                energy.Current += energy.GainRate;
                continue;
            }
            
            if (energy.TurnProcessed)
                continue; // Already acted this cycle
            
            // Enemy has a turn
            turnOrder.ActiveEntity = entity;
            
            // AI decides action (handled by AISystem)
            // For now, wait for AI to set TurnAction component
            if (!world.HasComponent<TurnAction>(entity))
            {
                // AI hasn't decided yet, skip for now
                continue;
            }
            
            var action = world.GetComponent<TurnAction>(entity);
            
            if (action.Type == ActionType.None)
                continue; // AI still thinking
            
            // Process enemy action
            energy.Current -= action.EnergyCost;
            energy.TurnProcessed = true;
            
            _logger?.LogDebug("Enemy action: {Action} at ({X}, {Y})", 
                action.Type, action.TargetX, action.TargetY);
            
            ProcessEntityAction(world, entity, action);
            
            // Clear action
            world.RemoveComponent<TurnAction>(entity);
        }
        
        // Check if all entities have acted
        if (AllEntitiesProcessed(world))
        {
            // Reset turn flags
            ResetTurnFlags(world);
            turnOrder.TurnNumber++;
            turnOrder.State = TurnState.PlayerTurn;
            
            _logger?.LogInformation("Turn {Turn} complete", turnOrder.TurnNumber);
        }
    }
    
    private void ProcessActions(IWorld world, TurnOrder turnOrder)
    {
        // Process any queued actions (future: animations, effects)
        turnOrder.State = TurnState.PlayerTurn;
    }
    
    private void ProcessEntityAction(IWorld world, EntityHandle entity, TurnAction action)
    {
        switch (action.Type)
        {
            case ActionType.Move:
                ProcessMoveAction(world, entity, action);
                break;
                
            case ActionType.MeleeAttack:
                ProcessMeleeAttack(world, entity, action);
                break;
                
            case ActionType.Wait:
                // Do nothing, energy already consumed
                break;
                
            // Other actions handled by specialized systems
        }
    }
    
    private void ProcessMoveAction(IWorld world, EntityHandle entity, TurnAction action)
    {
        if (!world.HasComponent<Position>(entity))
            return;
        
        ref var pos = ref world.GetComponent<Position>(entity);
        
        // Validate movement with map (RFC-0043 integration)
        var map = world.GetSingleton<DungeonMap>();
        if (map != null)
        {
            var tile = map.GetTile(action.TargetX, action.TargetY);
            if (tile == null || !tile.Value.IsWalkable)
            {
                _logger?.LogDebug("Movement blocked at ({X}, {Y})", 
                    action.TargetX, action.TargetY);
                return;
            }
        }
        
        // Apply movement
        pos.X = action.TargetX;
        pos.Y = action.TargetY;
    }
    
    private void ProcessMeleeAttack(IWorld world, EntityHandle entity, TurnAction action)
    {
        if (!action.TargetEntity.HasValue)
            return;
        
        // Delegate to CombatSystem
        // (CombatSystem will handle damage calculation)
        _logger?.LogDebug("Melee attack from {Attacker} to {Target}", 
            entity, action.TargetEntity.Value);
    }
    
    private bool HasEnemiesReady(IWorld world)
    {
        var enemyQuery = world.CreateQuery<Enemy, Energy>();
        
        foreach (var entity in enemyQuery)
        {
            var energy = world.GetComponent<Energy>(entity);
            if (energy.Current >= energy.Threshold && !energy.TurnProcessed)
                return true;
        }
        
        return false;
    }
    
    private bool AllEntitiesProcessed(IWorld world)
    {
        var entityQuery = world.CreateQuery<Energy>();
        
        foreach (var entity in entityQuery)
        {
            var energy = world.GetComponent<Energy>(entity);
            if (energy.Current >= energy.Threshold && !energy.TurnProcessed)
                return false;
        }
        
        return true;
    }
    
    private void ResetTurnFlags(IWorld world)
    {
        var entityQuery = world.CreateQuery<Energy>();
        
        foreach (var entity in entityQuery)
        {
            ref var energy = ref world.GetComponent<Energy>(entity);
            energy.TurnProcessed = false;
        }
    }
    
    private float CalculateEnergyGainRate(Stats stats)
    {
        // Base energy gain from Dexterity stat
        // Dex 10 = 10 energy/tick (10 ticks to reach 100)
        // Dex 20 = 20 energy/tick (5 ticks to reach 100)
        return Math.Max(1, stats.Dexterity);
    }
}
```

---

### 4. Input Integration

#### 4.1 Player Input Handler

```csharp
namespace WingedBean.Plugins.DungeonGame.Systems;

using WingedBean.Plugins.ArchECS.Internal;
using WingedBean.Plugins.DungeonGame.Components;

/// <summary>
/// Handles player input and converts to TurnAction.
/// Only processes input during player's turn.
/// </summary>
public class PlayerInputSystem : IECSSystem
{
    private readonly IInputService _inputService;
    
    public PlayerInputSystem(IInputService inputService)
    {
        _inputService = inputService;
    }
    
    public void Execute(IWorld world, float deltaTime)
    {
        var turnOrder = world.GetSingleton<TurnOrder>();
        if (turnOrder == null || !turnOrder.WaitingForPlayerInput)
            return; // Not player's turn
        
        // Find player entity
        var playerQuery = world.CreateQuery<Player, Position>();
        var playerEntity = playerQuery.FirstOrDefault();
        
        if (playerEntity == default)
            return;
        
        var pos = world.GetComponent<Position>(playerEntity);
        
        // Get input
        var input = _inputService.GetInput();
        
        TurnAction action = new TurnAction { Type = ActionType.None };
        
        switch (input.Key)
        {
            case ConsoleKey.UpArrow or ConsoleKey.W:
                action = CreateMoveAction(pos.X, pos.Y - 1);
                break;
                
            case ConsoleKey.DownArrow or ConsoleKey.S:
                action = CreateMoveAction(pos.X, pos.Y + 1);
                break;
                
            case ConsoleKey.LeftArrow or ConsoleKey.A:
                action = CreateMoveAction(pos.X - 1, pos.Y);
                break;
                
            case ConsoleKey.RightArrow or ConsoleKey.D:
                action = CreateMoveAction(pos.X + 1, pos.Y);
                break;
                
            case ConsoleKey.Spacebar:
                action = new TurnAction 
                { 
                    Type = ActionType.Wait, 
                    EnergyCost = 50 
                };
                break;
        }
        
        if (action.Type != ActionType.None)
        {
            world.AddOrSetComponent(playerEntity, action);
        }
    }
    
    private TurnAction CreateMoveAction(int targetX, int targetY)
    {
        return new TurnAction
        {
            Type = ActionType.Move,
            TargetX = targetX,
            TargetY = targetY,
            EnergyCost = 100
        };
    }
}
```

---

### 5. AI Integration

#### 5.1 AI Decision System

```csharp
namespace WingedBean.Plugins.DungeonGame.Systems;

using WingedBean.Plugins.ArchECS.Internal;
using WingedBean.Plugins.DungeonGame.Components;

/// <summary>
/// AI decision-making for enemy turns.
/// Runs during enemy turn phase, sets TurnAction for each enemy.
/// </summary>
public class AIDecisionSystem : IECSSystem
{
    public void Execute(IWorld world, float deltaTime)
    {
        var turnOrder = world.GetSingleton<TurnOrder>();
        if (turnOrder == null || turnOrder.State != TurnState.EnemyTurn)
            return; // Not enemy turn
        
        // Process enemies that need to decide
        var enemyQuery = world.CreateQuery<Enemy, Energy, Position>();
        
        foreach (var entity in enemyQuery)
        {
            var energy = world.GetComponent<Energy>(entity);
            
            if (energy.Current < energy.Threshold)
                continue; // Not enough energy
            
            if (energy.TurnProcessed)
                continue; // Already acted
            
            if (world.HasComponent<TurnAction>(entity))
                continue; // Already decided
            
            // Make AI decision
            var action = DecideAction(world, entity);
            world.AddComponent(entity, action);
        }
    }
    
    private TurnAction DecideAction(IWorld world, EntityHandle entity)
    {
        var enemy = world.GetComponent<Enemy>(entity);
        var pos = world.GetComponent<Position>(entity);
        
        // Simple AI: move toward player
        var playerPos = FindPlayerPosition(world);
        
        if (playerPos == null)
        {
            // No player, wait
            return new TurnAction { Type = ActionType.Wait, EnergyCost = 50 };
        }
        
        // Calculate direction to player
        int dx = Math.Sign(playerPos.Value.X - pos.X);
        int dy = Math.Sign(playerPos.Value.Y - pos.Y);
        
        // Check if adjacent (can attack)
        if (Math.Abs(playerPos.Value.X - pos.X) <= 1 && 
            Math.Abs(playerPos.Value.Y - pos.Y) <= 1)
        {
            // Attack player
            var playerEntity = FindPlayerEntity(world);
            return new TurnAction
            {
                Type = ActionType.MeleeAttack,
                TargetEntity = playerEntity,
                EnergyCost = 100
            };
        }
        
        // Move toward player
        return new TurnAction
        {
            Type = ActionType.Move,
            TargetX = pos.X + dx,
            TargetY = pos.Y + dy,
            EnergyCost = 100
        };
    }
    
    private Position? FindPlayerPosition(IWorld world)
    {
        var playerQuery = world.CreateQuery<Player, Position>();
        var playerEntity = playerQuery.FirstOrDefault();
        
        if (playerEntity == default)
            return null;
        
        return world.GetComponent<Position>(playerEntity);
    }
    
    private EntityHandle? FindPlayerEntity(IWorld world)
    {
        var playerQuery = world.CreateQuery<Player>();
        var playerEntity = playerQuery.FirstOrDefault();
        
        return playerEntity == default ? null : playerEntity;
    }
}
```

---

## Implementation Plan

### Phase 1: Core Components (2 hours)
- [ ] Create `Components/Energy.cs`
- [ ] Create `Components/TurnAction.cs`
- [ ] Create `Components/TurnOrder.cs`
- [ ] Unit tests for component initialization

### Phase 2: Turn Manager System (3 hours)
- [ ] Implement `TurnManagerSystem.cs`
- [ ] Energy accumulation logic
- [ ] Turn state machine
- [ ] Action processing
- [ ] Integration tests

### Phase 3: Input & AI Integration (2 hours)
- [ ] Update `PlayerInputSystem` for turn-based input
- [ ] Update `AIDecisionSystem` for turn-based decisions
- [ ] Test player vs enemy turn order

### Phase 4: UI & Polish (1 hour)
- [ ] Display turn order in UI
- [ ] Show "Your Turn" indicator
- [ ] Display energy bars (optional)
- [ ] Test with multiple enemies

---

## Testing Strategy

### Unit Tests

```csharp
[Fact]
public void Energy_AccumulatesBasedOnSpeed()
{
    var world = CreateTestWorld();
    var entity = world.Create();
    world.AddComponent(entity, new Stats { Dexterity = 10 });
    world.AddComponent(entity, new Energy { GainRate = 10, Threshold = 100 });
    
    var system = new TurnManagerSystem();
    
    // Simulate 10 ticks
    for (int i = 0; i < 10; i++)
    {
        system.Execute(world, 0.016f);
    }
    
    var energy = world.GetComponent<Energy>(entity);
    Assert.Equal(100, energy.Current);
}

[Fact]
public void FastEntity_ActsBeforeSlowEntity()
{
    var world = CreateTestWorld();
    
    var fast = world.Create();
    world.AddComponent(fast, new Energy { GainRate = 20, Threshold = 100 });
    
    var slow = world.Create();
    world.AddComponent(slow, new Energy { GainRate = 5, Threshold = 100 });
    
    var system = new TurnManagerSystem();
    
    // Fast entity should reach threshold first (5 ticks vs 20 ticks)
    for (int i = 0; i < 5; i++)
    {
        system.Execute(world, 0.016f);
    }
    
    Assert.True(world.GetComponent<Energy>(fast).Current >= 100);
    Assert.True(world.GetComponent<Energy>(slow).Current < 100);
}
```

### Integration Tests
- Player takes turn, then enemy takes turn
- Multiple enemies act in speed order
- Action costs affect turn frequency
- Game state is deterministic with fixed seed

---

## Migration Path

### Backward Compatibility

The turn-based system can coexist with real-time systems:

```csharp
// DungeonGame.cs
public void Update(float deltaTime)
{
    if (_useTurnBased)
    {
        _turnManagerSystem.Execute(_world, deltaTime);
        _aiDecisionSystem.Execute(_world, deltaTime);
    }
    else
    {
        // Legacy real-time systems
        _movementSystem.Execute(_world, deltaTime);
        _aiSystem.Execute(_world, deltaTime);
    }
    
    // Always run render
    _renderSystem.Execute(_world, deltaTime);
}
```

### Rollout Plan
1. **Phase 1**: Add components and systems (no behavior change)
2. **Phase 2**: Enable turn-based mode via config flag
3. **Phase 3**: Test and balance
4. **Phase 4**: Make turn-based default

---

## Risks & Mitigations

| Risk | Impact | Mitigation |
|------|--------|------------|
| **Player Confusion** | Medium | Clear UI indicators, tutorial |
| **AI Too Slow** | Low | Optimize decision-making, add timeout |
| **Balance Issues** | Medium | Extensive playtesting, tunable parameters |
| **Replay Desync** | Low | Ensure deterministic RNG, log all actions |

---

## Alternatives Considered

### 1. Initiative-Based System (D&D Style)
**Approach:** Roll initiative at start of combat, fixed turn order

**Pros:** Simple, familiar to RPG players  
**Cons:** Doesn't reflect ongoing speed differences, less dynamic  
**Verdict:** ❌ Rejected - Energy system more flexible

### 2. Simultaneous Turns
**Approach:** All entities choose actions, then resolve simultaneously

**Pros:** Faster gameplay, no waiting  
**Cons:** Complex conflict resolution, less strategic  
**Verdict:** ❌ Rejected - Too complex for initial implementation

### 3. Real-Time with Pause
**Approach:** Real-time gameplay, player can pause to issue commands

**Pros:** Feels more action-oriented  
**Cons:** Not truly turn-based, harder to balance  
**Verdict:** ❌ Rejected - Doesn't meet turn-based requirement

---

## Success Metrics

### Functional
- ✅ Player and enemies alternate turns correctly
- ✅ Fast entities act 2x as often as slow entities
- ✅ Actions consume correct energy amounts
- ✅ Turn order displayed accurately in UI

### Performance
- ✅ Turn processing < 1ms per entity
- ✅ No frame drops during turn transitions
- ✅ Supports 50+ entities without lag

### UX
- ✅ Player understands when it's their turn
- ✅ No confusion about action costs
- ✅ Game feels fair and strategic

---

## Future Enhancements

1. **Action Queue** (RFC-TBD)
   - Plan multiple actions in advance
   - "End turn" button to execute queue

2. **Interrupt Actions** (RFC-TBD)
   - Reactions, attacks of opportunity
   - Conditional triggers

3. **Time Manipulation** (RFC-TBD)
   - Haste/slow spells
   - Time stop effects

4. **Replay System** (RFC-TBD)
   - Record action log
   - Replay entire game
   - Share seeds for challenges

---

## Related RFCs

- **RFC-0007**: Arch ECS Integration ✅ Implemented
- **RFC-0043**: Tile-Based Map System ⏭️ In Progress (movement validation)
- **RFC-0047**: Enhanced Movement System ⏭️ Proposed (pathfinding integration)
- **RFC-0048**: Inventory System ⏭️ Proposed (use item actions)

---

## References

- Roguelike Turn Systems: [RogueBasin](http://www.roguebasin.com/index.php?title=Time_Systems)
- Energy Systems: [Brogue Source](https://github.com/tmewett/BrogueCE)
- Turn-Based Design: [Game Programming Patterns](https://gameprogrammingpatterns.com/game-loop.html)

---

**Decision:** Proposed - Awaiting Review  
**Next Steps:**
1. Review with team
2. Prototype energy accumulation
3. Test with simple AI
4. Create implementation branch

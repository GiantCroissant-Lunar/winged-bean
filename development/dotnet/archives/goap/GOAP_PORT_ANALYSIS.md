# GOAP Port Analysis & Recommendation

**Date:** 2025-10-13  
**Source:** https://github.com/crashkonijn/GOAP  
**Target:** WingedBean / Plate Layer

---

## Executive Summary

**Recommendation: Port to Plate Layer as a Reusable Framework**

The GOAP (Goal-Oriented Action Planning) library is a **general-purpose AI planning system** that should be ported to the **Plate layer** (`plate-projects/`) as a reusable framework, similar to how MessagePipe and ObservableCollections are positioned.

---

## GOAP Library Analysis

### What is GOAP?

**Goal-Oriented Action Planning** is an AI architecture where:
1. **Agents** have **Goals** (desired world states)
2. **Actions** have **Preconditions** (what must be true) and **Effects** (what they change)
3. A **Planner** finds the optimal sequence of actions to achieve a goal
4. **Sensors** provide world state information

### Key Features

From the README and code analysis:

- ✅ **Multi-threaded** - Uses Unity Job System for performance
- ✅ **High Performance** - Optimized for 2000+ agents
- ✅ **Framework Agnostic Core** - Core logic separated from Unity-specific code
- ✅ **Visualization Tools** - Graph viewer for debugging
- ✅ **Flexible Configuration** - ScriptableObjects or code-based
- ✅ **Well-Tested** - Comprehensive test suite

### Architecture Layers

```
CrashKonijn.Goap.Core/          ← Pure C# interfaces (no Unity)
CrashKonijn.Goap.Resolver/      ← Planning algorithm (no Unity)
CrashKonijn.Agent.Core/         ← Agent abstraction (no Unity)
CrashKonijn.Agent.Runtime/      ← Agent implementation (minimal Unity)
CrashKonijn.Goap.Runtime/       ← Unity-specific behaviors
```

**Key Insight:** The core GOAP logic is **already separated from Unity**!

---

## Unity Dependencies Analysis

### Minimal Unity Coupling

From the grep results:
- **16 files** reference `MonoBehaviour` out of **422 total C# files**
- Most Unity dependencies are in:
  - `CrashKonijn.Goap.Runtime/Behaviours/` - Unity MonoBehaviour wrappers
  - Editor tools - Not needed for runtime

### Core is Pure C#

The core planning system (`CrashKonijn.Goap.Core` and `CrashKonijn.Goap.Resolver`) has:
- ✅ **No Unity dependencies**
- ✅ **Interface-based design**
- ✅ **Dependency injection ready**
- ✅ **Testable**

---

## Port Strategy

### Option A: Plate Layer (Recommended) ✅

**Location:** `/plate-projects/goap-core/`

**Rationale:**
1. **Reusability** - GOAP is useful for ANY game with AI (not just dungeon crawlers)
2. **Framework Status** - It's a general-purpose AI framework like MessagePipe
3. **Multi-Project** - Can be used in future yokan projects
4. **Separation of Concerns** - Game logic stays in WingedBean, AI framework in Plate

**What to Port:**
```
plate-projects/goap-core/
├── dotnet/
│   └── framework/
│       └── src/
│           ├── Plate.Goap.Core/           ← Port CrashKonijn.Goap.Core
│           ├── Plate.Goap.Resolver/       ← Port CrashKonijn.Goap.Resolver  
│           ├── Plate.Agent.Core/          ← Port CrashKonijn.Agent.Core
│           └── Plate.Agent.Runtime/       ← Port CrashKonijn.Agent.Runtime
```

**Benefits:**
- ✅ Shared across all Plate-based projects
- ✅ Can be used in Unity, Godot, or standalone .NET
- ✅ Follows existing Plate architecture patterns
- ✅ Easier to maintain and version

---

### Option B: WingedBean Layer (Not Recommended) ❌

**Location:** `/winged-bean/development/dotnet/framework/src/WingedBean.AI.Goap/`

**Why Not:**
- ❌ Couples GOAP to this specific project
- ❌ Can't reuse in other yokan projects
- ❌ GOAP is a framework, not game-specific logic
- ❌ Violates separation of concerns

**When to Use WingedBean Layer:**
- Game-specific GOAP **actions** (e.g., `AttackEnemyAction`, `PickupItemAction`)
- Game-specific GOAP **goals** (e.g., `DefeatBossGoal`, `CollectTreasureGoal`)
- Game-specific **sensors** (e.g., `NearbyEnemySensor`, `InventoryFullSensor`)

---

## Integration Architecture

### Recommended Structure

```
┌─────────────────────────────────────────────────────────┐
│         WingedBean.Plugins.DungeonGame                  │
│  - Game-specific GOAP Actions (AttackAction, etc.)     │
│  - Game-specific GOAP Goals (DefeatBossGoal, etc.)     │
│  - Game-specific Sensors (EnemySensor, etc.)           │
│  - Integrates with ECS entities                        │
└─────────────────────────────────────────────────────────┘
                          ↓ uses
┌─────────────────────────────────────────────────────────┐
│              Plate.Goap.Core (Framework)                │
│  - IAction, IGoal, ISensor interfaces                  │
│  - Planner/Resolver (A* graph search)                  │
│  - Agent abstraction                                   │
│  - World state management                              │
└─────────────────────────────────────────────────────────┘
```

### Integration with Existing Systems

**With ECS (Arch):**
```csharp
// Add GOAP component to entities
public struct GoapAgentComponent
{
    public IGoapAgent Agent;
    public IGoal CurrentGoal;
    public IAction CurrentAction;
}

// GOAP system updates agents
public class GoapSystem : IECSSystem
{
    public void Execute(IWorld world, float deltaTime)
    {
        foreach (var entity in world.CreateQuery<GoapAgentComponent>())
        {
            ref var goap = ref world.GetComponent<GoapAgentComponent>(entity);
            goap.Agent.Update(deltaTime);
        }
    }
}
```

**With MessagePipe:**
```csharp
// Publish events when actions complete
public class AttackAction : IGoapAction
{
    private readonly IPublisher<CombatEvent> _publisher;
    
    public void Execute()
    {
        // ... attack logic ...
        _publisher.Publish(new CombatEvent(...));
    }
}
```

**With GoRogue:**
```csharp
// Use GoRogue pathfinding in GOAP actions
public class MoveToTargetAction : IGoapAction
{
    private readonly GoRoguePathfindingSystem _pathfinding;
    
    public void Execute()
    {
        // Use GoRogue to calculate path
        var path = _pathfinding.CalculatePath(start, goal);
    }
}
```

---

## Porting Effort Estimate

### Phase 1: Core Framework (Week 1-2)
- Port `CrashKonijn.Goap.Core` → `Plate.Goap.Core`
- Port `CrashKonijn.Goap.Resolver` → `Plate.Goap.Resolver`
- Port `CrashKonijn.Agent.Core` → `Plate.Agent.Core`
- Remove Unity dependencies (replace with abstractions)
- **Effort:** 20-30 hours

### Phase 2: Runtime Adaptation (Week 2-3)
- Port `CrashKonijn.Agent.Runtime` → `Plate.Agent.Runtime`
- Replace Unity-specific code:
  - `MonoBehaviour` → Interface-based
  - `Transform` → Custom position/rotation structs
  - `Coroutines` → async/await
- **Effort:** 15-20 hours

### Phase 3: Integration (Week 3-4)
- Create WingedBean GOAP plugin
- Implement game-specific actions/goals
- Integrate with ECS, MessagePipe, GoRogue
- **Effort:** 20-25 hours

### Phase 4: Testing & Documentation (Week 4-5)
- Unit tests for core framework
- Integration tests with dungeon game
- Documentation and examples
- **Effort:** 10-15 hours

**Total Estimate:** 65-90 hours (8-11 days)

---

## Benefits of GOAP for Dungeon Crawler

### Current AI Limitations

Your current `AISystem.cs` has:
- ❌ Simple state machine (Idle, Chase, Attack, Flee)
- ❌ No planning or goal-driven behavior
- ❌ Hard-coded decision logic
- ❌ Difficult to add complex behaviors

### With GOAP

```csharp
// Define goals
public class DefeatPlayerGoal : IGoal { }
public class FleeToSafetyGoal : IGoal { }
public class GuardTreasureGoal : IGoal { }

// Define actions
public class AttackAction : IGoapAction
{
    Preconditions: PlayerInRange, HasWeapon
    Effects: PlayerDamaged
}

public class FleeAction : IGoapAction
{
    Preconditions: LowHealth
    Effects: AtSafeLocation
}

public class PatrolAction : IGoapAction
{
    Preconditions: NoThreats
    Effects: AreaPatrolled
}

// GOAP automatically finds the best action sequence!
```

**Benefits:**
- ✅ **Emergent Behavior** - Complex behaviors from simple actions
- ✅ **Dynamic Planning** - Adapts to changing world state
- ✅ **Maintainable** - Add new actions without breaking existing ones
- ✅ **Debuggable** - Visualize decision-making process
- ✅ **Scalable** - Handles hundreds of agents efficiently

---

## Comparison with Alternatives

### GOAP vs Behavior Trees

| Feature | GOAP | Behavior Trees |
|---------|------|----------------|
| **Planning** | Dynamic (finds optimal path) | Static (predefined tree) |
| **Flexibility** | High (emergent behavior) | Medium (requires tree design) |
| **Performance** | Good (with caching) | Excellent |
| **Complexity** | Medium | Low |
| **Best For** | Complex, adaptive AI | Reactive, predictable AI |

**Recommendation:** Use GOAP for **strategic AI** (enemies, NPCs) and keep simple state machines for **reactive behaviors** (traps, doors).

---

## Implementation Roadmap

### Immediate (This Week)
1. ✅ **Decision:** Port to Plate layer
2. 📋 **Create** `plate-projects/goap-core/` structure
3. 📦 **Port** core interfaces and resolver

### Short-term (Next 2 Weeks)
4. 🔧 **Adapt** runtime for non-Unity usage
5. 🧪 **Test** core framework independently
6. 📚 **Document** API and usage patterns

### Medium-term (Next Month)
7. 🎮 **Integrate** with WingedBean dungeon game
8. 🤖 **Implement** enemy AI using GOAP
9. 📊 **Benchmark** performance with 100+ agents
10. 🎨 **Create** visualization tools (optional)

---

## Risks & Mitigation

### Risk 1: Unity Dependencies
**Mitigation:** Core is already Unity-agnostic. Replace Unity-specific code with abstractions.

### Risk 2: Performance
**Mitigation:** GOAP library is already optimized for 2000+ agents. Profile and optimize if needed.

### Risk 3: Complexity
**Mitigation:** Start with simple actions/goals. Gradually add complexity as needed.

### Risk 4: Maintenance
**Mitigation:** Keep Plate layer GOAP generic. Game-specific code stays in WingedBean.

---

## Conclusion

**✅ Port GOAP to Plate Layer**

**Rationale:**
1. GOAP is a **general-purpose AI framework**, not game-specific
2. Core is **already Unity-agnostic**
3. **Reusable** across multiple projects
4. Follows **Plate architecture** patterns (like MessagePipe, ObservableCollections)
5. Provides **significant AI improvements** for dungeon crawler

**Next Steps:**
1. Create `plate-projects/goap-core/` repository structure
2. Port core interfaces and resolver (no Unity dependencies)
3. Adapt runtime layer for .NET (replace MonoBehaviour with interfaces)
4. Create WingedBean plugin with game-specific GOAP actions/goals
5. Integrate with existing ECS, MessagePipe, and GoRogue systems

**This will give you a powerful, reusable AI planning framework for all future projects!** 🚀

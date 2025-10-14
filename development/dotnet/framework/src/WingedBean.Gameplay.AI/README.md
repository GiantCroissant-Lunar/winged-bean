# WingedBean.Gameplay.AI

**Rules Engine + GOAP Architecture for Game AI**

This framework provides a declarative, flexible AI system that combines:
- **Rules Engine** (Microsoft.RulesEngine): Defines WHAT CAN be done (capabilities)
- **GOAP Planner** (Plate.Goap): Determines WHAT SHOULD be done (optimal actions)

## Architecture Overview

```
┌─────────────────────────────────────────────────┐
│           Rules Engine (Capabilities)           │
│  - "Can this entity cast fireball?"            │
│  - "Is melee attack in range?"                 │
│  - "Does player have key for door?"            │
│  Input: Entity state, target, environment      │
│  Output: Boolean (can/cannot perform action)   │
└────────────────┬────────────────────────────────┘
                 │
                 ↓ Capability Results
                 │
┌─────────────────────────────────────────────────┐
│              GOAP Planner (Actions)             │
│  - Uses capability checks as preconditions     │
│  - Finds optimal sequence to reach goal        │
│  - Example: "Goal: Defeat enemy"               │
│    Plan: Move closer → Attack → Use potion     │
└────────────────┬────────────────────────────────┘
                 │
                 ↓ Action Sequence
                 │
         Execute Actions
```

## Why This Architecture?

### Clear Separation of Concerns
- **Rules** = "Can I do this?" (Boolean capability checks)
- **GOAP** = "Should I do this?" (Goal-oriented planning)

### Declarative Capabilities
- Define capabilities in JSON, not code
- Non-programmers can balance AI behavior
- Easy to test and iterate

### Dynamic Re-evaluation
- Capabilities re-checked during execution
- Handles changing conditions (health, inventory, environment)
- AI adapts to unexpected situations

### Performance
- Rules engine optimized for fast boolean checks
- GOAP only plans with valid actions
- No wasted planning on impossible actions

### Reusability
- Same rules work for players, NPCs, monsters
- Same planner handles different entity types
- Compose complex behaviors from simple rules

## Quick Start

### 1. Define Capabilities (JSON)

```json
{
  "WorkflowName": "GoblinCapabilities",
  "Rules": [
    {
      "RuleName": "CanMeleeAttack",
      "Expression": "input1.Health > 0 AND input1.Distance <= 1.5"
    },
    {
      "RuleName": "CanFlee",
      "Expression": "input1.HealthPercent < 0.3"
    }
  ]
}
```

### 2. Create Context

```csharp
var goblinContext = new GoblinContext
{
    EntityId = "goblin_001",
    Health = 15,
    MaxHealth = 20,
    Position = (5, 5),
    TargetPosition = (6, 5),
    Inventory = new GoblinInventory { Potions = 2, HasWeapon = true }
};
```

### 3. Evaluate Capabilities

```csharp
var evaluator = new RulesEngineCapabilityEvaluator(workflows, logger);

// Check all capabilities
var capabilities = await evaluator.EvaluateAllAsync("GoblinCapabilities", goblinContext);

// Check specific capability
var canAttack = await evaluator.CanPerformAsync("CanMeleeAttack", "GoblinCapabilities", goblinContext);
```

### 4. Create GOAP Actions

```csharp
public class GoblinMeleeAttackAction : CapabilityGoapAction
{
    public override string RequiredCapability => "CanMeleeAttack";
    public override string WorkflowName => "GoblinCapabilities";
    
    protected override Task<bool> ExecuteActionAsync(object context)
    {
        var goblin = (GoblinContext)context;
        // Apply damage, play animation, etc.
        return Task.FromResult(true);
    }
    
    public override IConnector[] Effects => new[]
    {
        new Connector { Key = "TargetDamaged", Value = true }
    };
    
    public override IConnector[] Preconditions => new[]
    {
        new Connector { Key = "HasTarget", Value = true }
    };
}
```

### 5. Plan and Execute

```csharp
// Filter actions by capabilities
var allActions = new IGoapAction[]
{
    new GoblinMeleeAttackAction(evaluator, logger),
    new GoblinFleeAction(evaluator, logger),
    new GoblinUsePotionAction(evaluator, logger)
};

var availableActions = new List<IGoapAction>();
foreach (var action in allActions)
{
    if (await action.CheckPreconditions(goblinContext))
    {
        availableActions.Add(action);
    }
}

// GOAP plans using only available actions
var plan = goapPlanner.Plan(
    availableActions.ToArray(),
    keyResolver,
    resolver => new RunData
    {
        StartState = currentState,
        GoalState = goalState
    });

// Execute plan
foreach (var action in plan.Actions)
{
    await action.Execute(goblinContext);
}
```

## Project Structure

```
WingedBean.Gameplay.AI/
├── Capabilities/                  # Rules-based capability evaluation
│   ├── ICapabilityEvaluator.cs   # Interface for capability checking
│   ├── RulesEngineCapabilityEvaluator.cs  # RulesEngine implementation
│   └── CapabilityResult.cs       # Result with cost/metadata
│
├── Behaviors/                     # Behavior execution
│   ├── BehaviorContext.cs        # Base context for AI evaluation
│   └── IBehaviorExecutor.cs      # Orchestrates capabilities + GOAP
│
├── Integration/                   # Rules + GOAP bridge
│   └── CapabilityGoapAction.cs   # GOAP action with capability check
│
└── Examples/                      # Example implementations
    ├── GoblinContext.cs          # Example entity context
    ├── GoblinActions.cs          # Example GOAP actions
    └── goblin-capabilities.json  # Example capability rules
```

## Design Patterns

### Entity Context
Derive from `BehaviorContext` for each entity type:

```csharp
public record GoblinContext : BehaviorContext
{
    public int Health { get; init; }
    public (int X, int Y) Position { get; init; }
    // ... other state for rules evaluation
}
```

### Capability Rules
Use expressions that reference context properties:

```json
{
  "RuleName": "CanAttack",
  "Expression": "input1.Health > 0 AND input1.Distance <= 1.5"
}
```

### GOAP Actions
Extend `CapabilityGoapAction` to auto-check capabilities:

```csharp
public class AttackAction : CapabilityGoapAction
{
    public override string RequiredCapability => "CanAttack";
    // ... action logic
}
```

## Advanced Features

### Capability Costs
Define action preferences in rules:

```json
{
  "RuleName": "CanFlee",
  "Expression": "input1.HealthPercent < 0.3",
  "SuccessEvent": {
    "Cost": 2.0,
    "Priority": "Critical"
  }
}
```

GOAP planner can use cost for optimization.

### Dynamic Re-evaluation
Capabilities are re-checked during execution, allowing AI to adapt:

```csharp
// Plan assumes goblin has potion
var plan = await planner.PlanAsync(...);

// Execute actions
foreach (var action in plan.Actions)
{
    // Re-check capability before executing
    if (await action.CheckPreconditions(updatedContext))
    {
        await action.Execute(updatedContext);
    }
    else
    {
        // Replan if capability no longer available
        plan = await planner.PlanAsync(updatedContext, ...);
    }
}
```

### Capability Discovery
Query what an entity can do:

```csharp
var capabilities = await evaluator.EvaluateAllAsync("GoblinCapabilities", context);
var available = capabilities.Where(c => c.CanPerform).ToArray();

// Use for: AI debugging, tutorials, UI tooltips
Console.WriteLine($"Goblin can: {string.Join(", ", available.Select(c => c.CapabilityName))}");
```

## Testing Pattern

See `WingedBean.Testing.Recorder/RecordingTestBotService.cs` for a production example:
- Uses RulesEngine to drive synthetic behavior
- Generates test data for CI validation
- Same pattern adapted here for game AI

## Integration with WingedBean

This framework integrates with:
- **WingedBean.Gameplay.Goap**: Existing GOAP planner
- **WingedBean.Testing.Recorder**: Test bot pattern (proof-of-concept)
- **WingedBean.Contracts.Gameplay**: Shared gameplay interfaces

## References

- **Microsoft RulesEngine**: https://github.com/microsoft/RulesEngine
- **Plate.Goap**: Cross-Milo GOAP implementation
- **GOAP Algorithm**: Jeff Orkin (F.E.A.R. 2005)

## Next Steps

1. **Implement full behavior executor** with planning loop
2. **Add capability caching** for performance optimization
3. **Create hierarchical capabilities** (e.g., "CanCastSpell" includes "CanCastFireball")
4. **Build visual editor** for designers to create/edit capabilities
5. **Integrate with ECS** for scalable entity management

---

**Status**: Framework complete, ready for integration  
**Pattern Source**: Extracted from `RecordingTestBotService`  
**Architecture**: Production-ready for game AI

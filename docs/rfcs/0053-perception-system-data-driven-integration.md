---
id: RFC-0053
title: Perception System Integration with Data-Driven Configuration
status: Proposed
category: game-engine, gameplay, ai
created: 2025-10-15
updated: 2025-10-15
priority: P1
effort: 10-14 hours
---

# RFC-0053: Perception System Integration with Data-Driven Configuration

**Status:** Proposed  
**Date:** 2025-10-15  
**Author:** Development Team  
**Category:** game-engine, gameplay, ai  
**Priority:** HIGH (P1)  
**Estimated Effort:** 10-14 hours  
**Depends On:**
- RFC-0007: Arch ECS Integration (✅ IMPLEMENTED)
- RFC-0052: Field of View System Integration (📋 Proposed)
- Plate.PerceptionSensor: Core perception algorithms library (📋 IN DEVELOPMENT)
- Microsoft RulesEngine: Already used in other projects (✅ Available)

## Summary

Integrate perception and awareness systems into WingedBean using a **data-driven architecture** that combines:
1. **Plate.PerceptionSensor** - Pure algorithmic library for spatial calculations (FOV, sound propagation, scent)
2. **Arch ECS** - Entity component system for gameplay behavior
3. **Microsoft RulesEngine** - JSON-based rules for determining perception capabilities
4. **JSON Configuration** - Data-driven perception type definitions (vision, hearing, magic sense, chi detection, etc.)

This architecture allows adding new perception types (e.g., "detect magic", "sense chi", "echolocation") without code changes, using only JSON configuration files.

---

## Motivation

### The Problem: Hardcoded Perception Systems

Traditional approach requires code changes for each perception type:

```csharp
// ❌ BAD: Hardcoded, requires recompilation for new perception types
public class VisionSystem : ISystem { }
public class HearingSystem : ISystem { }
public class DetectMagicSystem : ISystem { }
public class SenseChiSystem : ISystem { }
// ... endless new systems for each game mechanic
```

**Problems:**
- ❌ Can't add new perception types without recompiling
- ❌ Can't modify perception behavior in JSON
- ❌ Game designers can't experiment with perception mechanics
- ❌ Difficult to balance perception ranges/strengths

### The Solution: Data-Driven Perception

```csharp
// ✅ GOOD: Data-driven, configured via JSON
public class PerceptionSystem : ISystem 
{
    // Handles ALL perception types based on JSON config
}
```

**Benefits:**
- ✅ Add new perception types in JSON (no code changes)
- ✅ Game designers can tune perception parameters
- ✅ RulesEngine determines who has what perception abilities
- ✅ Easy to add game-specific mechanics (chi, magic, psionic)
- ✅ Moddable by users

---

## Architecture Overview

### Three-Layer Design

```
┌─────────────────────────────────────────────────────────┐
│  Layer 1: Plate.PerceptionSensor (Pure Algorithms)      │
│  - Shadowcasting (line-of-sight)                        │
│  - Flood fill (sound/smell propagation)                 │
│  - No ECS dependency, no game logic                     │
└─────────────────────────────────────────────────────────┘
                         ▼
┌─────────────────────────────────────────────────────────┐
│  Layer 2: WingedBean.Gameplay.Perception (ECS Systems)  │
│  - PerceptionSystem (Arch ECS)                          │
│  - Component definitions                                │
│  - Uses PerceptionSensor library for calculations       │
└─────────────────────────────────────────────────────────┘
                         ▼
┌─────────────────────────────────────────────────────────┐
│  Layer 3: Configuration & Rules (JSON + RulesEngine)    │
│  - perception-types.json (defines all perception types) │
│  - perception-rules.json (who gets what abilities)      │
│  - RulesEngine evaluates conditions                     │
└─────────────────────────────────────────────────────────┘
```

---

## Component Design

### 1. ECS Components

```csharp
namespace WingedBean.Gameplay.Perception;

/// <summary>
/// Entity has one or more perception capabilities.
/// </summary>
public struct PerceptionCapabilitiesComponent
{
    /// <summary>
    /// List of perception type IDs this entity can use (e.g., "vision", "hearing", "detect_magic").
    /// Loaded from rules evaluation and perception definitions.
    /// </summary>
    public List<string> PerceptionTypes { get; set; }
    
    /// <summary>
    /// Cached perception definitions (populated at runtime from JSON).
    /// </summary>
    public Dictionary<string, PerceptionDefinition> Definitions { get; set; }
}

/// <summary>
/// Results of perception calculation (what this entity currently perceives).
/// </summary>
public struct PerceptionResultsComponent
{
    /// <summary>
    /// All entities detected by ANY perception type.
    /// </summary>
    public HashSet<Entity> DetectedEntities { get; set; }
    
    /// <summary>
    /// Positions detected by each perception type.
    /// Key: perception type (e.g., "vision"), Value: detected positions
    /// </summary>
    public Dictionary<string, HashSet<Point>> DetectedPositionsByType { get; set; }
    
    /// <summary>
    /// Signal strength at each position (for distance-based effects).
    /// </summary>
    public Dictionary<Point, double> SignalStrengths { get; set; }
    
    /// <summary>
    /// When was perception last updated.
    /// </summary>
    public DateTime LastUpdate { get; set; }
}

/// <summary>
/// Entity is perceivable by certain perception types.
/// </summary>
public struct PerceivableComponent
{
    /// <summary>
    /// Which perception types can detect this entity.
    /// e.g., ["vision", "detect_magic"] means visible and detectable by magic sense
    /// </summary>
    public List<string> PerceivableBy { get; set; }
    
    /// <summary>
    /// Optional: Signature strength modifier (1.0 = normal, 2.0 = easier to detect, 0.5 = harder).
    /// </summary>
    public Dictionary<string, double> SignatureModifiers { get; set; }
}
```

---

## Data-Driven Configuration

### 2. Perception Type Definitions (JSON)

**File:** `data/gameplay/perception-types.json`

```json
{
  "perception_types": [
    {
      "id": "vision",
      "name": "Vision",
      "description": "Standard sight using light",
      "propagation_mode": "LineOfSight",
      "default_range": 10,
      "attenuation_rate": 0.05,
      "flows_around_obstacles": false,
      "blocked_by_material_types": ["wall", "door_closed"],
      "properties": {
        "requires_light": true,
        "affected_by_darkness": true
      }
    },
    {
      "id": "darkvision",
      "name": "Darkvision",
      "description": "See in darkness (up to range)",
      "propagation_mode": "LineOfSight",
      "default_range": 20,
      "attenuation_rate": 0.03,
      "flows_around_obstacles": false,
      "blocked_by_material_types": ["wall"],
      "properties": {
        "requires_light": false,
        "affected_by_darkness": false,
        "grayscale": true
      }
    },
    {
      "id": "hearing",
      "name": "Hearing",
      "description": "Detect sounds through walls",
      "propagation_mode": "FloodFill",
      "default_range": 25,
      "attenuation_rate": 0.15,
      "flows_around_obstacles": true,
      "blocked_by_material_types": [],
      "attenuated_by_material_types": {
        "wall": 0.5,
        "door_closed": 0.3
      },
      "properties": {
        "detects_noise_events": true
      }
    },
    {
      "id": "detect_magic",
      "name": "Detect Magic",
      "description": "Sense magical auras",
      "propagation_mode": "FloodFill",
      "default_range": 15,
      "attenuation_rate": 0.1,
      "flows_around_obstacles": true,
      "blocked_by_material_types": ["antimagic_field"],
      "properties": {
        "only_detects_magical_entities": true,
        "can_identify_spell_schools": true
      }
    },
    {
      "id": "sense_chi",
      "name": "Sense Chi/Life Force",
      "description": "Detect living beings by their life energy",
      "propagation_mode": "FloodFill",
      "default_range": 20,
      "attenuation_rate": 0.08,
      "flows_around_obstacles": true,
      "blocked_by_material_types": ["lead", "antimagic_field"],
      "properties": {
        "only_detects_living": true,
        "strength_indicates_health": true
      }
    },
    {
      "id": "tremorsense",
      "name": "Tremorsense",
      "description": "Detect vibrations through ground",
      "propagation_mode": "FloodFill",
      "default_range": 30,
      "attenuation_rate": 0.2,
      "flows_around_obstacles": true,
      "blocked_by_material_types": ["air", "water"],
      "properties": {
        "only_through_solid_ground": true,
        "detects_movement": true
      }
    },
    {
      "id": "echolocation",
      "name": "Echolocation",
      "description": "Use sound reflections to 'see'",
      "propagation_mode": "LineOfSight",
      "default_range": 15,
      "attenuation_rate": 0.1,
      "flows_around_obstacles": false,
      "blocked_by_material_types": ["wall"],
      "properties": {
        "requires_air": true,
        "immune_to_darkness": true,
        "detects_shapes": true
      }
    },
    {
      "id": "blindsight",
      "name": "Blindsight",
      "description": "Perfect awareness within range (no line of sight needed)",
      "propagation_mode": "FloodFill",
      "default_range": 10,
      "attenuation_rate": 0.0,
      "flows_around_obstacles": true,
      "blocked_by_material_types": [],
      "properties": {
        "perfect_awareness": true,
        "immune_to_darkness": true,
        "immune_to_invisibility": true
      }
    }
  ]
}
```

---

### 3. Perception Rules (RulesEngine)

**File:** `data/gameplay/perception-rules.json`

```json
{
  "rules": [
    {
      "ruleName": "StandardHumanoid",
      "expression": "race == \"human\" OR race == \"elf\" OR race == \"dwarf\"",
      "actions": {
        "onSuccess": {
          "grant_perception": ["vision", "hearing"]
        }
      }
    },
    {
      "ruleName": "ElfDarkvision",
      "expression": "race == \"elf\"",
      "actions": {
        "onSuccess": {
          "grant_perception": ["darkvision"],
          "modify_range": {
            "darkvision": 20
          }
        }
      }
    },
    {
      "ruleName": "DwarfDarkvision",
      "expression": "race == \"dwarf\"",
      "actions": {
        "onSuccess": {
          "grant_perception": ["darkvision"],
          "modify_range": {
            "darkvision": 30
          }
        }
      }
    },
    {
      "ruleName": "MageDetectMagic",
      "expression": "class == \"mage\" AND level >= 3",
      "actions": {
        "onSuccess": {
          "grant_perception": ["detect_magic"],
          "modify_range": {
            "detect_magic": 15
          }
        }
      }
    },
    {
      "ruleName": "MonkSenseChi",
      "expression": "class == \"monk\" AND level >= 5",
      "actions": {
        "onSuccess": {
          "grant_perception": ["sense_chi"],
          "modify_range": {
            "sense_chi": 20
          }
        }
      }
    },
    {
      "ruleName": "BatEcholocation",
      "expression": "creature_type == \"bat\"",
      "actions": {
        "onSuccess": {
          "grant_perception": ["echolocation"],
          "remove_perception": ["vision"]
        }
      }
    },
    {
      "ruleName": "BeholderAllSeeing",
      "expression": "creature_type == \"beholder\"",
      "actions": {
        "onSuccess": {
          "grant_perception": ["vision", "darkvision", "detect_magic", "blindsight"]
        }
      }
    },
    {
      "ruleName": "InvisibilitySpell",
      "expression": "has_status_effect == \"invisible\"",
      "actions": {
        "onSuccess": {
          "modify_perceivability": {
            "vision": 0.0,
            "darkvision": 0.0,
            "detect_magic": 1.5
          }
        }
      }
    }
  ]
}
```

---

## System Implementation

### 4. Perception System (Arch ECS)

```csharp
namespace WingedBean.Gameplay.Perception;

/// <summary>
/// Main perception system that calculates what entities perceive.
/// Data-driven: all perception types configured via JSON.
/// </summary>
public class PerceptionSystem : ISystem
{
    private readonly World _world;
    private readonly PerceptionEngine _perceptionEngine;
    private readonly IPerceptionFilter _mapFilter;
    private readonly PerceptionConfiguration _config;
    private readonly RulesEngine _rulesEngine;
    
    // Query: Entities that can perceive
    private readonly QueryDescription _perceiverQuery = new QueryDescription()
        .WithAll<PerceptionCapabilitiesComponent, PositionComponent>();

    public PerceptionSystem(
        World world,
        PerceptionEngine perceptionEngine,
        IPerceptionFilter mapFilter,
        PerceptionConfiguration config,
        RulesEngine rulesEngine)
    {
        _world = world;
        _perceptionEngine = perceptionEngine;
        _mapFilter = mapFilter;
        _config = config;
        _rulesEngine = rulesEngine;
    }

    public void Update(double deltaTime)
    {
        // For each entity that can perceive
        _world.Query(in _perceiverQuery, (Entity entity, 
            ref PerceptionCapabilitiesComponent capabilities,
            ref PositionComponent position) =>
        {
            // Get or add results component
            if (!_world.Has<PerceptionResultsComponent>(entity))
            {
                _world.Add(entity, new PerceptionResultsComponent
                {
                    DetectedEntities = new HashSet<Entity>(),
                    DetectedPositionsByType = new Dictionary<string, HashSet<Point>>(),
                    SignalStrengths = new Dictionary<Point, double>()
                });
            }

            ref var results = ref _world.Get<PerceptionResultsComponent>(entity);
            
            // Clear previous results
            results.DetectedEntities.Clear();
            results.DetectedPositionsByType.Clear();
            results.SignalStrengths.Clear();

            // Calculate perception for each perception type this entity has
            foreach (var perceptionTypeId in capabilities.PerceptionTypes)
            {
                if (!capabilities.Definitions.TryGetValue(perceptionTypeId, out var definition))
                    continue;

                // Create perception query using definition from JSON
                var query = CreatePerceptionQuery(position.Position, definition);
                
                // Calculate using PerceptionSensor library
                var perceptionResult = _perceptionEngine.Calculate(query, _mapFilter);

                // Store detected positions for this perception type
                var detectedPositions = new HashSet<Point>(perceptionResult.DetectedPositions);
                results.DetectedPositionsByType[perceptionTypeId] = detectedPositions;

                // Merge signal strengths
                foreach (var kvp in perceptionResult.SignalStrength)
                {
                    if (!results.SignalStrengths.ContainsKey(kvp.Key) || 
                        results.SignalStrengths[kvp.Key] < kvp.Value)
                    {
                        results.SignalStrengths[kvp.Key] = kvp.Value;
                    }
                }

                // Detect entities at perceived positions
                DetectEntitiesInPositions(
                    entity, 
                    perceptionTypeId, 
                    detectedPositions, 
                    ref results);
            }

            results.LastUpdate = DateTime.UtcNow;
        });
    }

    private IPerceptionQuery CreatePerceptionQuery(Point origin, PerceptionDefinition definition)
    {
        // Convert JSON definition to IPerceptionMedium
        var medium = CreateMediumFromDefinition(definition);
        
        return new PerceptionQuery(
            origin: origin,
            range: definition.DefaultRange,
            medium: medium
        );
    }

    private IPerceptionMedium CreateMediumFromDefinition(PerceptionDefinition definition)
    {
        // Map string propagation mode to enum
        PropagationMode mode = definition.PropagationMode switch
        {
            "LineOfSight" => PropagationMode.LineOfSight,
            "FloodFill" => PropagationMode.FloodFill,
            "Raycast" => PropagationMode.Raycast,
            "GradientField" => PropagationMode.GradientField,
            _ => PropagationMode.LineOfSight
        };

        // Create appropriate medium
        return mode switch
        {
            PropagationMode.LineOfSight => new LightMedium(definition.AttenuationRate),
            PropagationMode.FloodFill => new SoundMedium(definition.AttenuationRate),
            _ => new LightMedium(definition.AttenuationRate)
        };
    }

    private void DetectEntitiesInPositions(
        Entity perceiver,
        string perceptionTypeId,
        HashSet<Point> detectedPositions,
        ref PerceptionResultsComponent results)
    {
        // Query all perceivable entities
        var perceivableQuery = new QueryDescription()
            .WithAll<PerceivableComponent, PositionComponent>();

        _world.Query(in perceivableQuery, (Entity target,
            ref PerceivableComponent perceivable,
            ref PositionComponent targetPos) =>
        {
            // Skip self
            if (target == perceiver)
                return;

            // Check if position is detected
            if (!detectedPositions.Contains(targetPos.Position))
                return;

            // Check if this perception type can detect this entity
            if (!perceivable.PerceivableBy.Contains(perceptionTypeId))
                return;

            // Apply signature modifiers
            double signatureModifier = 1.0;
            if (perceivable.SignatureModifiers?.TryGetValue(perceptionTypeId, out var modifier) == true)
            {
                signatureModifier = modifier;
            }

            // Add to detected entities
            results.DetectedEntities.Add(target);
        });
    }
}
```

---

### 5. Perception Configuration Loader

```csharp
namespace WingedBean.Gameplay.Perception;

/// <summary>
/// Loads perception type definitions from JSON.
/// </summary>
public class PerceptionConfiguration
{
    public Dictionary<string, PerceptionDefinition> PerceptionTypes { get; set; } 
        = new Dictionary<string, PerceptionDefinition>();

    public static PerceptionConfiguration LoadFromJson(string jsonPath)
    {
        var json = File.ReadAllText(jsonPath);
        var data = JsonSerializer.Deserialize<PerceptionConfigFile>(json);
        
        var config = new PerceptionConfiguration();
        foreach (var def in data.PerceptionTypes)
        {
            config.PerceptionTypes[def.Id] = def;
        }
        
        return config;
    }
}

public class PerceptionConfigFile
{
    [JsonPropertyName("perception_types")]
    public List<PerceptionDefinition> PerceptionTypes { get; set; }
}

public class PerceptionDefinition
{
    [JsonPropertyName("id")]
    public string Id { get; set; }
    
    [JsonPropertyName("name")]
    public string Name { get; set; }
    
    [JsonPropertyName("description")]
    public string Description { get; set; }
    
    [JsonPropertyName("propagation_mode")]
    public string PropagationMode { get; set; }
    
    [JsonPropertyName("default_range")]
    public int DefaultRange { get; set; }
    
    [JsonPropertyName("attenuation_rate")]
    public double AttenuationRate { get; set; }
    
    [JsonPropertyName("flows_around_obstacles")]
    public bool FlowsAroundObstacles { get; set; }
    
    [JsonPropertyName("blocked_by_material_types")]
    public List<string> BlockedByMaterialTypes { get; set; } = new();
    
    [JsonPropertyName("attenuated_by_material_types")]
    public Dictionary<string, double> AttenuatedByMaterialTypes { get; set; } = new();
    
    [JsonPropertyName("properties")]
    public Dictionary<string, object> Properties { get; set; } = new();
}
```

---

## Integration with RulesEngine

### 6. Perception Rules Evaluator

```csharp
namespace WingedBean.Gameplay.Perception;

/// <summary>
/// Uses RulesEngine to evaluate which perception capabilities an entity should have.
/// </summary>
public class PerceptionRulesEvaluator
{
    private readonly RulesEngine _rulesEngine;
    private readonly PerceptionConfiguration _config;

    public PerceptionRulesEvaluator(RulesEngine rulesEngine, PerceptionConfiguration config)
    {
        _rulesEngine = rulesEngine;
        _config = config;
    }

    /// <summary>
    /// Evaluates rules and grants perception capabilities to entity.
    /// </summary>
    public void EvaluateAndGrantPerceptions(World world, Entity entity)
    {
        // Get entity data for rule evaluation
        var entityData = ExtractEntityDataForRules(world, entity);
        
        // Execute rules
        var results = _rulesEngine.ExecuteAllRulesAsync("PerceptionRules", entityData).Result;
        
        // Collect granted perceptions
        var grantedPerceptions = new HashSet<string>();
        var rangeModifiers = new Dictionary<string, int>();
        var perceivabilityModifiers = new Dictionary<string, double>();

        foreach (var result in results)
        {
            if (result.IsSuccess && result.ActionResult?.Output != null)
            {
                var actions = JsonSerializer.Deserialize<RuleActions>(result.ActionResult.Output.ToString());
                
                if (actions?.OnSuccess?.GrantPerception != null)
                {
                    foreach (var perception in actions.OnSuccess.GrantPerception)
                    {
                        grantedPerceptions.Add(perception);
                    }
                }
                
                if (actions?.OnSuccess?.RemovePerception != null)
                {
                    foreach (var perception in actions.OnSuccess.RemovePerception)
                    {
                        grantedPerceptions.Remove(perception);
                    }
                }
                
                if (actions?.OnSuccess?.ModifyRange != null)
                {
                    foreach (var kvp in actions.OnSuccess.ModifyRange)
                    {
                        rangeModifiers[kvp.Key] = kvp.Value;
                    }
                }
            }
        }

        // Apply perceptions to entity
        if (!world.Has<PerceptionCapabilitiesComponent>(entity))
        {
            world.Add(entity, new PerceptionCapabilitiesComponent
            {
                PerceptionTypes = new List<string>(),
                Definitions = new Dictionary<string, PerceptionDefinition>()
            });
        }

        ref var capabilities = ref world.Get<PerceptionCapabilitiesComponent>(entity);
        capabilities.PerceptionTypes = grantedPerceptions.ToList();
        
        // Load definitions with any range modifiers applied
        foreach (var perceptionType in grantedPerceptions)
        {
            if (_config.PerceptionTypes.TryGetValue(perceptionType, out var baseDef))
            {
                var modifiedDef = CloneDefinition(baseDef);
                
                if (rangeModifiers.TryGetValue(perceptionType, out var newRange))
                {
                    modifiedDef.DefaultRange = newRange;
                }
                
                capabilities.Definitions[perceptionType] = modifiedDef;
            }
        }
    }

    private Dictionary<string, object> ExtractEntityDataForRules(World world, Entity entity)
    {
        var data = new Dictionary<string, object>();
        
        // Extract components as rule inputs
        if (world.Has<RaceComponent>(entity))
            data["race"] = world.Get<RaceComponent>(entity).Race;
            
        if (world.Has<ClassComponent>(entity))
            data["class"] = world.Get<ClassComponent>(entity).Class;
            
        if (world.Has<LevelComponent>(entity))
            data["level"] = world.Get<LevelComponent>(entity).Level;
            
        if (world.Has<CreatureTypeComponent>(entity))
            data["creature_type"] = world.Get<CreatureTypeComponent>(entity).Type;
            
        if (world.Has<StatusEffectsComponent>(entity))
            data["has_status_effect"] = world.Get<StatusEffectsComponent>(entity).ActiveEffects;
        
        return data;
    }

    private PerceptionDefinition CloneDefinition(PerceptionDefinition source)
    {
        // Deep clone the definition
        var json = JsonSerializer.Serialize(source);
        return JsonSerializer.Deserialize<PerceptionDefinition>(json);
    }
}

public class RuleActions
{
    [JsonPropertyName("onSuccess")]
    public RuleActionDetails OnSuccess { get; set; }
}

public class RuleActionDetails
{
    [JsonPropertyName("grant_perception")]
    public List<string> GrantPerception { get; set; }
    
    [JsonPropertyName("remove_perception")]
    public List<string> RemovePerception { get; set; }
    
    [JsonPropertyName("modify_range")]
    public Dictionary<string, int> ModifyRange { get; set; }
    
    [JsonPropertyName("modify_perceivability")]
    public Dictionary<string, double> ModifyPerceivability { get; set; }
}
```

---

## Usage Examples

### Example 1: Adding New Perception Type (No Code Changes!)

**Want to add "Detect Undead" perception?**

Just edit `perception-types.json`:

```json
{
  "id": "detect_undead",
  "name": "Detect Undead",
  "description": "Sense undead creatures",
  "propagation_mode": "FloodFill",
  "default_range": 25,
  "attenuation_rate": 0.12,
  "flows_around_obstacles": true,
  "blocked_by_material_types": ["consecrated_ground"],
  "properties": {
    "only_detects_undead": true,
    "strength_indicates_power": true
  }
}
```

And add a rule in `perception-rules.json`:

```json
{
  "ruleName": "ClericDetectUndead",
  "expression": "class == \"cleric\" AND level >= 1",
  "actions": {
    "onSuccess": {
      "grant_perception": ["detect_undead"]
    }
  }
}
```

**Done! No recompilation needed.**

---

### Example 2: Entity Setup

```csharp
// Create player
var player = world.Create(
    new PositionComponent { Position = new Point(10, 10) },
    new RaceComponent { Race = "elf" },
    new ClassComponent { Class = "ranger" },
    new LevelComponent { Level = 5 }
);

// Evaluate rules to grant perceptions
perceptionRulesEvaluator.EvaluateAndGrantPerceptions(world, player);
// Player now has: vision, hearing, darkvision (from being an elf)

// Create monster
var orc = world.Create(
    new PositionComponent { Position = new Point(15, 15) },
    new PerceivableComponent 
    { 
        PerceivableBy = new List<string> { "vision", "hearing", "darkvision", "sense_chi" }
    }
);

// Update perception system
perceptionSystem.Update(deltaTime);

// Check what player perceives
ref var playerResults = ref world.Get<PerceptionResultsComponent>(player);
if (playerResults.DetectedEntities.Contains(orc))
{
    Console.WriteLine("Player sees the orc!");
}
```

---

## Implementation Plan

### Phase 1: Foundation (4-6 hours)

1. **Create component definitions**
   - `PerceptionCapabilitiesComponent`
   - `PerceptionResultsComponent`
   - `PerceivableComponent`

2. **Configuration loader**
   - `PerceptionConfiguration`
   - JSON schema definitions

3. **Basic PerceptionSystem**
   - Integration with PerceptionSensor library
   - Component queries

### Phase 2: RulesEngine Integration (3-4 hours)

1. **Rules evaluator**
   - `PerceptionRulesEvaluator`
   - Rule action processing

2. **Sample rules and definitions**
   - Basic perception types (vision, hearing)
   - Sample creature rules

### Phase 3: Advanced Features (3-4 hours)

1. **Material-based blocking**
   - Filter implementation for material types
   - Attenuation by material

2. **Status effect integration**
   - Invisibility, blindness, etc.
   - Dynamic perception modification

3. **UI Integration**
   - FOV rendering in Terminal.Gui
   - Debug visualization

---

## Benefits

### For Developers
✅ Clean separation: algorithms (library) vs gameplay (ECS) vs rules (JSON)
✅ Testable in isolation
✅ Easy to extend with new perception types

### For Designers
✅ Add perception types without coding
✅ Tune ranges and behaviors in JSON
✅ Experiment with game mechanics quickly

### For Players
✅ Rich perception mechanics (vision, hearing, magic sense, etc.)
✅ Interesting gameplay from varied perception abilities
✅ Moddable (can add custom perception types)

---

## Success Criteria

✅ Can add new perception type with only JSON changes
✅ RulesEngine determines entity capabilities
✅ PerceptionSensor library calculates FOV/perception
✅ Arch ECS manages entity state efficiently
✅ Performance: 100+ entities with perception at 60 FPS
✅ Example: Elf with darkvision, dwarf with tremorsense, mage with detect magic

---

## References

- **RFC-0007**: Arch ECS Integration
- **Plate.PerceptionSensor**: Core algorithms library
- **Microsoft RulesEngine**: https://github.com/microsoft/RulesEngine
- D&D 5E perception mechanics (inspiration)

---

## Related RFCs

This RFC should be implemented alongside:
- **perception-sensor RFC-0001**: Library foundation
- **perception-sensor RFC-0002**: Shadowcasting algorithm
- **perception-sensor RFC-0003**: Flood fill algorithm (TBD)

Together they form a complete, data-driven perception system.

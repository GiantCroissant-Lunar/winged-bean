# RFC-0007 Execution Plan: Arch ECS Integration

## Overview
This document outlines GitHub issues for implementing RFC-0007: High-Performance ECS Layer with Arch.

## Legend
- 🔴 **CRITICAL PATH** - Must be done before next phase
- 🟢 **PARALLEL** - Can be done simultaneously with other issues
- 🔵 **SERIAL** - Must wait for dependencies

---

## Phase 1: Contract Layer (Tier 1)

### Wave 1.1 (🟢 PARALLEL - Core Contracts)
After RFC-0005 and RFC-0006 complete:

```
┌─ #63 Create WingedBean.Contracts.ECS project (30 min)
│     └─ netstandard2.1 target
│     └─ Project structure, README
│     └─ 🔴 BLOCKS all other Phase 1 work
│
├─ #64 Define IECSService interface (45 min)
│     └─ Depends on: #63
│     └─ CreateWorld, DestroyWorld, GetWorld
│     └─ Core ECS abstraction
│     └─ 🔴 BLOCKS Phase 2
│
├─ #65 Define IWorld interface (30 min)
│     └─ Depends on: #63
│     └─ Entity creation, component attach/detach
│     └─ Query building abstraction
│     └─ 🔴 BLOCKS Phase 2
│
├─ #66 Define IEntity interface (30 min)
│     └─ Depends on: #63
│     └─ Entity ID, Alive status
│     └─ Component access
│     └─ 🔴 BLOCKS Phase 2
│
├─ #67 Define IQuery interface (30 min)
│     └─ Depends on: #63
│     └─ Query execution abstraction
│     └─ ForEach pattern
│     └─ 🔴 BLOCKS Phase 2
│
└─ #68 Define ISystem interface (30 min)
      └─ Depends on: #63
      └─ Initialize, Update lifecycle
      └─ System base contract
      └─ 🔴 BLOCKS Phase 3
```

**Parallelization**: 6 agents (1 for project setup, 5 for interfaces)
**Time savings**: 3.5 hrs → 1 hr

---

## Phase 2: Arch Plugin (Tier 3)

### Wave 2.1 (Serial - Project Setup)
```
#69 Create WingedBean.Plugins.ArchECS project (30 min)
   └─ Depends on: #63-#68 (contracts ready)
   └─ net8.0 target
   └─ Reference Arch 1.3.0
   └─ Reference WingedBean.Contracts.ECS
   └─ 🔴 BLOCKS all Phase 2 implementation
```

### Wave 2.2 (🟢 PARALLEL - Adapter Classes)
After #69 completes:

```
┌─ #70 Implement ArchECSService (1 hr)
│     └─ IECSService implementation
│     └─ World management
│     └─ Service registration
│     └─ 🔴 BLOCKS #76 (registration)
│
├─ #71 Implement ArchWorld adapter (45 min)
│     └─ IWorld → Arch.Core.World
│     └─ Entity creation wrapper
│     └─ Query builder wrapper
│
├─ #72 Implement ArchEntity adapter (30 min)
│     └─ IEntity → Arch EntityReference
│     └─ Component access wrapper
│
└─ #73 Implement ArchQuery adapter (45 min)
      └─ IQuery → Arch.Core.QueryDescription
      └─ ForEach delegation
```

**Parallelization**: 4 agents
**Time savings**: 3.25 hrs → 1 hr

---

## Phase 3: Game Components

### Wave 3.1 (🟢 PARALLEL - Component Definitions)
After #69 completes:

```
┌─ #74 Define core components (Position, Stats, Renderable) (45 min)
│     └─ Position (X, Y, Z coords)
│     └─ Stats (HP, MaxHP, Attack, Defense)
│     └─ Renderable (Character, Color, Layer)
│
├─ #75 Define entity components (Player, Enemy, Item) (30 min)
│     └─ Player marker component
│     └─ Enemy (AIType, AggroRange)
│     └─ Item (ItemType, Stackable)
│
└─ #76 Define inventory/combat components (30 min)
      └─ Inventory (Items list, Capacity)
      └─ CombatState (Target, Cooldown)
      └─ Movement (Speed, Direction)
```

**Parallelization**: 3 agents
**Time savings**: 1.75 hrs → 45 min

---

## Phase 4: Game Systems

### Wave 4.1 (Serial - System Base)
```
#77 Create SystemBase abstract class (30 min)
   └─ Depends on: #68, #69
   └─ ISystem implementation
   └─ World reference
   └─ Query caching
   └─ 🔴 BLOCKS all system implementations
```

### Wave 4.2 (🟢 PARALLEL - System Implementations)
After #77 completes:

```
┌─ #78 Implement MovementSystem (1 hr)
│     └─ Query: Position + Movement
│     └─ Update positions based on movement
│     └─ Collision detection hooks
│
├─ #79 Implement RenderSystem (1.5 hrs)
│     └─ Query: Position + Renderable
│     └─ Terminal.Gui integration
│     └─ Layer sorting
│     └─ 🔴 CRITICAL: Must work with xterm.js
│
├─ #80 Implement CombatSystem (1.5 hrs)
│     └─ Query: CombatState + Stats
│     └─ Damage calculation
│     └─ HP updates
│     └─ Death handling
│
└─ #81 Implement AISystem (2 hrs)
      └─ Query: Enemy + Position + Stats
      └─ Pathfinding to player
      └─ Attack logic
      └─ State machine
```

**Parallelization**: 4 agents
**Time savings**: 6 hrs → 2 hrs

---

## Phase 5: Plugin Registration

### Wave 5.1 (Serial - Plugin Integration)
```
#82 Create ArchECSPlugin class (1 hr)
   └─ Depends on: #70, #77-#81
   └─ IPlugin implementation
   └─ Register ArchECSService
   └─ Register all systems
   └─ Plugin metadata
   └─ 🔴 BLOCKS #83
```

### Wave 5.2 (Serial - Configuration)
```
#83 Add ArchECS to plugins.json (15 min)
   └─ Depends on: #82
   └─ Enable ArchECS plugin
   └─ Priority: 100 (load early)
   └─ 🔴 BLOCKS #84
```

### Wave 5.3 (Serial - Manifest)
```
#84 Create .plugin.json for ArchECS (15 min)
   └─ Depends on: #83
   └─ Plugin metadata
   └─ Dependency declarations
   └─ 🔴 BLOCKS #85
```

---

## Phase 6: ConsoleDungeon Integration

### Wave 6.1 (Serial - World Setup)
```
#85 Integrate ECS into ConsoleDungeon plugin (2 hrs)
   └─ Depends on: #84
   └─ Resolve IECSService
   └─ Create game world
   └─ Initialize systems
   └─ World lifecycle management
   └─ 🔴 BLOCKS #86
```

### Wave 6.2 (Serial - Entity Creation)
```
#86 Create player and enemy entities (1.5 hrs)
   └─ Depends on: #85
   └─ Player entity with Position, Stats, Renderable
   └─ Enemy spawning logic
   └─ Item placement
   └─ 🔴 BLOCKS #87
```

### Wave 6.3 (Serial - Game Loop)
```
#87 Implement ECS game loop (1 hr)
   └─ Depends on: #86
   └─ Update systems in order
   └─ Frame timing
   └─ Input handling bridge
   └─ 🔴 BLOCKS Phase 7
```

---

## Phase 7: Testing & Verification

### Wave 7.1 (🔵 SERIAL - Build Test)
```
#88 Verify ArchECS plugin builds (30 min)
   └─ Depends on: #87
   └─ Clean build
   └─ Verify Arch reference
   └─ 🔴 MUST PASS before #89
```

### Wave 7.2 (🔵 SERIAL - Plugin Load Test)
```
#89 🔴 CRITICAL: Verify ArchECS plugin loads (30 min)
   └─ Depends on: #88
   └─ Run ConsoleDungeon.Host
   └─ Verify ArchECS loads dynamically
   └─ Verify IECSService registered
   └─ 🔴 MUST PASS before #90
```

### Wave 7.3 (🔵 SERIAL - System Test)
```
#90 🔴 CRITICAL: Verify systems execute (1 hr)
   └─ Depends on: #89
   └─ Verify MovementSystem updates positions
   └─ Verify CombatSystem calculates damage
   └─ Verify AISystem moves enemies
   └─ 🔴 MUST PASS before #91
```

### Wave 7.4 (🔵 SERIAL - Render Test)
```
#91 🔴 CRITICAL: Verify rendering in xterm.js (1 hr)
   └─ Depends on: #90
   └─ Start ConsoleDungeon.Host + Astro frontend
   └─ Verify entities render via Terminal.Gui
   └─ Verify movement visible in xterm.js
   └─ Verify combat effects visible
   └─ 🔴 MUST PASS before #92
```

### Wave 7.5 (🔵 SERIAL - Performance Test)
```
#92 Benchmark ECS performance (1 hr)
   └─ Depends on: #91
   └─ Spawn 1000 entities
   └─ Measure frame time
   └─ Verify <16ms per frame (60 FPS)
   └─ Profile system bottlenecks
```

---

## Phase 8: Unit Testing

### Wave 8.1 (🟢 PARALLEL - Adapter Tests)
After #92 completes:

```
┌─ #93 Unit tests for ArchWorld adapter (1 hr)
│     └─ Entity creation tests
│     └─ Component attach/detach tests
│     └─ Query building tests
│
├─ #94 Unit tests for ArchEntity adapter (45 min)
│     └─ Component access tests
│     └─ Entity lifecycle tests
│
└─ #95 Unit tests for ArchQuery adapter (45 min)
      └─ Query execution tests
      └─ ForEach tests
```

**Parallelization**: 3 agents
**Time savings**: 2.5 hrs → 1 hr

### Wave 8.2 (🟢 PARALLEL - System Tests)
After #92 completes:

```
┌─ #96 Unit tests for MovementSystem (1 hr)
│     └─ Position update tests
│     └─ Collision tests
│
├─ #97 Unit tests for CombatSystem (1.5 hrs)
│     └─ Damage calculation tests
│     └─ Death handling tests
│
├─ #98 Unit tests for AISystem (1.5 hrs)
│     └─ Pathfinding tests
│     └─ Attack logic tests
│
└─ #99 Unit tests for RenderSystem (1 hr)
      └─ Rendering tests
      └─ Layer sorting tests
```

**Parallelization**: 4 agents
**Time savings**: 5 hrs → 1.5 hrs

---

## Phase 9: Documentation & Cleanup

### Wave 9.1 (🟢 PARALLEL - Documentation)
After #99 completes:

```
┌─ #100 Create ECS architecture guide (2 hrs)
│      └─ Component design patterns
│      └─ System implementation guide
│      └─ Query optimization tips
│      └─ Arch-specific best practices
│
├─ #101 Create game entity guide (1 hr)
│      └─ How to define components
│      └─ How to create systems
│      └─ Entity lifecycle management
│
└─ #102 Update dungeon crawler roadmap (30 min)
       └─ Mark Phase 1 complete
       └─ Celebrate ECS integration
       └─ Next steps (Phase 2: Map Generation)
```

**Parallelization**: 3 agents
**Time savings**: 3.5 hrs → 2 hrs

---

## Dependency Graph

```
RFC-0005 (#48) + RFC-0006 (#62)
   └─→ #63 (ECS project)
        ├─→ #64 (IECSService) ────┐
        ├─→ #65 (IWorld) ──────────┤
        ├─→ #66 (IEntity) ─────────┼─→ #69 (Arch plugin project)
        ├─→ #67 (IQuery) ──────────┤     ├─→ #70 (ArchECSService) ─────────┐
        └─→ #68 (ISystem) ─────────┘     ├─→ #71 (ArchWorld) ──────────────┤
                                          ├─→ #72 (ArchEntity) ─────────────┤
                                          ├─→ #73 (ArchQuery) ──────────────┤
                                          ├─→ #74 (components 1) ───────────┤
                                          ├─→ #75 (components 2) ───────────┤
                                          ├─→ #76 (components 3) ───────────┤
                                          └─→ #77 (SystemBase) ─────────────┤
                                               ├─→ #78 (MovementSystem) ────┤
                                               ├─→ #79 (RenderSystem) ──────┤
                                               ├─→ #80 (CombatSystem) ──────┤
                                               └─→ #81 (AISystem) ──────────┤
                                                                             │
#70 + #77-#81 ───────────────────────────────────────────────────────────→ #82 (ArchECSPlugin)
                                                                              └─→ #83 (plugins.json)
                                                                                    └─→ #84 (manifest)
                                                                                          └─→ #85 (integration)
                                                                                                └─→ #86 (entities)
                                                                                                      └─→ #87 (game loop)
                                                                                                            └─→ #88 (build)
                                                                                                                  └─→ #89 🔴 (load)
                                                                                                                        └─→ #90 🔴 (systems)
                                                                                                                              └─→ #91 🔴 (render)
                                                                                                                                    └─→ #92 (perf)
                                                                                                                                          ├─→ #93-#95 (adapter tests)
                                                                                                                                          ├─→ #96-#99 (system tests)
                                                                                                                                          └─→ #100-#102 (docs)
```

---

## Optimal Execution Strategy

### With 6+ Agents
```
Day 1 Morning:
  Agent 1: #63 (project setup)

Day 1 Afternoon:
  Agent 1-5: #64, #65, #66, #67, #68 (interfaces)

Day 2 Morning:
  Agent 1: #69 (Arch plugin project)

Day 2 Afternoon:
  Agent 1-4: #70, #71, #72, #73 (adapters)
  Agent 5-6: #74, #75, #76 (components)

Day 3 Morning:
  Agent 1: #77 (SystemBase)

Day 3 Afternoon:
  Agent 1-4: #78, #79, #80, #81 (systems)

Day 4 Morning:
  Agent 1: #82 → #83 → #84 (registration)

Day 4 Afternoon:
  Agent 1: #85 → #86 → #87 (integration)

Day 5 Morning:
  Agent 1: #88 → #89 → #90 → #91 → #92 (testing chain)

Day 5 Afternoon:
  Agent 1-3: #93, #94, #95 (adapter tests)
  Agent 4-6: #96, #97, #98, #99 (system tests)

Day 6 Morning:
  Agent 1-3: #100, #101, #102 (docs)
```
**Total time**: ~6 days (~26 hours)

### Single Agent
```
Day 1: #63 → #64 → #65 → #66 → #67 → #68 → #69
Day 2: #70 → #71 → #72 → #73 → #74 → #75 → #76
Day 3: #77 → #78 → #79 → #80 → #81
Day 4: #82 → #83 → #84 → #85 → #86 → #87
Day 5: #88 → #89 → #90 → #91 → #92
Day 6: #93 → #94 → #95 → #96 → #97
Day 7: #98 → #99 → #100 → #101 → #102
```
**Total time**: ~7 days (~33 hours)

---

## Critical Path Analysis

### Longest Serial Chain
```
#63 (30m) → #64-#68 (2.5h) → #69 (30m) → #70 (1h) → #77 (30m) →
#78 (1h) → #82 (1h) → #83 (15m) → #84 (15m) → #85 (2h) → #86 (1.5h) →
#87 (1h) → #88 (30m) → #89 (30m) → #90 (1h) → #91 (1h) → #92 (1h) →
#93 (1h) → #100 (2h)
```
**Critical path total**: ~19 hours

### Bottleneck Issues
1. **#79** RenderSystem (1.5 hrs) - Terminal.Gui integration
2. **#81** AISystem (2 hrs) - Complex pathfinding
3. **#85** ConsoleDungeon integration (2 hrs) - Service wiring
4. **#91** xterm.js rendering test (1 hr) - Critical integration

### Critical Tests
- **#89** Plugin load verification - MUST PASS
- **#90** System execution verification - MUST PASS
- **#91** xterm.js rendering - MUST PASS
- **#92** Performance benchmark - MUST achieve 60 FPS

If any critical test fails, STOP and debug before proceeding.

---

## Issue Count & Time Estimates

| Phase | Issues | Serial Time | Parallel Time (6 agents) |
|-------|--------|-------------|--------------------------|
| Phase 1 | 6 (#63-#68) | 3.5 hrs | 1 hr |
| Phase 2 | 5 (#69-#73) | 3.75 hrs | 1.5 hrs |
| Phase 3 | 3 (#74-#76) | 1.75 hrs | 45 min |
| Phase 4 | 5 (#77-#81) | 6.5 hrs | 2.5 hrs |
| Phase 5 | 3 (#82-#84) | 1.5 hrs | 1.5 hrs |
| Phase 6 | 3 (#85-#87) | 4.5 hrs | 4.5 hrs |
| Phase 7 | 5 (#88-#92) | 4 hrs | 4 hrs |
| Phase 8 | 7 (#93-#99) | 7.5 hrs | 2.5 hrs |
| Phase 9 | 3 (#100-#102) | 3.5 hrs | 2 hrs |
| **Total** | **40 issues** | **~33 hrs** | **~20 hrs** |

**Speedup**: ~40% with 6 agents (significant parallel work)

---

## Success Criteria

### Phase 1 Complete When:
- ✅ WingedBean.Contracts.ECS project created
- ✅ All core interfaces defined (IECSService, IWorld, IEntity, IQuery, ISystem)
- ✅ Contracts compile for netstandard2.1

### Phase 2 Complete When:
- ✅ WingedBean.Plugins.ArchECS project created
- ✅ All adapters implemented (ArchECSService, ArchWorld, ArchEntity, ArchQuery)
- ✅ Arch 1.3.0 referenced and working

### Phase 3 Complete When:
- ✅ All game components defined
- ✅ Position, Stats, Renderable components created
- ✅ Entity markers (Player, Enemy, Item) created

### Phase 4 Complete When:
- ✅ SystemBase abstract class implemented
- ✅ All systems implemented (Movement, Render, Combat, AI)
- ✅ Systems compile and reference correct components

### Phase 5 Complete When:
- ✅ ArchECSPlugin class implemented
- ✅ Plugin registered in plugins.json
- ✅ .plugin.json manifest created

### Phase 6 Complete When:
- ✅ ConsoleDungeon integrated with ECS
- ✅ Player and enemies spawn as entities
- ✅ Game loop executes systems

### Phase 7 Complete When:
- ✅ Plugin loads dynamically
- ✅ Systems execute correctly
- ✅ Rendering works in xterm.js
- ✅ 60 FPS achieved with 1000 entities

### Phase 8 Complete When:
- ✅ All adapter tests passing
- ✅ All system tests passing
- ✅ >80% code coverage

### RFC-0007 Complete When:
- ✅ All phases complete
- ✅ Documentation published
- ✅ ECS architecture guide available
- ✅ Team can proceed to Phase 2 (Map Generation)

---

## Risk Mitigation

### Risk: Arch Learning Curve
**Mitigation:**
- Follow Arch documentation closely
- Test adapters incrementally
- Profile performance early

### Risk: Terminal.Gui + ECS Integration
**Mitigation:**
- RenderSystem is isolated (#79)
- Keep existing Terminal.Gui code as reference
- Test rendering frequently

### Risk: xterm.js Regression
**Mitigation:**
- Critical test at #91
- Keep terminal output unchanged
- Use same pty-service integration

### Risk: Performance Issues
**Mitigation:**
- Benchmark at #92
- Profile with 1000+ entities
- Optimize query patterns if needed

---

## Notes

- **Dependencies:** RFC-0005 + RFC-0006 (must be complete)
- **Blocks:** Phase 2 of dungeon crawler roadmap (Map Generation)
- **Risk Level:** Medium-High (new library, performance critical)
- **Rollback Plan:** Keep non-ECS version in separate branch

---

**Created:** 2025-10-01
**RFC:** RFC-0007
**Status:** Ready for Implementation (after RFC-0005, RFC-0006)
**Total Issues:** 40 (#63-#102)
**Estimated Effort:** 20-33 hours (6-7 days)

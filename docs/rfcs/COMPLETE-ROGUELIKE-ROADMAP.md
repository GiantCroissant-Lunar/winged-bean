---
title: Complete Roguelike Dungeon Crawler - Implementation Roadmap
type: planning
status: active
date: 2025-10-14
---

# Complete Roguelike Dungeon Crawler - Implementation Roadmap

**Date:** 2025-10-14  
**Status:** Planning Complete

---

## Overview

This document provides a **complete roadmap** for implementing a fully-featured roguelike dungeon crawler. All RFCs have been created and prioritized.

---

## RFC Summary

### ✅ Existing/In Progress (Other Agent)
- **RFC-0007**: Arch ECS Integration ✅ Implemented
- **RFC-0043**: Tile-Based Map System ⏭️ In Progress
- **RFC-0044**: Procedural Dungeon Generation ⏭️ In Progress
- **RFC-0045**: Plate Library Integration ✅ Implemented

### 🆕 Core Gameplay (This Session)
- **RFC-0046**: Turn-Based Game Loop ⏭️ Proposed
- **RFC-0047**: Enhanced Movement & Pathfinding ⏭️ Proposed
- **RFC-0048**: Inventory & Item Management ⏭️ Proposed

### 🎥 Critical Rendering (This Session)
- **RFC-0049**: Camera & Viewport System ⏭️ Proposed ⭐⭐⭐
- **RFC-0050**: Fog of War Rendering ⏭️ Proposed ⭐⭐⭐
- **RFC-0051**: HUD & UI Overlay System ⏭️ Proposed ⭐⭐⭐

### 🔮 Future RFCs (Identified, Not Yet Written)
- **RFC-0052**: Targeting & Cursor System
- **RFC-0053**: Combat Feedback & Visual Effects
- **RFC-0054**: Status Effects System
- **RFC-0055**: Dynamic Lighting System
- **RFC-0056**: Save/Load System

---

## Implementation Phases

### 🏗️ Phase 0: Foundation (Other Agent - In Progress)
**Duration:** Current sprint  
**Goal:** Map and generation systems

- [x] RFC-0043: Tile-Based Map System
- [x] RFC-0044: Procedural Dungeon Generation
- [x] RFC-0045: Plate Library Integration

**Deliverable:** Procedurally generated dungeons with walkable/blocking tiles

---

### 🎮 Phase 1: Core Gameplay (Week 1)
**Duration:** 5-7 days  
**Goal:** Playable turn-based game

**Priority 1 - Turn System (Day 1-2):**
- [ ] RFC-0046: Turn-Based Game Loop
  - Energy system
  - Turn order
  - Action processing
  - Player/AI turn management

**Priority 2 - Movement (Day 2-3):**
- [ ] RFC-0047: Enhanced Movement & Pathfinding
  - A* pathfinding with ModernSatsuma
  - Movement validation
  - Terrain costs
  - AI navigation

**Priority 3 - Items (Day 3-4):**
- [ ] RFC-0048: Inventory & Item Management
  - Inventory system
  - Equipment slots
  - Consumables
  - Pickup/drop/use

**Milestone:** Can play turn-based dungeon crawler with items

---

### 🎥 Phase 2: Critical Rendering (Week 2)
**Duration:** 5-7 days  
**Goal:** Proper roguelike feel

**Priority 1 - Camera (Day 1-2):**
- [ ] RFC-0049: Camera & Viewport System ⭐⭐⭐
  - Camera follow modes
  - Viewport culling
  - Edge scrolling
  - Large map support

**Priority 2 - Fog of War (Day 2-3):**
- [ ] RFC-0050: Fog of War Rendering ⭐⭐⭐
  - Visible/explored/unexplored states
  - Integration with GoRogue FOV
  - Entity visibility
  - Exploration feel

**Priority 3 - HUD (Day 3-5):**
- [ ] RFC-0051: HUD & UI Overlay System ⭐⭐⭐
  - Health/mana bars
  - Message log
  - Equipment display
  - Turn indicator

**Milestone:** Looks and feels like a roguelike

---

### ⚔️ Phase 3: Combat & Interaction (Week 3)
**Duration:** 5-7 days  
**Goal:** Engaging combat

**Priority 1 - Targeting (Day 1-2):**
- [ ] RFC-0052: Targeting & Cursor System
  - Cursor movement
  - Target selection
  - Range checking
  - Area targeting

**Priority 2 - Combat Feedback (Day 2-3):**
- [ ] RFC-0053: Combat Feedback & Visual Effects
  - Damage numbers
  - Hit effects
  - Combat animations
  - Message log integration

**Priority 3 - Status Effects (Day 3-5):**
- [ ] RFC-0054: Status Effects System
  - Buffs/debuffs
  - DoT (damage over time)
  - Duration tracking
  - Visual indicators

**Milestone:** Combat is interesting and tactical

---

### 💡 Phase 4: Polish & Features (Week 4)
**Duration:** 5-7 days  
**Goal:** Production-ready

**Priority 1 - Lighting (Day 1-2):**
- [ ] RFC-0055: Dynamic Lighting System
  - Light sources
  - Light propagation
  - Colored lighting
  - FOV integration

**Priority 2 - Save/Load (Day 2-4):**
- [ ] RFC-0056: Save/Load System
  - Game state serialization
  - Save slots
  - Auto-save
  - Permadeath handling

**Priority 3 - Polish (Day 4-5):**
- [ ] Audio system (basic)
- [ ] Particle effects
- [ ] Screen shake
- [ ] Transitions

**Milestone:** Polished, complete game

---

## Critical Path

### Must-Have for MVP (Minimum Viable Product)

1. **RFC-0043** - Tile-Based Map ✅ (Other agent)
2. **RFC-0044** - Procedural Generation ✅ (Other agent)
3. **RFC-0046** - Turn-Based System ⭐⭐⭐
4. **RFC-0049** - Camera System ⭐⭐⭐
5. **RFC-0050** - Fog of War ⭐⭐⭐
6. **RFC-0051** - HUD System ⭐⭐⭐

**Estimated Time:** 2-3 weeks

---

## Dependency Graph

```
RFC-0007 (ECS)
    ├─> RFC-0043 (Map) ✅ Other agent
    │   ├─> RFC-0044 (Generation) ✅ Other agent
    │   ├─> RFC-0046 (Turn-Based)
    │   │   ├─> RFC-0047 (Movement)
    │   │   ├─> RFC-0048 (Inventory)
    │   │   └─> RFC-0052 (Targeting)
    │   ├─> RFC-0049 (Camera) ⭐⭐⭐
    │   │   └─> RFC-0050 (Fog of War) ⭐⭐⭐
    │   └─> RFC-0055 (Lighting)
    │
    ├─> RFC-0045 (Plate Libraries) ✅
    │   └─> RFC-0047 (Movement - uses ModernSatsuma)
    │
    └─> RFC-0051 (HUD) ⭐⭐⭐
        ├─> RFC-0046 (Turn indicator)
        ├─> RFC-0048 (Equipment display)
        └─> RFC-0053 (Combat feedback)
```

---

## Team Coordination

### Other Agent's Work (Current)
- RFC-0043: Tile-Based Map System
- RFC-0044: Procedural Dungeon Generation
- RFC-0045: Plate Library Integration

### Your Work (Next)
**Immediate Priority (Can Start Now):**
- RFC-0046: Turn-Based Game Loop (independent)
- RFC-0047: Movement & Pathfinding (needs map basics)
- RFC-0048: Inventory System (independent)

**After Map System Complete:**
- RFC-0049: Camera System (needs map)
- RFC-0050: Fog of War (needs map + FOV)
- RFC-0051: HUD System (needs stats/inventory)

---

## Success Metrics

### Phase 1 Complete
- ✅ Player can move turn-by-turn
- ✅ Enemies take turns intelligently
- ✅ Can pick up and use items
- ✅ Combat works with damage calculation

### Phase 2 Complete
- ✅ Camera follows player smoothly
- ✅ Fog of war works correctly
- ✅ Can see health/mana/equipment
- ✅ Message log shows combat events

### Phase 3 Complete
- ✅ Can target enemies for ranged attacks
- ✅ Combat has visual feedback
- ✅ Status effects work (poison, buffs, etc.)
- ✅ Game feels polished

### Phase 4 Complete
- ✅ Dynamic lighting works
- ✅ Can save and load games
- ✅ Audio and effects add juice
- ✅ Ready for playtesting

---

## Risk Assessment

### High Risk
- **Camera System** - Complex coordinate transformations
  - *Mitigation:* Extensive unit tests, prototype early
  
- **Fog of War** - Performance with large maps
  - *Mitigation:* Viewport culling, dirty flags
  
- **Turn-Based System** - State machine complexity
  - *Mitigation:* Clear state diagram, integration tests

### Medium Risk
- **Pathfinding** - ModernSatsuma integration
  - *Mitigation:* Use existing GoRogue pathfinding as fallback
  
- **Inventory** - Complex item interactions
  - *Mitigation:* Start simple, iterate

### Low Risk
- **HUD** - Mostly rendering logic
- **Message Log** - Simple queue
- **Status Effects** - Well-defined pattern

---

## Next Actions

### Immediate (Today)
1. ✅ Review gap analysis
2. ✅ Review all RFCs
3. [ ] Prioritize with team
4. [ ] Assign RFC ownership

### This Week
1. [ ] Other agent completes map system
2. [ ] Start RFC-0046 (Turn-Based)
3. [ ] Prototype camera system
4. [ ] Design HUD layout

### Next Week
1. [ ] Complete Phase 1 (Core Gameplay)
2. [ ] Start Phase 2 (Rendering)
3. [ ] First playable build

---

## Resources

### Documentation
- All RFCs in `/docs/rfcs/`
- Gap analysis: `ROGUELIKE-GAP-ANALYSIS.md`
- This roadmap: `COMPLETE-ROGUELIKE-ROADMAP.md`

### Code References
- Existing FOV: `GoRogueFOVSystem.cs`
- Existing Render: `RenderSystem.cs`
- Existing Components: `Components/` directory

### External References
- RogueBasin: http://www.roguebasin.com/
- r/roguelikedev: https://www.reddit.com/r/roguelikedev/
- Plate.ModernSatsuma: `/plate-projects/modern-satsuma`
- GoRogue: https://github.com/Chris3606/GoRogue

---

## Conclusion

**We have a complete plan** for building a fully-featured roguelike dungeon crawler:

- ✅ **9 RFCs created** (6 new this session)
- ✅ **4-phase implementation plan**
- ✅ **Clear priorities** (Camera, Fog, HUD are critical)
- ✅ **Risk mitigation strategies**
- ✅ **Team coordination plan**

**Next step:** Review with team and begin implementation!

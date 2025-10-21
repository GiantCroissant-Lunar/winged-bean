# RFC Summary - All Missing Features

**Date:** 2025-10-15
**Status:** Phase 1 Complete (4 RFCs), Phase 2-5 Stubs Created

---

## Completed RFCs

### Phase 1: Foundation (P0 - Critical)
✅ **4/4 RFCs Created**

1. **RFC-0001:** [Environment/Tilemap System](phase-1-foundation/RFC-0001-environment-tilemap-system.md)
   - Dungeon maps, tiles, collision, doors, traps
   - Effort: 2-3 weeks

2. **RFC-0002:** [Camera Integration](phase-1-foundation/RFC-0002-camera-integration.md)
   - camera-bioca implementation + WingedBean integration
   - Effort: 1 week

3. **RFC-0003:** [Field of View System](phase-1-foundation/RFC-0003-field-of-view-system.md)
   - perception-sensor (shadowcasting) + FOV integration
   - Effort: 2-3 weeks

4. **RFC-0004:** [Dungeon Generation Integration](phase-1-foundation/RFC-0004-dungeon-generation-integration.md)
   - edg-rogqd integration, floor transitions
   - Effort: 2 weeks

### Plate Layer Projects
✅ **1/6 RFCs Created**

1. **RFC-Plate-0001:** [Pathfinding Library](plate-projects/RFC-Plate-0001-pathfinding-library.md)
   - A* pathfinding, heuristics, grid adapter
   - Effort: 1-2 weeks

---

## Remaining RFCs (TODO)

### Phase 2: Core Gameplay (P1 - High)
**0/5 RFCs Created** - Stubs below

#### RFC-0005: Pathfinding System Integration
**Effort:** 1 week
**Dependencies:** RFC-0001, RFC-Plate-0001

**Summary:** Integrate Plate.Pathfinding into WingedBean
- Create PathfindingComponent (current path, target)
- Create PathfindingSystem (calculate paths, dynamic replanning)
- Integrate with AISystem (use paths for movement)
- Integrate with MapService (provide grid adapter)

---

#### RFC-0006: Spawn System
**Effort:** 1 week
**Dependencies:** RFC-0001, RFC-0004

**Summary:** Dynamic enemy and item spawning based on floor depth
- Create SpawnSystem
- Define spawn tables (JSON) - enemy types, item types per floor
- Implement difficulty scaling (more/harder enemies deeper)
- Safe spawn placement (not near player, on walkable tiles)
- Special room spawning (treasure rooms, boss rooms)

---

#### RFC-0007: Inventory Integration
**Effort:** 1-2 weeks
**Dependencies:** None (inventory-poki exists)

**Summary:** Integrate inventory-poki into WingedBean gameplay
- Add InventoryComponent to entities
- Create pickup/drop/use input actions
- Create equipment system (weapon, armor slots)
- Create inventory UI/display (text-based)
- Integrate with Stats (equipped items affect stats)

---

#### RFC-0008: Loot System
**Effort:** 1 week
**Dependencies:** RFC-0007, RFC-Plate-0004

**Summary:** Loot drops from enemies and chests
- Create LootSystem
- Define loot tables (JSON) - per enemy type, per floor
- Implement rarity tiers (common, uncommon, rare, epic, legendary)
- Drop rate calculations
- Chest/container loot
- Integration with combat (drop on death)

---

### Plate Layer Projects (Remaining)

#### RFC-Plate-0002: Status Effect System
**Effort:** 1-2 weeks

**Summary:** Reusable buff/debuff system for RPGs
- Status effect definitions (poison, stun, burn, slow, etc.)
- Timed effects with duration
- Stackable effects (rules for stacking)
- Effect interactions (immunity, cleanse)
- Visual indicators

---

#### RFC-Plate-0004: Loot Table Library
**Effort:** 1-2 weeks

**Summary:** Reusable loot generation framework
- Loot table definitions (JSON)
- Weighted random selection
- Rarity system
- Drop rate calculations
- Procedural item generation (random stats/modifiers)

---

### Phase 3: Combat & AI (P1 - High)
**0/3 RFCs Created**

#### RFC-0009: Advanced Combat System
**Effort:** 2-3 weeks
**Dependencies:** RFC-0007 (inventory), RFC-Plate-0002 (status effects)

**Summary:** Ranged combat, magic, status effects
- Ranged combat system (bows, thrown weapons)
- Magic/spell system
- Status effect application (poison, stun, burn, etc.)
- Damage types (physical, fire, ice, lightning)
- Resistances and vulnerabilities
- Critical hits
- Area of effect (AOE) attacks
- Combat formulas (damage calculation)

---

#### RFC-0010: Enhanced AI
**Effort:** 2-3 weeks
**Dependencies:** RFC-0003 (FOV), RFC-0005 (pathfinding), RFC-0009 (combat)

**Summary:** Advanced AI behaviors
- AI personalities (aggressive, defensive, cowardly, tactical)
- Group tactics (flanking, coordinated attacks)
- Ranged combat AI (maintain distance, use cover)
- Patrol routes and waypoints
- AI memory (last seen player position)
- Ability usage AI (spells, special attacks)
- State machine improvements

---

#### RFC-Plate-0002: Status Effect System (Plate)
See above

---

### Phase 4 & 5: See GAP_ANALYSIS.md

Remaining systems:
- Progression/XP
- Quest System
- Save/Load
- Dialogue/Narrative
- Minimap
- Particle/VFX
- Audio Integration

---

## Implementation Priority

### Immediate (Start Here)
1. RFC-0001: Environment/Tilemap ✅ Ready
2. RFC-0002: Camera Integration ✅ Ready
3. RFC-0003: FOV System ✅ Ready
4. RFC-0004: Dungeon Generation ✅ Ready

### Next (After Phase 1)
5. RFC-Plate-0001: Pathfinding Library ✅ Ready
6. RFC-0005: Pathfinding Integration (TODO - create full RFC)
7. RFC-0006: Spawn System (TODO - create full RFC)
8. RFC-0007: Inventory Integration (TODO - create full RFC)

### Then (Core Gameplay Complete)
9-13: See GAP_ANALYSIS.md

---

## RFC Status

| Phase | Total | Created | Ready | TODO |
|-------|-------|---------|-------|------|
| Phase 1 | 4 | 4 | 4 | 0 |
| Phase 2 | 6 | 0 | 0 | 6 |
| Phase 3 | 3 | 0 | 0 | 3 |
| Phase 4 | 5 | 0 | 0 | 5 |
| Phase 5 | 5 | 0 | 0 | 5 |
| **Plate** | 6 | 1 | 1 | 5 |
| **Total** | **29** | **5** | **5** | **24** |

---

## Next Steps for Implementation

1. **Implement Phase 1** (All RFCs ready)
   - RFC-0001 → RFC-0002 → RFC-0003 → RFC-0004

2. **Create Phase 2 Full RFCs** (Expand stubs to full RFCs)
   - RFC-0005, RFC-0006, RFC-0007, RFC-0008
   - RFC-Plate-0004

3. **Implement Phase 2** (After RFCs created)

4. **Create Phase 3 Full RFCs**
   - RFC-0009, RFC-0010
   - RFC-Plate-0002

5. **Continue with Phases 4-5**

---

**Note:** Phase 1 RFCs are complete and ready for implementation. Phase 2-5 RFCs have stubs/summaries in this document. Full RFCs can be created on demand when needed for implementation.

---

**Author:** WingedBean Team
**Date:** 2025-10-15
**Version:** 1.0

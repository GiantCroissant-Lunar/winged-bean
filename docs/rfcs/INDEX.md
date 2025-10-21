# WingedBean RFC Index - Missing Features Implementation Plan

**Date:** 2025-10-15
**Purpose:** Track implementation of missing features identified in GAP_ANALYSIS.md

---

## Quick Links

- **Current State:** [CURRENT_STATE.md](../CURRENT_STATE.md) - What WingedBean has
- **Gap Analysis:** [GAP_ANALYSIS.md](../GAP_ANALYSIS.md) - What WingedBean needs
- **Existing RFCs:** [README.md](README.md) - Historical RFCs (0001-0055)

---

## NEW RFCs - Missing Features

This index tracks NEW RFCs created to address gaps identified in GAP_ANALYSIS.md.

---

## Phase 1: Foundation (P0 - Critical)

**Status:** 4 RFCs Created
**Completion:** 0/4 Implemented

| RFC | Title | Status | Plate Layer | File |
|-----|-------|--------|-------------|------|
| RFC-0056 | Environment/Tilemap System | ✅ Proposed | No | [0056-environment-tilemap-system.md](0056-environment-tilemap-system.md) |
| RFC-0057 | Camera Integration | ✅ Proposed | camera-bioca | [0057-camera-integration.md](0057-camera-integration.md) |
| RFC-0058 | Field of View System | ✅ Proposed | perception-sensor | [0058-field-of-view-system.md](0058-field-of-view-system.md) |
| RFC-0059 | Dungeon Generation Integration | ✅ Proposed | edg-rogqd (exists) | [0059-dungeon-generation-integration.md](0059-dungeon-generation-integration.md) |

---

## Phase 2: Core Gameplay (P1 - High)

**Status:** 1 RFC Created
**Completion:** 0/6 Implemented

| RFC | Title | Status | Plate Layer | File |
|-----|-------|--------|-------------|------|
| RFC-0060 | Pathfinding Library (Plate) | ✅ Proposed | **NEW PROJECT** | [0060-pathfinding-library.md](0060-pathfinding-library.md) |
| RFC-0061 | Pathfinding System Integration | ⏳ TODO | pathfinding-astar | TBD |
| RFC-0062 | Spawn System | ⏳ TODO | No | TBD |
| RFC-0063 | Inventory Integration | ⏳ TODO | inventory-poki (exists) | TBD |
| RFC-0064 | Loot Table Library (Plate) | ⏳ TODO | **NEW PROJECT** | TBD |
| RFC-0065 | Loot System | ⏳ TODO | loot-table | TBD |

---

## Phase 3: Combat & AI (P1 - High)

**Status:** 0 RFCs Created
**Completion:** 0/3 Implemented

| RFC | Title | Status | Plate Layer | File |
|-----|-------|--------|-------------|------|
| RFC-0066 | Status Effect System (Plate) | ⏳ TODO | **NEW PROJECT** | TBD |
| RFC-0067 | Advanced Combat System | ⏳ TODO | status-effect | TBD |
| RFC-0068 | Enhanced AI | ⏳ TODO | No | TBD |

---

## Phase 4: Progression & Content (P2 - Medium)

**Status:** 0 RFCs Created
**Completion:** 0/5 Implemented

| RFC | Title | Status | Plate Layer | File |
|-----|-------|--------|-------------|------|
| RFC-0069 | Progression/XP Library (Plate) | ⏳ TODO | **NEW PROJECT** | TBD |
| RFC-0070 | Progression System | ⏳ TODO | progression-level | TBD |
| RFC-0071 | Quest System Library (Plate) | ⏳ TODO | **NEW PROJECT** | TBD |
| RFC-0072 | Quest System Integration | ⏳ TODO | quest-system | TBD |
| RFC-0073 | Save/Load System | ⏳ TODO | No | TBD |

---

## Phase 5: Polish & UX (P3 - Low)

**Status:** 0 RFCs Created
**Completion:** 0/5 Implemented

| RFC | Title | Status | Plate Layer | File |
|-----|-------|--------|-------------|------|
| RFC-0074 | Dialogue System Library (Plate) | ⏳ TODO | **NEW PROJECT** | TBD |
| RFC-0075 | Dialogue/Narrative Integration | ⏳ TODO | dialogue-tree | TBD |
| RFC-0076 | Minimap System | ⏳ TODO | No | TBD |
| RFC-0077 | Particle/VFX System | ⏳ TODO | No | TBD |
| RFC-0078 | Audio Integration | ⏳ TODO | No | TBD |

---

## Plate Layer Projects Summary

**Reusable framework components to be created**

| RFC | Project Name | Purpose | Status | File |
|-----|--------------|---------|--------|------|
| RFC-0060 | pathfinding-astar | A* pathfinding library | ✅ Proposed | [0060-pathfinding-library.md](0060-pathfinding-library.md) |
| RFC-0066 | status-effect | Status effects/buffs/debuffs | ⏳ TODO | TBD |
| RFC-0069 | progression-level | XP and leveling | ⏳ TODO | TBD |
| RFC-0064 | loot-table | Loot generation | ⏳ TODO | TBD |
| RFC-0071 | quest-system | Quest framework | ⏳ TODO | TBD |
| RFC-0074 | dialogue-tree | Dialogue system | ⏳ TODO | TBD |

---

## Summary

- **Total NEW RFCs Needed:** 29
- **Created:** 5 (RFC-0056 through RFC-0060)
- **TODO:** 24
- **Implemented:** 0

---

## Next Steps for Implementation Agent

1. ✅ **Phase 1 Foundation - ALL RFCS READY:**
   - ✅ RFC-0056: Environment/Tilemap System (READY)
   - ✅ RFC-0057: Camera Integration (READY)
   - ✅ RFC-0058: FOV System (READY)
   - ✅ RFC-0059: Dungeon Generation Integration (READY)

2. **Implement Phase 1** (All RFCs ready to code)
3. ✅ **Phase 2 Plate Layer Ready:**
   - ✅ RFC-0060: Pathfinding Library (READY)
4. **Create Phase 2 RFCs** (5 remaining RFCs)
5. **Implement Phase 2**
6. **Continue with Phases 3-5**

---

**Legend:**
- ✅ = RFC Created
- ⏳ = TODO (Not yet created)
- 🚧 = In Progress
- ✔️ = Implemented

---

**Last Updated:** 2025-10-15
**RFCs in Main Directory:** RFC-0056 through RFC-0060 (5 complete)
**All RFCs:** See [README.md](README.md) for complete index

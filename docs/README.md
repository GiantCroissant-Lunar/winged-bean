# WingedBean Documentation

**Project:** WingedBean - Multi-language, plugin-based dungeon crawler
**Stack:** .NET (primary), Node.js, Python
**Status:** Foundation Complete (~60%), Core Gameplay In Progress (~30%)

---

## Quick Start

### For Implementation Agents

1. **Read in this order:**
   - [CURRENT_STATE.md](CURRENT_STATE.md) - What exists
   - [GAP_ANALYSIS.md](GAP_ANALYSIS.md) - What's missing
   - [IMPLEMENTATION_GUIDE.md](IMPLEMENTATION_GUIDE.md) - How to implement
   - [rfcs/INDEX.md](rfcs/INDEX.md) - RFC tracker

2. **Start implementing:**
   - Phase 1: Foundation (RFCs 0001-0004) - **START HERE**
   - See [IMPLEMENTATION_GUIDE.md](IMPLEMENTATION_GUIDE.md) for details

### For Users/Players

- See [../README.md](../README.md) for game information
- See [../CLAUDE.md](../CLAUDE.md) for Claude Code instructions

---

## Documentation Structure

```
docs/
├── README.md                     ← You are here
├── CURRENT_STATE.md              ← What WingedBean has implemented
├── GAP_ANALYSIS.md               ← What's missing, prioritized
├── IMPLEMENTATION_GUIDE.md       ← How to implement missing features
│
├── rfcs/                         ← Technical specifications
│   ├── INDEX.md                  ← RFC tracker for missing features
│   ├── README.md                 ← Historical RFCs (0001-0055)
│   │
│   ├── phase-1-foundation/       ← P0 Critical (Blocking)
│   │   ├── RFC-0001-environment-tilemap-system.md
│   │   ├── RFC-0002-camera-integration.md
│   │   ├── RFC-0003-field-of-view-system.md
│   │   └── RFC-0004-dungeon-generation-integration.md (TODO)
│   │
│   ├── phase-2-core-gameplay/    ← P1 High (MVP)
│   ├── phase-3-combat-ai/        ← P1 High (MVP+)
│   └── plate-projects/           ← New Plate layer projects
│
├── adr/                          ← Architecture Decision Records
├── design/                       ← Design documents
└── development/                  ← Development guides
```

---

## Key Documents

### Current State & Planning

| Document | Purpose | Audience |
|----------|---------|----------|
| [CURRENT_STATE.md](CURRENT_STATE.md) | Inventory of implemented features | All |
| [GAP_ANALYSIS.md](GAP_ANALYSIS.md) | Missing features, prioritized by phase | Implementers |
| [IMPLEMENTATION_GUIDE.md](IMPLEMENTATION_GUIDE.md) | Step-by-step implementation guide | Implementers |
| [rfcs/INDEX.md](rfcs/INDEX.md) | RFC tracker for missing features | Implementers |

### Technical Specifications

| Document | Purpose | Status |
|----------|---------|--------|
| [RFC-0001](rfcs/phase-1-foundation/RFC-0001-environment-tilemap-system.md) | Environment/Tilemap System | ✅ Ready |
| [RFC-0002](rfcs/phase-1-foundation/RFC-0002-camera-integration.md) | Camera Integration | ✅ Ready |
| [RFC-0003](rfcs/phase-1-foundation/RFC-0003-field-of-view-system.md) | FOV System | ✅ Ready |
| More RFCs... | See [rfcs/INDEX.md](rfcs/INDEX.md) | Various |

---

## Implementation Status

### Phase 1: Foundation (P0 - Critical)
**Completion:** 0/4
- ⏳ RFC-0001: Environment/Tilemap System
- ⏳ RFC-0002: Camera Integration
- ⏳ RFC-0003: FOV System
- ⏳ RFC-0004: Dungeon Generation Integration

### Phase 2: Core Gameplay (P1 - High)
**Completion:** 0/6
- See [GAP_ANALYSIS.md](GAP_ANALYSIS.md) for full list

### Phase 3: Combat & AI (P1 - High)
**Completion:** 0/3

### Phase 4: Progression & Content (P2 - Medium)
**Completion:** 0/5

### Phase 5: Polish & UX (P3 - Low)
**Completion:** 0/5

**Overall Progress:** 0/23 core features implemented

---

## What Exists (Summary)

### Plate Layer (Framework Components)
- ✅ **camera-bioca** - 2D camera (RFC complete, not implemented)
- ✅ **capability-orlia** - Capability evaluation (implemented)
- ✅ **cross-milo** - Cross-platform contracts (implemented)
- ✅ **edg-rogqd** - Dungeon generation (implemented)
- ✅ **goap-ckpor** - GOAP AI planning (implemented)
- ✅ **inventory-poki** - Inventory system (implemented, not integrated)
- ⚠️ **perception-sensor** - Perception/FOV (planning phase)
- ✅ **input-nanat** - Input handling (implemented)
- ✅ **ui-blink** - UI contracts (implemented)
- ✅ **plugin-manoi** - Plugin system (implemented)
- ✅ **modern-satsuma** - Graph algorithms (implemented)

### Yokan Layer (WingedBean Game)
- ✅ **ArchECS Plugin** - ECS integration
- ✅ **Audio Plugin** - LibVLC audio
- ✅ **DungeonGame Plugin** - Basic game (player, enemies, combat, movement)
- ✅ **Gameplay.AI** - Rules Engine + GOAP
- ✅ **Gameplay.Goap** - GOAP planner service
- ⚠️ **Gameplay.Map** - Map system (partial)
- ⚠️ **Gameplay.Perception** - Perception (partial)

### What Works
- ✅ Basic ECS
- ✅ Player and enemy entities
- ✅ Basic melee combat
- ✅ Movement with bounds checking
- ✅ Text-based rendering
- ✅ Audio playback
- ✅ Input handling
- ✅ Plugin loading
- ✅ URF recording for testing

---

## What's Missing (Summary)

### Critical (Blocking Everything)
- ❌ Environment/Tilemap (walls, floors, doors, collision)
- ❌ Camera integration (viewport, tracking)
- ❌ FOV system (shadowcasting, fog of war)
- ❌ Dungeon generation integration

### High Priority (MVP)
- ❌ Pathfinding (A* algorithm)
- ❌ Spawn system (dynamic enemies/items)
- ❌ Inventory integration
- ❌ Loot system (drops, tables)
- ❌ Advanced combat (ranged, magic, status effects)
- ❌ Enhanced AI (tactics, personalities)

### Medium Priority (Full Release)
- ❌ Progression system (XP, leveling)
- ❌ Quest system
- ❌ Save/load system

### Low Priority (Polish)
- ❌ Dialogue/narrative
- ❌ Minimap
- ❌ Particle/VFX
- ❌ Audio integration

**See [GAP_ANALYSIS.md](GAP_ANALYSIS.md) for detailed breakdown**

---

## Architecture Overview

### Layer Separation

```
┌─────────────────────────────────────────┐
│  Plate Layer (Reusable Frameworks)     │
│  - camera-bioca, edg-rogqd, etc.       │
└─────────────────────────────────────────┘
                 ↓
┌─────────────────────────────────────────┐
│  Yokan Layer (Game-Specific)            │
│  - WingedBean.Plugins.*                 │
│  - WingedBean.Gameplay.*                │
└─────────────────────────────────────────┘
```

### Multi-Language Stack
- **Primary:** .NET 8.0 (console), .NET Standard 2.1 (plugins)
- **Secondary:** Node.js (PTY service, web)
- **Future:** Python (utilities), Unity/Godot (hosts)

---

## Build & Run

### Build
```bash
cd /path/to/winged-bean/build
task build-dotnet
```

### Run
```bash
cd _artifacts/latest/dotnet/console/bin
./Dungeon.Host.Console
```

### Run with Recording
```bash
./Dungeon.Host.Console --record --record-duration=30 --record-session=test
```

### Test
```bash
cd /path/to/winged-bean/build
task test
```

**See [IMPLEMENTATION_GUIDE.md](IMPLEMENTATION_GUIDE.md) for detailed workflows**

---

## For Implementation Agents

**START HERE:**

1. Read [CURRENT_STATE.md](CURRENT_STATE.md)
2. Read [GAP_ANALYSIS.md](GAP_ANALYSIS.md)
3. Read [IMPLEMENTATION_GUIDE.md](IMPLEMENTATION_GUIDE.md)
4. Implement Phase 1 RFCs in order:
   - [RFC-0001](rfcs/phase-1-foundation/RFC-0001-environment-tilemap-system.md) - Environment/Tilemap
   - [RFC-0002](rfcs/phase-1-foundation/RFC-0002-camera-integration.md) - Camera Integration
   - [RFC-0003](rfcs/phase-1-foundation/RFC-0003-field-of-view-system.md) - FOV System
   - RFC-0004 (TODO) - Dungeon Generation Integration

**All Phase 1 RFCs are critical and blocking. Implement them first.**

---

## Related Documents

- **Workspace Rules:** `../../../.agent/local/overrides.md`
- **Project Rules:** `../.agent/local/overrides.md`
- **Enhancement Plan:** `../development/dotnet/DUNGEON_CRAWLER_ENHANCEMENT_PLAN.md`
- **Game README:** `../README.md`

---

## Contributing

This is a yokan-project (game-specific). Framework components go in plate-projects.

- **Plate Projects:** `../../../plate-projects/` (reusable libraries)
- **Yokan Projects:** `../` (game-specific implementations)

---

## Version Information

**WingedBean Version:** 1.0.0 (in development)
**Documentation Version:** 1.0
**Last Updated:** 2025-10-15

---

**Status:** Foundation ~60% complete, Core Gameplay ~30% complete

**Next Milestone:** Phase 1 Foundation complete (RFCs 0001-0004)

**See [GAP_ANALYSIS.md](GAP_ANALYSIS.md) for detailed roadmap**

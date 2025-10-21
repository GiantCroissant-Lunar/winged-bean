# WingedBean - Gap Analysis

**Date:** 2025-10-15
**Version:** 1.0.0
**Purpose:** Identify missing features and prioritize implementation

---

## Summary

WingedBean has a strong technical foundation (~60% complete) but is missing critical gameplay systems (~30% complete). This document categorizes gaps into three tiers based on priority and dependency chains.

**Completion Metrics:**
- **Technical Infrastructure:** 60% ✅
- **Gameplay Features:** 30% ⚠️
- **Content Systems:** 10% ❌

---

## Gap Categories

### Tier 1: Critical Foundation (Blocking)
These gaps prevent core gameplay from functioning. Must be implemented first.

### Tier 2: Core Gameplay (High Priority)
These gaps prevent the game from being playable. Required for MVP.

### Tier 3: Content & Polish (Medium Priority)
These gaps limit content and player experience. Required for full release.

---

## TIER 1: Critical Foundation (Blocking)

### 1. Environment/Tilemap System ❌ **CRITICAL**

**Status:** Not Implemented
**Blocks:** All spatial gameplay, rendering, collision, FOV
**Effort:** 2-3 weeks
**Priority:** P0 - Highest

**Missing:**
- Dungeon map data structure (2D grid per floor)
- Tile types (floor, wall, door, stairs, trap, etc.)
- Tile properties (walkable, transparent, explored, visible)
- Collision detection against tiles
- Door mechanics (open/closed/locked/keys)
- Trap mechanics (spike, arrow, poison, teleport, alarm)
- Environmental effects (lava, water, ice, chasm)
- Stairs (up/down between floors)

**Current Situation:**
- Entities float in empty space
- No walls, floors, or obstacles
- No spatial structure
- Blocking component exists but unused

**Proposed Solution:**
- Create `WingedBean.Gameplay.Environment` project
- Define tile data structures
- Implement environment system
- Integrate with existing Movement and Render systems

**Dependencies:**
- None (foundational)

**RFC:** `rfcs/0001-environment-tilemap-system.md`

---

### 2. Camera Integration ⚠️ **CRITICAL**

**Status:** Plate layer exists (camera-bioca), not integrated
**Blocks:** Proper viewport rendering, large maps, player tracking
**Effort:** 1 week
**Priority:** P0 - Highest

**Missing:**
- Camera position tracking player
- Viewport boundaries calculation
- World-to-screen coordinate transformation
- Smooth camera movement
- Map bounds clamping
- Camera modes (follow, free, cinematic)

**Current Situation:**
- RenderSystem uses fixed 40x15 viewport
- No camera tracking
- Cannot support maps larger than viewport
- camera-bioca RFC complete but not implemented or integrated

**Proposed Solution:**
- Implement camera-bioca (Plate layer)
- Create WingedBean.Gameplay.Camera integration
- Update RenderSystem to use camera
- Add Camera ECS component

**Dependencies:**
- Environment/Tilemap (for map bounds)

**RFC:** `rfcs/0002-camera-integration.md`

---

### 3. Field of View (FOV) System ❌ **CRITICAL**

**Status:** perception-sensor in planning, Gameplay.Perception partial
**Blocks:** Visibility, stealth, exploration, fog of war
**Effort:** 2-3 weeks
**Priority:** P0 - Highest

**Missing:**
- Shadowcasting algorithm (FOV calculation)
- Fog of war (explored vs visible)
- Vision component (range, type)
- Light source component (torches, spells)
- Visibility updates per frame
- Entity visibility culling

**Current Situation:**
- All tiles and entities always visible
- No fog of war
- No line of sight
- perception-sensor (Plate layer) in planning phase
- Gameplay.Perception has components but no implementation

**Proposed Solution:**
- Complete perception-sensor (Plate layer) - Shadowcasting algorithm
- Implement FOVSystem in WingedBean.Gameplay.Perception
- Add Vision and LightSource components
- Integrate with Environment and Render systems

**Dependencies:**
- Environment/Tilemap (tile transparency)
- Camera (visibility culling)

**RFC:** `rfcs/0003-field-of-view-system.md`

---

### 4. Dungeon Generation Integration ⚠️ **CRITICAL**

**Status:** edg-rogqd exists (Plate layer), partial integration
**Blocks:** Procedural content, replayability
**Effort:** 2 weeks
**Priority:** P0 - Highest

**Missing:**
- Integration between edg-rogqd and WingedBean.Gameplay.Map
- Dungeon generation system in gameplay loop
- Floor generation on game start
- Conversion from edg-rogqd output to WingedBean tile format
- Spawn point placement (player, enemies, items)
- Stair placement (up/down connections)

**Current Situation:**
- edg-rogqd exists with graph-based generation
- WingedBean.Gameplay.Map has partial structure
- No active dungeon generation
- Hardcoded entity spawning

**Proposed Solution:**
- Create DungeonGenerationSystem
- Integrate edg-rogqd GraphBasedGeneratorGrid2D
- Convert edg-rogqd layouts to WingedBean tiles
- Add floor progression logic

**Dependencies:**
- Environment/Tilemap (target format)

**RFC:** `rfcs/0004-dungeon-generation-integration.md`

---

## TIER 2: Core Gameplay (High Priority)

### 5. Pathfinding System ❌ **HIGH**

**Status:** Not Implemented
**Blocks:** Intelligent enemy AI, player auto-move
**Effort:** 1-2 weeks
**Priority:** P1 - High

**Missing:**
- A* pathfinding algorithm
- Pathfinding component (current path, target)
- PathfindingSystem (calculate and update paths)
- Integration with AISystem
- Obstacle avoidance
- Dynamic path recalculation

**Current Situation:**
- Enemies move directly toward player (no obstacle avoidance)
- No pathfinding
- AI gets stuck on walls

**Proposed Solution:**
- Implement PathfindingSystem using modern-satsuma
- Add Pathfinding component
- Update AISystem to use paths
- Support dynamic replanning

**Dependencies:**
- Environment/Tilemap (walkability data)

**RFC:** `rfcs/0005-pathfinding-system.md`

---

### 6. Spawn System ❌ **HIGH**

**Status:** Not Implemented
**Blocks:** Dynamic enemy/item placement
**Effort:** 1 week
**Priority:** P1 - High

**Missing:**
- Spawn system for enemies and items
- Difficulty scaling per floor
- Enemy budget calculation
- Item/loot distribution
- Spawn point validation (safe distances, valid tiles)
- Boss/treasure room special spawning

**Current Situation:**
- EntityFactory exists but hardcoded usage
- No dynamic spawning
- No difficulty scaling

**Proposed Solution:**
- Create SpawnSystem
- Define spawn tables (JSON data)
- Implement difficulty curve
- Integrate with dungeon generation

**Dependencies:**
- Environment/Tilemap (spawn locations)
- Dungeon Generation (room types)

**RFC:** `rfcs/0006-spawn-system.md`

---

### 7. Inventory Integration ⚠️ **HIGH**

**Status:** inventory-poki exists (Plate layer), not integrated
**Blocks:** Item collection, equipment, consumables
**Effort:** 1-2 weeks
**Priority:** P1 - High

**Missing:**
- Integration with WingedBean gameplay
- Inventory UI (or text-based display)
- Item pickup/drop actions
- Equipment system (weapon, armor slots)
- Consumable usage (potions, scrolls)
- Item stats affecting player stats

**Current Situation:**
- inventory-poki exists with full inventory logic
- Item component exists in DungeonGame
- No inventory system in gameplay
- No item interaction

**Proposed Solution:**
- Create WingedBean.Gameplay.Inventory
- Integrate inventory-poki
- Add InventoryComponent to player/enemies
- Implement pickup/drop/use actions
- Create inventory UI/display

**Dependencies:**
- Input system (for inventory actions)

**RFC:** `rfcs/0007-inventory-integration.md`

---

### 8. Loot System ❌ **HIGH**

**Status:** Not Implemented
**Blocks:** Item rewards, progression incentive
**Effort:** 1-2 weeks
**Priority:** P1 - High

**Missing:**
- Loot tables (JSON definitions)
- Rarity tiers (common, uncommon, rare, epic, legendary)
- Drop rate calculations
- Enemy-specific loot tables
- Chest/container loot
- Procedural item generation (stats, modifiers)
- Loot distribution algorithm (balance)

**Current Situation:**
- Item component exists
- No loot generation
- No drop system

**Proposed Solution:**
- Create LootSystem
- Define loot tables (JSON)
- Implement drop rate algorithm
- Add loot to enemy death
- Support containers/chests

**Dependencies:**
- Inventory Integration (where loot goes)
- Spawn System (chest placement)

**RFC:** `rfcs/0008-loot-system.md`

---

### 9. Advanced Combat ⚠️ **HIGH**

**Status:** Basic melee exists, needs enhancement
**Blocks:** Combat variety, strategic depth
**Effort:** 2-3 weeks
**Priority:** P1 - High

**Missing:**
- Ranged combat (bows, spells, thrown weapons)
- Magic/spell system
- Status effects (poison, stun, bleed, burn, freeze, slow)
- Damage types (physical, fire, ice, lightning, poison)
- Resistances and vulnerabilities
- Critical hits and misses
- Area of effect (AOE) attacks
- Combat formulas (damage calculation)

**Current Situation:**
- CombatSystem exists with basic melee
- Stats exist (Str, Dex, Int, Def)
- Only adjacent melee attacks
- No damage types or status effects

**Proposed Solution:**
- Extend CombatSystem
- Add RangedCombatSystem
- Add MagicSystem
- Add StatusEffectSystem
- Define damage formulas
- Add combat components (Weapon, Spell, StatusEffect)

**Dependencies:**
- FOV (for ranged line-of-sight)
- Inventory (for weapons/spells)

**RFC:** `rfcs/0009-advanced-combat.md`

---

### 10. Enhanced AI ⚠️ **HIGH**

**Status:** Basic AI exists, needs enhancement
**Blocks:** Challenging gameplay, tactical depth
**Effort:** 2-3 weeks
**Priority:** P1 - High

**Missing:**
- AI personalities (aggressive, defensive, cowardly, cunning)
- Group tactics (flanking, coordinated attacks)
- Ranged combat AI (maintain distance, use cover)
- Patrol routes and waypoints
- AI memory (last seen player position)
- State machine complexity (more states)
- Ability usage AI (spells, special attacks)

**Current Situation:**
- AISystem exists with basic states (Idle, Patrol, Chase, Attack, Flee)
- Simple aggro and chase behavior
- No pathfinding integration
- No group coordination

**Proposed Solution:**
- Extend AISystem
- Add AIMemory component
- Add AIPersonality component
- Implement group tactics
- Integrate with Pathfinding
- Add ability usage logic

**Dependencies:**
- Pathfinding (intelligent movement)
- Advanced Combat (ranged, abilities)
- FOV (for AI vision)

**RFC:** `rfcs/0010-enhanced-ai.md`

---

## TIER 3: Content & Polish (Medium Priority)

### 11. Progression System ❌ **MEDIUM**

**Status:** Stats exist, no progression mechanics
**Blocks:** Character growth, player motivation
**Effort:** 2 weeks
**Priority:** P2 - Medium

**Missing:**
- Experience gain formula
- Level-up mechanics
- Stat point allocation
- Skill trees or ability unlocking
- Progression UI/notifications
- Class system (optional)
- Talent/perk system

**Current Situation:**
- Stats component has Level and Experience fields
- No leveling logic
- No skill system

**Proposed Solution:**
- Create ProgressionSystem
- Define XP curve
- Add level-up logic
- Create skill/ability trees (JSON data)
- Add progression UI

**Dependencies:**
- Combat (XP from kills)
- Quest System (XP from quests)

**RFC:** `rfcs/0011-progression-system.md`

---

### 12. Quest System ❌ **MEDIUM**

**Status:** IQuestService contract exists, no implementation
**Blocks:** Structured objectives, story progression
**Effort:** 2-3 weeks
**Priority:** P2 - Medium

**Missing:**
- Quest definitions (JSON)
- Quest types (kill, collect, explore, escort, deliver)
- Objective tracking
- Quest state management (available, active, completed, failed)
- Quest rewards (XP, items, gold)
- Quest chains and branching
- Quest UI/journal

**Current Situation:**
- IQuestService interface defined
- No implementation
- No quest data

**Proposed Solution:**
- Implement QuestService (IQuestService)
- Create quest data format (JSON)
- Add QuestComponent
- Create QuestSystem
- Add quest UI/journal

**Dependencies:**
- Inventory (quest item rewards)
- Progression (XP rewards)

**RFC:** `rfcs/0012-quest-system.md`

---

### 13. Save/Load System ❌ **MEDIUM**

**Status:** Not Implemented
**Blocks:** Persistence, death penalty, session continuity
**Effort:** 1-2 weeks
**Priority:** P2 - Medium

**Missing:**
- Save game serialization
- World state persistence (map, entities, player)
- Inventory serialization
- Quest state serialization
- Multiple save slots
- Auto-save functionality
- Load game on startup
- Death handling (permadeath vs respawn)

**Current Situation:**
- No persistence
- Game state lost on exit
- No save/load functionality

**Proposed Solution:**
- Create SaveSystem
- Define save format (JSON or binary)
- Serialize all game state
- Add save/load UI
- Implement auto-save

**Dependencies:**
- All major systems (to serialize their state)

**RFC:** `rfcs/0013-save-load-system.md`

---

### 14. Dialogue/Narrative System ❌ **MEDIUM**

**Status:** Not Implemented
**Blocks:** Story, NPC interaction, tutorials
**Effort:** 2-3 weeks
**Priority:** P2 - Medium

**Missing:**
- Dialogue system
- Conversation trees
- Branching narratives
- NPC interaction beyond combat
- Dialogue UI (or text display)
- Dialogue scripting (JSON or custom format)
- Voice/bark system (short NPC comments)

**Current Situation:**
- No dialogue
- No NPC interaction
- No story delivery mechanism

**Proposed Solution:**
- Create DialogueSystem
- Define dialogue format (JSON, Yarn, Ink, or custom)
- Add NPC interaction
- Create dialogue UI
- Add dialogue triggers

**Dependencies:**
- Input system (dialogue choices)
- Quest system (dialogue as quest triggers)

**RFC:** `rfcs/0014-dialogue-narrative-system.md`

---

### 15. Minimap ❌ **MEDIUM**

**Status:** Not Implemented
**Blocks:** Navigation, spatial awareness
**Effort:** 1 week
**Priority:** P2 - Medium

**Missing:**
- Minimap rendering
- Explored area tracking (persistent)
- Current floor display
- Stair/POI markers
- Minimap UI placement
- Zoom levels

**Current Situation:**
- No minimap
- No persistent exploration tracking
- Fog of war not persistent

**Proposed Solution:**
- Create MinimapSystem
- Add persistent exploration data
- Render minimap in UI
- Add POI markers

**Dependencies:**
- FOV (for explored tiles)
- Camera (for current view)
- Environment (for map data)

**RFC:** `rfcs/0015-minimap-system.md`

---

### 16. Particle/VFX System ❌ **LOW**

**Status:** Not Implemented
**Blocks:** Visual feedback, game feel
**Effort:** 2-3 weeks
**Priority:** P3 - Low

**Missing:**
- Particle system (explosions, magic, blood)
- Animation system (attack animations, movement)
- Visual effects for abilities
- Screen shake
- Flash effects
- Damage numbers display

**Current Situation:**
- No visual effects
- Static rendering
- No animation

**Proposed Solution:**
- Create ParticleSystem (text-based particles)
- Add animation components
- Create VFX definitions (JSON)
- Integrate with combat and magic

**Dependencies:**
- Render system (for particle display)
- Combat/Magic (for VFX triggers)

**RFC:** `rfcs/0016-particle-vfx-system.md`

---

### 17. Audio Integration ⚠️ **LOW**

**Status:** Audio plugin exists, no gameplay integration
**Blocks:** Sound effects, music, audio feedback
**Effort:** 1-2 weeks
**Priority:** P3 - Low

**Missing:**
- Sound effect definitions (JSON)
- Music tracks and playlists
- Audio event system (combat hit, footstep, door open, etc.)
- Spatial audio (distance-based volume)
- Audio triggers from gameplay events

**Current Situation:**
- WingedBean.Plugins.Audio exists (LibVLC)
- No audio integration with gameplay
- No sound effects or music

**Proposed Solution:**
- Create AudioManager
- Define audio events
- Integrate with combat, movement, environment
- Add music system (background, combat, boss)

**Dependencies:**
- Combat (combat sounds)
- Environment (door sounds, footsteps)

**RFC:** `rfcs/0017-audio-integration.md`

---

## Plate Layer Gaps (New Projects Needed)

### 1. **Pathfinding Library** (New Plate Project)

**Proposed Name:** `pathfinding-astar` or `pathfinding-route`
**Reason:** Reusable pathfinding for any grid-based game
**Effort:** 1-2 weeks
**Priority:** HIGH

**Features:**
- A* algorithm
- Dijkstra algorithm
- Grid-based pathfinding
- Generic cost function
- Heuristic function support
- Path smoothing (optional)

**Consumers:** WingedBean, future games

**RFC:** `rfcs/plate/0001-pathfinding-library.md`

---

### 2. **Status Effect System** (New Plate Project)

**Proposed Name:** `status-effect` or `buff-debuff`
**Reason:** Reusable status effect system for RPGs
**Effort:** 1-2 weeks
**Priority:** MEDIUM

**Features:**
- Status effect definitions
- Buff/debuff system
- Timed effects
- Stackable effects
- Effect interactions (cleanse, immunity)
- Visual indicators

**Consumers:** WingedBean combat, future RPGs

**RFC:** `rfcs/plate/0002-status-effect-system.md`

---

### 3. **Progression/XP System** (New Plate Project)

**Proposed Name:** `progression-level`
**Reason:** Reusable leveling for RPGs
**Effort:** 1 week
**Priority:** MEDIUM

**Features:**
- XP curve definitions
- Level calculation
- Stat scaling
- Progression events

**Consumers:** WingedBean, future RPGs

**RFC:** `rfcs/plate/0003-progression-library.md`

---

### 4. **Loot Table System** (New Plate Project)

**Proposed Name:** `loot-table`
**Reason:** Reusable loot generation
**Effort:** 1-2 weeks
**Priority:** MEDIUM

**Features:**
- Loot table definitions (JSON)
- Rarity system
- Drop rate calculations
- Procedural item generation
- Weighted random selection

**Consumers:** WingedBean, future games

**RFC:** `rfcs/plate/0004-loot-table-library.md`

---

### 5. **Quest System** (New Plate Project)

**Proposed Name:** `quest-system` or `quest-graph`
**Reason:** Reusable quest framework
**Effort:** 2-3 weeks
**Priority:** MEDIUM

**Features:**
- Quest definitions (JSON)
- Objective tracking
- Quest state machine
- Quest chains
- Branching quests
- Quest rewards

**Consumers:** WingedBean, future RPGs/adventure games

**RFC:** `rfcs/plate/0005-quest-system-library.md`

---

### 6. **Dialogue System** (New Plate Project)

**Proposed Name:** `dialogue-tree` or `narrative-flow`
**Reason:** Reusable dialogue for narrative games
**Effort:** 2-3 weeks
**Priority:** LOW

**Features:**
- Dialogue tree definitions
- Branching conversations
- Variables and conditions
- Integration with quest system
- Localization support

**Consumers:** WingedBean, future narrative games

**RFC:** `rfcs/plate/0006-dialogue-system-library.md`

---

## Priority Matrix

### Must Have (P0) - Foundation
1. ✅ Environment/Tilemap System
2. ✅ Camera Integration
3. ✅ FOV System
4. ✅ Dungeon Generation Integration

### Should Have (P1) - Core Gameplay
5. ✅ Pathfinding System (+ Plate project)
6. ✅ Spawn System
7. ✅ Inventory Integration
8. ✅ Loot System (+ Plate project)
9. ✅ Advanced Combat (+ Plate project for status effects)
10. ✅ Enhanced AI

### Could Have (P2) - Content
11. Progression System (+ Plate project)
12. Quest System (+ Plate project)
13. Save/Load System
14. Dialogue System (+ Plate project)
15. Minimap

### Nice to Have (P3) - Polish
16. Particle/VFX System
17. Audio Integration

---

## Implementation Phases

### Phase 1: Foundation (4-6 weeks) - **CRITICAL**
- RFC-0001: Environment/Tilemap System
- RFC-0002: Camera Integration
- RFC-0003: FOV System
- RFC-0004: Dungeon Generation Integration

**Goal:** Playable environment with visibility and navigation

---

### Phase 2: Core Gameplay (4-6 weeks) - **MVP**
- RFC-0005: Pathfinding System
- RFC-Plate-0001: Pathfinding Library (Plate)
- RFC-0006: Spawn System
- RFC-0007: Inventory Integration
- RFC-0008: Loot System
- RFC-Plate-0004: Loot Table Library (Plate)

**Goal:** Complete gameplay loop (explore, fight, loot)

---

### Phase 3: Combat & AI (3-4 weeks) - **MVP+**
- RFC-0009: Advanced Combat
- RFC-Plate-0002: Status Effect System (Plate)
- RFC-0010: Enhanced AI

**Goal:** Engaging combat and challenging AI

---

### Phase 4: Progression & Content (4-6 weeks) - **RELEASE**
- RFC-0011: Progression System
- RFC-Plate-0003: Progression Library (Plate)
- RFC-0012: Quest System
- RFC-Plate-0005: Quest System Library (Plate)
- RFC-0013: Save/Load System

**Goal:** Long-term progression and replayability

---

### Phase 5: Polish (2-4 weeks) - **POST-RELEASE**
- RFC-0014: Dialogue/Narrative System
- RFC-Plate-0006: Dialogue System Library (Plate)
- RFC-0015: Minimap
- RFC-0016: Particle/VFX System
- RFC-0017: Audio Integration

**Goal:** Enhanced player experience

---

## Metrics

### Current State
- **Technical Foundation:** 60% (strong)
- **Gameplay Systems:** 30% (basic)
- **Content Systems:** 10% (minimal)

### After Phase 1 (Foundation)
- **Technical Foundation:** 90%
- **Gameplay Systems:** 45%
- **Content Systems:** 10%

### After Phase 2 (Core Gameplay)
- **Technical Foundation:** 95%
- **Gameplay Systems:** 70%
- **Content Systems:** 20%

### After Phase 3 (Combat & AI)
- **Technical Foundation:** 95%
- **Gameplay Systems:** 85%
- **Content Systems:** 20%

### After Phase 4 (Progression & Content)
- **Technical Foundation:** 95%
- **Gameplay Systems:** 90%
- **Content Systems:** 60%

### After Phase 5 (Polish)
- **Technical Foundation:** 100%
- **Gameplay Systems:** 95%
- **Content Systems:** 85%

---

## Risk Assessment

### High Risk
- **FOV Implementation** - Complex algorithm, performance critical
- **Dungeon Generation Integration** - Data format conversion
- **AI Pathfinding** - Performance with many entities

### Medium Risk
- **Save/Load** - Comprehensive state serialization
- **Quest System** - Complex state management
- **Advanced Combat** - Balance and feel

### Low Risk
- **Inventory Integration** - Well-defined Plate library
- **Camera Integration** - Well-defined Plate library
- **Minimap** - Simple rendering extension

---

## Dependencies Graph

```
Environment/Tilemap ────┬────────────────────┬──────────┬─────────────┐
                        │                    │          │             │
                        ↓                    ↓          ↓             ↓
                    Camera ────┐         Dungeon    Pathfinding    FOV
                        │      │         Generation     │            │
                        ↓      ↓             │          ↓            │
                    Render  Minimap         │      Enhanced AI       │
                                             │          │            │
                                             ↓          ↓            ↓
                                          Spawn ──→ Combat ←─── Perception
                                             │          │
                                             ↓          ↓
                                          Loot ←─── Inventory
                                                       │
                                                       ↓
                                                  Progression
                                                       │
                                                       ↓
                                                    Quests
                                                       │
                                                       ↓
                                                   Save/Load
```

---

**Document Version:** 1.0
**Last Updated:** 2025-10-15
**See Also:** `CURRENT_STATE.md`, `docs/rfcs/`

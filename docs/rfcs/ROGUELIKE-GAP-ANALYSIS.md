---
title: Roguelike Dungeon Crawler - Gap Analysis
type: analysis
status: complete
date: 2025-10-14
---

# Roguelike Dungeon Crawler - Gap Analysis

**Date:** 2025-10-14  
**Status:** Analysis Complete

---

## Executive Summary

Analysis of existing RFCs and code reveals **good FOV foundation** (GoRogue integration exists) but **missing critical roguelike features** for a complete dungeon crawler experience. This document identifies gaps and proposes new RFCs.

---

## Current State Assessment

### ✅ What Exists

**Core Systems:**
- RFC-0007: Arch ECS Integration ✅ Implemented
- RFC-0043: Tile-Based Map System ⏭️ In Progress (other agent)
- RFC-0044: Procedural Dungeon Generation ⏭️ In Progress (other agent)
- RFC-0045: Plate Library Integration ✅ Implemented
- RFC-0046: Turn-Based Game Loop ⏭️ Proposed (this session)
- RFC-0047: Enhanced Movement & Pathfinding ⏭️ Proposed (this session)
- RFC-0048: Inventory & Item Management ⏭️ Proposed (this session)

**FOV/Visibility (Partial):**
- `GoRogueFOVSystem` - Recursive shadowcasting FOV ✅
- `FOVComponent` - Vision range, dirty flag ✅
- `MapMemoryComponent` - Explored/visible tiles ✅
- `LightSourceComponent` - Dynamic lighting (defined but unused) ⚠️

**Rendering (Basic):**
- `RenderSystem` - Simple text buffer rendering ⚠️
- Fixed viewport (40x15) - **No camera system** ❌
- No fog of war rendering ❌
- No lighting effects ❌

---

## Critical Missing Features

### 🎥 1. Camera/Viewport System ❌ **HIGH PRIORITY**

**Problem:**
- `RenderSystem` has hardcoded 40x15 viewport
- No camera following player
- No smooth scrolling
- Can't handle maps larger than viewport
- No zoom/pan support

**Impact:** 
- Player can't explore large procedurally generated dungeons
- Poor UX when moving near edges
- Can't see full tactical situation

**Needed:**
- Camera component with position, viewport size
- Camera follow modes (center on player, edge scrolling, free camera)
- Viewport culling (only render visible tiles)
- Smooth camera transitions

---

### 🌫️ 2. Fog of War Rendering ❌ **HIGH PRIORITY**

**Problem:**
- FOV system calculates visibility ✅
- But `RenderSystem` doesn't use FOV data ❌
- All tiles render at full brightness
- No visual distinction between:
  - Visible tiles (in FOV)
  - Explored tiles (seen before, now dark)
  - Unexplored tiles (never seen, black)

**Impact:**
- No sense of exploration
- Can see entire map at once (breaks roguelike feel)
- No tactical advantage from vision

**Needed:**
- Fog of war rendering modes
- Tile brightness based on visibility
- "Memory" rendering for explored but not visible tiles

---

### 💡 3. Dynamic Lighting System ❌ **MEDIUM PRIORITY**

**Problem:**
- `LightSourceComponent` exists but unused
- No light propagation
- No light blending from multiple sources
- No colored lighting

**Impact:**
- Torches, lanterns, spells don't emit light
- Dark dungeons not possible
- No atmospheric lighting effects

**Needed:**
- Light propagation system
- Light intensity falloff
- Multiple light source blending
- Integration with FOV

---

### 🎯 4. Targeting/Cursor System ❌ **MEDIUM PRIORITY**

**Problem:**
- No way to target enemies for ranged attacks
- No "look" mode to examine tiles/entities
- No area-of-effect targeting

**Impact:**
- Can't implement ranged combat
- Can't implement spells with targeting
- Poor UX for examining dungeon

**Needed:**
- Cursor component
- Targeting modes (single, line, area, cone)
- Visual feedback for valid/invalid targets
- Range checking

---

### 📊 5. HUD/UI Overlay System ❌ **HIGH PRIORITY**

**Problem:**
- No health/mana bars
- No message log
- No minimap
- No status effects display
- No equipment quick view

**Impact:**
- Player can't see critical information
- No feedback on actions
- Poor game feel

**Needed:**
- HUD layout system
- Health/mana/stamina bars
- Message log with color coding
- Status effect icons
- Equipment slots display
- Minimap (optional)

---

### ⚔️ 6. Combat Feedback System ❌ **MEDIUM PRIORITY**

**Problem:**
- `CombatSystem` exists but no visual feedback
- No damage numbers
- No hit/miss indicators
- No combat animations (even simple ones)

**Impact:**
- Combat feels flat
- Can't tell what's happening
- No juice/polish

**Needed:**
- Damage number pop-ups
- Hit effects (flash, shake)
- Status effect particles
- Combat log messages

---

### 🎲 7. Status Effects System ❌ **MEDIUM PRIORITY**

**Problem:**
- No buffs/debuffs
- No poison, burning, frozen, etc.
- No temporary stat modifiers
- No duration tracking

**Impact:**
- Limited tactical depth
- No interesting item/spell effects
- Boring combat

**Needed:**
- Status effect component
- Effect types (buff, debuff, DoT, control)
- Duration and tick system
- Stacking rules
- Visual indicators

---

### 🎵 8. Audio/Sound System ❌ **LOW PRIORITY**

**Problem:**
- No sound effects
- No music
- No audio feedback

**Impact:**
- Less immersive
- No audio cues for events

**Needed:**
- Sound effect system
- Music system
- Audio event triggers
- Volume controls

---

### 💾 9. Save/Load System ❌ **MEDIUM PRIORITY**

**Problem:**
- No save game functionality
- No permadeath handling
- No game state serialization

**Impact:**
- Can't save progress
- Can't implement roguelike permadeath properly
- No session persistence

**Needed:**
- Game state serialization
- Save/load UI
- Multiple save slots
- Auto-save on death (for morgue files)

---

### 📈 10. Character Progression ❌ **LOW PRIORITY**

**Problem:**
- Stats exist but no leveling
- No experience system
- No skill trees
- No character classes

**Impact:**
- No long-term progression
- Limited replayability

**Needed:**
- Experience and leveling
- Stat point allocation
- Skills/abilities system
- Character classes (optional)

---

## Recommended Implementation Priority

### Phase 1: Core Rendering (Week 1)
**Must-have for playable game:**
1. **RFC-0049: Camera and Viewport System** ⭐⭐⭐
2. **RFC-0050: Fog of War Rendering** ⭐⭐⭐
3. **RFC-0051: HUD and UI Overlay System** ⭐⭐⭐

### Phase 2: Combat & Interaction (Week 2)
**Makes combat interesting:**
4. **RFC-0052: Targeting and Cursor System** ⭐⭐
5. **RFC-0053: Combat Feedback and Visual Effects** ⭐⭐
6. **RFC-0054: Status Effects System** ⭐⭐

### Phase 3: Polish & Features (Week 3)
**Nice to have:**
7. **RFC-0055: Dynamic Lighting System** ⭐
8. **RFC-0056: Save/Load System** ⭐
9. Audio System (future)
10. Character Progression (future)

---

## Integration Points

### With Existing Systems

**Camera System integrates with:**
- RenderSystem (viewport culling)
- InputSystem (camera controls)
- Player Position (camera follow)

**Fog of War integrates with:**
- GoRogueFOVSystem (visibility data)
- RenderSystem (tile brightness)
- DungeonMap (explored state)

**HUD System integrates with:**
- Stats component (health/mana display)
- Inventory (equipment display)
- TurnOrder (turn indicator)
- Message log (combat feedback)

**Targeting System integrates with:**
- InputSystem (cursor movement)
- CombatSystem (ranged attacks)
- PathfindingSystem (line of sight)
- RenderSystem (target highlighting)

---

## Technical Considerations

### Performance
- **Viewport culling** critical for large maps (only render visible tiles)
- **FOV caching** - don't recalculate every frame
- **Light map** - pre-calculate light propagation
- **Dirty flags** - only update changed regions

### Architecture
- Keep systems decoupled via events (MessagePipe)
- Use ECS components for all game state
- Separate rendering from game logic
- Support multiple render backends (console, SadConsole, future GUI)

### Testing
- Unit tests for camera calculations
- Visual tests for fog of war
- Integration tests for targeting
- Performance benchmarks for rendering

---

## Next Steps

1. **Review this analysis** with team
2. **Prioritize RFCs** based on project goals
3. **Create RFC-0049** (Camera System) - highest priority
4. **Create RFC-0050** (Fog of War) - highest priority
5. **Create RFC-0051** (HUD System) - highest priority
6. **Prototype** camera and fog of war together
7. **Iterate** based on playtest feedback

---

## References

- Traditional Roguelikes: NetHack, DCSS, Brogue
- Modern Roguelikes: Caves of Qud, Cogmind, Jupiter Hell
- RogueBasin: http://www.roguebasin.com/
- r/roguelikedev: https://www.reddit.com/r/roguelikedev/

---

**Conclusion:** The game has solid ECS foundation and FOV calculation, but needs **camera, fog of war, and HUD** to feel like a proper roguelike. These three systems are critical for Phase 1.

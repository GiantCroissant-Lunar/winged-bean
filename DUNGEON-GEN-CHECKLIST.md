# Dungeon Generation Implementation Checklist

**Started**: 2025-10-14  
**Target**: Phase 1 Complete - Foundation

---

## Phase 1: Foundation ✅ READY TO START

### 1.1 Tile System Components
- [ ] Create `Components/TileType.cs` enum
- [ ] Create `Components/MapTile.cs` struct
- [ ] Create `Components/DungeonMap.cs` class
- [ ] Add unit tests for DungeonMap operations

### 1.2 Movement Integration
- [ ] Update `Systems/MovementSystem.cs` with map collision
- [ ] Add `IsWalkable()` checks before movement
- [ ] Test collision with hardcoded test map

### 1.3 Game Integration
- [ ] Add `_dungeonMap` field to `DungeonGame.cs`
- [ ] Create test map in `Initialize()` method
- [ ] Verify entities respect map boundaries

**Acceptance**: Player cannot walk through walls in test map

---

## Phase 2: EdgRogqd Integration

### 2.1 Generator Core
- [ ] Create `Generation/DungeonGenerator.cs`
- [ ] Create `Generation/DungeonGeneratorOptions.cs`
- [ ] Implement EdgRogqd wrapper
- [ ] Convert LayoutGrid2D to DungeonMap

### 2.2 Room Templates
- [ ] Create `Generation/RoomTemplates.cs`
- [ ] Define 3 room sizes (small, medium, large)
- [ ] Define corridor templates
- [ ] Test template instantiation

### 2.3 Level Description
- [ ] Create `Generation/DungeonLevelDescription.cs`
- [ ] Build room graph
- [ ] Assign templates to rooms
- [ ] Test level description creation

### 2.4 Integration
- [ ] Update `DungeonGame.Initialize()` to call generator
- [ ] Add seed support to `DungeonLevelData`
- [ ] Validate generated maps are connected
- [ ] Test with multiple seeds

**Acceptance**: Generated dungeons are walkable and connected

---

## Phase 3: Rendering

### 3.1 Render System
- [ ] Add `RenderMap()` method to `RenderSystem.cs`
- [ ] Emit render events for tiles
- [ ] Define tile symbols (# . + etc)

### 3.2 Console UI
- [ ] Update `ConsoleDungeonAppRefactored.cs`
- [ ] Subscribe to tile render events
- [ ] Draw map tiles before entities

### 3.3 Desktop UI
- [ ] Update `DungeonGameScreen.cs` (SadConsole)
- [ ] Render tiles with colors
- [ ] Layer tiles behind entities

**Acceptance**: Map visible in both console and desktop

---

## Phase 4: Enhancement (Future)

### 4.1 Pathfinding
- [ ] Integrate ModernSatsuma A*
- [ ] Build navigation graph from tiles
- [ ] Update AISystem with pathfinding

### 4.2 Special Rooms
- [ ] Define boss room template
- [ ] Define treasure room template
- [ ] Add room type metadata

### 4.3 Multi-Floor
- [ ] Add stairs/exit tiles
- [ ] Generate connected floors
- [ ] Level transition logic

---

## Current Status

**Phase**: 1 - Foundation  
**Progress**: 0% (Planning Complete)  
**Blockers**: None  
**Next**: Create MapTile and DungeonMap components

---

## Quick Start Commands

```bash
# Navigate to plugin directory
cd /Users/apprenticegc/Work/lunar-horse/personal-work/yokan-projects/winged-bean/development/dotnet/console/src/plugins/WingedBean.Plugins.DungeonGame

# Create components
mkdir -p Components
touch Components/TileType.cs
touch Components/MapTile.cs  
touch Components/DungeonMap.cs

# Build and test
dotnet build
dotnet test

# Run from artifacts
cd /Users/apprenticegc/Work/lunar-horse/personal-work/yokan-projects/winged-bean/build
task build-dotnet
cd _artifacts/latest/dotnet/console/bin
./ConsoleDungeon.Host
```

---

## Dependencies Verified ✅

- [x] Plate.EdgRogqd - Available
- [x] Plate.ModernSatsuma - Available  
- [x] Plate.GoAP-CKPor - Integrated as plugin
- [x] Plate.Capability-Orlia - Integrated as plugin
- [x] ArchECS - Working
- [x] Resource System - Working

---

**Ready to begin Phase 1 implementation!**

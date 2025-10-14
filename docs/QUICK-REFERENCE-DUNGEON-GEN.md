---
id: quick-reference-dungeon-gen
title: Quick Reference - Dungeon Generation
type: guide
category: documentation
created: 2025-10-14
updated: 2025-10-14
---

# Dungeon Generation - Quick Reference

**Date:** 2025-10-14
**Status:** Ready to Implement Phase 2 (RFC-0043)

---

## TL;DR

We're adding procedurally generated dungeons to WingedBean using:
- **Plate.EdgRogqd** - Graph-based dungeon generator
- **Tile-based map** - 2D grid with walls, floors, doors
- **ECS integration** - Map as component, systems check walkability

---

## Three RFCs

| RFC | Title | Status | Time | What It Does |
|-----|-------|--------|------|--------------|
| **0045** | Plate Libraries | ✅ DONE | 6h | Integrate graph/AI/generation libraries |
| **0043** | Tile Map System | ⏭️ NEXT | 4-6h | Add walkable/blocked tiles |
| **0044** | Proc Gen | 🔮 LATER | 8-12h | Generate dungeons algorithmically |

---

## What's Implemented (RFC-0045)

✅ Plate.ModernSatsuma - Graphs & pathfinding  
✅ Plate.EdgRogqd - Dungeon generation library  
✅ Plate.Plugins.GoapCkpor - AI planning (plugin)  
✅ Plate.Plugins.CapabilityOrlia - Capability checks (plugin)  
✅ Build system copies plugins to artifacts  
✅ Both console & desktop apps load plugins  

---

## What's Next (RFC-0043)

Create 3 files in `WingedBean.Plugins.DungeonGame/Components/`:

**1. TileType.cs** - Enum for tile types
```csharp
public enum TileType { Empty, Floor, Wall, Door, Corridor }
```

**2. MapTile.cs** - Tile data structure
```csharp
public readonly struct MapTile
{
    public TileType Type { get; init; }
    public bool IsWalkable { get; init; }
    public char Symbol { get; init; }
}
```

**3. DungeonMap.cs** - 2D tile grid
```csharp
public class DungeonMap
{
    public int Width { get; }
    public int Height { get; }
    public bool IsWalkable(int x, int y);
    public MapTile GetTile(int x, int y);
}
```

Then update:
- **MovementSystem** - Check `map.IsWalkable()` before moving
- **RenderSystem** - Draw map tiles with symbols (#=wall, .=floor)
- **DungeonGame** - Create test map, attach to world

---

## What's After That (RFC-0044)

Create `Generation/` folder with:

**DungeonGenerator.cs** - Main generator
```csharp
public DungeonMap Generate(DungeonGeneratorOptions options)
{
    // 1. Build room graph
    // 2. Call EdgRogqd to generate layout
    // 3. Convert to DungeonMap tiles
}
```

**RoomTemplates.cs** - Room shapes
```csharp
public static RoomTemplateGrid2D SmallRoom() { /* 5x5 */ }
public static RoomTemplateGrid2D MediumRoom() { /* 8x8 */ }
```

Update level JSON:
```json
{
  "generation": {
    "seed": 12345,
    "rooms": { "min": 5, "max": 10 }
  }
}
```

---

## Key Concepts

**Tile-Based Map**
- 2D array of tiles (`MapTile[width, height]`)
- Each tile has type, walkability, visual symbol
- Movement system checks before allowing moves

**Procedural Generation**
1. Define room graph (nodes=rooms, edges=corridors)
2. EdgRogqd generates layout (positions rooms)
3. Convert layout to tile array
4. Place doors, spawn entities

**Data-Driven**
- Level config in JSON
- Seed-based reproducibility
- Configurable room count, size, etc.

---

## Commands

```bash
# Build with plugins
cd /path/to/winged-bean/build
task build-dotnet

# Run console
cd _artifacts/latest/dotnet/console/bin
./ConsoleDungeon.Host

# Run desktop
cd _artifacts/latest/dotnet/desktop/bin
./Dungeon.Host.SadConsole
```

---

## File Structure

```
WingedBean.Plugins.DungeonGame/
├── Components/
│   ├── TileType.cs              ← RFC-0043
│   ├── MapTile.cs               ← RFC-0043
│   └── DungeonMap.cs            ← RFC-0043
├── Generation/                  ← RFC-0044
│   ├── DungeonGenerator.cs      ← RFC-0044
│   └── RoomTemplates.cs         ← RFC-0044
├── Systems/
│   ├── MovementSystem.cs        ← Update in RFC-0043
│   └── RenderSystem.cs          ← Update in RFC-0043
└── DungeonGame.cs               ← Update in both
```

---

## Testing

**RFC-0043:**
- Unit tests: DungeonMap operations
- Integration: Player collision with walls
- Visual: Map renders correctly

**RFC-0044:**
- Unit tests: Generation algorithms
- Connectivity: All rooms reachable
- Reproducibility: Same seed = same map
- Performance: Generation < 100ms

---

## Resources

**RFCs:**
- `/docs/rfcs/0043-tile-based-map-system.md`
- `/docs/rfcs/0044-procedural-dungeon-generation.md`
- `/docs/rfcs/0045-plate-library-integration.md`

**Planning:**
- `/DUNGEON-GENERATION-PLAN.md`
- `/DUNGEON-GEN-CHECKLIST.md`
- `/DUNGEON-GEN-ARCHITECTURE.md`

**Libraries:**
- Plate.EdgRogqd: `/plate-projects/edg-rogqd`
- Plate.ModernSatsuma: `/plate-projects/modern-satsuma`

---

## Timeline

- ✅ **Week 1:** Planning & RFC-0045 (DONE)
- ⏭️ **Week 2:** RFC-0043 implementation
- 🔮 **Week 3:** RFC-0044 implementation

---

**Status:** Phase 1 Complete, Ready for Phase 2  
**Blockers:** None  
**Next:** Create TileType, MapTile, DungeonMap components

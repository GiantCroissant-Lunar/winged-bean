---
title: Parallel Implementation Plan - RFCs 0046-0051
type: planning
status: active
date: 2025-10-14
---

# Parallel Implementation Plan - RFCs 0046-0051

**Date:** 2025-10-14  
**Status:** Active
**Question:** Can RFC-0046 through RFC-0051 be implemented while other agent works on RFC-0043 to RFC-0045?

---

## TL;DR Answer

**YES - with strategic ordering!** 
**YES - with strategic ordering!** ✅

- **3 RFCs can start immediately** (no dependencies)
- **3 RFCs need map basics** (can start after minimal map interface)
- **Coordination point:** Simple `DungeonMap` interface agreement

---

## Dependency Analysis

### RFC-0043: Tile-Based Map System (Other Agent)
**Provides:**
- `DungeonMap` class with `Width`, `Height`, `Tiles[,]`
- `MapTile` struct with `IsWalkable`, `IsTransparent`, `Glyph`
- `TileType` enum (Floor, Wall, Door, etc.)
- Basic `IsInBounds()`, `GetTile()`, `SetTile()` methods

**Critical for:**
- RFC-0047 (Movement - needs walkability checks)
- RFC-0049 (Camera - needs map bounds)
- RFC-0050 (Fog of War - needs tile data)

---

## Implementation Strategy

### ✅ Phase A: Can Start Immediately (No Dependencies)

#### **RFC-0046: Turn-Based Game Loop** 
**Status:** ✅ **START NOW** - Fully independent

**Why independent:**
- Only needs ECS (already exists)
- Works with `Position` component (already exists)
- Works with `Stats` component (already exists)
- Doesn't need map for core turn logic

**What it provides:**
```csharp
// Core components
public struct Energy { ... }
public struct TurnAction { ... }
public class TurnOrder { ... }

// Systems
public class TurnManagerSystem : IECSSystem { ... }
```

**Integration point with map (later):**
```csharp
// In TurnManagerSystem.ProcessMoveAction()
// Can add map validation later when RFC-0043 is ready
var map = world.GetSingleton<DungeonMap>();
if (map != null && !map.IsWalkable(targetX, targetY))
{
    return; // Block movement
}
```

**Recommendation:** ⭐ **Implement fully now**, add map validation hook later

---

#### **RFC-0048: Inventory & Item Management**
**Status:** ✅ **START NOW** - Fully independent

**Why independent:**
- Only needs ECS (already exists)
- Works with existing `Item`, `Stats` components
- Items are entities with `Position` (already exists)
- No map dependency for core inventory logic

**What it provides:**
```csharp
public class Inventory { ... }
public class Equipment { ... }
public struct ItemStats { ... }
public struct Consumable { ... }
public class InventorySystem : IECSSystem { ... }
```

**Integration point with map (later):**
```csharp
// In InventorySystem.DropItem()
// Items placed on ground at player position
world.AddComponent(itemEntity, new Position(pos.X, pos.Y));
// Map validation can be added later
```

**Recommendation:** ⭐ **Implement fully now**, works without map

---

#### **RFC-0051: HUD & UI Overlay**
**Status:** ✅ **START NOW** - Mostly independent

**Why mostly independent:**
- Reads from `Stats`, `Inventory`, `Equipment` components
- Message log is standalone
- Health/mana bars don't need map
- Turn indicator works with turn system

**What it provides:**
```csharp
public class HUDConfig { ... }
public class MessageLog { ... }
public class HUDRenderSystem : IECSSystem { ... }
```

**Minor dependency:**
- Minimap feature needs map (but minimap is optional)

**Recommendation:** ⭐ **Implement now** (skip minimap initially)

---

### ⏸️ Phase B: Needs Minimal Map Interface

#### **RFC-0047: Enhanced Movement & Pathfinding**
**Status:** ⚠️ **NEEDS MAP INTERFACE** - Can start with stub

**Hard dependencies:**
- Needs `DungeonMap.IsWalkable(x, y)` for movement validation
- Needs `DungeonMap.Width`, `Height` for navigation graph
- Needs `DungeonMap.GetTile()` for terrain costs

**Strategy - Option 1: Wait for RFC-0043**
```
Wait → Other agent completes map → Implement RFC-0047
Timeline: +4-6 hours delay
```

**Strategy - Option 2: Stub Interface** ⭐ **RECOMMENDED**
```csharp
// Create minimal stub in your codebase
namespace WingedBean.Plugins.DungeonGame.Components;

/// <summary>
/// Stub DungeonMap interface for development.
/// Will be replaced by RFC-0043 implementation.
/// </summary>
public class DungeonMap
{
    public int Width { get; init; } = 80;
    public int Height { get; init; } = 40;
    
    public bool IsWalkable(int x, int y)
    {
        // Stub: Everything walkable for now
        return IsInBounds(x, y);
    }
    
    public bool IsInBounds(int x, int y)
    {
        return x >= 0 && x < Width && y >= 0 && y < Height;
    }
    
    // Add more methods as needed
}
```

**Timeline:**
- Create stub: 30 minutes
- Implement RFC-0047: 6-8 hours
- Replace stub with real map: 1 hour (merge)

**Recommendation:** ⭐ **Create stub, implement now**, replace later

---

#### **RFC-0049: Camera & Viewport System**
**Status:** ⚠️ **NEEDS MAP BOUNDS** - Can start with stub

**Hard dependencies:**
- Needs `DungeonMap.Width`, `Height` for camera bounds clamping
- Needs `DungeonMap.GetTile()` for rendering tiles

**Strategy - Use same stub as RFC-0047:**
```csharp
// In CameraSystem.ClampToMapBounds()
var map = world.GetSingleton<DungeonMap>();
if (map == null)
    return;

camera.X = Math.Clamp(camera.X, halfWidth, map.Width - halfWidth - 1);
camera.Y = Math.Clamp(camera.Y, halfHeight, map.Height - halfHeight - 1);
```

**Timeline:**
- Use RFC-0047 stub: 0 hours (reuse)
- Implement RFC-0049: 4-6 hours
- Replace stub: 1 hour (merge)

**Recommendation:** ⭐ **Use stub, implement now**, replace later

---

#### **RFC-0050: Fog of War Rendering**
**Status:** ⚠️ **NEEDS MAP TILES** - Can start with stub

**Hard dependencies:**
- Needs `DungeonMap.GetTile()` for tile glyphs
- Needs `DungeonMap.IsInBounds()` for rendering
- Uses existing `GoRogueFOVSystem` (already exists ✅)

**Strategy - Extend stub:**
```csharp
public class DungeonMap
{
    // ... existing stub methods ...
    
    public Tile? GetTile(int x, int y)
    {
        if (!IsInBounds(x, y))
            return null;
        
        // Stub: Return simple floor tile
        return new Tile
        {
            Type = TileType.Floor,
            IsWalkable = true,
            IsTransparent = true,
            Glyph = '.',
            IsExplored = false,
            IsVisible = false
        };
    }
}
```

**Timeline:**
- Extend stub: 30 minutes
- Implement RFC-0050: 3-4 hours
- Replace stub: 1 hour (merge)

**Recommendation:** ⭐ **Extend stub, implement now**, replace later

---

## Coordination Plan

### Week 1: Parallel Development

#### **Other Agent (RFC-0043 to RFC-0045)**
**Days 1-3:**
- RFC-0043: Tile-Based Map System (4-6 hours)
- RFC-0044: Procedural Generation (8-12 hours)
- RFC-0045: Plate Integration (already done ✅)

#### **Your Work (RFC-0046 to RFC-0051)**

**Day 1: Independent RFCs**
- ✅ RFC-0046: Turn-Based System (6-8 hours)
- ✅ RFC-0048: Inventory System (8-10 hours)
- ✅ RFC-0051: HUD System (6-8 hours)

**Day 2: Create Stub Interface**
- Create minimal `DungeonMap` stub (30 min)
- Coordinate interface with other agent (30 min)
- Document stub → real map migration plan (30 min)

**Day 3: Map-Dependent RFCs**
- ✅ RFC-0047: Movement & Pathfinding (6-8 hours) - using stub
- ✅ RFC-0049: Camera System (4-6 hours) - using stub
- ✅ RFC-0050: Fog of War (3-4 hours) - using stub

**Day 4: Integration**
- Replace stub with real `DungeonMap` from RFC-0043
- Test all systems with real procedurally generated maps
- Fix any integration issues

---

## Stub Interface Agreement

### Minimal Interface Contract

Both agents agree on this minimal interface:

```csharp
namespace WingedBean.Plugins.DungeonGame.Components;

/// <summary>
/// Dungeon map interface (RFC-0043).
/// Stub version for parallel development.
/// </summary>
public class DungeonMap
{
    // Dimensions
    public int Width { get; init; }
    public int Height { get; init; }
    public int Floor { get; init; }
    
    // Tile array (can be stub or real)
    public Tile[,] Tiles { get; init; }
    
    // Core methods
    public bool IsInBounds(int x, int y);
    public Tile? GetTile(int x, int y);
    public void SetTile(int x, int y, Tile tile);
    public bool IsWalkable(int x, int y);
    public IEnumerable<(int X, int Y)> GetWalkablePositions();
}

public struct Tile
{
    public TileType Type { get; set; }
    public bool IsWalkable { get; set; }
    public bool IsTransparent { get; set; }
    public bool IsExplored { get; set; }
    public bool IsVisible { get; set; }
    public char Glyph { get; set; }
    // Colors can be added later
}

public enum TileType
{
    Empty = 0,
    Floor = 1,
    Wall = 2,
    Door = 3,
    Corridor = 4
}
```

### Stub Implementation

```csharp
// StubDungeonMap.cs - Temporary for parallel development
public class StubDungeonMap : DungeonMap
{
    public StubDungeonMap(int width, int height)
    {
        Width = width;
        Height = height;
        Floor = 1;
        Tiles = new Tile[width, height];
        
        // Initialize with simple open floor
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                Tiles[x, y] = new Tile
                {
                    Type = TileType.Floor,
                    IsWalkable = true,
                    IsTransparent = true,
                    Glyph = '.'
                };
            }
        }
    }
    
    public bool IsInBounds(int x, int y) =>
        x >= 0 && x < Width && y >= 0 && y < Height;
    
    public Tile? GetTile(int x, int y) =>
        IsInBounds(x, y) ? Tiles[x, y] : null;
    
    public void SetTile(int x, int y, Tile tile)
    {
        if (IsInBounds(x, y))
            Tiles[x, y] = tile;
    }
    
    public bool IsWalkable(int x, int y) =>
        IsInBounds(x, y) && Tiles[x, y].IsWalkable;
    
    public IEnumerable<(int X, int Y)> GetWalkablePositions()
    {
        for (int x = 0; x < Width; x++)
            for (int y = 0; y < Height; y++)
                if (Tiles[x, y].IsWalkable)
                    yield return (x, y);
    }
}
```

---

## Migration Strategy

### When RFC-0043 is Complete

**Step 1: Compare Interfaces**
```bash
# Check if real DungeonMap matches stub
diff StubDungeonMap.cs RealDungeonMap.cs
```

**Step 2: Update References**
```csharp
// Before (using stub)
var map = new StubDungeonMap(80, 40);

// After (using real map from RFC-0043)
var map = new DungeonMap(80, 40);
// Or from procedural generation
var map = dungeonGenerator.Generate(options);
```

**Step 3: Remove Stub**
```bash
# Delete stub file
rm StubDungeonMap.cs

# Update all references to use real DungeonMap
```

**Step 4: Test Integration**
- Run all tests with real map
- Test with procedurally generated dungeons
- Verify fog of war, camera, movement all work

---

## Risk Mitigation

### Risk 1: Interface Mismatch
**Problem:** Real `DungeonMap` has different interface than stub

**Mitigation:**
- ✅ Agree on interface contract upfront (see above)
- ✅ Document interface in both RFCs
- ✅ Regular sync between agents (daily check-in)

### Risk 2: Integration Issues
**Problem:** Systems work with stub but fail with real map

**Mitigation:**
- ✅ Keep stub simple and realistic
- ✅ Write integration tests that work with both
- ✅ Plan 1 day for integration/fixes

### Risk 3: Merge Conflicts
**Problem:** Both agents modify same files

**Mitigation:**
- ✅ Work in different namespaces/files
- ✅ Stub in separate file (`StubDungeonMap.cs`)
- ✅ Use feature branches
- ✅ Coordinate file ownership

---

## Timeline Comparison

### Sequential (Wait for RFC-0043)
```
Day 1-2: Other agent - RFC-0043 (6 hours)
Day 3:   You - RFC-0046 (8 hours)
Day 4:   You - RFC-0047 (8 hours) ← Blocked until Day 2
Day 5:   You - RFC-0048 (10 hours)
Day 6:   You - RFC-0049 (6 hours) ← Blocked until Day 2
Day 7:   You - RFC-0050 (4 hours) ← Blocked until Day 2
Day 8:   You - RFC-0051 (8 hours)

Total: 8 days (50 hours)
```

### Parallel (With Stub)
```
Day 1:   Other agent - RFC-0043 (6 hours)
         You - RFC-0046 + RFC-0048 (18 hours) ✅ Parallel
         
Day 2:   Other agent - RFC-0044 (8 hours)
         You - Create stub (1 hour) + RFC-0047 (8 hours) ✅ Parallel
         
Day 3:   Other agent - RFC-0044 cont. (4 hours)
         You - RFC-0049 + RFC-0050 (10 hours) ✅ Parallel
         
Day 4:   You - RFC-0051 (8 hours)
         Integration & testing (4 hours)

Total: 4 days (50 hours) - 50% faster! 🚀
```

---

## Recommendation

### ✅ **YES - Implement in Parallel with Stub Strategy**

**Immediate Actions:**

1. **Start Now (No Coordination Needed):**
   - RFC-0046: Turn-Based System
   - RFC-0048: Inventory System
   - RFC-0051: HUD System

2. **Coordinate Interface (30 min meeting):**
   - Agree on `DungeonMap` interface contract
   - Document in both RFC-0043 and your RFCs
   - Share stub implementation

3. **Implement with Stub (Day 2-3):**
   - RFC-0047: Movement & Pathfinding
   - RFC-0049: Camera System
   - RFC-0050: Fog of War

4. **Integrate (Day 4):**
   - Replace stub with real map
   - Test everything together
   - Fix integration issues

**Benefits:**
- ⚡ **50% faster** - Parallel development
- 🎯 **Lower risk** - Interface contract prevents mismatch
- 🔄 **Flexible** - Can adjust if other agent's timeline changes
- ✅ **Clean merge** - Minimal conflicts

**Effort:**
- Stub creation: 1 hour
- Integration: 4 hours
- **Total overhead: 5 hours**
- **Time saved: ~2 days (16 hours)**
- **Net benefit: 11 hours saved** 🎉

---

## Conclusion

**Answer: YES!** ✅

You can implement **all 6 RFCs** while the other agent works on RFC-0043 to RFC-0045:

- **3 RFCs immediately** (RFC-0046, RFC-0048, RFC-0051)
- **3 RFCs with stub** (RFC-0047, RFC-0049, RFC-0050)
- **Coordinate interface** (30 min)
- **Integrate later** (4 hours)

This approach is **faster, lower risk, and more efficient** than waiting sequentially.

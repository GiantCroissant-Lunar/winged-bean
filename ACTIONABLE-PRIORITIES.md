# WingedBean: Actionable Implementation Priorities

**Date:** 2025-10-15  
**Status:** What to build next

## TL;DR

Your game already works with:
- ✅ Camera system (Plate.Camera2D)
- ✅ FOV/fog of war (GoRogue)
- ✅ Pathfinding AI (GoRogue)
- ✅ Basic dungeon generation
- ✅ Tilemap/collision

**What's genuinely missing:**
1. Multi-floor dungeons with stairs
2. Doors (open/close/lock)
3. Traps and hazards (optional)

**The RFCs mostly propose replacing GoRogue with custom code - NOT recommended unless you hate external dependencies.**

---

## Priority 1: Multi-Floor Dungeons (HIGH VALUE)

**Why:** Essential for roguelike progression  
**Effort:** 3-5 days  
**Files to modify:**

```
WingedBean.Gameplay.Map/
  ├── Data/
  │   ├── TileType.cs (add StairsUp, StairsDown)
  │   └── FloorMetadata.cs (NEW - floor info)
  ├── Systems/
  │   └── FloorTransitionSystem.cs (NEW - handle stair traversal)
  └── Services/
      └── MapService.cs (extend to manage multiple floors)
```

**What to add:**
1. New tile types: `StairsUp`, `StairsDown`
2. `FloorTransitionSystem` - detect when player on stairs, handle floor change
3. Floor storage - `Dictionary<int, DungeonMap>` to store multiple floors
4. Spawn positioning - place player at correct stairs when changing floors
5. Floor generation on-demand - generate floor when first visited

**Pseudocode:**
```csharp
// In TileType enum
StairsUp,
StairsDown,

// New system
public class FloorTransitionSystem : IECSSystem
{
    public void Execute(IWorld world, float deltaTime)
    {
        var player = GetPlayer(world);
        var tile = GetTileAtPlayer(player);
        
        if (tile.Type == TileType.StairsDown && Input.IsPressed("Descend"))
        {
            TransitionToFloor(currentFloor + 1);
        }
        else if (tile.Type == TileType.StairsUp && Input.IsPressed("Ascend"))
        {
            TransitionToFloor(currentFloor - 1);
        }
    }
}
```

---

## Priority 2: Door System (MEDIUM VALUE)

**Why:** Adds tactical depth and room structure  
**Effort:** 2-3 days  
**Files to modify:**

```
WingedBean.Gameplay.Map/
  ├── Data/
  │   ├── TileType.cs (add DoorClosed, DoorOpen)
  │   └── DoorComponent.cs (NEW - door state)
  ├── Systems/
  │   └── DoorSystem.cs (NEW - door interactions)
  └── Components/
      └── InventoryComponent.cs (for keys, if needed)
```

**What to add:**
1. Door tile types: `DoorClosed`, `DoorOpen`
2. `DoorComponent` - IsOpen, IsLocked, RequiredKeyId
3. `DoorSystem` - handle open/close/unlock actions
4. Door entities - place entities at door tiles
5. Interaction input - 'o' to open door, check for keys if locked

**Pseudocode:**
```csharp
public struct DoorComponent
{
    public bool IsOpen;
    public bool IsLocked;
    public string? RequiredKeyId;
}

public class DoorSystem : IECSSystem
{
    public void Execute(IWorld world, float deltaTime)
    {
        if (Input.IsPressed("OpenDoor"))
        {
            var door = GetAdjacentDoor(player);
            if (door != null && !door.IsOpen)
            {
                if (door.IsLocked && !PlayerHasKey(player, door.RequiredKeyId))
                {
                    ShowMessage("Door is locked!");
                    return;
                }
                
                OpenDoor(door);
                UpdateTile(door.Position, TileType.DoorOpen);
            }
        }
    }
}
```

---

## Priority 3: Trap System (OPTIONAL)

**Why:** Adds danger and variety  
**Effort:** 2-3 days  
**Files to modify:**

```
WingedBean.Gameplay.Map/
  ├── Components/
  │   └── TrapComponent.cs (NEW - trap data)
  └── Systems/
      └── TrapSystem.cs (NEW - trap triggering)
```

**What to add:**
1. `TrapComponent` - TrapType, Damage, IsTriggered, IsVisible
2. `TrapSystem` - detect when entity steps on trap, apply effect
3. Trap types - spike, arrow, poison, teleport
4. Detection mechanics - perception check to reveal hidden traps

---

## DON'T Implement (Already Works via GoRogue)

### ❌ RFC-0057: Camera Integration
**Status:** Already done with Plate.Camera2D  
**Action:** None needed

### ❌ RFC-0058: FOV System (unless you hate GoRogue)
**Status:** Working perfectly via GoRogue FOV  
**Action:** Only replace if you must eliminate external dependencies  
**Effort if replacing:** 1-2 weeks for minimal gameplay value

### ❌ RFC-0060: Pathfinding (unless you hate GoRogue)
**Status:** Working perfectly via GoRogue pathfinding  
**Action:** Only replace if you must eliminate external dependencies  
**Effort if replacing:** 1-2 weeks for minimal gameplay value

---

## Optional Enhancement: Better Dungeon Generation

### Integrate EdgRogqd (OPTIONAL)

**Why:** Current simple room-and-corridor is basic  
**Effort:** 2 weeks  
**Only if:** You want more sophisticated dungeon layouts

**What to do:**
1. Create adapter for `Plate.EdgRogqd`
2. Convert EdgRogqd graph layouts to WingedBean tiles
3. Replace `DungeonGenerator` implementation
4. Keep same interface for compatibility

**Decision point:** Play your game first. If dungeons feel too simple/repetitive, then integrate EdgRogqd. Otherwise, current generator is fine.

---

## Implementation Plan: 1 Week Sprint

**Day 1-3: Multi-Floor Dungeons**
- Add StairsUp/StairsDown tile types
- Create FloorTransitionSystem
- Extend MapService for multiple floors
- Test floor transitions

**Day 4-5: Door System**
- Add DoorClosed/DoorOpen tile types
- Create DoorComponent and DoorSystem
- Add door interaction input
- Test doors in generated dungeons

**Day 6-7: Polish & Test**
- Generate stairs in dungeons
- Place doors in room entrances
- Test full gameplay loop
- Bug fixes

**Result:** Significantly enhanced dungeon crawler with vertical progression and tactical room structure.

---

## Files You'll Need to Create

```
NEW FILES:
WingedBean.Gameplay.Map/
  ├── Data/FloorMetadata.cs
  ├── Systems/FloorTransitionSystem.cs
  ├── Systems/DoorSystem.cs
  └── Components/DoorComponent.cs (if not using entity-based)

MODIFY:
WingedBean.Gameplay.Map/
  ├── Data/TileType.cs (add new tile types)
  ├── Data/MapTile.cs (ensure supports new types)
  ├── Generation/DungeonGenerator.cs (place stairs, doors)
  └── Services/MapService.cs (multi-floor storage)

WingedBean.Plugins.DungeonGame/
  └── DungeonGame.cs (register new systems)
```

---

## Testing Checklist

### Multi-Floor System:
- [ ] Player can descend stairs
- [ ] Player can ascend stairs
- [ ] New floor generates on first visit
- [ ] Player spawns at correct position after transition
- [ ] Explored tiles persist when returning to floor
- [ ] Can't ascend from floor 0
- [ ] Floor number displays correctly

### Door System:
- [ ] Closed doors block movement
- [ ] Open doors are walkable
- [ ] Can open closed doors
- [ ] Locked doors require keys
- [ ] Message displays when door is locked
- [ ] Doors render correctly (closed vs open)
- [ ] AI can't walk through closed doors

---

## Questions to Answer

1. **Multi-floor:** How many floors should the dungeon have? (Suggestion: 10-20)
2. **Doors:** Should ALL room entrances have doors, or randomly placed?
3. **Keys:** Simple boolean (has key) or specific key IDs?
4. **Traps:** Hidden until detected, or always visible?
5. **EdgRogqd:** Is current dungeon generation quality acceptable?

---

## Bottom Line

**FOCUS ON:** Multi-floor dungeons (Priority 1) and doors (Priority 2)  
**IGNORE:** Replacing GoRogue systems - they work fine  
**CONSIDER:** EdgRogqd integration only if current dungeons aren't good enough  

**Effort:** 1 week for multi-floor + doors = massive gameplay improvement  
**vs** 4-6 weeks rewriting FOV/pathfinding = minimal gameplay benefit

Build what adds gameplay value, not what adds architectural purity.

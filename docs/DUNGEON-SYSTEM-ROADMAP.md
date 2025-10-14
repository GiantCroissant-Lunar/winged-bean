---
id: roadmap-dungeon-system
title: Dungeon System Enhancement Roadmap
type: roadmap
category: planning
created: 2025-10-14
updated: 2025-10-14
---

# Dungeon System Enhancement Roadmap

**Date:** 2025-10-14  
**Status:** Planning

---

## Current State (Phase 1 & 2 Complete ✅)

### ✅ RFC-0043: Tile-Based Map System (COMPLETE)
- Basic map infrastructure with TileType, MapTile, DungeonMap
- ECS components: PositionComponent, MapComponent, BlocksMovementComponent
- MovementSystem with collision detection
- MapRenderSystem for ASCII rendering
- **22 unit tests, all passing**

### ✅ RFC-0044: Procedural Dungeon Generation (COMPLETE)
- Room-and-corridor algorithm
- Configurable generation (Small, Default, Large presets)
- Seed-based reproducibility
- Room collision detection
- **43 total unit tests, all passing**

---

## Enhancement Options Evaluation

### 1. ⭐ Field of View System Integration
**RFC:** RFC-0052  
**Priority:** P1 (HIGH)  
**Effort:** 2-4 hours  
**Value:** ⭐⭐⭐⭐⭐

**Why Now:**
- ✅ GoRogue already available and proven
- ✅ FOV implementation exists in DungeonGame plugin
- ✅ Natural next step after map generation
- ✅ Immediate player experience improvement

**What It Enables:**
- Fog of war (classic roguelike feel)
- Map exploration and discovery
- Foundation for stealth mechanics
- Strategic gameplay (plan around visibility)

**Evaluation:** **RECOMMENDED FOR PHASE 3**

---

### 2. 🎨 Room Templates & Pre-designed Layouts
**RFC:** *To Be Created*  
**Priority:** P2 (MEDIUM)  
**Effort:** 4-8 hours  
**Value:** ⭐⭐⭐⭐

**Why Later:**
- ❌ Requires design work (create templates)
- ❌ Current generation works adequately
- ✅ Nice-to-have but not essential
- ✅ Better after FOV (can design with visibility in mind)

**What It Enables:**
- Interesting room shapes (L-shaped, circular, etc.)
- Special rooms (treasure, boss, etc.)
- Thematic dungeons (library, crypt, etc.)
- Better level variety

**Evaluation:** **DEFER TO PHASE 4**

**Implementation Approach:**
```csharp
public class RoomTemplate
{
    public string Name { get; set; }
    public int Width { get; set; }
    public int Height { get; set; }
    public TileType[,] Tiles { get; set; }
    public List<(int x, int y)> DoorPositions { get; set; }
}
```

---

### 3. 🚪 Automatic Door Placement
**RFC:** *To Be Created*  
**Priority:** P2 (MEDIUM)  
**Effort:** 2-3 hours  
**Value:** ⭐⭐⭐

**Why Later:**
- ❌ Low impact on core gameplay
- ✅ Easy to add once room connections are stable
- ✅ Can be done alongside room templates

**What It Enables:**
- Natural room entrances
- Better visual separation
- Doors can be interacted with (open/close, locked)
- More dungeon variety

**Evaluation:** **DEFER TO PHASE 4**

**Implementation Approach:**
- Detect corridor-to-room junctions
- Place door at junction point
- Add DoorComponent for interaction

---

### 4. 🔀 Plate.EdgRogqd Integration
**RFC:** *To Be Created*  
**Priority:** P3 (LOW)  
**Effort:** 8-16 hours  
**Value:** ⭐⭐⭐

**Why Later:**
- ❌ EdgRogqd library incomplete (only 18 .cs files)
- ❌ Current generation works well
- ❌ High effort for unclear benefit
- ✅ Better algorithms theoretically

**What It Enables:**
- Graph-based generation
- More sophisticated room placement
- Better guaranteed connectivity
- Research-backed algorithms

**Evaluation:** **DEFER TO PHASE 5+**

**Status Check:**
```bash
# EdgRogqd source files: 18
# Status: Early development
# Key files: Interfaces defined, implementation incomplete
```

**Recommendation:** Monitor EdgRogqd maturity. Current BSP algorithm is sufficient for now.

---

### 5. 🏢 Multi-level Dungeons
**RFC:** *To Be Created*  
**Priority:** P2 (MEDIUM)  
**Effort:** 3-5 hours  
**Value:** ⭐⭐⭐⭐

**Why Later:**
- ✅ Foundation exists (level field in MapComponent)
- ❌ Requires level transition mechanics
- ❌ Requires stairs placement algorithm
- ✅ Good for content depth

**What It Enables:**
- Multiple floors (descend deeper)
- Level progression
- Save/load system implications
- More content variety

**Evaluation:** **DEFER TO PHASE 5**

**Implementation Approach:**
```csharp
public class LevelManager
{
    private Dictionary<int, DungeonMap> _levels;
    
    public void GenerateLevel(int level);
    public void TransitionToLevel(int level);
    public void PlaceStairs(Room from, Room to, int fromLevel, int toLevel);
}
```

---

### 6. 🎯 Advanced Pathfinding Integration
**RFC:** *To Be Created*  
**Priority:** P3 (LOW)  
**Effort:** 2-3 hours  
**Value:** ⭐⭐

**Why Later:**
- ✅ GoRogue pathfinding already exists in DungeonGame plugin
- ❌ Not needed until AI enemies
- ✅ Easy to add when needed

**What It Enables:**
- AI movement
- Player auto-explore
- Distance maps for AI decisions
- Tactical positioning

**Evaluation:** **DEFER TO PHASE 6 (AI Development)**

---

## Recommended Implementation Order

### Phase 3: FOV & Visibility ⭐ **NEXT**
**Effort:** 2-4 hours  
**RFC:** RFC-0052

1. Create framework FOV components
2. Implement shadowcasting FOV system
3. Update MapRenderSystem for fog of war
4. Add map memory tracking
5. Comprehensive testing

**Outcome:** Players can explore with fog of war

---

### Phase 4: Room Templates & Doors
**Effort:** 6-11 hours  
**RFCs:** *To Be Created*

1. Design room template system
2. Create 5-10 basic templates
3. Update generator to use templates
4. Add automatic door placement
5. Testing and balancing

**Outcome:** More interesting dungeon layouts

---

### Phase 5: Multi-level Support
**Effort:** 3-5 hours  
**RFC:** *To Be Created*

1. Implement level manager
2. Add stairs placement
3. Level transition mechanics
4. Persistence considerations

**Outcome:** Deeper dungeon exploration

---

### Phase 6: AI & Pathfinding
**Effort:** 5-8 hours  
**RFC:** *To Be Created*

1. Integrate GoRogue pathfinding
2. Add AI movement systems
3. Distance maps for AI decisions
4. Enemy behaviors

**Outcome:** Intelligent enemy movement

---

### Phase 7+: Advanced Features
**Effort:** Variable  
**RFCs:** *To Be Created*

- EdgRogqd integration (if mature)
- Special room types (shops, altars, etc.)
- Dungeon themes and biomes
- Dynamic lighting
- Sound propagation

---

## Decision Matrix

| Enhancement | Priority | Effort | Value | Dependencies | Phase |
|------------|----------|--------|-------|--------------|-------|
| **FOV System** | P1 | 2-4h | ⭐⭐⭐⭐⭐ | None | **3** |
| Room Templates | P2 | 4-8h | ⭐⭐⭐⭐ | None | 4 |
| Door Placement | P2 | 2-3h | ⭐⭐⭐ | Room Templates | 4 |
| Multi-level | P2 | 3-5h | ⭐⭐⭐⭐ | Stairs logic | 5 |
| Pathfinding | P3 | 2-3h | ⭐⭐ | AI systems | 6 |
| EdgRogqd | P3 | 8-16h | ⭐⭐⭐ | Library maturity | 7+ |

---

## Key Insights

### ✅ What's Working Well

1. **Current Generation** - BSP room-and-corridor is solid
2. **GoRogue Integration** - FOV/pathfinding already available
3. **Framework Design** - Clean separation of concerns
4. **Test Coverage** - 43 tests, all passing

### ⚠️ Key Considerations

1. **EdgRogqd Maturity** - Library is incomplete, defer integration
2. **Feature Creep** - Focus on playable experience first
3. **Dependencies** - FOV unlocks many other features
4. **Performance** - Keep generation < 100ms

### 🎯 Success Metrics

- **Player Experience:** Can explore fog-of-war dungeons
- **Performance:** < 100ms generation, < 5ms FOV
- **Code Quality:** > 90% test coverage
- **Reusability:** Framework systems usable across game modes

---

## Conclusion

**Recommended Next Step:** Implement **RFC-0052: Field of View System Integration**

**Rationale:**
- High immediate value (fog of war is core roguelike feature)
- Low effort (2-4 hours with existing GoRogue integration)
- Enables future features (stealth, lighting, etc.)
- Natural progression from map generation
- Improves player experience significantly

**After FOV:** Proceed with room templates and door placement for visual variety, then multi-level dungeons for depth.

---

## References

- RFC-0043: Tile-Based Map System
- RFC-0044: Procedural Dungeon Generation  
- RFC-0052: Field of View System Integration
- Existing: `WingedBean.Plugins.DungeonGame.Systems.GoRogueFOVSystem`
- Plate.EdgRogqd: `/personal-work/plate-projects/edg-rogqd/`

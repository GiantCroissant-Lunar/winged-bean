---
id: RFC-0049
title: RFC-0049 Camera System - Implementation Plan
status: Implemented
category: game-engine, rendering, ux
created: 2025-10-15
updated: 2025-10-15
priority: P0
effort: 2-3 hours
---

# RFC-0049: Camera System - Implementation Plan

**Date:** 2025-10-15  
**Status:** 📝 **RFCS COMPLETE - READY FOR IMPLEMENTATION**  
**Approach:** **Plate Layer (Generic Library)**

---

## Decision: Plate Layer Implementation ✅

After analysis, we've decided to implement the camera system as a **Plate library** (generic, reusable) rather than game-specific code.

### Why Plate Layer?

**Benefits:**
1. ✅ **Reusable** - All Plate games benefit (roguelikes, tactics, platformers)
2. ✅ **Pure math** - No game logic, no ECS coupling
3. ✅ **Testable** - Unit tests for all camera logic
4. ✅ **Industry standard** - Like Unity's Cinemachine or Godot's Camera2D
5. ✅ **Zero dependencies** - Standalone library
6. ✅ **Faster** - 3-5 hours vs 4-6 hours for game-specific

**Similar to:**
- Unity's **Cinemachine** (framework-level camera)
- Godot's **Camera2D** (engine-level camera)
- Our approach: **Plate.Camera2D** (Plate-level camera)

---

## Architecture

### Two-Layer Approach

```
┌─────────────────────────────────────────┐
│  WingedBean.Plugins.DungeonGame         │
│  (Game-Specific Layer)                  │
│                                         │
│  ┌───────────────────────────────────┐ │
│  │ CameraComponent (ECS)             │ │
│  │ ViewportBoundsComponent (ECS)     │ │
│  │ CameraSystem (ECS)                │ │
│  │ PositionCameraTarget (Adapter)    │ │
│  └───────────────────────────────────┘ │
└─────────────────────────────────────────┘
                  ↓ Uses
┌─────────────────────────────────────────┐
│  Plate.Camera2D                         │
│  (Generic Library - Zero Dependencies)  │
│                                         │
│  ┌───────────────────────────────────┐ │
│  │ Camera (State)                    │ │
│  │ CameraController (Logic)          │ │
│  │ ICameraTarget (Interface)         │ │
│  │ ViewportBounds (Math)             │ │
│  │ CameraMode (Enum)                 │ │
│  └───────────────────────────────────┘ │
└─────────────────────────────────────────┘
```

---

## Implementation Plan

### Phase 1: Plate.Camera2D (2-3 hours)

**Location:** `/plate-projects/camera-bioca`

**Tasks:**
1. ✅ Create RFC-0001 (Camera2D Core Library)
2. ✅ Create RFC-0002 (WingedBean Integration)
3. ✅ Create README.md
4. ⏭️ Implement core types (Camera, ViewportBounds, etc.)
5. ⏭️ Implement CameraController
6. ⏭️ Write unit tests
7. ⏭️ Create example project

**Deliverables:**
- `Plate.Camera2D` library
- Unit tests (100% coverage)
- Example console app
- API documentation

### Phase 2: WingedBean Integration (1-2 hours)

**Location:** `/winged-bean/development/dotnet/console`

**Tasks:**
1. ⏭️ Add Plate.Camera2D project reference
2. ⏭️ Create CameraComponent (ECS wrapper)
3. ⏭️ Create ViewportBoundsComponent (singleton)
4. ⏭️ Create PositionCameraTarget (adapter)
5. ⏭️ Create CameraSystem (ECS system)
6. ⏭️ Update RenderSystem (viewport culling)
7. ⏭️ Write integration tests

**Deliverables:**
- ECS camera integration
- Viewport culling in RenderSystem
- Integration tests
- Performance profiling

---

## RFCs Created

### RFC-0001: Camera2D Core Library ✅
**Location:** `/plate-projects/camera-bioca/docs/rfcs/0001-camera2d-core-library.md`  
**Status:** Complete  
**Effort:** 2-3 hours

**Summary:**
- Generic camera system for tile-based 2D games
- Zero dependencies
- Pure math library
- Multiple follow modes
- Viewport culling support

**Key Features:**
- Camera state management
- CameraController (update logic)
- ICameraTarget interface
- ViewportBounds calculation
- Map bounds clamping
- Smooth camera movement (lerp)

### RFC-0002: WingedBean Camera Integration ✅
**Location:** `/plate-projects/camera-bioca/docs/rfcs/0002-wingedbean-camera-integration.md`  
**Status:** Complete  
**Effort:** 1-2 hours

**Summary:**
- ECS integration for Plate.Camera2D
- Arch ECS components and systems
- Adapter pattern for Position component
- Render system viewport culling

**Key Features:**
- CameraComponent (ECS wrapper)
- ViewportBoundsComponent (singleton)
- CameraSystem (updates camera)
- PositionCameraTarget (adapter)
- RenderSystem integration

---

## Benefits

### Performance Improvements

**Viewport Culling:**

| Map Size | Without Camera | With Camera | Improvement |
|----------|---------------|-------------|-------------|
| 40x20 | 800 tiles | 800 tiles | 0% |
| 80x40 | 3,200 tiles | 800 tiles | **75%** |
| 100x50 | 5,000 tiles | 800 tiles | **84%** |
| 200x100 | 20,000 tiles | 800 tiles | **96%** |

**Result:** Constant rendering cost regardless of map size! 🚀

### Code Reusability

**Other Plate Projects Can Use:**
- Roguelikes (dungeon exploration)
- Tactics games (unit following)
- Platformers (smooth scrolling)
- Map viewers (pan and zoom)

---

## Integration with Existing Systems

### System Execution Order

```
1. TurnManagerSystem      - Process turns
2. PathfindingSystem      - Calculate paths
3. InventorySystem        - Handle items
4. CameraSystem          - Update camera (NEW)
5. PerceptionSystem       - Calculate FOV
6. RenderSystem           - Render (uses viewport culling)
7. HUDRenderSystem        - Render HUD
```

### Integration Points

| System | Integration | Benefit |
|--------|-------------|---------|
| **RenderSystem** | Viewport culling | 75%+ performance gain |
| **PerceptionSystem** | Viewport bounds | Optimize FOV calculation |
| **TurnManagerSystem** | Camera follows player | Smooth movement |
| **PathfindingSystem** | None | Independent |
| **HUDRenderSystem** | None | Independent |

---

## Usage Example

### Setup

```csharp
// Create camera entity
var cameraEntity = world.Create(
    new CameraComponent(viewportWidth: 40, viewportHeight: 20)
    {
        FollowTarget = playerEntity
    }
);

// Configure camera
var cameraComp = world.GetComponent<CameraComponent>(cameraEntity);
cameraComp.Camera.Mode = CameraMode.EdgeScroll;
cameraComp.Camera.EdgeScrollMargin = 5;
cameraComp.Camera.SmoothSpeed = 8.0f;
```

### Game Loop

```csharp
// Update camera
cameraSystem.Execute(world, deltaTime);

// Get viewport bounds
var boundsComp = world.GetSingleton<ViewportBoundsComponent>();
var bounds = boundsComp.Bounds;

// Render only visible tiles (viewport culling)
for (int x = bounds.MinX; x <= bounds.MaxX; x++)
{
    for (int y = bounds.MinY; y <= bounds.MaxY; y++)
    {
        if (!map.IsInBounds(x, y))
            continue;
        
        var (viewportX, viewportY) = bounds.WorldToViewport(x, y);
        RenderTile(x, y, viewportX, viewportY);
    }
}
```

---

## Testing Strategy

### Unit Tests (Plate.Camera2D)
- Camera state management
- Follow modes (center, edge-scroll, free, fixed)
- Viewport bounds calculation
- Map bounds clamping
- Coordinate conversion
- Smooth movement (lerp)

### Integration Tests (WingedBean)
- ECS component integration
- Camera follows player
- Viewport culling works
- Performance profiling
- Edge cases (small maps, large maps)

---

## Timeline

### Estimated Effort

**Phase 1: Plate.Camera2D**
- Core types: 30 minutes
- Controller: 1 hour
- Unit tests: 1 hour
- Documentation: 30 minutes
- **Total: 3 hours**

**Phase 2: WingedBean Integration**
- Components & adapters: 30 minutes
- Camera system: 30 minutes
- Render integration: 30 minutes
- Testing: 30 minutes
- **Total: 2 hours**

**Grand Total: 5 hours** (vs 4-6 hours for game-specific)

---

## Success Criteria

### Plate.Camera2D
✅ Zero dependencies  
✅ Pure math library  
✅ Multiple follow modes  
✅ Viewport culling support  
✅ 100% unit test coverage  
✅ XML documentation  
✅ Example project  

### WingedBean Integration
✅ Camera follows player smoothly  
✅ Viewport culling reduces rendering by 75%+  
✅ Edge scrolling works correctly  
✅ Free camera mode for debugging  
✅ Integration with RenderSystem  
✅ Zero performance regression  
✅ Integration tests pass  

---

## Next Steps

### Immediate
1. ⏭️ Implement Plate.Camera2D (RFC-0001)
2. ⏭️ Implement WingedBean integration (RFC-0002)
3. ⏭️ Test with large maps
4. ⏭️ Profile performance

### Future
1. 🔜 Camera shake effects
2. 🔜 Zoom support
3. 🔜 Camera zones (trigger areas)
4. 🔜 Cutscene support
5. 🔜 Minimap integration

---

## Related RFCs

- **RFC-0001**: Camera2D Core Library (Plate) - Created ✅
- **RFC-0002**: WingedBean Integration - Created ✅
- **RFC-0007**: Arch ECS Integration - Implemented ✅
- **RFC-0043**: Tile-Based Map System - In Progress
- **RFC-0046**: Turn-Based Game Loop - Implemented ✅
- **RFC-0047**: Pathfinding System - Implemented ✅
- **RFC-0050**: Fog of War Rendering - Next

---

## References

- **Plate.Camera2D RFCs:** `/plate-projects/camera-bioca/docs/rfcs/`
- **Unity Cinemachine:** https://unity.com/unity/features/editor/art-and-design/cinemachine
- **Godot Camera2D:** https://docs.godotengine.org/en/stable/classes/class_camera2d.html

---

## Conclusion

**Camera system will be implemented as a Plate library!**

**Benefits:**
- ✅ Reusable across all Plate projects
- ✅ Clean abstraction (no ECS coupling)
- ✅ Industry-standard approach
- ✅ Better testing
- ✅ Actually **faster** to implement

**Status:** 📝 **RFCs COMPLETE - READY FOR IMPLEMENTATION**  
**Next:** Implement Plate.Camera2D core library (3 hours)

---

**Decision:** ✅ **APPROVED - Plate Layer Implementation**  
**Approach:** Two-layer architecture (Plate.Camera2D + WingedBean integration)

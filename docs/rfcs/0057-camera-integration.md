# RFC-0002: Camera Integration

**Status:** Implemented - Already Complete
**Priority:** P0 - Critical (Blocking)
**Effort:** 1 week (actual: already done)
**Phase:** 1 - Foundation
**Dependencies:** RFC-0001 (Environment/Tilemap) ✅

---

## Problem Statement

RenderSystem currently uses a fixed 40x15 viewport with no camera tracking. This prevents:
- Following the player as they move
- Supporting maps larger than the viewport
- Viewport culling for performance
- Smooth camera movement

### Current State
- ✅ **camera-bioca** (Plate layer) - RFC complete, not implemented
- ❌ RenderSystem uses fixed viewport position (0, 0)
- ❌ No camera tracking
- ❌ Cannot display large maps

---

## Proposed Solution

1. **Implement camera-bioca** at Plate layer
2. **Integrate into WingedBean** via `WingedBean.Gameplay.Camera`
3. **Update RenderSystem** to use camera for viewport calculation

### Architecture

**Plate Layer (camera-bioca):**
```
camera-bioca/dotnet/framework/src/Plate.Camera2D/
├── Camera.cs                   # Camera state
├── CameraController.cs         # Camera update logic
├── ICameraTarget.cs            # Follow target interface
├── ViewportBounds.cs           # Viewport bounds calculation
└── CameraMode.cs               # Follow modes enum
```

**Yokan Layer (WingedBean):**
```
WingedBean.Gameplay.Camera/
├── Components/
│   └── CameraComponent.cs      # ECS camera component
├── Systems/
│   └── CameraSystem.cs         # ECS camera system
└── Adapters/
    └── PositionCameraTarget.cs # ICameraTarget adapter for Position component
```

---

## Implementation

### Step 1: Implement camera-bioca (Plate Layer)

See `camera-bioca/docs/rfcs/0001-camera2d-core-library.md` for full spec.

**Key Classes:**

```csharp
// Plate.Camera2D/Camera.cs
public class Camera
{
    public float X { get; set; }
    public float Y { get; set; }
    public int ViewportWidth { get; set; }
    public int ViewportHeight { get; set; }
    public CameraMode Mode { get; set; }
    public float SmoothSpeed { get; set; }
    public int EdgeScrollMargin { get; set; }
    public Rectangle? MapBounds { get; set; }

    public static Camera CreateDefault(int viewportWidth, int viewportHeight);
}

public enum CameraMode
{
    CenterOnTarget,  // Camera centered on target
    EdgeScroll,      // Camera scrolls when target near edge
    Free,            // Manual camera control
    Fixed            // Camera doesn't move
}

// Plate.Camera2D/CameraController.cs
public class CameraController
{
    public void Update(Camera camera, ICameraTarget target, float deltaTime);
    public ViewportBounds CalculateViewportBounds(Camera camera);
    public void MoveCamera(Camera camera, float deltaX, float deltaY);
}

// Plate.Camera2D/ViewportBounds.cs
public struct ViewportBounds
{
    public int MinX { get; }
    public int MaxX { get; }
    public int MinY { get; }
    public int MaxY { get; }

    public (int ViewportX, int ViewportY) WorldToViewport(int worldX, int worldY);
    public (int WorldX, int WorldY) ViewportToWorld(int viewportX, int viewportY);
    public bool Contains(int worldX, int worldY);
}
```

### Step 2: Create WingedBean.Gameplay.Camera

```csharp
// WingedBean.Gameplay.Camera/Components/CameraComponent.cs
public struct CameraComponent
{
    public Camera Camera;              // Plate.Camera2D.Camera
    public EntityHandle? FollowTarget; // Entity to follow (usually player)
}

// WingedBean.Gameplay.Camera/Adapters/PositionCameraTarget.cs
public class PositionCameraTarget : ICameraTarget
{
    private readonly IWorld _world;
    private readonly EntityHandle _entity;

    public float X => _world.Get<Position>(_entity).X;
    public float Y => _world.Get<Position>(_entity).Y;

    public PositionCameraTarget(IWorld world, EntityHandle entity)
    {
        _world = world;
        _entity = entity;
    }
}

// WingedBean.Gameplay.Camera/Systems/CameraSystem.cs
public class CameraSystem : IECSSystem
{
    private readonly CameraController _controller = new();
    private readonly IMapService _mapService;

    public void Execute(IWorld world, float deltaTime)
    {
        // Query for camera entity
        var query = world.Query<CameraComponent>();
        foreach (var cameraEntity in query)
        {
            var cameraComp = cameraEntity.Get<CameraComponent>();

            // Get follow target
            if (cameraComp.FollowTarget.HasValue)
            {
                var target = new PositionCameraTarget(world, cameraComp.FollowTarget.Value);

                // Update camera to follow target
                _controller.Update(cameraComp.Camera, target, deltaTime);

                // Clamp to map bounds
                var map = _mapService.GetMap(_mapService.GetCurrentFloor());
                cameraComp.Camera.MapBounds = new Rectangle(0, 0, map.Width, map.Height);
            }
        }
    }
}
```

### Step 3: Update RenderSystem

```csharp
// WingedBean.Plugins.DungeonGame/Systems/RenderSystem.cs
public class RenderSystem : IECSSystem
{
    // BEFORE
    private const int VIEWPORT_WIDTH = 40;
    private const int VIEWPORT_HEIGHT = 15;

    public void Execute(IWorld world, float deltaTime)
    {
        // Fixed viewport at (0, 0)
        for (int y = 0; y < VIEWPORT_HEIGHT; y++)
        {
            for (int x = 0; x < VIEWPORT_WIDTH; x++)
            {
                // Render tile/entity at (x, y)
            }
        }
    }

    // AFTER
    private readonly CameraController _cameraController = new();

    public void Execute(IWorld world, float deltaTime)
    {
        // Get camera
        var cameraQuery = world.Query<CameraComponent>();
        var cameraEntity = cameraQuery.FirstOrDefault();
        if (cameraEntity == null) return;

        var camera = cameraEntity.Get<CameraComponent>().Camera;

        // Calculate viewport bounds
        var bounds = _cameraController.CalculateViewportBounds(camera);

        // Render only visible tiles
        for (int worldY = bounds.MinY; worldY <= bounds.MaxY; worldY++)
        {
            for (int worldX = bounds.MinX; worldX <= bounds.MaxX; worldX++)
            {
                var (viewportX, viewportY) = bounds.WorldToViewport(worldX, worldY);

                // Render tile at (worldX, worldY) to screen position (viewportX, viewportY)
                RenderTile(worldX, worldY, viewportX, viewportY);
            }
        }

        // Render entities within viewport
        var entityQuery = world.Query<Position, Renderable>();
        foreach (var entity in entityQuery)
        {
            var pos = entity.Get<Position>();
            if (bounds.Contains(pos.X, pos.Y))
            {
                var (viewportX, viewportY) = bounds.WorldToViewport(pos.X, pos.Y);
                RenderEntity(entity, viewportX, viewportY);
            }
        }
    }
}
```

---

## Integration Steps

1. **Implement camera-bioca** (Plate layer)
   - Create `Plate.Camera2D` project
   - Implement Camera, CameraController, ViewportBounds
   - Write unit tests (100% coverage)

2. **Create WingedBean.Gameplay.Camera** (Yokan layer)
   - Create project
   - Add CameraComponent
   - Implement CameraSystem
   - Create PositionCameraTarget adapter

3. **Update RenderSystem**
   - Remove fixed viewport
   - Use CameraController for bounds calculation
   - Transform world → viewport coordinates

4. **Initialize Camera in DungeonGame**
   - Create camera entity with CameraComponent
   - Set follow target to player entity
   - Configure camera mode (CenterOnTarget)
   - Set map bounds

---

## Success Criteria

- ✅ camera-bioca implemented and tested (Plate layer)
- ✅ Camera follows player smoothly
- ✅ Viewport culling works (only visible tiles rendered)
- ✅ Large maps (200x200) perform well
- ✅ Camera clamped to map bounds
- ✅ World ↔ viewport coordinate transformation correct
- ✅ All camera modes functional (center, edge-scroll, free, fixed)

---

## Performance Targets

- Camera update: < 0.1ms
- Viewport bounds calculation: < 0.1ms
- Coordinate transformation: < 0.001ms per call

---

## Dependencies

- **RFC-0001:** Environment/Tilemap (for map bounds)
- **Plate Project:** camera-bioca (implement first)

---

## Follow-Up

- **RFC-0003:** FOV System (uses viewport bounds for visibility culling)
- **RFC-0015:** Minimap (uses camera position)

---

**Author:** WingedBean Team
**Date:** 2025-10-15
**Version:** 1.0

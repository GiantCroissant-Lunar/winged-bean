---
id: RFC-0049
title: Camera and Viewport System
status: Proposed
category: game-engine, rendering, ux
created: 2025-10-14
updated: 2025-10-14
priority: P0
effort: 4-6 hours
---

# RFC-0049: Camera and Viewport System

**Status:** Proposed  
**Date:** 2025-10-14  
**Author:** Development Team  
**Category:** game-engine, rendering, ux  
**Priority:** CRITICAL (P0)  
**Estimated Effort:** 4-6 hours  
**Depends On:** 
- RFC-0007: Arch ECS Integration (Implemented)
- RFC-0043: Tile-Based Map System (In Progress)
- RenderSystem (Existing)

---

## Summary

Implement a **camera and viewport system** that allows the game to render large dungeon maps by following the player and only displaying a portion of the map at a time. The camera supports multiple follow modes (center, edge-scroll, free), smooth transitions, and viewport culling for performance.

---

## Motivation

### Current Problem

`RenderSystem` has **hardcoded viewport**:

```csharp
// RenderSystem.cs - Current
private int _renderWidth = 40;
private int _renderHeight = 15;

// Renders ENTIRE map to fixed 40x15 buffer
// No camera, no scrolling, no following player
```

**Issues:**
1. **Can't handle large maps** - Procedural dungeons are 80x40 or larger
2. **Player off-screen** - No camera following
3. **Poor UX** - Can't see where you're going
4. **No zoom** - Fixed scale only
5. **Performance** - Renders entire map even if not visible

### Why Camera System?

**Gameplay Benefits:**
- 🎥 **Large Maps** - Explore procedurally generated dungeons
- 👁️ **Better Visibility** - See tactical situation around player
- 🎮 **Smooth Movement** - Camera follows player naturally
- 🔍 **Zoom Support** - Adjust view scale (future)

**Technical Benefits:**
- ✅ **Performance** - Only render visible tiles (viewport culling)
- ✅ **Flexible** - Support different screen sizes
- ✅ **Reusable** - Works with any map size

### Success Criteria

✅ Camera follows player smoothly  
✅ Viewport only renders visible portion of map  
✅ Edge scrolling when player near viewport edge  
✅ Support for different viewport sizes  
✅ Performance: 60 FPS with 80x40 map  
✅ Free camera mode for debugging

---

## Proposal

### 1. Camera Component

```csharp
namespace WingedBean.Plugins.DungeonGame.Components;

/// <summary>
/// Camera component for viewport rendering.
/// Typically attached to player or separate camera entity.
/// </summary>
public struct Camera
{
    /// <summary>Camera world position (center of viewport)</summary>
    public float X { get; set; }
    public float Y { get; set; }
    
    /// <summary>Viewport size in tiles</summary>
    public int ViewportWidth { get; set; }
    public int ViewportHeight { get; set; }
    
    /// <summary>Camera follow mode</summary>
    public CameraMode Mode { get; set; }
    
    /// <summary>Target entity to follow (if Mode = FollowEntity)</summary>
    public EntityHandle? FollowTarget { get; set; }
    
    /// <summary>Edge scroll margin (tiles from edge before scrolling)</summary>
    public int EdgeScrollMargin { get; set; }
    
    /// <summary>Camera movement speed for smooth transitions</summary>
    public float SmoothSpeed { get; set; }
    
    /// <summary>Zoom level (1.0 = normal, 2.0 = 2x zoom)</summary>
    public float Zoom { get; set; }
    
    /// <summary>Is camera locked to prevent movement?</summary>
    public bool IsLocked { get; set; }
}

public enum CameraMode
{
    /// <summary>Camera centered on target entity</summary>
    CenterOnTarget,
    
    /// <summary>Camera scrolls when target near edge</summary>
    EdgeScroll,
    
    /// <summary>Free camera (manual control)</summary>
    Free,
    
    /// <summary>Fixed position (no movement)</summary>
    Fixed
}
```

---

### 2. Viewport Bounds

```csharp
namespace WingedBean.Plugins.DungeonGame.Components;

/// <summary>
/// Viewport bounds in world coordinates.
/// Calculated by CameraSystem based on Camera component.
/// </summary>
public struct ViewportBounds
{
    public int MinX { get; set; }
    public int MinY { get; set; }
    public int MaxX { get; set; }
    public int MaxY { get; set; }
    
    public int Width => MaxX - MinX + 1;
    public int Height => MaxY - MinY + 1;
    
    /// <summary>Check if world position is within viewport</summary>
    public bool Contains(int x, int y)
    {
        return x >= MinX && x <= MaxX && y >= MinY && y <= MaxY;
    }
    
    /// <summary>Convert world position to viewport position</summary>
    public (int X, int Y) WorldToViewport(int worldX, int worldY)
    {
        return (worldX - MinX, worldY - MinY);
    }
    
    /// <summary>Convert viewport position to world position</summary>
    public (int X, int Y) ViewportToWorld(int viewportX, int viewportY)
    {
        return (viewportX + MinX, viewportY + MinY);
    }
}
```

---

### 3. Camera System

```csharp
namespace WingedBean.Plugins.DungeonGame.Systems;

using WingedBean.Plugins.ArchECS.Internal;
using WingedBean.Plugins.DungeonGame.Components;

/// <summary>
/// System that updates camera position and calculates viewport bounds.
/// </summary>
public class CameraSystem : IECSSystem
{
    public void Execute(IWorld world, float deltaTime)
    {
        // Process all cameras
        foreach (var entity in world.CreateQuery<Camera>())
        {
            ref var camera = ref world.GetComponent<Camera>(entity);
            
            if (camera.IsLocked)
                continue;
            
            // Update camera position based on mode
            switch (camera.Mode)
            {
                case CameraMode.CenterOnTarget:
                    UpdateCenterOnTarget(world, ref camera, deltaTime);
                    break;
                    
                case CameraMode.EdgeScroll:
                    UpdateEdgeScroll(world, ref camera, deltaTime);
                    break;
                    
                case CameraMode.Free:
                    // Free camera controlled by input
                    break;
                    
                case CameraMode.Fixed:
                    // No update needed
                    break;
            }
            
            // Clamp camera to map bounds
            ClampToMapBounds(world, ref camera);
            
            // Calculate and store viewport bounds
            var bounds = CalculateViewportBounds(camera);
            world.AddOrSetComponent(entity, bounds);
        }
    }
    
    private void UpdateCenterOnTarget(IWorld world, ref Camera camera, float deltaTime)
    {
        if (!camera.FollowTarget.HasValue)
            return;
        
        var target = camera.FollowTarget.Value;
        
        if (!world.IsAlive(target) || !world.HasComponent<Position>(target))
            return;
        
        var targetPos = world.GetComponent<Position>(target);
        
        // Smoothly move camera to target position
        if (camera.SmoothSpeed > 0)
        {
            camera.X = Lerp(camera.X, targetPos.X, camera.SmoothSpeed * deltaTime);
            camera.Y = Lerp(camera.Y, targetPos.Y, camera.SmoothSpeed * deltaTime);
        }
        else
        {
            // Instant snap
            camera.X = targetPos.X;
            camera.Y = targetPos.Y;
        }
    }
    
    private void UpdateEdgeScroll(IWorld world, ref Camera camera, float deltaTime)
    {
        if (!camera.FollowTarget.HasValue)
            return;
        
        var target = camera.FollowTarget.Value;
        
        if (!world.IsAlive(target) || !world.HasComponent<Position>(target))
            return;
        
        var targetPos = world.GetComponent<Position>(target);
        
        // Calculate target position in viewport coordinates
        int halfWidth = camera.ViewportWidth / 2;
        int halfHeight = camera.ViewportHeight / 2;
        
        int targetViewportX = targetPos.X - (int)camera.X + halfWidth;
        int targetViewportY = targetPos.Y - (int)camera.Y + halfHeight;
        
        // Check if target is near edge
        int margin = camera.EdgeScrollMargin;
        
        float targetCameraX = camera.X;
        float targetCameraY = camera.Y;
        
        if (targetViewportX < margin)
        {
            targetCameraX = targetPos.X - margin + halfWidth;
        }
        else if (targetViewportX >= camera.ViewportWidth - margin)
        {
            targetCameraX = targetPos.X + margin - halfWidth;
        }
        
        if (targetViewportY < margin)
        {
            targetCameraY = targetPos.Y - margin + halfHeight;
        }
        else if (targetViewportY >= camera.ViewportHeight - margin)
        {
            targetCameraY = targetPos.Y + margin - halfHeight;
        }
        
        // Smoothly move camera
        if (camera.SmoothSpeed > 0)
        {
            camera.X = Lerp(camera.X, targetCameraX, camera.SmoothSpeed * deltaTime);
            camera.Y = Lerp(camera.Y, targetCameraY, camera.SmoothSpeed * deltaTime);
        }
        else
        {
            camera.X = targetCameraX;
            camera.Y = targetCameraY;
        }
    }
    
    private void ClampToMapBounds(IWorld world, ref Camera camera)
    {
        var map = world.GetSingleton<DungeonMap>();
        if (map == null)
            return;
        
        int halfWidth = camera.ViewportWidth / 2;
        int halfHeight = camera.ViewportHeight / 2;
        
        // Clamp camera so viewport doesn't go outside map
        camera.X = Math.Clamp(camera.X, halfWidth, map.Width - halfWidth - 1);
        camera.Y = Math.Clamp(camera.Y, halfHeight, map.Height - halfHeight - 1);
    }
    
    private ViewportBounds CalculateViewportBounds(Camera camera)
    {
        int halfWidth = camera.ViewportWidth / 2;
        int halfHeight = camera.ViewportHeight / 2;
        
        return new ViewportBounds
        {
            MinX = (int)camera.X - halfWidth,
            MinY = (int)camera.Y - halfHeight,
            MaxX = (int)camera.X + halfWidth,
            MaxY = (int)camera.Y + halfHeight
        };
    }
    
    private float Lerp(float a, float b, float t)
    {
        return a + (b - a) * Math.Clamp(t, 0, 1);
    }
}
```

---

### 4. Enhanced Render System

```csharp
namespace WingedBean.Plugins.DungeonGame.Systems;

using WingedBean.Plugins.ArchECS.Internal;
using WingedBean.Plugins.DungeonGame.Components;

/// <summary>
/// Enhanced render system with camera/viewport support.
/// Only renders tiles within viewport bounds.
/// </summary>
public class EnhancedRenderSystem : IECSSystem
{
    private readonly List<(Position pos, Renderable render)> _renderBuffer = new();
    
    public string? LastRenderedFrame { get; private set; }
    
    public void Execute(IWorld world, float deltaTime)
    {
        // Get active camera
        var cameraQuery = world.CreateQuery<Camera, ViewportBounds>();
        var cameraEntity = cameraQuery.FirstOrDefault();
        
        if (cameraEntity == default)
        {
            // No camera, render nothing
            LastRenderedFrame = string.Empty;
            return;
        }
        
        var camera = world.GetComponent<Camera>(cameraEntity);
        var viewport = world.GetComponent<ViewportBounds>(cameraEntity);
        
        // Clear render buffer
        _renderBuffer.Clear();
        
        // Collect renderable entities WITHIN VIEWPORT
        foreach (var entity in world.CreateQuery<Position, Renderable>())
        {
            var pos = world.GetComponent<Position>(entity);
            
            // Viewport culling - skip entities outside viewport
            if (!viewport.Contains(pos.X, pos.Y))
                continue;
            
            var render = world.GetComponent<Renderable>(entity);
            _renderBuffer.Add((pos, render));
        }
        
        // Sort by render layer
        _renderBuffer.Sort((a, b) => a.render.RenderLayer.CompareTo(b.render.RenderLayer));
        
        // Render to text buffer
        LastRenderedFrame = RenderToTextBuffer(world, camera, viewport);
    }
    
    private string RenderToTextBuffer(IWorld world, Camera camera, ViewportBounds viewport)
    {
        var map = world.GetSingleton<DungeonMap>();
        
        var buffer = new char[camera.ViewportHeight, camera.ViewportWidth];
        
        // Render map tiles within viewport
        if (map != null)
        {
            for (int worldY = viewport.MinY; worldY <= viewport.MaxY; worldY++)
            {
                for (int worldX = viewport.MinX; worldX <= viewport.MaxX; worldX++)
                {
                    if (!map.IsInBounds(worldX, worldY))
                    {
                        var (vx, vy) = viewport.WorldToViewport(worldX, worldY);
                        if (vx >= 0 && vx < camera.ViewportWidth && 
                            vy >= 0 && vy < camera.ViewportHeight)
                        {
                            buffer[vy, vx] = ' '; // Out of bounds
                        }
                        continue;
                    }
                    
                    var tile = map.GetTile(worldX, worldY);
                    if (tile == null)
                        continue;
                    
                    var (viewportX, viewportY) = viewport.WorldToViewport(worldX, worldY);
                    
                    if (viewportX >= 0 && viewportX < camera.ViewportWidth &&
                        viewportY >= 0 && viewportY < camera.ViewportHeight)
                    {
                        buffer[viewportY, viewportX] = tile.Value.Glyph;
                    }
                }
            }
        }
        
        // Render entities on top of map
        foreach (var (pos, render) in _renderBuffer)
        {
            var (viewportX, viewportY) = viewport.WorldToViewport(pos.X, pos.Y);
            
            if (viewportX >= 0 && viewportX < camera.ViewportWidth &&
                viewportY >= 0 && viewportY < camera.ViewportHeight)
            {
                buffer[viewportY, viewportX] = render.Symbol;
            }
        }
        
        // Convert buffer to string
        var sb = new StringBuilder();
        for (int y = 0; y < camera.ViewportHeight; y++)
        {
            for (int x = 0; x < camera.ViewportWidth; x++)
            {
                sb.Append(buffer[y, x]);
            }
            if (y < camera.ViewportHeight - 1)
                sb.AppendLine();
        }
        
        return sb.ToString();
    }
}
```

---

## Implementation Plan

### Phase 1: Core Components (2 hours)
- [ ] Create `Camera` component
- [ ] Create `ViewportBounds` component
- [ ] Unit tests for coordinate conversion

### Phase 2: Camera System (2 hours)
- [ ] Implement `CameraSystem`
- [ ] Center-on-target mode
- [ ] Edge-scroll mode
- [ ] Map bounds clamping

### Phase 3: Render Integration (2 hours)
- [ ] Update `RenderSystem` with viewport culling
- [ ] Test with large maps (80x40, 100x100)
- [ ] Performance profiling
- [ ] Integration tests

---

## Testing Strategy

### Unit Tests
- Viewport bounds calculations
- World-to-viewport coordinate conversion
- Camera clamping to map bounds
- Edge scroll trigger zones

### Integration Tests
- Camera follows player smoothly
- Viewport culls off-screen entities
- Large map rendering performance
- Different viewport sizes

---

## Performance Impact

| Map Size | Without Camera | With Camera | Improvement |
|----------|----------------|-------------|-------------|
| **40x15** | 600 tiles | 600 tiles | 0% |
| **80x40** | 3,200 tiles | 600 tiles | **81% faster** |
| **100x100** | 10,000 tiles | 600 tiles | **94% faster** |

**Viewport culling is critical for large procedurally generated dungeons.**

---

## Related RFCs

- **RFC-0043**: Tile-Based Map System ⏭️ In Progress
- **RFC-0050**: Fog of War Rendering ⏭️ Proposed (next)
- **RFC-0051**: HUD System ⏭️ Proposed

---

**Decision:** Proposed - Awaiting Review  
**Next Steps:**
1. Review with team
2. Prototype camera follow modes
3. Test with procedurally generated maps
4. Create implementation branch

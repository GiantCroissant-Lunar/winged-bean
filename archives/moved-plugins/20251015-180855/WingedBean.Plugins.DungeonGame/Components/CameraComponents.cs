using Plate.Camera2D;
using WingedBean.Plugins.ArchECS.Internal;

namespace WingedBean.Plugins.DungeonGame.Components;

/// <summary>
/// ECS component that wraps Plate.Camera2D.Camera.
/// Attach to camera entity (usually player or separate camera entity).
/// </summary>
public class CameraComponent
{
    /// <summary>Underlying Plate.Camera2D camera</summary>
    public Camera Camera { get; set; }

    /// <summary>Entity to follow (if Mode = CenterOnTarget or EdgeScroll)</summary>
    public EntityHandle? FollowTarget { get; set; }

    /// <summary>
    /// Create a new camera component.
    /// </summary>
    /// <param name="viewportWidth">Viewport width in tiles</param>
    /// <param name="viewportHeight">Viewport height in tiles</param>
    public CameraComponent(int viewportWidth, int viewportHeight)
    {
        Camera = Camera.CreateDefault(viewportWidth, viewportHeight);
    }
}

/// <summary>
/// Singleton component storing current viewport bounds.
/// Used by RenderSystem and other systems for viewport culling.
/// </summary>
public class ViewportBoundsComponent
{
    /// <summary>Current viewport bounds in world coordinates</summary>
    public ViewportBounds Bounds { get; set; }

    /// <summary>
    /// Create viewport bounds component.
    /// </summary>
    public ViewportBoundsComponent()
    {
        // Default to empty bounds
        Bounds = new ViewportBounds(0, 0, 0, 0);
    }
}

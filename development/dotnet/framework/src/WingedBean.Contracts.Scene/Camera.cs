namespace WingedBean.Contracts.Scene;

/// <summary>
/// Camera configuration for viewport positioning and rendering.
/// Supports panning, zooming, and following entities.
/// </summary>
public readonly struct Camera
{
    /// <summary>Camera X position in world coordinates</summary>
    public int X { get; init; }
    
    /// <summary>Camera Y position in world coordinates</summary>
    public int Y { get; init; }
    
    /// <summary>Zoom level (1.0 = 100%, 2.0 = 200%, 0.5 = 50%)</summary>
    public float Zoom { get; init; }
    
    /// <summary>Entity ID to follow (null = no follow)</summary>
    public int? FollowEntityId { get; init; }
    
    /// <summary>Follow offset X (relative to followed entity)</summary>
    public int FollowOffsetX { get; init; }
    
    /// <summary>Follow offset Y (relative to followed entity)</summary>
    public int FollowOffsetY { get; init; }

    public Camera(int x, int y, float zoom = 1.0f, int? followEntityId = null)
    {
        X = x;
        Y = y;
        Zoom = zoom;
        FollowEntityId = followEntityId;
        FollowOffsetX = 0;
        FollowOffsetY = 0;
    }

    /// <summary>
    /// Creates a camera centered on an entity.
    /// </summary>
    public static Camera FollowEntity(int entityId, int offsetX = 0, int offsetY = 0, float zoom = 1.0f)
    {
        return new Camera
        {
            X = 0,
            Y = 0,
            Zoom = zoom,
            FollowEntityId = entityId,
            FollowOffsetX = offsetX,
            FollowOffsetY = offsetY
        };
    }

    /// <summary>
    /// Creates a static camera at specific coordinates.
    /// </summary>
    public static Camera Static(int x, int y, float zoom = 1.0f)
    {
        return new Camera(x, y, zoom);
    }

    /// <summary>
    /// Pans the camera by the specified delta.
    /// </summary>
    public Camera Pan(int deltaX, int deltaY)
    {
        return new Camera
        {
            X = X + deltaX,
            Y = Y + deltaY,
            Zoom = Zoom,
            FollowEntityId = FollowEntityId,
            FollowOffsetX = FollowOffsetX,
            FollowOffsetY = FollowOffsetY
        };
    }

    /// <summary>
    /// Changes the zoom level.
    /// </summary>
    public Camera SetZoom(float zoom)
    {
        return new Camera
        {
            X = X,
            Y = Y,
            Zoom = zoom,
            FollowEntityId = FollowEntityId,
            FollowOffsetX = FollowOffsetX,
            FollowOffsetY = FollowOffsetY
        };
    }
}

/// <summary>
/// Extended viewport with camera information.
/// </summary>
public readonly struct CameraViewport
{
    public Viewport Viewport { get; init; }
    public Camera Camera { get; init; }

    public CameraViewport(Viewport viewport, Camera camera)
    {
        Viewport = viewport;
        Camera = camera;
    }

    /// <summary>
    /// Converts world coordinates to viewport coordinates.
    /// </summary>
    public (int viewX, int viewY) WorldToView(int worldX, int worldY)
    {
        var scaledX = (int)((worldX - Camera.X) * Camera.Zoom);
        var scaledY = (int)((worldY - Camera.Y) * Camera.Zoom);
        return (scaledX, scaledY);
    }

    /// <summary>
    /// Converts viewport coordinates to world coordinates.
    /// </summary>
    public (int worldX, int worldY) ViewToWorld(int viewX, int viewY)
    {
        var worldX = (int)(viewX / Camera.Zoom) + Camera.X;
        var worldY = (int)(viewY / Camera.Zoom) + Camera.Y;
        return (worldX, worldY);
    }

    /// <summary>
    /// Checks if a world position is visible in the current viewport.
    /// </summary>
    public bool IsVisible(int worldX, int worldY)
    {
        var (viewX, viewY) = WorldToView(worldX, worldY);
        return viewX >= 0 && viewX < Viewport.Width && viewY >= 0 && viewY < Viewport.Height;
    }
}

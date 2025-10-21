using Arch.Core;
using Plate.Camera2D;

namespace WingedBean.Gameplay.Camera.Components;

/// <summary>
/// ECS component that wraps Plate.Camera2D.Camera for viewport management.
/// Attach to an entity to create a camera that can follow targets and manage viewport bounds.
/// </summary>
public struct CameraComponent
{
    /// <summary>
    /// The Plate.Camera2D camera instance managing viewport state.
    /// </summary>
    public Camera Camera { get; set; }

    /// <summary>
    /// Entity to follow (typically the player).
    /// When set, camera will track this entity's position.
    /// </summary>
    public EntityReference? FollowTarget { get; set; }

    /// <summary>
    /// Whether camera should update this frame.
    /// Set to false to pause camera movement.
    /// </summary>
    public bool IsActive { get; set; }

    /// <summary>
    /// Creates a camera component with default settings.
    /// </summary>
    /// <param name="viewportWidth">Viewport width in tiles</param>
    /// <param name="viewportHeight">Viewport height in tiles</param>
    /// <returns>New camera component</returns>
    public static CameraComponent CreateDefault(int viewportWidth, int viewportHeight)
    {
        return new CameraComponent
        {
            Camera = Camera.CreateDefault(viewportWidth, viewportHeight),
            FollowTarget = null,
            IsActive = true
        };
    }
}

namespace WingedBean.Gameplay.Map.Components;

/// <summary>
/// Tag component indicating that an entity blocks movement.
/// Used for collision detection with other entities.
/// </summary>
public struct BlocksMovementComponent
{
    /// <summary>
    /// Creates a new BlocksMovementComponent.
    /// </summary>
    public static BlocksMovementComponent Create() => new();
}

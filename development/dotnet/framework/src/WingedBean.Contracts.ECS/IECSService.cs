namespace WingedBean.Contracts.ECS;

/// <summary>
/// Platform-agnostic ECS service interface.
/// Manages ECS worlds and provides the entry point for Entity Component System operations.
/// Abstracts the underlying ECS implementation (Arch, EnTT, Unity ECS, etc.)
/// </summary>
public interface IECSService
{
    /// <summary>
    /// Creates a new ECS world.
    /// A world is an isolated container for entities, components, and systems.
    /// Multiple worlds can exist simultaneously for different scenes or simulation contexts.
    /// </summary>
    /// <returns>A new <see cref="IWorld"/> instance.</returns>
    IWorld CreateWorld();

    /// <summary>
    /// Destroys an existing ECS world and releases all associated resources.
    /// All entities, components, and systems within the world will be cleaned up.
    /// </summary>
    /// <param name="world">The world to destroy.</param>
    void DestroyWorld(IWorld world);

    /// <summary>
    /// Gets an existing world by its unique identifier.
    /// </summary>
    /// <param name="worldId">The unique identifier of the world.</param>
    /// <returns>The <see cref="IWorld"/> instance if found; otherwise, <c>null</c>.</returns>
    IWorld? GetWorld(int worldId);
}

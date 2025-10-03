using System;
using System.Collections.Generic;

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
    /// Gets the authoring world where authoring data is managed.
    /// Implementations should create this world lazily when first accessed.
    /// </summary>
    WorldHandle AuthoringWorld { get; }

    /// <summary>
    /// Gets the default runtime world that gameplay systems should target.
    /// </summary>
    WorldHandle DefaultRuntimeWorld { get; }

    /// <summary>
    /// Creates a new runtime world with the provided name.
    /// </summary>
    /// <param name="name">Logical identifier for the runtime world.</param>
    /// <returns>Handle representing the newly created world.</returns>
    WorldHandle CreateRuntimeWorld(string name);

    /// <summary>
    /// Destroys an existing ECS world and releases all associated resources.
    /// All entities, components, and systems within the world will be cleaned up.
    /// </summary>
    /// <param name="world">The world to destroy.</param>
    void DestroyWorld(IWorld world);

    /// <summary>
    /// Destroys an existing world identified by its handle.
    /// Authoring worlds cannot be destroyed through this method.
    /// </summary>
    /// <param name="handle">Handle for the world to destroy.</param>
    /// <returns><c>true</c> if a world was removed; otherwise <c>false</c>.</returns>
    bool DestroyWorld(WorldHandle handle);

    /// <summary>
    /// Gets an existing world by its unique identifier.
    /// </summary>
    /// <param name="worldId">The unique identifier of the world.</param>
    /// <returns>The <see cref="IWorld"/> instance if found; otherwise, <c>null</c>.</returns>
    IWorld? GetWorld(int worldId);

    /// <summary>
    /// Gets an existing world by its handle.
    /// </summary>
    /// <param name="handle">Handle representing the world.</param>
    /// <returns>The <see cref="IWorld"/> instance if found; otherwise, <c>null</c>.</returns>
    IWorld? GetWorld(WorldHandle handle);

    /// <summary>
    /// Enumerates the runtime worlds currently managed by the service.
    /// </summary>
    IEnumerable<WorldHandle> GetRuntimeWorlds();

    /// <summary>
    /// Gets the current gameplay mode.
    /// </summary>
    GameMode CurrentMode { get; }

    /// <summary>
    /// Sets the gameplay mode and notifies listeners.
    /// </summary>
    /// <param name="mode">New mode to activate.</param>
    void SetMode(GameMode mode);

    /// <summary>
    /// Event raised when the gameplay mode changes.
    /// </summary>
    event EventHandler<GameMode>? ModeChanged;

    /// <summary>
    /// Associates an authoring node identifier with a runtime entity.
    /// </summary>
    /// <param name="authoringId">Stable authoring identifier.</param>
    /// <param name="runtimeWorld">World that hosts the runtime entity.</param>
    /// <param name="runtimeEntity">Entity handle inside the runtime world.</param>
    void MapAuthoringToRuntime(AuthoringNodeId authoringId, WorldHandle runtimeWorld, EntityHandle runtimeEntity);

    /// <summary>
    /// Retrieves the runtime entity associated with the given authoring node.
    /// </summary>
    /// <param name="authoringId">Stable authoring identifier.</param>
    /// <returns>Runtime entity handle if mapped; otherwise <c>null</c>.</returns>
    EntityHandle? GetRuntimeEntity(AuthoringNodeId authoringId);
}

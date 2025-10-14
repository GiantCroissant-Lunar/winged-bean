using System;
using System.Collections.Generic;

namespace WingedBean.Plugins.ArchECS.Internal;

/// <summary>
/// Platform-agnostic ECS service interface.
/// Manages ECS worlds and provides the entry point for Entity Component System operations.
/// Internal to ArchECS plugin - not part of public API.
/// </summary>
public interface IECSService
{
    /// <summary>
    /// Creates a new ECS world.
    /// </summary>
    IWorld CreateWorld();

    /// <summary>
    /// Gets the authoring world where authoring data is managed.
    /// </summary>
    WorldHandle AuthoringWorld { get; }

    /// <summary>
    /// Gets the default runtime world that gameplay systems should target.
    /// </summary>
    WorldHandle DefaultRuntimeWorld { get; }

    /// <summary>
    /// Creates a new runtime world with the provided name.
    /// </summary>
    WorldHandle CreateRuntimeWorld(string name);

    /// <summary>
    /// Destroys an existing ECS world and releases all associated resources.
    /// </summary>
    void DestroyWorld(IWorld world);

    /// <summary>
    /// Destroys an existing world identified by its handle.
    /// </summary>
    bool DestroyWorld(WorldHandle handle);

    /// <summary>
    /// Gets an existing world by its unique identifier.
    /// </summary>
    IWorld? GetWorld(int worldId);

    /// <summary>
    /// Gets an existing world by its handle.
    /// </summary>
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
    void SetMode(GameMode mode);

    /// <summary>
    /// Event raised when the gameplay mode changes.
    /// </summary>
    event EventHandler<GameMode>? ModeChanged;

    /// <summary>
    /// Associates an authoring node identifier with a runtime entity.
    /// </summary>
    void MapAuthoringToRuntime(AuthoringNodeId authoringId, WorldHandle runtimeWorld, EntityHandle runtimeEntity);

    /// <summary>
    /// Retrieves the runtime entity associated with the given authoring node.
    /// </summary>
    EntityHandle? GetRuntimeEntity(AuthoringNodeId authoringId);
}

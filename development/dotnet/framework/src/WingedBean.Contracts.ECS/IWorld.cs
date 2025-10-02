using System.Collections.Generic;

namespace WingedBean.Contracts.ECS;

/// <summary>
/// Represents an ECS world instance that manages entities and their components.
/// </summary>
public interface IWorld
{
    /// <summary>
    /// Create a new entity in the world.
    /// </summary>
    /// <returns>Handle to the newly created entity.</returns>
    EntityHandle CreateEntity();

    /// <summary>
    /// Destroy an entity and all its components.
    /// </summary>
    /// <param name="entity">Handle to the entity to destroy.</param>
    void DestroyEntity(EntityHandle entity);

    /// <summary>
    /// Add a component to an entity.
    /// </summary>
    /// <typeparam name="T">Component type (must be a struct).</typeparam>
    /// <param name="entity">Handle to the entity.</param>
    /// <param name="component">Component data to attach.</param>
    void AttachComponent<T>(EntityHandle entity, T component) where T : struct;

    /// <summary>
    /// Remove a component from an entity.
    /// </summary>
    /// <typeparam name="T">Component type (must be a struct).</typeparam>
    /// <param name="entity">Handle to the entity.</param>
    void DetachComponent<T>(EntityHandle entity) where T : struct;

    /// <summary>
    /// Get a reference to a component on an entity.
    /// </summary>
    /// <typeparam name="T">Component type (must be a struct).</typeparam>
    /// <param name="entity">Handle to the entity.</param>
    /// <returns>Reference to the component data.</returns>
    ref T GetComponent<T>(EntityHandle entity) where T : struct;

    /// <summary>
    /// Check if an entity has a specific component.
    /// </summary>
    /// <typeparam name="T">Component type (must be a struct).</typeparam>
    /// <param name="entity">Handle to the entity.</param>
    /// <returns>True if the entity has the component, false otherwise.</returns>
    bool HasComponent<T>(EntityHandle entity) where T : struct;

    /// <summary>
    /// Check if an entity exists and is valid.
    /// </summary>
    /// <param name="entity">Handle to the entity.</param>
    /// <returns>True if the entity is alive, false otherwise.</returns>
    bool IsAlive(EntityHandle entity);

    /// <summary>
    /// Create a query to find entities with specific components.
    /// </summary>
    /// <typeparam name="T1">First component type to query for.</typeparam>
    /// <returns>Enumerable collection of entity handles matching the query.</returns>
    IEnumerable<EntityHandle> CreateQuery<T1>() where T1 : struct;

    /// <summary>
    /// Create a query to find entities with specific components.
    /// </summary>
    /// <typeparam name="T1">First component type to query for.</typeparam>
    /// <typeparam name="T2">Second component type to query for.</typeparam>
    /// <returns>Enumerable collection of entity handles matching the query.</returns>
    IEnumerable<EntityHandle> CreateQuery<T1, T2>()
        where T1 : struct
        where T2 : struct;

    /// <summary>
    /// Create a query to find entities with specific components.
    /// </summary>
    /// <typeparam name="T1">First component type to query for.</typeparam>
    /// <typeparam name="T2">Second component type to query for.</typeparam>
    /// <typeparam name="T3">Third component type to query for.</typeparam>
    /// <returns>Enumerable collection of entity handles matching the query.</returns>
    IEnumerable<EntityHandle> CreateQuery<T1, T2, T3>()
        where T1 : struct
        where T2 : struct
        where T3 : struct;

    /// <summary>
    /// Get total entity count in the world.
    /// </summary>
    int EntityCount { get; }
}

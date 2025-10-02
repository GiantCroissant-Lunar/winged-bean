namespace WingedBean.Contracts.ECS;

/// <summary>
/// Represents an entity in the ECS world.
/// Provides access to entity state and component operations.
/// </summary>
public interface IEntity
{
    /// <summary>
    /// Gets the unique identifier for this entity.
    /// </summary>
    int Id { get; }

    /// <summary>
    /// Gets a value indicating whether this entity is still alive (not destroyed).
    /// </summary>
    bool IsAlive { get; }

    /// <summary>
    /// Adds a component to this entity.
    /// </summary>
    /// <typeparam name="T">The component type (must be a struct)</typeparam>
    /// <param name="component">The component data to add</param>
    void AddComponent<T>(T component) where T : struct;

    /// <summary>
    /// Gets a component from this entity.
    /// </summary>
    /// <typeparam name="T">The component type (must be a struct)</typeparam>
    /// <returns>Reference to the component data</returns>
    ref T GetComponent<T>() where T : struct;

    /// <summary>
    /// Checks if this entity has a specific component.
    /// </summary>
    /// <typeparam name="T">The component type (must be a struct)</typeparam>
    /// <returns>True if the entity has the component, false otherwise</returns>
    bool HasComponent<T>() where T : struct;

    /// <summary>
    /// Removes a component from this entity.
    /// </summary>
    /// <typeparam name="T">The component type (must be a struct)</typeparam>
    void RemoveComponent<T>() where T : struct;

    /// <summary>
    /// Sets or updates a component on this entity.
    /// If the component exists, it will be updated; otherwise, it will be added.
    /// </summary>
    /// <typeparam name="T">The component type (must be a struct)</typeparam>
    /// <param name="component">The component data to set</param>
    void SetComponent<T>(T component) where T : struct;
}

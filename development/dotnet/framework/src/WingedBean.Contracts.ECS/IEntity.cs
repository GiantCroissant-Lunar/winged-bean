namespace WingedBean.Contracts.ECS;

/// <summary>
/// Represents an entity in the ECS world.
/// </summary>
public interface IEntity
{
    /// <summary>
    /// Gets the unique identifier for this entity.
    /// </summary>
    int Id { get; }

    /// <summary>
    /// Gets a value indicating whether this entity is alive and valid.
    /// </summary>
    bool IsAlive { get; }

    /// <summary>
    /// Add a component to this entity.
    /// </summary>
    /// <typeparam name="T">Component type</typeparam>
    /// <param name="component">Component data</param>
    void AddComponent<T>(T component) where T : struct;

    /// <summary>
    /// Get a component from this entity.
    /// </summary>
    /// <typeparam name="T">Component type</typeparam>
    /// <returns>Reference to the component</returns>
    ref T GetComponent<T>() where T : struct;

    /// <summary>
    /// Check if this entity has a specific component.
    /// </summary>
    /// <typeparam name="T">Component type</typeparam>
    /// <returns>True if the component exists</returns>
    bool HasComponent<T>() where T : struct;

    /// <summary>
    /// Remove a component from this entity.
    /// </summary>
    /// <typeparam name="T">Component type</typeparam>
    void RemoveComponent<T>() where T : struct;
}

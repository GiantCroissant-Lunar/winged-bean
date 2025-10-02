namespace WingedBean.Contracts.ECS;

/// <summary>
/// Represents an ECS world - an isolated container for entities, components, and systems.
/// This is a placeholder interface that will be fully implemented in issue #65.
/// </summary>
public interface IWorld
{
    /// <summary>
    /// Gets the unique identifier for this world.
    /// </summary>
    int Id { get; }
}

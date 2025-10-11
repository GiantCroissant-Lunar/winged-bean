namespace WingedBean.Plugins.ArchECS.Internal;

/// <summary>
/// Base interface for ECS system implementations.
/// Systems execute logic each frame on entities matching their query criteria.
/// Internal to ArchECS plugin - not part of public API.
/// </summary>
public interface IECSSystem
{
    /// <summary>
    /// Execute system logic for this frame.
    /// </summary>
    /// <param name="world">The world this system should operate on.</param>
    /// <param name="deltaTime">Time elapsed since the last frame in seconds.</param>
    void Execute(IWorld world, float deltaTime);
}

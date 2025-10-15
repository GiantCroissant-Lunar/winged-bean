using Plate.Camera2D;
using WingedBean.Plugins.ArchECS.Internal;
using WingedBean.Plugins.DungeonGame.Components;

namespace WingedBean.Plugins.DungeonGame.Adapters;

/// <summary>
/// Adapter that makes ECS Position component compatible with ICameraTarget.
/// Bridges Plate.Camera2D and Arch ECS.
/// </summary>
public class PositionCameraTarget : ICameraTarget
{
    private readonly IWorld _world;
    private readonly EntityHandle _entity;
    
    /// <summary>
    /// Target's world X position.
    /// Returns 0 if entity is dead or has no Position component.
    /// </summary>
    public float X
    {
        get
        {
            if (!_world.IsAlive(_entity) || !_world.HasComponent<Position>(_entity))
                return 0;
            return _world.GetComponent<Position>(_entity).X;
        }
    }
    
    /// <summary>
    /// Target's world Y position.
    /// Returns 0 if entity is dead or has no Position component.
    /// </summary>
    public float Y
    {
        get
        {
            if (!_world.IsAlive(_entity) || !_world.HasComponent<Position>(_entity))
                return 0;
            return _world.GetComponent<Position>(_entity).Y;
        }
    }
    
    /// <summary>
    /// Create a new position camera target adapter.
    /// </summary>
    /// <param name="world">ECS world</param>
    /// <param name="entity">Entity to follow</param>
    public PositionCameraTarget(IWorld world, EntityHandle entity)
    {
        _world = world;
        _entity = entity;
    }
}

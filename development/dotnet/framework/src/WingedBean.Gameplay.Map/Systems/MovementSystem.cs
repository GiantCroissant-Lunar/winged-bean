using Arch.Core;
using WingedBean.Gameplay.Map.Components;

namespace WingedBean.Gameplay.Map.Systems;

/// <summary>
/// System that handles entity movement and collision detection.
/// </summary>
public class MovementSystem
{
    private readonly World _world;

    public MovementSystem(World world)
    {
        _world = world;
    }

    /// <summary>
    /// Attempts to move an entity to a new position.
    /// Returns true if the move was successful, false if blocked.
    /// </summary>
    public bool TryMove(Entity entity, int dx, int dy)
    {
        if (!_world.Has<PositionComponent>(entity))
        {
            return false;
        }

        var position = _world.Get<PositionComponent>(entity);
        var newX = position.X + dx;
        var newY = position.Y + dy;

        // Get the map
        var mapQuery = new QueryDescription().WithAll<MapComponent>();
        var canMove = false;

        _world.Query(in mapQuery, (Entity mapEntity, ref MapComponent mapComp) =>
        {
            var map = mapComp.Map;

            // Check if the tile is walkable
            if (!map.IsWalkable(newX, newY))
            {
                return;
            }

            // Check for entity collisions
            if (IsPositionOccupied(newX, newY))
            {
                return;
            }

            // Move is valid
            canMove = true;
        });

        if (canMove)
        {
            // Update position
            _world.Set(entity, new PositionComponent(newX, newY));
            return true;
        }

        return false;
    }

    /// <summary>
    /// Checks if a position is occupied by an entity that blocks movement.
    /// </summary>
    public bool IsPositionOccupied(int x, int y)
    {
        var query = new QueryDescription()
            .WithAll<PositionComponent, BlocksMovementComponent>();

        var occupied = false;
        _world.Query(in query, (ref PositionComponent pos) =>
        {
            if (pos.X == x && pos.Y == y)
            {
                occupied = true;
            }
        });

        return occupied;
    }

    /// <summary>
    /// Gets the entity at the specified position, if any.
    /// </summary>
    public Entity? GetEntityAt(int x, int y)
    {
        var query = new QueryDescription().WithAll<PositionComponent>();

        Entity? foundEntity = null;
        _world.Query(in query, (Entity entity, ref PositionComponent pos) =>
        {
            if (pos.X == x && pos.Y == y)
            {
                foundEntity = entity;
            }
        });

        return foundEntity;
    }

    /// <summary>
    /// Teleports an entity to a new position without collision checking.
    /// </summary>
    public void Teleport(Entity entity, int x, int y)
    {
        if (!_world.Has<PositionComponent>(entity))
        {
            return;
        }

        _world.Set(entity, new PositionComponent(x, y));
    }
}

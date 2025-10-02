using WingedBean.Contracts.ECS;
using WingedBean.Plugins.DungeonGame.Components;

namespace WingedBean.Plugins.DungeonGame.Systems;

/// <summary>
/// System responsible for updating entity positions based on movement input.
/// Processes entities with Position components and applies movement logic.
/// </summary>
public class MovementSystem : IECSSystem
{
    private const int DungeonWidth = 80;
    private const int DungeonHeight = 24;
    private IWorld? _world;

    public void Execute(IECSService ecs, float deltaTime)
    {
        // Get or create the world reference
        _world ??= ecs.GetWorld(0) ?? ecs.CreateWorld();

        // Query all entities with Position components
        // Movement is typically triggered by input handlers that modify Position directly
        // This system ensures positions stay within bounds
        foreach (var entity in _world.CreateQuery<Position>())
        {
            ref var pos = ref _world.GetComponent<Position>(entity);

            // Clamp to dungeon bounds
            pos.X = Math.Clamp(pos.X, 0, DungeonWidth - 1);
            pos.Y = Math.Clamp(pos.Y, 0, DungeonHeight - 1);

            // Check for collisions with blocking entities
            if (_world.HasComponent<Player>(entity))
            {
                // Check if there's a blocking entity at the player's position
                foreach (var otherEntity in _world.CreateQuery<Position>())
                {
                    if (entity == otherEntity)
                        continue;

                    // Only check entities with Blocking component
                    if (!_world.HasComponent<Blocking>(otherEntity))
                        continue;

                    ref var otherPos = ref _world.GetComponent<Position>(otherEntity);
                    var blocking = _world.GetComponent<Blocking>(otherEntity);

                    // If positions overlap and entity blocks movement
                    if (pos.X == otherPos.X && pos.Y == otherPos.Y && pos.Floor == otherPos.Floor &&
                        blocking.BlocksMovement)
                    {
                        // In a real implementation, we'd store previous position and revert
                        // For now, just mark that we detected a collision
                        break;
                    }
                }
            }
        }
    }
}

using WingedBean.Contracts.ECS;
using WingedBean.Plugins.DungeonGame.Components;

namespace WingedBean.Plugins.DungeonGame.Systems;

/// <summary>
/// System responsible for AI behavior of enemy entities.
/// Handles pathfinding, aggro, and basic enemy AI states.
/// </summary>
public class AISystem : IECSSystem
{
    public void Execute(IECSService ecs, IWorld world, float deltaTime)
    {
        // Find player position
        Position? playerPosition = null;
        EntityHandle? playerHandle = null;

        foreach (var entity in world.CreateQuery<Player, Position>())
        {
            playerPosition = world.GetComponent<Position>(entity);
            playerHandle = entity;
            break;
        }

        if (!playerPosition.HasValue || !playerHandle.HasValue)
            return;

        // Process each enemy
        foreach (var enemyEntity in world.CreateQuery<Enemy, Position>())
        {
            ref var enemy = ref world.GetComponent<Enemy>(enemyEntity);
            ref var enemyPos = ref world.GetComponent<Position>(enemyEntity);

            // Calculate distance to player
            float distance = CalculateDistance(enemyPos, playerPosition.Value);

            // Update AI state based on distance
            switch (enemy.State)
            {
                case AIState.Idle:
                    // If player is within aggro range, start chasing
                    if (distance <= enemy.AggroRange)
                    {
                        enemy.State = AIState.Chase;
                        enemy.Target = playerHandle.Value;
                    }
                    break;

                case AIState.Patrol:
                    // Simple patrol logic - random movement
                    if (distance <= enemy.AggroRange)
                    {
                        enemy.State = AIState.Chase;
                        enemy.Target = playerHandle.Value;
                    }
                    else
                    {
                        // Random patrol movement
                        if (Random.Shared.NextDouble() < 0.1) // 10% chance to move each frame
                        {
                            int dx = Random.Shared.Next(-1, 2);
                            int dy = Random.Shared.Next(-1, 2);
                            enemyPos.X += dx;
                            enemyPos.Y += dy;
                        }
                    }
                    break;

                case AIState.Chase:
                    // Check if player is out of aggro range
                    if (distance > enemy.AggroRange * 1.5f)
                    {
                        enemy.State = AIState.Idle;
                        enemy.Target = null;
                    }
                    // If adjacent to player, switch to attack
                    else if (distance <= 1.5f)
                    {
                        enemy.State = AIState.Attack;
                    }
                    // Move towards player
                    else
                    {
                        MoveTowards(ref enemyPos, playerPosition.Value);
                    }
                    break;

                case AIState.Attack:
                    // Attack state is handled by CombatSystem
                    // Check if player moved away
                    if (distance > 1.5f)
                    {
                        enemy.State = AIState.Chase;
                    }
                    break;

                case AIState.Flee:
                    // Move away from player
                    if (distance > enemy.AggroRange)
                    {
                        enemy.State = AIState.Idle;
                    }
                    else
                    {
                        MoveAwayFrom(ref enemyPos, playerPosition.Value);
                    }
                    break;
            }
        }
    }

    private static float CalculateDistance(Position a, Position b)
    {
        if (a.Floor != b.Floor)
            return float.MaxValue;

        int dx = a.X - b.X;
        int dy = a.Y - b.Y;
        return MathF.Sqrt(dx * dx + dy * dy);
    }

    private static void MoveTowards(ref Position current, Position target)
    {
        // Simple pathfinding - move one step towards target
        if (current.X < target.X)
            current.X++;
        else if (current.X > target.X)
            current.X--;

        if (current.Y < target.Y)
            current.Y++;
        else if (current.Y > target.Y)
            current.Y--;
    }

    private static void MoveAwayFrom(ref Position current, Position target)
    {
        // Simple fleeing - move one step away from target
        if (current.X < target.X)
            current.X--;
        else if (current.X > target.X)
            current.X++;

        if (current.Y < target.Y)
            current.Y--;
        else if (current.Y > target.Y)
            current.Y++;
    }
}

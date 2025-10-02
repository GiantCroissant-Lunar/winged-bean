using WingedBean.Contracts.ECS;
using WingedBean.Plugins.DungeonGame.Components;

namespace WingedBean.Plugins.DungeonGame.Systems;

/// <summary>
/// System responsible for processing combat between entities.
/// Handles damage calculation, health management, and entity death.
/// </summary>
public class CombatSystem : IECSSystem
{
    private IWorld? _world;

    public void Execute(IECSService ecs, float deltaTime)
    {
        // Get or create the world reference
        _world ??= ecs.GetWorld(0) ?? ecs.CreateWorld();

        // Find player entity
        EntityHandle? playerHandle = null;
        foreach (var entity in _world.CreateQuery<Player, Position, Stats>())
        {
            playerHandle = entity;
            break;
        }

        if (!playerHandle.HasValue)
            return;

        ref var playerPos = ref _world.GetComponent<Position>(playerHandle.Value);
        ref var playerStats = ref _world.GetComponent<Stats>(playerHandle.Value);

        // Track entities to destroy (can't destroy during iteration)
        var entitiesToDestroy = new List<EntityHandle>();

        // Check all enemies for combat
        foreach (var enemyEntity in _world.CreateQuery<Enemy, Position, Stats>())
        {
            ref var enemyPos = ref _world.GetComponent<Position>(enemyEntity);
            ref var enemyStats = ref _world.GetComponent<Stats>(enemyEntity);

            // Combat occurs when entities are adjacent (including diagonals)
            if (IsAdjacent(playerPos, enemyPos))
            {
                // Calculate damage (attacker's strength minus defender's defense)
                int damageToEnemy = Math.Max(1, playerStats.Strength - enemyStats.Defense / 2);
                int damageToPlayer = Math.Max(1, enemyStats.Strength - playerStats.Defense / 2);

                // Apply damage
                enemyStats.CurrentHP -= damageToEnemy;
                playerStats.CurrentHP -= damageToPlayer;

                // Check for enemy death
                if (enemyStats.CurrentHP <= 0)
                {
                    entitiesToDestroy.Add(enemyEntity);

                    // Award experience to player
                    playerStats.Experience += 10;

                    // Check for level up
                    if (playerStats.Experience >= playerStats.Level * 100)
                    {
                        playerStats.Level++;
                        playerStats.Experience = 0;

                        // Increase stats on level up
                        playerStats.MaxHP += 10;
                        playerStats.CurrentHP = playerStats.MaxHP;
                        playerStats.Strength += 2;
                        playerStats.Defense += 1;
                    }
                }

                // Check for player death
                if (playerStats.CurrentHP <= 0)
                {
                    playerStats.CurrentHP = 0;
                    // In a full implementation, this would trigger game over
                }
            }
        }

        // Destroy dead entities
        foreach (var entity in entitiesToDestroy)
        {
            _world.DestroyEntity(entity);
        }
    }

    private static bool IsAdjacent(Position a, Position b)
    {
        // Check if two positions are adjacent (including diagonals)
        return Math.Abs(a.X - b.X) <= 1 &&
               Math.Abs(a.Y - b.Y) <= 1 &&
               a.Floor == b.Floor &&
               !(a.X == b.X && a.Y == b.Y); // Not the same position
    }
}

using Xunit;
using FluentAssertions;
using ConsoleDungeon.Systems;
using ConsoleDungeon.Components;
using WingedBean.Contracts.ECS;
using WingedBean.Plugins.ArchECS;

namespace ConsoleDungeon.Tests.Systems;

public class AISystemTests
{
    private readonly IECSService _ecsService;
    private readonly IWorld _world;
    private readonly AISystem _system;

    public AISystemTests()
    {
        _ecsService = new ArchECSService();
        _world = _ecsService.CreateWorld();
        _system = new AISystem();
    }

    [Fact]
    public void Execute_WithNoPlayer_DoesNothing()
    {
        // Arrange
        var enemy = CreateEnemy(10, 10);

        // Act & Assert - Should not throw
        var act = () => _system.Execute(_ecsService, 0.016f);
        act.Should().NotThrow();
    }

    [Fact]
    public void Execute_IdleEnemyNearPlayer_StartsChasing()
    {
        // Arrange
        CreatePlayer(10, 10);
        var enemy = CreateEnemy(12, 10, aggroRange: 5.0f);

        // Act
        _system.Execute(_ecsService, 0.016f);

        // Assert
        var enemyComponent = _world.GetComponent<Enemy>(enemy);
        enemyComponent.State.Should().Be(AIState.Chase);
        enemyComponent.Target.Should().NotBeNull();
    }

    [Fact]
    public void Execute_IdleEnemyFarFromPlayer_StaysIdle()
    {
        // Arrange
        CreatePlayer(10, 10);
        var enemy = CreateEnemy(30, 30, aggroRange: 5.0f);

        // Act
        _system.Execute(_ecsService, 0.016f);

        // Assert
        var enemyComponent = _world.GetComponent<Enemy>(enemy);
        enemyComponent.State.Should().Be(AIState.Idle);
        enemyComponent.Target.Should().BeNull();
    }

    [Fact]
    public void Execute_ChasingEnemyNearPlayer_MovesTowardsPlayer()
    {
        // Arrange
        var player = CreatePlayer(10, 10);
        var enemy = CreateEnemy(15, 15, aggroRange: 10.0f);

        var initialPos = _world.GetComponent<Position>(enemy);

        // Set enemy to chase state
        var enemyComponent = _world.GetComponent<Enemy>(enemy);
        enemyComponent.State = AIState.Chase;
        _world.AttachComponent(enemy, enemyComponent);

        // Act
        _system.Execute(_ecsService, 0.016f);

        // Assert
        var newPos = _world.GetComponent<Position>(enemy);
        var playerPos = _world.GetComponent<Position>(player);

        // Enemy should be closer to player
        var initialDistance = Math.Abs(initialPos.X - playerPos.X) + Math.Abs(initialPos.Y - playerPos.Y);
        var newDistance = Math.Abs(newPos.X - playerPos.X) + Math.Abs(newPos.Y - playerPos.Y);

        newDistance.Should().BeLessThanOrEqualTo(initialDistance);
    }

    [Fact]
    public void Execute_ChasingEnemyAdjacentToPlayer_SwitchesToAttack()
    {
        // Arrange
        CreatePlayer(10, 10);
        var enemy = CreateEnemy(11, 10, aggroRange: 10.0f);

        // Set enemy to chase state
        var enemyComponent = _world.GetComponent<Enemy>(enemy);
        enemyComponent.State = AIState.Chase;
        _world.AttachComponent(enemy, enemyComponent);

        // Act
        _system.Execute(_ecsService, 0.016f);

        // Assert
        enemyComponent = _world.GetComponent<Enemy>(enemy);
        enemyComponent.State.Should().Be(AIState.Attack);
    }

    [Fact]
    public void Execute_AttackingEnemyMovesAway_SwitchesToChase()
    {
        // Arrange
        CreatePlayer(10, 10);
        var enemy = CreateEnemy(13, 13, aggroRange: 10.0f);

        // Set enemy to attack state
        var enemyComponent = _world.GetComponent<Enemy>(enemy);
        enemyComponent.State = AIState.Attack;
        _world.AttachComponent(enemy, enemyComponent);

        // Act
        _system.Execute(_ecsService, 0.016f);

        // Assert
        enemyComponent = _world.GetComponent<Enemy>(enemy);
        enemyComponent.State.Should().Be(AIState.Chase);
    }

    private EntityHandle CreatePlayer(int x, int y)
    {
        var entity = _world.CreateEntity();
        _world.AttachComponent(entity, new Player());
        _world.AttachComponent(entity, new Position(x, y, 1));
        return entity;
    }

    private EntityHandle CreateEnemy(int x, int y, float aggroRange = 5.0f)
    {
        var entity = _world.CreateEntity();
        _world.AttachComponent(entity, new Position(x, y, 1));
        _world.AttachComponent(entity, new Enemy
        {
            Type = EnemyType.Goblin,
            State = AIState.Idle,
            AggroRange = aggroRange,
            Target = null
        });
        return entity;
    }
}

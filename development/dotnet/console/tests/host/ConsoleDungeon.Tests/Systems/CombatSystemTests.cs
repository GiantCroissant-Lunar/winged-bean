using Xunit;
using FluentAssertions;
using ConsoleDungeon.Systems;
using ConsoleDungeon.Components;
using WingedBean.Contracts.ECS;
using WingedBean.Plugins.ArchECS;

namespace ConsoleDungeon.Tests.Systems;

public class CombatSystemTests
{
    private readonly IECSService _ecsService;
    private readonly IWorld _world;
    private readonly CombatSystem _system;

    public CombatSystemTests()
    {
        _ecsService = new ArchECSService();
        _world = _ecsService.CreateWorld();
        _system = new CombatSystem();
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
    public void Execute_PlayerAndEnemyAdjacent_AppliesDamage()
    {
        // Arrange
        var player = CreatePlayer(10, 10, hp: 100, strength: 10, defense: 5);
        var enemy = CreateEnemy(11, 10, hp: 20, strength: 5, defense: 2);

        var initialPlayerHP = _world.GetComponent<Stats>(player).CurrentHP;
        var initialEnemyHP = _world.GetComponent<Stats>(enemy).CurrentHP;

        // Act
        _system.Execute(_ecsService, 0.016f);

        // Assert
        var playerStats = _world.GetComponent<Stats>(player);
        var enemyAlive = _world.IsAlive(enemy);

        playerStats.CurrentHP.Should().BeLessThan(initialPlayerHP);
        if (enemyAlive)
        {
            var enemyStats = _world.GetComponent<Stats>(enemy);
            enemyStats.CurrentHP.Should().BeLessThan(initialEnemyHP);
        }
    }

    [Fact]
    public void Execute_PlayerAndEnemyNotAdjacent_NoDamage()
    {
        // Arrange
        var player = CreatePlayer(10, 10, hp: 100, strength: 10, defense: 5);
        var enemy = CreateEnemy(20, 20, hp: 20, strength: 5, defense: 2);

        var initialPlayerHP = _world.GetComponent<Stats>(player).CurrentHP;

        // Act
        _system.Execute(_ecsService, 0.016f);

        // Assert
        var playerStats = _world.GetComponent<Stats>(player);
        var enemyStats = _world.GetComponent<Stats>(enemy);

        playerStats.CurrentHP.Should().Be(initialPlayerHP);
        enemyStats.CurrentHP.Should().Be(20);
    }

    [Fact]
    public void Execute_EnemyKilled_DestroysEntity()
    {
        // Arrange
        var player = CreatePlayer(10, 10, hp: 100, strength: 100, defense: 5);
        var enemy = CreateEnemy(11, 10, hp: 1, strength: 1, defense: 0);

        // Act
        _system.Execute(_ecsService, 0.016f);

        // Assert
        _world.IsAlive(enemy).Should().BeFalse();
    }

    [Fact]
    public void Execute_EnemyKilled_AwardsExperience()
    {
        // Arrange
        var player = CreatePlayer(10, 10, hp: 100, strength: 100, defense: 5);
        var enemy = CreateEnemy(11, 10, hp: 1, strength: 1, defense: 0);

        var initialXP = _world.GetComponent<Stats>(player).Experience;

        // Act
        _system.Execute(_ecsService, 0.016f);

        // Assert
        var playerStats = _world.GetComponent<Stats>(player);
        playerStats.Experience.Should().BeGreaterThan(initialXP);
    }

    [Fact]
    public void Execute_PlayerDies_SetsHPToZero()
    {
        // Arrange
        var player = CreatePlayer(10, 10, hp: 1, strength: 1, defense: 0);
        var enemy = CreateEnemy(11, 10, hp: 100, strength: 100, defense: 0);

        // Act
        _system.Execute(_ecsService, 0.016f);

        // Assert
        var playerStats = _world.GetComponent<Stats>(player);
        playerStats.CurrentHP.Should().Be(0);
    }

    private EntityHandle CreatePlayer(int x, int y, int hp = 100, int strength = 10, int defense = 5)
    {
        var entity = _world.CreateEntity();
        _world.AttachComponent(entity, new Player());
        _world.AttachComponent(entity, new Position(x, y, 1));
        _world.AttachComponent(entity, new Stats
        {
            MaxHP = hp,
            CurrentHP = hp,
            Strength = strength,
            Defense = defense,
            Level = 1,
            Experience = 0
        });
        return entity;
    }

    private EntityHandle CreateEnemy(int x, int y, int hp = 20, int strength = 5, int defense = 2)
    {
        var entity = _world.CreateEntity();
        _world.AttachComponent(entity, new Position(x, y, 1));
        _world.AttachComponent(entity, new Enemy
        {
            Type = EnemyType.Goblin,
            State = AIState.Idle,
            AggroRange = 5.0f
        });
        _world.AttachComponent(entity, new Stats
        {
            MaxHP = hp,
            CurrentHP = hp,
            Strength = strength,
            Defense = defense
        });
        return entity;
    }
}

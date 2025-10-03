using Xunit;
using FluentAssertions;
using ConsoleDungeon.Systems;
using ConsoleDungeon.Components;
using WingedBean.Contracts.ECS;
using WingedBean.Plugins.ArchECS;

namespace ConsoleDungeon.Tests.Systems;

public class MovementSystemTests
{
    private readonly IECSService _ecsService;
    private readonly IWorld _world;
    private readonly MovementSystem _system;

    public MovementSystemTests()
    {
        _ecsService = new ArchECSService();
        _world = _ecsService.CreateWorld();
        _system = new MovementSystem();
    }

    [Fact]
    public void Execute_ClampsPositionToUpperBounds()
    {
        // Arrange
        var entity = _world.CreateEntity();
        _world.AttachComponent(entity, new Position(100, 100, 1)); // Out of bounds

        // Act
        _system.Execute(_ecsService, 0.016f);

        // Assert
        var pos = _world.GetComponent<Position>(entity);
        pos.X.Should().BeLessOrEqualTo(79); // DungeonWidth - 1
        pos.Y.Should().BeLessOrEqualTo(23); // DungeonHeight - 1
    }

    [Fact]
    public void Execute_ClampsPositionToLowerBounds()
    {
        // Arrange
        var entity = _world.CreateEntity();
        _world.AttachComponent(entity, new Position(-10, -10, 1)); // Out of bounds

        // Act
        _system.Execute(_ecsService, 0.016f);

        // Assert
        var pos = _world.GetComponent<Position>(entity);
        pos.X.Should().BeGreaterOrEqualTo(0);
        pos.Y.Should().BeGreaterOrEqualTo(0);
    }

    [Fact]
    public void Execute_AllowsValidPositions()
    {
        // Arrange
        var entity = _world.CreateEntity();
        _world.AttachComponent(entity, new Position(40, 12, 1));

        // Act
        _system.Execute(_ecsService, 0.016f);

        // Assert
        var pos = _world.GetComponent<Position>(entity);
        pos.X.Should().Be(40);
        pos.Y.Should().Be(12);
        pos.Floor.Should().Be(1);
    }

    [Fact]
    public void Execute_ProcessesMultipleEntities()
    {
        // Arrange
        var entity1 = _world.CreateEntity();
        var entity2 = _world.CreateEntity();
        _world.AttachComponent(entity1, new Position(100, 50, 1));
        _world.AttachComponent(entity2, new Position(-5, -5, 1));

        // Act
        _system.Execute(_ecsService, 0.016f);

        // Assert
        var pos1 = _world.GetComponent<Position>(entity1);
        var pos2 = _world.GetComponent<Position>(entity2);

        pos1.X.Should().BeLessOrEqualTo(79);
        pos1.Y.Should().BeLessOrEqualTo(23);
        pos2.X.Should().BeGreaterOrEqualTo(0);
        pos2.Y.Should().BeGreaterOrEqualTo(0);
    }

    [Fact]
    public void Execute_IgnoresEntitiesWithoutPosition()
    {
        // Arrange
        var entityWithoutPos = _world.CreateEntity();
        _world.AttachComponent(entityWithoutPos, new Player());

        // Act & Assert - Should not throw
        var act = () => _system.Execute(_ecsService, 0.016f);
        act.Should().NotThrow();
    }
}

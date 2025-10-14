using Arch.Core;
using WingedBean.Gameplay.Map.Components;
using WingedBean.Gameplay.Map.Data;
using WingedBean.Gameplay.Map.Systems;

namespace WingedBean.Gameplay.Map.Tests.Systems;

public class MovementSystemTests : IDisposable
{
    private readonly World _world;
    private readonly MovementSystem _movementSystem;
    private readonly Entity _mapEntity;

    public MovementSystemTests()
    {
        _world = World.Create();
        _movementSystem = new MovementSystem(_world);

        // Create a test map
        var map = DungeonMap.CreateTestMap();
        _mapEntity = _world.Create(new MapComponent(map, 1));
    }

    public void Dispose()
    {
        World.Destroy(_world);
    }

    [Fact]
    public void TryMove_SucceedsOnFloorTile()
    {
        // Arrange
        var entity = _world.Create(new PositionComponent(10, 10));

        // Act
        var result = _movementSystem.TryMove(entity, 1, 0);

        // Assert
        Assert.True(result);
        var pos = _world.Get<PositionComponent>(entity);
        Assert.Equal(11, pos.X);
        Assert.Equal(10, pos.Y);
    }

    [Fact]
    public void TryMove_FailsOnWallTile()
    {
        // Arrange
        var entity = _world.Create(new PositionComponent(10, 10));

        // Act - try to move into wall (far left)
        var result = _movementSystem.TryMove(entity, -10, 0);

        // Assert
        Assert.False(result);
        var pos = _world.Get<PositionComponent>(entity);
        Assert.Equal(10, pos.X);
        Assert.Equal(10, pos.Y);
    }

    [Fact]
    public void TryMove_FailsWhenPositionOccupied()
    {
        // Arrange
        var entity1 = _world.Create(
            new PositionComponent(10, 10),
            BlocksMovementComponent.Create()
        );
        var entity2 = _world.Create(new PositionComponent(10, 11));

        // Act - try to move entity2 into entity1's position
        var result = _movementSystem.TryMove(entity2, 0, -1);

        // Assert
        Assert.False(result);
        var pos = _world.Get<PositionComponent>(entity2);
        Assert.Equal(10, pos.X);
        Assert.Equal(11, pos.Y);
    }

    [Fact]
    public void TryMove_SucceedsWhenTargetEntityDoesNotBlockMovement()
    {
        // Arrange
        var entity1 = _world.Create(new PositionComponent(10, 10));
        var entity2 = _world.Create(new PositionComponent(10, 11));

        // Act - entity2 can move into entity1's position since entity1 doesn't block
        var result = _movementSystem.TryMove(entity2, 0, -1);

        // Assert
        Assert.True(result);
        var pos = _world.Get<PositionComponent>(entity2);
        Assert.Equal(10, pos.X);
        Assert.Equal(10, pos.Y);
    }

    [Fact]
    public void IsPositionOccupied_ReturnsTrueWhenOccupied()
    {
        // Arrange
        _world.Create(
            new PositionComponent(10, 10),
            BlocksMovementComponent.Create()
        );

        // Act
        var result = _movementSystem.IsPositionOccupied(10, 10);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IsPositionOccupied_ReturnsFalseWhenEmpty()
    {
        // Act
        var result = _movementSystem.IsPositionOccupied(10, 10);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void GetEntityAt_ReturnsEntityAtPosition()
    {
        // Arrange
        var entity = _world.Create(new PositionComponent(10, 10));

        // Act
        var result = _movementSystem.GetEntityAt(10, 10);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(entity, result.Value);
    }

    [Fact]
    public void GetEntityAt_ReturnsNullWhenNoEntity()
    {
        // Act
        var result = _movementSystem.GetEntityAt(10, 10);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void Teleport_MovesEntityWithoutCollisionChecking()
    {
        // Arrange
        var entity = _world.Create(new PositionComponent(10, 10));

        // Act - teleport to a wall (should succeed)
        _movementSystem.Teleport(entity, 0, 0);

        // Assert
        var pos = _world.Get<PositionComponent>(entity);
        Assert.Equal(0, pos.X);
        Assert.Equal(0, pos.Y);
    }
}

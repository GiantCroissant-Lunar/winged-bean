using FluentAssertions;
using Plate.CrossMilo.Contracts.ECS;
using Xunit;

namespace WingedBean.Plugins.ArchECS.Tests;

// Test components
public struct Position
{
    public int X;
    public int Y;
}

public struct Velocity
{
    public float X;
    public float Y;
}

public struct Health
{
    public int Value;
}

/// <summary>
/// Unit tests for ArchWorld implementation.
/// </summary>
public class ArchWorldTests
{
    private IWorld CreateWorld()
    {
        var service = new ArchECSService();
        return service.CreateWorld();
    }

    [Fact]
    public void CreateEntity_ReturnsValidHandle()
    {
        // Arrange
        var world = CreateWorld();

        // Act
        var entity = world.CreateEntity();

        // Assert
        entity.Should().NotBe(default);
    }

    [Fact]
    public void CreateEntity_MultipleTimes_ReturnsDifferentHandles()
    {
        // Arrange
        var world = CreateWorld();

        // Act
        var entity1 = world.CreateEntity();
        var entity2 = world.CreateEntity();

        // Assert
        entity1.Should().NotBe(entity2);
    }

    [Fact]
    public void AttachComponent_AddsComponentToEntity()
    {
        // Arrange
        var world = CreateWorld();
        var entity = world.CreateEntity();
        var position = new Position { X = 10, Y = 20 };

        // Act
        world.AttachComponent(entity, position);

        // Assert
        world.HasComponent<Position>(entity).Should().BeTrue();
    }

    [Fact]
    public void GetComponent_ReturnsCorrectComponent()
    {
        // Arrange
        var world = CreateWorld();
        var entity = world.CreateEntity();
        var position = new Position { X = 10, Y = 20 };
        world.AttachComponent(entity, position);

        // Act
        ref var retrievedPosition = ref world.GetComponent<Position>(entity);

        // Assert
        retrievedPosition.X.Should().Be(10);
        retrievedPosition.Y.Should().Be(20);
    }

    [Fact]
    public void GetComponent_CanModifyComponent()
    {
        // Arrange
        var world = CreateWorld();
        var entity = world.CreateEntity();
        var position = new Position { X = 10, Y = 20 };
        world.AttachComponent(entity, position);

        // Act
        ref var retrievedPosition = ref world.GetComponent<Position>(entity);
        retrievedPosition.X = 100;
        retrievedPosition.Y = 200;

        // Assert
        ref var updatedPosition = ref world.GetComponent<Position>(entity);
        updatedPosition.X.Should().Be(100);
        updatedPosition.Y.Should().Be(200);
    }

    [Fact]
    public void DetachComponent_RemovesComponentFromEntity()
    {
        // Arrange
        var world = CreateWorld();
        var entity = world.CreateEntity();
        var position = new Position { X = 10, Y = 20 };
        world.AttachComponent(entity, position);

        // Act
        world.DetachComponent<Position>(entity);

        // Assert
        world.HasComponent<Position>(entity).Should().BeFalse();
    }

    [Fact]
    public void HasComponent_ReturnsFalseForNonExistentComponent()
    {
        // Arrange
        var world = CreateWorld();
        var entity = world.CreateEntity();

        // Act
        var hasComponent = world.HasComponent<Position>(entity);

        // Assert
        hasComponent.Should().BeFalse();
    }

    [Fact]
    public void HasComponent_ReturnsTrueForExistingComponent()
    {
        // Arrange
        var world = CreateWorld();
        var entity = world.CreateEntity();
        var position = new Position { X = 10, Y = 20 };
        world.AttachComponent(entity, position);

        // Act
        var hasComponent = world.HasComponent<Position>(entity);

        // Assert
        hasComponent.Should().BeTrue();
    }

    [Fact]
    public void IsAlive_ReturnsTrueForLiveEntity()
    {
        // Arrange
        var world = CreateWorld();
        var entity = world.CreateEntity();

        // Act
        var isAlive = world.IsAlive(entity);

        // Assert
        isAlive.Should().BeTrue();
    }

    [Fact]
    public void IsAlive_ReturnsFalseForDestroyedEntity()
    {
        // Arrange
        var world = CreateWorld();
        var entity = world.CreateEntity();

        // Act
        world.DestroyEntity(entity);
        var isAlive = world.IsAlive(entity);

        // Assert
        isAlive.Should().BeFalse();
    }

    [Fact]
    public void DestroyEntity_RemovesEntity()
    {
        // Arrange
        var world = CreateWorld();
        var entity = world.CreateEntity();
        var initialCount = world.EntityCount;

        // Act
        world.DestroyEntity(entity);

        // Assert
        world.EntityCount.Should().BeLessThan(initialCount);
        world.IsAlive(entity).Should().BeFalse();
    }

    [Fact]
    public void EntityCount_ReflectsNumberOfEntities()
    {
        // Arrange
        var world = CreateWorld();
        var initialCount = world.EntityCount;

        // Act
        world.CreateEntity();
        world.CreateEntity();
        world.CreateEntity();

        // Assert
        world.EntityCount.Should().Be(initialCount + 3);
    }

    [Fact]
    public void CreateQuery_SingleComponent_ReturnsMatchingEntities()
    {
        // Arrange
        var world = CreateWorld();
        var entity1 = world.CreateEntity();
        var entity2 = world.CreateEntity();
        var entity3 = world.CreateEntity();

        world.AttachComponent(entity1, new Position { X = 1, Y = 1 });
        world.AttachComponent(entity2, new Position { X = 2, Y = 2 });
        world.AttachComponent(entity3, new Velocity { X = 1.0f, Y = 1.0f }); // No Position

        // Act
        var results = world.CreateQuery<Position>().ToList();

        // Assert
        results.Should().HaveCount(2);
        results.Should().Contain(entity1);
        results.Should().Contain(entity2);
        results.Should().NotContain(entity3);
    }

    [Fact]
    public void CreateQuery_TwoComponents_ReturnsMatchingEntities()
    {
        // Arrange
        var world = CreateWorld();
        var entity1 = world.CreateEntity();
        var entity2 = world.CreateEntity();
        var entity3 = world.CreateEntity();

        world.AttachComponent(entity1, new Position { X = 1, Y = 1 });
        world.AttachComponent(entity1, new Velocity { X = 1.0f, Y = 1.0f });

        world.AttachComponent(entity2, new Position { X = 2, Y = 2 });
        // entity2 has no Velocity

        world.AttachComponent(entity3, new Velocity { X = 3.0f, Y = 3.0f });
        // entity3 has no Position

        // Act
        var results = world.CreateQuery<Position, Velocity>().ToList();

        // Assert
        results.Should().HaveCount(1);
        results.Should().Contain(entity1);
        results.Should().NotContain(entity2);
        results.Should().NotContain(entity3);
    }

    [Fact]
    public void CreateQuery_ThreeComponents_ReturnsMatchingEntities()
    {
        // Arrange
        var world = CreateWorld();
        var entity1 = world.CreateEntity();
        var entity2 = world.CreateEntity();

        world.AttachComponent(entity1, new Position { X = 1, Y = 1 });
        world.AttachComponent(entity1, new Velocity { X = 1.0f, Y = 1.0f });
        world.AttachComponent(entity1, new Health { Value = 100 });

        world.AttachComponent(entity2, new Position { X = 2, Y = 2 });
        world.AttachComponent(entity2, new Velocity { X = 2.0f, Y = 2.0f });
        // entity2 has no Health

        // Act
        var results = world.CreateQuery<Position, Velocity, Health>().ToList();

        // Assert
        results.Should().HaveCount(1);
        results.Should().Contain(entity1);
        results.Should().NotContain(entity2);
    }

    [Fact]
    public void MultipleComponents_WorkCorrectly()
    {
        // Arrange
        var world = CreateWorld();
        var entity = world.CreateEntity();

        // Act
        world.AttachComponent(entity, new Position { X = 10, Y = 20 });
        world.AttachComponent(entity, new Velocity { X = 1.5f, Y = 2.5f });
        world.AttachComponent(entity, new Health { Value = 100 });

        // Assert
        world.HasComponent<Position>(entity).Should().BeTrue();
        world.HasComponent<Velocity>(entity).Should().BeTrue();
        world.HasComponent<Health>(entity).Should().BeTrue();

        ref var position = ref world.GetComponent<Position>(entity);
        position.X.Should().Be(10);
        position.Y.Should().Be(20);

        ref var velocity = ref world.GetComponent<Velocity>(entity);
        velocity.X.Should().Be(1.5f);
        velocity.Y.Should().Be(2.5f);

        ref var health = ref world.GetComponent<Health>(entity);
        health.Value.Should().Be(100);
    }
}

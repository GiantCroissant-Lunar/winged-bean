using FluentAssertions;
using Plate.CrossMilo.Contracts.ECS;
using Xunit;

namespace WingedBean.Plugins.ArchECS.Tests;

/// <summary>
/// Unit tests for ArchEntity implementation.
/// </summary>
public class ArchEntityTests
{
    private IWorld CreateWorld()
    {
        var service = new ArchECSService();
        return service.CreateWorld();
    }

    private IEntity CreateEntity(IWorld world, EntityHandle handle)
    {
        // Access internal ArchWorld to get the underlying Arch.Core.World
        var archWorld = (ArchWorld)world;
        return new ArchEntity(handle, archWorld.GetArchWorld());
    }

    [Fact]
    public void Id_ReturnsCorrectEntityId()
    {
        // Arrange
        var world = CreateWorld();
        var handle = world.CreateEntity();
        var entity = CreateEntity(world, handle);

        // Act
        var id = entity.Id;

        // Assert
        id.Should().Be(handle.Id);
    }

    [Fact]
    public void IsAlive_ReturnsTrueForLiveEntity()
    {
        // Arrange
        var world = CreateWorld();
        var handle = world.CreateEntity();
        var entity = CreateEntity(world, handle);

        // Act
        var isAlive = entity.IsAlive;

        // Assert
        isAlive.Should().BeTrue();
    }

    [Fact]
    public void IsAlive_ReturnsFalseForDestroyedEntity()
    {
        // Arrange
        var world = CreateWorld();
        var handle = world.CreateEntity();
        var entity = CreateEntity(world, handle);

        // Act
        world.DestroyEntity(handle);
        var isAlive = entity.IsAlive;

        // Assert
        isAlive.Should().BeFalse();
    }

    [Fact]
    public void AddComponent_AddsComponentToEntity()
    {
        // Arrange
        var world = CreateWorld();
        var handle = world.CreateEntity();
        var entity = CreateEntity(world, handle);
        var position = new Position { X = 10, Y = 20 };

        // Act
        entity.AddComponent(position);

        // Assert
        entity.HasComponent<Position>().Should().BeTrue();
    }

    [Fact]
    public void GetComponent_ReturnsCorrectComponent()
    {
        // Arrange
        var world = CreateWorld();
        var handle = world.CreateEntity();
        var entity = CreateEntity(world, handle);
        var position = new Position { X = 10, Y = 20 };
        entity.AddComponent(position);

        // Act
        ref var retrievedPosition = ref entity.GetComponent<Position>();

        // Assert
        retrievedPosition.X.Should().Be(10);
        retrievedPosition.Y.Should().Be(20);
    }

    [Fact]
    public void GetComponent_CanModifyComponent()
    {
        // Arrange
        var world = CreateWorld();
        var handle = world.CreateEntity();
        var entity = CreateEntity(world, handle);
        var position = new Position { X = 10, Y = 20 };
        entity.AddComponent(position);

        // Act
        ref var retrievedPosition = ref entity.GetComponent<Position>();
        retrievedPosition.X = 100;
        retrievedPosition.Y = 200;

        // Assert
        ref var updatedPosition = ref entity.GetComponent<Position>();
        updatedPosition.X.Should().Be(100);
        updatedPosition.Y.Should().Be(200);
    }

    [Fact]
    public void HasComponent_ReturnsTrueForExistingComponent()
    {
        // Arrange
        var world = CreateWorld();
        var handle = world.CreateEntity();
        var entity = CreateEntity(world, handle);
        var position = new Position { X = 10, Y = 20 };
        entity.AddComponent(position);

        // Act
        var hasComponent = entity.HasComponent<Position>();

        // Assert
        hasComponent.Should().BeTrue();
    }

    [Fact]
    public void HasComponent_ReturnsFalseForNonExistentComponent()
    {
        // Arrange
        var world = CreateWorld();
        var handle = world.CreateEntity();
        var entity = CreateEntity(world, handle);

        // Act
        var hasComponent = entity.HasComponent<Position>();

        // Assert
        hasComponent.Should().BeFalse();
    }

    [Fact]
    public void RemoveComponent_RemovesComponentFromEntity()
    {
        // Arrange
        var world = CreateWorld();
        var handle = world.CreateEntity();
        var entity = CreateEntity(world, handle);
        var position = new Position { X = 10, Y = 20 };
        entity.AddComponent(position);

        // Act
        entity.RemoveComponent<Position>();

        // Assert
        entity.HasComponent<Position>().Should().BeFalse();
    }

    [Fact]
    public void SetComponent_AddsComponentWhenNotPresent()
    {
        // Arrange
        var world = CreateWorld();
        var handle = world.CreateEntity();
        var entity = CreateEntity(world, handle);
        var position = new Position { X = 10, Y = 20 };

        // Act
        entity.SetComponent(position);

        // Assert
        entity.HasComponent<Position>().Should().BeTrue();
        ref var retrievedPosition = ref entity.GetComponent<Position>();
        retrievedPosition.X.Should().Be(10);
        retrievedPosition.Y.Should().Be(20);
    }

    [Fact]
    public void SetComponent_UpdatesComponentWhenPresent()
    {
        // Arrange
        var world = CreateWorld();
        var handle = world.CreateEntity();
        var entity = CreateEntity(world, handle);
        var position = new Position { X = 10, Y = 20 };
        entity.AddComponent(position);

        // Act
        var newPosition = new Position { X = 100, Y = 200 };
        entity.SetComponent(newPosition);

        // Assert
        entity.HasComponent<Position>().Should().BeTrue();
        ref var retrievedPosition = ref entity.GetComponent<Position>();
        retrievedPosition.X.Should().Be(100);
        retrievedPosition.Y.Should().Be(200);
    }

    [Fact]
    public void MultipleComponents_WorkCorrectly()
    {
        // Arrange
        var world = CreateWorld();
        var handle = world.CreateEntity();
        var entity = CreateEntity(world, handle);

        // Act
        entity.AddComponent(new Position { X = 10, Y = 20 });
        entity.AddComponent(new Velocity { X = 1.5f, Y = 2.5f });
        entity.AddComponent(new Health { Value = 100 });

        // Assert
        entity.HasComponent<Position>().Should().BeTrue();
        entity.HasComponent<Velocity>().Should().BeTrue();
        entity.HasComponent<Health>().Should().BeTrue();

        ref var position = ref entity.GetComponent<Position>();
        position.X.Should().Be(10);
        position.Y.Should().Be(20);

        ref var velocity = ref entity.GetComponent<Velocity>();
        velocity.X.Should().Be(1.5f);
        velocity.Y.Should().Be(2.5f);

        ref var health = ref entity.GetComponent<Health>();
        health.Value.Should().Be(100);
    }
}

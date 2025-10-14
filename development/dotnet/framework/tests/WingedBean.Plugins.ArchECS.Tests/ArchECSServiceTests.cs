using FluentAssertions;
using WingedBean.Plugins.ArchECS;
using Xunit;

namespace WingedBean.Plugins.ArchECS.Tests;

/// <summary>
/// Tests for the ArchECS service (Entity Component System).
/// </summary>
public class ArchECSServiceTests
{
    [Fact]
    public void CreateWorld_ShouldReturnNewWorld()
    {
        // Arrange
        var service = new ArchECSService();

        // Act
        var world = service.CreateWorld();

        // Assert
        world.Should().NotBeNull();
    }

    [Fact]
    public void CreateEntity_ShouldReturnValidEntity()
    {
        // Arrange
        var service = new ArchECSService();
        var world = service.CreateWorld();

        // Act
        var entity = world.CreateEntity();

        // Assert
        entity.Should().NotBeNull();
        entity.IsAlive.Should().BeTrue();
    }

    [Fact]
    public void AddComponent_ShouldAttachComponentToEntity()
    {
        // Arrange
        var service = new ArchECSService();
        var world = service.CreateWorld();
        var entity = world.CreateEntity();

        // Act
        entity.Add(new TestComponent { Value = 42 });

        // Assert
        entity.Has<TestComponent>().Should().BeTrue();
        entity.Get<TestComponent>().Value.Should().Be(42);
    }

    [Fact]
    public void RemoveComponent_ShouldDetachComponentFromEntity()
    {
        // Arrange
        var service = new ArchECSService();
        var world = service.CreateWorld();
        var entity = world.CreateEntity();
        entity.Add(new TestComponent { Value = 42 });

        // Act
        entity.Remove<TestComponent>();

        // Assert
        entity.Has<TestComponent>().Should().BeFalse();
    }

    [Fact]
    public void Query_ShouldFindEntitiesWithComponents()
    {
        // Arrange
        var service = new ArchECSService();
        var world = service.CreateWorld();
        
        var entity1 = world.CreateEntity();
        entity1.Add(new TestComponent { Value = 1 });
        
        var entity2 = world.CreateEntity();
        entity2.Add(new TestComponent { Value = 2 });
        
        var entity3 = world.CreateEntity();
        // No component

        // Act
        var query = world.Query<TestComponent>();
        var results = query.ToList();

        // Assert
        results.Should().HaveCount(2);
    }

    [Fact]
    public void DestroyEntity_ShouldMarkEntityAsDead()
    {
        // Arrange
        var service = new ArchECSService();
        var world = service.CreateWorld();
        var entity = world.CreateEntity();

        // Act
        world.Destroy(entity);

        // Assert
        entity.IsAlive.Should().BeFalse();
    }

    [Fact]
    public void MultipleWorlds_ShouldBeIndependent()
    {
        // Arrange
        var service = new ArchECSService();
        var world1 = service.CreateWorld();
        var world2 = service.CreateWorld();

        // Act
        var entity1 = world1.CreateEntity();
        var entity2 = world2.CreateEntity();

        // Assert
        world1.EntityCount.Should().Be(1);
        world2.EntityCount.Should().Be(1);
    }
}

/// <summary>
/// Test component for ECS tests.
/// </summary>
public struct TestComponent
{
    public int Value { get; set; }
}

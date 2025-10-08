using FluentAssertions;
using Plate.CrossMilo.Contracts.ECS;
using Xunit;

namespace WingedBean.Plugins.ArchECS.Tests;

/// <summary>
/// Unit tests for ArchQuery implementation.
/// </summary>
public class ArchQueryTests
{
    private IWorld CreateWorld()
    {
        var service = new ArchECSService();
        return service.CreateWorld();
    }

    [Fact]
    public void Query_SingleComponent_ReturnsMatchingEntities()
    {
        // Arrange
        var world = CreateWorld() as ArchWorld;
        var entity1 = world!.CreateEntity();
        var entity2 = world.CreateEntity();
        var entity3 = world.CreateEntity();

        world.AttachComponent(entity1, new Position { X = 1, Y = 1 });
        world.AttachComponent(entity2, new Position { X = 2, Y = 2 });
        world.AttachComponent(entity3, new Velocity { X = 1.0f, Y = 1.0f }); // No Position

        // Act
        var query = world.Query<Position>();
        var entities = query.GetEntities().ToList();

        // Assert
        entities.Should().HaveCount(2);
        entities.Select(e => e.Id).Should().Contain(entity1.Id);
        entities.Select(e => e.Id).Should().Contain(entity2.Id);
        entities.Select(e => e.Id).Should().NotContain(entity3.Id);
    }

    [Fact]
    public void Query_TwoComponents_ReturnsMatchingEntities()
    {
        // Arrange
        var world = CreateWorld() as ArchWorld;
        var entity1 = world!.CreateEntity();
        var entity2 = world.CreateEntity();
        var entity3 = world.CreateEntity();

        world.AttachComponent(entity1, new Position { X = 1, Y = 1 });
        world.AttachComponent(entity1, new Velocity { X = 1.0f, Y = 1.0f });

        world.AttachComponent(entity2, new Position { X = 2, Y = 2 });
        // entity2 has no Velocity

        world.AttachComponent(entity3, new Velocity { X = 3.0f, Y = 3.0f });
        // entity3 has no Position

        // Act
        var query = world.Query<Position, Velocity>();
        var entities = query.GetEntities().ToList();

        // Assert
        entities.Should().HaveCount(1);
        entities.Select(e => e.Id).Should().Contain(entity1.Id);
        entities.Select(e => e.Id).Should().NotContain(entity2.Id);
        entities.Select(e => e.Id).Should().NotContain(entity3.Id);
    }

    [Fact]
    public void Query_ThreeComponents_ReturnsMatchingEntities()
    {
        // Arrange
        var world = CreateWorld() as ArchWorld;
        var entity1 = world!.CreateEntity();
        var entity2 = world.CreateEntity();

        world.AttachComponent(entity1, new Position { X = 1, Y = 1 });
        world.AttachComponent(entity1, new Velocity { X = 1.0f, Y = 1.0f });
        world.AttachComponent(entity1, new Health { Value = 100 });

        world.AttachComponent(entity2, new Position { X = 2, Y = 2 });
        world.AttachComponent(entity2, new Velocity { X = 2.0f, Y = 2.0f });
        // entity2 has no Health

        // Act
        var query = world.Query<Position, Velocity, Health>();
        var entities = query.GetEntities().ToList();

        // Assert
        entities.Should().HaveCount(1);
        entities.Select(e => e.Id).Should().Contain(entity1.Id);
        entities.Select(e => e.Id).Should().NotContain(entity2.Id);
    }

    [Fact]
    public void Query_Count_ReturnsCorrectCount()
    {
        // Arrange
        var world = CreateWorld() as ArchWorld;
        world!.CreateEntity();
        world.CreateEntity();
        world.CreateEntity();

        var entity1 = world.CreateEntity();
        var entity2 = world.CreateEntity();

        world.AttachComponent(entity1, new Position { X = 1, Y = 1 });
        world.AttachComponent(entity2, new Position { X = 2, Y = 2 });

        // Act
        var query = world.Query<Position>();

        // Assert
        query.Count.Should().Be(2);
    }

    [Fact]
    public void Query_ForEach_ExecutesActionForEachEntity()
    {
        // Arrange
        var world = CreateWorld() as ArchWorld;
        var entity1 = world!.CreateEntity();
        var entity2 = world.CreateEntity();
        var entity3 = world.CreateEntity();

        world.AttachComponent(entity1, new Position { X = 1, Y = 1 });
        world.AttachComponent(entity2, new Position { X = 2, Y = 2 });
        world.AttachComponent(entity3, new Velocity { X = 1.0f, Y = 1.0f }); // No Position

        var processedIds = new List<int>();

        // Act
        var query = world.Query<Position>();
        query.ForEach(entity =>
        {
            processedIds.Add(entity.Id);
        });

        // Assert
        processedIds.Should().HaveCount(2);
        processedIds.Should().Contain(entity1.Id);
        processedIds.Should().Contain(entity2.Id);
        processedIds.Should().NotContain(entity3.Id);
    }

    [Fact]
    public void Query_ForEach_CanAccessComponents()
    {
        // Arrange
        var world = CreateWorld() as ArchWorld;
        var entity1 = world!.CreateEntity();
        var entity2 = world.CreateEntity();

        world.AttachComponent(entity1, new Position { X = 10, Y = 20 });
        world.AttachComponent(entity2, new Position { X = 30, Y = 40 });

        var positions = new List<(int X, int Y)>();

        // Act
        var query = world.Query<Position>();
        query.ForEach(entity =>
        {
            ref var pos = ref entity.GetComponent<Position>();
            positions.Add((pos.X, pos.Y));
        });

        // Assert
        positions.Should().HaveCount(2);
        positions.Should().Contain((10, 20));
        positions.Should().Contain((30, 40));
    }

    [Fact]
    public void Query_ForEach_CanModifyComponents()
    {
        // Arrange
        var world = CreateWorld() as ArchWorld;
        var entity1 = world!.CreateEntity();
        var entity2 = world.CreateEntity();

        world.AttachComponent(entity1, new Position { X = 10, Y = 20 });
        world.AttachComponent(entity2, new Position { X = 30, Y = 40 });

        // Act - Modify all positions
        var query = world.Query<Position>();
        query.ForEach(entity =>
        {
            ref var pos = ref entity.GetComponent<Position>();
            pos.X += 100;
            pos.Y += 200;
        });

        // Assert
        ref var pos1 = ref world.GetComponent<Position>(entity1);
        pos1.X.Should().Be(110);
        pos1.Y.Should().Be(220);

        ref var pos2 = ref world.GetComponent<Position>(entity2);
        pos2.X.Should().Be(130);
        pos2.Y.Should().Be(240);
    }

    [Fact]
    public void Query_EmptyResults_ReturnsEmptyCollection()
    {
        // Arrange
        var world = CreateWorld() as ArchWorld;
        world!.CreateEntity();
        world.CreateEntity();
        // No entities have Position component

        // Act
        var query = world.Query<Position>();
        var entities = query.GetEntities();

        // Assert
        entities.Should().BeEmpty();
        query.Count.Should().Be(0);
    }

    [Fact]
    public void Query_GetEntities_ReturnsIEntityInstances()
    {
        // Arrange
        var world = CreateWorld() as ArchWorld;
        var entity = world!.CreateEntity();
        world.AttachComponent(entity, new Position { X = 10, Y = 20 });

        // Act
        var query = world.Query<Position>();
        var entities = query.GetEntities().ToList();

        // Assert
        entities.Should().HaveCount(1);
        var retrievedEntity = entities[0];
        retrievedEntity.Should().NotBeNull();
        retrievedEntity.Id.Should().Be(entity.Id);
        retrievedEntity.IsAlive.Should().BeTrue();
        retrievedEntity.HasComponent<Position>().Should().BeTrue();
    }

    [Fact]
    public void Query_MultipleIterations_ReturnsSameResults()
    {
        // Arrange
        var world = CreateWorld() as ArchWorld;
        var entity1 = world!.CreateEntity();
        var entity2 = world.CreateEntity();

        world.AttachComponent(entity1, new Position { X = 1, Y = 1 });
        world.AttachComponent(entity2, new Position { X = 2, Y = 2 });

        var query = world.Query<Position>();

        // Act
        var firstIteration = query.GetEntities().ToList();
        var secondIteration = query.GetEntities().ToList();

        // Assert
        firstIteration.Should().HaveCount(2);
        secondIteration.Should().HaveCount(2);
        firstIteration.Select(e => e.Id).Should().BeEquivalentTo(secondIteration.Select(e => e.Id));
    }
}

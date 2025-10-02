using FluentAssertions;
using WingedBean.Contracts.ECS;
using Xunit;

namespace WingedBean.Plugins.ArchECS.Tests;

// Test system implementation for testing
internal class TestSystem : SystemBase
{
    public int UpdateCallCount { get; private set; }
    public float LastDeltaTime { get; private set; }

    protected override void OnUpdate(float deltaTime)
    {
        UpdateCallCount++;
        LastDeltaTime = deltaTime;
    }

    // Expose protected members for testing
    public IWorld GetWorld() => World;
    
    public IQuery TestGetQuery<T1>() where T1 : struct => GetQuery<T1>();
    
    public IQuery TestGetQuery<T1, T2>() 
        where T1 : struct 
        where T2 : struct 
        => GetQuery<T1, T2>();
    
    public IQuery TestGetQuery<T1, T2, T3>() 
        where T1 : struct 
        where T2 : struct 
        where T3 : struct 
        => GetQuery<T1, T2, T3>();
}

// Test system that queries entities
internal class MovementTestSystem : SystemBase
{
    public int EntitiesProcessed { get; private set; }

    protected override void OnUpdate(float deltaTime)
    {
        var query = GetQuery<Position, Velocity>();
        var entities = query.GetEntities();

        foreach (var entity in entities)
        {
            EntitiesProcessed++;
        }
    }
}

/// <summary>
/// Unit tests for SystemBase abstract class.
/// </summary>
public class SystemBaseTests
{
    private IWorld CreateWorld()
    {
        var service = new ArchECSService();
        return service.CreateWorld();
    }

    [Fact]
    public void Initialize_SetsWorldReference()
    {
        // Arrange
        var system = new TestSystem();
        var world = CreateWorld();

        // Act
        system.Initialize(world);

        // Assert
        system.GetWorld().Should().NotBeNull();
        system.GetWorld().Should().Be(world);
    }

    [Fact]
    public void Initialize_WithNull_ThrowsArgumentNullException()
    {
        // Arrange
        var system = new TestSystem();

        // Act
        var act = () => system.Initialize(null!);

        // Assert
        act.Should().Throw<System.ArgumentNullException>();
    }

    [Fact]
    public void Execute_WithoutInitialize_ThrowsInvalidOperationException()
    {
        // Arrange
        var system = new TestSystem();
        var service = new ArchECSService();

        // Act
        var act = () => system.Execute(service, 0.016f);

        // Assert
        act.Should().Throw<System.InvalidOperationException>()
            .WithMessage("*not initialized*");
    }

    [Fact]
    public void Execute_CallsOnUpdate()
    {
        // Arrange
        var system = new TestSystem();
        var world = CreateWorld();
        var service = new ArchECSService();
        system.Initialize(world);

        // Act
        system.Execute(service, 0.016f);

        // Assert
        system.UpdateCallCount.Should().Be(1);
        system.LastDeltaTime.Should().Be(0.016f);
    }

    [Fact]
    public void Execute_MultipleTimes_CallsOnUpdateMultipleTimes()
    {
        // Arrange
        var system = new TestSystem();
        var world = CreateWorld();
        var service = new ArchECSService();
        system.Initialize(world);

        // Act
        system.Execute(service, 0.016f);
        system.Execute(service, 0.033f);
        system.Execute(service, 0.050f);

        // Assert
        system.UpdateCallCount.Should().Be(3);
        system.LastDeltaTime.Should().Be(0.050f);
    }

    [Fact]
    public void GetQuery_SingleComponent_ReturnsQuery()
    {
        // Arrange
        var system = new TestSystem();
        var world = CreateWorld();
        system.Initialize(world);

        // Create test entities
        var entity1 = world.CreateEntity();
        world.AttachComponent(entity1, new Position { X = 1, Y = 2 });

        var entity2 = world.CreateEntity();
        world.AttachComponent(entity2, new Position { X = 3, Y = 4 });

        // Act
        var query = system.TestGetQuery<Position>();

        // Assert
        query.Should().NotBeNull();
        query.Count.Should().Be(2);
    }

    [Fact]
    public void GetQuery_TwoComponents_ReturnsQuery()
    {
        // Arrange
        var system = new TestSystem();
        var world = CreateWorld();
        system.Initialize(world);

        // Create test entities
        var entity1 = world.CreateEntity();
        world.AttachComponent(entity1, new Position { X = 1, Y = 2 });
        world.AttachComponent(entity1, new Velocity { X = 0.5f, Y = 1.0f });

        var entity2 = world.CreateEntity();
        world.AttachComponent(entity2, new Position { X = 3, Y = 4 });

        // Act
        var query = system.TestGetQuery<Position, Velocity>();

        // Assert
        query.Should().NotBeNull();
        query.Count.Should().Be(1); // Only entity1 has both components
    }

    [Fact]
    public void GetQuery_ThreeComponents_ReturnsQuery()
    {
        // Arrange
        var system = new TestSystem();
        var world = CreateWorld();
        system.Initialize(world);

        // Create test entities
        var entity1 = world.CreateEntity();
        world.AttachComponent(entity1, new Position { X = 1, Y = 2 });
        world.AttachComponent(entity1, new Velocity { X = 0.5f, Y = 1.0f });
        world.AttachComponent(entity1, new Health { Value = 100 });

        var entity2 = world.CreateEntity();
        world.AttachComponent(entity2, new Position { X = 3, Y = 4 });
        world.AttachComponent(entity2, new Velocity { X = 0.2f, Y = 0.3f });

        // Act
        var query = system.TestGetQuery<Position, Velocity, Health>();

        // Assert
        query.Should().NotBeNull();
        query.Count.Should().Be(1); // Only entity1 has all three components
    }

    [Fact]
    public void GetQuery_CalledTwice_ReturnsCachedQuery()
    {
        // Arrange
        var system = new TestSystem();
        var world = CreateWorld();
        system.Initialize(world);

        // Act
        var query1 = system.TestGetQuery<Position>();
        var query2 = system.TestGetQuery<Position>();

        // Assert
        query1.Should().BeSameAs(query2);
    }

    [Fact]
    public void GetQuery_DifferentComponentTypes_ReturnsDifferentQueries()
    {
        // Arrange
        var system = new TestSystem();
        var world = CreateWorld();
        system.Initialize(world);

        // Act
        var queryPosition = system.TestGetQuery<Position>();
        var queryVelocity = system.TestGetQuery<Velocity>();

        // Assert
        queryPosition.Should().NotBeSameAs(queryVelocity);
    }

    [Fact]
    public void MovementSystem_ProcessesEntitiesWithComponents()
    {
        // Arrange
        var system = new MovementTestSystem();
        var world = CreateWorld();
        var service = new ArchECSService();
        system.Initialize(world);

        // Create test entities
        var entity1 = world.CreateEntity();
        world.AttachComponent(entity1, new Position { X = 1, Y = 2 });
        world.AttachComponent(entity1, new Velocity { X = 0.5f, Y = 1.0f });

        var entity2 = world.CreateEntity();
        world.AttachComponent(entity2, new Position { X = 3, Y = 4 });
        world.AttachComponent(entity2, new Velocity { X = 0.2f, Y = 0.3f });

        var entity3 = world.CreateEntity();
        world.AttachComponent(entity3, new Position { X = 5, Y = 6 });
        // No velocity component

        // Act
        system.Execute(service, 0.016f);

        // Assert
        system.EntitiesProcessed.Should().Be(2); // Only entities with both Position and Velocity
    }

    [Fact]
    public void GetQuery_WithNonArchWorld_ThrowsInvalidOperationException()
    {
        // Arrange
        var system = new TestSystem();
        var mockWorld = new MockWorld();
        system.Initialize(mockWorld);

        // Act
        var act = () => system.TestGetQuery<Position>();

        // Assert
        act.Should().Throw<System.InvalidOperationException>()
            .WithMessage("*ArchWorld*");
    }
}

// Mock IWorld implementation for testing error cases
internal class MockWorld : IWorld
{
    public EntityHandle CreateEntity() => default;
    public void DestroyEntity(EntityHandle entity) { }
    public void AttachComponent<T>(EntityHandle entity, T component) where T : struct { }
    public void DetachComponent<T>(EntityHandle entity) where T : struct { }
    public ref T GetComponent<T>(EntityHandle entity) where T : struct => throw new System.NotImplementedException();
    public bool HasComponent<T>(EntityHandle entity) where T : struct => false;
    public bool IsAlive(EntityHandle entity) => false;
    public System.Collections.Generic.IEnumerable<EntityHandle> CreateQuery<T1>() where T1 : struct => 
        System.Array.Empty<EntityHandle>();
    public System.Collections.Generic.IEnumerable<EntityHandle> CreateQuery<T1, T2>() 
        where T1 : struct where T2 : struct => 
        System.Array.Empty<EntityHandle>();
    public System.Collections.Generic.IEnumerable<EntityHandle> CreateQuery<T1, T2, T3>() 
        where T1 : struct where T2 : struct where T3 : struct => 
        System.Array.Empty<EntityHandle>();
    public int EntityCount => 0;
}

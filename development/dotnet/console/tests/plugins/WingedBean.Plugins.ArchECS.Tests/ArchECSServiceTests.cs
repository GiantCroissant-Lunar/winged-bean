using System;
using System.Linq;
using FluentAssertions;
using WingedBean.Contracts.ECS;
using Xunit;

namespace WingedBean.Plugins.ArchECS.Tests;

/// <summary>
/// Unit tests for ArchECSService.
/// </summary>
public class ArchECSServiceTests
{
    [Fact]
    public void Constructor_CreatesInstance()
    {
        // Act
        var service = new ArchECSService();

        // Assert
        service.Should().NotBeNull();
        service.Should().BeAssignableTo<IECSService>();
    }

    [Fact]
    public void CreateWorld_ReturnsNewWorld()
    {
        // Arrange
        var service = new ArchECSService();

        // Act
        var world = service.CreateWorld();

        // Assert
        world.Should().NotBeNull();
        world.Should().BeAssignableTo<IWorld>();
    }

    [Fact]
    public void CreateWorld_ReturnsDifferentWorlds()
    {
        // Arrange
        var service = new ArchECSService();

        // Act
        var world1 = service.CreateWorld();
        var world2 = service.CreateWorld();

        // Assert
        world1.Should().NotBeNull();
        world2.Should().NotBeNull();
        world1.Should().NotBeSameAs(world2);
    }

    [Fact]
    public void AuthoringWorld_IsCreatedLazily()
    {
        // Arrange
        var service = new ArchECSService();

        // Act
        var authoringHandle = service.AuthoringWorld;
        var authoringWorld = service.GetWorld(authoringHandle);

        // Assert
        authoringHandle.Kind.Should().Be(WorldKind.Authoring);
        authoringWorld.Should().NotBeNull();
    }

    [Fact]
    public void CreateRuntimeWorld_ReturnsHandle()
    {
        // Arrange
        var service = new ArchECSService();

        // Act
        var handle = service.CreateRuntimeWorld("combat-simulation");
        var runtimeWorld = service.GetWorld(handle);

        // Assert
        handle.Kind.Should().Be(WorldKind.Runtime);
        runtimeWorld.Should().NotBeNull();
        service.GetRuntimeWorlds().Should().Contain(handle);
    }

    [Fact]
    public void SetMode_RaisesEvent()
    {
        // Arrange
        var service = new ArchECSService();
        GameMode observedMode = service.CurrentMode;

        service.ModeChanged += (_, mode) => observedMode = mode;

        // Act
        service.SetMode(GameMode.EditOverlay);

        // Assert
        observedMode.Should().Be(GameMode.EditOverlay);
    }

    [Fact]
    public void MapAuthoringToRuntime_StoresMapping()
    {
        // Arrange
        var service = new ArchECSService();
        var runtimeHandle = service.CreateRuntimeWorld("primary");
        var runtimeWorld = service.GetWorld(runtimeHandle)!;
        var entity = runtimeWorld.CreateEntity();
        var authoringId = AuthoringNodeId.New();

        // Act
        service.MapAuthoringToRuntime(authoringId, runtimeHandle, entity);
        var resolved = service.GetRuntimeEntity(authoringId);

        // Assert
        resolved.Should().NotBeNull();
        resolved!.Value.Should().Be(entity);
    }

    [Fact]
    public void DestroyWorld_WithValidWorld_RemovesWorld()
    {
        // Arrange
        var service = new ArchECSService();
        var world = service.CreateWorld();

        // Act
        service.DestroyWorld(world);

        // Assert - no exception should be thrown
    }

    [Fact]
    public void DestroyWorld_WithInvalidWorld_ThrowsArgumentException()
    {
        // Arrange
        var service = new ArchECSService();
        var invalidWorld = new InvalidWorld();

        // Act & Assert
        Assert.Throws<ArgumentException>(() => service.DestroyWorld(invalidWorld));
    }

    [Fact]
    public void GetWorld_WithNonExistentId_ReturnsNull()
    {
        // Arrange
        var service = new ArchECSService();

        // Act
        var world = service.GetWorld(999);

        // Assert
        world.Should().BeNull();
    }

    [Fact]
    public void ArchECSService_HasPluginAttribute()
    {
        // Arrange
        var type = typeof(ArchECSService);

        // Act
        var attribute = type.GetCustomAttributes(typeof(WingedBean.Contracts.Core.PluginAttribute), false)
            .FirstOrDefault() as WingedBean.Contracts.Core.PluginAttribute;

        // Assert
        attribute.Should().NotBeNull();
        attribute!.Name.Should().Be("Arch.ECS");
        attribute.Provides.Should().Contain(typeof(IECSService));
        attribute.Priority.Should().Be(100);
    }

    // Helper class for testing invalid world
    private class InvalidWorld : IWorld
    {
        public EntityHandle CreateEntity() => throw new NotImplementedException();
        public void DestroyEntity(EntityHandle entity) => throw new NotImplementedException();
        public void AttachComponent<T>(EntityHandle entity, T component) where T : struct => throw new NotImplementedException();
        public void DetachComponent<T>(EntityHandle entity) where T : struct => throw new NotImplementedException();
        public ref T GetComponent<T>(EntityHandle entity) where T : struct => throw new NotImplementedException();
        public bool HasComponent<T>(EntityHandle entity) where T : struct => throw new NotImplementedException();
        public bool IsAlive(EntityHandle entity) => throw new NotImplementedException();
        public IEnumerable<EntityHandle> CreateQuery<T1>() where T1 : struct => throw new NotImplementedException();
        public IEnumerable<EntityHandle> CreateQuery<T1, T2>() where T1 : struct where T2 : struct => throw new NotImplementedException();
        public IEnumerable<EntityHandle> CreateQuery<T1, T2, T3>() where T1 : struct where T2 : struct where T3 : struct => throw new NotImplementedException();
        public int EntityCount => throw new NotImplementedException();
    }
}

using System;
using System.Threading.Tasks;
using FluentAssertions;
using Plate.PluginManoi.Contracts;
using Plate.CrossMilo.Contracts;
using ConsoleDungeon.Contracts;
using WingedBean.Plugins.ArchECS;
using Xunit;

namespace WingedBean.Plugins.DungeonGame.Tests;

/// <summary>
/// Tests for DungeonGamePlugin to verify it correctly registers both services.
/// </summary>
public class DungeonGamePluginTests
{
    [Fact]
    public async Task OnActivateAsync_RegistersDungeonService()
    {
        // Arrange - Mock ECS service so DungeonGameService can be created
        var registry = new TestRegistry();
        var ecsService = new ArchECSService();
        registry.Register<Plate.CrossMilo.Contracts.ECS.Services.IService>(ecsService);
        
        var plugin = new DungeonGamePlugin();

        // Act
        await plugin.OnActivateAsync(registry);

        // Assert - Check if dungeon service is registered
        registry.IsRegistered<IDungeonService>()
            .Should().BeTrue("Dungeon service should be registered");
            
        // Try to retrieve it
        var dungeonService = registry.Get<IDungeonService>();
        dungeonService.Should().NotBeNull();
        dungeonService.Should().BeOfType<DungeonGameService>();
    }

    [Fact]
    public void GetServices_ReturnsDungeonService()
    {
        // Arrange
        var registry = new TestRegistry();
        var ecsService = new ArchECSService();
        registry.Register<Plate.CrossMilo.Contracts.ECS.Services.IService>(ecsService);
        var plugin = new DungeonGamePlugin();
        
        // Act
        plugin.OnActivateAsync(registry).Wait();
        var services = plugin.GetServices();

        // Assert
        services.Should().HaveCount(1, "plugin should return dungeon service");
        services.Should().ContainSingle(s => s is IDungeonService);
    }

    private sealed class TestRegistry : IRegistry
    {
        private readonly Dictionary<Type, List<Entry>> _services = new();

        public void Register<TService>(TService implementation, int priority = 0) where TService : class
        {
            RegisterInternal(typeof(TService), implementation!, new ServiceMetadata { Priority = priority });
        }

        public void Register<TService>(TService implementation, ServiceMetadata metadata) where TService : class
        {
            RegisterInternal(typeof(TService), implementation!, metadata);
        }

        public TService Get<TService>(SelectionMode mode = SelectionMode.HighestPriority) where TService : class
        {
            if (!_services.TryGetValue(typeof(TService), out var list) || list.Count == 0)
            {
                throw new ServiceNotFoundException(typeof(TService));
            }

            return mode switch
            {
                SelectionMode.One => list.Count == 1
                    ? (TService)list[0].Implementation
                    : throw new MultipleServicesException(typeof(TService), list.Count),
                SelectionMode.HighestPriority => (TService)list
                    .OrderByDescending(entry => entry.Metadata.Priority)
                    .First().Implementation,
                SelectionMode.All => (TService)list
                    .OrderByDescending(entry => entry.Metadata.Priority)
                    .First().Implementation,
                _ => throw new ArgumentOutOfRangeException(nameof(mode), mode, null)
            };
        }

        public IEnumerable<TService> GetAll<TService>() where TService : class
        {
            if (!_services.TryGetValue(typeof(TService), out var list))
            {
                return Array.Empty<TService>();
            }

            return list.Select(entry => (TService)entry.Implementation).ToArray();
        }

        public bool IsRegistered<TService>() where TService : class
            => _services.TryGetValue(typeof(TService), out var list) && list.Count > 0;

        public bool Unregister<TService>(TService implementation) where TService : class
        {
            if (!_services.TryGetValue(typeof(TService), out var list))
            {
                return false;
            }

            var removed = list.RemoveAll(entry => ReferenceEquals(entry.Implementation, implementation)) > 0;
            if (list.Count == 0)
            {
                _services.Remove(typeof(TService));
            }

            return removed;
        }

        public void UnregisterAll<TService>() where TService : class
        {
            _services.Remove(typeof(TService));
        }

        public ServiceMetadata? GetMetadata<TService>(TService implementation) where TService : class
        {
            if (!_services.TryGetValue(typeof(TService), out var list))
            {
                return null;
            }

            return list.FirstOrDefault(entry => ReferenceEquals(entry.Implementation, implementation))?.Metadata;
        }

        private void RegisterInternal(Type serviceType, object implementation, ServiceMetadata metadata)
        {
            if (!_services.TryGetValue(serviceType, out var list))
            {
                list = new List<Entry>();
                _services[serviceType] = list;
            }

            list.Add(new Entry(implementation, metadata));
        }

        private sealed record Entry(object Implementation, ServiceMetadata Metadata);
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using System.Threading.Tasks;
using FluentAssertions;
using Plate.PluginManoi.Contracts;
using Plate.CrossMilo.Contracts.ECS;
using Plate.CrossMilo.Contracts.Game;
using WingedBean.Plugins.ArchECS;
using WingedBean.Plugins.DungeonGame;
using DungeonComponents = WingedBean.Plugins.DungeonGame.Components;
using Xunit;
using IECSService = Plate.CrossMilo.Contracts.ECS.Services.IService;

namespace WingedBean.Plugins.DungeonGame.Tests;

public class DungeonGameServiceTests
{
    private readonly TestRegistry _registry;

    public DungeonGameServiceTests()
    {
        _registry = new TestRegistry();
        _registry.Register<IECSService>(new ArchECSService(), priority: 100);
    }

    [Fact]
    public async Task Initialize_PublishesEntitiesAndStats()
    {
        var service = new DungeonGameService(_registry);

        var entitiesTask = service.EntitiesObservable
            .Skip(1)
            .FirstAsync()
            .ToTask();
        var statsTask = service.PlayerStatsObservable
            .Skip(1)
            .FirstAsync()
            .ToTask();

        service.Initialize();

        var entities = await entitiesTask;
        var stats = await statsTask;

        entities.Should().NotBeEmpty();
        entities.Should().Contain(e => e.Symbol == '@', "player snapshot should be published");

        stats.MaxHP.Should().Be(100);
        stats.CurrentHP.Should().Be(100);
    }

    [Fact]
    public void HandleInput_MovesPlayerUp()
    {
        var service = new DungeonGameService(_registry);
        service.Initialize();

        service.World.Should().NotBeNull();
        var world = service.World!;
        var player = world.CreateQuery<DungeonComponents.Player, DungeonComponents.Position>().First();
        ref var original = ref world.GetComponent<DungeonComponents.Position>(player);
        var originalY = original.Y;

        service.HandleInput(new GameInput(InputType.MoveUp));
        service.Update(0.016f);

        ref var updated = ref world.GetComponent<DungeonComponents.Position>(player);
        updated.Y.Should().Be(originalY - 1);
    }

    [Fact]
    public void HandleInput_QuitTransitionsToGameOver()
    {
        var service = new DungeonGameService(_registry);
        service.Initialize();

        service.HandleInput(new GameInput(InputType.Quit));

        service.CurrentState.Should().Be(GameState.GameOver);
    }

    [Fact]
    public async Task Update_PublishesLatestSnapshots()
    {
        var service = new DungeonGameService(_registry);
        service.Initialize();

        var updateTask = service.EntitiesObservable
            .Skip(1)
            .FirstAsync()
            .ToTask();

        // Trigger an update to cause enemies to potentially move (AI system)
        service.Update(0.016f);

        var entities = await updateTask;

        entities.Should().NotBeEmpty();
        entities.Select(e => e.RenderLayer).Should().OnlyContain(layer => layer >= 0);
    }


    [Fact]
    public void RuntimeWorldHandle_IsValidAfterInitialize()
    {
        var service = new DungeonGameService(_registry);
        service.Initialize();

        service.RuntimeWorldHandle.IsValid.Should().BeTrue();
        service.RuntimeWorldHandle.Kind.Should().Be(WorldKind.Runtime);
        service.RuntimeWorlds.Should().Contain(service.RuntimeWorldHandle);
    }

    [Fact]
    public void SetMode_RaisesModeChanged()
    {
        var service = new DungeonGameService(_registry);
        service.Initialize();

        GameMode observed = service.CurrentMode;
        service.ModeChanged += (_, mode) => observed = mode;

        service.SetMode(GameMode.EditOverlay);

        observed.Should().Be(GameMode.EditOverlay);
        service.CurrentMode.Should().Be(GameMode.EditOverlay);
    }

    [Fact]
    public void CreateRuntimeWorld_ReturnsHandleAndInitializes()
    {
        var service = new DungeonGameService(_registry);
        service.Initialize();

        var handle = service.CreateRuntimeWorld("test-world");

        handle.IsValid.Should().BeTrue();
        service.RuntimeWorlds.Should().Contain(handle);
    }

    [Fact]
    public void SwitchRuntimeWorld_ChangesActiveWorld()
    {
        var service = new DungeonGameService(_registry);
        service.Initialize();

        var handle = service.CreateRuntimeWorld("secondary");
        service.SwitchRuntimeWorld(handle);

        service.RuntimeWorldHandle.Should().Be(handle);
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

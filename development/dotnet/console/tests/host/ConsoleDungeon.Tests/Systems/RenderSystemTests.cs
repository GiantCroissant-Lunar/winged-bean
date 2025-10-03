using Xunit;
using FluentAssertions;
using ConsoleDungeon.Systems;
using ConsoleDungeon.Components;
using WingedBean.Contracts.ECS;
using WingedBean.Plugins.ArchECS;

namespace ConsoleDungeon.Tests.Systems;

public class RenderSystemTests
{
    private readonly IECSService _ecsService;
    private readonly IWorld _world;
    private readonly RenderSystem _system;

    public RenderSystemTests()
    {
        _ecsService = new ArchECSService();
        _world = _ecsService.CreateWorld();
        _system = new RenderSystem();
    }

    [Fact]
    public void Execute_WithNoEntities_DoesNotThrow()
    {
        // Act & Assert
        var act = () => _system.Execute(_ecsService, 0.016f);
        act.Should().NotThrow();
    }

    [Fact]
    public void Execute_WithRenderableEntities_DoesNotThrow()
    {
        // Arrange
        var entity = _world.CreateEntity();
        _world.AttachComponent(entity, new Position(10, 10, 1));
        _world.AttachComponent(entity, new Renderable
        {
            Symbol = '@',
            ForegroundColor = ConsoleColor.White,
            BackgroundColor = ConsoleColor.Black,
            RenderLayer = 2
        });

        // Act & Assert
        var act = () => _system.Execute(_ecsService, 0.016f);
        act.Should().NotThrow();
    }

    [Fact]
    public void Execute_WithPlayerStats_DoesNotThrow()
    {
        // Arrange
        var player = _world.CreateEntity();
        _world.AttachComponent(player, new Player());
        _world.AttachComponent(player, new Position(10, 10, 1));
        _world.AttachComponent(player, new Renderable
        {
            Symbol = '@',
            ForegroundColor = ConsoleColor.Yellow,
            BackgroundColor = ConsoleColor.Black,
            RenderLayer = 2
        });
        _world.AttachComponent(player, new Stats
        {
            MaxHP = 100,
            CurrentHP = 75,
            MaxMana = 50,
            CurrentMana = 30,
            Level = 3,
            Experience = 150
        });

        // Act & Assert
        var act = () => _system.Execute(_ecsService, 0.016f);
        act.Should().NotThrow();
    }

    [Fact]
    public void Execute_WithMultipleRenderableLayers_DoesNotThrow()
    {
        // Arrange - Create entities with different render layers
        var floor = _world.CreateEntity();
        _world.AttachComponent(floor, new Position(10, 10, 1));
        _world.AttachComponent(floor, new Renderable
        {
            Symbol = '.',
            ForegroundColor = ConsoleColor.Gray,
            BackgroundColor = ConsoleColor.Black,
            RenderLayer = 0 // Floor layer
        });

        var item = _world.CreateEntity();
        _world.AttachComponent(item, new Position(12, 10, 1));
        _world.AttachComponent(item, new Renderable
        {
            Symbol = '!',
            ForegroundColor = ConsoleColor.Yellow,
            BackgroundColor = ConsoleColor.Black,
            RenderLayer = 1 // Item layer
        });

        var creature = _world.CreateEntity();
        _world.AttachComponent(creature, new Position(14, 10, 1));
        _world.AttachComponent(creature, new Renderable
        {
            Symbol = 'g',
            ForegroundColor = ConsoleColor.Green,
            BackgroundColor = ConsoleColor.Black,
            RenderLayer = 2 // Creature layer
        });

        // Act & Assert
        var act = () => _system.Execute(_ecsService, 0.016f);
        act.Should().NotThrow();
    }

    [Fact]
    public void Execute_WithOutOfBoundsPosition_DoesNotThrow()
    {
        // Arrange - Position outside console bounds
        var entity = _world.CreateEntity();
        _world.AttachComponent(entity, new Position(1000, 1000, 1));
        _world.AttachComponent(entity, new Renderable
        {
            Symbol = 'X',
            ForegroundColor = ConsoleColor.Red,
            BackgroundColor = ConsoleColor.Black,
            RenderLayer = 2
        });

        // Act & Assert - Should handle gracefully
        var act = () => _system.Execute(_ecsService, 0.016f);
        act.Should().NotThrow();
    }
}

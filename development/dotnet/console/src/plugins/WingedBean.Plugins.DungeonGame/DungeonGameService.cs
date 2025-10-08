using System;
using System.Collections.Generic;
using System.Reactive.Subjects;
using Plate.PluginManoi.Contracts;
using Plate.CrossMilo.Contracts.ECS;
using Plate.CrossMilo.Contracts.Game;
using Plate.CrossMilo.Contracts.Game.Dungeon;
using WingedBean.Plugins.DungeonGame.Components;

// Type alias for backward compatibility during namespace migration
using IECSService = Plate.CrossMilo.Contracts.ECS.Services.IService;

namespace WingedBean.Plugins.DungeonGame;

/// <summary>
/// Implementation of IDungeonGameService with reactive patterns and multi-world support.
/// Wraps the core DungeonGame class and exposes observables for game state.
/// </summary>
public class DungeonGameService : IService, IDisposable
{
    private readonly IRegistry _registry;
    private readonly IECSService _ecs;
    private readonly DungeonGame _game;

    private readonly BehaviorSubject<GameState> _gameStateSubject;
    private readonly BehaviorSubject<IReadOnlyList<EntitySnapshot>> _entitiesSubject;
    private readonly BehaviorSubject<PlayerStats> _playerStatsSubject;

    public event EventHandler<GameMode>? ModeChanged;

    public DungeonGameService(IRegistry registry)
    {
        _registry = registry ?? throw new ArgumentNullException(nameof(registry));
        _ecs = _registry.Get<IECSService>();
        _game = new DungeonGame(registry);

        _ecs.ModeChanged += HandleModeChanged;

        _gameStateSubject = new BehaviorSubject<GameState>(GameState.NotStarted);
        _entitiesSubject = new BehaviorSubject<IReadOnlyList<EntitySnapshot>>(Array.Empty<EntitySnapshot>());
        _playerStatsSubject = new BehaviorSubject<PlayerStats>(new PlayerStats(0, 0, 0, 0, 0, 0, 0, 0, 0, 0));
    }

    public void Initialize()
    {
        _game.Initialize();
        _ecs.SetMode(GameMode.Play);
        _gameStateSubject.OnNext(GameState.Running);
        UpdateSnapshots();
    }

    public void Update(float deltaTime)
    {
        _game.Update();
        UpdateSnapshots();
    }

    public void Shutdown()
    {
        _gameStateSubject.OnNext(GameState.GameOver);
        _gameStateSubject.OnCompleted();
        _entitiesSubject.OnCompleted();
        _playerStatsSubject.OnCompleted();
        _ecs.ModeChanged -= HandleModeChanged;
    }

    public void HandleInput(GameInput input)
    {
        var world = _game.World;
        if (world == null)
        {
            return;
        }

        foreach (var entity in world.CreateQuery<Player, Components.Position>())
        {
            ref var pos = ref world.GetComponent<Components.Position>(entity);

            switch (input.Type)
            {
                case InputType.MoveUp:
                    pos.Y--;
                    break;
                case InputType.MoveDown:
                    pos.Y++;
                    break;
                case InputType.MoveLeft:
                    pos.X--;
                    break;
                case InputType.MoveRight:
                    pos.X++;
                    break;
                case InputType.Quit:
                    _gameStateSubject.OnNext(GameState.GameOver);
                    break;
            }

            break;
        }

        UpdateSnapshots();
    }

    public WorldHandle RuntimeWorldHandle => _game.RuntimeHandle;

    public IEnumerable<WorldHandle> RuntimeWorlds => _ecs.GetRuntimeWorlds();

    public GameMode CurrentMode => _ecs.CurrentMode;

    public void SetMode(GameMode mode)
    {
        _ecs.SetMode(mode);
    }

    public WorldHandle CreateRuntimeWorld(string name)
    {
        var handle = _ecs.CreateRuntimeWorld(name);
        _game.EnsureRuntimeWorld(handle);
        UpdateSnapshots();
        return handle;
    }

    public void SwitchRuntimeWorld(WorldHandle handle)
    {
        _game.SwitchRuntimeWorld(handle);
        UpdateSnapshots();
    }

    private void UpdateSnapshots()
    {
        var world = _game.World;
        if (world == null)
        {
            return;
        }

        var snapshots = new List<EntitySnapshot>();
        foreach (var entity in world.CreateQuery<Components.Position, Renderable>())
        {
            var pos = world.GetComponent<Components.Position>(entity);
            var render = world.GetComponent<Renderable>(entity);

            snapshots.Add(new EntitySnapshot(
                Guid.NewGuid(),
                new Plate.CrossMilo.Contracts.Game.Position(pos.X, pos.Y, pos.Floor),
                render.Symbol,
                render.ForegroundColor,
                render.BackgroundColor,
                render.RenderLayer
            ));
        }
        _entitiesSubject.OnNext(snapshots);

        foreach (var entity in world.CreateQuery<Player, Stats>())
        {
            var stats = world.GetComponent<Stats>(entity);
            var playerStats = new PlayerStats(
                stats.CurrentHP,
                stats.MaxHP,
                stats.CurrentMana,
                stats.MaxMana,
                stats.Level,
                stats.Experience,
                stats.Strength,
                stats.Dexterity,
                stats.Intelligence,
                stats.Defense
            );
            _playerStatsSubject.OnNext(playerStats);
            break;
        }
    }

    private void HandleModeChanged(object? sender, GameMode mode)
    {
        ModeChanged?.Invoke(this, mode);
    }

    public IObservable<GameState> GameStateObservable => _gameStateSubject;
    public IObservable<IReadOnlyList<EntitySnapshot>> EntitiesObservable => _entitiesSubject;
    public IObservable<PlayerStats> PlayerStatsObservable => _playerStatsSubject;
    public IWorld? World => _game.World;
    public GameState CurrentState => _gameStateSubject.Value;
    public PlayerStats CurrentPlayerStats => _playerStatsSubject.Value;

    public void Dispose()
    {
        _ecs.ModeChanged -= HandleModeChanged;
    }
}

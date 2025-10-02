using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Subjects;
using WingedBean.Contracts.Core;
using WingedBean.Contracts.ECS;
using WingedBean.Contracts.Game;
using WingedBean.Plugins.DungeonGame.Components;

namespace WingedBean.Plugins.DungeonGame;

/// <summary>
/// Implementation of IDungeonGameService with reactive patterns.
/// Wraps the core DungeonGame class and exposes observables for game state.
/// </summary>
public class DungeonGameService : IDungeonGameService
{
    private readonly IRegistry _registry;
    private readonly DungeonGame _game;

    private readonly BehaviorSubject<GameState> _gameStateSubject;
    private readonly BehaviorSubject<IReadOnlyList<EntitySnapshot>> _entitiesSubject;
    private readonly BehaviorSubject<PlayerStats> _playerStatsSubject;

    public DungeonGameService(IRegistry registry)
    {
        _registry = registry ?? throw new ArgumentNullException(nameof(registry));
        _game = new DungeonGame(registry);

        _gameStateSubject = new BehaviorSubject<GameState>(GameState.NotStarted);
        _entitiesSubject = new BehaviorSubject<IReadOnlyList<EntitySnapshot>>(Array.Empty<EntitySnapshot>());
        _playerStatsSubject = new BehaviorSubject<PlayerStats>(new PlayerStats(0, 0, 0, 0, 0, 0, 0, 0, 0, 0));
    }

    public void Initialize()
    {
        _game.Initialize();
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
    }

    public void HandleInput(GameInput input)
    {
        if (_game.World == null) return;

        // Find player and apply input
        foreach (var entity in _game.World.CreateQuery<Player, Components.Position>())
        {
            ref var pos = ref _game.World.GetComponent<Components.Position>(entity);

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

            break; // Only one player
        }

        UpdateSnapshots();
    }

    private void UpdateSnapshots()
    {
        if (_game.World == null) return;

        // Update entity snapshots
        var snapshots = new List<EntitySnapshot>();
        foreach (var entity in _game.World.CreateQuery<Components.Position, Renderable>())
        {
            var pos = _game.World.GetComponent<Components.Position>(entity);
            var render = _game.World.GetComponent<Renderable>(entity);

            snapshots.Add(new EntitySnapshot(
                Guid.NewGuid(), // TODO: Add proper entity ID tracking
                new Contracts.Game.Position(pos.X, pos.Y, pos.Floor),
                render.Symbol,
                render.ForegroundColor,
                render.BackgroundColor,
                render.RenderLayer
            ));
        }
        _entitiesSubject.OnNext(snapshots);

        // Update player stats
        foreach (var entity in _game.World.CreateQuery<Player, Stats>())
        {
            var stats = _game.World.GetComponent<Stats>(entity);
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
            break; // Only one player
        }
    }

    public IObservable<GameState> GameStateObservable => _gameStateSubject;
    public IObservable<IReadOnlyList<EntitySnapshot>> EntitiesObservable => _entitiesSubject;
    public IObservable<PlayerStats> PlayerStatsObservable => _playerStatsSubject;
    public IWorld? World => _game.World;
    public GameState CurrentState => _gameStateSubject.Value;
    public PlayerStats CurrentPlayerStats => _playerStatsSubject.Value;
}

using System;
using System.Collections.Generic;
using WingedBean.Contracts.ECS;

namespace WingedBean.Contracts.Game;

/// <summary>
/// Service contract for the dungeon crawler game logic.
/// Game logic plugin implements this to expose reactive game state to UI plugins.
/// </summary>
public interface IDungeonGameService
{
    /// <summary>
    /// Initialize the game world, systems, and entities.
    /// </summary>
    void Initialize();

    /// <summary>
    /// Update the game state for one frame.
    /// </summary>
    /// <param name="deltaTime">Time elapsed since last update in seconds</param>
    void Update(float deltaTime);

    /// <summary>
    /// Shutdown the game and cleanup resources.
    /// </summary>
    void Shutdown();

    /// <summary>
    /// Handle player input by sending commands to the game.
    /// Uses command pattern to decouple UI from game logic.
    /// </summary>
    /// <param name="input">Input command to process</param>
    void HandleInput(GameInput input);

    /// <summary>
    /// Observable stream of game state changes.
    /// UI subscribes to this to react to state transitions.
    /// </summary>
    IObservable<GameState> GameStateObservable { get; }

    /// <summary>
    /// Observable collection of entity snapshots for rendering.
    /// Updates when entities move, spawn, or die.
    /// </summary>
    IObservable<IReadOnlyList<EntitySnapshot>> EntitiesObservable { get; }

    /// <summary>
    /// Observable stream of player stats changes.
    /// Updates when HP, MP, level, etc. change.
    /// </summary>
    IObservable<PlayerStats> PlayerStatsObservable { get; }

    WorldHandle RuntimeWorldHandle { get; }

    IEnumerable<WorldHandle> RuntimeWorlds { get; }

    GameMode CurrentMode { get; }

    event EventHandler<GameMode>? ModeChanged;

    void SetMode(GameMode mode);

    WorldHandle CreateRuntimeWorld(string name);

    void SwitchRuntimeWorld(WorldHandle handle);


    /// <summary>
    /// Direct access to ECS world for advanced queries.
    /// Use sparingly - prefer observables for reactive updates.
    /// </summary>
    IWorld? World { get; }

    /// <summary>
    /// Current game state (for non-reactive scenarios).
    /// </summary>
    GameState CurrentState { get; }

    /// <summary>
    /// Current player stats snapshot (for non-reactive scenarios).
    /// </summary>
    PlayerStats CurrentPlayerStats { get; }
}

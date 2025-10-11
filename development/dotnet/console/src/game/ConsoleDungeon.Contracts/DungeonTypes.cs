using System;

namespace ConsoleDungeon.Contracts;

/// <summary>
/// Input command sent from UI to game logic.
/// Uses command pattern to decouple input handling from game state mutations.
/// </summary>
public record GameInput(InputType Type, object? Data = null);

/// <summary>
/// Types of player input commands.
/// </summary>
public enum InputType
{
    MoveUp,
    MoveDown,
    MoveLeft,
    MoveRight,
    Attack,
    UseItem,
    Inventory,
    Quit
}

/// <summary>
/// Overall game state enum.
/// </summary>
public enum GameState
{
    NotStarted,
    Running,
    Paused,
    GameOver,
    Victory
}

/// <summary>
/// Immutable snapshot of an entity for UI rendering.
/// Game logic publishes these, UI consumes them.
/// </summary>
public record EntitySnapshot(
    Guid Id,
    Position Position,
    char Symbol,
    ConsoleColor ForegroundColor,
    ConsoleColor BackgroundColor,
    int RenderLayer
);

/// <summary>
/// Position in the dungeon (shared type).
/// </summary>
public record Position(int X, int Y, int Z);

/// <summary>
/// Immutable snapshot of player statistics.
/// </summary>
public record PlayerStats(
    int CurrentHP,
    int MaxHP,
    int CurrentMana,
    int MaxMana,
    int Level,
    int Experience,
    int Strength,
    int Dexterity,
    int Intelligence,
    int Defense
);

/// <summary>
/// Types of entities in the game world.
/// </summary>
public enum EntityType
{
    Player,
    Enemy,
    Item,
    Wall,
    Floor
}

using System;
using System.Collections.Generic;

namespace WingedBean.Contracts.Gameplay;

/// <summary>
/// UI-agnostic snapshot stream of the current gameplay world state.
/// Consumable by any renderer (Terminal, SadConsole, etc.).
/// </summary>
public interface IWorldSnapshotProvider
{
    /// <summary>Observable stream of world snapshots.</summary>
    IObservable<WorldSnapshot> WorldObservable { get; }

    /// <summary>Latest world snapshot.</summary>
    WorldSnapshot CurrentWorld { get; }
}

/// <summary>
/// Lightweight, UI-agnostic representation of the world suitable for any renderer.
/// </summary>
public sealed class WorldSnapshot
{
    public IReadOnlyList<EntityView> Entities { get; init; } = Array.Empty<EntityView>();
    public PlayerHud Hud { get; init; } = new PlayerHud();
    public int Floor { get; init; }
}

/// <summary>
/// Minimal entity view model with glyph and colors.
/// </summary>
public sealed class EntityView
{
    public int X { get; init; }
    public int Y { get; init; }
    public int Z { get; init; }
    public char Glyph { get; init; }
    public ConsoleColor Foreground { get; init; }
    public ConsoleColor Background { get; init; }
    public int Layer { get; init; }
}

/// <summary>
/// Minimal player HUD information.
/// </summary>
public sealed class PlayerHud
{
    public int HP { get; init; }
    public int MaxHP { get; init; }
    public int Level { get; init; }
    public int XP { get; init; }
    public int MaxXP { get; init; }
}

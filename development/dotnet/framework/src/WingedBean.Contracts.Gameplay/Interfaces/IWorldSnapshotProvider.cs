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
    public IReadOnlyList<EntityView> Entities { get; set; } = Array.Empty<EntityView>();
    public PlayerHud Hud { get; set; } = new PlayerHud();
    public int Floor { get; set; }
}

/// <summary>
/// Minimal entity view model with glyph and colors.
/// </summary>
public sealed class EntityView
{
    public int X { get; set; }
    public int Y { get; set; }
    public int Z { get; set; }
    public char Glyph { get; set; }
    public ConsoleColor Foreground { get; set; }
    public ConsoleColor Background { get; set; }
    public int Layer { get; set; }
}

/// <summary>
/// Minimal player HUD information.
/// </summary>
public sealed class PlayerHud
{
    public int HP { get; set; }
    public int MaxHP { get; set; }
    public int Level { get; set; }
    public int XP { get; set; }
    public int MaxXP { get; set; }
}

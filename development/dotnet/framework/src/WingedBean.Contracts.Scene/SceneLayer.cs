using System.Collections.Generic;

namespace WingedBean.Contracts.Scene;

/// <summary>
/// Defines rendering layers for scene composition.
/// Lower values render first (background), higher values render last (foreground/UI).
/// </summary>
public enum SceneLayer
{
    /// <summary>Background layer - floor, walls, static environment</summary>
    Background = 0,
    
    /// <summary>Entity layer - characters, monsters, items</summary>
    Entities = 100,
    
    /// <summary>Effects layer - particles, animations, visual effects</summary>
    Effects = 200,
    
    /// <summary>UI layer - HUD, menus, dialogs, overlays</summary>
    UI = 300
}

/// <summary>
/// Represents a snapshot of entities to render on a specific layer.
/// </summary>
public readonly struct LayeredSnapshot
{
    public SceneLayer Layer { get; init; }
    public IReadOnlyList<WingedBean.Contracts.Game.EntitySnapshot> Entities { get; init; }

    public LayeredSnapshot(SceneLayer layer, IReadOnlyList<WingedBean.Contracts.Game.EntitySnapshot> entities)
    {
        Layer = layer;
        Entities = entities;
    }
}

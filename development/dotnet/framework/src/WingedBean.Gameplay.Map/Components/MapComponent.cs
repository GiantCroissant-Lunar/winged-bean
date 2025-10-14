using WingedBean.Gameplay.Map.Data;

namespace WingedBean.Gameplay.Map.Components;

/// <summary>
/// ECS component that holds the dungeon map data.
/// Typically attached to a singleton entity that represents the current level.
/// </summary>
public struct MapComponent
{
    /// <summary>
    /// The dungeon map instance.
    /// </summary>
    public DungeonMap Map { get; set; }

    /// <summary>
    /// The level number (for multi-level dungeons).
    /// </summary>
    public int Level { get; set; }

    /// <summary>
    /// Creates a new MapComponent with the specified map.
    /// </summary>
    public MapComponent(DungeonMap map, int level = 1)
    {
        Map = map;
        Level = level;
    }
}

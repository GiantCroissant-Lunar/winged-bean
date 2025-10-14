namespace WingedBean.Gameplay.Map.Data;

/// <summary>
/// Enumeration of tile types in the dungeon.
/// </summary>
public enum TileType
{
    /// <summary>
    /// Empty space, no tile.
    /// </summary>
    Empty = 0,

    /// <summary>
    /// Floor tile that can be walked on.
    /// </summary>
    Floor = 1,

    /// <summary>
    /// Wall tile that blocks movement.
    /// </summary>
    Wall = 2,

    /// <summary>
    /// Door tile that can be opened or closed.
    /// </summary>
    Door = 3,

    /// <summary>
    /// Stairs going up to previous level.
    /// </summary>
    StairsUp = 4,

    /// <summary>
    /// Stairs going down to next level.
    /// </summary>
    StairsDown = 5,

    /// <summary>
    /// Water tile (potentially passable with swimming).
    /// </summary>
    Water = 6,

    /// <summary>
    /// Chasm or pit (impassable or requires special ability).
    /// </summary>
    Chasm = 7
}

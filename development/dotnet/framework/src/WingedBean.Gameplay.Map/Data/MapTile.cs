namespace WingedBean.Gameplay.Map.Data;

/// <summary>
/// Represents a single tile in the dungeon map.
/// </summary>
public struct MapTile
{
    /// <summary>
    /// The type of this tile.
    /// </summary>
    public TileType Type { get; set; }

    /// <summary>
    /// Whether this tile blocks movement.
    /// </summary>
    public bool BlocksMovement { get; set; }

    /// <summary>
    /// Whether this tile blocks line of sight.
    /// </summary>
    public bool BlocksSight { get; set; }

    /// <summary>
    /// Whether this tile has been explored by the player.
    /// </summary>
    public bool IsExplored { get; set; }

    /// <summary>
    /// Whether this tile is currently visible to the player.
    /// </summary>
    public bool IsVisible { get; set; }

    /// <summary>
    /// Creates a new MapTile with the specified type and default properties.
    /// </summary>
    public MapTile(TileType type)
    {
        Type = type;
        BlocksMovement = type is TileType.Wall or TileType.Chasm;
        BlocksSight = type == TileType.Wall;
        IsExplored = false;
        IsVisible = false;
    }

    /// <summary>
    /// Creates a floor tile.
    /// </summary>
    public static MapTile Floor() => new(TileType.Floor)
    {
        BlocksMovement = false,
        BlocksSight = false
    };

    /// <summary>
    /// Creates a wall tile.
    /// </summary>
    public static MapTile Wall() => new(TileType.Wall)
    {
        BlocksMovement = true,
        BlocksSight = true
    };

    /// <summary>
    /// Creates a door tile.
    /// </summary>
    public static MapTile Door() => new(TileType.Door)
    {
        BlocksMovement = false,
        BlocksSight = false
    };

    /// <summary>
    /// Creates an empty tile.
    /// </summary>
    public static MapTile Empty() => new(TileType.Empty)
    {
        BlocksMovement = true,
        BlocksSight = true
    };
}

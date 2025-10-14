namespace WingedBean.Gameplay.Map.Data;

/// <summary>
/// Represents a dungeon map with a 2D grid of tiles.
/// </summary>
public class DungeonMap
{
    /// <summary>
    /// Width of the map in tiles.
    /// </summary>
    public int Width { get; }

    /// <summary>
    /// Height of the map in tiles.
    /// </summary>
    public int Height { get; }

    /// <summary>
    /// The tile data stored as a flat array [y * Width + x].
    /// </summary>
    private readonly MapTile[] _tiles;

    /// <summary>
    /// Creates a new dungeon map with the specified dimensions.
    /// All tiles are initialized as walls.
    /// </summary>
    public DungeonMap(int width, int height)
    {
        Width = width;
        Height = height;
        _tiles = new MapTile[width * height];

        // Initialize all tiles as walls
        for (var i = 0; i < _tiles.Length; i++)
        {
            _tiles[i] = MapTile.Wall();
        }
    }

    /// <summary>
    /// Gets the tile at the specified coordinates.
    /// Returns a wall tile if coordinates are out of bounds.
    /// </summary>
    public MapTile GetTile(int x, int y)
    {
        if (!IsInBounds(x, y))
        {
            return MapTile.Wall();
        }

        return _tiles[y * Width + x];
    }

    /// <summary>
    /// Sets the tile at the specified coordinates.
    /// Does nothing if coordinates are out of bounds.
    /// </summary>
    public void SetTile(int x, int y, MapTile tile)
    {
        if (!IsInBounds(x, y))
        {
            return;
        }

        _tiles[y * Width + x] = tile;
    }

    /// <summary>
    /// Checks if the specified coordinates are within map bounds.
    /// </summary>
    public bool IsInBounds(int x, int y)
    {
        return x >= 0 && x < Width && y >= 0 && y < Height;
    }

    /// <summary>
    /// Checks if the specified coordinates are walkable (not blocking movement).
    /// </summary>
    public bool IsWalkable(int x, int y)
    {
        if (!IsInBounds(x, y))
        {
            return false;
        }

        return !GetTile(x, y).BlocksMovement;
    }

    /// <summary>
    /// Checks if the specified coordinates block line of sight.
    /// </summary>
    public bool BlocksSight(int x, int y)
    {
        if (!IsInBounds(x, y))
        {
            return true;
        }

        return GetTile(x, y).BlocksSight;
    }

    /// <summary>
    /// Sets a tile as explored.
    /// </summary>
    public void SetExplored(int x, int y, bool explored = true)
    {
        if (!IsInBounds(x, y))
        {
            return;
        }

        var tile = _tiles[y * Width + x];
        tile.IsExplored = explored;
        _tiles[y * Width + x] = tile;
    }

    /// <summary>
    /// Sets a tile's visibility.
    /// </summary>
    public void SetVisible(int x, int y, bool visible = true)
    {
        if (!IsInBounds(x, y))
        {
            return;
        }

        var tile = _tiles[y * Width + x];
        tile.IsVisible = visible;
        _tiles[y * Width + x] = tile;
    }

    /// <summary>
    /// Clears all tile visibility flags.
    /// </summary>
    public void ClearVisibility()
    {
        for (var i = 0; i < _tiles.Length; i++)
        {
            _tiles[i].IsVisible = false;
        }
    }

    /// <summary>
    /// Creates a simple test map with a room.
    /// </summary>
    public static DungeonMap CreateTestMap()
    {
        var map = new DungeonMap(20, 20);

        // Create a simple room
        for (var y = 5; y < 15; y++)
        {
            for (var x = 5; x < 15; x++)
            {
                map.SetTile(x, y, MapTile.Floor());
            }
        }

        return map;
    }
}

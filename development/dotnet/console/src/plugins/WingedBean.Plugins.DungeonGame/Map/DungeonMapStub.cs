namespace WingedBean.Plugins.DungeonGame.Map;

/// <summary>
/// STUB: Minimal DungeonMap interface for parallel development.
/// This will be replaced by the full implementation from RFC-0043.
/// 
/// Purpose: Enable RFC-0047 (Pathfinding) to be implemented while
/// RFC-0043 (Map System) is being developed by another agent.
/// </summary>
public class DungeonMap
{
    public int Width { get; set; }
    public int Height { get; set; }
    
    private readonly Tile[,] _tiles;
    
    public DungeonMap(int width, int height)
    {
        Width = width;
        Height = height;
        _tiles = new Tile[width, height];
        
        // Initialize with default floor tiles
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                _tiles[x, y] = new Tile
                {
                    Type = TileType.Floor,
                    IsWalkable = true,
                    IsTransparent = true
                };
            }
        }
    }
    
    public Tile? GetTile(int x, int y)
    {
        if (!IsInBounds(x, y))
            return null;
        
        return _tiles[x, y];
    }
    
    public void SetTile(int x, int y, Tile tile)
    {
        if (IsInBounds(x, y))
        {
            _tiles[x, y] = tile;
        }
    }
    
    public bool IsInBounds(int x, int y)
    {
        return x >= 0 && x < Width && y >= 0 && y < Height;
    }
    
    public bool IsWalkable(int x, int y)
    {
        var tile = GetTile(x, y);
        return tile?.IsWalkable ?? false;
    }
}

/// <summary>
/// STUB: Minimal Tile structure.
/// This will be replaced by the full implementation from RFC-0043.
/// </summary>
public struct Tile
{
    public TileType Type { get; set; }
    public bool IsWalkable { get; set; }
    public bool IsTransparent { get; set; }
    public bool IsExplored { get; set; }
    public bool IsVisible { get; set; }
}

/// <summary>
/// STUB: Tile types for pathfinding costs.
/// This will be expanded in RFC-0043.
/// </summary>
public enum TileType
{
    Floor,
    Wall,
    Door,
    Water,
    Grass,
    Lava,
    Stairs
}

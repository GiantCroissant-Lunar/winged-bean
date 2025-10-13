using SadRogue.Primitives;

namespace WingedBean.Plugins.DungeonGame.Components;

/// <summary>
/// Tile data for dungeon map.
/// </summary>
public struct Tile
{
    public TileType Type;
    public bool IsWalkable;
    public bool IsTransparent;  // For FOV calculations
    public bool IsExplored;     // Has been seen before
    public bool IsVisible;      // Currently visible
    public char Glyph;
    public Color Foreground;
    public Color Background;
}

/// <summary>
/// Types of tiles in the dungeon.
/// </summary>
public enum TileType
{
    Floor,
    Wall,
    Door,
    LockedDoor,
    StairsUp,
    StairsDown,
    Trap,
    Water,
    Lava,
    Chasm,
    Grass,
    Tree
}

/// <summary>
/// Dungeon map data structure.
/// </summary>
public class DungeonMap
{
    public int Width { get; init; }
    public int Height { get; init; }
    public int Floor { get; init; }
    public Tile[,] Tiles { get; init; }
    
    public DungeonMap(int width, int height, int floor)
    {
        Width = width;
        Height = height;
        Floor = floor;
        Tiles = new Tile[width, height];
    }
    
    /// <summary>
    /// Check if a position is within map bounds.
    /// </summary>
    public bool IsInBounds(int x, int y)
    {
        return x >= 0 && x < Width && y >= 0 && y < Height;
    }
    
    /// <summary>
    /// Check if a position is within map bounds.
    /// </summary>
    public bool IsInBounds(Point position)
    {
        return IsInBounds(position.X, position.Y);
    }
    
    /// <summary>
    /// Get tile at position (safe).
    /// </summary>
    public Tile? GetTile(int x, int y)
    {
        return IsInBounds(x, y) ? Tiles[x, y] : null;
    }
    
    /// <summary>
    /// Get tile at position (safe).
    /// </summary>
    public Tile? GetTile(Point position)
    {
        return GetTile(position.X, position.Y);
    }
    
    /// <summary>
    /// Set tile at position (safe).
    /// </summary>
    public void SetTile(int x, int y, Tile tile)
    {
        if (IsInBounds(x, y))
        {
            Tiles[x, y] = tile;
        }
    }
    
    /// <summary>
    /// Set tile at position (safe).
    /// </summary>
    public void SetTile(Point position, Tile tile)
    {
        SetTile(position.X, position.Y, tile);
    }
}

/// <summary>
/// Door component for interactive doors.
/// </summary>
public struct Door
{
    public bool IsOpen;
    public bool IsLocked;
    public string? RequiredKeyId;
}

/// <summary>
/// Trap component.
/// </summary>
public struct Trap
{
    public TrapType Type;
    public int Damage;
    public bool IsTriggered;
    public bool IsVisible;
    public bool IsDisarmed;
}

/// <summary>
/// Types of traps.
/// </summary>
public enum TrapType
{
    Spike,
    Arrow,
    Poison,
    Teleport,
    Alarm,
    Pit,
    Fire
}

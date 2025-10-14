namespace WingedBean.Gameplay.Map.Generation;

/// <summary>
/// Represents a rectangular room in the dungeon.
/// </summary>
public class Room
{
    public int X { get; }
    public int Y { get; }
    public int Width { get; }
    public int Height { get; }

    public int CenterX => X + Width / 2;
    public int CenterY => Y + Height / 2;

    public int Left => X;
    public int Right => X + Width - 1;
    public int Top => Y;
    public int Bottom => Y + Height - 1;

    public Room(int x, int y, int width, int height)
    {
        X = x;
        Y = y;
        Width = width;
        Height = height;
    }

    /// <summary>
    /// Checks if this room intersects with another room (with optional padding).
    /// </summary>
    public bool Intersects(Room other, int padding = 1)
    {
        return X - padding <= other.Right + padding &&
               Right + padding >= other.X - padding &&
               Y - padding <= other.Bottom + padding &&
               Bottom + padding >= other.Y - padding;
    }

    /// <summary>
    /// Gets all tile positions in this room (including walls).
    /// </summary>
    public IEnumerable<(int x, int y)> GetAllTiles()
    {
        for (var y = Y; y < Y + Height; y++)
        {
            for (var x = X; x < X + Width; x++)
            {
                yield return (x, y);
            }
        }
    }

    /// <summary>
    /// Gets floor tile positions (excluding outer walls).
    /// </summary>
    public IEnumerable<(int x, int y)> GetFloorTiles()
    {
        for (var y = Y + 1; y < Y + Height - 1; y++)
        {
            for (var x = X + 1; x < X + Width - 1; x++)
            {
                yield return (x, y);
            }
        }
    }

    public override string ToString()
    {
        return $"Room({X}, {Y}, {Width}x{Height})";
    }
}

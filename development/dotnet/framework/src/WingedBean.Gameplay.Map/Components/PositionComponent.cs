namespace WingedBean.Gameplay.Map.Components;

/// <summary>
/// ECS component that represents an entity's position on the map.
/// </summary>
public struct PositionComponent
{
    /// <summary>
    /// X coordinate on the map.
    /// </summary>
    public int X { get; set; }

    /// <summary>
    /// Y coordinate on the map.
    /// </summary>
    public int Y { get; set; }

    /// <summary>
    /// Creates a new PositionComponent at the specified coordinates.
    /// </summary>
    public PositionComponent(int x, int y)
    {
        X = x;
        Y = y;
    }

    /// <summary>
    /// Calculates the Manhattan distance to another position.
    /// </summary>
    public int ManhattanDistance(PositionComponent other)
    {
        return Math.Abs(X - other.X) + Math.Abs(Y - other.Y);
    }

    /// <summary>
    /// Calculates the Euclidean distance to another position.
    /// </summary>
    public double EuclideanDistance(PositionComponent other)
    {
        var dx = X - other.X;
        var dy = Y - other.Y;
        return Math.Sqrt(dx * dx + dy * dy);
    }

    /// <summary>
    /// Checks if this position equals another position.
    /// </summary>
    public bool Equals(PositionComponent other)
    {
        return X == other.X && Y == other.Y;
    }

    public override string ToString()
    {
        return $"({X}, {Y})";
    }
}

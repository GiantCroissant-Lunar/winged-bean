using Plate.PerceptionSensor.Core;

namespace WingedBean.Gameplay.Perception.Filters;

/// <summary>
/// Simple implementation of IPerceptionFilter for basic map-based filtering.
/// In a real game, this would integrate with your tile/map system.
/// </summary>
public class SimpleMapFilter : IPerceptionFilter
{
    private readonly int _width;
    private readonly int _height;
    private readonly HashSet<Point> _blocked;

    /// <summary>
    /// Creates a new simple map filter.
    /// </summary>
    /// <param name="width">Map width</param>
    /// <param name="height">Map height</param>
    public SimpleMapFilter(int width, int height)
    {
        _width = width;
        _height = height;
        _blocked = new HashSet<Point>();
    }

    /// <summary>
    /// Adds a blocking position (wall, obstacle, etc.).
    /// </summary>
    /// <param name="position">Position to block</param>
    public void AddBlocked(Point position)
    {
        _blocked.Add(position);
    }

    /// <summary>
    /// Removes a blocking position.
    /// </summary>
    /// <param name="position">Position to unblock</param>
    public void RemoveBlocked(Point position)
    {
        _blocked.Remove(position);
    }

    /// <summary>
    /// Checks if a position blocks perception.
    /// </summary>
    public bool Blocks(Point position) => _blocked.Contains(position);

    /// <summary>
    /// Gets attenuation at a position.
    /// </summary>
    public double GetAttenuation(Point position) => Blocks(position) ? 1.0 : 0.0;

    /// <summary>
    /// Checks if a position is within bounds.
    /// </summary>
    public bool IsInBounds(Point position) => 
        position.X >= 0 && position.X < _width && 
        position.Y >= 0 && position.Y < _height;
}

using Plate.PerceptionSensor.Core;

namespace WingedBean.Gameplay.Perception.Components;

/// <summary>
/// Entity has a position in the world.
/// Required for perception calculations.
/// </summary>
public struct PositionComponent
{
    /// <summary>
    /// Position in the world.
    /// </summary>
    public Point Position { get; set; }

    /// <summary>
    /// Initializes a new position component.
    /// </summary>
    /// <param name="x">X coordinate</param>
    /// <param name="y">Y coordinate</param>
    public PositionComponent(int x, int y)
    {
        Position = new Point(x, y);
    }

    /// <summary>
    /// Initializes a new position component.
    /// </summary>
    /// <param name="position">Position</param>
    public PositionComponent(Point position)
    {
        Position = position;
    }
}

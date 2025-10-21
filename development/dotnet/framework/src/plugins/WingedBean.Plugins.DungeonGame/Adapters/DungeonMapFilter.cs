using Plate.PerceptionSensor.Core;
using WingedBean.Plugins.DungeonGame.Components;

namespace WingedBean.Plugins.DungeonGame.Adapters;

/// <summary>
/// Adapter that provides dungeon map transparency data to PerceptionSensor.
/// Implements IPerceptionFilter to integrate DungeonMap with Plate.PerceptionSensor.
/// </summary>
public class DungeonMapFilter : IPerceptionFilter
{
    private readonly DungeonMap _map;

    /// <summary>
    /// Creates a new dungeon map filter.
    /// </summary>
    /// <param name="map">The dungeon map</param>
    public DungeonMapFilter(DungeonMap map)
    {
        _map = map ?? throw new ArgumentNullException(nameof(map));
    }

    /// <summary>
    /// Checks if the position completely blocks perception.
    /// </summary>
    /// <param name="position">Position to check</param>
    /// <returns>True if position blocks perception</returns>
    public bool Blocks(Point position)
    {
        if (!IsInBounds(position))
            return true;

        var tile = _map.GetTile(position.X, position.Y);
        return tile.HasValue && !tile.Value.IsTransparent;
    }

    /// <summary>
    /// Gets attenuation factor at position (0.0 = transparent, 1.0 = opaque).
    /// </summary>
    /// <param name="position">Position to check</param>
    /// <returns>Attenuation factor (0.0 = transparent, 1.0 = opaque)</returns>
    public double GetAttenuation(Point position)
    {
        if (!IsInBounds(position))
            return 1.0; // Fully opaque

        var tile = _map.GetTile(position.X, position.Y);
        return (tile.HasValue && !tile.Value.IsTransparent) ? 1.0 : 0.0;
    }

    /// <summary>
    /// Checks if position is within valid bounds.
    /// </summary>
    /// <param name="position">Position to check</param>
    /// <returns>True if position is in bounds</returns>
    public bool IsInBounds(Point position)
    {
        return _map.IsInBounds(position.X, position.Y);
    }
}

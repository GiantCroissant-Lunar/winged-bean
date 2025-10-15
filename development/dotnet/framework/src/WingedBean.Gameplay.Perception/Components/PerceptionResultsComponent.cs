using Arch.Core;
using Plate.PerceptionSensor.Core;

namespace WingedBean.Gameplay.Perception.Components;

/// <summary>
/// Results of perception calculation (what this entity currently perceives).
/// Updated each frame by PerceptionSystem.
/// </summary>
public struct PerceptionResultsComponent
{
    /// <summary>
    /// All entities detected by ANY perception type.
    /// </summary>
    public HashSet<Entity> DetectedEntities { get; set; }
    
    /// <summary>
    /// Positions detected by each perception type.
    /// Key: perception type (e.g., "vision"), Value: detected positions
    /// </summary>
    public Dictionary<string, HashSet<Point>> DetectedPositionsByType { get; set; }
    
    /// <summary>
    /// Signal strength at each position (for distance-based effects).
    /// Higher values = stronger/clearer perception.
    /// </summary>
    public Dictionary<Point, double> SignalStrengths { get; set; }
    
    /// <summary>
    /// When was perception last updated.
    /// </summary>
    public DateTime LastUpdate { get; set; }

    /// <summary>
    /// Initializes a new perception results component.
    /// </summary>
    public PerceptionResultsComponent()
    {
        DetectedEntities = new HashSet<Entity>();
        DetectedPositionsByType = new Dictionary<string, HashSet<Point>>();
        SignalStrengths = new Dictionary<Point, double>();
        LastUpdate = DateTime.UtcNow;
    }
}

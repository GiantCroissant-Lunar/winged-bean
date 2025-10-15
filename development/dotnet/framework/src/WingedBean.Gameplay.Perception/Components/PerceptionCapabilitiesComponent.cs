using Plate.PerceptionSensor.Core;
using WingedBean.Gameplay.Perception.Configuration;

namespace WingedBean.Gameplay.Perception.Components;

/// <summary>
/// Entity has one or more perception capabilities.
/// Configured via rules evaluation and JSON perception definitions.
/// </summary>
public struct PerceptionCapabilitiesComponent
{
    /// <summary>
    /// List of perception type IDs this entity can use (e.g., "vision", "hearing", "detect_magic").
    /// Populated from rules evaluation.
    /// </summary>
    public List<string> PerceptionTypes { get; set; }
    
    /// <summary>
    /// Cached perception definitions (loaded from JSON configuration).
    /// Key: perception type ID, Value: definition with medium configuration.
    /// </summary>
    public Dictionary<string, PerceptionDefinition> Definitions { get; set; }

    /// <summary>
    /// Initializes a new perception capabilities component.
    /// </summary>
    public PerceptionCapabilitiesComponent()
    {
        PerceptionTypes = new List<string>();
        Definitions = new Dictionary<string, PerceptionDefinition>();
    }
}

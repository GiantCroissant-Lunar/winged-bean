using System.Text.Json.Serialization;

namespace WingedBean.Gameplay.Perception.Configuration;

/// <summary>
/// Defines a perception type loaded from JSON configuration.
/// </summary>
public class PerceptionDefinition
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;
    
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;
    
    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;
    
    [JsonPropertyName("propagation_mode")]
    public string PropagationMode { get; set; } = string.Empty;
    
    [JsonPropertyName("default_range")]
    public int DefaultRange { get; set; }
    
    [JsonPropertyName("attenuation_rate")]
    public double AttenuationRate { get; set; }
    
    [JsonPropertyName("flows_around_obstacles")]
    public bool FlowsAroundObstacles { get; set; }
    
    [JsonPropertyName("blocked_by_material_types")]
    public List<string> BlockedByMaterialTypes { get; set; } = new();
    
    [JsonPropertyName("attenuated_by_material_types")]
    public Dictionary<string, double> AttenuatedByMaterialTypes { get; set; } = new();
    
    [JsonPropertyName("properties")]
    public Dictionary<string, object> Properties { get; set; } = new();
}

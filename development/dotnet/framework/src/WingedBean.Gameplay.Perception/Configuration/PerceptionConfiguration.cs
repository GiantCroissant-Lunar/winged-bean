using System.Text.Json;
using System.Text.Json.Serialization;

namespace WingedBean.Gameplay.Perception.Configuration;

/// <summary>
/// Loads and manages perception type definitions from JSON.
/// </summary>
public class PerceptionConfiguration
{
    /// <summary>
    /// All perception types available in the game.
    /// Key: perception type ID, Value: definition
    /// </summary>
    public Dictionary<string, PerceptionDefinition> PerceptionTypes { get; set; } 
        = new Dictionary<string, PerceptionDefinition>();

    /// <summary>
    /// Loads perception configuration from a JSON file.
    /// </summary>
    /// <param name="jsonPath">Path to perception-types.json</param>
    /// <returns>Loaded configuration</returns>
    public static PerceptionConfiguration LoadFromJson(string jsonPath)
    {
        if (!File.Exists(jsonPath))
        {
            throw new FileNotFoundException($"Perception configuration file not found: {jsonPath}");
        }

        var json = File.ReadAllText(jsonPath);
        var data = JsonSerializer.Deserialize<PerceptionConfigFile>(json, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        if (data == null || data.PerceptionTypes == null)
        {
            throw new InvalidOperationException("Failed to deserialize perception configuration");
        }
        
        var config = new PerceptionConfiguration();
        foreach (var def in data.PerceptionTypes)
        {
            config.PerceptionTypes[def.Id] = def;
        }
        
        return config;
    }

    /// <summary>
    /// Gets a perception definition by ID.
    /// </summary>
    /// <param name="id">Perception type ID</param>
    /// <returns>Definition if found, null otherwise</returns>
    public PerceptionDefinition? GetDefinition(string id)
    {
        return PerceptionTypes.TryGetValue(id, out var def) ? def : null;
    }
}

/// <summary>
/// Root structure for perception-types.json file.
/// </summary>
public class PerceptionConfigFile
{
    [JsonPropertyName("perception_types")]
    public List<PerceptionDefinition> PerceptionTypes { get; set; } = new();
}

using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using Plate.PluginManoi.Core;

namespace ConsoleDungeon.Host;

/// <summary>
/// Describes a plugin to be loaded by the host.
/// </summary>
public class PluginDescriptor
{
    /// <summary>
    /// Unique identifier for the plugin (e.g., "wingedbean.plugins.config").
    /// </summary>
    [JsonPropertyName("id")]
    public string Id { get; set; } = "";

    /// <summary>
    /// Path to the plugin assembly relative to the plugin directory.
    /// </summary>
    [JsonPropertyName("path")]
    public string Path { get; set; } = "";

    /// <summary>
    /// Load priority (higher values load first).
    /// </summary>
    [JsonPropertyName("priority")]
    public int Priority { get; set; } = 0;

    /// <summary>
    /// Strategy for when to load the plugin.
    /// </summary>
    [JsonPropertyName("loadStrategy")]
    public LoadStrategy LoadStrategy { get; set; } = LoadStrategy.Eager;

    /// <summary>
    /// Whether the plugin is enabled.
    /// </summary>
    [JsonPropertyName("enabled")]
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Optional metadata about the plugin (description, author, version, etc.).
    /// </summary>
    [JsonPropertyName("metadata")]
    public Dictionary<string, string>? Metadata { get; set; }

    /// <summary>
    /// Plugin dependencies.
    /// Can be a list of strings (legacy) or structured PluginDependencies object.
    /// </summary>
    [JsonPropertyName("dependencies")]
    [JsonConverter(typeof(PluginDependenciesConverter))]
    public PluginDependencies? Dependencies { get; set; }
}

/// <summary>
/// Custom JSON converter to handle both legacy (string array) and new (object) dependency formats.
/// </summary>
public class PluginDependenciesConverter : JsonConverter<PluginDependencies?>
{
    public override PluginDependencies? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null)
        {
            return null;
        }
        
        // Check if it's an array (legacy format: ["plugin1", "plugin2"])
        if (reader.TokenType == JsonTokenType.StartArray)
        {
            var plugins = JsonSerializer.Deserialize<List<string>>(ref reader, options);
            return new PluginDependencies { Plugins = plugins };
        }
        
        // Check if it's an object (new format with nuget)
        if (reader.TokenType == JsonTokenType.StartObject)
        {
            return JsonSerializer.Deserialize<PluginDependencies>(ref reader, options);
        }
        
        // Check if it's a single string (very old legacy format)
        if (reader.TokenType == JsonTokenType.String)
        {
            var singlePlugin = reader.GetString();
            return new PluginDependencies 
            { 
                Plugins = singlePlugin != null ? new List<string> { singlePlugin } : null 
            };
        }
        
        throw new JsonException("Invalid dependencies format");
    }

    public override void Write(Utf8JsonWriter writer, PluginDependencies? value, JsonSerializerOptions options)
    {
        if (value == null)
        {
            writer.WriteNullValue();
            return;
        }
        
        // Always write as object format for new manifests
        JsonSerializer.Serialize(writer, value, options);
    }
}

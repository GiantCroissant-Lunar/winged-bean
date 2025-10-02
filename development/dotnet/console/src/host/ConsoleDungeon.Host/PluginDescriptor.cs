using System.Collections.Generic;
using System.Text.Json.Serialization;

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
    /// Optional list of plugin IDs that this plugin depends on.
    /// </summary>
    [JsonPropertyName("dependencies")]
    public List<string>? Dependencies { get; set; }
}

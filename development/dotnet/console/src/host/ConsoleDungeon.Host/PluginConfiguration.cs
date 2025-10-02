using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace ConsoleDungeon.Host;

/// <summary>
/// Root configuration for the plugin system.
/// </summary>
public class PluginConfiguration
{
    /// <summary>
    /// Configuration file format version.
    /// </summary>
    [JsonPropertyName("version")]
    public string Version { get; set; } = "1.0";

    /// <summary>
    /// Directory containing plugin assemblies, relative to the host executable.
    /// </summary>
    [JsonPropertyName("pluginDirectory")]
    public string PluginDirectory { get; set; } = "plugins";

    /// <summary>
    /// List of plugins to load.
    /// </summary>
    [JsonPropertyName("plugins")]
    public List<PluginDescriptor> Plugins { get; set; } = new();
}

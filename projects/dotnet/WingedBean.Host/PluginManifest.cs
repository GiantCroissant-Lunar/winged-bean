using System.Text.Json.Serialization;

namespace WingedBean.Host;

/// <summary>
/// Plugin manifest metadata loaded from .plugin.json files
/// </summary>
public class PluginManifest
{
    /// <summary>Plugin unique identifier (e.g., wingedbean.providers.pty.node)</summary>
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    /// <summary>Plugin version (semver)</summary>
    [JsonPropertyName("version")]
    public string Version { get; set; } = string.Empty;

    /// <summary>Plugin display name</summary>
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>Plugin description</summary>
    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;

    /// <summary>Plugin author</summary>
    [JsonPropertyName("author")]
    public string Author { get; set; } = string.Empty;

    /// <summary>Plugin license</summary>
    [JsonPropertyName("license")]
    public string License { get; set; } = string.Empty;

    /// <summary>Entry points for different runtime profiles</summary>
    [JsonPropertyName("entryPoint")]
    public PluginEntryPoint EntryPoint { get; set; } = new();

    /// <summary>Plugin dependencies (id -> version requirement)</summary>
    [JsonPropertyName("dependencies")]
    public Dictionary<string, string> Dependencies { get; set; } = new();

    /// <summary>Services exported by this plugin</summary>
    [JsonPropertyName("exports")]
    public PluginExports Exports { get; set; } = new();

    /// <summary>Plugin capabilities/tags</summary>
    [JsonPropertyName("capabilities")]
    public List<string> Capabilities { get; set; } = new();

    /// <summary>Supported runtime profiles</summary>
    [JsonPropertyName("supportedProfiles")]
    public List<string> SupportedProfiles { get; set; } = new();

    /// <summary>Plugin load strategy (eager/lazy)</summary>
    [JsonPropertyName("loadStrategy")]
    public string LoadStrategy { get; set; } = "lazy";

    /// <summary>Quiesce time in seconds before unloading</summary>
    [JsonPropertyName("quiesceSeconds")]
    public int QuiesceSeconds { get; set; } = 5;
}

/// <summary>
/// Entry points for different runtime profiles
/// </summary>
public class PluginEntryPoint
{
    /// <summary>.NET assembly path</summary>
    [JsonPropertyName("dotnet")]
    public string? Dotnet { get; set; }

    /// <summary>Node.js module path</summary>
    [JsonPropertyName("nodejs")]
    public string? Nodejs { get; set; }

    /// <summary>Unity assembly path</summary>
    [JsonPropertyName("unity")]
    public string? Unity { get; set; }

    /// <summary>Godot assembly path</summary>
    [JsonPropertyName("godot")]
    public string? Godot { get; set; }
}

/// <summary>
/// Services exported by the plugin
/// </summary>
public class PluginExports
{
    /// <summary>Service registrations</summary>
    [JsonPropertyName("services")]
    public List<PluginServiceExport> Services { get; set; } = new();
}

/// <summary>
/// Individual service export
/// </summary>
public class PluginServiceExport
{
    /// <summary>Service interface name</summary>
    [JsonPropertyName("interface")]
    public string Interface { get; set; } = string.Empty;

    /// <summary>Implementation class name</summary>
    [JsonPropertyName("implementation")]
    public string Implementation { get; set; } = string.Empty;

    /// <summary>Service lifecycle (singleton/scoped/transient)</summary>
    [JsonPropertyName("lifecycle")]
    public string Lifecycle { get; set; } = "transient";
}

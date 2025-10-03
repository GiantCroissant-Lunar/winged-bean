using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace WingedBean.PluginSystem;

/// <summary>
/// Discovers plugins by scanning directories for .plugin.json manifest files
/// </summary>
public class PluginDiscovery
{
    private readonly string[] _pluginDirectories;
    private readonly ILogger<PluginDiscovery>? _logger;

    /// <summary>
    /// Initialize plugin discovery with directories to scan
    /// </summary>
    /// <param name="pluginDirectories">Directories to scan for plugins</param>
    public PluginDiscovery(params string[] pluginDirectories)
    {
        _pluginDirectories = pluginDirectories;
    }

    /// <summary>
    /// Initialize plugin discovery with directories and logger
    /// </summary>
    /// <param name="logger">Logger instance</param>
    /// <param name="pluginDirectories">Directories to scan for plugins</param>
    public PluginDiscovery(ILogger<PluginDiscovery> logger, params string[] pluginDirectories)
    {
        _logger = logger;
        _pluginDirectories = pluginDirectories;
    }

    /// <summary>
    /// Discover all plugins in the configured directories
    /// </summary>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Collection of discovered plugin manifests</returns>
    public async Task<IEnumerable<PluginManifest>> DiscoverPluginsAsync(CancellationToken ct = default)
    {
        var manifests = new List<PluginManifest>();

        foreach (var directory in _pluginDirectories)
        {
            if (!Directory.Exists(directory))
            {
                _logger?.LogWarning("Plugin directory does not exist: {Directory}", directory);
                continue;
            }

            _logger?.LogInformation("Scanning plugin directory: {Directory}", directory);

            var manifestFiles = Directory.GetFiles(directory, "*.plugin.json", SearchOption.AllDirectories);

            foreach (var manifestFile in manifestFiles)
            {
                try
                {
                    _logger?.LogDebug("Loading plugin manifest: {ManifestFile}", manifestFile);

                    var json = await File.ReadAllTextAsync(manifestFile, ct);
                    var manifest = JsonSerializer.Deserialize<PluginManifest>(json);

                    if (manifest == null)
                    {
                        _logger?.LogWarning("Failed to deserialize plugin manifest: {ManifestFile}", manifestFile);
                        continue;
                    }

                    if (string.IsNullOrEmpty(manifest.Id))
                    {
                        _logger?.LogWarning("Plugin manifest missing ID: {ManifestFile}", manifestFile);
                        continue;
                    }

                    // Resolve relative paths to absolute paths based on manifest directory
                    var manifestDir = Path.GetDirectoryName(manifestFile)!;
                    ResolveRelativePaths(manifest, manifestDir);

                    manifests.Add(manifest);
                    _logger?.LogInformation("Discovered plugin: {PluginId} v{Version}", manifest.Id, manifest.Version);
                }
                catch (Exception ex)
                {
                    _logger?.LogError(ex, "Error loading plugin manifest: {ManifestFile}", manifestFile);
                }
            }
        }

        _logger?.LogInformation("Discovered {Count} plugins", manifests.Count);
        return manifests;
    }

    /// <summary>
    /// Resolve relative paths in manifest to absolute paths
    /// </summary>
    /// <param name="manifest">Plugin manifest</param>
    /// <param name="baseDirectory">Base directory to resolve relative paths from</param>
    private static void ResolveRelativePaths(PluginManifest manifest, string baseDirectory)
    {
        if (!string.IsNullOrEmpty(manifest.EntryPoint.Dotnet) && !Path.IsPathRooted(manifest.EntryPoint.Dotnet))
        {
            manifest.EntryPoint.Dotnet = Path.GetFullPath(Path.Combine(baseDirectory, manifest.EntryPoint.Dotnet));
        }

        if (!string.IsNullOrEmpty(manifest.EntryPoint.Nodejs) && !Path.IsPathRooted(manifest.EntryPoint.Nodejs))
        {
            manifest.EntryPoint.Nodejs = Path.GetFullPath(Path.Combine(baseDirectory, manifest.EntryPoint.Nodejs));
        }

        if (!string.IsNullOrEmpty(manifest.EntryPoint.Unity) && !Path.IsPathRooted(manifest.EntryPoint.Unity))
        {
            manifest.EntryPoint.Unity = Path.GetFullPath(Path.Combine(baseDirectory, manifest.EntryPoint.Unity));
        }

        if (!string.IsNullOrEmpty(manifest.EntryPoint.Godot) && !Path.IsPathRooted(manifest.EntryPoint.Godot))
        {
            manifest.EntryPoint.Godot = Path.GetFullPath(Path.Combine(baseDirectory, manifest.EntryPoint.Godot));
        }
    }
}

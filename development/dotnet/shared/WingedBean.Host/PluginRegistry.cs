using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace WingedBean.Host;

/// <summary>
/// Plugin registry for storing and managing plugin metadata
/// </summary>
public interface IPluginRegistry
{
    /// <summary>Register a plugin in the registry</summary>
    Task RegisterPluginAsync(PluginManifest manifest, CancellationToken ct = default);

    /// <summary>Unregister a plugin from the registry</summary>
    Task UnregisterPluginAsync(string pluginId, CancellationToken ct = default);

    /// <summary>Find all plugins matching criteria</summary>
    Task<IEnumerable<PluginManifest>> FindPluginsAsync(PluginSearchCriteria? criteria = null, CancellationToken ct = default);

    /// <summary>Get a specific plugin by ID and optional version</summary>
    Task<PluginManifest?> GetPluginAsync(string pluginId, string? version = null, CancellationToken ct = default);

    /// <summary>Get all versions of a plugin</summary>
    Task<IEnumerable<PluginManifest>> GetPluginVersionsAsync(string pluginId, CancellationToken ct = default);

    /// <summary>Update plugin metadata</summary>
    Task UpdatePluginAsync(PluginManifest manifest, CancellationToken ct = default);

    /// <summary>Get plugin statistics</summary>
    Task<PluginStatistics> GetStatisticsAsync(CancellationToken ct = default);
}

/// <summary>
/// Search criteria for finding plugins
/// </summary>
public class PluginSearchCriteria
{
    /// <summary>Plugin ID pattern (supports wildcards)</summary>
    public string? IdPattern { get; set; }

    /// <summary>Name search term</summary>
    public string? NameContains { get; set; }

    /// <summary>Description search term</summary>
    public string? DescriptionContains { get; set; }

    /// <summary>Author name</summary>
    public string? Author { get; set; }

    /// <summary>Required capabilities</summary>
    public List<string> RequiredCapabilities { get; set; } = new();

    /// <summary>Supported profiles</summary>
    public List<string> SupportedProfiles { get; set; } = new();

    /// <summary>Minimum version</summary>
    public string? MinVersion { get; set; }

    /// <summary>Maximum version</summary>
    public string? MaxVersion { get; set; }

    /// <summary>Security level requirement</summary>
    public SecurityLevel? SecurityLevel { get; set; }

    /// <summary>Only signed plugins</summary>
    public bool OnlySigned { get; set; } = false;

    /// <summary>Load strategy</summary>
    public string? LoadStrategy { get; set; }

    /// <summary>Tags to include</summary>
    public List<string> Tags { get; set; } = new();

    /// <summary>Maximum results to return</summary>
    public int? MaxResults { get; set; }

    /// <summary>Sort order</summary>
    public PluginSortOrder SortOrder { get; set; } = PluginSortOrder.Name;
}

/// <summary>
/// Plugin sort order options
/// </summary>
public enum PluginSortOrder
{
    Name,
    Version,
    Author,
    LoadOrder,
    Popularity,
    LastUpdated
}

/// <summary>
/// Plugin registry statistics
/// </summary>
public class PluginStatistics
{
    /// <summary>Total number of plugins</summary>
    public int TotalPlugins { get; set; }

    /// <summary>Total number of unique plugin IDs</summary>
    public int UniquePlugins { get; set; }

    /// <summary>Number of signed plugins</summary>
    public int SignedPlugins { get; set; }

    /// <summary>Plugins by profile</summary>
    public Dictionary<string, int> PluginsByProfile { get; set; } = new();

    /// <summary>Plugins by security level</summary>
    public Dictionary<SecurityLevel, int> PluginsBySecurityLevel { get; set; } = new();

    /// <summary>Most popular plugins</summary>
    public List<PluginPopularity> PopularPlugins { get; set; } = new();
}

/// <summary>
/// Plugin popularity information
/// </summary>
public class PluginPopularity
{
    /// <summary>Plugin ID</summary>
    public string PluginId { get; set; } = string.Empty;

    /// <summary>Plugin name</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Download count</summary>
    public long Downloads { get; set; }

    /// <summary>Usage count</summary>
    public long Usage { get; set; }

    /// <summary>Rating</summary>
    public double Rating { get; set; }
}

/// <summary>
/// File-based plugin registry implementation
/// </summary>
public class FilePluginRegistry : IPluginRegistry
{
    private readonly string _registryPath;
    private readonly ILogger<FilePluginRegistry>? _logger;
    private readonly Dictionary<string, List<PluginManifest>> _plugins = new();
    private readonly SemaphoreSlim _semaphore = new(1, 1);

    public FilePluginRegistry(string registryPath, ILogger<FilePluginRegistry>? logger = null)
    {
        _registryPath = registryPath;
        _logger = logger;

        Directory.CreateDirectory(Path.GetDirectoryName(_registryPath) ?? ".");
        LoadRegistryAsync().GetAwaiter().GetResult();
    }

    public async Task RegisterPluginAsync(PluginManifest manifest, CancellationToken ct = default)
    {
        await _semaphore.WaitAsync(ct);
        try
        {
            if (!_plugins.ContainsKey(manifest.Id))
                _plugins[manifest.Id] = new List<PluginManifest>();

            // Remove existing version if present
            _plugins[manifest.Id].RemoveAll(p => p.Version == manifest.Version);

            // Add new version
            _plugins[manifest.Id].Add(manifest);

            // Sort by version descending
            _plugins[manifest.Id] = _plugins[manifest.Id]
                .OrderByDescending(p => p.SemanticVersion)
                .ToList();

            await SaveRegistryAsync(ct);

            _logger?.LogInformation("Registered plugin {PluginId} v{Version}", manifest.Id, manifest.Version);
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public async Task UnregisterPluginAsync(string pluginId, CancellationToken ct = default)
    {
        await _semaphore.WaitAsync(ct);
        try
        {
            if (_plugins.Remove(pluginId))
            {
                await SaveRegistryAsync(ct);
                _logger?.LogInformation("Unregistered plugin {PluginId}", pluginId);
            }
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public async Task<IEnumerable<PluginManifest>> FindPluginsAsync(PluginSearchCriteria? criteria = null, CancellationToken ct = default)
    {
        await _semaphore.WaitAsync(ct);
        try
        {
            var allPlugins = _plugins.Values.SelectMany(versions => versions).ToList();

            if (criteria == null)
                return allPlugins;

            var filtered = allPlugins.AsEnumerable();

            // Apply filters
            if (!string.IsNullOrEmpty(criteria.IdPattern))
            {
                var pattern = criteria.IdPattern.Replace("*", ".*");
                filtered = filtered.Where(p => System.Text.RegularExpressions.Regex.IsMatch(p.Id, pattern));
            }

            if (!string.IsNullOrEmpty(criteria.NameContains))
                filtered = filtered.Where(p => p.Name.Contains(criteria.NameContains, StringComparison.OrdinalIgnoreCase));

            if (!string.IsNullOrEmpty(criteria.DescriptionContains))
                filtered = filtered.Where(p => p.Description.Contains(criteria.DescriptionContains, StringComparison.OrdinalIgnoreCase));

            if (!string.IsNullOrEmpty(criteria.Author))
                filtered = filtered.Where(p => p.Author.Equals(criteria.Author, StringComparison.OrdinalIgnoreCase));

            if (criteria.RequiredCapabilities.Any())
                filtered = filtered.Where(p => criteria.RequiredCapabilities.All(cap => p.Capabilities.Contains(cap)));

            if (criteria.SupportedProfiles.Any())
                filtered = filtered.Where(p => criteria.SupportedProfiles.Any(profile => p.SupportedProfiles.Contains(profile)));

            if (!string.IsNullOrEmpty(criteria.MinVersion) && SemanticVersion.TryParse(criteria.MinVersion, out var minVer))
                filtered = filtered.Where(p => p.SemanticVersion >= minVer);

            if (!string.IsNullOrEmpty(criteria.MaxVersion) && SemanticVersion.TryParse(criteria.MaxVersion, out var maxVer))
                filtered = filtered.Where(p => p.SemanticVersion <= maxVer);

            if (criteria.SecurityLevel.HasValue)
                filtered = filtered.Where(p => p.Security?.SecurityLevel == criteria.SecurityLevel.Value);

            if (criteria.OnlySigned)
                filtered = filtered.Where(p => p.Security?.Signature != null);

            if (!string.IsNullOrEmpty(criteria.LoadStrategy))
                filtered = filtered.Where(p => p.LoadStrategy.Equals(criteria.LoadStrategy, StringComparison.OrdinalIgnoreCase));

            // Apply sorting
            filtered = criteria.SortOrder switch
            {
                PluginSortOrder.Name => filtered.OrderBy(p => p.Name),
                PluginSortOrder.Version => filtered.OrderByDescending(p => p.SemanticVersion),
                PluginSortOrder.Author => filtered.OrderBy(p => p.Author),
                PluginSortOrder.LastUpdated => filtered.OrderByDescending(p => p.Security?.Signature?.SignedAt ?? DateTimeOffset.MinValue),
                _ => filtered.OrderBy(p => p.Name)
            };

            // Apply limit
            if (criteria.MaxResults.HasValue)
                filtered = filtered.Take(criteria.MaxResults.Value);

            return filtered.ToList();
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public async Task<PluginManifest?> GetPluginAsync(string pluginId, string? version = null, CancellationToken ct = default)
    {
        await _semaphore.WaitAsync(ct);
        try
        {
            if (!_plugins.TryGetValue(pluginId, out var versions))
                return null;

            if (string.IsNullOrEmpty(version))
                return versions.FirstOrDefault(); // Latest version

            return versions.FirstOrDefault(p => p.Version == version);
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public async Task<IEnumerable<PluginManifest>> GetPluginVersionsAsync(string pluginId, CancellationToken ct = default)
    {
        await _semaphore.WaitAsync(ct);
        try
        {
            return _plugins.TryGetValue(pluginId, out var versions) ? versions : Enumerable.Empty<PluginManifest>();
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public async Task UpdatePluginAsync(PluginManifest manifest, CancellationToken ct = default)
    {
        await RegisterPluginAsync(manifest, ct); // Same as register - replaces existing version
    }

    public async Task<PluginStatistics> GetStatisticsAsync(CancellationToken ct = default)
    {
        await _semaphore.WaitAsync(ct);
        try
        {
            var allPlugins = _plugins.Values.SelectMany(versions => versions).ToList();

            var stats = new PluginStatistics
            {
                TotalPlugins = allPlugins.Count,
                UniquePlugins = _plugins.Count,
                SignedPlugins = allPlugins.Count(p => p.Security?.Signature != null)
            };

            // Group by profile
            foreach (var plugin in allPlugins)
            {
                foreach (var profile in plugin.SupportedProfiles)
                {
                    if (!stats.PluginsByProfile.ContainsKey(profile))
                        stats.PluginsByProfile[profile] = 0;
                    stats.PluginsByProfile[profile]++;
                }
            }

            // Group by security level
            foreach (var securityLevel in Enum.GetValues<SecurityLevel>())
            {
                stats.PluginsBySecurityLevel[securityLevel] = allPlugins.Count(p =>
                    (p.Security?.SecurityLevel ?? SecurityLevel.Standard) == securityLevel);
            }

            return stats;
        }
        finally
        {
            _semaphore.Release();
        }
    }

    private async Task LoadRegistryAsync()
    {
        try
        {
            if (!File.Exists(_registryPath))
            {
                _logger?.LogInformation("Plugin registry file not found, starting with empty registry");
                return;
            }

            var json = await File.ReadAllTextAsync(_registryPath);
            var registryData = JsonSerializer.Deserialize<Dictionary<string, List<PluginManifest>>>(json);

            if (registryData != null)
            {
                _plugins.Clear();
                foreach (var (pluginId, versions) in registryData)
                {
                    _plugins[pluginId] = versions.OrderByDescending(v => v.SemanticVersion).ToList();
                }
            }

            _logger?.LogInformation("Loaded plugin registry with {UniquePlugins} unique plugins, {TotalVersions} total versions",
                _plugins.Count, _plugins.Values.Sum(v => v.Count));
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to load plugin registry from {Path}", _registryPath);
        }
    }

    private async Task SaveRegistryAsync(CancellationToken ct = default)
    {
        try
        {
            var json = JsonSerializer.Serialize(_plugins, new JsonSerializerOptions
            {
                WriteIndented = true
            });
            await File.WriteAllTextAsync(_registryPath, json, ct);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to save plugin registry to {Path}", _registryPath);
        }
    }
}

using Microsoft.Extensions.Logging;

namespace WingedBean.PluginSystem;

/// <summary>
/// Resolves plugin dependencies and determines load order using topological sorting
/// </summary>
public class PluginDependencyResolver
{
    private readonly ILogger<PluginDependencyResolver>? _logger;

    /// <summary>
    /// Initialize dependency resolver
    /// </summary>
    public PluginDependencyResolver()
    {
    }

    /// <summary>
    /// Initialize dependency resolver with logger
    /// </summary>
    /// <param name="logger">Logger instance</param>
    public PluginDependencyResolver(ILogger<PluginDependencyResolver> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Resolve plugin load order using topological sort (Kahn's algorithm) with version compatibility
    /// </summary>
    /// <param name="manifests">Plugin manifests to order</param>
    /// <param name="hostVersion">Host version for compatibility checking</param>
    /// <returns>Plugins ordered by dependency requirements</returns>
    /// <exception cref="InvalidOperationException">Thrown when circular dependencies are detected</exception>
    public IEnumerable<PluginManifest> ResolveLoadOrder(IEnumerable<PluginManifest> manifests, SemanticVersion? hostVersion = null)
    {
        var manifestList = manifests.ToList();

        // Filter by host compatibility if version provided
        if (hostVersion != null)
        {
            manifestList = manifestList.Where(m => m.IsCompatibleWith(hostVersion)).ToList();
            _logger?.LogInformation("Filtered {Original} plugins to {Filtered} compatible with host version {Version}",
                manifests.Count(), manifestList.Count, hostVersion);
        }

        // Group by plugin ID and select best version for each
        var bestVersions = SelectBestVersions(manifestList);
        _logger?.LogInformation("Selected best versions for {Count} unique plugins", bestVersions.Count);

        var manifestMap = bestVersions.ToDictionary(m => m.Id, m => m);

        // Validate version dependencies
        ValidateVersionDependencies(bestVersions);

        // Build dependency graph: plugin -> list of plugins it depends on
        var dependencyGraph = new Dictionary<string, List<string>>();
        var inDegree = new Dictionary<string, int>();

        // Initialize graph
        foreach (var manifest in bestVersions)
        {
            dependencyGraph[manifest.Id] = new List<string>();
            inDegree[manifest.Id] = 0;
        }

        // Populate dependencies
        foreach (var manifest in bestVersions)
        {
            foreach (var dependency in manifest.Dependencies.Keys)
            {
                // Only consider dependencies that are in our manifest set
                if (manifestMap.ContainsKey(dependency))
                {
                    dependencyGraph[manifest.Id].Add(dependency);
                    inDegree[dependency]++;

                    _logger?.LogDebug("Plugin {Plugin} v{Version} depends on {Dependency}",
                        manifest.Id, manifest.Version, dependency);
                }
                else
                {
                    _logger?.LogWarning("Plugin {Plugin} depends on {Dependency} which was not found",
                        manifest.Id, dependency);
                }
            }
        }

        // Kahn's algorithm for topological sorting
        var sorted = new List<PluginManifest>();
        var queue = new Queue<string>();

        // Start with plugins that have no dependencies
        foreach (var kvp in inDegree.Where(kv => kv.Value == 0))
        {
            queue.Enqueue(kvp.Key);
            _logger?.LogDebug("Plugin {Plugin} has no dependencies, adding to load queue", kvp.Key);
        }

        while (queue.Count > 0)
        {
            var pluginId = queue.Dequeue();
            sorted.Add(manifestMap[pluginId]);

            _logger?.LogDebug("Adding plugin {Plugin} v{Version} to load order (position {Position})",
                pluginId, manifestMap[pluginId].Version, sorted.Count);

            // Reduce in-degree for all plugins that depend on this one
            foreach (var dependent in dependencyGraph.Keys)
            {
                if (dependencyGraph[dependent].Contains(pluginId))
                {
                    inDegree[dependent]--;
                    if (inDegree[dependent] == 0)
                    {
                        queue.Enqueue(dependent);
                        _logger?.LogDebug("Plugin {Plugin} dependencies resolved, adding to load queue", dependent);
                    }
                }
            }
        }

        // Check for circular dependencies
        if (sorted.Count != bestVersions.Count)
        {
            var remaining = bestVersions.Where(m => !sorted.Contains(m)).Select(m => $"{m.Id} v{m.Version}");
            var message = $"Circular dependency detected in plugins: {string.Join(", ", remaining)}";
            _logger?.LogError(message);
            throw new InvalidOperationException(message);
        }

        _logger?.LogInformation("Plugin load order resolved: {LoadOrder}",
            string.Join(" -> ", sorted.Select(m => $"{m.Id} v{m.Version}")));

        return sorted;
    }

    /// <summary>
    /// Select the best version for each plugin ID from multiple available versions
    /// </summary>
    private List<PluginManifest> SelectBestVersions(List<PluginManifest> manifests)
    {
        return manifests
            .GroupBy(m => m.Id)
            .Select(g => g.OrderByDescending(m => m.SemanticVersion).First())
            .ToList();
    }

    /// <summary>
    /// Validate that version dependencies can be satisfied
    /// </summary>
    private void ValidateVersionDependencies(List<PluginManifest> manifests)
    {
        var availableVersions = manifests.ToDictionary(m => m.Id, m => m.SemanticVersion);

        foreach (var manifest in manifests)
        {
            foreach (var (dependencyId, versionRange) in manifest.Dependencies)
            {
                if (!availableVersions.TryGetValue(dependencyId, out var availableVersion))
                {
                    _logger?.LogWarning("Plugin {Plugin} depends on {Dependency} which is not available",
                        manifest.Id, dependencyId);
                    continue;
                }

                if (!VersionRange.TryParse(versionRange, out var range))
                {
                    _logger?.LogWarning("Plugin {Plugin} has invalid version range {Range} for dependency {Dependency}",
                        manifest.Id, versionRange, dependencyId);
                    continue;
                }

                if (!range.Satisfies(availableVersion))
                {
                    throw new InvalidOperationException(
                        $"Plugin {manifest.Id} requires {dependencyId} {versionRange}, but version {availableVersion} is available");
                }

                _logger?.LogDebug("Dependency satisfied: {Plugin} requires {Dependency} {Range}, found {Version}",
                    manifest.Id, dependencyId, versionRange, availableVersion);
            }
        }
    }

    /// <summary>
    /// Validate that all plugin dependencies can be satisfied
    /// </summary>
    /// <param name="manifests">Plugin manifests to validate</param>
    /// <returns>True if all dependencies can be satisfied</returns>
    public bool ValidateDependencies(IEnumerable<PluginManifest> manifests)
    {
        var manifestList = manifests.ToList();
        var availableVersions = manifestList.ToDictionary(m => m.Id, m => m.SemanticVersion);
        var isValid = true;

        foreach (var manifest in manifestList)
        {
            foreach (var (dependencyId, versionRange) in manifest.Dependencies)
            {
                if (!availableVersions.TryGetValue(dependencyId, out var availableVersion))
                {
                    _logger?.LogError("Plugin {Plugin} depends on {Dependency} which is not available",
                        manifest.Id, dependencyId);
                    isValid = false;
                    continue;
                }

                if (!VersionRange.TryParse(versionRange, out var range))
                {
                    _logger?.LogError("Plugin {Plugin} has invalid version range {Range} for dependency {Dependency}",
                        manifest.Id, versionRange, dependencyId);
                    isValid = false;
                    continue;
                }

                if (!range.Satisfies(availableVersion))
                {
                    _logger?.LogError("Plugin {Plugin} requires {Dependency} {Range}, but version {Version} is available",
                        manifest.Id, dependencyId, versionRange, availableVersion);
                    isValid = false;
                }
            }

            // Check for conflicting plugins
            foreach (var conflict in manifest.Compatibility.Conflicts)
            {
                if (availableVersions.ContainsKey(conflict))
                {
                    _logger?.LogError("Plugin {Plugin} conflicts with {Conflict} which is also present",
                        manifest.Id, conflict);
                    isValid = false;
                }
            }
        }

        return isValid;
    }

    /// <summary>
    /// Find all available versions of a plugin across multiple manifests
    /// </summary>
    /// <param name="manifests">All available plugin manifests</param>
    /// <param name="pluginId">Plugin ID to search for</param>
    /// <returns>List of available versions sorted descending</returns>
    public List<SemanticVersion> FindAvailableVersions(IEnumerable<PluginManifest> manifests, string pluginId)
    {
        return manifests
            .Where(m => m.Id == pluginId)
            .Select(m => m.SemanticVersion)
            .OrderByDescending(v => v)
            .ToList();
    }

    /// <summary>
    /// Find the best version of a plugin that satisfies a version range
    /// </summary>
    /// <param name="manifests">All available plugin manifests</param>
    /// <param name="pluginId">Plugin ID to search for</param>
    /// <param name="versionRange">Version range requirement</param>
    /// <returns>Best matching plugin manifest or null if none found</returns>
    public PluginManifest? FindBestVersion(IEnumerable<PluginManifest> manifests, string pluginId, string versionRange)
    {
        if (!VersionRange.TryParse(versionRange, out var range))
            return null;

        return manifests
            .Where(m => m.Id == pluginId && range.Satisfies(m.SemanticVersion))
            .OrderByDescending(m => m.SemanticVersion)
            .FirstOrDefault();
    }
}

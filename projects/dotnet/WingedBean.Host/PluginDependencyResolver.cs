using Microsoft.Extensions.Logging;

namespace WingedBean.Host;

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
    /// Resolve plugin load order using topological sort (Kahn's algorithm)
    /// </summary>
    /// <param name="manifests">Plugin manifests to order</param>
    /// <returns>Plugins ordered by dependency requirements</returns>
    /// <exception cref="InvalidOperationException">Thrown when circular dependencies are detected</exception>
    public IEnumerable<PluginManifest> ResolveLoadOrder(IEnumerable<PluginManifest> manifests)
    {
        var manifestList = manifests.ToList();
        var manifestMap = manifestList.ToDictionary(m => m.Id, m => m);

        _logger?.LogInformation("Resolving load order for {Count} plugins", manifestList.Count);

        // Build dependency graph: plugin -> list of plugins it depends on
        var dependencyGraph = new Dictionary<string, List<string>>();
        var inDegree = new Dictionary<string, int>();

        // Initialize graph
        foreach (var manifest in manifestList)
        {
            dependencyGraph[manifest.Id] = new List<string>();
            inDegree[manifest.Id] = 0;
        }

        // Populate dependencies
        foreach (var manifest in manifestList)
        {
            foreach (var dependency in manifest.Dependencies.Keys)
            {
                // Only consider dependencies that are in our manifest set
                if (manifestMap.ContainsKey(dependency))
                {
                    dependencyGraph[manifest.Id].Add(dependency);
                    inDegree[dependency]++;
                    
                    _logger?.LogDebug("Plugin {Plugin} depends on {Dependency}", manifest.Id, dependency);
                }
                else
                {
                    _logger?.LogWarning("Plugin {Plugin} depends on {Dependency} which was not found", manifest.Id, dependency);
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
            
            _logger?.LogDebug("Adding plugin {Plugin} to load order (position {Position})", pluginId, sorted.Count);

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
        if (sorted.Count != manifestList.Count)
        {
            var remaining = manifestList.Where(m => !sorted.Contains(m)).Select(m => m.Id);
            var message = $"Circular dependency detected in plugins: {string.Join(", ", remaining)}";
            _logger?.LogError(message);
            throw new InvalidOperationException(message);
        }

        _logger?.LogInformation("Plugin load order resolved: {LoadOrder}", 
            string.Join(" -> ", sorted.Select(m => m.Id)));

        return sorted;
    }

    /// <summary>
    /// Validate that all plugin dependencies can be satisfied
    /// </summary>
    /// <param name="manifests">Plugin manifests to validate</param>
    /// <returns>True if all dependencies can be satisfied</returns>
    public bool ValidateDependencies(IEnumerable<PluginManifest> manifests)
    {
        var manifestList = manifests.ToList();
        var availablePlugins = new HashSet<string>(manifestList.Select(m => m.Id));
        var isValid = true;

        foreach (var manifest in manifestList)
        {
            foreach (var dependency in manifest.Dependencies.Keys)
            {
                if (!availablePlugins.Contains(dependency))
                {
                    _logger?.LogError("Plugin {Plugin} depends on {Dependency} which is not available", 
                        manifest.Id, dependency);
                    isValid = false;
                }
            }
        }

        return isValid;
    }
}

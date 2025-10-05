using System.Reflection;
using System.Runtime.Loader;
using Microsoft.Extensions.Logging;
using WingedBean.PluginSystem;

namespace WingedBean.Host.Console;

/// <summary>
/// AssemblyLoadContext-based plugin loader for Console profile (.NET)
/// Supports hot-reload through collectible load contexts
/// </summary>
public class AlcPluginLoader : IPluginLoader
{
    private readonly Dictionary<string, AssemblyLoadContext> _loadContexts = new();
    private readonly Dictionary<string, LoadedPlugin> _loadedPlugins = new();
    private readonly ILogger<AlcPluginLoader>? _logger;

    /// <summary>
    /// Initialize ALC plugin loader
    /// </summary>
    public AlcPluginLoader()
    {
    }

    /// <summary>
    /// Initialize ALC plugin loader with logger
    /// </summary>
    /// <param name="logger">Logger instance</param>
    public AlcPluginLoader(ILogger<AlcPluginLoader> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Load a plugin using AssemblyLoadContext
    /// </summary>
    /// <param name="manifest">Plugin manifest</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Loaded plugin instance</returns>
    public async Task<ILoadedPlugin> LoadPluginAsync(PluginManifest manifest, CancellationToken ct = default)
    {
        _logger?.LogInformation("Loading plugin: {PluginId} v{Version}", manifest.Id, manifest.Version);

        var entryPoint = manifest.EntryPoint.Dotnet;
        if (string.IsNullOrEmpty(entryPoint))
        {
            throw new InvalidOperationException($"Plugin {manifest.Id} has no .NET entry point");
        }

        if (!File.Exists(entryPoint))
        {
            throw new FileNotFoundException($"Plugin assembly not found: {entryPoint}");
        }

        try
        {
            // Create isolated, collectible ALC for hot-swap support
            var contextName = $"{manifest.Id}_v{manifest.Version}_{Guid.NewGuid():N}";
            var alc = new AssemblyLoadContext(contextName, isCollectible: true);

            // Configure dependency resolution to prefer plugin directory
            var entryFullPath = Path.GetFullPath(entryPoint);
            var pluginDir = Path.GetDirectoryName(entryFullPath)!;
            var resolver = new AssemblyDependencyResolver(entryFullPath);

            alc.Resolving += (ctx, name) =>
            {
                // Try resolver first (uses .deps.json if present)
                var resolvedPath = resolver.ResolveAssemblyToPath(name);
                if (!string.IsNullOrEmpty(resolvedPath) && File.Exists(resolvedPath))
                {
                    _logger?.LogDebug("Resolved {AssemblyName} via resolver: {Path}", name, resolvedPath);
                    return ctx.LoadFromAssemblyPath(resolvedPath);
                }

                // Fallback to same directory as plugin entry assembly
                var candidate = Path.Combine(pluginDir, name.Name + ".dll");
                if (File.Exists(candidate))
                {
                    _logger?.LogDebug("Resolved {AssemblyName} via plugin dir: {Path}", name, candidate);
                    return ctx.LoadFromAssemblyPath(candidate);
                }

                _logger?.LogDebug("Failed to resolve dependency {AssemblyName} for {PluginId}", name, manifest.Id);
                return null;
            };

            _logger?.LogDebug("Created load context: {ContextName}", contextName);

            // Load the plugin assembly
            var assembly = alc.LoadFromAssemblyPath(entryFullPath);

            _logger?.LogDebug("Loaded assembly: {AssemblyName} from {EntryPoint}", assembly.FullName, entryPoint);

            // Find IPluginActivator implementation
            var activatorType = assembly.GetTypes()
                .FirstOrDefault(t => typeof(IPluginActivator).IsAssignableFrom(t) && !t.IsInterface && !t.IsAbstract);

            if (activatorType == null)
            {
                alc.Unload(); // Clean up on failure
                throw new InvalidOperationException($"Plugin {manifest.Id} does not implement IPluginActivator");
            }

            _logger?.LogDebug("Found activator type: {ActivatorType}", activatorType.FullName);

            // Create activator instance
            var activator = (IPluginActivator)Activator.CreateInstance(activatorType)!;

            // Store load context for cleanup
            _loadContexts[manifest.Id] = alc;

            // Create loaded plugin wrapper
            var loadedPlugin = new LoadedPlugin(manifest, activator, alc, assembly);
            _loadedPlugins[manifest.Id] = loadedPlugin;

            _logger?.LogInformation("Successfully loaded plugin: {PluginId}", manifest.Id);

            return loadedPlugin;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to load plugin: {PluginId}", manifest.Id);
            throw;
        }
    }

    /// <summary>
    /// Unload a plugin and free its resources
    /// </summary>
    /// <param name="plugin">Plugin to unload</param>
    /// <param name="ct">Cancellation token</param>
    public async Task UnloadPluginAsync(ILoadedPlugin plugin, CancellationToken ct = default)
    {
        _logger?.LogInformation("Unloading plugin: {PluginId}", plugin.Id);

        try
        {
            // Deactivate first if needed
            if (plugin.State == PluginState.Activated)
            {
                await plugin.DeactivateAsync(ct);
            }

            // Quiesce period to allow in-flight operations to complete
            if (plugin.Manifest.QuiesceSeconds > 0)
            {
                _logger?.LogDebug("Quiescing plugin {PluginId} for {Seconds} seconds",
                    plugin.Id, plugin.Manifest.QuiesceSeconds);
                await Task.Delay(plugin.Manifest.QuiesceSeconds * 1000, ct);
            }

            // Unload ALC if it exists
            if (_loadContexts.TryGetValue(plugin.Id, out var alc))
            {
                _logger?.LogDebug("Unloading assembly load context for plugin: {PluginId}", plugin.Id);
                alc.Unload();
                _loadContexts.Remove(plugin.Id);
            }

            _loadedPlugins.Remove(plugin.Id);

            // Force GC to clean up plugin assemblies
            GC.Collect();
            GC.WaitForPendingFinalizers();

            _logger?.LogInformation("Successfully unloaded plugin: {PluginId}", plugin.Id);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to unload plugin: {PluginId}", plugin.Id);
            throw;
        }
    }

    /// <summary>
    /// Reload a plugin (unload + load)
    /// </summary>
    /// <param name="plugin">Plugin to reload</param>
    /// <param name="ct">Cancellation token</param>
    public async Task ReloadPluginAsync(ILoadedPlugin plugin, CancellationToken ct = default)
    {
        _logger?.LogInformation("Reloading plugin: {PluginId}", plugin.Id);

        var manifest = plugin.Manifest;

        // Unload the current plugin
        await UnloadPluginAsync(plugin, ct);

        // Load the plugin again
        await LoadPluginAsync(manifest, ct);

        _logger?.LogInformation("Successfully reloaded plugin: {PluginId}", plugin.Id);
    }

    /// <summary>
    /// Get all currently loaded plugins
    /// </summary>
    /// <returns>Collection of loaded plugins</returns>
    public IEnumerable<ILoadedPlugin> GetLoadedPlugins()
    {
        return _loadedPlugins.Values;
    }

    /// <summary>
    /// Dispose of all resources
    /// </summary>
    public void Dispose()
    {
        foreach (var plugin in _loadedPlugins.Values.ToList())
        {
            try
            {
                UnloadPluginAsync(plugin).GetAwaiter().GetResult();
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error disposing plugin: {PluginId}", plugin.Id);
            }
        }
    }
}

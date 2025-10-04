using System.Reflection;
using Microsoft.Extensions.Logging;
using WingedBean.Contracts.Core;
using WingedBean.Providers.AssemblyContext;

namespace WingedBean.PluginLoader;

/// <summary>
/// Tier 3 orchestration layer for plugin loading.
/// Delegates assembly loading to Tier 4 AssemblyContext provider.
/// Implements IPluginLoader from WingedBean.Contracts.Core for console profile.
/// </summary>
public class ActualPluginLoader : IPluginLoader
{
    private readonly AssemblyContextProvider _contextProvider;
    private readonly Dictionary<string, LoadedPluginWrapper> _loadedPlugins = new();
    private readonly ILogger<ActualPluginLoader>? _logger;
    private readonly object _lock = new();

    /// <summary>
    /// Initialize ActualPluginLoader with AssemblyContext provider.
    /// </summary>
    /// <param name="contextProvider">Tier 4 assembly context provider</param>
    public ActualPluginLoader(AssemblyContextProvider contextProvider)
    {
        _contextProvider = contextProvider ?? throw new ArgumentNullException(nameof(contextProvider));
    }

    /// <summary>
    /// Initialize ActualPluginLoader with AssemblyContext provider and logger.
    /// </summary>
    /// <param name="contextProvider">Tier 4 assembly context provider</param>
    /// <param name="logger">Logger instance</param>
    public ActualPluginLoader(AssemblyContextProvider contextProvider, ILogger<ActualPluginLoader> logger)
        : this(contextProvider)
    {
        _logger = logger;
    }

    /// <summary>
    /// Load a plugin from a specified path.
    /// </summary>
    /// <param name="pluginPath">Path to plugin assembly</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Loaded plugin handle</returns>
    public async Task<ILoadedPlugin> LoadAsync(string pluginPath, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(pluginPath);

        _logger?.LogInformation("Loading plugin from path: {PluginPath}", pluginPath);

        if (!File.Exists(pluginPath))
        {
            // Fallbacks for artifact layouts: try filename in common plugin locations
            var fileName = Path.GetFileName(pluginPath);
            var candidates = new List<string?>
            {
                Path.Combine("plugins", fileName),
                fileName,
                Path.Combine(Path.GetDirectoryName(pluginPath) ?? string.Empty, fileName)
            };

            string? resolved = candidates.FirstOrDefault(p => !string.IsNullOrWhiteSpace(p) && File.Exists(p));
            if (resolved != null)
            {
                _logger?.LogInformation("Resolved plugin path fallback: {Resolved}", resolved);
                pluginPath = resolved;
            }
            else
            {
                var hint = string.Join(", ", candidates.Where(c => !string.IsNullOrWhiteSpace(c)));
                throw new FileNotFoundException($"Plugin assembly not found: {pluginPath}. Tried: {hint}", pluginPath);
            }
        }

        // Create a simple manifest from the path
        var pluginId = Path.GetFileNameWithoutExtension(pluginPath);
        var fullPath = Path.GetFullPath(pluginPath);
        var manifest = new PluginManifest
        {
            Id = pluginId,
            Version = "1.0.0",
            LoadStrategy = LoadStrategy.Explicit,
            EntryPoints = new Dictionary<string, string> { { "assembly", fullPath } }
        };

        // Delegate to LoadAsync(PluginManifest)
        return await LoadAsync(manifest, cancellationToken);
    }

    /// <summary>
    /// Load a plugin with explicit metadata.
    /// </summary>
    /// <param name="manifest">Plugin manifest</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Loaded plugin handle</returns>
    public async Task<ILoadedPlugin> LoadAsync(PluginManifest manifest, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(manifest);
        ArgumentException.ThrowIfNullOrWhiteSpace(manifest.Id);

        _logger?.LogInformation("Loading plugin: {PluginId} v{Version}", manifest.Id, manifest.Version);

        lock (_lock)
        {
            if (_loadedPlugins.ContainsKey(manifest.Id))
            {
                throw new InvalidOperationException($"Plugin {manifest.Id} is already loaded");
            }
        }

        try
        {
            // Create isolated context for this plugin using Tier 4 provider
            var contextName = $"plugin_{manifest.Id}_{Guid.NewGuid():N}";
            _contextProvider.CreateContext(contextName, isCollectible: true);

            _logger?.LogDebug("Created assembly context: {ContextName}", contextName);

            // Determine plugin assembly path
            // Use path from EntryPoints if available, otherwise use Id as file name
            var assemblyPath = manifest.EntryPoints?.ContainsKey("assembly") == true
                ? manifest.EntryPoints["assembly"]
                : Path.GetFullPath($"{manifest.Id}.dll");

            // Load the plugin assembly using Tier 4 provider
            var assembly = _contextProvider.LoadAssembly(contextName, assemblyPath);

            _logger?.LogDebug("Loaded assembly: {AssemblyName}", assembly.FullName);

            // Find plugin implementation (IPlugin interface)
            var pluginType = assembly.GetTypes()
                .FirstOrDefault(t => typeof(IPlugin).IsAssignableFrom(t) && !t.IsInterface && !t.IsAbstract);

            IPlugin? plugin = null;
            if (pluginType != null)
            {
                _logger?.LogDebug("Found plugin type: {PluginType}", pluginType.FullName);
                plugin = (IPlugin?)Activator.CreateInstance(pluginType);
            }

            // Create loaded plugin wrapper
            var loadedPlugin = new LoadedPluginWrapper(manifest, assembly, contextName, plugin, _contextProvider);

            lock (_lock)
            {
                _loadedPlugins[manifest.Id] = loadedPlugin;
            }

            _logger?.LogInformation("Successfully loaded plugin: {PluginId}", manifest.Id);

            // Simulate async operation
            await Task.CompletedTask;

            return loadedPlugin;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to load plugin: {PluginId}", manifest.Id);
            throw;
        }
    }

    /// <summary>
    /// Unload a previously loaded plugin.
    /// </summary>
    /// <param name="plugin">Plugin to unload</param>
    /// <param name="cancellationToken">Cancellation token</param>
    public async Task UnloadAsync(ILoadedPlugin plugin, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(plugin);

        _logger?.LogInformation("Unloading plugin: {PluginId}", plugin.Id);

        try
        {
            // Deactivate plugin first if activated
            if (plugin.State == PluginState.Activated)
            {
                await plugin.DeactivateAsync(cancellationToken);
            }

            LoadedPluginWrapper? wrapper;
            lock (_lock)
            {
                if (!_loadedPlugins.TryGetValue(plugin.Id, out wrapper))
                {
                    _logger?.LogWarning("Plugin {PluginId} not found for unload", plugin.Id);
                    return;
                }

                _loadedPlugins.Remove(plugin.Id);
            }

            // Unload the assembly context using Tier 4 provider
            await _contextProvider.UnloadContextAsync(wrapper.ContextName, waitForUnload: true);

            _logger?.LogInformation("Successfully unloaded plugin: {PluginId}", plugin.Id);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to unload plugin: {PluginId}", plugin.Id);
            throw;
        }
    }

    /// <summary>
    /// Reload a plugin (unload + load).
    /// </summary>
    /// <param name="plugin">Plugin to reload</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Reloaded plugin handle</returns>
    public async Task<ILoadedPlugin> ReloadAsync(ILoadedPlugin plugin, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(plugin);

        _logger?.LogInformation("Reloading plugin: {PluginId}", plugin.Id);

        var manifest = plugin.Manifest;

        // Unload the current plugin
        await UnloadAsync(plugin, cancellationToken);

        // Load the plugin again with the same manifest
        var reloadedPlugin = await LoadAsync(manifest, cancellationToken);

        _logger?.LogInformation("Successfully reloaded plugin: {PluginId}", plugin.Id);

        return reloadedPlugin;
    }

    /// <summary>
    /// Get all currently loaded plugins.
    /// </summary>
    /// <returns>Collection of loaded plugins</returns>
    public IEnumerable<ILoadedPlugin> GetLoadedPlugins()
    {
        lock (_lock)
        {
            return _loadedPlugins.Values.ToList();
        }
    }

    /// <summary>
    /// Check if a plugin is loaded.
    /// </summary>
    /// <param name="pluginId">Plugin identifier</param>
    /// <returns>True if the plugin is loaded, false otherwise</returns>
    public bool IsLoaded(string pluginId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(pluginId);

        lock (_lock)
        {
            return _loadedPlugins.ContainsKey(pluginId);
        }
    }

    /// <summary>
    /// Get a loaded plugin by ID.
    /// </summary>
    /// <param name="pluginId">Plugin identifier</param>
    /// <returns>Loaded plugin instance, or null if not found</returns>
    public ILoadedPlugin? GetPlugin(string pluginId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(pluginId);

        lock (_lock)
        {
            return _loadedPlugins.TryGetValue(pluginId, out var plugin) ? plugin : null;
        }
    }
}

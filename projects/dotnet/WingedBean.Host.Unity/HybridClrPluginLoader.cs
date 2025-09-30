using Microsoft.Extensions.Logging;
using WingedBean.Host;
using System.Reflection;
using System.Collections.Concurrent;

#if UNITY
using UnityEngine;
using HybridCLR;
#endif

namespace WingedBean.Host.Unity;

/// <summary>
/// Plugin loader for Unity environment using HybridCLR for hot-reload capabilities
/// </summary>
public class HybridClrPluginLoader : IPluginLoader
{
    private readonly ILogger<HybridClrPluginLoader>? _logger;
    private readonly ConcurrentDictionary<string, LoadedUnityPlugin> _loadedPlugins = new();
    private readonly Dictionary<string, byte[]> _assemblyCache = new();

#if UNITY
    private readonly Dictionary<string, GameObject> _pluginGameObjects = new();
#endif

    /// <summary>
    /// Initialize HybridCLR plugin loader
    /// </summary>
    /// <param name="logger">Logger instance</param>
    public HybridClrPluginLoader(ILogger<HybridClrPluginLoader>? logger = null)
    {
        _logger = logger;
        _logger?.LogInformation("HybridCLR Plugin Loader initialized");
    }

    /// <summary>
    /// Load a plugin using HybridCLR hot-load capabilities
    /// </summary>
    /// <param name="manifest">Plugin manifest</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Loaded plugin instance</returns>
    public async Task<ILoadedPlugin> LoadPluginAsync(PluginManifest manifest, CancellationToken ct = default)
    {
        try
        {
            _logger?.LogInformation("Loading Unity plugin: {PluginId} v{Version}", manifest.Id, manifest.Version);

            // Get Unity-specific entry point
            var entryPoint = GetUnityEntryPoint(manifest);
            if (string.IsNullOrEmpty(entryPoint))
            {
                throw new InvalidOperationException($"Plugin {manifest.Id} has no Unity entry point");
            }

            // Resolve full path
            var pluginPath = Path.GetFullPath(entryPoint);
            if (!File.Exists(pluginPath))
            {
                throw new FileNotFoundException($"Plugin assembly not found: {pluginPath}");
            }

            // Load assembly using HybridCLR
            var assembly = await LoadAssemblyWithHybridClrAsync(pluginPath, manifest.Id, ct);

            // Find plugin activator
            var activatorType = FindPluginActivator(assembly, manifest.Id);
            var activator = CreatePluginActivator(activatorType);

            // Create Unity-specific plugin container
#if UNITY
            var pluginGameObject = CreatePluginGameObject(manifest);
            _pluginGameObjects[manifest.Id] = pluginGameObject;
#endif

            var loadedPlugin = new LoadedUnityPlugin(manifest, activator, assembly, this)
            {
#if UNITY
                GameObjectContainer = pluginGameObject
#endif
            };

            _loadedPlugins[manifest.Id] = loadedPlugin;

            _logger?.LogInformation("Successfully loaded Unity plugin: {PluginId}", manifest.Id);
            return loadedPlugin;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to load Unity plugin: {PluginId}", manifest.Id);
            throw;
        }
    }

    /// <summary>
    /// Unload a Unity plugin
    /// </summary>
    /// <param name="plugin">Plugin to unload</param>
    /// <param name="ct">Cancellation token</param>
    public async Task UnloadPluginAsync(ILoadedPlugin plugin, CancellationToken ct = default)
    {
        if (!_loadedPlugins.TryGetValue(plugin.Id, out var unityPlugin))
        {
            _logger?.LogWarning("Plugin not found for unloading: {PluginId}", plugin.Id);
            return;
        }

        try
        {
            _logger?.LogInformation("Unloading Unity plugin: {PluginId}", plugin.Id);

            // Deactivate plugin first
            if (unityPlugin.State == PluginState.Activated)
            {
                await unityPlugin.DeactivateAsync(ct);
            }

#if UNITY
            // Destroy Unity GameObject container
            if (_pluginGameObjects.TryGetValue(plugin.Id, out var gameObject))
            {
                if (gameObject != null)
                {
                    UnityEngine.Object.DestroyImmediate(gameObject);
                }
                _pluginGameObjects.Remove(plugin.Id);
            }
#endif

            // Note: HybridCLR doesn't support assembly unloading like ALC
            // So we mark as unloaded but assemblies remain in memory
            unityPlugin.SetState(PluginState.Unloaded);

            _loadedPlugins.TryRemove(plugin.Id, out _);
            _assemblyCache.Remove(plugin.Id);

            _logger?.LogInformation("Successfully unloaded Unity plugin: {PluginId}", plugin.Id);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to unload Unity plugin: {PluginId}", plugin.Id);
            throw;
        }
    }

    /// <summary>
    /// Reload a Unity plugin (hot-reload)
    /// </summary>
    /// <param name="plugin">Plugin to reload</param>
    /// <param name="ct">Cancellation token</param>
    public async Task ReloadPluginAsync(ILoadedPlugin plugin, CancellationToken ct = default)
    {
        _logger?.LogInformation("Hot-reloading Unity plugin: {PluginId}", plugin.Id);

        try
        {
            // Store current state for restoration
            var wasActivated = plugin.State == PluginState.Activated;

            // Deactivate current plugin
            if (wasActivated)
            {
                await plugin.DeactivateAsync(ct);
            }

            // Load new version (HybridCLR will handle the hot-reload)
            var newPlugin = await LoadPluginAsync(plugin.Manifest, ct);

            // Activate if it was previously activated
            if (wasActivated &amp;&amp; newPlugin is LoadedUnityPlugin unityPlugin)
            {
                // Re-activate with preserved state if possible
                await unityPlugin.ActivateAsync(null, ct); // Host services will be injected
            }

            _logger?.LogInformation("Successfully hot-reloaded Unity plugin: {PluginId}", plugin.Id);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to hot-reload Unity plugin: {PluginId}", plugin.Id);
            throw;
        }
    }

    /// <summary>
    /// Get Unity-specific entry point from manifest
    /// </summary>
    private static string? GetUnityEntryPoint(PluginManifest manifest)
    {
        // Try Unity-specific entry point first
        if (!string.IsNullOrEmpty(manifest.EntryPoint.Unity))
        {
            return manifest.EntryPoint.Unity;
        }

        // Fall back to .NET entry point if Unity-specific not specified
        return manifest.EntryPoint.Dotnet;
    }

    /// <summary>
    /// Load assembly using HybridCLR for hot-reload support
    /// </summary>
    private async Task<Assembly> LoadAssemblyWithHybridClrAsync(string assemblyPath, string pluginId, CancellationToken ct)
    {
#if UNITY
        try
        {
            // Read assembly bytes
            var assemblyBytes = await File.ReadAllBytesAsync(assemblyPath, ct);
            
            // Cache for potential reload
            _assemblyCache[pluginId] = assemblyBytes;

            // Load metadata for AOT assembly (required for HybridCLR)
            HybridCLR.RuntimeApi.LoadMetadataForAOTAssembly(assemblyBytes, HybridCLR.HomologousImageMode.SuperSet);

            // Load the assembly
            var assembly = Assembly.Load(assemblyBytes);
            
            _logger?.LogDebug("Loaded assembly via HybridCLR: {AssemblyName}", assembly.FullName);
            return assembly;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "HybridCLR assembly loading failed for: {AssemblyPath}", assemblyPath);
            throw;
        }
#else
        // Fallback for non-Unity environments (testing/development)
        _logger?.LogWarning("HybridCLR not available, using standard Assembly.LoadFrom");
        return Assembly.LoadFrom(assemblyPath);
#endif
    }

    /// <summary>
    /// Find plugin activator type in assembly
    /// </summary>
    private Type FindPluginActivator(Assembly assembly, string pluginId)
    {
        var activatorType = assembly.GetTypes()
            .FirstOrDefault(t =&gt; typeof(IPluginActivator).IsAssignableFrom(t) &amp;&amp; !t.IsInterface &amp;&amp; !t.IsAbstract);

        if (activatorType == null)
        {
            throw new InvalidOperationException($"Plugin {pluginId} does not implement IPluginActivator");
        }

        _logger?.LogDebug("Found plugin activator: {ActivatorType}", activatorType.FullName);
        return activatorType;
    }

    /// <summary>
    /// Create plugin activator instance
    /// </summary>
    private IPluginActivator CreatePluginActivator(Type activatorType)
    {
        try
        {
            var activator = (IPluginActivator)Activator.CreateInstance(activatorType)!;
            return activator;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to create plugin activator instance: {ActivatorType}", activatorType.FullName);
            throw;
        }
    }

#if UNITY
    /// <summary>
    /// Create Unity GameObject container for plugin
    /// </summary>
    private GameObject CreatePluginGameObject(PluginManifest manifest)
    {
        var pluginGameObject = new GameObject($"Plugin_{manifest.Id}")
        {
            hideFlags = HideFlags.DontSave // Don't save with scene
        };

        // Make it persistent across scene loads if needed
        if (manifest.Unity?.PersistAcrossScenes == true)
        {
            UnityEngine.Object.DontDestroyOnLoad(pluginGameObject);
        }

        _logger?.LogDebug("Created GameObject container for plugin: {PluginId}", manifest.Id);
        return pluginGameObject;
    }
#endif

    /// <summary>
    /// Get currently loaded plugins
    /// </summary>
    public IReadOnlyDictionary<string, ILoadedPlugin> LoadedPlugins => 
        _loadedPlugins.ToDictionary(kvp => kvp.Key, kvp => (ILoadedPlugin)kvp.Value);

    /// <summary>
    /// Dispose resources
    /// </summary>
    public void Dispose()
    {
        _logger?.LogInformation("Disposing HybridCLR Plugin Loader");

#if UNITY
        // Clean up all plugin GameObjects
        foreach (var gameObject in _pluginGameObjects.Values)
        {
            if (gameObject != null)
            {
                UnityEngine.Object.DestroyImmediate(gameObject);
            }
        }
        _pluginGameObjects.Clear();
#endif

        _loadedPlugins.Clear();
        _assemblyCache.Clear();
    }
}

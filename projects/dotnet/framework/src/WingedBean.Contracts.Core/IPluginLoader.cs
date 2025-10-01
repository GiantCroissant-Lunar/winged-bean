using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace WingedBean.Contracts.Core;

/// <summary>
/// Orchestrates plugin loading, unloading, and lifecycle management.
/// Foundation service - manually instantiated at bootstrap, delegates to platform-specific providers.
/// </summary>
public interface IPluginLoader
{
    /// <summary>
    /// Load a plugin from a specified path.
    /// </summary>
    /// <param name="pluginPath">Path to plugin assembly</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Loaded plugin handle</returns>
    Task<ILoadedPlugin> LoadAsync(string pluginPath, CancellationToken cancellationToken = default);

    /// <summary>
    /// Load a plugin with explicit metadata.
    /// </summary>
    Task<ILoadedPlugin> LoadAsync(PluginManifest manifest, CancellationToken cancellationToken = default);

    /// <summary>
    /// Unload a previously loaded plugin.
    /// </summary>
    /// <param name="plugin">Plugin to unload</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task UnloadAsync(ILoadedPlugin plugin, CancellationToken cancellationToken = default);

    /// <summary>
    /// Reload a plugin (unload + load).
    /// </summary>
    Task<ILoadedPlugin> ReloadAsync(ILoadedPlugin plugin, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get all currently loaded plugins.
    /// </summary>
    IEnumerable<ILoadedPlugin> GetLoadedPlugins();

    /// <summary>
    /// Check if a plugin is loaded.
    /// </summary>
    bool IsLoaded(string pluginId);

    /// <summary>
    /// Get a loaded plugin by ID.
    /// </summary>
    ILoadedPlugin? GetPlugin(string pluginId);
}

/// <summary>
/// Represents a loaded plugin with lifecycle management.
/// </summary>
public interface ILoadedPlugin
{
    /// <summary>
    /// Unique plugin identifier.
    /// </summary>
    string Id { get; }

    /// <summary>
    /// Plugin version.
    /// </summary>
    string Version { get; }

    /// <summary>
    /// Plugin manifest metadata.
    /// </summary>
    PluginManifest Manifest { get; }

    /// <summary>
    /// Current state of the plugin.
    /// </summary>
    PluginState State { get; }

    /// <summary>
    /// Get a service provided by this plugin.
    /// </summary>
    TService? GetService<TService>() where TService : class;

    /// <summary>
    /// Get all services provided by this plugin.
    /// </summary>
    IEnumerable<object> GetServices();

    /// <summary>
    /// Activate the plugin (register services, initialize resources).
    /// </summary>
    Task ActivateAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Deactivate the plugin (cleanup, prepare for unload).
    /// </summary>
    Task DeactivateAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Plugin lifecycle states.
/// </summary>
public enum PluginState
{
    Discovered,
    Loading,
    Loaded,
    Activating,
    Activated,
    Deactivating,
    Deactivated,
    Unloading,
    Unloaded,
    Failed
}

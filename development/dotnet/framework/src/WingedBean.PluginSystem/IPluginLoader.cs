namespace WingedBean.PluginSystem;

/// <summary>
/// Profile-agnostic plugin loading abstraction
/// </summary>
public interface IPluginLoader
{
    /// <summary>
    /// Load a plugin from its manifest
    /// </summary>
    /// <param name="manifest">Plugin manifest</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Loaded plugin instance</returns>
    Task<ILoadedPlugin> LoadPluginAsync(PluginManifest manifest, CancellationToken ct = default);

    /// <summary>
    /// Unload a plugin and free its resources
    /// </summary>
    /// <param name="plugin">Plugin to unload</param>
    /// <param name="ct">Cancellation token</param>
    Task UnloadPluginAsync(ILoadedPlugin plugin, CancellationToken ct = default);

    /// <summary>
    /// Reload a plugin (unload + load)
    /// </summary>
    /// <param name="plugin">Plugin to reload</param>
    /// <param name="ct">Cancellation token</param>
    Task ReloadPluginAsync(ILoadedPlugin plugin, CancellationToken ct = default);
}

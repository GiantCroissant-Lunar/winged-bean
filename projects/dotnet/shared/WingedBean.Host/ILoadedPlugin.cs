using Microsoft.Extensions.DependencyInjection;

namespace WingedBean.Host;

/// <summary>
/// Represents a loaded plugin instance
/// </summary>
public interface ILoadedPlugin
{
    /// <summary>Plugin unique identifier</summary>
    string Id { get; }

    /// <summary>Plugin version</summary>
    Version Version { get; }

    /// <summary>Plugin manifest metadata</summary>
    PluginManifest Manifest { get; }

    /// <summary>Current plugin state</summary>
    PluginState State { get; }

    /// <summary>Services registered by this plugin</summary>
    IServiceCollection Services { get; }

    /// <summary>
    /// Activate the plugin and register its services
    /// </summary>
    /// <param name="hostServices">Host services available to the plugin</param>
    /// <param name="ct">Cancellation token</param>
    Task ActivateAsync(IServiceProvider hostServices, CancellationToken ct = default);

    /// <summary>
    /// Deactivate the plugin and clean up resources
    /// </summary>
    /// <param name="ct">Cancellation token</param>
    Task DeactivateAsync(CancellationToken ct = default);
}

using Microsoft.Extensions.DependencyInjection;

namespace WingedBean.PluginSystem;

/// <summary>
/// Interface that every plugin must implement to register its services
/// </summary>
public interface IPluginActivator
{
    /// <summary>
    /// Called when the plugin is activated. Register your services here.
    /// </summary>
    /// <param name="services">Service collection to register services</param>
    /// <param name="hostServices">Host services that can be injected</param>
    /// <param name="ct">Cancellation token</param>
    Task ActivateAsync(IServiceCollection services, IServiceProvider hostServices, CancellationToken ct = default);

    /// <summary>
    /// Called when the plugin is being deactivated. Clean up resources here.
    /// </summary>
    /// <param name="ct">Cancellation token</param>
    Task DeactivateAsync(CancellationToken ct = default);
}

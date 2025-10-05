using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using WingedBean.Contracts.Resilience;
using WingedBean.PluginSystem;

namespace WingedBean.Plugins.Resilience;

/// <summary>
/// Plugin activator for Resilience service.
/// Registers IResilienceService backed by PollyResilienceService for ALC discovery path.
/// </summary>
public class ResiliencePluginActivator : IPluginActivator
{
    public Task ActivateAsync(IServiceCollection services, IServiceProvider hostServices, CancellationToken ct = default)
    {
        var logger = hostServices.GetService<ILogger<ResiliencePluginActivator>>();
        logger?.LogInformation("Registering IResilienceService -> PollyResilienceService");

        services.AddSingleton<IResilienceService, PollyResilienceService>();
        return Task.CompletedTask;
    }

    public Task DeactivateAsync(CancellationToken ct = default)
    {
        return Task.CompletedTask;
    }
}

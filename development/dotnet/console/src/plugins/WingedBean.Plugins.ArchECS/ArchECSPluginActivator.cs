using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using WingedBean.Contracts.ECS;
using WingedBean.PluginSystem;

namespace WingedBean.Plugins.ArchECS;

/// <summary>
/// Plugin activator for Arch ECS service.
/// Registers IECSService backed by ArchECSService for ALC discovery path.
/// </summary>
public class ArchECSPluginActivator : IPluginActivator
{
    public Task ActivateAsync(IServiceCollection services, IServiceProvider hostServices, CancellationToken ct = default)
    {
        var logger = hostServices.GetService<ILogger<ArchECSPluginActivator>>();
        logger?.LogInformation("Registering IECSService -> ArchECSService");

        services.AddSingleton<IECSService, ArchECSService>();
        return Task.CompletedTask;
    }

    public Task DeactivateAsync(CancellationToken ct = default)
    {
        return Task.CompletedTask;
    }
}

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Plate.CrossMilo.Contracts.ECS;
using Plate.CrossMilo.Contracts.ECS.Services;
using Plate.PluginManoi.Core;

namespace WingedBean.Plugins.ArchECS;

/// <summary>
/// Plugin activator for Arch ECS service.
/// Registers IService backed by ArchECSService for ALC discovery path.
/// </summary>
public class ArchECSPluginActivator : IPluginActivator
{
    public Task ActivateAsync(IServiceCollection services, IServiceProvider hostServices, CancellationToken ct = default)
    {
        var logger = hostServices.GetService<ILogger<ArchECSPluginActivator>>();
        logger?.LogInformation("Registering IService -> ArchECSService");

        services.AddSingleton<IService, ArchECSService>();
        return Task.CompletedTask;
    }

    public Task DeactivateAsync(CancellationToken ct = default)
    {
        return Task.CompletedTask;
    }
}

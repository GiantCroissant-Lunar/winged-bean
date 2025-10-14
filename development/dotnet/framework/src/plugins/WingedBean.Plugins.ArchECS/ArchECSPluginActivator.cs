using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
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
        logger?.LogInformation("Registering ArchECSService (internal ECS implementation)");

        // ECS service is now internal to the plugin, used by DungeonGame
        services.AddSingleton<ArchECSService>();
        return Task.CompletedTask;
    }

    public Task DeactivateAsync(CancellationToken ct = default)
    {
        return Task.CompletedTask;
    }
}

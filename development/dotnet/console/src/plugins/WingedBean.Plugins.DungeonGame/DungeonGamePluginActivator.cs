using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using WingedBean.Contracts.Game;
using WingedBean.PluginSystem;

namespace WingedBean.Plugins.DungeonGame;

/// <summary>
/// Plugin activator for Dungeon Game logic.
/// Registers IDungeonGameService backed by DungeonGameService when loaded via ALC discovery.
/// </summary>
public class DungeonGamePluginActivator : IPluginActivator
{
    public Task ActivateAsync(IServiceCollection services, IServiceProvider hostServices, CancellationToken ct = default)
    {
        var logger = hostServices.GetService<ILogger<DungeonGamePluginActivator>>();
        logger?.LogInformation("Registering IDungeonGameService -> DungeonGameService");

        services.AddSingleton<IDungeonGameService, DungeonGameService>();
        return Task.CompletedTask;
    }

    public Task DeactivateAsync(CancellationToken ct = default)
    {
        return Task.CompletedTask;
    }
}

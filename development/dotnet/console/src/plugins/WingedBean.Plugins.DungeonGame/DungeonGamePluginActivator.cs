using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Plate.CrossMilo.Contracts.Game.Dungeon;
using Plate.PluginManoi.Core;

namespace WingedBean.Plugins.DungeonGame;

/// <summary>
/// Plugin activator for Dungeon Game logic.
/// Registers IService (Game.Dungeon) backed by DungeonGameService when loaded via ALC discovery.
/// NOTE: The DungeonGamePlugin bridge class is now the primary registration method for manifest-based loading.
/// </summary>
public class DungeonGamePluginActivator : IPluginActivator
{
    public Task ActivateAsync(IServiceCollection services, IServiceProvider hostServices, CancellationToken ct = default)
    {
        var logger = hostServices.GetService<ILogger<DungeonGamePluginActivator>>();
        logger?.LogInformation("Registering IService (Game.Dungeon) -> DungeonGameService");

        services.AddSingleton<IService, DungeonGameService>();
        return Task.CompletedTask;
    }

    public Task DeactivateAsync(CancellationToken ct = default)
    {
        return Task.CompletedTask;
    }
}

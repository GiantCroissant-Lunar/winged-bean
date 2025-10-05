using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using WingedBean.Contracts.Terminal;
using WingedBean.PluginSystem;

namespace WingedBean.Plugins.ConsoleDungeon;

/// <summary>
/// Plugin activator that registers the Console Dungeon Terminal.Gui application
/// </summary>
public class ConsoleDungeonActivator : IPluginActivator
{
    public Task ActivateAsync(IServiceCollection services, IServiceProvider hostServices, CancellationToken ct = default)
    {
        var logger = hostServices.GetService<ILogger<ConsoleDungeonActivator>>();
        logger?.LogInformation("Registering ITerminalApp -> ConsoleDungeonAppRefactored (RFC-0020/0021)");

        services.AddSingleton<ITerminalApp, ConsoleDungeonAppRefactored>();
        return Task.CompletedTask;
    }

    public Task DeactivateAsync(CancellationToken ct = default)
    {
        // Nothing to clean up for this simple service
        return Task.CompletedTask;
    }
}

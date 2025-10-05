using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using WingedBean.Contracts.TerminalUI;
using WingedBean.PluginSystem;

namespace WingedBean.Plugins.TerminalUI;

/// <summary>
/// Plugin activator for Terminal UI service.
/// Registers ITerminalUIService backed by TerminalGuiService for ALC discovery path.
/// </summary>
public class TerminalUIPluginActivator : IPluginActivator
{
    public Task ActivateAsync(IServiceCollection services, IServiceProvider hostServices, CancellationToken ct = default)
    {
        var logger = hostServices.GetService<ILogger<TerminalUIPluginActivator>>();
        logger?.LogInformation("Registering ITerminalUIService -> TerminalGuiService");

        services.AddSingleton<ITerminalUIService, TerminalGuiService>();
        return Task.CompletedTask;
    }

    public Task DeactivateAsync(CancellationToken ct = default)
    {
        return Task.CompletedTask;
    }
}

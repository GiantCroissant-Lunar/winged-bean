using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using WingedBean.Contracts.Config;
using WingedBean.PluginSystem;

namespace WingedBean.Plugins.Config;

/// <summary>
/// Plugin activator for configuration services.
/// Registers IConfigService backed by ConfigService into DI when loaded via ALC discovery.
/// </summary>
public class ConfigPluginActivator : IPluginActivator
{
    public Task ActivateAsync(IServiceCollection services, IServiceProvider hostServices, CancellationToken ct = default)
    {
        var logger = hostServices.GetService<ILogger<ConfigPluginActivator>>();
        logger?.LogInformation("Registering IConfigService -> ConfigService");

        services.AddSingleton<IConfigService, ConfigService>();
        return Task.CompletedTask;
    }

    public Task DeactivateAsync(CancellationToken ct = default)
    {
        return Task.CompletedTask;
    }
}

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using WingedBean.Contracts;
using WingedBean.Host;

namespace WingedBean.Plugins.PtyService;

/// <summary>
/// Plugin activator that registers the PTY service implementation
/// </summary>
public class PtyServiceActivator : IPluginActivator
{
    public Task ActivateAsync(IServiceCollection services, IServiceProvider hostServices, CancellationToken ct = default)
    {
        var logger = hostServices.GetService<ILogger<PtyServiceActivator>>();
        logger?.LogInformation("Registering IPtyService -> NodePtyService");

        services.AddSingleton<IPtyService, NodePtyService>();
        return Task.CompletedTask;
    }

    public Task DeactivateAsync(CancellationToken ct = default)
    {
        // Nothing to clean up for this simple service
        return Task.CompletedTask;
    }
}

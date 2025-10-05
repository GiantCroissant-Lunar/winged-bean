using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using WingedBean.Contracts.Resource;
using WingedBean.PluginSystem;

namespace WingedBean.Plugins.Resource;

/// <summary>
/// Plugin activator for the Resource service.
/// Registers IResourceService backed by FileSystemResourceService into DI when loaded via ALC discovery.
/// </summary>
public class ResourcePluginActivator : IPluginActivator
{
    public Task ActivateAsync(IServiceCollection services, IServiceProvider hostServices, CancellationToken ct = default)
    {
        var logger = hostServices.GetService<ILogger<ResourcePluginActivator>>();
        logger?.LogInformation("Registering IResourceService -> FileSystemResourceService");

        services.AddSingleton<IResourceService>(sp =>
        {
            var serviceLogger = sp.GetRequiredService<ILogger<FileSystemResourceService>>();
            
            // TODO: Read base path from configuration if available
            // For now, use default (AppContext.BaseDirectory/resources)
            return new FileSystemResourceService(serviceLogger);
        });

        return Task.CompletedTask;
    }

    public Task DeactivateAsync(CancellationToken ct = default)
    {
        return Task.CompletedTask;
    }
}

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Plate.CrossMilo.Contracts.Resource;
using Plate.CrossMilo.Contracts.Resource.Services;
using Plate.PluginManoi.Core;
// Type alias for IService (IResourceService)
using IResourceService = Plate.CrossMilo.Contracts.Resource.Services.IService;

namespace WingedBean.Plugins.Resource;

/// <summary>
/// Plugin activator for the Resource service.
/// Registers IResourceService (backed by FileSystemResourceService with ResourceManager) into DI when loaded via ALC discovery.
/// RFC-0054 Phase 2: Now also registers ResourceManager for direct access.
/// </summary>
public class ResourcePluginActivator : IPluginActivator
{
    public Task ActivateAsync(IServiceCollection services, IServiceProvider hostServices, CancellationToken ct = default)
    {
        var logger = hostServices.GetService<ILogger<ResourcePluginActivator>>();
        logger?.LogInformation("Registering Resource services (RFC-0054 Phase 2)");

        // Register FileSystemResourceService as singleton
        services.AddSingleton<FileSystemResourceService>(sp =>
        {
            var serviceLogger = sp.GetRequiredService<ILogger<FileSystemResourceService>>();

            // TODO: Read configuration for:
            // - Base path from configuration
            // - ResourceManagerOptions from configuration
            // For now, use defaults

            var options = new ResourceManagerOptions
            {
                // Production-ready defaults
                CleanupInterval = TimeSpan.FromSeconds(30),
                IdleTimeout = TimeSpan.FromMinutes(5),
                EnableAutoCleanup = true,
                EnableDebugLogging = false
            };

            return new FileSystemResourceService(
                serviceLogger,
                basePath: null, // Default: AppContext.BaseDirectory/resources
                customProviders: null, // No custom providers by default
                options: options
            );
        });

        // Register IResourceService as factory that returns FileSystemResourceService
        services.AddSingleton<IResourceService>(sp =>
            sp.GetRequiredService<FileSystemResourceService>()
        );

        // Register ResourceManager for direct access (advanced scenarios)
        // This allows consumers to use the new handle-based API
        services.AddSingleton<ResourceManager>(sp =>
        {
            var fileSystemService = sp.GetRequiredService<FileSystemResourceService>();
            return fileSystemService.Manager;
        });

        logger?.LogInformation("Registered IResourceService (FileSystemResourceService with ResourceManager)");
        logger?.LogInformation("Registered ResourceManager for advanced scenarios (RFC-0054)");

        return Task.CompletedTask;
    }

    public Task DeactivateAsync(CancellationToken ct = default)
    {
        return Task.CompletedTask;
    }
}

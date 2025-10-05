using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using WingedBean.Contracts.Analytics;
using WingedBean.PluginSystem;

namespace WingedBean.Plugins.Analytics;

/// <summary>
/// Plugin activator for Analytics service
/// </summary>
public class AnalyticsPluginActivator : IPluginActivator
{
    public Task ActivateAsync(IServiceCollection services, IServiceProvider hostServices, CancellationToken ct = default)
    {
        var logger = hostServices.GetService<ILogger<AnalyticsPluginActivator>>();
        logger?.LogInformation("Activating Analytics plugin...");

        // Register analytics configuration (could be loaded from config)
        services.AddSingleton(new AnalyticsConfig
        {
            Enabled = true,
            Backend = AnalyticsBackend.InMemory,
            ScrubPii = true,
            RetentionDays = 90,
            BatchSize = 10,
            FlushIntervalSeconds = 30,
            TrackAnonymous = true,
            EnableBreadcrumbs = true,
            MaxBreadcrumbs = 100
        });

        // Register backend
        services.AddSingleton<IAnalyticsBackend, InMemoryAnalyticsBackend>();

        // Register the analytics service
        services.AddSingleton<IAnalyticsService, AnalyticsService>();

        logger?.LogInformation("Analytics plugin activated successfully");
        return Task.CompletedTask;
    }

    public Task DeactivateAsync(CancellationToken ct = default)
    {
        // Nothing special to clean up for analytics
        return Task.CompletedTask;
    }
}

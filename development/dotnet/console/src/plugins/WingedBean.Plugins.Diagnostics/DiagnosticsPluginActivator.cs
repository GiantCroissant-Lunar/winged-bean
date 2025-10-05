using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using WingedBean.Contracts.Diagnostics;
using WingedBean.PluginSystem;

namespace WingedBean.Plugins.Diagnostics;

/// <summary>
/// Plugin activator for Diagnostics service
/// </summary>
public class DiagnosticsPluginActivator : IPluginActivator
{
    public Task ActivateAsync(IServiceCollection services, IServiceProvider hostServices, CancellationToken ct = default)
    {
        var logger = hostServices.GetService<ILogger<DiagnosticsPluginActivator>>();
        logger?.LogInformation("Activating Diagnostics plugin...");

        // Register diagnostics configuration
        services.AddSingleton(new DiagnosticsConfig
        {
            Enabled = true,
            Backend = DiagnosticsBackend.InMemory,
            SamplingRate = 1.0,
            MaxBreadcrumbs = 100,
            HealthCheckIntervalSeconds = 60,
            AlertEvaluationIntervalSeconds = 30,
            RetentionDays = 7,
            FlushIntervalSeconds = 30,
            CaptureThreadDumps = true,
            CaptureHeapDumps = false,
            MaxSnapshots = 50
        });

        // Register backend
        services.AddSingleton<IDiagnosticsBackend, InMemoryDiagnosticsBackend>();

        // Register the diagnostics service
        services.AddSingleton<IDiagnosticsService, DiagnosticsService>();

        logger?.LogInformation("Diagnostics plugin activated successfully");
        return Task.CompletedTask;
    }

    public Task DeactivateAsync(CancellationToken ct = default)
    {
        // Nothing special to clean up for diagnostics
        return Task.CompletedTask;
    }
}

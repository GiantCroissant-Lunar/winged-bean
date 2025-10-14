using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Plate.PluginManoi.Contracts;

namespace WingedBean.Plugins.Diagnostics.Sentry;

/// <summary>
/// Plugin activator for Sentry diagnostics provider (Tier 4).
/// Registers the Sentry backend with the diagnostics service when loaded.
/// </summary>
public class SentryDiagnosticsPluginActivator : IPluginActivator
{
    public string Name => "SentryDiagnostics";
    public string Version => "1.0.0";
    public string Description => "Sentry diagnostics backend provider for error tracking and performance monitoring";

    public void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        // Register the Sentry backend as a singleton
        services.AddSingleton<SentryDiagnosticsBackend>(sp =>
        {
            var logger = sp.GetRequiredService<ILogger<SentryDiagnosticsBackend>>();
            var dsn = configuration["Diagnostics:Sentry:Dsn"];
            var environment = configuration["Diagnostics:Sentry:Environment"] ?? "production";

            if (string.IsNullOrEmpty(dsn))
            {
                logger.LogWarning("Sentry DSN not configured. Sentry diagnostics will not be initialized.");
                throw new InvalidOperationException("Sentry DSN is required but not configured.");
            }

            return new SentryDiagnosticsBackend(logger, dsn, environment);
        });

        // Register as IDiagnosticsBackend for DI
        services.AddSingleton<WingedBean.Plugins.Diagnostics.IDiagnosticsBackend>(sp =>
            sp.GetRequiredService<SentryDiagnosticsBackend>());
    }

    public void Initialize(IServiceProvider serviceProvider)
    {
        var logger = serviceProvider.GetRequiredService<ILogger<SentryDiagnosticsPluginActivator>>();
        var configuration = serviceProvider.GetRequiredService<IConfiguration>();

        // Check if Sentry is enabled
        var enabled = configuration.GetValue<bool>("Diagnostics:Sentry:Enabled", true);
        if (!enabled)
        {
            logger.LogInformation("Sentry diagnostics provider is disabled in configuration");
            return;
        }

        try
        {
            // Get the diagnostics service and register the backend
            var diagnosticsService = serviceProvider.GetService<WingedBean.Plugins.Diagnostics.IDiagnosticsService>();
            if (diagnosticsService != null)
            {
                var backend = serviceProvider.GetRequiredService<SentryDiagnosticsBackend>();
                diagnosticsService.RegisterBackend("sentry", backend);
                logger.LogInformation("Sentry diagnostics provider registered successfully");
            }
            else
            {
                logger.LogWarning("IDiagnosticsService not found. Sentry backend cannot be registered.");
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to initialize Sentry diagnostics provider");
            throw;
        }
    }

    public void Shutdown()
    {
        // Cleanup if needed - Sentry SDK handles its own cleanup
    }
}

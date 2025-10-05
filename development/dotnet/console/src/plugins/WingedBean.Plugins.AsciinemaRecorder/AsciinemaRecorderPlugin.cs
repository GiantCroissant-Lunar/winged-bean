using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using WingedBean.Contracts.Recorder;
using WingedBean.PluginSystem;

namespace WingedBean.Plugins.AsciinemaRecorder;

/// <summary>
/// Plugin activator for AsciinemaRecorder
/// </summary>
public class AsciinemaRecorderPlugin : IPluginActivator
{
    public Task ActivateAsync(IServiceCollection services, IServiceProvider hostServices, CancellationToken ct = default)
    {
        // Register the AsciinemaRecorder as the IRecorder implementation
        services.AddTransient<IRecorder, AsciinemaRecorder>();

        // Log activation
        var logger = hostServices.GetService<ILogger<AsciinemaRecorderPlugin>>();
        logger?.LogInformation("AsciinemaRecorder plugin activated");

        return Task.CompletedTask;
    }

    public Task DeactivateAsync(CancellationToken ct = default)
    {
        // No cleanup needed for this plugin
        return Task.CompletedTask;
    }
}

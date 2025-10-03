using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using WingedBean.Contracts;
using WingedBean.Contracts.Audio;
using WingedBean.Host;

namespace WingedBean.Plugins.Audio;

/// <summary>
/// Plugin activator for Audio services
/// </summary>
public class AudioPlugin : IPluginActivator
{
    public Task ActivateAsync(IServiceCollection services, IServiceProvider hostServices, CancellationToken ct = default)
    {
        // Register LibVlcAudioService as the IAudioService implementation
        services.AddSingleton<IAudioService, LibVlcAudioService>();

        // Log activation
        var logger = hostServices.GetService<ILogger<AudioPlugin>>();
        logger?.LogInformation("Audio plugin activated (LibVLC)");

        return Task.CompletedTask;
    }

    public Task DeactivateAsync(CancellationToken ct = default)
    {
        // LibVlcAudioService implements IDisposable and will be disposed by DI container
        return Task.CompletedTask;
    }
}

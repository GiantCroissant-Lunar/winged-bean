using Microsoft.Extensions.Logging;
using Plate.PluginManoi.Contracts;
using IService = Plate.CrossMilo.Contracts.Audio.Services.IService;

namespace WingedBean.Plugins.Audio;

/// <summary>
/// Registers the Audio service implementation with the runtime registry.
/// </summary>
public class Plugin : IPlugin
{
    private LibVlcAudioService? _service;
    private ILogger<Plugin>? _logger;

    public string Id => "wingedbean.plugins.audio";
    public string Version => "1.0.0";

    public Task OnActivateAsync(IRegistry registry, CancellationToken ct = default)
    {
        // Try to get logger from registry
        _logger = registry.GetAll<ILogger<Plugin>>().FirstOrDefault();

        var serviceLogger = registry.GetAll<ILogger<LibVlcAudioService>>().FirstOrDefault()
                           ?? Microsoft.Extensions.Logging.Abstractions.NullLogger<LibVlcAudioService>.Instance;

        _service = new LibVlcAudioService(serviceLogger);
        registry.Register<IService>(_service, priority: 50);

        _logger?.LogInformation("========================================");
        _logger?.LogInformation("✓ AUDIO SERVICE REGISTERED");
        _logger?.LogInformation("========================================");

        return Task.CompletedTask;
    }

    public Task OnDeactivateAsync(CancellationToken ct = default)
    {
        _service?.Dispose();
        _service = null;
        return Task.CompletedTask;
    }

    public IEnumerable<object> GetServices()
    {
        if (_service != null)
            yield return _service;
    }
}

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Plate.PluginManoi.Contracts;
using Plate.CrossMilo.Contracts.Audio.Services;

namespace WingedBean.Plugins.Audio;

/// <summary>
/// Legacy plugin interface implementation for backward compatibility with the current host.
/// This bridges the gap between the new IPluginActivator pattern and the old IPlugin pattern.
/// </summary>
public class AudioPlugin : IPlugin
{
    private LibVlcAudioService? _serviceInstance;

    public string Id => "wingedbean.plugins.audio";
    public string Version => "1.0.0";

    public Task OnActivateAsync(IRegistry registry, CancellationToken ct = default)
    {
        // Try to get ILoggerFactory from registry, otherwise use NullLoggerFactory
        ILoggerFactory? loggerFactory = null;
        try
        {
            loggerFactory = registry.Get<ILoggerFactory>();
        }
        catch
        {
            // Fall back to null logger if not available
        }

        var logger = loggerFactory?.CreateLogger<LibVlcAudioService>() 
                     ?? NullLogger<LibVlcAudioService>.Instance;

        // Create the audio service instance
        _serviceInstance = new LibVlcAudioService(logger);
        
        // Register the service directly with the registry (priority 50 from manifest)
        registry.Register<IService>(_serviceInstance, priority: 50);
        
        return Task.CompletedTask;
    }

    public Task OnDeactivateAsync(CancellationToken ct = default)
    {
        _serviceInstance?.Dispose();
        return Task.CompletedTask;
    }

    public IEnumerable<object> GetServices()
    {
        if (_serviceInstance != null)
        {
            yield return _serviceInstance;
        }
    }
}

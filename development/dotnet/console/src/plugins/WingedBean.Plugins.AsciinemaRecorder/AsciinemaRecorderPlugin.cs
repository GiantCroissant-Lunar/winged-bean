using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Plate.PluginManoi.Contracts;
using Plate.CrossMilo.Contracts.Recorder.Services;

namespace WingedBean.Plugins.AsciinemaRecorder;

/// <summary>
/// Legacy plugin interface implementation for backward compatibility with the current host.
/// This bridges the gap between the new IPluginActivator pattern and the old IPlugin pattern.
/// </summary>
public class AsciinemaRecorderPlugin : IPlugin
{
    private AsciinemaRecorder? _serviceInstance;

    public string Id => "wingedbean.plugins.recorder.asciinema";
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

        var logger = loggerFactory?.CreateLogger<AsciinemaRecorder>() 
                     ?? NullLogger<AsciinemaRecorder>.Instance;

        // Create the recorder instance (transient lifecycle from manifest)
        _serviceInstance = new AsciinemaRecorder(logger);
        
        // Register the service directly with the registry
        registry.Register<IService>(_serviceInstance, priority: 50);
        
        return Task.CompletedTask;
    }

    public Task OnDeactivateAsync(CancellationToken ct = default)
    {
        // Recorder service doesn't have explicit cleanup
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

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Plate.CrossMilo.Contracts;
using Plate.CrossMilo.Contracts.Recorder.Services;
using Plate.PluginManoi.Contracts;

namespace WingedBean.Plugins.AsciinemaRecorder;

/// <summary>
/// Legacy plugin interface implementation for backward compatibility with the current host.
/// This bridges the gap between the new IPluginActivator pattern and the old IPlugin pattern.
/// Now uses URFRecorderService for Universal Recording Format support.
/// </summary>
public class AsciinemaRecorderPlugin : IPlugin
{
    private URFRecorderService? _serviceInstance;

    public string Id => "wingedbean.plugins.recorder.asciinema";
    public string Version => "2.0.0";  // Updated to 2.0 for URF support

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

        var logger = loggerFactory?.CreateLogger<URFRecorderService>()
                     ?? NullLogger<URFRecorderService>.Instance;

        // Create the URF recorder instance (transient lifecycle from manifest)
        _serviceInstance = new URFRecorderService(logger);

        // Register the service with priority 110 (takes precedence over legacy recorder)
        registry.Register<IService>(_serviceInstance, priority: 110);

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

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Plate.CrossMilo.Contracts;
using Plate.PluginManoi.Contracts;
using WingedBean.Contracts.Gameplay;
using IRecorderService = Plate.CrossMilo.Contracts.Recorder.Services.IService;

namespace WingedBean.Plugins.Recorder.Snapshots;

/// <summary>
/// Plugin interface implementation for desktop recorder with URF support.
/// Bridges the gap between the new IPluginActivator pattern and the old IPlugin pattern.
/// Uses URFRecorderService for Universal Recording Format support.
/// </summary>
public class RecorderSnapshotsPlugin : IPlugin
{
    private URFRecorderService? _serviceInstance;

    public string Id => "winged-bean.plugins.recorder.snapshots";
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

        // Try to get IWorldSnapshotProvider (optional for desktop recording)
        IWorldSnapshotProvider? snapshotProvider = null;
        try
        {
            snapshotProvider = registry.Get<IWorldSnapshotProvider>();
        }
        catch
        {
            logger.LogWarning("IWorldSnapshotProvider not available - recording will be limited to visual output only");
        }

        // Create the URF recorder instance with snapshot provider
        // Note: Constructor takes (provider, logger) not (logger, provider)
        // Pass registry for lazy provider resolution in StartRecordingAsync
        _serviceInstance = new URFRecorderService(snapshotProvider, logger, registry);

        // Register the service with priority 110 (takes precedence over legacy recorder)
        registry.Register<IRecorderService>(_serviceInstance, priority: 110);

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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Plate.CrossMilo.Contracts;
using Plate.CrossMilo.Contracts.Recorder.Services;
using IRecorderService = Plate.CrossMilo.Contracts.Recorder.Services.IService;

namespace Dungeon.Host.SadConsole;

/// <summary>
/// Hosted service that controls recording based on command-line arguments
/// Supports: --record, --record-duration=30, --record-session=test-run
/// </summary>
public class RecordingControlService : BackgroundService
{
    private readonly IServiceProvider _services;
    private readonly IConfiguration _configuration;
    private readonly ILogger<RecordingControlService> _logger;
    private readonly IHostApplicationLifetime _lifetime;

    public RecordingControlService(
        IServiceProvider services,
        IConfiguration configuration,
        ILogger<RecordingControlService> logger,
        IHostApplicationLifetime lifetime)
    {
        _services = services;
        _configuration = configuration;
        _logger = logger;
        _lifetime = lifetime;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            // Check if recording is requested via command line
            // Supports both --record=true and --record (flag)
            var recordValue = _configuration["record"];
            var shouldRecord = !string.IsNullOrEmpty(recordValue) && 
                              (recordValue.Equals("true", StringComparison.OrdinalIgnoreCase) || recordValue.Equals(""));
            var recordDuration = _configuration.GetValue<int>("record-duration", 30);
            var sessionId = _configuration["record-session"] 
                ?? $"desktop-{DateTime.UtcNow:yyyyMMdd-HHmmss}";

            if (!shouldRecord)
            {
                _logger.LogDebug("Recording not requested (use --record=true to enable)");
                return;
            }

            _logger.LogInformation("Recording requested: session={SessionId}, duration={Duration}s", 
                sessionId, recordDuration);

            // Wait for plugins to load and services to be available
            await Task.Delay(3000, stoppingToken);

            // Get the recorder service from registry
            var registry = _services.GetService(typeof(IRegistry)) as IRegistry;
            if (registry == null)
            {
                _logger.LogWarning("Registry not available, cannot start recording");
                return;
            }

            IRecorderService? recorderService = null;
            try
            {
                recorderService = registry.Get<IRecorderService>();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Recorder service not available");
                return;
            }

            if (recorderService == null)
            {
                _logger.LogWarning("No recorder service registered");
                return;
            }

            _logger.LogInformation("Starting recording with {RecorderType}", recorderService.GetType().Name);

            // Start recording
            var metadata = new SessionMetadata
            {
                Width = 120,
                Height = 40,
                Title = sessionId,
                Environment = new Dictionary<string, string>
                {
                    ["TERM"] = "xterm-256color",
                    ["PLATFORM"] = "desktop"
                }
            };

            await recorderService.StartRecordingAsync(sessionId, metadata, stoppingToken);
            _logger.LogInformation("✓ Recording started: {SessionId}", sessionId);

            // Record for specified duration
            await Task.Delay(TimeSpan.FromSeconds(recordDuration), stoppingToken);

            // Stop recording
            var recordingPath = await recorderService.StopRecordingAsync(sessionId, stoppingToken);
            _logger.LogInformation("✓ Recording stopped: {Path}", recordingPath);

            // Give time for export to complete
            await Task.Delay(500, stoppingToken);

            _logger.LogInformation("Recording complete, shutting down host");
            _lifetime.StopApplication();
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Recording control cancelled");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in recording control");
        }
    }
}

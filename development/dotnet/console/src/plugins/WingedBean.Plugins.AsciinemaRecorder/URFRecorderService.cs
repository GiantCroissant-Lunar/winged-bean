using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Plate.CrossMilo.Contracts.Recorder.URF;
using Plate.CrossMilo.Contracts.Recorder.URF.Events;
using Plate.CrossMilo.Contracts.Recorder.URF.Exporters;
using Plate.PluginManoi.Contracts;
using IRecorderService = Plate.CrossMilo.Contracts.Recorder.Services.IService;
using RecorderSessionMetadata = Plate.CrossMilo.Contracts.Recorder.Services.SessionMetadata;

namespace WingedBean.Plugins.AsciinemaRecorder;

/// <summary>
/// URF-based recorder for console apps
/// Records terminal output and exports to Asciinema format
/// </summary>
[Plugin(
    Name = "URFConsoleRecorderService",
    Provides = new[] { typeof(IRecorderService) },
    Priority = 110  // Higher priority than legacy recorder
)]
public class URFRecorderService : IRecorderService, IDisposable
{
    private readonly ILogger<URFRecorderService> _logger;
    private readonly UniversalRecorder _recorder;
    private DateTimeOffset _startTime;
    private string? _currentSessionId;

    public URFRecorderService(ILogger<URFRecorderService> logger)
    {
        _logger = logger;
        _recorder = new UniversalRecorder();
    }

    public async Task StartRecordingAsync(string sessionId, RecorderSessionMetadata metadata, CancellationToken ct = default)
    {
        if (_currentSessionId != null)
        {
            _logger.LogWarning("Already recording session {CurrentSession}, ignoring new session {NewSession}",
                _currentSessionId, sessionId);
            return;
        }

        _currentSessionId = sessionId;
        _startTime = DateTimeOffset.UtcNow;

        // Create URF metadata
        var urfMetadata = new RecordingMetadata
        {
            Id = sessionId,
            Started = _startTime,
            Generator = "WingedBean Console Recorder v1.0",
            Platform = "console",
            Application = new ApplicationInfo
            {
                Name = "WingedBean Console",
                Version = "1.0.0"
            },
            Terminal = new TerminalInfo
            {
                Width = metadata.Width,
                Height = metadata.Height,
                ColorDepth = 256,
                Encoding = "utf-8"
            },
            Streams = new() { "terminal" }
        };

        // Add environment variables
        if (metadata.Environment != null)
        {
            foreach (var kvp in metadata.Environment)
            {
                urfMetadata.Environment[kvp.Key] = kvp.Value;
            }
        }

        await _recorder.StartRecordingAsync(sessionId, urfMetadata, ct);

        _logger.LogInformation("URF recording started: {SessionId}", sessionId);
    }

    public async Task RecordDataAsync(string sessionId, byte[] data, DateTimeOffset timestamp, CancellationToken ct = default)
    {
        if (_currentSessionId != sessionId)
        {
            _logger.LogWarning("No active recording for session {SessionId}", sessionId);
            return;
        }

        if (data == null || data.Length == 0)
            return;

        var elapsed = (timestamp - _startTime).TotalSeconds;

        // Record terminal output event
        await _recorder.RecordEventAsync(sessionId, new OutputEvent
        {
            Timestamp = elapsed,
            Stream = "terminal",
            Data = new TerminalOutputData
            {
                Format = "ansi",
                Bytes = Convert.ToBase64String(data),
                Text = Encoding.UTF8.GetString(data)
            }
        }, ct);
    }

    public async Task<string> StopRecordingAsync(string sessionId, CancellationToken ct = default)
    {
        if (_currentSessionId != sessionId)
        {
            _logger.LogWarning("No active recording for session {SessionId}", sessionId);
            return string.Empty;
        }

        var urfPath = await _recorder.StopRecordingAsync(sessionId, ct);
        _currentSessionId = null;

        _logger.LogInformation("URF recording stopped: {Path}", urfPath);

        // Auto-export to Asciinema format for backward compatibility
        try
        {
            var exporter = new AsciinemaExporter();
            var castPath = urfPath.Replace(".urf.jsonl", ".cast");
            await exporter.ExportAsync(urfPath, castPath, ct);
            _logger.LogInformation("Asciinema export created: {Path}", castPath);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to export to Asciinema format");
        }

        return urfPath;
    }

    public void Dispose()
    {
        _recorder?.Dispose();
    }
}

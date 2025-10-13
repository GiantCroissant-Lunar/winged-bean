using System;
using System.Linq;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Plate.CrossMilo.Contracts.Recorder.URF;
using Plate.CrossMilo.Contracts.Recorder.URF.Events;
using Plate.CrossMilo.Contracts.Recorder.URF.Exporters;
using Plate.PluginManoi.Contracts;
using WingedBean.Contracts.Gameplay;
using IRecorderService = Plate.CrossMilo.Contracts.Recorder.Services.IService;
using RecorderSessionMetadata = Plate.CrossMilo.Contracts.Recorder.Services.SessionMetadata;

namespace WingedBean.Plugins.Recorder.Snapshots;

/// <summary>
/// URF-based recorder for desktop apps (SadConsole)
/// Records both cell grid output and game state snapshots
/// </summary>
[Plugin(
    Name = "URFRecorderService",
    Provides = new[] { typeof(IRecorderService) },
    Priority = 110  // Higher priority than legacy recorder
)]
public class URFRecorderService : IRecorderService, IDisposable
{
    private readonly ILogger? _logger;
    private readonly IWorldSnapshotProvider? _provider;
    private readonly UniversalRecorder _recorder;
    private IDisposable? _subscription;
    private DateTimeOffset _startTime;
    private string? _currentSessionId;

    public URFRecorderService(IWorldSnapshotProvider? provider = null, ILogger? logger = null)
    {
        _provider = provider;
        _logger = logger;
        _recorder = new UniversalRecorder();
    }

    public async Task StartRecordingAsync(string sessionId, RecorderSessionMetadata metadata, CancellationToken ct = default)
    {
        if (_currentSessionId != null)
        {
            _logger?.LogWarning("Already recording session {CurrentSession}, ignoring new session {NewSession}",
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
            Generator = "WingedBean Desktop Recorder v1.0",
            Platform = "desktop",
            Application = new ApplicationInfo
            {
                Name = "WingedBean Desktop (SadConsole)",
                Version = "1.0.0"
            },
            Terminal = new TerminalInfo
            {
                Width = metadata.Width,
                Height = metadata.Height
            },
            Streams = new() { "cells", "gameplay", "input" }
        };

        if (!string.IsNullOrEmpty(metadata.Command))
            urfMetadata.Application.Name += $" - {metadata.Command}";

        await _recorder.StartRecordingAsync(sessionId, urfMetadata, ct);

        // Subscribe to world snapshots
        if (_provider != null)
        {
            _subscription = _provider.WorldObservable
                .Throttle(TimeSpan.FromMilliseconds(16)) // ~60 FPS
                .Subscribe(async ws =>
                {
                    try
                    {
                        var elapsed = (DateTimeOffset.UtcNow - _startTime).TotalSeconds;

                        // Record cell grid output
                        await _recorder.RecordEventAsync(sessionId, new OutputEvent
                        {
                            Timestamp = elapsed,
                            Stream = "cells",
                            Data = new CellGridOutputData
                            {
                                Width = metadata.Width,
                                Height = metadata.Height,
                                Cells = ws.Entities.Select(e => new CellData
                                {
                                    X = e.X,
                                    Y = e.Y,
                                    Char = e.Glyph == '\0' ? "." : e.Glyph.ToString(),
                                    Foreground = e.Foreground.ToString(),
                                    Background = e.Background.ToString()
                                }).ToList()
                            }
                        }, ct);

                        // Record game state
                        await _recorder.RecordEventAsync(sessionId, new StateEvent
                        {
                            Timestamp = elapsed,
                            Stream = "gameplay",
                            Data = new
                            {
                                floor = ws.Floor,
                                hud = new
                                {
                                    hp = ws.Hud.HP,
                                    maxHp = ws.Hud.MaxHP,
                                    level = ws.Hud.Level,
                                    xp = ws.Hud.XP,
                                    maxXp = ws.Hud.MaxXP
                                },
                                entityCount = ws.Entities.Count
                            }
                        }, ct);
                    }
                    catch (Exception ex)
                    {
                        _logger?.LogWarning(ex, "Failed to record snapshot");
                    }
                });
        }
        else
        {
            _logger?.LogWarning("IWorldSnapshotProvider not available; only metadata will be recorded");
        }

        _logger?.LogInformation("URF recording started: {SessionId}", sessionId);
    }

    public Task RecordDataAsync(string sessionId, byte[] data, DateTimeOffset timestamp, CancellationToken ct = default)
    {
        // Not used for desktop recording (we use WorldObservable instead)
        // Could be used for recording raw input data if needed
        return Task.CompletedTask;
    }

    public async Task<string> StopRecordingAsync(string sessionId, CancellationToken ct = default)
    {
        if (_currentSessionId != sessionId)
        {
            _logger?.LogWarning("No active recording for session {SessionId}", sessionId);
            return string.Empty;
        }

        _subscription?.Dispose();
        _subscription = null;

        var urfPath = await _recorder.StopRecordingAsync(sessionId, ct);
        _currentSessionId = null;

        _logger?.LogInformation("URF recording stopped: {Path}", urfPath);

        // Auto-export to Asciinema format for compatibility
        // (even though desktop uses cells, having Asciinema export is useful)
        try
        {
            var exporter = new AsciinemaExporter();
            var castPath = urfPath.Replace(".urf.jsonl", ".cast");
            await exporter.ExportAsync(urfPath, castPath, ct);
            _logger?.LogInformation("Asciinema export created: {Path}", castPath);
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "Failed to export to Asciinema format");
        }

        return urfPath;
    }

    public void Dispose()
    {
        _subscription?.Dispose();
        _recorder?.Dispose();
    }
}

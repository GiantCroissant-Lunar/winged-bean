using System;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Plate.PluginManoi.Contracts;
using WingedBean.Contracts.Gameplay;
using IRecorderService = Plate.CrossMilo.Contracts.Recorder.Services.IService;
using RecorderSessionMetadata = Plate.CrossMilo.Contracts.Recorder.Services.SessionMetadata;

namespace WingedBean.Plugins.Recorder.Snapshots;

[Plugin(
    Name = "SnapshotRecorderService",
    Provides = new[] { typeof(IRecorderService) },
    Priority = 100
)]
public class RecorderSnapshotsService : IRecorderService
{
    private readonly ILogger? _logger;
    private readonly IWorldSnapshotProvider? _provider;
    private IDisposable? _subscription;
    private StreamWriter? _writer;
    private DateTimeOffset _start;
    private string? _filePath;

    public RecorderSnapshotsService(IWorldSnapshotProvider? provider = null, ILogger? logger = null)
    {
        _provider = provider;
        _logger = logger;
    }

    public Task StartRecordingAsync(string sessionId, RecorderSessionMetadata metadata, CancellationToken ct = default)
    {
        if (_writer != null) return Task.CompletedTask;

        var dir = Path.Combine(AppContext.BaseDirectory, "recordings");
        Directory.CreateDirectory(dir);
        var stamp = DateTimeOffset.Now.ToString("yyyyMMdd-HHmmss");
        _filePath = Path.Combine(dir, $"snapshots-{sessionId}-{stamp}.jsonl");
        _start = DateTimeOffset.Now;

        _writer = new StreamWriter(new FileStream(_filePath, FileMode.Create, FileAccess.Write, FileShare.Read))
        {
            AutoFlush = true
        };

        // Write meta
        var meta = new
        {
            type = "meta",
            sessionId,
            t0 = _start.ToUnixTimeMilliseconds(),
            term = new { width = metadata.Width, height = metadata.Height, title = metadata.Title },
            env = metadata.Environment,
            command = metadata.Command,
            cwd = metadata.WorkingDirectory
        };
        _writer.WriteLine(JsonSerializer.Serialize(meta));

        if (_provider != null)
        {
            _subscription = _provider.WorldObservable.Subscribe(ws =>
            {
                try
                {
                    var t = (DateTimeOffset.Now - _start).TotalSeconds;
                    var record = new
                    {
                        type = "snapshot",
                        t,
                        floor = ws.Floor,
                        hud = new { ws.Hud.HP, ws.Hud.MaxHP, ws.Hud.Level, ws.Hud.XP, ws.Hud.MaxXP },
                        entities = ws.Entities.Select(e => new
                        {
                            e.X,
                            e.Y,
                            glyph = e.Glyph == '\0' ? "." : e.Glyph.ToString(),
                            fg = e.Foreground,
                            bg = e.Background
                        })
                    };
                    _writer!.WriteLine(JsonSerializer.Serialize(record));
                }
                catch (Exception ex)
                {
                    _logger?.LogWarning(ex, "Failed to write snapshot record");
                }
            });
        }
        else
        {
            _logger?.LogWarning("IWorldSnapshotProvider not available; snapshot recording will be empty");
        }

        _logger?.LogInformation("Snapshot recording started: {Path}", _filePath);
        return Task.CompletedTask;
    }

    public Task RecordDataAsync(string sessionId, byte[] data, DateTimeOffset timestamp, CancellationToken ct = default)
    {
        // Optional: allow additional annotations to be recorded
        if (_writer == null) return Task.CompletedTask;
        var rel = (timestamp - _start).TotalSeconds;
        var evt = new { type = "data", t = rel, len = data?.Length ?? 0 };
        _writer.WriteLine(JsonSerializer.Serialize(evt));
        return Task.CompletedTask;
    }

    public Task<string> StopRecordingAsync(string sessionId, CancellationToken ct = default)
    {
        var path = _filePath ?? string.Empty;
        try
        {
            _subscription?.Dispose();
            _subscription = null;
            _writer?.Flush();
            _writer?.Dispose();
            _writer = null;
            _logger?.LogInformation("Snapshot recording stopped: {Path}", path);
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "Error stopping snapshot recording");
        }
        finally
        {
            _filePath = null;
        }
        return Task.FromResult(path);
    }
}

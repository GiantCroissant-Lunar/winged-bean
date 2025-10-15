using System;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Plate.CrossMilo.Contracts;
using Plate.CrossMilo.Contracts.Recorder.Video;
using Plate.PluginManoi.Contracts;
using WingedBean.Contracts.Gameplay;
using IRecorderService = Plate.CrossMilo.Contracts.Recorder.Services.IService;
using RecorderSessionMetadata = Plate.CrossMilo.Contracts.Recorder.Services.SessionMetadata;

namespace WingedBean.Plugins.Recorder.Snapshots;

/// <summary>
/// Hybrid recorder that synchronizes URF and video recording.
/// Extends URFRecorderService with optional video encoding.
/// </summary>
[Plugin(
    Name = "HybridRecorder",
    Provides = new[] { typeof(IRecorderService) },
    Priority = 120  // Higher priority than URFRecorderService
)]
public class HybridRecorder : URFRecorderService
{
    private readonly IVideoEncoder? _videoEncoder;
    private readonly ILogger? _logger;
    private DateTimeOffset _startTime;
    private string? _currentSessionId;
    private RecorderSessionMetadata? _currentMetadata;
    private bool _videoEnabled;

    public HybridRecorder(
        IWorldSnapshotProvider? provider = null,
        IRegistry? registry = null,
        ILogger? logger = null) : base(provider, logger)
    {
        _logger = logger;

        // Try to get video encoder from registry (optional)
        if (registry?.IsRegistered<IVideoEncoder>() == true)
        {
            _videoEncoder = registry.Get<IVideoEncoder>();
            _logger?.LogInformation("✓ Hybrid recording available (Video + URF)");
        }
        else
        {
            _logger?.LogInformation("ℹ URF-only recording (no video encoder available)");
        }
    }

    public new async Task StartRecordingAsync(
        string sessionId,
        RecorderSessionMetadata metadata,
        CancellationToken ct = default)
    {
        _currentSessionId = sessionId;
        _currentMetadata = metadata;
        _startTime = DateTimeOffset.UtcNow;

        // Check if video recording is enabled via environment variable
        _videoEnabled = _videoEncoder != null &&
                       Environment.GetEnvironmentVariable("ENABLE_VIDEO") == "1";

        // Start URF recording (always)
        await base.StartRecordingAsync(sessionId, metadata, ct);
        _logger?.LogInformation("✓ URF recording started");

        // Start video recording (if enabled and available)
        if (_videoEnabled && _videoEncoder != null)
        {
            try
            {
                var outputDir = metadata.OutputDirectory ?? "recordings";
                var videoPath = Path.Combine(outputDir, $"{sessionId}.mp4");

                // Calculate video dimensions from terminal size
                // Each cell is approximately 8x16 pixels (standard terminal font)
                var videoWidth = metadata.Width * 8;
                var videoHeight = metadata.Height * 16;
                var fps = 60; // Target 60 FPS

                _videoEncoder.StartRecording(videoPath, videoWidth, videoHeight, fps);
                _logger?.LogInformation("✓ Video recording started: {Path}", videoPath);
                _logger?.LogInformation("  Resolution: {Width}x{Height} @ {FPS} fps", 
                    videoWidth, videoHeight, fps);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Failed to start video recording");
                _videoEnabled = false; // Disable video for this session
            }
        }

        // Save synchronization metadata
        if (_videoEnabled)
        {
            await SaveSyncMetadata(sessionId, metadata);
        }
    }

    public new async Task<string> StopRecordingAsync(string sessionId, CancellationToken ct = default)
    {
        // Stop URF recording
        var urfPath = await base.StopRecordingAsync(sessionId, ct);
        _logger?.LogInformation("✓ URF recording stopped: {Path}", urfPath);

        // Stop video recording
        if (_videoEnabled && _videoEncoder != null && _videoEncoder.IsRecording)
        {
            try
            {
                await _videoEncoder.StopRecordingAsync(ct);
                _logger?.LogInformation("✓ Video recording stopped");
                _logger?.LogInformation("  Encoded {FrameCount} frames in {Duration:g}",
                    _videoEncoder.FrameCount, _videoEncoder.Duration);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error stopping video recording");
            }
        }

        // Update sync metadata with final duration
        if (_videoEnabled && _currentMetadata != null)
        {
            await UpdateSyncMetadata(sessionId, _currentMetadata);
        }

        _logger?.LogInformation("✓ Hybrid recording complete: {SessionId}", sessionId);
        _currentSessionId = null;
        _currentMetadata = null;

        return urfPath;
    }

    private async Task SaveSyncMetadata(string sessionId, RecorderSessionMetadata metadata)
    {
        try
        {
            var outputDir = metadata.OutputDirectory ?? "recordings";
            var syncPath = Path.Combine(outputDir, $"{sessionId}.sync.json");

            var syncData = new RecordingSyncMetadata
            {
                SessionId = sessionId,
                StartTime = _startTime,
                VideoFile = $"{sessionId}.mp4",
                URFFile = $"{sessionId}.urf.jsonl",
                FPS = 60,
                Width = metadata.Width * 8,
                Height = metadata.Height * 16,
                DurationMs = 0, // Will be updated when recording stops
                Metadata = new Dictionary<string, string>
                {
                    ["recorder"] = "WingedBean.Plugins.Recorder.Snapshots.HybridRecorder",
                    ["videoEncoder"] = "Plate.Plugins.FFmpeg",
                    ["platform"] = "desktop",
                    ["terminalWidth"] = metadata.Width.ToString(),
                    ["terminalHeight"] = metadata.Height.ToString()
                }
            };

            var json = JsonSerializer.Serialize(syncData, new JsonSerializerOptions
            {
                WriteIndented = true
            });

            await File.WriteAllTextAsync(syncPath, json);
            _logger?.LogDebug("Sync metadata saved: {Path}", syncPath);
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "Failed to save sync metadata");
        }
    }

    private async Task UpdateSyncMetadata(string sessionId, RecorderSessionMetadata metadata)
    {
        try
        {
            var outputDir = metadata.OutputDirectory ?? "recordings";
            var syncPath = Path.Combine(outputDir, $"{sessionId}.sync.json");

            if (!File.Exists(syncPath))
                return;

            var json = await File.ReadAllTextAsync(syncPath);
            var syncData = JsonSerializer.Deserialize<RecordingSyncMetadata>(json);

            if (syncData != null)
            {
                syncData.DurationMs = (long)(DateTimeOffset.UtcNow - _startTime).TotalMilliseconds;

                var updatedJson = JsonSerializer.Serialize(syncData, new JsonSerializerOptions
                {
                    WriteIndented = true
                });

                await File.WriteAllTextAsync(syncPath, updatedJson);
                _logger?.LogDebug("Sync metadata updated with duration: {Duration}ms", syncData.DurationMs);
            }
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "Failed to update sync metadata");
        }
    }
}

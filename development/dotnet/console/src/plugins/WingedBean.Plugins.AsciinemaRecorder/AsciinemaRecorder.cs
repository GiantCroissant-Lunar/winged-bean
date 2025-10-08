using System.Text.Json;
using Microsoft.Extensions.Logging;
using Plate.CrossMilo.Contracts.Recorder.Services;
using Plate.CrossMilo.Contracts.Recorder;

namespace WingedBean.Plugins.AsciinemaRecorder;

/// <summary>
/// Asciinema v2 format recorder implementation
/// </summary>
public class AsciinemaRecorder : IService
{
    private readonly Dictionary<string, RecordingContext> _activeRecordings = new();
    private readonly ILogger<AsciinemaRecorder> _logger;
    private readonly string _recordingsDirectory;

    public AsciinemaRecorder(ILogger<AsciinemaRecorder> logger)
    {
        _logger = logger;
        _recordingsDirectory = GitVersionHelper.GetRecordingsDirectory();
        Directory.CreateDirectory(_recordingsDirectory);
        _logger.LogInformation("Asciinema recordings will be saved to: {RecordingsDirectory}", _recordingsDirectory);
    }

    public async Task StartRecordingAsync(string sessionId, SessionMetadata metadata, CancellationToken ct = default)
    {
        _logger.LogInformation("Starting recording for session: {SessionId}", sessionId);

        if (_activeRecordings.ContainsKey(sessionId))
        {
            throw new InvalidOperationException($"Recording already active for session: {sessionId}");
        }

        var outputPath = Path.Combine(_recordingsDirectory, $"{sessionId}_{DateTime.UtcNow:yyyyMMdd_HHmmss}.cast");
        var context = new RecordingContext(sessionId, outputPath, metadata);

        // Write asciinema v2 header
        var header = new AsciinemaHeader
        {
            Version = 2,
            Width = metadata.Width,
            Height = metadata.Height,
            Timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
            Title = metadata.Title,
            Environment = new Dictionary<string, string>
            {
                ["TERM"] = metadata.Environment.GetValueOrDefault("TERM", "xterm-256color"),
                ["SHELL"] = metadata.Environment.GetValueOrDefault("SHELL", "/bin/bash")
            }
        };

        var headerJson = JsonSerializer.Serialize(header, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
        await File.WriteAllTextAsync(context.OutputPath, headerJson + "\n", ct);

        _activeRecordings[sessionId] = context;

        _logger.LogInformation("Recording started: {OutputPath}", outputPath);
    }

    public async Task RecordDataAsync(string sessionId, byte[] data, DateTimeOffset timestamp, CancellationToken ct = default)
    {
        if (!_activeRecordings.TryGetValue(sessionId, out var context))
        {
            _logger.LogWarning("No active recording for session: {SessionId}", sessionId);
            return;
        }

        // Calculate relative timestamp (seconds since recording start)
        var relativeTime = (timestamp - context.StartTime).TotalSeconds;

        // Create asciinema event: [time, "o", "data"]
        var eventData = new object[] { relativeTime, "o", System.Text.Encoding.UTF8.GetString(data) };
        var eventJson = JsonSerializer.Serialize(eventData);

        await File.AppendAllTextAsync(context.OutputPath, eventJson + "\n", ct);

        context.EventCount++;

        _logger.LogTrace("Recorded {DataSize} bytes for session {SessionId} at {Time}s",
            data.Length, sessionId, relativeTime);
    }

    public async Task<string> StopRecordingAsync(string sessionId, CancellationToken ct = default)
    {
        _logger.LogInformation("Stopping recording for session: {SessionId}", sessionId);

        if (!_activeRecordings.TryGetValue(sessionId, out var context))
        {
            throw new InvalidOperationException($"No active recording for session: {sessionId}");
        }

        _activeRecordings.Remove(sessionId);

        var duration = (DateTimeOffset.UtcNow - context.StartTime).TotalSeconds;

        _logger.LogInformation("Recording stopped: {OutputPath}, Duration: {Duration}s, Events: {EventCount}",
            context.OutputPath, duration, context.EventCount);

        return context.OutputPath;
    }

    private class RecordingContext
    {
        public string SessionId { get; }
        public string OutputPath { get; }
        public SessionMetadata Metadata { get; }
        public DateTimeOffset StartTime { get; }
        public int EventCount { get; set; }

        public RecordingContext(string sessionId, string outputPath, SessionMetadata metadata)
        {
            SessionId = sessionId;
            OutputPath = outputPath;
            Metadata = metadata;
            StartTime = DateTimeOffset.UtcNow;
        }
    }
}

/// <summary>
/// Asciinema v2 header format
/// </summary>
internal class AsciinemaHeader
{
    public int Version { get; set; }
    public int Width { get; set; }
    public int Height { get; set; }
    public long Timestamp { get; set; }
    public string Title { get; set; } = string.Empty;
    public Dictionary<string, string> Environment { get; set; } = new();
}

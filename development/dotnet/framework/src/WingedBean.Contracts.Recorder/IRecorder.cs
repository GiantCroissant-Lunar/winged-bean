using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace WingedBean.Contracts.Recorder;

/// <summary>
/// Interface for recording terminal sessions (e.g., Asciinema format)
/// </summary>
public interface IRecorder
{
    /// <summary>
    /// Start recording a session
    /// </summary>
    /// <param name="sessionId">Unique session identifier</param>
    /// <param name="metadata">Session metadata</param>
    /// <param name="ct">Cancellation token</param>
    Task StartRecordingAsync(string sessionId, SessionMetadata metadata, CancellationToken ct = default);

    /// <summary>
    /// Record data for a session
    /// </summary>
    /// <param name="sessionId">Session identifier</param>
    /// <param name="data">Data to record</param>
    /// <param name="timestamp">Event timestamp</param>
    /// <param name="ct">Cancellation token</param>
    Task RecordDataAsync(string sessionId, byte[] data, DateTimeOffset timestamp, CancellationToken ct = default);

    /// <summary>
    /// Stop recording and finalize the session
    /// </summary>
    /// <param name="sessionId">Session identifier</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Path to the recorded file</returns>
    Task<string> StopRecordingAsync(string sessionId, CancellationToken ct = default);
}

/// <summary>
/// Session metadata for recording
/// </summary>
public class SessionMetadata
{
    /// <summary>Terminal width in columns</summary>
    public int Width { get; set; }

    /// <summary>Terminal height in rows</summary>
    public int Height { get; set; }

    /// <summary>Terminal title</summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>Environment variables</summary>
    public Dictionary<string, string> Environment { get; set; } = new();

    /// <summary>Shell command</summary>
    public string Command { get; set; } = string.Empty;

    /// <summary>Working directory</summary>
    public string WorkingDirectory { get; set; } = string.Empty;
}

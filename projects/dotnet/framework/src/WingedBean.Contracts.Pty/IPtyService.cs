namespace WingedBean.Contracts;

/// <summary>
/// Interface for PTY (Pseudo Terminal) services
/// </summary>
public interface IPtyService
{
    /// <summary>
    /// Start a PTY session with the specified configuration
    /// </summary>
    /// <param name="config">PTY configuration</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Session information</returns>
    Task<PtySession> StartSessionAsync(PtyConfig config, CancellationToken ct = default);

    /// <summary>
    /// Stop a PTY session
    /// </summary>
    /// <param name="sessionId">Session identifier</param>
    /// <param name="ct">Cancellation token</param>
    Task StopSessionAsync(string sessionId, CancellationToken ct = default);

    /// <summary>
    /// Send data to a PTY session
    /// </summary>
    /// <param name="sessionId">Session identifier</param>
    /// <param name="data">Data to send</param>
    /// <param name="ct">Cancellation token</param>
    Task SendDataAsync(string sessionId, byte[] data, CancellationToken ct = default);

    /// <summary>
    /// Resize a PTY session
    /// </summary>
    /// <param name="sessionId">Session identifier</param>
    /// <param name="cols">New column count</param>
    /// <param name="rows">New row count</param>
    /// <param name="ct">Cancellation token</param>
    Task ResizeAsync(string sessionId, int cols, int rows, CancellationToken ct = default);

    /// <summary>
    /// Event raised when data is received from a PTY session
    /// </summary>
    event EventHandler<PtyDataReceivedEventArgs> DataReceived;

    /// <summary>
    /// Event raised when a PTY session exits
    /// </summary>
    event EventHandler<PtySessionExitedEventArgs> SessionExited;
}

/// <summary>
/// PTY configuration
/// </summary>
public class PtyConfig
{
    /// <summary>Command to execute</summary>
    public string Command { get; set; } = string.Empty;

    /// <summary>Command arguments</summary>
    public string[] Args { get; set; } = Array.Empty<string>();

    /// <summary>Working directory</summary>
    public string? WorkingDirectory { get; set; }

    /// <summary>Environment variables</summary>
    public Dictionary<string, string> Environment { get; set; } = new();

    /// <summary>Terminal width in columns</summary>
    public int Cols { get; set; } = 80;

    /// <summary>Terminal height in rows</summary>
    public int Rows { get; set; } = 24;

    /// <summary>Terminal type</summary>
    public string TerminalType { get; set; } = "xterm-256color";
}

/// <summary>
/// PTY session information
/// </summary>
public class PtySession
{
    /// <summary>Unique session identifier</summary>
    public string SessionId { get; set; } = string.Empty;

    /// <summary>Process ID</summary>
    public int ProcessId { get; set; }

    /// <summary>Session configuration</summary>
    public PtyConfig Config { get; set; } = new();

    /// <summary>Session start time</summary>
    public DateTimeOffset StartTime { get; set; }
}

/// <summary>
/// Event args for PTY data received
/// </summary>
public class PtyDataReceivedEventArgs : EventArgs
{
    /// <summary>Session identifier</summary>
    public string SessionId { get; set; } = string.Empty;

    /// <summary>Received data</summary>
    public byte[] Data { get; set; } = Array.Empty<byte>();

    /// <summary>Timestamp</summary>
    public DateTimeOffset Timestamp { get; set; }
}

/// <summary>
/// Event args for PTY session exited
/// </summary>
public class PtySessionExitedEventArgs : EventArgs
{
    /// <summary>Session identifier</summary>
    public string SessionId { get; set; } = string.Empty;

    /// <summary>Exit code</summary>
    public int ExitCode { get; set; }

    /// <summary>Exit signal</summary>
    public string? Signal { get; set; }

    /// <summary>Exit timestamp</summary>
    public DateTimeOffset Timestamp { get; set; }
}

namespace WingedBean.Contracts;

/// <summary>
/// Interface for Terminal applications
/// </summary>
public interface ITerminalApp
{
    /// <summary>
    /// Start the terminal application
    /// </summary>
    /// <param name="config">Application configuration</param>
    /// <param name="ct">Cancellation token</param>
    Task StartAsync(TerminalAppConfig config, CancellationToken ct = default);

    /// <summary>
    /// Stop the terminal application
    /// </summary>
    /// <param name="ct">Cancellation token</param>
    Task StopAsync(CancellationToken ct = default);

    /// <summary>
    /// Send input to the terminal application
    /// </summary>
    /// <param name="data">Input data</param>
    /// <param name="ct">Cancellation token</param>
    Task SendInputAsync(byte[] data, CancellationToken ct = default);

    /// <summary>
    /// Resize the terminal application
    /// </summary>
    /// <param name="cols">New column count</param>
    /// <param name="rows">New row count</param>
    /// <param name="ct">Cancellation token</param>
    Task ResizeAsync(int cols, int rows, CancellationToken ct = default);

    /// <summary>
    /// Event raised when the application produces output
    /// </summary>
    event EventHandler<TerminalOutputEventArgs> OutputReceived;

    /// <summary>
    /// Event raised when the application exits
    /// </summary>
    event EventHandler<TerminalExitEventArgs> Exited;
}

/// <summary>
/// Terminal application configuration
/// </summary>
public class TerminalAppConfig
{
    /// <summary>Application name</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Terminal width in columns</summary>
    public int Cols { get; set; } = 80;

    /// <summary>Terminal height in rows</summary>
    public int Rows { get; set; } = 24;

    /// <summary>Working directory</summary>
    public string? WorkingDirectory { get; set; }

    /// <summary>Environment variables</summary>
    public Dictionary<string, string> Environment { get; set; } = new();

    /// <summary>Additional configuration parameters</summary>
    public Dictionary<string, object> Parameters { get; set; } = new();
}

/// <summary>
/// Event args for terminal output
/// </summary>
public class TerminalOutputEventArgs : EventArgs
{
    /// <summary>Output data</summary>
    public byte[] Data { get; set; } = Array.Empty<byte>();

    /// <summary>Timestamp</summary>
    public DateTimeOffset Timestamp { get; set; }
}

/// <summary>
/// Event args for terminal exit
/// </summary>
public class TerminalExitEventArgs : EventArgs
{
    /// <summary>Exit code</summary>
    public int ExitCode { get; set; }

    /// <summary>Exit timestamp</summary>
    public DateTimeOffset Timestamp { get; set; }
}

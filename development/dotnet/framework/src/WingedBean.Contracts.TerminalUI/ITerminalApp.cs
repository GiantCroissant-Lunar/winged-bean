using WingedBean.Contracts.UI;

namespace WingedBean.Contracts.TerminalUI;

/// <summary>
/// Terminal-specific UI application.
/// Extends IUIApp with terminal-specific capabilities (ANSI, PTY, etc.).
/// </summary>
public interface ITerminalApp : IUIApp
{
    // Terminal-specific operations
    Task SendRawInputAsync(byte[] data, CancellationToken ct = default);
    Task SetCursorPositionAsync(int x, int y, CancellationToken ct = default);
    Task WriteAnsiAsync(string ansiSequence, CancellationToken ct = default);

    // Terminal-specific events
    event EventHandler<TerminalOutputEventArgs>? OutputReceived;
    event EventHandler<TerminalExitEventArgs>? Exited;
}

// Configuration (from RFC-0029, now under TerminalUI namespace)
public class TerminalAppConfig
{
    public string Name { get; set; } = string.Empty;
    public int Cols { get; set; } = 80;
    public int Rows { get; set; } = 24;
    public string? WorkingDirectory { get; set; }
    public Dictionary<string, string> Environment { get; set; } = new();
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

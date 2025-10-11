using System.Collections.Generic;

namespace WingedBean.Plugins.TerminalUI;

/// <summary>
/// Configuration for terminal application.
/// From RFC-0029, moved from CrossMilo.Contracts.TerminalUI to plugin implementation.
/// </summary>
public class TerminalAppConfig
{
    public string Name { get; set; } = string.Empty;
    public int Cols { get; set; } = 80;
    public int Rows { get; set; } = 24;
    public string? WorkingDirectory { get; set; }
    public Dictionary<string, string> Environment { get; set; } = new();
}

namespace WingedBean.Plugins.TerminalUI;

/// <summary>
/// Terminal UI service for text-based user interface rendering.
/// Platform implementations: Terminal.Gui v2 (Console), custom TUI frameworks (other platforms).
/// Moved from CrossMilo.Contracts.TerminalUI.Services.IService to plugin implementation.
/// </summary>
public interface ITerminalUIService
{
    /// <summary>
    /// Initialize the terminal UI system.
    /// Sets up the terminal environment and prepares for rendering.
    /// </summary>
    void Initialize();

    /// <summary>
    /// Run the terminal UI main loop.
    /// This method blocks until the UI is closed.
    /// </summary>
    void Run();

    /// <summary>
    /// Get the current screen content as a string.
    /// Useful for testing, debugging, or screen capture.
    /// </summary>
    /// <returns>Current screen content</returns>
    string GetScreenContent();
}

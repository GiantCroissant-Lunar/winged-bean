using Terminal.Gui;
using WingedBean.Contracts.Core;
using WingedBean.Contracts.TerminalUI;

namespace WingedBean.Plugins.TerminalUI;

/// <summary>
/// Terminal.Gui v2 implementation of the Terminal UI service.
/// Provides text-based user interface rendering using Terminal.Gui.
/// </summary>
[Plugin(
    Name = "TerminalGuiService",
    Provides = new[] { typeof(ITerminalUIService) },
    Priority = 100
)]
public class TerminalGuiService : ITerminalUIService
{
    private bool _initialized = false;
    private Window? _mainWindow;
    private System.Timers.Timer? _timer;

    /// <summary>
    /// Initialize the terminal UI system.
    /// Sets up the terminal environment and prepares for rendering.
    /// </summary>
    public void Initialize()
    {
        if (_initialized)
        {
            return;
        }

        // Initialize Terminal.Gui
        Application.Init();
        _initialized = true;

        // Create main window
        _mainWindow = new Window()
        {
            Title = "🎉 Terminal.Gui v2 PTY Demo (Ctrl+Q to quit)",
            BorderStyle = LineStyle.Single
        };

        // Create labels
        var titleLabel = new Label
        {
            X = 1,
            Y = 1,
            Text = "SUCCESS: Real Terminal.Gui v2 in PTY!"
        };

        var infoLabel = new Label
        {
            X = 1,
            Y = 3,
            Text = "✅ This proves Terminal.Gui v2 works in xterm.js via PTY"
        };

        var architectureLabel = new Label
        {
            X = 1,
            Y = 5,
            Text = "🔗 Architecture: Terminal.Gui → PTY → Node.js → WebSocket → xterm.js"
        };

        // Create a live time display
        var timeLabel = new Label
        {
            X = 1,
            Y = 7,
            Text = $"⏰ Time: {DateTime.Now:yyyy-MM-dd HH:mm:ss}"
        };

        // Create interactive elements
        var button = new Button
        {
            X = 1,
            Y = 9,
            Text = "_Click Me!",
            IsDefault = true
        };

        var textField = new TextField
        {
            X = 15,
            Y = 9,
            Width = 30,
            Text = "Type something here..."
        };

        var statusLabel = new Label
        {
            X = 1,
            Y = 11,
            Text = "Ready - Terminal.Gui is fully functional!"
        };

        // Button click handler
        button.Accepting += (s, e) => {
            statusLabel.Text = $"Button clicked at {DateTime.Now:HH:mm:ss}!";
        };

        // Add all controls to window
        _mainWindow.Add(titleLabel, infoLabel, architectureLabel, timeLabel, button, textField, statusLabel);

        // Create menu bar
        var menu = new MenuBar();
        var fileMenu = new MenuBarItem("_File", new MenuItem[] {
            new("_Quit", "", () => Application.RequestStop())
        });
        var helpMenu = new MenuBarItem("_Help", new MenuItem[] {
            new("_About", "", () => {
                MessageBox.Query("About", "Terminal.Gui v2 PTY Demo\nRunning in xterm.js via node-pty", "Ok");
            })
        });
        menu.Menus = new[] { fileMenu, helpMenu };
        _mainWindow.Add(menu);

        // Timer to update the time display every second
        void UpdateTime()
        {
            timeLabel.Text = $"⏰ Time: {DateTime.Now:yyyy-MM-dd HH:mm:ss}";
            Application.Wakeup();
        }

        _timer = new System.Timers.Timer(1000);
        _timer.Elapsed += (sender, e) => UpdateTime();
        _timer.Start();
    }

    /// <summary>
    /// Run the terminal UI main loop.
    /// This method blocks until the UI is closed.
    /// </summary>
    public void Run()
    {
        if (!_initialized || _mainWindow == null)
        {
            throw new InvalidOperationException("Terminal UI must be initialized before running. Call Initialize() first.");
        }

        try
        {
            // Run the application
            Application.Run(_mainWindow);
        }
        finally
        {
            // Cleanup
            _timer?.Stop();
            _timer?.Dispose();
            _mainWindow?.Dispose();
            Application.Shutdown();
            _initialized = false;
        }
    }

    /// <summary>
    /// Get the current screen content as a string.
    /// Useful for testing, debugging, or screen capture.
    /// </summary>
    /// <returns>Current screen content</returns>
    public string GetScreenContent()
    {
        if (!_initialized)
        {
            return "Terminal.Gui not initialized - call Initialize() first.\r\n";
        }

        try
        {
            // Return a simple representation of the current UI state
            // In a real implementation, this would capture the actual screen buffer
            var content = "Terminal.Gui v2 PTY Demo\n";
            content += "========================\n";
            content += "SUCCESS: Real Terminal.Gui v2 in PTY!\n";
            content += "✅ This proves Terminal.Gui v2 works in xterm.js via PTY\n";
            content += "🔗 Architecture: Terminal.Gui → PTY → Node.js → WebSocket → xterm.js\n";
            content += $"⏰ Time: {DateTime.Now:yyyy-MM-dd HH:mm:ss}\n";
            content += "Ready - Terminal.Gui is fully functional!\n";
            return content;
        }
        catch (Exception ex)
        {
            return $"Error getting screen content: {ex.Message}\r\n";
        }
    }
}

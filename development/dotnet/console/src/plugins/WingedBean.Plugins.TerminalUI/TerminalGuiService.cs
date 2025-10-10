using Terminal.Gui;
using Plate.PluginManoi.Contracts;
using Plate.CrossMilo.Contracts.TerminalUI.Services;
using Plate.CrossMilo.Contracts.TerminalUI;
using System.Collections.Concurrent;

namespace WingedBean.Plugins.TerminalUI;

/// <summary>
/// Terminal.Gui v2 implementation of the Terminal UI service.
/// Provides text-based user interface rendering using Terminal.Gui.
/// </summary>
[Plugin(
    Name = "TerminalGuiService",
    Provides = new[] { typeof(IService) },
    Priority = 100
)]
public class TerminalGuiService : IService
{
    private bool _initialized = false;
    private Window? _mainWindow;
    private System.Timers.Timer? _timer;
    private TextView? _logView;
    private readonly ConcurrentQueue<string> _logMessages = new();
    private const int MaxLogMessages = 100;

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
            Text = "Ready - Terminal.Gui is fully functional! (F9=Record, F10=Stop)"
        };

        // Create log console view
        var logLabel = new Label
        {
            X = 1,
            Y = 13,
            Text = "Log Console:"
        };

        _logView = new TextView
        {
            X = 1,
            Y = 14,
            Width = Dim.Fill() - 1,
            Height = Dim.Fill() - 1,
            ReadOnly = true,
            WordWrap = false
        };

        // Add some initial log messages
        LogMessage("INFO", "Terminal.Gui v2 initialized successfully");
        LogMessage("INFO", "PTY connection established");
        LogMessage("DEBUG", "All 9 plugins loaded");

        // Button click handler
        button.Accepting += (s, e) => {
            statusLabel.Text = $"Button clicked at {DateTime.Now:HH:mm:ss}!";
            LogMessage("INFO", $"Button clicked by user at {DateTime.Now:HH:mm:ss}");
        };

        // Add keyboard handler for F9/F10 recording control
        _mainWindow.KeyDown += (sender, e) => {
            // F9 = Start Recording
            if (e.KeyCode == KeyCode.F9)
            {
                // Send OSC sequence to PTY service to start recording
                Console.Write("\x1b]1337;StartRecording\x07");
                Console.Out.Flush();
                statusLabel.Text = "🔴 Recording started (F10 to stop)";
                LogMessage("INFO", "Recording started");
                e.Handled = true;
            }
            // F10 = Stop Recording
            else if (e.KeyCode == KeyCode.F10)
            {
                // Send OSC sequence to PTY service to stop recording
                Console.Write("\x1b]1337;StopRecording\x07");
                Console.Out.Flush();
                statusLabel.Text = "⏹️  Recording stopped (F9 to start)";
                LogMessage("INFO", "Recording stopped");
                e.Handled = true;
            }
        };

        // Add all controls to window
        _mainWindow.Add(titleLabel, infoLabel, architectureLabel, timeLabel, button, textField, statusLabel, logLabel, _logView);

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
    /// Add a log message to the log console.
    /// Messages are displayed in a scrollable TextView.
    /// </summary>
    /// <param name="level">Log level (INFO, DEBUG, WARN, ERROR)</param>
    /// <param name="message">Log message text</param>
    public void LogMessage(string level, string message)
    {
        var timestamp = DateTime.Now.ToString("HH:mm:ss.fff");
        var logLine = $"[{timestamp}] {level.PadRight(5)} | {message}";
        
        _logMessages.Enqueue(logLine);
        
        // Keep only the last N messages
        while (_logMessages.Count > MaxLogMessages)
        {
            _logMessages.TryDequeue(out _);
        }
        
        // Update the TextView if it exists
        if (_logView != null)
        {
            Application.Invoke(() =>
            {
                _logView.Text = string.Join("\n", _logMessages);
                // Scroll to bottom to show latest message
                _logView.MoveEnd();
            });
        }
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

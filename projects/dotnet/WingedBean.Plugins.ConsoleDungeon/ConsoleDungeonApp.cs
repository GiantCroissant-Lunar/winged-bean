using Microsoft.Extensions.Logging;
using System.Text;
using Terminal.Gui;
using WingedBean.Contracts;

namespace WingedBean.Plugins.ConsoleDungeon;

/// <summary>
/// Console Dungeon Terminal.Gui application
/// </summary>
public class ConsoleDungeonApp : ITerminalApp, IDisposable
{
    private readonly ILogger<ConsoleDungeonApp> _logger;
    private bool _isRunning = false;
    private bool _disposed = false;
    private Toplevel? _mainWindow;
    private CancellationTokenSource? _cancellationTokenSource;
    private TerminalAppConfig? _config;

    public event EventHandler<TerminalOutputEventArgs>? OutputReceived;
    public event EventHandler<TerminalExitEventArgs>? Exited;

    public ConsoleDungeonApp(ILogger<ConsoleDungeonApp> logger)
    {
        _logger = logger;
    }

    public async Task StartAsync(TerminalAppConfig config, CancellationToken ct = default)
    {
        if (_isRunning)
        {
            _logger.LogWarning("Console Dungeon is already running");
            return;
        }

        _config = config;
        _cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(ct);
        
        _logger.LogInformation("Starting Console Dungeon application...");

        try
        {
            // Initialize Terminal.Gui
            Application.Init();
            
            _isRunning = true;

            // Create main window
            CreateMainWindow();

            // Run the application in a background task
            await Task.Run(() =>
            {
                try
                {
                    Application.Run(_mainWindow);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error running Console Dungeon application");
                }
                finally
                {
                    _isRunning = false;
                    
                    // Notify that the application has exited
                    var exitArgs = new TerminalExitEventArgs
                    {
                        ExitCode = 0,
                        Timestamp = DateTimeOffset.UtcNow
                    };
                    Exited?.Invoke(this, exitArgs);
                }
            }, _cancellationTokenSource.Token);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to start Console Dungeon application");
            _isRunning = false;
            throw;
        }
    }

    public async Task StopAsync(CancellationToken ct = default)
    {
        if (!_isRunning)
        {
            _logger.LogWarning("Console Dungeon is not running");
            return;
        }

        _logger.LogInformation("Stopping Console Dungeon application...");

        try
        {
            _cancellationTokenSource?.Cancel();
            
            // Request shutdown of the Terminal.Gui application
            Application.RequestStop();
            
            // Wait a bit for graceful shutdown
            await Task.Delay(1000, ct);
            
            _isRunning = false;
            _logger.LogInformation("Console Dungeon application stopped");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error stopping Console Dungeon application");
            throw;
        }
    }

    public Task SendInputAsync(byte[] data, CancellationToken ct = default)
    {
        if (!_isRunning)
        {
            _logger.LogWarning("Cannot send input - Console Dungeon is not running");
            return Task.CompletedTask;
        }

        try
        {
            // Convert input data to key events
            var input = Encoding.UTF8.GetString(data);
            _logger.LogDebug("Received input: {Input}", input);

            // In a real implementation, we would convert the input to Terminal.Gui key events
            // For now, just log the input
            // This would require more sophisticated input handling to convert bytes to KeyEvent objects

            return Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing input");
            return Task.CompletedTask;
        }
    }

    public Task ResizeAsync(int cols, int rows, CancellationToken ct = default)
    {
        _logger.LogInformation("Resize request: {Cols}x{Rows}", cols, rows);

        if (_config != null)
        {
            _config.Cols = cols;
            _config.Rows = rows;
        }

        // In Terminal.Gui, resizing is typically handled automatically by the driver
        // We could potentially trigger a refresh or layout update here

        return Task.CompletedTask;
    }

    private void CreateMainWindow()
    {
        _mainWindow = new Toplevel
        {
            ColorScheme = Colors.TopLevel
        };

        // Create a simple UI
        var frame = new FrameView("Console Dungeon - Terminal.Gui v2")
        {
            X = 0,
            Y = 0,
            Width = Dim.Fill(),
            Height = Dim.Fill()
        };

        var statusLabel = new Label("WebSocket server integration - Plugin System Active")
        {
            X = 1,
            Y = 1,
            Width = Dim.Fill(1),
            Height = 1
        };

        var pluginInfo = new Label($"Loaded as plugin at {DateTime.Now:HH:mm:ss}")
        {
            X = 1,
            Y = 3,
            Width = Dim.Fill(1),
            Height = 1
        };

        var instructionLabel = new Label("This Terminal.Gui app is now running as a hot-reloadable plugin!")
        {
            X = 1,
            Y = 5,
            Width = Dim.Fill(1),
            Height = 1,
            ColorScheme = Colors.Dialog
        };

        var quitButton = new Button("Quit")
        {
            X = Pos.Center(),
            Y = Pos.Bottom(_mainWindow) - 3
        };

        quitButton.Clicked += () =>
        {
            _logger.LogInformation("Quit button clicked");
            Application.RequestStop();
        };

        frame.Add(statusLabel, pluginInfo, instructionLabel, quitButton);
        _mainWindow.Add(frame);

        // Handle key events
        _mainWindow.KeyPress += (e) =>
        {
            if (e.KeyEvent.Key == Key.q || e.KeyEvent.Key == Key.Q)
            {
                _logger.LogInformation("Quit key pressed");
                Application.RequestStop();
                e.Handled = true;
            }
        };

        // Send initial output event
        SendOutputEvent("Console Dungeon started successfully as plugin\n");
    }

    private void SendOutputEvent(string message)
    {
        var data = Encoding.UTF8.GetBytes(message);
        var args = new TerminalOutputEventArgs
        {
            Data = data,
            Timestamp = DateTimeOffset.UtcNow
        };
        OutputReceived?.Invoke(this, args);
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _logger.LogInformation("Disposing Console Dungeon application");
            
            try
            {
                if (_isRunning)
                {
                    StopAsync().Wait(TimeSpan.FromSeconds(5));
                }

                _cancellationTokenSource?.Dispose();
                
                // Shutdown Terminal.Gui
                Application.Shutdown();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during Console Dungeon disposal");
            }

            _disposed = true;
        }
    }
}

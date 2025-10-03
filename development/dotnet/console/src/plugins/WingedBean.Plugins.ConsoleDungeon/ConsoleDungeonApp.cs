using Microsoft.Extensions.Logging;
using System.Text;
using Terminal.Gui;
using WingedBean.Contracts;
using WingedBean.Contracts.Core;
using WingedBean.Contracts.Game;

namespace WingedBean.Plugins.ConsoleDungeon;

/// <summary>
/// Console Dungeon Terminal.Gui application
/// </summary>
[Plugin(
    Name = "ConsoleDungeonApp",
    Provides = new[] { typeof(ITerminalApp) },
    Priority = 50
)]
public class ConsoleDungeonApp : ITerminalApp, IDisposable
{
    private readonly ILogger<ConsoleDungeonApp> _logger;
    private bool _isRunning = false;
    private bool _disposed = false;
    private Window? _mainWindow;
    private CancellationTokenSource? _cancellationTokenSource;
    private TerminalAppConfig? _config;
    private IDungeonGameService? _gameService;
    private System.Timers.Timer? _uiTimer;
    private Label? _statusLabel;

    public event EventHandler<TerminalOutputEventArgs>? OutputReceived;
    public event EventHandler<TerminalExitEventArgs>? Exited;

    public ConsoleDungeonApp(ILogger<ConsoleDungeonApp> logger)
    {
        _logger = logger;
    }

    // Parameterless constructor for plugin loader instantiation
    public ConsoleDungeonApp() : this(new LoggerFactory().CreateLogger<ConsoleDungeonApp>())
    {
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

            // Wire gameplay service if provided
            if (_config?.Parameters != null && _config.Parameters.TryGetValue("gameService", out var svcObj))
            {
                _gameService = svcObj as IDungeonGameService;
                if (_gameService != null)
                {
                    _logger.LogInformation("Initializing DungeonGame via plugin service");
                    _gameService.Initialize();

                    // Start a simple 10 FPS tick to drive the game
                    _uiTimer = new System.Timers.Timer(100);
                    _uiTimer.Elapsed += (s, e) =>
                    {
                        try
                        {
                            _gameService.Update(0.1f);
                            if (_statusLabel != null)
                            {
                                _statusLabel.Text = $"Gameplay active - {DateTime.Now:HH:mm:ss}";
                            }
                            Application.Wakeup();
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Error during game update");
                        }
                    };
                    _uiTimer.Start();
                }
            }

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

            // Stop gameplay tick and shutdown
            if (_uiTimer != null)
            {
                _uiTimer.Stop();
                _uiTimer.Dispose();
                _uiTimer = null;
            }
            _gameService?.Shutdown();

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
        // Use Window (Terminal.Gui v2) with property initializers
        _mainWindow = new Window()
        {
            Title = "Console Dungeon - Terminal.Gui v2",
            BorderStyle = LineStyle.Single,
            X = 0,
            Y = 0,
            Width = Dim.Fill(),
            Height = Dim.Fill()
        };

        _statusLabel = new Label
        {
            X = 1,
            Y = 1,
            Text = "WebSocket server integration - Plugin System Active"
        };

        var pluginInfo = new Label
        {
            X = 1,
            Y = 3,
            Text = $"Loaded as plugin at {DateTime.Now:HH:mm:ss}"
        };

        var instructionLabel = new Label
        {
            X = 1,
            Y = 5,
            Text = "This Terminal.Gui v2 app is running via plugin architecture"
        };

        var quitButton = new Button
        {
            X = Pos.Center(),
            Y = Pos.AnchorEnd(2),
            Text = "_Quit",
            IsDefault = true
        };

        // v2: use Accepting event
        quitButton.Accepting += (s, e) => {
            _logger.LogInformation("Quit button pressed");
            Application.RequestStop();
        };

        _mainWindow.Add(_statusLabel, pluginInfo, instructionLabel, quitButton);

        // v2: KeyDown event
        _mainWindow.KeyDown += (sender, e) => {
            if (e.KeyCode == KeyCode.Q)
            {
                _logger.LogInformation("Quit key pressed");
                Application.RequestStop();
                e.Handled = true;
            }
        };

        // Send initial output event
        SendOutputEvent("Console Dungeon (v2) started as plugin\n");
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

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
    private readonly string _logFilePath;
    private bool _isRunning = false;
    private bool _disposed = false;
    private Window? _mainWindow;
    private CancellationTokenSource? _cancellationTokenSource;
    private TerminalAppConfig? _config;
    private IDungeonGameService? _gameService;
    private System.Timers.Timer? _uiTimer;
    private Label? _statusLabel;
    private Label? _gameStateLabel;
    private Label? _entityCountLabel;
    private TextView? _logView;
    private IDisposable? _statsSubscription;
    private readonly List<string> _logMessages = new();
    private bool _isRecording = false;

    public event EventHandler<TerminalOutputEventArgs>? OutputReceived;
    public event EventHandler<TerminalExitEventArgs>? Exited;

    public ConsoleDungeonApp(ILogger<ConsoleDungeonApp> logger)
    {
        _logger = logger;
        
        // Set up file-based logging (Terminal.Gui hides console output)
        var logsDir = Path.Combine(AppContext.BaseDirectory, "logs");
        Directory.CreateDirectory(logsDir);
        _logFilePath = Path.Combine(logsDir, $"console-dungeon-{DateTime.Now:yyyyMMdd-HHmmss}.log");
        
        LogToFile("=== ConsoleDungeonApp Constructor ===");
    }

    // Parameterless constructor for plugin loader instantiation
    public ConsoleDungeonApp() : this(new LoggerFactory().CreateLogger<ConsoleDungeonApp>())
    {
    }
    
    private void LogToFile(string message)
    {
        try
        {
            var timestamped = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] {message}";
            File.AppendAllText(_logFilePath, timestamped + Environment.NewLine);
            
            // Also add to in-TUI log view
            _logMessages.Add(timestamped);
            if (_logMessages.Count > 100) _logMessages.RemoveAt(0); // Keep last 100
            
            // Update log view if it exists
            if (_logView != null)
            {
                _logView.Text = string.Join(Environment.NewLine, _logMessages);
            }
        }
        catch
        {
            // Ignore logging errors
        }
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
                    LogToFile("=== DungeonGame Service Found ===");
                    _logger.LogInformation("=== DungeonGame Service Found ===");
                    LogToFile("Initializing DungeonGame via plugin service");
                    _logger.LogInformation("Initializing DungeonGame via plugin service");
                    
                    try
                    {
                        _gameService.Initialize();
                        LogToFile($"✓ Game initialized. State: {_gameService.CurrentState}");
                        LogToFile($"✓ Game mode: {_gameService.CurrentMode}");
                        LogToFile($"✓ World entity count: {_gameService.World?.EntityCount ?? 0}");
                        _logger.LogInformation($"✓ Game initialized. State: {_gameService.CurrentState}");
                        _logger.LogInformation($"✓ Game mode: {_gameService.CurrentMode}");
                        _logger.LogInformation($"✓ World entity count: {_gameService.World?.EntityCount ?? 0}");
                    }
                    catch (Exception ex)
                    {
                        LogToFile($"❌ Failed to initialize game service: {ex.Message}");
                        _logger.LogError(ex, "❌ Failed to initialize game service");
                    }

                    // Subscribe to game state observables
                    _statsSubscription = _gameService.PlayerStatsObservable.Subscribe(stats =>
                    {
                        LogToFile($"[Observable] Stats updated: HP={stats.CurrentHP}/{stats.MaxHP}");
                        _logger.LogInformation($"[Observable] Stats updated: HP={stats.CurrentHP}/{stats.MaxHP}");
                        if (_statusLabel != null)
                        {
                            var text = $"HP: {stats.CurrentHP}/{stats.MaxHP} | MP: {stats.CurrentMana}/{stats.MaxMana} | Lvl: {stats.Level} | XP: {stats.Experience}";
                            LogToFile($"[Observable] Setting statusLabel.Text = {text}");
                            _logger.LogInformation($"[Observable] Setting statusLabel.Text = {text}");
                            _statusLabel.Text = text;
                        }
                    });

                    // Start a simple 10 FPS tick to drive the game
                    _uiTimer = new System.Timers.Timer(100);
                    _uiTimer.Elapsed += (s, e) =>
                    {
                        try
                        {
                            _gameService.Update(0.1f);
                            
                            // Update UI labels directly (Terminal.Gui v2 auto-redraws on Text change)
                            if (_entityCountLabel != null && _gameService.World != null)
                            {
                                _entityCountLabel.Text = $"Entities in world: {_gameService.World.EntityCount}";
                            }
                            
                            if (_gameStateLabel != null)
                            {
                                _gameStateLabel.Text = $"Game State: {_gameService.CurrentState} | Mode: {_gameService.CurrentMode} | {DateTime.Now:HH:mm:ss}";
                            }
                        }
                        catch (Exception ex)
                        {
                            LogToFile($"Error during game update: {ex.Message}");
                            _logger.LogError(ex, "Error during game update");
                        }
                    };
                    _uiTimer.Start();
                    LogToFile("✓ Game update timer started (100ms interval)");
                    _logger.LogInformation("✓ Game update timer started (100ms interval)");
                }
                else
                {
                    LogToFile("gameService parameter was null after cast");
                    _logger.LogWarning("gameService parameter was null after cast");
                }
            }
            else
            {
                LogToFile("No gameService found in Parameters");
                _logger.LogWarning("No gameService found in Parameters");
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
            _statsSubscription?.Dispose();
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
        LogToFile("CreateMainWindow called");
        
        // Use Window (Terminal.Gui v2) with property initializers
        _mainWindow = new Window()
        {
            Title = "Console Dungeon - ECS Dungeon Crawler | F9=Record F10=Stop Q=Quit",
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
            Text = "Loading game stats..."
        };

        _gameStateLabel = new Label
        {
            X = 1,
            Y = 3,
            Text = "Game initializing..."
        };
        
        _entityCountLabel = new Label
        {
            X = 1,
            Y = 5,
            Text = "Entity count loading..."
        };

        var instructionLabel = new Label
        {
            X = 1,
            Y = 7,
            Text = "Dungeon game is running in the background (ECS systems active)"
        };
        
        var recordingLabel = new Label
        {
            X = 1,
            Y = 9,
            Text = "Press F9 to start recording, F10 to stop (Asciinema replay feature)"
        };
        
        // In-TUI log view at the bottom
        var logFrame = new FrameView()
        {
            Title = "Debug Log",
            X = 1,
            Y = 11,
            Width = Dim.Fill(1),
            Height = Dim.Fill(3),
            BorderStyle = LineStyle.Single
        };
        
        _logView = new TextView
        {
            X = 0,
            Y = 0,
            Width = Dim.Fill(),
            Height = Dim.Fill(),
            ReadOnly = true,
            Text = $"Log file: {_logFilePath}\n"
        };
        logFrame.Add(_logView);

        var quitButton = new Button
        {
            X = Pos.Center(),
            Y = Pos.AnchorEnd(1),
            Text = "_Quit",
            IsDefault = true
        };

        // v2: use Accepting event
        quitButton.Accepting += (s, e) => {
            LogToFile("Quit button pressed");
            _logger.LogInformation("Quit button pressed");
            Application.RequestStop();
        };

        _mainWindow.Add(_statusLabel, _gameStateLabel, _entityCountLabel, instructionLabel, recordingLabel, logFrame, quitButton);

        // v2: KeyDown event - handle Q, F9, F10
        _mainWindow.KeyDown += (sender, e) => {
            if (e.KeyCode == KeyCode.Q)
            {
                LogToFile("Q key pressed - quitting");
                _logger.LogInformation("Quit key pressed");
                Application.RequestStop();
                e.Handled = true;
            }
            else if (e.KeyCode == KeyCode.F9)
            {
                // Start asciinema recording via OSC sequence
                LogToFile("F9 pressed - starting asciinema recording");
                SendOSCSequence("\x1b]1337;StartRecording\x07");
                _isRecording = true;
                _mainWindow.Title = "Console Dungeon - 🔴 RECORDING | F10=Stop Q=Quit";
                e.Handled = true;
            }
            else if (e.KeyCode == KeyCode.F10)
            {
                // Stop asciinema recording via OSC sequence
                LogToFile("F10 pressed - stopping asciinema recording");
                SendOSCSequence("\x1b]1337;StopRecording\x07");
                _isRecording = false;
                _mainWindow.Title = "Console Dungeon - ECS Dungeon Crawler | F9=Record F10=Stop Q=Quit";
                e.Handled = true;
            }
        };

        // Send initial output event
        SendOutputEvent("Console Dungeon (ECS) started - Dungeon gameplay is running!\n");
        LogToFile("CreateMainWindow completed");
    }
    
    private void SendOSCSequence(string sequence)
    {
        try
        {
            // Send OSC sequence directly to stdout (PTY will capture it)
            Console.Write(sequence);
            Console.Out.Flush();
            LogToFile($"Sent OSC sequence: {sequence.Replace("\x1b", "ESC").Replace("\x07", "BEL")}");
        }
        catch (Exception ex)
        {
            LogToFile($"Error sending OSC sequence: {ex.Message}");
        }
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

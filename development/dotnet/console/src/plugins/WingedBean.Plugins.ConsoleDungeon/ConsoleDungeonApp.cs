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
    private IRegistry? _registry;
    private IDungeonGameService? _gameService;
    private IRenderService? _renderService;
    private IGameUIService? _uiService;
    private System.Timers.Timer? _uiTimer;
    private Label? _statusLabel;
    private TextView? _gameWorldView;
    private IDisposable? _statsSubscription;
    private IDisposable? _entitiesSubscription;
    private IDisposable? _inputSubscription;
    private IReadOnlyList<EntitySnapshot>? _currentEntities;
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
            
            // Keep log messages in memory (for debugging)
            _logMessages.Add(timestamped);
            if (_logMessages.Count > 100) _logMessages.RemoveAt(0); // Keep last 100
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

            // Get registry from config if available
            if (_config?.Parameters != null && _config.Parameters.TryGetValue("registry", out var regObj))
            {
                _registry = regObj as IRegistry;
            }

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
                    
                    // Inject services from registry
                    if (_registry != null)
                    {
                        try
                        {
                            _renderService = _registry.Get<IRenderService>();
                            // Disable color mode - Terminal.Gui TextView doesn't support ANSI codes
                            _renderService.SetRenderMode(RenderMode.ASCII);
                            LogToFile("✓ IRenderService injected from registry (ASCII mode)");
                            _logger.LogInformation("✓ IRenderService injected from registry (ASCII mode)");
                        }
                        catch (Exception ex)
                        {
                            LogToFile($"⚠️ IRenderService not available: {ex.Message}");
                            _logger.LogWarning(ex, "IRenderService not available");
                        }

                        try
                        {
                            _uiService = _registry.Get<IGameUIService>();
                            _uiService.Initialize(_mainWindow!);
                            LogToFile("✓ IGameUIService injected and initialized");
                            _logger.LogInformation("✓ IGameUIService injected and initialized");
                        }
                        catch (Exception ex)
                        {
                            LogToFile($"⚠️ IGameUIService not available: {ex.Message}");
                            _logger.LogWarning(ex, "IGameUIService not available");
                        }
                    }
                    
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
                        if (_statusLabel != null)
                        {
                            var text = $"HP: {stats.CurrentHP}/{stats.MaxHP} | MP: {stats.CurrentMana}/{stats.MaxMana} | Lvl: {stats.Level} | XP: {stats.Experience} | M=Menu";
                            Application.Invoke(() =>
                            {
                                _statusLabel.Text = text;
                                // In Terminal.Gui v2, SetNeedsDisplay() is automatically called when Text changes
                            });
                        }
                    });
                    
                    // Subscribe to entities observable for rendering
                    _entitiesSubscription = _gameService.EntitiesObservable.Subscribe(entities =>
                    {
                        _currentEntities = entities;
                        LogToFile($"[Observable] Entities updated: {entities.Count} entities");
                    });

                    // Subscribe to UI service input events
                    if (_uiService != null)
                    {
                        _inputSubscription = _uiService.InputObservable.Subscribe(inputEvent =>
                        {
                            HandleGameInput(inputEvent);
                        });
                        LogToFile("✓ Subscribed to UI service input events");
                    }

                    // Start a simple 10 FPS tick to drive the game
                    _uiTimer = new System.Timers.Timer(100);
                    _uiTimer.Elapsed += (s, e) =>
                    {
                        try
                        {
                            _gameService.Update(0.1f);
                            
                            // Update game world view with rendered entities via service
                            if (_gameWorldView != null && _renderService != null && _currentEntities != null)
                            {
                                var buffer = _renderService.Render(_currentEntities, 60, 18);
                                var text = buffer.ToText();
                                
                                // ✅ Marshal UI update to main thread (Terminal.Gui v2)
                                Application.Invoke(() => 
                                {
                                    _gameWorldView.Text = text;
                                    LogToFile($"[Render] Updated view with {_currentEntities.Count} entities");
                                });
                            }
                        }
                        catch (Exception ex)
                        {
                            LogToFile($"Error during game update: {ex.Message}\n{ex.StackTrace}");
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
            _entitiesSubscription?.Dispose();
            _inputSubscription?.Dispose();
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
            Title = "Console Dungeon - ECS Dungeon Crawler | M=Menu",
            BorderStyle = LineStyle.Single,
            X = 0,
            Y = 0,
            Width = Dim.Fill(),
            Height = Dim.Fill()
        };

        // Game world view (full width, 90% height)
        _gameWorldView = new TextView
        {
            X = 0,
            Y = 0,
            Width = Dim.Fill(),
            Height = Dim.Percent(90),
            ReadOnly = true,
            Text = "Game world initializing..."
        };
        
        // Status bar at the bottom (10% height)
        _statusLabel = new Label
        {
            X = 0,
            Y = Pos.AnchorEnd(1),
            Width = Dim.Fill(),
            Text = "Loading game stats... | M=Menu"
        };
        
        _mainWindow.Add(_gameWorldView, _statusLabel);

        // Handle keyboard input - map to game events
        _mainWindow.KeyDown += HandleKeyInput;

        // Send initial output event
        SendOutputEvent("Console Dungeon (ECS) started - Use arrow keys to move, M for menu!\n");
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
    
    private void HandleKeyInput(object? sender, Key e)
    {
        // Log the key for debugging
        LogToFile($"KeyDown: KeyCode={e.KeyCode}");
        
        // Map Terminal.Gui keys to GameInputEvents
        var inputType = MapKeyToGameInput(e);
        
        if (inputType.HasValue)
        {
            var inputEvent = new GameInputEvent(inputType.Value, DateTimeOffset.UtcNow);
            
            // Handle the input directly
            HandleGameInput(inputEvent);
            
            // Mark as handled to prevent Terminal.Gui navigation
            e.Handled = true;
            LogToFile($"Handled game input: {inputType.Value}");
        }
    }
    
    private GameInputType? MapKeyToGameInput(Key key)
    {
        // Check KeyCode first for special keys (arrows, space)
        var fromKeyCode = key.KeyCode switch
        {
            KeyCode.CursorUp => GameInputType.MoveUp,
            KeyCode.CursorDown => GameInputType.MoveDown,
            KeyCode.CursorLeft => GameInputType.MoveLeft,
            KeyCode.CursorRight => GameInputType.MoveRight,
            KeyCode.Space => GameInputType.Attack,
            _ => (GameInputType?)null
        };
        
        if (fromKeyCode.HasValue)
            return fromKeyCode;
        
        // Check character for letter keys (case-insensitive)
        // Get the rune value and convert to char
        var rune = key.AsRune;
        if (rune.Value >= 32 && rune.Value < 127) // Printable ASCII
        {
            var ch = char.ToUpper((char)rune.Value);
            return ch switch
            {
                'W' => GameInputType.MoveUp,
                'S' => GameInputType.MoveDown,
                'A' => GameInputType.MoveLeft,
                'D' => GameInputType.MoveRight,
                'E' => GameInputType.Use,
                'G' => GameInputType.Pickup,
                'M' => GameInputType.ToggleMenu,
                'I' => GameInputType.ToggleInventory,
                'Q' => GameInputType.Quit,
                _ => null
            };
        }
        
        return null;
    }
    
    private void HandleGameInput(GameInputEvent inputEvent)
    {
        LogToFile($"Game input received: {inputEvent.Type}");
        
        switch (inputEvent.Type)
        {
            case GameInputType.ToggleMenu:
                if (_uiService != null)
                {
                    if (_uiService.IsMenuVisible)
                    {
                        _uiService.HideMenu();
                    }
                    else
                    {
                        _uiService.ShowMenu(MenuType.Main);
                    }
                }
                break;
                
            case GameInputType.ToggleInventory:
                _uiService?.ShowMenu(MenuType.Inventory);
                break;
                
            case GameInputType.Quit:
                LogToFile("Quit requested via game input");
                Application.RequestStop();
                break;
                
            default:
                // Forward movement and action inputs to game service
                if (_gameService != null)
                {
                    var gameInput = MapToGameInput(inputEvent.Type);
                    _gameService.HandleInput(gameInput);
                }
                break;
        }
    }
    
    private GameInput MapToGameInput(GameInputType inputType)
    {
        return inputType switch
        {
            GameInputType.MoveUp => new GameInput(InputType.MoveUp),
            GameInputType.MoveDown => new GameInput(InputType.MoveDown),
            GameInputType.MoveLeft => new GameInput(InputType.MoveLeft),
            GameInputType.MoveRight => new GameInput(InputType.MoveRight),
            GameInputType.Attack => new GameInput(InputType.Attack),
            GameInputType.Use => new GameInput(InputType.UseItem),
            GameInputType.Pickup => new GameInput(InputType.UseItem), // Map pickup to use item for now
            _ => new GameInput(InputType.Quit) // Default fallback
        };
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

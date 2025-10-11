using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Plate.CrossMilo.Contracts;
using Plate.CrossMilo.Contracts.Terminal;
using Plate.PluginManoi.Contracts;
using ConsoleDungeon.Contracts;
using Plate.CrossMilo.Contracts.Input;
using Plate.CrossMilo.Contracts.Scene;
using Plate.CrossMilo.Contracts.Scene.Services;
using WingedBean.Plugins.ConsoleDungeon.Input;
using WingedBean.Plugins.ConsoleDungeon.Scene;
using WingedBean.Plugins.TerminalUI;
using System.IO;
using Terminal.Gui;

// Type aliases for IService pattern
using ISceneService = Plate.CrossMilo.Contracts.Scene.Services.IService;
using IInputRouter = Plate.CrossMilo.Contracts.Input.Router.IService;
using IInputMapper = Plate.CrossMilo.Contracts.Input.Mapper.IService;
using IAudioService = Plate.CrossMilo.Contracts.Audio.Services.IService;

namespace WingedBean.Plugins.ConsoleDungeon;

/// <summary>
/// Refactored Console Dungeon application (RFC-0020 & RFC-0021).
/// Clean separation: NO Terminal.Gui dependencies, uses ISceneService and IInputRouter.
/// ~150 lines vs original 853 lines.
/// </summary>
[Plugin(
    Name = "ConsoleDungeonAppRefactored",
    Provides = new[] { typeof(ITerminalApp) },
    Priority = 51  // Higher priority than original
)]
public class ConsoleDungeonAppRefactored : ITerminalApp, IRegistryAware, IDisposable
{
    private readonly ILogger<ConsoleDungeonAppRefactored> _logger;
    private TerminalAppConfig _config;
    private IDungeonService? _gameService;
    private IRegistry? _registry;
    private ISceneService? _sceneService;
    private Microsoft.Extensions.Hosting.IHostApplicationLifetime? _hostLifetime;
    // TODO: RenderService removed from framework - using inline rendering in TerminalGuiSceneProvider
    // private IRenderService? _renderService;
    private IInputRouter? _inputRouter;
    private IInputMapper? _inputMapper;
    private IDisposable? _gameplayScope;
    private IDisposable? _entitiesSubscription;
    private IDisposable? _statsSubscription;
    private System.Timers.Timer? _gameTimer;
    private bool _isRunning = false;
    private bool _disposed = false;

    public event EventHandler<TerminalOutputEventArgs>? OutputReceived;
    public event EventHandler<TerminalExitEventArgs>? Exited;

    // IWingedBeanApp members
    public string Name => _config?.Name ?? "Console Dungeon";
    public Plate.CrossMilo.Contracts.Hosting.App.AppState State { get; private set; } = Plate.CrossMilo.Contracts.Hosting.App.AppState.NotStarted;
    public event EventHandler<Plate.CrossMilo.Contracts.Hosting.App.AppStateChangedEventArgs>? StateChanged;

    // IUIApp members  
    public event EventHandler<Plate.CrossMilo.Contracts.UI.App.UIEventArgs>? UIEvent;

    public ConsoleDungeonAppRefactored(
        ILogger<ConsoleDungeonAppRefactored> logger,
        IOptions<TerminalAppConfig> config,
        IDungeonService gameService,
        IRegistry registry)
    {
        _logger = logger;
        _config = config.Value;
        _gameService = gameService;
        _registry = registry;
    }

    // Parameterless constructor for plugin loader
    public ConsoleDungeonAppRefactored()
    {
        _logger = new LoggerFactory().CreateLogger<ConsoleDungeonAppRefactored>();
        // Config will be set by SetRegistry
        _config = new TerminalAppConfig { Name = "Console Dungeon", Cols = 80, Rows = 24 };
    }

    // IRegistryAware.SetRegistry - called by plugin loader (RFC-0038)
    public void SetRegistry(IRegistry registry)
    {
        _registry = registry;
        
        // Resolve configuration from registry (registered by host)
        _config = registry.IsRegistered<TerminalAppConfig>()
            ? registry.Get<TerminalAppConfig>()
            : new TerminalAppConfig { Name = "Console Dungeon", Cols = 80, Rows = 24 };
        
        // Try to resolve game service from registry
        _gameService = registry.IsRegistered<IDungeonService>()
            ? registry.Get<IDungeonService>()
            : null;
        
        Diag($"SetRegistry called: config={_config?.Name}, gameService={_gameService != null}");
    }

    // IHostedService.StartAsync - no config parameter needed
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("========================================");
        _logger.LogInformation("ConsoleDungeonAppRefactored.StartAsync CALLED");
        _logger.LogInformation("========================================");
        Diag("StartAsync invoked");
        if (_isRunning)
        {
            _logger.LogWarning("Console Dungeon is already running");
            return;
        }

        _logger.LogInformation("Starting Console Dungeon: {Name} ({Cols}x{Rows})", 
            _config.Name, _config.Cols, _config.Rows);

        try
        {
            // Using inline rendering (RenderService removed from framework)
            _logger.LogInformation("Using inline rendering (RenderService removed from framework)");

            // Create input infrastructure
            _inputMapper = new DefaultInputMapper();
            _inputRouter = new DefaultInputRouter();

            // Resolve Audio service from registry (optional)
            _logger.LogInformation("Attempting to resolve IAudioService from registry...");
            IAudioService? audioService = null;
            try
            {
                if (_registry != null)
                {
                    audioService = _registry.Get<IAudioService>();
                    if (audioService != null)
                    {
                        _logger.LogInformation("========================================");
                        _logger.LogInformation("✓ AUDIO SERVICE RESOLVED SUCCESSFULLY!");
                        _logger.LogInformation("========================================");
                    }
                    else
                    {
                        _logger.LogWarning("========================================");
                        _logger.LogWarning("❌ AUDIO SERVICE IS NULL AFTER Get<IAudioService>()");
                        _logger.LogWarning("========================================");
                    }
                }
                else
                {
                    _logger.LogWarning("Registry is null - cannot resolve IAudioService");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "========================================");
                _logger.LogError(ex, "❌ EXCEPTION RESOLVING AUDIO SERVICE: {Message}", ex.Message);
                _logger.LogError(ex, "========================================");
            }

            // Create scene service (without render service - using inline rendering)
            _sceneService = new TerminalGuiSceneProvider(_inputMapper, _inputRouter, audioService);
            _sceneService.Initialize();
            Diag("Scene initialized");

            if (_gameService == null)
            {
                // Try to resolve from registry when not injected by DI
                try
                {
                    if (_registry != null)
                    {
                        _gameService = _registry.Get<IDungeonService>();
                        _logger.LogInformation("✓ IDungeonService resolved from registry");
                        Diag("IDungeonService resolved from registry");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "IDungeonService not available");
                    Diag("Abort: IDungeonService is null");
                    return;
                }
            }

            // Register gameplay input scope
            var gameplayScope = new GameplayInputScope(_gameService, _logger);
            _gameplayScope = _inputRouter.PushScope(gameplayScope);
            _logger.LogInformation("✓ Gameplay input scope registered");
            Diag("Gameplay input scope registered");

            // Subscribe to entity updates for rendering
            _entitiesSubscription = _gameService.EntitiesObservable.Subscribe(entities =>
            {
                _sceneService?.UpdateWorld(entities);
            });

            // Subscribe to stats updates for status display
            _statsSubscription = _gameService.PlayerStatsObservable.Subscribe(stats =>
            {
                var statusText = $"HP: {stats.CurrentHP}/{stats.MaxHP} | MP: {stats.CurrentMana}/{stats.MaxMana} | Lvl: {stats.Level} | XP: {stats.Experience} | M=Menu";
                (_sceneService as TerminalGuiSceneProvider)?.UpdateStatus(statusText);
            });

            // Resolve IHostApplicationLifetime early for clean shutdown
            if (_hostLifetime == null && _registry != null)
            {
                try
                {
                    _hostLifetime = _registry.Get<Microsoft.Extensions.Hosting.IHostApplicationLifetime>();
                    Diag("IHostApplicationLifetime resolved from registry");
                }
                catch (Exception ex)
                {
                    Diag($"Could not resolve IHostApplicationLifetime: {ex.Message}");
                }
            }

            // Handle scene shutdown
            _sceneService.Shutdown += (s, e) =>
            {
                _logger.LogInformation("Scene shutdown requested");
                _gameService.Shutdown();
                // Note: Don't set _isRunning = false here, let StopAsync handle it
            };

            // Initialize game
            _gameService.Initialize();
            _logger.LogInformation($"✓ Game initialized. State: {_gameService.CurrentState}");
            Diag($"Game initialized. State: {_gameService.CurrentState}");

            // Trigger initial render
            try
            {
                _gameService.Update(0.0f);
                Diag("Initial game update/render triggered");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during initial game update");
                Diag($"Initial update error: {ex.Message}");
            }

            // Start game update timer (10 FPS)
            var updateCount = 0;
            _gameTimer = new System.Timers.Timer(100);
            _gameTimer.Elapsed += (s, e) =>
            {
                try
                {
                    _gameService?.Update(0.1f);
                    updateCount++;
                    // Game update diagnostics written to file only (every 50th update)
                    if (updateCount == 1 || updateCount % 50 == 0)
                    {
                        Diag($"Game update #{updateCount}");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error during game update");
                }
            };
            _gameTimer.Start();
            _logger.LogInformation("✓ Game update timer started");
            Diag("Game timer started; entering UI loop");

            _isRunning = true;

            // Run scene in background (don't await - it will signal shutdown when done)
            _ = Task.Run(async () =>
            {
                var suppressShutdown = false;
                try
                {
                    // Capture driver info to determine if we are on a real TTY
                    var driverName = Application.Driver?.GetType().Name ?? "(null)";
                    var isHeadlessDriver = string.Equals(driverName, "FakeDriver", StringComparison.OrdinalIgnoreCase)
                                           || string.Equals(driverName, "HeadlessDriver", StringComparison.OrdinalIgnoreCase);

                    var runStart = DateTime.UtcNow;
                    Diag($"Entering scene.Run() [Driver={driverName}, Headless={isHeadlessDriver}]");
                    _sceneService.Run();
                    var ranFor = DateTime.UtcNow - runStart;
                    Diag($"scene.Run() returned after {ranFor.TotalMilliseconds:F0}ms");

                    // If Terminal.Gui failed to actually start (no TTY / fake driver)
                    // Application.Run tends to return immediately. In that case we should
                    // NOT stop the host — keep the process alive so a PTY-attached run
                    // (via the web UI) can launch correctly.
                    // Treat very-fast returns as failed UI init; keep process alive for PTY attach
                    var fastReturnThreshold = TimeSpan.FromSeconds(2);
                    if (isHeadlessDriver || ranFor < fastReturnThreshold)
                    {
                        Diag("Detected headless or fast-return UI; suppressing host shutdown and keeping process alive");

                        try
                        {
                            // Wait for host shutdown signal to exit cleanly
                            if (_hostLifetime != null)
                            {
                                await Task.Run(() => _hostLifetime.ApplicationStopping.WaitHandle.WaitOne());
                                Diag("ApplicationStopping signaled; exiting headless keepalive");
                            }
                        }
                        catch (Exception ex)
                        {
                            Diag($"Error during headless keepalive wait: {ex.Message}");
                        }
                        // Skip normal shutdown signaling below
                        suppressShutdown = true;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error running scene");
                    Diag($"Error in scene.Run: {ex.Message}");
                    
                    // If scene.Run() throws an exception, treat it like a headless/fast-return scenario
                    // Keep the process alive rather than shutting down the host
                    Diag("Exception during scene.Run(); treating as headless mode - keeping process alive");
                    suppressShutdown = true;
                    
                    // Wait for host shutdown signal
                    try
                    {
                        if (_hostLifetime != null)
                        {
                            await Task.Run(() => _hostLifetime.ApplicationStopping.WaitHandle.WaitOne());
                            Diag("ApplicationStopping signaled after scene error");
                        }
                    }
                    catch (Exception waitEx)
                    {
                        Diag($"Error during keepalive wait after scene error: {waitEx.Message}");
                    }
                }
                finally
                {
                    Diag("UI loop finished");
                    if (!suppressShutdown)
                    {
                        // Stop game timer immediately to prevent further updates
                        try
                        {
                            _gameTimer?.Stop();
                            _gameTimer?.Dispose();
                            _gameTimer = null;
                            Diag("Game timer stopped");
                        }
                        catch (Exception ex)
                        {
                            Diag($"Error stopping timer: {ex.Message}");
                        }
                        
                        Exited?.Invoke(this, new TerminalExitEventArgs
                        {
                            ExitCode = 0,
                            Timestamp = DateTimeOffset.UtcNow
                        });

                        // Decide whether to terminate the host
                        var exitEnv = (Environment.GetEnvironmentVariable("DUNGEON_EXIT_ON_UI") ?? string.Empty).ToLowerInvariant();
                        var shouldExitHost = exitEnv == "1" || exitEnv == "true";
                        if (shouldExitHost && _hostLifetime != null)
                        {
                            Diag("Env DUNGEON_EXIT_ON_UI enabled; requesting host shutdown");
                            _hostLifetime.StopApplication();
                        }
                        else
                        {
                            Diag("Preserving host process (no StopApplication)");
                        }
                    }
                    else
                    {
                        Diag("Headless mode: skipping game timer stop and host shutdown");
                    }
                }
            }, cancellationToken);
            
            Diag("StartAsync returning immediately (UI running in background)");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to start Console Dungeon");
            _isRunning = false;
            Diag($"Failed to start: {ex.Message}");
            throw;
        }
        
        Diag("StartAsync completed, returning to host");
    }

    private void Diag(string msg)
    {
        try
        {
            var dir = Path.Combine(AppContext.BaseDirectory, "logs");
            Directory.CreateDirectory(dir);
            var line = $"[{DateTimeOffset.Now:HH:mm:ss}] [ConsoleDungeonApp] {msg}\n";
            File.AppendAllText(Path.Combine(dir, "ui-diag.log"), line);
            // Console output removed - use log file only to avoid interfering with Terminal.Gui
            // Console.Write(line);
            // Console.Out.Flush();
        }
        catch { }
    }

    public async Task StopAsync(CancellationToken ct = default)
    {
        Diag("StopAsync called");
        if (!_isRunning)
        {
            Diag("Already stopped, returning");
            return;
        }

        _logger.LogInformation("Stopping Console Dungeon...");
        Diag("Stopping game services...");

        try
        {
            _gameTimer?.Stop();
            _gameTimer?.Dispose();
            _gameplayScope?.Dispose();
            _entitiesSubscription?.Dispose();
            _statsSubscription?.Dispose();
            _gameService?.Shutdown();

            (_inputMapper as IDisposable)?.Dispose();

            _isRunning = false;
            _logger.LogInformation("Console Dungeon stopped");
            Diag("Console Dungeon stopped successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error stopping Console Dungeon");
        }

        await Task.CompletedTask;
    }

    public Task SendInputAsync(byte[] data, CancellationToken ct = default)
    {
        // Input is now handled via IInputRouter through Terminal.Gui KeyDown events
        return Task.CompletedTask;
    }

    // ITerminalApp.SendRawInputAsync - send raw terminal input
    public Task SendRawInputAsync(byte[] data, CancellationToken ct = default)
    {
        return SendInputAsync(data, ct);
    }

    // ITerminalApp.SetCursorPositionAsync - set cursor position
    public Task SetCursorPositionAsync(int x, int y, CancellationToken ct = default)
    {
        // Terminal.Gui manages cursor position automatically
        return Task.CompletedTask;
    }

    // ITerminalApp.WriteAnsiAsync - write ANSI escape sequences
    public Task WriteAnsiAsync(string ansiSequence, CancellationToken ct = default)
    {
        // Terminal.Gui handles ANSI rendering
        return Task.CompletedTask;
    }

    // IUIApp.HandleInputAsync - handle platform-agnostic input
    public Task HandleInputAsync(Plate.CrossMilo.Contracts.UI.App.InputEvent input, CancellationToken cancellationToken = default)
    {
        // Terminal-specific input handling is done via SendRawInputAsync and Terminal.Gui events
        // This method can be used for cross-platform input events if needed
        return Task.CompletedTask;
    }

    // IUIApp.RenderAsync - render current frame
    public Task RenderAsync(CancellationToken cancellationToken = default)
    {
        // Terminal.Gui handles rendering automatically via its main loop
        // Custom rendering is done via the GameWorldView control
        return Task.CompletedTask;
    }

    // ITerminalApp.ResizeAsync - override for terminal-specific resize
    public Task ResizeAsync(int cols, int rows, CancellationToken ct = default)
    {
        _logger.LogInformation("Resize request: {Cols}x{Rows}", cols, rows);
        // Terminal.Gui handles resizing automatically
        return Task.CompletedTask;
    }

    // IUIApp.ResizeAsync - platform-agnostic resize (width/height in pixels or logical units)
    Task Plate.CrossMilo.Contracts.UI.App.IService.ResizeAsync(int width, int height, CancellationToken cancellationToken)
    {
        // Convert to cols/rows for terminal (assuming character size)
        return ResizeAsync(width, height, cancellationToken);
    }

    public void Dispose()
    {
        if (_disposed) return;

        _gameTimer?.Dispose();
        _gameplayScope?.Dispose();
        _entitiesSubscription?.Dispose();
        _statsSubscription?.Dispose();
        (_inputMapper as IDisposable)?.Dispose();

        _disposed = true;
    }
}

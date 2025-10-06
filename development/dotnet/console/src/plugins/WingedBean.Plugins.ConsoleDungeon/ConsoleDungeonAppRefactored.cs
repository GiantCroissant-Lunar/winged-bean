using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using WingedBean.Contracts.Terminal;
using WingedBean.Contracts.Core;
using WingedBean.Contracts.Game;
using WingedBean.Contracts.Input;
using WingedBean.Contracts.Scene;
using WingedBean.Plugins.ConsoleDungeon.Input;
using WingedBean.Plugins.ConsoleDungeon.Scene;
using System.IO;

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
    private IDungeonGameService? _gameService;
    private IRegistry? _registry;
    private ISceneService? _sceneService;
    private Microsoft.Extensions.Hosting.IHostApplicationLifetime? _hostLifetime;
    private IRenderService? _renderService;
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

    public ConsoleDungeonAppRefactored(
        ILogger<ConsoleDungeonAppRefactored> logger,
        IOptions<TerminalAppConfig> config,
        IDungeonGameService gameService,
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
        _gameService = registry.IsRegistered<IDungeonGameService>()
            ? registry.Get<IDungeonGameService>()
            : null;
        
        Diag($"SetRegistry called: config={_config?.Name}, gameService={_gameService != null}");
    }

    // IHostedService.StartAsync - no config parameter needed
    public async Task StartAsync(CancellationToken cancellationToken)
    {
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
            // Get render service from registry
            {
                try
                {
                    if (_registry == null)
                        throw new InvalidOperationException("Registry is not available");
                    _renderService = _registry.Get<IRenderService>();
                    _renderService.SetRenderMode(RenderMode.ASCII);
                    _logger.LogInformation("✓ IRenderService injected (ASCII mode)");
                    Diag("IRenderService ready (ASCII mode)");
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "IRenderService not available");
                    Diag($"IRenderService not available: {ex.Message}");
                }
            }

            // Create input infrastructure
            _inputMapper = new DefaultInputMapper();
            _inputRouter = new DefaultInputRouter();

            // Create scene service
            if (_renderService == null)
            {
                _logger.LogError("Cannot create scene without render service");
                try { System.Console.WriteLine("[ConsoleDungeonApp] Abort: render service is null"); } catch { }
                return;
            }

            _sceneService = new TerminalGuiSceneProvider(_renderService, _inputMapper, _inputRouter);
            _sceneService.Initialize();
            Diag("Scene initialized");

            if (_gameService == null)
            {
                // Try to resolve from registry when not injected by DI
                try
                {
                    if (_registry != null)
                    {
                        _gameService = _registry.Get<IDungeonGameService>();
                        _logger.LogInformation("✓ IDungeonGameService resolved from registry");
                        Diag("IDungeonGameService resolved from registry");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "IDungeonGameService not available");
                    Diag("Abort: IDungeonGameService is null");
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
            _ = Task.Run(() =>
            {
                try
                {
                    Diag("Entering scene.Run()");
                    _sceneService.Run();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error running scene");
                    Diag($"Error in scene.Run: {ex.Message}");
                }
                finally
                {
                    Diag("UI loop finished");
                    
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
                    
                    // Request host shutdown (which will call StopAsync to complete cleanup)
                    if (_hostLifetime != null)
                    {
                        Diag("Requesting host shutdown via IHostApplicationLifetime.StopApplication()");
                        _hostLifetime.StopApplication();
                    }
                    else
                    {
                        Diag("WARNING: IHostApplicationLifetime not available, host will not shutdown automatically");
                        _isRunning = false;  // Only set here if we can't trigger proper shutdown
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
            Console.Write(line);
            Console.Out.Flush();
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

    public Task ResizeAsync(int cols, int rows, CancellationToken ct = default)
    {
        _logger.LogInformation("Resize request: {Cols}x{Rows}", cols, rows);
        // Terminal.Gui handles resizing automatically
        return Task.CompletedTask;
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

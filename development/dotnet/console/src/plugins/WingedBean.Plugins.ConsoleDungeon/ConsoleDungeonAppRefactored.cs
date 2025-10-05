using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using WingedBean.Contracts.Terminal;
using WingedBean.Contracts.Core;
using WingedBean.Contracts.Game;
using WingedBean.Contracts.Input;
using WingedBean.Contracts.Scene;
using WingedBean.Plugins.ConsoleDungeon.Input;
using WingedBean.Plugins.ConsoleDungeon.Scene;

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
public class ConsoleDungeonAppRefactored : ITerminalApp, IDisposable
{
    private readonly ILogger<ConsoleDungeonAppRefactored> _logger;
    private readonly TerminalAppConfig _config;
    private readonly IDungeonGameService _gameService;
    private readonly IRegistry _registry;
    private ISceneService? _sceneService;
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

    // Parameterless constructor for plugin loader (legacy compatibility)
    public ConsoleDungeonAppRefactored() : this(
        new LoggerFactory().CreateLogger<ConsoleDungeonAppRefactored>(),
        Options.Create(new TerminalAppConfig()),
        null!, // This will cause issues - plugin loader should use DI
        null!) // This will cause issues - plugin loader should use DI
    {
    }

    // IHostedService.StartAsync - no config parameter needed
    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
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
                    _renderService = _registry.Get<IRenderService>();
                    _renderService.SetRenderMode(RenderMode.ASCII);
                    _logger.LogInformation("✓ IRenderService injected (ASCII mode)");
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "IRenderService not available");
                }
            }

            // Create input infrastructure
            _inputMapper = new DefaultInputMapper();
            _inputRouter = new DefaultInputRouter();

            // Create scene service
            if (_renderService == null)
            {
                _logger.LogError("Cannot create scene without render service");
                return;
            }

            _sceneService = new TerminalGuiSceneProvider(_renderService, _inputMapper, _inputRouter);
            _sceneService.Initialize();

            // Register gameplay input scope
            var gameplayScope = new GameplayInputScope(_gameService, _logger);
            _gameplayScope = _inputRouter.PushScope(gameplayScope);
            _logger.LogInformation("✓ Gameplay input scope registered");

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

            // Handle scene shutdown
            _sceneService.Shutdown += (s, e) =>
            {
                _logger.LogInformation("Scene shutdown requested");
                _gameService.Shutdown();
                _isRunning = false;
            };

            // Initialize game
            _gameService.Initialize();
            _logger.LogInformation($"✓ Game initialized. State: {_gameService.CurrentState}");

            // Start game update timer (10 FPS)
            _gameTimer = new System.Timers.Timer(100);
            _gameTimer.Elapsed += (s, e) =>
            {
                try
                {
                    _gameService?.Update(0.1f);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error during game update");
                }
            };
            _gameTimer.Start();
            _logger.LogInformation("✓ Game update timer started");

            _isRunning = true;

            // Run scene (blocks until UI closes)
            await Task.Run(() =>
            {
                try
                {
                    _sceneService.Run();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error running scene");
                }
                finally
                {
                    _isRunning = false;
                    Exited?.Invoke(this, new TerminalExitEventArgs
                    {
                        ExitCode = 0,
                        Timestamp = DateTimeOffset.UtcNow
                    });
                }
            }, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to start Console Dungeon");
            _isRunning = false;
            throw;
        }
    }

    public async Task StopAsync(CancellationToken ct = default)
    {
        if (!_isRunning)
        {
            return;
        }

        _logger.LogInformation("Stopping Console Dungeon...");

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

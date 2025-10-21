using System;
using System.Threading;
using System.Threading.Tasks;
using ConsoleDungeon.Contracts;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Plate.CrossMilo.Contracts.Capability;
using Plate.CrossMilo.Contracts.Goap;
using Plate.CrossMilo.Contracts.UI.App;
using SadConsole;
using SadConsole.Configuration;
using WingedBean.Contracts.Gameplay;
using IUIAppService = Plate.CrossMilo.Contracts.UI.App.IService;
using RecorderService = Plate.CrossMilo.Contracts.Recorder.Services.IService;
using WingedBeanGameplay = WingedBean.Contracts.Gameplay.IService;

namespace WingedBean.Plugins.SadConsole;

/// <summary>
/// SadConsole-based UI application.
/// Implements IService (IUIApp) for desktop window rendering.
/// </summary>
public class SadConsoleApp : IUIAppService
{
    private readonly ILogger<SadConsoleApp>? _logger;
    private Game? _game;
    private DungeonGameScreen? _gameScreen;
    private CancellationTokenSource? _runCts;
    private Plate.CrossMilo.Contracts.Hosting.App.AppState _state = Plate.CrossMilo.Contracts.Hosting.App.AppState.NotStarted;
    private readonly IDungeonService? _dungeon;
    private readonly WingedBeanGameplay? _gameplay;
    private readonly IWorldSnapshotProvider? _worldProvider;
    private readonly RecorderService? _recorder;
    private readonly IGoapPlannerService? _planner;
    private readonly ICapabilityEvaluator? _capability;
    private bool _plannerDemoRan;

    public string Name => "SadConsole UI";
    public Plate.CrossMilo.Contracts.Hosting.App.AppState State => _state;
    public event EventHandler<Plate.CrossMilo.Contracts.Hosting.App.AppStateChangedEventArgs>? StateChanged;
    public event EventHandler<UIEventArgs>? UIEvent;

    public SadConsoleApp(ILogger<SadConsoleApp>? logger = null, IDungeonService? dungeon = null, WingedBeanGameplay? gameplay = null, IWorldSnapshotProvider? worldProvider = null, RecorderService? recorder = null, IGoapPlannerService? planner = null, ICapabilityEvaluator? capability = null)
    {
        _logger = logger;
        _dungeon = dungeon;
        _gameplay = gameplay;
        _worldProvider = worldProvider;
        _recorder = recorder;
        _planner = planner;
        _capability = capability;
    }

    public void RunGame()
    {
        System.Console.WriteLine("DEBUG: RunGame() called directly");
        ((IHostedService)this).StartAsync(CancellationToken.None).GetAwaiter().GetResult();
    }

    Task IHostedService.StartAsync(CancellationToken cancellationToken)
    {
        System.Console.WriteLine("DEBUG: IHostedService.StartAsync called (explicit interface implementation)");
        _logger?.LogInformation("Starting SadConsole...");
        SetState(Plate.CrossMilo.Contracts.Hosting.App.AppState.Starting);

        // Configure and create SadConsole game
        System.Console.WriteLine("DEBUG: Creating SadConsole game...");
        Game.Create(120, 40);
        System.Console.WriteLine("DEBUG: Game.Create completed");

        Game.Instance.Started += OnGameStarted;
        _game = Game.Instance;
        System.Console.WriteLine($"DEBUG: Game instance: {_game != null}");

        // Fallback: eagerly create the main screen before the Started event.
        // On some environments the Started event may not fire reliably; this ensures
        // the dungeon initializes and updates even if the event is missed.
        try
        {
            if (_gameScreen == null)
            {
                _logger?.LogInformation("Creating game screen eagerly (pre-Started fallback)...");
                _gameScreen = new DungeonGameScreen(120, 40, _logger, _dungeon, _gameplay, _worldProvider, _recorder);
                GameHost.Instance.Screen = _gameScreen;
                _logger?.LogInformation("Game screen set as current (fallback)");
            }
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "Failed to create screen in fallback path");
        }

        SetState(Plate.CrossMilo.Contracts.Hosting.App.AppState.Running);
        _logger?.LogInformation("SadConsole started successfully");

        // Start the game loop - this will block until the window is closed
        // On macOS, this MUST run on the main thread
        System.Console.WriteLine("DEBUG: Starting game loop...");
        _runCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        _game.Run();
        System.Console.WriteLine("DEBUG: Game loop completed");

        return Task.CompletedTask;
    }

    private void OnGameStarted(object? sender, GameHost host)
    {
        _logger?.LogInformation("OnGameStarted fired!");
        _logger?.LogInformation("_dungeon is {Status}", _dungeon == null ? "NULL" : "NOT NULL");
        _logger?.LogInformation("_logger is {Status}", _logger == null ? "NULL" : "NOT NULL");
        _logger?.LogInformation("SadConsole game started...");

        // Create main game screen
        _gameScreen = new DungeonGameScreen(120, 40, _logger, _dungeon, _gameplay, _worldProvider, _recorder);
        GameHost.Instance.Screen = _gameScreen;

        _logger?.LogInformation("Game screen created");

        // Optional: run planner demo once if service is available
        if (!_plannerDemoRan && _planner != null)
        {
            _plannerDemoRan = true;
            _planner.PlanDemo(_logger);
        }
    }

    Task IHostedService.StopAsync(CancellationToken cancellationToken)
    {
        _logger?.LogInformation("Stopping SadConsole...");
        SetState(Plate.CrossMilo.Contracts.Hosting.App.AppState.Stopping);

        _runCts?.Cancel();
        _game?.Dispose();
        _game = null;
        _gameScreen = null;

        SetState(Plate.CrossMilo.Contracts.Hosting.App.AppState.Stopped);
        return Task.CompletedTask;
    }

    public Task RenderAsync(CancellationToken cancellationToken = default)
    {
        // SadConsole handles its own rendering loop
        return Task.CompletedTask;
    }

    public Task HandleInputAsync(InputEvent input, CancellationToken cancellationToken = default)
    {
        // SadConsole handles its own input
        return Task.CompletedTask;
    }

    public Task ResizeAsync(int width, int height, CancellationToken cancellationToken = default)
    {
        _logger?.LogInformation("Resize requested: {Width}x{Height}", width, height);
        // SadConsole window size is fixed at initialization
        return Task.CompletedTask;
    }

    private void SetState(Plate.CrossMilo.Contracts.Hosting.App.AppState newState)
    {
        var oldState = _state;
        _state = newState;
        StateChanged?.Invoke(this, new Plate.CrossMilo.Contracts.Hosting.App.AppStateChangedEventArgs
        {
            PreviousState = oldState,
            NewState = newState
        });
    }
}

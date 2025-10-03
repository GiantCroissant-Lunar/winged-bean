using System.Collections.Generic;
using System.Linq;
using System.Text;
using WingedBean.Contracts.Core;
using WingedBean.Contracts.ECS;
using WingedBean.Contracts.Game;
using WingedBean.Contracts.TerminalUI;
using WingedBean.Contracts.WebSocket;
using Terminal.Gui;

namespace ConsoleDungeon;

public sealed class Program : IDisposable
{
    private const int TargetWidth = 80;
    private const int TargetHeight = 24;

    private readonly IRegistry _registry;
    private IWebSocketService? _webSocketService;
    private ITerminalUIService? _terminalUIService;
    private IDungeonGameService? _gameService;

    private DungeonView? _dungeonView;
    private Label? _statsLabel;

    private bool _webSocketConnected;
    private bool _gameRunning;

    private readonly object _stateLock = new();
    private IReadOnlyList<EntitySnapshot> _currentEntities = Array.Empty<EntitySnapshot>();
    private PlayerStats _currentPlayerStats = EmptyPlayerStats();
    private readonly List<WorldHandle> _runtimeWorlds = new();
    private int _currentWorldIndex;
    private GameMode _cachedMode = GameMode.Play;

    private IDisposable? _entitiesSubscription;
    private IDisposable? _playerStatsSubscription;
    private IDisposable? _gameStateSubscription;

    public Program(IRegistry registry)
    {
        _registry = registry ?? throw new ArgumentNullException(nameof(registry));
    }

    public async Task RunAsync()
    {
        Console.WriteLine("Console Dungeon - Starting with Service Registry...");
        Console.WriteLine();

        if (!_registry.IsRegistered<IDungeonGameService>())
        {
            Console.WriteLine("❌ ERROR: IDungeonGameService not found!");
            Console.WriteLine("   Ensure wingedbean.plugins.dungeongame is loaded via plugins.json.");
            return;
        }

        _gameService = _registry.Get<IDungeonGameService>();
        SubscribeToGameStreams();
        _gameService.Initialize();
        _cachedMode = _gameService.CurrentMode;
        _gameService.ModeChanged += OnModeChanged;
        RefreshRuntimeWorlds();

        TryLoadWebSocketService();
        TryLoadTerminalUIService();

        Console.WriteLine();
        Console.WriteLine("Starting Terminal.Gui v2 application...");
        Console.WriteLine();

        _gameRunning = true;

        try
        {
            await Task.Run(RunTerminalGuiApp);
        }
        finally
        {
            _gameRunning = false;
            if (_webSocketService != null)
            {
                _webSocketService.MessageReceived -= HandleWebSocketMessage;
            }

            _gameService?.Shutdown();
            Dispose();
        }
    }

    private void TryLoadWebSocketService()
    {
        try
        {
            _webSocketService = _registry.Get<IWebSocketService>();
            Console.WriteLine("✓ WebSocket service loaded");
            _webSocketService.MessageReceived += HandleWebSocketMessage;
            _webSocketService.Start(4040);
            Console.WriteLine("✓ WebSocket server started (ws://localhost:4040)");
        }
        catch (ServiceNotFoundException)
        {
            Console.WriteLine("⚠ WebSocket service not found - running without WebSocket");
        }
    }

    private void TryLoadTerminalUIService()
    {
        if (_registry.IsRegistered<ITerminalUIService>())
        {
            _terminalUIService = _registry.Get<ITerminalUIService>();
            Console.WriteLine("✓ TerminalUI service loaded");
        }
        else
        {
            Console.WriteLine("⚠ TerminalUI service not found - using local Terminal.Gui renderer");
        }
    }

    private void RunTerminalGuiApp()
    {
        Application.Init();

        var top = Application.Top;
        var window = new Window("Console Dungeon - Plugin Mode")
        {
            X = 0,
            Y = 1,
            Width = Dim.Fill(),
            Height = Dim.Fill() - 1
        };

        _dungeonView = new DungeonView(GetCurrentEntitiesSnapshot)
        {
            X = 0,
            Y = 0,
            Width = TargetWidth,
            Height = TargetHeight
        };

        _statsLabel = new Label(string.Empty)
        {
            X = 0,
            Y = Pos.Bottom(_dungeonView),
            Width = Dim.Fill(),
            Height = 1
        };

        window.Add(_dungeonView, _statsLabel);

        var menu = new MenuBar(new[]
        {
            new MenuBarItem("_File", new[]
            {
                new MenuItem("_Quit", string.Empty, () =>
                {
                    _gameRunning = false;
                    Application.RequestStop();
                })
            })
        });

        top.Add(menu, window);

        // Handle local keyboard input
        window.KeyDown += e =>
        {
            HandleKeyPress(e.KeyEvent);
            e.Handled = true;
        };

        var updateToken = Application.MainLoop.AddTimeout(TimeSpan.FromMilliseconds(100), _ => UpdateGameLoop());

        try
        {
            Application.Run();
        }
        finally
        {
            Application.MainLoop.RemoveTimeout(updateToken);
            Application.Shutdown();
        }
    }

    private bool UpdateGameLoop()
    {
        if (!_gameRunning)
        {
            Application.RequestStop();
            return false;
        }

        _gameService?.Update(0.1f);
        UpdateStatsLabel();
        _dungeonView?.SetNeedsDisplay();

        if (_webSocketConnected && _webSocketService != null)
        {
            var screenContent = GetGameScreenContent();
            _webSocketService.Broadcast($"screen:{screenContent}");
        }

        return true;
    }

    private void UpdateStatsLabel()
    {
        if (_statsLabel == null)
        {
            return;
        }

        PlayerStats stats;
        int entityCount;
        lock (_stateLock)
        {
            stats = _currentPlayerStats;
            entityCount = _currentEntities.Count;
        }

        var mode = _gameService?.CurrentMode ?? GameMode.Play;
        var worldHandle = _gameService?.RuntimeWorldHandle ?? WorldHandle.Invalid;

        _statsLabel.Text = $"Mode: {mode}  World: {FormatWorld(worldHandle)}  HP: {stats.CurrentHP}/{stats.MaxHP}  " +
                           $"MP: {stats.CurrentMana}/{stats.MaxMana}  " +
                           $"Lvl: {stats.Level}  " +
                           $"XP: {stats.Experience}  " +
                           $"Entities: {entityCount}";
    }

    private void HandleKeyPress(KeyEvent key)
    {
        if (_gameService == null)
        {
            return;
        }

        switch (key.Key)
        {
            case Key.W or Key.CursorUp:
                _gameService.HandleInput(new GameInput(InputType.MoveUp));
                break;
            case Key.S or Key.CursorDown:
                _gameService.HandleInput(new GameInput(InputType.MoveDown));
                break;
            case Key.A or Key.CursorLeft:
                _gameService.HandleInput(new GameInput(InputType.MoveLeft));
                break;
            case Key.D or Key.CursorRight:
                _gameService.HandleInput(new GameInput(InputType.MoveRight));
                break;
            case Key.M:
                CycleGameMode();
                break;
            case Key.C:
                CreateAndSwitchRuntimeWorld();
                break;
            case Key.Tab:
                CycleRuntimeWorld();
                break;
            case Key.Tab | Key.ShiftMask:
                CycleRuntimeWorld(forward: false);
                break;
            case Key.Q:
                _gameService.HandleInput(new GameInput(InputType.Quit));
                _gameRunning = false;
                Application.RequestStop();
                break;
        }
    }

    private string GetScreenContent()
    {
        if (_terminalUIService != null)
        {
            return _terminalUIService.GetScreenContent();
        }

        return GetGameScreenContent();
    }

    private string GetGameScreenContent()
    {
        var (entities, stats) = GetCurrentState();
        var mode = _gameService?.CurrentMode ?? GameMode.Play;
        var worldHandle = _gameService?.RuntimeWorldHandle ?? WorldHandle.Invalid;

        var sb = new StringBuilder();
        sb.Append("\x1b[H\x1b[2J");

        for (int y = 0; y < TargetHeight; y++)
        {
            sb.Append($"\x1b[{y + 1};1H");
            for (int x = 0; x < TargetWidth; x++)
            {
                sb.Append('.');
            }
        }

        foreach (var entity in entities.OrderBy(e => e.RenderLayer))
        {
            var pos = entity.Position;
            if (pos.X < 0 || pos.X >= TargetWidth || pos.Y < 0 || pos.Y >= TargetHeight)
            {
                continue;
            }

            sb.Append($"\x1b[{pos.Y + 1};{pos.X + 1}H");
            sb.Append($"\x1b[{MapAnsiColor(entity.ForegroundColor)}m{entity.Symbol}\x1b[0m");
        }

        sb.Append($"\x1b[{TargetHeight + 1};1H");
        sb.Append($"Mode: {mode}  World: {FormatWorld(worldHandle)}  HP: {stats.CurrentHP}/{stats.MaxHP}  MP: {stats.CurrentMana}/{stats.MaxMana}  ");
        sb.Append($"Lvl: {stats.Level}  XP: {stats.Experience}");

        return sb.ToString();
    }

    private void HandleWebSocketMessage(string message)
    {
        _webSocketConnected = true;

        if (message.StartsWith("key:", StringComparison.OrdinalIgnoreCase))
        {
            var keyData = message[4..];
            HandleWebSocketKey(keyData);
        }
        else if (string.Equals(message, "init", StringComparison.OrdinalIgnoreCase))
        {
            var screenContent = GetGameScreenContent();
            _webSocketService?.Broadcast($"screen:{screenContent}");
        }
    }

    private void HandleWebSocketKey(string keyData)
    {
        if (_gameService == null)
        {
            return;
        }

        switch (keyData.ToLowerInvariant())
        {
            case "w":
            case "arrowup":
                _gameService.HandleInput(new GameInput(InputType.MoveUp));
                break;
            case "s":
            case "arrowdown":
                _gameService.HandleInput(new GameInput(InputType.MoveDown));
                break;
            case "a":
            case "arrowleft":
                _gameService.HandleInput(new GameInput(InputType.MoveLeft));
                break;
            case "d":
            case "arrowright":
                _gameService.HandleInput(new GameInput(InputType.MoveRight));
                break;
            case "m":
                CycleGameMode();
                break;
            case "c":
                CreateAndSwitchRuntimeWorld();
                break;
            case "tab":
                CycleRuntimeWorld();
                break;
            case "shift+tab":
            case "backtab":
                CycleRuntimeWorld(forward: false);
                break;
        }
    }

    private void SubscribeToGameStreams()
    {
        if (_gameService == null)
        {
            return;
        }

        _entitiesSubscription = _gameService.EntitiesObservable.Subscribe(new AnonymousObserver<IReadOnlyList<EntitySnapshot>>(snapshot =>
        {
            lock (_stateLock)
            {
                _currentEntities = snapshot.ToArray();
            }
        }));

        _playerStatsSubscription = _gameService.PlayerStatsObservable.Subscribe(new AnonymousObserver<PlayerStats>(stats =>
        {
            lock (_stateLock)
            {
                _currentPlayerStats = stats;
            }
        }));

        _gameStateSubscription = _gameService.GameStateObservable.Subscribe(new AnonymousObserver<GameState>(state =>
        {
            if (state is GameState.GameOver or GameState.Victory)
            {
                _gameRunning = false;
            }
        }));
    }

    private void CycleGameMode(bool forward = true)
    {
        if (_gameService == null)
        {
            return;
        }

        var modes = new[] { GameMode.Play, GameMode.EditOverlay, GameMode.EditPaused };
        var currentIndex = Array.IndexOf(modes, _gameService.CurrentMode);
        if (currentIndex < 0)
        {
            currentIndex = 0;
        }
        currentIndex = forward
            ? (currentIndex + 1) % modes.Length
            : (currentIndex - 1 + modes.Length) % modes.Length;

        _gameService.SetMode(modes[currentIndex]);
        _cachedMode = modes[currentIndex];
        UpdateStatsLabel();
    }

    private void CreateAndSwitchRuntimeWorld()
    {
        if (_gameService == null)
        {
            return;
        }

        var handle = _gameService.CreateRuntimeWorld($"runtime-{DateTime.Now:HHmmss}");
        _gameService.SwitchRuntimeWorld(handle);
        RefreshRuntimeWorlds();
        UpdateStatsLabel();
    }

    private void CycleRuntimeWorld(bool forward = true)
    {
        if (_gameService == null)
        {
            return;
        }

        RefreshRuntimeWorlds();
        if (_runtimeWorlds.Count == 0)
        {
            return;
        }

        var currentHandle = _gameService.RuntimeWorldHandle;
        var currentIndex = _runtimeWorlds.FindIndex(handle => handle.Equals(currentHandle));
        if (currentIndex < 0)
        {
            currentIndex = 0;
        }

        currentIndex = forward
            ? (currentIndex + 1) % _runtimeWorlds.Count
            : (currentIndex - 1 + _runtimeWorlds.Count) % _runtimeWorlds.Count;

        var target = _runtimeWorlds[currentIndex];
        _gameService.SwitchRuntimeWorld(target);
        RefreshRuntimeWorlds();
        UpdateStatsLabel();
    }

    private void RefreshRuntimeWorlds()
    {
        if (_gameService == null)
        {
            return;
        }

        _runtimeWorlds.Clear();
        _runtimeWorlds.AddRange(_gameService.RuntimeWorlds);
        _currentWorldIndex = _runtimeWorlds.FindIndex(handle => handle.Equals(_gameService.RuntimeWorldHandle));
        if (_currentWorldIndex < 0 && _runtimeWorlds.Count > 0)
        {
            _currentWorldIndex = 0;
        }
    }

    private void OnModeChanged(object? sender, GameMode mode)
    {
        _cachedMode = mode;
        UpdateStatsLabel();
    }

    private static string FormatWorld(WorldHandle handle) => handle.IsValid ? handle.ToString() : "n/a";


    private IReadOnlyList<EntitySnapshot> GetCurrentEntitiesSnapshot()
    {
        lock (_stateLock)
        {
            return _currentEntities;
        }
    }

    private (IReadOnlyList<EntitySnapshot> Entities, PlayerStats Stats) GetCurrentState()
    {
        lock (_stateLock)
        {
            return (_currentEntities, _currentPlayerStats);
        }
    }

    private static int MapAnsiColor(ConsoleColor color) => color switch
    {
        ConsoleColor.Black => 30,
        ConsoleColor.DarkRed => 31,
        ConsoleColor.DarkGreen => 32,
        ConsoleColor.DarkYellow => 33,
        ConsoleColor.DarkBlue => 34,
        ConsoleColor.DarkMagenta => 35,
        ConsoleColor.DarkCyan => 36,
        ConsoleColor.Gray => 37,
        ConsoleColor.Red => 91,
        ConsoleColor.Green => 92,
        ConsoleColor.Yellow => 93,
        ConsoleColor.Blue => 94,
        ConsoleColor.Magenta => 95,
        ConsoleColor.Cyan => 96,
        ConsoleColor.White => 97,
        _ => 37
    };

    private static PlayerStats EmptyPlayerStats() => new(0, 0, 0, 0, 0, 0, 0, 0, 0, 0);

    public void Dispose()
    {
        _entitiesSubscription?.Dispose();
        _playerStatsSubscription?.Dispose();
        _gameStateSubscription?.Dispose();
        if (_gameService != null)
        {
            _gameService.ModeChanged -= OnModeChanged;
        }
    }

    private sealed class AnonymousObserver<T> : IObserver<T>
    {
        private readonly Action<T> _onNext;

        public AnonymousObserver(Action<T> onNext)
        {
            _onNext = onNext;
        }

        public void OnCompleted()
        {
        }

        public void OnError(Exception error)
        {
        }

        public void OnNext(T value)
        {
            _onNext(value);
        }
    }

    // Static entry point kept for backwards compatibility with older tooling.
    public static async Task Main(string[] args)
    {
        Console.WriteLine("ConsoleDungeon.Program.Main() called - this entry point is deprecated.");
        Console.WriteLine("Please use ConsoleDungeon.Host instead, which loads gameplay via plugins.");
        await Task.Delay(1000);
        throw new InvalidOperationException(
            "ConsoleDungeon.Program.Main() should not be called directly. " +
            "Use ConsoleDungeon.Host to properly initialize services via the Registry pattern.");
    }
}

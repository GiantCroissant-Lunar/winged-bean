using System;
using System.Collections.Generic;
using ConsoleDungeon.Contracts;
using Microsoft.Extensions.Logging;
using SadConsole;
using SadConsole.Input;
using SadRogue.Primitives;
using WingedBean.Contracts.Gameplay;
using Console = SadConsole.Console;
using RecorderMetadata = Plate.CrossMilo.Contracts.Recorder.Services.SessionMetadata;
using RecorderService = Plate.CrossMilo.Contracts.Recorder.Services.IService;
using WingedBeanGameplay = WingedBean.Contracts.Gameplay.IService;

namespace WingedBean.Plugins.SadConsole;

/// <summary>
/// Main game screen for the dungeon game.
/// </summary>
public class DungeonGameScreen : ScreenObject
{
    private readonly Console _mainConsole;
    private readonly Console _statusBar;
    private readonly ILogger? _logger;
    private readonly IDungeonService? _dungeon;
    private readonly WingedBeanGameplay? _gameplay;
    private readonly IWorldSnapshotProvider? _worldProvider;
    private readonly RecorderService? _recorder;
    private string? _recSessionId;

    private IReadOnlyList<EntitySnapshot> _entities = Array.Empty<EntitySnapshot>();
    private PlayerStats _playerStats = new PlayerStats(0, 0, 0, 0, 1, 0, 0, 0, 0, 0);
    private GameState _gameState = GameState.NotStarted;

    private IDisposable? _entitiesSub;
    private IDisposable? _statsSub;
    private IDisposable? _stateSub;
    private IDisposable? _worldSub;

    // UI-agnostic snapshot cache
    private IReadOnlyList<EntityView>? _viewEntities;
    private PlayerHud _hud = new PlayerHud();

    public DungeonGameScreen(int width, int height, ILogger? logger = null, IDungeonService? dungeon = null, WingedBeanGameplay? gameplay = null, IWorldSnapshotProvider? worldProvider = null, RecorderService? recorder = null)
    {
        _logger = logger;
        _dungeon = dungeon;
        _gameplay = gameplay;
        _worldProvider = worldProvider;
        _recorder = recorder;

        // Create main game console
        _mainConsole = new Console(width, height - 3)
        {
            Position = new Point(0, 0)
        };
        Children.Add(_mainConsole);

        // Create status bar
        _statusBar = new Console(width, 3)
        {
            Position = new Point(0, height - 3)
        };
        Children.Add(_statusBar);

        // Initialize gameplay if available
        _logger?.LogInformation("DungeonGameScreen constructor - _dungeon is {Status}", _dungeon == null ? "NULL" : "NOT NULL");
        _logger?.LogInformation("DungeonGameScreen constructor - _logger is {Status}", _logger == null ? "NULL" : "NOT NULL");
        _logger?.LogInformation("Initializing dungeon service...");
        if (_dungeon == null)
        {
            _logger?.LogWarning("⚠ IDungeonService is NULL - game will not initialize");
        }
        else
        {
            try
            {
                _logger?.LogInformation("Calling _dungeon.Initialize()...");
                _dungeon.Initialize();
                _logger?.LogInformation("✓ Dungeon initialized successfully. State: {State}", _dungeon.CurrentState);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "✗ Failed to initialize dungeon");
            }
        }

        try
        {
            _ = _gameplay?.StartAsync();
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "Failed to start gameplay service");
        }

        // Subscriptions
        if (_worldProvider != null)
        {
            _worldSub = Subscribe(_worldProvider.WorldObservable, ws =>
            {
                _viewEntities = ws.Entities;
                _hud = ws.Hud;
                _playerStats = new PlayerStats(_hud.HP, _hud.MaxHP, 0, 0, _hud.Level, _hud.XP, 0, 0, 0, 0);
                RedrawWorld();
                DrawStatusBar();
            });
        }

        if (_dungeon != null)
        {
            _entitiesSub = Subscribe(_dungeon.EntitiesObservable, list =>
            {
                _entities = list ?? Array.Empty<EntitySnapshot>();
                RedrawWorld();
            });
            _statsSub = Subscribe(_dungeon.PlayerStatsObservable, stats =>
            {
                _playerStats = stats;
                DrawStatusBar();
            });
            _stateSub = Subscribe(_dungeon.GameStateObservable, s =>
            {
                _gameState = s;
                DrawStatusBar();
            });
            // Initialize local snapshots
            _playerStats = _dungeon.CurrentPlayerStats;
            _gameState = _dungeon.CurrentState;

            // Emit a concise verification message to console for recording/debug
            try
            {
                var bot = (Environment.GetEnvironmentVariable("ENABLE_BOT_MODE") == "1");
                var count = _entities?.Count ?? 0;
                System.Console.WriteLine($"[Verify] UI ready; state={_gameState}; entities={count}; bot={bot}");
            }
            catch { }
        }

        // Initial draw
        DrawWelcomeScreen();
        DrawStatusBar();

        _logger?.LogInformation("DungeonGameScreen created ({Width}x{Height})", width, height);
    }

    private void DrawWelcomeScreen()
    {
        _mainConsole.Clear();

        // Title
        _mainConsole.Print(2, 2, "╔══════════════════════════════════════════════════════════╗", Color.Yellow);
        _mainConsole.Print(2, 3, "║         Console Dungeon - SadConsole Edition            ║", Color.Yellow);
        _mainConsole.Print(2, 4, "╚══════════════════════════════════════════════════════════╝", Color.Yellow);

        _mainConsole.Print(2, 6, "Press any key to start...", Color.White);

        _mainConsole.Print(2, 8, "Controls:", Color.Cyan);
        _mainConsole.Print(4, 9, "Arrow Keys - Move", Color.White);
        _mainConsole.Print(4, 10, "ESC - Quit", Color.White);

        _mainConsole.Print(2, 12, "Features:", Color.Cyan);
        _mainConsole.Print(4, 13, "✓ Plugin-based architecture", Color.Green);
        _mainConsole.Print(4, 14, "✓ Shared game logic", Color.Green);
        _mainConsole.Print(4, 15, "✓ Cross-platform rendering", Color.Green);
    }

    private void DrawStatusBar()
    {
        _statusBar.Clear();
        _statusBar.DrawLine(new Point(0, 0), new Point(_statusBar.Width - 1, 0), '═', Color.Gray);
        _statusBar.Print(2, 1, $"HP: {_playerStats.CurrentHP}/{_playerStats.MaxHP}", Color.Green);
        _statusBar.Print(20, 1, $"Level: {_playerStats.Level}", Color.Yellow);
        _statusBar.Print(35, 1, $"XP: {_playerStats.Experience}/?", Color.Cyan);
        _statusBar.Print(55, 1, $"State: {_gameState}", Color.White);
        _statusBar.Print(78, 1, "Backend: SadConsole", Color.Magenta);
    }

    public override bool ProcessKeyboard(Keyboard keyboard)
    {
        // Handle input
        if (keyboard.IsKeyPressed(Keys.Escape))
        {
            _logger?.LogInformation("ESC pressed, exiting...");
            // Exit using MonoGame API
            Game.Instance.MonoGameInstance.Exit();
            return true;
        }

        if (keyboard.IsKeyPressed(Keys.Up))
        {
            _logger?.LogDebug("UP pressed");
            _dungeon?.HandleInput(new GameInput(InputType.MoveUp));
        }
        else if (keyboard.IsKeyPressed(Keys.Down))
        {
            _logger?.LogDebug("DOWN pressed");
            _dungeon?.HandleInput(new GameInput(InputType.MoveDown));
        }
        else if (keyboard.IsKeyPressed(Keys.Left))
        {
            _logger?.LogDebug("LEFT pressed");
            _dungeon?.HandleInput(new GameInput(InputType.MoveLeft));
        }
        else if (keyboard.IsKeyPressed(Keys.Right))
        {
            _logger?.LogDebug("RIGHT pressed");
            _dungeon?.HandleInput(new GameInput(InputType.MoveRight));
        }

        // Recorder controls
        if (_recorder != null)
        {
            if (keyboard.IsKeyPressed(Keys.F9))
            {
                TryStartRecording();
                return true;
            }
            if (keyboard.IsKeyPressed(Keys.F10))
            {
                TryStopRecording();
                return true;
            }
        }

        return base.ProcessKeyboard(keyboard);
    }

    public override void Update(TimeSpan delta)
    {
        base.Update(delta);
        try
        {
            if (_gameplay != null)
            {
                _ = _gameplay.UpdateAsync(delta);
            }
            else
            {
                // fallback to dungeon service tick
                _dungeon?.Update((float)delta.TotalSeconds);
            }
        }
        catch { /* ignore update errors to keep UI responsive */ }
    }

    private void RedrawWorld()
    {
        _mainConsole.Clear();
        if (_viewEntities is { Count: > 0 })
        {
            foreach (var e in _viewEntities)
            {
                var fg = ToColor(e.Foreground);
                var bg = ToColor(e.Background);
                var x = Math.Clamp(e.X, 0, _mainConsole.Width - 1);
                var y = Math.Clamp(e.Y, 0, _mainConsole.Height - 1);
                _mainConsole.Print(x, y, e.Glyph.ToString(), fg, bg);
            }
            return;
        }

        foreach (var e in _entities)
        {
            var fg = ToColor(e.ForegroundColor);
            var bg = ToColor(e.BackgroundColor);
            // Clamp within console bounds
            var x = Math.Clamp(e.Position.X, 0, _mainConsole.Width - 1);
            var y = Math.Clamp(e.Position.Y, 0, _mainConsole.Height - 1);
            _mainConsole.Print(x, y, e.Symbol.ToString(), fg, bg);
        }
    }

    private void TryStartRecording()
    {
        if (_recorder == null) return;
        if (!string.IsNullOrEmpty(_recSessionId)) return;
        try
        {
            _recSessionId = $"sadconsole-{DateTimeOffset.Now:yyyyMMdd-HHmmss}";
            var meta = new RecorderMetadata
            {
                Width = _mainConsole.Width,
                Height = _mainConsole.Height + _statusBar.Height,
                Title = "SadConsole Session",
                Command = "SadConsole",
                WorkingDirectory = Environment.CurrentDirectory
            };
            _ = _recorder.StartRecordingAsync(_recSessionId, meta);
            _logger?.LogInformation("Recording started: {Session}", _recSessionId);
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "Failed to start recording");
            _recSessionId = null;
        }
    }

    private void TryStopRecording()
    {
        if (_recorder == null) return;
        if (string.IsNullOrEmpty(_recSessionId)) return;
        try
        {
            var sid = _recSessionId;
            _recSessionId = null;
            var path = _recorder.StopRecordingAsync(sid!).GetAwaiter().GetResult();
            _logger?.LogInformation("Recording stopped: {Path}", path);
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "Failed to stop recording");
        }
    }

    private static Color ToColor(ConsoleColor c) => c switch
    {
        ConsoleColor.Black => Color.Black,
        ConsoleColor.DarkBlue => Color.DarkBlue,
        ConsoleColor.DarkGreen => Color.DarkGreen,
        ConsoleColor.DarkCyan => Color.DarkCyan,
        ConsoleColor.DarkRed => Color.DarkRed,
        ConsoleColor.DarkMagenta => Color.DarkMagenta,
        ConsoleColor.DarkYellow => new Color(128, 128, 0),
        ConsoleColor.Gray => Color.Gray,
        ConsoleColor.DarkGray => Color.DarkGray,
        ConsoleColor.Blue => Color.Blue,
        ConsoleColor.Green => Color.Green,
        ConsoleColor.Cyan => Color.Cyan,
        ConsoleColor.Red => Color.Red,
        ConsoleColor.Magenta => Color.Magenta,
        ConsoleColor.Yellow => Color.Yellow,
        ConsoleColor.White => Color.White,
        _ => Color.White
    };

    private static IDisposable Subscribe<T>(IObservable<T> source, Action<T> onNext)
        => source.Subscribe(new Observer<T>(onNext));

    private sealed class Observer<T> : IObserver<T>
    {
        private readonly Action<T> _onNext;
        public Observer(Action<T> onNext)
        {
            _onNext = onNext;
        }

        public void OnCompleted() { }
        public void OnError(Exception error) { }
        public void OnNext(T value) => _onNext(value);
    }
}

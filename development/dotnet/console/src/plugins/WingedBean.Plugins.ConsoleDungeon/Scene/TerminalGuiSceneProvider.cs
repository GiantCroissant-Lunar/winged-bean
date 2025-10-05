using System;
using System.Text;
using Terminal.Gui;
using WingedBean.Contracts.Game;
using WingedBean.Contracts.Input;
using WingedBean.Contracts.Scene;

namespace WingedBean.Plugins.ConsoleDungeon.Scene;

/// <summary>
/// Custom view that renders game content using a Label to avoid infinite render loops.
/// </summary>
internal class GameWorldView : View
{
    private Label _contentLabel;
    private int _updateCount = 0;

    public GameWorldView()
    {
        CanFocus = true;
        _contentLabel = new Label
        {
            X = 0,
            Y = 0,
            Width = Dim.Fill(),
            Height = Dim.Fill(),
            Text = ""
        };
        Add(_contentLabel);
    }

    public void SetContent(string content)
    {
        _updateCount++;
        if (_updateCount == 1 || _updateCount % 100 == 0)
        {
            try { System.Console.WriteLine($"[GameWorldView] SetContent called #{_updateCount}"); } catch { }
        }
        _contentLabel.Text = content;
    }
}

/// <summary>
/// Terminal.Gui implementation of ISceneService.
/// Owns ALL Terminal.Gui lifecycle, window management, and UI thread marshaling.
/// Supports camera system and layered rendering.
/// </summary>
public class TerminalGuiSceneProvider : ISceneService
{
    private readonly IRenderService _renderService;
    private readonly IInputMapper _inputMapper;
    private readonly IInputRouter _inputRouter;
    private Window? _mainWindow;
    private GameWorldView? _gameWorldView;
    private Label? _statusLabel;
    private bool _initialized = false;
    private Camera _camera = Camera.Static(0, 0);

    // Debouncing and throttling
    private IReadOnlyList<EntitySnapshot>? _pendingSnapshots;
    private readonly object _lock = new();
    private bool _updatePending = false;
    private DateTime _lastUpdate = DateTime.MinValue;
    private const int MinUpdateIntervalMs = 50; // Max 20 FPS

    public event EventHandler<SceneShutdownEventArgs>? Shutdown;

    public TerminalGuiSceneProvider(
        IRenderService renderService,
        IInputMapper inputMapper,
        IInputRouter inputRouter)
    {
        _renderService = renderService;
        _inputMapper = inputMapper;
        _inputRouter = inputRouter;
    }

    public void Initialize()
    {
        if (_initialized) return;

        if (Application.Driver == null)
        {
            Application.Init();
            try
            {
                System.Console.WriteLine($"[TerminalGuiSceneProvider] Terminal.Gui initialized. Driver={Application.Driver?.GetType().FullName}");
            }
            catch { /* ignore console failures */ }
        }

        _mainWindow = new Window
        {
            Title = "Console Dungeon",
            X = 0,
            Y = 0,
            Width = Dim.Fill(),
            Height = Dim.Fill(),
            BorderStyle = LineStyle.Single
        };

        _statusLabel = new Label
        {
            X = 0,
            Y = 0,
            Width = Dim.Fill(),
            Height = 1,
            Text = "Loading..."
        };

        _gameWorldView = new GameWorldView
        {
            X = 0,
            Y = 1,
            Width = Dim.Fill(),
            Height = Dim.Fill()
        };
        _gameWorldView.SetContent("Initializing game...");

        // Attach KeyDown handler to the window to catch all key events
        _mainWindow.KeyDown += OnKeyDown;
        _gameWorldView.KeyDown += OnKeyDown;

        _mainWindow.Add(_statusLabel);
        _mainWindow.Add(_gameWorldView);
        
        // Set focus on the game view so it receives key events
        try
        {
            _gameWorldView.SetFocus();
            System.Console.WriteLine("[TerminalGuiSceneProvider] Window setup complete, focus set on game view");
        }
        catch (Exception ex)
        {
            try { System.Console.WriteLine($"[TerminalGuiSceneProvider] Warning: Could not set focus: {ex.Message}"); } catch { }
        }

        _initialized = true;
    }

    private void OnKeyDown(object? sender, Key keyEvent)
    {
        try { 
            System.Console.WriteLine($"[TerminalGuiSceneProvider] KeyDown: KeyCode={keyEvent.KeyCode}, AsRune={keyEvent.AsRune.Value}"); 
        } catch { }
        
        // Handle ESC key to exit the application
        if (keyEvent.KeyCode == KeyCode.Esc)
        {
            try { System.Console.WriteLine("[TerminalGuiSceneProvider] ESC detected, closing window"); } catch { }
            // In Terminal.Gui v2, when running a specific window, we need to request stop on that window
            _mainWindow?.RequestStop();
            keyEvent.Handled = true;
            return;
        }

        var rawEvent = new RawKeyEvent(
            virtualKey: GetConsoleKey(keyEvent),
            rune: keyEvent.AsRune.Value > 0 ? (uint?)keyEvent.AsRune.Value : null,
            isCtrl: keyEvent.IsCtrl,
            isAlt: keyEvent.IsAlt,
            isShift: keyEvent.IsShift,
            timestamp: DateTimeOffset.UtcNow
        );

        var mapped = _inputMapper.Map(rawEvent);
        if (mapped != null)
        {
            _inputRouter.Dispatch(mapped);
            keyEvent.Handled = true;
        }
    }

    private int? GetConsoleKey(Key key)
    {
        return key.KeyCode switch
        {
            KeyCode.CursorUp => 38,
            KeyCode.CursorDown => 40,
            KeyCode.CursorLeft => 37,
            KeyCode.CursorRight => 39,
            KeyCode.Space => 32,
            KeyCode.Esc => 27,
            _ => key.AsRune.Value > 0 && key.AsRune.Value < 128 ? (int)key.AsRune.Value : null
        };
    }

    public Viewport GetViewport()
    {
        if (_gameWorldView == null)
            return new Viewport(80, 24);

        // Use Frame dimensions if available, otherwise fallback to 80x24
        int width = _gameWorldView.Frame.Width;
        int height = _gameWorldView.Frame.Height;

        // If Frame hasn't been laid out yet (width/height are 0), use defaults
        if (width <= 0) width = 80;
        if (height <= 0) height = 23;  // 24 minus 1 for status bar

        return new Viewport(width, height);
    }

    public CameraViewport GetCameraViewport()
    {
        return new CameraViewport(GetViewport(), _camera);
    }

    public void SetCamera(Camera camera)
    {
        _camera = camera;
    }

    public void UpdateWorld(IReadOnlyList<EntitySnapshot> snapshots)
    {
        lock (_lock)
        {
            _pendingSnapshots = snapshots;
            
            // Throttle updates to avoid infinite render loop
            var now = DateTime.UtcNow;
            if (_updatePending || (now - _lastUpdate).TotalMilliseconds < MinUpdateIntervalMs)
            {
                return;
            }
            _updatePending = true;
            _lastUpdate = now;
        }

        Application.Invoke(() =>
        {
            try
            {
                IReadOnlyList<EntitySnapshot>? toRender;
                lock (_lock)
                {
                    toRender = _pendingSnapshots;
                    _pendingSnapshots = null;
                    _updatePending = false;
                }

                if (toRender == null || _gameWorldView == null) return;

                var viewport = GetViewport();
                var buffer = _renderService.Render(toRender, viewport.Width, viewport.Height);
                var text = buffer.ToPlainText();
                
                _gameWorldView.SetContent(text);
            }
            catch (Exception ex)
            {
                try { System.Console.WriteLine($"[TerminalGuiSceneProvider] UpdateWorld error: {ex.Message}"); } catch { }
            }
        });
    }

    public void UpdateWorldLayered(IReadOnlyList<LayeredSnapshot> layers)
    {
        // For now, flatten all layers into single render pass
        // Future: render each layer separately and composite
        var allEntities = new List<EntitySnapshot>();
        foreach (var layer in layers.OrderBy(l => l.Layer))
        {
            allEntities.AddRange(layer.Entities);
        }
        UpdateWorld(allEntities);
    }

    public void UpdateStatus(string status)
    {
        Application.Invoke(() =>
        {
            if (_statusLabel != null)
            {
                _statusLabel.Text = status;
            }
        });
    }

    public void Run()
    {
        if (!_initialized || _mainWindow == null)
            throw new InvalidOperationException("Scene not initialized. Call Initialize() first.");

        try
        {
            try { System.Console.WriteLine($"[TerminalGuiSceneProvider] Entering Application.Run with _mainWindow... (CanFocus={_gameWorldView?.CanFocus}, HasFocus={_mainWindow.HasFocus})"); } catch { }
            try { System.Console.WriteLine($"[TerminalGuiSceneProvider] Window state: Subviews={_mainWindow.Subviews?.Count}, Width={_mainWindow.Frame.Width}, Height={_mainWindow.Frame.Height}"); } catch { }
            
            // Run the main window explicitly - this blocks until window is closed
            Application.Run(_mainWindow);
            
            try { System.Console.WriteLine("[TerminalGuiSceneProvider] Application.Run returned"); } catch { }
        }
        finally
        {
            try { System.Console.WriteLine("[TerminalGuiSceneProvider] Invoking Shutdown event"); } catch { }
            Shutdown?.Invoke(this, new SceneShutdownEventArgs
            {
                Reason = ShutdownReason.UserRequest
            });

            try { System.Console.WriteLine("[TerminalGuiSceneProvider] Calling Application.Shutdown()"); } catch { }
            Application.Shutdown();
            try { System.Console.WriteLine("[TerminalGuiSceneProvider] Application.Run finished. Shutdown complete."); } catch { }
        }
    }
}

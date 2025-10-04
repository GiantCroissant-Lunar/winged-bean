using System;
using Terminal.Gui;
using WingedBean.Contracts.Game;
using WingedBean.Contracts.Input;
using WingedBean.Contracts.Scene;

namespace WingedBean.Plugins.ConsoleDungeon.Scene;

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
    private TextView? _gameWorldView;  // Changed from Label to TextView for multi-line text
    private Label? _statusLabel;
    private bool _initialized = false;
    private Camera _camera = Camera.Static(0, 0);

    // Debouncing
    private IReadOnlyList<EntitySnapshot>? _pendingSnapshots;
    private readonly object _lock = new();

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
        }

        _mainWindow = new Window
        {
            Title = "Console Dungeon",
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

        _gameWorldView = new TextView
        {
            X = 0,
            Y = 1,
            Width = Dim.Fill(),
            Height = Dim.Fill(),
            Text = "Initializing game...",
            ReadOnly = true,
            WordWrap = false
        };

        _gameWorldView.KeyDown += OnKeyDown;

        _mainWindow.Add(_statusLabel);
        _mainWindow.Add(_gameWorldView);

        _gameWorldView.SetFocus();

        _initialized = true;
    }

    private void OnKeyDown(object? sender, Key keyEvent)
    {
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
        }

        Application.Invoke(() =>
        {
            IReadOnlyList<EntitySnapshot>? toRender;
            lock (_lock)
            {
                toRender = _pendingSnapshots;
                _pendingSnapshots = null;
            }

            if (toRender == null || _gameWorldView == null) return;

            var viewport = GetViewport();
            var buffer = _renderService.Render(toRender, viewport.Width, viewport.Height);
            var text = buffer.ToPlainText(); // Use plain text (TextView will render it directly)
            _gameWorldView.Text = text;
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
            Application.Run(_mainWindow);
        }
        finally
        {
            Shutdown?.Invoke(this, new SceneShutdownEventArgs
            {
                Reason = ShutdownReason.UserRequest
            });

            Application.Shutdown();
        }
    }
}

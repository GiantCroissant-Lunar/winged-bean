using Terminal.Gui;
using WingedBean.Contracts.Game;
using WingedBean.Contracts.Input;
using WingedBean.Contracts.Scene;

namespace WingedBean.Plugins.ConsoleDungeon.Scene;

/// <summary>
/// Terminal.Gui implementation of ISceneService.
/// Owns ALL Terminal.Gui lifecycle, window management, and UI thread marshaling.
/// </summary>
public class TerminalGuiSceneProvider : ISceneService
{
    private readonly IRenderService _renderService;
    private readonly IInputMapper _inputMapper;
    private readonly IInputRouter _inputRouter;
    private Window? _mainWindow;
    private View? _inputView;
    private Label? _gameWorldView;
    private Label? _statusLabel;
    private bool _initialized = false;

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

        _gameWorldView = new Label
        {
            X = 0,
            Y = 1,
            Width = Dim.Fill(),
            Height = Dim.Fill(),
            Text = "Initializing game..."
        };

        _inputView = new View
        {
            X = 0,
            Y = 0,
            Width = Dim.Fill(),
            Height = Dim.Fill(),
            CanFocus = true
        };

        _inputView.KeyDown += OnKeyDown;

        _mainWindow.Add(_statusLabel);
        _mainWindow.Add(_gameWorldView);
        _mainWindow.Add(_inputView);

        _inputView.SetFocus();

        _initialized = true;
    }

    private void OnKeyDown(object? sender, Key keyEvent)
    {
        var rawEvent = new RawKeyEvent(
            virtualKey: GetConsoleKey(keyEvent),
            rune: keyEvent.AsRune.Value,
            isCtrl: keyEvent.IsCtrl,
            isAlt: keyEvent.IsAlt,
            isShift: keyEvent.IsShift,
            timestamp: DateTimeOffset.UtcNow
        );

        var mapped = _inputMapper.Map(rawEvent);
        if (mapped.HasValue)
        {
            _inputRouter.Dispatch(mapped.Value);
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

        return new Viewport(_gameWorldView.Bounds.Width, _gameWorldView.Bounds.Height);
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
            _gameWorldView.Text = buffer.ToText();
        });
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

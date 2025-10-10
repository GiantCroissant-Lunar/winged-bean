using Terminal.Gui;
using Plate.CrossMilo.Contracts.Game;
using Plate.CrossMilo.Contracts.Input;
using Plate.CrossMilo.Contracts.Scene.Services;
using Plate.CrossMilo.Contracts.Scene;
using System.Collections.Concurrent;
using System.Reflection;

namespace WingedBean.Providers.TerminalGuiScene;

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
    private View? _inputView;
    private Label? _gameWorldView;
    private Label? _statusLabel;
    private TextView? _logView;
    private readonly ConcurrentQueue<string> _logMessages = new();
    private const int MaxLogMessages = 100;
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

        // Menu hint bar at top
        var menuHint = new Label
        {
            X = 0,
            Y = 0,
            Width = Dim.Fill(),
            Height = 1,
            Text = "F1=Help | F2=Version | F3=Plugins | F4=Audio | ESC=Quit",
            ColorScheme = new ColorScheme
            {
                Normal = Application.Driver.MakeAttribute(Color.Black, Color.Gray)
            }
        };

        _statusLabel = new Label
        {
            X = 0,
            Y = 1,
            Width = Dim.Fill(),
            Height = 1,
            Text = "Loading..."
        };

        // Game world view - rows 2-16 (15 rows for game)
        _gameWorldView = new Label
        {
            X = 0,
            Y = 2,
            Width = Dim.Fill(),
            Height = 15,  // 15 rows for game world
            Text = "Initializing game..."
        };

        // Separator line before log console
        var logSeparator = new Label
        {
            X = 0,
            Y = 17,
            Width = Dim.Fill(),
            Height = 1,
            Text = "─────────────────────────────── Log Console ────────────────────────────────"
        };

        // Log console at bottom - 4 lines as simple TextView (no frame to save space)
        _logView = new TextView
        {
            X = 0,
            Y = 18,  // Start at row 18
            Width = Dim.Fill(),
            Height = 5,  // 5 rows for logs (18-22)
            ReadOnly = true,
            WordWrap = false,
            Text = "[00:00:00.000] INFO   | Terminal.Gui v2 initialized\n[00:00:00.001] DEBUG  | PTY active\n[00:00:00.002] DEBUG  | Log console ready"
        };

        // Input view for keyboard capture - overlays game world only (rows 2-16)
        _inputView = new View
        {
            X = 0,
            Y = 2,
            Width = Dim.Fill(),
            Height = 15,  // Match game world view exactly
            CanFocus = true
        };

        _inputView.KeyDown += OnKeyDown;

        // Add views in correct Z-order
        _mainWindow.Add(menuHint);
        _mainWindow.Add(_statusLabel);
        _mainWindow.Add(_gameWorldView);
        _mainWindow.Add(logSeparator);
        _mainWindow.Add(_logView);
        _mainWindow.Add(_inputView);  // Add last so it's on top for input

        _inputView.SetFocus();

        // Add initial log messages
        LogMessage("INFO", "Terminal.Gui v2 scene provider initialized");
        LogMessage("DEBUG", "PTY connection active");
        LogMessage("DEBUG", "Menu bar: F1=Help, F2=Version, F3=Plugins, F4=Audio");
        LogMessage("DEBUG", $"Game view: Y={_gameWorldView.Y}, Height={_gameWorldView.Height}");
        LogMessage("DEBUG", $"Log view: Y={_logView.Y}, Height={_logView.Height}");
        
        _initialized = true;
    }

    private void OnKeyDown(object? sender, Key keyEvent)
    {
        // Handle menu F-keys first
        switch (keyEvent.KeyCode)
        {
            case KeyCode.F1:
                ShowHelpDialog();
                keyEvent.Handled = true;
                return;
            case KeyCode.F2:
                ShowVersionDialog();
                keyEvent.Handled = true;
                return;
            case KeyCode.F3:
                ShowPluginsDialog();
                keyEvent.Handled = true;
                return;
            case KeyCode.F4:
                ShowAudioDialog();
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
            LogMessage("INPUT", $"Key: {keyEvent.KeyCode}");
            keyEvent.Handled = true;
        }
    }

    private void ShowHelpDialog()
    {
        var helpText = @"Console Dungeon - Help

Movement:
  ↑/↓/←/→  Move player
  ESC      Quit game

Menu:
  F1       Show this help
  F2       Show version info
  F3       Show loaded plugins
  F4       Show audio info

The game world is displayed in the center.
Log messages appear at the bottom.";

        MessageBox.Query("Help", helpText, "OK");
        LogMessage("MENU", "Help dialog shown");
    }

    private void ShowVersionDialog()
    {
        try
        {
            var assembly = Assembly.GetExecutingAssembly();
            var version = assembly.GetName().Version;
            var fileVersion = assembly.GetCustomAttribute<AssemblyFileVersionAttribute>()?.Version;
            var infoVersion = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;

            var message = $"Console Dungeon Plugin\n\n" +
                         $"Version: {version}\n" +
                         $"File Version: {fileVersion ?? "N/A"}\n" +
                         $"Info Version: {infoVersion ?? "N/A"}\n\n" +
                         $"Framework: .NET 8.0\n" +
                         $"Terminal.Gui: 2.0.0";

            MessageBox.Query("Version", message, "OK");
            LogMessage("MENU", "Version dialog shown");
        }
        catch (Exception ex)
        {
            MessageBox.ErrorQuery("Error", $"Failed to get version info: {ex.Message}", "OK");
            LogMessage("ERROR", $"Version dialog failed: {ex.Message}");
        }
    }

    private void ShowPluginsDialog()
    {
        try
        {
            var message = "Loaded Services:\n\n";
            message += "✓ Render Service\n";
            message += "✓ Input Mapper Service\n";
            message += "✓ Input Router Service\n";
            message += "✓ Scene Service (Terminal.Gui)\n";

            MessageBox.Query("Plugins & Services", message, "OK");
            LogMessage("MENU", "Plugins dialog shown");
        }
        catch (Exception ex)
        {
            MessageBox.ErrorQuery("Error", $"Failed to get plugin info: {ex.Message}", "OK");
            LogMessage("ERROR", $"Plugins dialog failed: {ex.Message}");
        }
    }

    private void ShowAudioDialog()
    {
        var message = "Audio Service: Not Available\n\n" +
                     "The audio plugin is optional and\n" +
                     "requires LibVLC to be installed.\n\n" +
                     "Enable it in plugins.json to use\n" +
                     "audio features.";

        MessageBox.Query("Audio", message, "OK");
        LogMessage("MENU", "Audio dialog shown");
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

        return new Viewport(_gameWorldView.Frame.Width, _gameWorldView.Frame.Height);
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
            _gameWorldView.Text = buffer.ToText();
            
            LogMessage("RENDER", $"Entities: {toRender.Count}, Viewport: {viewport.Width}x{viewport.Height}");
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
        
        LogMessage("STATUS", status);
    }

    /// <summary>
    /// Add a log message to the log console view.
    /// Messages are displayed in a scrollable TextView at the bottom of the screen.
    /// </summary>
    /// <param name="level">Log level (INFO, DEBUG, RENDER, INPUT, STATUS)</param>
    /// <param name="message">Log message text</param>
    private void LogMessage(string level, string message)
    {
        var timestamp = DateTime.Now.ToString("HH:mm:ss.fff");
        var logLine = $"[{timestamp}] {level.PadRight(6)} | {message}";
        
        _logMessages.Enqueue(logLine);
        
        // Keep only the last N messages
        while (_logMessages.Count > MaxLogMessages)
        {
            _logMessages.TryDequeue(out _);
        }
        
        // Update the TextView if it exists
        if (_logView != null)
        {
            Application.Invoke(() =>
            {
                _logView.Text = string.Join("\n", _logMessages);
                // Scroll to bottom to show latest message
                _logView.MoveEnd();
            });
        }
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

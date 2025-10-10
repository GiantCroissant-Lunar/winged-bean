using System;
using System.Reflection;
using System.Text;
using Terminal.Gui;
using Plate.CrossMilo.Contracts.Game;
using Plate.CrossMilo.Contracts.Input;
using Plate.CrossMilo.Contracts.Scene;
using Plate.CrossMilo.Contracts.Scene.Services;

// Type aliases for IService pattern
using IRenderService = Plate.CrossMilo.Contracts.Game.Render.IService;
using IInputRouter = Plate.CrossMilo.Contracts.Input.Router.IService;
using IInputMapper = Plate.CrossMilo.Contracts.Input.Mapper.IService;
using ISceneService = Plate.CrossMilo.Contracts.Scene.Services.IService;

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
    private bool _headlessMode = false;
    private Window? _mainWindow;
    private GameWorldView? _gameWorldView;
    private Label? _statusLabel;
    private TextView? _consoleLogView;
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
        try
        {
            var term = Environment.GetEnvironmentVariable("TERM");
            var redirected = System.Console.IsInputRedirected || System.Console.IsOutputRedirected;
            _headlessMode = string.IsNullOrEmpty(term) || redirected;
        }
        catch { _headlessMode = true; }

        if (_headlessMode)
        {
            try { System.Console.WriteLine("[TerminalGuiSceneProvider] HEADLESS mode: Skipping Terminal.Gui initialization (no TTY/TERM)"); } catch { }
            _initialized = true; // Mark as initialized for headless no-op operations
            return;
        }

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
                Normal = new Terminal.Gui.Attribute(Color.Black, Color.Gray)
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

        _gameWorldView = new GameWorldView
        {
            X = 0,
            Y = 2,
            Width = Dim.Fill(),
            Height = Dim.Fill() - 6  // Leave space for console log at bottom (4 lines + padding)
        };
        _gameWorldView.SetContent("Initializing game...");

        // Console log view at bottom (4 lines for messages)
        _consoleLogView = new TextView
        {
            X = 0,
            Y = Pos.Bottom(_gameWorldView),
            Width = Dim.Fill(),
            Height = 4,
            ReadOnly = true,
            Text = "=== Console Log ===\n> Game started\n> Use arrow keys to move\n> Press ESC to quit"
        };

        // Attach KeyDown handler to the window to catch all key events
        _mainWindow.KeyDown += OnKeyDown;
        _gameWorldView.KeyDown += OnKeyDown;

        _mainWindow.Add(menuHint);
        _mainWindow.Add(_statusLabel);
        _mainWindow.Add(_gameWorldView);
        _mainWindow.Add(_consoleLogView);
        
        // Set focus on the game view so it receives key events
        try
        {
            _gameWorldView.SetFocus();
            System.Console.WriteLine("[TerminalGuiSceneProvider] Window setup complete, focus set on game view");
            System.Console.WriteLine("[TerminalGuiSceneProvider] Menu bar: F1=Help, F2=Version, F3=Plugins, F4=Audio");
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
        if (_headlessMode)
        {
            // No-op in headless mode
            return;
        }
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
        if (_headlessMode)
        {
            return; // No-op
        }
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
        if (_headlessMode)
        {
            // Immediately return; caller is responsible for keepalive behavior
            try { System.Console.WriteLine("[TerminalGuiSceneProvider] HEADLESS Run(): returning immediately"); } catch { }
            return;
        }

        if (!_initialized)
        {
            try { System.Console.WriteLine("[TerminalGuiSceneProvider] ERROR: Scene not initialized. Call Initialize() first."); } catch { }
            throw new InvalidOperationException("Scene not initialized. Call Initialize() first.");
        }

        if (_mainWindow == null)
        {
            // Window is null but we're not in headless mode - this means initialization failed
            // Treat this as headless mode to avoid crash
            try { System.Console.WriteLine("[TerminalGuiSceneProvider] WARNING: _mainWindow is null (initialization may have failed). Treating as headless mode."); } catch { }
            _headlessMode = true;
            return;
        }

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

The game world is displayed in the center.";

        try { System.Console.WriteLine("[TerminalGuiSceneProvider] Showing help dialog"); } catch { }
        MessageBox.Query("Help", helpText, "OK");
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

            System.Console.WriteLine("[TerminalGuiSceneProvider] Showing version dialog");
            MessageBox.Query("Version", message, "OK");
        }
        catch (Exception ex)
        {
            try { System.Console.WriteLine($"[TerminalGuiSceneProvider] Version dialog error: {ex.Message}"); } catch { }
            MessageBox.ErrorQuery("Error", $"Failed to get version info: {ex.Message}", "OK");
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

            System.Console.WriteLine("[TerminalGuiSceneProvider] Showing plugins dialog");
            MessageBox.Query("Plugins & Services", message, "OK");
        }
        catch (Exception ex)
        {
            try { System.Console.WriteLine($"[TerminalGuiSceneProvider] Plugins dialog error: {ex.Message}"); } catch { }
            MessageBox.ErrorQuery("Error", $"Failed to get plugin info: {ex.Message}", "OK");
        }
    }

    private void ShowAudioDialog()
    {
        var message = "Audio Service: Not Available\n\n" +
                     "The audio plugin is optional and\n" +
                     "requires LibVLC to be installed.\n\n" +
                     "Enable it in plugins.json to use\n" +
                     "audio features.";

        try { System.Console.WriteLine("[TerminalGuiSceneProvider] Showing audio dialog"); } catch { }
        MessageBox.Query("Audio", message, "OK");
    }
}

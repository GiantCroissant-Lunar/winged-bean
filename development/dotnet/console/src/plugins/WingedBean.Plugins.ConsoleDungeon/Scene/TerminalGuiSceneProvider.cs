using System;
using System.IO;
using System.Reflection;
using System.Text;
using Microsoft.Extensions.Logging;
using Terminal.Gui;
using ConsoleDungeon.Contracts;
using Plate.CrossMilo.Contracts.Input;
using Plate.CrossMilo.Contracts.Scene;
using Plate.CrossMilo.Contracts.Scene.Services;
using Plate.CrossMilo.Contracts.Audio;

// Type aliases for IService pattern
using IRenderService = Plate.CrossMilo.Contracts.Game.Render.IService;
using IInputRouter = Plate.CrossMilo.Contracts.Input.Router.IService;
using IInputMapper = Plate.CrossMilo.Contracts.Input.Mapper.IService;
using ISceneService = Plate.CrossMilo.Contracts.Scene.Services.IService;
using IAudioService = Plate.CrossMilo.Contracts.Audio.Services.IService;

namespace WingedBean.Plugins.ConsoleDungeon.Scene;

/// <summary>
/// Custom view that renders game content using a Label to avoid infinite render loops.
/// </summary>
internal class GameWorldView : View
{
    private readonly ILogger? _logger;
    private Label _contentLabel;
    private int _updateCount = 0;

    public GameWorldView(ILogger? logger = null)
    {
        _logger = logger;
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
            _logger?.LogDebug("GameWorldView SetContent called #{UpdateCount}", _updateCount);
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
    private readonly ILogger<TerminalGuiSceneProvider>? _logger;
    private readonly IRenderService _renderService;
    private readonly IInputMapper _inputMapper;
    private readonly IInputRouter _inputRouter;
    private readonly IAudioService? _audioService;
    private bool _headlessMode = false;
    private bool _soundEffectsEnabled = true; // Toggle for sound effects
    private Window? _mainWindow;
    private GameWorldView? _gameWorldView;
    private Label? _statusLabel;
    private TextView? _consoleLogView;
    private bool _initialized = false;
    private Camera _camera = Camera.Static(0, 0);
    private MenuBar? _menuBar;

    // Debouncing and throttling
    private IReadOnlyList<EntitySnapshot>? _pendingSnapshots;
    private readonly object _lock = new();
    private bool _updatePending = false;
    private DateTime _lastUpdate = DateTime.MinValue;
    private const int MinUpdateIntervalMs = 50; // Max 20 FPS

    // Console log buffer (stores last 100 messages with timestamps)
    private readonly System.Collections.Generic.Queue<string> _consoleLogBuffer = new(100);
    private readonly object _logLock = new();

    public event EventHandler<SceneShutdownEventArgs>? Shutdown;

    public TerminalGuiSceneProvider(
        IRenderService renderService,
        IInputMapper inputMapper,
        IInputRouter inputRouter,
        IAudioService? audioService = null,
        ILogger<TerminalGuiSceneProvider>? logger = null)
    {
        _logger = logger;
        _renderService = renderService;
        _inputMapper = inputMapper;
        _inputRouter = inputRouter;
        _audioService = audioService;
        
        if (_audioService != null)
        {
            _logger?.LogInformation("🎵 Audio service detected - movement sounds enabled");
        }
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
            _logger?.LogInformation("HEADLESS mode: Skipping Terminal.Gui initialization (no TTY/TERM)");
            _initialized = true; // Mark as initialized for headless no-op operations
            return;
        }

        if (Application.Driver == null)
        {
            Application.Init();
            _logger?.LogInformation("Terminal.Gui initialized. Driver={DriverType}", Application.Driver?.GetType().FullName);
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

        // Create proper menu bar (added directly to window)
        _menuBar = new MenuBar
        {
            X = 0,
            Y = 0,
            Width = Dim.Fill()
        };
        
        _menuBar.Menus = new[]
        {
            new MenuBarItem("_File", new MenuItem[]
            {
                new MenuItem("_Quit", "Exit the game", () => Application.RequestStop(), null, null, KeyCode.Esc)
            }),
            new MenuBarItem("_View", new MenuItem[]
            {
                new MenuItem("_Version", "Show version information", ShowVersionDialog, null, null, KeyCode.F2),
                new MenuItem("_Plugins", "Show loaded plugins", ShowPluginsDialog, null, null, KeyCode.F3)
            }),
            new MenuBarItem("_Audio", new MenuItem[]
            {
                new MenuItem("Audio _Info", "Show audio information", ShowAudioDialog, null, null, KeyCode.F4)
            }),
            new MenuBarItem("_Help", new MenuItem[]
            {
                new MenuItem("_Help", "Show help", ShowHelpDialog, null, null, KeyCode.F1),
                new MenuItem("_About", "About Console Dungeon", ShowAboutDialog)
            })
        };

        _statusLabel = new Label
        {
            X = 0,
            Y = 1,
            Width = Dim.Fill(),
            Height = 1,
            Text = "Loading..."
        };

        _gameWorldView = new GameWorldView(_logger)
        {
            X = 0,
            Y = 2,
            Width = Dim.Fill(),
            Height = Dim.Fill() - 6  // Leave space for console log at bottom (4 lines + padding)
        };
        _gameWorldView.SetContent("Initializing game...");

        // Console log view at bottom (4 lines for messages) - scrollable
        _consoleLogView = new TextView
        {
            X = 0,
            Y = Pos.Bottom(_gameWorldView),
            Width = Dim.Fill(),
            Height = 4,
            ReadOnly = true,
            Text = "=== Console Log (scrollable) ==="
        };
        
        // Add initial message
        AddConsoleLog("Game initialized. Ready to play!");

        // Attach KeyDown handler to the window to catch all key events
        _mainWindow.KeyDown += OnKeyDown;
        _gameWorldView.KeyDown += OnKeyDown;

        // Add menu bar FIRST, then other components
        _mainWindow.Add(_menuBar);
        _mainWindow.Add(_statusLabel);
        _mainWindow.Add(_gameWorldView);
        _mainWindow.Add(_consoleLogView);
        
        // Set focus on the game view so it receives key events
        try
        {
            _gameWorldView.SetFocus();
            _logger?.LogInformation("Window setup complete, focus set on game view");
            _logger?.LogInformation("Menu bar: File, View, Audio, Help (Alt+F/V/A/H or F9 to open)");
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "Could not set focus");
        }

        _initialized = true;
    }

    private void OnKeyDown(object? sender, Key keyEvent)
    {
        _logger?.LogDebug("KeyDown: KeyCode={KeyCode}, AsRune={Rune}", keyEvent.KeyCode, keyEvent.AsRune.Value);
        
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
            _logger?.LogInformation("ESC detected, closing window");
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
            
            // Log movement actions to console with sound effect indicator
            var actionName = mapped.Type switch
            {
                GameInputType.MoveUp => "Moved north",
                GameInputType.MoveDown => "Moved south",
                GameInputType.MoveLeft => "Moved west",
                GameInputType.MoveRight => "Moved east",
                _ => null
            };
            
            if (actionName != null)
            {
                // Play movement sound effect
                if (_soundEffectsEnabled)
                {
                    PlayMovementSound();
                    AddConsoleLog($"🔊 {actionName}");
                }
                else
                {
                    AddConsoleLog(actionName);
                }
            }
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
                _logger?.LogError(ex, "UpdateWorld error");
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

    /// <summary>
    /// Adds a message to the console log view with timestamp (keeps last 100 messages).
    /// </summary>
    public void AddConsoleLog(string message)
    {
        if (_headlessMode)
        {
            return; // No-op
        }
        
        lock (_logLock)
        {
            // Keep only last 100 messages
            if (_consoleLogBuffer.Count >= 100)
            {
                _consoleLogBuffer.Dequeue();
            }
            
            // Add timestamp to message
            var timestamp = DateTime.Now.ToString("HH:mm:ss");
            _consoleLogBuffer.Enqueue($"[{timestamp}] {message}");
            
            // Update the TextView
            Application.Invoke(() =>
            {
                if (_consoleLogView != null)
                {
                    var sb = new StringBuilder("=== Console Log (scrollable) ===\n");
                    foreach (var msg in _consoleLogBuffer)
                    {
                        sb.AppendLine(msg);
                    }
                    _consoleLogView.Text = sb.ToString().TrimEnd();
                    
                    // Auto-scroll to bottom
                    try
                    {
                        _consoleLogView.MoveEnd();
                    }
                    catch
                    {
                        // Ignore scrolling errors
                    }
                }
            });
        }
    }

    public void Run()
    {
        if (_headlessMode)
        {
            // Immediately return; caller is responsible for keepalive behavior
            _logger?.LogInformation("HEADLESS Run(): returning immediately");
            return;
        }

        if (!_initialized)
        {
            _logger?.LogError("ERROR: Scene not initialized. Call Initialize() first");
            throw new InvalidOperationException("Scene not initialized. Call Initialize() first.");
        }

        if (_mainWindow == null)
        {
            // Window is null but we're not in headless mode - this means initialization failed
            // Treat this as headless mode to avoid crash
            _logger?.LogWarning("_mainWindow is null (initialization may have failed). Treating as headless mode");
            _headlessMode = true;
            return;
        }

        try
        {
            _logger?.LogInformation("Entering Application.Run with _mainWindow (CanFocus={CanFocus}, HasFocus={HasFocus})",
                _gameWorldView?.CanFocus, _mainWindow.HasFocus);
            _logger?.LogDebug("Window state: Subviews={SubviewCount}, Width={Width}, Height={Height}",
                _mainWindow.Subviews?.Count, _mainWindow.Frame.Width, _mainWindow.Frame.Height);

            // Run the main window - this blocks until window is closed
            Application.Run(_mainWindow);

            _logger?.LogInformation("Application.Run returned");
        }
        finally
        {
            _logger?.LogInformation("Invoking Shutdown event");
            Shutdown?.Invoke(this, new SceneShutdownEventArgs
            {
                Reason = ShutdownReason.UserRequest
            });

            _logger?.LogInformation("Calling Application.Shutdown()");
            Application.Shutdown();
            _logger?.LogInformation("Application.Run finished. Shutdown complete");
        }
    }

    private void ShowHelpDialog()
    {
        var helpText = @"Console Dungeon - Help

Movement:
  ↑/↓/←/→  Move player
  ESC      Quit game

Menus:
  Alt+F    File menu (Quit)
  Alt+V    View menu (Version, Plugins)
  Alt+A    Audio menu
  Alt+H    Help menu
  F1       This help dialog
  F2       Version info
  F3       Loaded plugins
  F4       Audio info

Mouse:
  Click menu bar items to open menus
  
Console Log:
  Scroll with ↑/↓ when focused
  Shows timestamped game events";

        _logger?.LogDebug("Showing help dialog");
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

            _logger?.LogDebug("Showing version dialog");
            MessageBox.Query("Version", message, "OK");
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Version dialog error");
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
            message += _audioService != null 
                ? "✓ Audio Service (LibVLC)\n" 
                : "✗ Audio Service (Not available)\n";

            _logger?.LogDebug("Showing plugins dialog");
            MessageBox.Query("Plugins & Services", message, "OK");
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Plugins dialog error");
            MessageBox.ErrorQuery("Error", $"Failed to get plugin info: {ex.Message}", "OK");
        }
    }

    private void ShowAudioDialog()
    {
        var hasAudio = _audioService != null;
        var message = hasAudio
            ? $"Sound Effects: {(_soundEffectsEnabled ? "Enabled 🔊" : "Disabled 🔇")}\n\n" +
              "✓ Audio service available\n" +
              "✓ LibVLC audio engine\n\n" +
              "Movement sounds play when player moves.\n\n" +
              "Press 'M' to toggle sound effects."
            : "Sound Effects: Not Available\n\n" +
              "The audio plugin is optional and\n" +
              "requires LibVLC to be installed.\n\n" +
              "Install LibVLC and enable the audio\n" +
              "plugin in plugins.json to use audio.";

        _logger?.LogDebug("Showing audio dialog");
        MessageBox.Query("Audio", message, "OK");
    }
    
    private void PlayMovementSound()
    {
        if (_audioService == null || !_soundEffectsEnabled)
        {
            return;
        }
        
        try
        {
            // Construct path to sound file relative to the application
            // In production, this could be configurable via settings
            var baseDir = AppContext.BaseDirectory;
            var soundPath = Path.Combine(baseDir, "..", "..", "..", "assets", "sounds", "movement-step.wav");
            var fullPath = Path.GetFullPath(soundPath);
            
            _logger?.LogInformation("🔊 Playing sound: {Path} (exists: {Exists})", fullPath, File.Exists(fullPath));
            
            _audioService.Play(fullPath, new AudioPlayOptions 
            { 
                Volume = 0.3f,
                Loop = false 
            });
            
            _logger?.LogDebug("🎵 Movement sound triggered: {Path}", fullPath);
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "Failed to play movement sound: {Error}", ex.Message);
        }
    }

    private void ShowAboutDialog()
    {
        var message = @"Console Dungeon
Rogue-like Adventure Game

Built with:
  • .NET 8.0
  • Terminal.Gui 2.0
  • ECS Architecture

Features:
  • ASCII graphics
  • Turn-based combat
  • Procedural dungeons
  • Plugin system

© 2025 Winged Bean Project";

        _logger?.LogDebug("Showing about dialog");
        MessageBox.Query("About Console Dungeon", message, "OK");
    }
}

using WingedBean.Contracts.Core;
using WingedBean.Contracts.WebSocket;
using WingedBean.Contracts.TerminalUI;
using WingedBean.Contracts.ECS;
using System.Text;
using Terminal.Gui;

namespace ConsoleDungeon;

public class Program
{
    private readonly IRegistry _registry;
    private IWebSocketService? _webSocketService;
    private ITerminalUIService? _terminalUIService;
    private DungeonGame? _game;
    private bool _webSocketConnected = false;
    private bool _gameRunning = false;

    public Program(IRegistry registry)
    {
        _registry = registry ?? throw new ArgumentNullException(nameof(registry));
    }

    private string GetScreenContent()
    {
        try
        {
            // If we have a TerminalUI service, use it
            if (_terminalUIService != null)
            {
                return _terminalUIService.GetScreenContent();
            }

            // Otherwise, return a simple demo interface
            var sb = new StringBuilder();

            // Format content properly for xterm.js with proper line endings and cursor positioning
            sb.Append("\x1b[H"); // Move cursor to home position
            sb.Append("\x1b[2J"); // Clear entire screen

            // Build the Terminal.Gui interface line by line with proper cursor positioning
            sb.Append("\x1b[1;1H┌─ Console Dungeon - Service Registry Mode ───────────────────────────────────┐\r\n");
            sb.Append("\x1b[2;1H│                                                                              │\r\n");
            sb.Append("\x1b[3;1H│ WebSocket server running on port 4040                                       │\r\n");
            sb.Append("\x1b[4;1H│                                                                              │\r\n");
            sb.Append($"\x1b[5;1H│ WebSocket connected: {(_webSocketConnected ? "Yes" : "No")}                                                    │\r\n");
            sb.Append("\x1b[6;1H│                                                                              │\r\n");

            // Add empty lines
            for (int i = 7; i <= 21; i++)
            {
                sb.Append($"\x1b[{i};1H│                                                                              │\r\n");
            }

            sb.Append("\x1b[22;1H│                                  [ Quit ]                                    │\r\n");
            sb.Append("\x1b[23;1H└──────────────────────────────────────────────────────────────────────────────┘\r\n");

            return sb.ToString();
        }
        catch (Exception ex)
        {
            return $"Error getting screen content: {ex.Message}\r\n";
        }
    }

    public async Task RunAsync()
    {
        Console.WriteLine("Console Dungeon - Starting with Service Registry...");
        Console.WriteLine();

        // Check if ECS service is available
        if (!_registry.IsRegistered<IECSService>())
        {
            Console.WriteLine("❌ ERROR: IECSService not found!");
            Console.WriteLine("   The ArchECS plugin must be loaded for the game to run.");
            Console.WriteLine("   Check that plugins.json includes 'wingedbean.plugins.archecs'");
            return;
        }

        // Get services from registry
        try
        {
            _webSocketService = _registry.Get<IWebSocketService>();
            Console.WriteLine("✓ WebSocket service loaded");
        }
        catch (ServiceNotFoundException)
        {
            Console.WriteLine("⚠ WebSocket service not found - running without WebSocket");
        }

        // Try to get TerminalUI service (optional)
        if (_registry.IsRegistered<ITerminalUIService>())
        {
            _terminalUIService = _registry.Get<ITerminalUIService>();
            Console.WriteLine("✓ TerminalUI service loaded");
        }

        // Initialize the dungeon crawler game
        Console.WriteLine();
        Console.WriteLine("Initializing Dungeon Crawler...");
        _game = new DungeonGame(_registry);
        _game.Initialize();
        Console.WriteLine();

        // Setup WebSocket message handling if available
        if (_webSocketService != null)
        {
            _webSocketService.MessageReceived += HandleWebSocketMessage;
            Console.WriteLine("Starting WebSocket server on port 4040...");
            _webSocketService.Start(4040);
            Console.WriteLine("✓ WebSocket server started");
            Console.WriteLine("   Connect via: ws://localhost:4040");
        }

        Console.WriteLine();
        Console.WriteLine("Starting Terminal.Gui v2 application...");
        Console.WriteLine();

        _gameRunning = true;

        // Initialize Terminal.Gui and run the game
        await Task.Run(() => RunTerminalGuiApp());
    }

    private void RunTerminalGuiApp()
    {
        Application.Init();

        var top = Application.Top;

        // Create main window
        var win = new Window("Console Dungeon - Arch ECS")
        {
            X = 0,
            Y = 1,
            Width = Dim.Fill(),
            Height = Dim.Fill() - 1
        };

        // Create DungeonView for rendering the game
        var dungeonView = new DungeonView(_game!)
        {
            X = 0,
            Y = 0,
            Width = 80,
            Height = 24
        };

        // Create stats label
        var statsLabel = new Label("")
        {
            X = 0,
            Y = Pos.Bottom(dungeonView),
            Width = Dim.Fill(),
            Height = 1
        };

        win.Add(dungeonView, statsLabel);

        // Create menu bar
        var menu = new MenuBar(new MenuBarItem[]
        {
            new MenuBarItem("_File", new MenuItem[]
            {
                new MenuItem("_Quit", "", () =>
                {
                    _gameRunning = false;
                    Application.RequestStop();
                })
            })
        });

        top.Add(menu, win);

        // Game update timer (10 FPS = 100ms)
        var updateTimer = Application.MainLoop.AddTimeout(TimeSpan.FromMilliseconds(100), (_) =>
        {
            if (!_gameRunning)
            {
                Application.RequestStop();
                return false;
            }

            // Update game (ECS systems run: AI, Movement, Combat, Render)
            _game?.Update();

            // Update stats display
            UpdateStatsLabel(statsLabel);

            // Refresh the dungeon view
            dungeonView.Refresh();

            // Send update to WebSocket clients if connected
            if (_webSocketConnected && _webSocketService != null)
            {
                var screenContent = GetGameScreenContent();
                _webSocketService.Broadcast($"screen:{screenContent}");
            }

            return true; // Continue timer
        });

        // Handle keyboard input for player movement
        win.KeyDown += (e) =>
        {
            HandleKeyPress(e.KeyEvent);
            e.Handled = true;
        };

        Application.Run();
        Application.Shutdown();
    }

    private void UpdateStatsLabel(Label statsLabel)
    {
        if (_game == null || _game.World == null)
            return;

        // Find player stats
        foreach (var entity in _game.World.CreateQuery<Components.Player, Components.Stats>())
        {
            var stats = _game.World.GetComponent<Components.Stats>(entity);
            statsLabel.Text = $"HP: {stats.CurrentHP}/{stats.MaxHP}  " +
                             $"MP: {stats.CurrentMana}/{stats.MaxMana}  " +
                             $"Lvl: {stats.Level}  " +
                             $"XP: {stats.Experience}  " +
                             $"Entities: {_game.EntityCount}";
            break;
        }
    }

    private void HandleKeyPress(KeyEvent key)
    {
        if (_game == null || _game.World == null)
            return;

        // Find player entity
        foreach (var entity in _game.World.CreateQuery<Components.Player, Components.Position>())
        {
            ref var pos = ref _game.World.GetComponent<Components.Position>(entity);

            // Handle movement
            switch (key.Key)
            {
                case Key.W:
                case Key.CursorUp:
                    pos.Y--;
                    break;
                case Key.S:
                case Key.CursorDown:
                    pos.Y++;
                    break;
                case Key.A:
                case Key.CursorLeft:
                    pos.X--;
                    break;
                case Key.D:
                case Key.CursorRight:
                    pos.X++;
                    break;
                case Key.Q:
                    _gameRunning = false;
                    Application.RequestStop();
                    break;
            }

            break; // Only move first player
        }
    }

    private string GetGameScreenContent()
    {
        if (_game == null || _game.World == null)
            return GetScreenContent(); // Fallback to old demo screen

        // Return ANSI-formatted game screen for xterm.js
        var sb = new StringBuilder();
        sb.Append("\x1b[H\x1b[2J"); // Clear screen

        // Similar rendering as RenderGame() but with ANSI codes
        const int width = 80;
        const int height = 24;

        // Render dungeon
        for (int y = 0; y < height; y++)
        {
            sb.Append($"\x1b[{y + 1};1H");
            for (int x = 0; x < width; x++)
            {
                sb.Append('.');
            }
        }

        // Render entities
        foreach (var entity in _game.World.CreateQuery<Components.Position, Components.Renderable>())
        {
            var pos = _game.World.GetComponent<Components.Position>(entity);
            var render = _game.World.GetComponent<Components.Renderable>(entity);

            if (pos.X >= 0 && pos.X < width && pos.Y >= 0 && pos.Y < height)
            {
                sb.Append($"\x1b[{pos.Y + 1};{pos.X + 1}H");

                // Set color (simplified - map ConsoleColor to ANSI)
                int colorCode = render.ForegroundColor switch
                {
                    ConsoleColor.Yellow => 33,
                    ConsoleColor.Green => 32,
                    ConsoleColor.Red => 31,
                    ConsoleColor.White => 37,
                    _ => 37
                };
                sb.Append($"\x1b[{colorCode}m{render.Symbol}\x1b[0m");
            }
        }

        // Add stats at bottom
        sb.Append($"\x1b[{height + 1};1H");
        foreach (var entity in _game.World.CreateQuery<Components.Player, Components.Stats>())
        {
            var stats = _game.World.GetComponent<Components.Stats>(entity);
            sb.Append($"HP: {stats.CurrentHP}/{stats.MaxHP}  MP: {stats.CurrentMana}/{stats.MaxMana}  Lvl: {stats.Level}  XP: {stats.Experience}");
            break;
        }

        return sb.ToString();
    }

    private void HandleWebSocketMessage(string message)
    {
        _webSocketConnected = true;

        if (message.StartsWith("key:"))
        {
            var keyData = message.Substring(4);
            // Parse key from WebSocket and handle movement
            HandleWebSocketKey(keyData);
        }
        else if (message == "init")
        {
            // Send initial game screen
            var screenContent = GetGameScreenContent();
            _webSocketService?.Broadcast($"screen:{screenContent}");
        }
    }

    private void HandleWebSocketKey(string keyData)
    {
        if (_game == null || _game.World == null)
            return;

        // Map WebSocket key data to movements
        foreach (var entity in _game.World.CreateQuery<Components.Player, Components.Position>())
        {
            ref var pos = ref _game.World.GetComponent<Components.Position>(entity);

            switch (keyData.ToLower())
            {
                case "w":
                case "arrowup":
                    pos.Y--;
                    break;
                case "s":
                case "arrowdown":
                    pos.Y++;
                    break;
                case "a":
                case "arrowleft":
                    pos.X--;
                    break;
                case "d":
                case "arrowright":
                    pos.X++;
                    break;
            }

            break;
        }
    }

    // Static entry point for backwards compatibility
    public static async Task Main(string[] args)
    {
        Console.WriteLine("ConsoleDungeon.Program.Main() called - this entry point is deprecated.");
        Console.WriteLine("Please use ConsoleDungeon.Host instead, which properly initializes the service registry.");
        await Task.Delay(1000);
        throw new InvalidOperationException(
            "ConsoleDungeon.Program.Main() should not be called directly. " +
            "Use ConsoleDungeon.Host to properly initialize services via the Registry pattern.");
    }
}

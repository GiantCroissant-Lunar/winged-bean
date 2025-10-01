using WingedBean.Contracts.Core;
using WingedBean.Contracts.WebSocket;
using WingedBean.Contracts.TerminalUI;
using System.Text;

namespace ConsoleDungeon;

public class Program
{
    private readonly IRegistry _registry;
    private IWebSocketService? _webSocketService;
    private ITerminalUIService? _terminalUIService;
    private bool _webSocketConnected = false;

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

        // Get services from registry
        try
        {
            _webSocketService = _registry.Get<IWebSocketService>();
            Console.WriteLine("✓ WebSocket service loaded from registry");
        }
        catch (ServiceNotFoundException)
        {
            Console.WriteLine("⚠ WebSocket service not found in registry");
            throw;
        }

        // Try to get TerminalUI service (optional)
        if (_registry.IsRegistered<ITerminalUIService>())
        {
            _terminalUIService = _registry.Get<ITerminalUIService>();
            Console.WriteLine("✓ TerminalUI service loaded from registry");
        }
        else
        {
            Console.WriteLine("⚠ TerminalUI service not available - using simple demo interface");
        }

        // Subscribe to WebSocket messages
        _webSocketService.MessageReceived += async (message) =>
        {
            Console.WriteLine($"WebSocket message received: {message}");
            _webSocketConnected = true;

            if (message.Length > 0)
            {
                if (message.StartsWith("key:"))
                {
                    var keyData = message.Substring(4);
                    Console.WriteLine($"Key received: {keyData}");
                    // Acknowledge key press and send screen update
                    var screenContent = GetScreenContent();
                    _webSocketService.Broadcast($"screen:{screenContent}");
                }
                else if (message == "init")
                {
                    // Send initial screen
                    var screenContent = GetScreenContent();
                    Console.WriteLine($"Sending screen content length: {screenContent.Length} characters");
                    Console.WriteLine("First 100 chars: " + screenContent.Substring(0, Math.Min(100, screenContent.Length)));
                    _webSocketService.Broadcast($"screen:{screenContent}");
                    Console.WriteLine("Screen content sent successfully");
                }
            }
        };

        // Start WebSocket server
        Console.WriteLine("Starting WebSocket server on port 4040...");
        _webSocketService.Start(4040);
        Console.WriteLine("✓ WebSocket server started");

        // Initialize TerminalUI if available
        if (_terminalUIService != null)
        {
            _terminalUIService.Initialize();
            Console.WriteLine("✓ TerminalUI initialized");
        }

        // Just wait for WebSocket connections
        Console.WriteLine("Running. Press Ctrl+C to exit.");
        await Task.Delay(-1);  // Wait forever
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

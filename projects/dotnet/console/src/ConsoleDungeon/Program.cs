using SuperSocket.WebSocket.Server;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Configuration;
using Terminal.Gui;
using System.Text;

namespace ConsoleDungeon;

public class Program
{
    private static WebSocketSession? _webSocketSession;
    private static bool _uiInitialized = false;
    private static Timer? _screenUpdateTimer;

    private static string GetScreenContent()
    {
        if (!_uiInitialized)
        {
            return "Terminal.Gui not available - run server in a terminal window with TTY.\r\nWebSocket server is running on port 4040.\r\n";
        }

        try
        {
            var sb = new StringBuilder();

            // Format content properly for xterm.js with proper line endings and cursor positioning
            sb.Append("\x1b[H"); // Move cursor to home position
            sb.Append("\x1b[2J"); // Clear entire screen

            // Build the Terminal.Gui interface line by line with proper cursor positioning
            sb.Append("\x1b[1;1H┌─ Console Dungeon - Terminal.Gui v2 ─────────────────────────────────────────┐\r\n");
            sb.Append("\x1b[2;1H│                                                                              │\r\n");
            sb.Append("\x1b[3;1H│ WebSocket server running on port 4040                                       │\r\n");
            sb.Append("\x1b[4;1H│                                                                              │\r\n");
            sb.Append($"\x1b[5;1H│ Connected session: {(_webSocketSession != null ? "Yes" : "No")}                                                        │\r\n");
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

    private static async void SendScreenUpdate(object? state)
    {
        if (_webSocketSession != null)
        {
            try
            {
                var screenContent = GetScreenContent();
                await _webSocketSession.SendAsync($"screen:{screenContent}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error sending screen update: {ex.Message}");
            }
        }
    }

    public static async Task Main(string[] args)
    {
        Console.WriteLine("Console Dungeon - Starting...");

        var host = WebSocketHostBuilder.Create(args)
            .ConfigureAppConfiguration((hostCtx, configApp) =>
            {
                configApp.AddJsonFile("ConsoleDungeon/appsettings.json");
            })
            .UseWebSocketMessageHandler(async (session, message) =>
            {
                Console.WriteLine($"WebSocket message received: {message.Message}");
                _webSocketSession = session;

                if (message.Message.Length > 0)
                {
                    if (message.Message.StartsWith("key:"))
                    {
                        var keyData = message.Message.Substring(4);
                        Console.WriteLine($"Key received: {keyData}");
                        // For now, just acknowledge the key press
                        // In a real implementation, you'd send this to Terminal.Gui
                        var screenContent = GetScreenContent();
                        await session.SendAsync($"screen:{screenContent}");
                    }
                    else if (message.Message == "init")
                    {
                        // Send initial screen only once
                        var screenContent = GetScreenContent();
                        Console.WriteLine($"Sending screen content length: {screenContent.Length} characters");
                        Console.WriteLine("First 100 chars: " + screenContent.Substring(0, Math.Min(100, screenContent.Length)));
                        await session.SendAsync($"screen:{screenContent}");

                        // Don't start timer to avoid continuous updates
                        Console.WriteLine("Screen content sent successfully");
                    }
                }
            })
            .Build();

        Console.WriteLine("WebSocket server configured. Starting in background...");

        // Start WebSocket server in background
        _ = Task.Run(() => host.Run());

        // For now, just run in WebSocket-only mode to avoid Terminal.Gui complexity
        Console.WriteLine("Running in WebSocket-only mode for demonstration.");
        _uiInitialized = true; // Set to true so WebSocket clients get the demo interface

        // Just wait for WebSocket connections
        Console.WriteLine("Press Ctrl+C to exit.");
        await Task.Delay(-1);  // Wait forever
    }

    private static string ProcessCommand(string command)
    {
        var cmd = command.ToLower();
        switch (cmd)
        {
            case "help":
                return "Available commands:\n  help - Show this help\n  echo <text> - Echo text\n  time - Show current time\n  status - Show WebSocket status\n  quit - Exit application";

            case "time":
                return $"Current time: {DateTime.Now:yyyy-MM-dd HH:mm:ss}";

            case "status":
                var status = _webSocketSession != null ? "WebSocket connected" : "WebSocket not connected";
                return $"Status: {status}";

            default:
                if (cmd.StartsWith("echo "))
                {
                    return cmd.Substring(5);
                }
                else if (!string.IsNullOrWhiteSpace(cmd))
                {
                    return $"Unknown command: {cmd}";
                }
                break;
        }
        return "";
    }
}

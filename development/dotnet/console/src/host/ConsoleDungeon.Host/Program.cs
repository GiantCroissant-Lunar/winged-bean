using WingedBean.Contracts.Core;
using WingedBean.Contracts.WebSocket;
using WingedBean.Contracts.TerminalUI;
using WingedBean.Contracts.Config;
using WingedBean.Registry;
using WingedBean.Plugins.WebSocket;
using WingedBean.Plugins.TerminalUI;
using WingedBean.Plugins.Config;

namespace ConsoleDungeon.Host;

/// <summary>
/// Console host entry point.
/// Initializes Registry, loads service plugins, and launches ConsoleDungeon app.
/// </summary>
public class Program
{
    public static async System.Threading.Tasks.Task Main(string[] args)
    {
        System.Console.WriteLine("========================================");
        System.Console.WriteLine("ConsoleDungeon.Host - Service Registry Mode");
        System.Console.WriteLine("========================================");
        System.Console.WriteLine();

        // Step 1: Create Registry
        System.Console.WriteLine("[1/4] Initializing ActualRegistry...");
        var registry = new ActualRegistry();
        System.Console.WriteLine("✓ Registry initialized");
        System.Console.WriteLine();

        // Step 2: Register service plugins
        System.Console.WriteLine("[2/4] Registering service plugins...");

        // Register WebSocket service (SuperSocket implementation)
        var webSocketService = new SuperSocketWebSocketService();
        registry.Register<IWebSocketService>(webSocketService, priority: 100);
        System.Console.WriteLine("  ✓ IWebSocketService <- SuperSocketWebSocketService (priority: 100)");

        // Register TerminalUI service (Terminal.Gui implementation)
        var terminalUIService = new TerminalGuiService();
        registry.Register<ITerminalUIService>(terminalUIService, priority: 100);
        System.Console.WriteLine("  ✓ ITerminalUIService <- TerminalGuiService (priority: 100)");

        // Register Config service (MS.Extensions.Configuration wrapper)
        var configService = new ConfigService();
        registry.Register<IConfigService>(configService, priority: 100);
        System.Console.WriteLine("  ✓ IConfigService <- ConfigService (priority: 100)");

        System.Console.WriteLine();

        // Step 3: Verify registry
        System.Console.WriteLine("[3/4] Verifying registry...");
        System.Console.WriteLine($"  IWebSocketService registered: {registry.IsRegistered<IWebSocketService>()}");
        System.Console.WriteLine($"  ITerminalUIService registered: {registry.IsRegistered<ITerminalUIService>()}");
        System.Console.WriteLine($"  IConfigService registered: {registry.IsRegistered<IConfigService>()}");
        System.Console.WriteLine();

        // Step 4: Launch ConsoleDungeon with Registry
        System.Console.WriteLine("[4/4] Launching ConsoleDungeon with Registry...");
        System.Console.WriteLine();

        var app = new ConsoleDungeon.Program(registry);
        await app.RunAsync();
    }
}

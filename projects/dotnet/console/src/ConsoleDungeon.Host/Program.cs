namespace ConsoleDungeon.Host;

/// <summary>
/// Console host entry point.
/// MVP: Simply launches the ConsoleDungeon TUI app.
/// Future: Will initialize Registry, PluginLoader, and load services as plugins.
/// </summary>
public class Program
{
    public static async System.Threading.Tasks.Task Main(string[] args)
    {
        System.Console.WriteLine("ConsoleDungeon.Host starting (MVP mode)...");

        // MVP: Direct launch - no plugin system yet
        // TODO Phase 3: Add Registry + PluginLoader bootstrap
        await ConsoleDungeon.Program.Main(args);
    }
}

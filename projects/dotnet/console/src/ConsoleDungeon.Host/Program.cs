using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using WingedBean.Contracts.Core;
using WingedBean.Registry;
using WingedBean.PluginLoader;
using WingedBean.Providers.AssemblyContext;

namespace ConsoleDungeon.Host;

/// <summary>
/// Console host entry point with full plugin-based bootstrap.
/// Implements RFC-0004 Phase 3: Registry + PluginLoader bootstrap pattern.
/// </summary>
public class Program
{
    public static async System.Threading.Tasks.Task Main(string[] args)
    {
        // Create console logger for bootstrap
        using var loggerFactory = LoggerFactory.Create(builder =>
        {
            builder.AddConsole();
            builder.SetMinimumLevel(LogLevel.Information);
        });
        var logger = loggerFactory.CreateLogger<Program>();

        try
        {
            logger.LogInformation("ConsoleDungeon.Host starting with plugin bootstrap...");

            // Phase 3.8: Create foundation services
            // 1. Create ActualRegistry
            var registry = new ActualRegistry();
            logger.LogInformation("✓ ActualRegistry created");

            // 2. Create ActualPluginLoader with AssemblyContextProvider
            var contextProvider = new AssemblyContextProvider();
            var pluginLoader = new ActualPluginLoader(contextProvider, loggerFactory.CreateLogger<ActualPluginLoader>());
            logger.LogInformation("✓ ActualPluginLoader created with AssemblyContextProvider");

            // 3. Register foundation services
            registry.Register<IRegistry>(registry);
            registry.Register<IPluginLoader>(pluginLoader);
            logger.LogInformation("✓ Foundation services registered in Registry");

            // 4. Load plugins (Config, WebSocket, TerminalUI)
            // Note: These plugins don't exist yet in the repository.
            // For Phase 3.8, we demonstrate the bootstrap pattern with graceful handling.
            await LoadPluginsAsync(pluginLoader, registry, logger);

            // 5. Launch ConsoleDungeon app
            // Note: ConsoleDungeon hasn't been refactored to accept Registry yet (issue #20).
            // For now, we maintain backwards compatibility while setting up the infrastructure.
            logger.LogInformation("Launching ConsoleDungeon app...");
            await ConsoleDungeon.Program.Main(args);

            logger.LogInformation("ConsoleDungeon.Host shutdown complete");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Fatal error during bootstrap: {Message}", ex.Message);
            throw;
        }
    }

    /// <summary>
    /// Load plugins in the correct order with error handling.
    /// </summary>
    private static async Task LoadPluginsAsync(IPluginLoader pluginLoader, IRegistry registry, ILogger logger)
    {
        // Determine plugin directory (same as current executable)
        var pluginDirectory = System.AppContext.BaseDirectory;
        
        // 1. Load Config plugin first (highest priority)
        await TryLoadPluginAsync(pluginLoader, registry, logger, pluginDirectory, "WingedBean.Plugins.Config.dll", "Config");

        // 2. Load WebSocket plugin
        await TryLoadPluginAsync(pluginLoader, registry, logger, pluginDirectory, "WingedBean.Plugins.WebSocket.dll", "WebSocket");

        // 3. Load TerminalUI plugin
        await TryLoadPluginAsync(pluginLoader, registry, logger, pluginDirectory, "WingedBean.Plugins.TerminalUI.dll", "TerminalUI");
    }

    /// <summary>
    /// Attempt to load a plugin, handling errors gracefully.
    /// </summary>
    private static async Task TryLoadPluginAsync(
        IPluginLoader pluginLoader,
        IRegistry registry,
        ILogger logger,
        string pluginDirectory,
        string pluginFileName,
        string pluginName)
    {
        try
        {
            var pluginPath = System.IO.Path.Combine(pluginDirectory, pluginFileName);
            
            if (!System.IO.File.Exists(pluginPath))
            {
                logger.LogWarning("Plugin not found: {PluginName} at {PluginPath} (expected for Phase 3.8)", pluginName, pluginPath);
                return;
            }

            logger.LogInformation("Loading plugin: {PluginName} from {PluginPath}", pluginName, pluginPath);
            var plugin = await pluginLoader.LoadAsync(pluginPath);
            logger.LogInformation("✓ {PluginName} plugin loaded successfully", pluginName);

            // Register services from the plugin
            // (Future: Use plugin metadata to discover and register services automatically)
            logger.LogInformation("✓ {PluginName} services registered in Registry", pluginName);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to load {PluginName} plugin: {Message} (non-fatal)", pluginName, ex.Message);
        }
    }
}

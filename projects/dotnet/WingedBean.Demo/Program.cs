using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using WingedBean.Contracts;
using WingedBean.Host;
using WingedBean.Host.Console;

namespace WingedBean.Demo;

class Program
{
    static async Task Main(string[] args)
    {
        // Set up logging
        using var loggerFactory = LoggerFactory.Create(builder =>
        {
            builder.AddConsole();
            builder.SetMinimumLevel(LogLevel.Information);
        });

        var logger = loggerFactory.CreateLogger<Program>();

        logger.LogInformation("WingedBean Plugin System Demo");

        try
        {
            // Create plugin loader for Console profile
            var pluginLoader = new AlcPluginLoader(loggerFactory.CreateLogger<AlcPluginLoader>());

            // Get plugins directory (assumes build output structure)
            var currentDir = Path.GetDirectoryName(typeof(Program).Assembly.Location)!;
            var pluginsDir = Path.Combine(currentDir, "..", "..", "..", "..", "WingedBean.Plugins.AsciinemaRecorder", "bin", "Debug", "net9.0");
            pluginsDir = Path.GetFullPath(pluginsDir);

            logger.LogInformation("Scanning for plugins in: {PluginsDir}", pluginsDir);

            // Bootstrap the host
            var bootstrap = new HostBootstrap(
                pluginLoader,
                loggerFactory.CreateLogger<HostBootstrap>(),
                pluginsDir
            );

            // Boot the host
            var serviceProvider = await bootstrap.BootAsync();

            // Demonstrate plugin usage
            var recorder = serviceProvider.GetService<IRecorder>();
            if (recorder != null)
            {
                logger.LogInformation("✅ IRecorder service available from plugin!");

                // Test recording functionality
                var sessionId = Guid.NewGuid().ToString();
                var metadata = new SessionMetadata
                {
                    Width = 80,
                    Height = 24,
                    Title = "Demo Session",
                    Command = "demo",
                    WorkingDirectory = Environment.CurrentDirectory
                };

                await recorder.StartRecordingAsync(sessionId, metadata);
                logger.LogInformation("Recording started for session: {SessionId}", sessionId);

                // Simulate some terminal output
                await recorder.RecordDataAsync(sessionId, System.Text.Encoding.UTF8.GetBytes("Hello, World!\r\n"), DateTimeOffset.UtcNow);
                await Task.Delay(1000);
                await recorder.RecordDataAsync(sessionId, System.Text.Encoding.UTF8.GetBytes("This is a test recording.\r\n"), DateTimeOffset.UtcNow);
                await Task.Delay(500);
                await recorder.RecordDataAsync(sessionId, System.Text.Encoding.UTF8.GetBytes("Plugin system is working!\r\n"), DateTimeOffset.UtcNow);

                var outputPath = await recorder.StopRecordingAsync(sessionId);
                logger.LogInformation("Recording saved to: {OutputPath}", outputPath);

                // Show file contents
                if (File.Exists(outputPath))
                {
                    var content = await File.ReadAllTextAsync(outputPath);
                    logger.LogInformation("Recording content:\n{Content}", content);
                }
            }
            else
            {
                logger.LogWarning("❌ IRecorder service not available");
            }

            // Show loaded plugins
            logger.LogInformation("Loaded plugins:");
            foreach (var plugin in bootstrap.LoadedPlugins)
            {
                logger.LogInformation("  - {PluginId} v{Version} ({State})", plugin.Id, plugin.Version, plugin.State);
            }

            // Wait for user input before shutdown
            logger.LogInformation("Press any key to shutdown...");
            Console.ReadKey();

            // Shutdown gracefully
            await bootstrap.ShutdownAsync();
            logger.LogInformation("Demo completed successfully");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Demo failed");
            Environment.Exit(1);
        }
    }
}

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Dungeon.Host.SadConsole;
using HostLand.Desktop;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Plate.PluginManoi.Contracts;
using Plate.PluginManoi.Loader;
using Plate.PluginManoi.Loader.AssemblyContext;
using Plate.PluginManoi.Registry;
using Scrutor;
// Type aliases
using IRegistry = Plate.CrossMilo.Contracts.IRegistry;
using IUIApp = Plate.CrossMilo.Contracts.UI.App.IService;

// Desktop host entry point with dynamic plugin loading.
// Initializes Registry, loads plugins from configuration, and launches SadConsole app.

// ============================================================================
// DIAGNOSTIC LOGGING - Capture startup errors
// ============================================================================
var diagnosticLogPath = Path.Combine(
    AppContext.BaseDirectory,
    "logs",
    $"diagnostic-startup-{DateTime.Now:yyyy-MM-dd-HHmmss}.log"
);
Directory.CreateDirectory(Path.GetDirectoryName(diagnosticLogPath)!);

// Create early logger for diagnostic output before host is built
using var earlyLoggerFactory = LoggerFactory.Create(builder =>
{
    builder.AddConsole();
    builder.SetMinimumLevel(LogLevel.Debug);
});
var diagnosticLogger = earlyLoggerFactory.CreateLogger("Diagnostic");

try
{
    diagnosticLogger.LogInformation("=== Dungeon.Host.SadConsole Diagnostic Startup ===");
    diagnosticLogger.LogInformation("Process ID: {ProcessId}", Environment.ProcessId);
    diagnosticLogger.LogInformation("Current Directory: {CurrentDirectory}", Environment.CurrentDirectory);
    diagnosticLogger.LogInformation("Base Directory: {BaseDirectory}", AppContext.BaseDirectory);
    diagnosticLogger.LogInformation("Command Line: {CommandLine}", Environment.CommandLine);

    diagnosticLogger.LogInformation("Starting host builder...");
}
catch (Exception ex)
{
    diagnosticLogger.LogError(ex, "ERROR in pre-initialization");
    throw;
}

Console.WriteLine("Dungeon.Host.SadConsole starting...");
var host = HostLand.Desktop.Host.CreateDesktopBuilder(args)
        .ConfigureAppConfiguration(config =>
        {
            diagnosticLogger.LogInformation("Configuring app configuration...");
            config.AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);
            config.AddEnvironmentVariables(prefix: "DUNGEON_");
            config.AddCommandLine(args);
            diagnosticLogger.LogInformation("App configuration complete");
        })
        .ConfigureServices(services =>
        {
            diagnosticLogger.LogInformation("Configuring services...");

            // Register foundation services
            services.AddSingleton<IRegistry, ActualRegistry>();
            services.AddSingleton<AssemblyContextProvider>();
            services.AddSingleton<Plate.PluginManoi.Contracts.IPluginLoader>(sp =>
            {
                var contextProvider = sp.GetRequiredService<AssemblyContextProvider>();
                var logger = sp.GetService<ILogger<ActualPluginLoader>>();
                return new ActualPluginLoader(contextProvider, logger!);
            });

            // Register plugin loader hosted service (runs before UI app)
            services.AddHostedService<PluginLoaderHostedService>();

            // Register IUIApp factory that resolves from registry
            // Factory executes lazily when IUIApp is first requested
            services.AddSingleton<IUIApp>(sp =>
            {
                var registry = sp.GetRequiredService<IRegistry>();
                return registry.Get<IUIApp>();
            });

            // Note: UI app will be started on main thread after host.StartAsync()
            // to avoid macOS threading issues with MonoGame/SDL

            // Bridge IHostApplicationLifetime to IRegistry for plugin access
            services.AddSingleton<IHostedService>(sp =>
            {
                var registry = sp.GetRequiredService<IRegistry>();
                var lifetime = sp.GetRequiredService<IHostApplicationLifetime>();
                var logger = sp.GetService<ILogger<Program>>();

                // Register the lifetime in the registry so plugins can access it
                registry.Register<IHostApplicationLifetime>(lifetime);
                logger?.LogInformation("IHostApplicationLifetime registered in IRegistry");

                // Return a no-op hosted service
                return new HostLifetimeBridgeService();
            });

            diagnosticLogger.LogInformation("Services configuration complete");

            // Decorate all hosted services with logging
            services.Decorate<IHostedService, HostedServiceLoggingDecorator>();
        })
        .ConfigureLogging(logging =>
        {
            diagnosticLogger.LogInformation("Configuring logging...");
            var configBuilder = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                .AddEnvironmentVariables(prefix: "DUNGEON_")
                .AddCommandLine(args);
            var configuration = configBuilder.Build();
            logging.AddConfiguration(configuration.GetSection("Logging"));
            diagnosticLogger.LogInformation("Logging configuration complete");
        })
        .Build();

diagnosticLogger.LogInformation("Host build complete");

Console.WriteLine("Host built, starting synchronously on main thread...");
try
{
    diagnosticLogger.LogInformation("Calling host.StartAsync() synchronously...");
    // CRITICAL: Use GetAwaiter().GetResult() to keep execution on main thread
    // MonoGame/SDL on macOS requires the game loop to run on the main thread
    host.StartAsync().GetAwaiter().GetResult();
    diagnosticLogger.LogInformation("host.StartAsync() completed, getting UI app...");

    // Get the UI app and run it on the main thread (required for macOS)
    var uiApp = host.Services.GetRequiredService<IUIApp>();
    diagnosticLogger.LogInformation("Starting UI app on main thread: {AppName}", uiApp.Name);
    diagnosticLogger.LogInformation("UI app type: {Type}", uiApp.GetType().FullName);
    Console.WriteLine($"DEBUG: UI app type: {uiApp.GetType().FullName}");

    // Try to call RunGame() via reflection
    var runGameMethod = uiApp.GetType().GetMethod("RunGame");
    if (runGameMethod != null)
    {
        Console.WriteLine($"DEBUG: Calling RunGame() via reflection...");
        runGameMethod.Invoke(uiApp, null);
        Console.WriteLine($"DEBUG: RunGame() completed");
    }
    else
    {
        Console.WriteLine($"ERROR: RunGame() method not found!");
    }

    diagnosticLogger.LogInformation("UI app completed, stopping host...");
    host.StopAsync().GetAwaiter().GetResult();
    diagnosticLogger.LogInformation("Host stopped successfully");
}
catch (Exception ex)
{
    diagnosticLogger.LogCritical(ex, "FATAL ERROR in host.RunAsync()");
    Console.WriteLine($"Fatal error: {ex.Message}");
    Console.WriteLine(ex.StackTrace);

    // Return exit code 1 for errors
    Environment.ExitCode = 1;
    throw;
}

// ============================================================================
// HELPER CLASSES
// ============================================================================

/// <summary>
/// No-op hosted service that runs early to bridge IHostApplicationLifetime to IRegistry
/// </summary>
class HostLifetimeBridgeService : IHostedService
{
    public Task StartAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}

/// <summary>
/// Hosted service that runs the UI app after plugins are loaded
/// </summary>
class UIAppRunnerHostedService : BackgroundService
{
    private readonly IServiceProvider _services;
    private readonly ILogger<UIAppRunnerHostedService>? _logger;

    public UIAppRunnerHostedService(IServiceProvider services)
    {
        _services = services;
        _logger = services.GetService<ILogger<UIAppRunnerHostedService>>();
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            _logger?.LogInformation("UIAppRunnerHostedService starting...");

            // Give plugins a moment to load
            await Task.Delay(500, stoppingToken);

            var uiApp = _services.GetRequiredService<IUIApp>();
            _logger?.LogInformation("Resolved IUIApp: {AppName}", uiApp.Name);

            // Start the UI app
            _logger?.LogInformation("Starting UI app...");
            await uiApp.StartAsync(stoppingToken);
            _logger?.LogInformation("UI app started successfully");

            // Wait for cancellation
            await Task.Delay(Timeout.Infinite, stoppingToken);
        }
        catch (OperationCanceledException)
        {
            _logger?.LogInformation("UIAppRunnerHostedService cancelled");
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "UIAppRunnerHostedService failed");
            throw;
        }
    }
}

/// <summary>
/// Decorator that logs hosted service lifecycle events
/// </summary>
class HostedServiceLoggingDecorator : IHostedService
{
    private readonly IHostedService _inner;
    private readonly ILogger<HostedServiceLoggingDecorator> _logger;

    public HostedServiceLoggingDecorator(IHostedService inner, ILogger<HostedServiceLoggingDecorator> logger)
    {
        _inner = inner;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        var serviceName = _inner.GetType().Name;
        _logger.LogInformation("→ Starting {ServiceName}", serviceName);
        try
        {
            await _inner.StartAsync(cancellationToken);
            _logger.LogInformation("✓ Started {ServiceName}", serviceName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "✗ Failed to start {ServiceName}", serviceName);
            throw;
        }
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        var serviceName = _inner.GetType().Name;
        _logger.LogInformation("→ Stopping {ServiceName}", serviceName);
        try
        {
            await _inner.StopAsync(cancellationToken);
            _logger.LogInformation("✓ Stopped {ServiceName}", serviceName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "✗ Failed to stop {ServiceName}", serviceName);
            throw;
        }
    }
}

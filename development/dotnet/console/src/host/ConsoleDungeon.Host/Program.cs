using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using ConsoleDungeon.Host;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Scrutor;
using Plate.PluginManoi.Contracts;
using Plate.CrossMilo.Contracts;
using Plate.PluginManoi.Registry;
using Plate.PluginManoi.Loader;
using Plate.PluginManoi.Loader.AssemblyContext;
using Plate.PluginManoi.Core;
using WingedBean.Host.Console;
using WingedBean.Hosting;

// Terminal and UI contracts now in Plate.CrossMilo.Contracts namespace
using ITerminalApp = Plate.CrossMilo.Contracts.TerminalUI.ITerminalApp;
using TerminalAppConfig = Plate.CrossMilo.Contracts.TerminalUI.TerminalAppConfig;
using LegacyTerminalAppAdapter = Plate.CrossMilo.Contracts.Terminal.LegacyTerminalAppAdapter;
// Console host entry point with dynamic plugin loading.
// Initializes Registry, loads plugins from configuration, and launches ConsoleDungeon app.

// ============================================================================
// DIAGNOSTIC LOGGING - Capture startup errors for PTY debugging
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

// Also write to diagnostic log file manually for troubleshooting
void WriteDiagnosticLog(string message)
{
    try
    {
        var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
        File.AppendAllText(diagnosticLogPath, $"[{timestamp}] {message}\n");
    }
    catch { /* Ignore file write errors */ }
}

try
{
    diagnosticLogger.LogInformation("=== ConsoleDungeon.Host Diagnostic Startup ===");
    diagnosticLogger.LogInformation("Process ID: {ProcessId}", Environment.ProcessId);
    diagnosticLogger.LogInformation("Current Directory: {CurrentDirectory}", Environment.CurrentDirectory);
    diagnosticLogger.LogInformation("Base Directory: {BaseDirectory}", AppContext.BaseDirectory);
    diagnosticLogger.LogInformation("Command Line: {CommandLine}", Environment.CommandLine);
    diagnosticLogger.LogInformation("Is Interactive: {IsInteractive}", Environment.UserInteractive);

    // Ensure sane TERM defaults to avoid ncurses init errors in headless
    var term = Environment.GetEnvironmentVariable("TERM");
    if (string.IsNullOrWhiteSpace(term))
    {
        Environment.SetEnvironmentVariable("TERM", "xterm-256color");
        term = "xterm-256color";
    }
    var colorTerm = Environment.GetEnvironmentVariable("COLORTERM");
    if (string.IsNullOrWhiteSpace(colorTerm))
    {
        Environment.SetEnvironmentVariable("COLORTERM", "truecolor");
        colorTerm = "truecolor";
    }
    diagnosticLogger.LogInformation("TERM: {Term}", term);
    diagnosticLogger.LogInformation("COLORTERM: {ColorTerm}", colorTerm);
    diagnosticLogger.LogInformation("Console.IsInputRedirected: {IsInputRedirected}", Console.IsInputRedirected);
    diagnosticLogger.LogInformation("Console.IsOutputRedirected: {IsOutputRedirected}", Console.IsOutputRedirected);
    diagnosticLogger.LogInformation("Console.IsErrorRedirected: {IsErrorRedirected}", Console.IsErrorRedirected);

    try
    {
        diagnosticLogger.LogDebug("Console.BufferHeight: {BufferHeight}", Console.BufferHeight);
        diagnosticLogger.LogDebug("Console.BufferWidth: {BufferWidth}", Console.BufferWidth);
        diagnosticLogger.LogDebug("Console.WindowHeight: {WindowHeight}", Console.WindowHeight);
        diagnosticLogger.LogDebug("Console.WindowWidth: {WindowWidth}", Console.WindowWidth);
    }
    catch (Exception ex)
    {
        diagnosticLogger.LogWarning(ex, "Console properties unavailable");
    }

    diagnosticLogger.LogInformation("Starting host builder...");
}
catch (Exception ex)
{
    diagnosticLogger.LogError(ex, "ERROR in pre-initialization");
    throw;
}

Console.WriteLine("ConsoleDungeon.Host starting...");
var host = WingedBeanHost.CreateConsoleBuilder(args)
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
            // Register configuration - we'll need to build it manually since we don't have context
            var configBuilder = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                .AddEnvironmentVariables(prefix: "DUNGEON_")
                .AddCommandLine(args);
            var configuration = configBuilder.Build();

            services.Configure<TerminalAppConfig>(
                configuration.GetSection("Terminal"));

            // Register foundation services
            services.AddSingleton<IRegistry, ActualRegistry>();
            services.AddSingleton<AssemblyContextProvider>();
            services.AddSingleton<Plate.PluginManoi.Contracts.IPluginLoader>(sp =>
            {
                var contextProvider = sp.GetRequiredService<AssemblyContextProvider>();
                var logger = sp.GetService<ILogger<ActualPluginLoader>>();
                return new ActualPluginLoader(contextProvider, logger!);
            });

            // Register plugin loader hosted service (runs before terminal app)
            services.AddHostedService<PluginLoaderHostedService>();

            // Register ITerminalApp factory that resolves from registry
            // Factory executes lazily when ITerminalApp is first requested
            services.AddSingleton<ITerminalApp>(sp =>
            {
                var registry = sp.GetRequiredService<IRegistry>();
                return registry.Get<ITerminalApp>();
            });

            // Detect headless/non-interactive environment: avoid starting Terminal UI here
            var headless = false;
            try
            {
                headless = Console.IsOutputRedirected || Console.IsInputRedirected ||
                           string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("TERM")) ||
                           (Environment.GetEnvironmentVariable("DUNGEON_HEADLESS") ?? "")
                               .Equals("1", StringComparison.OrdinalIgnoreCase);
            }
            catch { headless = true; }

            if (headless)
            {
                diagnosticLogger.LogInformation("Headless environment detected: skipping LegacyTerminalAppAdapter registration");
                // Start a lightweight keepalive + WebSocket bootstrapper so the host
                // stays running and can accept connections from the web UI.
                services.AddHostedService<HeadlessKeepaliveHostedService>();
                services.AddHostedService<HeadlessBlockingService>();
                services.AddHostedService<WebSocketBootstrapperHostedService>();
            }
            else
            {
                // Prefer a resilient runner that doesn't fail startup if the UI exits early.
                // It resolves ITerminalApp after plugins load and runs it in the background.
                services.AddHostedService<TerminalAppRunnerHostedService>();
                services.AddHostedService<WebSocketBootstrapperHostedService>();
                // Keep the host alive even if UI returns quickly; Ctrl+C still stops the app
                services.AddHostedService<KeepAliveHostedService>();
            }

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

            // Decorate all hosted services with a startup sentinel that logs Start/Stop entry/exit
            // This helps identify which service cancels or throws during Host.StartAsync
            services.Decorate<IHostedService, HostedServiceLoggingDecorator>();
        })
        .ConfigureLogging(logging =>
        {
            diagnosticLogger.LogInformation("Configuring logging...");
            // Note: Configuration needs to be built separately for logging
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

Console.WriteLine("Host built, starting RunAsync...");
try
{
    diagnosticLogger.LogInformation("Calling host.RunAsync()...");
    await host.RunAsync();
    diagnosticLogger.LogInformation("host.RunAsync() completed successfully");
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

// No-op hosted service that runs early to bridge IHostApplicationLifetime to IRegistry
class HostLifetimeBridgeService : IHostedService
{
    public Task StartAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}

// Keeps the host alive in headless environments and boots the WebSocket service.
// This ensures verify scripts and PTY web UI can connect to the host.
class HeadlessKeepaliveHostedService : IHostedService
{
    private readonly IServiceProvider _services;
    private readonly ILogger<HeadlessKeepaliveHostedService>? _logger;
    private Task? _background;
    private CancellationTokenSource? _cts;

    public HeadlessKeepaliveHostedService(IServiceProvider services)
    {
        _services = services;
        _logger = services.GetService<ILogger<HeadlessKeepaliveHostedService>>();
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        var ct = _cts.Token;

        _background = Task.Run(async () =>
        {
            try
            {
                _logger?.LogInformation("HeadlessKeepalive StartAsync");
                // Give the PluginLoaderHostedService a moment to register services
                await Task.Delay(200, ct);

                var registry = _services.GetRequiredService<IRegistry>();

                // Try to start WebSocket service on configured port (default 4040)
                var port = 4040;
                var envPort = Environment.GetEnvironmentVariable("DUNGEON_WS_PORT");
                if (!string.IsNullOrWhiteSpace(envPort) && int.TryParse(envPort, out var p))
                    port = p;

                try
                {
                    // Resolve IWebSocket service by name without compile-time reference
                    var wsInterface = AppDomain.CurrentDomain
                        .GetAssemblies()
                        .SelectMany(a =>
                        {
                            try { return a.GetTypes(); } catch { return Array.Empty<Type>(); }
                        })
                        .FirstOrDefault(t => t.FullName == "Plate.CrossMilo.Contracts.WebSocket.Services.IService");
                    if (wsInterface != null)
                    {
                        var getAll = typeof(IRegistry).GetMethod("GetAll")!.MakeGenericMethod(wsInterface);
                        object? resolved = null;
                        for (var i = 0; i < 25 && !ct.IsCancellationRequested; i++)
                        {
                            var results = getAll.Invoke(registry, Array.Empty<object>());
                            var enumerable = results as System.Collections.IEnumerable;
                            var list = enumerable?.Cast<object>().ToList();
                            _logger?.LogDebug("IWebSocket candidates: {Count} (attempt {Attempt})", list?.Count ?? 0, i + 1);
                            resolved = list?.FirstOrDefault();
                            if (resolved != null) break;
                            await Task.Delay(200, ct);
                        }

                        if (resolved != null)
                        {
                            var startMethod = wsInterface.GetMethod("Start", new[] { typeof(int) });
                            startMethod?.Invoke(resolved, new object[] { port });
                            _logger?.LogInformation("WebSocket server start requested on port {Port}", port);
                        }
                        else
                        {
                            _logger?.LogWarning("IWebSocket service not found after waiting; attempting direct instantiation");
                            // Fallback: try to instantiate SuperSocketWebSocketService directly
                            var implType = AppDomain.CurrentDomain
                                .GetAssemblies()
                                .SelectMany(a => { try { return a.GetTypes(); } catch { return Array.Empty<Type>(); } })
                                .FirstOrDefault(t => t.FullName == "WingedBean.Plugins.WebSocket.SuperSocketWebSocketService");
                            if (implType != null)
                            {
                                try
                                {
                                    var instance = Activator.CreateInstance(implType);
                                    var start = implType.GetMethod("Start", new[] { typeof(int) });
                                    start?.Invoke(instance, new object[] { port });
                                    _logger?.LogInformation("Direct WebSocket start requested on port {Port}", port);
                                }
                                catch (Exception ex)
                                {
                                    _logger?.LogWarning(ex, "Direct WebSocket start failed");
                                }
                            }
                            else
                            {
                                _logger?.LogWarning("WebSocket implementation type not found");
                            }
                        }
                    }
                    else
                    {
                        _logger?.LogWarning("WebSocket interface type not found");
                    }
                }
                catch (Exception ex)
                {
                    _logger?.LogWarning(ex, "WebSocket service not available or failed to start");
                }

                // Idle until cancellation
                try
                {
                    await Task.Delay(Timeout.Infinite, ct);
                }
                catch (OperationCanceledException) { }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Headless keepalive task failed");
            }
        }, ct);

        return Task.CompletedTask;
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        try
        {
            _cts?.Cancel();
            if (_background != null)
            {
                var t = await Task.WhenAny(_background, Task.Delay(1000, cancellationToken));
            }
        }
        catch { }
    }
}

// Simple BackgroundService that blocks until cancellation to keep host alive in headless mode
sealed class HeadlessBlockingService : BackgroundService
{
    protected override Task ExecuteAsync(CancellationToken stoppingToken)
        => Task.Delay(Timeout.Infinite, stoppingToken);
}

// Always-on keepalive to prevent premature host exit when UI returns quickly.
// The process can still be stopped via Ctrl+C, signals, or IHostApplicationLifetime.
sealed class KeepAliveHostedService : BackgroundService
{
    protected override Task ExecuteAsync(CancellationToken stoppingToken)
        => Task.Delay(Timeout.Infinite, stoppingToken);
}

// Attempts to resolve and start the IWebSocket service on startup (port 4040 by default).
// Uses reflection to avoid compile-time dependency.
class WebSocketBootstrapperHostedService : IHostedService
{
    private readonly IServiceProvider _services;
    private readonly IHostApplicationLifetime _lifetime;
    private readonly ILogger<WebSocketBootstrapperHostedService>? _logger;
    private Task? _startupTask;

    public WebSocketBootstrapperHostedService(IServiceProvider services, IHostApplicationLifetime lifetime)
    {
        _services = services;
        _lifetime = lifetime;
        _logger = services.GetService<ILogger<WebSocketBootstrapperHostedService>>();
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        // Start the WebSocket bootstrapping in the background to avoid blocking host startup
        // Use ApplicationStopping token instead of the startup cancellationToken
        _startupTask = Task.Run(async () =>
        {
            try
            {
                _logger?.LogInformation("WebSocketBootstrapper StartAsync");
                // Give plugin loader a moment to complete
                await Task.Delay(500);

                var registry = _services.GetRequiredService<IRegistry>();

                // Find interface type across all ALCs
                var wsInterface = AppDomain.CurrentDomain
                    .GetAssemblies()
                    .SelectMany(a => { try { return a.GetTypes(); } catch { return Array.Empty<Type>(); } })
                    .FirstOrDefault(t => t.FullName == "Plate.CrossMilo.Contracts.WebSocket.Services.IService");
                if (wsInterface == null)
                {
                    _logger?.LogWarning("IWebSocket interface not found");
                    return;
                }

                var getAll = typeof(IRegistry).GetMethod("GetAll")!.MakeGenericMethod(wsInterface);
                // small wait loop to let plugin activation register the service
                object? resolved = null;
                for (var i = 0; i < 25 && !_lifetime.ApplicationStopping.IsCancellationRequested; i++)
                {
                    var results = getAll.Invoke(registry, Array.Empty<object>());
                    var list = (results as System.Collections.IEnumerable)?.Cast<object>().ToList() ?? new List<object>();
                    _logger?.LogDebug("IWebSocket candidates: {Count} (attempt {Attempt})", list.Count, i + 1);
                    resolved = list.FirstOrDefault();
                    if (resolved != null) break;
                    await Task.Delay(200);
                }

                if (resolved == null)
                {
                    _logger?.LogWarning("No IWebSocket service registered");
                    return;
                }

                // Start on configured port (default 4040)
                var port = 4040;
                var envPort = Environment.GetEnvironmentVariable("DUNGEON_WS_PORT");
                if (!string.IsNullOrWhiteSpace(envPort) && int.TryParse(envPort, out var p)) port = p;

                var startMethod = wsInterface.GetMethod("Start", new[] { typeof(int) });
                startMethod?.Invoke(resolved, new object[] { port });
                _logger?.LogInformation("WebSocket server start requested on port {Port}", port);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "WebSocketBootstrapper error");
            }
        });

        // Return immediately without waiting for background task
        return Task.CompletedTask;
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        if (_startupTask != null)
        {
            await Task.WhenAny(_startupTask, Task.Delay(1000, cancellationToken));
        }
    }
}

// Runs ITerminalApp in the background so Host.StartAsync does not fail if the UI exits quickly.
// Honors DUNGEON_EXIT_ON_UI=1 to stop the host when the UI completes normally.
sealed class TerminalAppRunnerHostedService : IHostedService
{
    private readonly IServiceProvider _services;
    private readonly IHostApplicationLifetime _lifetime;
    private readonly ILogger<TerminalAppRunnerHostedService>? _logger;
    private Task? _runner;
    private CancellationTokenSource? _cts;

    public TerminalAppRunnerHostedService(IServiceProvider services, IHostApplicationLifetime lifetime)
    {
        _services = services;
        _lifetime = lifetime;
        _logger = services.GetService<ILogger<TerminalAppRunnerHostedService>>();
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        // Link with application stop but do not let a canceled Start token abort startup
        // Use only the ApplicationStopping token to avoid premature cancellation during startup
        _cts = CancellationTokenSource.CreateLinkedTokenSource(_lifetime.ApplicationStopping);
        var ct = _cts.Token;

        _runner = Task.Run(async () =>
        {
            try
            {
                var app = _services.GetRequiredService<ITerminalApp>();
                _logger?.LogInformation("TerminalAppRunner: starting ITerminalApp {Type}", app.GetType().FullName);
                await app.StartAsync(ct);

                // If UI returned without cancellation and exit-on-ui is requested, stop the host.
                var exitOnUi = (Environment.GetEnvironmentVariable("DUNGEON_EXIT_ON_UI") ?? "0")
                    .Equals("1", StringComparison.OrdinalIgnoreCase) ||
                               (Environment.GetEnvironmentVariable("DUNGEON_EXIT_ON_UI") ?? "false")
                                   .Equals("true", StringComparison.OrdinalIgnoreCase);
                if (!ct.IsCancellationRequested && exitOnUi)
                {
                    _logger?.LogInformation("TerminalAppRunner: UI completed; requesting host stop (DUNGEON_EXIT_ON_UI)");
                    _lifetime.StopApplication();
                }
            }
            catch (OperationCanceledException)
            {
                // Normal on shutdown
                _logger?.LogDebug("TerminalAppRunner: canceled");
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "TerminalAppRunner: ITerminalApp failed");
            }
        }, CancellationToken.None);

        // Do not block startup; background runner manages the UI lifecycle
        return Task.CompletedTask;
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        try
        {
            _cts?.Cancel();
            if (_runner != null)
            {
                await Task.WhenAny(_runner, Task.Delay(2000, cancellationToken));
            }
        }
        catch { }
    }
}

// Decorator that logs StartAsync/StopAsync entry and exit for each hosted service.
// Useful to pinpoint which service cancels or throws during Host.StartAsync.
sealed class HostedServiceLoggingDecorator : IHostedService
{
    private readonly IHostedService _inner;
    private readonly ILogger<HostedServiceLoggingDecorator>? _logger;
    private readonly string _innerTypeName;

    public HostedServiceLoggingDecorator(IHostedService inner, IServiceProvider services)
    {
        _inner = inner;
        _logger = services.GetService<ILogger<HostedServiceLoggingDecorator>>();
        _innerTypeName = inner.GetType().FullName ?? inner.GetType().Name;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _logger?.LogInformation("[StartupSentinel] -> StartAsync: {ServiceType}", _innerTypeName);
        try
        {
            await _inner.StartAsync(cancellationToken);
            _logger?.LogInformation("[StartupSentinel] <- StartAsync OK: {ServiceType}", _innerTypeName);
        }
        catch (OperationCanceledException oce)
        {
            _logger?.LogWarning(oce, "[StartupSentinel] <- StartAsync CANCELED: {ServiceType}", _innerTypeName);
            throw;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "[StartupSentinel] <- StartAsync FAILED: {ServiceType}", _innerTypeName);
            throw;
        }
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger?.LogInformation("[StartupSentinel] -> StopAsync: {ServiceType}", _innerTypeName);
        try
        {
            await _inner.StopAsync(cancellationToken);
            _logger?.LogInformation("[StartupSentinel] <- StopAsync OK: {ServiceType}", _innerTypeName);
        }
        catch (OperationCanceledException oce)
        {
            _logger?.LogWarning(oce, "[StartupSentinel] <- StopAsync CANCELED: {ServiceType}", _innerTypeName);
            throw;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "[StartupSentinel] <- StopAsync FAILED: {ServiceType}", _innerTypeName);
            throw;
        }
    }
}

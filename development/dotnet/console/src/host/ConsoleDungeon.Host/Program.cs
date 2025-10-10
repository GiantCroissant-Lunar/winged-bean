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

void LogDiagnostic(string message)
{
    var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
    var logMessage = $"[{timestamp}] {message}\n";
    File.AppendAllText(diagnosticLogPath, logMessage);
    Console.WriteLine(message);
}

try
{
    LogDiagnostic("=== ConsoleDungeon.Host Diagnostic Startup ===");
    LogDiagnostic($"Process ID: {Environment.ProcessId}");
    LogDiagnostic($"Current Directory: {Environment.CurrentDirectory}");
    LogDiagnostic($"Base Directory: {AppContext.BaseDirectory}");
    LogDiagnostic($"Command Line: {Environment.CommandLine}");
    LogDiagnostic($"Is Interactive: {Environment.UserInteractive}");
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
    LogDiagnostic($"TERM: {term}");
    LogDiagnostic($"COLORTERM: {colorTerm}");
    LogDiagnostic($"Console.IsInputRedirected: {Console.IsInputRedirected}");
    LogDiagnostic($"Console.IsOutputRedirected: {Console.IsOutputRedirected}");
    LogDiagnostic($"Console.IsErrorRedirected: {Console.IsErrorRedirected}");
    
    try
    {
        LogDiagnostic($"Console.BufferHeight: {Console.BufferHeight}");
        LogDiagnostic($"Console.BufferWidth: {Console.BufferWidth}");
        LogDiagnostic($"Console.WindowHeight: {Console.WindowHeight}");
        LogDiagnostic($"Console.WindowWidth: {Console.WindowWidth}");
    }
    catch (Exception ex)
    {
        LogDiagnostic($"Console properties unavailable: {ex.Message}");
    }
    
    LogDiagnostic("Starting host builder...");
}
catch (Exception ex)
{
    LogDiagnostic($"ERROR in pre-initialization: {ex}");
    throw;
}

Console.WriteLine("ConsoleDungeon.Host starting...");
var host = WingedBeanHost.CreateConsoleBuilder(args)
        .ConfigureAppConfiguration(config =>
        {
            LogDiagnostic("Configuring app configuration...");
            Console.WriteLine("Configuring app configuration...");
            config.AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);
            config.AddEnvironmentVariables(prefix: "DUNGEON_");
            config.AddCommandLine(args);
            LogDiagnostic("App configuration complete");
        })
        .ConfigureServices(services =>
        {
            LogDiagnostic("Configuring services...");
            Console.WriteLine("Configuring services...");
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
                LogDiagnostic("Headless environment detected: skipping LegacyTerminalAppAdapter registration");
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
                
                // Register the lifetime in the registry so plugins can access it
                registry.Register<IHostApplicationLifetime>(lifetime);
                Console.WriteLine("[Program] IHostApplicationLifetime registered in IRegistry");
                
                // Return a no-op hosted service
                return new HostLifetimeBridgeService();
            });
            LogDiagnostic("Services configuration complete");

            // Decorate all hosted services with a startup sentinel that logs Start/Stop entry/exit
            // This helps identify which service cancels or throws during Host.StartAsync
            services.Decorate<IHostedService, HostedServiceLoggingDecorator>();
        })
        .ConfigureLogging(logging =>
        {
            LogDiagnostic("Configuring logging...");
            Console.WriteLine("Configuring logging...");
            // Note: Configuration needs to be built separately for logging
            var configBuilder = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                .AddEnvironmentVariables(prefix: "DUNGEON_")
                .AddCommandLine(args);
            var configuration = configBuilder.Build();
            logging.AddConfiguration(configuration.GetSection("Logging"));
            LogDiagnostic("Logging configuration complete");
        })
        .Build();

LogDiagnostic("Host build complete");

Console.WriteLine("Host built, starting RunAsync...");
try
{
    LogDiagnostic("Calling host.RunAsync()...");
    await host.RunAsync();
    LogDiagnostic("host.RunAsync() completed successfully");
}
catch (Exception ex)
{
    LogDiagnostic($"FATAL ERROR in host.RunAsync(): {ex.GetType().Name}: {ex.Message}");
    LogDiagnostic($"Stack trace: {ex.StackTrace}");
    LogDiagnostic($"Inner exception: {ex.InnerException?.ToString() ?? "(none)"}");
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
                Console.WriteLine("[HeadlessKeepalive] StartAsync");
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
                            Console.WriteLine($"[HeadlessKeepalive] IWebSocket candidates: {list?.Count ?? 0} (attempt {i+1})");
                            resolved = list?.FirstOrDefault();
                            if (resolved != null) break;
                            await Task.Delay(200, ct);
                        }

                        if (resolved != null)
                        {
                            var startMethod = wsInterface.GetMethod("Start", new[] { typeof(int) });
                            startMethod?.Invoke(resolved, new object[] { port });
                            Console.WriteLine($"[HeadlessKeepalive] WebSocket server start requested on {port}");
                            _logger?.LogInformation("Headless keepalive: WebSocket server started on port {Port}", port);
                        }
                        else
                        {
                            Console.WriteLine("[HeadlessKeepalive] IWebSocket service not found after waiting; attempting direct instantiation");
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
                                    Console.WriteLine($"[HeadlessKeepalive] Direct WebSocket start requested on {port}");
                                }
                                catch (Exception ex)
                                {
                                    Console.WriteLine($"[HeadlessKeepalive] Direct WebSocket start failed: {ex.Message}");
                                }
                            }
                            else
                            {
                                Console.WriteLine("[HeadlessKeepalive] WebSocket implementation type not found");
                            }
                        }
                    }
                    else
                    {
                        _logger?.LogWarning("Headless keepalive: WebSocket interface type not found");
                    }
                }
                catch (Exception ex)
                {
                    _logger?.LogWarning(ex, "Headless keepalive: WebSocket service not available or failed to start");
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
    private Task? _startupTask;

    public WebSocketBootstrapperHostedService(IServiceProvider services, IHostApplicationLifetime lifetime)
    {
        _services = services;
        _lifetime = lifetime;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        // Start the WebSocket bootstrapping in the background to avoid blocking host startup
        // Use ApplicationStopping token instead of the startup cancellationToken
        _startupTask = Task.Run(async () =>
        {
            try
            {
                Console.WriteLine("[WebSocketBootstrapper] StartAsync");
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
                    Console.WriteLine("[WebSocketBootstrapper] IWebSocket interface not found");
                    return;
                }

                var getAll = typeof(IRegistry).GetMethod("GetAll")!.MakeGenericMethod(wsInterface);
                // small wait loop to let plugin activation register the service
                object? resolved = null;
                for (var i = 0; i < 25 && !_lifetime.ApplicationStopping.IsCancellationRequested; i++)
                {
                    var results = getAll.Invoke(registry, Array.Empty<object>());
                    var list = (results as System.Collections.IEnumerable)?.Cast<object>().ToList() ?? new List<object>();
                    Console.WriteLine($"[WebSocketBootstrapper] candidates: {list.Count} (attempt {i + 1})");
                    resolved = list.FirstOrDefault();
                    if (resolved != null) break;
                    await Task.Delay(200);
                }

                if (resolved == null)
                {
                    Console.WriteLine("[WebSocketBootstrapper] No IWebSocket service registered");
                    return;
                }

                // Start on configured port (default 4040)
                var port = 4040;
                var envPort = Environment.GetEnvironmentVariable("DUNGEON_WS_PORT");
                if (!string.IsNullOrWhiteSpace(envPort) && int.TryParse(envPort, out var p)) port = p;

                var startMethod = wsInterface.GetMethod("Start", new[] { typeof(int) });
                startMethod?.Invoke(resolved, new object[] { port });
                Console.WriteLine($"[WebSocketBootstrapper] WebSocket server start requested on {port}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[WebSocketBootstrapper] Error: {ex.Message}");
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
        var msgStart = $"[StartupSentinel] -> StartAsync: {_innerTypeName}";
        Console.WriteLine(msgStart);
        _logger?.LogInformation("{Message}", msgStart);
        try
        {
            await _inner.StartAsync(cancellationToken);
            var msgOk = $"[StartupSentinel] <- StartAsync OK: {_innerTypeName}";
            Console.WriteLine(msgOk);
            _logger?.LogInformation("{Message}", msgOk);
        }
        catch (OperationCanceledException oce)
        {
            var msgCanceled = $"[StartupSentinel] <- StartAsync CANCELED: {_innerTypeName}: {oce.Message}";
            Console.WriteLine(msgCanceled);
            _logger?.LogWarning(oce, "{Message}", msgCanceled);
            throw;
        }
        catch (Exception ex)
        {
            var msgFail = $"[StartupSentinel] <- StartAsync FAILED: {_innerTypeName}: {ex.Message}";
            Console.WriteLine(msgFail);
            _logger?.LogError(ex, "{Message}", msgFail);
            throw;
        }
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        var msgStart = $"[StartupSentinel] -> StopAsync: {_innerTypeName}";
        Console.WriteLine(msgStart);
        _logger?.LogInformation("{Message}", msgStart);
        try
        {
            await _inner.StopAsync(cancellationToken);
            var msgOk = $"[StartupSentinel] <- StopAsync OK: {_innerTypeName}";
            Console.WriteLine(msgOk);
            _logger?.LogInformation("{Message}", msgOk);
        }
        catch (OperationCanceledException oce)
        {
            var msgCanceled = $"[StartupSentinel] <- StopAsync CANCELED: {_innerTypeName}: {oce.Message}";
            Console.WriteLine(msgCanceled);
            _logger?.LogWarning(oce, "{Message}", msgCanceled);
            throw;
        }
        catch (Exception ex)
        {
            var msgFail = $"[StartupSentinel] <- StopAsync FAILED: {_innerTypeName}: {ex.Message}";
            Console.WriteLine(msgFail);
            _logger?.LogError(ex, "{Message}", msgFail);
            throw;
        }
    }
}

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
using WingedBean.Contracts.Core;
using WingedBean.Contracts.Terminal;
using WingedBean.Contracts.Game;
using WingedBean.Registry;
using WingedBean.PluginLoader;
using WingedBean.Providers.AssemblyContext;
using WingedBean.PluginSystem;
using WingedBean.Host.Console;
using WingedBean.Hosting;
// Console host entry point with dynamic plugin loading.
// Initializes Registry, loads plugins from configuration, and launches ConsoleDungeon app.

Console.WriteLine("ConsoleDungeon.Host starting...");
var host = WingedBeanHost.CreateConsoleBuilder(args)
        .ConfigureAppConfiguration(config =>
        {
            Console.WriteLine("Configuring app configuration...");
            config.AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);
            config.AddEnvironmentVariables(prefix: "DUNGEON_");
            config.AddCommandLine(args);
        })
        .ConfigureServices(services =>
        {
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
            services.AddSingleton<WingedBean.Contracts.Core.IPluginLoader, ActualPluginLoader>();

            // Register plugin loader hosted service (runs before terminal app)
            services.AddHostedService<PluginLoaderHostedService>();

            // Register terminal app and adapter - deferred resolution after plugins load
            services.AddSingleton<ITerminalApp>(sp => new LazyTerminalAppResolver(sp.GetRequiredService<IRegistry>()));

            services.AddHostedService<LegacyTerminalAppAdapter>();

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
        })
        .ConfigureLogging(logging =>
        {
            Console.WriteLine("Configuring logging...");
            // Note: Configuration needs to be built separately for logging
            var configBuilder = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                .AddEnvironmentVariables(prefix: "DUNGEON_")
                .AddCommandLine(args);
            var configuration = configBuilder.Build();
            logging.AddConfiguration(configuration.GetSection("Logging"));
        })
        .Build();

Console.WriteLine("Host built, starting RunAsync...");
try
{
    await host.RunAsync();
}
catch (Exception ex)
{
    Console.WriteLine($"Fatal error: {ex.Message}");
    Console.WriteLine(ex.StackTrace);
    throw;
}

class LazyTerminalAppResolver : ITerminalApp
{
    private readonly IRegistry _registry;
    private ITerminalApp? _resolvedApp;

    public LazyTerminalAppResolver(IRegistry registry)
    {
        _registry = registry;
    }

    private ITerminalApp GetApp()
    {
        if (_resolvedApp == null)
        {
            try
            {
                _resolvedApp = _registry.Get<ITerminalApp>();
                try
                {
                    var t = _resolvedApp.GetType();
                    var asm = t.Assembly;
                    Console.WriteLine($"ITerminalApp resolved from registry successfully");
                    Console.WriteLine($"  → Type: {t.FullName}");
                    Console.WriteLine($"  → Assembly: {System.IO.Path.GetFileName(asm.Location)}");
                    Console.WriteLine($"  → Path: {asm.Location}");
                }
                catch { }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ITerminalApp not found in registry: {ex.Message}");
                throw new InvalidOperationException("Terminal app not available - plugins may not have loaded correctly", ex);
            }
        }
        return _resolvedApp;
    }

    public Task SendInputAsync(byte[] data, CancellationToken ct = default)
        => GetApp().SendInputAsync(data, ct);

    public Task ResizeAsync(int cols, int rows, CancellationToken ct = default)
        => GetApp().ResizeAsync(cols, rows, ct);

    public Task StartAsync(CancellationToken cancellationToken)
        => GetApp().StartAsync(cancellationToken);

    public Task StopAsync(CancellationToken cancellationToken)
        => GetApp().StopAsync(cancellationToken);

    public event EventHandler<TerminalOutputEventArgs>? OutputReceived
    {
        add => GetApp().OutputReceived += value;
        remove => GetApp().OutputReceived -= value;
    }

    public event EventHandler<TerminalExitEventArgs>? Exited
    {
        add => GetApp().Exited += value;
        remove => GetApp().Exited -= value;
    }
}

// No-op hosted service that runs early to bridge IHostApplicationLifetime to IRegistry
class HostLifetimeBridgeService : IHostedService
{
    public Task StartAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}

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

            // Register terminal app adapter
            // LegacyTerminalAppAdapter resolves ITerminalApp from DI in StartAsync()
            // This ensures plugins are loaded by PluginLoaderHostedService first
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

// No-op hosted service that runs early to bridge IHostApplicationLifetime to IRegistry
class HostLifetimeBridgeService : IHostedService
{
    public Task StartAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}

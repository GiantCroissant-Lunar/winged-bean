using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Threading.Tasks;
using ConsoleDungeon.Host;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Scrutor;
using WingedBean.Contracts.TerminalUI;
using WingedBean.Contracts.Core;
using WingedBean.Contracts.Game;
using WingedBean.Registry;
using WingedBean.PluginLoader;
using WingedBean.Providers.AssemblyContext;
using WingedBean.PluginSystem;
using WingedBean.Host.Console;
using WingedBean.Hosting;

// Console host entry point with dynamic plugin loading.
// Initializes Registry, loads plugins from configuration, and launches ConsoleDungeon app.

static async Task Main(string[] args)
{
    var host = WingedBeanHost.CreateConsoleBuilder(args)
        .ConfigureAppConfiguration(config =>
        {
            config.AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);
            config.AddEnvironmentVariables(prefix: "DUNGEON_");
            config.AddCommandLine(args);
        })
        .ConfigureServices(services =>
        {
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

            // Register terminal app and adapter
            services.AddSingleton<ITerminalApp>(sp =>
            {
                var registry = sp.GetRequiredService<IRegistry>();
                return registry.Get<ITerminalApp>();
            });

            services.AddHostedService<LegacyTerminalAppAdapter>();
        })
        .ConfigureLogging(logging =>
        {
            logging.AddConsole();
            // Note: Configuration needs to be built separately for logging
            var configBuilder = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                .AddEnvironmentVariables(prefix: "DUNGEON_")
                .AddCommandLine(args);
            var configuration = configBuilder.Build();
            logging.AddConfiguration(configuration.GetSection("Logging"));
        })
        .Build();

    await host.RunAsync();
}

static async Task<PluginConfiguration> LoadPluginConfigurationAsync(string path)
{
    if (!File.Exists(path))
    {
        throw new FileNotFoundException($"Plugin configuration file not found: {path}");
    }

    var json = await File.ReadAllTextAsync(path);
    var config = JsonSerializer.Deserialize<PluginConfiguration>(json, new JsonSerializerOptions
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip
    });

    return config ?? throw new InvalidOperationException("Failed to parse plugin configuration");
}

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using WingedBean.Contracts.Hosting;

namespace WingedBean.Hosting.Console;

/// <summary>
/// Console/terminal host using .NET Generic Host.
/// Direct wrapper around Microsoft.Extensions.Hosting.IHost.
/// </summary>
public class ConsoleWingedBeanHost : IWingedBeanHost
{
    private readonly IHost _host;

    public ConsoleWingedBeanHost(IHost host)
    {
        _host = host;
    }

    public Task RunAsync(CancellationToken cancellationToken = default)
        => _host.RunAsync(cancellationToken);

    public Task StartAsync(CancellationToken cancellationToken = default)
        => _host.StartAsync(cancellationToken);

    public Task StopAsync(CancellationToken cancellationToken = default)
        => _host.StopAsync(cancellationToken);

    public IServiceProvider Services => _host.Services;
}

/// <summary>
/// Builder for console hosts.
/// </summary>
public class ConsoleWingedBeanHostBuilder : IWingedBeanHostBuilder
{
    private readonly IHostBuilder _hostBuilder;

    public ConsoleWingedBeanHostBuilder(string[] args)
    {
        _hostBuilder = Host.CreateDefaultBuilder(args)
            // Graceful shutdown on Ctrl+C; suppress status messages to avoid
            // interfering with Terminal.Gui alternate screen rendering.
            .UseConsoleLifetime(opts => opts.SuppressStatusMessages = true);
    }

    public IWingedBeanHostBuilder ConfigureServices(Action<IServiceCollection> configure)
    {
        _hostBuilder.ConfigureServices((context, services) => configure(services));
        return this;
    }

    public IWingedBeanHostBuilder ConfigureAppConfiguration(Action<IConfigurationBuilder> configure)
    {
        _hostBuilder.ConfigureAppConfiguration((context, config) => configure(config));
        return this;
    }

    public IWingedBeanHostBuilder ConfigureLogging(Action<ILoggingBuilder> configure)
    {
        _hostBuilder.ConfigureLogging((context, logging) => configure(logging));
        return this;
    }

    public IWingedBeanHost Build()
    {
        var host = _hostBuilder.Build();
        return new ConsoleWingedBeanHost(host);
    }
}

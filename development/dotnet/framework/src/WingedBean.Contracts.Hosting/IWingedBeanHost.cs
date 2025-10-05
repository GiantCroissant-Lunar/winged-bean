using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace WingedBean.Contracts.Hosting;

/// <summary>
/// Abstraction for platform-specific hosts.
/// Wraps .NET Generic Host or native platform lifecycle.
/// </summary>
public interface IWingedBeanHost
{
    /// <summary>
    /// Run the host and block until shutdown.
    /// </summary>
    Task RunAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Start the host without blocking.
    /// </summary>
    Task StartAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Stop the host gracefully.
    /// </summary>
    Task StopAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Access to DI container.
    /// </summary>
    IServiceProvider Services { get; }
}

/// <summary>
/// Builder for configuring Winged Bean hosts.
/// </summary>
public interface IWingedBeanHostBuilder
{
    IWingedBeanHostBuilder ConfigureServices(Action<IServiceCollection> configure);
    IWingedBeanHostBuilder ConfigureAppConfiguration(Action<IConfigurationBuilder> configure);
    IWingedBeanHostBuilder ConfigureLogging(Action<ILoggingBuilder> configure);
    IWingedBeanHost Build();
}

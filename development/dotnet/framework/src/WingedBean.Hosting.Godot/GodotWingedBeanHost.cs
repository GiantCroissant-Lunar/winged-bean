using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Plate.CrossMilo.Contracts.Hosting.Host;
using Plate.CrossMilo.Contracts.Hosting.App;

// Type aliases to avoid IService ambiguity
using IWingedBeanHost = Plate.CrossMilo.Contracts.Hosting.Host.IService;
using IWingedBeanApp = Plate.CrossMilo.Contracts.Hosting.App.IService;
using IWingedBeanHostBuilder = Plate.CrossMilo.Contracts.Hosting.Host.IServiceBuilder;
using IUIApp = Plate.CrossMilo.Contracts.UI.App.IService;

namespace WingedBean.Hosting.Godot;

/// <summary>
/// Godot host that bridges Godot Node lifecycle to IWingedBeanHost.
/// Godot's lifecycle is authoritative (_Ready, _Process, _ExitTree, etc.).
/// </summary>
#if GODOT
public partial class GodotWingedBeanHost : Godot.Node, IWingedBeanHost
{
    private IServiceProvider? _services;
    private IWingedBeanApp? _app;
    private CancellationTokenSource? _cts;
    private Action<IServiceCollection>? _configureServices;

    public IServiceProvider Services => _services
        ?? throw new InvalidOperationException("Host not started");

    public override void _Ready()
    {
        // Build service provider
        var services = new ServiceCollection();

        // Configure services (set by builder before Node creation)
        _configureServices?.Invoke(services);

        _services = services.BuildServiceProvider();
        _app = _services.GetRequiredService<IWingedBeanApp>();

        _cts = new CancellationTokenSource();
        _ = StartAsync(_cts.Token);
    }

    public override void _Process(double delta)
    {
#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
        // Call RenderAsync if app is IUIApp
        if (_app is IUIApp uiApp)
        {
            // Godot _Process is synchronous, so we fire-and-forget
            // In production, consider queuing or using Godot's async patterns
            _ = uiApp.RenderAsync(_cts?.Token ?? default);
        }
#pragma warning restore CS4014
    }

    public override void _ExitTree()
    {
        _ = StopAsync(default);
        _cts?.Dispose();
    }

    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
        if (_app != null)
            await _app.StartAsync(cancellationToken);
    }

    public async Task StopAsync(CancellationToken cancellationToken = default)
    {
        if (_app != null)
            await _app.StopAsync(cancellationToken);
    }

    public Task RunAsync(CancellationToken cancellationToken = default)
    {
        // Godot controls the run loop
        return Task.CompletedTask;
    }

    // Builder state (set before Node is created)
    public static GodotWingedBeanHostBuilder CreateBuilder()
        => new GodotWingedBeanHostBuilder();
}
#else
// Stub implementation for non-Godot environments
public partial class GodotWingedBeanHost : IWingedBeanHost
{
    private IServiceProvider? _services;
    private IWingedBeanApp? _app;
    private CancellationTokenSource? _cts;

    public IServiceProvider Services => _services
        ?? throw new InvalidOperationException("Host not started");

    public GodotWingedBeanHost(IServiceProvider services, IWingedBeanApp app)
    {
        _services = services;
        _app = app;
        _cts = new CancellationTokenSource();
    }

    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
        if (_app != null)
            await _app.StartAsync(cancellationToken);
    }

    public async Task StopAsync(CancellationToken cancellationToken = default)
    {
        if (_app != null)
            await _app.StopAsync(cancellationToken);
    }

    public Task RunAsync(CancellationToken cancellationToken = default)
    {
        // Godot controls the run loop
        return Task.CompletedTask;
    }

    public static GodotWingedBeanHostBuilder CreateBuilder()
        => new GodotWingedBeanHostBuilder();
}
#endif

public class GodotWingedBeanHostBuilder : IWingedBeanHostBuilder
{
    private Action<IServiceCollection>? _configureServices;
    // Note: _configureConfig is reserved for future Godot-specific configuration

    public IWingedBeanHostBuilder ConfigureServices(Action<IServiceCollection> configure)
    {
        _configureServices += configure;
        return this;
    }

    public IWingedBeanHostBuilder ConfigureAppConfiguration(Action<IConfigurationBuilder> configure)
    {
        // Godot config integration - reserved for future use
        return this;
    }

    public IWingedBeanHostBuilder ConfigureLogging(Action<ILoggingBuilder> configure)
    {
        // Godot logging integration
        return this;
    }

    public IWingedBeanHost Build()
    {
#if GODOT
        // Create Godot node with host
        var host = new GodotWingedBeanHost();

        // Pass configuration to host before _Ready runs
        // This requires the configuration to be stored in a way accessible to the node
        // For now, we'll use a field approach (not ideal but functional)
        host._configureServices = _configureServices;

        return host;
#else
        // Build service provider
        var services = new ServiceCollection();
        _configureServices?.Invoke(services);

        var serviceProvider = services.BuildServiceProvider();
        var app = serviceProvider.GetRequiredService<IWingedBeanApp>();

        return new GodotWingedBeanHost(serviceProvider, app);
#endif
    }
}

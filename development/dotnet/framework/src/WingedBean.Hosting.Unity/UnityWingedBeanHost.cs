using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using WingedBean.Contracts.Hosting;
using WingedBean.Contracts.UI;

namespace WingedBean.Hosting.Unity;

/// <summary>
/// Unity host that bridges Unity MonoBehaviour lifecycle to IWingedBeanApp.
/// Unity's lifecycle is authoritative (Awake, Start, Update, OnDestroy, etc.).
/// </summary>
#if UNITY
public class UnityWingedBeanHost : UnityEngine.MonoBehaviour, IWingedBeanHost
{
    private IServiceProvider? _services;
    private IWingedBeanApp? _app;
    private CancellationTokenSource? _cts;
    private Action<IServiceCollection>? _configureServices;

    public IServiceProvider Services => _services
        ?? throw new InvalidOperationException("Host not started");

    private void Awake()
    {
        // Build service provider (Unity doesn't use Generic Host)
        var services = new ServiceCollection();

        // Configure services (set by builder before GameObject creation)
        _configureServices?.Invoke(services);

        _services = services.BuildServiceProvider();
        _app = _services.GetRequiredService<IWingedBeanApp>();
    }

    private async void Start()
    {
        _cts = new CancellationTokenSource();
        await StartAsync(_cts.Token);
    }

    private void Update()
    {
#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
        // Call RenderAsync if app is IUIApp
        if (_app is IUIApp uiApp)
        {
            // Unity Update is synchronous, so we fire-and-forget
            // In production, consider queuing or using Unity's async patterns
            _ = uiApp.RenderAsync(_cts?.Token ?? default);
        }
#pragma warning restore CS4014
    }

    private async void OnDestroy()
    {
        await StopAsync(default);
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
        // Unity controls the run loop, so this is a no-op
        // The MonoBehaviour lifecycle keeps the app running
        return Task.CompletedTask;
    }

    // Builder state (set before GameObject is created)
    public static UnityWingedBeanHostBuilder CreateBuilder()
        => new UnityWingedBeanHostBuilder();
}
#else
// Stub implementation for non-Unity environments
public class UnityWingedBeanHost : IWingedBeanHost
{
    private IServiceProvider? _services;
    private IWingedBeanApp? _app;
    private CancellationTokenSource? _cts;

    public IServiceProvider Services => _services
        ?? throw new InvalidOperationException("Host not started");

    public UnityWingedBeanHost(IServiceProvider services, IWingedBeanApp app)
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
        // Unity controls the run loop, so this is a no-op
        // The MonoBehaviour lifecycle keeps the app running
        return Task.CompletedTask;
    }

    public static UnityWingedBeanHostBuilder CreateBuilder()
        => new UnityWingedBeanHostBuilder();
}
#endif

public class UnityWingedBeanHostBuilder : IWingedBeanHostBuilder
{
    private Action<IServiceCollection>? _configureServices;
    private Action<IConfigurationBuilder>? _configureConfig;

    public IWingedBeanHostBuilder ConfigureServices(Action<IServiceCollection> configure)
    {
        _configureServices += configure;
        return this;
    }

    public IWingedBeanHostBuilder ConfigureAppConfiguration(Action<IConfigurationBuilder> configure)
    {
        _configureConfig += configure;
        return this;
    }

    public IWingedBeanHostBuilder ConfigureLogging(Action<ILoggingBuilder> configure)
    {
        // Unity uses its own logging - could bridge to ILogger
        return this;
    }

    public IWingedBeanHost Build()
    {
#if UNITY
        // Create Unity GameObject with host component
        var hostObject = new UnityEngine.GameObject("WingedBeanHost");
        var host = hostObject.AddComponent<UnityWingedBeanHost>();

        // Pass configuration to host before Awake runs
        // This requires the configuration to be stored in a way accessible to the component
        // For now, we'll use a static approach (not ideal but functional)
        host._configureServices = _configureServices;

        return host;
#else
        // Build service provider (Unity doesn't use Generic Host)
        var services = new ServiceCollection();

        // Configure services (set by builder before GameObject creation)
        _configureServices?.Invoke(services);

        var serviceProvider = services.BuildServiceProvider();
        var app = serviceProvider.GetRequiredService<IWingedBeanApp>();

        return new UnityWingedBeanHost(serviceProvider, app);
#endif
    }
}

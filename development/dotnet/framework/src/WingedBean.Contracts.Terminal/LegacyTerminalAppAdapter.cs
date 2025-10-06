using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using WingedBean.Contracts.Terminal;

namespace WingedBean.Hosting;

/// <summary>
/// Adapter that wraps ITerminalApp and registers it as an IHostedService.
/// Per RFC-0029, ITerminalApp already extends IHostedService, so this adapter
/// primarily exists for explicit registration and resolving timing.
/// 
/// Key design: ITerminalApp is resolved in StartAsync() (not constructor) to ensure
/// plugins have been loaded by PluginLoaderHostedService which runs first.
/// This solves the "constructor resolution timing problem" - all IHostedService
/// constructors are called before any StartAsync() executes.
/// </summary>
public class LegacyTerminalAppAdapter : IHostedService
{
    private readonly IServiceProvider _serviceProvider;
    private ITerminalApp? _terminalApp;

    public LegacyTerminalAppAdapter(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        // Resolve ITerminalApp NOW (after PluginLoaderHostedService.StartAsync completed)
        // This timing is critical: plugins must be loaded before we can resolve ITerminalApp
        _terminalApp = _serviceProvider.GetRequiredService<ITerminalApp>();
        
        // ITerminalApp extends IHostedService per RFC-0029
        // Configuration is already injected via SetRegistry
        await _terminalApp.StartAsync(cancellationToken);
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        if (_terminalApp != null)
            await _terminalApp.StopAsync(cancellationToken);
    }
}

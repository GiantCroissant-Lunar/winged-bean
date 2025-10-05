using Microsoft.Extensions.Hosting;
using WingedBean.Contracts.Terminal;

namespace WingedBean.Hosting;

/// <summary>
/// Adapter that wraps ITerminalApp and registers it as an IHostedService.
/// Per RFC-0029, ITerminalApp already extends IHostedService, so this adapter
/// primarily exists for explicit registration and future compatibility.
/// Configuration is injected via SetRegistry before this adapter is invoked.
/// </summary>
public class LegacyTerminalAppAdapter : IHostedService
{
    private readonly ITerminalApp _terminalApp;

    public LegacyTerminalAppAdapter(ITerminalApp terminalApp)
    {
        _terminalApp = terminalApp;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        // ITerminalApp extends IHostedService per RFC-0029
        // Configuration is already injected via SetRegistry or constructor
        return _terminalApp.StartAsync(cancellationToken);
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return _terminalApp.StopAsync(cancellationToken);
    }
}

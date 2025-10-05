using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using WingedBean.Contracts.Terminal;

namespace WingedBean.Hosting;

/// <summary>
/// Adapter that wraps a legacy ITerminalApp (pre-IHostedService)
/// to work with .NET Generic Host.
/// </summary>
public class LegacyTerminalAppAdapter : IHostedService
{
    private readonly ITerminalApp _terminalApp;
    private readonly TerminalAppConfig _config;

    public LegacyTerminalAppAdapter(ITerminalApp terminalApp, IOptions<TerminalAppConfig> config)
    {
        _terminalApp = terminalApp;
        _config = config.Value;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        // Call legacy StartAsync(config, ct) signature
        var legacyStart = _terminalApp.GetType().GetMethod("StartAsync",
            new[] { typeof(TerminalAppConfig), typeof(CancellationToken) });

        if (legacyStart != null)
        {
            return (Task)legacyStart.Invoke(_terminalApp, new object[] { _config, cancellationToken })!;
        }

        // Fallback to new signature
        return _terminalApp.StartAsync(cancellationToken);
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return _terminalApp.StopAsync(cancellationToken);
    }
}

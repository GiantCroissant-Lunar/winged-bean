using Microsoft.Extensions.Logging;
using Plate.PluginManoi.Contracts;
using IService = Plate.CrossMilo.Contracts.WebSocket.Services.IService;

namespace WingedBean.Plugins.WebSocket;

/// <summary>
/// Registers the WebSocket service implementation with the runtime registry.
/// This enables the host (and headless keepalive) to resolve and start it.
/// </summary>
public class WebSocketPlugin : IPlugin
{
    private SuperSocketWebSocketService? _service;
    private ILogger<WebSocketPlugin>? _logger;

    public string Id => "wingedbean.plugins.websocket";
    public string Version => "1.0.0";

    public Task OnActivateAsync(IRegistry registry, CancellationToken ct = default)
    {
        // Try to get logger from registry
        _logger = registry.GetAll<ILogger<WebSocketPlugin>>().FirstOrDefault();

        _service = new SuperSocketWebSocketService(registry.GetAll<ILogger<SuperSocketWebSocketService>>().FirstOrDefault());
        registry.Register<IService>(_service, priority: 10);
        // Auto-start on default port for integration verification
        try
        {
            var port = 4040;
            var envPort = Environment.GetEnvironmentVariable("DUNGEON_WS_PORT");
            if (!string.IsNullOrWhiteSpace(envPort) && int.TryParse(envPort, out var p))
                port = p;
            _logger?.LogInformation("Autostarting WebSocket on port {Port}", port);
            _service.Start(port);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Autostart error");
        }
        return Task.CompletedTask;
    }

    public Task OnDeactivateAsync(CancellationToken ct = default)
    {
        _service = null;
        return Task.CompletedTask;
    }

    public IEnumerable<object> GetServices()
    {
        if (_service != null)
            yield return _service;
    }
}

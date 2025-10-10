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

    public string Id => "wingedbean.plugins.websocket";
    public string Version => "1.0.0";

    public Task OnActivateAsync(IRegistry registry, CancellationToken ct = default)
    {
        _service = new SuperSocketWebSocketService();
        registry.Register<IService>(_service, priority: 10);
        // Auto-start on default port for integration verification
        try
        {
            var port = 4040;
            var envPort = Environment.GetEnvironmentVariable("DUNGEON_WS_PORT");
            if (!string.IsNullOrWhiteSpace(envPort) && int.TryParse(envPort, out var p))
                port = p;
            Console.WriteLine($"[WebSocketPlugin] Autostarting WebSocket on port {port}");
            _service.Start(port);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[WebSocketPlugin] Autostart error: {ex.Message}");
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

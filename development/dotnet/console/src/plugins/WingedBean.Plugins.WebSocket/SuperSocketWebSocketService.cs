using SuperSocket.WebSocket.Server;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Configuration;
using WingedBean.Contracts.Core;
using WingedBean.Contracts.WebSocket;

namespace WingedBean.Plugins.WebSocket;

/// <summary>
/// SuperSocket-based WebSocket service implementation.
/// Provides WebSocket server functionality for console applications.
/// </summary>
[Plugin(
    Name = "WebSocket.SuperSocket",
    Provides = new[] { typeof(IWebSocketService) },
    Priority = 10
)]
public class SuperSocketWebSocketService : IWebSocketService
{
    private IHost? _host;
    private int _port;
    private readonly List<WebSocketSession> _sessions = new();
    private readonly object _lock = new();

    /// <inheritdoc />
    public event Action<string>? MessageReceived;

    /// <inheritdoc />
    public void Start(int port)
    {
        _port = port;

        // Create configuration with specified port
        var configBuilder = new ConfigurationBuilder();
        configBuilder.AddInMemoryCollection(new Dictionary<string, string?>
        {
            { "serverOptions:name", "WingedBeanWebSocketServer" },
            { "serverOptions:listeners:0:ip", "Any" },
            { "serverOptions:listeners:0:port", port.ToString() }
        });
        var configuration = configBuilder.Build();

        // Build the WebSocket host
        _host = WebSocketHostBuilder.Create(Array.Empty<string>())
            .ConfigureAppConfiguration((hostCtx, configApp) =>
            {
                configApp.AddConfiguration(configuration);
            })
            .UseWebSocketMessageHandler((session, message) =>
            {
                // Track connected session
                lock (_lock)
                {
                    if (!_sessions.Contains(session))
                    {
                        _sessions.Add(session);
                    }
                }

                // Raise the MessageReceived event
                MessageReceived?.Invoke(message.Message);

                return ValueTask.CompletedTask;
            })
            .Build();

        // Start the WebSocket server in background
        _ = Task.Run(() => _host.Run());

        Console.WriteLine($"WebSocket server started on port {port}");
    }

    /// <inheritdoc />
    public void Broadcast(string message)
    {
        if (_host == null)
        {
            throw new InvalidOperationException("WebSocket server has not been started. Call Start() first.");
        }

        lock (_lock)
        {
            // Send to all connected sessions
            foreach (var session in _sessions.ToList())
            {
                try
                {
                    // ValueTask.AsTask() to get a Task that we can wait on
                    session.SendAsync(message).AsTask().Wait();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error broadcasting to session: {ex.Message}");
                    // Remove disconnected session
                    _sessions.Remove(session);
                }
            }
        }
    }
}

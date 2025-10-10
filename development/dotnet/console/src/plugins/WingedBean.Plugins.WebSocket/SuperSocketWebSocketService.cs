using SuperSocket.WebSocket.Server;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Configuration;
using Plate.PluginManoi.Contracts;
using Plate.CrossMilo.Contracts.WebSocket.Services;
using Plate.CrossMilo.Contracts.WebSocket;
using IService = Plate.CrossMilo.Contracts.WebSocket.Services.IService;
using System.Net.Sockets;
using System.IO;

namespace WingedBean.Plugins.WebSocket;

/// <summary>
/// SuperSocket-based WebSocket service implementation.
/// Provides WebSocket server functionality for console applications.
/// </summary>
[Plugin(
    Name = "WebSocket.SuperSocket",
    Provides = new[] { typeof(IService) },
    Priority = 10
)]
public class SuperSocketWebSocketService : IService
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
        // Try to start on the requested port, with fallback to alternative ports
        var portsToTry = new List<int> { port, 4041, 4042, 4043, 4044 };
        Exception? lastException = null;
        
        foreach (var tryPort in portsToTry)
        {
            try
            {
                _port = tryPort;
                
                // Create configuration with specified port
                var configBuilder = new ConfigurationBuilder();
                configBuilder.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    { "serverOptions:name", "WingedBeanWebSocketServer" },
                    { "serverOptions:listeners:0:ip", "Any" },
                    { "serverOptions:listeners:0:port", tryPort.ToString() }
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

                // Small delay to let the server start and potentially fail
                Thread.Sleep(100);
                
                Console.WriteLine($"✓ WebSocket server started on port {_port}");
                
                // Write port info file for PTY service discovery
                WritePortInfoFile(_port);
                
                return; // Success!
            }
            catch (Exception ex)
            {
                lastException = ex;
                Console.WriteLine($"✗ Failed to start on port {tryPort}: {ex.Message}");
                _host = null;
            }
        }
        
        // All ports failed - log but don't throw (WebSocket is optional)
        Console.WriteLine($"⚠ WebSocket server could not start on any port. Tried: {string.Join(", ", portsToTry)}");
        Console.WriteLine($"  Last error: {lastException?.Message}");
        Console.WriteLine($"  Application will continue without WebSocket support.");
    }
    
    private void WritePortInfoFile(int actualPort)
    {
        try
        {
            // Write to both current directory and logs directory
            var portInfoPath = Path.Combine(Directory.GetCurrentDirectory(), "websocket-port.txt");
            var logsDir = Path.Combine(Directory.GetCurrentDirectory(), "logs");
            Directory.CreateDirectory(logsDir);
            var logsPortInfoPath = Path.Combine(logsDir, "websocket-port.txt");
            
            var portInfo = $"{actualPort}";
            File.WriteAllText(portInfoPath, portInfo);
            File.WriteAllText(logsPortInfoPath, portInfo);
            
            Console.WriteLine($"✓ Port info written to: {portInfoPath}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"⚠ Failed to write port info file: {ex.Message}");
        }
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

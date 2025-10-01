namespace WingedBean.Contracts.WebSocket;

/// <summary>
/// WebSocket service for real-time bidirectional communication.
/// Platform implementations: SuperSocket (Console), custom implementations (Unity/Web).
/// </summary>
public interface IWebSocketService
{
    /// <summary>
    /// Start the WebSocket server on the specified port.
    /// </summary>
    /// <param name="port">Port number to listen on</param>
    void Start(int port);

    /// <summary>
    /// Broadcast a message to all connected clients.
    /// </summary>
    /// <param name="message">Message to broadcast</param>
    void Broadcast(string message);

    /// <summary>
    /// Event raised when a message is received from any client.
    /// </summary>
    event Action<string> MessageReceived;
}

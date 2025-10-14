# WingedBean.Plugins.WebSocket

WebSocket service plugin (Tier 3) implementing `IWebSocketService` using SuperSocket.

## Overview

This plugin provides WebSocket server functionality for console applications using SuperSocket.WebSocket.Server. It implements the `IWebSocketService` interface defined in `WingedBean.Contracts.WebSocket`.

## Features

- Start WebSocket server on specified port
- Broadcast messages to all connected clients
- Receive messages from clients via event
- Automatic session management
- Thread-safe message handling

## Usage

### Basic Usage

```csharp
using WingedBean.Plugins.WebSocket;

// Create and start the service
var wsService = new SuperSocketWebSocketService();
wsService.Start(4040);

// Subscribe to incoming messages
wsService.MessageReceived += (message) =>
{
    Console.WriteLine($"Received: {message}");

    // Echo back to all clients
    wsService.Broadcast($"Echo: {message}");
};

// Broadcast to all connected clients
wsService.Broadcast("Hello from server!");
```

### Plugin Metadata

The service is marked with the `[Plugin]` attribute:

```csharp
[Plugin(
    Name = "WebSocket.SuperSocket",
    Provides = new[] { typeof(IWebSocketService) },
    Priority = 10
)]
```

## Interface Implementation

Implements `IWebSocketService`:

- `void Start(int port)` - Starts the WebSocket server
- `void Broadcast(string message)` - Broadcasts message to all clients
- `event Action<string> MessageReceived` - Event raised when message is received

## Dependencies

- SuperSocket.WebSocket.Server (v2.0.0-beta.15)
- Microsoft.Extensions.Hosting (v8.0.0)
- Microsoft.Extensions.Configuration (v8.0.0)
- WingedBean.Contracts.Core
- WingedBean.Contracts.WebSocket

## Testing

The plugin includes comprehensive unit tests:

```bash
cd tests/WingedBean.Plugins.WebSocket.Tests
dotnet test
```

All tests should pass:
- Interface implementation verification
- Server start functionality
- Event subscription
- Broadcast validation
- Plugin attribute verification

## Architecture

This is a Tier 3 service plugin in the WingedBean architecture:

- **Tier 1**: Contracts (`WingedBean.Contracts.WebSocket`)
- **Tier 2**: Registry (`WingedBean.Registry`)
- **Tier 3**: Service Plugins (this plugin)
- **Tier 4**: Providers

## See Also

- RFC-0004: Project Organization and Folder Structure
- `WingedBean.Contracts.WebSocket.IWebSocketService`
- `framework/src/WingedBean.Contracts.WebSocket/`

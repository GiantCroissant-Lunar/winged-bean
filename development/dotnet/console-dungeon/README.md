# Console Dungeon

A .NET console application demonstrating Terminal.Gui v2 with WebSocket support for web-based terminal access using xterm.js.

## Features

- **Terminal.Gui v2**: Modern console UI framework for .NET
- **SuperSocket WebSocket Server**: Real-time communication with web clients
- **xterm.js Integration**: Web-based terminal interface
- **Interactive Commands**: Built-in command processing
- **WebSocket Communication**: Bi-directional communication between console app and web browser

## Project Structure

```
ConsoleDungeon/
├── ConsoleDungeon.sln          # Visual Studio solution file
└── ConsoleDungeon/
    ├── ConsoleDungeon.csproj   # Project file
    └── Program.cs              # Main application
```

## Prerequisites

- .NET 8.0 SDK
- Node.js (for the web frontend)

## Building and Running

### 1. Build the .NET Application

```bash
cd projects/dotnet/console-dungeon
dotnet build
```

### 2. Run the Console Application

```bash
dotnet run --project ConsoleDungeon/ConsoleDungeon.csproj
```

The application will start a WebSocket server on `ws://localhost:4040` and display a Terminal.Gui interface.

### 3. Access from Web Browser

Open the Astro documentation site (from the nodejs project) and navigate to the "Live Terminal" section. The xterm.js terminal will connect to the WebSocket server.

## Available Commands

When connected via WebSocket, you can use these commands in the web terminal:

- `help` - Show available commands
- `echo <text>` - Echo text back
- `time` - Show current server time
- `status` - Show WebSocket connection status
- `clear` - Clear the terminal output
- `quit` - Exit the application

## Architecture

The application consists of:

1. **Console Application**: Uses Terminal.Gui v2 to create a rich console interface
2. **WebSocket Server**: SuperSocket handles WebSocket connections from web clients
3. **Web Frontend**: xterm.js provides a web-based terminal interface
4. **Real-time Communication**: Commands typed in the web terminal are sent to the console app and responses are displayed

## Dependencies

- Terminal.Gui v2.0.0-pre.1729
- SuperSocket 2.0.0-beta.15
- SuperSocket.WebSocket 2.0.0-beta.15
- Microsoft.Extensions.Hosting 8.0.0
- Microsoft.Extensions.Logging.Console 8.0.0

## Reference Projects

This project draws inspiration from:

- Terminal.Gui examples in `/ref-projects/Terminal.Gui/Examples/UICatalog/Scenarios`
- SuperSocket samples in `/ref-projects/SuperSocket/samples`

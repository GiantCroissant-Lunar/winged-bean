# Terminal.Gui v2 PTY Integration with xterm.js
## Project Handover Document

### Project Overview

This project demonstrates real Terminal.Gui v2 applications running in web browsers using PTY (pseudo-terminal), node-pty, WebSocket streaming, and xterm.js. The system provides end-to-end integration allowing native Terminal.Gui applications to run in browsers with full terminal emulation.

**Architecture**: Terminal.Gui → PTY → Node.js → WebSocket → xterm.js

### System Components

1. **Astro Web Frontend**: Website with xterm.js terminal emulator
2. **Node.js PTY Service**: WebSocket server with node-pty for terminal streaming
3. **.NET Terminal.Gui Application**: Real Terminal.Gui v2 application with interactive UI
4. **Real-time Communication**: Binary WebSocket streaming of PTY output to browser

### Architecture

```
┌─────────────────┐   WebSocket    ┌──────────────────┐   PTY    ┌──────────────────┐
│   Web Browser   │◄──────────────►│  Node.js Server  │◄────────►│  .NET Terminal   │
│                 │  (ws://4041)   │                  │          │  .Gui App        │
│ • xterm.js      │                │ • node-pty       │          │                  │
│ • Mouse/Kbd     │    Binary      │ • WebSocket      │  ANSI    │ • Terminal.Gui   │
│   Input         │    Streaming   │ • bash shell     │  Escape  │   v2.0.0         │
└─────────────────┘                └──────────────────┘  Seq.    └──────────────────┘
```

### Directory Structure

```
winged-bean/
├── docs/
│   ├── handover/
│   │   └── TERMINAL_GUI_PTY_INTEGRATION.md  # This document
│   └── chat-history/                        # Conversation exports (not tracked)
├── projects/
│   ├── nodejs/
│   │   ├── pty-service/                     # PTY WebSocket service
│   │   │   ├── server.js                    # Main PTY server
│   │   │   ├── test-pty.js                  # PTY functionality tests
│   │   │   ├── test-client.js               # WebSocket client test
│   │   │   └── package.json
│   │   └── sites/docs/                      # Astro web application
│   │       ├── src/
│   │       │   ├── components/
│   │       │   │   └── XTerm.astro          # xterm.js component
│   │       │   └── pages/
│   │       │       └── index.astro
│   │       └── package.json
│   └── dotnet/
│       └── console-dungeon/                 # Terminal.Gui application
│           ├── ConsoleDungeon/
│           │   ├── TerminalGuiApp.cs        # Main Terminal.Gui app
│           │   ├── Program.cs               # Legacy WebSocket server
│           │   └── ConsoleDungeon.csproj
│           └── ConsoleDungeon.sln
└── ref-projects/                            # Reference implementations
    ├── SuperSocket/
    └── Terminal.Gui/
```

### Node.js PTY Service

#### Purpose
Bridges Terminal.Gui applications to web browsers by:
- Spawning .NET applications in PTY environment
- Streaming binary terminal output via WebSocket
- Forwarding keyboard and mouse input from browser to PTY
- Handling terminal resize events

#### Key Features
- **PTY Process Management**: Spawns .NET app via bash with proper TERM environment
- **Binary Streaming**: Raw PTY output streamed as ArrayBuffer to xterm.js
- **Mouse Support**: SGR mouse event protocol for Terminal.Gui interaction
- **Terminal Environment**: xterm-256color with true color support
- **Auto-cleanup**: Kills PTY process on WebSocket disconnect

#### Server Configuration
```javascript
// WebSocket server on port 4041
const ptyProcess = pty.spawn('/bin/bash', ['-lc', cmd], {
    name: 'xterm-256color',
    cols: 80,
    rows: 24,
    cwd: path.dirname(DOTNET_PROJECT_PATH),
    env: {
        TERM: 'xterm-256color',
        COLORTERM: 'truecolor',
        LANG: 'en_US.UTF-8'
    }
});
```

#### Dependencies
```json
{
  "node-pty": "^1.0.0",
  "ws": "^8.18.0"
}
```

### Web Frontend (Astro + xterm.js)

#### XTerm.astro Component

**Features:**
- xterm.js v5.3.0 terminal emulator
- Binary WebSocket connection to PTY service
- Automatic keyboard and mouse event forwarding
- Terminal resize synchronization
- Auto-reconnection on disconnect
- Dark Dracula-inspired theme

**Terminal Configuration:**
```javascript
const terminal = new Terminal({
  cols: 80,
  rows: 24,
  fontSize: 14,
  fontFamily: 'Consolas, "Liberation Mono", Menlo, monospace',
  theme: { /* Dracula colors */ }
});
```

**WebSocket Handling:**
- Binary mode: `ws.binaryType = 'arraybuffer'`
- Input: `terminal.onData(data => ws.send(data))`
- Output: `terminal.write(new Uint8Array(event.data))`
- Resize: JSON message with `{type: 'resize', cols, rows}`

### .NET Terminal.Gui Application

#### Technologies
- **.NET 9.0**: Target framework
- **Terminal.Gui v2.0.0**: Console UI framework
- **System.Timers**: Live updates

#### TerminalGuiApp.cs Implementation

**Features:**
- Window with menu bar (File, Help)
- Interactive button ("Click Me!")
- Text field for input
- Status label showing button clicks
- Live timestamp updates every second
- Proper Terminal.Gui v2 API usage

**Key Components:**
```csharp
// Application initialization
Application.Init();

// Window creation
Window appWindow = new() {
    Title = "🎉 Terminal.Gui v2 PTY Demo (Ctrl+Q to quit)",
    BorderStyle = LineStyle.Single
};

// Interactive elements
var button = new Button {
    X = 1, Y = 9,
    Text = "_Click Me!",
    IsDefault = true
};

button.Accepting += (s, e) => {
    statusLabel.Text = $"Button clicked at {DateTime.Now:HH:mm:ss}!";
};

// Run application
Application.Run(appWindow);
```

**Dependencies:**
```xml
<PackageReference Include="Terminal.Gui" Version="2.0.0" />
```

### Setup Instructions

#### Prerequisites
- Node.js v18+ (for PTY service and Astro)
- .NET 9.0 SDK (for Terminal.Gui app)
- pnpm (for Node.js workspace management)
- Modern web browser with WebSocket support

#### 1. Install Dependencies

**Node.js workspace:**
```bash
cd projects/nodejs
pnpm install
```

**PTY service:**
```bash
cd projects/nodejs/pty-service
npm install
# Rebuild node-pty for current Node.js version
npm rebuild
```

**.NET application:**
```bash
cd projects/dotnet/console-dungeon
dotnet restore
dotnet build
```

#### 2. Start Services

**Terminal 1 - PTY Service:**
```bash
cd projects/nodejs/pty-service
node server.js
# Listens on ws://localhost:4041
```

**Terminal 2 - Astro Website:**
```bash
cd projects/nodejs/sites/docs
npm run dev
# Serves on http://localhost:4321
```

#### 3. Access Application
1. Open http://localhost:4321 in browser
2. Terminal.Gui application automatically starts in PTY
3. Interact with UI using mouse and keyboard

### Current Status

#### ✅ Working Features
- Real Terminal.Gui v2 application running in PTY environment
- Binary WebSocket streaming of PTY output to xterm.js
- Terminal.Gui interface displays correctly in browser
- Keyboard input works (Tab navigation, text entry)
- Live timestamp updates every second
- Terminal resize synchronization
- Automatic reconnection on disconnect
- PTY process cleanup on browser close

#### ⚠️ Known Issues
- **Mouse clicks not working**: Mouse events are being sent to PTY (confirmed in logs) but Terminal.Gui doesn't respond to clicks
  - Mouse move events are transmitted correctly (SGR protocol: `\u001b[<35;x;yM`)
  - Button clicks sent but no visual feedback or event handling
  - Tab keyboard navigation works correctly as workaround
  - Likely requires Terminal.Gui mouse mode configuration

#### 🔄 Next Steps
1. Fix mouse click event handling in Terminal.Gui
2. Test with more complex Terminal.Gui examples (from UICatalog)
3. Add proper error handling and logging
4. Implement session management for multiple users
5. Add security (authentication, WSS)

### Technical Details

#### PTY Environment Variables
```bash
TERM=xterm-256color          # Terminal type
COLORTERM=truecolor          # True color support
LANG=en_US.UTF-8            # Locale settings
```

#### WebSocket Protocol
- **Endpoint**: `ws://localhost:4041`
- **Binary Mode**: ArrayBuffer for terminal output
- **Text Mode**: JSON for control messages
- **Input**: Raw keyboard/mouse data from xterm.js
- **Output**: Raw ANSI escape sequences from PTY
- **Control**: `{type: 'resize', cols: 80, rows: 24}`

#### xterm.js Configuration
- Terminal size: 80x24
- Font: 14px Consolas/Liberation Mono/Menlo
- Binary mode enabled for PTY output
- Automatic input forwarding via `onData` handler

#### Mouse Event Format
- SGR (Select Graphic Rendition) protocol
- Format: `\u001b[<button;x;yM` (press) or `m` (release)
- Example: `\u001b[<35;12;10M` = button 35 at column 12, row 10
- Button codes: 0=left, 1=middle, 2=right, 35=move

### Development Notes

#### Build Commands
```bash
# PTY Service
cd projects/nodejs/pty-service
node server.js                    # Start PTY WebSocket server

# Web Frontend
cd projects/nodejs/sites/docs
npm run dev                       # Development server
npm run build                     # Production build

# .NET Application
cd projects/dotnet/console-dungeon
dotnet build                      # Build project
dotnet run --project ConsoleDungeon/ConsoleDungeon.csproj  # Run directly
```

#### Testing PTY Functionality
```bash
cd projects/nodejs/pty-service
node test-pty.js                  # Test node-pty spawning
node test-client.js               # Test WebSocket client
```

#### Debugging

**PTY Service Logs:**
- Check console output for WebSocket connections
- Mouse/keyboard input logged as: `Sending input to PTY: "..."`
- PTY process PID and exit codes logged

**Browser Console:**
- Check WebSocket connection status
- Monitor binary data transfer
- Inspect mouse/keyboard events

**Terminal.Gui Issues:**
- Verify TERM environment variable
- Check if Terminal.Gui initializes properly
- Test keyboard-only interaction as fallback

### Future Enhancements

1. **Fix Mouse Support**
   - Debug Terminal.Gui mouse event handling
   - Verify mouse mode initialization
   - Test with Terminal.Gui example applications

2. **Enhanced Features**
   - Multiple terminal sessions
   - Session persistence and reconnection
   - Recording and playback of sessions
   - File upload/download support

3. **Production Readiness**
   - WebSocket Secure (WSS) support
   - Authentication and authorization
   - Rate limiting and DDoS protection
   - Horizontal scaling with load balancer
   - Docker containerization

4. **Advanced Terminal.Gui Integration**
   - Test with UICatalog scenarios
   - Support for complex layouts
   - Dialog and menu interactions
   - Form input validation

### Troubleshooting

#### PTY Process Exits Immediately
- **Cause**: Incorrect command or missing bash shell
- **Solution**: Verify DOTNET_PROJECT_PATH is correct
- **Test**: Run `dotnet run --project <path>` manually

#### Mouse Events Not Working
- **Current Status**: Events sent but not processed
- **Workaround**: Use Tab key for navigation
- **Investigation**: Check Terminal.Gui mouse mode settings

#### WebSocket Connection Failed
- **Check**: PTY service is running on port 4041
- **Check**: No firewall blocking WebSocket connections
- **Check**: Browser console for error messages

#### node-pty Module Error
- **Cause**: Native module compiled for different Node.js version
- **Solution**: Run `npm rebuild` in pty-service directory
- **Note**: Required after Node.js version changes

### Reference Materials

- **Terminal.Gui**: https://github.com/gui-cs/Terminal.Gui
- **node-pty**: https://github.com/microsoft/node-pty
- **xterm.js**: https://github.com/xtermjs/xterm.js
- **Astro**: https://astro.build/
- **WebSocket Protocol**: https://datatracker.ietf.org/doc/html/rfc6455

### Git Repository Status

**Clean State**: All changes committed with conventional commit messages

**Recent Commits:**
- `chore(nodejs): ignore package-lock.json in pnpm workspace`
- `chore(docs): update .gitignore to exclude chat history`
- `chore: clean up .gitignore and remove chat history from tracking`
- `feat(astro): add website with xterm.js Terminal.Gui integration`
- `feat(nodejs): add PTY WebSocket service for Terminal.Gui streaming`
- `feat(dotnet): add Terminal.Gui v2 application with PTY support`

---

**Handover Date**: September 30, 2025
**Status**: PTY integration working - Terminal.Gui displays in browser, keyboard works, mouse events pending
**Last Updated**: September 30, 2025 - Repository cleaned and committed
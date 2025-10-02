---
title: ADR 0005: Use PTY (Pseudo-Terminal) for Terminal.Gui Web Integration
---

# ADR 0005: Use PTY (Pseudo-Terminal) for Terminal.Gui Web Integration

## Status

Accepted

## Date

2025-09-30

## Context

We needed a way to run Terminal.Gui v2 applications in web browsers using xterm.js. The goal was to provide a seamless experience where native .NET Terminal.Gui applications could be accessed through a web interface without modification.

### Initial Approach (Rejected)

Initially, we attempted a simulated approach:

- .NET WebSocket server (SuperSocket) directly connected to xterm.js
- Manual ANSI escape sequence generation to simulate Terminal.Gui interface
- Custom screen content rendering with cursor positioning

**Problems with this approach:**

1. **Not Real Terminal.Gui**: Generated simulated interface, not actual Terminal.Gui v2
2. **High Maintenance**: Required manual ANSI sequence crafting for every UI element
3. **Limited Functionality**: Difficult to support complex Terminal.Gui features
4. **No True Terminal Environment**: Terminal.Gui expected TTY but got programmatic API
5. **Cursor Positioning Loops**: Terminal.Gui's own ANSI sequences conflicted with manual output

### Alternative Approaches Considered

1. **Direct Terminal.Gui API Capture**
   - Pros: Could theoretically capture Terminal.Gui's output
   - Cons: Terminal.Gui needs TTY, difficult to intercept ANSI sequences cleanly
   - Cons: Would require patching Terminal.Gui or complex output redirection

2. **VNC/Remote Desktop**
   - Pros: Battle-tested solution
   - Cons: Heavy-weight, requires full desktop environment
   - Cons: Poor performance, not suitable for lightweight terminal apps

3. **Terminal Recording/Playback (Asciinema)**
   - Pros: Works well for static recordings
   - Cons: No real-time interaction, can't handle live input/output

## Decision

We will use **PTY (Pseudo-Terminal) with node-pty** to bridge Terminal.Gui applications to web browsers.

### Architecture

```
Terminal.Gui App → PTY → node-pty → WebSocket → xterm.js (Browser)
                   ↑                    ↓
                   └────── Binary ──────┘
                        Streaming
```

### Key Components

1. **node-pty**: Node.js library providing PTY functionality
   - Spawns .NET Terminal.Gui app in proper terminal environment
   - Provides TTY that Terminal.Gui expects
   - Captures raw ANSI escape sequences

2. **WebSocket Server (Node.js)**:
   - Streams binary PTY output to browser
   - Forwards keyboard/mouse input from browser to PTY
   - Handles terminal resize events

3. **xterm.js**:
   - Terminal emulator in browser
   - Renders ANSI escape sequences from PTY
   - Sends input events to WebSocket server

### Implementation Details

**PTY Spawning:**

```javascript
const ptyProcess = pty.spawn('/bin/bash', ['-lc', cmd], {
    name: 'xterm-256color',
    cols: 80,
    rows: 24,
    env: {
        TERM: 'xterm-256color',
        COLORTERM: 'truecolor'
    }
});
```

**Binary Streaming:**

- PTY output → WebSocket as ArrayBuffer
- xterm.js writes binary data directly
- No encoding/decoding overhead

**Input Forwarding:**

- Keyboard: xterm.js `onData` → WebSocket → PTY
- Mouse: SGR mouse protocol events → PTY
- Resize: JSON control message → PTY resize

## Consequences

### Positive

1. **Real Terminal.Gui**: Actual Terminal.Gui v2 application runs unchanged
2. **No Code Modification**: Terminal.Gui app requires no web-specific changes
3. **Full Feature Support**: All Terminal.Gui features work (windows, menus, dialogs, forms)
4. **Battle-Tested**: PTY is proven technology used by VS Code, Hyper, etc.
5. **Proper Terminal Environment**: Terminal.Gui gets real TTY with ANSI support
6. **Low Latency**: Binary streaming with minimal overhead
7. **Standard Protocols**: Uses standard ANSI escape sequences, no custom protocol

### Negative

1. **Server-Side Execution**: Requires Node.js server, can't run client-side only
2. **One Process Per Session**: Each browser connection spawns new .NET process
3. **Platform Dependency**: node-pty requires native compilation (platform-specific)
4. **Resource Usage**: More memory per session than simulated approach
5. **Rebuild Requirement**: node-pty must be rebuilt when Node.js version changes

### Neutral

1. **Node.js Dependency**: Adds Node.js to stack (alongside .NET)
2. **Binary Protocol**: WebSocket uses binary mode, not text-based
3. **Mouse Support**: Requires Terminal.Gui to enable mouse mode (in progress)

## Implementation Status

### Working ✅

- PTY process spawning with proper terminal environment
- Binary WebSocket streaming from PTY to browser
- xterm.js rendering Terminal.Gui interface
- Keyboard input (Tab navigation, text entry)
- Terminal resize synchronization
- Live updates (timestamp, dynamic content)
- Process cleanup on disconnect

### In Progress 🔄

- Mouse click event handling in Terminal.Gui
- Testing with complex Terminal.Gui examples (UICatalog scenarios)

### Future Enhancements 📋

- Session management for multiple users
- Authentication and authorization
- WebSocket Secure (WSS) for production
- Horizontal scaling with session affinity
- Recording and playback capabilities

## Alternatives Not Chosen

### 1. WebAssembly Terminal.Gui

**Reason**: Terminal.Gui is .NET-based, would require:

- Full .NET runtime in WASM (large bundle size)
- TTY/terminal emulation in browser
- Significant engineering effort with uncertain compatibility

### 2. Terminal.Gui Native Web Backend

**Reason**: Would require:

- Major changes to Terminal.Gui core
- New rendering backend for web
- Fork or upstream contribution complexity
- Maintenance burden for every Terminal.Gui update

### 3. Screen Scraping/OCR

**Reason**:

- Extremely fragile and unreliable
- Can't capture colors, formatting, or interactive elements
- High latency
- No real-time input handling

## Related Decisions

- [ADR-0001: Use Astro with Asciinema Player](0001-use-astro-with-asciinema-player.md) - Established xterm.js and terminal recording approach
- Future: ADR for session management strategy
- Future: ADR for production deployment architecture

## References

- [node-pty GitHub](https://github.com/microsoft/node-pty)
- [Terminal.Gui Documentation](https://gui-cs.github.io/Terminal.Gui/)
- [xterm.js Documentation](https://xtermjs.org/)
- [PTY Specification (POSIX)](https://pubs.opengroup.org/onlinepubs/9699919799/basedefs/V1_chap11.html)
- Handover Document: `/docs/handover/TERMINAL_GUI_PTY_INTEGRATION.md`

## Notes

This decision enables real Terminal.Gui applications to run in browsers with minimal changes. The PTY approach is proven by major terminal emulators (VS Code, Hyper, Terminus) and provides the cleanest separation between terminal application and web interface.

The main trade-off is server-side execution vs. client-side, but this is acceptable given:

1. Terminal applications inherently expect server/system resources
2. Similar architecture to SSH web terminals (wetty, ttyd)
3. Better security (app logic stays on server)
4. Easier to scale horizontally with session management

---

**Author**: Claude (AI Assistant)
**Reviewed By**: [Pending]
**Supersedes**: Initial simulated Terminal.Gui approach (undocumented)

# Console MVP Migration Plan

## Overview

This document outlines the **Minimum Viable Product (MVP)** for migrating the existing `console-dungeon` application to the new `console/` structure while **preserving exact xterm.js functionality**.

> **Note (2025-10-03):** The legacy `development/dotnet/console-dungeon` project has now been removed. All active work should target the `development/dotnet/console/` hosts and plugins described below.

## Critical Success Criteria

The MVP MUST maintain the current working demo:
- ✅ Terminal.Gui v2 interface renders in browser via xterm.js
- ✅ WebSocket communication on port 4040
- ✅ Real-time updates between C# console and web browser
- ✅ All existing commands work (help, echo, time, status, quit)

**Failure condition**: If xterm.js stops working after migration, the MVP has failed.

## Current Working Setup

### Architecture
```
┌─────────────────────────────────────────────────────────────┐
│                    Browser (Astro + xterm.js)               │
│                                                             │
│  xterm.js Terminal ←──────── WebSocket (ws://localhost:4040) │
└─────────────────────────────────────────────────────────────┘
                              ↑
                              │ WebSocket Protocol
                              │ Messages: "screen:<content>"
                              ↓
┌─────────────────────────────────────────────────────────────┐
│         ConsoleDungeon (.NET Console App)                   │
│                                                             │
│  ┌──────────────────┐         ┌─────────────────────────┐  │
│  │ TerminalGuiApp   │         │ SuperSocket WebSocket   │  │
│  │ (Terminal.Gui v2)│◄────────┤ Server (Port 4040)      │  │
│  │                  │         │                         │  │
│  │ - UI Rendering   │         │ - Session Management    │  │
│  │ - Screen Content │         │ - Message Handling      │  │
│  └──────────────────┘         └─────────────────────────┘  │
└─────────────────────────────────────────────────────────────┘
```

### Key Files (Current)
```
projects/dotnet/console-dungeon/
├── ConsoleDungeon/
│   ├── Program.cs              # WebSocket server logic
│   ├── TerminalGuiApp.cs       # Terminal.Gui v2 interface
│   └── ConsoleDungeon.csproj   # Dependencies: Terminal.Gui, SuperSocket
├── ConsoleDungeon.sln
└── README.md
```

### Dependencies
- **Terminal.Gui** v2.0.0 - TUI framework
- **SuperSocket.WebSocket.Server** v2.0.0-beta.15 - WebSocket server
- **Microsoft.Extensions.Hosting** v8.0.0 - Hosting infrastructure
- **Microsoft.Extensions.Configuration.Json** v8.0.0 - Configuration

### Current Behavior
1. User runs: `dotnet run --project ConsoleDungeon/ConsoleDungeon.csproj`
2. App starts WebSocket server on port 4040
3. App initializes Terminal.Gui v2 interface (currently simplified for demo)
4. Browser connects to `ws://localhost:4040`
5. Browser sends `init` message
6. App responds with `screen:<content>` containing formatted Terminal.Gui output
7. xterm.js renders the TUI interface in browser
8. User types in browser → WebSocket → App → Response → xterm.js

## MVP Target Structure

### New Folder Layout
```
projects/dotnet/console/
├── src/
│   ├── ConsoleDungeon/                    # Existing app (unchanged)
│   │   ├── Program.cs                     # Same WebSocket + Terminal.Gui code
│   │   ├── TerminalGuiApp.cs              # Same TUI code
│   │   ├── appsettings.json               # Same config
│   │   └── ConsoleDungeon.csproj          # Same dependencies
│   │
│   └── ConsoleDungeon.Host/               # NEW: Minimal wrapper
│       ├── Program.cs                     # Simple bootstrap
│       └── ConsoleDungeon.Host.csproj     # References ConsoleDungeon
│
├── tests/
│   └── WingedBean.Plugins.DungeonGame.Tests/ # Plugin-level gameplay tests
│
├── Console.sln                            # NEW: Solution file
└── README.md                              # NEW: Documentation
```

### ConsoleDungeon.Host (MVP Bootstrap)

**Purpose**: Thin wrapper that launches the existing ConsoleDungeon app.

**Why?**: Establishes entry point structure for future plugin system without changing app logic.

```csharp
// projects/dotnet/console/src/ConsoleDungeon.Host/Program.cs
namespace ConsoleDungeon.Host;

/// <summary>
/// Console host entry point.
/// MVP: Simply launches the ConsoleDungeon TUI app.
/// Future: Will initialize Registry, PluginLoader, and load services as plugins.
/// </summary>
public class Program
{
    public static async Task Main(string[] args)
    {
        Console.WriteLine("ConsoleDungeon.Host starting (MVP mode)...");

        // MVP: Direct launch - no plugin system yet
        // TODO Phase 3: Add Registry + PluginLoader bootstrap
        await ConsoleDungeon.Program.Main(args);
    }
}
```

```xml
<!-- projects/dotnet/console/src/ConsoleDungeon.Host/ConsoleDungeon.Host.csproj -->
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net8.0</TargetFramework>
    <RootNamespace>ConsoleDungeon.Host</RootNamespace>
    <Nullable>enable</Nullable>
  </PropertyGroup>

  <ItemGroup>
    <!-- Reference the existing ConsoleDungeon app -->
    <ProjectReference Include="../ConsoleDungeon/ConsoleDungeon.csproj" />
  </ItemGroup>
</Project>
```

## Migration Steps

### Step 1: Create Directory Structure
```bash
cd /Users/apprenticegc/Work/lunar-horse/personal-work/winged-bean/projects/dotnet

# Create new console/ structure
mkdir -p console/src
mkdir -p console/tests

echo "Console project structure created"
```

### Step 2: Copy Existing ConsoleDungeon
```bash
# Copy the entire ConsoleDungeon project unchanged
cp -r console-dungeon/ConsoleDungeon console/src/ConsoleDungeon

echo "ConsoleDungeon copied to console/src/"
```

### Step 3: Create ConsoleDungeon.Host
```bash
cd console/src

# Create new project
dotnet new console -n ConsoleDungeon.Host
cd ConsoleDungeon.Host

# Replace Program.cs with MVP bootstrap code (see above)
# Update .csproj to reference ConsoleDungeon (see above)

cd ../../..
```

### Step 4: Create Solution
```bash
cd console

# Create solution
dotnet new sln -n Console

# Add projects
dotnet sln add src/ConsoleDungeon/ConsoleDungeon.csproj
dotnet sln add src/ConsoleDungeon.Host/ConsoleDungeon.Host.csproj

echo "Console.sln created"
```

### Step 5: Build and Test
```bash
# Build
dotnet build Console.sln

# Expected: Successful build, all projects compile

# Run
dotnet run --project src/ConsoleDungeon.Host/ConsoleDungeon.Host.csproj

# Expected output:
# "ConsoleDungeon.Host starting (MVP mode)..."
# "Console Dungeon - Starting..."
# "WebSocket server configured. Starting in background..."
# "Running in WebSocket-only mode for demonstration."
# "Press Ctrl+C to exit."
```

### Step 6: Verify xterm.js Integration
```bash
# 1. Keep the console app running (from Step 5)

# 2. In another terminal, start the Astro frontend
cd projects/nodejs/documentation
npm run dev

# 3. Open browser to http://localhost:4321
# 4. Navigate to "Live Terminal" or wherever xterm.js is integrated
# 5. Verify:
#    - xterm.js connects to ws://localhost:4040
#    - Terminal.Gui interface appears in browser
#    - Commands work (help, echo, time, status)
#    - Real-time updates work

# If all above work: MVP SUCCESS ✅
# If any fail: DO NOT PROCEED, fix the issue first ❌
```

### Step 7: Create README
```bash
cd console

cat > README.md << 'EOF'
# Console Project

Console implementation of WingedBean platform with Terminal.Gui v2 and xterm.js integration.

## Structure

- `src/ConsoleDungeon/` - Main Terminal.Gui v2 TUI application
- `src/ConsoleDungeon.Host/` - Bootstrap entry point
- `tests/` - Test projects

## Running

```bash
# Build
dotnet build Console.sln

# Run
dotnet run --project src/ConsoleDungeon.Host/ConsoleDungeon.Host.csproj
```

The WebSocket server will start on port 4040. Connect from the Astro frontend's xterm.js terminal.

## Development Status

**Phase**: MVP Complete ✅
- [x] Folder structure migrated
- [x] ConsoleDungeon app preserved unchanged
- [x] ConsoleDungeon.Host wrapper created
- [x] xterm.js integration verified working

**Next Phase**: Plugin Architecture
- [ ] Add Registry + PluginLoader
- [ ] Extract WebSocket as plugin
- [ ] Extract Terminal.Gui as plugin
- [ ] Refactor ConsoleDungeon to use services

See RFC-0004 for full roadmap.
EOF

echo "README.md created"
```

## Verification Checklist

Before marking MVP as complete, verify ALL of these:

### Build & Run
- [ ] `dotnet build Console.sln` succeeds without errors
- [ ] `dotnet run --project src/ConsoleDungeon.Host/...` launches successfully
- [ ] No runtime errors or exceptions
- [ ] Application stays running (doesn't crash)

### WebSocket Server
- [ ] WebSocket server starts on port 4040
- [ ] Server logs indicate successful initialization
- [ ] Port is listening (can verify with `netstat` or similar)

### xterm.js Integration
- [ ] Browser can connect to `ws://localhost:4040`
- [ ] Connection succeeds (check browser console for errors)
- [ ] Initial `screen:<content>` message received
- [ ] Terminal.Gui interface renders in xterm.js
- [ ] Interface is readable and properly formatted

### Interactive Commands
- [ ] `help` command shows available commands
- [ ] `echo <text>` echoes text back
- [ ] `time` shows current time
- [ ] `status` shows WebSocket connection status
- [ ] All commands produce expected output in xterm.js

### Regression Testing
- [ ] Behavior identical to original `console-dungeon` app
- [ ] No new bugs introduced
- [ ] Performance is comparable (no noticeable slowdown)

### Documentation
- [ ] README.md exists and is accurate
- [ ] Instructions for running the app are correct
- [ ] Structure is documented

### Code Quality
- [ ] No code changes to ConsoleDungeon (except namespace if needed)
- [ ] ConsoleDungeon.Host is minimal (< 20 lines of code)
- [ ] Comments explain future plugin integration

## Common Issues & Solutions

### Issue: Port 4040 Already in Use
**Symptom**: "Address already in use" error
**Solution**:
1. Kill existing process: `lsof -ti:4040 | xargs kill`
2. Or change port in appsettings.json

### Issue: xterm.js Can't Connect
**Symptom**: WebSocket connection fails in browser
**Solution**:
1. Verify WebSocket server is running: Check console output
2. Check browser console for connection errors
3. Verify URL is `ws://localhost:4040` (not `wss://`)
4. Check firewall/security settings

### Issue: Terminal.Gui Not Rendering
**Symptom**: xterm.js connects but shows blank or garbled output
**Solution**:
1. Verify `screen:<content>` messages contain ANSI escape codes
2. Check Program.cs `GetScreenContent()` method
3. Verify xterm.js is configured to handle ANSI codes

### Issue: Build Fails
**Symptom**: Compilation errors
**Solution**:
1. Verify all dependencies are restored: `dotnet restore`
2. Check .csproj files for correct references
3. Ensure Terminal.Gui and SuperSocket packages are available

## Success Criteria Summary

**MVP is complete when**:
1. ✅ New `console/` structure exists
2. ✅ ConsoleDungeon code is migrated unchanged
3. ✅ ConsoleDungeon.Host wrapper works
4. ✅ Console.sln builds successfully
5. ✅ xterm.js integration works exactly as before
6. ✅ All commands functional
7. ✅ No regressions from original behavior

**Failure conditions** (DO NOT proceed to Phase 3):
- ❌ xterm.js can't connect
- ❌ Terminal.Gui doesn't render
- ❌ Commands don't work
- ❌ Build errors
- ❌ Runtime crashes

## Next Steps (Phase 3)

After MVP is verified working:
1. Define Tier 1 contracts for console services (IWebSocketService, ITerminalUIService)
2. Implement Tier 2 Registry (ActualRegistry)
3. Implement Tier 3 PluginLoader + Tier 4 AssemblyContext provider
4. Extract SuperSocket into WebSocket service plugin
5. Extract Terminal.Gui into TerminalUI service plugin
6. Refactor ConsoleDungeon to request services from Registry
7. Test that xterm.js still works (regression test)

See RFC-0004 Phase 3 for detailed plan.

---

**Status**: Ready for Implementation
**Author**: Ray Wang (with Claude AI assistance)
**Date**: 2025-09-30

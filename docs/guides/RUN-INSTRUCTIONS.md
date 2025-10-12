# How to Run ConsoleDungeon with Terminal.Gui UI

## Quick Start

**In iTerm2, run**:
```bash
cd /Users/apprenticegc/Work/lunar-horse/personal-work/yokan-projects/winged-bean
./run-ui.sh
```

This script sets `FORCE_UI=1` which forces the Terminal.Gui UI to appear even if console redirection is detected.

## What Was Fixed

### Problem
ConsoleDungeon.Host was running in headless mode (WebSocket only) instead of showing the Terminal.Gui UI, even when run directly in iTerm2.

### Root Cause
Two headless detection points:
1. `Program.cs` - Initial detection (was working correctly)
2. `TerminalGuiSceneProvider.cs` - Secondary detection **← Was triggering headless mode**

The second check was seeing `Console.IsOutputRedirected == true` or `Console.IsInputRedirected == true` and forcing headless mode.

### Solution
Added `FORCE_UI` environment variable support in `TerminalGuiSceneProvider.cs`:
- When `FORCE_UI=1` is set, it overrides the redirection detection
- The UI will start even if console redirection is detected
- Useful for debugging and when you know you're in a real terminal

## Running Options

### Option 1: Use the Script (Recommended)
```bash
./run-ui.sh
```
- Automatically sets `FORCE_UI=1`
- Runs from correct directory
- Shows Terminal.Gui UI

### Option 2: Manual with Environment Variable
```bash
export FORCE_UI=1
./build/_artifacts/latest/dotnet/bin/ConsoleDungeon.Host
```

### Option 3: Direct Run (May Not Show UI)
```bash
./build/_artifacts/latest/dotnet/bin/ConsoleDungeon.Host
```
- Might run in headless mode if console redirection detected
- Use this to test normal detection behavior

### Option 4: Through Task System
```bash
cd build
task console:normal
```
- Check if Taskfile has proper environment setup

## Controls (When UI Appears)

- **Arrow Keys**: Move character
- **F2**: Show version
- **F3**: Show loaded plugins
- **F4**: Show audio info
- **Esc or File→Quit**: Exit

## Troubleshooting

### UI Still Not Showing?

**Run diagnostic**:
```bash
./check-console.sh
```

This shows:
- What TERM is set to
- Whether console is redirected
- Whether headless mode would be triggered

### Check Logs

Look for these messages:
```
FORCE_UI=1 detected: UI mode forced
```
or
```
HEADLESS mode: Skipping Terminal.Gui initialization
```

### Verify Build

```bash
# Check artifact timestamp
ls -lh build/_artifacts/latest/dotnet/bin/ConsoleDungeon.Host

# Should show recent time (just built)
```

### PM2 Mode (Headless)

To run in headless WebSocket mode (for web access):
```bash
cd build
task dev:start
pm2 logs console-dungeon
```

Access via: http://localhost:4321/demo/

## Audio Status

Audio plugin is still not building (separate issue). The game works without it, you just won't hear sounds.

## Files Modified

1. `TerminalGuiSceneProvider.cs` - Added FORCE_UI support
2. `run-ui.sh` - Created convenience script
3. `check-console.sh` - Created diagnostic script

## Next Steps

1. Run `./run-ui.sh` in iTerm2
2. You should see the Terminal.Gui interface
3. Test gameplay with arrow keys
4. Report if it works or what you see

## Expected Behavior

When working correctly, you'll see:
- Full-screen Terminal.Gui application
- Window with "Console Dungeon" title
- Menu bar at top (File, View, Audio, Help)
- Game view with player character (@) and dungeon
- Status information

## If Audio Plugin Gets Fixed

Once audio builds successfully:
- Movement will play beep sounds  
- F4 menu will show audio status
- afplay fallback will work on macOS

---

*Last updated: 2025-10-12 00:01*
*Version: 0.0.1-422 with FORCE_UI support*

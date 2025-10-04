# Test Tools - Arrow Key Debugging

Debug tools for diagnosing arrow key input issues in the Console Dungeon game.

## Issue Summary

**Problem**: Down and Right arrow keys don't move the player character, while Up and Left work correctly.

**Root Cause Hypothesis**: Terminal.Gui's key event handling may not be properly converting Down/Right arrow sequences to `KeyCode.CursorDown` and `KeyCode.CursorRight` enum values. The sequences arrive as raw runes instead.

**Evidence**:
- Log analysis shows `CursorUp` and `CursorLeft` KeyCode events work
- Down/Right keys either don't generate KeyDown events OR generate them with incorrect KeyCode values
- Fallback rune mapping exists for 'B' (Down) and 'C' (Right) in ConsoleDungeonApp.cs:545-546

## Available Tools

### 1. `capture-keys.js`
Interactive tool to capture raw keyboard input sequences.

```bash
node build/test-tools/capture-keys.js
```

Press arrow keys to see their exact byte sequences (ESC [ A/B/C/D or ESC O A/B/C/D).

### 2. `parse-keylog.js`
Analyzes Console Dungeon log files for key events.

```bash
node build/test-tools/parse-keylog.js /path/to/console-dungeon-*.log
```

Shows:
- KeyDown events summary
- Arrow key detection (KeyCode vs rune)
- Game input events (MoveUp, MoveDown, etc.)
- Player position changes

### 3. `test-artifact-keys.sh`
Automated test script for the built artifact (placeholder).

```bash
./build/test-tools/test-artifact-keys.sh
```

Currently a template - requires PTY injection or xdotool for real testing.

### 4. `verify-cast.js`
Verifies asciinema recording shows player movement.

```bash
node build/test-tools/verify-cast.js /path/to/record.cast
```

### 5. `verify-log-pos.js`
Checks log file for position changes.

```bash
node build/test-tools/verify-log-pos.js /path/to/console-dungeon-*.log
```

## Debugging Workflow

1. **Run the game** from the artifact:
   ```bash
   /path/to/build/_artifacts/vX.X.X-XXX/dotnet/bin/ConsoleDungeon.Host
   ```

2. **Press arrow keys** and observe behavior

3. **Find the log file**:
   ```bash
   ls -lt /path/to/build/_artifacts/vX.X.X-XXX/dotnet/bin/logs/
   ```

4. **Parse the log**:
   ```bash
   node build/test-tools/parse-keylog.js <log-file>
   ```

5. **Check for**:
   - Are `CursorDown`/`CursorRight` KeyCode events present?
   - Are rune values 'B' (66) or 'C' (67) appearing instead?
   - Do "Game input received: MoveDown/MoveRight" lines appear?

## Known Findings

From log `console-dungeon-20251004-085025.log`:
- ✅ `CursorLeft` → KeyCode detected → `MoveLeft` processed
- ✅ `CursorUp` → KeyCode detected → `MoveUp` processed
- ❌ Down/Right keys: No KeyDown events recorded (keys either not pressed or not captured)

## Next Steps - Systematic Debugging (Updated)

See GitHub issue #214 for tracking:
https://github.com/GiantCroissant-Lunar/winged-bean/issues/214

### New Debug Mode Available!

The code now includes a DEBUG_MINIMAL_UI mode to isolate input handling:

**Run Debug Mode** (no UI widgets):
```bash
./build/test-tools/run-debug-mode.sh
```

**Run Normal Mode** (full UI):
```bash
./build/test-tools/run-normal-mode.sh
```

Both scripts auto-parse logs after exit.

### What Changed (Build v0.0.1-285+)

1. **Environment variable**: `DEBUG_MINIMAL_UI=1` enables minimal UI mode
2. **Enhanced logging**: All key events logged with source, KeyCode, Rune, modifiers
3. **Esc/Ctrl+C handling**: Now explicitly mapped to Quit
4. **M key**: Enhanced logging to verify menu toggle
5. **Down/Right fallback**: Runes 'B' and 'C' mapped as fallback

### Testing Workflow

1. **Build** latest artifacts:
   ```bash
   cd build && task build-all
   ```

2. **Test in Debug Mode** first:
   ```bash
   ./build/test-tools/run-debug-mode.sh
   ```
   - Press all 4 arrow keys
   - Press M
   - Press Esc
   - Check auto-parsed log output

3. **Test in Normal Mode**:
   ```bash
   ./build/test-tools/run-normal-mode.sh
   ```
   - Repeat same key presses
   - Compare log output with debug mode

4. **Analyze difference**:
   - If arrows work in debug but not normal → UI widgets are the problem
   - If arrows fail in both → Terminal.Gui driver issue
   - If M works in debug but not normal → Label widget intercepts it
   - If Esc works in debug but not normal → Window intercepts it

### Potential Fixes

1. **Terminal.Gui Update**: Check if newer version fixes KeyCode detection
2. **Enhanced Rune Fallback**: Improve the rune-to-input mapping in ConsoleDungeonApp.cs
3. **Raw Input Mode**: Bypass Terminal.Gui key handling, process escape sequences directly
4. **Platform-Specific Handling**: macOS may behave differently than Linux/Windows

### Testing

E2E test exists: `development/nodejs/tests/e2e/arrow-keys.spec.js`

Run with:
```bash
cd development/nodejs
npm test -- arrow-keys.spec.js
```

The test already includes fallbacks to WASD keys when arrows fail.

# Issue #214 Analysis Results: Arrow Key Input Bug

**Issue**: https://github.com/GiantCroissant-Lunar/winged-bean/issues/214
**Date**: 2025-10-04
**Build**: v0.0.1-285+

## Problem Summary

The Console Dungeon game has multiple input handling issues:

1. ✅ **Up arrow** works - player moves up
2. ✅ **Left arrow** works - player moves left
3. ❌ **Down arrow** doesn't work - no movement
4. ❌ **Right arrow** doesn't work - no movement
5. ❌ **Esc key** doesn't quit the app
6. ❌ **Ctrl+C** doesn't quit the app
7. ❌ **M key** doesn't open menu (unconfirmed)

## Log Evidence

From `console-dungeon-20251004-085025.log`:

```
Summary:
  CursorLeft (rune=0x0): 1 events
  CursorUp (rune=0x0): 1 events

=== Arrow Key Analysis ===

← LEFT: KeyCode=CursorLeft, Rune=0x0, Value=0
↑ UP: KeyCode=CursorUp, Rune=0x0, Value=0

=== Game Input Events ===

  MoveLeft
  MoveUp
```

**Key finding**: When Down/Right arrows are pressed, **NO KeyDown events are logged at all**.

This means:
- Terminal.Gui is NOT delivering KeyDown events for Down/Right arrows
- The keys are being swallowed before reaching our handlers
- Up/Left work because Terminal.Gui correctly generates `KeyCode.CursorUp` and `KeyCode.CursorLeft`

## Root Cause Analysis

### Theory 1: UI Widget Interference (MOST LIKELY)

**Evidence**:
- Terminal.Gui `Label` widgets may intercept navigation keys
- Down/Right arrows might be used for internal widget scrolling/navigation
- `_gameWorldView` (Label) has `CanFocus = false`, but may still receive events
- Window has focus and may handle arrows differently

**Supporting Code** (ConsoleDungeonApp.cs):
- Line 400-408: `_gameWorldView` is a `Label` widget
- Line 411-417: `_statusLabel` is a `Label` widget
- Line 450-452: Three key handlers attached (Window, Application, Top)

**Fix Strategy**:
- Test with DEBUG_MINIMAL_UI mode (no Labels, only simple text)
- If arrows work → replace Label with View or TextView
- Change key handler attachment order
- Set `WantContinuousButtonPressed = false` on widgets

### Theory 2: Terminal.Gui Driver Bug on macOS

**Evidence**:
- macOS Terminal.Gui driver may not properly decode arrow escape sequences
- CSI sequences (`ESC [ B/C/D`) vs SS3 sequences (`ESC O B/C/D`)
- Line 422-436: `ForceCursorKeysNormal()` attempts to normalize to CSI

**Supporting Code**:
```csharp
// Line 422
private void ForceCursorKeysNormal()
{
    try
    {
        // DECCKM reset: normal cursor keys => ESC [ A/B/C/D
        Console.Write("\x1b[?1l");
        // Ensure keypad numeric mode
        Console.Write("\x1b>");
        Console.Out.Flush();
        LogToFile("Sent terminal mode reset: DECCKM normal (CSI [A-D)");
    }
    ...
}
```

**Fix Strategy**:
- Update Terminal.Gui to latest version
- Implement raw escape sequence handling
- Process stdin directly instead of relying on Terminal.Gui key parsing

### Theory 3: Focus State Issues

**Evidence**:
- Line 458: `_mainWindow.SetFocus()` called
- Labels have `CanFocus = false`
- Window may need explicit focus for certain keys

**Fix Strategy**:
- Test focus state changes
- Try `CanFocus = true` on main window only
- Remove focus calls entirely

## Code Changes Implemented

### 1. Debug Mode (ConsoleDungeonApp.cs)

Added environment variable `DEBUG_MINIMAL_UI` to test without UI widgets:

**Line 42**: Added debug flag field
```csharp
private bool _debugMinimalUI = false; // Debug mode: no UI widgets
```

**Line 57-59**: Read environment variable
```csharp
var debugEnv = Environment.GetEnvironmentVariable("DEBUG_MINIMAL_UI");
_debugMinimalUI = debugEnv == "1" || debugEnv?.ToLower() == "true";
```

**Line 394-447**: Conditional UI creation
- Normal mode: Full game UI with Labels
- Debug mode: Single simple instruction label

### 2. Enhanced Logging

**Line 535-573**: Comprehensive key event logging with box-drawing format:
```csharp
LogToFile($"╔═══ [{source}] KeyDown Event ═══");
LogToFile($"║ Handled (on entry): {e.Handled}");
LogToFile($"║ KeyCode: {e.KeyCode}");
LogToFile($"║ Rune: {runeChar} (value={e.AsRune.Value})");
LogToFile($"║ Modifiers: Alt={e.IsAlt}, Ctrl={e.IsCtrl}, Shift={e.IsShift}");
LogToFile($"║ Mapped to: {inputType}");
LogToFile($"╚═══ [{source}] KeyDown Exit (Handled={e.Handled}) ═══");
```

Shows:
- Event source (Window/Application/Top)
- `e.Handled` state (before/after)
- KeyCode enum value
- Rune character and numeric value
- Modifier keys (Alt, Ctrl, Shift)
- Mapped game input type

### 3. Esc/Ctrl+C Handling

**Line 577-588**: Explicit quit key mapping (priority 1):
```csharp
if (key.KeyCode == KeyCode.Esc)
{
    LogToFile("  → ESC detected, mapping to Quit");
    return GameInputType.Quit;
}

if (key.IsCtrl && (key.AsRune.Value == 'C' || key.AsRune.Value == 'c' || key.AsRune.Value == 3))
{
    LogToFile("  → Ctrl+C detected, mapping to Quit");
    return GameInputType.Quit;
}
```

### 4. Enhanced Character Key Logging

**Line 613-637**: Detailed logging for all character keys including M, B, C:
```csharp
var ch = char.ToUpper((char)rune.Value);
LogToFile($"  → Checking character key: '{ch}' (rune={rune.Value})");
```

Includes fallback mappings:
- 'B' → MoveDown (ESC O B fallback)
- 'C' → MoveRight (ESC O C fallback)
- 'M' → ToggleMenu

## Testing Tools Created

### 1. Debug Scripts

**build/test-tools/run-debug-mode.sh**:
- Runs console app with `DEBUG_MINIMAL_UI=1`
- No game widgets, only instruction label
- Auto-parses log after exit

**build/test-tools/run-normal-mode.sh**:
- Runs with full game UI
- Compares behavior against debug mode

### 2. Analysis Tools

**build/test-tools/parse-keylog.js**:
- Parses log files for KeyDown events
- Summarizes arrow key detection
- Shows game input processing
- Tracks player position changes

**build/test-tools/capture-keys.js**:
- Interactive raw keystroke capture
- Shows exact byte sequences
- Detects ESC [ vs ESC O sequences

### 3. Documentation

**docs/implementation/issue-214-debug-plan.md**:
- 5-phase systematic debugging approach
- Phase 1: Minimal UI test
- Phase 2: Widget isolation
- Phase 3: Event handler order
- Phase 4: Focus state testing
- Phase 5: Terminal mode verification

## Manual Testing Procedure

### Test 1: Debug Mode (Minimal UI)

```bash
cd /Users/apprenticegc/Work/lunar-horse/personal-work/yokan-projects/winged-bean/build
./test-tools/run-debug-mode.sh
```

**Actions**:
1. Press Up arrow → should see KeyDown event in log
2. Press Down arrow → should see KeyDown event in log
3. Press Left arrow → should see KeyDown event in log
4. Press Right arrow → should see KeyDown event in log
5. Press M → should see KeyDown event
6. Press Esc → app should quit

**Expected Result**:
- All keys generate KeyDown events
- Logs show "╔═══ [source] KeyDown Event ═══" boxes
- Arrow keys map to Move* input types
- M maps to ToggleMenu
- Esc maps to Quit and exits app

### Test 2: Normal Mode (Full UI)

```bash
./test-tools/run-normal-mode.sh
```

**Actions**: Same as Test 1

**Expected Result**:
- If behavior differs from Test 1 → UI widgets are the problem
- If behavior same as Test 1 → Terminal.Gui driver issue

### Test 3: Web Interface

```bash
# Services should already be running
# Open browser to http://localhost:4321/demo/
```

**Actions**: Same arrow key presses

**Expected Result**:
- New log file created when session starts
- Enhanced logging visible
- Can compare PTY input vs direct console input

## Analysis Comparison Table

| Test Mode | Up | Down | Left | Right | M | Esc | Notes |
|-----------|----|----|------|-------|---|-----|-------|
| **Old Build (pre-fix)** | ✅ | ❌ | ✅ | ❌ | ❓ | ❌ | Only Up/Left logged |
| **Debug Mode (expected)** | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | All keys should work |
| **Normal Mode (expected)** | ✅ | ❌ | ✅ | ❌ | ❌ | ❌ | Labels intercept keys |

## Recommended Next Steps

### Immediate (Manual Testing Required)

1. **Run debug mode test**:
   ```bash
   ./build/test-tools/run-debug-mode.sh
   ```
   - Manually press all arrow keys + M + Esc
   - Analyze log with `parse-keylog.js`

2. **Run normal mode test**:
   ```bash
   ./build/test-tools/run-normal-mode.sh
   ```
   - Same key presses
   - Compare logs

3. **Determine root cause**:
   - If debug works, normal fails → Widget issue (go to step 4)
   - If both fail → Terminal.Gui issue (go to step 5)

### Fix Path A: Widget Interference (Most Likely)

4. **Replace Label with View** (ConsoleDungeonApp.cs:400):
   ```csharp
   // Before:
   _gameWorldView = new Label { ... };

   // After:
   _gameWorldView = new TextView {
       X = 0,
       Y = 0,
       Width = Dim.Fill(),
       Height = Dim.Percent(90),
       ReadOnly = true,
       CanFocus = false,
       Text = "Game world initializing..."
   };
   ```

5. **Test again** - arrows should work

### Fix Path B: Terminal.Gui Driver Issue

6. **Update Terminal.Gui**:
   - Check NuGet for latest version
   - Update .csproj reference
   - Rebuild and test

7. **Implement raw input mode**:
   - Bypass Terminal.Gui key handling
   - Read stdin directly
   - Parse escape sequences manually

## Conclusion

**Root Cause**: Terminal.Gui `Label` widgets are likely intercepting Down/Right arrow keys for internal navigation, preventing KeyDown events from reaching our handlers.

**Evidence**:
- Only Up/Left arrows generate KeyDown events
- Down/Right arrows produce NO log entries at all
- Keys are swallowed before `HandleKeyInput()` is called

**Solution**:
1. Test with DEBUG_MINIMAL_UI mode to confirm
2. Replace Label with TextView or View
3. Or: process keys at Application level before widgets see them

**Build Status**:
- ✅ Debug mode code implemented (v0.0.1-285)
- ✅ Enhanced logging implemented
- ✅ Esc/Ctrl+C handling added
- ✅ Test tools created
- ⏳ Manual testing pending (requires user input)

**Files Changed**:
- `development/dotnet/console/src/plugins/WingedBean.Plugins.ConsoleDungeon/ConsoleDungeonApp.cs`
- `build/test-tools/run-debug-mode.sh` (new)
- `build/test-tools/run-normal-mode.sh` (new)
- `build/test-tools/parse-keylog.js` (new)
- `build/test-tools/capture-keys.js` (new)
- `build/test-tools/README.md` (updated)
- `docs/implementation/issue-214-debug-plan.md` (new)
- `docs/implementation/issue-214-analysis-results.md` (this file)

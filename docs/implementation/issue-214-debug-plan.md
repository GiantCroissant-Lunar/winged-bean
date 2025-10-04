# Issue #214 Debug Plan: Arrow Key Input & UI Event Handling

**Issue**: https://github.com/GiantCroissant-Lunar/winged-bean/issues/214

## Problem Statement

Multiple input handling issues in ConsoleDungeonApp:

1. ❌ **Down/Right arrows don't work** - player doesn't move
2. ❌ **Esc doesn't quit** - app doesn't respond to Esc key
3. ❌ **Ctrl+C doesn't quit** - terminal interrupt not working
4. ❌ **M key doesn't open menu** - no menu toggle response

**Hypothesis**: UI widgets (Label, Window) are intercepting/transforming key events before they reach our handlers.

## Current Architecture

```
Terminal Input
    ↓
Terminal.Gui Driver
    ↓
Application.KeyDown  ← Handler attached (line 410)
    ↓
Application.Top.KeyDown  ← Handler attached (line 412)
    ↓
_mainWindow.KeyDown  ← Handler attached (line 408)
    ↓
[Label widgets may intercept here]
    ↓
HandleKeyInput() method
    ↓
MapKeyToGameInput()
    ↓
Game logic
```

**Issue**: Labels, Windows, or other widgets in the chain may:
- Transform arrow keys before KeyDown fires
- Mark events as `Handled` and stop propagation
- Convert KeyCode to different representations
- Swallow certain key combinations (Esc, Ctrl+C, M)

## Debugging Strategy

### Phase 1: Minimal Input Test (No UI Widgets)

**Goal**: Verify raw key events reach our handlers without widget interference.

**Approach**:
1. Create `TestInputMode` flag to disable all UI widgets
2. Run with empty Window (no Label children)
3. Log every key event at all 3 handler levels
4. Test all problematic keys: Up, Down, Left, Right, Esc, Ctrl+C, M

**Expected Outcome**:
- If keys work → widgets are the problem
- If keys fail → Terminal.Gui driver or terminal mode issue

### Phase 2: Widget Isolation Testing

**Goal**: Identify which specific widget intercepts keys.

**Test Sequence**:
1. Window only (no children) → test keys
2. Window + single Label (game world view) → test keys
3. Window + single Label (status bar) → test keys
4. Window + both Labels → test keys

**For each configuration**, log:
- Which KeyDown handler fires (Application/Top/Window)
- KeyCode value
- Rune value
- `e.Handled` status before and after our handler

### Phase 3: Event Handler Order Testing

**Goal**: Determine if handler attachment order matters.

**Test Variations**:
1. Only `_mainWindow.KeyDown`
2. Only `Application.KeyDown`
3. Only `Application.Top.KeyDown`
4. All three (current setup)
5. Reverse order attachment

### Phase 4: Focus & CanFocus Investigation

**Goal**: Check if focus state affects key routing.

**Current State** (ConsoleDungeonApp.cs):
- Line 392: `_gameWorldView.CanFocus = false`
- Line 415: `_mainWindow.SetFocus()`

**Test**:
- Set `_gameWorldView.CanFocus = true` → test keys
- Set `_statusLabel.CanFocus = true` → test keys
- Remove `SetFocus()` call → test keys
- Call `SetFocus()` on different widgets → test keys

### Phase 5: Terminal Mode & Key Normalization

**Goal**: Verify terminal mode settings are correct.

**Current Setup** (ConsoleDungeonApp.cs:422-436):
```csharp
ForceCursorKeysNormal() {
    Console.Write("\x1b[?1l");  // DECCKM reset → CSI mode
    Console.Write("\x1b>");      // Keypad numeric mode
}
```

**Test**:
- Log raw terminal sequences at OS level
- Check if Down/Right send different codes than Up/Left
- Verify Terminal.Gui driver receives correct sequences
- Compare CSI (`ESC [ B`) vs SS3 (`ESC O B`) handling

## Implementation Plan

### Step 1: Add Debug Mode Flag

Add to ConsoleDungeonApp.cs constructor:

```csharp
private readonly bool _debugMinimalUI;

public ConsoleDungeonApp(ILogger<ConsoleDungeonApp> logger, bool debugMinimalUI = false)
{
    _logger = logger;
    _debugMinimalUI = debugMinimalUI;
    // ...
}
```

### Step 2: Comprehensive Key Event Logging

Enhance `HandleKeyInput()` (line 493):

```csharp
private void HandleKeyInput(object? sender, Key e)
{
    var source = sender == _mainWindow ? "Window" :
                 sender == Application.Top ? "Top" : "Application";

    LogToFile($"[{source}] KeyDown ENTRY: Handled={e.Handled}");
    LogToFile($"  KeyCode={e.KeyCode}");
    LogToFile($"  Rune=0x{e.AsRune.Value:X} ({(char)e.AsRune.Value})");
    LogToFile($"  IsAlt={e.IsAlt}, IsCtrl={e.IsCtrl}, IsShift={e.IsShift}");

    // Existing logic...

    LogToFile($"[{source}] KeyDown EXIT: Handled={e.Handled}, Mapped={inputType}");
}
```

### Step 3: Minimal UI Mode

Modify `CreateMainWindow()` (line 370):

```csharp
private void CreateMainWindow()
{
    LogToFile($"CreateMainWindow called (debugMinimalUI={_debugMinimalUI})");

    _mainWindow = new Window()
    {
        Title = _debugMinimalUI ? "Debug: Minimal Input Test" : "Console Dungeon...",
        // ... existing config
    };

    if (!_debugMinimalUI)
    {
        // Only add UI widgets in normal mode
        _gameWorldView = new Label { /* ... */ };
        _statusLabel = new Label { /* ... */ };
        _mainWindow.Add(_gameWorldView, _statusLabel);
    }
    else
    {
        LogToFile("DEBUG MODE: No UI widgets added");
        // Add simple text overlay to show we're in debug mode
        var debugLabel = new Label
        {
            X = 0,
            Y = 0,
            Text = "DEBUG MODE - Press keys to test...\nEsc=quit, Arrows=move, M=menu, Ctrl+C=interrupt",
            CanFocus = false
        };
        _mainWindow.Add(debugLabel);
    }

    // Attach handlers in specific order for testing
    LogToFile("Attaching key handlers: Window -> Application -> Top");
    _mainWindow.KeyDown += HandleKeyInput;
    Application.KeyDown += HandleKeyInput;
    try { Application.Top.KeyDown += HandleKeyInput; } catch { }

    // ... rest of setup
}
```

### Step 4: Quit Key Handling

Add explicit Esc/Ctrl+C handling in `MapKeyToGameInput()` (line 513):

```csharp
private GameInputType? MapKeyToGameInput(Key key)
{
    // Priority 1: Quit keys
    if (key.KeyCode == KeyCode.Esc)
    {
        LogToFile("ESC detected - mapping to Quit");
        return GameInputType.Quit;
    }

    if (key.IsCtrl && key.AsRune.Value == 'C')
    {
        LogToFile("Ctrl+C detected - mapping to Quit");
        return GameInputType.Quit;
    }

    // Priority 2: KeyCode for arrows
    var fromKeyCode = key.KeyCode switch
    {
        // ... existing arrow mappings
    };

    // ... rest of method
}
```

### Step 5: Menu Key Fix

Check if 'M' is being intercepted. Add logging:

```csharp
if (rune.Value >= 32 && rune.Value < 127)
{
    var ch = char.ToUpper((char)rune.Value);
    LogToFile($"Checking character key: '{ch}' (rune={rune.Value})");

    return ch switch
    {
        'M' => GameInputType.ToggleMenu,  // This should work...
        // ...
    };
}
```

## Testing Checklist

### Test A: Minimal UI Mode
- [ ] Build with `_debugMinimalUI = true`
- [ ] Run artifact
- [ ] Press Up → logs show KeyDown + MoveUp
- [ ] Press Down → logs show KeyDown + MoveDown
- [ ] Press Left → logs show KeyDown + MoveLeft
- [ ] Press Right → logs show KeyDown + MoveRight
- [ ] Press M → logs show KeyDown + ToggleMenu
- [ ] Press Esc → logs show KeyDown + Quit → app exits
- [ ] Press Ctrl+C → logs show KeyDown + Quit → app exits

### Test B: Widget Interference
- [ ] Build with `_debugMinimalUI = false` (normal mode)
- [ ] Run artifact
- [ ] Repeat all keys from Test A
- [ ] Compare logs: which keys work vs fail?
- [ ] Check: does `e.Handled` get set to true before our handler?

### Test C: Handler Priority
- [ ] Comment out `Application.KeyDown +=`
- [ ] Comment out `Application.Top.KeyDown +=`
- [ ] Leave only `_mainWindow.KeyDown +=`
- [ ] Test all keys
- [ ] Reverse: only Application-level handler
- [ ] Test all keys

### Test D: Focus States
- [ ] Set `CanFocus = true` on game world Label
- [ ] Call `SetFocus()` on it
- [ ] Test arrow keys
- [ ] Check logs for focus-related events

## Expected Results

| Scenario | Expected | Action if Fails |
|----------|----------|----------------|
| Minimal UI + arrows | All 4 arrows work | Terminal.Gui driver issue → check version/platform |
| Minimal UI + Esc | App quits | KeyCode.Esc not mapped → add mapping |
| Minimal UI + Ctrl+C | App quits | Terminal raw mode blocks signal → handle in code |
| Full UI + arrows | Up/Left work, Down/Right fail | Label widget intercepts → replace with View |
| Full UI + M | Menu toggles | Widget swallows 'M' → change menu key or widget |

## Success Criteria

✅ All tests pass in minimal UI mode
✅ Identify exact widget causing interference
✅ Document workaround or fix
✅ Update GitHub issue #214 with findings

## Implementation Order

1. ✅ Create this debug plan
2. ⏭️ Add debug logging to `HandleKeyInput()`
3. ⏭️ Add minimal UI mode flag
4. ⏭️ Add Esc/Ctrl+C handling
5. ⏭️ Build and test
6. ⏭️ Analyze logs
7. ⏭️ Implement fix based on findings
8. ⏭️ Update issue #214

## References

- **Code**: `development/dotnet/console/src/plugins/WingedBean.Plugins.ConsoleDungeon/ConsoleDungeonApp.cs`
- **Issue**: https://github.com/GiantCroissant-Lunar/winged-bean/issues/214
- **Terminal.Gui Docs**: https://gui-cs.github.io/Terminal.Gui/
- **Key Event Handling**: Check Terminal.Gui `Responder.ProcessKey()` source

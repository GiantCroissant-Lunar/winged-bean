---
id: RFC-0021
title: Input Mapping and Scoped Routing
status: Implemented
category: framework, architecture, console, input
created: 2025-10-04
updated: 2025-01-09
implemented: 2025-01-09
author: Claude Code & ApprenticeGC
related: RFC-0020, RFC-0018, RFC-0002
implementation: docs/implementation/rfc-0020-0021-implementation-summary.md
commits: a331a35, 0b393a3
---

# RFC-0021: Input Mapping and Scoped Routing

## Summary

Define framework-agnostic input contracts and a scoped routing model so UI providers (e.g., Terminal.Gui v2) capture raw key events, map them into domain `GameInputEvent`s, and route them deterministically to the correct consumer (dialog vs gameplay). This RFC complements RFC-0020 by removing input responsibilities from Tier 3 app code and placing them behind contracts usable by any scene/UI provider.

## Motivation

### Problems Observed

- Global key handlers in `ConsoleDungeonApp` can conflict with dialogs/menus, causing keys to be handled in the wrong context.
- Terminal/driver differences require robust handling of arrow keys via CSI/SS3 sequences and fallbacks.
- Input logic is hard to test when interleaved with UI framework code.

### Goals

- Framework-agnostic input mapping and routing.
- Scoped, modal-safe input delivery where dialogs capture input without leaks.
- Deterministic propagation with clear "handled" semantics.
- Reusable, unit-testable input components.

## Proposal

### Contracts (Tier 1)

Location: `development/dotnet/framework/src/WingedBean.Contracts.Input/`

```csharp
namespace WingedBean.Contracts.Input;

using System;
using WingedBean.Contracts.Game;

/// <summary>
/// Raw key/rune event from a platform.
/// Implementations translate framework-specific events (Terminal.Gui, Unity, etc.) into this.
/// </summary>
public readonly record struct RawKeyEvent(
    int? VirtualKey,        // e.g., ConsoleKey cast or framework-specific code
    uint? Rune,             // Unicode code point if available
    bool IsCtrl,
    bool IsAlt,
    bool IsShift,
    DateTimeOffset Timestamp
);

/// <summary>
/// Maps raw key events into high-level game input events.
/// Framework-agnostic: implementations handle platform-specific quirks
/// (CSI sequences, SS3, ESC disambiguation, etc.)
/// </summary>
public interface IInputMapper
{
    /// <summary>
    /// Map a raw key into a GameInputEvent or null if not recognized/pending.
    /// Implementations may buffer incomplete sequences (e.g., ESC [ A)
    /// or use short timers to disambiguate standalone ESC vs CSI sequences.
    /// </summary>
    GameInputEvent? Map(RawKeyEvent rawEvent);

    /// <summary>
    /// Reset mapper state (clear buffered sequences, timers).
    /// Called when focus lost or context changed.
    /// </summary>
    void Reset();
}

/// <summary>
/// Scoped routing model for input events.
/// New scopes are pushed when dialogs/menus open and popped on close.
/// Enables modal input capture without leaking to gameplay.
/// </summary>
public interface IInputRouter
{
    /// <summary>
    /// Push a new input scope onto the stack.
    /// Returns IDisposable that pops the scope when disposed.
    /// </summary>
    IDisposable PushScope(IInputScope scope);

    /// <summary>
    /// Dispatch a game input event to the current top scope.
    /// If top scope doesn't handle it and CaptureAll is false,
    /// optionally propagates to lower scopes (default: no propagation).
    /// </summary>
    void Dispatch(GameInputEvent inputEvent);

    /// <summary>
    /// Get the current top scope (active input handler).
    /// Null if no scopes pushed.
    /// </summary>
    IInputScope? Top { get; }
}

/// <summary>
/// A handler for a scope of input.
/// Scopes are pushed/popped to handle modal dialogs, menus, gameplay, etc.
/// </summary>
public interface IInputScope
{
    /// <summary>
    /// Handle the input event.
    /// Returns true if handled (stops propagation).
    /// Returns false if not handled (may propagate to lower scope if CaptureAll is false).
    /// </summary>
    bool Handle(GameInputEvent inputEvent);

    /// <summary>
    /// If true, this scope captures ALL input events even if not handled.
    /// Used for modal dialogs to prevent input leaking to gameplay.
    /// </summary>
    bool CaptureAll { get; }
}
```

### Default Implementations (Tier 4)

**Location**: `development/dotnet/console/src/providers/WingedBean.Providers.Input/`

#### DefaultInputMapper

Concrete implementation of `IInputMapper` for console/terminal environments:

**Mapping priority**:
1. **VirtualKey codes** (ConsoleKey.UpArrow, etc.) - most reliable on real terminals
2. **CSI sequences**: ESC `[` `A|B|C|D` → Up/Down/Right/Left (xterm standard)
3. **SS3 sequences**: ESC `O` `A|B|C|D` → Up/Down/Right/Left (VT100 application mode)
4. **WASD fallback**: W/A/S/D → Up/Left/Down/Right
5. **Special keys** (examples):
   - Space → Attack
   - M → ToggleMenu, I → ToggleInventory
   - Q → Quit, E → Use
   - `Ctrl+C` → Quit, standalone `ESC` → Quit (with disambiguation)

**ESC disambiguation**:
- Use 150-200ms timer after ESC received
- If `[` or `O` received within timer → CSI/SS3 sequence
- If timer expires with no follow-up → standalone ESC = Quit

**State management**:
- Buffer incomplete sequences (ESC waiting for `[` or `O`)
- Reset state on focus loss via `Reset()` method

#### DefaultInputRouter

Stack-based scope router:

**Behavior**:
- Maintains `Stack<IInputScope>`
- `PushScope()` returns `IDisposable` that pops on dispose
- `Dispatch()` calls `Top.Handle(inputEvent)`
- If `Top.Handle()` returns false:
  - If `Top.CaptureAll == true`: event stops (modal capture)
  - If `Top.CaptureAll == false`: **optionally** propagate to next scope (configurable)
  - **Default**: No propagation for simplicity/predictability

**Thread safety**: Not thread-safe by default (assumes UI thread dispatch)

### Provider Integration (Terminal.Gui v2)

Provider: `WingedBean.Providers.TerminalGuiScene`

- Capture keys on a top-level focusable view (`CanFocus = true`).
- Translate Terminal.Gui key args to `RawKeyEvent` and call `IInputMapper.Map`.
- If a `GameInputEvent` is returned, call `IInputRouter.Dispatch(e)` and set `args.Handled = true` when the router handles it, to avoid default navigation.
- Push a modal dialog scope when opening dialogs (`CaptureAll = true`), pop on close.
- Maintain focus on the input view after dialog closes.

Example (pseudo-code):
```csharp
view.KeyDown += (s, args) => {
    var rk = new RawKeyEvent(
        VirtualKey: (int?)args.Key.KeyCodeAsConsoleKey(),
        Rune: args.Key.AsRune.Value,
        IsCtrl: args.Key.IsCtrl,
        IsAlt: args.Key.IsAlt,
        IsShift: args.Key.IsShift,
        Timestamp: DateTimeOffset.UtcNow
    );
    var mapped = mapper.Map(rk);
    if (mapped is { } e) {
        router.Dispatch(e);
        args.Handled = true; // prevent default navigation when handled
    }
};
```

### App Wiring

- Gameplay scope (default):
  - Converts `GameInputEvent` → `GameInput` and calls `IDungeonGameService.HandleInput`.
  - Lives for the duration of the app under the router.

- UI scope(s):
  - For menus/inventory, a modal scope handles toggle/selection keys and returns handled.
  - Gameplay scope does not receive keys while modal scope is active.

Integration choice:
- Either subscribe the app to router outputs and call `IDungeonGameService.HandleInput`, or
- Feed mapped events into `IGameUIService.InputObservable` and let the app subscribe there.
Both patterns are acceptable; prefer the router-first approach for fewer hops.

### Relationship to Other RFCs

**RFC-0020 (Scene Service)**:
- RFC-0020 defines `ISceneService` for rendering and UI lifecycle
- This RFC defines input capture, mapping, and routing
- Scene providers implement both: own the UI, capture input, and use router/mapper to deliver events
- `TerminalGuiSceneProvider` will use both `ISceneService` and input contracts

**RFC-0018 (Render and UI Services)**:
- RFC-0018 introduced `GameInputEvent` in `WingedBean.Contracts.Game`
- This RFC reuses `GameInputEvent` as the mapped output type
- `IInputMapper.Map()` returns `GameInputEvent` (already defined)
- `IGameUIService.InputObservable` uses `GameInputEvent` (already exists)

**Existing types reused**:
- `GameInputEvent` (from RFC-0018) - high-level input command
- `GameInputType` enum - MoveUp, MoveDown, Attack, etc.
- No new domain types needed - this RFC focuses on mapping/routing infrastructure

## Implementation Plan

### Phase 1: Create Contracts (Tier 1)

1. Create `WingedBean.Contracts.Input` project
2. Define interfaces: `IInputMapper`, `IInputRouter`, `IInputScope`
3. Define types: `RawKeyEvent`
4. Add to `Framework.sln`
5. Run `dotnet build` to verify

### Phase 2: Create Default Providers (Tier 4)

1. Create `WingedBean.Providers.Input` project
2. Implement `DefaultInputMapper` with:
   - VirtualKey mapping (ConsoleKey → GameInputType)
   - CSI/SS3 sequence buffering
   - ESC disambiguation with timer
   - WASD fallback
3. Implement `DefaultInputRouter` with:
   - Stack-based scope management
   - IDisposable scope handle
   - Configurable propagation (default: top-only)
4. Add unit tests for mapper and router

### Phase 3: Integrate with TerminalGuiSceneProvider

1. Add `IInputMapper` and `IInputRouter` dependencies to `TerminalGuiSceneProvider`
2. Create focusable input view (`CanFocus = true`)
3. Wire `KeyDown` event:
   - Translate `Key` → `RawKeyEvent`
   - Call `mapper.Map(rawEvent)`
   - If mapped, call `router.Dispatch(inputEvent)` and set `args.Handled = true`
4. Push/pop dialog scopes when opening/closing menus
5. Restore focus to input view after dialog closes

### Phase 4: Refactor ConsoleDungeonApp

1. Remove all direct key handling code
2. Remove `using Terminal.Gui` references
3. Create gameplay `IInputScope` implementation:
   - Converts `GameInputEvent` → `GameInput`
   - Calls `IDungeonGameService.HandleInput()`
4. Register gameplay scope with router during startup

### Phase 5: Update GameUIServiceProvider

1. Have menu dialogs push their own `IInputScope`
2. Set `CaptureAll = true` for modal dialogs
3. Handle menu-specific keys (I for inventory, etc.)
4. Pop scope when dialog closes

### Phase 6: Testing & Documentation

1. Write unit tests for mapper (CSI/SS3/ESC sequences)
2. Write unit tests for router (scope push/pop, capture)
3. Add integration tests with Terminal.Gui FakeDriver
4. Update RFC-0020 to cross-reference RFC-0021
5. Update implementation docs

## Testing Strategy

Unit tests
- `DefaultInputMapper`:
  - ConsoleKey arrows map correctly.
  - ESC `[` `B` → MoveDown; ESC `O` `C` → MoveRight.
  - WASD fallback; `Ctrl+C` and standalone `Esc` → Quit with timeout.
- `DefaultInputRouter`:
  - Scope push/pop order, `CaptureAll` behavior, and handled semantics.

Integration tests (Terminal.Gui FakeDriver)
- Send Right/Down arrows and verify `IDungeonGameService.HandleInput` receives `MoveRight/MoveDown`.
- Send ESC `[` `B` and verify `MoveDown`.
- Open a modal dialog scope; verify gameplay inputs do not reach game until dialog closes.

## Benefits

- Modal correctness via scope stacking.
- Portability across UI frameworks.
- Testable input mapping independent of UI.
- Clear ownership: providers capture keys; mapper/ router define logic.

## Risks & Mitigations

- Timing differences for ESC sequences: use configurable timeout, cover with tests.
- Focus loss in Terminal.Gui: enforce `CanFocus = true` and restore focus after dialogs.
- Over-bubbling complexity: default to top-scope only to keep behavior predictable.

## Success Criteria

- [ ] Contracts compile in `Framework.sln`.
- [ ] TerminalGui scene provider routes inputs through mapper/router.
- [ ] Gameplay and dialog scopes behave correctly under tests.
- [ ] ConsoleDungeonApp has no direct key handling.

## Related Work

- RFC-0020: Scene Service and Terminal UI Separation.
- RFC-0018: Render and UI Services.
- RFC-0002: 4-Tier Service Architecture.

## Appendix A: Project Summary

### NEW Projects

1. **WingedBean.Contracts.Input** (Tier 1)
   - Location: `framework/src/WingedBean.Contracts.Input/`
   - Contains: `IInputMapper`, `IInputRouter`, `IInputScope`, `RawKeyEvent`
   - Target: `netstandard2.1`

2. **WingedBean.Providers.Input** (Tier 4)
   - Location: `console/src/providers/WingedBean.Providers.Input/`
   - Contains: `DefaultInputMapper`, `DefaultInputRouter`
   - Dependencies: `WingedBean.Contracts.Input`, `WingedBean.Contracts.Game`
   - Target: `net8.0`

3. **WingedBean.Providers.Input.Tests**
   - Location: `console/tests/providers/WingedBean.Providers.Input.Tests/`
   - Unit tests for mapper and router

### UPDATED Projects

1. **WingedBean.Providers.TerminalGuiScene**
   - Add dependency: `WingedBean.Contracts.Input`, `WingedBean.Providers.Input`
   - Wire input: `KeyDown` → mapper → router

2. **WingedBean.Plugins.ConsoleDungeon**
   - Remove all direct key handling
   - Implement gameplay `IInputScope`

3. **WingedBean.Plugins.DungeonGame** (GameUIServiceProvider)
   - Push/pop dialog input scopes

## Appendix B: File Structure

```
development/dotnet/
├── framework/src/
│   └── WingedBean.Contracts.Input/           [NEW]
│       ├── IInputMapper.cs
│       ├── IInputRouter.cs
│       ├── IInputScope.cs
│       └── RawKeyEvent.cs
│
├── console/
│   ├── src/
│   │   ├── providers/
│   │   │   ├── WingedBean.Providers.Input/   [NEW]
│   │   │   │   ├── DefaultInputMapper.cs
│   │   │   │   ├── DefaultInputRouter.cs
│   │   │   │
│   │   │   └── WingedBean.Providers.TerminalGuiScene/  [UPDATE]
│   │   │       └── (Wire KeyDown → mapper → router)
│   │   │
│   │   └── plugins/
│   │       └── WingedBean.Plugins.ConsoleDungeon/  [REFACTOR]
│   │           └── GameplayInputScope.cs  [NEW]
│   │
│   └── tests/
│       └── providers/
│           └── WingedBean.Providers.Input.Tests/  [NEW]
│               ├── DefaultInputMapperTests.cs
│               └── DefaultInputRouterTests.cs
```

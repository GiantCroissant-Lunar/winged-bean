---
id: RFC-0026
title: Input Action System
status: Proposed
category: framework, architecture, input
created: 2025-10-04
updated: 2025-10-04
author: Claude Code & ApprenticeGC
related: RFC-0021, RFC-0020, RFC-0018, RFC-0002
---

# RFC-0026: Input Action System

## Summary

Introduce a Unity-inspired Input Action System to enhance RFC-0021's input mapping with action maps, composite bindings, action phases, and rebindable controls. This provides a higher-level, more flexible input abstraction while maintaining compatibility with the existing `IInputMapper`/`IInputRouter` foundation.

## Motivation

### Current State (RFC-0021)

RFC-0021 provides low-level input mapping:

```
RawKeyEvent → IInputMapper → GameInputEvent → IInputRouter → IInputScope
```

**Limitations**:
1. **No context grouping**: "Gameplay keys" vs "Menu keys" mixed in one mapper
2. **No composite inputs**: WASD requires 4 separate `MoveUp/Down/Left/Right` events instead of `Vector2`
3. **No action phases**: Can't detect "tap vs hold" or "started vs canceled"
4. **Hardcoded bindings**: Key mappings live in code, not configuration
5. **No rebinding**: Users can't customize controls
6. **Single-value actions**: Can't read current state (e.g., "is Attack button held?")

### Unity Input System Benefits

Unity's Input System (https://docs.unity3d.com/Packages/com.unity.inputsystem@1.7/) provides:

1. **Action Maps**: Group related actions ("Gameplay", "Menu", "Inventory")
2. **Composite Bindings**: WASD/Arrows → `Vector2`, Multi-button combos
3. **Action Phases**: `started` → `performed` → `canceled` lifecycle
4. **Processors**: Transform values (normalize, invert, scale, clamp)
5. **Interactions**: Detect patterns (tap, hold, double-tap, multi-tap)
6. **JSON Configuration**: Define bindings in `.inputactions` files
7. **Rebinding UI**: Runtime control customization

### Why This Matters for Winged Bean

**Current pain points**:
- `ConsoleDungeonApp` has 70+ lines of key mapping logic (lines 688-757)
- WASD and Arrow keys handled separately (duplicate logic)
- No way to add "hold Shift to run" without code changes
- Menu vs gameplay input handled with `if (_uiService.IsMenuVisible)` checks
- Can't support gamepad without rewriting `IInputMapper`

**What we need**:
- **Multi-platform support**: Console (keyboard), Unity (gamepad, touch), Godot (all devices)
- **Configurable controls**: Users customize keys in settings
- **Context switching**: Auto-enable/disable input based on game state
- **Advanced patterns**: Charge attacks, combos, twin-stick aiming

## Proposal

### Architecture Overview

**Three-layer design** (builds on RFC-0021):

```
┌─ Layer 1: Raw Input (RFC-0021) ───────────────────────────┐
│ RawKeyEvent → IInputMapper → GameInputEvent               │
│ (Device-specific, low-level)                               │
└────────────────────────────────────────────────────────────┘
                            ↓
┌─ Layer 2: Input Actions (NEW - this RFC) ─────────────────┐
│ IInputActionMap → IInputAction → InputActionEvent         │
│ (Game-specific, high-level, rebindable)                   │
└────────────────────────────────────────────────────────────┘
                            ↓
┌─ Layer 3: Game Logic ──────────────────────────────────────┐
│ Subscribe to actions: player.Move.performed += OnMove      │
│ Or poll values: var movement = player.Move.ReadValue<Vector2>() │
└────────────────────────────────────────────────────────────┘
```

### Key Concepts

#### 1. Input Action

High-level game command (e.g., "Move", "Attack", "Jump"):

```csharp
namespace WingedBean.Contracts.InputSystem;

/// <summary>
/// Represents a high-level game action (e.g., Move, Attack, Jump).
/// Abstracts away device-specific input details.
/// </summary>
public interface IInputAction
{
    /// <summary>
    /// Unique name of the action (e.g., "Move", "Attack").
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Action type determines value type and behavior.
    /// </summary>
    InputActionType ActionType { get; }

    /// <summary>
    /// Observable stream of action events (started, performed, canceled).
    /// Use for event-driven input handling.
    /// </summary>
    IObservable<InputActionEvent> Events { get; }

    /// <summary>
    /// Read the current value of the action.
    /// Use for polling-style input (e.g., in Update loop).
    /// </summary>
    /// <typeparam name="TValue">Expected value type (float, Vector2, bool)</typeparam>
    TValue ReadValue<TValue>() where TValue : struct;

    /// <summary>
    /// Check if action is currently active (button pressed, axis non-zero).
    /// </summary>
    bool IsPressed { get; }

    /// <summary>
    /// Enable this action to start receiving input.
    /// </summary>
    void Enable();

    /// <summary>
    /// Disable this action to stop receiving input.
    /// </summary>
    void Disable();

    /// <summary>
    /// Is this action currently enabled?
    /// </summary>
    bool IsEnabled { get; }
}

/// <summary>
/// Action type determines expected value and behavior.
/// </summary>
public enum InputActionType
{
    /// <summary>
    /// Button action: bool value, triggers on press/release.
    /// Examples: Attack, Jump, Use
    /// </summary>
    Button,

    /// <summary>
    /// Value action: continuous value (float, Vector2, Vector3).
    /// Examples: Move (Vector2), Look (Vector2), Throttle (float)
    /// </summary>
    Value,

    /// <summary>
    /// Pass-through action: forwards raw input without processing.
    /// Examples: Text input, mouse position
    /// </summary>
    PassThrough
}
```

#### 2. Action Map

Group of related actions with shared lifecycle:

```csharp
/// <summary>
/// Action map groups related actions with shared enable/disable lifecycle.
/// Only one map is typically active at a time (e.g., Gameplay OR Menu).
/// </summary>
public interface IInputActionMap
{
    /// <summary>
    /// Unique name of the action map (e.g., "Gameplay", "Menu", "Inventory").
    /// </summary>
    string Name { get; }

    /// <summary>
    /// All actions in this map.
    /// </summary>
    IReadOnlyList<IInputAction> Actions { get; }

    /// <summary>
    /// Get action by name.
    /// </summary>
    IInputAction? GetAction(string actionName);

    /// <summary>
    /// Enable all actions in this map.
    /// Typically disables other maps (exclusive activation).
    /// </summary>
    void Enable();

    /// <summary>
    /// Disable all actions in this map.
    /// </summary>
    void Disable();

    /// <summary>
    /// Is this map currently enabled?
    /// </summary>
    bool IsEnabled { get; }

    /// <summary>
    /// Indexer for convenient action access.
    /// </summary>
    IInputAction this[string actionName] { get; }
}
```

#### 3. Action Event

Lifecycle events for actions:

```csharp
/// <summary>
/// Event raised when an action changes phase.
/// </summary>
public record InputActionEvent(
    IInputAction Action,
    InputActionPhase Phase,
    object? Value,  // Current value (Vector2, float, bool, etc.)
    DateTimeOffset Timestamp
);

/// <summary>
/// Action lifecycle phases.
/// </summary>
public enum InputActionPhase
{
    /// <summary>
    /// Action is disabled.
    /// </summary>
    Disabled,

    /// <summary>
    /// Action is enabled but waiting for input.
    /// </summary>
    Waiting,

    /// <summary>
    /// Input started (e.g., button pressed, axis moved).
    /// </summary>
    Started,

    /// <summary>
    /// Action performed (e.g., button released for tap, threshold reached).
    /// </summary>
    Performed,

    /// <summary>
    /// Action canceled (e.g., button released before threshold).
    /// </summary>
    Canceled
}
```

#### 4. Input Binding

Maps device input to actions:

```csharp
/// <summary>
/// Binds device input paths to actions.
/// Supports simple (single key) and composite (multiple keys) bindings.
/// </summary>
public interface IInputBinding
{
    /// <summary>
    /// Action name this binding targets.
    /// </summary>
    string ActionName { get; }

    /// <summary>
    /// Binding type (simple or composite).
    /// </summary>
    InputBindingType BindingType { get; }

    /// <summary>
    /// Device path (e.g., "Keyboard/W", "Gamepad/LeftStick").
    /// Only for simple bindings.
    /// </summary>
    string? Path { get; }

    /// <summary>
    /// Composite name (e.g., "2DVector", "ButtonWithModifier").
    /// Only for composite bindings.
    /// </summary>
    string? CompositeName { get; }

    /// <summary>
    /// Part bindings for composites (e.g., up/down/left/right for 2DVector).
    /// </summary>
    IReadOnlyList<IInputBinding>? CompositeBindings { get; }

    /// <summary>
    /// Processors applied to input values (e.g., normalize, invert, scale).
    /// </summary>
    IReadOnlyList<IInputProcessor>? Processors { get; }

    /// <summary>
    /// Interaction required (e.g., hold, tap, multi-tap).
    /// </summary>
    IInputInteraction? Interaction { get; }
}

public enum InputBindingType
{
    /// <summary>
    /// Simple binding: single input control.
    /// </summary>
    Simple,

    /// <summary>
    /// Composite binding: multiple inputs combined.
    /// </summary>
    Composite
}
```

#### 5. Composite Bindings

Combine multiple inputs into one value:

```csharp
/// <summary>
/// Composite binding types supported.
/// </summary>
public enum CompositeType
{
    /// <summary>
    /// 2D vector from 4 buttons (up/down/left/right or WASD).
    /// Outputs: Vector2
    /// </summary>
    Vector2D,

    /// <summary>
    /// 1D axis from 2 buttons (negative/positive or A/D).
    /// Outputs: float
    /// </summary>
    Axis1D,

    /// <summary>
    /// Button with modifier (e.g., Shift+Click).
    /// Outputs: bool
    /// </summary>
    ButtonWithModifier,

    /// <summary>
    /// Two buttons pressed simultaneously.
    /// Outputs: bool
    /// </summary>
    TwoButtons
}
```

**Example: WASD Composite**

```json
{
  "name": "Move",
  "type": "Value",
  "bindings": [
    {
      "composite": "2DVector",
      "parts": [
        { "name": "up", "path": "Keyboard/W" },
        { "name": "down", "path": "Keyboard/S" },
        { "name": "left", "path": "Keyboard/A" },
        { "name": "right", "path": "Keyboard/D" }
      ]
    }
  ]
}
```

Results in: `Move.ReadValue<Vector2>()` returns `(0, 1)` when W pressed, `(-1, 0)` when A pressed, etc.

#### 6. Processors

Transform input values before delivery:

```csharp
/// <summary>
/// Processes input values (normalize, invert, scale, clamp, etc.).
/// </summary>
public interface IInputProcessor
{
    /// <summary>
    /// Process the input value.
    /// </summary>
    object Process(object value);
}

// Built-in processors
public class NormalizeProcessor : IInputProcessor { ... }
public class InvertProcessor : IInputProcessor { ... }
public class ScaleProcessor : IInputProcessor { ... }
public class ClampProcessor : IInputProcessor { ... }
```

**Example**: Normalize WASD vector

```json
{
  "name": "Move",
  "bindings": [
    {
      "composite": "2DVector",
      "processors": ["normalize"]
    }
  ]
}
```

Results in: Diagonal movement (W+D) = `(0.707, 0.707)` instead of `(1, 1)`

#### 7. Interactions

Detect input patterns:

```csharp
/// <summary>
/// Interaction patterns (tap, hold, double-tap, etc.).
/// </summary>
public interface IInputInteraction
{
    /// <summary>
    /// Interaction name (e.g., "tap", "hold", "multiTap").
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Evaluate input and determine action phase.
    /// </summary>
    InputActionPhase Evaluate(object value, float deltaTime);
}

// Built-in interactions
public class TapInteraction : IInputInteraction { ... }      // Quick press+release
public class HoldInteraction : IInputInteraction { ... }     // Press and hold 0.4s
public class MultiTapInteraction : IInputInteraction { ... } // Double-tap, triple-tap
public class SlowTapInteraction : IInputInteraction { ... }  // Hold for confirmation
```

**Example**: Hold Space to charge attack

```json
{
  "name": "ChargeAttack",
  "type": "Button",
  "bindings": [
    {
      "path": "Keyboard/Space",
      "interaction": "hold",
      "interactionParams": { "duration": 0.5 }
    }
  ]
}
```

Usage:
```csharp
chargeAttack.started += (evt) => { StartChargingVisual(); };
chargeAttack.performed += (evt) => { ReleaseChargedAttack(); };
chargeAttack.canceled += (evt) => { CancelCharge(); };
```

### JSON Configuration Format

Action maps defined in `.inputactions` JSON files:

```json
{
  "name": "ConsoleDungeonControls",
  "actionMaps": [
    {
      "name": "Gameplay",
      "actions": [
        {
          "name": "Move",
          "type": "Value",
          "expectedControlType": "Vector2",
          "bindings": [
            {
              "composite": "2DVector",
              "processors": ["normalize"],
              "parts": [
                { "name": "up", "path": "Keyboard/W" },
                { "name": "down", "path": "Keyboard/S" },
                { "name": "left", "path": "Keyboard/A" },
                { "name": "right", "path": "Keyboard/D" }
              ]
            },
            {
              "composite": "2DVector",
              "processors": ["normalize"],
              "parts": [
                { "name": "up", "path": "Keyboard/UpArrow" },
                { "name": "down", "path": "Keyboard/DownArrow" },
                { "name": "left", "path": "Keyboard/LeftArrow" },
                { "name": "right", "path": "Keyboard/RightArrow" }
              ]
            },
            {
              "path": "Gamepad/LeftStick",
              "processors": ["stickDeadzone"]
            }
          ]
        },
        {
          "name": "Attack",
          "type": "Button",
          "bindings": [
            { "path": "Keyboard/Space" },
            { "path": "Keyboard/LeftCtrl" },
            { "path": "Gamepad/ButtonSouth" }
          ]
        },
        {
          "name": "ChargeAttack",
          "type": "Button",
          "bindings": [
            {
              "path": "Keyboard/Space",
              "interaction": "hold",
              "interactionParams": { "duration": 0.5 }
            }
          ]
        },
        {
          "name": "Use",
          "type": "Button",
          "bindings": [
            { "path": "Keyboard/E" },
            { "path": "Gamepad/ButtonWest" }
          ]
        },
        {
          "name": "ToggleMenu",
          "type": "Button",
          "bindings": [
            { "path": "Keyboard/M" },
            { "path": "Keyboard/Escape" },
            { "path": "Gamepad/Start" }
          ]
        }
      ]
    },
    {
      "name": "Menu",
      "actions": [
        {
          "name": "Navigate",
          "type": "Value",
          "expectedControlType": "Vector2",
          "bindings": [
            {
              "composite": "2DVector",
              "parts": [
                { "name": "up", "path": "Keyboard/UpArrow" },
                { "name": "down", "path": "Keyboard/DownArrow" },
                { "name": "left", "path": "Keyboard/LeftArrow" },
                { "name": "right", "path": "Keyboard/RightArrow" }
              ]
            },
            { "path": "Gamepad/DPad" }
          ]
        },
        {
          "name": "Select",
          "type": "Button",
          "bindings": [
            { "path": "Keyboard/Enter" },
            { "path": "Keyboard/Space" },
            { "path": "Gamepad/ButtonSouth" }
          ]
        },
        {
          "name": "Back",
          "type": "Button",
          "bindings": [
            { "path": "Keyboard/Escape" },
            { "path": "Gamepad/ButtonEast" }
          ]
        }
      ]
    }
  ]
}
```

### Integration with RFC-0021

**RFC-0021 remains the foundation**, Input Action System builds on top:

```
┌─ RFC-0021: Low-level input ────────────────────────────────┐
│ Terminal.Gui KeyDown → RawKeyEvent → IInputMapper          │
│ → GameInputEvent → IInputRouter → IInputScope              │
└─────────────────────────────────────────────────────────────┘
                            ↓
┌─ RFC-0026: High-level actions ─────────────────────────────┐
│ GameInputEvent → InputActionMapEvaluator → IInputAction    │
│ → InputActionEvent (started/performed/canceled)            │
└─────────────────────────────────────────────────────────────┘
```

**Compatibility layer**:

```csharp
/// <summary>
/// Bridges RFC-0021 GameInputEvent to RFC-0026 InputAction.
/// Evaluates action bindings, processors, and interactions.
/// </summary>
public class InputActionMapEvaluator
{
    private readonly IInputActionMap _actionMap;
    private readonly Dictionary<string, ActionState> _states;

    public void ProcessGameInputEvent(GameInputEvent inputEvent)
    {
        // Find actions bound to this input
        foreach (var action in _actionMap.Actions)
        {
            foreach (var binding in action.Bindings)
            {
                if (MatchesBinding(inputEvent, binding))
                {
                    // Apply processors
                    var value = ApplyProcessors(inputEvent, binding.Processors);

                    // Evaluate interaction (tap, hold, etc.)
                    var phase = EvaluateInteraction(action, value, binding.Interaction);

                    // Update action state and raise events
                    UpdateActionState(action, phase, value);
                }
            }
        }
    }
}
```

## Implementation Plan

### Phase 1: Core Contracts (Week 1)

1. Create `WingedBean.Contracts.InputSystem` project
2. Define interfaces:
   - `IInputAction`
   - `IInputActionMap`
   - `IInputBinding`
3. Define types:
   - `InputActionEvent`
   - `InputActionPhase`
   - `InputActionType`
4. Add to `Framework.sln`

### Phase 2: Action Map Loader (Week 2)

1. Create `WingedBean.Providers.InputSystem` project
2. Implement `JsonActionMapLoader`:
   - Parse `.inputactions` JSON files
   - Build `IInputActionMap` instances
3. Implement `InputAction` and `InputActionMap` classes
4. Add unit tests for JSON parsing

### Phase 3: Composite Bindings (Week 3)

1. Implement composite evaluators:
   - `Vector2DComposite` (WASD → Vector2)
   - `Axis1DComposite` (A/D → float)
   - `ButtonWithModifierComposite` (Shift+Click → bool)
2. Integrate with `InputActionMapEvaluator`
3. Add tests for composite evaluation

### Phase 4: Processors (Week 4)

1. Implement built-in processors:
   - `NormalizeProcessor`
   - `InvertProcessor`
   - `ScaleProcessor`
   - `ClampProcessor`
   - `DeadzoneProcessor` (for gamepad sticks)
2. Add processor pipeline to binding evaluation
3. Add tests for processor chains

### Phase 5: Interactions (Week 5)

1. Implement built-in interactions:
   - `TapInteraction` (quick press+release)
   - `HoldInteraction` (press 0.4s+)
   - `MultiTapInteraction` (double-tap, triple-tap)
   - `SlowTapInteraction` (hold for confirmation)
2. Add state machines for interaction tracking
3. Add tests for interaction patterns

### Phase 6: Integration with ConsoleDungeon (Week 6)

1. Create `ConsoleDungeonControls.inputactions` JSON
2. Load action maps in `ConsoleDungeonApp`
3. Replace key mapping logic with action subscriptions:
   ```csharp
   gameplay["Move"].performed += (evt) => {
       var movement = evt.Value as Vector2?;
       // Handle movement
   };
   ```
4. Switch action maps on context change:
   ```csharp
   void ShowMenu() {
       gameplayMap.Disable();
       menuMap.Enable();
   }
   ```

### Phase 7: Rebinding UI (Week 7-8)

1. Implement `IInputActionRebinder`:
   - `StartRebind(string actionName)`
   - `CancelRebind()`
   - `CompleteRebind(RawKeyEvent newBinding)`
2. Create rebinding UI components (Terminal.Gui dialogs)
3. Persist custom bindings to user settings
4. Add tests for rebinding flow

## Usage Examples

### Example 1: Basic Action Subscription

```csharp
// Load action map from JSON
var loader = new JsonActionMapLoader();
var actionAsset = loader.Load("ConsoleDungeonControls.inputactions");
var gameplay = actionAsset.GetActionMap("Gameplay");

// Subscribe to actions
gameplay["Move"].performed += (evt) => {
    var movement = evt.ReadValue<Vector2>();
    player.Move(movement);
};

gameplay["Attack"].performed += (evt) => {
    player.Attack();
};

gameplay["ToggleMenu"].performed += (evt) => {
    ToggleMenu();
};

// Enable gameplay inputs
gameplay.Enable();
```

### Example 2: Polling-Style Input

```csharp
// In game update loop
public void Update(float deltaTime)
{
    // Poll current movement value
    var movement = gameplay["Move"].ReadValue<Vector2>();
    if (movement != Vector2.Zero)
    {
        player.Move(movement * deltaTime);
    }

    // Check button state
    if (gameplay["Attack"].IsPressed)
    {
        player.Charge(deltaTime);
    }
}
```

### Example 3: Context Switching

```csharp
public class ConsoleDungeonApp
{
    private IInputActionMap _gameplayMap;
    private IInputActionMap _menuMap;

    public void ShowMenu()
    {
        _gameplayMap.Disable();
        _menuMap.Enable();
    }

    public void HideMenu()
    {
        _menuMap.Disable();
        _gameplayMap.Enable();
    }
}
```

### Example 4: Charge Attack with Hold Interaction

```csharp
var chargeAttack = gameplay["ChargeAttack"];

chargeAttack.started += (evt) => {
    // User pressed Space, start charging visual
    player.StartChargingEffect();
};

chargeAttack.performed += (evt) => {
    // User held for 0.5s, release charged attack
    player.ReleaseChargedAttack();
};

chargeAttack.canceled += (evt) => {
    // User released before threshold, cancel charge
    player.CancelCharge();
};
```

### Example 5: Rebinding Controls

```csharp
public class ControlsSettingsUI
{
    private IInputActionRebinder _rebinder;

    public void OnRebindAttackButton()
    {
        // Start listening for new input
        _rebinder.StartRebind("Attack");

        // Show UI prompt: "Press any key to bind to Attack..."
        ShowRebindPrompt();

        // User presses a key, captured by IInputMapper
        // _rebinder.CompleteRebind(newKeyEvent) called

        // Save to user settings
        SaveBindings();
    }
}
```

## Benefits

### For Developers

1. **Less boilerplate**: No manual key mapping code
2. **Configuration-driven**: Change bindings without code changes
3. **Testable**: Mock `IInputAction` instead of keyboard events
4. **Multi-platform**: Same action maps work on Console, Unity, Godot
5. **Composable**: Combine processors, interactions, composites

### For Players

1. **Customizable controls**: Rebind keys in settings
2. **Multi-device support**: Keyboard, gamepad, touch (future)
3. **Consistent UX**: Same patterns across all games in framework
4. **Accessibility**: Remap for one-handed play, different layouts

### For the Project

1. **Unity parity**: Familiar patterns for Unity developers
2. **Framework-agnostic**: Input system works across all profiles
3. **Future-proof**: Easy to add VR controllers, motion sensors, etc.
4. **Ecosystem**: Third-party plugins can define their own action maps

## Risks & Mitigations

### Risk: Complexity Creep

**Mitigation**:
- Implement incrementally (phases 1-5 are independent)
- Keep RFC-0021 as fallback for simple cases
- Provide both event-driven and polling APIs

### Risk: Performance (JSON Parsing, Reflection)

**Mitigation**:
- Cache parsed action maps (load once at startup)
- Use source generators for binding evaluation (future)
- Benchmark composite evaluation (target: <1ms per frame)

### Risk: Breaking Changes to RFC-0021

**Mitigation**:
- Input Action System is **additive**, not replacement
- RFC-0021 `IInputMapper` remains unchanged
- Bridge layer handles conversion (no breaking changes)

### Risk: JSON Schema Complexity

**Mitigation**:
- Provide JSON schema (.schema.json) for validation
- Add Visual Studio Code extension for autocomplete
- Include example `.inputactions` files in docs

## Future Enhancements

### Phase 8+: Advanced Features

1. **Input Hints**: Show context-sensitive button prompts (e.g., "Press E to use")
2. **Haptic Feedback**: Vibration patterns for gamepad rumble
3. **Input Recording**: Record/replay input sequences for testing
4. **Network Prediction**: Rollback/predict input for multiplayer
5. **AI Input**: Bots can use same action API as players

### Integration with Other Profiles

1. **Unity Profile**: Use Unity's native Input System, bridge to our contracts
2. **Godot Profile**: Use Godot's InputMap, bridge to our contracts
3. **Web Profile**: Translate JavaScript events to our contracts

## Success Criteria

- [ ] `WingedBean.Contracts.InputSystem` project created and builds
- [ ] JSON action map loader parses `.inputactions` files
- [ ] Composite bindings: WASD → Vector2 works
- [ ] Processors: Normalize, invert, scale work
- [ ] Interactions: Tap, hold, multi-tap work
- [ ] ConsoleDungeon uses action maps instead of manual key mapping
- [ ] Rebinding UI allows customizing controls
- [ ] All RFC-0021 tests still pass (no breaking changes)
- [ ] Performance: <1ms per frame for action evaluation

## Related Work

- **RFC-0021**: Input Mapping and Scoped Routing (foundation)
- **RFC-0020**: Scene Service and Terminal UI Separation (context switching)
- **RFC-0018**: Render and UI Services (game loop integration)
- **Unity Input System**: https://docs.unity3d.com/Packages/com.unity.inputsystem@1.7/
- **Godot InputMap**: https://docs.godotengine.org/en/stable/tutorials/inputs/inputeventaction.html

## Appendix A: Project Structure

```
development/dotnet/
├── framework/src/
│   └── WingedBean.Contracts.InputSystem/           [NEW]
│       ├── IInputAction.cs
│       ├── IInputActionMap.cs
│       ├── IInputBinding.cs
│       ├── IInputProcessor.cs
│       ├── IInputInteraction.cs
│       ├── InputActionEvent.cs
│       ├── InputActionPhase.cs
│       └── InputActionType.cs
│
├── console/
│   ├── src/
│   │   ├── providers/
│   │   │   └── WingedBean.Providers.InputSystem/   [NEW]
│   │   │       ├── JsonActionMapLoader.cs
│   │   │       ├── InputAction.cs
│   │   │       ├── InputActionMap.cs
│   │   │       ├── InputActionMapEvaluator.cs
│   │   │       ├── Composites/
│   │   │       │   ├── Vector2DComposite.cs
│   │   │       │   ├── Axis1DComposite.cs
│   │   │       │   └── ButtonWithModifierComposite.cs
│   │   │       ├── Processors/
│   │   │       │   ├── NormalizeProcessor.cs
│   │   │       │   ├── InvertProcessor.cs
│   │   │       │   ├── ScaleProcessor.cs
│   │   │       │   └── DeadzoneProcessor.cs
│   │   │       └── Interactions/
│   │   │           ├── TapInteraction.cs
│   │   │           ├── HoldInteraction.cs
│   │   │           └── MultiTapInteraction.cs
│   │   │
│   │   └── plugins/
│   │       └── WingedBean.Plugins.ConsoleDungeon/
│   │           └── ConsoleDungeonControls.inputactions  [NEW]
│   │
│   └── tests/
│       └── providers/
│           └── WingedBean.Providers.InputSystem.Tests/  [NEW]
│               ├── JsonActionMapLoaderTests.cs
│               ├── CompositeTests.cs
│               ├── ProcessorTests.cs
│               └── InteractionTests.cs
```

## Appendix B: Comparison with Unity Input System

| Feature | Unity Input System | RFC-0026 (This RFC) | Status |
|---------|-------------------|---------------------|--------|
| **Action Maps** | ✅ | ✅ | Planned |
| **Composite Bindings** | ✅ (2D Vector, 1D Axis, etc.) | ✅ (Same types) | Planned |
| **Processors** | ✅ (Normalize, Invert, etc.) | ✅ (Same types) | Planned |
| **Interactions** | ✅ (Tap, Hold, MultiTap) | ✅ (Same types) | Planned |
| **JSON Config** | ✅ (.inputactions) | ✅ (.inputactions) | Planned |
| **Rebinding UI** | ✅ (RebindOperation) | ✅ (IInputActionRebinder) | Planned |
| **Control Schemes** | ✅ (Keyboard, Gamepad, etc.) | ❌ (Future) | Not in scope |
| **Device Layouts** | ✅ (Custom devices) | ❌ (Future) | Not in scope |
| **Editor Integration** | ✅ (Visual editor) | ❌ (Text-only) | Not in scope |

## Appendix C: Migration Path from RFC-0021

**Step 1**: Add Input Action System alongside RFC-0021 (no breaking changes)

```csharp
// Old way (RFC-0021) - still works
_inputSubscription = _uiService.InputObservable.Subscribe(inputEvent => {
    HandleGameInput(inputEvent);
});

// New way (RFC-0026) - opt-in
var gameplay = actionAsset.GetActionMap("Gameplay");
gameplay["Move"].performed += (evt) => {
    var movement = evt.ReadValue<Vector2>();
    player.Move(movement);
};
```

**Step 2**: Gradually migrate actions to action maps

```csharp
// Migrate one action at a time
gameplay["Attack"].performed += (evt) => player.Attack();
// Other inputs still use old system
```

**Step 3**: Once all actions migrated, remove old code

```csharp
// Remove _inputSubscription, HandleGameInput(), etc.
```

## Conclusion

RFC-0026 introduces a Unity-inspired Input Action System that significantly enhances RFC-0021's input mapping with action maps, composite bindings, processors, interactions, and rebindable controls. This provides a higher-level, more flexible abstraction while maintaining full compatibility with the existing low-level input foundation.

The phased implementation approach allows gradual adoption without breaking changes, and the JSON-based configuration enables data-driven input design that scales across all WingedBean profiles (Console, Unity, Godot, Web).

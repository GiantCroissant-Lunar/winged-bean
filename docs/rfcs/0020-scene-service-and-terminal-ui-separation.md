---
id: RFC-0020
title: Scene Service and Terminal UI Separation
status: Implemented
category: framework, architecture, console
created: 2025-10-04
updated: 2025-01-09
implemented: 2025-01-09
author: Claude Code & ApprenticeGC
related: RFC-0018, RFC-0002, RFC-0021
implementation: docs/implementation/rfc-0020-0021-implementation-summary.md
commits: a331a35, 0b393a3
---

# RFC-0020: Scene Service and Terminal UI Separation

## Summary

Introduce `ISceneService` to complete the 4-tier architecture separation for the Console profile by extracting all Terminal.Gui-specific code from `ConsoleDungeonApp` into a dedicated Tier 4 provider. This RFC fixes the architectural violation where game logic (Tier 3) directly depends on UI framework APIs (Terminal.Gui).

## Motivation

### Current Architecture Violation

**Problem**: `WingedBean.Plugins.ConsoleDungeon/ConsoleDungeonApp.cs` (800+ lines) directly uses Terminal.Gui APIs, violating RFC-0002's 4-tier architecture:

```csharp
// ❌ ConsoleDungeonApp.cs (Tier 3 plugin) - SHOULD NOT know about Terminal.Gui
using Terminal.Gui;

public class ConsoleDungeonApp : ITerminalApp
{
    private Window? _mainWindow;
    private Label? _gameWorldView;

    public async Task StartAsync(...)
    {
        Application.Init();                    // ❌ Direct Terminal.Gui API
        _mainWindow = new Window() { ... };    // ❌ Direct Terminal.Gui API
        _gameWorldView = new Label() { ... };  // ❌ Direct Terminal.Gui API

        Application.Invoke(() => {             // ❌ Direct UI thread marshaling
            _gameWorldView.Text = buffer.ToText();
        });

        Application.Run(_mainWindow);          // ❌ Direct Terminal.Gui API
    }
}
```

**Why this is wrong**:

1. **Tier violation**: Tier 3 (plugin/game logic) depends on Tier 4 implementation details (Terminal.Gui)
2. **Not testable**: Cannot test `ConsoleDungeonApp` without Terminal.Gui
3. **Not portable**: Cannot reuse game logic with different UI frameworks
4. **Tight coupling**: Viewport management, threading, and UI lifecycle mixed with game coordination

### RFC-0018 Partial Implementation

RFC-0018 introduced `IRenderService` and `IGameUIService`, but:

- `IRenderService` ✅ correctly abstracts rendering (implemented by `RenderServiceProvider`)
- `IGameUIService` ⚠️ only handles menu dialogs, NOT main window/viewport
- `ISceneService` ❌ does NOT exist - no abstraction for Terminal.Gui lifecycle

**Current state**:

```
ConsoleDungeonApp (Tier 3)
├─ ✅ Uses IRenderService.Render() - CORRECT
├─ ✅ Uses IGameUIService.ShowMenu() - CORRECT
└─ ❌ DIRECTLY uses Terminal.Gui for window/viewport - WRONG
```

### Existing `WingedBean.Plugins.TerminalUI` Project

A `WingedBean.Plugins.TerminalUI` project exists with:
- `ITerminalUIService` contract (Tier 1)
- `TerminalGuiService` provider (Tier 4)
- Created Sept 30, 2025 as proof-of-concept

**Problem**: `ITerminalUIService` is too limited:

```csharp
// ❌ Current ITerminalUIService - insufficient for game needs
public interface ITerminalUIService
{
    void Initialize();
    void Run();
    string GetScreenContent();  // ❌ No way to UPDATE content
}
```

**Missing capabilities**:
- No viewport management
- No way to update game world view
- No integration with `IRenderService`
- Hardcoded demo UI, not game-ready

## Proposal

### Architecture: Introduce `ISceneService`

Create `ISceneService` (Tier 1 contract) to own ALL Terminal.Gui concerns:

```
┌─ Tier 1: Contracts ────────────────────────────────────────┐
│ WingedBean.Contracts.Scene/                                │
│ ├── ISceneService.cs          (Scene/UI lifecycle)         │
│ └── Viewport.cs               (Viewport dimensions)        │
│                                                             │
│ WingedBean.Contracts.Game/ (RFC-0018)                      │
│ ├── IRenderService.cs         (Entity → RenderBuffer)      │
│ └── IGameUIService.cs         (Menu dialogs)               │
└─────────────────────────────────────────────────────────────┘
                            ↓
┌─ Tier 3: Plugins ──────────────────────────────────────────┐
│ WingedBean.Plugins.ConsoleDungeon/                         │
│ └── ConsoleDungeonApp.cs      (Coordinates services)       │
│     - NO Terminal.Gui dependencies                         │
│     - Calls scene.UpdateWorld(snapshots)                   │
└─────────────────────────────────────────────────────────────┘
                            ↓
┌─ Tier 4: Providers ────────────────────────────────────────┐
│ WingedBean.Providers.TerminalGuiScene/                     │
│ └── TerminalGuiSceneProvider.cs (Terminal.Gui owner)       │
│     - Implements ISceneService                             │
│     - Manages Window, Labels, viewport                     │
│     - Handles UI thread marshaling                         │
│     - Integrates IRenderService                            │
└─────────────────────────────────────────────────────────────┘
```

### 1. ISceneService Contract (Tier 1)

**Location**: `development/dotnet/framework/src/WingedBean.Contracts.Scene/ISceneService.cs`

```csharp
namespace WingedBean.Contracts.Scene;

/// <summary>
/// Scene service manages UI lifecycle, viewport, and game world rendering.
/// Platform-agnostic: implementations can use Terminal.Gui, Unity, Godot, ImGui, etc.
/// </summary>
public interface ISceneService
{
    /// <summary>
    /// Initialize the scene (create window, setup UI).
    /// Must be called before Run() or UpdateWorld().
    /// </summary>
    void Initialize();

    /// <summary>
    /// Get current viewport dimensions.
    /// Used by game logic to determine camera bounds.
    /// </summary>
    Viewport GetViewport();

    /// <summary>
    /// Update the game world view with latest entity snapshots.
    /// Thread-safe: can be called from any thread.
    /// Implementation handles debouncing, rendering, and UI marshaling.
    /// </summary>
    /// <param name="snapshots">Current entity positions/appearances</param>
    void UpdateWorld(IReadOnlyList<EntitySnapshot> snapshots);

    /// <summary>
    /// Run the scene main loop (blocks until UI closes).
    /// Must be called on the main thread.
    /// </summary>
    void Run();

    /// <summary>
    /// Shutdown event - raised when user closes the UI.
    /// </summary>
    event EventHandler<SceneShutdownEventArgs>? Shutdown;
}

/// <summary>
/// Viewport dimensions (width, height in characters/pixels).
/// </summary>
public record Viewport(int Width, int Height);

/// <summary>
/// Event args for scene shutdown.
/// </summary>
public class SceneShutdownEventArgs : EventArgs
{
    public ShutdownReason Reason { get; init; }
}

public enum ShutdownReason
{
    UserRequest,    // User closed window or pressed quit
    Error,          // Unhandled error
    Timeout         // Idle timeout (optional)
}
```

### Input Handling (see RFC-0021)

This RFC intentionally keeps input details minimal and defers specifics to RFC-0021: Input Mapping and Scoped Routing. In short:

- Terminal.Gui-specific key capture, ESC/CSI handling, and focus management live in the scene provider.
- Providers must use a scoped input router so modal dialogs capture input without leaks to gameplay.
- ConsoleDungeonApp no longer handles keys directly; it consumes mapped `GameInputEvent`s (via router or `IGameUIService.InputObservable`).

Terminal.Gui v2 correctness to preserve:
- Use a top-level focusable view to receive keys; set focus after init and when dialogs close.
- Map `KeyCode.Cursor*` first, then fall back to rune-based WASD/SS3.
- Use a short ESC disambiguation timer so standalone ESC maps to Quit while ESC-[ A–D maps to arrows.
- Mark handled events to prevent default navigation.

### 2. TerminalGuiSceneProvider (Tier 4)

**Location**: `development/dotnet/console/src/providers/WingedBean.Providers.TerminalGuiScene/TerminalGuiSceneProvider.cs`

```csharp
using Terminal.Gui;
using WingedBean.Contracts.Scene;
using WingedBean.Contracts.Game;

namespace WingedBean.Providers.TerminalGuiScene;

/// <summary>
/// Terminal.Gui implementation of ISceneService.
/// Owns ALL Terminal.Gui lifecycle, window management, and UI thread marshaling.
/// </summary>
[Plugin(
    Name = "TerminalGuiSceneProvider",
    Provides = new[] { typeof(ISceneService) },
    Priority = 100
)]
public class TerminalGuiSceneProvider : ISceneService
{
    private readonly IRenderService _renderService;
    private Window? _mainWindow;
    private Label? _gameWorldView;
    private bool _initialized = false;

    // Debouncing: coalesce rapid updates
    private IReadOnlyList<EntitySnapshot>? _pendingSnapshots;
    private readonly object _lock = new();

    public TerminalGuiSceneProvider(IRenderService renderService)
    {
        _renderService = renderService;
    }

    public void Initialize()
    {
        if (_initialized) return;

        Application.Init();

        _mainWindow = new Window
        {
            Title = "Console Dungeon",
            BorderStyle = LineStyle.Single
        };

        _gameWorldView = new Label
        {
            X = 0,
            Y = 0,
            Width = Dim.Fill(),
            Height = Dim.Fill(),
            Text = "Loading..."
        };

        _mainWindow.Add(_gameWorldView);
        _initialized = true;
    }

    public Viewport GetViewport()
    {
        if (_gameWorldView == null)
            return new Viewport(80, 24); // Default fallback

        // Terminal.Gui uses Dim, resolve to actual size
        int width = _gameWorldView.Bounds.Width;
        int height = _gameWorldView.Bounds.Height;
        return new Viewport(width, height);
    }

    public void UpdateWorld(IReadOnlyList<EntitySnapshot> snapshots)
    {
        lock (_lock)
        {
            _pendingSnapshots = snapshots;
        }

        // Marshal to UI thread
        Application.Invoke(() =>
        {
            IReadOnlyList<EntitySnapshot>? toRender;
            lock (_lock)
            {
                toRender = _pendingSnapshots;
                _pendingSnapshots = null;
            }

            if (toRender == null || _gameWorldView == null) return;

            // Get current viewport and render
            var viewport = GetViewport();
            var buffer = _renderService.Render(toRender, viewport.Width, viewport.Height);
            _gameWorldView.Text = buffer.ToText();
        });
    }

    public void Run()
    {
        if (!_initialized || _mainWindow == null)
            throw new InvalidOperationException("Scene not initialized. Call Initialize() first.");

        try
        {
            Application.Run(_mainWindow);
        }
        finally
        {
            Shutdown?.Invoke(this, new SceneShutdownEventArgs
            {
                Reason = ShutdownReason.UserRequest
            });

            Application.Shutdown();
        }
    }

    public event EventHandler<SceneShutdownEventArgs>? Shutdown;
}
```

### 3. Refactored ConsoleDungeonApp (Tier 3)

**Location**: `development/dotnet/console/src/plugins/WingedBean.Plugins.ConsoleDungeon/ConsoleDungeonApp.cs`

```csharp
// ✅ NO Terminal.Gui usings - clean separation!
using WingedBean.Contracts.Scene;
using WingedBean.Contracts.Game;

namespace WingedBean.Plugins.ConsoleDungeon;

/// <summary>
/// Console Dungeon application coordinator.
/// Tier 3 plugin: coordinates services, NO direct UI framework dependencies.
/// </summary>
[Plugin(
    Name = "ConsoleDungeonApp",
    Provides = new[] { typeof(ITerminalApp) },
    Priority = 50
)]
public class ConsoleDungeonApp : ITerminalApp
{
    private readonly ISceneService _sceneService;
    private readonly IDungeonGameService _gameService;
    private IDisposable? _entitiesSubscription;

    public ConsoleDungeonApp(
        ISceneService sceneService,
        IDungeonGameService gameService)
    {
        _sceneService = sceneService;
        _gameService = gameService;
    }

    public async Task StartAsync(TerminalAppConfig config, CancellationToken ct = default)
    {
        // Initialize scene
        _sceneService.Initialize();

        // Subscribe to entity updates - scene handles rendering
        _entitiesSubscription = _gameService.EntitiesObservable.Subscribe(snapshots =>
        {
            _sceneService.UpdateWorld(snapshots);
        });

        // Handle shutdown
        _sceneService.Shutdown += (s, e) =>
        {
            _gameService.Shutdown();
        };

        // Start game logic
        await _gameService.InitializeAsync(ct);

        // Run scene (blocks until UI closes)
        _sceneService.Run();

        await Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken ct = default)
    {
        _entitiesSubscription?.Dispose();
        return Task.CompletedTask;
    }
}
```

**Key improvements**:

- ✅ **Zero Terminal.Gui dependencies** - 100% framework-agnostic
- ✅ **Simple coordination** - just wires services together
- ✅ **Testable** - can inject mock `ISceneService`
- ✅ **~150 lines** instead of 800+ (complexity moved to provider)

## Project Structure Changes

### NEW Projects to Create

1. **WingedBean.Contracts.Scene** (Tier 1)
   - Location: `development/dotnet/framework/src/WingedBean.Contracts.Scene/`
   - Contains: `ISceneService.cs`, `Viewport.cs`, `SceneShutdownEventArgs.cs`
   - Target: `netstandard2.1`

2. **WingedBean.Providers.TerminalGuiScene** (Tier 4)
   - Location: `development/dotnet/console/src/providers/WingedBean.Providers.TerminalGuiScene/`
   - Contains: `TerminalGuiSceneProvider.cs`
   - Dependencies: `WingedBean.Contracts.Scene`, `WingedBean.Contracts.Game`, `Terminal.Gui`
   - Target: `net8.0`

3. **WingedBean.Providers.TerminalGuiScene.Tests**
   - Location: `development/dotnet/console/tests/providers/WingedBean.Providers.TerminalGuiScene.Tests/`
   - Tests scene provider with mock `IRenderService`

### Projects to RENAME/REFACTOR

1. **WingedBean.Plugins.TerminalUI** → **DEPRECATE or REPURPOSE**
   - Current: Generic Terminal.Gui demo (proof-of-concept)
   - Option A: Delete (superseded by `WingedBean.Providers.TerminalGuiScene`)
   - Option B: Rename to `WingedBean.Plugins.TerminalUiDemo` (keep as example)
   - **Recommendation**: Delete - no longer needed

2. **WingedBean.Plugins.ConsoleDungeon** → Keep name, REFACTOR content
   - Remove all Terminal.Gui code
   - Reduce from 800+ lines to ~150 lines
   - Add dependency on `WingedBean.Contracts.Scene`

3. **WingedBean.Plugins.DungeonGame** → Keep as-is (future: split into Core/Systems/Components)
   - Already correct tier 3 plugin
   - `RenderServiceProvider` and `GameUIServiceProvider` stay here (tier 4)

### Projects to UPDATE (Dependencies)

**ConsoleDungeon.Host** (`development/dotnet/console/src/host/ConsoleDungeon.Host/ConsoleDungeon.Host.csproj`):
- No changes needed (uses dynamic plugin loading)

**Console.sln** (`development/dotnet/console/Console.sln`):
- Add `WingedBean.Providers.TerminalGuiScene` project
- Add `WingedBean.Providers.TerminalGuiScene.Tests` project
- Remove `WingedBean.Plugins.TerminalUI` (if deleted)

**Framework.sln** (`development/dotnet/framework/Framework.sln`):
- Add `WingedBean.Contracts.Scene` project

## Implementation Plan

### Phase 1: Create Contracts (Tier 1)

1. Create `WingedBean.Contracts.Scene` project
2. Define `ISceneService`, `Viewport`, `SceneShutdownEventArgs`
3. Add to `Framework.sln`
4. Run `dotnet build` to verify

### Phase 2: Create Provider (Tier 4)

1. Create `WingedBean.Providers.TerminalGuiScene` project
2. Implement `TerminalGuiSceneProvider`
3. Extract Terminal.Gui code from `ConsoleDungeonApp`
4. Add `.plugin.json` for dynamic loading
5. Create tests with mock `IRenderService`

### Phase 3: Refactor ConsoleDungeonApp (Tier 3)

1. Remove all `using Terminal.Gui` statements
2. Inject `ISceneService` via constructor
3. Replace direct Terminal.Gui calls with `ISceneService` methods
4. Remove `_mainWindow`, `_gameWorldView`, `Application.Invoke()` code
5. Reduce from 800+ to ~150 lines

### Phase 4: Update Plugin Loading

1. Update `plugins.json` to include `TerminalGuiSceneProvider`
2. Ensure `ISceneService` is registered before `ConsoleDungeonApp` loads
3. Test dynamic loading with `ConsoleDungeon.Host`

### Phase 5: Cleanup

1. Delete `WingedBean.Plugins.TerminalUI` (or rename to demo)
2. Delete `WingedBean.Contracts.TerminalUI/ITerminalUIService.cs` (superseded)
3. Update documentation
4. Run full test suite

## Testing Strategy

### Unit Tests

1. **ISceneService mock tests** (in `ConsoleDungeon.Host.Tests`):
   - Test `ConsoleDungeonApp` with mock scene service
   - Verify `UpdateWorld()` called on entity updates
   - Verify `Run()` blocks until shutdown

2. **TerminalGuiSceneProvider tests** (in `WingedBean.Providers.TerminalGuiScene.Tests`):
   - Test viewport calculation
   - Test debouncing/coalescing
   - Test UI thread marshaling
   - Mock `IRenderService` to isolate behavior

### Integration Tests

1. **E2E tests** (in `WingedBean.Tests.E2E.ConsoleDungeon`):
   - Existing Playwright tests continue to work
   - Verify arrow keys, input handling
   - Verify rendering updates

## Benefits

### Architectural

- ✅ **4-tier compliance**: Tier 3 no longer depends on Tier 4 implementation
- ✅ **Single Responsibility**: Each service does ONE thing well
- ✅ **Testability**: All components mockable and testable in isolation
- ✅ **Portability**: Game logic can run with different UI frameworks

### Code Quality

- ✅ **Reduced complexity**: `ConsoleDungeonApp` from 800+ to ~150 lines
- ✅ **Clear contracts**: `ISceneService` API is self-documenting
- ✅ **Thread safety**: UI marshaling centralized in provider
- ✅ **Maintainability**: Terminal.Gui updates only affect one project

### Developer Experience

- ✅ **Easier testing**: Mock `ISceneService` instead of Terminal.Gui
- ✅ **Faster iteration**: Change UI without touching game logic
- ✅ **Better debugging**: Clear separation of concerns

## Risks & Mitigations

### Risk: Breaking Existing Functionality

**Mitigation**:
- Keep existing E2E Playwright tests
- Incremental refactoring with `git commit` after each phase
- Test after each phase before proceeding

### Risk: Plugin Loading Order

**Mitigation**:
- Use plugin priority in `.plugin.json`:
  - `TerminalGuiSceneProvider`: Priority 100 (load first)
  - `ConsoleDungeonApp`: Priority 50 (load after scene provider)

### Risk: Performance Regression (Debouncing)

**Mitigation**:
- Benchmark before/after
- Make debounce interval configurable
- Monitor frame rate in E2E tests

## Future Enhancements

1. **Multiple Scene Providers**:
   - `WingedBean.Providers.UnityScene` for Unity profile
   - `WingedBean.Providers.GodotScene` for Godot profile
   - `WingedBean.Providers.ImGuiScene` for native profile

2. **Camera System**:
   - Add `Camera` to `ISceneService.GetViewport()`
   - Support panning, zooming, following player

3. **Resize Handling**:
   - `ISceneService.Resize(int width, int height)` event
   - Trigger re-render on terminal resize

4. **Scene Layers**:
   - Background layer (floor, walls)
   - Entity layer (player, monsters)
   - UI overlay layer (HUD, menus)

## Success Criteria

- [ ] `WingedBean.Contracts.Scene` project created and builds
- [ ] `WingedBean.Providers.TerminalGuiScene` implements `ISceneService`
- [ ] `ConsoleDungeonApp` has ZERO `using Terminal.Gui` statements
- [ ] `ConsoleDungeonApp` reduced to <200 lines
- [ ] All existing E2E tests pass
- [ ] Unit tests for `ISceneService` mock scenarios
- [ ] `WingedBean.Plugins.TerminalUI` deleted or marked deprecated

## Related Work

- **RFC-0002**: 4-Tier Service Architecture (foundation)
- **RFC-0018**: Render and UI Services (introduced `IRenderService`, `IGameUIService`)
- **RFC-0006**: Dynamic Plugin Loading (plugin system used here)

## Appendix A: Comparison Table

| Aspect | Before (Current) | After (Proposed) |
|--------|------------------|------------------|
| **ConsoleDungeonApp lines** | 800+ | ~150 |
| **Terminal.Gui dependencies** | ConsoleDungeonApp (Tier 3) | TerminalGuiSceneProvider (Tier 4) |
| **Viewport management** | Mixed in ConsoleDungeonApp | ISceneService.GetViewport() |
| **UI thread marshaling** | `Application.Invoke()` in app | Hidden in provider |
| **Testability** | Requires Terminal.Gui FakeDriver | Mock ISceneService |
| **Tier compliance** | ❌ Tier 3 → Tier 4 violation | ✅ Tier 3 → Tier 1 contract |

## Appendix B: File Structure

```
development/dotnet/
├── framework/src/
│   └── WingedBean.Contracts.Scene/          [NEW]
│       ├── ISceneService.cs
│       ├── Viewport.cs
│       └── SceneShutdownEventArgs.cs
│
├── console/
│   ├── src/
│   │   ├── plugins/
│   │   │   ├── WingedBean.Plugins.ConsoleDungeon/  [REFACTOR]
│   │   │   │   └── ConsoleDungeonApp.cs         (150 lines, no Terminal.Gui)
│   │   │   │
│   │   │   └── WingedBean.Plugins.TerminalUI/   [DELETE or RENAME]
│   │   │
│   │   └── providers/
│   │       └── WingedBean.Providers.TerminalGuiScene/  [NEW]
│   │           ├── TerminalGuiSceneProvider.cs
│   │           └── .plugin.json
│   │
│   └── tests/
│       └── providers/
│           └── WingedBean.Providers.TerminalGuiScene.Tests/  [NEW]
```

## Conclusion

This RFC completes the architectural separation started in RFC-0018 by extracting the final Terminal.Gui dependencies from game logic into a proper Tier 4 provider. The introduction of `ISceneService` enables true framework-agnostic game development while maintaining all existing functionality.

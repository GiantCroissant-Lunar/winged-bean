---
id: RFC-0018
title: Render and UI Services for Console Profile
status: Proposed
category: framework, architecture, console
created: 2025-10-03
updated: 2025-10-03
author: GitHub Copilot
---

# RFC-0018: Render and UI Services for Console Profile

## Summary

Implement proper `IRenderService` and `IGameUIService` contracts and providers to separate rendering/UI concerns from game logic and application code. This refactoring aligns the Console profile with RFC-0002's 4-tier service architecture and fixes critical UX issues (input handling, UI layout).

## Motivation

### Current Problems

The ConsoleDungeon Terminal.Gui implementation has several architectural and UX issues:

1. **❌ Architecture Violation: Rendering Logic Mixed in Application Code**
   - `ConsoleDungeonApp.cs` contains rendering logic (`RenderGameWorld()`)
   - `RenderSystem.cs` has no abstraction layer (writes directly to text buffer)
   - Violates RFC-0002: No `IRenderService` contract exists
   - Makes rendering non-reusable across profiles (Unity, Godot)

2. **❌ Input Handling Broken**
   - Arrow keys captured by Terminal.Gui for UI navigation
   - Player cannot move character with arrow keys
   - Input not routed to `IDungeonGameService.HandleInput()`
   - No distinction between game input vs. UI navigation

3. **❌ Inefficient UI Layout**
   - Fixed "Controls" panel wastes 40% of screen width
   - Shows static text that should be in a menu
   - Game view limited to 60% width (should be 100%)
   - No menu system for game options

4. **❌ Tight Coupling**
   - ConsoleDungeonApp directly queries `IWorld` and entity snapshots
   - No service abstraction for rendering
   - Difficult to test, hard to extend, impossible to reuse

### Why Services?

**Benefits of service-oriented architecture (RFC-0002):**

1. **Separation of Concerns**: Rendering is a service, not application logic
2. **Reusability**: `IRenderService` can be implemented for Unity, Godot, Web
3. **Testability**: Services can be mocked, tested in isolation
4. **Composability**: Mix and match providers (ASCII, Unicode, Color rendering)
5. **4-Tier Compliance**: Contracts → Façades → Adapters → Providers

## Proposal

### Architecture Overview

Following RFC-0002's 4-tier architecture:

```
┌─ Tier 1: Contracts ────────────────────────────────────────┐
│ WingedBean.Contracts.Game/                                 │
│ ├── IRenderService.cs          (Render game world to buffer)│
│ ├── IGameUIService.cs          (Menu management, input)    │
│ ├── RenderBuffer.cs            (2D char buffer + colors)   │
│ └── GameInputEvent.cs          (Input abstraction)         │
└────────────────────────────────────────────────────────────┘
                            ↓
┌─ Tier 2: Source-Generated Façades (Future) ────────────────┐
│ [RealizeService(typeof(IRenderService))]                   │
│ [RealizeService(typeof(IGameUIService))]                   │
└────────────────────────────────────────────────────────────┘
                            ↓
┌─ Tier 3: Adapters (Future: Resilience, Telemetry) ────────┐
│ (Not needed for this RFC - future enhancement)             │
└────────────────────────────────────────────────────────────┘
                            ↓
┌─ Tier 4: Providers (Console Profile) ──────────────────────┐
│ WingedBean.Plugins.DungeonGame/                            │
│ ├── RenderService.cs           (ASCII rendering provider)  │
│ └── GameUIService.cs           (Menu management provider)  │
└────────────────────────────────────────────────────────────┘
```

### 1. IRenderService Contract (Tier 1)

**Purpose**: Abstract rendering logic from game and UI

```csharp
namespace WingedBean.Contracts.Game;

/// <summary>
/// Service for rendering game world to a display buffer.
/// Profile-agnostic: implementations can render ASCII, Unicode, or graphics.
/// </summary>
public interface IRenderService
{
    /// <summary>
    /// Render the game world to a 2D buffer.
    /// </summary>
    /// <param name="entitySnapshots">Current entity positions and appearances</param>
    /// <param name="width">Buffer width in characters</param>
    /// <param name="height">Buffer height in characters</param>
    /// <returns>Rendered buffer ready for display</returns>
    RenderBuffer Render(
        IReadOnlyList<EntitySnapshot> entitySnapshots, 
        int width, 
        int height
    );
    
    /// <summary>
    /// Set rendering mode (ASCII, Unicode, Color).
    /// </summary>
    void SetRenderMode(RenderMode mode);
    
    /// <summary>
    /// Current rendering mode.
    /// </summary>
    RenderMode CurrentMode { get; }
}

public enum RenderMode
{
    ASCII,      // Simple ASCII characters
    Unicode,    // Unicode box drawing, emojis
    Color,      // ANSI color codes
    TrueColor   // 24-bit RGB (future)
}
```

**RenderBuffer.cs**:

```csharp
namespace WingedBean.Contracts.Game;

/// <summary>
/// Rendered game world buffer.
/// Contains character data and optional color information.
/// </summary>
public record RenderBuffer(
    char[,] Cells,
    Dictionary<(int X, int Y), ConsoleColor>? ForegroundColors = null,
    Dictionary<(int X, int Y), ConsoleColor>? BackgroundColors = null
)
{
    /// <summary>
    /// Convert buffer to string (for Terminal.Gui TextView).
    /// </summary>
    public string ToText()
    {
        int height = Cells.GetLength(0);
        int width = Cells.GetLength(1);
        var sb = new StringBuilder();
        
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                sb.Append(Cells[y, x]);
            }
            if (y < height - 1)
            {
                sb.AppendLine();
            }
        }
        
        return sb.ToString();
    }
}
```

### 2. IGameUIService Contract (Tier 1)

**Purpose**: Abstract UI management (menus, dialogs) from application

```csharp
namespace WingedBean.Contracts.Game;

/// <summary>
/// Service for game UI management (menus, dialogs, HUD).
/// Profile-agnostic: implementations can use Terminal.Gui, Unity UI, ImGui, etc.
/// </summary>
public interface IGameUIService
{
    /// <summary>
    /// Initialize the UI service with the main window.
    /// </summary>
    void Initialize(object mainWindow);
    
    /// <summary>
    /// Show a menu overlay.
    /// </summary>
    void ShowMenu(MenuType type);
    
    /// <summary>
    /// Hide the current menu.
    /// </summary>
    void HideMenu();
    
    /// <summary>
    /// Is a menu currently visible?
    /// </summary>
    bool IsMenuVisible { get; }
    
    /// <summary>
    /// Observable stream of game input events (movement, actions).
    /// </summary>
    IObservable<GameInputEvent> InputObservable { get; }
}

public enum MenuType
{
    Main,       // Main menu (Resume, Inventory, Save, Quit)
    Inventory,  // Inventory screen
    Settings,   // Game settings
    Help        // Help/controls screen
}

/// <summary>
/// Game input event (decoupled from Terminal.Gui KeyEvent).
/// </summary>
public record GameInputEvent(
    GameInputType Type,
    DateTimeOffset Timestamp
);

public enum GameInputType
{
    // Movement
    MoveUp,
    MoveDown,
    MoveLeft,
    MoveRight,
    
    // Actions
    Attack,
    Use,
    Pickup,
    
    // UI
    ToggleMenu,
    ToggleInventory,
    
    // System
    Quit
}
```

### 3. RenderService Provider (Tier 4)

**Location**: `WingedBean.Plugins.DungeonGame/Services/RenderService.cs`

```csharp
namespace WingedBean.Plugins.DungeonGame.Services;

[Plugin(
    Name = "RenderService",
    Provides = new[] { typeof(IRenderService) },
    Priority = 100
)]
public class RenderService : IRenderService
{
    private RenderMode _currentMode = RenderMode.ASCII;
    
    public RenderBuffer Render(
        IReadOnlyList<EntitySnapshot> entitySnapshots,
        int width,
        int height)
    {
        // Create buffer filled with floor tiles
        var cells = new char[height, width];
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                cells[y, x] = '.';
            }
        }
        
        // Sort entities by render layer
        var sorted = entitySnapshots.OrderBy(e => e.RenderLayer).ToList();
        
        // Render entities
        foreach (var entity in sorted)
        {
            // Scale world coordinates to buffer (world is ~80x24, buffer varies)
            int bufX = entity.Position.X * width / 80;
            int bufY = entity.Position.Y * height / 24;
            
            if (bufX >= 0 && bufX < width && bufY >= 0 && bufY < height)
            {
                cells[bufY, bufX] = entity.Symbol;
            }
        }
        
        // TODO: Add color support in future
        return new RenderBuffer(cells);
    }
    
    public void SetRenderMode(RenderMode mode)
    {
        _currentMode = mode;
    }
    
    public RenderMode CurrentMode => _currentMode;
}
```

### 4. GameUIService Provider (Tier 4)

**Location**: `WingedBean.Plugins.DungeonGame/Services/GameUIService.cs`

```csharp
namespace WingedBean.Plugins.DungeonGame.Services;

[Plugin(
    Name = "GameUIService",
    Provides = new[] { typeof(IGameUIService) },
    Priority = 100
)]
public class GameUIService : IGameUIService
{
    private Window? _mainWindow;
    private Dialog? _currentMenu;
    private readonly Subject<GameInputEvent> _inputSubject = new();
    
    public void Initialize(object mainWindow)
    {
        if (mainWindow is not Window window)
        {
            throw new ArgumentException("mainWindow must be a Terminal.Gui Window", nameof(mainWindow));
        }
        
        _mainWindow = window;
    }
    
    public void ShowMenu(MenuType type)
    {
        if (_mainWindow == null)
        {
            throw new InvalidOperationException("UI service not initialized");
        }
        
        // Hide existing menu
        HideMenu();
        
        // Create menu dialog
        _currentMenu = type switch
        {
            MenuType.Main => CreateMainMenu(),
            MenuType.Inventory => CreateInventoryMenu(),
            MenuType.Settings => CreateSettingsMenu(),
            MenuType.Help => CreateHelpMenu(),
            _ => throw new ArgumentException($"Unknown menu type: {type}")
        };
        
        Application.Run(_currentMenu);
    }
    
    public void HideMenu()
    {
        if (_currentMenu != null)
        {
            Application.RequestStop(_currentMenu);
            _currentMenu.Dispose();
            _currentMenu = null;
        }
    }
    
    public bool IsMenuVisible => _currentMenu != null;
    
    public IObservable<GameInputEvent> InputObservable => _inputSubject;
    
    /// <summary>
    /// Process key input and emit game events.
    /// Call this from window's KeyDown handler.
    /// </summary>
    public void ProcessInput(KeyEvent keyEvent)
    {
        // Don't process input if menu is visible
        if (IsMenuVisible)
        {
            return;
        }
        
        var inputType = MapKeyToInput(keyEvent.KeyCode);
        if (inputType.HasValue)
        {
            _inputSubject.OnNext(new GameInputEvent(
                inputType.Value,
                DateTimeOffset.UtcNow
            ));
        }
    }
    
    private GameInputType? MapKeyToInput(KeyCode keyCode)
    {
        return keyCode switch
        {
            KeyCode.CursorUp or KeyCode.W => GameInputType.MoveUp,
            KeyCode.CursorDown or KeyCode.S => GameInputType.MoveDown,
            KeyCode.CursorLeft or KeyCode.A => GameInputType.MoveLeft,
            KeyCode.CursorRight or KeyCode.D => GameInputType.MoveRight,
            KeyCode.Space => GameInputType.Attack,
            KeyCode.E => GameInputType.Use,
            KeyCode.G => GameInputType.Pickup,
            KeyCode.M => GameInputType.ToggleMenu,
            KeyCode.I => GameInputType.ToggleInventory,
            KeyCode.Q => GameInputType.Quit,
            _ => null
        };
    }
    
    private Dialog CreateMainMenu()
    {
        var dialog = new Dialog
        {
            Title = "Game Menu",
            Width = Dim.Percent(50),
            Height = Dim.Percent(50),
            X = Pos.Center(),
            Y = Pos.Center()
        };
        
        var resumeBtn = new Button { Text = "[R]esume", X = Pos.Center(), Y = 1 };
        var inventoryBtn = new Button { Text = "[I]nventory", X = Pos.Center(), Y = 3 };
        var saveBtn = new Button { Text = "[S]ave Game", X = Pos.Center(), Y = 5 };
        var quitBtn = new Button { Text = "[Q]uit", X = Pos.Center(), Y = 7 };
        
        resumeBtn.Accepting += (s, e) => HideMenu();
        inventoryBtn.Accepting += (s, e) => { HideMenu(); ShowMenu(MenuType.Inventory); };
        quitBtn.Accepting += (s, e) => {
            _inputSubject.OnNext(new GameInputEvent(GameInputType.Quit, DateTimeOffset.UtcNow));
            HideMenu();
        };
        
        dialog.Add(resumeBtn, inventoryBtn, saveBtn, quitBtn);
        
        return dialog;
    }
    
    private Dialog CreateInventoryMenu()
    {
        // TODO: Implement inventory UI
        var dialog = new Dialog { Title = "Inventory", Width = 40, Height = 20 };
        return dialog;
    }
    
    private Dialog CreateSettingsMenu()
    {
        // TODO: Implement settings UI
        var dialog = new Dialog { Title = "Settings", Width = 40, Height = 20 };
        return dialog;
    }
    
    private Dialog CreateHelpMenu()
    {
        var dialog = new Dialog
        {
            Title = "Controls",
            Width = Dim.Percent(50),
            Height = Dim.Percent(60),
            X = Pos.Center(),
            Y = Pos.Center()
        };
        
        var helpText = new TextView
        {
            X = 0,
            Y = 0,
            Width = Dim.Fill(),
            Height = Dim.Fill(1),
            ReadOnly = true,
            Text = @"Movement:
  Arrow Keys / WASD - Move player
  
Actions:
  Space - Attack
  E - Use item
  G - Pick up item
  
UI:
  M - Toggle menu
  I - Inventory
  Q - Quit
  
Press ESC to close this help."
        };
        
        dialog.Add(helpText);
        
        return dialog;
    }
}
```

### 5. ConsoleDungeonApp Refactoring

**Changes**:

1. **Inject services via registry**:
   ```csharp
   private IRenderService? _renderService;
   private IGameUIService? _uiService;
   
   // In StartAsync:
   _renderService = _registry.Get<IRenderService>();
   _uiService = _registry.Get<IGameUIService>();
   ```

2. **New UI layout**:
   ```csharp
   private void CreateMainWindow()
   {
       _mainWindow = new Window()
       {
           Title = "Console Dungeon | M=Menu",
           X = 0, Y = 0,
           Width = Dim.Fill(),
           Height = Dim.Fill()
       };
       
       // Game view (90% height)
       _gameWorldView = new TextView
       {
           X = 0, Y = 0,
           Width = Dim.Fill(),
           Height = Dim.Percent(90),
           ReadOnly = true
       };
       
       // Status bar (10% height)
       _statusLabel = new Label
       {
           X = 0,
           Y = Pos.AnchorEnd(1),
           Width = Dim.Fill(),
           Text = "HP: 100/100 | MP: 50/50 | Lvl: 1 | M=Menu"
       };
       
       _mainWindow.Add(_gameWorldView, _statusLabel);
   }
   ```

3. **Input handling**:
   ```csharp
   _mainWindow.KeyDown += (sender, e) => {
       // Let UI service process input
       _uiService?.ProcessInput(e);
       
       // Mark as handled to prevent Terminal.Gui navigation
       if (ShouldCaptureInput(e.KeyCode))
       {
           e.Handled = true;
       }
   };
   
   // Subscribe to game input events
   _uiService?.InputObservable.Subscribe(input =>
   {
       if (input.Type == GameInputType.ToggleMenu)
       {
           if (_uiService.IsMenuVisible)
               _uiService.HideMenu();
           else
               _uiService.ShowMenu(MenuType.Main);
       }
       else
       {
           // Map to GameInput and send to game service
           var gameInput = MapToGameInput(input);
           _gameService?.HandleInput(gameInput);
       }
   });
   ```

4. **Rendering**:
   ```csharp
   _uiTimer.Elapsed += (s, e) =>
   {
       _gameService.Update(0.1f);
       
       // Render game world via service
       if (_renderService != null && _currentEntities != null)
       {
           var buffer = _renderService.Render(
               _currentEntities,
               40,  // Width in chars
               20   // Height in chars
           );
           _gameWorldView.Text = buffer.ToText();
       }
       
       // Update status bar
       var stats = _gameService.CurrentPlayerStats;
       _statusLabel.Text = $"HP: {stats.CurrentHP}/{stats.MaxHP} | " +
                          $"MP: {stats.CurrentMana}/{stats.MaxMana} | " +
                          $"Lvl: {stats.Level} | M=Menu";
   };
   ```

## Implementation Plan

### Phase 1: Contracts (Tier 1)
1. Create `IRenderService.cs` in `WingedBean.Contracts.Game`
2. Create `IGameUIService.cs` in `WingedBean.Contracts.Game`
3. Create `RenderBuffer.cs` in `WingedBean.Contracts.Game`
4. Create `GameInputEvent.cs` in `WingedBean.Contracts.Game`

### Phase 2: Providers (Tier 4)
1. Create `RenderService.cs` in `WingedBean.Plugins.DungeonGame/Services/`
2. Create `GameUIService.cs` in `WingedBean.Plugins.DungeonGame/Services/`
3. Register services in `DungeonGamePlugin.cs`

### Phase 3: Application Refactoring
1. Update `ConsoleDungeonApp.cs`:
   - Inject `IRenderService` and `IGameUIService`
   - Remove `RenderGameWorld()` method (use service)
   - Refactor UI layout (remove controls panel, add status bar)
   - Fix input handling (capture game keys, route to service)
2. Simplify or remove `RenderSystem.cs` (logic moved to `RenderService`)

### Phase 4: Testing
1. Build and run: `task build && pm2 restart console-dungeon`
2. Test arrow keys move player
3. Test M key toggles menu
4. Test menu navigation (ESC closes, buttons work)
5. Run Playwright tests: `pnpm exec playwright test`

## Benefits

### 1. Architecture Compliance (RFC-0002)
- ✅ Proper 4-tier separation
- ✅ Service contracts in Tier 1
- ✅ Providers in Tier 4
- ✅ Reusable across profiles (Unity, Godot can implement same contracts)

### 2. Better UX
- ✅ Full-screen game view (100% width, 90% height)
- ✅ Menu system (M key toggle, overlay design)
- ✅ Working input (arrow keys control player)
- ✅ Clean status bar (no wasted space)

### 3. Better Code Quality
- ✅ Separation of concerns (rendering is a service)
- ✅ Testability (mock services in tests)
- ✅ Extensibility (add color rendering, different modes)
- ✅ Maintainability (clear responsibilities)

## Future Enhancements

1. **Color Rendering**: Add `RenderMode.Color` support using ANSI codes
2. **Unicode Mode**: Use box drawing characters for walls, emojis for entities
3. **Canvas Rendering**: Custom Terminal.Gui view with Cell-based rendering (full color support)
4. **Multiple Render Providers**: Switch between ASCII, Unicode, Color at runtime
5. **UI Themes**: Different color schemes, layouts
6. **Accessibility**: High contrast mode, screen reader support

## Alternatives Considered

### Alternative 1: Keep rendering in ConsoleDungeonApp
**Rejected**: Violates RFC-0002, not reusable, tightly coupled

### Alternative 2: Use Terminal.Gui Canvas directly
**Deferred**: More complex, overkill for ASCII rendering. Can add later as alternative provider.

### Alternative 3: Render in RenderSystem (no service)
**Rejected**: ECS systems should be game logic only, not rendering. Rendering is a cross-cutting concern that belongs in a service.

## References

- RFC-0002: Service Platform Core (4-Tier, Multi-Profile Architecture)
- RFC-0007: Arch ECS Integration
- RFC-0017: Reactive Plugin Architecture for Dungeon Game
- Terminal.Gui v2 Documentation: https://gui-cs.github.io/Terminal.GuiV2Docs/

## Appendix: File Changes

### New Files
- `WingedBean.Contracts.Game/IRenderService.cs`
- `WingedBean.Contracts.Game/IGameUIService.cs`
- `WingedBean.Contracts.Game/RenderBuffer.cs`
- `WingedBean.Contracts.Game/GameInputEvent.cs`
- `WingedBean.Plugins.DungeonGame/Services/RenderService.cs`
- `WingedBean.Plugins.DungeonGame/Services/GameUIService.cs`

### Modified Files
- `WingedBean.Plugins.ConsoleDungeon/ConsoleDungeonApp.cs` (major refactoring)
- `WingedBean.Plugins.DungeonGame/DungeonGamePlugin.cs` (register new services)
- `WingedBean.Plugins.DungeonGame/Systems/RenderSystem.cs` (simplify or remove)

### Test Files
- `WingedBean.Plugins.DungeonGame.Tests/Services/RenderServiceTests.cs` (new)
- `development/nodejs/tests/e2e/check-dungeon-display.spec.js` (update assertions)

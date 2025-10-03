using System;
using System.Reactive.Subjects;
using Terminal.Gui;
using WingedBean.Contracts.Core;
using WingedBean.Contracts.Game;

namespace WingedBean.Plugins.DungeonGame.Services;

/// <summary>
/// Tier 4 Provider: Terminal.Gui UI management implementation.
/// Part of RFC-0018: Render and UI Services for Console Profile.
/// </summary>
[Plugin(
    Name = "GameUIServiceProvider",
    Provides = new[] { typeof(IGameUIService) },
    Priority = 100
)]
public class GameUIServiceProvider : IGameUIService
{
    private Window? _mainWindow;
    private Dialog? _currentMenu;
    private readonly Subject<GameInputEvent> _inputSubject = new();
    
    public void Initialize(object mainWindow)
    {
        // More flexible type checking - just store the object and try to use it
        _mainWindow = mainWindow as Window;
        
        if (_mainWindow == null)
        {
            // Log warning but don't throw - allow graceful degradation
            System.Console.WriteLine($"[GameUIService] Warning: mainWindow is not a Window (type: {mainWindow?.GetType().Name ?? "null"})");
        }
    }
    
    public void ShowMenu(MenuType type)
    {
        if (_mainWindow == null)
        {
            throw new InvalidOperationException("UI service not initialized. Call Initialize() first.");
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
            _ => throw new ArgumentException($"Unknown menu type: {type}", nameof(type))
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
    /// Emit a game input event.
    /// Called by ConsoleDungeonApp after mapping Terminal.Gui keys.
    /// </summary>
    public void EmitInput(GameInputEvent inputEvent)
    {
        // Don't emit input if menu is visible (menu captures input)
        if (!IsMenuVisible)
        {
            _inputSubject.OnNext(inputEvent);
        }
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
        var helpBtn = new Button { Text = "[H]elp", X = Pos.Center(), Y = 5 };
        var quitBtn = new Button { Text = "[Q]uit", X = Pos.Center(), Y = 7 };
        
        resumeBtn.Accepting += (s, e) => HideMenu();
        inventoryBtn.Accepting += (s, e) => { 
            HideMenu(); 
            ShowMenu(MenuType.Inventory); 
        };
        helpBtn.Accepting += (s, e) => { 
            HideMenu(); 
            ShowMenu(MenuType.Help); 
        };
        quitBtn.Accepting += (s, e) => {
            _inputSubject.OnNext(new GameInputEvent(GameInputType.Quit, DateTimeOffset.UtcNow));
            HideMenu();
        };
        
        dialog.Add(resumeBtn, inventoryBtn, helpBtn, quitBtn);
        
        return dialog;
    }
    
    private Dialog CreateInventoryMenu()
    {
        var dialog = new Dialog
        {
            Title = "Inventory",
            Width = Dim.Percent(60),
            Height = Dim.Percent(60),
            X = Pos.Center(),
            Y = Pos.Center()
        };
        
        var infoLabel = new Label
        {
            X = 1,
            Y = 1,
            Text = "Inventory system not yet implemented.\nPress ESC to close."
        };
        
        dialog.Add(infoLabel);
        
        return dialog;
    }
    
    private Dialog CreateSettingsMenu()
    {
        var dialog = new Dialog
        {
            Title = "Settings",
            Width = Dim.Percent(50),
            Height = Dim.Percent(50),
            X = Pos.Center(),
            Y = Pos.Center()
        };
        
        var infoLabel = new Label
        {
            X = 1,
            Y = 1,
            Text = "Settings not yet implemented.\nPress ESC to close."
        };
        
        dialog.Add(infoLabel);
        
        return dialog;
    }
    
    private Dialog CreateHelpMenu()
    {
        var dialog = new Dialog
        {
            Title = "Controls & Help",
            Width = Dim.Percent(60),
            Height = Dim.Percent(70),
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
            Text = @"=== MOVEMENT ===
  Arrow Keys / WASD - Move player

=== ACTIONS ===
  Space - Attack
  E - Use item
  G - Pick up item

=== UI ===
  M - Toggle menu
  I - Inventory
  Q - Quit game

=== TIPS ===
  - Use arrow keys or WASD to move your character (@)
  - Avoid enemies (g) or attack them with Space
  - Press M anytime to open this menu

Press ESC to close this help."
        };
        
        dialog.Add(helpText);
        
        return dialog;
    }
}

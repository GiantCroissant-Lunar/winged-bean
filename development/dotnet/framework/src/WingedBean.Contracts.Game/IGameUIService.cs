using System;

namespace WingedBean.Contracts.Game;

/// <summary>
/// Service for game UI management (menus, dialogs, HUD).
/// Profile-agnostic: implementations can use Terminal.Gui, Unity UI, ImGui, etc.
/// Part of RFC-0018: Render and UI Services for Console Profile.
/// </summary>
public interface IGameUIService
{
    /// <summary>
    /// Initialize the UI service with the main window.
    /// </summary>
    /// <param name="mainWindow">Platform-specific window object (e.g., Terminal.Gui Window)</param>
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

/// <summary>
/// Menu types for game UI.
/// </summary>
public enum MenuType
{
    /// <summary>
    /// Main menu (Resume, Inventory, Save, Quit)
    /// </summary>
    Main,
    
    /// <summary>
    /// Inventory screen
    /// </summary>
    Inventory,
    
    /// <summary>
    /// Game settings
    /// </summary>
    Settings,
    
    /// <summary>
    /// Help/controls screen
    /// </summary>
    Help
}

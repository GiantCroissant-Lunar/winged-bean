using System;

namespace WingedBean.Contracts.Game;

/// <summary>
/// Game input event (decoupled from Terminal.Gui KeyEvent).
/// Part of RFC-0018: Render and UI Services for Console Profile.
/// </summary>
public record GameInputEvent(
    GameInputType Type,
    DateTimeOffset Timestamp
);

/// <summary>
/// Game input types (platform-agnostic).
/// </summary>
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

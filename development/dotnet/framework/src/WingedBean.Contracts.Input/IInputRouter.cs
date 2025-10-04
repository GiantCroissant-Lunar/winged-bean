using System;
using WingedBean.Contracts.Game;

namespace WingedBean.Contracts.Input;

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

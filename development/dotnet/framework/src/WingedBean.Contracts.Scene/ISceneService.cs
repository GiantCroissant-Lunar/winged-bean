using System;
using System.Collections.Generic;
using WingedBean.Contracts.Game;

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

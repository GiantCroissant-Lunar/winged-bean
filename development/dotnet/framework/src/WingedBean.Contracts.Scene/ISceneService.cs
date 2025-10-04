using System;
using System.Collections.Generic;
using WingedBean.Contracts.Game;

namespace WingedBean.Contracts.Scene;

/// <summary>
/// Scene service manages UI lifecycle, viewport, and game world rendering.
/// Platform-agnostic: implementations can use Terminal.Gui, Unity, Godot, ImGui, etc.
/// Supports multi-layer rendering for compositing background, entities, effects, and UI.
/// Supports camera system for panning, zooming, and following entities.
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
    /// Get current camera viewport (viewport + camera state).
    /// </summary>
    CameraViewport GetCameraViewport();

    /// <summary>
    /// Set camera position and configuration.
    /// </summary>
    void SetCamera(Camera camera);

    /// <summary>
    /// Update the game world view with latest entity snapshots.
    /// Thread-safe: can be called from any thread.
    /// Implementation handles debouncing, rendering, and UI marshaling.
    /// </summary>
    /// <param name="snapshots">Current entity positions/appearances</param>
    void UpdateWorld(IReadOnlyList<EntitySnapshot> snapshots);

    /// <summary>
    /// Update world with layered snapshots for multi-layer rendering.
    /// Layers render in order: Background → Entities → Effects → UI.
    /// Allows separate rendering passes for different visual elements.
    /// </summary>
    /// <param name="layers">Layered snapshots to composite</param>
    void UpdateWorldLayered(IReadOnlyList<LayeredSnapshot> layers);

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

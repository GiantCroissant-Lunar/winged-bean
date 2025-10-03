using System.Collections.Generic;

namespace WingedBean.Contracts.Game;

/// <summary>
/// Service for rendering game world to a display buffer.
/// Profile-agnostic: implementations can render ASCII, Unicode, or graphics.
/// Part of RFC-0018: Render and UI Services for Console Profile.
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

/// <summary>
/// Rendering mode for game world display.
/// </summary>
public enum RenderMode
{
    /// <summary>
    /// Simple ASCII characters (., @, g, etc.)
    /// </summary>
    ASCII,
    
    /// <summary>
    /// Unicode box drawing, emojis
    /// </summary>
    Unicode,
    
    /// <summary>
    /// ANSI color codes (8/16 colors)
    /// </summary>
    Color,
    
    /// <summary>
    /// 24-bit RGB colors (future)
    /// </summary>
    TrueColor
}

using System.Text;
using Microsoft.Extensions.Logging;
using WingedBean.Plugins.ArchECS.Internal;
using WingedBean.Plugins.DungeonGame.Components;

namespace WingedBean.Plugins.DungeonGame.Systems;

/// <summary>
/// System responsible for rendering entities with Renderable components.
/// Renders to a text buffer that can be displayed by Terminal.Gui or console.
/// Supports viewport culling when camera system is active.
/// </summary>
public class RenderSystem : IECSSystem
{
    private readonly ILogger<RenderSystem>? _logger;
    private readonly List<(Position pos, Renderable render)> _renderBuffer = new();

    // Public property to access the last rendered frame
    public string? LastRenderedFrame { get; private set; }

    // Render dimensions (used when no camera is active)
    private int _renderWidth = 40;
    private int _renderHeight = 15;

    // Statistics
    public int LastFrameEntityCount { get; private set; }
    public int LastFrameCulledCount { get; private set; }

    public RenderSystem(ILogger<RenderSystem>? logger = null)
    {
        _logger = logger;
    }

    public void Execute(IWorld world, float deltaTime)
    {
        // Clear the render buffer
        _renderBuffer.Clear();
        LastFrameEntityCount = 0;
        LastFrameCulledCount = 0;

        // Get viewport bounds from camera system (if available)
        var boundsComp = world.GetSingleton<ViewportBoundsComponent>();
        
        if (boundsComp != null)
        {
            // Camera-based rendering with viewport culling
            RenderWithViewportCulling(world, boundsComp);
        }
        else
        {
            // Fallback: render without camera (legacy behavior)
            RenderWithoutCamera(world);
        }

        // Sort by render layer (lower layers render first)
        _renderBuffer.Sort((a, b) => a.render.RenderLayer.CompareTo(b.render.RenderLayer));

        // Render to text buffer
        LastRenderedFrame = RenderToTextBuffer(boundsComp);
        
        _logger?.LogTrace("Rendered {EntityCount} entities ({CulledCount} culled)", 
            LastFrameEntityCount, LastFrameCulledCount);
    }

    private void RenderWithViewportCulling(IWorld world, ViewportBoundsComponent boundsComp)
    {
        var bounds = boundsComp.Bounds;
        
        // Collect only entities within viewport bounds
        foreach (var entity in world.CreateQuery<Position, Renderable>())
        {
            var pos = world.GetComponent<Position>(entity);
            var render = world.GetComponent<Renderable>(entity);
            
            // Viewport culling: skip entities outside viewport
            if (!bounds.Contains(pos.X, pos.Y))
            {
                LastFrameCulledCount++;
                continue;
            }
            
            LastFrameEntityCount++;
            _renderBuffer.Add((pos, render));
        }
        
        _logger?.LogDebug("Viewport culling: {Visible}/{Total} entities visible", 
            LastFrameEntityCount, LastFrameEntityCount + LastFrameCulledCount);
    }

    private void RenderWithoutCamera(IWorld world)
    {
        // Legacy behavior: collect all renderable entities
        foreach (var entity in world.CreateQuery<Position, Renderable>())
        {
            var pos = world.GetComponent<Position>(entity);
            var render = world.GetComponent<Renderable>(entity);
            
            LastFrameEntityCount++;
            _renderBuffer.Add((pos, render));
        }
    }

    private string RenderToTextBuffer(ViewportBoundsComponent? boundsComp)
    {
        int bufferWidth, bufferHeight;
        
        if (boundsComp != null)
        {
            // Use viewport dimensions
            var bounds = boundsComp.Bounds;
            bufferWidth = bounds.Width;
            bufferHeight = bounds.Height;
        }
        else
        {
            // Use fixed dimensions (legacy)
            bufferWidth = _renderWidth;
            bufferHeight = _renderHeight;
        }
        
        // Create a 2D buffer filled with empty spaces
        var buffer = new char[bufferHeight, bufferWidth];
        for (var y = 0; y < bufferHeight; y++)
        {
            for (var x = 0; x < bufferWidth; x++)
            {
                buffer[y, x] = '.'; // Floor/background character
            }
        }

        // Draw entities into the buffer
        foreach (var (pos, render) in _renderBuffer)
        {
            int screenX, screenY;
            
            if (boundsComp != null)
            {
                // Convert world coordinates to viewport coordinates
                var bounds = boundsComp.Bounds;
                (screenX, screenY) = bounds.WorldToViewport(pos.X, pos.Y);
            }
            else
            {
                // Use world coordinates directly (legacy)
                screenX = pos.X;
                screenY = pos.Y;
            }
            
            // Only render if within buffer bounds
            if (screenX >= 0 && screenX < bufferWidth &&
                screenY >= 0 && screenY < bufferHeight)
            {
                buffer[screenY, screenX] = render.Symbol;
            }
        }

        // Convert buffer to string
        var sb = new StringBuilder();
        for (var y = 0; y < bufferHeight; y++)
        {
            for (var x = 0; x < bufferWidth; x++)
            {
                sb.Append(buffer[y, x]);
            }
            if (y < bufferHeight - 1)
            {
                sb.AppendLine();
            }
        }

        return sb.ToString();
    }

    public void SetRenderDimensions(int width, int height)
    {
        _renderWidth = Math.Max(1, width);
        _renderHeight = Math.Max(1, height);
    }
}

using System.Text;
using Microsoft.Extensions.Logging;
using WingedBean.Plugins.ArchECS.Internal;
using WingedBean.Plugins.DungeonGame.Components;
using WingedBean.Plugins.DungeonGame.Map;
using WingedBean.Gameplay.Perception.Components;
using TheSadRogue.Primitives;

namespace WingedBean.Plugins.DungeonGame.Systems;

/// <summary>
/// System responsible for rendering entities with Renderable components.
/// Renders to a text buffer that can be displayed by Terminal.Gui or console.
/// Supports viewport culling when camera system is active.
/// Supports fog of war rendering (visible/explored/unexplored states).
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
    public int LastFrameFogCulledCount { get; private set; }

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
        LastFrameFogCulledCount = 0;

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
        LastRenderedFrame = RenderToTextBuffer(world, boundsComp);

        _logger?.LogTrace("Rendered {EntityCount} entities ({CulledCount} culled, {FogCulled} fog culled)",
            LastFrameEntityCount, LastFrameCulledCount, LastFrameFogCulledCount);
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

    private string RenderToTextBuffer(IWorld world, ViewportBoundsComponent? boundsComp)
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

        // Get fog of war configuration
        var fogConfig = world.GetSingleton<FogOfWarConfig>();
        var hasFog = fogConfig != null && fogConfig.Mode != FogMode.None;

        // Create buffers
        var buffer = new char[bufferHeight, bufferWidth];
        var visibilityBuffer = new TileVisibility[bufferHeight, bufferWidth];

        // Initialize buffers
        for (var y = 0; y < bufferHeight; y++)
        {
            for (var x = 0; x < bufferWidth; x++)
            {
                buffer[y, x] = '.'; // Floor/background character
                visibilityBuffer[y, x] = TileVisibility.Unexplored;
            }
        }

        // Render with fog of war if enabled
        if (hasFog && boundsComp != null)
        {
            RenderWithFogOfWar(world, buffer, visibilityBuffer, boundsComp, fogConfig!);
        }
        else
        {
            // Legacy rendering without fog
            RenderWithoutFog(buffer, boundsComp);
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

    private void RenderWithoutFog(char[,] buffer, ViewportBoundsComponent? boundsComp)
    {
        int bufferWidth = buffer.GetLength(1);
        int bufferHeight = buffer.GetLength(0);

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
    }

    private void RenderWithFogOfWar(
        IWorld world,
        char[,] buffer,
        TileVisibility[,] visibilityBuffer,
        ViewportBoundsComponent boundsComp,
        FogOfWarConfig fogConfig)
    {
        int bufferWidth = buffer.GetLength(1);
        int bufferHeight = buffer.GetLength(0);
        var bounds = boundsComp.Bounds;

        // Get player's map memory and perception results
        MapMemoryComponent? mapMemory = null;
        HashSet<(int, int)>? visibleTiles = null;

        // Query for player with perception results and map memory
        var playerQuery = world.CreateQuery<PlayerComponent, PerceptionResultsComponent, MapMemoryComponent>();
        var playerEntity = playerQuery.FirstOrDefault();

        if (playerEntity != default)
        {
            var perceptionResults = world.GetComponent<PerceptionResultsComponent>(playerEntity);
            mapMemory = world.GetComponent<MapMemoryComponent>(playerEntity);

            // Extract visible tiles from perception results ("vision" type)
            if (perceptionResults.DetectedPositionsByType.TryGetValue("vision", out var visionTiles))
            {
                visibleTiles = new HashSet<(int, int)>();
                foreach (var point in visionTiles)
                {
                    visibleTiles.Add((point.X, point.Y));
                    mapMemory.MarkExplored(point.X, point.Y);
                }
            }
        }

        // Render map tiles with visibility
        for (int worldY = bounds.MinY; worldY <= bounds.MaxY; worldY++)
        {
            for (int worldX = bounds.MinX; worldX <= bounds.MaxX; worldX++)
            {
                var (vx, vy) = bounds.WorldToViewport(worldX, worldY);

                if (vx < 0 || vx >= bufferWidth || vy < 0 || vy >= bufferHeight)
                    continue;

                // Determine visibility
                var visibility = GetTileVisibility(worldX, worldY, mapMemory, visibleTiles, fogConfig);
                visibilityBuffer[vy, vx] = visibility;

                // Render based on visibility
                switch (visibility)
                {
                    case TileVisibility.Unexplored:
                        buffer[vy, vx] = fogConfig.UnexploredGlyph;
                        break;

                    case TileVisibility.Explored:
                        buffer[vy, vx] = GetExploredGlyph(worldX, worldY);
                        break;

                    case TileVisibility.Visible:
                        buffer[vy, vx] = GetVisibleGlyph(worldX, worldY);
                        break;
                }
            }
        }

        // Render entities (only if visible)
        foreach (var (pos, render) in _renderBuffer)
        {
            var (vx, vy) = bounds.WorldToViewport(pos.X, pos.Y);

            if (vx < 0 || vx >= bufferWidth || vy < 0 || vy >= bufferHeight)
                continue;

            var visibility = visibilityBuffer[vy, vx];

            // Only show entities in visible tiles (or if ShowEntitiesInMemory is true)
            if (visibility == TileVisibility.Visible)
            {
                buffer[vy, vx] = render.Symbol;
            }
            else if (visibility == TileVisibility.Explored && fogConfig.ShowEntitiesInMemory)
            {
                buffer[vy, vx] = render.Symbol;
            }
            else
            {
                LastFrameFogCulledCount++;
            }
        }
    }

    private TileVisibility GetTileVisibility(
        int x, int y,
        MapMemoryComponent? mapMemory,
        HashSet<(int, int)>? visibleTiles,
        FogOfWarConfig fogConfig)
    {
        if (fogConfig.Mode == FogMode.None)
            return TileVisibility.Visible;

        // Check if currently visible
        bool isVisible = visibleTiles != null && visibleTiles.Contains((x, y));

        if (isVisible)
            return TileVisibility.Visible;

        if (fogConfig.Mode == FogMode.OnlyVisible)
            return TileVisibility.Unexplored;

        // Check if explored
        if (mapMemory != null && mapMemory.IsExplored(x, y))
            return TileVisibility.Explored;

        return TileVisibility.Unexplored;
    }

    private char GetVisibleGlyph(int x, int y)
    {
        // TODO: Get from map when available
        return '.';
    }

    private char GetExploredGlyph(int x, int y)
    {
        // TODO: Get from map when available
        // Could be dimmed version or simplified glyph
        return '·'; // Middle dot for explored tiles
    }

    public void SetRenderDimensions(int width, int height)
    {
        _renderWidth = Math.Max(1, width);
        _renderHeight = Math.Max(1, height);
    }
}

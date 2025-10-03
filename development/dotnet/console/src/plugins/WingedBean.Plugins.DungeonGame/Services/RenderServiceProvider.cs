using System.Collections.Generic;
using System.Linq;
using WingedBean.Contracts.Core;
using WingedBean.Contracts.Game;

namespace WingedBean.Plugins.DungeonGame.Services;

/// <summary>
/// Tier 4 Provider: ASCII rendering implementation for game world.
/// Part of RFC-0018: Render and UI Services for Console Profile.
/// </summary>
[Plugin(
    Name = "RenderServiceProvider",
    Provides = new[] { typeof(IRenderService) },
    Priority = 100
)]
public class RenderServiceProvider : IRenderService
{
    private RenderMode _currentMode = RenderMode.Color; // Enable colors by default
    
    public RenderBuffer Render(
        IReadOnlyList<EntitySnapshot> entitySnapshots,
        int width,
        int height)
    {
        // Create buffer filled with floor tiles
        var cells = new char[height, width];
        var foregroundColors = new Dictionary<(int X, int Y), System.ConsoleColor>();
        var backgroundColors = new Dictionary<(int X, int Y), System.ConsoleColor>();
        
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                cells[y, x] = '.';
                // Floor is dark gray on black
                if (_currentMode == RenderMode.Color || _currentMode == RenderMode.TrueColor)
                {
                    foregroundColors[(x, y)] = System.ConsoleColor.DarkGray;
                    backgroundColors[(x, y)] = System.ConsoleColor.Black;
                }
            }
        }
        
        // Sort entities by render layer (lower layers render first)
        var sorted = entitySnapshots.OrderBy(e => e.RenderLayer).ToList();
        
        // Render entities with colors
        foreach (var entity in sorted)
        {
            // Scale world coordinates to buffer
            // World is ~80x24, buffer varies based on view size
            int bufX = entity.Position.X * width / 80;
            int bufY = entity.Position.Y * height / 24;
            
            if (bufX >= 0 && bufX < width && bufY >= 0 && bufY < height)
            {
                cells[bufY, bufX] = entity.Symbol;
                
                // Add colors if in color mode
                if (_currentMode == RenderMode.Color || _currentMode == RenderMode.TrueColor)
                {
                    foregroundColors[(bufX, bufY)] = entity.ForegroundColor;
                    backgroundColors[(bufX, bufY)] = entity.BackgroundColor;
                }
            }
        }
        
        // Return buffer with colors if in color mode
        if (_currentMode == RenderMode.Color || _currentMode == RenderMode.TrueColor)
        {
            return new RenderBuffer(cells, foregroundColors, backgroundColors);
        }
        else
        {
            return new RenderBuffer(cells);
        }
    }
    
    public void SetRenderMode(RenderMode mode)
    {
        _currentMode = mode;
    }
    
    public RenderMode CurrentMode => _currentMode;
}

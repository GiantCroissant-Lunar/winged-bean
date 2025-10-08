using System.Text;
using Plate.CrossMilo.Contracts.ECS;
using WingedBean.Plugins.DungeonGame.Components;

// Type alias for backward compatibility during namespace migration
using IECSService = Plate.CrossMilo.Contracts.ECS.Services.IService;

namespace WingedBean.Plugins.DungeonGame.Systems;

/// <summary>
/// System responsible for rendering entities with Renderable components.
/// Renders to a text buffer that can be displayed by Terminal.Gui or console.
/// </summary>
public class RenderSystem : IECSSystem
{
    private readonly List<(Position pos, Renderable render)> _renderBuffer = new();
    
    // Public property to access the last rendered frame
    public string? LastRenderedFrame { get; private set; }
    
    // Render dimensions
    private int _renderWidth = 40;
    private int _renderHeight = 15;
    
    public void Execute(IECSService ecs, IWorld world, float deltaTime)
    {
        // Clear the render buffer
        _renderBuffer.Clear();

        // Collect all renderable entities
        foreach (var entity in world.CreateQuery<Position, Renderable>())
        {
            var pos = world.GetComponent<Position>(entity);
            var render = world.GetComponent<Renderable>(entity);
            _renderBuffer.Add((pos, render));
        }

        // Sort by render layer (lower layers render first)
        _renderBuffer.Sort((a, b) => a.render.RenderLayer.CompareTo(b.render.RenderLayer));

        // Render to text buffer instead of console
        LastRenderedFrame = RenderToTextBuffer();
    }
    
    private string RenderToTextBuffer()
    {
        // Create a 2D buffer filled with empty spaces
        var buffer = new char[_renderHeight, _renderWidth];
        for (int y = 0; y < _renderHeight; y++)
        {
            for (int x = 0; x < _renderWidth; x++)
            {
                buffer[y, x] = '.'; // Floor/background character
            }
        }
        
        // Draw entities into the buffer
        foreach (var (pos, render) in _renderBuffer)
        {
            // Only render if within buffer bounds
            if (pos.X >= 0 && pos.X < _renderWidth &&
                pos.Y >= 0 && pos.Y < _renderHeight)
            {
                buffer[pos.Y, pos.X] = render.Symbol;
            }
        }
        
        // Convert buffer to string
        var sb = new StringBuilder();
        for (int y = 0; y < _renderHeight; y++)
        {
            for (int x = 0; x < _renderWidth; x++)
            {
                sb.Append(buffer[y, x]);
            }
            if (y < _renderHeight - 1)
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

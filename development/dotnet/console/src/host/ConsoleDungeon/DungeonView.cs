using Terminal.Gui;
using WingedBean.Contracts.ECS;
using ConsoleDungeon.Components;

namespace ConsoleDungeon;

/// <summary>
/// Terminal.Gui v2 custom view that renders the dungeon crawler game.
/// This view integrates the ECS-based game world with Terminal.Gui's rendering system.
/// </summary>
public class DungeonView : View
{
    private readonly DungeonGame _game;
    private const int DungeonWidth = 80;
    private const int DungeonHeight = 24;
    private char[,] _renderBuffer = new char[DungeonHeight, DungeonWidth];
    private Terminal.Gui.Attribute[,] _colorBuffer = new Terminal.Gui.Attribute[DungeonHeight, DungeonWidth];

    public DungeonView(DungeonGame game)
    {
        _game = game ?? throw new ArgumentNullException(nameof(game));

        // Set view dimensions
        Width = DungeonWidth;
        Height = DungeonHeight;
        CanFocus = true;

        // Initialize buffers
        ClearBuffers();
    }

    private void ClearBuffers()
    {
        var emptyAttr = new Terminal.Gui.Attribute(Color.Gray, Color.Black);
        for (int y = 0; y < DungeonHeight; y++)
        {
            for (int x = 0; x < DungeonWidth; x++)
            {
                _renderBuffer[y, x] = '.';
                _colorBuffer[y, x] = emptyAttr;
            }
        }
    }

    public override void OnDrawContent(Rect contentArea)
    {
        base.OnDrawContent(contentArea);

        if (_game.World == null)
            return;

        // Clear buffers
        ClearBuffers();

        // Collect all renderable entities
        var renderables = new List<(Position pos, Renderable render)>();
        foreach (var entity in _game.World.CreateQuery<Position, Renderable>())
        {
            var pos = _game.World.GetComponent<Position>(entity);
            var render = _game.World.GetComponent<Renderable>(entity);
            renderables.Add((pos, render));
        }

        // Sort by render layer and populate buffers
        foreach (var (pos, render) in renderables.OrderBy(r => r.render.RenderLayer))
        {
            if (pos.X >= 0 && pos.X < DungeonWidth && pos.Y >= 0 && pos.Y < DungeonHeight)
            {
                _renderBuffer[pos.Y, pos.X] = render.Symbol;
                _colorBuffer[pos.Y, pos.X] = new Terminal.Gui.Attribute(
                    MapConsoleColor(render.ForegroundColor),
                    MapConsoleColor(render.BackgroundColor)
                );
            }
        }

        // Draw the buffers to the view
        for (int y = 0; y < Math.Min(DungeonHeight, contentArea.Height); y++)
        {
            for (int x = 0; x < Math.Min(DungeonWidth, contentArea.Width); x++)
            {
                Driver.SetAttribute(_colorBuffer[y, x]);
                Driver.Move(x, y);
                Driver.AddRune(_renderBuffer[y, x]);
            }
        }
    }

    private Color MapConsoleColor(ConsoleColor color)
    {
        return color switch
        {
            ConsoleColor.Black => Color.Black,
            ConsoleColor.DarkBlue => Color.Blue,
            ConsoleColor.DarkGreen => Color.Green,
            ConsoleColor.DarkCyan => Color.Cyan,
            ConsoleColor.DarkRed => Color.Red,
            ConsoleColor.DarkMagenta => Color.Magenta,
            ConsoleColor.DarkYellow => Color.Brown,
            ConsoleColor.Gray => Color.Gray,
            ConsoleColor.DarkGray => Color.DarkGray,
            ConsoleColor.Blue => Color.BrightBlue,
            ConsoleColor.Green => Color.BrightGreen,
            ConsoleColor.Cyan => Color.BrightCyan,
            ConsoleColor.Red => Color.BrightRed,
            ConsoleColor.Magenta => Color.BrightMagenta,
            ConsoleColor.Yellow => Color.BrightYellow,
            ConsoleColor.White => Color.White,
            _ => Color.White
        };
    }

    /// <summary>
    /// Refresh the dungeon view (call after game update).
    /// </summary>
    public void Refresh()
    {
        SetNeedsDisplay();
    }
}

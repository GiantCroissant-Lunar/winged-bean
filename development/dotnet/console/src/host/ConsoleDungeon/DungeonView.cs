using System.Collections.Generic;
using System.Linq;
using Terminal.Gui;
using GuiAttribute = Terminal.Gui.Attribute;
using WingedBean.Contracts.Game;

namespace ConsoleDungeon;

/// <summary>
/// Terminal.Gui view that renders entity snapshots produced by the gameplay plugin.
/// </summary>
public sealed class DungeonView : View
{
    private const int DungeonWidth = 80;
    private const int DungeonHeight = 24;

    private readonly Func<IReadOnlyList<EntitySnapshot>> _entityProvider;
    private readonly char[,] _renderBuffer = new char[DungeonHeight, DungeonWidth];
    private readonly GuiAttribute[,] _colorBuffer = new GuiAttribute[DungeonHeight, DungeonWidth];

    public DungeonView(Func<IReadOnlyList<EntitySnapshot>> entityProvider)
    {
        _entityProvider = entityProvider ?? throw new ArgumentNullException(nameof(entityProvider));

        Width = DungeonWidth;
        Height = DungeonHeight;
        CanFocus = true;
    }

    public override void OnDrawContent(Rect contentArea)
    {
        base.OnDrawContent(contentArea);

        ClearBuffers();

        var entities = _entityProvider();
        foreach (var snapshot in entities.OrderBy(e => e.RenderLayer))
        {
            var pos = snapshot.Position;
            if (pos.X < 0 || pos.X >= DungeonWidth || pos.Y < 0 || pos.Y >= DungeonHeight)
            {
                continue;
            }

            _renderBuffer[pos.Y, pos.X] = snapshot.Symbol;
            _colorBuffer[pos.Y, pos.X] = new GuiAttribute(
                MapConsoleColor(snapshot.ForegroundColor),
                MapConsoleColor(snapshot.BackgroundColor));
        }

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

    private void ClearBuffers()
    {
        var defaultAttribute = new GuiAttribute(Color.Gray, Color.Black);
        for (int y = 0; y < DungeonHeight; y++)
        {
            for (int x = 0; x < DungeonWidth; x++)
            {
                _renderBuffer[y, x] = '.';
                _colorBuffer[y, x] = defaultAttribute;
            }
        }
    }

    private static Color MapConsoleColor(ConsoleColor color) => color switch
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

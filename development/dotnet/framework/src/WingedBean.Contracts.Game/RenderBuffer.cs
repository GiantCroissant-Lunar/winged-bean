using System;
using System.Collections.Generic;
using System.Text;

namespace WingedBean.Contracts.Game;

/// <summary>
/// Rendered game world buffer.
/// Contains character data and optional color information.
/// Part of RFC-0018: Render and UI Services for Console Profile.
/// </summary>
public record RenderBuffer(
    char[,] Cells,
    Dictionary<(int X, int Y), ConsoleColor>? ForegroundColors = null,
    Dictionary<(int X, int Y), ConsoleColor>? BackgroundColors = null
)
{
    /// <summary>
    /// Convert buffer to string with ANSI color codes (for Terminal.Gui TextView).
    /// </summary>
    public string ToText()
    {
        int height = Cells.GetLength(0);
        int width = Cells.GetLength(1);
        var sb = new StringBuilder();
        
        // If we have colors, use ANSI codes
        bool hasColors = ForegroundColors != null && ForegroundColors.Count > 0;
        
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                if (hasColors && ForegroundColors!.TryGetValue((x, y), out var fg))
                {
                    // Add ANSI color code
                    sb.Append($"\x1b[{GetAnsiColorCode(fg)}m");
                    sb.Append(Cells[y, x]);
                    sb.Append("\x1b[0m"); // Reset color
                }
                else
                {
                    sb.Append(Cells[y, x]);
                }
            }
            if (y < height - 1)
            {
                sb.AppendLine();
            }
        }
        
        return sb.ToString();
    }
    
    /// <summary>
    /// Map ConsoleColor to ANSI color code.
    /// </summary>
    private static int GetAnsiColorCode(ConsoleColor color)
    {
        return color switch
        {
            ConsoleColor.Black => 30,
            ConsoleColor.DarkRed => 31,
            ConsoleColor.DarkGreen => 32,
            ConsoleColor.DarkYellow => 33,
            ConsoleColor.DarkBlue => 34,
            ConsoleColor.DarkMagenta => 35,
            ConsoleColor.DarkCyan => 36,
            ConsoleColor.Gray => 37,
            ConsoleColor.DarkGray => 90,
            ConsoleColor.Red => 91,
            ConsoleColor.Green => 92,
            ConsoleColor.Yellow => 93,
            ConsoleColor.Blue => 94,
            ConsoleColor.Magenta => 95,
            ConsoleColor.Cyan => 96,
            ConsoleColor.White => 97,
            _ => 37 // Default to gray
        };
    }
    
    /// <summary>
    /// Get buffer dimensions (width, height).
    /// </summary>
    public (int Width, int Height) GetDimensions()
    {
        return (Cells.GetLength(1), Cells.GetLength(0));
    }
}

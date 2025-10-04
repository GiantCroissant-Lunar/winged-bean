using System.Linq;
using WingedBean.Contracts.FigmaSharp;

namespace WingedBean.FigmaSharp.Core;

/// <summary>
/// Builds style data from Figma objects
/// </summary>
internal class StyleBuilder
{
    public StyleData BuildStyleData(FObject figma)
    {
        var style = new StyleData();
        
        // Background color from fills
        if (figma.Fills != null && figma.Fills.Count > 0)
        {
            var firstFill = figma.Fills.FirstOrDefault(f => f.Visible);
            if (firstFill != null)
            {
                style.BackgroundColor = firstFill.Color;
            }
        }
        
        // Border from strokes
        if (figma.Strokes != null && figma.Strokes.Count > 0 && figma.StrokeWeight > 0)
        {
            var firstStroke = figma.Strokes.FirstOrDefault(s => s.Visible);
            if (firstStroke != null)
            {
                style.Border = new BorderStyle
                {
                    Width = figma.StrokeWeight,
                    Color = firstStroke.Color
                };
            }
        }
        
        // Shadow from effects
        if (figma.Effects != null && figma.Effects.Count > 0)
        {
            var shadow = figma.Effects.FirstOrDefault(e => 
                e.Type == "DROP_SHADOW" && e.Visible);
            
            if (shadow != null)
            {
                style.Shadow = new ShadowStyle
                {
                    OffsetX = shadow.Offset.X,
                    OffsetY = shadow.Offset.Y,
                    Blur = shadow.Radius,
                    Color = shadow.Color
                };
            }
        }
        
        // Text style
        if (figma.Type == NodeType.TEXT)
        {
            style.Text = new TextStyle
            {
                Content = figma.Characters ?? string.Empty,
                FontFamily = figma.Style?.FontFamily ?? "Arial",
                FontSize = figma.Style?.FontSize ?? 14,
                Bold = (figma.Style?.FontWeight ?? 400) >= 600,
                Italic = figma.Style?.Italic ?? false,
                Color = figma.Fills?.FirstOrDefault(f => f.Visible)?.Color ?? Color.Black,
                HorizontalAlign = MapHorizontalAlign(figma.Style?.TextAlignHorizontal),
                VerticalAlign = MapVerticalAlign(figma.Style?.TextAlignVertical)
            };
        }
        
        return style;
    }
    
    private HorizontalAlign MapHorizontalAlign(string figmaAlign)
    {
        return figmaAlign?.ToUpperInvariant() switch
        {
            "LEFT" => HorizontalAlign.Left,
            "CENTER" => HorizontalAlign.Center,
            "RIGHT" => HorizontalAlign.Right,
            _ => HorizontalAlign.Left
        };
    }
    
    private VerticalAlign MapVerticalAlign(string figmaAlign)
    {
        return figmaAlign?.ToUpperInvariant() switch
        {
            "TOP" => VerticalAlign.Top,
            "CENTER" => VerticalAlign.Center,
            "BOTTOM" => VerticalAlign.Bottom,
            _ => VerticalAlign.Top
        };
    }
}

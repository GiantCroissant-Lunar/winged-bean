namespace WingedBean.Contracts.FigmaSharp;

/// <summary>
/// Framework-agnostic style data
/// </summary>
public class StyleData
{
    public Color BackgroundColor { get; set; }
    public Color ForegroundColor { get; set; }
    public BorderStyle? Border { get; set; }
    public ShadowStyle? Shadow { get; set; }
    public TextStyle? Text { get; set; }
}

/// <summary>
/// Border styling
/// </summary>
public class BorderStyle
{
    public float Width { get; set; }
    public Color Color { get; set; }
    public float TopLeftRadius { get; set; }
    public float TopRightRadius { get; set; }
    public float BottomLeftRadius { get; set; }
    public float BottomRightRadius { get; set; }
}

/// <summary>
/// Shadow styling
/// </summary>
public class ShadowStyle
{
    public float OffsetX { get; set; }
    public float OffsetY { get; set; }
    public float Blur { get; set; }
    public Color Color { get; set; }
}

/// <summary>
/// Text styling
/// </summary>
public class TextStyle
{
    public string Content { get; set; } = string.Empty;
    public string FontFamily { get; set; } = "Arial";
    public float FontSize { get; set; } = 14;
    public bool Bold { get; set; }
    public bool Italic { get; set; }
    public Color Color { get; set; }
    public HorizontalAlign HorizontalAlign { get; set; }
    public VerticalAlign VerticalAlign { get; set; }
}

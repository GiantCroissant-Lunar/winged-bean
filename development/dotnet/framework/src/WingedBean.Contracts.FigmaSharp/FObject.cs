using System.Collections.Generic;

namespace WingedBean.Contracts.FigmaSharp;

/// <summary>
/// Figma object model (ported from D.A. Assets)
/// Represents a node in the Figma document tree
/// </summary>
public class FObject
{
    // Identity
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public NodeType Type { get; set; }
    public bool Visible { get; set; } = true;
    
    // Layout
    public Vector2 Size { get; set; }
    public BoundingBox? AbsoluteBoundingBox { get; set; }
    public LayoutMode LayoutMode { get; set; }
    public LayoutWrap LayoutWrap { get; set; }
    public PrimaryAxisAlignItem PrimaryAxisAlignItems { get; set; }
    public CounterAxisAlignItem CounterAxisAlignItems { get; set; }
    public PrimaryAxisSizingMode PrimaryAxisSizingMode { get; set; }
    public CounterAxisSizingMode CounterAxisSizingMode { get; set; }
    public LayoutAlign LayoutAlign { get; set; }
    public LayoutPositioning LayoutPositioning { get; set; }
    
    // Spacing
    public float? ItemSpacing { get; set; }
    public float? CounterAxisSpacing { get; set; }
    public float? PaddingLeft { get; set; }
    public float? PaddingRight { get; set; }
    public float? PaddingTop { get; set; }
    public float? PaddingBottom { get; set; }
    
    // Hierarchy
    public List<FObject>? Children { get; set; }
    public FObject? Parent { get; set; }
    
    // Styling
    public List<Paint>? Fills { get; set; }
    public List<Paint>? Strokes { get; set; }
    public float StrokeWeight { get; set; }
    public List<Effect>? Effects { get; set; }
    
    // Text
    public string? Characters { get; set; }
    public TypeStyle? Style { get; set; }
    
    // Other
    public float? LayoutGrow { get; set; }
    public float Rotation { get; set; }
}

/// <summary>
/// Bounding box for Figma objects
/// </summary>
public struct BoundingBox
{
    public float X { get; set; }
    public float Y { get; set; }
    public float Width { get; set; }
    public float Height { get; set; }
    
    public BoundingBox(float x, float y, float width, float height)
    {
        X = x;
        Y = y;
        Width = width;
        Height = height;
    }
}

/// <summary>
/// Paint (fill or stroke)
/// </summary>
public class Paint
{
    public string Type { get; set; } = "SOLID";
    public bool Visible { get; set; } = true;
    public Color Color { get; set; }
    public float Opacity { get; set; } = 1.0f;
}

/// <summary>
/// Visual effect (shadow, blur, etc.)
/// </summary>
public class Effect
{
    public string Type { get; set; } = string.Empty;
    public bool Visible { get; set; } = true;
    public Vector2 Offset { get; set; }
    public float Radius { get; set; }
    public Color Color { get; set; }
}

/// <summary>
/// Text style properties
/// </summary>
public class TypeStyle
{
    public string FontFamily { get; set; } = "Arial";
    public float FontSize { get; set; } = 14;
    public float FontWeight { get; set; } = 400;
    public string TextAlignHorizontal { get; set; } = "LEFT";
    public string TextAlignVertical { get; set; } = "TOP";
    public bool Italic { get; set; }
}

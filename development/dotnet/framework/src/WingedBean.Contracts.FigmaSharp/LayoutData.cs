namespace WingedBean.Contracts.FigmaSharp;

/// <summary>
/// Framework-agnostic layout data
/// </summary>
public class LayoutData
{
    // Position
    public PositionMode PositionMode { get; set; }
    public Vector2 AbsolutePosition { get; set; }
    public AnchorData? Anchors { get; set; }
    public AlignmentData? Alignment { get; set; }
    
    // Size
    public SizeMode WidthMode { get; set; }
    public SizeMode HeightMode { get; set; }
    public float FixedWidth { get; set; }
    public float FixedHeight { get; set; }
    public float PercentWidth { get; set; }
    public float PercentHeight { get; set; }
    
    // Auto Layout
    public AutoLayoutData? AutoLayout { get; set; }
    
    // Spacing
    public Padding Padding { get; set; }
    public Margin Margin { get; set; }
}

/// <summary>
/// Position mode for UI elements
/// </summary>
public enum PositionMode
{
    Absolute,      // Fixed position
    Relative,      // Relative to parent
    Anchored,      // Anchored to edges
    AutoLayout     // Managed by parent's auto layout
}

/// <summary>
/// Size mode for width/height
/// </summary>
public enum SizeMode
{
    Fixed,         // Fixed size in pixels
    Fill,          // Fill available space
    Auto,          // Size to content
    Percent        // Percentage of parent
}

/// <summary>
/// Auto-layout configuration
/// </summary>
public class AutoLayoutData
{
    public LayoutDirection Direction { get; set; }
    public bool WrapEnabled { get; set; }
    public float Spacing { get; set; }
    public PrimaryAxisAlign PrimaryAlign { get; set; }
    public CrossAxisAlign CrossAlign { get; set; }
    public SizingMode PrimarySizing { get; set; }
    public SizingMode CrossSizing { get; set; }
    public Padding? Padding { get; set; }
}

/// <summary>
/// Layout direction
/// </summary>
public enum LayoutDirection
{
    Horizontal,
    Vertical,
    None
}

/// <summary>
/// Primary axis alignment (main axis)
/// </summary>
public enum PrimaryAxisAlign
{
    Start,
    Center,
    End,
    SpaceBetween
}

/// <summary>
/// Cross axis alignment
/// </summary>
public enum CrossAxisAlign
{
    Start,
    Center,
    End,
    Stretch,
    Baseline
}

/// <summary>
/// Sizing mode for auto-layout
/// </summary>
public enum SizingMode
{
    Fixed,
    Auto
}

/// <summary>
/// Padding (inner spacing)
/// </summary>
public struct Padding
{
    public float Left { get; set; }
    public float Right { get; set; }
    public float Top { get; set; }
    public float Bottom { get; set; }
    
    public Padding(float all)
    {
        Left = Right = Top = Bottom = all;
    }
    
    public Padding(float horizontal, float vertical)
    {
        Left = Right = horizontal;
        Top = Bottom = vertical;
    }
    
    public Padding(float left, float right, float top, float bottom)
    {
        Left = left;
        Right = right;
        Top = top;
        Bottom = bottom;
    }
}

/// <summary>
/// Margin (outer spacing)
/// </summary>
public struct Margin
{
    public float Left { get; set; }
    public float Right { get; set; }
    public float Top { get; set; }
    public float Bottom { get; set; }
    
    public Margin(float all)
    {
        Left = Right = Top = Bottom = all;
    }
    
    public Margin(float horizontal, float vertical)
    {
        Left = Right = horizontal;
        Top = Bottom = vertical;
    }
    
    public Margin(float left, float right, float top, float bottom)
    {
        Left = left;
        Right = right;
        Top = top;
        Bottom = bottom;
    }
}

/// <summary>
/// Anchor data for positioning
/// </summary>
public class AnchorData
{
    public bool Left { get; set; }
    public bool Right { get; set; }
    public bool Top { get; set; }
    public bool Bottom { get; set; }
}

/// <summary>
/// Alignment data
/// </summary>
public class AlignmentData
{
    public HorizontalAlign Horizontal { get; set; }
    public VerticalAlign Vertical { get; set; }
}

/// <summary>
/// Horizontal alignment
/// </summary>
public enum HorizontalAlign
{
    Left,
    Center,
    Right
}

/// <summary>
/// Vertical alignment
/// </summary>
public enum VerticalAlign
{
    Top,
    Center,
    Bottom
}

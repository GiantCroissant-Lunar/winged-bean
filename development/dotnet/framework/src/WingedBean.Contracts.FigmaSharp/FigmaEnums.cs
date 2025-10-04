namespace WingedBean.Contracts.FigmaSharp;

/// <summary>
/// Figma node types
/// </summary>
public enum NodeType
{
    DOCUMENT,
    CANVAS,
    FRAME,
    GROUP,
    VECTOR,
    BOOLEAN_OPERATION,
    STAR,
    LINE,
    ELLIPSE,
    REGULAR_POLYGON,
    RECTANGLE,
    TEXT,
    SLICE,
    COMPONENT,
    COMPONENT_SET,
    INSTANCE,
    STICKY,
    SHAPE_WITH_TEXT,
    CONNECTOR
}

/// <summary>
/// Figma layout mode (auto-layout direction)
/// </summary>
public enum LayoutMode
{
    NONE,
    HORIZONTAL,
    VERTICAL
}

/// <summary>
/// Figma layout wrap mode
/// </summary>
public enum LayoutWrap
{
    NO_WRAP,
    WRAP
}

/// <summary>
/// Primary axis alignment (main axis in flexbox terms)
/// </summary>
public enum PrimaryAxisAlignItem
{
    NONE,
    MIN,
    CENTER,
    MAX,
    SPACE_BETWEEN
}

/// <summary>
/// Counter axis alignment (cross axis in flexbox terms)
/// </summary>
public enum CounterAxisAlignItem
{
    NONE,
    MIN,
    CENTER,
    MAX,
    BASELINE
}

/// <summary>
/// Primary axis sizing mode
/// </summary>
public enum PrimaryAxisSizingMode
{
    FIXED,
    AUTO
}

/// <summary>
/// Counter axis sizing mode
/// </summary>
public enum CounterAxisSizingMode
{
    FIXED,
    AUTO
}

/// <summary>
/// Layout alignment for children
/// </summary>
public enum LayoutAlign
{
    INHERIT,
    STRETCH,
    MIN,
    CENTER,
    MAX
}

/// <summary>
/// Layout positioning mode
/// </summary>
public enum LayoutPositioning
{
    AUTO,
    ABSOLUTE
}

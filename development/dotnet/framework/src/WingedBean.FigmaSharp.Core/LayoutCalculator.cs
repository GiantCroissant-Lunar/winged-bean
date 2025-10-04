using WingedBean.Contracts.FigmaSharp;

namespace WingedBean.FigmaSharp.Core;

/// <summary>
/// Calculates layout data from Figma objects
/// Ported from D.A. Assets TransformSetter.cs
/// </summary>
internal class LayoutCalculator
{
    private readonly AutoLayoutTransformer _autoLayoutTransformer;
    private readonly AlignmentMapper _alignmentMapper;
    private readonly SizeModeCalculator _sizeModeCalculator;
    private readonly PaddingCalculator _paddingCalculator;
    
    public LayoutCalculator()
    {
        _autoLayoutTransformer = new AutoLayoutTransformer();
        _alignmentMapper = new AlignmentMapper();
        _sizeModeCalculator = new SizeModeCalculator();
        _paddingCalculator = new PaddingCalculator();
    }
    
    public LayoutData BuildLayoutData(FObject figma)
    {
        // Calculate global rect (position and size)
        var rect = CalculateGlobalRect(figma);
        
        var layout = new LayoutData
        {
            AbsolutePosition = rect.Position,
            FixedWidth = rect.Size.X,
            FixedHeight = rect.Size.Y
        };
        
        // Determine position mode
        if (figma.LayoutPositioning == LayoutPositioning.ABSOLUTE)
        {
            layout.PositionMode = PositionMode.Absolute;
        }
        else if (figma.Parent?.LayoutMode != LayoutMode.NONE)
        {
            layout.PositionMode = PositionMode.AutoLayout;
        }
        else
        {
            layout.PositionMode = PositionMode.Relative;
        }
        
        // Determine size modes
        layout.WidthMode = _sizeModeCalculator.DetermineSizeMode(figma, isWidth: true);
        layout.HeightMode = _sizeModeCalculator.DetermineSizeMode(figma, isWidth: false);
        
        // Auto layout
        if (figma.LayoutMode != LayoutMode.NONE)
        {
            layout.AutoLayout = _autoLayoutTransformer.BuildAutoLayoutData(figma);
        }
        
        // Alignment
        layout.Alignment = _alignmentMapper.GetAlignment(figma);
        
        // Padding
        layout.Padding = _paddingCalculator.CalculatePadding(figma, rect.Size);
        
        return layout;
    }
    
    /// <summary>
    /// Calculate global rect (position and size)
    /// Ported from D.A. Assets TransformSetter.GetGlobalRect()
    /// </summary>
    private FRect CalculateGlobalRect(FObject figma)
    {
        var rect = new FRect();
        
        // Get bounding box
        if (figma.AbsoluteBoundingBox.HasValue)
        {
            var bbox = figma.AbsoluteBoundingBox.Value;
            rect.Position = new Vector2(bbox.X, bbox.Y);
            rect.Size = new Vector2(bbox.Width, bbox.Height);
        }
        else
        {
            // Fallback to Size property
            rect.Position = Vector2.Zero;
            rect.Size = figma.Size;
        }
        
        // Handle rotation (simplified for now)
        // TODO: Port full rotation logic from D.A. Assets in Phase 2
        rect.Angle = figma.Rotation;
        
        return rect;
    }
}

/// <summary>
/// Internal struct for rect calculation
/// </summary>
internal struct FRect
{
    public Vector2 Position { get; set; }
    public Vector2 Size { get; set; }
    public float Angle { get; set; }
}

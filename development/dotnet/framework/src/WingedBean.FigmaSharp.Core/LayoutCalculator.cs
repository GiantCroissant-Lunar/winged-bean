using System;
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
        
        // Anchors (for constraint-based positioning)
        layout.Anchors = BuildAnchorData(figma);
        
        // Padding
        layout.Padding = _paddingCalculator.CalculatePadding(figma, rect.Size);
        
        return layout;
    }
    
    /// <summary>
    /// Build anchor data from Figma constraints
    /// </summary>
    private AnchorData? BuildAnchorData(FObject figma)
    {
        // Only apply anchors for absolute positioned elements with constraints
        if (figma.LayoutPositioning != LayoutPositioning.ABSOLUTE)
            return null;
        
        // In a full implementation, we would check figma.Constraints
        // For now, return null (constraints will be added in future enhancement)
        return null;
    }
    
    /// <summary>
    /// Calculate global rect (position and size)
    /// Ported from D.A. Assets TransformSetter.GetGlobalRect()
    /// </summary>
    private FRect CalculateGlobalRect(FObject figma)
    {
        var rect = new FRect();
        
        // Get bounding box and size
        bool hasBoundingBox = figma.AbsoluteBoundingBox.HasValue;
        Vector2 bSize = hasBoundingBox 
            ? new Vector2(figma.AbsoluteBoundingBox.Value.Width, figma.AbsoluteBoundingBox.Value.Height)
            : figma.Size;
        Vector2 bPos = hasBoundingBox
            ? new Vector2(figma.AbsoluteBoundingBox.Value.X, figma.AbsoluteBoundingBox.Value.Y)
            : Vector2.Zero;
        
        // Calculate rotation angles
        rect.Angle = figma.Rotation;
        rect.AbsoluteAngle = GetAbsoluteRotationAngle(figma);
        
        // Determine position and size based on rotation
        Vector2 position;
        Vector2 size;
        
        if (rect.AbsoluteAngle != 0)
        {
            // Rotated element - calculate offset
            size = figma.Size;
            var offset = CalculateRotationOffset(size.X, size.Y, rect.AbsoluteAngle);
            position = new Vector2(bPos.X + offset.X, bPos.Y + offset.Y);
        }
        else
        {
            // Normal element
            size = bSize;
            position = bPos;
        }
        
        rect.Size = size;
        rect.Position = position;
        
        return rect;
    }
    
    /// <summary>
    /// Calculate absolute rotation angle (including parent rotations)
    /// </summary>
    private float GetAbsoluteRotationAngle(FObject figma)
    {
        float angle = figma.Rotation;
        var current = figma.Parent;
        
        while (current != null)
        {
            angle += current.Rotation;
            current = current.Parent;
        }
        
        return angle;
    }
    
    /// <summary>
    /// Calculate position offset for rotated elements
    /// Ported from D.A. Assets rotation logic
    /// </summary>
    private Vector2 CalculateRotationOffset(float width, float height, float angleDegrees)
    {
        float radians = angleDegrees * (MathF.PI / 180f);
        float cos = MathF.Cos(radians);
        float sin = MathF.Sin(radians);
        
        float offsetX = (width * (1 - cos) + height * sin) / 2;
        float offsetY = (height * (1 - cos) - width * sin) / 2;
        
        return new Vector2(offsetX, offsetY);
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
    public float AbsoluteAngle { get; set; }
}

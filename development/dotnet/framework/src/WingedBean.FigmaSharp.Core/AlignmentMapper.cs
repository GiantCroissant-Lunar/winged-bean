using Plate.CrossMilo.Contracts.FigmaSharp;

namespace WingedBean.FigmaSharp.Core;

/// <summary>
/// Maps Figma alignment to framework-agnostic alignment
/// Ported from D.A. Assets AutoLayoutExtensions alignment logic
/// </summary>
internal class AlignmentMapper
{
    public AlignmentData? GetAlignment(FObject figma)
    {
        if (figma.Parent == null)
            return null;
        
        if (figma.Parent.LayoutMode == LayoutMode.HORIZONTAL)
        {
            return GetHorizontalLayoutAlignment(figma.Parent);
        }
        else if (figma.Parent.LayoutMode == LayoutMode.VERTICAL)
        {
            return GetVerticalLayoutAlignment(figma.Parent);
        }
        
        return null;
    }
    
    /// <summary>
    /// Get alignment for horizontal layout
    /// Ported from D.A. Assets GetHorLayoutAnchor()
    /// </summary>
    private AlignmentData GetHorizontalLayoutAlignment(FObject parent)
    {
        string key = $"{parent.PrimaryAxisAlignItems} {parent.CounterAxisAlignItems}";
        
        return key switch
        {
            "NONE NONE" => new AlignmentData { Horizontal = HorizontalAlign.Left, Vertical = VerticalAlign.Top },
            "SPACE_BETWEEN NONE" => new AlignmentData { Horizontal = HorizontalAlign.Center, Vertical = VerticalAlign.Top },
            "CENTER NONE" => new AlignmentData { Horizontal = HorizontalAlign.Center, Vertical = VerticalAlign.Top },
            "MAX NONE" => new AlignmentData { Horizontal = HorizontalAlign.Right, Vertical = VerticalAlign.Top },
            "NONE CENTER" => new AlignmentData { Horizontal = HorizontalAlign.Left, Vertical = VerticalAlign.Center },
            "CENTER CENTER" => new AlignmentData { Horizontal = HorizontalAlign.Center, Vertical = VerticalAlign.Center },
            "MAX CENTER" => new AlignmentData { Horizontal = HorizontalAlign.Right, Vertical = VerticalAlign.Center },
            "NONE MAX" => new AlignmentData { Horizontal = HorizontalAlign.Left, Vertical = VerticalAlign.Bottom },
            "CENTER MAX" => new AlignmentData { Horizontal = HorizontalAlign.Center, Vertical = VerticalAlign.Bottom },
            "MAX MAX" => new AlignmentData { Horizontal = HorizontalAlign.Right, Vertical = VerticalAlign.Bottom },
            _ => new AlignmentData { Horizontal = HorizontalAlign.Left, Vertical = VerticalAlign.Top }
        };
    }
    
    /// <summary>
    /// Get alignment for vertical layout
    /// Ported from D.A. Assets GetVertLayoutAnchor()
    /// </summary>
    private AlignmentData GetVerticalLayoutAlignment(FObject parent)
    {
        string key = $"{parent.PrimaryAxisAlignItems} {parent.CounterAxisAlignItems}";
        
        return key switch
        {
            "NONE NONE" => new AlignmentData { Horizontal = HorizontalAlign.Left, Vertical = VerticalAlign.Top },
            "NONE CENTER" => new AlignmentData { Horizontal = HorizontalAlign.Center, Vertical = VerticalAlign.Top },
            "NONE MAX" => new AlignmentData { Horizontal = HorizontalAlign.Right, Vertical = VerticalAlign.Top },
            "CENTER NONE" => new AlignmentData { Horizontal = HorizontalAlign.Left, Vertical = VerticalAlign.Center },
            "CENTER CENTER" => new AlignmentData { Horizontal = HorizontalAlign.Center, Vertical = VerticalAlign.Center },
            "CENTER MAX" => new AlignmentData { Horizontal = HorizontalAlign.Right, Vertical = VerticalAlign.Center },
            "MAX NONE" => new AlignmentData { Horizontal = HorizontalAlign.Left, Vertical = VerticalAlign.Bottom },
            "MAX CENTER" => new AlignmentData { Horizontal = HorizontalAlign.Center, Vertical = VerticalAlign.Bottom },
            "MAX MAX" => new AlignmentData { Horizontal = HorizontalAlign.Right, Vertical = VerticalAlign.Bottom },
            _ => new AlignmentData { Horizontal = HorizontalAlign.Left, Vertical = VerticalAlign.Top }
        };
    }
}

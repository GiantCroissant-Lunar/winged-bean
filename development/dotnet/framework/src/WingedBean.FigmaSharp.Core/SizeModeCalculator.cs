using Plate.CrossMilo.Contracts.FigmaSharp;

namespace WingedBean.FigmaSharp.Core;

/// <summary>
/// Determines size mode (Fixed, Fill, Auto, Percent) for UI elements
/// </summary>
internal class SizeModeCalculator
{
    public SizeMode DetermineSizeMode(FObject figma, bool isWidth)
    {
        // Check if child should grow (fill available space)
        if (figma.LayoutGrow.HasValue && figma.LayoutGrow.Value == 1)
        {
            return SizeMode.Fill;
        }
        
        // Check if child should stretch
        if (figma.LayoutAlign == LayoutAlign.STRETCH)
        {
            // Stretch applies to cross-axis
            bool isCrossAxis = figma.Parent?.LayoutMode == LayoutMode.HORIZONTAL ? !isWidth : isWidth;
            if (isCrossAxis)
            {
                return SizeMode.Fill;
            }
        }
        
        // Check parent's sizing mode
        if (figma.Parent != null)
        {
            // If parent is auto-sizing on this axis, child should be auto too
            if (isWidth && figma.Parent.PrimaryAxisSizingMode == PrimaryAxisSizingMode.AUTO)
            {
                return SizeMode.Auto;
            }
            
            if (!isWidth && figma.Parent.CounterAxisSizingMode == CounterAxisSizingMode.AUTO)
            {
                return SizeMode.Auto;
            }
        }
        
        // Default to fixed size
        return SizeMode.Fixed;
    }
}

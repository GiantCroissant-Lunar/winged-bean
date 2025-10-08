using System;
using System.Linq;
using Plate.CrossMilo.Contracts.FigmaSharp;

namespace WingedBean.FigmaSharp.Core;

/// <summary>
/// Calculates padding with adjustment for oversized children
/// Ported from D.A. Assets AdjustPadding logic
/// </summary>
internal class PaddingCalculator
{
    public Padding CalculatePadding(FObject figma, Vector2 parentSize)
    {
        var padding = new Padding(
            figma.PaddingLeft ?? 0,
            figma.PaddingRight ?? 0,
            figma.PaddingTop ?? 0,
            figma.PaddingBottom ?? 0
        );
        
        // If no children, return padding as-is
        if (figma.Children == null || figma.Children.Count == 0)
            return padding;
        
        // Find max child dimensions
        float maxChildWidth = figma.Children.Max(c => c.Size.X);
        float maxChildHeight = figma.Children.Max(c => c.Size.Y);
        
        float totalHorizontalPadding = padding.Left + padding.Right;
        float totalVerticalPadding = padding.Top + padding.Bottom;
        
        // Adjust horizontal padding if children exceed parent width
        if (maxChildWidth + totalHorizontalPadding > parentSize.X && totalHorizontalPadding != 0)
        {
            float excessWidth = (maxChildWidth + totalHorizontalPadding) - parentSize.X;
            float leftRatio = padding.Left / totalHorizontalPadding;
            float rightRatio = padding.Right / totalHorizontalPadding;
            
            padding.Left -= MathF.Ceiling(leftRatio * excessWidth);
            padding.Right -= MathF.Ceiling(rightRatio * excessWidth);
            
            // Ensure non-negative
            padding.Left = MathF.Max(0, padding.Left);
            padding.Right = MathF.Max(0, padding.Right);
        }
        
        // Adjust vertical padding if children exceed parent height
        if (maxChildHeight + totalVerticalPadding > parentSize.Y && totalVerticalPadding != 0)
        {
            float excessHeight = (maxChildHeight + totalVerticalPadding) - parentSize.Y;
            float topRatio = padding.Top / totalVerticalPadding;
            float bottomRatio = padding.Bottom / totalVerticalPadding;
            
            padding.Top -= MathF.Ceiling(topRatio * excessHeight);
            padding.Bottom -= MathF.Ceiling(bottomRatio * excessHeight);
            
            // Ensure non-negative
            padding.Top = MathF.Max(0, padding.Top);
            padding.Bottom = MathF.Max(0, padding.Bottom);
        }
        
        return padding;
    }
}

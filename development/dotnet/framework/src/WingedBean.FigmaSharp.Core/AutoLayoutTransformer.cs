using System;
using System.Linq;
using Plate.CrossMilo.Contracts.FigmaSharp;

namespace WingedBean.FigmaSharp.Core;

/// <summary>
/// Transforms Figma auto-layout to framework-agnostic auto-layout data
/// Ported from D.A. Assets AutoLayoutExtensions.cs
/// </summary>
internal class AutoLayoutTransformer
{
    public AutoLayoutData BuildAutoLayoutData(FObject figma)
    {
        var autoLayout = new AutoLayoutData
        {
            Direction = MapLayoutDirection(figma.LayoutMode),
            WrapEnabled = figma.LayoutWrap == LayoutWrap.WRAP,
            Spacing = CalculateSpacing(figma),
            PrimaryAlign = MapPrimaryAlign(figma.PrimaryAxisAlignItems),
            CrossAlign = MapCrossAlign(figma.CounterAxisAlignItems),
            PrimarySizing = figma.PrimaryAxisSizingMode == PrimaryAxisSizingMode.AUTO 
                ? SizingMode.Auto 
                : SizingMode.Fixed,
            CrossSizing = figma.CounterAxisSizingMode == CounterAxisSizingMode.AUTO 
                ? SizingMode.Auto 
                : SizingMode.Fixed,
            Padding = new Padding(
                figma.PaddingLeft ?? 0,
                figma.PaddingRight ?? 0,
                figma.PaddingTop ?? 0,
                figma.PaddingBottom ?? 0
            )
        };
        
        return autoLayout;
    }
    
    private LayoutDirection MapLayoutDirection(LayoutMode mode)
    {
        return mode switch
        {
            LayoutMode.HORIZONTAL => LayoutDirection.Horizontal,
            LayoutMode.VERTICAL => LayoutDirection.Vertical,
            _ => LayoutDirection.None
        };
    }
    
    /// <summary>
    /// Calculate spacing, handling SPACE_BETWEEN special case
    /// Enhanced with better edge case handling
    /// </summary>
    private float CalculateSpacing(FObject figma)
    {
        // Handle SPACE_BETWEEN distribution
        if (figma.PrimaryAxisAlignItems == PrimaryAxisAlignItem.SPACE_BETWEEN)
        {
            if (figma.Children == null || figma.Children.Count <= 1)
                return 0;
            
            // Filter only visible children
            var visibleChildren = figma.Children.Where(c => c.Visible).ToList();
            if (visibleChildren.Count <= 1)
                return 0;
            
            int childCount = visibleChildren.Count;
            int spacingCount = childCount - 1;
            
            // Calculate total space available
            float parentSize = figma.LayoutMode == LayoutMode.HORIZONTAL 
                ? figma.Size.X 
                : figma.Size.Y;
            
            // Subtract padding
            float paddingStart = figma.LayoutMode == LayoutMode.HORIZONTAL
                ? (figma.PaddingLeft ?? 0)
                : (figma.PaddingTop ?? 0);
            float paddingEnd = figma.LayoutMode == LayoutMode.HORIZONTAL
                ? (figma.PaddingRight ?? 0)
                : (figma.PaddingBottom ?? 0);
            
            parentSize -= (paddingStart + paddingEnd);
            
            // Calculate total children size (only visible children)
            float allChildrenSize = 0;
            foreach (var child in visibleChildren)
            {
                float childSize = figma.LayoutMode == LayoutMode.HORIZONTAL 
                    ? child.Size.X 
                    : child.Size.Y;
                allChildrenSize += childSize;
            }
            
            // Calculate spacing
            float availableSpace = parentSize - allChildrenSize;
            
            // Ensure non-negative spacing
            return spacingCount > 0 ? MathF.Max(0, availableSpace / spacingCount) : 0;
        }
        
        return figma.ItemSpacing ?? 0;
    }
    
    private PrimaryAxisAlign MapPrimaryAlign(PrimaryAxisAlignItem figmaAlign)
    {
        return figmaAlign switch
        {
            PrimaryAxisAlignItem.MIN => PrimaryAxisAlign.Start,
            PrimaryAxisAlignItem.CENTER => PrimaryAxisAlign.Center,
            PrimaryAxisAlignItem.MAX => PrimaryAxisAlign.End,
            PrimaryAxisAlignItem.SPACE_BETWEEN => PrimaryAxisAlign.SpaceBetween,
            _ => PrimaryAxisAlign.Start
        };
    }
    
    private CrossAxisAlign MapCrossAlign(CounterAxisAlignItem figmaAlign)
    {
        return figmaAlign switch
        {
            CounterAxisAlignItem.MIN => CrossAxisAlign.Start,
            CounterAxisAlignItem.CENTER => CrossAxisAlign.Center,
            CounterAxisAlignItem.MAX => CrossAxisAlign.End,
            CounterAxisAlignItem.BASELINE => CrossAxisAlign.Baseline,
            _ => CrossAxisAlign.Start
        };
    }
}

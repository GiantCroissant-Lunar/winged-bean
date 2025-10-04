---
id: RFC-0023
title: FigmaSharp Layout Transformation
status: Draft
category: architecture, ui, figma, layout
created: 2025-10-04
updated: 2025-10-04
---

# RFC-0023: FigmaSharp Layout Transformation

## Status

Draft

## Date

2025-10-04

## Summary

Define the **layout transformation algorithm** for converting Figma's auto-layout system to framework-agnostic layout data. This RFC specifies how to map Figma's layout properties (auto-layout, constraints, sizing modes) to an abstract layout model that can be rendered by any UI framework (Terminal.Gui, Unity UGUI, Godot UI).

**Key Focus**: Port D.A. Assets' layout calculation logic (`TransformSetter`, `AutoLayoutExtensions`) to plain C# and adapt it for multi-framework support.

## Motivation

### Vision

Create a **universal layout transformation engine** that:

1. **Understands Figma's layout model** (auto-layout, constraints, absolute positioning)
2. **Calculates framework-agnostic layout data** (position, size, alignment, spacing)
3. **Supports all layout modes** (horizontal, vertical, wrap, space-between)
4. **Handles edge cases** (rotation, nested auto-layouts, constraints)

### Problems to Solve

1. **Figma Auto-Layout ≠ Framework Layouts**
   - Figma: Flexbox-like auto-layout with primary/counter axis
   - Unity UGUI: `HorizontalLayoutGroup`, `VerticalLayoutGroup`
   - Terminal.Gui: Manual `Pos`/`Dim` calculation
   - Godot: Container nodes with layout modes

2. **Complex Layout Scenarios**
   - Nested auto-layouts (auto-layout inside auto-layout)
   - SPACE_BETWEEN distribution
   - LayoutGrow and LayoutAlign
   - Constraints and anchoring
   - Rotation and absolute positioning

3. **Framework-Specific Quirks**
   - Terminal.Gui: Character-based units (not pixels)
   - Unity: Bottom-left origin vs. top-left origin
   - Godot: Different anchor system

### Current D.A. Assets Approach

D.A. Assets has excellent layout logic but it's Unity-specific:

```csharp
// D.A. Assets TransformSetter.cs
public FRect GetGlobalRect(FObject fobject)
{
    // Calculate position and size
    // Uses Unity's RectTransform, Vector2, etc.
}

// D.A. Assets AutoLayoutExtensions.cs
public static TextAnchor GetHorLayoutAnchor(this FObject fobject)
{
    // Map Figma alignment to Unity TextAnchor
}
```

**We need to port this to plain C# and make it framework-agnostic.**

## Proposal

### Layout Transformation Pipeline

```
┌─────────────────────────────────────────────────────────────────┐
│                    Figma Layout Properties                      │
│  - LayoutMode (HORIZONTAL/VERTICAL/NONE)                        │
│  - PrimaryAxisAlignItems (MIN/CENTER/MAX/SPACE_BETWEEN)         │
│  - CounterAxisAlignItems (MIN/CENTER/MAX/STRETCH/BASELINE)      │
│  - ItemSpacing, PaddingLeft/Right/Top/Bottom                    │
│  - LayoutGrow, LayoutAlign                                      │
│  - PrimaryAxisSizingMode, CounterAxisSizingMode                 │
│  - AbsoluteBoundingBox, Constraints                             │
└─────────────────────────────────────────────────────────────────┘
                             ↓
┌─────────────────────────────────────────────────────────────────┐
│              Layout Calculator (Core Algorithm)                 │
│  1. Calculate global rect (position + size)                     │
│  2. Determine position mode (absolute/relative/auto-layout)     │
│  3. Determine size modes (fixed/fill/auto/percent)              │
│  4. Build auto-layout data (if applicable)                      │
│  5. Calculate padding and margins                               │
│  6. Handle constraints and anchoring                            │
└─────────────────────────────────────────────────────────────────┘
                             ↓
┌─────────────────────────────────────────────────────────────────┐
│           Abstract Layout Data (Framework-Agnostic)             │
│  - PositionMode: Absolute | Relative | Anchored | AutoLayout    │
│  - AbsolutePosition: Vector2(x, y)                              │
│  - SizeMode: Fixed | Fill | Auto | Percent                      │
│  - FixedWidth/Height, PercentWidth/Height                       │
│  - AutoLayoutData: Direction, Spacing, Alignment, Sizing        │
│  - Padding, Margin, Anchors                                     │
└─────────────────────────────────────────────────────────────────┘
                             ↓
┌─────────────────────────────────────────────────────────────────┐
│         Framework Renderers (Apply Layout)                      │
│  - Terminal.Gui: Pos.At(x), Dim.Fill(), Pos.Center()            │
│  - Unity UGUI: RectTransform.anchoredPosition, sizeDelta        │
│  - Godot: position, size, anchor_left/right/top/bottom          │
└─────────────────────────────────────────────────────────────────┘
```

### Core Algorithm: Global Rect Calculation

Port from D.A. Assets `TransformSetter.GetGlobalRect()`:

```csharp
namespace WingedBean.FigmaSharp.Core;

public class LayoutCalculator
{
    public FRect CalculateGlobalRect(FObject fobject)
    {
        var rect = new FRect();
        
        // Get bounding box and render bounds
        bool hasBoundingSize = TryGetBoundingSize(fobject, out Vector2 bSize);
        bool hasBoundingPos = TryGetBoundingPosition(fobject, out Vector2 bPos);
        bool hasRenderSize = TryGetRenderSize(fobject, out Vector2 rSize);
        bool hasRenderPos = TryGetRenderPosition(fobject, out Vector2 rPos);
        bool hasLocalPos = TryGetLocalPosition(fobject, out Vector2 lPos);
        
        // Calculate rotation
        rect.Angle = fobject.Data.FcuImageType == FcuImageType.Downloadable 
            ? 0 
            : GetFigmaRotationAngle(fobject);
        rect.AbsoluteAngle = fobject.Data.FcuImageType == FcuImageType.Downloadable 
            ? 0 
            : GetAbsoluteAngle(fobject);
        
        // Determine position and size based on state
        Vector2 position;
        Vector2 size;
        
        if (HasScaleInSpriteName(fobject, out float scale))
        {
            // State 1: Sprite with scale in name
            position = rPos;
            size = new Vector2(fobject.Data.SpriteSize.X / scale, fobject.Data.SpriteSize.Y / scale);
        }
        else if (rect.AbsoluteAngle != 0)
        {
            // State 2: Rotated element
            size = fobject.Size;
            var offset = CalculateRotationOffset(size.X, size.Y, rect.AbsoluteAngle);
            position = new Vector2(bPos.X + offset.X, bPos.Y + offset.Y);
        }
        else
        {
            // State 3: Normal element
            size = bSize;
            position = bPos;
        }
        
        // Fix size with stroke if needed
        if (TryFixSizeWithStroke(fobject, size.Y, out float newY))
        {
            size.Y = newY;
        }
        
        rect.Size = size;
        rect.Position = position;
        
        // Calculate padding
        rect.Padding = CalculatePadding(fobject, size);
        
        return rect;
    }
    
    private Vector2 CalculateRotationOffset(float width, float height, float angle)
    {
        // Port from D.A. Assets
        float radians = angle * (MathF.PI / 180f);
        float cos = MathF.Cos(radians);
        float sin = MathF.Sin(radians);
        
        float offsetX = (width * (1 - cos) + height * sin) / 2;
        float offsetY = (height * (1 - cos) - width * sin) / 2;
        
        return new Vector2(offsetX, offsetY);
    }
}
```

### Auto-Layout Transformation

Port from D.A. Assets `AutoLayoutExtensions`:

```csharp
namespace WingedBean.FigmaSharp.Core;

public class AutoLayoutTransformer
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
                : SizingMode.Fixed
        };
        
        return autoLayout;
    }
    
    private float CalculateSpacing(FObject figma)
    {
        // Handle SPACE_BETWEEN
        if (figma.PrimaryAxisAlignItems == PrimaryAxisAlignItem.SPACE_BETWEEN)
        {
            if (figma.Children == null || figma.Children.Count <= 1)
                return 0;
            
            int childCount = figma.Children.Count;
            int spacingCount = childCount - 1;
            
            float parentSize = figma.LayoutMode == LayoutMode.HORIZONTAL 
                ? figma.Size.X 
                : figma.Size.Y;
            
            float allChildrenSize = figma.Children.Sum(c => 
                figma.LayoutMode == LayoutMode.HORIZONTAL ? c.Size.X : c.Size.Y
            );
            
            return (parentSize - allChildrenSize) / spacingCount;
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
```

### Alignment Mapping

Port from D.A. Assets alignment logic:

```csharp
namespace WingedBean.FigmaSharp.Core;

public class AlignmentMapper
{
    public AlignmentData GetAlignment(FObject figma)
    {
        var alignment = new AlignmentData();
        
        if (figma.Parent?.LayoutMode == LayoutMode.HORIZONTAL)
        {
            alignment = GetHorizontalLayoutAlignment(figma.Parent);
        }
        else if (figma.Parent?.LayoutMode == LayoutMode.VERTICAL)
        {
            alignment = GetVerticalLayoutAlignment(figma.Parent);
        }
        
        return alignment;
    }
    
    private AlignmentData GetHorizontalLayoutAlignment(FObject parent)
    {
        // Port from D.A. Assets GetHorLayoutAnchor()
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
    
    private AlignmentData GetVerticalLayoutAlignment(FObject parent)
    {
        // Port from D.A. Assets GetVertLayoutAnchor()
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
```

### Size Mode Determination

```csharp
namespace WingedBean.FigmaSharp.Core;

public class SizeModeCalculator
{
    public SizeMode DetermineSizeMode(FObject figma, bool isWidth)
    {
        // Check if child should grow
        if (figma.LayoutGrow == 1)
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
        var parentSizingMode = isWidth 
            ? figma.Parent?.PrimaryAxisSizingMode 
            : figma.Parent?.CounterAxisSizingMode;
        
        if (parentSizingMode == PrimaryAxisSizingMode.AUTO)
        {
            return SizeMode.Auto;
        }
        
        // Default to fixed
        return SizeMode.Fixed;
    }
}
```

### Padding Calculation

Port from D.A. Assets `AdjustPadding()`:

```csharp
namespace WingedBean.FigmaSharp.Core;

public class PaddingCalculator
{
    public Padding CalculatePadding(FObject figma, Vector2 parentSize)
    {
        var padding = new Padding
        {
            Left = figma.PaddingLeft ?? 0,
            Right = figma.PaddingRight ?? 0,
            Top = figma.PaddingTop ?? 0,
            Bottom = figma.PaddingBottom ?? 0
        };
        
        // Adjust padding if it exceeds parent size
        if (figma.Children == null || figma.Children.Count == 0)
            return padding;
        
        float maxChildWidth = figma.Children.Max(c => c.Size.X);
        float maxChildHeight = figma.Children.Max(c => c.Size.Y);
        
        float totalHorizontalPadding = padding.Left + padding.Right;
        float totalVerticalPadding = padding.Top + padding.Bottom;
        
        // Adjust horizontal padding
        if (maxChildWidth + totalHorizontalPadding > parentSize.X && totalHorizontalPadding != 0)
        {
            float excessWidth = (maxChildWidth + totalHorizontalPadding) - parentSize.X;
            float leftRatio = padding.Left / totalHorizontalPadding;
            float rightRatio = padding.Right / totalHorizontalPadding;
            
            padding.Left -= MathF.Ceiling(leftRatio * excessWidth);
            padding.Right -= MathF.Ceiling(rightRatio * excessWidth);
        }
        
        // Adjust vertical padding
        if (maxChildHeight + totalVerticalPadding > parentSize.Y && totalVerticalPadding != 0)
        {
            float excessHeight = (maxChildHeight + totalVerticalPadding) - parentSize.Y;
            float topRatio = padding.Top / totalVerticalPadding;
            float bottomRatio = padding.Bottom / totalVerticalPadding;
            
            padding.Top -= MathF.Ceiling(topRatio * excessHeight);
            padding.Bottom -= MathF.Ceiling(bottomRatio * excessHeight);
        }
        
        return padding;
    }
}
```

## Framework-Specific Adaptations

### Terminal.Gui Renderer

Terminal.Gui uses character-based units and manual positioning:

```csharp
public class TerminalGuiLayoutAdapter
{
    public (Pos x, Pos y, Dim width, Dim height) AdaptLayout(LayoutData layout)
    {
        // Convert pixels to characters (assuming 8x16 character size)
        int charX = (int)(layout.AbsolutePosition.X / 8);
        int charY = (int)(layout.AbsolutePosition.Y / 16);
        int charWidth = (int)(layout.FixedWidth / 8);
        int charHeight = (int)(layout.FixedHeight / 16);
        
        // Position
        Pos x = layout.PositionMode switch
        {
            PositionMode.Absolute => Pos.At(charX),
            PositionMode.Anchored when layout.Alignment?.Horizontal == HorizontalAlign.Center => Pos.Center(),
            PositionMode.Anchored when layout.Alignment?.Horizontal == HorizontalAlign.Right => Pos.AnchorEnd(0),
            _ => Pos.At(0)
        };
        
        Pos y = layout.PositionMode switch
        {
            PositionMode.Absolute => Pos.At(charY),
            PositionMode.Anchored when layout.Alignment?.Vertical == VerticalAlign.Center => Pos.Center(),
            PositionMode.Anchored when layout.Alignment?.Vertical == VerticalAlign.Bottom => Pos.AnchorEnd(0),
            _ => Pos.At(0)
        };
        
        // Size
        Dim width = layout.WidthMode switch
        {
            SizeMode.Fixed => Dim.Absolute(charWidth),
            SizeMode.Fill => Dim.Fill(),
            SizeMode.Auto => Dim.Auto(),
            SizeMode.Percent => Dim.Percent((int)layout.PercentWidth),
            _ => Dim.Absolute(charWidth)
        };
        
        Dim height = layout.HeightMode switch
        {
            SizeMode.Fixed => Dim.Absolute(charHeight),
            SizeMode.Fill => Dim.Fill(),
            SizeMode.Auto => Dim.Auto(),
            SizeMode.Percent => Dim.Percent((int)layout.PercentHeight),
            _ => Dim.Absolute(charHeight)
        };
        
        return (x, y, width, height);
    }
}
```

### Unity UGUI Renderer

Unity uses `RectTransform` with anchors and pivots:

```csharp
public class UguiLayoutAdapter
{
    public void ApplyLayout(RectTransform rt, LayoutData layout)
    {
        // Set anchor and pivot
        rt.anchorMin = new Vector2(0, 1); // Top-left
        rt.anchorMax = new Vector2(0, 1);
        rt.pivot = new Vector2(0.5f, 0.5f); // Center
        
        // Set size
        rt.sizeDelta = new Vector2(layout.FixedWidth, layout.FixedHeight);
        
        // Set position (Unity uses bottom-left origin, Figma uses top-left)
        rt.anchoredPosition = new Vector2(
            layout.AbsolutePosition.X, 
            -layout.AbsolutePosition.Y // Flip Y
        );
        
        // Apply auto layout if needed
        if (layout.AutoLayout != null)
        {
            ApplyAutoLayout(rt.gameObject, layout.AutoLayout);
        }
    }
    
    private void ApplyAutoLayout(GameObject go, AutoLayoutData autoLayout)
    {
        if (autoLayout.Direction == LayoutDirection.Horizontal)
        {
            var layoutGroup = go.AddComponent<HorizontalLayoutGroup>();
            layoutGroup.spacing = autoLayout.Spacing;
            layoutGroup.childAlignment = MapAlignment(autoLayout);
            // ... configure other properties
        }
        else if (autoLayout.Direction == LayoutDirection.Vertical)
        {
            var layoutGroup = go.AddComponent<VerticalLayoutGroup>();
            layoutGroup.spacing = autoLayout.Spacing;
            layoutGroup.childAlignment = MapAlignment(autoLayout);
            // ... configure other properties
        }
    }
}
```

## Testing Strategy

### Unit Tests

```csharp
[Test]
public void CalculateGlobalRect_AbsolutePosition_ReturnsCorrectRect()
{
    var figma = new FObject
    {
        AbsoluteBoundingBox = new BoundingBox { X = 100, Y = 200, Width = 300, Height = 400 },
        LayoutPositioning = LayoutPositioning.ABSOLUTE
    };
    
    var calculator = new LayoutCalculator();
    var rect = calculator.CalculateGlobalRect(figma);
    
    Assert.AreEqual(100, rect.Position.X);
    Assert.AreEqual(200, rect.Position.Y);
    Assert.AreEqual(300, rect.Size.X);
    Assert.AreEqual(400, rect.Size.Y);
}

[Test]
public void BuildAutoLayoutData_HorizontalSpaceBetween_CalculatesCorrectSpacing()
{
    var figma = new FObject
    {
        LayoutMode = LayoutMode.HORIZONTAL,
        PrimaryAxisAlignItems = PrimaryAxisAlignItem.SPACE_BETWEEN,
        Size = new Vector2(1000, 100),
        Children = new List<FObject>
        {
            new FObject { Size = new Vector2(200, 100) },
            new FObject { Size = new Vector2(200, 100) },
            new FObject { Size = new Vector2(200, 100) }
        }
    };
    
    var transformer = new AutoLayoutTransformer();
    var autoLayout = transformer.BuildAutoLayoutData(figma);
    
    // (1000 - 600) / 2 = 200
    Assert.AreEqual(200, autoLayout.Spacing);
}
```

### Integration Tests

```csharp
[Test]
public async Task TransformFigmaDesign_ComplexLayout_ProducesCorrectUIElement()
{
    // Load real Figma design
    var apiClient = new FigmaApiClient();
    var project = await apiClient.LoadFromApiAsync(fileKey, token);
    
    // Transform
    var transformer = new FigmaTransformer();
    var uiElement = transformer.Transform(project.Document.Children[0]);
    
    // Validate
    Assert.AreEqual(UIElementType.Container, uiElement.Type);
    Assert.IsNotNull(uiElement.Layout.AutoLayout);
    Assert.AreEqual(LayoutDirection.Vertical, uiElement.Layout.AutoLayout.Direction);
}
```

## Implementation Plan

### Phase 1: Core Algorithm (Week 1)

1. **Port LayoutCalculator**
   - [ ] `CalculateGlobalRect()`
   - [ ] `CalculateRotationOffset()`
   - [ ] `TryFixSizeWithStroke()`

2. **Port AutoLayoutTransformer**
   - [ ] `BuildAutoLayoutData()`
   - [ ] `CalculateSpacing()` (including SPACE_BETWEEN)
   - [ ] `MapPrimaryAlign()`, `MapCrossAlign()`

3. **Port AlignmentMapper**
   - [ ] `GetHorizontalLayoutAlignment()`
   - [ ] `GetVerticalLayoutAlignment()`

4. **Port SizeModeCalculator**
   - [ ] `DetermineSizeMode()` (LayoutGrow, LayoutAlign logic)

5. **Port PaddingCalculator**
   - [ ] `CalculatePadding()` with adjustment logic

### Phase 2: Framework Adapters (Week 2)

1. **Terminal.Gui Adapter**
   - [ ] Pixel-to-character conversion
   - [ ] `Pos`/`Dim` mapping

2. **Unity UGUI Adapter**
   - [ ] `RectTransform` configuration
   - [ ] Auto-layout to `LayoutGroup` mapping

3. **Godot Adapter** (Future)
   - [ ] Container node configuration
   - [ ] Anchor system mapping

### Phase 3: Testing (Week 2)

1. **Unit Tests**
   - [ ] Test all calculation methods
   - [ ] Test edge cases (rotation, SPACE_BETWEEN, nested layouts)

2. **Integration Tests**
   - [ ] Test with real Figma designs
   - [ ] Validate output for each framework

## Success Criteria

1. ✅ All D.A. Assets layout logic ported to plain C#
2. ✅ Handles all Figma layout modes (horizontal, vertical, wrap)
3. ✅ Correctly calculates SPACE_BETWEEN spacing
4. ✅ Supports LayoutGrow and LayoutAlign
5. ✅ Handles rotation and absolute positioning
6. ✅ Framework adapters produce correct layout for each target
7. ✅ Unit tests cover all scenarios

## References

- D.A. Assets `TransformSetter.cs`
- D.A. Assets `AutoLayoutExtensions.cs`
- [Figma Auto Layout Documentation](https://help.figma.com/hc/en-us/articles/360040451373-Create-dynamic-designs-with-Auto-layout)
- RFC-0022: FigmaSharp Core Architecture

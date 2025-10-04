---
id: RFC-0025
title: FigmaSharp Terminal.Gui v2 Renderer Implementation
status: Implemented
category: implementation, ui, figma, renderers, terminal-gui
created: 2025-10-04
updated: 2025-10-04
---

# RFC-0025: FigmaSharp Terminal.Gui v2 Renderer Implementation

## Status

Implemented

## Date

2025-10-04

## Summary

Define the **complete implementation specification** for the FigmaSharp Terminal.Gui v2 renderer plugin. This renderer translates the framework-agnostic `UIElement` model to Terminal.Gui v2 components, handling the unique challenges of character-based terminal UIs.

**Key Focus**: Complete Terminal.Gui v2 renderer with full feature support, including advanced layout, styling, component mapping, and terminal-specific optimizations.

## Motivation

### Vision

Provide **complete renderer implementations** that:

1. **Correctly map abstract UI to framework UI** (layout, style, components)
2. **Handle framework-specific quirks** (coordinate systems, units, limitations)
3. **Support all Figma features** (auto-layout, constraints, styling)
4. **Maintain consistency** across frameworks where possible

### Renderer Requirements

Each renderer must:

- ✅ Implement `IUIRenderer` interface
- ✅ Create framework-specific UI components
- ✅ Apply layout data (position, size, alignment)
- ✅ Apply style data (colors, borders, shadows, text)
- ✅ Handle parent-child hierarchy
- ✅ Support auto-layout transformation
- ✅ Handle edge cases (rotation, constraints, nested layouts)

## Renderer Specifications

### 1. Terminal.Gui v2 Renderer

#### Overview

**Target**: Console applications (.NET 8.0)  
**Framework**: Terminal.Gui v2  
**Units**: Character cells (not pixels)  
**Coordinate System**: Top-left origin  
**Limitations**: No rotation, limited colors, text-based rendering

#### Component Mapping

| UIElementType | Terminal.Gui Component |
|---------------|------------------------|
| Container | `View` |
| Text | `Label` |
| Button | `Button` |
| Input | `TextField` |
| Toggle | `CheckBox` |
| ScrollView | `ScrollView` |
| Image | `View` (with custom rendering) |

#### Layout Mapping

```csharp
public class TerminalGuiRenderer : IUIRenderer
{
    private const int CharWidth = 8;   // Pixels per character width
    private const int CharHeight = 16; // Pixels per character height
    
    public void ApplyLayout(object target, LayoutData layout)
    {
        var view = (View)target;
        
        // Convert pixels to characters
        int charX = (int)(layout.AbsolutePosition.X / CharWidth);
        int charY = (int)(layout.AbsolutePosition.Y / CharHeight);
        int charWidth = (int)(layout.FixedWidth / CharWidth);
        int charHeight = (int)(layout.FixedHeight / CharHeight);
        
        // Position
        view.X = CalculatePos(layout, isX: true, charX);
        view.Y = CalculatePos(layout, isX: false, charY);
        
        // Size
        view.Width = CalculateDim(layout, isWidth: true, charWidth);
        view.Height = CalculateDim(layout, isWidth: false, charHeight);
    }
    
    private Pos CalculatePos(LayoutData layout, bool isX, int charPos)
    {
        if (layout.PositionMode == PositionMode.Absolute)
        {
            return Pos.At(charPos);
        }
        
        if (layout.PositionMode == PositionMode.Anchored)
        {
            if (isX && layout.Alignment?.Horizontal == HorizontalAlign.Center)
                return Pos.Center();
            if (isX && layout.Alignment?.Horizontal == HorizontalAlign.Right)
                return Pos.AnchorEnd(0);
            if (!isX && layout.Alignment?.Vertical == VerticalAlign.Center)
                return Pos.Center();
            if (!isX && layout.Alignment?.Vertical == VerticalAlign.Bottom)
                return Pos.AnchorEnd(0);
        }
        
        return Pos.At(0);
    }
    
    private Dim CalculateDim(LayoutData layout, bool isWidth, int charSize)
    {
        var mode = isWidth ? layout.WidthMode : layout.HeightMode;
        
        return mode switch
        {
            SizeMode.Fixed => Dim.Absolute(charSize),
            SizeMode.Fill => Dim.Fill(),
            SizeMode.Auto => Dim.Auto(),
            SizeMode.Percent => Dim.Percent((int)(isWidth ? layout.PercentWidth : layout.PercentHeight)),
            _ => Dim.Absolute(charSize)
        };
    }
}
```

#### Auto-Layout Handling

Terminal.Gui doesn't have built-in auto-layout, so we manually position children:

```csharp
public void ApplyAutoLayout(View container, AutoLayoutData autoLayout, List<View> children)
{
    if (autoLayout.Direction == LayoutDirection.Horizontal)
    {
        int currentX = (int)(autoLayout.Padding?.Left ?? 0);
        int spacing = (int)autoLayout.Spacing;
        
        foreach (var child in children)
        {
            child.X = Pos.At(currentX);
            child.Y = GetVerticalAlignment(autoLayout.CrossAlign);
            currentX += child.Frame.Width + spacing;
        }
    }
    else if (autoLayout.Direction == LayoutDirection.Vertical)
    {
        int currentY = (int)(autoLayout.Padding?.Top ?? 0);
        int spacing = (int)autoLayout.Spacing;
        
        foreach (var child in children)
        {
            child.X = GetHorizontalAlignment(autoLayout.CrossAlign);
            child.Y = Pos.At(currentY);
            currentY += child.Frame.Height + spacing;
        }
    }
}

private Pos GetVerticalAlignment(CrossAxisAlign align)
{
    return align switch
    {
        CrossAxisAlign.Center => Pos.Center(),
        CrossAxisAlign.End => Pos.AnchorEnd(0),
        _ => Pos.At(0)
    };
}
```

#### Style Mapping

```csharp
public void ApplyStyle(object target, StyleData style)
{
    var view = (View)target;
    
    // Terminal.Gui has limited color support
    if (style?.BackgroundColor != null)
    {
        var terminalColor = MapToTerminalColor(style.BackgroundColor);
        view.ColorScheme = new ColorScheme
        {
            Normal = new Terminal.Gui.Attribute(Color.White, terminalColor)
        };
    }
    
    // Text style
    if (target is Label label && style?.Text != null)
    {
        label.Text = style.Text.Content;
        // Terminal.Gui doesn't support font size/weight
    }
}

private Color MapToTerminalColor(WingedBean.Contracts.FigmaSharp.Color figmaColor)
{
    // Map RGB to nearest Terminal.Gui color
    // Terminal.Gui supports: Black, Blue, Green, Cyan, Red, Magenta, Brown, Gray, etc.
    
    if (figmaColor.R > 0.8f && figmaColor.G < 0.2f && figmaColor.B < 0.2f)
        return Color.Red;
    if (figmaColor.R < 0.2f && figmaColor.G > 0.8f && figmaColor.B < 0.2f)
        return Color.Green;
    if (figmaColor.R < 0.2f && figmaColor.G < 0.2f && figmaColor.B > 0.8f)
        return Color.Blue;
    
    return Color.White;
}
```

#### Limitations

- ❌ No rotation support
- ❌ No gradients
- ❌ No shadows
- ❌ Limited color palette (16 colors)
- ❌ No pixel-perfect rendering
- ✅ Auto-layout via manual positioning
- ✅ Basic styling (colors, text)

---

### 2. Unity UGUI Renderer

#### Overview

**Target**: Unity 2021.3+ (.NET Standard 2.1)  
**Framework**: Unity UGUI (uGUI)  
**Units**: Pixels  
**Coordinate System**: Bottom-left origin (Y-axis flipped)  
**Features**: Full layout support, rich styling, prefab generation

#### Component Mapping

| UIElementType | Unity UGUI Component |
|---------------|----------------------|
| Container | `GameObject` + `RectTransform` |
| Text | `GameObject` + `Text` or `TextMeshProUGUI` |
| Button | `GameObject` + `Button` + `Image` + `Text` |
| Input | `GameObject` + `InputField` + `Text` |
| Toggle | `GameObject` + `Toggle` + `Image` + `Text` |
| ScrollView | `GameObject` + `ScrollRect` + `Mask` |
| Image | `GameObject` + `Image` + `Sprite` |

#### Layout Mapping

```csharp
public class UguiRenderer : IUIRenderer
{
    public void ApplyLayout(object target, LayoutData layout)
    {
        var go = (GameObject)target;
        var rt = go.GetComponent<RectTransform>();
        
        // Set anchor and pivot
        rt.anchorMin = new Vector2(0, 1); // Top-left
        rt.anchorMax = new Vector2(0, 1);
        rt.pivot = new Vector2(0.5f, 0.5f); // Center
        
        // Set size
        rt.sizeDelta = new Vector2(layout.FixedWidth, layout.FixedHeight);
        
        // Set position (flip Y for Unity's bottom-left origin)
        rt.anchoredPosition = new Vector2(
            layout.AbsolutePosition.X,
            -layout.AbsolutePosition.Y // Flip Y
        );
        
        // Apply constraints/anchors
        if (layout.Anchors != null)
        {
            ApplyAnchors(rt, layout.Anchors);
        }
        
        // Apply auto layout
        if (layout.AutoLayout != null)
        {
            ApplyAutoLayout(go, layout.AutoLayout);
        }
    }
    
    private void ApplyAnchors(RectTransform rt, AnchorData anchors)
    {
        // Map Figma constraints to Unity anchors
        if (anchors.Left && anchors.Right)
        {
            rt.anchorMin = new Vector2(0, rt.anchorMin.y);
            rt.anchorMax = new Vector2(1, rt.anchorMax.y);
        }
        
        if (anchors.Top && anchors.Bottom)
        {
            rt.anchorMin = new Vector2(rt.anchorMin.x, 0);
            rt.anchorMax = new Vector2(rt.anchorMax.x, 1);
        }
    }
}
```

#### Auto-Layout Mapping

```csharp
private void ApplyAutoLayout(GameObject go, AutoLayoutData autoLayout)
{
    if (autoLayout.Direction == LayoutDirection.Horizontal)
    {
        var layoutGroup = go.AddComponent<HorizontalLayoutGroup>();
        ConfigureLayoutGroup(layoutGroup, autoLayout);
    }
    else if (autoLayout.Direction == LayoutDirection.Vertical)
    {
        var layoutGroup = go.AddComponent<VerticalLayoutGroup>();
        ConfigureLayoutGroup(layoutGroup, autoLayout);
    }
    
    // Add ContentSizeFitter if needed
    if (autoLayout.PrimarySizing == SizingMode.Auto || autoLayout.CrossSizing == SizingMode.Auto)
    {
        var csf = go.AddComponent<ContentSizeFitter>();
        
        if (autoLayout.Direction == LayoutDirection.Horizontal)
        {
            csf.horizontalFit = autoLayout.PrimarySizing == SizingMode.Auto 
                ? ContentSizeFitter.FitMode.PreferredSize 
                : ContentSizeFitter.FitMode.Unconstrained;
            csf.verticalFit = autoLayout.CrossSizing == SizingMode.Auto 
                ? ContentSizeFitter.FitMode.PreferredSize 
                : ContentSizeFitter.FitMode.Unconstrained;
        }
        else
        {
            csf.verticalFit = autoLayout.PrimarySizing == SizingMode.Auto 
                ? ContentSizeFitter.FitMode.PreferredSize 
                : ContentSizeFitter.FitMode.Unconstrained;
            csf.horizontalFit = autoLayout.CrossSizing == SizingMode.Auto 
                ? ContentSizeFitter.FitMode.PreferredSize 
                : ContentSizeFitter.FitMode.Unconstrained;
        }
    }
}

private void ConfigureLayoutGroup(HorizontalOrVerticalLayoutGroup layoutGroup, AutoLayoutData autoLayout)
{
    layoutGroup.spacing = autoLayout.Spacing;
    layoutGroup.childAlignment = MapAlignment(autoLayout);
    layoutGroup.childControlWidth = false;
    layoutGroup.childControlHeight = false;
    layoutGroup.childForceExpandWidth = autoLayout.PrimaryAlign == PrimaryAxisAlign.SpaceBetween;
    layoutGroup.childForceExpandHeight = autoLayout.CrossAlign == CrossAxisAlign.Stretch;
    
    // Padding
    layoutGroup.padding = new RectOffset(
        (int)(autoLayout.Padding?.Left ?? 0),
        (int)(autoLayout.Padding?.Right ?? 0),
        (int)(autoLayout.Padding?.Top ?? 0),
        (int)(autoLayout.Padding?.Bottom ?? 0)
    );
}

private TextAnchor MapAlignment(AutoLayoutData autoLayout)
{
    // Map Figma alignment to Unity TextAnchor
    string key = $"{autoLayout.PrimaryAlign} {autoLayout.CrossAlign}";
    
    return key switch
    {
        "Start Start" => TextAnchor.UpperLeft,
        "Center Center" => TextAnchor.MiddleCenter,
        "End End" => TextAnchor.LowerRight,
        // ... all combinations
        _ => TextAnchor.UpperLeft
    };
}
```

#### Style Mapping

```csharp
public void ApplyStyle(object target, StyleData style)
{
    var go = (GameObject)target;
    
    // Background color
    if (style?.BackgroundColor != null && go.TryGetComponent<Image>(out var image))
    {
        image.color = new Color(
            style.BackgroundColor.R,
            style.BackgroundColor.G,
            style.BackgroundColor.B,
            style.BackgroundColor.A
        );
    }
    
    // Border
    if (style?.Border != null)
    {
        // Unity doesn't have built-in borders, use Outline component or custom shader
        var outline = go.AddComponent<Outline>();
        outline.effectColor = new Color(
            style.Border.Color.R,
            style.Border.Color.G,
            style.Border.Color.B,
            style.Border.Color.A
        );
        outline.effectDistance = new Vector2(style.Border.Width, style.Border.Width);
    }
    
    // Shadow
    if (style?.Shadow != null)
    {
        var shadow = go.AddComponent<Shadow>();
        shadow.effectColor = new Color(
            style.Shadow.Color.R,
            style.Shadow.Color.G,
            style.Shadow.Color.B,
            style.Shadow.Color.A
        );
        shadow.effectDistance = new Vector2(style.Shadow.OffsetX, style.Shadow.OffsetY);
    }
    
    // Text style
    if (style?.Text != null && go.TryGetComponent<Text>(out var text))
    {
        text.text = style.Text.Content;
        text.fontSize = (int)style.Text.FontSize;
        text.fontStyle = style.Text.Bold ? FontStyle.Bold : FontStyle.Normal;
        text.alignment = MapTextAlignment(style.Text.HorizontalAlign, style.Text.VerticalAlign);
    }
}
```

#### Features

- ✅ Full auto-layout support (`HorizontalLayoutGroup`, `VerticalLayoutGroup`)
- ✅ Rotation support
- ✅ Rich styling (colors, borders, shadows)
- ✅ Prefab generation
- ✅ Sprite/texture support
- ✅ TextMeshPro support

---

### 3. Unity UI Toolkit Renderer

#### Overview

**Target**: Unity 2021.3+ (.NET Standard 2.1)  
**Framework**: Unity UI Toolkit (formerly UIElements)  
**Units**: Pixels  
**Coordinate System**: Top-left origin  
**Features**: Flexbox layout, USS styling, UXML generation

#### Component Mapping

| UIElementType | UI Toolkit Component |
|---------------|----------------------|
| Container | `VisualElement` |
| Text | `Label` |
| Button | `Button` |
| Input | `TextField` |
| Toggle | `Toggle` |
| ScrollView | `ScrollView` |
| Image | `Image` |

#### Layout Mapping

```csharp
public class UIToolkitRenderer : IUIRenderer
{
    public void ApplyLayout(object target, LayoutData layout)
    {
        var ve = (VisualElement)target;
        
        // Position
        ve.style.position = Position.Absolute;
        ve.style.left = layout.AbsolutePosition.X;
        ve.style.top = layout.AbsolutePosition.Y;
        
        // Size
        if (layout.WidthMode == SizeMode.Fixed)
            ve.style.width = layout.FixedWidth;
        else if (layout.WidthMode == SizeMode.Percent)
            ve.style.width = new Length(layout.PercentWidth, LengthUnit.Percent);
        
        if (layout.HeightMode == SizeMode.Fixed)
            ve.style.height = layout.FixedHeight;
        else if (layout.HeightMode == SizeMode.Percent)
            ve.style.height = new Length(layout.PercentHeight, LengthUnit.Percent);
        
        // Auto layout (Flexbox)
        if (layout.AutoLayout != null)
        {
            ApplyFlexbox(ve, layout.AutoLayout);
        }
    }
    
    private void ApplyFlexbox(VisualElement ve, AutoLayoutData autoLayout)
    {
        ve.style.flexDirection = autoLayout.Direction == LayoutDirection.Horizontal
            ? FlexDirection.Row
            : FlexDirection.Column;
        
        // Alignment
        ve.style.justifyContent = MapJustifyContent(autoLayout.PrimaryAlign);
        ve.style.alignItems = MapAlignItems(autoLayout.CrossAlign);
        
        // Wrap
        if (autoLayout.WrapEnabled)
        {
            ve.style.flexWrap = Wrap.Wrap;
        }
        
        // Padding
        ve.style.paddingLeft = autoLayout.Padding?.Left ?? 0;
        ve.style.paddingRight = autoLayout.Padding?.Right ?? 0;
        ve.style.paddingTop = autoLayout.Padding?.Top ?? 0;
        ve.style.paddingBottom = autoLayout.Padding?.Bottom ?? 0;
    }
    
    private Justify MapJustifyContent(PrimaryAxisAlign align)
    {
        return align switch
        {
            PrimaryAxisAlign.Start => Justify.FlexStart,
            PrimaryAxisAlign.Center => Justify.Center,
            PrimaryAxisAlign.End => Justify.FlexEnd,
            PrimaryAxisAlign.SpaceBetween => Justify.SpaceBetween,
            _ => Justify.FlexStart
        };
    }
    
    private Align MapAlignItems(CrossAxisAlign align)
    {
        return align switch
        {
            CrossAxisAlign.Start => Align.FlexStart,
            CrossAxisAlign.Center => Align.Center,
            CrossAxisAlign.End => Align.FlexEnd,
            CrossAxisAlign.Stretch => Align.Stretch,
            _ => Align.FlexStart
        };
    }
}
```

#### Style Mapping

```csharp
public void ApplyStyle(object target, StyleData style)
{
    var ve = (VisualElement)target;
    
    // Background color
    if (style?.BackgroundColor != null)
    {
        ve.style.backgroundColor = new Color(
            style.BackgroundColor.R,
            style.BackgroundColor.G,
            style.BackgroundColor.B,
            style.BackgroundColor.A
        );
    }
    
    // Border
    if (style?.Border != null)
    {
        ve.style.borderLeftWidth = style.Border.Width;
        ve.style.borderRightWidth = style.Border.Width;
        ve.style.borderTopWidth = style.Border.Width;
        ve.style.borderBottomWidth = style.Border.Width;
        
        ve.style.borderLeftColor = new Color(
            style.Border.Color.R,
            style.Border.Color.G,
            style.Border.Color.B,
            style.Border.Color.A
        );
        // ... set other border colors
        
        ve.style.borderTopLeftRadius = style.Border.TopLeftRadius;
        ve.style.borderTopRightRadius = style.Border.TopRightRadius;
        ve.style.borderBottomLeftRadius = style.Border.BottomLeftRadius;
        ve.style.borderBottomRightRadius = style.Border.BottomRightRadius;
    }
    
    // Text style
    if (target is Label label && style?.Text != null)
    {
        label.text = style.Text.Content;
        label.style.fontSize = style.Text.FontSize;
        label.style.unityFontStyleAndWeight = style.Text.Bold ? FontStyle.Bold : FontStyle.Normal;
        label.style.color = new Color(
            style.Text.Color.R,
            style.Text.Color.G,
            style.Text.Color.B,
            style.Text.Color.A
        );
    }
}
```

#### Features

- ✅ Native flexbox support (perfect for Figma auto-layout)
- ✅ USS styling (CSS-like)
- ✅ UXML generation (declarative UI)
- ✅ Rich styling (borders, shadows, gradients)
- ✅ Better performance than UGUI

---

### 4. Godot UI Renderer

#### Overview

**Target**: Godot 4.x (.NET Standard 2.1)  
**Framework**: Godot UI (Control nodes)  
**Units**: Pixels  
**Coordinate System**: Top-left origin  
**Features**: Scene tree, anchors, containers

#### Component Mapping

| UIElementType | Godot Component |
|---------------|-----------------|
| Container | `Control` or `Container` |
| Text | `Label` |
| Button | `Button` |
| Input | `LineEdit` |
| Toggle | `CheckBox` |
| ScrollView | `ScrollContainer` |
| Image | `TextureRect` |

#### Layout Mapping

```csharp
public class GodotRenderer : IUIRenderer
{
    public void ApplyLayout(object target, LayoutData layout)
    {
        var control = (Control)target;
        
        // Position
        control.Position = new Vector2(layout.AbsolutePosition.X, layout.AbsolutePosition.Y);
        
        // Size
        control.Size = new Vector2(layout.FixedWidth, layout.FixedHeight);
        
        // Anchors
        if (layout.Anchors != null)
        {
            ApplyAnchors(control, layout.Anchors);
        }
        
        // Auto layout
        if (layout.AutoLayout != null)
        {
            ApplyContainer(control, layout.AutoLayout);
        }
    }
    
    private void ApplyAnchors(Control control, AnchorData anchors)
    {
        if (anchors.Left && anchors.Right)
        {
            control.AnchorLeft = 0;
            control.AnchorRight = 1;
        }
        
        if (anchors.Top && anchors.Bottom)
        {
            control.AnchorTop = 0;
            control.AnchorBottom = 1;
        }
    }
    
    private void ApplyContainer(Control control, AutoLayoutData autoLayout)
    {
        // Godot uses Container nodes for layout
        Container container = autoLayout.Direction == LayoutDirection.Horizontal
            ? new HBoxContainer()
            : new VBoxContainer();
        
        // Transfer children to container
        foreach (var child in control.GetChildren())
        {
            control.RemoveChild(child);
            container.AddChild(child);
        }
        
        control.AddChild(container);
        
        // Configure container
        container.AddThemeConstantOverride("separation", (int)autoLayout.Spacing);
    }
}
```

#### Features

- ✅ Scene tree hierarchy
- ✅ Anchor system
- ✅ Container nodes for layout
- ✅ Rich styling via themes
- ✅ Shader support

---

## Testing Strategy

### Unit Tests

```csharp
[Test]
public void TerminalGuiRenderer_ApplyLayout_ConvertsPixelsToCharacters()
{
    var renderer = new TerminalGuiRenderer();
    var view = new View();
    var layout = new LayoutData
    {
        AbsolutePosition = new Vector2(80, 160), // 10 chars, 10 rows
        FixedWidth = 240, // 30 chars
        FixedHeight = 320, // 20 rows
        PositionMode = PositionMode.Absolute,
        WidthMode = SizeMode.Fixed,
        HeightMode = SizeMode.Fixed
    };
    
    renderer.ApplyLayout(view, layout);
    
    Assert.AreEqual(10, view.X.GetValue());
    Assert.AreEqual(10, view.Y.GetValue());
    Assert.AreEqual(30, view.Width.GetValue());
    Assert.AreEqual(20, view.Height.GetValue());
}

[Test]
public void UguiRenderer_ApplyAutoLayout_CreatesHorizontalLayoutGroup()
{
    var renderer = new UguiRenderer();
    var go = new GameObject();
    var layout = new LayoutData
    {
        AutoLayout = new AutoLayoutData
        {
            Direction = LayoutDirection.Horizontal,
            Spacing = 10,
            PrimaryAlign = PrimaryAxisAlign.Center
        }
    };
    
    renderer.ApplyLayout(go, layout);
    
    Assert.IsTrue(go.TryGetComponent<HorizontalLayoutGroup>(out var layoutGroup));
    Assert.AreEqual(10, layoutGroup.spacing);
}
```

## Implementation Plan

### Phase 1: Terminal.Gui Renderer (Week 1)

1. **Core Implementation**
   - [ ] Component creation methods
   - [ ] Layout application (Pos/Dim mapping)
   - [ ] Style application (color mapping)
   - [ ] Auto-layout manual positioning

2. **Testing**
   - [ ] Unit tests for layout conversion
   - [ ] Integration tests with real Figma data

### Phase 2: Unity Renderers (Week 2-3)

1. **UGUI Renderer**
   - [ ] Component creation
   - [ ] Layout application (RectTransform)
   - [ ] Auto-layout (LayoutGroup)
   - [ ] Style application

2. **UI Toolkit Renderer**
   - [ ] Component creation
   - [ ] Flexbox layout
   - [ ] USS styling
   - [ ] UXML generation

### Phase 3: Godot Renderer (Week 4)

1. **Core Implementation**
   - [ ] Component creation
   - [ ] Layout application
   - [ ] Container nodes
   - [ ] Style application

## Success Criteria

1. ✅ All renderers implement `IUIRenderer` correctly
2. ✅ Layout transformation works for each framework
3. ✅ Auto-layout maps correctly (manual for Terminal.Gui, LayoutGroup for UGUI, Flexbox for UI Toolkit)
4. ✅ Style application produces correct visual output
5. ✅ Unit tests cover all renderers
6. ✅ Integration tests validate with real Figma designs

## References

- [Terminal.Gui v2 Documentation](https://gui-cs.github.io/Terminal.Gui/)
- [Unity UGUI Documentation](https://docs.unity3d.com/Manual/UISystem.html)
- [Unity UI Toolkit Documentation](https://docs.unity3d.com/Manual/UIElements.html)
- [Godot UI Documentation](https://docs.godotengine.org/en/stable/tutorials/ui/index.html)
- RFC-0022: FigmaSharp Core Architecture
- RFC-0023: FigmaSharp Layout Transformation
- RFC-0024: FigmaSharp Plugin Integration

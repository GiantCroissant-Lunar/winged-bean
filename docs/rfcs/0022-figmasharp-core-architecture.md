---
id: RFC-0022
title: FigmaSharp Core Architecture
status: Draft
category: architecture, ui, figma
created: 2025-10-04
updated: 2025-10-04
---

# RFC-0022: FigmaSharp Core Architecture

## Status

Draft

## Date

2025-10-04

## Summary

Establish the **FigmaSharp** framework architecture for transforming Figma designs into UI framework implementations. FigmaSharp provides a **framework-agnostic abstraction layer** that sits between Figma's design model and specific UI frameworks (Terminal.Gui, Unity UGUI, Unity UI Toolkit, Godot UI), enabling one-to-many transformation through a plugin-based renderer system.

**Key Principle**: Port D.A. Assets' Figma-to-Unity transformation logic to plain C#, create an abstract UI model, and implement framework-specific renderers as WingedBean plugins.

## Motivation

### Vision

Build a **universal Figma-to-UI transformation pipeline** where:

1. **Figma designs** are transformed to a framework-agnostic UI model
2. **Renderer plugins** convert the abstract model to specific frameworks
3. **Hot-swappable renderers** allow switching frameworks without code changes
4. **Reusable transformation logic** eliminates duplicate implementation across frameworks

### Problems to Solve

1. **Unity Dependency**: D.A. Assets plugin is tightly coupled to Unity types (`GameObject`, `RectTransform`, `Vector2`, etc.)
2. **Code Duplication**: Each framework (UGUI, UI Toolkit, Nova) has separate drawer implementations
3. **No Terminal.Gui Support**: Existing plugin doesn't support console UI frameworks
4. **Monolithic Design**: Cannot mix renderers or switch frameworks at runtime
5. **Limited Extensibility**: Adding new frameworks requires modifying core code

### Current D.A. Assets Architecture

```
Figma API → FObject Model → [CanvasDrawer | NovaDrawer | UITK_Converter]
                                    ↓              ↓              ↓
                                  UGUI          Nova UI      UI Toolkit
```

**Issues:**
- Each drawer reimplements layout transformation
- Unity-specific types throughout codebase
- No abstraction layer between Figma and frameworks
- Cannot support non-Unity frameworks

## Proposal

### FigmaSharp Architecture Overview

```
┌─────────────────────────────────────────────────────────────────┐
│                    Figma API Client                             │
│  - HTTP client for Figma REST API                               │
│  - OAuth/token authentication                                   │
│  - JSON deserialization to FObject model                        │
└─────────────────────────────────────────────────────────────────┘
                             ↓
┌─────────────────────────────────────────────────────────────────┐
│              Figma Domain Model (Plain C#)                      │
│  - FObject struct (no Unity types)                              │
│  - Layout enums (LayoutMode, PrimaryAxisAlign, etc.)            │
│  - Style data (Paint, Effect, Style)                            │
└─────────────────────────────────────────────────────────────────┘
                             ↓
┌─────────────────────────────────────────────────────────────────┐
│           FigmaTransformer (Core Logic)                         │
│  - Port from D.A. Assets TransformSetter                        │
│  - Calculate global rect, anchors, auto layout                  │
│  - Tag classification (Button, Text, Image, etc.)               │
│  - Output: Abstract UI Model                                    │
└─────────────────────────────────────────────────────────────────┘
                             ↓
┌─────────────────────────────────────────────────────────────────┐
│         Abstract UI Model (Framework-Agnostic)                  │
│  - UIElement (type, layout, style, children)                    │
│  - LayoutData (position, size, auto layout, padding)            │
│  - StyleData (colors, borders, shadows, text)                   │
└─────────────────────────────────────────────────────────────────┘
                             ↓
┌─────────────────────────────────────────────────────────────────┐
│              IUIRenderer Interface                              │
│  - CreateContainer/Text/Button/Image/Input()                    │
│  - ApplyLayout(target, LayoutData)                              │
│  - ApplyStyle(target, StyleData)                                │
│  - AddChild(parent, child)                                      │
└─────────────────────────────────────────────────────────────────┘
                             ↓
        ┌────────────────────┴────────────────────┐
        ↓                    ↓                    ↓
┌──────────────┐    ┌──────────────┐    ┌──────────────┐
│ Terminal.Gui │    │ Unity UGUI   │    │ Godot UI     │
│  Renderer    │    │  Renderer    │    │  Renderer    │
└──────────────┘    └──────────────┘    └──────────────┘
```

### Core Components

#### 1. Figma Domain Model (Tier 1 - Contracts)

**Package**: `WingedBean.Contracts.FigmaSharp`  
**Target**: `.NET Standard 2.1` (Unity/Godot compatible)

```csharp
namespace WingedBean.Contracts.FigmaSharp;

// Replace Unity types with plain C#
public struct Vector2
{
    public float X { get; set; }
    public float Y { get; set; }
}

public struct Color
{
    public float R { get; set; }
    public float G { get; set; }
    public float B { get; set; }
    public float A { get; set; }
}

// Port from D.A. Assets
public struct FObject
{
    public string Id { get; set; }
    public string Name { get; set; }
    public NodeType Type { get; set; }
    public Vector2 Size { get; set; }
    public BoundingBox AbsoluteBoundingBox { get; set; }
    public LayoutMode LayoutMode { get; set; }
    public PrimaryAxisAlignItem PrimaryAxisAlignItems { get; set; }
    public CounterAxisAlignItem CounterAxisAlignItems { get; set; }
    public float? ItemSpacing { get; set; }
    public List<FObject> Children { get; set; }
    // ... all other Figma properties
}

public enum LayoutMode { NONE, HORIZONTAL, VERTICAL }
public enum PrimaryAxisAlignItem { NONE, MIN, CENTER, MAX, SPACE_BETWEEN }
// ... all other enums
```

#### 2. Abstract UI Model (Tier 1 - Contracts)

**Package**: `WingedBean.Contracts.FigmaSharp`  
**Target**: `.NET Standard 2.1`

```csharp
namespace WingedBean.Contracts.FigmaSharp;

public class UIElement
{
    public string Id { get; set; }
    public string Name { get; set; }
    public UIElementType Type { get; set; }
    public LayoutData Layout { get; set; }
    public StyleData Style { get; set; }
    public List<UIElement> Children { get; set; }
    public Dictionary<string, object> Metadata { get; set; }
}

public enum UIElementType
{
    Container,
    Text,
    Button,
    Image,
    Input,
    Toggle,
    ScrollView
}

public class LayoutData
{
    // Position
    public PositionMode PositionMode { get; set; }
    public Vector2 AbsolutePosition { get; set; }
    public AnchorData Anchors { get; set; }
    public AlignmentData Alignment { get; set; }
    
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

public enum PositionMode { Absolute, Relative, Anchored, AutoLayout }
public enum SizeMode { Fixed, Fill, Auto, Percent }

public class AutoLayoutData
{
    public LayoutDirection Direction { get; set; }
    public bool WrapEnabled { get; set; }
    public float Spacing { get; set; }
    public PrimaryAxisAlign PrimaryAlign { get; set; }
    public CrossAxisAlign CrossAlign { get; set; }
    public SizingMode PrimarySizing { get; set; }
    public SizingMode CrossSizing { get; set; }
}

public enum LayoutDirection { Horizontal, Vertical, None }
public enum PrimaryAxisAlign { Start, Center, End, SpaceBetween }
public enum CrossAxisAlign { Start, Center, End, Stretch, Baseline }
public enum SizingMode { Fixed, Auto }

public class StyleData
{
    public Color BackgroundColor { get; set; }
    public Color ForegroundColor { get; set; }
    public BorderStyle Border { get; set; }
    public ShadowStyle Shadow { get; set; }
    public TextStyle Text { get; set; }
}
```

#### 3. Renderer Interface (Tier 1 - Contracts)

**Package**: `WingedBean.Contracts.FigmaSharp`  
**Target**: `.NET Standard 2.1`

```csharp
namespace WingedBean.Contracts.FigmaSharp;

public interface IUIRenderer
{
    string FrameworkName { get; }
    
    // Element creation
    object CreateContainer(UIElement element);
    object CreateText(UIElement element);
    object CreateButton(UIElement element);
    object CreateImage(UIElement element);
    object CreateInput(UIElement element);
    object CreateToggle(UIElement element);
    object CreateScrollView(UIElement element);
    
    // Layout and style application
    void ApplyLayout(object target, LayoutData layout);
    void ApplyStyle(object target, StyleData style);
    
    // Hierarchy management
    void AddChild(object parent, object child);
    void RemoveChild(object parent, object child);
}

public interface IFigmaTransformer
{
    UIElement Transform(FObject figmaObject);
    Task<FigmaProject> LoadFromApiAsync(string fileKey, string token, CancellationToken ct = default);
}
```

#### 4. Core Transformer (Tier 2 - Infrastructure)

**Package**: `WingedBean.FigmaSharp.Core`  
**Target**: `.NET Standard 2.1`

```csharp
namespace WingedBean.FigmaSharp.Core;

public class FigmaTransformer : IFigmaTransformer
{
    public UIElement Transform(FObject figmaObject)
    {
        var element = new UIElement
        {
            Id = figmaObject.Id,
            Name = figmaObject.Name,
            Type = MapElementType(figmaObject),
            Layout = BuildLayoutData(figmaObject),
            Style = BuildStyleData(figmaObject),
            Children = figmaObject.Children?.Select(Transform).ToList() ?? new()
        };
        
        return element;
    }
    
    private LayoutData BuildLayoutData(FObject figma)
    {
        // Port from D.A. Assets TransformSetter.GetGlobalRect()
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
        
        // Determine size modes
        layout.WidthMode = DetermineSizeMode(figma, isWidth: true);
        layout.HeightMode = DetermineSizeMode(figma, isWidth: false);
        
        // Auto layout
        if (figma.LayoutMode != LayoutMode.NONE)
        {
            layout.AutoLayout = BuildAutoLayoutData(figma);
        }
        
        // Padding
        layout.Padding = new Padding
        {
            Left = figma.PaddingLeft ?? 0,
            Right = figma.PaddingRight ?? 0,
            Top = figma.PaddingTop ?? 0,
            Bottom = figma.PaddingBottom ?? 0
        };
        
        return layout;
    }
    
    private AutoLayoutData BuildAutoLayoutData(FObject figma)
    {
        // Port from D.A. Assets AutoLayoutExtensions
        return new AutoLayoutData
        {
            Direction = figma.LayoutMode == LayoutMode.HORIZONTAL 
                ? LayoutDirection.Horizontal 
                : LayoutDirection.Vertical,
            WrapEnabled = figma.LayoutWrap == LayoutWrap.WRAP,
            Spacing = figma.ItemSpacing ?? 0,
            PrimaryAlign = MapPrimaryAlign(figma.PrimaryAxisAlignItems),
            CrossAlign = MapCrossAlign(figma.CounterAxisAlignItems),
            PrimarySizing = figma.PrimaryAxisSizingMode == PrimaryAxisSizingMode.AUTO 
                ? SizingMode.Auto 
                : SizingMode.Fixed,
            CrossSizing = figma.CounterAxisSizingMode == CounterAxisSizingMode.AUTO 
                ? SizingMode.Auto 
                : SizingMode.Fixed
        };
    }
    
    // Port all helper methods from D.A. Assets
    private FRect CalculateGlobalRect(FObject fobject) { /* ... */ }
    private SizeMode DetermineSizeMode(FObject figma, bool isWidth) { /* ... */ }
    private UIElementType MapElementType(FObject figma) { /* ... */ }
}
```

#### 5. Transformation Pipeline (Tier 2 - Infrastructure)

**Package**: `WingedBean.FigmaSharp.Core`  
**Target**: `.NET Standard 2.1`

```csharp
namespace WingedBean.FigmaSharp.Core;

public class FigmaToUIPipeline
{
    private readonly IFigmaTransformer _transformer;
    private readonly IUIRenderer _renderer;
    
    public FigmaToUIPipeline(IFigmaTransformer transformer, IUIRenderer renderer)
    {
        _transformer = transformer;
        _renderer = renderer;
    }
    
    public object Convert(FObject figmaRoot)
    {
        // Step 1: Transform Figma to abstract model
        var abstractUI = _transformer.Transform(figmaRoot);
        
        // Step 2: Render to specific framework
        return RenderElement(abstractUI);
    }
    
    private object RenderElement(UIElement element)
    {
        // Create framework-specific object
        object target = element.Type switch
        {
            UIElementType.Container => _renderer.CreateContainer(element),
            UIElementType.Text => _renderer.CreateText(element),
            UIElementType.Button => _renderer.CreateButton(element),
            UIElementType.Image => _renderer.CreateImage(element),
            UIElementType.Input => _renderer.CreateInput(element),
            UIElementType.Toggle => _renderer.CreateToggle(element),
            UIElementType.ScrollView => _renderer.CreateScrollView(element),
            _ => _renderer.CreateContainer(element)
        };
        
        // Apply layout and style
        _renderer.ApplyLayout(target, element.Layout);
        _renderer.ApplyStyle(target, element.Style);
        
        // Render children recursively
        foreach (var child in element.Children ?? new List<UIElement>())
        {
            var childTarget = RenderElement(child);
            _renderer.AddChild(target, childTarget);
        }
        
        return target;
    }
}
```

### Framework Targeting

Following RFC-0005 (Target Framework Compliance):

| Component | Target Framework | Reason |
|-----------|-----------------|--------|
| `WingedBean.Contracts.FigmaSharp` | `.NET Standard 2.1` | Unity/Godot compatible |
| `WingedBean.FigmaSharp.Core` | `.NET Standard 2.1` | Portable infrastructure |
| Renderer Plugins | `.NET 8.0` or `.NET Standard 2.1` | Depends on target framework |

### Project Structure

```
winged-bean/development/dotnet/
├── framework/src/
│   ├── WingedBean.Contracts.FigmaSharp/        # Tier 1
│   │   ├── IUIRenderer.cs
│   │   ├── IFigmaTransformer.cs
│   │   ├── Models/
│   │   │   ├── UIElement.cs
│   │   │   ├── LayoutData.cs
│   │   │   ├── StyleData.cs
│   │   │   └── FObject.cs
│   │   └── Enums/
│   │       ├── LayoutEnums.cs
│   │       └── StyleEnums.cs
│   │
│   └── WingedBean.FigmaSharp.Core/             # Tier 2
│       ├── FigmaTransformer.cs
│       ├── FigmaApiClient.cs
│       ├── FigmaToUIPipeline.cs
│       ├── LayoutCalculator.cs
│       └── StyleBuilder.cs
│
└── console/src/plugins/
    └── (Renderer plugins - see RFC-0025)
```

## Implementation Plan

### Phase 1: Core Infrastructure (Week 1-2)

**Goal**: Port D.A. Assets to plain C#

1. **Create Contracts Package**
   - [ ] Define `FObject` struct (remove Unity types)
   - [ ] Define `UIElement`, `LayoutData`, `StyleData`
   - [ ] Define `IUIRenderer`, `IFigmaTransformer` interfaces
   - [ ] Port all enums from D.A. Assets

2. **Create Core Package**
   - [ ] Port `TransformSetter.GetGlobalRect()` → `CalculateGlobalRect()`
   - [ ] Port `AutoLayoutExtensions` → `BuildAutoLayoutData()`
   - [ ] Port `TagSetter` → `MapElementType()`
   - [ ] Implement `FigmaTransformer`
   - [ ] Implement `FigmaToUIPipeline`

3. **Create Figma API Client**
   - [ ] HTTP client for Figma REST API
   - [ ] OAuth/token authentication
   - [ ] JSON deserialization (Newtonsoft.Json or System.Text.Json)

### Phase 2: Testing & Validation (Week 2)

1. **Unit Tests**
   - [ ] Test `FigmaTransformer` with sample Figma data
   - [ ] Test layout calculation edge cases
   - [ ] Test auto layout transformation
   - [ ] Test element type mapping

2. **Integration Tests**
   - [ ] Load real Figma design via API
   - [ ] Transform to abstract UI model
   - [ ] Validate output structure

### Phase 3: Documentation (Week 2)

1. **API Documentation**
   - [ ] XML docs for all public APIs
   - [ ] Usage examples
   - [ ] Migration guide from D.A. Assets

2. **Architecture Docs**
   - [ ] Transformation pipeline diagram
   - [ ] Layout calculation algorithm
   - [ ] Extension points

## Dependencies

- **RFC-0003**: Plugin Architecture Foundation (for renderer plugins)
- **RFC-0005**: Target Framework Compliance (for .NET Standard 2.1)
- **D.A. Assets**: Source code for porting transformation logic

## Success Criteria

1. ✅ All D.A. Assets transformation logic ported to plain C#
2. ✅ No Unity dependencies in core packages
3. ✅ Abstract UI model can represent all Figma layout features
4. ✅ `FigmaTransformer` produces correct `UIElement` tree
5. ✅ Unit tests cover all transformation scenarios
6. ✅ Framework targeting complies with RFC-0005

## Future Work

- **RFC-0023**: FigmaSharp Layout Transformation (detailed algorithm)
- **RFC-0024**: FigmaSharp Plugin Integration (WingedBean integration)
- **RFC-0025**: FigmaSharp Renderer Implementations (Terminal.Gui, Unity, Godot)
- Image asset handling (sprites, textures)
- Font loading and text rendering
- Animation and interaction support

## References

- [D.A. Assets Figma Converter](https://assetstore.unity.com/packages/tools/gui/figma-converter-for-unity-251716)
- [Figma REST API](https://www.figma.com/developers/api)
- RFC-0003: Plugin Architecture Foundation
- RFC-0005: Target Framework Compliance

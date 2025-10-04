---
title: FigmaSharp Implementation Guide
date: 2025-10-04
category: implementation, figma
status: ready-to-start
related: RFC-0022, RFC-0023, RFC-0024, RFC-0025
---

# FigmaSharp Implementation Guide

## Overview

This guide provides step-by-step instructions for implementing RFC-0022 through RFC-0025. Follow the phases in order, as each builds on the previous.

**Total Estimated Time**: 11-14 weeks  
**Team Size**: 1-2 developers  
**Prerequisites**: Familiarity with C#, .NET Standard 2.1, WingedBean plugin architecture

---

## Phase 1: Core Infrastructure (RFC-0022)

**Duration**: 2-3 weeks  
**Priority**: P0 (Foundation)  
**Goal**: Port D.A. Assets transformation logic to plain C#

### Week 1: Project Setup & Contracts

#### Step 1.1: Create Contracts Project

```bash
cd /Users/apprenticegc/Work/lunar-horse/personal-work/yokan-projects/winged-bean/development/dotnet/framework/src

# Create project
dotnet new classlib -n WingedBean.Contracts.FigmaSharp -f netstandard2.1

# Add to solution
dotnet sln ../../Framework.sln add WingedBean.Contracts.FigmaSharp/WingedBean.Contracts.FigmaSharp.csproj
```

#### Step 1.2: Define Core Types

Create `WingedBean.Contracts.FigmaSharp/Vector2.cs`:
```csharp
namespace WingedBean.Contracts.FigmaSharp;

/// <summary>
/// 2D vector (replaces UnityEngine.Vector2)
/// </summary>
public struct Vector2
{
    public float X { get; set; }
    public float Y { get; set; }
    
    public Vector2(float x, float y)
    {
        X = x;
        Y = y;
    }
    
    public static Vector2 Zero => new(0, 0);
    public static Vector2 One => new(1, 1);
    
    public static Vector2 operator +(Vector2 a, Vector2 b) => new(a.X + b.X, a.Y + b.Y);
    public static Vector2 operator -(Vector2 a, Vector2 b) => new(a.X - b.X, a.Y - b.Y);
    public static Vector2 operator *(Vector2 a, float scalar) => new(a.X * scalar, a.Y * scalar);
}
```

Create `WingedBean.Contracts.FigmaSharp/Color.cs`:
```csharp
namespace WingedBean.Contracts.FigmaSharp;

/// <summary>
/// RGBA color (replaces UnityEngine.Color)
/// </summary>
public struct Color
{
    public float R { get; set; }
    public float G { get; set; }
    public float B { get; set; }
    public float A { get; set; }
    
    public Color(float r, float g, float b, float a = 1.0f)
    {
        R = r;
        G = g;
        B = b;
        A = a;
    }
    
    public static Color White => new(1, 1, 1, 1);
    public static Color Black => new(0, 0, 0, 1);
    public static Color Transparent => new(0, 0, 0, 0);
}
```

#### Step 1.3: Port Figma Enums

Create `WingedBean.Contracts.FigmaSharp/FigmaEnums.cs`:
```csharp
namespace WingedBean.Contracts.FigmaSharp;

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

public enum LayoutMode
{
    NONE,
    HORIZONTAL,
    VERTICAL
}

public enum LayoutWrap
{
    NO_WRAP,
    WRAP
}

public enum PrimaryAxisAlignItem
{
    NONE,
    MIN,
    CENTER,
    MAX,
    SPACE_BETWEEN
}

public enum CounterAxisAlignItem
{
    NONE,
    MIN,
    CENTER,
    MAX,
    BASELINE
}

public enum PrimaryAxisSizingMode
{
    FIXED,
    AUTO
}

public enum CounterAxisSizingMode
{
    FIXED,
    AUTO
}

public enum LayoutAlign
{
    INHERIT,
    STRETCH,
    MIN,
    CENTER,
    MAX
}

public enum LayoutPositioning
{
    AUTO,
    ABSOLUTE
}
```

#### Step 1.4: Port FObject Model

Create `WingedBean.Contracts.FigmaSharp/FObject.cs`:
```csharp
namespace WingedBean.Contracts.FigmaSharp;

/// <summary>
/// Figma object model (ported from D.A. Assets)
/// </summary>
public struct FObject
{
    public string Id { get; set; }
    public string Name { get; set; }
    public NodeType Type { get; set; }
    public bool Visible { get; set; }
    
    // Layout
    public Vector2 Size { get; set; }
    public BoundingBox? AbsoluteBoundingBox { get; set; }
    public LayoutMode LayoutMode { get; set; }
    public LayoutWrap LayoutWrap { get; set; }
    public PrimaryAxisAlignItem PrimaryAxisAlignItems { get; set; }
    public CounterAxisAlignItem CounterAxisAlignItems { get; set; }
    public PrimaryAxisSizingMode PrimaryAxisSizingMode { get; set; }
    public CounterAxisSizingMode CounterAxisSizingMode { get; set; }
    public LayoutAlign LayoutAlign { get; set; }
    public LayoutPositioning LayoutPositioning { get; set; }
    
    // Spacing
    public float? ItemSpacing { get; set; }
    public float? CounterAxisSpacing { get; set; }
    public float? PaddingLeft { get; set; }
    public float? PaddingRight { get; set; }
    public float? PaddingTop { get; set; }
    public float? PaddingBottom { get; set; }
    
    // Hierarchy
    public List<FObject>? Children { get; set; }
    public FObject? Parent { get; set; }
    
    // Styling
    public List<Paint>? Fills { get; set; }
    public List<Paint>? Strokes { get; set; }
    public float StrokeWeight { get; set; }
    public List<Effect>? Effects { get; set; }
    
    // Text
    public string? Characters { get; set; }
    public TypeStyle? Style { get; set; }
    
    // Other
    public float? LayoutGrow { get; set; }
    public float Rotation { get; set; }
}

public struct BoundingBox
{
    public float X { get; set; }
    public float Y { get; set; }
    public float Width { get; set; }
    public float Height { get; set; }
}

public struct Paint
{
    public string Type { get; set; }
    public bool Visible { get; set; }
    public Color Color { get; set; }
}

public struct Effect
{
    public string Type { get; set; }
    public bool Visible { get; set; }
    public Vector2 Offset { get; set; }
    public float Radius { get; set; }
    public Color Color { get; set; }
}

public struct TypeStyle
{
    public string FontFamily { get; set; }
    public float FontSize { get; set; }
    public float FontWeight { get; set; }
    public string TextAlignHorizontal { get; set; }
    public string TextAlignVertical { get; set; }
}
```

#### Step 1.5: Define Abstract UI Model

Create `WingedBean.Contracts.FigmaSharp/UIElement.cs`:
```csharp
namespace WingedBean.Contracts.FigmaSharp;

/// <summary>
/// Framework-agnostic UI element
/// </summary>
public class UIElement
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public UIElementType Type { get; set; }
    public LayoutData Layout { get; set; } = new();
    public StyleData Style { get; set; } = new();
    public List<UIElement> Children { get; set; } = new();
    public Dictionary<string, object> Metadata { get; set; } = new();
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
```

Create `WingedBean.Contracts.FigmaSharp/LayoutData.cs`:
```csharp
namespace WingedBean.Contracts.FigmaSharp;

/// <summary>
/// Framework-agnostic layout data
/// </summary>
public class LayoutData
{
    // Position
    public PositionMode PositionMode { get; set; }
    public Vector2 AbsolutePosition { get; set; }
    public AnchorData? Anchors { get; set; }
    public AlignmentData? Alignment { get; set; }
    
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
    public Padding? Padding { get; set; }
}

public enum LayoutDirection { Horizontal, Vertical, None }
public enum PrimaryAxisAlign { Start, Center, End, SpaceBetween }
public enum CrossAxisAlign { Start, Center, End, Stretch, Baseline }
public enum SizingMode { Fixed, Auto }

public struct Padding
{
    public float Left { get; set; }
    public float Right { get; set; }
    public float Top { get; set; }
    public float Bottom { get; set; }
}

public struct Margin
{
    public float Left { get; set; }
    public float Right { get; set; }
    public float Top { get; set; }
    public float Bottom { get; set; }
}

public class AnchorData
{
    public bool Left { get; set; }
    public bool Right { get; set; }
    public bool Top { get; set; }
    public bool Bottom { get; set; }
}

public class AlignmentData
{
    public HorizontalAlign Horizontal { get; set; }
    public VerticalAlign Vertical { get; set; }
}

public enum HorizontalAlign { Left, Center, Right }
public enum VerticalAlign { Top, Center, Bottom }
```

Create `WingedBean.Contracts.FigmaSharp/StyleData.cs`:
```csharp
namespace WingedBean.Contracts.FigmaSharp;

/// <summary>
/// Framework-agnostic style data
/// </summary>
public class StyleData
{
    public Color BackgroundColor { get; set; }
    public Color ForegroundColor { get; set; }
    public BorderStyle? Border { get; set; }
    public ShadowStyle? Shadow { get; set; }
    public TextStyle? Text { get; set; }
}

public class BorderStyle
{
    public float Width { get; set; }
    public Color Color { get; set; }
    public float TopLeftRadius { get; set; }
    public float TopRightRadius { get; set; }
    public float BottomLeftRadius { get; set; }
    public float BottomRightRadius { get; set; }
}

public class ShadowStyle
{
    public float OffsetX { get; set; }
    public float OffsetY { get; set; }
    public float Blur { get; set; }
    public Color Color { get; set; }
}

public class TextStyle
{
    public string Content { get; set; } = string.Empty;
    public string FontFamily { get; set; } = "Arial";
    public float FontSize { get; set; } = 14;
    public bool Bold { get; set; }
    public bool Italic { get; set; }
    public Color Color { get; set; }
    public HorizontalAlign HorizontalAlign { get; set; }
    public VerticalAlign VerticalAlign { get; set; }
}
```

#### Step 1.6: Define Renderer Interface

Create `WingedBean.Contracts.FigmaSharp/IUIRenderer.cs`:
```csharp
namespace WingedBean.Contracts.FigmaSharp;

/// <summary>
/// Interface for framework-specific UI renderers
/// </summary>
public interface IUIRenderer
{
    /// <summary>
    /// Framework name (e.g., "Terminal.Gui v2", "Unity UGUI")
    /// </summary>
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
```

Create `WingedBean.Contracts.FigmaSharp/IFigmaTransformer.cs`:
```csharp
namespace WingedBean.Contracts.FigmaSharp;

/// <summary>
/// Transforms Figma objects to abstract UI elements
/// </summary>
public interface IFigmaTransformer
{
    /// <summary>
    /// Transform a Figma object to abstract UI element
    /// </summary>
    UIElement Transform(FObject figmaObject);
    
    /// <summary>
    /// Load Figma project from API
    /// </summary>
    Task<FigmaProject> LoadFromApiAsync(string fileKey, string token, CancellationToken ct = default);
}

public class FigmaProject
{
    public string Name { get; set; } = string.Empty;
    public FObject Document { get; set; }
}
```

### Week 2: Core Transformer Implementation

#### Step 2.1: Create Core Project

```bash
cd /Users/apprenticegc/Work/lunar-horse/personal-work/yokan-projects/winged-bean/development/dotnet/framework/src

# Create project
dotnet new classlib -n WingedBean.FigmaSharp.Core -f netstandard2.1

# Add reference to contracts
cd WingedBean.FigmaSharp.Core
dotnet add reference ../WingedBean.Contracts.FigmaSharp/WingedBean.Contracts.FigmaSharp.csproj

# Add to solution
cd ../..
dotnet sln Framework.sln add src/WingedBean.FigmaSharp.Core/WingedBean.FigmaSharp.Core.csproj
```

#### Step 2.2: Implement FigmaTransformer

Create `WingedBean.FigmaSharp.Core/FigmaTransformer.cs`:
```csharp
using WingedBean.Contracts.FigmaSharp;

namespace WingedBean.FigmaSharp.Core;

public class FigmaTransformer : IFigmaTransformer
{
    private readonly LayoutCalculator _layoutCalculator;
    private readonly StyleBuilder _styleBuilder;
    private readonly ElementTypeMapper _typeMapper;
    
    public FigmaTransformer()
    {
        _layoutCalculator = new LayoutCalculator();
        _styleBuilder = new StyleBuilder();
        _typeMapper = new ElementTypeMapper();
    }
    
    public UIElement Transform(FObject figmaObject)
    {
        var element = new UIElement
        {
            Id = figmaObject.Id,
            Name = figmaObject.Name,
            Type = _typeMapper.MapElementType(figmaObject),
            Layout = _layoutCalculator.BuildLayoutData(figmaObject),
            Style = _styleBuilder.BuildStyleData(figmaObject),
            Children = figmaObject.Children?.Select(Transform).ToList() ?? new()
        };
        
        return element;
    }
    
    public async Task<FigmaProject> LoadFromApiAsync(string fileKey, string token, CancellationToken ct = default)
    {
        // TODO: Implement Figma API client
        throw new NotImplementedException("Figma API client not yet implemented");
    }
}
```

#### Step 2.3: Port Layout Calculator

Create `WingedBean.FigmaSharp.Core/LayoutCalculator.cs`:
```csharp
using WingedBean.Contracts.FigmaSharp;

namespace WingedBean.FigmaSharp.Core;

/// <summary>
/// Calculates layout data from Figma objects
/// Ported from D.A. Assets TransformSetter.cs
/// </summary>
public class LayoutCalculator
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
    
    private FRect CalculateGlobalRect(FObject figma)
    {
        // TODO: Port from D.A. Assets TransformSetter.GetGlobalRect()
        // This is the core layout calculation algorithm
        
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
            rect.Position = Vector2.Zero;
            rect.Size = figma.Size;
        }
        
        // Handle rotation
        rect.Angle = figma.Rotation;
        
        return rect;
    }
}

internal struct FRect
{
    public Vector2 Position { get; set; }
    public Vector2 Size { get; set; }
    public float Angle { get; set; }
    public Padding Padding { get; set; }
}
```

#### Step 2.4: Implement Pipeline

Create `WingedBean.FigmaSharp.Core/FigmaToUIPipeline.cs`:
```csharp
using WingedBean.Contracts.FigmaSharp;

namespace WingedBean.FigmaSharp.Core;

/// <summary>
/// Orchestrates Figma-to-UI transformation
/// </summary>
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
        foreach (var child in element.Children)
        {
            var childTarget = RenderElement(child);
            _renderer.AddChild(target, childTarget);
        }
        
        return target;
    }
}
```

### Week 3: Testing & Documentation

#### Step 3.1: Create Unit Tests

```bash
cd /Users/apprenticegc/Work/lunar-horse/personal-work/yokan-projects/winged-bean/development/dotnet/framework/tests

# Create test project
dotnet new xunit -n WingedBean.FigmaSharp.Core.Tests -f net8.0

# Add references
cd WingedBean.FigmaSharp.Core.Tests
dotnet add reference ../../src/WingedBean.FigmaSharp.Core/WingedBean.FigmaSharp.Core.csproj
dotnet add reference ../../src/WingedBean.Contracts.FigmaSharp/WingedBean.Contracts.FigmaSharp.csproj

# Add to solution
cd ../..
dotnet sln Framework.sln add tests/WingedBean.FigmaSharp.Core.Tests/WingedBean.FigmaSharp.Core.Tests.csproj
```

Create test file `FigmaTransformerTests.cs`:
```csharp
using WingedBean.Contracts.FigmaSharp;
using WingedBean.FigmaSharp.Core;
using Xunit;

namespace WingedBean.FigmaSharp.Core.Tests;

public class FigmaTransformerTests
{
    [Fact]
    public void Transform_SimpleFrame_CreatesContainer()
    {
        // Arrange
        var figma = new FObject
        {
            Id = "1:1",
            Name = "TestFrame",
            Type = NodeType.FRAME,
            Size = new Vector2(100, 100),
            AbsoluteBoundingBox = new BoundingBox { X = 0, Y = 0, Width = 100, Height = 100 }
        };
        
        var transformer = new FigmaTransformer();
        
        // Act
        var result = transformer.Transform(figma);
        
        // Assert
        Assert.Equal("1:1", result.Id);
        Assert.Equal("TestFrame", result.Name);
        Assert.Equal(UIElementType.Container, result.Type);
        Assert.Equal(100, result.Layout.FixedWidth);
        Assert.Equal(100, result.Layout.FixedHeight);
    }
    
    // TODO: Add more tests
}
```

#### Step 3.2: Build & Verify

```bash
cd /Users/apprenticegc/Work/lunar-horse/personal-work/yokan-projects/winged-bean/development/dotnet

# Build all projects
dotnet build framework/Framework.sln

# Run tests
dotnet test framework/Framework.sln
```

---

## Phase 2: Layout Transformation (RFC-0023)

**Duration**: 2 weeks  
**Priority**: P0  
**Goal**: Implement detailed layout algorithms

### Tasks

1. Port `TransformSetter.GetGlobalRect()` completely
2. Implement `AutoLayoutTransformer` with SPACE_BETWEEN
3. Implement `AlignmentMapper` for all 9 combinations
4. Implement `SizeModeCalculator` with LayoutGrow/LayoutAlign
5. Implement `PaddingCalculator` with adjustment logic
6. Add comprehensive unit tests

**See RFC-0023 for detailed specifications.**

---

## Phase 3: Plugin Integration (RFC-0024)

**Duration**: 2-3 weeks  
**Priority**: P1  
**Goal**: Integrate with WingedBean plugin system

### Tasks

1. Create plugin manifest templates
2. Implement `IPluginActivator` for renderers
3. Test service registration via DI
4. Validate hot-reload functionality
5. Test profile-specific loading

**See RFC-0024 for detailed specifications.**

---

## Phase 4: Terminal.Gui Renderer (RFC-0025)

**Duration**: 2 weeks  
**Priority**: P1  
**Goal**: First renderer implementation

### Tasks

1. Create `WingedBean.Plugins.FigmaSharp.TerminalGui` project
2. Implement `TerminalGuiRenderer`
3. Implement pixel-to-character conversion
4. Implement manual auto-layout positioning
5. Implement color mapping
6. Create integration tests

**See RFC-0025 for detailed specifications.**

---

## Quick Start Commands

```bash
# Navigate to framework directory
cd /Users/apprenticegc/Work/lunar-horse/personal-work/yokan-projects/winged-bean/development/dotnet/framework

# Create all projects at once
dotnet new classlib -n src/WingedBean.Contracts.FigmaSharp -f netstandard2.1
dotnet new classlib -n src/WingedBean.FigmaSharp.Core -f netstandard2.1
dotnet new xunit -n tests/WingedBean.FigmaSharp.Core.Tests -f net8.0

# Add references
cd src/WingedBean.FigmaSharp.Core
dotnet add reference ../WingedBean.Contracts.FigmaSharp/WingedBean.Contracts.FigmaSharp.csproj

cd ../../tests/WingedBean.FigmaSharp.Core.Tests
dotnet add reference ../../src/WingedBean.FigmaSharp.Core/WingedBean.FigmaSharp.Core.csproj
dotnet add reference ../../src/WingedBean.Contracts.FigmaSharp/WingedBean.Contracts.FigmaSharp.csproj

# Add to solution
cd ../..
dotnet sln Framework.sln add src/WingedBean.Contracts.FigmaSharp/WingedBean.Contracts.FigmaSharp.csproj
dotnet sln Framework.sln add src/WingedBean.FigmaSharp.Core/WingedBean.FigmaSharp.Core.csproj
dotnet sln Framework.sln add tests/WingedBean.FigmaSharp.Core.Tests/WingedBean.FigmaSharp.Core.Tests.csproj

# Build
dotnet build Framework.sln

# Run tests
dotnet test Framework.sln
```

---

## Next Steps

1. ✅ Review this implementation guide
2. ✅ Set up development environment
3. ✅ Create Phase 1 projects (Week 1)
4. ✅ Implement core contracts (Week 1)
5. ✅ Port transformation logic (Week 2)
6. ✅ Add unit tests (Week 3)
7. → Continue with Phase 2 (RFC-0023)

---

## References

- [FigmaSharp Architecture Analysis](../design/figmasharp-architecture-analysis.md)
- RFC-0022: FigmaSharp Core Architecture
- RFC-0023: FigmaSharp Layout Transformation
- RFC-0024: FigmaSharp Plugin Integration
- RFC-0025: FigmaSharp Renderer Implementations

---

**Status**: ✅ Ready to Start  
**Last Updated**: 2025-10-04  
**Next Review**: After Phase 1 completion

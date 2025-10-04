---
title: FigmaSharp Architecture Analysis & Findings
date: 2025-10-04
category: design, architecture, figma
status: completed
related: RFC-0022, RFC-0023, RFC-0024, RFC-0025
---

# FigmaSharp Architecture Analysis & Findings

## Executive Summary

This document captures the architectural analysis and key findings from evaluating the feasibility of porting the D.A. Assets Figma-to-Unity plugin to plain C# and integrating it with the WingedBean plugin architecture.

**Verdict**: ✅ **Highly Feasible with Excellent Integration Potential**

The D.A. Assets plugin's architecture aligns perfectly with WingedBean's 4-tier plugin system, enabling a clean separation between framework-agnostic transformation logic and framework-specific renderers.

---

## Key Findings

### 1. D.A. Assets Architecture Analysis

**Current Structure** (Unity-specific):
```
Figma API → FObject Model → [CanvasDrawer | NovaDrawer | UITK_Converter]
                                    ↓              ↓              ↓
                                  UGUI          Nova UI      UI Toolkit
```

**Key Components Identified**:
- ✅ **Portable**: Figma API client, FObject model, layout calculation logic
- ⚠️ **Needs Replacement**: Unity types (`GameObject`, `RectTransform`, `Vector2`, `Color`)
- ⚠️ **Framework-Specific**: Drawer implementations for each UI framework

**Critical Insight**: The transformation logic in `TransformSetter`, `AutoLayoutExtensions`, and `TagSetter` is **framework-agnostic** and can be ported to plain C# without Unity dependencies.

---

### 2. Layout Transformation Algorithm

**Core Logic** (from D.A. Assets):

1. **Global Rect Calculation** (`TransformSetter.GetGlobalRect()`):
   - Calculates absolute position and size
   - Handles rotation and bounding boxes
   - Adjusts for sprite scaling

2. **Auto-Layout Transformation** (`AutoLayoutExtensions`):
   - Maps Figma's flexbox-like layout to framework layouts
   - Handles horizontal/vertical/wrap modes
   - Calculates SPACE_BETWEEN distribution
   - Processes LayoutGrow and LayoutAlign

3. **Alignment Mapping**:
   - Converts Figma's primary/counter axis alignment
   - Maps to framework-specific alignment enums
   - Handles 9 alignment combinations

4. **Padding Calculation**:
   - Extracts padding from Figma properties
   - Adjusts padding when children exceed parent size
   - Maintains aspect ratios

**Portability**: All algorithms use basic math and data structures - **100% portable to plain C#**.

---

### 3. Framework Comparison

#### Figma Auto-Layout vs. Target Frameworks

| Feature | Figma | Terminal.Gui v2 | Unity UGUI | Unity UI Toolkit | Godot UI |
|---------|-------|-----------------|------------|------------------|----------|
| **Layout System** | Flexbox-like | Manual Pos/Dim | LayoutGroup | Flexbox | Container nodes |
| **Units** | Pixels | Characters | Pixels | Pixels | Pixels |
| **Coordinate Origin** | Top-left | Top-left | Bottom-left | Top-left | Top-left |
| **Auto-Layout** | Native | Manual positioning | LayoutGroup components | Native flexbox | Container nodes |
| **Rotation** | Yes | No | Yes | No | Yes |
| **Gradients** | Yes | No | Yes | Yes | Yes |
| **Shadows** | Yes | No | Yes | Yes | Yes |

**Key Insight**: Terminal.Gui requires the most adaptation (character-based units, manual layout), while UI Toolkit has the best native support for Figma's layout model.

---

### 4. WingedBean Integration Analysis

**Perfect Architectural Fit**:

```
┌─────────────────────────────────────────────────────────────────┐
│                    WingedBean Host                              │
│  - Plugin Discovery & Loading (RFC-0003)                        │
│  - Dependency Resolution                                        │
│  - Hot-Reload Support                                           │
└─────────────────────────────────────────────────────────────────┘
                             ↓
┌─────────────────────────────────────────────────────────────────┐
│           FigmaSharp Core (Tier 1/2 - Framework)                │
│  - WingedBean.Contracts.FigmaSharp (.NET Standard 2.1)          │
│  - WingedBean.FigmaSharp.Core (.NET Standard 2.1)               │
│  - Provides: IUIRenderer, IFigmaTransformer                     │
└─────────────────────────────────────────────────────────────────┘
                             ↓
┌─────────────────────────────────────────────────────────────────┐
│         Renderer Plugins (Tier 4 - Hot-Swappable)               │
│  - WingedBean.Plugins.FigmaSharp.TerminalGui                    │
│  - WingedBean.Plugins.FigmaSharp.UnityUGUI                      │
│  - WingedBean.Plugins.FigmaSharp.UIToolkit                      │
│  - WingedBean.Plugins.FigmaSharp.Godot                          │
└─────────────────────────────────────────────────────────────────┘
```

**Benefits of Integration**:
1. ✅ **Hot-Swappable Renderers**: Switch frameworks at runtime
2. ✅ **Profile-Specific Loading**: Console loads Terminal.Gui, Unity loads UGUI
3. ✅ **Dependency Resolution**: Core loads before renderers automatically
4. ✅ **Service Composition**: Multiple renderers can coexist
5. ✅ **Framework Targeting**: Complies with RFC-0005 (.NET Standard 2.1)

---

### 5. Abstraction Layer Design

**Three-Layer Architecture**:

```
Layer 1: Figma Domain Model (Plain C#)
├── FObject struct (no Unity types)
├── Vector2, Color (custom structs)
└── Layout enums (LayoutMode, PrimaryAxisAlign, etc.)

Layer 2: Abstract UI Model (Framework-Agnostic)
├── UIElement (type, layout, style, children)
├── LayoutData (position, size, auto-layout, padding)
└── StyleData (colors, borders, shadows, text)

Layer 3: Framework Renderers (Specific Implementations)
├── IUIRenderer interface
├── CreateContainer/Text/Button/Image()
├── ApplyLayout(target, LayoutData)
└── ApplyStyle(target, StyleData)
```

**Key Design Decision**: The abstract UI model is **framework-agnostic** but **expressive enough** to represent all Figma layout features. Renderers translate this model to framework-specific APIs.

---

### 6. Porting Strategy

**What to Port from D.A. Assets**:

| Component | Source | Destination | Complexity |
|-----------|--------|-------------|------------|
| FObject model | `Model/SyncData.cs` | `WingedBean.Contracts.FigmaSharp` | Low |
| Layout calculation | `TransformSetter.cs` | `FigmaSharp.Core/LayoutCalculator.cs` | Medium |
| Auto-layout logic | `AutoLayoutExtensions.cs` | `FigmaSharp.Core/AutoLayoutTransformer.cs` | Medium |
| Alignment mapping | `AutoLayoutExtensions.cs` | `FigmaSharp.Core/AlignmentMapper.cs` | Low |
| Tag classification | `TagSetter.cs` | `FigmaSharp.Core/ElementTypeMapper.cs` | Low |

**What to Replace**:
- `UnityEngine.Vector2` → Custom `Vector2` struct
- `UnityEngine.Color` → Custom `Color` struct
- `GameObject` → Abstract `UIElement`
- `RectTransform` → Abstract `LayoutData`
- `HorizontalLayoutGroup` → Framework-specific implementation

**Estimated Effort**: 2-3 weeks for core porting, 1-2 weeks per renderer implementation.

---

### 7. Terminal.Gui Specific Challenges

**Unique Constraints**:
1. **Character-Based Units**: Must convert pixels to character cells (8x16 ratio)
2. **No Native Auto-Layout**: Must manually position children
3. **Limited Colors**: 16-color palette (map RGB to nearest)
4. **No Rotation**: Skip rotated elements or warn
5. **Text-Only**: No gradients, shadows, or pixel-perfect rendering

**Adaptation Strategy**:
```csharp
// Pixel-to-character conversion
int charX = (int)(layout.AbsolutePosition.X / 8);
int charY = (int)(layout.AbsolutePosition.Y / 16);

// Manual auto-layout positioning
if (autoLayout.Direction == LayoutDirection.Horizontal) {
    int currentX = 0;
    foreach (var child in children) {
        child.X = Pos.At(currentX);
        currentX += child.Frame.Width + spacing;
    }
}
```

**Feasibility**: ✅ Achievable with custom adapter logic.

---

### 8. Unity Renderer Advantages

**UGUI Renderer**:
- ✅ Direct mapping to `HorizontalLayoutGroup`/`VerticalLayoutGroup`
- ✅ Full styling support (colors, borders, shadows)
- ✅ Rotation support
- ✅ Prefab generation for reusability

**UI Toolkit Renderer**:
- ✅ Native flexbox (perfect for Figma auto-layout)
- ✅ USS styling (CSS-like)
- ✅ UXML generation (declarative UI)
- ✅ Better performance than UGUI

**Key Insight**: UI Toolkit is the **best target** for Figma designs due to native flexbox support.

---

### 9. Plugin Manifest Design

**Example: Terminal.Gui Renderer Plugin**

```json
{
  "id": "wingedbean.plugins.figmasharp.terminalgui",
  "version": "1.0.0",
  "name": "FigmaSharp Terminal.Gui Renderer",
  "description": "Renders Figma designs to Terminal.Gui v2",
  
  "entryPoint": {
    "dotnet": "./WingedBean.Plugins.FigmaSharp.TerminalGui.dll"
  },
  
  "dependencies": {
    "wingedbean.contracts.figmasharp": "^1.0.0",
    "wingedbean.figmasharp.core": "^1.0.0"
  },
  
  "exports": {
    "services": [
      {
        "interface": "IUIRenderer",
        "implementation": "TerminalGuiRenderer",
        "lifecycle": "singleton"
      }
    ]
  },
  
  "capabilities": ["ui-rendering", "terminal-gui", "figma-transformation"],
  "supportedProfiles": ["console"],
  "loadStrategy": "lazy"
}
```

**Benefits**:
- ✅ Automatic dependency resolution (core loads first)
- ✅ Profile-specific loading (only loads in console profile)
- ✅ Service registration via DI
- ✅ Hot-reload support

---

### 10. Relationship to Other RFCs

**FigmaSharp RFCs (0022-0025)**:
- Focus: **UI design transformation** (Figma → code)
- Domain: Design-to-code pipeline
- Dependencies: RFC-0003 (Plugins), RFC-0005 (Framework Targeting)

**Input Action System (RFC-0026)**:
- Focus: **Game input handling** (keyboard/gamepad → actions)
- Domain: Runtime input for gameplay
- Dependencies: RFC-0021 (Input Mapping), RFC-0020 (Scene Service)

**Relationship**: ❌ **Not directly related**

**Potential Integration**: UI generated by FigmaSharp can be wired to Input Actions for navigation:
```csharp
// FigmaSharp generates menu UI
var menuUI = figmaSharpPipeline.Convert(figmaMenuDesign);

// Input Actions handle menu navigation
menuActionMap["Navigate"].performed += (evt) => {
    menuUI.Navigate(evt.ReadValue<Vector2>());
};
```

But they solve different problems and don't have architectural dependencies.

---

## Implementation Recommendations

### Phase 1: Core Infrastructure (RFC-0022)
**Priority**: P0 (Foundation)  
**Effort**: 2-3 weeks  
**Focus**: Port D.A. Assets to plain C#

**Deliverables**:
1. `WingedBean.Contracts.FigmaSharp` (Tier 1)
   - FObject model (no Unity types)
   - IUIRenderer, IFigmaTransformer interfaces
   - Abstract UI model (UIElement, LayoutData, StyleData)

2. `WingedBean.FigmaSharp.Core` (Tier 2)
   - FigmaTransformer (port from D.A. Assets)
   - LayoutCalculator (port TransformSetter)
   - AutoLayoutTransformer (port AutoLayoutExtensions)
   - FigmaToUIPipeline (orchestration)

3. Unit tests for transformation logic

### Phase 2: Layout Transformation (RFC-0023)
**Priority**: P0 (Foundation)  
**Effort**: 2 weeks  
**Focus**: Detailed layout algorithm implementation

**Deliverables**:
1. Global rect calculation with rotation support
2. Auto-layout transformation (horizontal/vertical/wrap)
3. SPACE_BETWEEN distribution
4. LayoutGrow and LayoutAlign handling
5. Padding calculation with adjustment
6. Comprehensive unit tests

### Phase 3: Plugin Integration (RFC-0024)
**Priority**: P1 (High)  
**Effort**: 2-3 weeks  
**Focus**: WingedBean plugin system integration

**Deliverables**:
1. Plugin manifest templates
2. IPluginActivator implementations
3. Service registration via DI
4. Hot-reload testing
5. Profile-specific loading validation

### Phase 4: Terminal.Gui Renderer (RFC-0025)
**Priority**: P1 (High)  
**Effort**: 2 weeks  
**Focus**: First renderer implementation

**Deliverables**:
1. TerminalGuiRenderer implementation
2. Pixel-to-character conversion
3. Manual auto-layout positioning
4. Color mapping (RGB → 16-color palette)
5. Integration tests with real Figma designs

### Phase 5: Unity Renderers (RFC-0025)
**Priority**: P2 (Medium)  
**Effort**: 3-4 weeks  
**Focus**: Unity UGUI and UI Toolkit renderers

**Deliverables**:
1. UguiRenderer (RectTransform, LayoutGroup)
2. UIToolkitRenderer (Flexbox, USS styling)
3. Prefab/UXML generation
4. Unity integration tests

### Phase 6: Godot Renderer (RFC-0025)
**Priority**: P3 (Low)  
**Effort**: 2 weeks  
**Focus**: Godot UI renderer

**Deliverables**:
1. GodotRenderer (Control nodes, Container layout)
2. Godot integration tests

---

## Success Metrics

### Technical Metrics
- ✅ All D.A. Assets transformation logic ported to plain C#
- ✅ No Unity dependencies in Tier 1/2 packages
- ✅ 100% unit test coverage for layout algorithms
- ✅ <1ms transformation time for typical Figma designs
- ✅ Hot-reload works for renderer plugins

### Functional Metrics
- ✅ Terminal.Gui renderer produces correct character-based layout
- ✅ Unity UGUI renderer matches D.A. Assets output
- ✅ UI Toolkit renderer leverages native flexbox
- ✅ Profile-specific loading works (console vs Unity)
- ✅ Multiple renderers can coexist

### Quality Metrics
- ✅ RFC-0005 framework targeting compliance
- ✅ RFC-0003 plugin architecture compliance
- ✅ Clean separation of concerns (Tier 1/2/4)
- ✅ Comprehensive documentation and examples

---

## Risks & Mitigations

### Risk 1: Complexity of Layout Algorithm
**Impact**: High  
**Probability**: Medium  
**Mitigation**: Port incrementally, validate each step with unit tests, reference D.A. Assets implementation

### Risk 2: Terminal.Gui Limitations
**Impact**: Medium  
**Probability**: High  
**Mitigation**: Document limitations upfront, provide fallback strategies, focus on common use cases

### Risk 3: Unity Renderer Parity
**Impact**: Medium  
**Probability**: Low  
**Mitigation**: Use D.A. Assets as reference, validate output visually, add regression tests

### Risk 4: Plugin System Integration
**Impact**: High  
**Probability**: Low  
**Mitigation**: Leverage existing WingedBean plugin infrastructure, test hot-reload thoroughly

---

## Conclusion

The FigmaSharp architecture is **well-designed and highly feasible**. The integration with WingedBean's plugin system provides significant benefits:

1. ✅ **Reusable transformation logic** across all frameworks
2. ✅ **Hot-swappable renderers** without code changes
3. ✅ **Profile-specific loading** for optimal performance
4. ✅ **Clean separation** between core and renderers
5. ✅ **Extensible** for future frameworks (Blazor, MAUI, etc.)

The D.A. Assets plugin provides an excellent foundation, and the porting effort is **justified by the multi-framework benefits**.

**Recommendation**: ✅ **Proceed with implementation** following the phased approach outlined above.

---

## References

- [D.A. Assets Figma Converter](https://assetstore.unity.com/packages/tools/gui/figma-converter-for-unity-251716)
- [Figma REST API](https://www.figma.com/developers/api)
- [Terminal.Gui v2 Documentation](https://gui-cs.github.io/Terminal.Gui/)
- [Unity UGUI Documentation](https://docs.unity3d.com/Manual/UISystem.html)
- [Unity UI Toolkit Documentation](https://docs.unity3d.com/Manual/UIElements.html)
- RFC-0003: Plugin Architecture Foundation
- RFC-0005: Target Framework Compliance
- RFC-0022: FigmaSharp Core Architecture
- RFC-0023: FigmaSharp Layout Transformation
- RFC-0024: FigmaSharp Plugin Integration
- RFC-0025: FigmaSharp Renderer Implementations

---

**Document Status**: ✅ Completed  
**Date**: 2025-10-04  
**Authors**: Claude Code & ApprenticeGC  
**Next Steps**: Begin RFC-0022 implementation (Phase 1)

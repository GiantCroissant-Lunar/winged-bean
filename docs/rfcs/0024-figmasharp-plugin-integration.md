---
id: RFC-0024
title: FigmaSharp Plugin Integration
status: Draft
category: architecture, plugins, figma
created: 2025-10-04
updated: 2025-10-04
---

# RFC-0024: FigmaSharp Plugin Integration

## Status

Draft

## Date

2025-10-04

## Summary

Integrate **FigmaSharp** with the **WingedBean plugin architecture** (RFC-0003) to enable hot-swappable UI renderers. FigmaSharp core will be a framework package, while renderer implementations (Terminal.Gui, Unity UGUI, UI Toolkit, Godot) will be **plugins** that can be loaded, unloaded, and swapped at runtime.

**Key Principle**: FigmaSharp core provides interfaces and transformation logic; renderers are plugins that implement `IUIRenderer` and register via `IPluginActivator`.

## Motivation

### Vision

Build a **plugin-based UI transformation system** where:

1. **Core transformation logic** is shared across all frameworks
2. **Renderer plugins** are hot-swappable without restart
3. **Profile-specific loading** ensures only relevant renderers load (Console → Terminal.Gui, Unity → UGUI/UI Toolkit)
4. **Dependency resolution** ensures core loads before renderers
5. **Service composition** allows multiple renderers to coexist

### Problems to Solve

1. **Monolithic Renderers**: Current approach requires recompiling to switch frameworks
2. **No Hot-Reload**: Cannot swap renderers at runtime (e.g., Terminal.Gui → Unity UGUI)
3. **Profile Mismatch**: Unity renderers shouldn't load in console profile
4. **Dependency Management**: Renderers depend on core; need automatic resolution
5. **Service Discovery**: Host needs to find and activate renderers

### Current WingedBean Plugin System

From RFC-0003, WingedBean provides:

- **Plugin Discovery**: Scans directories for `.plugin.json` manifests
- **Dependency Resolution**: Topological sort for load order
- **Hot-Reload**: AssemblyLoadContext-based unload/reload
- **Profile-Agnostic**: Console (ALC), Unity (HybridCLR), Godot (ALC), Web (ES modules)
- **Service Registration**: Plugins register services via `IPluginActivator`

**Perfect fit for FigmaSharp!**

## Proposal

### Architecture Overview

```
┌─────────────────────────────────────────────────────────────────┐
│                    WingedBean Host                              │
│  - PluginDiscovery (scan ./plugins)                             │
│  - PluginDependencyResolver (topological sort)                  │
│  - AlcPluginLoader (load/unload/reload)                         │
│  - HostBootstrap (orchestrate lifecycle)                        │
└─────────────────────────────────────────────────────────────────┘
                             ↓ discovers & loads
┌─────────────────────────────────────────────────────────────────┐
│           FigmaSharp Core (Framework Package)                   │
│  - WingedBean.Contracts.FigmaSharp (Tier 1)                     │
│  - WingedBean.FigmaSharp.Core (Tier 2)                          │
│  - Provides: IUIRenderer, IFigmaTransformer                     │
└─────────────────────────────────────────────────────────────────┘
                             ↓ depends on
┌─────────────────────────────────────────────────────────────────┐
│         Renderer Plugins (Tier 4 - Hot-Swappable)               │
│                                                                 │
│  ┌───────────────────────────────────────────────────────────┐ │
│  │  WingedBean.Plugins.FigmaSharp.TerminalGui                │ │
│  │  - Implements IUIRenderer                                 │ │
│  │  - Registers via IPluginActivator                         │ │
│  │  - supportedProfiles: ["console"]                         │ │
│  └───────────────────────────────────────────────────────────┘ │
│                                                                 │
│  ┌───────────────────────────────────────────────────────────┐ │
│  │  WingedBean.Plugins.FigmaSharp.UnityUGUI                  │ │
│  │  - Implements IUIRenderer                                 │ │
│  │  - Registers via IPluginActivator                         │ │
│  │  - supportedProfiles: ["unity"]                           │ │
│  └───────────────────────────────────────────────────────────┘ │
│                                                                 │
│  ┌───────────────────────────────────────────────────────────┐ │
│  │  WingedBean.Plugins.FigmaSharp.UIToolkit                  │ │
│  │  - Implements IUIRenderer                                 │ │
│  │  - Registers via IPluginActivator                         │ │
│  │  - supportedProfiles: ["unity"]                           │ │
│  └───────────────────────────────────────────────────────────┘ │
└─────────────────────────────────────────────────────────────────┘
```

### Plugin Manifest Structure

#### Core Package (Not a Plugin)

FigmaSharp core is a **framework package**, not a plugin:

```xml
<!-- WingedBean.FigmaSharp.Core.csproj -->
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>netstandard2.1</TargetFramework>
  </PropertyGroup>
  
  <ItemGroup>
    <ProjectReference Include="../WingedBean.Contracts.FigmaSharp/WingedBean.Contracts.FigmaSharp.csproj" />
  </ItemGroup>
</Project>
```

#### Terminal.Gui Renderer Plugin

```json
// WingedBean.Plugins.FigmaSharp.TerminalGui.plugin.json
{
  "id": "wingedbean.plugins.figmasharp.terminalgui",
  "version": "1.0.0",
  "name": "FigmaSharp Terminal.Gui Renderer",
  "description": "Renders Figma designs to Terminal.Gui v2 console UI",
  "author": "WingedBean Team",
  "license": "MIT",
  
  "entryPoint": {
    "dotnet": "./WingedBean.Plugins.FigmaSharp.TerminalGui.dll",
    "nodejs": null,
    "unity": null,
    "godot": null
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
  "loadStrategy": "lazy",
  "quiesceSeconds": 5
}
```

#### Unity UGUI Renderer Plugin

```json
// WingedBean.Plugins.FigmaSharp.UnityUGUI.plugin.json
{
  "id": "wingedbean.plugins.figmasharp.ugui",
  "version": "1.0.0",
  "name": "FigmaSharp Unity UGUI Renderer",
  "description": "Renders Figma designs to Unity UGUI",
  
  "entryPoint": {
    "dotnet": null,
    "nodejs": null,
    "unity": "./WingedBean.Plugins.FigmaSharp.UnityUGUI.dll",
    "godot": null
  },
  
  "dependencies": {
    "wingedbean.contracts.figmasharp": "^1.0.0",
    "wingedbean.figmasharp.core": "^1.0.0"
  },
  
  "exports": {
    "services": [
      {
        "interface": "IUIRenderer",
        "implementation": "UguiRenderer",
        "lifecycle": "singleton"
      }
    ]
  },
  
  "capabilities": ["ui-rendering", "unity-ugui", "figma-transformation"],
  "supportedProfiles": ["unity"],
  "loadStrategy": "lazy"
}
```

### Plugin Implementation

#### Terminal.Gui Renderer Plugin

```csharp
// WingedBean.Plugins.FigmaSharp.TerminalGui/TerminalGuiRenderer.cs
using WingedBean.Contracts.FigmaSharp;
using Terminal.Gui;

namespace WingedBean.Plugins.FigmaSharp.TerminalGui;

public class TerminalGuiRenderer : IUIRenderer
{
    public string FrameworkName => "Terminal.Gui v2";
    
    public object CreateContainer(UIElement element)
    {
        return new View { Id = element.Id };
    }
    
    public object CreateText(UIElement element)
    {
        return new Label { Text = element.Style?.Text?.Content ?? "" };
    }
    
    public object CreateButton(UIElement element)
    {
        return new Button { Text = element.Style?.Text?.Content ?? "Button" };
    }
    
    public void ApplyLayout(object target, LayoutData layout)
    {
        var view = (View)target;
        
        // Convert pixels to characters
        int charX = (int)(layout.AbsolutePosition.X / 8);
        int charY = (int)(layout.AbsolutePosition.Y / 16);
        
        // Position
        view.X = layout.PositionMode switch
        {
            PositionMode.Absolute => Pos.At(charX),
            PositionMode.Anchored when layout.Alignment?.Horizontal == HorizontalAlign.Center => Pos.Center(),
            _ => Pos.At(0)
        };
        
        view.Y = layout.PositionMode switch
        {
            PositionMode.Absolute => Pos.At(charY),
            PositionMode.Anchored when layout.Alignment?.Vertical == VerticalAlign.Center => Pos.Center(),
            _ => Pos.At(0)
        };
        
        // Size
        view.Width = layout.WidthMode switch
        {
            SizeMode.Fixed => Dim.Absolute((int)(layout.FixedWidth / 8)),
            SizeMode.Fill => Dim.Fill(),
            SizeMode.Auto => Dim.Auto(),
            _ => Dim.Absolute(10)
        };
        
        view.Height = layout.HeightMode switch
        {
            SizeMode.Fixed => Dim.Absolute((int)(layout.FixedHeight / 16)),
            SizeMode.Fill => Dim.Fill(),
            SizeMode.Auto => Dim.Auto(),
            _ => Dim.Absolute(1)
        };
    }
    
    public void ApplyStyle(object target, StyleData style)
    {
        var view = (View)target;
        
        if (style?.BackgroundColor != null)
        {
            // Map Figma color to Terminal.Gui color
            // (Terminal.Gui has limited color palette)
        }
    }
    
    public void AddChild(object parent, object child)
    {
        var parentView = (View)parent;
        var childView = (View)child;
        parentView.Add(childView);
    }
    
    public void RemoveChild(object parent, object child)
    {
        var parentView = (View)parent;
        var childView = (View)child;
        parentView.Remove(childView);
    }
}

// WingedBean.Plugins.FigmaSharp.TerminalGui/TerminalGuiRendererPlugin.cs
using Microsoft.Extensions.DependencyInjection;
using WingedBean.PluginSystem;
using WingedBean.Contracts.FigmaSharp;

namespace WingedBean.Plugins.FigmaSharp.TerminalGui;

public class TerminalGuiRendererPlugin : IPluginActivator
{
    public Task ActivateAsync(IServiceCollection services, IServiceProvider hostServices, CancellationToken ct = default)
    {
        // Register Terminal.Gui renderer
        services.AddSingleton<IUIRenderer, TerminalGuiRenderer>();
        
        return Task.CompletedTask;
    }
    
    public Task DeactivateAsync(CancellationToken ct = default)
    {
        // Cleanup if needed
        return Task.CompletedTask;
    }
}
```

#### Unity UGUI Renderer Plugin

```csharp
// WingedBean.Plugins.FigmaSharp.UnityUGUI/UguiRenderer.cs
using WingedBean.Contracts.FigmaSharp;
using UnityEngine;
using UnityEngine.UI;

namespace WingedBean.Plugins.FigmaSharp.UnityUGUI;

public class UguiRenderer : IUIRenderer
{
    public string FrameworkName => "Unity UGUI";
    
    public object CreateContainer(UIElement element)
    {
        var go = new GameObject(element.Name);
        go.AddComponent<RectTransform>();
        return go;
    }
    
    public object CreateButton(UIElement element)
    {
        var go = new GameObject(element.Name);
        var rt = go.AddComponent<RectTransform>();
        var button = go.AddComponent<Button>();
        
        // Add text child
        var textGo = new GameObject("Text");
        textGo.transform.SetParent(go.transform);
        var text = textGo.AddComponent<Text>();
        text.text = element.Style?.Text?.Content ?? "Button";
        
        return go;
    }
    
    public void ApplyLayout(object target, LayoutData layout)
    {
        var go = (GameObject)target;
        var rt = go.GetComponent<RectTransform>();
        
        // Set anchor and pivot
        rt.anchorMin = new Vector2(0, 1); // Top-left
        rt.anchorMax = new Vector2(0, 1);
        rt.pivot = new Vector2(0.5f, 0.5f);
        
        // Set size
        rt.sizeDelta = new Vector2(layout.FixedWidth, layout.FixedHeight);
        
        // Set position (flip Y for Unity's bottom-left origin)
        rt.anchoredPosition = new Vector2(layout.AbsolutePosition.X, -layout.AbsolutePosition.Y);
        
        // Apply auto layout
        if (layout.AutoLayout != null)
        {
            ApplyAutoLayout(go, layout.AutoLayout);
        }
    }
    
    private void ApplyAutoLayout(GameObject go, AutoLayoutData autoLayout)
    {
        if (autoLayout.Direction == LayoutDirection.Horizontal)
        {
            var layoutGroup = go.AddComponent<HorizontalLayoutGroup>();
            layoutGroup.spacing = autoLayout.Spacing;
            // ... configure other properties
        }
        else if (autoLayout.Direction == LayoutDirection.Vertical)
        {
            var layoutGroup = go.AddComponent<VerticalLayoutGroup>();
            layoutGroup.spacing = autoLayout.Spacing;
            // ... configure other properties
        }
    }
    
    public void ApplyStyle(object target, StyleData style) { /* ... */ }
    public void AddChild(object parent, object child) { /* ... */ }
    public void RemoveChild(object parent, object child) { /* ... */ }
}

// WingedBean.Plugins.FigmaSharp.UnityUGUI/UguiRendererPlugin.cs
public class UguiRendererPlugin : IPluginActivator
{
    public Task ActivateAsync(IServiceCollection services, IServiceProvider hostServices, CancellationToken ct = default)
    {
        services.AddSingleton<IUIRenderer, UguiRenderer>();
        return Task.CompletedTask;
    }
    
    public Task DeactivateAsync(CancellationToken ct = default) => Task.CompletedTask;
}
```

### Host Integration

#### Console Host Usage

```csharp
// ConsoleDungeon.Host/Program.cs
using WingedBean.PluginSystem;
using WingedBean.Contracts.FigmaSharp;
using WingedBean.FigmaSharp.Core;

var bootstrap = new HostBootstrap(
    new AlcPluginLoader(),
    logger,
    hostVersion: "1.0.0",
    pluginDirectories: ["./plugins"]
);

// Boot host and load plugins
var serviceProvider = await bootstrap.BootAsync();

// Get services (auto-registered by plugins)
var transformer = serviceProvider.GetRequiredService<IFigmaTransformer>();
var renderer = serviceProvider.GetRequiredService<IUIRenderer>(); // Terminal.Gui renderer

// Load Figma design
var figmaProject = await transformer.LoadFromApiAsync(fileKey, token);
var page = figmaProject.Document.Children[0];

// Convert to Terminal.Gui
var pipeline = new FigmaToUIPipeline(transformer, renderer);
var terminalView = (Terminal.Gui.View)pipeline.Convert(page);

// Run in Terminal.Gui
Application.Run(terminalView);
```

#### Unity Host Usage

```csharp
// Unity/FigmaImporter.cs
using WingedBean.PluginSystem;
using WingedBean.Contracts.FigmaSharp;
using WingedBean.FigmaSharp.Core;

public class FigmaImporter : MonoBehaviour
{
    async void Start()
    {
        var bootstrap = new HostBootstrap(
            new HybridClrPluginLoader(), // Unity-specific loader
            hostVersion: "1.0.0",
            Application.streamingAssetsPath + "/plugins"
        );
        
        var serviceProvider = await bootstrap.BootAsync();
        
        var transformer = serviceProvider.GetRequiredService<IFigmaTransformer>();
        var renderer = serviceProvider.GetRequiredService<IUIRenderer>(); // UGUI renderer
        
        var figmaProject = await transformer.LoadFromApiAsync(fileKey, token);
        var pipeline = new FigmaToUIPipeline(transformer, renderer);
        var unityGameObject = (GameObject)pipeline.Convert(figmaProject.Document.Children[0]);
        
        Instantiate(unityGameObject);
    }
}
```

### Hot-Reload Scenario

```csharp
// Switch from Terminal.Gui to Unity UGUI at runtime
var bootstrap = new HostBootstrap(/* ... */);
var sp = await bootstrap.BootAsync();

// Initially using Terminal.Gui
var terminalRenderer = sp.GetService<IUIRenderer>();
Console.WriteLine(terminalRenderer.FrameworkName); // "Terminal.Gui v2"

// Hot-reload to Unity UGUI
await bootstrap.UnloadPluginAsync("wingedbean.plugins.figmasharp.terminalgui");
await bootstrap.LoadPluginAsync("wingedbean.plugins.figmasharp.ugui");

// Now using Unity UGUI
var unityRenderer = sp.GetService<IUIRenderer>();
Console.WriteLine(unityRenderer.FrameworkName); // "Unity UGUI"
```

### Multi-Renderer Support

```csharp
// Load multiple renderers
var renderers = serviceProvider.GetServices<IUIRenderer>().ToList();

Console.WriteLine("Available renderers:");
foreach (var r in renderers)
{
    Console.WriteLine($"- {r.FrameworkName}");
}

// User selects renderer
var selectedRenderer = renderers.First(r => r.FrameworkName == userChoice);
var pipeline = new FigmaToUIPipeline(transformer, selectedRenderer);
```

## Project Structure

```
winged-bean/development/dotnet/
├── framework/src/
│   ├── WingedBean.Contracts.FigmaSharp/        # Tier 1
│   │   └── IUIRenderer.cs
│   │
│   └── WingedBean.FigmaSharp.Core/             # Tier 2
│       ├── FigmaTransformer.cs
│       └── FigmaToUIPipeline.cs
│
└── console/src/plugins/
    ├── WingedBean.Plugins.FigmaSharp.TerminalGui/
    │   ├── TerminalGuiRenderer.cs
    │   ├── TerminalGuiRendererPlugin.cs
    │   └── WingedBean.Plugins.FigmaSharp.TerminalGui.plugin.json
    │
    ├── WingedBean.Plugins.FigmaSharp.UnityUGUI/
    │   ├── UguiRenderer.cs
    │   ├── UguiRendererPlugin.cs
    │   └── WingedBean.Plugins.FigmaSharp.UnityUGUI.plugin.json
    │
    └── WingedBean.Plugins.FigmaSharp.UIToolkit/
        ├── UIToolkitRenderer.cs
        ├── UIToolkitRendererPlugin.cs
        └── WingedBean.Plugins.FigmaSharp.UIToolkit.plugin.json
```

## Implementation Plan

### Phase 1: Core Integration (Week 1)

1. **Create Framework Packages**
   - [ ] `WingedBean.Contracts.FigmaSharp` (Tier 1)
   - [ ] `WingedBean.FigmaSharp.Core` (Tier 2)
   - [ ] Reference from host

2. **Create Plugin Template**
   - [ ] Base renderer plugin structure
   - [ ] Manifest template
   - [ ] `IPluginActivator` implementation

### Phase 2: Renderer Plugins (Week 2)

1. **Terminal.Gui Renderer**
   - [ ] Implement `TerminalGuiRenderer`
   - [ ] Create plugin manifest
   - [ ] Test with WingedBean host

2. **Unity UGUI Renderer**
   - [ ] Implement `UguiRenderer`
   - [ ] Create plugin manifest
   - [ ] Test with Unity HybridCLR loader

3. **UI Toolkit Renderer**
   - [ ] Implement `UIToolkitRenderer`
   - [ ] Create plugin manifest
   - [ ] Test with Unity

### Phase 3: Testing & Documentation (Week 3)

1. **Integration Tests**
   - [ ] Test plugin discovery
   - [ ] Test dependency resolution
   - [ ] Test hot-reload
   - [ ] Test multi-renderer support

2. **Documentation**
   - [ ] Plugin development guide
   - [ ] Renderer implementation guide
   - [ ] Hot-reload usage examples

## Success Criteria

1. ✅ FigmaSharp core integrates with WingedBean plugin system
2. ✅ Renderer plugins load via `PluginDiscovery`
3. ✅ Dependency resolution ensures core loads before renderers
4. ✅ Profile-specific loading works (console → Terminal.Gui, Unity → UGUI)
5. ✅ Hot-reload allows switching renderers at runtime
6. ✅ Multiple renderers can coexist
7. ✅ Service registration via `IPluginActivator` works correctly

## Dependencies

- **RFC-0003**: Plugin Architecture Foundation (plugin system)
- **RFC-0022**: FigmaSharp Core Architecture (core packages)
- **RFC-0023**: FigmaSharp Layout Transformation (transformation logic)

## Future Work

- **RFC-0025**: FigmaSharp Renderer Implementations (detailed renderer specs)
- Godot renderer plugin
- Web renderer plugin (Blazor/MAUI)
- Plugin marketplace for community renderers

## References

- RFC-0003: Plugin Architecture Foundation
- RFC-0022: FigmaSharp Core Architecture
- WingedBean Plugin System Documentation

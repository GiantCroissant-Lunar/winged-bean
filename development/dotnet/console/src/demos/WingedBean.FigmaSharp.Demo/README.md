# FigmaSharp Demo

This demo shows the complete FigmaSharp pipeline transforming a stub Figma design into Terminal.Gui v2 UI.

## What it demonstrates

- Creating a stub Figma design in code (simulating Figma API data)
- Transforming Figma objects to framework-agnostic UI elements
- Rendering the abstract UI to Terminal.Gui v2
- Complete end-to-end pipeline: Figma → Abstract UI → Terminal.Gui

## Running the demo

```bash
# Set terminal type (required for Terminal.Gui)
export TERM=xterm-256color

# Run the demo
dotnet run --project console/src/demos/WingedBean.FigmaSharp.Demo/WingedBean.FigmaSharp.Demo.csproj
```

## What you'll see

The demo creates a simple UI with:
- A title: "Welcome to FigmaSharp!"
- Description text explaining the demo
- A green button labeled "Click Me!"
- Info text: "Press ESC to exit"

All of this is defined as Figma objects and transformed to Terminal.Gui!

## Architecture flow

```
Stub Figma Design (FObject tree)
    ↓
FigmaTransformer.Transform()
    ↓
Abstract UI (UIElement tree)
    ↓
FigmaToUIPipeline.Convert()
    ↓
TerminalGuiRenderer
    ↓
Terminal.Gui Views
```

## Key features demonstrated

1. **Figma Model** - Complete FObject hierarchy with:
   - Frames (containers)
   - Text nodes with styling
   - Auto-layout (vertical with spacing)
   - Padding and alignment
   - Colors and fills

2. **Transformation** - FigmaTransformer converts:
   - Figma types to UI element types
   - Figma layout to abstract layout data
   - Figma styles to abstract style data

3. **Rendering** - TerminalGuiRenderer creates:
   - Terminal.Gui Views from containers
   - Labels from text nodes
   - Buttons from button patterns
   - Pixel-to-character conversion (8x16 ratio)

## Next steps

- Try modifying the stub design (colors, text, layout)
- Add more UI elements (inputs, toggles, etc.)
- Load real Figma data from the API
- Create additional renderers (Unity UGUI, UI Toolkit, Godot)

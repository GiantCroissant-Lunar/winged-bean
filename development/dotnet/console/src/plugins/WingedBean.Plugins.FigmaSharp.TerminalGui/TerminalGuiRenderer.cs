using Terminal.Gui;
using Plate.CrossMilo.Contracts.FigmaSharp;
using Plate.CrossMilo.Contracts.FigmaSharp.Renderer;

namespace WingedBean.Plugins.FigmaSharp.TerminalGui;

/// <summary>
/// Renders framework-agnostic UI elements to Terminal.Gui v2
/// </summary>
public class TerminalGuiRenderer : IService
{
    private const int CharWidth = 8;   // Pixels per character width
    private const int CharHeight = 16; // Pixels per character height
    
    public string FrameworkName => "Terminal.Gui v2";
    
    // Element creation
    public object CreateContainer(UIElement element)
    {
        return new View 
        { 
            Id = element.Id,
            Title = element.Name
        };
    }
    
    public object CreateText(UIElement element)
    {
        return new Label 
        { 
            Text = element.Style?.Text?.Content ?? "",
            Id = element.Id
        };
    }
    
    public object CreateButton(UIElement element)
    {
        return new Button 
        { 
            Text = element.Style?.Text?.Content ?? element.Name,
            Id = element.Id
        };
    }
    
    public object CreateImage(UIElement element)
    {
        // Terminal.Gui doesn't support images, use a placeholder view
        return new View 
        { 
            Id = element.Id,
            Title = $"[Image: {element.Name}]"
        };
    }
    
    public object CreateInput(UIElement element)
    {
        return new TextField 
        { 
            Text = element.Style?.Text?.Content ?? "",
            Id = element.Id
        };
    }
    
    public object CreateToggle(UIElement element)
    {
        return new CheckBox 
        { 
            Text = element.Name,
            Id = element.Id
        };
    }
    
    public object CreateScrollView(UIElement element)
    {
        // ScrollView in Terminal.Gui v2 - use FrameView as alternative
        return new FrameView 
        { 
            Id = element.Id,
            Title = element.Name
        };
    }
    
    // Layout application
    public void ApplyLayout(object target, LayoutData layout)
    {
        var view = (View)target;
        
        // Convert pixels to characters
        int charX = (int)(layout.AbsolutePosition.X / CharWidth);
        int charY = (int)(layout.AbsolutePosition.Y / CharHeight);
        
        // Position
        view.X = CalculatePos(layout, isX: true, charX);
        view.Y = CalculatePos(layout, isX: false, charY);
        
        // Size
        view.Width = CalculateDim(layout, isWidth: true);
        view.Height = CalculateDim(layout, isWidth: false);
        
        // Apply auto-layout if present
        if (layout.AutoLayout != null)
        {
            ApplyAutoLayout(view, layout.AutoLayout);
        }
    }
    
    private Pos CalculatePos(LayoutData layout, bool isX, int charPos)
    {
        if (layout.PositionMode == PositionMode.Absolute)
        {
            return Pos.Absolute(charPos);
        }
        
        if (layout.PositionMode == PositionMode.Anchored && layout.Alignment != null)
        {
            if (isX && layout.Alignment.Horizontal == HorizontalAlign.Center)
                return Pos.Center();
            if (isX && layout.Alignment.Horizontal == HorizontalAlign.Right)
                return Pos.AnchorEnd(0);
            if (!isX && layout.Alignment.Vertical == VerticalAlign.Center)
                return Pos.Center();
            if (!isX && layout.Alignment.Vertical == VerticalAlign.Bottom)
                return Pos.AnchorEnd(0);
        }
        
        return Pos.Absolute(0);
    }
    
    private Dim CalculateDim(LayoutData layout, bool isWidth)
    {
        var mode = isWidth ? layout.WidthMode : layout.HeightMode;
        var fixedSize = isWidth ? layout.FixedWidth : layout.FixedHeight;
        var percentSize = isWidth ? layout.PercentWidth : layout.PercentHeight;
        
        // Convert pixels to characters for fixed size
        int charSize = (int)(fixedSize / (isWidth ? CharWidth : CharHeight));
        
        return mode switch
        {
            SizeMode.Fixed => Dim.Absolute(charSize),
            SizeMode.Fill => Dim.Fill(),
            SizeMode.Auto => Dim.Auto(),
            SizeMode.Percent => Dim.Percent((int)percentSize),
            _ => Dim.Absolute(charSize)
        };
    }
    
    // Style application
    public void ApplyStyle(object target, StyleData style)
    {
        var view = (View)target;
        
        // Terminal.Gui has limited styling
        // We can set colors if available
        if (style?.BackgroundColor != null)
        {
            var terminalColor = MapToTerminalColor(style.BackgroundColor);
            // Note: Terminal.Gui v2 color scheme would be set here
            // For now, we'll skip as it requires more complex setup
        }
        
        // For text elements, set the text content
        if (target is Label label && style?.Text != null)
        {
            label.Text = style.Text.Content;
        }
        else if (target is Button button && style?.Text != null)
        {
            button.Text = style.Text.Content;
        }
    }
    
    private Terminal.Gui.Color MapToTerminalColor(Plate.CrossMilo.Contracts.FigmaSharp.Color figmaColor)
    {
        // Map RGB to nearest Terminal.Gui color
        // Terminal.Gui supports: Black, Blue, Green, Cyan, Red, Magenta, Brown, Gray, etc.
        
        if (figmaColor.R > 0.8f && figmaColor.G < 0.2f && figmaColor.B < 0.2f)
            return Terminal.Gui.Color.Red;
        if (figmaColor.R < 0.2f && figmaColor.G > 0.8f && figmaColor.B < 0.2f)
            return Terminal.Gui.Color.Green;
        if (figmaColor.R < 0.2f && figmaColor.G < 0.2f && figmaColor.B > 0.8f)
            return Terminal.Gui.Color.Blue;
        if (figmaColor.R > 0.8f && figmaColor.G > 0.8f && figmaColor.B < 0.2f)
            return Terminal.Gui.Color.BrightYellow;
        if (figmaColor.R < 0.2f && figmaColor.G > 0.8f && figmaColor.B > 0.8f)
            return Terminal.Gui.Color.Cyan;
        if (figmaColor.R > 0.8f && figmaColor.G < 0.2f && figmaColor.B > 0.8f)
            return Terminal.Gui.Color.Magenta;
        if (figmaColor.R > 0.8f && figmaColor.G > 0.8f && figmaColor.B > 0.8f)
            return Terminal.Gui.Color.White;
        if (figmaColor.R < 0.2f && figmaColor.G < 0.2f && figmaColor.B < 0.2f)
            return Terminal.Gui.Color.Black;
        
        return Terminal.Gui.Color.Gray;
    }
    
    /// <summary>
    /// Apply auto-layout to a container view
    /// Terminal.Gui doesn't have built-in auto-layout, so we manually position children
    /// </summary>
    private void ApplyAutoLayout(View container, AutoLayoutData autoLayout)
    {
        // Terminal.Gui doesn't have auto-layout, but we can manually position children
        // This would be applied after all children are added
        // For now, we store the auto-layout data as metadata for manual positioning
        // In a full implementation, we would position children based on direction and spacing
        
        // Note: Actual child positioning happens in the pipeline after all children are added
        // This is a placeholder for future enhancement
    }
    
    // Hierarchy management
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

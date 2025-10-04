using WingedBean.Contracts.FigmaSharp;

namespace WingedBean.FigmaSharp.Core;

/// <summary>
/// Maps Figma node types to UI element types
/// </summary>
internal class ElementTypeMapper
{
    public UIElementType MapElementType(FObject figma)
    {
        // Check for text nodes
        if (figma.Type == NodeType.TEXT)
        {
            return UIElementType.Text;
        }
        
        // Check for button-like patterns (name-based heuristic)
        if (IsButton(figma))
        {
            return UIElementType.Button;
        }
        
        // Check for input-like patterns
        if (IsInput(figma))
        {
            return UIElementType.Input;
        }
        
        // Check for toggle/checkbox patterns
        if (IsToggle(figma))
        {
            return UIElementType.Toggle;
        }
        
        // Check for scroll view patterns
        if (IsScrollView(figma))
        {
            return UIElementType.ScrollView;
        }
        
        // Check for image nodes
        if (IsImage(figma))
        {
            return UIElementType.Image;
        }
        
        // Default to container
        return UIElementType.Container;
    }
    
    private bool IsButton(FObject figma)
    {
        var nameLower = figma.Name.ToLowerInvariant();
        return nameLower.Contains("button") || 
               nameLower.Contains("btn") ||
               nameLower.StartsWith("cta");
    }
    
    private bool IsInput(FObject figma)
    {
        var nameLower = figma.Name.ToLowerInvariant();
        return nameLower.Contains("input") || 
               nameLower.Contains("textfield") ||
               nameLower.Contains("text-field");
    }
    
    private bool IsToggle(FObject figma)
    {
        var nameLower = figma.Name.ToLowerInvariant();
        return nameLower.Contains("toggle") || 
               nameLower.Contains("checkbox") ||
               nameLower.Contains("switch");
    }
    
    private bool IsScrollView(FObject figma)
    {
        var nameLower = figma.Name.ToLowerInvariant();
        return nameLower.Contains("scroll") || 
               nameLower.Contains("list") ||
               nameLower.Contains("scrollview");
    }
    
    private bool IsImage(FObject figma)
    {
        // Images are typically RECTANGLE or VECTOR nodes with fills
        return (figma.Type == NodeType.RECTANGLE || 
                figma.Type == NodeType.VECTOR ||
                figma.Type == NodeType.ELLIPSE) &&
               figma.Fills != null && 
               figma.Fills.Count > 0 &&
               figma.Children == null;
    }
}

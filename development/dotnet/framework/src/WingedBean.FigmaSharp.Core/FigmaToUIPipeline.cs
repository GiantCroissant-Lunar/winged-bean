using WingedBean.Contracts.FigmaSharp;

namespace WingedBean.FigmaSharp.Core;

/// <summary>
/// Orchestrates Figma-to-UI transformation pipeline
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
    
    /// <summary>
    /// Convert Figma object to framework-specific UI
    /// </summary>
    public object Convert(FObject figmaRoot)
    {
        // Step 1: Transform Figma to abstract model
        var abstractUI = _transformer.Transform(figmaRoot);
        
        // Step 2: Render to specific framework
        return RenderElement(abstractUI);
    }
    
    /// <summary>
    /// Recursively render UI element tree
    /// </summary>
    private object RenderElement(UIElement element)
    {
        // Create framework-specific object based on type
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

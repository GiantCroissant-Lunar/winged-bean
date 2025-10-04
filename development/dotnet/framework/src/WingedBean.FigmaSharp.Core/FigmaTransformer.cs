using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using WingedBean.Contracts.FigmaSharp;

namespace WingedBean.FigmaSharp.Core;

/// <summary>
/// Transforms Figma objects to framework-agnostic UI elements
/// </summary>
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
    
    /// <summary>
    /// Transform a Figma object to abstract UI element
    /// </summary>
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
        
        // Store original Figma data in metadata for debugging
        element.Metadata["figmaType"] = figmaObject.Type.ToString();
        element.Metadata["figmaVisible"] = figmaObject.Visible;
        
        return element;
    }
    
    /// <summary>
    /// Load Figma project from API (placeholder for now)
    /// </summary>
    public Task<FigmaProject> LoadFromApiAsync(string fileKey, string token, CancellationToken ct = default)
    {
        // TODO: Implement Figma API client in future phase
        throw new System.NotImplementedException(
            "Figma API client not yet implemented. " +
            "This will be added in a future phase. " +
            "For now, use Transform() with manually loaded FObject data."
        );
    }
}

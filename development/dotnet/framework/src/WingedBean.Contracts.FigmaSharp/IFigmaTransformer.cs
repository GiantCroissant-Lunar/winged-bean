using System.Threading;
using System.Threading.Tasks;

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

/// <summary>
/// Figma project container
/// </summary>
public class FigmaProject
{
    public string Name { get; set; } = string.Empty;
    public FObject Document { get; set; } = new();
}

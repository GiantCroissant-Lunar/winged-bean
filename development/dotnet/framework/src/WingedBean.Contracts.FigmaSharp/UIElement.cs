using System.Collections.Generic;

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

/// <summary>
/// UI element types
/// </summary>
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

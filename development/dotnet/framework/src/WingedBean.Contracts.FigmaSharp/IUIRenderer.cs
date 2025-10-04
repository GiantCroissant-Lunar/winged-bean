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

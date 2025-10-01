using System;

namespace WingedBean.Contracts.Core;

/// <summary>
/// Marks a partial class as a proxy service that realizes a specific interface.
/// Source generator will implement all interface methods by delegating to the registry.
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public class RealizeServiceAttribute : Attribute
{
    public Type ServiceType { get; }

    public RealizeServiceAttribute(Type serviceType)
    {
        ServiceType = serviceType;
    }
}

/// <summary>
/// Specifies the selection strategy for retrieving service implementations from the registry.
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
public class SelectionStrategyAttribute : Attribute
{
    public SelectionMode Mode { get; }

    public SelectionStrategyAttribute(SelectionMode mode)
    {
        Mode = mode;
    }
}

/// <summary>
/// Marks a class as a plugin with metadata.
/// Source generator can read this to generate registration code.
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
public class PluginAttribute : Attribute
{
    public string? Name { get; set; }
    public Type[]? Provides { get; set; }
    public Type[]? Dependencies { get; set; }
    public int Priority { get; set; }
}

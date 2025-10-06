namespace WingedBean.Contracts.Core;

/// <summary>
/// Marker interface for objects that require registry injection.
/// Replaces reflection-based SetRegistry discovery (RFC-0038).
/// </summary>
/// <remarks>
/// This interface provides a compile-time contract for registry injection,
/// eliminating the need for runtime reflection to find the SetRegistry method.
/// Plugins and services implementing this interface will have their registry
/// injected automatically during plugin activation.
/// </remarks>
public interface IRegistryAware
{
    /// <summary>
    /// Called by the plugin loader to inject the registry.
    /// </summary>
    /// <param name="registry">The registry instance containing all registered services.</param>
    /// <remarks>
    /// This method is typically called during plugin activation, before any
    /// other initialization occurs. Implementations should store the registry
    /// for later use in resolving dependencies.
    /// </remarks>
    void SetRegistry(IRegistry registry);
}

using WingedBean.Contracts.Core;

namespace WingedBean.Contracts.Resource;

/// <summary>
/// Proxy service for IResourceService.
/// Source generator will implement all interface methods by delegating to the registry.
/// </summary>
[RealizeService(typeof(IResourceService))]
[SelectionStrategy(SelectionMode.HighestPriority)]
public partial class ProxyService : IResourceService
{
    private readonly IRegistry _registry;

    /// <summary>
    /// Initializes a new instance of the ProxyService class.
    /// </summary>
    /// <param name="registry">The service registry for resolving implementations.</param>
    public ProxyService(IRegistry registry)
    {
        _registry = registry;
    }

    // Source generator will implement all interface methods below
}

using WingedBean.Contracts.Core;

namespace WingedBean.Contracts.Resilience;

/// <summary>
/// Proxy service for IResilienceService (partial class, source gen fills in methods).
/// </summary>
[RealizeService(typeof(IResilienceService))]
[SelectionStrategy(SelectionMode.HighestPriority)]
public partial class ProxyService : IResilienceService
{
    private readonly IRegistry _registry;

    /// <summary>
    /// Initializes a new instance of the ProxyService class.
    /// </summary>
    /// <param name="registry">The registry to use for service resolution</param>
    public ProxyService(IRegistry registry)
    {
        _registry = registry;
    }

    // Source generator will implement all interface methods below
}

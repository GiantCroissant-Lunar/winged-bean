using WingedBean.Contracts.Core;

namespace WingedBean.Contracts.TerminalUI;

/// <summary>
/// Proxy service for ITerminalUIService.
/// Source generator will implement all interface methods by delegating to the registry.
/// </summary>
[RealizeService(typeof(ITerminalUIService))]
[SelectionStrategy(SelectionMode.HighestPriority)]
public partial class ProxyService : ITerminalUIService
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


using WingedBean.Contracts.Core;

namespace WingedBean.Contracts.Game;

/// <summary>
/// Proxy service for IRenderService.
/// Source generator will implement all interface methods by delegating to the registry.
/// Part of RFC-0018: Render and UI Services for Console Profile.
/// </summary>
[RealizeService(typeof(IRenderService))]
[SelectionStrategy(SelectionMode.HighestPriority)]
public partial class RenderServiceProxy : IRenderService
{
    private readonly IRegistry _registry;

    /// <summary>
    /// Initializes a new instance of the RenderServiceProxy class.
    /// </summary>
    /// <param name="registry">The registry to use for service resolution</param>
    public RenderServiceProxy(IRegistry registry)
    {
        _registry = registry;
    }

    // Source generator will implement all interface methods below
}

using WingedBean.Contracts.Core;

namespace WingedBean.Contracts.Audio;

/// <summary>
/// Proxy service for IAudioService.
/// </summary>
[RealizeService(typeof(IAudioService))]
[SelectionStrategy(SelectionMode.HighestPriority)]
public partial class ProxyService : IAudioService
{
    private readonly IRegistry _registry;

    public ProxyService(IRegistry registry)
    {
        _registry = registry;
    }

    // Source generator will implement all interface methods below
}

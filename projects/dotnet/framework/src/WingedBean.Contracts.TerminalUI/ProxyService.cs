using System;
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

    // TODO: Source gen will fill in these methods with proper delegation to registry
    // For now, providing stub implementations to allow compilation

    /// <inheritdoc />
    public void Initialize()
    {
        throw new NotImplementedException("Source generation pending");
    }

    /// <inheritdoc />
    public void Run()
    {
        throw new NotImplementedException("Source generation pending");
    }

    /// <inheritdoc />
    public string GetScreenContent()
    {
        throw new NotImplementedException("Source generation pending");
    }
}

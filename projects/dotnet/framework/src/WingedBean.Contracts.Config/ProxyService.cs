using System;
using System.Threading;
using System.Threading.Tasks;
using WingedBean.Contracts.Core;

namespace WingedBean.Contracts.Config;

/// <summary>
/// Proxy service for IConfigService (partial class, source gen fills in methods).
/// </summary>
[RealizeService(typeof(IConfigService))]
[SelectionStrategy(SelectionMode.HighestPriority)]
public partial class ProxyService : IConfigService
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
    public string? Get(string key) => throw new NotImplementedException("Source generation pending");

    /// <inheritdoc />
    public T? Get<T>(string key) => throw new NotImplementedException("Source generation pending");

    /// <inheritdoc />
    public IConfigSection GetSection(string key) => throw new NotImplementedException("Source generation pending");

    /// <inheritdoc />
    public void Set(string key, string value) => throw new NotImplementedException("Source generation pending");

    /// <inheritdoc />
    public bool Exists(string key) => throw new NotImplementedException("Source generation pending");

    /// <inheritdoc />
    public Task ReloadAsync(CancellationToken cancellationToken = default) => throw new NotImplementedException("Source generation pending");

    /// <inheritdoc />
    public event EventHandler<ConfigChangedEventArgs>? ConfigChanged
    {
        add => throw new NotImplementedException("Source generation pending");
        remove => throw new NotImplementedException("Source generation pending");
    }
}

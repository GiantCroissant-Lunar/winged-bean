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

    // Source gen fills in all methods below
    // Temporary stub implementations until source generator is available
    
    /// <inheritdoc />
    public Task<TResource?> LoadAsync<TResource>(string resourceId, CancellationToken cancellationToken = default)
        where TResource : class
    {
        var service = _registry.Get<IResourceService>(SelectionMode.HighestPriority);
        return service.LoadAsync<TResource>(resourceId, cancellationToken);
    }

    /// <inheritdoc />
    public Task<IEnumerable<TResource>> LoadAllAsync<TResource>(string pattern, CancellationToken cancellationToken = default)
        where TResource : class
    {
        var service = _registry.Get<IResourceService>(SelectionMode.HighestPriority);
        return service.LoadAllAsync<TResource>(pattern, cancellationToken);
    }

    /// <inheritdoc />
    public void Unload(string resourceId)
    {
        var service = _registry.Get<IResourceService>(SelectionMode.HighestPriority);
        service.Unload(resourceId);
    }

    /// <inheritdoc />
    public void UnloadAll<TResource>() where TResource : class
    {
        var service = _registry.Get<IResourceService>(SelectionMode.HighestPriority);
        service.UnloadAll<TResource>();
    }

    /// <inheritdoc />
    public bool IsLoaded(string resourceId)
    {
        var service = _registry.Get<IResourceService>(SelectionMode.HighestPriority);
        return service.IsLoaded(resourceId);
    }

    /// <inheritdoc />
    public Task<ResourceMetadata?> GetMetadataAsync(string resourceId, CancellationToken cancellationToken = default)
    {
        var service = _registry.Get<IResourceService>(SelectionMode.HighestPriority);
        return service.GetMetadataAsync(resourceId, cancellationToken);
    }

    /// <inheritdoc />
    public Task PreloadAsync(IEnumerable<string> resourceIds, CancellationToken cancellationToken = default)
    {
        var service = _registry.Get<IResourceService>(SelectionMode.HighestPriority);
        return service.PreloadAsync(resourceIds, cancellationToken);
    }
}

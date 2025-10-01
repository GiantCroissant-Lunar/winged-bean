namespace WingedBean.Contracts.Resource;

/// <summary>
/// Resource loading service for assets, data files, etc.
/// Platform implementations: File system (Console), Addressables (Unity), YooAsset (Unity alt), etc.
/// </summary>
public interface IResourceService
{
    /// <summary>
    /// Load a resource asynchronously.
    /// </summary>
    Task<TResource?> LoadAsync<TResource>(string resourceId, CancellationToken cancellationToken = default)
        where TResource : class;

    /// <summary>
    /// Load multiple resources asynchronously.
    /// </summary>
    Task<IEnumerable<TResource>> LoadAllAsync<TResource>(string pattern, CancellationToken cancellationToken = default)
        where TResource : class;

    /// <summary>
    /// Unload a resource (free memory).
    /// </summary>
    void Unload(string resourceId);

    /// <summary>
    /// Unload all resources of a specific type.
    /// </summary>
    void UnloadAll<TResource>() where TResource : class;

    /// <summary>
    /// Check if a resource is loaded.
    /// </summary>
    bool IsLoaded(string resourceId);

    /// <summary>
    /// Get resource metadata without loading the full resource.
    /// </summary>
    Task<ResourceMetadata?> GetMetadataAsync(string resourceId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Preload resources (load into memory without instantiating).
    /// </summary>
    Task PreloadAsync(IEnumerable<string> resourceIds, CancellationToken cancellationToken = default);
}

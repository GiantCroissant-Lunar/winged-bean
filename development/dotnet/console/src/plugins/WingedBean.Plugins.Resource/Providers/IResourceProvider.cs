using Plate.CrossMilo.Contracts.Resource;

namespace WingedBean.Plugins.Resource.Providers;

/// <summary>
/// Provider abstraction for resource loading strategies.
/// Enables pluggable support for different resource types (files, bundles, NuGet packages, etc.).
/// </summary>
public interface IResourceProvider
{
    /// <summary>
    /// Check if this provider can handle the given resource URI.
    /// </summary>
    /// <param name="resourceId">Resource identifier (e.g., "data/level.json", "nuget:PackageId")</param>
    bool CanHandle(string resourceId);
    
    /// <summary>
    /// Load a resource asynchronously.
    /// </summary>
    /// <typeparam name="TResource">Resource type</typeparam>
    /// <param name="resourceId">Resource identifier</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task<TResource?> LoadAsync<TResource>(
        string resourceId,
        CancellationToken cancellationToken = default)
        where TResource : class;
    
    /// <summary>
    /// Get resource metadata without loading full resource.
    /// </summary>
    /// <param name="resourceId">Resource identifier</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task<ResourceMetadata?> GetMetadataAsync(
        string resourceId,
        CancellationToken cancellationToken = default);
}

using Microsoft.Extensions.Logging;
using Plate.CrossMilo.Contracts.Resource.Services;
using Plate.CrossMilo.Contracts.Resource;
using WingedBean.Plugins.Resource.Providers;

namespace WingedBean.Plugins.Resource;

/// <summary>
/// Resource service for Console profile with pluggable provider support.
/// Supports file system resources, bundles, and NuGet packages through provider pattern.
/// </summary>
public class FileSystemResourceService : IService
{
    private readonly ILogger<FileSystemResourceService> _logger;
    private readonly ResourceCache _cache;
    private readonly List<IResourceProvider> _providers;

    public FileSystemResourceService(
        ILogger<FileSystemResourceService> logger,
        string? basePath = null,
        IEnumerable<IResourceProvider>? customProviders = null)
    {
        _logger = logger;
        _cache = new ResourceCache();
        _providers = new List<IResourceProvider>();
        
        // Add custom providers first (higher priority)
        if (customProviders != null)
        {
            _providers.AddRange(customProviders);
        }
        
        // Add NuGet provider for Console profile (compile-time check)
        #if !UNITY_2021_1_OR_NEWER && !GODOT
        try
        {
            // Dynamically load NuGet provider if available
            var nugetProviderType = Type.GetType(
                "WingedBean.Plugins.Resource.NuGet.NuGetResourceProvider, WingedBean.Plugins.Resource.NuGet"
            );
            
            if (nugetProviderType != null)
            {
                var nugetProvider = Activator.CreateInstance(nugetProviderType, logger, null);
                if (nugetProvider is IResourceProvider provider)
                {
                    _providers.Add(provider);
                    logger.LogInformation("NuGet resource provider loaded successfully");
                }
            }
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "NuGet resource provider not available (optional)");
        }
        #endif
        
        // Add default file system provider (lowest priority)
        _providers.Add(new FileSystemProvider(logger, basePath));
        
        logger.LogInformation(
            "Resource service initialized with {ProviderCount} provider(s)",
            _providers.Count
        );
    }

    /// <inheritdoc/>
    public async Task<TResource?> LoadAsync<TResource>(
        string resourceId, 
        CancellationToken cancellationToken = default)
        where TResource : class
    {
        // Check cache first
        if (_cache.TryGet<TResource>(resourceId, out var cached))
        {
            _logger.LogDebug("Resource '{ResourceId}' loaded from cache", resourceId);
            return cached;
        }

        // Try each provider in order
        foreach (var provider in _providers)
        {
            if (provider.CanHandle(resourceId))
            {
                _logger.LogDebug(
                    "Attempting to load '{ResourceId}' with provider {ProviderType}",
                    resourceId,
                    provider.GetType().Name
                );
                
                var resource = await provider.LoadAsync<TResource>(resourceId, cancellationToken);
                
                if (resource != null)
                {
                    _cache.Set(resourceId, resource);
                    _logger.LogInformation(
                        "Resource '{ResourceId}' loaded successfully by {ProviderType}",
                        resourceId,
                        provider.GetType().Name
                    );
                    return resource;
                }
            }
        }

        _logger.LogWarning("Resource '{ResourceId}' not found by any provider", resourceId);
        return null;
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<TResource>> LoadAllAsync<TResource>(
        string pattern, 
        CancellationToken cancellationToken = default)
        where TResource : class
    {
        // LoadAll is pattern-based, primarily for file system
        // Delegate to first provider that can handle it
        foreach (var provider in _providers)
        {
            if (provider.CanHandle(pattern))
            {
                // For now, only file system provider supports patterns
                // Load matching resources individually
                var resources = new List<TResource>();
                
                // This is a simplified implementation
                // Real implementation would need provider-specific pattern matching
                var resource = await provider.LoadAsync<TResource>(pattern, cancellationToken);
                if (resource != null)
                {
                    resources.Add(resource);
                }
                
                return resources;
            }
        }
        
        return Enumerable.Empty<TResource>();
    }

    /// <inheritdoc/>
    public void Unload(string resourceId)
    {
        if (_cache.Remove(resourceId))
        {
            _logger.LogDebug("Resource '{ResourceId}' unloaded from cache", resourceId);
        }
    }

    /// <inheritdoc/>
    public void UnloadAll<TResource>() where TResource : class
    {
        var count = _cache.RemoveAll<TResource>();
        _logger.LogInformation(
            "Unloaded {Count} resources of type {Type}", 
            count, 
            typeof(TResource).Name
        );
    }

    /// <inheritdoc/>
    public bool IsLoaded(string resourceId)
    {
        return _cache.Contains(resourceId);
    }

    /// <inheritdoc/>
    public async Task<ResourceMetadata?> GetMetadataAsync(
        string resourceId, 
        CancellationToken cancellationToken = default)
    {
        // Try each provider
        foreach (var provider in _providers)
        {
            if (provider.CanHandle(resourceId))
            {
                var metadata = await provider.GetMetadataAsync(resourceId, cancellationToken);
                if (metadata != null)
                {
                    return metadata;
                }
            }
        }
        
        return null;
    }

    /// <inheritdoc/>
    public async Task PreloadAsync(
        IEnumerable<string> resourceIds, 
        CancellationToken cancellationToken = default)
    {
        var tasks = resourceIds.Select(id => 
            LoadAsync<object>(id, cancellationToken)
        );

        await Task.WhenAll(tasks);

        _logger.LogInformation(
            "Preloaded {Count} resources", 
            resourceIds.Count()
        );
    }
}

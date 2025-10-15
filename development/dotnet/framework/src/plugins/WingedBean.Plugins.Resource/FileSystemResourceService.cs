using Microsoft.Extensions.Logging;
using Plate.CrossMilo.Contracts.Resource;
using Plate.CrossMilo.Contracts.Resource.Services;
using WingedBean.Plugins.Resource.Providers;

namespace WingedBean.Plugins.Resource;

/// <summary>
/// Resource service for Console profile with pluggable provider support.
/// Now backed by ResourceManager (RFC-0054) for reference counting and lifecycle management.
/// Supports file system resources, bundles, and NuGet packages through provider pattern.
/// </summary>
public class FileSystemResourceService : IService
{
    private readonly ILogger<FileSystemResourceService> _logger;
    private readonly ResourceManager _resourceManager;
    private readonly List<IResourceProvider> _providers;

    /// <summary>
    /// Initializes a new instance of FileSystemResourceService.
    /// </summary>
    /// <param name="logger">Logger instance</param>
    /// <param name="basePath">Base path for file system resources (optional)</param>
    /// <param name="customProviders">Custom providers to add (higher priority, optional)</param>
    /// <param name="options">ResourceManager options (optional)</param>
    public FileSystemResourceService(
        ILogger<FileSystemResourceService> logger,
        string? basePath = null,
        IEnumerable<IResourceProvider>? customProviders = null,
        ResourceManagerOptions? options = null)
    {
        _logger = logger;
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

        // Create ResourceManager with providers (RFC-0054)
        var managerLogger = new LoggerWrapper<ResourceManager>(logger);
        _resourceManager = new ResourceManager(managerLogger, _providers, options);

        logger.LogInformation(
            "Resource service initialized with {ProviderCount} provider(s) (backed by ResourceManager with ref counting)",
            _providers.Count
        );
    }

    /// <summary>
    /// Gets the underlying ResourceManager for advanced scenarios.
    /// Allows direct access to handle-based API.
    /// </summary>
    public ResourceManager Manager => _resourceManager;

    /// <inheritdoc/>
    public async Task<TResource?> LoadAsync<TResource>(
        string resourceId,
        CancellationToken cancellationToken = default)
        where TResource : class
    {
        // Delegate to ResourceManager (RFC-0054)
        // This provides automatic deduplication and caching
        return await _resourceManager.LoadAsync<TResource>(resourceId, cancellationToken);
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
        // Note: With ResourceManager, resources are only unloaded when refcount reaches zero
        // This method is kept for backward compatibility but doesn't force immediate unload
        _logger.LogDebug(
            "Unload called for '{ResourceId}' (note: resource will only be unloaded when all references are released)",
            resourceId
        );
        
        // We don't force unload anymore - let ResourceManager handle lifecycle
        // Resources will be automatically unloaded after idle timeout when refcount is zero
    }

    /// <inheritdoc/>
    public void UnloadAll<TResource>() where TResource : class
    {
        // Similar to Unload() - this is now a no-op
        // ResourceManager handles cleanup automatically
        _logger.LogInformation(
            "UnloadAll<{Type}> called (resource cleanup is now automatic via ResourceManager)",
            typeof(TResource).Name
        );
    }

    /// <inheritdoc/>
    public bool IsLoaded(string resourceId)
    {
        return _resourceManager.IsLoaded(resourceId);
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
            _resourceManager.LoadAsync<object>(id, cancellationToken)
        );

        await Task.WhenAll(tasks);

        _logger.LogInformation(
            "Preloaded {Count} resources",
            resourceIds.Count()
        );
    }

    /// <summary>
    /// Logger wrapper to adapt ILogger&lt;FileSystemResourceService&gt; to ILogger&lt;ResourceManager&gt;.
    /// </summary>
    private class LoggerWrapper<T> : ILogger<T>
    {
        private readonly ILogger _inner;

        public LoggerWrapper(ILogger inner)
        {
            _inner = inner;
        }

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull
            => _inner.BeginScope(state);

        public bool IsEnabled(LogLevel logLevel)
            => _inner.IsEnabled(logLevel);

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
            => _inner.Log(logLevel, eventId, state, exception, formatter);
    }
}


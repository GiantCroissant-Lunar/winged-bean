using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using WingedBean.Plugins.Resource.Providers;

namespace WingedBean.Plugins.Resource;

/// <summary>
/// Central resource manager with reference counting and lifecycle management.
/// Implements RFC-0054: Resource Reference Counting and Lifecycle Management.
/// </summary>
public sealed class ResourceManager : IDisposable
{
    private readonly ILogger<ResourceManager> _logger;
    private readonly ResourceCache _cache;
    private readonly IEnumerable<IResourceProvider> _providers;
    private readonly ResourceManagerOptions _options;
    private readonly Timer? _cleanupTimer;
    private readonly ConcurrentDictionary<string, SemaphoreSlim> _loadLocks;
    private bool _disposed;

    /// <summary>
    /// Event raised when a resource is about to be reloaded.
    /// </summary>
    public event Action<string>? OnReloading;

    /// <summary>
    /// Event raised when a resource has been successfully reloaded.
    /// </summary>
    public event Action<string>? OnReloaded;

    /// <summary>
    /// Initializes a new instance of ResourceManager.
    /// </summary>
    /// <param name="logger">Logger instance</param>
    /// <param name="providers">Resource providers in priority order</param>
    /// <param name="options">Configuration options</param>
    public ResourceManager(
        ILogger<ResourceManager> logger,
        IEnumerable<IResourceProvider> providers,
        ResourceManagerOptions? options = null)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _providers = providers ?? throw new ArgumentNullException(nameof(providers));
        _cache = new ResourceCache();
        _options = options ?? ResourceManagerOptions.Default;
        _loadLocks = new ConcurrentDictionary<string, SemaphoreSlim>();

        if (_options.EnableAutoCleanup)
        {
            // Start cleanup timer for zero-refcount resources
            _cleanupTimer = new Timer(
                CleanupCallback,
                null,
                _options.CleanupInterval,
                _options.CleanupInterval
            );

            _logger.LogInformation(
                "Resource manager initialized with auto-cleanup (interval: {Interval}, idle timeout: {Timeout})",
                _options.CleanupInterval,
                _options.IdleTimeout
            );
        }
        else
        {
            _logger.LogInformation("Resource manager initialized (auto-cleanup disabled)");
        }
    }

    /// <summary>
    /// Get a strong handle to a resource (increments refcount).
    /// </summary>
    /// <typeparam name="T">Resource type</typeparam>
    /// <param name="resourceId">Resource identifier</param>
    /// <returns>A handle that manages the resource lifetime</returns>
    public ResourceHandle<T> GetHandle<T>(string resourceId) where T : class
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        return new ResourceHandle<T>(this, resourceId);
    }

    /// <summary>
    /// Get a weak handle to a resource (does not increment refcount).
    /// </summary>
    /// <typeparam name="T">Resource type</typeparam>
    /// <param name="resourceId">Resource identifier</param>
    /// <returns>A weak handle for observing resource state</returns>
    public WeakResourceHandle<T> GetWeakHandle<T>(string resourceId) where T : class
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        return new WeakResourceHandle<T>(this, resourceId);
    }

    /// <summary>
    /// Load a resource immediately (for backward compatibility with IResourceService).
    /// Returns a detached resource that is NOT reference counted.
    /// The resource is cached but not kept alive by this call.
    /// Note: Prefer using GetHandle() for automatic lifetime management.
    /// </summary>
    /// <typeparam name="T">Resource type</typeparam>
    /// <param name="resourceId">Resource identifier</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>The loaded resource, or null if loading failed</returns>
    public async Task<T?> LoadAsync<T>(
        string resourceId,
        CancellationToken ct = default) where T : class
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        // Load directly without handle to avoid ref counting
        // The resource will be cached but not kept alive
        return await LoadInternalAsync<T>(resourceId, ct);
    }

    /// <summary>
    /// Reload a resource (removes from cache and reloads).
    /// </summary>
    /// <param name="resourceId">Resource identifier</param>
    /// <param name="ct">Cancellation token</param>
    public async Task ReloadAsync(string resourceId, CancellationToken ct = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        OnReloading?.Invoke(resourceId);

        _logger.LogInformation("Reloading resource: {ResourceId}", resourceId);

        // Remove from cache (forces reload)
        _cache.Remove(resourceId);

        // Reload by creating temporary handle
        using var handle = GetHandle<object>(resourceId);
        var resource = await handle.GetAsync(ct);

        if (resource != null)
        {
            OnReloaded?.Invoke(resourceId);
            _logger.LogInformation("Resource reloaded successfully: {ResourceId}", resourceId);
        }
        else
        {
            _logger.LogWarning("Resource reload failed: {ResourceId}", resourceId);
        }
    }

    #region Internal Methods (called by handles)

    internal void AddReference(string resourceId)
    {
        _cache.AddReference(resourceId);

        if (_options.EnableDebugLogging)
        {
            var refCount = _cache.GetRefCount(resourceId);
            _logger.LogDebug(
                "Reference added for '{ResourceId}', RefCount={RefCount}",
                resourceId,
                refCount
            );
        }
    }

    internal void RemoveReference(string resourceId)
    {
        if (_cache.RemoveReference(resourceId))
        {
            var refCount = _cache.GetRefCount(resourceId);

            if (_options.EnableDebugLogging)
            {
                _logger.LogDebug(
                    "Reference removed for '{ResourceId}', RefCount={RefCount}",
                    resourceId,
                    refCount
                );
            }
        }
    }

    internal async ValueTask<T?> LoadInternalAsync<T>(
        string resourceId,
        CancellationToken ct) where T : class
    {
        // Check cache first (fast path, no lock)
        if (_cache.TryGet<T>(resourceId, out var cached))
        {
            var state = _cache.GetState(resourceId);
            if (state == ResourceLoadState.Loaded)
            {
                if (_options.EnableDebugLogging)
                {
                    _logger.LogDebug("Resource '{ResourceId}' loaded from cache", resourceId);
                }
                return cached;
            }
        }

        // Get or create semaphore for this resource
        var semaphore = _loadLocks.GetOrAdd(resourceId, _ => new SemaphoreSlim(1, 1));

        // Wait for semaphore (serializes all loads for this resource)
        await semaphore.WaitAsync(ct);

        try
        {
            // Double-check cache after acquiring lock
            if (_cache.TryGet<T>(resourceId, out cached))
            {
                var state = _cache.GetState(resourceId);
                if (state == ResourceLoadState.Loaded)
                {
                    if (_options.EnableDebugLogging)
                    {
                        _logger.LogDebug("Resource '{ResourceId}' loaded from cache (after lock)", resourceId);
                    }
                    return cached;
                }
            }

            // Start new load
            return await LoadNewAsync<T>(resourceId, ct);
        }
        finally
        {
            semaphore.Release();
        }
    }

    internal T? GetIfLoaded<T>(string resourceId) where T : class
    {
        return _cache.TryGet<T>(resourceId, out var resource) ? resource : null;
    }

    internal bool IsLoaded(string resourceId)
    {
        return _cache.GetState(resourceId) == ResourceLoadState.Loaded;
    }

    internal ResourceLoadState GetState(string resourceId)
    {
        return _cache.GetState(resourceId);
    }

    internal IObservable<T> GetLoadedObservable<T>(string resourceId) where T : class
    {
        return _cache.GetLoadedObservable<T>(resourceId);
    }

    internal IObservable<System.Reactive.Unit> GetUnloadedObservable(string resourceId)
    {
        return _cache.GetUnloadedObservable(resourceId);
    }

    #endregion

    #region Private Methods

    private async ValueTask<T?> LoadNewAsync<T>(
        string resourceId,
        CancellationToken ct) where T : class
    {
        // Mark as loading
        _cache.SetState(resourceId, ResourceLoadState.Loading);

        var loadTask = LoadViaProvidersAsync<T>(resourceId, ct).AsTask();
        _cache.SetPendingLoad(resourceId, loadTask);

        try
        {
            var resource = await loadTask;

            if (resource != null)
            {
                _cache.Set(resourceId, resource);
                _cache.SetState(resourceId, ResourceLoadState.Loaded);
                _cache.NotifyLoaded(resourceId, resource);

                _logger.LogInformation(
                    "Resource '{ResourceId}' loaded successfully (type: {Type})",
                    resourceId,
                    typeof(T).Name
                );

                return resource;
            }

            _cache.SetState(resourceId, ResourceLoadState.Failed);
            _logger.LogWarning(
                "Resource '{ResourceId}' failed to load (not found by any provider)",
                resourceId
            );

            return null;
        }
        catch (Exception ex)
        {
            _cache.SetState(resourceId, ResourceLoadState.Failed);
            _logger.LogError(
                ex,
                "Resource '{ResourceId}' failed to load (exception)",
                resourceId
            );
            return null;
        }
        finally
        {
            _cache.SetPendingLoad(resourceId, null);
        }
    }

    private async ValueTask<T?> LoadViaProvidersAsync<T>(
        string resourceId,
        CancellationToken ct) where T : class
    {
        foreach (var provider in _providers)
        {
            if (provider.CanHandle(resourceId))
            {
                _logger.LogDebug(
                    "Attempting to load '{ResourceId}' with {ProviderType}",
                    resourceId,
                    provider.GetType().Name
                );

                try
                {
                    var resource = await provider.LoadAsync<T>(resourceId, ct);
                    if (resource != null)
                    {
                        return resource;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(
                        ex,
                        "Provider {ProviderType} failed to load '{ResourceId}'",
                        provider.GetType().Name,
                        resourceId
                    );
                }
            }
        }

        return null;
    }

    private void CleanupCallback(object? state)
    {
        try
        {
            var candidates = _cache.GetCandidatesForUnload(_options.IdleTimeout);

            if (candidates.Count > 0)
            {
                _logger.LogDebug(
                    "Found {Count} idle resources eligible for cleanup",
                    candidates.Count
                );
            }

            foreach (var resourceId in candidates)
            {
                var refCount = _cache.GetRefCount(resourceId);
                if (refCount == 0)
                {
                    _logger.LogDebug(
                        "Unloading idle resource '{ResourceId}'",
                        resourceId
                    );

                    _cache.NotifyUnloaded(resourceId);
                    _cache.Remove(resourceId);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during resource cleanup");
        }
    }

    #endregion

    /// <summary>
    /// Dispose the resource manager and cleanup timer.
    /// </summary>
    public void Dispose()
    {
        if (_disposed) return;

        _cleanupTimer?.Dispose();

        // Notify unload for all cached resources
        var allResourceIds = new List<string>();
        for (int i = 0; i < _cache.Count; i++)
        {
            // Get all resource IDs (implementation would need cache enumeration)
        }

        foreach (var resourceId in allResourceIds)
        {
            _cache.NotifyUnloaded(resourceId);
        }

        _cache.Clear();
        _disposed = true;

        _logger.LogInformation("Resource manager disposed");
    }
}

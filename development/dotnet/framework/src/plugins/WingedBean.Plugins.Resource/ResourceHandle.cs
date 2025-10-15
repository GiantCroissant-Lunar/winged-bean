namespace WingedBean.Plugins.Resource;

/// <summary>
/// Strong reference to a resource with automatic reference counting.
/// Resource is guaranteed to stay loaded while handle exists.
/// Implements IDisposable for automatic cleanup.
/// </summary>
/// <typeparam name="T">The resource type</typeparam>
public sealed class ResourceHandle<T> : IDisposable where T : class
{
    private readonly ResourceManager _manager;
    private readonly string _resourceId;
    private T? _resource;
    private bool _disposed;

    /// <summary>
    /// Observable stream that emits when resource is loaded.
    /// Completes on disposal or load failure.
    /// </summary>
    public IObservable<T> OnLoaded { get; }

    /// <summary>
    /// Observable stream that emits when resource is unloaded.
    /// </summary>
    public IObservable<System.Reactive.Unit> OnUnloaded { get; }

    /// <summary>
    /// Gets the resource ID this handle references.
    /// </summary>
    public string ResourceId => _resourceId;

    internal ResourceHandle(ResourceManager manager, string resourceId)
    {
        _manager = manager ?? throw new ArgumentNullException(nameof(manager));
        _resourceId = resourceId ?? throw new ArgumentNullException(nameof(resourceId));

        // Atomically increment refcount
        _manager.AddReference(resourceId);

        // Create observables from manager's subjects
        OnLoaded = _manager.GetLoadedObservable<T>(resourceId);
        OnUnloaded = _manager.GetUnloadedObservable(resourceId);
    }

    /// <summary>
    /// Get the resource, loading if necessary.
    /// Returns cached instance on subsequent calls.
    /// </summary>
    /// <param name="ct">Cancellation token</param>
    /// <returns>The loaded resource, or null if loading failed</returns>
    /// <exception cref="ObjectDisposedException">Handle has been disposed</exception>
    public async ValueTask<T?> GetAsync(CancellationToken ct = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (_resource != null)
            return _resource;

        _resource = await _manager.LoadInternalAsync<T>(_resourceId, ct);
        return _resource;
    }

    /// <summary>
    /// Get the resource synchronously if already loaded.
    /// Returns null if not loaded yet.
    /// </summary>
    /// <returns>The resource if loaded, otherwise null</returns>
    /// <exception cref="ObjectDisposedException">Handle has been disposed</exception>
    public T? GetIfLoaded()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        return _resource ?? _manager.GetIfLoaded<T>(_resourceId);
    }

    /// <summary>
    /// Check if resource is currently loaded.
    /// </summary>
    public bool IsLoaded
    {
        get
        {
            if (_disposed) return false;
            return _manager.IsLoaded(_resourceId);
        }
    }

    /// <summary>
    /// Get current load state.
    /// </summary>
    public ResourceLoadState State
    {
        get
        {
            if (_disposed) return ResourceLoadState.Unloaded;
            return _manager.GetState(_resourceId);
        }
    }

    /// <summary>
    /// Releases the reference. Resource may be unloaded if refcount reaches zero.
    /// </summary>
    public void Dispose()
    {
        if (_disposed) return;

        _manager.RemoveReference(_resourceId);
        _resource = null;
        _disposed = true;

        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Finalizer to catch undisposed handles in debug builds.
    /// </summary>
    ~ResourceHandle()
    {
#if DEBUG
        if (!_disposed)
        {
            System.Diagnostics.Debug.WriteLine(
                $"Warning: ResourceHandle<{typeof(T).Name}> for '{_resourceId}' was not disposed properly. " +
                "Use 'using' statement or call Dispose() explicitly."
            );
        }
#endif
        Dispose();
    }
}

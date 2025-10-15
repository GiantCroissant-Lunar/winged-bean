namespace WingedBean.Plugins.Resource;

/// <summary>
/// Weak reference to a resource. Does not increment refcount.
/// Resource may be unloaded at any time if no strong handles exist.
/// Useful for observing resource state without owning the resource.
/// </summary>
/// <typeparam name="T">The resource type</typeparam>
public sealed class WeakResourceHandle<T> where T : class
{
    private readonly ResourceManager _manager;
    private readonly string _resourceId;

    /// <summary>
    /// Observable stream for load events (best-effort, may miss if unloaded).
    /// </summary>
    public IObservable<T> OnLoaded { get; }

    /// <summary>
    /// Observable stream for unload events.
    /// </summary>
    public IObservable<System.Reactive.Unit> OnUnloaded { get; }

    /// <summary>
    /// Gets the resource ID this handle references.
    /// </summary>
    public string ResourceId => _resourceId;

    internal WeakResourceHandle(ResourceManager manager, string resourceId)
    {
        _manager = manager ?? throw new ArgumentNullException(nameof(manager));
        _resourceId = resourceId ?? throw new ArgumentNullException(nameof(resourceId));

        OnLoaded = _manager.GetLoadedObservable<T>(resourceId);
        OnUnloaded = _manager.GetUnloadedObservable(resourceId);
    }

    /// <summary>
    /// Get the resource if currently loaded.
    /// Returns null if not loaded or already unloaded.
    /// </summary>
    /// <returns>The resource if loaded, otherwise null</returns>
    public T? GetIfLoaded()
    {
        return _manager.GetIfLoaded<T>(_resourceId);
    }

    /// <summary>
    /// Check if resource is currently loaded.
    /// </summary>
    public bool IsLoaded => _manager.IsLoaded(_resourceId);

    /// <summary>
    /// Get current load state.
    /// </summary>
    public ResourceLoadState State => _manager.GetState(_resourceId);

    /// <summary>
    /// Upgrade to a strong handle (increments refcount).
    /// Returns null if resource is not loaded.
    /// </summary>
    /// <returns>A new strong handle, or null if not loaded</returns>
    public ResourceHandle<T>? ToStrongHandle()
    {
        if (!IsLoaded) return null;
        return _manager.GetHandle<T>(_resourceId);
    }
}

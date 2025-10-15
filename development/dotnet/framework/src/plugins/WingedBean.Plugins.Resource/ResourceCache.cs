using System.Collections.Concurrent;
using System.Reactive.Subjects;
using System.Reactive.Linq;

namespace WingedBean.Plugins.Resource;

/// <summary>
/// Thread-safe in-memory cache for loaded resources with reference counting.
/// Enhanced to support RFC-0054: Reference counting and lifecycle management.
/// </summary>
internal class ResourceCache
{
    private readonly ConcurrentDictionary<string, CacheEntry> _cache = new();

    public bool TryGet<TResource>(string resourceId, out TResource? resource)
        where TResource : class
    {
        if (_cache.TryGetValue(resourceId, out var entry) &&
            entry.Resource is TResource typedResource)
        {
            entry.LastAccessed = DateTime.UtcNow;
            resource = typedResource;
            return true;
        }

        resource = null;
        return false;
    }

    public void Set<TResource>(string resourceId, TResource resource)
        where TResource : class
    {
        var entry = new CacheEntry
        {
            Resource = resource,
            ResourceType = typeof(TResource),
            State = ResourceLoadState.Loaded,
            LoadedAt = DateTime.UtcNow,
            LastAccessed = DateTime.UtcNow,
            RefCount = 0
        };

        _cache[resourceId] = entry;
    }

    public bool AddReference(string resourceId)
    {
        if (_cache.TryGetValue(resourceId, out var entry))
        {
            Interlocked.Increment(ref entry.RefCount);
            entry.LastAccessed = DateTime.UtcNow;
            return true;
        }

        // Create placeholder entry for resources being loaded
        var newEntry = new CacheEntry
        {
            Resource = null!,
            ResourceType = typeof(object),
            State = ResourceLoadState.Unloaded,
            LoadedAt = DateTime.UtcNow,
            LastAccessed = DateTime.UtcNow,
            RefCount = 1
        };

        _cache.TryAdd(resourceId, newEntry);
        return true;
    }

    public bool RemoveReference(string resourceId)
    {
        if (_cache.TryGetValue(resourceId, out var entry))
        {
            var newCount = Interlocked.Decrement(ref entry.RefCount);
            entry.LastAccessed = DateTime.UtcNow;
            return newCount >= 0;
        }

        return false;
    }

    public int GetRefCount(string resourceId)
    {
        if (_cache.TryGetValue(resourceId, out var entry))
        {
            return entry.RefCount;
        }

        return 0;
    }

    public ResourceLoadState GetState(string resourceId)
    {
        if (_cache.TryGetValue(resourceId, out var entry))
        {
            return entry.State;
        }

        return ResourceLoadState.Unloaded;
    }

    public bool SetState(string resourceId, ResourceLoadState state)
    {
        if (_cache.TryGetValue(resourceId, out var entry))
        {
            entry.State = state;
            return true;
        }

        return false;
    }

    public bool SetPendingLoad(string resourceId, Task? loadTask)
    {
        if (_cache.TryGetValue(resourceId, out var entry))
        {
            entry.PendingLoadTask = loadTask;
            return true;
        }

        return false;
    }

    public Task? GetPendingLoad(string resourceId)
    {
        if (_cache.TryGetValue(resourceId, out var entry))
        {
            return entry.PendingLoadTask;
        }

        return null;
    }

    public void NotifyLoaded(string resourceId, object resource)
    {
        if (_cache.TryGetValue(resourceId, out var entry))
        {
            entry.OnLoaded.OnNext(resource);
        }
    }

    public void NotifyUnloaded(string resourceId)
    {
        if (_cache.TryGetValue(resourceId, out var entry))
        {
            entry.OnUnloaded.OnNext(System.Reactive.Unit.Default);
            entry.OnLoaded.OnCompleted();
            entry.OnUnloaded.OnCompleted();
        }
    }

    public IObservable<T> GetLoadedObservable<T>(string resourceId) where T : class
    {
        if (_cache.TryGetValue(resourceId, out var entry))
        {
            // Use System.Reactive.Linq.Observable.OfType for proper observable filtering
            return System.Reactive.Linq.Observable.Where(
                entry.OnLoaded,
                obj => obj is T
            ).Select(obj => (T)obj);
        }

        // Return empty observable for non-existent resources
        return System.Reactive.Linq.Observable.Empty<T>();
    }

    public IObservable<System.Reactive.Unit> GetUnloadedObservable(string resourceId)
    {
        if (_cache.TryGetValue(resourceId, out var entry))
        {
            return entry.OnUnloaded;
        }

        return System.Reactive.Linq.Observable.Empty<System.Reactive.Unit>();
    }

    public List<string> GetCandidatesForUnload(TimeSpan idleTime)
    {
        var cutoff = DateTime.UtcNow - idleTime;
        var candidates = new List<string>();

        foreach (var kvp in _cache)
        {
            if (kvp.Value.RefCount == 0 &&
                kvp.Value.LastAccessed < cutoff &&
                kvp.Value.State == ResourceLoadState.Loaded)
            {
                candidates.Add(kvp.Key);
            }
        }

        return candidates;
    }

    public bool Remove(string resourceId)
    {
        return _cache.TryRemove(resourceId, out _);
    }

    public int RemoveAll<TResource>() where TResource : class
    {
        var targetType = typeof(TResource);
        var count = 0;

        foreach (var kvp in _cache)
        {
            if (kvp.Value.ResourceType == targetType)
            {
                if (_cache.TryRemove(kvp.Key, out _))
                {
                    count++;
                }
            }
        }

        return count;
    }

    public bool Contains(string resourceId)
    {
        return _cache.ContainsKey(resourceId);
    }

    public void Clear()
    {
        _cache.Clear();
    }

    public int Count => _cache.Count;

    /// <summary>
    /// Enhanced cache entry with reference counting and lifecycle events.
    /// </summary>
    private class CacheEntry
    {
        public required object Resource { get; set; }
        public required Type ResourceType { get; init; }

        // Reference counting
        public int RefCount;

        // Load state machine
        public ResourceLoadState State { get; set; }

        // Timestamps
        public DateTime LoadedAt { get; set; }
        public DateTime LastAccessed { get; set; }

        // Lifecycle events (System.Reactive)
        public Subject<object> OnLoaded { get; init; } = new();
        public Subject<System.Reactive.Unit> OnUnloaded { get; init; } = new();

        // Pending load task (for deduplication)
        public Task? PendingLoadTask { get; set; }
    }
}

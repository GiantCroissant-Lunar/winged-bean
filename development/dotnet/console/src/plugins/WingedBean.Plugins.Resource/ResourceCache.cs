using System.Collections.Concurrent;

namespace WingedBean.Plugins.Resource;

/// <summary>
/// Thread-safe in-memory cache for loaded resources.
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
            LastAccessed = DateTime.UtcNow
        };

        _cache[resourceId] = entry;
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

    private class CacheEntry
    {
        public required object Resource { get; init; }
        public required Type ResourceType { get; init; }
        public DateTime LastAccessed { get; set; }
    }
}

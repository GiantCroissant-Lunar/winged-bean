using System.Collections.Concurrent;

namespace WingedBean.Plugins.Resource.NuGet;

/// <summary>
/// Thread-safe in-memory cache for loaded NuGet packages.
/// </summary>
public class NuGetPackageCache
{
    private readonly ConcurrentDictionary<string, CacheEntry> _cache = new();
    
    /// <summary>
    /// Try to get a cached package.
    /// </summary>
    public bool TryGet(string packageId, string? version, out NuGetPackageResource? package)
    {
        var key = GetCacheKey(packageId, version);
        
        if (_cache.TryGetValue(key, out var entry))
        {
            entry.LastAccessed = DateTime.UtcNow;
            entry.AccessCount++;
            package = entry.Package;
            return true;
        }
        
        package = null;
        return false;
    }
    
    /// <summary>
    /// Add or update a package in the cache.
    /// </summary>
    public void Set(NuGetPackageResource package)
    {
        var key = GetCacheKey(package.PackageId, package.Version);
        
        var entry = new CacheEntry
        {
            Package = package,
            CachedAt = DateTime.UtcNow,
            LastAccessed = DateTime.UtcNow,
            AccessCount = 0
        };
        
        _cache[key] = entry;
    }
    
    /// <summary>
    /// Remove a specific package from cache.
    /// </summary>
    public bool Remove(string packageId, string? version = null)
    {
        if (version != null)
        {
            var key = GetCacheKey(packageId, version);
            return _cache.TryRemove(key, out _);
        }
        
        // Remove all versions of this package
        var keysToRemove = _cache.Keys
            .Where(k => k.StartsWith($"{packageId.ToLowerInvariant()}:"))
            .ToList();
        
        var removed = false;
        foreach (var key in keysToRemove)
        {
            if (_cache.TryRemove(key, out _))
            {
                removed = true;
            }
        }
        
        return removed;
    }
    
    /// <summary>
    /// Clear entire cache.
    /// </summary>
    public void Clear()
    {
        _cache.Clear();
    }
    
    /// <summary>
    /// Get cache statistics.
    /// </summary>
    public CacheStatistics GetStatistics()
    {
        var entries = _cache.Values.ToList();
        
        return new CacheStatistics
        {
            TotalPackages = entries.Count,
            TotalAccessCount = entries.Sum(e => e.AccessCount),
            OldestCachedAt = entries.Any() ? entries.Min(e => e.CachedAt) : (DateTime?)null,
            MostRecentAccess = entries.Any() ? entries.Max(e => e.LastAccessed) : (DateTime?)null,
            MostAccessedPackage = entries
                .OrderByDescending(e => e.AccessCount)
                .FirstOrDefault()
                ?.Package.PackageId
        };
    }
    
    /// <summary>
    /// Remove least recently used entries to stay under size limit.
    /// </summary>
    public int EvictLeastRecentlyUsed(int maxEntries)
    {
        if (_cache.Count <= maxEntries)
        {
            return 0;
        }
        
        var entriesToRemove = _cache
            .OrderBy(kvp => kvp.Value.LastAccessed)
            .Take(_cache.Count - maxEntries)
            .Select(kvp => kvp.Key)
            .ToList();
        
        var removed = 0;
        foreach (var key in entriesToRemove)
        {
            if (_cache.TryRemove(key, out _))
            {
                removed++;
            }
        }
        
        return removed;
    }
    
    private static string GetCacheKey(string packageId, string? version)
    {
        var normalizedId = packageId.ToLowerInvariant();
        var normalizedVersion = version?.ToLowerInvariant() ?? "latest";
        return $"{normalizedId}:{normalizedVersion}";
    }
    
    private class CacheEntry
    {
        public required NuGetPackageResource Package { get; init; }
        public DateTime CachedAt { get; init; }
        public DateTime LastAccessed { get; set; }
        public int AccessCount { get; set; }
    }
}

/// <summary>
/// Cache statistics.
/// </summary>
public class CacheStatistics
{
    public int TotalPackages { get; init; }
    public long TotalAccessCount { get; init; }
    public DateTime? OldestCachedAt { get; init; }
    public DateTime? MostRecentAccess { get; init; }
    public string? MostAccessedPackage { get; init; }
}

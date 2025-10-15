---
id: RFC-0055
title: Enhanced Resource Provider Ecosystem
status: Proposed
category: framework, resource-management, providers
created: 2025-10-15
updated: 2025-10-15
author: GitHub Copilot
priority: MEDIUM (P2)
depends_on: RFC-0054
---

# RFC-0055: Enhanced Resource Provider Ecosystem

## Status

**Proposed** - Depends on RFC-0054

## Date

2025-10-15

## Summary

Extend the WingedBean resource provider ecosystem with new backend providers (HTTP/CDN, rclone cloud storage, addressable assets), persistent caching with MemoryPack, and hot reload support. This RFC builds on RFC-0054's reference counting foundation to enable advanced resource loading scenarios.

## Motivation

### Current Provider Landscape

After RFC-0054, WingedBean has solid reference counting and lifecycle management. Current providers:

- ✅ **FileSystemProvider**: Local file access (RFC-0027)
- ✅ **NuGetProvider**: NuGet package resources (RFC-0039)
- ✅ **BundleProvider**: Packed resource bundles (RFC-0027)

### New Requirements

1. **Cloud Storage**: Load assets from GCS/S3/Azure without manual downloads
2. **CDN Support**: Fetch assets from CDN with local caching
3. **Addressable System**: Unity Addressables-like logical addressing
4. **Persistent Cache**: Faster startup by caching deserialized resources
5. **Hot Reload**: Detect file changes and reload resources dynamically
6. **Version Migration**: Handle asset format changes (like FlaxEngine's IAssetUpgrader)

### Comparison with FlaxEngine

| Feature | FlaxEngine | WingedBean Current | This RFC |
|---------|-----------|-------------------|----------|
| **File System** | ✅ FlaxStorage | ✅ FileSystemProvider | ✅ Keep |
| **Packages/Bundles** | ✅ FlaxPackage | ✅ ResourceBundle | ✅ Keep |
| **Remote/HTTP** | ❌ Not built-in | ❌ None | ✅ Add |
| **Cloud Storage** | ❌ Not built-in | ❌ None | ✅ Add (rclone) |
| **Addressables** | ❌ Path-based only | ❌ Path-based | ✅ Add |
| **Persistent Cache** | ✅ AssetsCache | ❌ Memory only | ✅ Add (MemoryPack) |
| **Hot Reload** | ✅ OnReloading event | ❌ None | ✅ Add (FileSystemWatcher) |
| **Versioning** | ✅ IAssetUpgrader | ❌ None | ✅ Add (migration system) |

### Use Cases

**UC-1: Remote Asset Delivery**
```
Game downloads levels from CDN at runtime instead of bundling all in APK/binary.
Reduces initial download size from 500MB to 50MB.
```

**UC-2: Cloud Save Integration**
```
Load player profiles from Google Cloud Storage using rclone.
No custom backend code needed - rclone handles auth and sync.
```

**UC-3: Logical Addressing**
```
Reference "ui/button-primary" instead of "Assets/UI/Buttons/Primary/button-primary.png"
Allows swapping implementations without code changes (e.g., A/B testing).
```

**UC-4: Fast Startup**
```
Cache deserialized JSON resources with MemoryPack.
Startup time reduced from 5s to 500ms for 1000 JSON assets.
```

**UC-5: Live Development**
```
Edit JSON files while game running, press F5 to reload.
No need to restart game for data tweaks (balancing, text edits).
```

## Proposal

### Architecture Overview

```
┌──────────────────────────────────────────────────────────────────┐
│                    ResourceManager (RFC-0054)                    │
│              Reference Counting + Lifecycle Events               │
└──────────────────────────────────────────────────────────────────┘
                                ↓ uses
┌──────────────────────────────────────────────────────────────────┐
│                     Provider Chain (Priority)                    │
├──────────────────────────────────────────────────────────────────┤
│  1. AddressableProvider      ⬅ NEW (logical → physical mapping) │
│  2. PersistentCacheProvider  ⬅ NEW (MemoryPack cache)           │
│  3. HttpProvider             ⬅ NEW (HTTP/HTTPS + CDN)            │
│  4. RcloneProvider           ⬅ NEW (Cloud storage via rclone)    │
│  5. BundleProvider           ✅ EXISTING (packed resources)      │
│  6. NuGetProvider            ✅ EXISTING (RFC-0039)              │
│  7. FileSystemProvider       ✅ EXISTING (RFC-0027)              │
└──────────────────────────────────────────────────────────────────┘
                                ↓ supports
┌──────────────────────────────────────────────────────────────────┐
│                     Cross-Cutting Concerns                       │
├──────────────────────────────────────────────────────────────────┤
│  • HotReloadService          ⬅ NEW (FileSystemWatcher)          │
│  • MigrationService          ⬅ NEW (version upgrades)           │
│  • PersistentCache           ⬅ NEW (MemoryPack serialization)   │
│  • MetricsCollector          ⬅ NEW (load times, cache hits)     │
└──────────────────────────────────────────────────────────────────┘
```

### 1. AddressableProvider - Logical Asset Addressing

**Purpose**: Map logical addresses to physical paths/URLs.

```csharp
/// <summary>
/// Provider that maps logical addresses to physical resource locations.
/// Enables flexible resource routing without hardcoded paths.
/// </summary>
public class AddressableProvider : IResourceProvider
{
    private readonly ILogger<AddressableProvider> _logger;
    private readonly AddressableManifest _manifest;
    private readonly IResourceProvider _backingProvider;
    
    public AddressableProvider(
        ILogger<AddressableProvider> logger,
        AddressableManifest manifest,
        IResourceProvider backingProvider)
    {
        _logger = logger;
        _manifest = manifest;
        _backingProvider = backingProvider;
    }
    
    public bool CanHandle(string resourceId)
    {
        // Handle addressable format: "addressable:{address}"
        // or just use logical names directly
        return _manifest.HasAddress(resourceId);
    }
    
    public async Task<TResource?> LoadAsync<TResource>(
        string resourceId,
        CancellationToken ct = default) where TResource : class
    {
        // Resolve logical address to physical path
        if (!_manifest.TryResolve(resourceId, out var physicalPath))
        {
            _logger.LogWarning(
                "Addressable '{Address}' not found in manifest",
                resourceId
            );
            return null;
        }
        
        _logger.LogDebug(
            "Resolved addressable '{Address}' to '{Path}'",
            resourceId,
            physicalPath
        );
        
        // Delegate to backing provider (could be HTTP, filesystem, etc.)
        return await _backingProvider.LoadAsync<TResource>(physicalPath, ct);
    }
    
    public Task<ResourceMetadata?> GetMetadataAsync(
        string resourceId,
        CancellationToken ct = default)
    {
        if (_manifest.TryResolve(resourceId, out var physicalPath))
        {
            return _backingProvider.GetMetadataAsync(physicalPath, ct);
        }
        
        return Task.FromResult<ResourceMetadata?>(null);
    }
}

/// <summary>
/// Manifest mapping logical addresses to physical locations.
/// Serialized as JSON for easy editing.
/// </summary>
public class AddressableManifest
{
    public Dictionary<string, AddressableEntry> Entries { get; set; } = new();
    
    public bool HasAddress(string address) => Entries.ContainsKey(address);
    
    public bool TryResolve(string address, out string physicalPath)
    {
        if (Entries.TryGetValue(address, out var entry))
        {
            physicalPath = entry.PhysicalPath;
            return true;
        }
        
        physicalPath = string.Empty;
        return false;
    }
    
    public static AddressableManifest LoadFromFile(string path)
    {
        var json = File.ReadAllText(path);
        return JsonSerializer.Deserialize<AddressableManifest>(json)
            ?? new AddressableManifest();
    }
}

public class AddressableEntry
{
    /// <summary>
    /// Logical address (e.g., "ui/button-primary")
    /// </summary>
    public required string Address { get; set; }
    
    /// <summary>
    /// Physical location (e.g., "https://cdn.example.com/assets/ui/btn-primary.png")
    /// </summary>
    public required string PhysicalPath { get; set; }
    
    /// <summary>
    /// Resource type hint (optional)
    /// </summary>
    public string? TypeHint { get; set; }
    
    /// <summary>
    /// Tags for filtering/grouping (optional)
    /// </summary>
    public List<string>? Tags { get; set; }
}
```

**Example Manifest** (`addressables.json`):
```json
{
  "entries": {
    "ui/button-primary": {
      "address": "ui/button-primary",
      "physicalPath": "https://cdn.example.com/ui/btn-primary.png",
      "typeHint": "Texture",
      "tags": ["ui", "button"]
    },
    "levels/dungeon1": {
      "address": "levels/dungeon1",
      "physicalPath": "bundles/levels.bundle#dungeon1",
      "typeHint": "LevelData",
      "tags": ["level", "dungeon"]
    },
    "config/game-balance": {
      "address": "config/game-balance",
      "physicalPath": "gcs:my-bucket/config/balance-v2.json",
      "typeHint": "BalanceConfig",
      "tags": ["config"]
    }
  }
}
```

**Usage**:
```csharp
// Load via logical address
using var handle = resourceManager.GetHandle<Texture>("ui/button-primary");
var texture = await handle.GetAsync();

// Under the hood:
// 1. AddressableProvider resolves to "https://cdn.example.com/ui/btn-primary.png"
// 2. HttpProvider fetches from CDN
// 3. PersistentCacheProvider caches locally
```

### 2. HttpProvider - HTTP/HTTPS Resource Loading

**Purpose**: Fetch resources from web servers and CDNs.

```csharp
/// <summary>
/// Provider for loading resources via HTTP/HTTPS.
/// Supports CDN, versioning, and local caching.
/// </summary>
public class HttpProvider : IResourceProvider
{
    private readonly ILogger<HttpProvider> _logger;
    private readonly HttpClient _httpClient;
    private readonly string? _cacheDirectory;
    private readonly bool _enableCache;
    
    public HttpProvider(
        ILogger<HttpProvider> logger,
        HttpClient? httpClient = null,
        HttpProviderOptions? options = null)
    {
        _logger = logger;
        _httpClient = httpClient ?? new HttpClient();
        
        var opts = options ?? HttpProviderOptions.Default;
        _cacheDirectory = opts.CacheDirectory;
        _enableCache = opts.EnableCache && _cacheDirectory != null;
        
        if (_enableCache && _cacheDirectory != null)
        {
            Directory.CreateDirectory(_cacheDirectory);
        }
    }
    
    public bool CanHandle(string resourceId)
    {
        return resourceId.StartsWith("http://") 
            || resourceId.StartsWith("https://");
    }
    
    public async Task<TResource?> LoadAsync<TResource>(
        string resourceId,
        CancellationToken ct = default) where TResource : class
    {
        // Check local cache first
        if (_enableCache)
        {
            var cached = TryLoadFromCache<TResource>(resourceId);
            if (cached != null)
            {
                _logger.LogDebug("HTTP resource loaded from cache: {Url}", resourceId);
                return cached;
            }
        }
        
        try
        {
            _logger.LogInformation("Downloading resource from {Url}", resourceId);
            
            var response = await _httpClient.GetAsync(resourceId, ct);
            response.EnsureSuccessStatusCode();
            
            var bytes = await response.Content.ReadAsByteArrayAsync(ct);
            
            // Deserialize based on content type or resource type
            var resource = await DeserializeAsync<TResource>(bytes, response.Content.Headers.ContentType?.MediaType);
            
            // Cache locally
            if (_enableCache && resource != null)
            {
                await SaveToCacheAsync(resourceId, bytes);
            }
            
            return resource;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Failed to download resource from {Url}", resourceId);
            return null;
        }
    }
    
    public async Task<ResourceMetadata?> GetMetadataAsync(
        string resourceId,
        CancellationToken ct = default)
    {
        try
        {
            // HEAD request to get metadata without downloading
            var request = new HttpRequestMessage(HttpMethod.Head, resourceId);
            var response = await _httpClient.SendAsync(request, ct);
            
            if (response.IsSuccessStatusCode)
            {
                return new ResourceMetadata
                {
                    Path = resourceId,
                    Size = response.Content.Headers.ContentLength ?? 0,
                    LastModified = response.Content.Headers.LastModified?.UtcDateTime,
                    ContentType = response.Content.Headers.ContentType?.MediaType
                };
            }
        }
        catch (HttpRequestException ex)
        {
            _logger.LogWarning(ex, "Failed to get metadata for {Url}", resourceId);
        }
        
        return null;
    }
    
    private TResource? TryLoadFromCache<TResource>(string url) where TResource : class
    {
        var cachePath = GetCachePath(url);
        if (!File.Exists(cachePath))
            return null;
        
        try
        {
            var bytes = File.ReadAllBytes(cachePath);
            return DeserializeAsync<TResource>(bytes, null).GetAwaiter().GetResult();
        }
        catch
        {
            return null;
        }
    }
    
    private async Task SaveToCacheAsync(string url, byte[] data)
    {
        var cachePath = GetCachePath(url);
        await File.WriteAllBytesAsync(cachePath, data);
    }
    
    private string GetCachePath(string url)
    {
        // Hash URL to create safe filename
        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(url)));
        return Path.Combine(_cacheDirectory!, $"{hash}.cache");
    }
    
    private async Task<TResource?> DeserializeAsync<TResource>(
        byte[] data,
        string? contentType) where TResource : class
    {
        // Handle different formats
        if (typeof(TResource) == typeof(string))
        {
            return Encoding.UTF8.GetString(data) as TResource;
        }
        
        if (typeof(TResource) == typeof(byte[]))
        {
            return data as TResource;
        }
        
        // JSON by default or if content-type indicates JSON
        if (contentType?.Contains("json") ?? false || contentType == null)
        {
            var json = Encoding.UTF8.GetString(data);
            return JsonSerializer.Deserialize<TResource>(json);
        }
        
        // Add more formats as needed (XML, MessagePack, etc.)
        
        return null;
    }
}

public class HttpProviderOptions
{
    public bool EnableCache { get; set; } = true;
    public string? CacheDirectory { get; set; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "WingedBean", "HttpCache"
    );
    public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(30);
    
    public static HttpProviderOptions Default => new();
}
```

### 3. RcloneProvider - Cloud Storage via rclone

**Purpose**: Access cloud storage (GCS, S3, Azure) using rclone as backend.

```csharp
/// <summary>
/// Provider for cloud storage via rclone.
/// Supports GCS, S3, Azure Blob, Dropbox, etc. through rclone remotes.
/// </summary>
public class RcloneProvider : IResourceProvider
{
    private readonly ILogger<RcloneProvider> _logger;
    private readonly string _rclonePath;
    private readonly string _cacheDirectory;
    
    public RcloneProvider(
        ILogger<RcloneProvider> logger,
        RcloneProviderOptions? options = null)
    {
        _logger = logger;
        
        var opts = options ?? RcloneProviderOptions.Default;
        _rclonePath = opts.RclonePath ?? "rclone"; // Assume in PATH
        _cacheDirectory = opts.CacheDirectory;
        
        Directory.CreateDirectory(_cacheDirectory);
    }
    
    public bool CanHandle(string resourceId)
    {
        // Format: "rclone:{remote}:{path}"
        // or "gcs:{bucket}/{path}", "s3:{bucket}/{path}"
        return resourceId.StartsWith("rclone:") 
            || resourceId.StartsWith("gcs:")
            || resourceId.StartsWith("s3:")
            || resourceId.StartsWith("azure:");
    }
    
    public async Task<TResource?> LoadAsync<TResource>(
        string resourceId,
        CancellationToken ct = default) where TResource : class
    {
        // Parse resource ID
        var parts = resourceId.Split(':', 2);
        if (parts.Length != 2)
        {
            _logger.LogWarning("Invalid rclone resource ID: {ResourceId}", resourceId);
            return null;
        }
        
        var protocol = parts[0]; // "rclone", "gcs", "s3", etc.
        var remotePath = parts[1];
        
        // Map protocol to rclone remote name
        var rcloneRemote = MapProtocolToRemote(protocol, remotePath);
        
        // Check cache
        var cacheKey = $"{protocol}_{remotePath.Replace('/', '_')}";
        var cachePath = Path.Combine(_cacheDirectory, cacheKey);
        
        if (File.Exists(cachePath))
        {
            _logger.LogDebug("Loading from rclone cache: {CacheKey}", cacheKey);
            var bytes = await File.ReadAllBytesAsync(cachePath, ct);
            return DeserializeResource<TResource>(bytes);
        }
        
        // Download via rclone
        try
        {
            _logger.LogInformation("Downloading via rclone: {Remote}", rcloneRemote);
            
            var startInfo = new ProcessStartInfo
            {
                FileName = _rclonePath,
                Arguments = $"cat \"{rcloneRemote}\"",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };
            
            using var process = Process.Start(startInfo);
            if (process == null)
            {
                _logger.LogError("Failed to start rclone process");
                return null;
            }
            
            using var memoryStream = new MemoryStream();
            await process.StandardOutput.BaseStream.CopyToAsync(memoryStream, ct);
            await process.WaitForExitAsync(ct);
            
            if (process.ExitCode != 0)
            {
                var error = await process.StandardError.ReadToEndAsync(ct);
                _logger.LogError("Rclone failed: {Error}", error);
                return null;
            }
            
            var data = memoryStream.ToArray();
            
            // Save to cache
            await File.WriteAllBytesAsync(cachePath, data, ct);
            
            return DeserializeResource<TResource>(data);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Rclone download failed for {Remote}", rcloneRemote);
            return null;
        }
    }
    
    public async Task<ResourceMetadata?> GetMetadataAsync(
        string resourceId,
        CancellationToken ct = default)
    {
        // Use rclone lsjson to get metadata
        var parts = resourceId.Split(':', 2);
        if (parts.Length != 2) return null;
        
        var remotePath = MapProtocolToRemote(parts[0], parts[1]);
        
        try
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = _rclonePath,
                Arguments = $"lsjson \"{remotePath}\" --no-modtime=false",
                RedirectStandardOutput = true,
                UseShellExecute = false
            };
            
            using var process = Process.Start(startInfo);
            if (process == null) return null;
            
            var output = await process.StandardOutput.ReadToEndAsync(ct);
            await process.WaitForExitAsync(ct);
            
            if (process.ExitCode != 0) return null;
            
            var entries = JsonSerializer.Deserialize<List<RcloneFileInfo>>(output);
            var entry = entries?.FirstOrDefault();
            
            if (entry != null)
            {
                return new ResourceMetadata
                {
                    Path = resourceId,
                    Size = entry.Size,
                    LastModified = entry.ModTime
                };
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to get rclone metadata for {ResourceId}", resourceId);
        }
        
        return null;
    }
    
    private string MapProtocolToRemote(string protocol, string path)
    {
        // Map common protocols to rclone remote names
        // User must configure remotes in rclone.conf
        return protocol switch
        {
            "gcs" => $"gcs:{path}",
            "s3" => $"s3:{path}",
            "azure" => $"azure:{path}",
            "rclone" => path, // Already in rclone format
            _ => path
        };
    }
    
    private TResource? DeserializeResource<TResource>(byte[] data) where TResource : class
    {
        if (typeof(TResource) == typeof(byte[]))
            return data as TResource;
        
        if (typeof(TResource) == typeof(string))
            return Encoding.UTF8.GetString(data) as TResource;
        
        // Assume JSON
        var json = Encoding.UTF8.GetString(data);
        return JsonSerializer.Deserialize<TResource>(json);
    }
    
    private class RcloneFileInfo
    {
        public string Path { get; set; } = "";
        public string Name { get; set; } = "";
        public long Size { get; set; }
        public DateTime ModTime { get; set; }
    }
}

public class RcloneProviderOptions
{
    public string? RclonePath { get; set; }
    public string CacheDirectory { get; set; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "WingedBean", "RcloneCache"
    );
    
    public static RcloneProviderOptions Default => new();
}
```

### 4. PersistentCacheProvider - MemoryPack Serialization Cache

**Purpose**: Cache deserialized resources to disk for faster startup.

```csharp
/// <summary>
/// Provider that caches deserialized resources using MemoryPack.
/// Wraps another provider and adds persistent caching layer.
/// </summary>
public class PersistentCacheProvider : IResourceProvider
{
    private readonly ILogger<PersistentCacheProvider> _logger;
    private readonly IResourceProvider _backingProvider;
    private readonly string _cacheDirectory;
    
    public PersistentCacheProvider(
        ILogger<PersistentCacheProvider> logger,
        IResourceProvider backingProvider,
        string? cacheDirectory = null)
    {
        _logger = logger;
        _backingProvider = backingProvider;
        _cacheDirectory = cacheDirectory ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "WingedBean", "PersistentCache"
        );
        
        Directory.CreateDirectory(_cacheDirectory);
    }
    
    public bool CanHandle(string resourceId)
    {
        return _backingProvider.CanHandle(resourceId);
    }
    
    public async Task<TResource?> LoadAsync<TResource>(
        string resourceId,
        CancellationToken ct = default) where TResource : class
    {
        // Check persistent cache
        var cached = TryLoadFromCache<TResource>(resourceId);
        if (cached != null)
        {
            _logger.LogDebug("Loaded from persistent cache: {ResourceId}", resourceId);
            return cached;
        }
        
        // Load via backing provider
        var resource = await _backingProvider.LoadAsync<TResource>(resourceId, ct);
        
        // Save to persistent cache
        if (resource != null)
        {
            await SaveToCacheAsync(resourceId, resource);
        }
        
        return resource;
    }
    
    public Task<ResourceMetadata?> GetMetadataAsync(
        string resourceId,
        CancellationToken ct = default)
    {
        return _backingProvider.GetMetadataAsync(resourceId, ct);
    }
    
    private TResource? TryLoadFromCache<TResource>(string resourceId) where TResource : class
    {
        var cachePath = GetCachePath(resourceId, typeof(TResource));
        
        if (!File.Exists(cachePath))
            return null;
        
        try
        {
            var bytes = File.ReadAllBytes(cachePath);
            
            // Use MemoryPack if type supports it
            if (HasMemoryPackableAttribute<TResource>())
            {
                return MemoryPackSerializer.Deserialize<TResource>(bytes);
            }
            
            // Fallback to JSON
            var json = Encoding.UTF8.GetString(bytes);
            return JsonSerializer.Deserialize<TResource>(json);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to load from cache: {CachePath}", cachePath);
            
            // Delete corrupted cache file
            try { File.Delete(cachePath); } catch { }
            
            return null;
        }
    }
    
    private async Task SaveToCacheAsync<TResource>(string resourceId, TResource resource)
        where TResource : class
    {
        var cachePath = GetCachePath(resourceId, typeof(TResource));
        
        try
        {
            byte[] bytes;
            
            // Use MemoryPack if available
            if (HasMemoryPackableAttribute<TResource>())
            {
                bytes = MemoryPackSerializer.Serialize(resource);
            }
            else
            {
                // Fallback to JSON
                var json = JsonSerializer.Serialize(resource);
                bytes = Encoding.UTF8.GetBytes(json);
            }
            
            await File.WriteAllBytesAsync(cachePath, bytes);
            
            _logger.LogDebug("Saved to persistent cache: {CachePath}", cachePath);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to save to cache: {CachePath}", cachePath);
        }
    }
    
    private string GetCachePath(string resourceId, Type resourceType)
    {
        // Create safe filename from resource ID + type
        var hash = Convert.ToHexString(
            SHA256.HashData(Encoding.UTF8.GetBytes(resourceId + resourceType.FullName))
        );
        return Path.Combine(_cacheDirectory, $"{hash}.cache");
    }
    
    private bool HasMemoryPackableAttribute<T>()
    {
        return typeof(T).GetCustomAttribute<MemoryPackableAttribute>() != null;
    }
}
```

### 5. HotReloadService - Dynamic Resource Reloading

**Purpose**: Watch file system for changes and reload resources automatically.

```csharp
/// <summary>
/// Service that watches for file changes and triggers resource reload.
/// </summary>
public class HotReloadService : IDisposable
{
    private readonly ILogger<HotReloadService> _logger;
    private readonly ResourceManager _resourceManager;
    private readonly FileSystemWatcher _watcher;
    private readonly Dictionary<string, string> _pathToResourceId;
    
    public HotReloadService(
        ILogger<HotReloadService> logger,
        ResourceManager resourceManager,
        HotReloadOptions options)
    {
        _logger = logger;
        _resourceManager = resourceManager;
        _pathToResourceId = new Dictionary<string, string>();
        
        _watcher = new FileSystemWatcher(options.WatchDirectory)
        {
            NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName,
            Filter = options.FileFilter,
            IncludeSubdirectories = options.IncludeSubdirectories,
            EnableRaisingEvents = false // Start manually
        };
        
        _watcher.Changed += OnFileChanged;
        _watcher.Created += OnFileChanged;
        _watcher.Deleted += OnFileDeleted;
        _watcher.Renamed += OnFileRenamed;
    }
    
    public void Start()
    {
        _watcher.EnableRaisingEvents = true;
        _logger.LogInformation("Hot reload started watching: {Path}", _watcher.Path);
    }
    
    public void Stop()
    {
        _watcher.EnableRaisingEvents = false;
        _logger.LogInformation("Hot reload stopped");
    }
    
    public void RegisterResource(string filePath, string resourceId)
    {
        _pathToResourceId[Path.GetFullPath(filePath)] = resourceId;
    }
    
    private void OnFileChanged(object sender, FileSystemEventArgs e)
    {
        if (_pathToResourceId.TryGetValue(e.FullPath, out var resourceId))
        {
            _logger.LogInformation("File changed, reloading: {Path} -> {ResourceId}", e.FullPath, resourceId);
            
            // Trigger reload via OnReloading event
            Task.Run(async () =>
            {
                // Small delay to ensure file write is complete
                await Task.Delay(100);
                
                await _resourceManager.ReloadAsync(resourceId);
            });
        }
    }
    
    private void OnFileDeleted(object sender, FileSystemEventArgs e)
    {
        if (_pathToResourceId.TryGetValue(e.FullPath, out var resourceId))
        {
            _logger.LogWarning("File deleted: {Path} -> {ResourceId}", e.FullPath, resourceId);
            _pathToResourceId.Remove(e.FullPath);
        }
    }
    
    private void OnFileRenamed(object sender, RenamedEventArgs e)
    {
        if (_pathToResourceId.TryGetValue(e.OldFullPath, out var resourceId))
        {
            _logger.LogInformation("File renamed: {OldPath} -> {NewPath}", e.OldFullPath, e.FullPath);
            _pathToResourceId.Remove(e.OldFullPath);
            _pathToResourceId[e.FullPath] = resourceId;
        }
    }
    
    public void Dispose()
    {
        _watcher?.Dispose();
    }
}

public class HotReloadOptions
{
    public required string WatchDirectory { get; set; }
    public string FileFilter { get; set; } = "*.*";
    public bool IncludeSubdirectories { get; set; } = true;
}

// Extension to ResourceManager
public partial class ResourceManager
{
    public event Action<string>? OnReloading;
    public event Action<string>? OnReloaded;
    
    public async Task ReloadAsync(string resourceId)
    {
        OnReloading?.Invoke(resourceId);
        
        // Remove from cache (forces reload)
        _cache.Remove(resourceId);
        
        // Reload
        using var handle = GetHandle<object>(resourceId);
        var resource = await handle.GetAsync();
        
        if (resource != null)
        {
            OnReloaded?.Invoke(resourceId);
            _logger.LogInformation("Resource reloaded: {ResourceId}", resourceId);
        }
    }
}
```

### 6. MigrationService - Asset Version Upgrades

**Purpose**: Handle asset format changes (like FlaxEngine's IAssetUpgrader).

```csharp
/// <summary>
/// Interface for asset migration from old version to new version.
/// </summary>
public interface IResourceMigrator
{
    /// <summary>
    /// Source version this migrator upgrades from.
    /// </summary>
    int SourceVersion { get; }
    
    /// <summary>
    /// Target version this migrator upgrades to.
    /// </summary>
    int TargetVersion { get; }
    
    /// <summary>
    /// Resource type this migrator handles.
    /// </summary>
    Type ResourceType { get; }
    
    /// <summary>
    /// Migrate resource from old format to new format.
    /// </summary>
    Task<object> MigrateAsync(object oldResource, CancellationToken ct = default);
}

/// <summary>
/// Service that applies migration chains to resources.
/// </summary>
public class MigrationService
{
    private readonly ILogger<MigrationService> _logger;
    private readonly Dictionary<Type, List<IResourceMigrator>> _migrators;
    
    public MigrationService(
        ILogger<MigrationService> logger,
        IEnumerable<IResourceMigrator>? migrators = null)
    {
        _logger = logger;
        _migrators = new Dictionary<Type, List<IResourceMigrator>>();
        
        if (migrators != null)
        {
            foreach (var migrator in migrators)
            {
                RegisterMigrator(migrator);
            }
        }
    }
    
    public void RegisterMigrator(IResourceMigrator migrator)
    {
        if (!_migrators.ContainsKey(migrator.ResourceType))
        {
            _migrators[migrator.ResourceType] = new List<IResourceMigrator>();
        }
        
        _migrators[migrator.ResourceType].Add(migrator);
        _migrators[migrator.ResourceType].Sort((a, b) => a.SourceVersion.CompareTo(b.SourceVersion));
    }
    
    public async Task<T> MigrateAsync<T>(T resource, int fromVersion, int toVersion, CancellationToken ct = default)
        where T : class
    {
        if (fromVersion == toVersion)
            return resource;
        
        if (!_migrators.TryGetValue(typeof(T), out var migrators))
        {
            _logger.LogWarning("No migrators found for type {Type}", typeof(T).Name);
            return resource;
        }
        
        object current = resource;
        int currentVersion = fromVersion;
        
        while (currentVersion < toVersion)
        {
            var migrator = migrators.FirstOrDefault(m => m.SourceVersion == currentVersion);
            if (migrator == null)
            {
                _logger.LogError(
                    "No migrator found for {Type} from version {From} to {To}",
                    typeof(T).Name, currentVersion, toVersion
                );
                break;
            }
            
            _logger.LogInformation(
                "Migrating {Type} from v{From} to v{To}",
                typeof(T).Name, migrator.SourceVersion, migrator.TargetVersion
            );
            
            current = await migrator.MigrateAsync(current, ct);
            currentVersion = migrator.TargetVersion;
        }
        
        return (T)current;
    }
}

// Example migrator
public class LevelDataMigrator_V1_To_V2 : IResourceMigrator
{
    public int SourceVersion => 1;
    public int TargetVersion => 2;
    public Type ResourceType => typeof(LevelData);
    
    public Task<object> MigrateAsync(object oldResource, CancellationToken ct = default)
    {
        var oldLevel = (LevelDataV1)oldResource;
        
        var newLevel = new LevelData
        {
            Version = 2,
            Name = oldLevel.Name,
            Width = oldLevel.Width,
            Height = oldLevel.Height,
            Tiles = oldLevel.Tiles,
            // New field in V2
            Metadata = new LevelMetadata
            {
                Author = "Unknown",
                CreatedDate = DateTime.UtcNow
            }
        };
        
        return Task.FromResult<object>(newLevel);
    }
}
```

## Usage Examples

### Example 1: Addressable Assets

```csharp
// Setup
var manifest = AddressableManifest.LoadFromFile("addressables.json");
var httpProvider = new HttpProvider(logger);
var addressableProvider = new AddressableProvider(logger, manifest, httpProvider);

var resourceManager = new ResourceManager(logger, new[] { addressableProvider });

// Usage - load via logical address
using var handle = resourceManager.GetHandle<Texture>("ui/button-primary");
var texture = await handle.GetAsync();

// Under the hood:
// 1. AddressableProvider resolves "ui/button-primary" to "https://cdn.example.com/ui/btn-primary.png"
// 2. HttpProvider fetches from CDN
// 3. Cached locally for next load
```

### Example 2: Cloud Storage with rclone

```csharp
// Setup rclone (one-time config)
// $ rclone config
// Configure "mygcs" remote for Google Cloud Storage

// Use in game
var rcloneProvider = new RcloneProvider(logger);
var resourceManager = new ResourceManager(logger, new[] { rcloneProvider });

using var handle = resourceManager.GetHandle<PlayerProfile>("gcs:my-bucket/profiles/player123.json");
var profile = await handle.GetAsync();
```

### Example 3: Persistent Cache for Fast Startup

```csharp
// Without persistent cache: Load 1000 JSON files = 5 seconds
// With persistent cache: Load from MemoryPack = 500ms

var fileProvider = new FileSystemProvider(logger, "data/");
var cachedProvider = new PersistentCacheProvider(logger, fileProvider);

var resourceManager = new ResourceManager(logger, new[] { cachedProvider });

// First load: slow (5s) - loads from disk + deserializes JSON
var items = await LoadItems();

// Subsequent loads: fast (500ms) - loads from MemoryPack cache
var items2 = await LoadItems();
```

### Example 4: Hot Reload During Development

```csharp
var hotReload = new HotReloadService(logger, resourceManager, new HotReloadOptions
{
    WatchDirectory = "data/",
    FileFilter = "*.json"
});

// Register resources to watch
hotReload.RegisterResource("data/levels/dungeon1.json", "levels/dungeon1");

// Subscribe to reload events
resourceManager.OnReloaded += resourceId =>
{
    Console.WriteLine($"Resource reloaded: {resourceId}");
    // Update game state
};

hotReload.Start();

// Now edit "data/levels/dungeon1.json" in editor
// Game automatically reloads the level!
```

### Example 5: Asset Migration

```csharp
var migrationService = new MigrationService(logger, new[]
{
    new LevelDataMigrator_V1_To_V2(),
    new LevelDataMigrator_V2_To_V3()
});

// Load old format
var oldLevel = LoadOldLevelFile("level.json"); // Version 1

// Migrate to current version (3)
var newLevel = await migrationService.MigrateAsync(oldLevel, fromVersion: 1, toVersion: 3);

// Save in new format
SaveLevel("level.json", newLevel);
```

## Implementation Plan

### Phase 1: HTTP + Persistent Cache (Week 1-2)

1. Implement `HttpProvider`
2. Implement `PersistentCacheProvider` with MemoryPack
3. Write unit tests
4. Integration tests with real CDN

**Deliverables**:
- ✅ HttpProvider class
- ✅ PersistentCacheProvider class
- ✅ 30+ unit tests

### Phase 2: Addressable System (Week 2-3)

1. Design addressable manifest format
2. Implement `AddressableProvider`
3. Create manifest builder tools
4. Write examples and docs

**Deliverables**:
- ✅ AddressableProvider class
- ✅ Manifest JSON schema
- ✅ CLI tool for building manifests
- ✅ 20+ unit tests

### Phase 3: Cloud Storage (Week 3-4)

1. Implement `RcloneProvider`
2. Test with GCS, S3, Azure
3. Document rclone setup
4. Write integration tests

**Deliverables**:
- ✅ RcloneProvider class
- ✅ Setup guide for each cloud
- ✅ 25+ integration tests

### Phase 4: Hot Reload (Week 4-5)

1. Implement `HotReloadService`
2. Add `OnReloading`/`OnReloaded` to ResourceManager
3. Create dev mode toggle
4. Write hot reload examples

**Deliverables**:
- ✅ HotReloadService class
- ✅ ResourceManager extensions
- ✅ Documentation
- ✅ 15+ tests

### Phase 5: Migration System (Week 5-6)

1. Design migrator interface
2. Implement `MigrationService`
3. Create example migrators
4. Write migration guide

**Deliverables**:
- ✅ IResourceMigrator interface
- ✅ MigrationService class
- ✅ Example migrators
- ✅ Migration guide

## Testing Strategy

### Unit Tests

- HTTP provider: Mock HttpClient responses
- rclone provider: Mock Process execution
- Addressable provider: Test manifest resolution
- Persistent cache: Test MemoryPack serialization
- Hot reload: Mock FileSystemWatcher events

### Integration Tests

- HTTP: Real CDN fetch + cache verification
- rclone: Real GCS/S3 access (requires credentials)
- Addressable: End-to-end logical → physical → load
- Hot reload: Real file edits + reload events

### Performance Tests

- HTTP cache hit rate: >95% for repeated loads
- Persistent cache speedup: >5x vs. raw JSON
- rclone cache: <100ms for cached items
- Hot reload latency: <500ms from file change to reload

## Dependencies

- **RFC-0054**: Reference counting foundation (REQUIRED)
- **HttpClient**: Built-in .NET HTTP client
- **MemoryPack**: Zero-allocation serialization
- **rclone**: External tool for cloud storage (optional)
- **FileSystemWatcher**: Built-in .NET file monitoring

## Security Considerations

1. **HTTP Provider**:
   - Validate HTTPS certificates
   - Sanitize cache filenames (prevent path traversal)
   - Implement rate limiting for CDN requests

2. **rclone Provider**:
   - Never log rclone credentials
   - Validate rclone command injection
   - Restrict accessible remotes via config

3. **Hot Reload**:
   - Disable in production builds (#if DEBUG)
   - Validate watched directories (prevent arbitrary file access)

4. **Addressable Manifest**:
   - Validate URLs before loading
   - Sign manifests to prevent tampering

## Documentation Updates

- [ ] Add provider comparison table to README
- [ ] Document addressable manifest format
- [ ] Write rclone setup guide for each cloud
- [ ] Add hot reload tutorial
- [ ] Document migration workflow

## Success Criteria

1. ✅ HTTP provider achieves >95% cache hit rate
2. ✅ Persistent cache provides >5x startup speedup
3. ✅ rclone provider works with GCS, S3, Azure
4. ✅ Addressable system supports 10,000+ entries
5. ✅ Hot reload latency <500ms
6. ✅ Zero breaking changes to existing code (RFC-0027, RFC-0054)

## Open Questions

1. **Should addressable manifests support wildcards?**
   - Example: `"textures/*" -> "bundles/textures.bundle#*"`
   - **Answer**: Phase 2 enhancement

2. **How to handle CDN failover?**
   - Multiple CDN URLs per addressable?
   - **Answer**: Add fallback URLs in Phase 2

3. **Should persistent cache validate against source file hash?**
   - Detect when source file changed?
   - **Answer**: Yes, add hash validation

## Related RFCs

- **RFC-0054**: Resource Reference Counting (REQUIRED - foundation)
- **RFC-0027**: Resource Service Console Implementation (compatible)
- **RFC-0039**: NuGet Package Resource Provider (compatible)
- **RFC-0017**: Reactive Plugin Architecture (R3 events)

## Conclusion

This RFC extends WingedBean's resource system with enterprise-grade features inspired by FlaxEngine while maintaining the flexible provider architecture. The additions enable cloud deployment, faster iteration via hot reload, and production-ready caching strategies.

**Key Benefits**:
- ✅ Cloud storage support (GCS, S3, Azure via rclone)
- ✅ CDN integration for asset delivery
- ✅ 5x+ faster startup with persistent cache
- ✅ Live resource editing via hot reload
- ✅ Addressable system for logical asset references
- ✅ Asset versioning and migration

**Recommendation**: **Accept** after RFC-0054 is implemented (dependency).

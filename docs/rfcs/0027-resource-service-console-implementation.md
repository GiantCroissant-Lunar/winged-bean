---
id: RFC-0027
title: Resource Service Console Implementation
status: Proposed
category: framework, console
created: 2025-01-10
updated: 2025-01-10
author: GitHub Copilot
---

# RFC-0027: Resource Service Console Implementation

## Status

**Proposed** - Ready for Implementation

## Date

2025-01-10

## Summary

Implement the missing Tier 3 Console plugin for `IResourceService` to complete the resource loading architecture. The `WingedBean.Contracts.Resource` (Tier 1) contract already exists but lacks a Console profile implementation. This RFC proposes a file system-based resource loader plugin (`WingedBean.Plugins.Resource`) that follows the existing plugin architecture patterns.

## Motivation

### Current State

The WingedBean framework has a complete Tier 1 contract for resource loading:

- ✅ **Tier 1**: `WingedBean.Contracts.Resource` exists in `framework/src/`
  - `IResourceService` interface
  - `ResourceMetadata` record
  - `ProxyService` (partial class for source generation)

- ❌ **Tier 3**: No Console implementation exists in `console/src/plugins/`
- ❌ **Tier 4**: No specialized providers (not needed for basic file system access)

### Architecture Gap

According to RFC-0004 (Project Organization), each service should have:

1. **Tier 1 Contract** (framework) - ✅ Exists
2. **Tier 3 Plugin** (profile-specific) - ❌ Missing for Console
3. **Tier 4 Provider** (technology-specific) - Not required for basic scenarios

**Other services already have Console implementations:**
- `WingedBean.Plugins.Config` - Configuration service (MS.Extensions.Configuration)
- `WingedBean.Plugins.TerminalUI` - Terminal UI service (Terminal.Gui)
- `WingedBean.Plugins.WebSocket` - WebSocket service (SuperSocket)
- `WingedBean.Plugins.ArchECS` - ECS service (Arch library)
- `WingedBean.Plugins.Audio` - Audio service (NAudio)

The Resource service is the only Tier 1 contract without a Console plugin implementation.

### Use Cases

Resource loading is essential for:

1. **Game Assets**: Load dungeon maps, item definitions, enemy stats
2. **Configuration Data**: Load JSON/YAML data files
3. **Text Resources**: Load dialog text, help content, descriptions
4. **Binary Resources**: Load images (for future ASCII art conversion)
5. **Mod Support**: Load user-created content from mod directories

### Why Now?

The dungeon crawler game (RFC-0007) needs resource loading:
- Dungeon map definitions (JSON/YAML)
- Item/enemy templates
- Dialog and narrative content
- Game balance data (stats, formulas)

Without `IResourceService`, these must be hardcoded or loaded manually.

## Proposal

### Architecture Overview

Following RFC-0002's 4-tier architecture and the existing plugin patterns:

```
┌─ Tier 1: Contracts (Framework) ────────────────────────────┐
│ WingedBean.Contracts.Resource/                             │
│ ├── IResourceService.cs         ✅ EXISTS                  │
│ ├── ResourceMetadata.cs         ✅ EXISTS                  │
│ └── ProxyService.cs              ✅ EXISTS (partial)       │
└────────────────────────────────────────────────────────────┘
                            ↓ implements
┌─ Tier 3: Plugin (Console Profile) ─────────────────────────┐
│ WingedBean.Plugins.Resource/                               │
│ ├── FileSystemResourceService.cs   ⬅ NEW                  │
│ ├── ResourcePluginActivator.cs     ⬅ NEW                  │
│ ├── ResourceCache.cs                ⬅ NEW                  │
│ ├── .plugin.json                    ⬅ NEW                  │
│ └── WingedBean.Plugins.Resource.csproj  ⬅ NEW             │
└────────────────────────────────────────────────────────────┘
```

### 1. FileSystemResourceService Implementation

**Purpose**: Load resources from the file system using path-based addressing.

```csharp
namespace WingedBean.Plugins.Resource;

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using WingedBean.Contracts.Resource;

/// <summary>
/// File system-based resource service for Console profile.
/// Loads resources from configured directories with caching support.
/// </summary>
public class FileSystemResourceService : IResourceService
{
    private readonly ILogger<FileSystemResourceService> _logger;
    private readonly ResourceCache _cache;
    private readonly string _basePath;
    private readonly JsonSerializerOptions _jsonOptions;

    public FileSystemResourceService(
        ILogger<FileSystemResourceService> logger,
        string? basePath = null)
    {
        _logger = logger;
        _cache = new ResourceCache();
        _basePath = basePath ?? Path.Combine(
            AppContext.BaseDirectory, 
            "resources"
        );
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            ReadCommentHandling = JsonCommentHandling.Skip,
            AllowTrailingCommas = true
        };

        EnsureBasePathExists();
    }

    /// <inheritdoc/>
    public async Task<TResource?> LoadAsync<TResource>(
        string resourceId, 
        CancellationToken cancellationToken = default)
        where TResource : class
    {
        if (_cache.TryGet<TResource>(resourceId, out var cached))
        {
            _logger.LogDebug("Resource '{ResourceId}' loaded from cache", resourceId);
            return cached;
        }

        var path = ResolveResourcePath(resourceId);
        if (!File.Exists(path))
        {
            _logger.LogWarning(
                "Resource '{ResourceId}' not found at path: {Path}", 
                resourceId, 
                path
            );
            return null;
        }

        try
        {
            var resource = await LoadResourceFromFileAsync<TResource>(
                path, 
                cancellationToken
            );

            if (resource != null)
            {
                _cache.Set(resourceId, resource);
                _logger.LogInformation(
                    "Resource '{ResourceId}' loaded successfully", 
                    resourceId
                );
            }

            return resource;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex, 
                "Failed to load resource '{ResourceId}' from {Path}", 
                resourceId, 
                path
            );
            return null;
        }
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<TResource>> LoadAllAsync<TResource>(
        string pattern, 
        CancellationToken cancellationToken = default)
        where TResource : class
    {
        var searchPath = ResolveResourcePath(pattern);
        var directory = Path.GetDirectoryName(searchPath) ?? _basePath;
        var searchPattern = Path.GetFileName(searchPath);

        if (!Directory.Exists(directory))
        {
            _logger.LogWarning(
                "Directory not found for pattern '{Pattern}': {Directory}", 
                pattern, 
                directory
            );
            return Enumerable.Empty<TResource>();
        }

        var files = Directory.GetFiles(directory, searchPattern, SearchOption.AllDirectories);
        var resources = new List<TResource>();

        foreach (var file in files)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var resourceId = GetResourceIdFromPath(file);
            var resource = await LoadAsync<TResource>(resourceId, cancellationToken);
            
            if (resource != null)
            {
                resources.Add(resource);
            }
        }

        _logger.LogInformation(
            "Loaded {Count} resources matching pattern '{Pattern}'", 
            resources.Count, 
            pattern
        );

        return resources;
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
        var path = ResolveResourcePath(resourceId);
        if (!File.Exists(path))
        {
            return null;
        }

        var fileInfo = new FileInfo(path);
        var extension = fileInfo.Extension.TrimStart('.');

        return new ResourceMetadata
        {
            Id = resourceId,
            Name = Path.GetFileNameWithoutExtension(fileInfo.Name),
            Type = DetermineResourceType(extension),
            Size = fileInfo.Length,
            Format = extension.ToUpperInvariant(),
            Properties = new Dictionary<string, object>
            {
                ["FullPath"] = fileInfo.FullName,
                ["LastModified"] = fileInfo.LastWriteTimeUtc,
                ["IsReadOnly"] = fileInfo.IsReadOnly
            }
        };
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

    // Private helper methods

    private void EnsureBasePathExists()
    {
        if (!Directory.Exists(_basePath))
        {
            Directory.CreateDirectory(_basePath);
            _logger.LogInformation(
                "Created resource base directory: {Path}", 
                _basePath
            );
        }
    }

    private string ResolveResourcePath(string resourceId)
    {
        // Handle absolute paths
        if (Path.IsPathRooted(resourceId))
        {
            return resourceId;
        }

        // Handle relative paths
        return Path.Combine(_basePath, resourceId);
    }

    private string GetResourceIdFromPath(string filePath)
    {
        // Convert absolute path back to resource ID
        if (filePath.StartsWith(_basePath))
        {
            return filePath.Substring(_basePath.Length).TrimStart(
                Path.DirectorySeparatorChar, 
                Path.AltDirectorySeparatorChar
            );
        }

        return filePath;
    }

    private async Task<TResource?> LoadResourceFromFileAsync<TResource>(
        string path, 
        CancellationToken cancellationToken)
        where TResource : class
    {
        var extension = Path.GetExtension(path).ToLowerInvariant();

        // Handle different resource types based on extension
        return extension switch
        {
            ".json" => await LoadJsonResourceAsync<TResource>(path, cancellationToken),
            ".txt" => await LoadTextResourceAsync<TResource>(path, cancellationToken),
            ".bin" => await LoadBinaryResourceAsync<TResource>(path, cancellationToken),
            _ => await LoadDefaultResourceAsync<TResource>(path, cancellationToken)
        };
    }

    private async Task<TResource?> LoadJsonResourceAsync<TResource>(
        string path, 
        CancellationToken cancellationToken)
        where TResource : class
    {
        await using var stream = File.OpenRead(path);
        
        // If TResource is string, return raw JSON
        if (typeof(TResource) == typeof(string))
        {
            using var reader = new StreamReader(stream);
            var content = await reader.ReadToEndAsync(cancellationToken);
            return content as TResource;
        }

        // Otherwise deserialize to TResource
        return await JsonSerializer.DeserializeAsync<TResource>(
            stream, 
            _jsonOptions, 
            cancellationToken
        );
    }

    private async Task<TResource?> LoadTextResourceAsync<TResource>(
        string path, 
        CancellationToken cancellationToken)
        where TResource : class
    {
        var content = await File.ReadAllTextAsync(path, cancellationToken);
        
        if (typeof(TResource) == typeof(string))
        {
            return content as TResource;
        }

        // Try to deserialize as JSON if TResource is not string
        try
        {
            return JsonSerializer.Deserialize<TResource>(content, _jsonOptions);
        }
        catch
        {
            return null;
        }
    }

    private async Task<TResource?> LoadBinaryResourceAsync<TResource>(
        string path, 
        CancellationToken cancellationToken)
        where TResource : class
    {
        var bytes = await File.ReadAllBytesAsync(path, cancellationToken);
        
        if (typeof(TResource) == typeof(byte[]))
        {
            return bytes as TResource;
        }

        return null;
    }

    private async Task<TResource?> LoadDefaultResourceAsync<TResource>(
        string path, 
        CancellationToken cancellationToken)
        where TResource : class
    {
        // Default: try as text first, then binary
        try
        {
            return await LoadTextResourceAsync<TResource>(path, cancellationToken);
        }
        catch
        {
            return await LoadBinaryResourceAsync<TResource>(path, cancellationToken);
        }
    }

    private static string DetermineResourceType(string extension)
    {
        return extension.ToLowerInvariant() switch
        {
            "json" => "data",
            "txt" => "text",
            "md" => "markdown",
            "yaml" or "yml" => "data",
            "png" or "jpg" or "jpeg" or "gif" or "bmp" => "image",
            "wav" or "mp3" or "ogg" => "audio",
            "bin" or "dat" => "binary",
            _ => "unknown"
        };
    }
}
```

### 2. ResourceCache Implementation

**Purpose**: In-memory cache to avoid redundant file I/O.

```csharp
namespace WingedBean.Plugins.Resource;

using System;
using System.Collections.Concurrent;

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

    private class CacheEntry
    {
        public required object Resource { get; init; }
        public required Type ResourceType { get; init; }
        public DateTime LastAccessed { get; set; }
    }
}
```

### 3. ResourcePluginActivator

**Purpose**: Plugin activator following the established pattern.

```csharp
namespace WingedBean.Plugins.Resource;

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using WingedBean.Contracts.Resource;
using WingedBean.PluginSystem;

/// <summary>
/// Plugin activator for the Resource service.
/// </summary>
public class ResourcePluginActivator : IPluginActivator
{
    public Task ActivateAsync(
        IServiceCollection services, 
        IServiceProvider hostServices, 
        CancellationToken cancellationToken = default)
    {
        // Register the file system resource service
        services.AddSingleton<IResourceService>(sp =>
        {
            var logger = sp.GetRequiredService<ILogger<FileSystemResourceService>>();
            
            // TODO: Read base path from configuration
            // For now, use default (AppContext.BaseDirectory/resources)
            return new FileSystemResourceService(logger);
        });

        return Task.CompletedTask;
    }

    public Task DeactivateAsync(CancellationToken cancellationToken = default)
    {
        // Cleanup if needed
        return Task.CompletedTask;
    }
}
```

### 4. Plugin Manifest (.plugin.json)

**Purpose**: Plugin metadata for discovery and loading.

```json
{
  "schemaVersion": "1.0",
  "id": "wingedbean.plugins.resource",
  "name": "Resource Service",
  "version": "1.0.0",
  "description": "File system-based resource loading service for Console profile",
  "author": "WingedBean Team",
  "category": "infrastructure",
  "priority": 100,
  "entryPoint": {
    "dotnet": "WingedBean.Plugins.Resource.dll"
  },
  "exports": {
    "services": [
      {
        "interface": "WingedBean.Contracts.Resource.IResourceService",
        "implementation": "WingedBean.Plugins.Resource.FileSystemResourceService",
        "lifecycle": "singleton",
        "priority": 100
      }
    ]
  },
  "dependencies": {
    "plugins": [],
    "packages": [
      {
        "id": "System.Text.Json",
        "version": "8.0.0"
      }
    ]
  },
  "configuration": {
    "basePath": "resources",
    "enableCaching": true,
    "supportedFormats": ["json", "txt", "yaml", "bin"]
  }
}
```

### 5. Project File

**Purpose**: Define project dependencies and build settings.

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <LangVersion>latest</LangVersion>
    <GenerateDocumentationFile>true</GenerateDocumentationFile>
  </PropertyGroup>

  <ItemGroup>
    <!-- Framework Contracts -->
    <ProjectReference Include="../../../../framework/src/WingedBean.Contracts.Resource/WingedBean.Contracts.Resource.csproj" />
    <ProjectReference Include="../../../../framework/src/WingedBean.PluginSystem/WingedBean.PluginSystem.csproj" />
  </ItemGroup>

  <ItemGroup>
    <!-- Microsoft Extensions -->
    <PackageReference Include="Microsoft.Extensions.DependencyInjection" Version="8.0.0" />
    <PackageReference Include="Microsoft.Extensions.Logging.Abstractions" Version="8.0.0" />
    
    <!-- JSON Support -->
    <PackageReference Include="System.Text.Json" Version="8.0.0" />
  </ItemGroup>

  <ItemGroup>
    <!-- Include plugin manifest in output -->
    <None Include=".plugin.json" CopyToOutputDirectory="Always" />
  </ItemGroup>

</Project>
```

## Implementation Plan

### Phase 1: Core Implementation (Week 1)

1. **Create project structure**
   ```bash
   mkdir -p development/dotnet/console/src/plugins/WingedBean.Plugins.Resource
   mkdir -p development/dotnet/console/tests/plugins/WingedBean.Plugins.Resource.Tests
   ```

2. **Implement core classes**
   - `FileSystemResourceService.cs`
   - `ResourceCache.cs`
   - `ResourcePluginActivator.cs`
   - `.plugin.json`
   - `WingedBean.Plugins.Resource.csproj`

3. **Add to Console.sln**
   ```bash
   cd development/dotnet/console
   dotnet sln add src/plugins/WingedBean.Plugins.Resource/WingedBean.Plugins.Resource.csproj
   ```

4. **Build and verify**
   ```bash
   dotnet build development/dotnet/console/Console.sln
   ```

### Phase 2: Testing (Week 1-2)

1. **Create test project**
   - `FileSystemResourceServiceTests.cs`
   - `ResourceCacheTests.cs`
   - Test fixtures with sample resources

2. **Test scenarios**
   - Load JSON resources
   - Load text resources
   - Load binary resources
   - Pattern matching (LoadAllAsync)
   - Cache hit/miss
   - Metadata retrieval
   - Preloading
   - Unload operations

3. **Integration tests**
   - Plugin activation
   - Registry integration
   - Host loading

### Phase 3: Integration with Host (Week 2)

1. **Update host to load Resource plugin**
   - Add to plugin discovery
   - Verify service registration in Registry

2. **Documentation**
   - Usage examples
   - Resource directory structure
   - Configuration options

3. **Sample resources**
   - Create `resources/` directory structure
   - Add sample JSON files for testing
   - Document resource naming conventions

## Resource Directory Structure

Proposed standard structure for Console profile:

```
<AppContext.BaseDirectory>/
└── resources/
    ├── data/               # JSON/YAML data files
    │   ├── dungeons/
    │   │   ├── level-01.json
    │   │   └── level-02.json
    │   ├── items/
    │   │   ├── weapons.json
    │   │   └── armor.json
    │   └── enemies/
    │       └── monsters.json
    ├── text/               # Text content
    │   ├── dialog/
    │   └── help/
    ├── config/             # Configuration files
    └── mods/               # User-created content (future)
```

## Configuration

The plugin will respect configuration from `IConfigService`:

```json
{
  "Plugins": {
    "Resource": {
      "BasePath": "resources",
      "EnableCaching": true,
      "MaxCacheSize": 1073741824,  // 1 GB in bytes
      "SupportedFormats": ["json", "txt", "yaml", "yml", "bin"],
      "SearchPaths": [
        "resources",
        "mods"
      ]
    }
  }
}
```

## Usage Examples

### Load a Single Resource

```csharp
// Get service from registry
var resourceService = registry.Get<IResourceService>();

// Load a dungeon map
var dungeon = await resourceService.LoadAsync<DungeonMap>("data/dungeons/level-01.json");

if (dungeon != null)
{
    Console.WriteLine($"Loaded dungeon: {dungeon.Name}");
}
```

### Load Multiple Resources

```csharp
// Load all item definitions
var items = await resourceService.LoadAllAsync<ItemDefinition>("data/items/*.json");

Console.WriteLine($"Loaded {items.Count()} items");
```

### Check Metadata

```csharp
var metadata = await resourceService.GetMetadataAsync("data/dungeons/level-01.json");

if (metadata != null)
{
    Console.WriteLine($"Resource: {metadata.Name}");
    Console.WriteLine($"Size: {metadata.Size} bytes");
    Console.WriteLine($"Format: {metadata.Format}");
}
```

### Preload Resources

```csharp
// Preload resources for faster access
var resourceIds = new[]
{
    "data/dungeons/level-01.json",
    "data/items/weapons.json",
    "data/enemies/monsters.json"
};

await resourceService.PreloadAsync(resourceIds);
```

## Benefits

### Architectural Consistency

1. **Completes Tier 1-3 mapping**: IResourceService now has a Console implementation
2. **Follows established patterns**: Uses IPluginActivator, .plugin.json, Registry
3. **Service-oriented**: Resource loading is a proper service, not ad-hoc code

### Developer Experience

1. **Easy to use**: Simple async API for loading resources
2. **Type-safe**: Generic methods with strong typing
3. **Performant**: Built-in caching reduces file I/O
4. **Flexible**: Supports JSON, text, binary resources

### Game Development

1. **Data-driven design**: Game content in JSON files, not hardcoded
2. **Hot-reload ready**: File system watch (future enhancement)
3. **Mod support**: Clear path for loading user content
4. **Testing friendly**: Easy to create test resources

## Future Enhancements

### Phase 4: Advanced Features (Future)

1. **Hot-reload support**
   - FileSystemWatcher integration
   - Notify dependents on resource changes

2. **Compression support**
   - Load resources from ZIP archives
   - Transparent decompression

3. **Remote resources**
   - HTTP/HTTPS resource loading
   - CDN support for downloadable content

4. **Resource validation**
   - JSON schema validation
   - Resource integrity checks

5. **Advanced caching**
   - LRU eviction policy
   - Memory pressure awareness
   - Disk cache for large resources

6. **Asset bundling**
   - Pack multiple resources into bundles
   - Streaming from bundles

## Testing Strategy

### Unit Tests

```csharp
[Fact]
public async Task LoadAsync_ValidJsonResource_ReturnsDeserialized()
{
    // Arrange
    var service = new FileSystemResourceService(logger, testResourcePath);
    
    // Act
    var result = await service.LoadAsync<TestData>("test.json");
    
    // Assert
    Assert.NotNull(result);
    Assert.Equal("expected-value", result.Property);
}

[Fact]
public async Task LoadAsync_CachedResource_UsesCache()
{
    // Arrange
    var service = new FileSystemResourceService(logger, testResourcePath);
    
    // Act
    var first = await service.LoadAsync<TestData>("test.json");
    var second = await service.LoadAsync<TestData>("test.json");
    
    // Assert
    Assert.Same(first, second); // Should be same instance from cache
}
```

### Integration Tests

```csharp
[Fact]
public async Task Plugin_LoadsAndRegistersService()
{
    // Arrange
    var host = new TestHost();
    var registry = host.Registry;
    var pluginLoader = host.PluginLoader;
    
    // Act
    await pluginLoader.LoadAsync("WingedBean.Plugins.Resource.dll");
    
    // Assert
    Assert.True(registry.IsRegistered<IResourceService>());
    
    var service = registry.Get<IResourceService>();
    Assert.NotNull(service);
    Assert.IsType<FileSystemResourceService>(service);
}
```

## Dependencies

### Project References

- `WingedBean.Contracts.Resource` (Tier 1)
- `WingedBean.PluginSystem` (Tier 2)
- `Microsoft.Extensions.DependencyInjection`
- `Microsoft.Extensions.Logging.Abstractions`
- `System.Text.Json`

### Related RFCs

- **RFC-0002**: 4-Tier Architecture (defines tier structure)
- **RFC-0003**: Plugin Architecture Foundation (plugin loading)
- **RFC-0004**: Project Organization (folder structure)
- **RFC-0007**: Arch ECS Integration (consumer of resource service)

## Definition of Done

- [ ] `WingedBean.Plugins.Resource` project created
- [ ] `FileSystemResourceService` implemented with all IResourceService methods
- [ ] `ResourceCache` implemented with thread-safe operations
- [ ] `ResourcePluginActivator` implemented
- [ ] `.plugin.json` manifest created
- [ ] Project added to `Console.sln`
- [ ] Unit tests created with >80% coverage
- [ ] Integration tests verify plugin loading
- [ ] Documentation updated with usage examples
- [ ] Sample resources directory created
- [ ] Host successfully loads and uses the plugin

## Risks and Mitigations

### Risk: Performance with Large Files

**Impact**: Loading large JSON files could block the thread

**Mitigation**: 
- Use async I/O throughout
- Consider streaming for very large files
- Document recommended resource sizes

### Risk: Cache Memory Usage

**Impact**: Caching many large resources could exhaust memory

**Mitigation**:
- Start with simple cache, add eviction policy in Phase 4
- Document memory considerations
- Provide configuration for cache limits

### Risk: File Path Security

**Impact**: Path traversal vulnerabilities if resource IDs not validated

**Mitigation**:
- Validate and sanitize resource IDs
- Restrict loading to configured base paths
- Document security considerations

## Alternatives Considered

### Alternative 1: Embed Resources in Assemblies

**Pros**: No file I/O, resources always available

**Cons**: 
- Harder to mod/customize
- Requires recompilation for content changes
- Larger assembly sizes

**Decision**: File system approach is more flexible for game development

### Alternative 2: Use Existing Asset Library (e.g., AssetBundle)

**Pros**: Battle-tested, feature-rich

**Cons**:
- Heavy dependency
- Unity-specific (AssetBundle)
- Overkill for simple scenarios

**Decision**: Custom implementation matches WingedBean philosophy of minimal dependencies

### Alternative 3: Unity Addressables Pattern

**Pros**: Industry-standard pattern, async-first

**Cons**:
- Complex to implement fully
- Requires content build pipeline
- Future Unity plugin should use real Addressables

**Decision**: Keep Console simple, Unity plugin can use Addressables later

## References

- [IResourceService Contract](../../development/dotnet/framework/src/WingedBean.Contracts.Resource/IResourceService.cs)
- [System.Text.Json Documentation](https://learn.microsoft.com/en-us/dotnet/standard/serialization/system-text-json/)
- [File I/O Best Practices](https://learn.microsoft.com/en-us/dotnet/standard/io/)
- [Plugin Architecture Adjustments](../implementation/PLUGIN-ARCHITECTURE-ADJUSTMENTS.md)

---

**Author**: GitHub Copilot (with Ray Wang guidance)
**Status**: Proposed - Ready for Review and Implementation

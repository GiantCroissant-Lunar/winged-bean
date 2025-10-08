---
id: RFC-0039
title: NuGet Package Resource Provider
status: Proposed
category: framework, console, infrastructure
created: 2025-01-07
updated: 2025-01-07
author: Ray Wang (with GitHub Copilot)
---

# RFC-0039: NuGet Package Resource Provider

## Status

**Proposed** - Ready for Review

## Date

2025-01-07

## Summary

Extend the Resource Service architecture to support loading NuGet packages as resources for Console profile, enabling dynamic runtime dependency management. This proposal introduces a Console-specific Tier 4 provider that uses `NuGet.Protocol` to download and load NuGet packages on-demand, while maintaining platform isolation for Unity and Godot profiles.

## Motivation

### Current Limitation

The existing `FileSystemResourceService` (RFC-0027) successfully loads static content files:

```csharp
// Load game data
var dungeon = await resourceService.LoadAsync<DungeonMap>("data/dungeons/level-01.json");
var items = await resourceService.LoadAllAsync<ItemDefinition>("data/items/*.json");
```

**However**, all .NET assembly dependencies must be:
- Pre-compiled into the application
- Bundled with distribution packages
- Updated through full application releases

This creates several problems:

1. **Large Distribution Size**: All dependencies bundled even if features unused
2. **Plugin Inflexibility**: Plugins can't declare dynamic dependencies
3. **Version Conflicts**: Multiple plugins can't use different versions of same package
4. **Mod Limitations**: Modders must redistribute DLLs with mods
5. **Feature Coupling**: Optional features force inclusion of heavy dependencies

### Proposed Solution

Enable runtime NuGet package loading through the Resource Service abstraction:

```csharp
// Load NuGet package as a resource
var package = await resourceService.LoadAsync<NuGetPackageResource>(
    "nuget:Newtonsoft.Json/13.0.3"
);

// Access package assemblies
var assembly = package.GetAssembly("Newtonsoft.Json.dll");

// Or load types directly
var jsonSerializer = package.LoadType<JsonSerializer>("Newtonsoft.Json.JsonSerializer");
```

### Key Use Cases

#### 1. Plugin Dependencies

Plugins declare NuGet package dependencies in manifests:

```json
{
  "id": "wingedbean.plugins.scripting",
  "version": "1.0.0",
  "dependencies": {
    "nuget": [
      {
        "packageId": "Microsoft.CodeAnalysis.CSharp.Scripting",
        "version": "4.8.0"
      }
    ]
  }
}
```

Host automatically loads packages before plugin activation.

#### 2. Optional Heavy Features

```csharp
// Only load ML.NET when AI features enabled (saves 50+ MB)
if (gameSettings.EnableAI)
{
    var mlPackage = await resourceService.LoadAsync<NuGetPackageResource>(
        "nuget:Microsoft.ML/3.0.0"
    );
    
    var aiService = new AIService(mlPackage.GetAssemblies());
}
```

#### 3. Dynamic Scripting/Modding

```csharp
// Load Roslyn compiler for runtime C# compilation
var roslynPackage = await resourceService.LoadAsync<NuGetPackageResource>(
    "nuget:Microsoft.CodeAnalysis.CSharp.Scripting/4.8.0"
);

var scriptEngine = new CSharpScriptEngine(roslynPackage);
var result = await scriptEngine.EvaluateAsync<int>(userScript);
```

#### 4. Mod Ecosystem

Mods declare professional-grade dependencies without redistributing DLLs:

```json
{
  "modId": "physics-overhaul",
  "version": "2.1.0",
  "dependencies": {
    "nuget": [
      {
        "packageId": "MathNet.Numerics",
        "version": "5.0.0",
        "reason": "Advanced physics calculations"
      },
      {
        "packageId": "BulletSharp",
        "version": "0.11.0",
        "reason": "Rigid body dynamics"
      }
    ]
  }
}
```

### Why This Matters

**Distribution Size Reduction:**
- Base console app: 10 MB (down from 100+ MB)
- Plugins: 5 MB each (without heavy dependencies)
- NuGet packages: Downloaded on-demand, shared via cache

**Version Isolation:**
- Plugin A uses Newtonsoft.Json 13.0.3
- Plugin B uses Newtonsoft.Json 12.0.3
- No conflicts via AssemblyLoadContext

**Easier Updates:**
- Clear package cache to force latest versions
- Selective dependency updates without full rebuild
- Faster iteration for mod developers

## Proposal

### Architecture Overview

Following RFC-0002 (4-Tier Architecture), this proposal adds a new Tier 4 provider:

```
┌─ Tier 1: Contracts (Cross-Platform) ────────────────────────┐
│ Plate.CrossMilo.Contracts.Resource/                         │
│ └── Services/IService.cs                    ✅ EXISTS       │
│     ├── LoadAsync<TResource>(resourceId)                    │
│     └── Supports URI scheme: "nuget:PackageId/Version"      │
└─────────────────────────────────────────────────────────────┘
                            ↓
┌─ Tier 3: Plugin (Console Profile) ──────────────────────────┐
│ WingedBean.Plugins.Resource/                ✅ EXISTS       │
│ ├── FileSystemResourceService.cs                            │
│ └── Providers/                              ⬅ EXTEND        │
│     ├── IResourceProvider.cs (new interface)                │
│     ├── FileSystemProvider.cs (extract from existing)       │
│     └── NuGetResourceProvider.cs (new)                      │
└─────────────────────────────────────────────────────────────┘
                            ↓
┌─ Tier 4: Provider (NuGet-Specific) ─────────────────────────┐
│ WingedBean.Plugins.Resource.NuGet/          ⬅ NEW PROJECT   │
│ ├── NuGetPackageResource.cs (resource type)                 │
│ ├── NuGetPackageLoader.cs (NuGet.Protocol wrapper)          │
│ ├── NuGetPackageCache.cs (local cache)                      │
│ └── Dependencies:                                            │
│     ├── NuGet.Protocol (6.8.0+)                              │
│     ├── NuGet.Packaging (6.8.0+)                             │
│     └── NuGet.Configuration (6.8.0+)                         │
└─────────────────────────────────────────────────────────────┘
```

### Design Rationale

**Q: Why separate Tier 4 project?**

A: Platform isolation and dependency management:

1. **Platform Isolation**: Unity/Godot implementations never reference NuGet libraries
2. **Dependency Isolation**: Heavy NuGet dependencies only in Console builds
3. **Clear Abstraction**: `IResourceService` contract remains platform-agnostic
4. **Testing**: Can mock NuGet provider for unit tests without real downloads

**Q: Why extend IResourceService instead of new IPackageService?**

A: Conceptual consistency:

1. **Resource Abstraction**: NuGet packages ARE resources (code resources vs data resources)
2. **Unified Interface**: Same LoadAsync pattern for all resource types
3. **Cache Sharing**: Leverage existing resource cache infrastructure
4. **Discovery**: Consistent resource ID scheme ("nuget:" prefix)

### Resource URI Scheme

Extend resource addressing to support NuGet packages:

```
# Existing file resources
"data/level.json"                    → File system
"config/settings.yaml"               → File system
"resources/image.png"                → File system or bundle

# New NuGet resources
"nuget:Newtonsoft.Json"              → Latest stable version
"nuget:Newtonsoft.Json/13.0.3"       → Specific version
"nuget:Newtonsoft.Json/[13.0,14.0)"  → Version range
"nuget:Newtonsoft.Json@MyFeed"       → Custom feed URL
"nuget:Newtonsoft.Json/13.0.3@https://my-feed.com/v3/index.json"
```

URI parsing logic:
```
nuget:<PackageId>[/<Version>][@<FeedUrl>]
```

### Key Components

#### 1. IResourceProvider Interface

New abstraction for pluggable resource loading strategies:

```csharp
namespace WingedBean.Plugins.Resource.Providers;

/// <summary>
/// Provider abstraction for resource loading strategies.
/// Enables pluggable support for different resource types.
/// </summary>
public interface IResourceProvider
{
    /// <summary>
    /// Check if this provider can handle the given resource URI.
    /// </summary>
    bool CanHandle(string resourceId);
    
    /// <summary>
    /// Load a resource asynchronously.
    /// </summary>
    Task<TResource?> LoadAsync<TResource>(
        string resourceId,
        CancellationToken cancellationToken = default)
        where TResource : class;
    
    /// <summary>
    /// Get resource metadata without loading full resource.
    /// </summary>
    Task<ResourceMetadata?> GetMetadataAsync(
        string resourceId,
        CancellationToken cancellationToken = default);
}
```

#### 2. NuGetPackageResource Class

Represents a loaded NuGet package:

```csharp
namespace WingedBean.Plugins.Resource.NuGet;

/// <summary>
/// Represents a loaded NuGet package as a resource.
/// </summary>
public class NuGetPackageResource
{
    public string PackageId { get; init; }
    public string Version { get; init; }
    public string InstallPath { get; init; }
    public PackageMetadata Metadata { get; init; }
    
    /// <summary>
    /// Get assemblies for the current target framework (.NET 8.0).
    /// </summary>
    public IEnumerable<Assembly> GetAssemblies();
    
    /// <summary>
    /// Get a specific assembly by name.
    /// </summary>
    public Assembly? GetAssembly(string assemblyName);
    
    /// <summary>
    /// Load a type from this package.
    /// </summary>
    /// <typeparam name="T">Expected type (for validation)</typeparam>
    /// <param name="typeName">Full type name</param>
    public T? LoadType<T>(string typeName) where T : class;
    
    /// <summary>
    /// Get dependencies of this package.
    /// </summary>
    public IEnumerable<PackageDependency> GetDependencies();
}
```

#### 3. NuGetResourceProvider Class

Provider implementation for NuGet packages:

```csharp
namespace WingedBean.Plugins.Resource.Providers;

/// <summary>
/// Resource provider for NuGet packages (Console profile only).
/// </summary>
public class NuGetResourceProvider : IResourceProvider
{
    private readonly NuGetPackageLoader _loader;
    private readonly NuGetPackageCache _cache;
    private readonly ILogger<NuGetResourceProvider> _logger;
    
    public NuGetResourceProvider(
        ILogger<NuGetResourceProvider> logger,
        NuGetConfiguration? config = null)
    {
        _logger = logger;
        _loader = new NuGetPackageLoader(logger, config);
        _cache = new NuGetPackageCache();
    }
    
    public bool CanHandle(string resourceId)
    {
        return resourceId.StartsWith("nuget:", StringComparison.OrdinalIgnoreCase);
    }
    
    public async Task<TResource?> LoadAsync<TResource>(
        string resourceId,
        CancellationToken cancellationToken = default)
        where TResource : class
    {
        var (packageId, version, feed) = ParseNuGetUri(resourceId);
        
        // Check cache
        if (_cache.TryGet(packageId, version, out var cached))
        {
            return ConvertResource<TResource>(cached);
        }
        
        // Download and load package
        var package = await _loader.LoadPackageAsync(
            packageId,
            version,
            feed,
            cancellationToken
        );
        
        _cache.Set(package);
        return ConvertResource<TResource>(package);
    }
}
```

#### 4. NuGetPackageLoader Class

Core NuGet integration using NuGet.Protocol:

```csharp
namespace WingedBean.Plugins.Resource.NuGet;

using global::NuGet.Common;
using global::NuGet.Packaging;
using global::NuGet.Protocol;
using global::NuGet.Protocol.Core.Types;

/// <summary>
/// Low-level NuGet package loader using NuGet.Protocol.
/// Handles package discovery, download, and extraction.
/// </summary>
public class NuGetPackageLoader
{
    private readonly ILogger _logger;
    private readonly SourceCacheContext _cacheContext;
    private readonly string _packagesDirectory;
    private readonly NuGetConfiguration _config;
    
    public NuGetPackageLoader(
        ILogger logger,
        NuGetConfiguration? config = null)
    {
        _logger = logger;
        _config = config ?? NuGetConfiguration.Default;
        _cacheContext = new SourceCacheContext();
        
        // Default: ~/.wingedbean/packages
        _packagesDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            ".wingedbean",
            "packages"
        );
        
        Directory.CreateDirectory(_packagesDirectory);
    }
    
    public async Task<NuGetPackageResource> LoadPackageAsync(
        string packageId,
        string? version,
        string? feedUrl,
        CancellationToken cancellationToken)
    {
        // 1. Create source repository
        var repository = Repository.Factory.GetCoreV3(
            feedUrl ?? "https://api.nuget.org/v3/index.json",
            FeedType.HttpV3
        );
        
        // 2. Find package metadata
        var metadataResource = await repository.GetResourceAsync<PackageMetadataResource>();
        var metadata = await metadataResource.GetMetadataAsync(
            packageId,
            includePrerelease: _config.IncludePrerelease,
            includeUnlisted: false,
            _cacheContext,
            NullLogger.Instance,
            cancellationToken
        );
        
        // 3. Resolve version (latest if not specified)
        var packageMetadata = version == null
            ? metadata.OrderByDescending(m => m.Identity.Version).FirstOrDefault()
            : metadata.FirstOrDefault(m => m.Identity.Version.ToString() == version);
        
        if (packageMetadata == null)
        {
            throw new PackageNotFoundException(
                $"Package '{packageId}' version '{version ?? "latest"}' not found"
            );
        }
        
        // 4. Download if needed
        var installPath = GetPackageInstallPath(packageMetadata.Identity);
        if (!Directory.Exists(installPath))
        {
            await DownloadPackageAsync(repository, packageMetadata.Identity, installPath, cancellationToken);
        }
        
        // 5. Load from disk
        return await LoadPackageFromDiskAsync(installPath, packageMetadata.Identity);
    }
    
    private async Task<NuGetPackageResource> LoadPackageFromDiskAsync(
        string installPath,
        PackageIdentity identity)
    {
        // Parse .nuspec for metadata
        var nuspecPath = Directory.GetFiles(installPath, "*.nuspec").FirstOrDefault();
        if (nuspecPath == null)
            throw new InvalidOperationException($"No .nuspec found in {installPath}");
        
        // Extract assemblies for target framework (.NET 8.0)
        var targetFramework = NuGetFramework.Parse("net8.0");
        var assemblies = LoadAssembliesForFramework(installPath, targetFramework);
        
        return new NuGetPackageResource
        {
            PackageId = identity.Id,
            Version = identity.Version.ToString(),
            InstallPath = installPath,
            Metadata = /* parsed from nuspec */,
            _assemblies = assemblies
        };
    }
}
```

### Integration with FileSystemResourceService

Refactor existing service to support provider pattern:

```csharp
public class FileSystemResourceService : IService
{
    private readonly List<IResourceProvider> _providers;
    private readonly ResourceCache _cache;
    
    public FileSystemResourceService(
        ILogger<FileSystemResourceService> logger,
        string? basePath = null,
        IEnumerable<IResourceProvider>? customProviders = null)
    {
        _cache = new ResourceCache();
        _providers = new List<IResourceProvider>();
        
        // Add custom providers first (higher priority)
        if (customProviders != null)
        {
            _providers.AddRange(customProviders);
        }
        
        // Add NuGet provider for Console profile
        #if !UNITY_2021_1_OR_NEWER && !GODOT
        _providers.Add(new NuGetResourceProvider(logger));
        #endif
        
        // Add default file system provider (lowest priority)
        _providers.Add(new FileSystemProvider(basePath, logger));
    }
    
    public async Task<TResource?> LoadAsync<TResource>(
        string resourceId,
        CancellationToken cancellationToken = default)
        where TResource : class
    {
        // Check cache
        if (_cache.TryGet<TResource>(resourceId, out var cached))
            return cached;
        
        // Try each provider in order
        foreach (var provider in _providers)
        {
            if (provider.CanHandle(resourceId))
            {
                var resource = await provider.LoadAsync<TResource>(resourceId, cancellationToken);
                if (resource != null)
                {
                    _cache.Set(resourceId, resource);
                    return resource;
                }
            }
        }
        
        return null;
    }
}
```

### Plugin Manifest Integration

Extend `.plugin.json` schema to support NuGet dependencies:

```json
{
  "schemaVersion": "1.0",
  "id": "wingedbean.plugins.scripting",
  "version": "1.0.0",
  "name": "C# Scripting Support",
  "description": "Runtime C# script compilation and execution",
  
  "dependencies": {
    "plugins": [],
    "nuget": [
      {
        "packageId": "Microsoft.CodeAnalysis.CSharp.Scripting",
        "version": "4.8.0",
        "optional": false,
        "reason": "Runtime C# compilation"
      },
      {
        "packageId": "Microsoft.Extensions.DependencyModel",
        "version": "8.0.0",
        "optional": false,
        "reason": "Assembly resolution"
      }
    ]
  }
}
```

Plugin loader automatically resolves NuGet dependencies:

```csharp
// In PluginLoaderHostedService
private async Task LoadPluginWithDependenciesAsync(PluginManifest manifest)
{
    // Load NuGet dependencies first
    if (manifest.Dependencies?.NuGet != null)
    {
        var resourceService = _registry.Get<IResourceService>();
        
        foreach (var nugetDep in manifest.Dependencies.NuGet)
        {
            _logger.LogInformation(
                "Loading NuGet dependency for plugin '{PluginId}': {PackageId} {Version}",
                manifest.Id,
                nugetDep.PackageId,
                nugetDep.Version
            );
            
            var package = await resourceService.LoadAsync<NuGetPackageResource>(
                $"nuget:{nugetDep.PackageId}/{nugetDep.Version}"
            );
            
            if (package == null && !nugetDep.Optional)
            {
                throw new PluginLoadException(
                    $"Required NuGet dependency not found: {nugetDep.PackageId}"
                );
            }
            
            // Assemblies are now available via AssemblyLoadContext
        }
    }
    
    // Load plugin DLL (dependencies already loaded)
    var plugin = await _pluginLoader.LoadAsync(manifest);
    return plugin;
}
```

## Benefits

### 1. Smaller Distribution Size

**Before:**
```
ConsoleDungeon.Host.exe
├── Core: 15 MB
├── Microsoft.ML: 50 MB (always included)
├── ImageSharp: 5 MB (always included)
├── MathNet.Numerics: 10 MB (always included)
└── Total: 80 MB
```

**After:**
```
ConsoleDungeon.Host.exe
├── Core: 15 MB
└── Total: 15 MB

First run with AI enabled:
└── Downloads Microsoft.ML: 50 MB to ~/.wingedbean/packages
```

### 2. Version Isolation

Different plugins can use different versions without conflicts:

```csharp
// Plugin A loads in its own AssemblyLoadContext
var packageA = await resourceService.LoadAsync<NuGetPackageResource>(
    "nuget:Newtonsoft.Json/13.0.3"
);

// Plugin B loads in separate AssemblyLoadContext
var packageB = await resourceService.LoadAsync<NuGetPackageResource>(
    "nuget:Newtonsoft.Json/12.0.3"
);

// No conflict! Different ALCs isolate versions
```

### 3. Easier Plugin Development

Plugin developers declare dependencies declaratively:

```json
{
  "dependencies": {
    "nuget": ["Polly", "Serilog", "FluentValidation"]
  }
}
```

No need to:
- Redistribute DLLs with plugin
- Worry about version conflicts
- Manually manage dependency resolution

### 4. Dynamic Feature Loading

Enable expensive features on-demand:

```csharp
public async Task EnableAdvancedPhysicsAsync()
{
    // Only load when user enables feature (saves 30 MB)
    var bulletSharp = await _resourceService.LoadAsync<NuGetPackageResource>(
        "nuget:BulletSharp/0.11.0"
    );
    
    _physicsEngine = new BulletPhysicsEngine(bulletSharp.GetAssemblies());
}
```

### 5. Mod Ecosystem Support

Mods can depend on professional libraries:

```json
{
  "modId": "advanced-ai",
  "dependencies": {
    "nuget": [
      {
        "packageId": "Accord.MachineLearning",
        "version": "3.8.0"
      }
    ]
  }
}
```

Modders don't redistribute libraries, just declare dependencies.

## Implementation Plan

### Phase 1: Foundation (Week 1)

**Deliverables:**
- [x] Create `WingedBean.Plugins.Resource.NuGet` project
- [x] Add NuGet.Protocol, NuGet.Packaging dependencies
- [x] Implement `NuGetPackageResource` class
- [x] Implement `NuGetPackageLoader` class
- [x] Implement `NuGetPackageCache` class
- [x] Unit tests with well-known packages (Newtonsoft.Json)

**Tasks:**
1. Create project structure
   ```bash
   mkdir -p console/src/plugins/WingedBean.Plugins.Resource.NuGet
   ```

2. Add project references
   ```xml
   <PackageReference Include="NuGet.Protocol" Version="6.8.0" />
   <PackageReference Include="NuGet.Packaging" Version="6.8.0" />
   <PackageReference Include="NuGet.Configuration" Version="6.8.0" />
   ```

3. Implement core classes

4. Write unit tests
   ```csharp
   [Fact]
   public async Task LoadPackageAsync_NewtonoftJson_LoadsSuccessfully()
   {
       var loader = new NuGetPackageLoader(logger);
       var package = await loader.LoadPackageAsync("Newtonsoft.Json", "13.0.3", null, CancellationToken.None);
       
       Assert.NotNull(package);
       Assert.Equal("Newtonsoft.Json", package.PackageId);
       Assert.Equal("13.0.3", package.Version);
       Assert.NotEmpty(package.GetAssemblies());
   }
   ```

### Phase 2: Provider Integration (Week 2)

**Deliverables:**
- [x] Create `IResourceProvider` interface
- [x] Implement `NuGetResourceProvider`
- [x] Refactor `FileSystemResourceService` to use providers
- [x] Configuration system for NuGet feeds
- [x] Integration tests

**Tasks:**
1. Define provider interface

2. Implement NuGet provider

3. Refactor existing service

4. Add configuration support
   ```json
   {
     "ResourceService": {
       "NuGet": {
         "DefaultFeed": "https://api.nuget.org/v3/index.json",
         "IncludePrerelease": false,
         "PackagesDirectory": "~/.wingedbean/packages",
         "RequireSignedPackages": true
       }
     }
   }
   ```

5. Integration tests
   ```csharp
   [Fact]
   public async Task ResourceService_NuGetUri_LoadsPackage()
   {
       var service = new FileSystemResourceService(logger);
       var package = await service.LoadAsync<NuGetPackageResource>(
           "nuget:Newtonsoft.Json/13.0.3"
       );
       
       Assert.NotNull(package);
   }
   ```

### Phase 3: Plugin Support (Week 3)

**Deliverables:**
- [x] Extend `.plugin.json` schema for NuGet dependencies
- [x] Update `PluginLoaderHostedService` to load NuGet deps
- [x] Handle dependency resolution order
- [x] Error handling for missing packages
- [x] Documentation

**Tasks:**
1. Extend manifest schema

2. Update plugin loader

3. Test with real plugin
   ```csharp
   // Create test plugin with NuGet dependency
   {
     "id": "test.plugin",
     "dependencies": {
       "nuget": [{"packageId": "Newtonsoft.Json", "version": "13.0.3"}]
     }
   }
   ```

4. Document usage patterns

### Phase 4: Polish & Security (Week 4)

**Deliverables:**
- [x] Package signature verification
- [x] Whitelist/blacklist support
- [x] Cache management commands
- [x] Progress reporting for downloads
- [x] Performance optimizations
- [x] Security audit

**Tasks:**
1. Implement security features
   ```csharp
   public class NuGetSecurityValidator
   {
       public async Task<ValidationResult> ValidatePackageAsync(PackageIdentity identity)
       {
           // Check signature
           // Check against whitelist
           // Verify hash
       }
   }
   ```

2. Add cache management
   ```bash
   # Clear all cached packages
   ConsoleDungeon.Host --clear-nuget-cache
   
   # Clear specific package
   ConsoleDungeon.Host --clear-nuget-package Newtonsoft.Json
   ```

3. Progress reporting
   ```csharp
   var progress = new Progress<DownloadProgress>(p =>
   {
       Console.WriteLine($"Downloading {p.PackageId}: {p.PercentComplete}%");
   });
   
   await loader.LoadPackageAsync(packageId, version, null, CancellationToken.None, progress);
   ```

## Security Considerations

### 1. Package Trust

**Risk:** Malicious packages contain harmful code

**Mitigations:**
- **Package Signing**: Require signed packages by default
  ```csharp
  config.RequireSignedPackages = true;
  ```
  
- **Whitelist**: Limit to trusted packages
  ```json
  {
    "NuGet": {
      "AllowedPackages": [
        "Microsoft.*",
        "Newtonsoft.Json",
        "Serilog"
      ]
    }
  }
  ```
  
- **Warnings**: Alert on unsigned/unknown packages
  ```
  ⚠️  WARNING: Package 'Unknown.Package' is not signed
  Continue? [y/N]:
  ```

### 2. Dependency Confusion

**Risk:** Attacker publishes malicious package with same name as internal package

**Mitigations:**
- **Feed Priority**: Internal feeds first
  ```json
  {
    "NuGet": {
      "Feeds": [
        {
          "name": "internal",
          "url": "https://internal-feed.company.com",
          "priority": 1
        },
        {
          "name": "nuget.org",
          "url": "https://api.nuget.org/v3/index.json",
          "priority": 10
        }
      ]
    }
  }
  ```
  
- **Package Prefixes**: Namespace internal packages
  ```
  Company.InternalLib.* → only from internal feed
  ```

### 3. Version Pinning

**Risk:** Automatic updates pull malicious version

**Mitigations:**
- **Lock Files**: Pin versions in production
  ```json
  // nuget.lock.json
  {
    "Newtonsoft.Json": "13.0.3",  // Exact version
    "Serilog": "3.1.1"
  }
  ```
  
- **Hash Verification**: Verify package integrity
  ```csharp
  if (package.ComputedHash != manifest.ExpectedHash)
  {
      throw new SecurityException("Package hash mismatch");
  }
  ```

### 4. HTTPS Enforcement

**Risk:** Man-in-the-middle package replacement

**Mitigations:**
- **HTTPS Only**: Reject HTTP feeds
  ```csharp
  if (!feedUrl.StartsWith("https://"))
      throw new SecurityException("Only HTTPS feeds allowed");
  ```
  
- **Certificate Validation**: Verify SSL certificates

### 5. Sandboxing

**Risk:** Malicious package code executes with full privileges

**Mitigations:**
- **AssemblyLoadContext**: Isolate package assemblies
- **Limited API Surface**: Don't expose dangerous APIs to loaded code
- **User Confirmation**: Prompt for permission on first load

## Performance Considerations

### 1. Download Time

**Issue:** First load of large packages can be slow (50+ MB for ML.NET)

**Optimizations:**
- **Background Preloading**: Download packages on app start
  ```csharp
  // During startup, before main menu
  _ = Task.Run(() => PreloadCommonPackagesAsync());
  ```
  
- **Progress Reporting**: Show download status
  ```
  Downloading Microsoft.ML (52.3 MB)... 65%
  ```
  
- **Parallel Downloads**: Download multiple packages concurrently
  ```csharp
  await Task.WhenAll(
      LoadPackageAsync("Package1"),
      LoadPackageAsync("Package2"),
      LoadPackageAsync("Package3")
  );
  ```
  
- **Resume Support**: Resume interrupted downloads

### 2. Disk Space

**Issue:** Package cache grows large (100s of MB)

**Optimizations:**
- **LRU Eviction**: Remove least-recently-used packages
  ```csharp
  if (cacheSize > maxSize)
  {
      RemoveLeastRecentlyUsed();
  }
  ```
  
- **Configurable Limits**: 
  ```json
  {
    "NuGet": {
      "MaxCacheSizeBytes": 1073741824  // 1 GB
    }
  }
  ```
  
- **Shared Cache**: Use global NuGet cache (`~/.nuget/packages`)
  ```csharp
  // Leverage existing NuGet infrastructure
  var globalCache = Path.Combine(
      Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
      ".nuget",
      "packages"
  );
  ```

### 3. Assembly Loading

**Issue:** Loading assemblies from packages adds startup time

**Optimizations:**
- **Lazy Loading**: Load on first use, not during discovery
  ```csharp
  public class LazyNuGetPackageResource
  {
      private List<Assembly>? _assemblies;
      
      public IEnumerable<Assembly> GetAssemblies()
      {
          if (_assemblies == null)
          {
              _assemblies = LoadAssembliesFromDisk();
          }
          return _assemblies;
      }
  }
  ```
  
- **Assembly Caching**: Keep frequently-used assemblies loaded
  
- **Preload Critical Packages**: Load essential packages during splash screen

### 4. Network Efficiency

**Issue:** Redundant API calls to NuGet feed

**Optimizations:**
- **Local Metadata Cache**: Cache package metadata
  ```json
  // ~/.wingedbean/nuget-metadata.json
  {
    "Newtonsoft.Json": {
      "latestVersion": "13.0.3",
      "lastChecked": "2025-01-07T10:00:00Z"
    }
  }
  ```
  
- **HTTP Caching**: Respect HTTP cache headers
  
- **Batch Metadata**: Fetch multiple package metadata in one request

## Testing Strategy

### Unit Tests

```csharp
namespace WingedBean.Plugins.Resource.NuGet.Tests;

public class NuGetPackageLoaderTests
{
    [Fact]
    public async Task LoadPackageAsync_ValidPackage_LoadsSuccessfully()
    {
        // Arrange
        var loader = new NuGetPackageLoader(logger);
        
        // Act
        var package = await loader.LoadPackageAsync(
            "Newtonsoft.Json",
            "13.0.3",
            null,
            CancellationToken.None
        );
        
        // Assert
        Assert.NotNull(package);
        Assert.Equal("Newtonsoft.Json", package.PackageId);
        Assert.Equal("13.0.3", package.Version);
        
        var assembly = package.GetAssembly("Newtonsoft.Json.dll");
        Assert.NotNull(assembly);
    }
    
    [Fact]
    public async Task LoadPackageAsync_InvalidPackage_ThrowsException()
    {
        var loader = new NuGetPackageLoader(logger);
        
        await Assert.ThrowsAsync<PackageNotFoundException>(() =>
            loader.LoadPackageAsync("NonExistent.Package", "1.0.0", null, CancellationToken.None)
        );
    }
    
    [Fact]
    public async Task LoadPackageAsync_CachedPackage_UsesCache()
    {
        var loader = new NuGetPackageLoader(logger);
        
        // Load twice
        var package1 = await loader.LoadPackageAsync("Newtonsoft.Json", "13.0.3", null, CancellationToken.None);
        var package2 = await loader.LoadPackageAsync("Newtonsoft.Json", "13.0.3", null, CancellationToken.None);
        
        // Second load should be instant (from cache)
        Assert.Equal(package1.InstallPath, package2.InstallPath);
    }
}
```

### Integration Tests

```csharp
namespace WingedBean.Plugins.Resource.Tests;

public class NuGetResourceProviderIntegrationTests
{
    [Fact]
    public async Task ResourceService_LoadNuGetPackage_WorksEndToEnd()
    {
        // Arrange
        var resourceService = new FileSystemResourceService(logger);
        
        // Act
        var package = await resourceService.LoadAsync<NuGetPackageResource>(
            "nuget:Newtonsoft.Json/13.0.3"
        );
        
        // Assert
        Assert.NotNull(package);
        
        var jsonType = package.LoadType<object>("Newtonsoft.Json.JsonConvert");
        Assert.NotNull(jsonType);
    }
    
    [Fact]
    public async Task PluginLoader_WithNuGetDependencies_LoadsSuccessfully()
    {
        // Arrange: Create test plugin with NuGet dependency
        var manifest = new PluginManifest
        {
            Id = "test.plugin",
            Dependencies = new
            {
                NuGet = new[]
                {
                    new { PackageId = "Newtonsoft.Json", Version = "13.0.3" }
                }
            }
        };
        
        // Act
        var plugin = await pluginLoader.LoadAsync(manifest);
        
        // Assert
        Assert.NotNull(plugin);
        
        // Verify Newtonsoft.Json is available to plugin
        var jsonAssembly = AppDomain.CurrentDomain.GetAssemblies()
            .FirstOrDefault(a => a.GetName().Name == "Newtonsoft.Json");
        Assert.NotNull(jsonAssembly);
    }
}
```

### Performance Tests

```csharp
public class NuGetPerformanceTests
{
    [Fact]
    public async Task LoadPackage_LargePackage_CompletesInReasonableTime()
    {
        var loader = new NuGetPackageLoader(logger);
        var stopwatch = Stopwatch.StartNew();
        
        // ML.NET is ~50 MB
        var package = await loader.LoadPackageAsync("Microsoft.ML", "3.0.0", null, CancellationToken.None);
        
        stopwatch.Stop();
        
        // Should complete within 60 seconds on typical connection
        Assert.True(stopwatch.Elapsed < TimeSpan.FromSeconds(60));
    }
}
```

## Dependencies

### NuGet Packages

```xml
<PackageReference Include="NuGet.Protocol" Version="6.8.0" />
<PackageReference Include="NuGet.Packaging" Version="6.8.0" />
<PackageReference Include="NuGet.Configuration" Version="6.8.0" />
<PackageReference Include="NuGet.Frameworks" Version="6.8.0" />
<PackageReference Include="NuGet.Versioning" Version="6.8.0" />
```

### Project References

```xml
<!-- Tier 3: Resource plugin -->
<ProjectReference Include="../WingedBean.Plugins.Resource/WingedBean.Plugins.Resource.csproj" />

<!-- Tier 1: Contracts -->
<ProjectReference Include="../../../../../../plate-projects/cross-milo/dotnet/framework/src/CrossMilo.Contracts.Resource/CrossMilo.Contracts.Resource.csproj" />
```

## Related RFCs

- **RFC-0002**: 4-Tier Architecture - Defines tier structure
- **RFC-0003**: Plugin Architecture Foundation - Plugin loading system
- **RFC-0027**: Resource Service Console Implementation - Base resource service
- **RFC-0037**: Shared Contract Loading Strategy - Plugin contract isolation
- **RFC-0038**: Source Generator Plugin System - Service registration

## Alternatives Considered

### Alternative 1: Separate Package Manager Tool

**Approach:** Standalone CLI tool for package management

```bash
wingedbean-pkg install Microsoft.ML 3.0.0
wingedbean-pkg list
wingedbean-pkg uninstall Microsoft.ML
```

**Pros:**
- Clear separation of concerns
- Standard package manager patterns
- Easier to understand

**Cons:**
- Extra step for users (not seamless)
- Duplication with NuGet tooling
- Not integrated with resource system

**Decision:** Rejected - Want seamless runtime integration

### Alternative 2: Bundle All Dependencies

**Approach:** Pre-compile all possible dependencies

**Pros:**
- Simple, no runtime complexity
- Works offline
- Predictable

**Cons:**
- 100+ MB distributions
- Version conflicts inevitable
- Can't support mods easily

**Decision:** Rejected - Too limiting for plugin ecosystem

### Alternative 3: Use Paket

**Approach:** Use Paket instead of NuGet.Protocol

**Pros:**
- Battle-tested
- Better dependency resolution
- Supports multiple sources

**Cons:**
- Extra dependency
- More complex
- Not official Microsoft tooling

**Decision:** Rejected - NuGet.Protocol is official and sufficient

### Alternative 4: MEF (Managed Extensibility Framework)

**Approach:** Use MEF for plugin composition

**Pros:**
- Built into .NET
- Well-documented
- Proven pattern

**Cons:**
- Doesn't handle NuGet packages
- More restrictive than current system
- Migration effort

**Decision:** Rejected - Doesn't solve package management problem

## Risks and Mitigations

| Risk | Likelihood | Impact | Mitigation |
|------|-----------|---------|-----------|
| Network dependency | HIGH | MEDIUM | Cache packages, offline mode |
| Malicious packages | MEDIUM | CRITICAL | Signature verification, whitelisting |
| Version conflicts | LOW | MEDIUM | AssemblyLoadContext isolation |
| Large cache size | MEDIUM | LOW | LRU eviction, size limits |
| Slow downloads | HIGH | LOW | Background loading, progress UI |
| Breaking API changes | LOW | MEDIUM | Pin versions in production |
| NuGet.org downtime | LOW | HIGH | Multiple feed support, local mirror |

## Definition of Done

- [ ] `WingedBean.Plugins.Resource.NuGet` project created
- [ ] `NuGetPackageResource`, `NuGetPackageLoader`, `NuGetPackageCache` implemented
- [ ] `IResourceProvider` interface defined
- [ ] `NuGetResourceProvider` implemented
- [ ] `FileSystemResourceService` refactored to use provider pattern
- [ ] `.plugin.json` schema extended for NuGet dependencies
- [ ] `PluginLoaderHostedService` loads NuGet dependencies
- [ ] Configuration system for NuGet feeds
- [ ] Unit tests with >80% coverage
- [ ] Integration tests verify end-to-end loading
- [ ] Performance tests for large packages
- [ ] Security features (signature verification, whitelist)
- [ ] Cache management commands
- [ ] Documentation complete:
  - [ ] Usage guide
  - [ ] Plugin developer guide
  - [ ] Security best practices
  - [ ] Troubleshooting guide
- [ ] Example plugin with NuGet dependencies
- [ ] Console app successfully loads NuGet package at runtime

## Success Metrics

### Quantitative

- Distribution size reduced by 50% or more
- Plugin installation time < 2 seconds (with cached packages)
- Large package (50 MB) download < 60 seconds on typical connection
- Cache hit rate > 80% for common packages
- Zero version conflicts reported

### Qualitative

- Plugin developers report easier dependency management
- Modders successfully use professional libraries
- No security incidents related to malicious packages
- Positive feedback on performance (no noticeable delays)

## Future Enhancements

### Phase 5: Advanced Features (Post-1.0)

1. **Custom Package Repositories**
   - Support private feeds
   - GitHub Packages integration
   - Azure Artifacts integration

2. **Package Bundling**
   - Bundle multiple packages for offline distribution
   - "Essential packages" bundle for first run

3. **Dependency Graph Visualization**
   ```
   ConsoleDungeon.Host
   ├── Scripting Plugin
   │   ├── Microsoft.CodeAnalysis.CSharp (4.8.0)
   │   └── Microsoft.Extensions.DependencyModel (8.0.0)
   └── AI Plugin
       └── Microsoft.ML (3.0.0)
           └── System.Numerics.Tensors (8.0.0)
   ```

4. **Hot-Reload Support**
   - Reload NuGet packages without restart
   - Update plugins with new package versions

5. **Analytics**
   - Track package usage
   - Recommend packages based on features
   - Report popular packages

6. **Compression**
   - Compress cached packages
   - Delta updates for package versions

## Conclusion

This RFC proposes extending the Resource Service architecture to support NuGet package loading as a Console-specific feature. By maintaining strict tier separation and using the provider pattern, we enable powerful runtime dependency management without compromising platform portability.

**Key Benefits:**
- 50%+ reduction in distribution size
- Dynamic feature loading
- Flexible plugin dependencies
- Better mod ecosystem support
- Consistent resource abstraction

**Next Steps:**
1. Review and discuss RFC with team
2. Gather feedback on security considerations
3. Approve RFC
4. Begin Phase 1 implementation
5. Iterate based on testing feedback

---

**Author:** Ray Wang (with GitHub Copilot assistance)  
**Status:** Proposed - Ready for Review  
**Date:** 2025-01-07

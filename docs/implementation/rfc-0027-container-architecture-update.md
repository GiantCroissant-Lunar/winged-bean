# RFC-0027 Container Architecture Update

**Date**: 2025-01-10
**Status**: Complete ✅
**Related**: [RFC-0027](../rfcs/0027-resource-service-console-implementation.md)

---

## Overview

Updated the WingedBean.Plugins.Resource implementation to prioritize **container-based (bundle) loading** following industry standards like NuGet packages, JAR files, Unity AssetBundles, and Addressables.

## Rationale

Container-based resource loading provides several advantages over individual file loading:

### Performance Benefits
- **Reduced I/O Operations**: Load one bundle file instead of hundreds of individual files
- **Better Compression**: ZIP compression reduces disk space and transfer time
- **Faster Startup**: Bundle manifest provides instant resource catalog
- **Efficient Caching**: Load entire bundle once, access resources instantly

### Organization Benefits
- **Atomic Updates**: Replace single bundle file instead of many individual files
- **Versioning**: Bundle-level versioning tracks resource sets
- **Dependencies**: Manifest declares resource dependencies
- **Metadata Rich**: Tags, types, properties embedded in manifest

### Distribution Benefits
- **Single File Distribution**: Easier to download and deploy
- **Mod Support**: Users can add/remove bundles without touching core files
- **CDN Friendly**: Single file downloads are more cache-efficient
- **Integrity Verification**: Bundle-level checksums ensure completeness

---

## Implementation Changes

### New Files Created

1. **ResourceBundleManifest.cs** - Bundle metadata structure
   - Bundle ID, version, name, description, author
   - Resource entries with metadata (ID, path, type, format, size, tags, dependencies)
   - Additional properties dictionary for extensibility

2. **ResourceBundle.cs** - Bundle accessor class
   - Wraps ZIP archive with manifest-driven access
   - Lazy loading of resources from bundle
   - Thread-safe bundle operations
   - Automatic format detection (JSON, text, binary)

3. **ResourceBundleBuilder.cs** - Bundle creation tool
   - Fluent API for building bundles
   - Add individual files or entire directories
   - Automatic metadata generation
   - Creates properly structured .wbundle files

### Updated Files

4. **FileSystemResourceService.cs** - Core service with bundle priority
   - Discovers `.wbundle` files on initialization
   - Prioritizes bundle loading over individual files
   - Falls back to file system if resource not in bundles
   - Maintains backward compatibility

5. **README.md** - Updated documentation
   - Bundle-first approach documented
   - Bundle format specification
   - Builder usage examples
   - Migration guide

---

## Bundle Format Specification

### File Extension
`.wbundle` - WingedBean Bundle (ZIP archive)

### Structure
```
game-data.wbundle (ZIP)
├── manifest.json              # Bundle manifest (required)
└── resources/                 # Resource files
    ├── data/
    │   ├── dungeons/
    │   │   ├── level-01.json
    │   │   └── level-02.json
    │   └── items/
    │       ├── weapons.json
    │       └── armor.json
    ├── text/
    │   └── help.txt
    └── audio/
        └── music.mp3
```

### Manifest Format
```json
{
  "id": "game-data",
  "version": "1.0.0",
  "name": "Game Data Bundle",
  "description": "Core game resources",
  "author": "WingedBean Team",
  "createdAt": "2025-01-10T12:00:00Z",
  "resources": [
    {
      "id": "dungeons/level-01",
      "path": "resources/data/dungeons/level-01.json",
      "type": "data",
      "format": "JSON",
      "size": 2048,
      "tags": ["dungeon", "level", "gameplay"],
      "dependencies": ["items/sword"],
      "properties": {
        "difficulty": "normal",
        "requiredLevel": 1
      }
    }
  ],
  "metadata": {
    "category": "game-content",
    "license": "MIT"
  }
}
```

---

## Usage Examples

### Creating a Bundle

```csharp
// Build a game data bundle
var builder = new ResourceBundleBuilder("game-data", "1.0.0")
    .WithMetadata(
        name: "Game Data Bundle",
        description: "Core game data resources",
        author: "WingedBean Team"
    );

// Add individual resources
builder.AddResource(
    "data/dungeons/level-01.json",
    "dungeons/level-01",
    type: "data",
    tags: new[] { "dungeon", "level" }
);

// Add entire directories
builder.AddDirectory(
    "data/items",
    resourcePrefix: "items",
    recursive: true,
    filePatterns: new[] { "*.json" }
);

// Build the bundle
await builder.BuildAsync("resources/game-data.wbundle");
```

### Loading from Bundles

```csharp
var resourceService = registry.Get<IResourceService>();

// Service automatically discovers .wbundle files in resources/ directory
// Resources are loaded from bundles first, then fallback to individual files

// Load a resource (checks bundles first)
var dungeon = await resourceService.LoadAsync<DungeonMap>("dungeons/level-01");

// Load multiple resources
var items = await resourceService.LoadAllAsync<ItemDef>("items/*");

// Fallback to individual file if not in bundle
var config = await resourceService.LoadAsync<Settings>("config/settings.json");
```

---

## Priority System

The service uses the following priority order:

1. **Check Cache**: If resource already loaded, return from cache
2. **Check Bundles**: Search all loaded bundles for resource ID
3. **Check Files**: Fall back to individual file loading
4. **Return Null**: Resource not found

This ensures maximum performance (cache first) while maintaining flexibility (file fallback).

---

## Bundle Discovery

Bundles are automatically discovered during service initialization:

1. Scan `resources/` directory recursively for `*.wbundle` files
2. Load and parse each bundle's `manifest.json`
3. Register bundle in internal dictionary by bundle ID
4. Log discovery results

Bundles can be in subdirectories:
```
resources/
├── core.wbundle              # Core game data
├── expansions/
│   ├── dlc1.wbundle         # DLC content
│   └── dlc2.wbundle
└── mods/
    └── user-mod.wbundle      # User-created content
```

---

## Migration Guide

### From Individual Files to Bundles

**Before (individual files):**
```
resources/
├── data/
│   ├── dungeons/level-01.json
│   ├── items/sword.json
│   └── enemies/goblin.json
└── text/
    └── help.txt
```

**After (bundled):**
```
resources/
└── game-data.wbundle         # Contains all above files
```

**Migration Steps:**

1. Create a bundle builder
2. Add all resource files
3. Build bundle
4. Test with resource service
5. Replace individual files with bundle
6. Delete individual files (optional - keeps working as fallback)

**No Code Changes Required**: Resource IDs remain the same, service handles bundle loading transparently.

---

## Test Results

### New Tests (5 tests)
- ✅ `BuildBundle_WithResources_CreatesValidBundle`
- ✅ `BuildBundle_WithDirectory_AddsAllFiles`
- ✅ `FileSystemResourceService_LoadsFromBundle`
- ✅ `FileSystemResourceService_FallsBackToFile_WhenNotInBundle`
- ✅ `FileSystemResourceService_PrioritizesBundleOverFile`

### Total Test Suite
- **Total**: 35 tests
- **Passed**: 35 ✅
- **Failed**: 0
- **Duration**: ~200ms

---

## Performance Considerations

### Bundle Loading Overhead

- **One-time Cost**: Bundle discovery and manifest parsing on startup
- **Minimal**: ZIP archive opening is fast
- **Lazy**: Resources loaded on-demand, not all at once
- **Amortized**: Cost spread across application lifetime

### Memory Footprint

- **Manifest Only**: Bundles keep only manifest in memory
- **Resources On-Demand**: ZIP entries read when requested
- **Cache Benefits**: Loaded resources cached as before
- **Disposal**: Bundles properly disposed when service disposed

### Recommended Bundle Sizes

- **Small Bundles**: < 10 MB - Fast loading, easy updates
- **Medium Bundles**: 10-50 MB - Good balance
- **Large Bundles**: 50-100 MB - Consider splitting
- **Very Large**: > 100 MB - Split by category or level

---

## Comparison with Industry Standards

### NuGet Packages
- ✅ ZIP-based container
- ✅ Manifest with metadata
- ✅ Versioning support
- ⚠️ NuGet uses `.nuspec` XML, we use JSON

### Unity AssetBundles
- ✅ Container-based loading
- ✅ Resource catalog/manifest
- ✅ Dependencies tracking
- ⚠️ AssetBundles are binary format, we use ZIP

### JAR Files
- ✅ ZIP-based container
- ✅ Manifest file (`META-INF/MANIFEST.MF`)
- ⚠️ JAR uses text manifest, we use JSON

### Our Approach
- **Standard ZIP**: Cross-platform, tool-friendly
- **JSON Manifest**: Developer-friendly, extensible
- **Human-Readable**: Can open and inspect with any ZIP tool
- **Simple**: No custom binary formats or complex tools required

---

## Future Enhancements

### Phase 2: Enhanced Bundle Features

1. **Bundle Streaming**
   - Load bundles from HTTP/HTTPS URLs
   - Progressive loading for large bundles
   - CDN integration

2. **Bundle Encryption**
   - Encrypted bundle support
   - Asset protection for commercial games
   - License key validation

3. **Bundle Compression Levels**
   - Configurable compression (fast vs. small)
   - No compression for already-compressed assets
   - Store-only mode for debugging

4. **Bundle Dependencies**
   - Bundle-level dependencies (bundle A requires bundle B)
   - Automatic dependency resolution
   - Version compatibility checking

5. **Bundle Hot-Reload**
   - Watch for bundle file changes
   - Reload bundles without restart
   - Invalidate affected caches

6. **Bundle Verification**
   - SHA256 checksums in manifest
   - Signature verification
   - Tamper detection

---

## Benefits Summary

### For Developers
- ✅ Simpler distribution (one file vs. many)
- ✅ Easier versioning and updates
- ✅ Better organization of related resources
- ✅ Tool-friendly (standard ZIP format)

### For Users/Modders
- ✅ Easy mod installation (drop bundle in folder)
- ✅ Clean uninstall (remove bundle file)
- ✅ Clear versioning (bundle version in filename)
- ✅ No file conflicts (resources namespaced by bundle)

### For Performance
- ✅ Reduced I/O operations
- ✅ Better compression ratios
- ✅ Faster startup times
- ✅ Efficient caching

### For Compatibility
- ✅ Backward compatible (files still work)
- ✅ Gradual migration (mix bundles and files)
- ✅ No breaking changes (same API)
- ✅ Standard formats (ZIP, JSON)

---

## Conclusion

The container-based architecture aligns WingedBean with industry standards while maintaining simplicity and backward compatibility. The `.wbundle` format provides an efficient, developer-friendly way to distribute game content that scales from small indie projects to large commercial games.

**Status**: ✅ Implementation Complete
**Tests**: ✅ 35/35 Passing
**Documentation**: ✅ Updated
**Ready**: ✅ For Production Use

---

## References

- [RFC-0027](../rfcs/0027-resource-service-console-implementation.md)
- [ResourceBundle.cs](../../development/dotnet/console/src/plugins/WingedBean.Plugins.Resource/ResourceBundle.cs)
- [ResourceBundleBuilder.cs](../../development/dotnet/console/src/plugins/WingedBean.Plugins.Resource/ResourceBundleBuilder.cs)
- [Plugin README](../../development/dotnet/console/src/plugins/WingedBean.Plugins.Resource/README.md)

# WingedBean.Plugins.Resource

Resource loading service for Console profile with automatic reference counting and lifecycle management (RFC-0054).

## Overview

This plugin provides an implementation of `IResourceService` backed by **ResourceManager** - a sophisticated resource orchestrator with:

- ✨ **Automatic reference counting** - Resources kept alive while in use
- 🔄 **Load deduplication** - Multiple concurrent loads → single I/O operation
- 🧵 **Thread-safe** - Safe resource sharing between systems  
- 📊 **Lifecycle events** - Observable streams for OnLoaded/OnUnloaded
- 🎯 **Smart cleanup** - Automatic unload after idle timeout
- 📦 **Bundle support** - Load from `.wbundle` containers or individual files

## Quick Start

### Basic Usage (Old API - Still Works!)

```csharp
var resourceService = serviceProvider.GetRequiredService<IResourceService>();

// Load a resource (cached automatically)
var config = await resourceService.LoadAsync<GameConfig>("config.json");
```

### New API: Handle-Based (Recommended)

```csharp
var resourceManager = serviceProvider.GetRequiredService<ResourceManager>();

// Automatic cleanup with using statement
using var handle = resourceManager.GetHandle<LevelData>("levels/dungeon1.json");
var level = await handle.GetAsync();
// ... use level ...
// ✨ Automatic cleanup when handle disposed
```

## Features

### RFC-0054: Reference Counting & Lifecycle (NEW!)

- **Automatic Resource Management**: Use `ResourceHandle<T>` with `using` statements
- **Load Deduplication**: Concurrent loads of same resource → single I/O operation
- **Reference Counting**: Resources automatically unloaded when no longer in use
- **Observable Events**: Subscribe to OnLoaded/OnUnloaded via System.Reactive
- **State Tracking**: Monitor resource state (Unloaded/Loading/Loaded/Failed)
- **Weak References**: Observe resources without keeping them alive
- **Thread-Safe**: Safe sharing between multiple systems

### Bundle-Based Loading (Primary)
- **Container Format**: ZIP-based `.wbundle` files with JSON manifests
- **Efficient Loading**: Load entire bundles once, access resources instantly
- **Metadata Rich**: Bundle manifests include versioning, dependencies, tags
- **Lazy Loading**: Resources loaded on-demand from bundles
- **Multiple Bundles**: Supports loading from multiple bundle files

### Individual File Loading (Fallback)
- **JSON Support**: Automatic deserialization using System.Text.Json
- **Text Files**: Load plain text files as strings
- **Binary Files**: Load binary data as byte arrays
- **Pattern Matching**: Load multiple resources using wildcards

### Performance
- **In-Memory Caching**: Avoids redundant I/O operations
- **Thread-Safe**: Concurrent resource loading supported
- **Metadata Queries**: Query resource info without loading content

## Usage

### Handle-Based API (RFC-0054 - Recommended)

```csharp
// Inject ResourceManager
public class GameSystem
{
    private readonly ResourceManager _resources;
    
    public GameSystem(ResourceManager resources)
    {
        _resources = resources;
    }
    
    // Example 1: Automatic cleanup
    public async Task LoadLevel(string levelId)
    {
        using var handle = _resources.GetHandle<LevelData>($"levels/{levelId}.json");
        var level = await handle.GetAsync();
        // ... use level ...
        // ✨ Automatic cleanup on dispose
    }
    
    // Example 2: Long-lived resource
    private ResourceHandle<GameData>? _gameDataHandle;
    
    public async Task InitializeGame()
    {
        _gameDataHandle = _resources.GetHandle<GameData>("game-data.json");
        var gameData = await _gameDataHandle.GetAsync();
        // Resource stays alive while handle exists
    }
    
    public void Cleanup()
    {
        _gameDataHandle?.Dispose(); // Release reference
    }
}
```

### Shared Resources (Safe with Reference Counting)

```csharp
// System A
using var handleA = resourceManager.GetHandle<Texture>("sprites/player.png");
var textureA = await handleA.GetAsync();

// System B (concurrent)
using var handleB = resourceManager.GetHandle<Texture>("sprites/player.png");
var textureB = await handleB.GetAsync(); // ✅ Instant - deduplicated load

// textureA and textureB are the same instance
// Resource stays loaded until BOTH handles are disposed
```

### Observable Lifecycle Events

```csharp
using var handle = resourceManager.GetHandle<Config>("config.json");

// Subscribe to load event
handle.OnLoaded.Subscribe(config =>
{
    Console.WriteLine($"Config loaded: {config.Version}");
});

// Subscribe to unload event
handle.OnUnloaded.Subscribe(_ =>
{
    Console.WriteLine("Config unloaded");
});

var config = await handle.GetAsync();
```

### Weak References (Observers)

```csharp
// Weak handle doesn't increment refcount
var weakHandle = resourceManager.GetWeakHandle<GameData>("data.json");

// Can observe but won't prevent unload
weakHandle.OnLoaded.Subscribe(data =>
{
    Console.WriteLine($"Data loaded: {data.Name}");
});

// Upgrade to strong handle if needed
var strongHandle = weakHandle.ToStrongHandle();
if (strongHandle != null)
{
    // Now we own a reference
    var data = await strongHandle.GetAsync();
    strongHandle.Dispose();
}
```

### Load from Bundle (Primary Method)

```csharp
var resourceService = registry.Get<IResourceService>();

// Resources are automatically loaded from .wbundle files in the resources directory
// The service searches all loaded bundles first, then falls back to individual files

// Load a JSON resource from a bundle
var dungeon = await resourceService.LoadAsync<DungeonMap>("dungeons/level-01");

// Load a text resource
var helpText = await resourceService.LoadAsync<string>("text/help");
```

### Creating Resource Bundles

```csharp
// Create a bundle builder
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

### Bundle Format

A `.wbundle` file is a ZIP archive with the following structure:

```
game-data.wbundle (ZIP)
├── manifest.json              # Bundle manifest
└── resources/                 # Resource files
    ├── dungeons/
    │   ├── level-01.json
    │   └── level-02.json
    └── items/
        ├── weapons.json
        └── armor.json
```

**manifest.json** structure:
```json
{
  "id": "game-data",
  "version": "1.0.0",
  "name": "Game Data Bundle",
  "description": "Core game data resources",
  "author": "WingedBean Team",
  "createdAt": "2025-01-10T12:00:00Z",
  "resources": [
    {
      "id": "dungeons/level-01",
      "path": "resources/dungeons/level-01.json",
      "type": "data",
      "format": "JSON",
      "size": 2048,
      "tags": ["dungeon", "level"]
    }
  ]
}
```

### Fallback to Individual Files

If a resource is not found in any bundle, the service automatically falls back to loading individual files:

```csharp
// This will check bundles first, then look for data/config/settings.json file
var settings = await resourceService.LoadAsync<Settings>("data/config/settings.json");
```

### Load Multiple Resources

```csharp
// Load all JSON files matching a pattern (works with both bundles and individual files)
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
// Preload resources for faster access later
var resourceIds = new[]
{
    "data/dungeons/level-01.json",
    "data/items/weapons.json",
    "data/enemies/monsters.json"
};

await resourceService.PreloadAsync(resourceIds);
```

## Resource Directory Structure

By default, resources are loaded from `<AppContext.BaseDirectory>/resources/`:

```
resources/
├── data/               # JSON/YAML data files
│   ├── dungeons/
│   ├── items/
│   └── enemies/
├── text/               # Text content
│   ├── dialog/
│   └── help/
└── config/             # Configuration files
```

## Supported Formats

- **JSON** (`.json`): Automatic deserialization to typed objects
- **Text** (`.txt`, `.md`): Loaded as strings
- **Binary** (`.bin`, `.dat`): Loaded as byte arrays
- **YAML** (`.yaml`, `.yml`): Treated as data files

## Configuration

The plugin uses sensible defaults but can be configured through `IConfigService` (future enhancement):

```json
{
  "Plugins": {
    "Resource": {
      "BasePath": "resources",
      "EnableCaching": true,
      "SupportedFormats": ["json", "txt", "yaml", "bin"]
    }
  }
}
```

## Architecture

This plugin follows the WingedBean 4-tier architecture and implements RFC-0054:

- **Tier 1**: `CrossMilo.Contracts.Resource.IResourceService` (framework contract)
- **Tier 3**: `FileSystemResourceService` (plugin implementation)
  - Now backed by `ResourceManager` (RFC-0054)
  - Automatic reference counting
  - Load deduplication
  - Lifecycle events

### Component Diagram

```
┌──────────────────────────────────────────────────┐
│          IResourceService (Public API)           │ ← Tier 1 Contract
├──────────────────────────────────────────────────┤
│       FileSystemResourceService                  │ ← Tier 3 Plugin
│  (delegates to ResourceManager)                  │
├──────────────────────────────────────────────────┤
│  ResourceManager (RFC-0054)                      │
│  • Reference counting                            │
│  • Load deduplication                            │
│  • Lifecycle events                              │
├──────────────────────────────────────────────────┤
│  ResourceHandle<T> │ WeakResourceHandle<T>       │
│  (Strong ref)      │ (Weak ref)                  │
├──────────────────────────────────────────────────┤
│          Enhanced ResourceCache                  │
│  • RefCount per entry                            │
│  • State machine tracking                        │
│  • Observable event streams                      │
├──────────────────────────────────────────────────┤
│         IResourceProvider Chain                  │
│  • FileSystemProvider                            │
│  • NuGetProvider (optional)                      │
│  • BundleProvider                                │
└──────────────────────────────────────────────────┘
```

## Migration from Old API

See [MIGRATION-GUIDE.md](./MIGRATION-GUIDE.md) for detailed migration instructions.

**TL;DR:**
- ✅ Old API still works (fully backward compatible)
- 🔄 Gradually migrate to handle-based API for better resource management
- 🎯 Use handles for long-lived or shared resources
- 📚 Migration patterns for common scenarios provided

## Dependencies

- `CrossMilo.Contracts.Resource` (Tier 1 contract)
- `PluginManoi.Core` (plugin infrastructure)
- `Microsoft.Extensions.Logging.Abstractions` (logging)
- `System.Text.Json` (JSON serialization)
- `System.Reactive` (lifecycle events - NEW in RFC-0054)

## See Also

- **[RFC-0054: Resource Reference Counting and Lifecycle Management](../../../../../docs/rfcs/0054-resource-reference-counting-lifecycle.md)** - Full specification
- **[MIGRATION-GUIDE.md](./MIGRATION-GUIDE.md)** - How to migrate from old API
- [RFC-0027: Resource Service Console Implementation](../../../../../docs/rfcs/0027-resource-service-console-implementation.md) - Original design
- [IResourceService Contract](../../../../../../../plate-projects/cross-milo/dotnet/framework/src/CrossMilo.Contracts.Resource/)

## Version History

- **v2.0** (RFC-0054 Phase 2): Added ResourceManager, reference counting, lifecycle events
- **v1.0** (RFC-0027): Initial implementation with bundle support


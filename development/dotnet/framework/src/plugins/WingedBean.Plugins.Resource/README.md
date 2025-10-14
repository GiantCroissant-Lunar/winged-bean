# WingedBean.Plugins.Resource

Resource loading service for Console profile with container-based (bundle) architecture.

## Overview

This plugin provides an implementation of `IResourceService` that prioritizes loading from **resource bundles** (`.wbundle` files - ZIP containers with manifests) with fallback to individual files. This follows industry standards like NuGet packages, Unity AssetBundles, and JAR files.

## Features

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

This plugin follows the WingedBean 4-tier architecture:

- **Tier 1**: `WingedBean.Contracts.Resource.IResourceService` (framework)
- **Tier 3**: `WingedBean.Plugins.Resource.FileSystemResourceService` (this plugin)

## Dependencies

- `WingedBean.Contracts.Resource` (Tier 1 contract)
- `WingedBean.PluginSystem` (plugin infrastructure)
- `Microsoft.Extensions.Logging.Abstractions` (logging)
- `System.Text.Json` (JSON serialization)

## See Also

- [RFC-0027: Resource Service Console Implementation](../../../../../docs/rfcs/0027-resource-service-console-implementation.md)
- [IResourceService Contract](../../../../framework/src/WingedBean.Contracts.Resource/IResourceService.cs)
- [Plugin Architecture Adjustments](../../../../../docs/implementation/PLUGIN-ARCHITECTURE-ADJUSTMENTS.md)

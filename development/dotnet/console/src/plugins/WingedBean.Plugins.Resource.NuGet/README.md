# WingedBean.Plugins.Resource.NuGet

NuGet package resource provider for the WingedBean Resource Service (Console profile only).

## Overview

This Tier 4 provider extends the Resource Service to support loading NuGet packages dynamically at runtime. It enables plugins and applications to declare NuGet package dependencies that are automatically downloaded and loaded on-demand.

## Features

- **Dynamic NuGet Loading**: Load packages from NuGet.org or custom feeds
- **Version Resolution**: Support for latest, specific, or range-based versions
- **Caching**: In-memory and disk caching to avoid redundant downloads
- **Framework Targeting**: Automatic selection of .NET 8.0 compatible assemblies
- **Security**: Optional package signing verification and whitelisting
- **Statistics**: Track cache hits, access patterns, and usage

## Usage

### Basic Package Loading

```csharp
var resourceService = registry.Get<IResourceService>();

// Load latest version
var package = await resourceService.LoadAsync<NuGetPackageResource>("nuget:Newtonsoft.Json");

// Load specific version
var package = await resourceService.LoadAsync<NuGetPackageResource>("nuget:Newtonsoft.Json/13.0.3");

// Load from custom feed
var package = await resourceService.LoadAsync<NuGetPackageResource>(
    "nuget:MyPackage@https://my-feed.com/v3/index.json"
);
```

### Accessing Package Assemblies

```csharp
// Get all assemblies
foreach (var assembly in package.GetAssemblies())
{
    Console.WriteLine($"Assembly: {assembly.FullName}");
}

// Get specific assembly
var jsonAssembly = package.GetAssembly("Newtonsoft.Json");

// Load type from package
var jsonSerializer = package.LoadType<object>("Newtonsoft.Json.JsonSerializer");
```

### Plugin Dependencies

Declare NuGet dependencies in `.plugin.json`:

```json
{
  "id": "my-plugin",
  "version": "1.0.0",
  "dependencies": {
    "nuget": [
      {
        "packageId": "Newtonsoft.Json",
        "version": "13.0.3"
      }
    ]
  }
}
```

## URI Format

```
nuget:<PackageId>[/<Version>][@<FeedUrl>]
```

**Examples:**
- `nuget:Newtonsoft.Json` - Latest stable version from NuGet.org
- `nuget:Newtonsoft.Json/13.0.3` - Specific version
- `nuget:Newtonsoft.Json@https://custom-feed.com/v3/index.json` - Custom feed

## Configuration

Configure via `appsettings.json`:

```json
{
  "ResourceService": {
    "NuGet": {
      "DefaultFeed": "https://api.nuget.org/v3/index.json",
      "IncludePrerelease": false,
      "PackagesDirectory": "~/.wingedbean/packages",
      "RequireSignedPackages": false,
      "AllowedPackages": [],
      "WarnOnUntrustedPackages": true,
      "MaxCacheSizeBytes": 1073741824
    }
  }
}
```

## Architecture

```
IResourceService (Tier 1 Contract)
    ↓
FileSystemResourceService (Tier 3 Plugin)
    ↓
NuGetResourceProvider (implements IResourceProvider)
    ↓
NuGetPackageLoader (Tier 4 - uses NuGet.Protocol)
```

## Dependencies

- NuGet.Protocol 6.8.0+
- NuGet.Packaging 6.8.0+
- NuGet.Configuration 6.8.0+
- NuGet.Frameworks 6.8.0+
- NuGet.Versioning 6.8.0+

## Cache Location

Default cache directory: `~/.wingedbean/packages`

Package structure:
```
~/.wingedbean/packages/
  newtonsoft.json/
    13.0.3/
      lib/
        net8.0/
          Newtonsoft.Json.dll
      newtonsoft.json.nuspec
```

## Security

### Package Trust

- Configure `RequireSignedPackages` to enforce package signatures
- Use `AllowedPackages` whitelist to restrict loadable packages
- Enable `WarnOnUntrustedPackages` for security prompts

### Version Pinning

Always pin versions in production:
```json
"dependencies": {
  "nuget": [
    {"packageId": "Package", "version": "1.2.3"}  // Exact version
  ]
}
```

## Performance

- **First Load**: Downloads package (~5-60 seconds depending on size)
- **Cached Load**: <100ms from memory cache
- **Disk Cache**: ~500ms from disk cache

## Limitations

- Console profile only (Unity/Godot use different package managers)
- Requires internet connection for first download
- .NET 8.0 target framework only
- No support for native dependencies (C++ DLLs)

## Related

- RFC-0039: NuGet Package Resource Provider
- RFC-0027: Resource Service Console Implementation
- Plate.CrossMilo.Contracts.Resource (Tier 1 contracts)

## License

MIT License (same as WingedBean framework)

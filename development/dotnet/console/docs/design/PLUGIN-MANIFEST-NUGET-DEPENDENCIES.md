# Plugin Manifest: NuGet Dependencies

## Overview

Plugins can now declare NuGet package dependencies in their `.plugin.json` manifests. The host automatically loads these packages before the plugin assembly is loaded, making them available to the plugin at runtime.

## Manifest Schema

### Basic Structure

```json
{
  "id": "my-plugin",
  "version": "1.0.0",
  "name": "My Plugin",
  
  "dependencies": {
    "plugins": [],
    "nuget": [
      {
        "packageId": "Newtonsoft.Json",
        "version": "13.0.3",
        "optional": false,
        "reason": "JSON serialization"
      }
    ]
  }
}
```

### NuGet Dependency Fields

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| `packageId` | string | ✅ Yes | NuGet package ID (e.g., "Newtonsoft.Json") |
| `version` | string | ❌ No | Specific version (e.g., "13.0.3"). Null = latest |
| `feed` | string | ❌ No | Custom NuGet feed URL. Null = NuGet.org |
| `optional` | boolean | ❌ No | If true, plugin loads even if package fails. Default: false |
| `reason` | string | ❌ No | Human-readable explanation for this dependency |

## Examples

### Example 1: Simple Dependency

```json
{
  "id": "wingedbean.plugins.json",
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

### Example 2: Latest Version

```json
{
  "id": "wingedbean.plugins.http",
  "dependencies": {
    "nuget": [
      {
        "packageId": "Polly",
        "version": null  // or omit version field
      }
    ]
  }
}
```

### Example 3: Optional Dependency

```json
{
  "id": "wingedbean.plugins.ai",
  "dependencies": {
    "nuget": [
      {
        "packageId": "Microsoft.ML",
        "version": "3.0.0",
        "optional": false,
        "reason": "Core AI functionality"
      },
      {
        "packageId": "TorchSharp",
        "version": "0.100.0",
        "optional": true,
        "reason": "Optional GPU acceleration"
      }
    ]
  }
}
```

### Example 4: Custom Feed

```json
{
  "id": "wingedbean.plugins.internal",
  "dependencies": {
    "nuget": [
      {
        "packageId": "Company.InternalLib",
        "version": "2.1.0",
        "feed": "https://internal-feed.company.com/v3/index.json"
      }
    ]
  }
}
```

### Example 5: Multiple Dependencies

```json
{
  "id": "wingedbean.plugins.web",
  "dependencies": {
    "plugins": ["wingedbean.plugins.resource"],
    "nuget": [
      {
        "packageId": "Microsoft.Extensions.Http",
        "version": "8.0.0",
        "reason": "HTTP client factory"
      },
      {
        "packageId": "Polly",
        "version": "8.5.0",
        "reason": "Resilience and retry policies"
      },
      {
        "packageId": "Serilog",
        "version": "3.1.1",
        "reason": "Structured logging"
      }
    ]
  }
}
```

## Loading Behavior

### Load Order

1. **Plugin Dependencies** - Other plugins loaded first
2. **NuGet Dependencies** - Packages downloaded/loaded
3. **Plugin Assembly** - Your plugin DLL loaded
4. **Service Registration** - Services registered in DI

### Failure Handling

**Required Dependencies (`optional: false`):**
- Plugin load fails if package not found
- Error logged and plugin skipped
- Other plugins continue loading

**Optional Dependencies (`optional: true`):**
- Plugin loads even if package fails
- Warning logged
- Plugin should check if package available

### Version Resolution

```json
{"version": "13.0.3"}      // Exact version
{"version": null}           // Latest stable
{"version": "[13.0,14.0)"}  // Version range (future)
```

## Logging

### Successful Load

```
→ Loading manifest plugin: my-plugin from /path/to/MyPlugin.dll
  → Loading 2 NuGet dependencies for my-plugin...
    → Loading NuGet: Newtonsoft.Json 13.0.3
      ✓ Loaded: Newtonsoft.Json v13.0.3
    → Loading NuGet: Polly latest
      ✓ Loaded: Polly v8.5.0
  ✓ NuGet dependencies loaded for my-plugin
  ✓ Loaded: my-plugin v1.0.0
```

### Failed Load (Required)

```
→ Loading manifest plugin: my-plugin from /path/to/MyPlugin.dll
  → Loading 1 NuGet dependencies for my-plugin...
    → Loading NuGet: NonExistent.Package 1.0.0
      ✗ Failed to load required NuGet package 'NonExistent.Package': Package not found
  ✗ Failed to load manifest plugin from /path/.plugin.json
```

### Failed Load (Optional)

```
→ Loading manifest plugin: my-plugin from /path/to/MyPlugin.dll
  → Loading 1 NuGet dependencies for my-plugin...
    → Loading NuGet: Optional.Package 1.0.0
      ⊘ Failed to load optional NuGet package 'Optional.Package': Package not found
  ✓ NuGet dependencies loaded for my-plugin (1 optional failed)
  ✓ Loaded: my-plugin v1.0.0
```

## Usage in Plugin Code

### Accessing Loaded Packages

NuGet packages are loaded into the process and their assemblies are available:

```csharp
public class MyPlugin : IPlugin
{
    public Task OnActivateAsync(IRegistry registry, CancellationToken ct)
    {
        // Package already loaded by host - just use it!
        var json = Newtonsoft.Json.JsonConvert.SerializeObject(new { Hello = "World" });
        
        return Task.CompletedTask;
    }
}
```

### Checking Optional Packages

```csharp
public class AIPlugin : IPlugin
{
    private bool _gpuAccelerationAvailable;
    
    public Task OnActivateAsync(IRegistry registry, CancellationToken ct)
    {
        // Check if optional TorchSharp loaded
        _gpuAccelerationAvailable = AppDomain.CurrentDomain.GetAssemblies()
            .Any(a => a.GetName().Name == "TorchSharp");
        
        if (_gpuAccelerationAvailable)
        {
            _logger.LogInformation("GPU acceleration enabled");
        }
        
        return Task.CompletedTask;
    }
}
```

## Configuration

### Global NuGet Settings

Configure NuGet package loading in `appsettings.json`:

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

## Limitations

1. **Console Profile Only** - Unity/Godot don't support NuGet provider
2. **.NET 8.0 Only** - Packages must have .NET 8.0 compatible assemblies
3. **No Native Dependencies** - C++ DLLs not supported
4. **First Load Delay** - Initial download takes time (5-60 seconds)
5. **Network Required** - First load requires internet connection

## Best Practices

### 1. Pin Versions in Production

```json
{
  "packageId": "Newtonsoft.Json",
  "version": "13.0.3"  // ✅ Explicit version
}
```

Not:
```json
{
  "packageId": "Newtonsoft.Json"  // ❌ Latest (unpredictable)
}
```

### 2. Document Reasons

```json
{
  "packageId": "Polly",
  "version": "8.5.0",
  "reason": "Resilience and retry policies for HTTP calls"  // ✅ Clear purpose
}
```

### 3. Mark Optional Appropriately

```json
{
  "packageId": "Core.Library",
  "optional": false  // ✅ Plugin won't work without this
}
{
  "packageId": "GPU.Acceleration",
  "optional": true  // ✅ Nice to have, but not required
}
```

### 4. Use Resource Plugin for Dependencies

Ensure Resource plugin loads first:

```json
{
  "id": "my-plugin",
  "priority": 50,  // ✅ Lower than Resource plugin (100)
  "dependencies": {
    "plugins": ["wingedbean.plugins.resource"],  // ✅ Explicit dependency
    "nuget": [...]
  }
}
```

## Troubleshooting

### Problem: "Resource service not available"

**Cause:** Resource plugin not loaded yet

**Solution:** Add Resource plugin dependency:
```json
{
  "dependencies": {
    "plugins": ["wingedbean.plugins.resource"]
  }
}
```

### Problem: "Package not found"

**Causes:**
- Network connectivity issues
- Package name misspelled
- Version doesn't exist
- Custom feed not configured

**Solutions:**
1. Check package exists on NuGet.org
2. Verify version number
3. Test network connectivity
4. Configure custom feed if needed

### Problem: "Wrong framework version"

**Cause:** Package doesn't have .NET 8.0 binaries

**Solution:** Use a compatible package version or find alternative

## Migration Guide

### Legacy (plugins.json)

```json
{
  "plugins": [
    {
      "id": "my-plugin",
      "path": "plugins/MyPlugin/MyPlugin.dll",
      "dependencies": ["resource"]
    }
  ]
}
```

### New (.plugin.json)

```json
{
  "id": "my-plugin",
  "dependencies": {
    "plugins": ["wingedbean.plugins.resource"],
    "nuget": [
      {
        "packageId": "Newtonsoft.Json",
        "version": "13.0.3"
      }
    ]
  },
  "entryPoint": {
    "dotnet": "./MyPlugin.dll"
  }
}
```

## See Also

- RFC-0039: NuGet Package Resource Provider
- Plugin Manifest Schema Documentation
- Resource Service Documentation
- WingedBean.Plugins.Resource.NuGet Package

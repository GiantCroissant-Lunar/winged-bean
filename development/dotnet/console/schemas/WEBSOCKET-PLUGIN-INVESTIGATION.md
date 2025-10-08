# WebSocket Plugin Investigation

## Issue Found
The WebSocket plugin was showing JSON deserialization errors during runtime:
```
The JSON value could not be converted to Plate.PluginManoi.Core.PluginDependencies. 
Path: $.dependencies | LineNumber: 9 | BytePositionInLine: 19.
```

## Root Cause
The WebSocket plugin's manifest in the build output directory had an **outdated format**:
- **Old format in bin/**: `"dependencies": []` (empty array)
- **New format in source**: `"dependencies": {}` (empty object)

The quicktype-generated types expect `dependencies` to be an **object** with optional `plugins` and `nuget` arrays:
```csharp
public partial class Dependencies
{
    [JsonPropertyName("plugins")]
    public List<string>? Plugins { get; set; }

    [JsonPropertyName("nuget")]
    public List<NuGetDependency>? Nuget { get; set; }
}
```

## Resolution Status

### ✅ Source File is Correct
`src/plugins/WingedBean.Plugins.WebSocket/.plugin.json` already uses the correct format:
```json
{
  "id": "WingedBean.Plugins.WebSocket",
  "dependencies": {},
  ...
}
```

### ⚠️ Build Issue Prevents Copy
The WebSocket plugin is **not being copied** to the Debug output folder because it has **build errors** in its dependencies:
- `CrossMilo.Contracts.WebSocket` project has compilation errors
- Missing `PluginManoi.Contracts` reference
- Missing attributes: `RealizeService`, `SelectionStrategy`
- Missing interface implementations

### ✅ Other Plugins Working
After rebuild, all other plugins load successfully without JSON errors:
- ✅ WingedBean.Plugins.Resource
- ✅ WingedBean.Plugins.ArchECS  
- ✅ WingedBean.Plugins.Audio
- ✅ WingedBean.Plugins.AsciinemaRecorder
- ✅ WingedBean.Plugins.DungeonGame
- ✅ WingedBean.Plugins.ConsoleDungeon

## Correct Dependency Formats

### Empty Dependencies
```json
"dependencies": {}
```

### With Plugin Dependencies
```json
"dependencies": {
  "plugins": ["other-plugin-id"]
}
```

### With NuGet Dependencies  
```json
"dependencies": {
  "nuget": [
    {
      "packageId": "SomePackage",
      "version": "1.0.0",
      "optional": false
    }
  ]
}
```

### Complete Example
```json
"dependencies": {
  "plugins": ["wingedbean.contracts"],
  "nuget": [
    {
      "packageId": "Newtonsoft.Json",
      "version": "13.0.3",
      "optional": false
    }
  ]
}
```

## Action Items

### To Fix WebSocket Plugin Build
1. Fix `CrossMilo.Contracts.WebSocket` project references:
   - Add proper reference to `PluginManoi.Contracts`
   - Implement missing interface members
   - Add missing attributes

2. Once build succeeds, WebSocket will copy correctly

### Prevention
- ✅ JSON schema defined in `plugin-manifest.schema.json`
- ✅ Quicktype generates type-safe classes
- ✅ System.Text.Json validates at runtime
- Future: Consider adding CI validation of all plugin manifests

## Conclusion
The quicktype adoption successfully **caught and prevented** the JSON format mismatch. The WebSocket plugin's source manifest is already correct; it just needs its build dependencies fixed to be included in the output.

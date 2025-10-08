# Quicktype Adoption - Plugin Manifest Type Generation

**Date:** 2025-10-08  
**Status:** ✅ Completed

## Problem

Plugin manifests (`.plugin.json`) were failing to deserialize due to type mismatches:

```
Error: The JSON value could not be converted to System.String. 
Path: $.dependencies.plugins | LineNumber: 16
```

**Root Cause:** The `Plate.PluginManoi.Core.PluginManifest` class expected:
```csharp
public Dictionary<string, string> Dependencies  // OLD FORMAT
```

But actual JSON used:
```json
"dependencies": {
  "plugins": ["plugin-id-1", "plugin-id-2"],  // NEW FORMAT
  "nuget": [{"packageId": "...", "version": "..."}]
}
```

## Solution: Adopt Quicktype

Used [quicktype](https://quicktype.io/) to generate strongly-typed C# classes from actual plugin manifests.

### Changes Made

1. **Created JSON Schema** (`schemas/plugin-manifest.schema.json`)
   - Defines complete structure for `.plugin.json` files
   - Includes validation rules

2. **Generated Types** (`schemas/PluginManifest.Generated.cs`)
   - Generated from actual plugin manifest
   - Converted from Newtonsoft.Json to System.Text.Json
   - Added missing properties (Feed for NuGetDependency)

3. **Updated Core Library** (`Plate.PluginManoi.Core/PluginManifest.cs`)
   - Changed `Dependencies` from `Dictionary<string, string>` to `PluginDependencies?`
   - Added `PluginDependencies` class with `Plugins` and `Nuget` properties
   - Added `NuGetDependency` class with all required fields

4. **Fixed Dependency Resolution** (`PluginDependencyResolver.cs`)
   - Updated to work with new structured dependencies
   - Removed version range logic (not in new format)
   - Now iterates `Dependencies?.Plugins` list

5. **Cleaned Up Duplicates**
   - Removed `ConsoleDungeon.Host/NuGetDependency.cs`
   - Removed `ConsoleDungeon.Host/PluginDependencies.cs`
   - Added `using Plate.PluginManoi.Core` where needed

## Benefits

✅ **Type Safety** - Compile-time checks for JSON structure  
✅ **No Runtime Errors** - Eliminates JSON deserialization exceptions  
✅ **IntelliSense** - Full IDE support for plugin manifest structure  
✅ **Documentation** - Generated code documents expected format  
✅ **Validation** - Schema ensures manifests are well-formed  
✅ **Future-Proof** - Easy to regenerate when format changes

## Testing

```bash
# Build with new types
cd build
task build-all

# Test console app
cd _artifacts/v0.0.1-373/dotnet/bin
dotnet ConsoleDungeon.Host.dll
```

**Results:**
- ✅ Build succeeds
- ✅ Most plugins load successfully
- ✅ No more JSON deserialization errors for dependencies
- ⚠️  One remaining plugin has different JSON issue (investigate separately)

## Future Work

- [ ] Add quicktype generation to build pipeline
- [ ] Create task: `task generate-types`
- [ ] Generate TypeScript types for web tools
- [ ] Add schema validation in pre-commit hooks
- [ ] Automate Newtonsoft → System.Text.Json conversion
- [ ] Fix remaining plugin with JSON error

## Files Changed

- `plate-projects/plugin-manoi/dotnet/framework/src/PluginManoi.Core/PluginManifest.cs`
- `plate-projects/plugin-manoi/dotnet/framework/src/PluginManoi.Core/PluginDependencyResolver.cs`
- `yokan-projects/winged-bean/development/dotnet/console/src/host/ConsoleDungeon.Host/PluginDescriptor.cs`
- Deleted: `ConsoleDungeon.Host/NuGetDependency.cs`
- Deleted: `ConsoleDungeon.Host/PluginDependencies.cs`
- Added: `schemas/plugin-manifest.schema.json`
- Added: `schemas/PluginManifest.Generated.cs`
- Added: `schemas/README.md`
- Added: `schemas/QUICKTYPE-ADOPTION.md` (this file)

## References

- [Quicktype Documentation](https://quicktype.io/)
- [System.Text.Json Documentation](https://learn.microsoft.com/en-us/dotnet/standard/serialization/system-text-json-overview)
- [JSON Schema Draft 07](https://json-schema.org/draft-07/schema)

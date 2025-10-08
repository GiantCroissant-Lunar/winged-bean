# JSON Schema Validation for Plugin Manifests

## Overview

We've implemented automated JSON schema validation to catch plugin manifest format errors **before** they cause runtime issues.

## Problem It Solves

Without validation, format errors aren't discovered until runtime:
```
❌ The JSON value could not be converted to Plate.PluginManoi.Core.PluginDependencies
```

With validation, errors are caught immediately during build:
```bash
✅ Valid:   10
❌ Invalid: 0
📊 Total:   10
```

## Implementation

### 1. JSON Schema Definition
`plugin-manifest.schema.json` - Defines the structure and constraints:
- ID must be lowercase kebab-case: `^[a-z0-9.-]+$`
- LoadStrategy enum: `["eager", "lazy", "on-demand"]`
- Dependencies must be object with `plugins` and `nuget` arrays
- Required fields: `id`, `version`, `name`, `entryPoint`

### 2. Validation Script
`validate-manifests.sh` - Validates all plugin manifests:
```bash
#!/usr/bin/env bash
# Finds all .plugin.json files (excluding build output)
# Validates each against the schema using ajv-cli
# Reports detailed errors with file path and issue
```

### 3. Build Integration
`Taskfile.yml` - Runs validation before build:
```yaml
tasks:
  validate-manifests:
    desc: "Validate all plugin manifests against JSON schema"
    cmds:
      - ./schemas/validate-manifests.sh

  build:
    desc: "Build ConsoleDungeon game"
    deps:
      - validate-manifests  # ✅ Blocks build if validation fails
    cmds:
      - dotnet build Console.sln
```

## Issues Found and Fixed

### Issue 1: Invalid Load Strategy
**Plugin**: WingedBean.Plugins.DungeonGame

**Error**:
```json
{
  "instancePath": "/loadStrategy",
  "message": "must be equal to one of the allowed values"
}
```

**Fix**: Changed `"Eager"` → `"eager"`

### Issue 2: Invalid ID Format
**Plugin**: WingedBean.Plugins.WebSocket

**Error**:
```json
{
  "instancePath": "/id",
  "schemaPath": "#/properties/id/pattern",
  "message": "must match pattern \"^[a-z0-9.-]+$\""
}
```

**Fix**: Changed `"WingedBean.Plugins.WebSocket"` → `"wingedbean.plugins.websocket"`

## Usage

### Validate Manually
```bash
# Run validation
task validate-manifests

# Or directly
./schemas/validate-manifests.sh
```

### Auto-Validation During Build
```bash
# Validation runs automatically
task build
```

### Output Format
```
==========================================
Plugin Manifest Validation
==========================================
Schema: /path/to/plugin-manifest.schema.json

Found 10 plugin manifest(s) to validate

✅ WingedBean.Plugins.ArchECS
✅ WingedBean.Plugins.AsciinemaRecorder
✅ WingedBean.Plugins.Audio
✅ WingedBean.Plugins.Config
✅ WingedBean.Plugins.ConsoleDungeon
✅ WingedBean.Plugins.DungeonGame
✅ WingedBean.Plugins.Resilience
✅ WingedBean.Plugins.Resource
✅ WingedBean.Plugins.TerminalUI
✅ WingedBean.Plugins.WebSocket

==========================================
Validation Summary
==========================================
✅ Valid:   10
❌ Invalid: 0
📊 Total:   10

✅ All plugin manifests are valid!
```

## Benefits

### 1. Early Error Detection
- Catches format errors at **build time**, not runtime
- Provides clear error messages with file and line number
- Prevents broken manifests from being deployed

### 2. Consistent Format
- Enforces naming conventions (lowercase kebab-case IDs)
- Validates enum values (loadStrategy, lifecycle)
- Ensures required fields are present

### 3. Documentation
- Schema serves as documentation for manifest format
- Validation errors guide developers to correct format
- Examples in schema show expected structure

### 4. CI/CD Integration
- Returns non-zero exit code on failure
- Easy to integrate into CI/CD pipelines
- Fast validation (completes in <1 second)

### 5. Type Safety Layer
Works alongside quicktype-generated C# types:
- **Schema validation**: Catches format errors at build time
- **Type generation**: Provides compile-time type safety
- **Runtime deserialization**: Type-safe JSON parsing

## Common Validation Errors

### Invalid ID Format
```json
// ❌ Wrong
"id": "WingedBean.Plugins.Resource"

// ✅ Correct
"id": "wingedbean.plugins.resource"
```

### Invalid Load Strategy
```json
// ❌ Wrong
"loadStrategy": "Eager"

// ✅ Correct
"loadStrategy": "eager"
```

### Invalid Dependencies Format
```json
// ❌ Wrong - Array format
"dependencies": []

// ✅ Correct - Object format
"dependencies": {}

// ✅ Correct - With plugins
"dependencies": {
  "plugins": ["wingedbean.contracts"]
}
```

### Missing Required Fields
```json
// ❌ Wrong - Missing entryPoint
{
  "id": "my.plugin",
  "version": "1.0.0",
  "name": "My Plugin"
}

// ✅ Correct
{
  "id": "my.plugin",
  "version": "1.0.0",
  "name": "My Plugin",
  "entryPoint": {
    "dotnet": "./MyPlugin.dll"
  }
}
```

## Future Enhancements

- [ ] Add to pre-commit hooks
- [ ] Add CI/CD pipeline validation
- [ ] Generate validation report artifacts
- [ ] Add schema versioning
- [ ] Create JSON Schema linter for VS Code

## Files Modified

1. `schemas/plugin-manifest.schema.json` - Existing schema (unchanged)
2. `schemas/validate-manifests.sh` - **NEW** validation script
3. `Taskfile.yml` - Added `validate-manifests` task and build dependency
4. `schemas/README.md` - Updated with validation documentation
5. `src/plugins/WingedBean.Plugins.DungeonGame/.plugin.json` - Fixed loadStrategy
6. `src/plugins/WingedBean.Plugins.WebSocket/.plugin.json` - Fixed ID format

## Conclusion

JSON schema validation provides a **first line of defense** against plugin manifest errors, catching issues at build time before they cause runtime failures. Combined with quicktype-generated types, we now have both **build-time validation** and **compile-time type safety**.

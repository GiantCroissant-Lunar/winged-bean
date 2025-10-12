# Build System Migration: NUKE → Build-Arcane

**Date**: 2025-10-11  
**Status**: ✅ Complete

---

## Overview

WingedBean Console has been migrated from a custom NUKE build system to **Build-Arcane** (`lunar-build`), a config-driven .NET tool that provides centralized build logic.

---

## Migration Summary

### Before: Custom NUKE Build
- **Location**: `/build/nuke/build/Build.cs` (182 lines + partial classes)
- **Features**: 
  - Custom components (IBuildConfigurationComponent, IWrapperPathComponent, INfunReportComponent)
  - Multi-project support (console vs framework)
  - NuGet packaging
  - Test reporting
  - Complex configuration

### After: Build-Arcane
- **Location**: `build-config.json` (35 lines)
- **Features**:
  - Simple config-driven builds
  - Standard targets (clean, restore, compile, test, pack)
  - GitVersion integration
  - Automatic artifact organization
  - Consistent with cross-milo and plugin-manoi

---

## How to Build

### Using Build-Arcane (Recommended)

```bash
# Restore tools
dotnet tool restore

# Build only
dotnet lunar-build compile

# Build and test
dotnet lunar-build test

# Full CI pipeline
dotnet lunar-build ci
```

### Using Standard dotnet CLI (Still works)

```bash
dotnet build Console.sln
dotnet test Console.sln
```

---

## Artifacts Location

Build-Arcane generates versioned artifacts:

```
build/_artifacts/
  {version}/              # e.g., 0.0.1-420
    build-info.txt        # Build metadata
    build/                # Compiled outputs
      ConsoleDungeon.Host/
      ConsoleDungeon.Contracts/
      WingedBean.Plugins.ArchECS/
      WingedBean.Plugins.ConsoleDungeon/
      WingedBean.Plugins.DungeonGame/
    test-results/         # Test results (TRX files)
```

---

## Configuration

The build is configured via `build-config.json`:

```json
{
  "solution": "Console.sln",
  "projects": [
    "src/host/ConsoleDungeon.Host/ConsoleDungeon.Host.csproj",
    "src/game/ConsoleDungeon.Contracts/ConsoleDungeon.Contracts.csproj",
    "src/plugins/WingedBean.Plugins.ArchECS/WingedBean.Plugins.ArchECS.csproj",
    "src/plugins/WingedBean.Plugins.ConsoleDungeon/WingedBean.Plugins.ConsoleDungeon.csproj",
    "src/plugins/WingedBean.Plugins.DungeonGame/WingedBean.Plugins.DungeonGame.csproj"
  ],
  "tests": {
    "enabled": true
  }
}
```

---

## Build Targets

| Target    | Description                          |
|-----------|--------------------------------------|
| `clean`   | Clean artifacts directory            |
| `restore` | Restore NuGet packages               |
| `compile` | Build projects                       |
| `test`    | Run tests (if enabled)               |
| `pack`    | Create NuGet packages (if enabled)   |
| `ci`      | Full pipeline (all above)            |

---

## Comparison: NUKE vs Build-Arcane

| Feature | Custom NUKE | Build-Arcane |
|---------|-------------|--------------|
| **Lines of Code** | ~500+ | ~35 |
| **Configuration** | C# code | JSON config |
| **Maintenance** | Project-specific | Centralized |
| **Versioning** | GitVersion | GitVersion |
| **Artifacts** | Custom paths | Standardized |
| **Test Execution** | Custom | Built-in |
| **Multi-project** | Custom logic | Config-driven |
| **Reporting** | Custom components | Standard TRX |
| **NuGet Packaging** | Custom | Built-in |

---

## What Was Lost in Migration

The custom NUKE build had some advanced features that are not yet in Build-Arcane:

### 1. **Custom Reporting (INfunReportComponent)**
- **Before**: Multi-format reports (HTML, JSON, Markdown)
- **After**: Standard TRX files only
- **Workaround**: Use external tools to process TRX files

### 2. **Multi-Project Switching**
- **Before**: `--project console` or `--project framework` parameter
- **After**: Separate build-config.json files needed
- **Workaround**: Create `build-config.framework.json` if needed

### 3. **Custom NuGet Workflows**
- **Before**: `INuGetLocalRepoSyncComponent` for local feed sync
- **After**: Standard pack only
- **Workaround**: Manual sync or CI/CD scripts

### 4. **Wrapper Path Resolution**
- **Before**: `IWrapperPathComponent` for complex path resolution
- **After**: Standard relative paths
- **Impact**: Minimal - standard paths work fine

---

## Benefits of Migration

### ✅ Consistency
- Same build system as `cross-milo` and `plugin-manoi`
- Easier for new developers to understand
- Consistent artifact structure across projects

### ✅ Simplicity
- 35 lines of JSON vs 500+ lines of C#
- No custom build project to maintain
- Config-driven instead of code-driven

### ✅ Maintenance
- Build logic is centralized in build-arcane tool
- Updates to build logic benefit all projects
- No per-project build code to maintain

### ✅ Standardization
- Standard artifact paths: `build/_artifacts/{version}/`
- Standard test results: TRX format
- Standard versioning: GitVersion

---

## Old NUKE Build (Preserved)

The old NUKE build is preserved in `/build/nuke/` for reference:
- `build/Build.cs` - Main build class
- `build/Build.Configuration.cs` - Configuration component
- `build/Build.Reporting.cs` - Reporting component
- `build/Build.NuGetPackaging.cs` - NuGet packaging
- `build/Build.WrapperPath.cs` - Path resolution

**Status**: ⚠️ Deprecated - Use build-arcane instead

---

## Migration Checklist

- [x] Create `build-config.json`
- [x] Install build-arcane as local tool (`.config/dotnet-tools.json`)
- [x] Test `dotnet lunar-build compile`
- [x] Test `dotnet lunar-build test`
- [x] Verify artifacts in `build/_artifacts/{version}/`
- [x] Update documentation
- [ ] Update CI/CD pipelines (if any)
- [ ] Remove old NUKE build (optional - currently preserved)

---

## CI/CD Integration

For GitHub Actions, GitLab CI, or Azure Pipelines:

```yaml
- name: Restore tools
  run: dotnet tool restore

- name: Build and Test
  run: dotnet lunar-build ci

- name: Upload artifacts
  uses: actions/upload-artifact@v3
  with:
    name: build-artifacts
    path: build/_artifacts/
```

---

## Troubleshooting

### Tool not found
```bash
dotnet tool restore
```

### Wrong artifacts location
Build-Arcane uses `build/_artifacts/{version}/`, not the old NUKE paths.

### Missing features
If you need custom reporting or NuGet workflows, you may need to:
1. Use the old NUKE build (preserved in `/build/nuke/`)
2. Request features in build-arcane
3. Use external tools/scripts

---

## See Also

- Build-Arcane README: `/plate-projects/build-arcane/README.md`
- Build System Documentation: `/plate-projects/BUILD_SYSTEM.md`
- Detection Script: `/plate-projects/detect-build-system.sh`

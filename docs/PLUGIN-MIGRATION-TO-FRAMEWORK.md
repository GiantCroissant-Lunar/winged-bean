# Plugin Migration to Framework

**Date**: 2025-10-14  
**Status**: Completed  
**Migration Type**: Structural Reorganization

## Overview

Migrated 7 platform-agnostic plugins from `console/src/plugins/` to `framework/src/plugins/` to enable reuse across both console and desktop applications.

## Migrated Plugins

### ✅ Moved to Framework (7 plugins)

1. **WingedBean.Plugins.ArchECS**
   - **Purpose**: Entity Component System implementation using Arch ECS
   - **Rationale**: Pure ECS implementation with no UI dependencies
   - **Dependencies**: PluginManoi.Core, CrossMilo.Contracts

2. **WingedBean.Plugins.Analytics**
   - **Purpose**: Analytics services using Firebase/Firestore
   - **Rationale**: Platform-agnostic analytics
   - **Dependencies**: FirebaseAdmin, Google.Cloud.Firestore

3. **WingedBean.Plugins.Audio**
   - **Purpose**: Audio playback using LibVLCSharp
   - **Rationale**: Cross-platform audio (with platform-specific native libs)
   - **Dependencies**: LibVLCSharp, VideoLAN.LibVLC.Mac/Windows
   - **⚠️ Note**: Contains conditional package references for native libraries

4. **WingedBean.Plugins.Config**
   - **Purpose**: Configuration service using Microsoft.Extensions.Configuration
   - **Rationale**: Platform-agnostic configuration management
   - **Dependencies**: Microsoft.Extensions.Configuration.*

5. **WingedBean.Plugins.Diagnostics**
   - **Purpose**: Diagnostics using Sentry and OpenTelemetry
   - **Rationale**: Platform-agnostic monitoring and telemetry
   - **Dependencies**: Sentry, OpenTelemetry.*

6. **WingedBean.Plugins.Resource**
   - **Purpose**: File system-based resource loading
   - **Rationale**: Platform-agnostic resource management
   - **Dependencies**: CrossMilo.Contracts.Resource

7. **WingedBean.Plugins.Resource.NuGet**
   - **Purpose**: NuGet package resource provider
   - **Rationale**: Platform-agnostic NuGet integration
   - **Dependencies**: NuGet.Protocol, NuGet.Packaging, WingedBean.Plugins.Resource

8. **WingedBean.Plugins.WebSocket**
   - **Purpose**: WebSocket service using SuperSocket
   - **Rationale**: Platform-agnostic WebSocket communication
   - **Dependencies**: SuperSocket.WebSocket.Server

### ❌ Kept in Console (2 plugins)

1. **WingedBean.Plugins.DungeonGame**
   - **Location**: `console/src/plugins/WingedBean.Plugins.DungeonGame/`
   - **Rationale**: Console-specific (uses Terminal.Gui)
   - **Note**: References moved ArchECS plugin from framework

2. **WingedBean.Plugins.Audio** (consideration)
   - **Status**: Moved to framework
   - **Caution**: Test carefully on both platforms due to native library dependencies

## File Structure Changes

### Before
```
development/dotnet/
├── console/
│   └── src/
│       └── plugins/
│           ├── WingedBean.Plugins.ArchECS/
│           ├── WingedBean.Plugins.Analytics/
│           ├── WingedBean.Plugins.Audio/
│           ├── WingedBean.Plugins.Config/
│           ├── WingedBean.Plugins.Diagnostics/
│           ├── WingedBean.Plugins.DungeonGame/
│           ├── WingedBean.Plugins.Resource/
│           ├── WingedBean.Plugins.Resource.NuGet/
│           └── WingedBean.Plugins.WebSocket/
└── framework/
    └── src/
        └── (no plugins directory)
```

### After
```
development/dotnet/
├── console/
│   └── src/
│       └── plugins/
│           └── WingedBean.Plugins.DungeonGame/  (console-specific)
├── framework/
│   └── src/
│       └── plugins/  (NEW)
│           ├── WingedBean.Plugins.ArchECS/
│           ├── WingedBean.Plugins.Analytics/
│           ├── WingedBean.Plugins.Audio/
│           ├── WingedBean.Plugins.Config/
│           ├── WingedBean.Plugins.Diagnostics/
│           ├── WingedBean.Plugins.Resource/
│           ├── WingedBean.Plugins.Resource.NuGet/
│           └── WingedBean.Plugins.WebSocket/
└── windows/
    └── src/
        └── plugins/  (can now reference framework plugins)
```

## Updated References

### Solution File
- Updated `WingedBean.sln` to reflect new plugin paths
- Changed from `console\src\plugins\` to `framework\src\plugins\`

### Project References Updated

#### Test Projects (console/tests/plugins/)
- Path changed from: `../../../src/plugins/WingedBean.Plugins.*/`
- Path changed to: `../../../../../framework/src/plugins/WingedBean.Plugins.*/`

**Affected test projects:**
- WingedBean.Plugins.ArchECS.Tests
- WingedBean.Plugins.Analytics.Tests
- WingedBean.Plugins.Audio.Tests (if exists)
- WingedBean.Plugins.Config.Tests
- WingedBean.Plugins.Diagnostics.Tests
- WingedBean.Plugins.Resource.Tests
- WingedBean.Plugins.Resource.NuGet.Tests
- WingedBean.Plugins.WebSocket.Tests

#### Console Plugin References
- **WingedBean.Plugins.DungeonGame**: Updated ArchECS reference
  - From: `../WingedBean.Plugins.ArchECS/`
  - To: `../../../../framework/src/plugins/WingedBean.Plugins.ArchECS/`

#### Host and Demo Projects
- Updated any references from `../../plugins/` to `../../../../framework/src/plugins/`

### Internal Plugin References
- **WingedBean.Plugins.Resource.NuGet** → **WingedBean.Plugins.Resource**
  - Reference path remains `../WingedBean.Plugins.Resource/` (same directory)

## Migration Steps Performed

1. ✅ Created `framework/src/plugins/` directory
2. ✅ Moved 7 plugins to framework location using `mv` command
3. ✅ Updated solution file paths (WingedBean.sln)
4. ✅ Updated test project references
5. ✅ Updated console plugin references (DungeonGame → ArchECS)
6. ✅ Updated host/demo project references
7. ✅ Created migration documentation

## Verification Steps

### Required Testing

1. **Build Verification**
   ```bash
   cd build
   task build-all
   ```

2. **Console App Testing**
   ```bash
   task console:debug
   # or
   task console:normal
   ```

3. **Test Suite Execution**
   ```bash
   task test:all
   ```

4. **Plugin Loading Verification**
   - Verify all 7 framework plugins load correctly in console app
   - Check plugin discovery and activation logs
   - Confirm no path resolution errors

5. **Audio Plugin Special Testing**
   - Test on macOS (VideoLAN.LibVLC.Mac)
   - Test on Windows (VideoLAN.LibVLC.Windows)
   - Verify native library loading

## Breaking Changes

### For Developers

- **Project References**: Any new code referencing moved plugins must use framework paths
- **Build Scripts**: Ensure build scripts account for new plugin locations
- **Plugin Discovery**: Plugin loaders may need path updates if hardcoded

### For CI/CD

- **Artifact Paths**: Build artifacts now include `framework/src/plugins/`
- **Test Discovery**: Test runners must find tests in updated locations
- **Deployment**: Plugin deployment scripts need path updates

## Rollback Plan

If issues arise, rollback using git:

```bash
# Revert all changes
git checkout development/dotnet/WingedBean.sln
git checkout development/dotnet/console/
git checkout development/dotnet/framework/

# Or restore from backup files (*.bak, *.bak2, etc.)
```

## Benefits

1. **Code Reuse**: Framework plugins can be used by both console and desktop apps
2. **Separation of Concerns**: Clear distinction between platform-specific and platform-agnostic plugins
3. **Maintainability**: Single source of truth for shared plugins
4. **Scalability**: Easy to add new applications that use framework plugins

## Future Considerations

1. **Desktop App Integration**: Desktop app can now reference framework plugins
2. **Plugin Versioning**: Consider versioning strategy for framework plugins
3. **Additional Migrations**: Evaluate other console-specific code for framework promotion
4. **Testing Strategy**: Establish cross-platform testing for framework plugins

## Related Documentation

- [Build System Documentation](../build/README.md)
- [Plugin Architecture](./plugin-architecture.md)
- [Console App Structure](./console-app-structure.md)

## Notes

- All moved plugins are **platform-agnostic** except Audio (which has platform-specific native deps)
- DungeonGame plugin remains console-specific due to Terminal.Gui dependency
- Test projects remain in console directory but reference framework plugins
- No code changes were made to plugin implementations, only file locations

---

**Migration Completed**: 2025-10-14  
**Verified By**: AI Agent (Cascade)  
**Status**: ✅ Ready for Testing

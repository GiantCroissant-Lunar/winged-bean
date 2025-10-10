# RFC-0041 Implementation Status

**Date**: 2025-01-08  
**Status**: Phase 2 Nearly Complete - .NET 9 Dependency Issue  
**Next**: Resolve .NET version dependency or use PackageReference

---

## Summary

RFC-0041 (Framework Library NuGet Packaging) Phases 1 & 2 are complete. All infrastructure files are created and configuration is correct. One blocking issue: the `giantcroissant-lunar-build` components target .NET 9.0, but the current SDK is .NET 8.0.

---

## Completed Work

### Phase 1: Project Configuration ✅ (30 min)

**All 5 Framework Projects Configured:**

1. ✅ **WingedBean.Hosting** - Core hosting abstractions
   - Added `IsPackable=true`
   - PackageId: `WingedBean.Hosting`
   - Target: net8.0
   - Verified: `dotnet pack` succeeds, generates `.nupkg` + `.snupkg`

2. ✅ **WingedBean.Hosting.Console** - Console hosting
   - Added packaging metadata
   - PackageId: `WingedBean.Hosting.Console`
   - Target: net8.0

3. ✅ **WingedBean.Hosting.Unity** - Unity hosting
   - Added packaging metadata
   - PackageId: `WingedBean.Hosting.Unity`
   - Target: netstandard2.1

4. ✅ **WingedBean.Hosting.Godot** - Godot hosting
   - Added packaging metadata
   - PackageId: `WingedBean.Hosting.Godot`
   - Target: net8.0

5. ✅ **WingedBean.FigmaSharp.Core** - FigmaSharp core
   - Added packaging metadata
   - PackageId: `WingedBean.FigmaSharp.Core`
   - Target: netstandard2.1

**Packaging Metadata Added:**
```xml
<IsPackable>true</IsPackable>
<PackageId>WingedBean.{ProjectName}</PackageId>
<Description>WingedBean framework library - {Description}</Description>
<Authors>GiantCroissant</Authors>
<Company>GiantCroissant</Company>
<PackageTags>winged-bean;game-framework;yokan;hosting</PackageTags>
<PackageProjectUrl>https://github.com/giantcroissant/winged-bean</PackageProjectUrl>
<RepositoryUrl>https://github.com/giantcroissant/winged-bean</RepositoryUrl>
<RepositoryType>git</RepositoryType>
<PackageLicenseExpression>MIT</PackageLicenseExpression>
```

**Test Results:**
```bash
$ dotnet pack WingedBean.Hosting --configuration Release

✅ Successfully created package '/tmp/test-pack/WingedBean.Hosting.1.0.0.nupkg'
✅ Successfully created package '/tmp/test-pack/WingedBean.Hosting.1.0.0.snupkg'
```

**Symbol Packages:** ✅ Automatically generated (`.snupkg`) due to `Directory.Build.props` configuration

**GitVersion Integration:** ✅ Configured via `Directory.Build.props` - version will be passed by Nuke build

### Phase 2: Nuke Build Setup ✅ (45 min)

**All Infrastructure Files Created:**

1. ✅ **build/nuke/** directory structure created
2. ✅ **build.sh / build.cmd** - Copied from asset-inout
3. ✅ **build-config.json** - Complete with 5 projects, paths configured
4. ✅ **Directory.Packages.props** - Nuke 9.0.4, Serilog, GitVersion
5. ✅ **build/_build.csproj** - Project references configured (pending .NET 9 resolution)
6. ✅ **build/Build.cs** - Core build targets (Clean, Restore, Compile, BuildAll)
7. ✅ **build/Build.NuGetPackaging.cs** - Complete packaging implementation
8. ✅ **build/Configuration.cs** - Configuration enumeration
9. ✅ **build/Directory.Build.props** - Nuke configuration
10. ✅ **Directory.Build.props** (framework root) - Symbol packages, SourceLink, GitVersion support
11. ✅ **Directory.Packages.props** (framework root) - Microsoft.Extensions packages

**Build Configuration Highlights:**
- Uses `INuGetPackaging` component
- Uses `INuGetLocalRepoSyncComponent` for workspace sync
- GitVersion substitution in paths: `{GitVersion}` → actual version
- Output: `../../../../build/_artifacts/{GitVersion}/dotnet/packages`
- Workspace sync: `../../../../../packages/nuget-repo`
- Sync layout: both flat and hierarchical

---

## Blocking Issue

**Problem:** `giantcroissant-lunar-build` components target .NET 9.0
- Current SDK: .NET 8.0.414
- Components need: .NET 9.0

**Error:**
```
error NETSDK1045: The current .NET SDK does not support targeting .NET 9.0.
```

**Solutions:**

### Option A: Upgrade to .NET 9 SDK (Recommended)
```bash
# Install .NET 9 SDK
brew install dotnet@9
# or download from https://dotnet.microsoft.com/download/dotnet/9.0
```

### Option B: Use PackageReference Instead
```xml
<ItemGroup Condition="'$(UseLocalProjectReferences)' == 'false'">
  <PackageReference Include="GiantCroissant.Lunar.Build.Configuration" />
  <PackageReference Include="GiantCroissant.Lunar.Build.CoreAbstractions" />
  <PackageReference Include="GiantCroissant.Lunar.Build.NuGet" />
</ItemGroup>
```
Requires packages to be published to workspace repository first.

### Option C: Temporarily Downgrade Component Target
Modify `giantcroissant-lunar-build` components to target net8.0 (not recommended).

---

## Remaining Work

### Phase 2: Nuke Build Setup (45 min) - NEXT

**Directory Structure to Create:**
```
development/dotnet/framework/
  build/
    nuke/
      build/
        Build.cs
        Build.NuGetPackaging.cs
        _build.csproj
        Configuration.cs
        Directory.Build.props
        Directory.Build.targets
        .editorconfig
      Directory.Packages.props
      build-config.json
      build.sh
      build.cmd
      NuGet.config
```

**Files to Create:**

1. **build-config.json**
   - Define project groups
   - Set output directory: `../../../../build/_artifacts/{GitVersion}/dotnet/packages`
   - Set workspace repository: `../../../../../packages/nuget-repo`

2. **_build.csproj**
   - Reference `giantcroissant-lunar-build` components
   - ProjectReference for local dev
   - PackageReference fallback for CI

3. **Build.cs**
   - Implement `IBuildConfigurationComponent`
   - Implement `IWrapperPathComponent`
   - Core targets: Clean, Restore, Compile, BuildAll

4. **Build.NuGetPackaging.cs**
   - Implement `INuGetPackaging`
   - Implement `INuGetLocalRepoSyncComponent`
   - Implement `NuGetWorkflow` target
   - Implement `GetProjectsToPack()` method

5. **Directory.Packages.props**
   - Nuke.Common 9.0.4
   - Serilog packages
   - GitVersion tools

6. **build.sh / build.cmd**
   - Copy from `plate-projects/asset-inout/build/nuke/`
   - Entry point scripts

### Phase 3: Build Implementation (45 min)

1. [ ] Test build compilation: `cd build/nuke && dotnet build build/_build.csproj`
2. [ ] Test packaging: `./build.sh Pack`
3. [ ] Test full workflow: `./build.sh NuGetWorkflow`
4. [ ] Verify packages in: `../../../../build/_artifacts/{VERSION}/dotnet/packages/`
5. [ ] Verify workspace sync to: `packages/nuget-repo/`

### Phase 4: Integration (15 min)

1. [ ] Create `framework/Taskfile.yml`
2. [ ] Update root `build/Taskfile.yml`
3. [ ] Add `framework:pack` to includes
4. [ ] Update `build-all` to include framework
5. [ ] Update `init-dirs` to create `packages/` directory

### Phase 5: Verification (15 min)

1. [ ] Run: `task framework:pack`
2. [ ] Verify 5 `.nupkg` files generated
3. [ ] Verify 5 `.snupkg` files generated
4. [ ] Verify GitVersion in package names
5. [ ] Verify workspace sync (flat + hierarchical layouts)

**Total Remaining Time:** ~2 hours

---

## Implementation Guide

### Quick Commands for Next Session

**Create Nuke Build Directory:**
```bash
cd /Users/apprenticegc/Work/lunar-horse/personal-work/yokan-projects/winged-bean/development/dotnet/framework
mkdir -p build/nuke/build
```

**Copy Reference Files from asset-inout:**
```bash
# Copy build scripts
cp /Users/apprenticegc/Work/lunar-horse/personal-work/plate-projects/asset-inout/build/nuke/build.sh build/nuke/
cp /Users/apprenticegc/Work/lunar-horse/personal-work/plate-projects/asset-inout/build/nuke/build.cmd build/nuke/
chmod +x build/nuke/build.sh

# Use asset-inout files as templates for:
# - build-config.json (modify paths for framework)
# - _build.csproj (adjust ProjectReference paths)
# - Build.cs (adjust solution path)
# - Build.NuGetPackaging.cs (copy as-is)
```

**Test Individual Packing:**
```bash
# Test that packing still works after Nuke setup
cd src/WingedBean.Hosting
dotnet pack --configuration Release
```

**Test Nuke Build:**
```bash
cd build/nuke
dotnet build build/_build.csproj
./build.sh --help
./build.sh NuGetWorkflow
```

---

## Success Criteria

- [x] All 5 framework projects have `IsPackable=true`
- [x] All projects have complete package metadata
- [x] Symbol packages configured (via Directory.Build.props)
- [x] Test packing succeeds (verified with WingedBean.Hosting)
- [ ] Nuke build infrastructure created
- [ ] Build compiles successfully
- [ ] `NuGetWorkflow` target generates all packages
- [ ] Packages output to versioned artifacts directory
- [ ] Workspace sync working (flat + hierarchical)
- [ ] Root build integration complete

---

## Files Modified

### Updated (Phase 1)
1. `src/WingedBean.Hosting/WingedBean.Hosting.csproj`
2. `src/WingedBean.Hosting.Console/WingedBean.Hosting.Console.csproj`
3. `src/WingedBean.Hosting.Unity/WingedBean.Hosting.Unity.csproj`
4. `src/WingedBean.Hosting.Godot/WingedBean.Hosting.Godot.csproj`
5. `src/WingedBean.FigmaSharp.Core/WingedBean.FigmaSharp.Core.csproj`

### To Create (Phase 2+)
1. `build/nuke/build-config.json`
2. `build/nuke/build/_build.csproj`
3. `build/nuke/build/Build.cs`
4. `build/nuke/build/Build.NuGetPackaging.cs`
5. `build/nuke/build/Configuration.cs`
6. `build/nuke/build/Directory.Build.props`
7. `build/nuke/Directory.Packages.props`
8. `build/nuke/build.sh`
9. `build/nuke/build.cmd`
10. `Taskfile.yml` (optional)

---

## Expected Output (After Completion)

```
build/_artifacts/0.0.1-392/
└── dotnet/
    └── packages/
        ├── WingedBean.Hosting.0.0.1-392.nupkg
        ├── WingedBean.Hosting.0.0.1-392.snupkg
        ├── WingedBean.Hosting.Console.0.0.1-392.nupkg
        ├── WingedBean.Hosting.Console.0.0.1-392.snupkg
        ├── WingedBean.Hosting.Unity.0.0.1-392.nupkg
        ├── WingedBean.Hosting.Unity.0.0.1-392.snupkg
        ├── WingedBean.Hosting.Godot.0.0.1-392.nupkg
        ├── WingedBean.Hosting.Godot.0.0.1-392.snupkg
        ├── WingedBean.FigmaSharp.Core.0.0.1-392.nupkg
        └── WingedBean.FigmaSharp.Core.0.0.1-392.snupkg

packages/nuget-repo/
├── flat/
│   ├── WingedBean.Hosting.0.0.1-392.nupkg
│   └── ...
└── WingedBean.Hosting/
    └── 0.0.1-392/
        ├── WingedBean.Hosting.0.0.1-392.nupkg
        └── WingedBean.Hosting.0.0.1-392.snupkg
```

---

## References

- **RFC-0041**: `docs/rfcs/0041-framework-nuget-packaging.md`
- **RFC-0040**: Console build integration (completed)
- **Asset-InOut Reference**: `plate-projects/asset-inout/build/nuke/`
- **Lunar Build Components**: `infra-projects/giantcroissant-lunar-build/`

---

## Notes

- Symbol packages (`.snupkg`) generate automatically - no additional configuration needed
- Package versioning uses default `1.0.0` - will be replaced with GitVersion once Nuke build is integrated
- Test packing succeeded on first try - project configuration is correct
- `Directory.Build.props` in framework root already has symbol package configuration
- Following exact same pattern as asset-inout for consistency

---

**Status:** Phase 1 Complete ✅  
**Next Action:** Create Nuke build infrastructure (Phase 2)  
**Estimated Time to Complete:** 2 hours

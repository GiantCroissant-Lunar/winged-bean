---
id: RFC-0041
title: Framework Library NuGet Packaging
status: Proposed
category: infra, build, packaging
created: 2025-01-08
updated: 2025-01-08
priority: P2
effort: 2-3 hours
---

# RFC-0041: Framework Library NuGet Packaging

**Status:** Proposed  
**Date:** 2025-01-08  
**Author:** Development Team  
**Category:** infra, build, packaging  
**Priority:** MEDIUM (P2)  
**Estimated Effort:** 2-3 hours  
**Depends On:** RFC-0040 (Nuke Build Component Integration)

---

## Summary

Enable NuGet package generation for WingedBean framework libraries (`WingedBean.Hosting.*`, `WingedBean.FigmaSharp.Core`) using the same `giantcroissant-lunar-build` infrastructure successfully demonstrated in `plate-projects/asset-inout`. Packages will be versioned with GitVersion and synced to the workspace NuGet repository.

---

## Motivation

### Current Problems

1. **No Distribution Mechanism** - Framework libraries cannot be distributed or consumed as NuGet packages
2. **Manual Assembly References** - Projects must reference framework assemblies directly, not via NuGet
3. **No Versioning** - Framework libraries not versioned alongside console application
4. **No Workspace Integration** - Cannot use framework packages in other workspace projects
5. **Inconsistent Build** - Framework has no build automation (no Taskfile, no Nuke)
6. **Missing from CI** - Framework libraries not included in CI pipeline

### Why NuGet Packaging?

**Distribution Benefits:**
- Framework libraries can be consumed as NuGet packages by Unity projects, Godot projects, and other consumers
- Proper versioning tied to GitVersion (same as console app)
- Workspace-level NuGet repository for local consumption
- Ready for publishing to NuGet.org or private feeds

**Build Consistency:**
- Same build infrastructure as asset-inout (proven pattern)
- Component-based Nuke build with `INuGetPackaging`
- Automated package generation on every build
- Integrated with existing workspace conventions

### Reference Implementation

`plate-projects/asset-inout/build/nuke` demonstrates successful use of:
- `INuGetPackaging` component for package generation
- `INuGetLocalRepoSyncComponent` for workspace repository sync
- `build-config.json` with `projectGroups` for multi-library packaging
- ProjectReference to `giantcroissant-lunar-build` components
- GitVersion-aware artifact paths: `build/_artifacts/{GitVersion}/nuget-packages`

---

## Proposal

### 1. Configure Framework Projects for Packaging

Add packaging metadata to each `.csproj` in `development/dotnet/framework/src/`:

```xml
<PropertyGroup>
  <!-- Packaging -->
  <IsPackable>true</IsPackable>
  <PackageId>WingedBean.$(MSBuildProjectName)</PackageId>
  <Description>WingedBean framework library - $(MSBuildProjectName)</Description>
  <Authors>GiantCroissant</Authors>
  <Company>GiantCroissant</Company>
  <PackageTags>winged-bean;game-framework;yokan;hosting</PackageTags>
  <PackageProjectUrl>https://github.com/giantcroissant/winged-bean</PackageProjectUrl>
  <RepositoryUrl>https://github.com/giantcroissant/winged-bean</RepositoryUrl>
  <RepositoryType>git</RepositoryType>
  <PackageLicenseExpression>MIT</PackageLicenseExpression>
  
  <!-- Symbol packages already configured in Directory.Build.props -->
  <!-- <IncludeSymbols>true</IncludeSymbols> -->
  <!-- <SymbolPackageFormat>snupkg</SymbolPackageFormat> -->
</PropertyGroup>
```

**Projects to Configure:**
- `WingedBean.Hosting/WingedBean.Hosting.csproj`
- `WingedBean.Hosting.Console/WingedBean.Hosting.Console.csproj`
- `WingedBean.Hosting.Unity/WingedBean.Hosting.Unity.csproj`
- `WingedBean.Hosting.Godot/WingedBean.Hosting.Godot.csproj`
- `WingedBean.FigmaSharp.Core/WingedBean.FigmaSharp.Core.csproj`

### 2. Create Nuke Build Infrastructure

**Location:** `development/dotnet/framework/build/nuke/`

**Directory Structure:**
```
framework/
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

### 3. Create build-config.json

**Location:** `development/dotnet/framework/build/nuke/build-config.json`

```json
{
  "$schema": "https://json-schema.org/draft/2020-12/schema",
  "description": "Build configuration for WingedBean Framework Libraries - RFC-0041",
  "projectType": "multi-group-build",

  "paths": {
    "projectDiscoveryPaths": [
      "../../src/**/*.csproj"
    ],
    "sourceDirectory": "../../src"
  },

  "projectGroups": [
    {
      "name": "hosting-framework",
      "buildType": "dotnet-library",
      "sourceDirectory": "../../src",
      "outputs": [
        {
          "type": "nuget-package",
          "directory": "../../../../build/_artifacts/{GitVersion}/dotnet/packages"
        }
      ],
      "explicitProjects": [
        "WingedBean.Hosting/WingedBean.Hosting.csproj",
        "WingedBean.Hosting.Console/WingedBean.Hosting.Console.csproj",
        "WingedBean.Hosting.Unity/WingedBean.Hosting.Unity.csproj",
        "WingedBean.Hosting.Godot/WingedBean.Hosting.Godot.csproj",
        "WingedBean.FigmaSharp.Core/WingedBean.FigmaSharp.Core.csproj"
      ],
      "buildConfiguration": {
        "configuration": "Release",
        "targetFramework": "net8.0"
      }
    }
  ],

  "globalPaths": {
    "artifactsDirectory": "../../../../build/_artifacts",
    "nugetRepositoryDirectory": "../../../../../packages/nuget-repo"
  },

  "nuget": {
    "localNugetRepositories": ["../../../../../packages/nuget-repo"],
    "syncLayout": "both",
    "outputDirectory": "../../../../build/_artifacts/{GitVersion}/dotnet/packages"
  },

  "reporting": {
    "enabled": false,
    "outputDirectory": "../../../../build/_artifacts/{GitVersion}/reports"
  }
}
```

### 4. Create Build.cs

**Location:** `development/dotnet/framework/build/nuke/build/Build.cs`

```csharp
using System;
using System.Linq;
using Nuke.Common;
using Nuke.Common.IO;
using Nuke.Common.Tools.DotNet;
using Nuke.Common.Tools.GitVersion;
using Serilog;
using Lunar.Build.Configuration;
using Lunar.Build.Components;
using static Nuke.Common.EnvironmentInfo;
using static Nuke.Common.Tools.DotNet.DotNetTasks;

#nullable enable

/// <summary>
/// WingedBean Framework Build - NuGet Packaging (RFC-0041)
/// </summary>
partial class Build :
    IBuildConfigurationComponent,
    IWrapperPathComponent
{
    public static int Main() => Execute<Build>(x => x.NuGetWorkflow);

    [Parameter("Configuration to build - Default is 'Release'")]
    readonly Configuration Configuration = Configuration.Release;

    [GitVersion(NoFetch = true)]
    GitVersion _gitVersion = null!;

    public GitVersion GitVersion => _gitVersion;

    // Solution path
    AbsolutePath SolutionPath => RootDirectory.Parent.Parent / "Framework.sln";

    // IWrapperPathComponent implementation
    public string[] ProjectConfigIdentifiers => new[]
    {
        "winged-bean.framework",
        "yokan-framework"
    };

    [Parameter("NUKE root directory override from wrapper script", Name = "wrapper-nuke-root")]
    readonly string? _wrapperNukeRootParam;

    [Parameter("Config path override from wrapper script", Name = "wrapper-config-path")]
    readonly string? _wrapperConfigPathParam;

    [Parameter("Script directory from wrapper script", Name = "wrapper-script-dir")]
    readonly string? _wrapperScriptDirParam;

    public string? WrapperNukeRootParam => _wrapperNukeRootParam;
    public string? WrapperConfigPathParam => _wrapperConfigPathParam;
    public string? WrapperScriptDirParam => _wrapperScriptDirParam;

    // IBuildConfigurationComponent implementation
    public string BuildConfigPath => 
        ((IWrapperPathComponent)this).EffectiveRootDirectory / "build" / "nuke" / "build-config.json";

    AbsolutePath ArtifactsDirectory => 
        ((IWrapperPathComponent)this).EffectiveRootDirectory.Parent.Parent / "build" / "_artifacts" / GitVersion.SemVer;

    Target Clean => _ => _
        .Before(Restore)
        .Executes(() =>
        {
            Log.Information("Cleaning solution...");
            if (SolutionPath.FileExists())
            {
                DotNetClean(s => s
                    .SetProject(SolutionPath)
                    .SetConfiguration(Configuration));
            }
        });

    Target Restore => _ => _
        .Executes(() =>
        {
            Log.Information("Restoring solution...");
            if (SolutionPath.FileExists())
            {
                DotNetRestore(s => s
                    .SetProjectFile(SolutionPath));
            }
        });

    Target Compile => _ => _
        .DependsOn(Restore)
        .Executes(() =>
        {
            Log.Information("Building solution...");
            if (SolutionPath.FileExists())
            {
                DotNetBuild(s => s
                    .SetProjectFile(SolutionPath)
                    .SetConfiguration(Configuration)
                    .EnableNoRestore());
            }
        });

    Target BuildAll => _ => _
        .Description("Build all framework projects")
        .DependsOn(Compile)
        .Executes(() =>
        {
            Log.Information("Build completed for version {Version}", GitVersion.SemVer);
            Log.Information("Artifacts directory: {Directory}", ArtifactsDirectory);
        });
}

/// <summary>
/// Simple service provider for component coordination
/// </summary>
public class SimpleServiceProvider : IServiceProvider
{
    public object? GetService(Type serviceType) => null;
}
```

### 5. Create Build.NuGetPackaging.cs

**Location:** `development/dotnet/framework/build/nuke/build/Build.NuGetPackaging.cs`

```csharp
using System.Linq;
using Nuke.Common;
using Nuke.Common.IO;
using Serilog;
using Lunar.Build.NuGet;
using Lunar.Build.Components;
using Lunar.Build.Configuration;

#nullable enable

/// <summary>
/// WingedBean Framework Build - NuGet Packaging Implementation (RFC-0041)
/// </summary>
partial class Build : INuGetPackaging, INuGetLocalRepoSyncComponent
{
    /// <summary>
    /// Override NuGetRepository to use GitVersion artifacts path
    /// </summary>
    public AbsolutePath NuGetRepository => GetNuGetOutputDirectoryFromConfig();

    /// <summary>
    /// Override BuiltPackagesDirectory for workspace sync
    /// </summary>
    public AbsolutePath BuiltPackagesDirectory => GetNuGetOutputDirectoryFromConfig();

    /// <summary>
    /// Complete NuGet workflow with workspace sync
    /// </summary>
    public Target NuGetWorkflow => _ => _
        .Description("Complete NuGet workflow - build, pack, and sync to workspace - RFC-0041")
        .DependsOn(BuildAll)
        .DependsOn(((INuGetPackaging)this).Pack)
        .DependsOn(((INuGetLocalRepoSyncComponent)this).SyncNugetPackagesToLocalFeeds)
        .Executes(() => {
            Log.Information("✅ RFC-0041: NuGet workflow completed!");
            Log.Information("📦 Packages: {Path}", NuGetRepository);
            Log.Information("📦 Workspace: {Path}", 
                ((IBuildConfigurationComponent)this).GetConfigurableValue(
                    "globalPaths.nugetRepositoryDirectory", null, "packages/nuget-repo"));
        });

    /// <summary>
    /// Get projects to pack - discovers from configuration
    /// </summary>
    public System.Collections.Generic.List<AbsolutePath> GetProjectsToPack()
    {
        var projectStrings = DiscoverProjectsFromConfiguration();
        var allProjects = projectStrings
            .Where(p => !string.IsNullOrEmpty(p))
            .Select(p => (AbsolutePath)p)
            .Where(p => p.FileExists())
            .ToList();

        Log.Information("📋 RFC-0041: Projects to pack:");
        foreach (var project in allProjects)
        {
            Log.Information("  ✅ {ProjectName}", project.NameWithoutExtension);
        }

        return allProjects;
    }

    /// <summary>
    /// Get NuGet output directory with GitVersion support
    /// </summary>
    private AbsolutePath GetNuGetOutputDirectoryFromConfig()
    {
        try
        {
            var configValue = ((IBuildConfigurationComponent)this).GetConfigurableValue(
                "nuget.outputDirectory",
                null,
                "../../../../build/_artifacts/{GitVersion}/dotnet/packages");

            if (!string.IsNullOrEmpty(configValue) && GitVersion != null && configValue.Contains("{GitVersion}"))
            {
                configValue = configValue.Replace("{GitVersion}", GitVersion.SemVer);
            }

            var root = ((IWrapperPathComponent)this).EffectiveRootDirectory;
            var resolved = root / configValue;
            Log.Information("📂 NuGet output: {Path}", resolved);
            return resolved;
        }
        catch (System.Exception ex)
        {
            Log.Warning("Failed to get NuGet output directory: {Message}", ex.Message);
            var root = ((IWrapperPathComponent)this).EffectiveRootDirectory;
            return root / "../../../../build/_artifacts/local/dotnet/packages";
        }
    }

    /// <summary>
    /// Discover projects from configuration
    /// </summary>
    private string[] DiscoverProjectsFromConfiguration()
    {
        var projects = new System.Collections.Generic.List<string>();
        try
        {
            var config = ((IBuildConfigurationComponent)this).BuildConfig;

            if (config.ProjectType == "multi-group-build")
            {
                var all = config.ProjectGroups?.SelectMany(group =>
                {
                    if (group.ExplicitProjects == null || !group.ExplicitProjects.Any())
                    {
                        return System.Linq.Enumerable.Empty<string>();
                    }

                    if (!string.IsNullOrEmpty(group.SourceDirectory))
                    {
                        var effectiveRoot = ((IWrapperPathComponent)this).EffectiveRootDirectory;
                        return group.ExplicitProjects.Select(p => (effectiveRoot / group.SourceDirectory / p).ToString());
                    }
                    return group.ExplicitProjects;
                }).ToArray() ?? System.Array.Empty<string>();

                projects.AddRange(all);
            }
        }
        catch (System.Exception ex)
        {
            Log.Warning("Failed to discover projects from configuration: {Message}", ex.Message);
        }

        return projects.ToArray();
    }
}
```

### 6. Create _build.csproj

**Location:** `development/dotnet/framework/build/nuke/build/_build.csproj`

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net9.0</TargetFramework>
    <RootNamespace></RootNamespace>
    <NoWarn>CS0649;CS0169;CA1050;CA1822;CA2211;IDE1006</NoWarn>
    <NukeRootDirectory>..</NukeRootDirectory>
    <NukeScriptDirectory>..</NukeScriptDirectory>
    <NukeTelemetryVersion>1</NukeTelemetryVersion>
    <IsPackable>false</IsPackable>
    <UseLocalProjectReferences Condition="'$(UseLocalProjectReferences)' == ''">true</UseLocalProjectReferences>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Nuke.Common" />
    <PackageReference Include="Serilog" />
    <PackageReference Include="Serilog.Sinks.Console" />
  </ItemGroup>

  <ItemGroup>
    <PackageDownload Include="GitVersion.Tool" Version="[6.3.0]" />
  </ItemGroup>

  <!-- Prefer ProjectReference for local development -->
  <ItemGroup Condition="'$(UseLocalProjectReferences)' != 'false'">
    <ProjectReference Include="..\..\..\..\..\..\infra-projects\giantcroissant-lunar-build\build\nuke\components\Configuration\Lunar.Build.Configuration.csproj" />
    <ProjectReference Include="..\..\..\..\..\..\infra-projects\giantcroissant-lunar-build\build\nuke\components\CoreAbstractions\Lunar.Build.CoreAbstractions.csproj" />
    <ProjectReference Include="..\..\..\..\..\..\infra-projects\giantcroissant-lunar-build\build\nuke\components\NuGet\Lunar.Build.NuGet.csproj" />
  </ItemGroup>

  <!-- Fallback to PackageReference -->
  <ItemGroup Condition="'$(UseLocalProjectReferences)' == 'false'">
    <PackageReference Include="GiantCroissant.Lunar.Build.Configuration" />
    <PackageReference Include="GiantCroissant.Lunar.Build.CoreAbstractions" />
    <PackageReference Include="GiantCroissant.Lunar.Build.NuGet" />
  </ItemGroup>

</Project>
```

### 7. Create Directory.Packages.props

**Location:** `development/dotnet/framework/build/nuke/Directory.Packages.props`

```xml
<Project>
  <PropertyGroup>
    <ManagePackageVersionsCentrally>true</ManagePackageVersionsCentrally>
  </PropertyGroup>
  
  <ItemGroup>
    <!-- Nuke Framework -->
    <PackageVersion Include="Nuke.Common" Version="9.0.4" />
    
    <!-- Logging -->
    <PackageVersion Include="Serilog" Version="4.3.0" />
    <PackageVersion Include="Serilog.Sinks.Console" Version="6.0.0" />
    
    <!-- Tools -->
    <PackageVersion Include="GitVersion.Tool" Version="6.3.0" />
    <PackageVersion Include="GitVersion.MsBuild" Version="6.3.0" />
  </ItemGroup>
</Project>
```

### 8. Create build.sh and build.cmd

Copy from `plate-projects/asset-inout/build/nuke/build.sh` and `build.cmd`, adjusting paths if needed.

### 9. Create Taskfile.yml (Optional)

**Location:** `development/dotnet/framework/Taskfile.yml`

```yaml
version: '3'

vars:
  VERSION:
    sh: ../../../build/get-version.sh
  ARTIFACT_DIR: ../../../build/_artifacts/{{.VERSION}}/dotnet/packages

tasks:
  build:
    desc: "Build framework libraries"
    cmds:
      - dotnet build Framework.sln

  pack:
    desc: "Pack NuGet packages via Nuke"
    dir: build/nuke
    cmds:
      - ./build.sh NuGetWorkflow --no-logo

  clean:
    desc: "Clean build artifacts"
    cmds:
      - dotnet clean Framework.sln
      - rm -rf {{.ARTIFACT_DIR}}/*

  ci:
    desc: "Full CI pipeline for framework"
    cmds:
      - task: clean
      - task: pack

  list-packages:
    desc: "List generated NuGet packages"
    cmds:
      - ls -lh {{.ARTIFACT_DIR}}/
```

### 10. Integrate with Root Build

**Location:** `build/Taskfile.yml`

Add to includes:
```yaml
includes:
  framework:
    taskfile: ../development/dotnet/framework/Taskfile.yml
    dir: ../development/dotnet/framework
```

Update `init-dirs`:
```yaml
init-dirs:
  cmds:
    - mkdir -p {{.ARTIFACT_DIR}}/dotnet/{bin,packages,recordings,logs}
    # 'packages' directory added for framework NuGet packages
```

Update `build-all`:
```yaml
build-all:
  deps:
    - build-dotnet
    - build-web
    - build-pty
    - framework:pack  # Add framework packaging
```

---

## Expected Output Structure

After successful implementation:

```
build/_artifacts/0.0.1-392/
├── dotnet/
│   ├── bin/                         # Console app binaries (from RFC-0040)
│   ├── packages/                    # ⭐ Framework NuGet packages (RFC-0041)
│   │   ├── WingedBean.Hosting.0.0.1-392.nupkg
│   │   ├── WingedBean.Hosting.0.0.1-392.snupkg
│   │   ├── WingedBean.Hosting.Console.0.0.1-392.nupkg
│   │   ├── WingedBean.Hosting.Console.0.0.1-392.snupkg
│   │   ├── WingedBean.Hosting.Unity.0.0.1-392.nupkg
│   │   ├── WingedBean.Hosting.Unity.0.0.1-392.snupkg
│   │   ├── WingedBean.Hosting.Godot.0.0.1-392.nupkg
│   │   ├── WingedBean.Hosting.Godot.0.0.1-392.snupkg
│   │   ├── WingedBean.FigmaSharp.Core.0.0.1-392.nupkg
│   │   └── WingedBean.FigmaSharp.Core.0.0.1-392.snupkg
│   ├── test-results/                # Test results (from RFC-0040)
│   ├── recordings/
│   └── logs/
├── web/
├── pty/
└── reports/                         # Component reports (from RFC-0040)

packages/nuget-repo/                 # ⭐ Workspace NuGet repository
├── flat/                            # Flat layout
│   ├── WingedBean.Hosting.0.0.1-392.nupkg
│   ├── WingedBean.Hosting.Console.0.0.1-392.nupkg
│   └── ...
└── WingedBean.Hosting/              # Hierarchical layout
    └── 0.0.1-392/
        ├── WingedBean.Hosting.0.0.1-392.nupkg
        └── WingedBean.Hosting.0.0.1-392.snupkg
```

---

## Implementation Plan

### Phase 1: Project Configuration (30 min)

1. [ ] Add packaging metadata to all framework `.csproj` files
2. [ ] Verify `Directory.Build.props` has symbol package configuration
3. [ ] Test basic `dotnet pack` on individual projects

### Phase 2: Nuke Build Setup (45 min)

1. [ ] Create `framework/build/nuke/` directory structure
2. [ ] Copy build scripts from asset-inout
3. [ ] Create `build-config.json` with project groups
4. [ ] Create `_build.csproj` with component references
5. [ ] Create `Directory.Packages.props`
6. [ ] Test build: `cd build/nuke && dotnet build build/_build.csproj`

### Phase 3: Build Implementation (45 min)

1. [ ] Create `Build.cs` with core targets
2. [ ] Create `Build.NuGetPackaging.cs` with packaging logic
3. [ ] Test packaging: `./build.sh NuGetWorkflow`
4. [ ] Verify `.nupkg` files in artifacts directory
5. [ ] Verify workspace sync to `packages/nuget-repo/`

### Phase 4: Integration (15 min)

1. [ ] Create `framework/Taskfile.yml` (optional)
2. [ ] Update root `build/Taskfile.yml` with framework tasks
3. [ ] Test: `task framework:pack` from root
4. [ ] Test: `task build-all` includes framework

### Phase 5: Verification (15 min)

1. [ ] Run full build: `task build-all`
2. [ ] Verify 5 `.nupkg` files + 5 `.snupkg` files created
3. [ ] Verify packages synced to workspace repository
4. [ ] Test consuming a package in another project (optional)

**Total Estimated Time:** 2.5 hours

---

## Testing Strategy

### Unit Tests

- [ ] Verify project discovery from configuration
- [ ] Verify NuGet repository path resolution
- [ ] Verify GitVersion substitution in paths

### Integration Tests

- [ ] Build framework libraries: `./build.sh BuildAll`
- [ ] Pack NuGet packages: `./build.sh Pack`
- [ ] Full workflow: `./build.sh NuGetWorkflow`
- [ ] Verify package contents with `nuget verify`

### End-to-End Tests

- [ ] Full build from root: `task build-all`
- [ ] Verify all 10 package files (5 `.nupkg` + 5 `.snupkg`)
- [ ] Verify workspace sync (both flat and hierarchical)
- [ ] Test package restoration from workspace repository

---

## Success Criteria

- [ ] All 5 framework projects configured with `IsPackable=true`
- [ ] Nuke build infrastructure created and compiles
- [ ] `NuGetWorkflow` target successfully generates packages
- [ ] Packages output to `_artifacts/{VERSION}/dotnet/packages/`
- [ ] Packages synced to `packages/nuget-repo/` (flat + hierarchical)
- [ ] All packages include symbols (`.snupkg` files)
- [ ] GitVersion integration working (version in package names)
- [ ] Root build includes framework packaging
- [ ] Can consume packages from workspace repository

---

## Benefits

1. **Distribution Ready** - Framework libraries packaged for NuGet.org or private feeds
2. **Version Consistency** - Same GitVersion as console app
3. **Workspace Integration** - Local consumption via workspace NuGet repository
4. **Build Automation** - Automatic packaging on every build
5. **Component Reuse** - Uses proven `giantcroissant-lunar-build` infrastructure
6. **Symbol Support** - Debug symbols in separate `.snupkg` packages
7. **CI/CD Ready** - Easy integration with GitHub Actions or other CI

---

## Risks and Mitigations

### Risk 1: Path Complexity

**Risk:** Deeply nested paths may cause issues  
**Mitigation:** Follow asset-inout pattern exactly, test paths early

### Risk 2: Package Dependencies

**Risk:** Framework packages may have inter-dependencies  
**Mitigation:** Pack in correct order, verify `PackageReference` in `.csproj` files

### Risk 3: GitVersion Not Available

**Risk:** Build might fail if GitVersion not accessible  
**Mitigation:** Same setup as RFC-0040 (already working)

---

## Alternatives Considered

### Alternative 1: Manual Packaging

**Pros:** Simple, no build infrastructure needed  
**Cons:** Manual process, no automation, error-prone, no versioning

**Decision:** REJECTED - Automation is essential for consistency

### Alternative 2: Shared Build with Console

**Pros:** Single build entry point  
**Cons:** Tight coupling, harder to maintain, mixed concerns

**Decision:** REJECTED - Separation of concerns is better (follows asset-inout pattern)

### Alternative 3: Different Artifact Location

**Pros:** Could use separate artifacts directory  
**Cons:** Inconsistent with console app artifacts

**Decision:** REJECTED - Use shared `_artifacts/{VERSION}/dotnet/packages/` for consistency

---

## Future Enhancements

1. **Publishing to NuGet.org** - Add `Push` target with API key configuration
2. **Pre-release Packages** - Support alpha/beta/rc suffixes
3. **Package Validation** - Add NuGet package validation checks
4. **Dependency Analysis** - Automated dependency graph generation
5. **Package Documentation** - Generate package README from XML docs

---

## References

- **RFC-0040**: Nuke Build Component Integration (console build)
- **Asset-InOut Example**: `plate-projects/asset-inout/build/nuke/` (proven implementation)
- **Lunar Build NuGet Component**: `infra-projects/giantcroissant-lunar-build/build/nuke/components/NuGet/`
- **Investigation Document**: `NUGET-PACKAGING-STATUS.md` (analysis and planning)
- **Comparison Document**: `NUGET-PACKAGING-VS-RFC0040.md` (clarifies differences)

---

## Appendix A: Package Metadata Details

### Package IDs

- `WingedBean.Hosting` - Core hosting framework
- `WingedBean.Hosting.Console` - Console application hosting
- `WingedBean.Hosting.Unity` - Unity game engine hosting
- `WingedBean.Hosting.Godot` - Godot game engine hosting
- `WingedBean.FigmaSharp.Core` - FigmaSharp core library

### Package Dependencies

Framework packages may have inter-dependencies. Ensure correct `PackageReference` in `.csproj` files:

```xml
<ItemGroup>
  <!-- WingedBean.Hosting.Console depends on WingedBean.Hosting -->
  <PackageReference Include="WingedBean.Hosting" Version="$(Version)" />
</ItemGroup>
```

### Symbol Packages

Symbol packages (`.snupkg`) are automatically generated due to `Directory.Build.props` configuration:
```xml
<IncludeSymbols>true</IncludeSymbols>
<SymbolPackageFormat>snupkg</SymbolPackageFormat>
```

---

## Appendix B: Workspace NuGet Repository

### Local NuGet Feed Configuration

The workspace repository at `packages/nuget-repo/` serves as a local NuGet feed:

**Add to global `NuGet.Config`:**
```xml
<packageSources>
  <add key="lunar-horse-workspace" value="/Users/apprenticegc/Work/lunar-horse/personal-work/packages/nuget-repo/flat" />
</packageSources>
```

**Or project-specific `NuGet.Config`:**
```xml
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <packageSources>
    <clear />
    <add key="nuget.org" value="https://api.nuget.org/v3/index.json" />
    <add key="workspace" value="../../../../../packages/nuget-repo/flat" />
  </packageSources>
</configuration>
```

### Package Source Mapping

For more control, use package source mapping:
```xml
<packageSourceMapping>
  <packageSource key="nuget.org">
    <package pattern="*" />
  </packageSource>
  <packageSource key="workspace">
    <package pattern="WingedBean.*" />
    <package pattern="GiantCroissant.*" />
  </packageSource>
</packageSourceMapping>
```

---

**Status:** Ready for Implementation  
**Next Steps:** Begin Phase 1 (Project Configuration)

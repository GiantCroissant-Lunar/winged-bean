---
id: RFC-0040
title: Nuke Build Component Integration and Artifact Path Standardization
status: Proposed
category: infra, build, tooling, testing
created: 2025-01-08
updated: 2025-01-08
priority: P1
effort: 6-8 hours
---

# RFC-0040: Nuke Build Component Integration and Artifact Path Standardization

**Status:** Proposed  
**Date:** 2025-01-08  
**Author:** Development Team  
**Category:** infra, build, tooling, testing  
**Priority:** HIGH (P1)  
**Estimated Effort:** 6-8 hours

---

## Summary

Adopt `giantcroissant-lunar-build` Nuke build components for WingedBean Console to gain automatic test reporting, structured metrics collection, and multi-format report generation. This RFC also standardizes the artifact path format from `build/_artifacts/v{GitVersion}` to `build/_artifacts/{GitVersion}` for consistency with component configurations.

---

## Motivation

### Current Problems

1. **Manual test result handling** - No structured test metric collection
2. **No reporting infrastructure** - Test results only visible in console output
3. **Basic Nuke build** - Only Compile/Clean/Restore targets, no configuration-driven builds
4. **Path inconsistency** - Using `v{GitVersion}` prefix while components use `{GitVersion}` token
5. **Missing CI artifacts** - No machine-readable test results for GitHub Actions
6. **No quality gates** - Manual verification of test coverage and failure thresholds
7. **Duplicate effort** - Building reporting from scratch when reusable components exist

### Why Nuke Build Components?

**Existing infrastructure from `giantcroissant-lunar-build`:**
- `IBuildConfigurationComponent` - Configuration-driven builds from `build-config.json`
- `INfunReportComponent` - Multi-format report generation (JSON, XML, YAML, Markdown)
- `IWrapperPathComponent` - Consistent path resolution across wrapper scripts
- `INuGetLocalRepoSyncComponent` - Local NuGet repository synchronization
- `CodeQualityReportProvider` - **Automatic test metric collection** from TRX files

**Reference implementation:** `plate-projects/asset-inout/build/nuke` demonstrates successful adoption.

### What We Get Automatically

When we use the components, **test reporting is built-in**:

```
build/_artifacts/{GitVersion}/
├── dotnet/
│   ├── test-results/
│   │   ├── test-results.trx          # ⭐ Parsed automatically
│   │   ├── test-report.html
│   │   └── coverage/coverage.cobertura.xml
│   └── bin/
└── reports/                           # ⭐ Generated automatically
    ├── components/
    │   └── codequality/
    │       ├── component-report.json
    │       ├── analysis-report.json
    │       ├── testing-report.json    # ⭐ Test metrics here
    │       └── formatting-report.json
    ├── TestMetrics/                   # ⭐ Multi-format via NFunReport
    │   ├── TestMetrics.json
    │   ├── TestMetrics.xml
    │   ├── TestMetrics.yaml
    │   └── TestMetrics.md
    └── aggregated-report.json
```

---

## Proposal

### 1. Artifact Path Standardization

**Change:** `build/_artifacts/v{GitVersion}` → `build/_artifacts/{GitVersion}`

**Rationale:**
- Component configurations use `{GitVersion}` token without `v` prefix
- Example from `asset-inout/build-config.json`: `"outputDirectory": "build/_artifacts/{GitVersion}/reports"`
- Consistency across all lunar-build-based projects
- Simpler token replacement logic

**Impact:**
- Update `build/Taskfile.yml` variable: `ARTIFACT_DIR: _artifacts/{{.VERSION}}`
- Update `build/get-version.sh` output (if it prepends `v`)
- Update references in documentation
- Clean old `v*` directories after migration

**Migration:**
```bash
# One-time cleanup
cd build/_artifacts
for dir in v0.0.1-*; do
  new_name="${dir#v}"  # Remove 'v' prefix
  if [ -d "$dir" ] && [ ! -d "$new_name" ]; then
    mv "$dir" "$new_name"
  fi
done
```

### 2. Create build-config.json

**Location:** `build/nuke/build-config.json`

**Contents:**
```json
{
  "$schema": "https://json-schema.org/draft/2020-12/schema",
  "description": "Build configuration for WingedBean Console using Nuke components",
  "projectType": "multi-group-build",

  "paths": {
    "projectDiscoveryPaths": [
      "../../development/dotnet/console/src/**/*.csproj",
      "../../development/dotnet/console/tests/**/*.csproj"
    ],
    "sourceDirectory": "../../development/dotnet/console/src",
    "packageDirectory": "../_artifacts/{GitVersion}/nuget-packages"
  },

  "projectGroups": [
    {
      "name": "console-host",
      "buildType": "dotnet-console",
      "sourceDirectory": "../../development/dotnet/console/src",
      "outputs": [
        {
          "type": "console-executable",
          "directory": "../_artifacts/{GitVersion}/dotnet/bin"
        }
      ],
      "buildConfiguration": {
        "configuration": "Debug",
        "targetFramework": "net8.0"
      }
    },
    {
      "name": "console-tests",
      "buildType": "dotnet-test",
      "sourceDirectory": "../../development/dotnet/console/tests",
      "outputs": [
        {
          "type": "test-results",
          "directory": "../_artifacts/{GitVersion}/dotnet/test-results"
        }
      ]
    }
  ],

  "globalPaths": {
    "artifactsDirectory": "../_artifacts",
    "nugetRepositoryDirectory": "../../../packages/nuget-repo"
  },

  "codeQuality": {
    "outputDirectory": "../_artifacts/{GitVersion}/dotnet/test-results",
    "failOnIssues": false,
    "solutionFile": "../../development/dotnet/console/Console.sln",
    "thresholds": {
      "maxWarnings": 100,
      "maxErrors": 0,
      "minCoverage": 80.0
    },
    "coverage": {
      "enable": true,
      "format": "cobertura",
      "threshold": 80.0
    }
  },

  "reporting": {
    "enabled": true,
    "outputDirectory": "../_artifacts/{GitVersion}/reports"
  },

  "reportProviders": []
}
```

### 3. Upgrade Build.cs to Use Components

**Update:** `build/nuke/build/Build.cs`

**Before (Basic):**
```csharp
class Build : NukeBuild
{
    [Solution("../../development/dotnet/console/Console.sln")]
    readonly Solution Solution;
    
    Target Compile => _ => _
        .DependsOn(Restore)
        .Executes(() => { DotNetBuild(...); });
}
```

**After (Component-Based):**
```csharp
using System;
using Nuke.Common;
using Nuke.Common.Tools.DotNet;
using Nuke.Common.Tools.GitVersion;
using Nuke.Common.IO;
using Serilog;
using Lunar.Build.Configuration;
using Lunar.Build.Components;
using Lunar.Build.CodeQuality.Reporting;
using Lunar.Build.Abstractions.Services;
using Lunar.NfunReport.MNuke.Components;
using static Nuke.Common.Tools.DotNet.DotNetTasks;

namespace WingedBean.Build.MNuke;

/// <summary>
/// WingedBean Console Build - Using giantcroissant-lunar-build components
/// RFC-0040: Component integration for automatic test reporting
/// </summary>
partial class Build :
    INfunReportComponent,              // Multi-format reporting
    IBuildConfigurationComponent,       // Config-driven builds
    IWrapperPathComponent              // Path resolution
{
    [Parameter("Configuration to build")]
    readonly Configuration Configuration = IsLocalBuild ? Configuration.Debug : Configuration.Release;

    [GitVersion(NoFetch = true)]
    GitVersion _gitVersion = null!;
    public GitVersion GitVersion => _gitVersion;

    // IWrapperPathComponent implementation
    public string[] ProjectConfigIdentifiers => new[] { "winged-bean", "console-dungeon" };

    [Parameter("Wrapper parameters", Name = "wrapper-nuke-root")]
    readonly string? _wrapperNukeRootParam;
    [Parameter(Name = "wrapper-config-path")]
    readonly string? _wrapperConfigPathParam;
    [Parameter(Name = "wrapper-script-dir")]
    readonly string? _wrapperScriptDirParam;

    public string? WrapperNukeRootParam => _wrapperNukeRootParam;
    public string? WrapperConfigPathParam => _wrapperConfigPathParam;
    public string? WrapperScriptDirParam => _wrapperScriptDirParam;

    // IBuildConfigurationComponent implementation
    public string BuildConfigPath => 
        ((IWrapperPathComponent)this).EffectiveRootDirectory / "build" / "nuke" / "build-config.json";

    /// <summary>
    /// Build all console projects
    /// </summary>
    public Target BuildAll => _ => _
        .Description("Build console host and plugins")
        .Executes(() =>
        {
            var projects = ((IProjectDiscoveryComponent)this).GetProjectsToBuild();
            
            Log.Information("🏗️ Building {Count} projects", projects.Count);
            
            foreach (var project in projects)
            {
                DotNetBuild(s => s
                    .SetProjectFile(project)
                    .SetConfiguration(Configuration));
            }
            
            Log.Information("✅ All projects built successfully");
        });

    /// <summary>
    /// Run tests with structured output to artifacts directory
    /// </summary>
    public Target Test => _ => _
        .Description("Run tests and collect metrics to versioned artifacts")
        .DependsOn(BuildAll)
        .Executes(() =>
        {
            var version = GitVersion?.SemVer ?? "local";
            var artifactDir = ((IBuildConfigurationComponent)this).GetConfigurablePath(
                "globalPaths.artifactsDirectory",
                null,
                "../_artifacts");
            
            // Use {GitVersion} token, not v{GitVersion}
            var testResultsDir = artifactDir / version / "dotnet" / "test-results";
            var coverageDir = testResultsDir / "coverage";
            
            testResultsDir.CreateDirectory();
            coverageDir.CreateDirectory();

            Log.Information("📋 Running tests, output to: {Dir}", testResultsDir);

            DotNetTest(s => s
                .SetProjectFile("../../development/dotnet/console/Console.sln")
                .SetConfiguration(Configuration)
                .SetLoggers(
                    $"trx;LogFileName={testResultsDir}/test-results.trx",
                    $"html;LogFileName={testResultsDir}/test-report.html")
                .SetResultsDirectory(testResultsDir)
                .SetProperty("CollectCoverage", "true")
                .SetProperty("CoverletOutputFormat", "cobertura")
                .SetProperty("CoverletOutput", $"{coverageDir}/"));
                
            Log.Information("✅ Test results saved to {Dir}", testResultsDir);
        });

    /// <summary>
    /// Generate component reports (includes test metrics from CodeQuality component)
    /// </summary>
    public Target GenerateComponentReports => _ => _
        .Description("Generate reports from build components (includes test metrics)")
        .DependsOn(Test)
        .Executes(async () =>
        {
            var version = GitVersion?.SemVer ?? "local";
            var reportOutput = ((IBuildConfigurationComponent)this).GetConfigurablePath(
                "reporting.outputDirectory",
                null,
                $"../_artifacts/{version}/reports");

            Log.Information("📊 Generating component reports to: {Dir}", reportOutput);
            
            reportOutput.CreateDirectory();
            var componentsReportOutput = reportOutput / "components";
            componentsReportOutput.CreateDirectory();

            var coordinator = new ComponentReportCoordinator(new SimpleServiceProvider());

            // Register CodeQuality provider - this automatically collects test metrics!
            var cqOutput = ((IBuildConfigurationComponent)this).GetConfigurablePath(
                "codeQuality.outputDirectory",
                null,
                $"../_artifacts/{version}/dotnet/test-results");

            var cqSolution = ((IBuildConfigurationComponent)this).GetConfigurableValue(
                "codeQuality.solutionFile",
                null,
                "../../development/dotnet/console/Console.sln");

            var codeQualityProvider = new CodeQualityReportProvider(
                outputDirectory: cqOutput,
                solutionPath: cqSolution,
                failOnIssues: false,
                coverageEnabled: true);

            coordinator.RegisterProvider(codeQualityProvider);
            Log.Information("✅ Registered CodeQuality report provider");

            // Generate reports
            var providers = await coordinator.DiscoverReportProvidersAsync();
            
            foreach (var provider in providers)
            {
                try
                {
                    Log.Information("📋 Generating {ComponentName} report...", provider.ComponentName);
                    
                    var reportData = provider.GetReportData();
                    var metadata = provider.GetReportMetadata();
                    var parameters = provider.GetReportParameters();
                    
                    var componentDir = componentsReportOutput / provider.ComponentName.ToLowerInvariant();
                    componentDir.CreateDirectory();
                    
                    var report = new
                    {
                        Component = provider.ComponentName,
                        Metadata = metadata,
                        Data = reportData,
                        Parameters = parameters,
                        GeneratedAt = DateTime.UtcNow
                    };
                    
                    var componentReportPath = componentDir / "component-report.json";
                    var jsonContent = System.Text.Json.JsonSerializer.Serialize(
                        report, 
                        new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
                    
                    System.IO.File.WriteAllText(componentReportPath, jsonContent);
                    Log.Information("💾 {ComponentName} report saved: {Path}", 
                        provider.ComponentName, componentReportPath);
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "❌ Failed to generate {ComponentName} report", provider.ComponentName);
                }
            }

            Log.Information("✅ Component reports generated successfully");
        });

    /// <summary>
    /// Full CI pipeline with all reporting
    /// </summary>
    public Target CI => _ => _
        .Description("Complete CI workflow: build → test → report")
        .DependsOn(BuildAll)
        .DependsOn(Test)
        .DependsOn(GenerateComponentReports)
        .Executes(() =>
        {
            Log.Information("✅ CI pipeline completed successfully");
        });

    public static int Main()
    {
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .WriteTo.Console(outputTemplate: "[{Level:u3}] {Message:lj}{NewLine}{Exception}")
            .CreateLogger();

        return Execute<Build>(x => x.CI);
    }
}

/// <summary>
/// Simple service provider for component coordination
/// </summary>
public class SimpleServiceProvider : IServiceProvider
{
    public object? GetService(Type serviceType) => null;
}
```

### 4. Update _build.csproj References

**Add:** Component package references

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net8.0</TargetFramework>
    <RootNamespace></RootNamespace>
    <NoWarn>CS0649;CS0169</NoWarn>
    <NukeRootDirectory>..</NukeRootDirectory>
    <NukeScriptDirectory>..</NukeScriptDirectory>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Nuke.Common" Version="8.1.2" />
    <PackageReference Include="GitVersion.Tool" Version="6.0.2" />
    <PackageReference Include="Serilog.Sinks.Console" Version="6.0.0" />
    
    <!-- ⭐ Lunar Build Component References -->
    <PackageReference Include="Lunar.Build.Configuration" Version="*" />
    <PackageReference Include="Lunar.Build.Components" Version="*" />
    <PackageReference Include="Lunar.Build.CodeQuality" Version="*" />
    <PackageReference Include="Lunar.NfunReport.MNuke" Version="*" />
  </ItemGroup>

  <ItemGroup>
    <None Remove="build-config.json" />
  </ItemGroup>
</Project>
```

### 5. Update Taskfile.yml Integration

**Update:** `build/Taskfile.yml`

**Changes:**
- Remove `v` prefix from `ARTIFACT_DIR`
- Add Nuke delegation targets
- Keep Task as orchestrator

```yaml
version: '3'

vars:
  VERSION:
    sh: ./get-version.sh
  ARTIFACT_DIR: _artifacts/{{.VERSION}}  # ⭐ Changed from v{{.VERSION}}

tasks:
  # Nuke build targets
  nuke-build:
    desc: "Build via Nuke components"
    dir: nuke
    cmds:
      - ./build.sh BuildAll

  nuke-test:
    desc: "Test via Nuke (includes metric collection)"
    dir: nuke
    cmds:
      - ./build.sh Test

  nuke-reports:
    desc: "Generate component reports"
    dir: nuke
    cmds:
      - ./build.sh GenerateComponentReports

  nuke-ci:
    desc: "Full Nuke CI pipeline"
    dir: nuke
    cmds:
      - ./build.sh CI

  # Legacy compatibility targets (delegate to Nuke)
  build-all:
    desc: "Build all (delegates to Nuke)"
    cmds:
      - task: nuke-build

  test-dotnet:
    desc: "Run .NET tests (delegates to Nuke)"
    cmds:
      - task: nuke-test

  ci:
    desc: "CI pipeline (delegates to Nuke)"
    cmds:
      - task: nuke-ci

  # Direct access to artifacts
  show-reports:
    desc: "Show location of generated reports"
    cmds:
      - echo "📊 Component Reports:"
      - echo "  {{.ARTIFACT_DIR}}/reports/components/"
      - echo ""
      - echo "📈 Test Results:"
      - echo "  {{.ARTIFACT_DIR}}/dotnet/test-results/"
      - echo ""
      - echo "📋 Aggregated Report:"
      - echo "  {{.ARTIFACT_DIR}}/reports/aggregated-report.json"

  open-reports:
    desc: "Open test report in browser"
    cmds:
      - open {{.ARTIFACT_DIR}}/dotnet/test-results/test-report.html
```

### 6. Update get-version.sh (if needed)

**Check:** Does it output `v0.0.1-379` or `0.0.1-379`?

**If it includes `v` prefix, remove it:**
```bash
#!/usr/bin/env bash
# Return version WITHOUT 'v' prefix for component compatibility
gitversion /showvariable SemVer
```

---

## Implementation Plan

### Phase 1: Path Standardization (1-2 hours)

1. [ ] Update `build/Taskfile.yml`: Remove `v` from `ARTIFACT_DIR`
2. [ ] Update `build/get-version.sh`: Remove `v` prefix if present
3. [ ] Update documentation references
4. [ ] Test build with new paths: `task build-all`
5. [ ] Clean old `v*` directories

### Phase 2: Component Integration (2-3 hours)

1. [ ] Create `build/nuke/build-config.json`
2. [ ] Update `build/nuke/build/_build.csproj` with component references
3. [ ] Update `build/nuke/build/Build.cs` to implement interfaces
4. [ ] Test basic build: `cd build/nuke && ./build.sh BuildAll`

### Phase 3: Test Reporting (2-3 hours)

1. [ ] Implement `Test` target with TRX/coverage output
2. [ ] Test metric collection: `./build.sh Test`
3. [ ] Verify TRX files in `_artifacts/{VERSION}/dotnet/test-results/`
4. [ ] Implement `GenerateComponentReports` target
5. [ ] Test report generation: `./build.sh GenerateComponentReports`
6. [ ] Verify `testing-report.json` has metrics

### Phase 4: CI Integration (1 hour)

1. [ ] Update Taskfile to delegate to Nuke
2. [ ] Test full pipeline: `task ci`
3. [ ] Verify all reports generated
4. [ ] Update GitHub Actions workflow (if exists)

---

## Expected Outcomes

### Automatic Test Reporting

**Without code changes**, once components are integrated:

```json
// _artifacts/{GitVersion}/reports/components/codequality/testing-report.json
{
  "ReportType": "Testing",
  "TestsDiscovered": 42,
  "TestsExecuted": 42,
  "TestsPassed": 40,
  "TestsFailed": 2,
  "TestCoveragePercentage": 85.3,
  "TestDuration": "00:01:23",
  "GeneratedAt": "2025-01-08T10:30:00Z"
}
```

### Consistent Artifact Paths

**Before:**
```
build/_artifacts/v0.0.1-379/
build/_artifacts/v0.0.1-380/
```

**After:**
```
build/_artifacts/0.0.1-379/
build/_artifacts/0.0.1-380/
```

**Benefits:**
- Matches component configuration token: `{GitVersion}`
- Simpler token replacement (no special `v` handling)
- Consistent with `asset-inout` and other lunar-build projects

### New Capabilities

1. **Structured test metrics** - JSON reports for CI consumption
2. **Quality gates** - Configurable pass/fail thresholds
3. **Multi-format reports** - JSON, XML, YAML, Markdown via NFunReport
4. **Component coordination** - Aggregated reports across build aspects
5. **Configuration-driven** - Centralized build config in `build-config.json`

---

## Testing Strategy

### Unit Tests

- [ ] Verify path resolution without `v` prefix
- [ ] Test build-config.json parsing
- [ ] Verify component registration

### Integration Tests

- [ ] Build with new artifact paths: `task build-all`
- [ ] Run tests with Nuke: `./build.sh Test`
- [ ] Generate reports: `./build.sh GenerateComponentReports`
- [ ] Verify report contents

### End-to-End Tests

- [ ] Full CI pipeline: `task ci`
- [ ] Verify all artifacts in correct locations
- [ ] Check test metrics accuracy
- [ ] Validate report formats

---

## Migration Path

### For Developers

**No impact** - Task commands remain the same:
```bash
task build-all  # Still works
task ci         # Still works
```

**New commands available:**
```bash
task nuke-reports     # View component reports
task show-reports     # Show report locations
task open-reports     # Open test report in browser
```

### For CI

**Update GitHub Actions** (if exists):
```yaml
- name: Build and Test
  run: task ci

- name: Upload Test Results
  uses: actions/upload-artifact@v4
  with:
    name: test-results
    path: build/_artifacts/${{ env.VERSION }}/dotnet/test-results/
    
- name: Upload Reports
  uses: actions/upload-artifact@v4
  with:
    name: reports
    path: build/_artifacts/${{ env.VERSION }}/reports/
```

---

## Risks and Mitigations

### Risk 1: Component Package Availability

**Risk:** Lunar build component packages not published to accessible feed

**Mitigation:**
- Use ProjectReference to local components during development
- Publish to local NuGet repo: `../../../packages/nuget-repo`
- Document package build process

### Risk 2: Path Change Breaking Scripts

**Risk:** Existing scripts may hard-code `v{VERSION}` paths

**Mitigation:**
- Search codebase for `_artifacts/v` patterns before migration
- Update documentation thoroughly
- Keep both formats temporarily during transition

### Risk 3: Learning Curve

**Risk:** Team unfamiliar with component interfaces

**Mitigation:**
- Provide `asset-inout` as reference implementation
- Document component usage patterns
- Start with minimal integration, expand gradually

---

## Alternatives Considered

### Alternative 1: Build Custom Test Reporter

**Pros:** Full control, no dependencies  
**Cons:** Duplicate effort, maintenance burden, no multi-format support

**Decision:** REJECTED - Components provide proven, maintained solution

### Alternative 2: Keep v Prefix

**Pros:** No path migration needed  
**Cons:** Inconsistent with components, requires special token handling

**Decision:** REJECTED - Standardization is worth one-time migration

### Alternative 3: Use Only NFunReport (No Components)

**Pros:** Simpler dependencies  
**Cons:** Lose automatic test collection, have to implement IReportDataProvider

**Decision:** REJECTED - Components provide immediate value

---

## Success Criteria

- [ ] Artifact paths use `{GitVersion}` without `v` prefix
- [ ] `build-config.json` successfully drives builds
- [ ] Test metrics automatically collected from TRX files
- [ ] `testing-report.json` generated with accurate metrics
- [ ] Component reports accessible at `_artifacts/{VERSION}/reports/`
- [ ] Task commands work unchanged for developers
- [ ] CI pipeline generates structured test results
- [ ] Documentation updated with new patterns

---

## References

- **RFC-0010**: Multi-Language Build Orchestration with Task (artifact structure)
- **Asset-InOut Build**: `plate-projects/asset-inout/build/nuke/` (reference implementation)
- **Lunar Build Components**: `infra-projects/giantcroissant-lunar-build/`
- **NFunReport**: `infra-projects/giantcroissant-lunar-report/`
- **CodeQualityReportProvider**: Test metric collection implementation

---

## Open Questions

1. Should we also migrate E2E test reports to component structure?
2. Do we need custom IReportDataProvider for game-specific metrics?
3. Should PTY tests be formalized to participate in reporting?
4. What's the retention policy for versioned reports?

---

## Appendix A: Example Output Structure

```
build/_artifacts/0.0.1-379/         # ⭐ No 'v' prefix
├── dotnet/
│   ├── bin/                         # Built executables
│   ├── test-results/                # ⭐ TRX, coverage
│   │   ├── test-results.trx
│   │   ├── test-report.html
│   │   └── coverage/
│   │       └── coverage.cobertura.xml
│   ├── logs/                        # Runtime logs
│   └── recordings/                  # Asciinema
├── web/
│   ├── dist/                        # Built website
│   ├── test-reports/                # Playwright HTML
│   ├── test-results/                # Screenshots, videos
│   ├── logs/
│   └── recordings/
├── pty/
│   ├── dist/                        # PTY service
│   └── logs/
└── reports/                         # ⭐ Component reports
    ├── components/
    │   └── codequality/
    │       ├── component-report.json
    │       ├── analysis-report.json
    │       ├── testing-report.json  # ⭐ Test metrics
    │       └── formatting-report.json
    └── aggregated-report.json
```

---

## Appendix B: build-config.json Schema Reference

**Key sections for test reporting:**

```json
{
  "codeQuality": {
    "outputDirectory": "../_artifacts/{GitVersion}/dotnet/test-results",
    "thresholds": {
      "maxWarnings": 100,    // Quality gate
      "maxErrors": 0,        // Fail if any errors
      "minCoverage": 80.0    // Minimum coverage %
    },
    "coverage": {
      "enable": true,
      "format": "cobertura"
    }
  },
  "reporting": {
    "enabled": true,
    "outputDirectory": "../_artifacts/{GitVersion}/reports"
  }
}
```

---

**Status:** Ready for Implementation  
**Next Steps:** Begin Phase 1 (Path Standardization)

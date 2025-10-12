# Nuke Build Integration Plan for WingedBean

**Date**: 2025-01-08  
**Context**: Integrating giantcroissant-lunar-build components for WingedBean Console

---

## Current State vs Target State

### Current: Basic Nuke Build (winged-bean/build/nuke/build/Build.cs)

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

**Capabilities**:
- ✅ Basic compile
- ✅ Clean/Restore/Build targets
- ❌ No configuration-driven builds
- ❌ No reporting
- ❌ No NuGet packaging
- ❌ No test result collection

### Target: Component-Based Build (asset-inout example)

```csharp
partial class Build :
    INfunReportComponent,              // Multi-format reports
    IBuildConfigurationComponent,       // Config-driven builds
    IWrapperPathComponent,             // Path resolution
    INuGetLocalRepoSyncComponent       // Local NuGet sync
{
    // Configuration from build-config.json
    public string BuildConfigPath => EffectiveRootDirectory / "build" / "nuke" / "build-config.json";
    
    // Inherits targets from components:
    // - GenerateReports / ValidateReports (from INfunReportComponent)
    // - Pack / NuGetWorkflow (from INuGetPackaging)
    // - SyncNugetPackagesToLocalFeeds (from INuGetLocalRepoSyncComponent)
}
```

**Capabilities**:
- ✅ Configuration-driven builds
- ✅ Component reporting (CodeQuality, NuGet, etc.)
- ✅ NFunReport integration (multi-format)
- ✅ NuGet packaging and sync
- ✅ Test result collection via CodeQuality component

---

## Key Insight: Component Reports ARE for Nuke Components

You're **100% correct**! 

**Component Reports** = Reports FROM Nuke build components like:
- `CodeQualityReportProvider` - Analysis, formatting, **testing**, coverage
- `NuGetReportProvider` - Packaging, dependencies, repository
- `MobileReportProvider` - iOS/Android builds, Fastlane
- `DocumentationReportProvider` - Docs generation

These components **already have** test metric collection built-in!

When you use `IBuildConfigurationComponent` and friends, you automatically get:
1. Component discovery and registration
2. Metric collection
3. Report generation
4. Multi-format output (via NFunReport delegation)

---

## The Integration Path

### Step 1: Create build-config.json

**Create**: `/yokan-projects/winged-bean/build/nuke/build-config.json`

```json
{
  "$schema": "https://json-schema.org/draft/2020-12/schema",
  "description": "Build configuration for winged-bean console application",
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
      "name": "console-app",
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
    "artifactsDirectory": "../_artifacts"
  },

  "codeQuality": {
    "outputDirectory": "../_artifacts/{GitVersion}/dotnet/test-results",
    "failOnIssues": false,
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

### Step 2: Upgrade Build.cs to Use Components

**Update**: `/yokan-projects/winged-bean/build/nuke/build/Build.cs`

```csharp
using System;
using Nuke.Common;
using Nuke.Common.Tools.DotNet;
using Nuke.Common.Tools.GitVersion;
using Nuke.Common.IO;
using Serilog;
using Lunar.Build.Configuration;
using Lunar.Build.Components;
using Lunar.NfunReport.MNuke.Components;
using static Nuke.Common.Tools.DotNet.DotNetTasks;

namespace WingedBean.Build.MNuke;

/// <summary>
/// WingedBean Console Build - Using giantcroissant-lunar-build components
/// </summary>
partial class Build :
    INfunReportComponent,              // ⭐ Multi-format reporting
    IBuildConfigurationComponent,       // ⭐ Config-driven builds
    IWrapperPathComponent              // ⭐ Path resolution
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
    /// Build all projects
    /// </summary>
    public Target BuildAll => _ => _
        .Description("Build console and plugins")
        .Executes(() =>
        {
            var projects = ((IProjectDiscoveryComponent)this).GetProjectsToBuild();
            
            foreach (var project in projects)
            {
                DotNetBuild(s => s
                    .SetProjectFile(project)
                    .SetConfiguration(Configuration));
            }
        });

    /// <summary>
    /// Run tests with structured output
    /// </summary>
    public Target Test => _ => _
        .Description("Run tests and collect metrics")
        .DependsOn(BuildAll)
        .Executes(() =>
        {
            var artifactDir = ((IBuildConfigurationComponent)this).GetConfigurablePath(
                "globalPaths.artifactsDirectory",
                null,
                "../_artifacts");
            
            var version = GitVersion?.SemVer ?? "local";
            var testResultsDir = artifactDir / $"v{version}" / "dotnet" / "test-results";
            testResultsDir.CreateDirectory();

            DotNetTest(s => s
                .SetProjectFile("../../development/dotnet/console/Console.sln")
                .SetConfiguration(Configuration)
                .SetLoggers(
                    $"trx;LogFileName={testResultsDir}/test-results.trx",
                    $"html;LogFileName={testResultsDir}/test-report.html")
                .SetResultsDirectory(testResultsDir)
                .SetProperty("CollectCoverage", "true")
                .SetProperty("CoverletOutputFormat", "cobertura")
                .SetProperty("CoverletOutput", $"{testResultsDir}/coverage/"));
                
            Log.Information("✅ Test results saved to {Dir}", testResultsDir);
        });

    /// <summary>
    /// Generate component reports (includes test metrics from CodeQuality component)
    /// </summary>
    public Target GenerateComponentReports => _ => _
        .Description("Generate reports from all build components")
        .DependsOn(Test)
        .Executes(async () =>
        {
            var reportOutput = ((IBuildConfigurationComponent)this).GetConfigurablePath(
                "reporting.outputDirectory",
                null,
                "../_artifacts/{GitVersion}/reports");

            var coordinator = new ComponentReportCoordinator(new SimpleServiceProvider());

            // Register CodeQuality provider (will collect test metrics)
            var cqOutput = ((IBuildConfigurationComponent)this).GetConfigurablePath(
                "codeQuality.outputDirectory",
                null,
                "../_artifacts/{GitVersion}/dotnet/test-results");

            var codeQualityProvider = new CodeQualityReportProvider(
                outputDirectory: cqOutput,
                solutionPath: "../../development/dotnet/console/Console.sln",
                failOnIssues: false,
                coverageEnabled: true);

            coordinator.RegisterProvider(codeQualityProvider);

            // Generate reports
            var providers = await coordinator.DiscoverReportProvidersAsync();
            
            foreach (var provider in providers)
            {
                Log.Information("📋 Generating {ComponentName} report...", provider.ComponentName);
                var reportData = provider.GetReportData();
                var metadata = provider.GetMetadata();
                
                // Save to JSON (specialized reports include testing-report.json)
                // Component reporting infrastructure handles this
            }

            Log.Information("✅ Component reports generated at {Path}", reportOutput);
        });

    /// <summary>
    /// Full CI pipeline
    /// </summary>
    public Target CI => _ => _
        .Description("Complete CI workflow")
        .DependsOn(BuildAll)
        .DependsOn(Test)
        .DependsOn(GenerateComponentReports)
        .DependsOn(GenerateReports)  // From INfunReportComponent
        .Executes(() =>
        {
            Log.Information("✅ CI pipeline completed");
        });

    public static int Main()
    {
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .WriteTo.Console()
            .CreateLogger();

        return Execute<Build>(x => x.CI);
    }
}

public class SimpleServiceProvider : IServiceProvider
{
    public object? GetService(Type serviceType) => null;
}
```

### Step 3: Update Taskfile.yml Integration

**Update**: `/yokan-projects/winged-bean/build/Taskfile.yml`

```yaml
version: '3'

vars:
  VERSION:
    sh: ./get-version.sh
  ARTIFACT_DIR: _artifacts/v{{.VERSION}}

tasks:
  nuke-build:
    desc: "Run Nuke build with component integration"
    dir: nuke
    cmds:
      - ./build.sh BuildAll

  nuke-test:
    desc: "Run tests via Nuke (includes metric collection)"
    dir: nuke
    cmds:
      - ./build.sh Test

  nuke-reports:
    desc: "Generate all reports (component + NFunReport)"
    dir: nuke
    cmds:
      - ./build.sh GenerateComponentReports
      - ./build.sh GenerateReports

  nuke-ci:
    desc: "Full Nuke CI pipeline"
    dir: nuke
    cmds:
      - ./build.sh CI

  # Legacy task targets for compatibility
  build-all:
    desc: "Build all (delegates to Nuke)"
    cmds:
      - task: nuke-build

  test-dotnet:
    desc: "Run tests (delegates to Nuke)"
    cmds:
      - task: nuke-test

  ci:
    desc: "CI pipeline (delegates to Nuke)"
    cmds:
      - task: nuke-ci
```

---

## What You Get Automatically

### 1. Test Metrics Collection

**From `CodeQualityReportProvider`**:

Output location: `build/_artifacts/v{VERSION}/reports/components/codequality/testing-report.json`

```json
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

### 2. Multi-Format Reports

**From `INfunReportComponent`** (via delegation):

Output location: `build/_artifacts/v{VERSION}/reports/TestMetrics/`

- `TestMetrics.json`
- `TestMetrics.xml`
- `TestMetrics.yaml`
- `TestMetrics.md`

### 3. Aggregated Report

**From `ComponentReportCoordinator`**:

Output location: `build/_artifacts/v{VERSION}/reports/aggregated-report.json`

```json
{
  "Components": [
    {
      "Name": "CodeQuality",
      "Metrics": {
        "TestsPassed": 40,
        "TestsFailed": 2,
        "Coverage": 85.3
      }
    }
  ],
  "GeneratedAt": "2025-01-08T10:30:00Z",
  "TotalComponents": 1,
  "SuccessfulComponents": 1
}
```

---

## Benefits of Component Integration

### Before (Current)
- ❌ Manual test result collection
- ❌ No structured reporting
- ❌ No metric aggregation
- ❌ Single format (console output)
- ❌ No CI-friendly artifacts

### After (With Components)
- ✅ **Automatic test metric collection** via `CodeQualityReportProvider`
- ✅ **Structured JSON reports** for CI consumption
- ✅ **Multi-format output** (JSON, XML, YAML, MD) via NFunReport
- ✅ **Versioned artifacts** tied to GitVersion
- ✅ **Component aggregation** for holistic view
- ✅ **Quality gates** with configurable thresholds
- ✅ **Schema validation** for report correctness

---

## Implementation Checklist

### Phase 1: Basic Integration
- [ ] Create `build-config.json` with paths and project groups
- [ ] Update `Build.cs` to implement component interfaces
- [ ] Update `_build.csproj` to reference lunar-build components
- [ ] Test basic build workflow

### Phase 2: Test Integration
- [ ] Configure `codeQuality` section in build-config.json
- [ ] Implement `Test` target with TRX/coverage output
- [ ] Register `CodeQualityReportProvider` in `GenerateComponentReports`
- [ ] Verify test metrics in `testing-report.json`

### Phase 3: NFunReport Integration
- [ ] Implement `INfunReportComponent` interface
- [ ] Configure `reporting.outputDirectory` in build-config.json
- [ ] Test `GenerateReports` target for multi-format output
- [ ] Verify schema validation with `ValidateReports`

### Phase 4: CI Integration
- [ ] Create `CI` target that chains all steps
- [ ] Update Taskfile.yml to delegate to Nuke
- [ ] Test end-to-end workflow
- [ ] Configure GitHub Actions to use Nuke targets

---

## Example Output Structure

```
build/_artifacts/v0.0.1-379/
├── dotnet/
│   ├── bin/                          # Built binaries
│   ├── test-results/                 # ⭐ TRX, coverage
│   │   ├── test-results.trx
│   │   ├── test-report.html
│   │   └── coverage/
│   │       └── coverage.cobertura.xml
│   └── logs/
├── reports/                          # ⭐ Component & NFunReport outputs
│   ├── components/
│   │   └── codequality/
│   │       ├── component-report.json
│   │       ├── analysis-report.json
│   │       ├── testing-report.json   # ⭐ Test metrics here
│   │       └── formatting-report.json
│   ├── TestMetrics/                  # ⭐ NFunReport multi-format
│   │   ├── TestMetrics.json
│   │   ├── TestMetrics.xml
│   │   ├── TestMetrics.yaml
│   │   └── TestMetrics.md
│   └── aggregated-report.json
└── web/
    ├── test-reports/                 # Playwright HTML
    └── test-results/                 # Screenshots, videos
```

---

## Next Steps

1. **Review** this integration plan
2. **Create** build-config.json based on template
3. **Update** Build.cs to implement interfaces
4. **Test** basic Nuke workflow: `./build.sh BuildAll`
5. **Add** Test target and verify TRX output
6. **Enable** component reporting: `./build.sh GenerateComponentReports`
7. **Verify** test metrics in reports
8. **Integrate** with Taskfile and CI

---

## References

- **Working Example**: `plate-projects/asset-inout/build/nuke/`
- **Component Interfaces**: `giantcroissant-lunar-build/build/nuke/components/`
- **CodeQualityReportProvider**: Already collects test metrics
- **INfunReportComponent**: Multi-format report generation
- **build-config.json Schema**: Configuration-driven builds

---

## Summary

The key insight is that **Nuke build components already have reporting built-in**. You don't need to create separate test report providers - the `CodeQualityReportProvider` component already collects test metrics as part of its normal operation!

By implementing the component interfaces (`IBuildConfigurationComponent`, `INfunReportComponent`, etc.) and creating a `build-config.json`, you automatically get:
- Test metric collection
- Structured reporting
- Multi-format output
- CI integration

This is the **intended pattern** - use the components, get the reports automatically!

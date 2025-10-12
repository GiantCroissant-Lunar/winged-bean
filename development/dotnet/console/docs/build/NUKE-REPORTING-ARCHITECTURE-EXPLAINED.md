# Nuke Reporting Architecture - Complete Explanation

**Date**: 2025-01-08  
**Context**: Understanding the two-layer reporting system in Lunar Build infrastructure

---

## The Confusion: Two Different Reporting Systems

You're right to be confused! There are **TWO SEPARATE** reporting systems that work at different layers:

### Layer 1: Component Reports (lunar-build)
**Location**: `/infra-projects/giantcroissant-lunar-build/`

**Purpose**: Collects build-time metrics from various components
- `IComponentReportProvider<T>` in `Lunar.Build.Abstractions`
- Simple JSON reports per component
- Used by `Build.ComponentReporting.cs`

### Layer 2: NFunReport System (lunar-report)
**Location**: `/infra-projects/giantcroissant-lunar-report/`

**Purpose**: Advanced report generation engine with multiple format support
- `IReportDataProvider<T>` in `Lunar.NfunReport.Abstractions`
- Multi-format output (JSON, XML, YAML, Markdown)
- Schema validation, determinism checks
- Used by `INfunReportComponent.cs`

---

## Layer 1: Component Reports (Simple Build Metrics)

### Interface: `IComponentReportProvider<T>`

**Location**: `giantcroissant-lunar-build/build/nuke/components/CoreAbstractions/Interfaces/IComponentReportProvider.cs`

```csharp
public interface IComponentReportProvider<T>
{
    string ComponentName { get; }
    IEnumerable<T> GetReportData();
    ComponentReportMetadata GetReportMetadata();
    IReadOnlyDictionary<string, object>? GetReportParameters();
}
```

### How It Works

1. **Registration** (in `Build.ComponentReporting.cs`):
```csharp
var coordinator = new ComponentReportCoordinator(serviceProvider);

// Register providers for active components
var codeQualityProvider = new CodeQualityReportProvider(...);
coordinator.RegisterProvider(codeQualityProvider);

var nugetProvider = new NuGetReportProvider(...);
coordinator.RegisterProvider(nugetProvider);
```

2. **Data Collection**:
```csharp
// Each provider returns its metrics
public IEnumerable<CodeQualityMetricsData> GetReportData()
{
    yield return new CodeQualityMetricsData
    {
        ComponentName = "CodeQuality",
        TestsDiscovered = 42,
        TestsPassed = 40,
        TestsFailed = 2,
        TestCoveragePercentage = 85.3m,
        // ... other metrics
    };
}
```

3. **Output Structure**:
```
build/_reports/components/
├── codequality/
│   ├── component-report.json       # Full component data
│   ├── analysis-report.json        # Specialized sub-report
│   ├── testing-report.json         # ⭐ Test metrics here
│   └── formatting-report.json      # Specialized sub-report
├── nuget/
│   ├── component-report.json
│   ├── packaging-report.json
│   └── dependencies-report.json
└── aggregated-report.json          # Combined summary
```

### Build Target

```bash
nuke GenerateComponentReports
```

This is **currently working** and generates simple JSON reports.

---

## Layer 2: NFunReport System (Advanced Report Engine)

### Interface: `IReportDataProvider<T>`

**Location**: `giantcroissant-lunar-report/src/Lunar.NfunReport.Abstractions/IReportDataProvider.cs`

```csharp
public interface IReportDataProvider<T>
{
    IEnumerable<T> GetData();
    ReportMetadata GetMetadata();
    ReportTemplate GetTemplate();
    IReadOnlyDictionary<string, object>? GetParameters();
}
```

### How It Works

1. **Component Integration** (via `INfunReportComponent`):
```csharp
// Your Build.cs can inherit from INfunReportComponent
partial class Build : INfunReportComponent
{
    // Inherits targets: GenerateReports, ValidateReports
}
```

2. **Provider Discovery** (in `ProviderDiscovery.cs`):
```csharp
// Reads build-config.json
{
  "projectGroups": {
    "reportProviders": [
      "path/to/TestReportProvider.csproj"
    ]
  }
}

// Discovers providers from assemblies using reflection
var providers = await discovery.DiscoverProvidersAsync(providerProjects);
```

3. **Report Generation Flow**:
```
IReportDataProvider.GetData() 
  → FastReportEngine.GenerateAsync()
    → Creates PreparedModel
      → JsonRenderer, XmlRenderer, YamlRenderer, MarkdownRenderer
        → Outputs: report.{json,xml,yaml,md}
```

4. **Output Structure**:
```
artifacts/v{VERSION}/reports/
├── TestMetrics/
│   ├── TestMetrics.json
│   ├── TestMetrics.xml
│   ├── TestMetrics.yaml
│   └── TestMetrics.md
├── CodeQuality/
│   ├── CodeQualityMetrics.json
│   ├── CodeQualityMetrics.xml
│   └── ...
└── _determinism/                   # Validation artifacts
```

### Build Targets

```bash
nuke GenerateReports       # From INfunReportComponent
nuke ValidateReports       # Validates schemas + determinism
```

### Delegation Pattern

**In `Build.ReportingDelegation.cs`**:
```csharp
// Main Build doesn't inherit INfunReportComponent
// Instead, it delegates to a private runner class
public Target GenerateReportsCanonical => _ => _
    .Executes(() =>
    {
        // Execute separate build with INfunReportComponent inheritance
        NukeBuild.Execute<ReportingRunner>(x => x.CanonicalGenerateReports);
    });

private sealed class ReportingRunner : INfunReportComponent
{
    // This class inherits the component and exposes its targets
}
```

**Why?** Keeps main `Build` class clean while still accessing NFunReport features.

---

## Key Differences Between the Two Systems

| Aspect | Component Reports (Layer 1) | NFunReport (Layer 2) |
|--------|------------------------------|----------------------|
| **Interface** | `IComponentReportProvider<T>` | `IReportDataProvider<T>` |
| **Package** | `Lunar.Build.Abstractions` | `Lunar.NfunReport.Abstractions` |
| **Output** | Simple JSON per component | Multi-format (JSON, XML, YAML, MD) |
| **Validation** | None | Schema validation, determinism checks |
| **Discovery** | Manual registration | Auto-discovery from build-config.json |
| **Location** | `build/_reports/components/` | `artifacts/v{VERSION}/reports/` |
| **Target** | `GenerateComponentReports` | `GenerateReports` (canonical) |
| **Complexity** | Simple, immediate | Advanced, configurable |
| **Use Case** | Build-time metrics for CI | Comprehensive, versioned reports |

---

## How They Relate

### Current State (lunar-build)

**Component Reports** are actively used:
- `CodeQualityReportProvider` collects test metrics
- `NuGetReportProvider` collects packaging metrics
- Output: `build/_reports/components/*/component-report.json`

**NFunReport** delegation exists but uses separate runner:
- `Build.ReportingDelegation.cs` provides bridge
- Not directly integrated with component providers
- Would need separate `IReportDataProvider<T>` implementations

### The Missing Link

To use **NFunReport's advanced features** with your **component data**, you'd need to:

1. Create adapter providers that implement `IReportDataProvider<T>`
2. Register them in `build-config.json`
3. Let NFunReport discover and process them
4. Get multi-format, validated, deterministic output

**Example**:
```csharp
// Adapter: Component → NFunReport
public class TestMetricsNFunProvider : IReportDataProvider<TestMetricsData>
{
    private readonly CodeQualityReportProvider _codeQualityProvider;
    
    public IEnumerable<TestMetricsData> GetData()
    {
        // Extract test data from CodeQualityReportProvider
        var cqData = _codeQualityProvider.GetReportData().First();
        yield return new TestMetricsData
        {
            TestsDiscovered = cqData.TestsDiscovered,
            TestsPassed = cqData.TestsPassed,
            // ...
        };
    }
    
    public ReportMetadata GetMetadata()
    {
        return new ReportMetadata
        {
            Id = "TestMetrics",
            Title = "Test Execution Report",
            Columns = new[]
            {
                new ReportColumnInfo 
                { 
                    PropertyName = "TestsPassed", 
                    DisplayName = "Passed" 
                },
                // ...
            }
        };
    }
    
    public ReportTemplate GetTemplate() => new();
    public IReadOnlyDictionary<string, object>? GetParameters() => null;
}
```

---

## For WingedBean Console: Which Should You Use?

### Option A: Component Reports (Simpler, Immediate)

**Extend `CodeQualityReportProvider`**:
- Already collects test metrics
- Just wire up TRX file parsing
- Output to `build/_reports/components/codequality/testing-report.json`
- Works immediately with existing infrastructure

**Pros**:
- ✅ Already working
- ✅ Simple JSON output
- ✅ No new interfaces to implement
- ✅ Integrated with build-config.json settings

**Cons**:
- ❌ Only JSON format
- ❌ No schema validation
- ❌ Manual quality gate checks

### Option B: NFunReport (Advanced, More Setup)

**Create dedicated `IReportDataProvider<T>`**:
- Implement provider for test metrics
- Register in `build-config.json` reportProviders
- Get multi-format, validated output
- Version output in artifacts directory

**Pros**:
- ✅ Multi-format output (JSON, XML, YAML, MD)
- ✅ Schema validation
- ✅ Determinism checks
- ✅ Versioned artifacts
- ✅ Consistent with overall reporting strategy

**Cons**:
- ❌ More setup required
- ❌ Need to build separate provider project
- ❌ Delegation pattern complexity

---

## Recommended Approach for WingedBean

### Phase 1: Use Component Reports (Now)

1. Update `test-dotnet` task to output TRX and coverage
2. Extend `CodeQualityReportProvider` to parse TRX files
3. Generate specialized testing-report.json
4. Use `GenerateComponentReports` target

**Implementation**:
```yaml
# Taskfile.yml
test-dotnet:
  cmds:
    - |
      dotnet test Console.sln \
        --logger "trx;LogFileName={{.ARTIFACT_DIR}}/dotnet/test-results/test-results.trx" \
        --results-directory {{.ARTIFACT_DIR}}/dotnet/test-results \
        /p:CollectCoverage=true
```

**Output**:
```
build/_reports/components/codequality/testing-report.json
```

### Phase 2: Migrate to NFunReport (Later, Optional)

If you need:
- Multiple output formats for different consumers
- Schema validation for CI integration
- Determinism checks for reproducibility
- Versioned reports alongside artifacts

Then create:
```csharp
// TestReportProvider.csproj
public class TestReportProvider : IReportDataProvider<TestMetricsData>
{
    // Implementation
}
```

Register in `build-config.json`:
```json
{
  "projectGroups": {
    "reportProviders": [
      "yokan-projects/winged-bean/build/reporting/TestReportProvider.csproj"
    ]
  }
}
```

Use canonical targets:
```bash
nuke GenerateReportsCanonical
nuke ValidateReportsCanonical
```

---

## Answering Your Original Questions

### Q: How do we use `/infra-projects/giantcroissant-lunar-report/build/nuke/components/Reporting`?

**A**: You **don't use it directly** in WingedBean. It's a **Nuke component** that other build projects can **inherit from**:

```csharp
// Option 1: Direct inheritance (changes main Build class)
partial class Build : INfunReportComponent
{
    // Inherits GenerateReports, ValidateReports targets
}

// Option 2: Delegation (keeps Build class clean)
// See Build.ReportingDelegation.cs in lunar-build
public Target GenerateReportsCanonical => _ => _
    .Executes(() =>
    {
        NukeBuild.Execute<ReportingRunner>(x => x.CanonicalGenerateReports);
    });
```

### Q: How do we use `/infra-projects/giantcroissant-lunar-report/src/Lunar.NfunReport.Abstractions`?

**A**: You **implement `IReportDataProvider<T>`** in your own report provider project:

```csharp
// In your project: WingedBean.Build.Reporting
using Lunar.NfunReport.Abstractions;

public class TestMetricsProvider : IReportDataProvider<TestMetricsData>
{
    public IEnumerable<TestMetricsData> GetData() { /* ... */ }
    public ReportMetadata GetMetadata() { /* ... */ }
    public ReportTemplate GetTemplate() => new();
    public IReadOnlyDictionary<string, object>? GetParameters() => null;
}
```

Then:
1. Build this project
2. Register it in `build-config.json` under `reportProviders`
3. NFunReport discovers and processes it
4. Output goes to `artifacts/v{VERSION}/reports/TestMetrics/*`

---

## Summary: Two Layers, Two Use Cases

**Layer 1: Component Reports**
- Simple, immediate, JSON-only
- For build-time CI metrics
- Manual registration, simple output
- Use **now** for test reporting

**Layer 2: NFunReport**
- Advanced, multi-format, validated
- For comprehensive versioned reports
- Auto-discovery, determinism checks
- Use **later** if needed for advanced features

For WingedBean test reporting, **start with Layer 1** (Component Reports) and only move to Layer 2 if you need its advanced features.

---

## Next Steps

1. ✅ Understand the two-layer architecture
2. [ ] Decide: Component Reports (simple) or NFunReport (advanced)?
3. [ ] Update Taskfile to output TRX and coverage files
4. [ ] Wire up test metric parsing
5. [ ] Generate reports with chosen system
6. [ ] Integrate into CI pipeline

---

## References

- `Build.ComponentReporting.cs` - Layer 1 implementation
- `Build.ReportingDelegation.cs` - Layer 2 delegation
- `INfunReportComponent.cs` - Layer 2 component base
- `CodeQualityReportProvider.cs` - Example Layer 1 provider
- `IComponentReportProvider.cs` - Layer 1 interface
- `IReportDataProvider.cs` - Layer 2 interface

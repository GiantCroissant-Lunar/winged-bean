# Nuke Build Report Integration for Test Results

**Date**: 2025-01-08  
**Context**: Integrating test output with Lunar Build's NFunReport system  
**Reference**: `giantcroissant-lunar-report` and `giantcroissant-lunar-build`

---

## Overview

The Lunar Build infrastructure provides a comprehensive **NFunReport** system that:
1. Collects metrics from various build components (CodeQuality, NuGet, Mobile, Documentation, etc.)
2. Generates reports in multiple formats (JSON, XML, YAML, Markdown)
3. Validates reports against schemas
4. Provides deterministic, versioned output

This document outlines how to integrate test results into this reporting system.

---

## Current Nuke Report Architecture

### Core Components

#### 1. **lunar-report** (Report Engine)
Location: `/infra-projects/giantcroissant-lunar-report/`

**Key Files**:
- `components/Reporting/INfunReportComponent.cs` - Nuke build component
- `components/Reporting/ProviderDiscovery.cs` - Discovers report providers

**Capabilities**:
- Multi-format report generation (JSON, XML, YAML, MD)
- Schema validation (JSON Schema, XSD)
- Determinism checks (re-run and compare)
- Parallel generation with configurable parallelism
- Error artifacts with redaction support
- Versioned output directory structure

**Build Targets**:
```bash
nuke GenerateReports    # Generate all component reports
nuke ValidateReports    # Validate reports against schemas
```

#### 2. **lunar-build** (Component Implementations)
Location: `/infra-projects/giantcroissant-lunar-build/`

**Component Structure**:
```
components/
├── CodeQuality/
│   └── Reporting/
│       ├── CodeQualityReportProvider.cs    # IComponentReportProvider<T>
│       └── CodeQualityMetricsData.cs       # Data model
├── NuGet/Reporting/
├── Mobile/Reporting/
├── Documentation/Reporting/
└── CoreAbstractions/
    ├── Interfaces/
    │   └── IComponentReportProvider.cs      # Core interface
    └── Models/
        └── ComponentReportMetadata.cs
```

---

## Report Provider Pattern

### Interface Definition

```csharp
public interface IComponentReportProvider<T>
{
    string ComponentName { get; }
    IEnumerable<T> GetReportData();
    ComponentReportMetadata GetReportMetadata();
    IReadOnlyDictionary<string, object>? GetReportParameters();
}
```

### Example: CodeQualityReportProvider

The `CodeQualityReportProvider` demonstrates how test metrics are integrated:

**Metrics Collected**:
- `TestsDiscovered` - Total tests found
- `TestsExecuted` - Tests actually run
- `TestsPassed` - Successful tests
- `TestsFailed` - Failed tests
- `TestCoveragePercentage` - Code coverage %

**Data Sources**:
- TRX files: `outputDirectory/test-results.xml`
- Coverage reports: `outputDirectory/coverage.xml`
- Build logs: `outputDirectory/build.log`

**Report Structure**:
```csharp
new CodeQualityMetricsData
{
    ComponentName = "CodeQuality",
    ReportTimestamp = DateTime.UtcNow,
    TestsDiscovered = ParseTestCount(testResults, "discovered"),
    TestsExecuted = ParseTestCount(testResults, "executed"),
    TestsPassed = ParseTestCount(testResults, "passed"),
    TestsFailed = ParseTestCount(testResults, "failed"),
    TestCoveragePercentage = ParseCoveragePercentage(coverageReport),
    QualityGatesPassed = DetermineQualityGateStatus(...),
    // ... other metrics
}
```

---

## Integration Plan for WingedBean Console Tests

### Option 1: Extend Existing CodeQualityReportProvider (Recommended)

**Pros**:
- Leverages existing infrastructure
- Test metrics already part of CodeQuality component
- No new component needed

**Cons**:
- Couples test reporting with code quality

**Implementation**:
```yaml
# build/Taskfile.yml
test-dotnet:
  desc: "Run .NET tests with reporting"
  dir: ../development/dotnet/console
  cmds:
    - |
      dotnet test Console.sln \
        --logger "trx;LogFileName={{.ARTIFACT_DIR}}/dotnet/test-results/test-results.trx" \
        --results-directory {{.ARTIFACT_DIR}}/dotnet/test-results \
        /p:CollectCoverage=true \
        /p:CoverletOutputFormat=cobertura \
        /p:CoverletOutput={{.ARTIFACT_DIR}}/dotnet/test-results/coverage/
```

**Report Provider Configuration**:
The CodeQuality component would read from artifact paths:
```csharp
private TestMetricsSummary CollectTestMetrics()
{
    // Read from versioned artifact directory
    var testResults = artifactDirectory / "dotnet" / "test-results" / "test-results.trx";
    
    return new TestMetricsSummary
    {
        TestsDiscovered = ParseTrxFile(testResults, "total"),
        TestsExecuted = ParseTrxFile(testResults, "executed"),
        TestsPassed = ParseTrxFile(testResults, "passed"),
        TestsFailed = ParseTrxFile(testResults, "failed"),
    };
}
```

### Option 2: Create Dedicated TestReportProvider

**Pros**:
- Separation of concerns
- More detailed test-specific metrics
- Independent test reporting

**Cons**:
- More infrastructure code
- Need to integrate into discovery system

**Implementation**:
```csharp
// components/Testing/Reporting/TestReportProvider.cs
public class TestReportProvider : IComponentReportProvider<TestMetricsData>
{
    private readonly AbsolutePath artifactDirectory;
    
    public string ComponentName => "Testing";
    
    public IEnumerable<TestMetricsData> GetReportData()
    {
        var dotnetResults = CollectDotNetTests();
        var nodeResults = CollectNodeTests();
        var e2eResults = CollectPlaywrightTests();
        
        yield return new TestMetricsData
        {
            ComponentName = "DotNet",
            TestsTotal = dotnetResults.Total,
            TestsPassed = dotnetResults.Passed,
            TestsFailed = dotnetResults.Failed,
            TestsSkipped = dotnetResults.Skipped,
            Duration = dotnetResults.Duration,
            CoveragePercent = dotnetResults.Coverage,
            ResultsPath = dotnetResults.Path,
        };
        
        yield return new TestMetricsData
        {
            ComponentName = "E2E",
            TestsTotal = e2eResults.Total,
            TestsPassed = e2eResults.Passed,
            TestsFailed = e2eResults.Failed,
            Duration = e2eResults.Duration,
            ResultsPath = e2eResults.Path,
        };
    }
    
    public ComponentReportMetadata GetReportMetadata()
    {
        return new ComponentReportMetadata
        {
            Id = "TestMetrics",
            Title = "Test Execution Report",
            Description = "Test results across .NET, Node.js, and E2E test suites",
            SchemaVersion = "1.0",
            Columns = new[]
            {
                new ReportColumnMetadata { 
                    PropertyName = "ComponentName", 
                    DisplayName = "Test Suite", 
                    DataType = "string", 
                    IncludeInSummary = true, 
                    SortOrder = 1 
                },
                new ReportColumnMetadata { 
                    PropertyName = "TestsTotal", 
                    DisplayName = "Total Tests", 
                    DataType = "int", 
                    IncludeInSummary = true, 
                    SortOrder = 2 
                },
                new ReportColumnMetadata { 
                    PropertyName = "TestsPassed", 
                    DisplayName = "Passed", 
                    DataType = "int", 
                    IncludeInSummary = true, 
                    SortOrder = 3 
                },
                new ReportColumnMetadata { 
                    PropertyName = "TestsFailed", 
                    DisplayName = "Failed", 
                    DataType = "int", 
                    IncludeInSummary = true, 
                    SortOrder = 4 
                },
                new ReportColumnMetadata { 
                    PropertyName = "Duration", 
                    DisplayName = "Duration", 
                    DataType = "timespan", 
                    Format = "mm\\:ss", 
                    IncludeInSummary = true, 
                    SortOrder = 5 
                },
                new ReportColumnMetadata { 
                    PropertyName = "CoveragePercent", 
                    DisplayName = "Coverage %", 
                    DataType = "decimal", 
                    Format = "0.00", 
                    IncludeInSummary = true, 
                    SortOrder = 6 
                },
            },
        };
    }
}
```

---

## Artifact Directory Integration

### Current Structure
```
build/_artifacts/v{VERSION}/
├── dotnet/
│   ├── bin/
│   ├── logs/
│   ├── recordings/
│   ├── test-reports/     # ⭐ NEW
│   └── test-results/     # ⭐ NEW (TRX, coverage)
├── pty/
│   └── dist/
└── web/
    ├── dist/
    ├── test-reports/     # ✅ Playwright HTML
    └── test-results/     # ✅ Screenshots, videos, traces
```

### Report Output Structure
```
build/_artifacts/v{VERSION}/
└── reports/              # ⭐ NFunReport outputs
    ├── CodeQuality/
    │   ├── CodeQualityMetrics.json
    │   ├── CodeQualityMetrics.xml
    │   ├── CodeQualityMetrics.yaml
    │   └── CodeQualityMetrics.md
    ├── Testing/
    │   ├── TestMetrics.json
    │   ├── TestMetrics.xml
    │   ├── TestMetrics.yaml
    │   └── TestMetrics.md
    └── _determinism/     # Determinism check artifacts
```

---

## Report Format Examples

### JSON Output
```json
{
  "id": "TestMetrics",
  "title": "Test Execution Report",
  "generatedAt": "2025-01-08T10:30:00Z",
  "schemaVersion": "1.0",
  "sections": [
    {
      "kind": "Detail",
      "rows": [
        {
          "ComponentName": "DotNet",
          "TestsTotal": 42,
          "TestsPassed": 40,
          "TestsFailed": 2,
          "TestsSkipped": 0,
          "Duration": "00:01:23",
          "CoveragePercent": 85.3,
          "ResultsPath": "dotnet/test-results/test-results.trx"
        },
        {
          "ComponentName": "E2E",
          "TestsTotal": 7,
          "TestsPassed": 6,
          "TestsFailed": 1,
          "Duration": "00:02:45",
          "ResultsPath": "web/test-results/"
        }
      ]
    }
  ]
}
```

### Markdown Output
```markdown
# Test Execution Report

**Generated**: 2025-01-08 10:30:00 UTC  
**Version**: 0.0.1-379

## Summary

| Test Suite | Total | Passed | Failed | Duration | Coverage |
|------------|-------|--------|--------|----------|----------|
| DotNet     | 42    | 40     | 2      | 01:23    | 85.3%    |
| E2E        | 7     | 6      | 1      | 02:45    | N/A      |

## Test Results by Component

### DotNet Tests
- **Total Tests**: 42
- **Passed**: 40
- **Failed**: 2
- **Duration**: 00:01:23
- **Coverage**: 85.3%
- **Results**: `dotnet/test-results/test-results.trx`

### E2E Tests
- **Total Tests**: 7
- **Passed**: 6
- **Failed**: 1
- **Duration**: 00:02:45
- **Results**: `web/test-results/`
```

---

## Configuration

### build-config.json

```json
{
  "projectGroups": {
    "reportProviders": [
      "components/Testing/Reporting/TestReportProvider.csproj"
    ]
  },
  "reporting": {
    "nfunReport": {
      "outputDirectory": "artifacts/v{VERSION}/reports",
      "formats": "json,xml,yaml,md",
      "validateSchema": true,
      "continueOnError": false,
      "parallel": true,
      "maxDegreeOfParallelism": 4,
      "determinismChecks": true
    }
  }
}
```

### Environment Variables

```bash
# Override output directory
export NFUNREPORT_REPORTING_NFUNREPORT_OUTPUTDIRECTORY="custom/path"

# Set formats
export NFUNREPORT_REPORTING_NFUNREPORT_FORMATS="json,md"

# Disable determinism checks for faster builds
export NFUNREPORT_REPORTING_NFUNREPORT_DETERMINISMCHECKS=false
```

---

## Task Integration

### Updated Taskfile.yml

```yaml
# build/Taskfile.yml
version: '3'

vars:
  VERSION:
    sh: ./get-version.sh
  ARTIFACT_DIR: _artifacts/v{{.VERSION}}

tasks:
  init-dirs:
    desc: "Initialize artifact directories"
    cmds:
      - mkdir -p {{.ARTIFACT_DIR}}/dotnet/{bin,logs,recordings,test-reports,test-results/coverage}
      - mkdir -p {{.ARTIFACT_DIR}}/web/{dist,logs,recordings,test-reports,test-results}
      - mkdir -p {{.ARTIFACT_DIR}}/pty/{dist,logs}
      - mkdir -p {{.ARTIFACT_DIR}}/reports
      - mkdir -p {{.ARTIFACT_DIR}}/_logs

  test-dotnet:
    desc: "Run .NET tests with structured output"
    deps: [init-dirs, build-dotnet]
    dir: ../development/dotnet/console
    cmds:
      - |
        dotnet test Console.sln \
          --no-build \
          --logger "trx;LogFileName={{.ARTIFACT_DIR}}/dotnet/test-results/test-results.trx" \
          --logger "html;LogFileName={{.ARTIFACT_DIR}}/dotnet/test-reports/test-report.html" \
          --results-directory {{.ARTIFACT_DIR}}/dotnet/test-results \
          /p:CollectCoverage=true \
          /p:CoverletOutputFormat=cobertura \
          /p:CoverletOutput={{.ARTIFACT_DIR}}/dotnet/test-results/coverage/

  test-e2e:
    desc: "Run E2E tests (Playwright)"
    deps: [init-dirs, build-all]
    dir: ../development/nodejs
    cmds:
      - pnpm test:e2e

  test-all:
    desc: "Run all tests"
    cmds:
      - task: test-dotnet
      - task: test-e2e

  generate-reports:
    desc: "Generate NFunReport outputs from test results"
    deps: [test-all]
    cmds:
      - |
        # If using Nuke build with NFunReport component
        cd nuke-build-dir && nuke GenerateReports --output-directory {{.ARTIFACT_DIR}}/reports

  validate-reports:
    desc: "Validate generated reports"
    deps: [generate-reports]
    cmds:
      - cd nuke-build-dir && nuke ValidateReports

  ci:
    desc: "Full CI pipeline with reporting"
    cmds:
      - task: clean
      - task: build-all
      - task: test-all
      - task: generate-reports
      - task: validate-reports
```

---

## Benefits of NFunReport Integration

1. **Unified Reporting**: All test results in consistent format
2. **Multi-Format**: JSON for CI, Markdown for humans, XML for tools
3. **Versioned**: Reports tied to specific build versions
4. **Validated**: Schema validation ensures correctness
5. **Deterministic**: Reproducible report generation
6. **Aggregated**: Cross-component test metrics in one place
7. **CI-Ready**: Machine-readable formats for GitHub Actions

---

## Next Steps

### Immediate (Option 1 - Extend CodeQuality)
1. ✅ Review CodeQualityReportProvider implementation
2. [ ] Update dotnet test task to output TRX
3. [ ] Implement TRX parser in CodeQualityReportProvider
4. [ ] Add coverage report parsing
5. [ ] Test report generation

### Future (Option 2 - Dedicated TestReportProvider)
1. [ ] Create TestReportProvider component
2. [ ] Implement DotNet test parser (TRX)
3. [ ] Implement Playwright test parser (JSON)
4. [ ] Add PTY test support
5. [ ] Register provider in build-config.json
6. [ ] Create test metrics data model
7. [ ] Add quality gates for tests

---

## References

- **Lunar Report**: `/infra-projects/giantcroissant-lunar-report/`
- **Lunar Build**: `/infra-projects/giantcroissant-lunar-build/`
- **CodeQualityReportProvider**: Example implementation with test metrics
- **INfunReportComponent**: Nuke build target integration
- **RFC008/RFC016**: NFunReport architecture RFCs
- **TEST-OUTPUT-STRUCTURE-ANALYSIS.md**: Current analysis

---

## Questions for Discussion

1. **Option Selection**: Should we extend CodeQuality or create dedicated TestReportProvider?
2. **Report Scope**: Should reports include PTY test scripts or only formal test frameworks?
3. **Coverage**: Should we mandate code coverage collection?
4. **Quality Gates**: What test thresholds should fail the build?
5. **CI Integration**: Should reports be uploaded as GitHub artifacts?
6. **Retention**: How long should test reports be kept?
7. **Real-time**: Should we support real-time test reporting during execution?

# Summary: RFC-0040 Implementation Guide

**Date**: 2025-01-08  
**RFC**: [RFC-0040: Nuke Build Component Integration](../../../docs/rfcs/0040-nuke-build-component-integration.md)

---

## Quick Overview

We're upgrading WingedBean's Nuke build from basic Compile/Clean/Restore to a **component-based system** that gives us automatic test reporting for free.

### Current State
```csharp
class Build : NukeBuild
{
    Target Compile => _ => _
        .Executes(() => { DotNetBuild(...); });
}
```
- ✅ Basic builds work
- ❌ No test reporting
- ❌ No structured metrics

### Target State
```csharp
partial class Build :
    INfunReportComponent,         // Multi-format reports
    IBuildConfigurationComponent,  // Config-driven builds
    IWrapperPathComponent         // Path resolution
{
    // Inherits: GenerateReports, Test metrics collection
}
```
- ✅ Automatic test reporting
- ✅ JSON/XML/YAML/MD outputs
- ✅ Quality gates
- ✅ Configuration-driven

---

## Two Key Changes

### 1. Artifact Path Standardization

**Before:** `build/_artifacts/v0.0.1-379/`  
**After:** `build/_artifacts/0.0.1-379/`

**Why?** Components use `{GitVersion}` token without `v` prefix

**Impact:**
- Update `build/Taskfile.yml`: `ARTIFACT_DIR: _artifacts/{{.VERSION}}`
- Update `build/get-version.sh` if it adds `v` prefix
- Clean old `v*` directories

### 2. Component Integration

**Add:** `build/nuke/build-config.json`
```json
{
  "projectType": "multi-group-build",
  "codeQuality": {
    "outputDirectory": "../_artifacts/{GitVersion}/dotnet/test-results",
    "thresholds": {
      "maxErrors": 0,
      "minCoverage": 80.0
    }
  },
  "reporting": {
    "enabled": true,
    "outputDirectory": "../_artifacts/{GitVersion}/reports"
  }
}
```

**Update:** `build/nuke/build/Build.cs` to implement interfaces

---

## What You Get Automatically

Once components are integrated, **without writing any parser code**:

### Test Metrics Report
**Location:** `_artifacts/{VERSION}/reports/components/codequality/testing-report.json`

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

### How It Works

1. **Your code runs:** `dotnet test` with `--logger "trx"`
2. **Output goes to:** `_artifacts/{VERSION}/dotnet/test-results/test-results.trx`
3. **CodeQualityReportProvider** (from component) automatically:
   - Finds TRX file
   - Parses test counts
   - Reads coverage reports
   - Generates `testing-report.json`
4. **NFunReport** (optional) converts to XML/YAML/MD

**You don't write the parser** - the component does it!

---

## Implementation Checklist

### Phase 1: Path Standardization (30 min)
- [ ] Update `build/Taskfile.yml` - remove `v` from `ARTIFACT_DIR`
- [ ] Check/update `build/get-version.sh` 
- [ ] Test: `task build-all`
- [ ] Clean old `v*` directories

### Phase 2: Basic Integration (1 hour)
- [ ] Create `build/nuke/build-config.json`
- [ ] Add component packages to `_build.csproj`
- [ ] Update `Build.cs` - add interface implementations
- [ ] Test: `cd build/nuke && ./build.sh BuildAll`

### Phase 3: Test Reporting (2 hours)
- [ ] Add `Test` target with TRX output
- [ ] Add `GenerateComponentReports` target
- [ ] Test: `./build.sh Test && ./build.sh GenerateComponentReports`
- [ ] Verify `testing-report.json` exists and has data

### Phase 4: Task Integration (30 min)
- [ ] Update Taskfile to delegate to Nuke
- [ ] Test: `task ci`
- [ ] Verify all reports generated

**Total Estimated Time:** 4-6 hours

---

## Example Commands

### After Implementation

```bash
# Build (same as before)
task build-all

# Run tests with reporting
cd build/nuke
./build.sh Test

# Generate reports
./build.sh GenerateComponentReports

# Full CI pipeline
task ci

# View test metrics
cat build/_artifacts/$(./get-version.sh)/reports/components/codequality/testing-report.json | jq .
```

---

## Benefits Summary

| Before | After |
|--------|-------|
| Console test output only | JSON/XML/YAML/MD reports |
| Manual metric tracking | Automatic collection |
| No quality gates | Configurable thresholds |
| No CI artifacts | Machine-readable results |
| Custom parsing needed | Component handles it |
| Single format | Multi-format output |

---

## Reference Materials

1. **RFC-0040**: Full technical specification
2. **Asset-InOut Example**: `plate-projects/asset-inout/build/nuke/`
3. **Analysis Documents**:
   - `TEST-OUTPUT-STRUCTURE-ANALYSIS.md`
   - `NUKE-REPORTING-ARCHITECTURE-EXPLAINED.md`
   - `NUKE-BUILD-INTEGRATION-PLAN.md`
   - `NUKE-TEST-REPORT-INTEGRATION.md`

---

## Key Insight

**"Component Reports" = Reports FROM Nuke Build Components**

When you use `IBuildConfigurationComponent` and register `CodeQualityReportProvider`, the component:
- Knows where to find test results (from build-config.json)
- Knows how to parse TRX files
- Knows how to generate reports
- **Does it all automatically**

You just:
1. Configure paths in `build-config.json`
2. Output TRX files to those paths
3. Call `GenerateComponentReports` target
4. **Reports appear** ✨

---

## Questions?

- **Q: Do we need to write TRX parsers?**  
  A: No! `CodeQualityReportProvider` has them built-in

- **Q: What about Playwright E2E tests?**  
  A: They already output to `web/test-reports/` - separate system

- **Q: Do we lose control?**  
  A: No - components are configurable via build-config.json

- **Q: Can we customize reports?**  
  A: Yes - implement custom `IReportDataProvider<T>` if needed

---

## Next Steps

1. Review RFC-0040 for full details
2. Start with Phase 1 (path standardization)
3. Test incrementally after each phase
4. Update documentation as you go

---

**Status:** Ready to implement  
**Priority:** P1 (High)  
**Effort:** 6-8 hours

# RFC-0040 Implementation Checklist

Quick reference for implementing Nuke build component integration.

**RFC**: [docs/rfcs/0040-nuke-build-component-integration.md](../docs/rfcs/0040-nuke-build-component-integration.md)  
**Handover**: [development/dotnet/console/HANDOVER-RFC0040-IMPLEMENTATION.md](../development/dotnet/console/HANDOVER-RFC0040-IMPLEMENTATION.md)

---

## Pre-Flight Checks

- [ ] Read RFC-0040 completely
- [ ] Read handover document
- [ ] Create feature branch: `git checkout -b feature/rfc-0040-nuke-components`
- [ ] Verify environment:
  - [ ] `dotnet --version` (8.0+)
  - [ ] `task --version`
  - [ ] `./nuke/build.sh --help`
- [ ] Current build works: `task build-all`

---

## Phase 1: Path Standardization (30 min)

### 1.1 Check Current Version Output
```bash
cd build
./get-version.sh
# Expected: 0.0.1-XXX (no 'v' prefix)
# If it has 'v', update script to remove it
```
- [ ] Version script outputs correct format

### 1.2 Update Taskfile.yml
**File**: `build/Taskfile.yml`
```yaml
# Change line ~14:
vars:
  VERSION:
    sh: ./get-version.sh
  ARTIFACT_DIR: _artifacts/{{.VERSION}}  # ← Remove 'v' prefix
```
- [ ] Updated `ARTIFACT_DIR` variable
- [ ] Saved file

### 1.3 Search for Hardcoded Paths
```bash
cd ..
rg "_artifacts/v\{" --type yaml --type md
# Fix any hardcoded references found
```
- [ ] No hardcoded `v{VERSION}` patterns remain

### 1.4 Test Build with New Paths
```bash
cd build
task build-all
ls -la _artifacts/
# Should see: 0.0.1-XXX (no 'v' prefix)
```
- [ ] Build succeeds
- [ ] Artifacts in correct location
- [ ] Commit: `git commit -am "chore: standardize artifact paths (remove v prefix)"`

### 1.5 Clean Old Directories (Optional)
```bash
cd _artifacts
# Backup first if needed
for dir in v0.0.1-*; do
  new="${dir#v}"
  [ -d "$dir" ] && [ ! -d "$new" ] && mv "$dir" "$new"
done
```
- [ ] Old `v*` directories migrated or cleaned

---

## Phase 2: Component Integration (1-2 hours)

### 2.1 Create build-config.json
**File**: `build/nuke/build-config.json`
```bash
# Copy template from RFC-0040 Section 2
# Or from: development/dotnet/console/HANDOVER-RFC0040-IMPLEMENTATION.md
```
- [ ] File created with full template
- [ ] Paths verified (relative to `build/nuke/`)
- [ ] JSON valid: `jq . build-config.json`

### 2.2 Update _build.csproj
**File**: `build/nuke/build/_build.csproj`

Add package references:
```xml
<!-- After existing PackageReference items -->
<PackageReference Include="Lunar.Build.Configuration" Version="*" />
<PackageReference Include="Lunar.Build.Components" Version="*" />
<PackageReference Include="Lunar.Build.CodeQuality" Version="*" />
<PackageReference Include="Lunar.NfunReport.MNuke" Version="*" />
```
- [ ] Package references added
- [ ] File saved

### 2.3 Restore Packages
```bash
cd build/nuke
dotnet restore build/_build.csproj
# If packages not found, may need to build them locally
```
- [ ] Packages restored successfully
- [ ] OR: Alternative approach documented (local build/ProjectReference)

### 2.4 Update Build.cs
**File**: `build/nuke/build/Build.cs`

**Major changes** (see RFC-0040 Section 3 for complete code):
1. Add using statements
2. Change class declaration to implement interfaces
3. Add interface properties
4. Add BuildAll target
5. Add Test target (comes in Phase 3)

**Quick verify compile**:
```bash
cd build/nuke
dotnet build build/_build.csproj
```
- [ ] Using statements added
- [ ] Class implements interfaces
- [ ] Interface members implemented
- [ ] BuildAll target added
- [ ] Compiles successfully
- [ ] Commit: `git commit -am "feat: integrate Nuke build components"`

### 2.5 Test Basic Build
```bash
cd build/nuke
./build.sh --help
# Should list: BuildAll, Compile, Clean, Restore, etc.

./build.sh BuildAll
# Should build successfully using component discovery
```
- [ ] Help shows all targets
- [ ] BuildAll works
- [ ] Projects discovered correctly

---

## Phase 3: Test Reporting (2 hours)

### 3.1 Add Test Target to Build.cs
**File**: `build/nuke/build/Build.cs`

Add Test target (see RFC-0040 Section 3):
```csharp
public Target Test => _ => _
    .Description("Run tests with structured output")
    .DependsOn(BuildAll)
    .Executes(() =>
    {
        // ... (full code in RFC)
    });
```
- [ ] Test target added
- [ ] TRX logger configured
- [ ] HTML logger configured
- [ ] Coverage collection configured
- [ ] Output paths use {GitVersion} (no v)
- [ ] Compiles successfully

### 3.2 Test TRX Output
```bash
cd build/nuke
./build.sh Test

# Verify outputs
ls -la ../../_artifacts/0.0.1-*/dotnet/test-results/
```

**Expected files**:
- [ ] `test-results.trx` exists
- [ ] `test-report.html` exists
- [ ] `coverage/coverage.cobertura.xml` exists
- [ ] TRX file is valid XML
- [ ] Commit: `git commit -am "feat: add Test target with structured output"`

### 3.3 Add GenerateComponentReports Target
**File**: `build/nuke/build/Build.cs`

Add GenerateComponentReports target (see RFC-0040 Section 3):
```csharp
public Target GenerateComponentReports => _ => _
    .Description("Generate reports from build components")
    .DependsOn(Test)
    .Executes(async () =>
    {
        // ... (full code in RFC)
    });
```
- [ ] Target added
- [ ] ComponentReportCoordinator created
- [ ] CodeQualityReportProvider registered
- [ ] Report generation logic added
- [ ] Compiles successfully

### 3.4 Test Report Generation
```bash
./build.sh GenerateComponentReports

# Verify outputs
ls -la ../../_artifacts/0.0.1-*/reports/
```

**Expected structure**:
- [ ] `reports/components/` directory exists
- [ ] `reports/components/codequality/` directory exists
- [ ] `component-report.json` exists
- [ ] `testing-report.json` exists
- [ ] `analysis-report.json` exists
- [ ] Commit: `git commit -am "feat: add component report generation"`

### 3.5 Verify Test Metrics
```bash
cat ../../_artifacts/0.0.1-*/reports/components/codequality/testing-report.json | jq .
```

**Check values**:
- [ ] `TestsDiscovered` > 0
- [ ] `TestsExecuted` > 0
- [ ] `TestsPassed` > 0
- [ ] Numbers match actual test run
- [ ] `TestCoveragePercentage` present (if coverage ran)
- [ ] `GeneratedAt` timestamp present

### 3.6 Add CI Target
**File**: `build/nuke/build/Build.cs`
```csharp
public Target CI => _ => _
    .Description("Complete CI workflow")
    .DependsOn(BuildAll)
    .DependsOn(Test)
    .DependsOn(GenerateComponentReports)
    .Executes(() =>
    {
        Log.Information("✅ CI pipeline completed");
    });
```
- [ ] CI target added
- [ ] Compiles
- [ ] `./build.sh CI` runs full pipeline
- [ ] All reports generated
- [ ] Commit: `git commit -am "feat: add CI target for full pipeline"`

---

## Phase 4: Task Integration (30 min)

### 4.1 Add Nuke Delegation Tasks
**File**: `build/Taskfile.yml`

Add new tasks:
```yaml
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

show-reports:
  desc: "Show location of generated reports"
  cmds:
    - echo "📊 Reports: {{.ARTIFACT_DIR}}/reports/"
    - echo "📋 Test Results: {{.ARTIFACT_DIR}}/dotnet/test-results/"

open-test-report:
  desc: "Open test report in browser"
  cmds:
    - open {{.ARTIFACT_DIR}}/dotnet/test-results/test-report.html
```
- [ ] New tasks added
- [ ] YAML valid: `task --list`

### 4.2 Update Legacy Tasks
Update existing tasks to delegate:
```yaml
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
```
- [ ] Legacy tasks updated
- [ ] Still work as before

### 4.3 Test Task Integration
```bash
cd build
task build-all        # Should call Nuke
task test-dotnet      # Should run tests
task nuke-reports     # Should generate reports
task show-reports     # Should show paths
task ci               # Should run full pipeline
```
- [ ] All tasks work
- [ ] Reports generated
- [ ] Commit: `git commit -am "feat: integrate Nuke targets with Task orchestration"`

---

## Phase 5: Documentation & Cleanup (30 min)

### 5.1 Update Build Documentation
**File**: `build/README.md` (create if needed)
- [ ] Document new Nuke targets
- [ ] Document Task commands
- [ ] Document report locations
- [ ] Add troubleshooting section

### 5.2 Update Root README (if needed)
- [ ] Mention component-based build
- [ ] Link to RFC-0040
- [ ] Update build instructions

### 5.3 Verify All Phases
```bash
# Full clean build
cd build
task clean
task ci

# Verify all outputs
ls -la _artifacts/0.0.1-*/
ls -la _artifacts/0.0.1-*/dotnet/test-results/
ls -la _artifacts/0.0.1-*/reports/components/codequality/
```
- [ ] Clean build works
- [ ] All artifacts in correct locations
- [ ] All reports generated
- [ ] Test metrics accurate

### 5.4 Final Commit and PR
```bash
git add .
git commit -am "docs: update build documentation for RFC-0040"
git push origin feature/rfc-0040-nuke-components
# Create PR
```
- [ ] All changes committed
- [ ] PR created
- [ ] RFC-0040 marked as Implemented (after merge)

---

## Success Criteria

### Must Have (Phase 1-3)
- [x] Artifact paths use `{GitVersion}` (no `v`)
- [x] build-config.json created and valid
- [x] Build.cs implements component interfaces
- [x] Test target outputs TRX files
- [x] GenerateComponentReports works
- [x] testing-report.json has accurate metrics

### Should Have (Phase 4)
- [x] Task commands delegate to Nuke
- [x] CI pipeline runs end-to-end
- [x] Documentation updated

### Nice to Have
- [ ] NFunReport multi-format output (XML, YAML, MD)
- [ ] Aggregated report includes multiple components
- [ ] Quality gates enforced

---

## Troubleshooting

### Issue: Component packages not found
**Solution**: Build locally from `infra-projects/giantcroissant-lunar-build`
```bash
cd ../../../infra-projects/giantcroissant-lunar-build/build/nuke
./build.sh Pack
# Packages go to local NuGet repo
```

### Issue: TRX file not found by parser
**Check**:
1. TRX file exists: `ls _artifacts/*/dotnet/test-results/*.trx`
2. Path matches build-config.json: `codeQuality.outputDirectory`
3. File is valid XML: `xmllint --noout file.trx`

### Issue: Zero tests in report
**Check**:
1. Tests actually ran: Check console output
2. TRX file has test results: `grep "TestRun" file.trx`
3. CodeQualityReportProvider logs for parsing errors

### Issue: Compilation errors in Build.cs
**Solution**: Copy working implementation from `asset-inout/build/nuke/build/Build.cs`

---

## Quick Commands Reference

```bash
# Path check
cd build && ./get-version.sh

# Build
cd build/nuke && ./build.sh BuildAll

# Test with reporting
./build.sh Test && ./build.sh GenerateComponentReports

# Full pipeline
./build.sh CI

# Via Task
cd ../.. && task ci

# View reports
cat _artifacts/0.0.1-*/reports/components/codequality/testing-report.json | jq .
open _artifacts/0.0.1-*/dotnet/test-results/test-report.html
```

---

**Estimated Time**: 4-6 hours total  
**Status**: Ready to implement  
**Next**: Phase 1 - Path Standardization

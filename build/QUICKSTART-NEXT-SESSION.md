# Quick Start - Next Session

**Goal**: Implement RFC-0040 Nuke Build Component Integration  
**Time**: 4-6 hours  
**Current Version**: 0.0.1-379

---

## 🚀 Start Here

1. **Read the handover document** (5 min)
   ```bash
   cat development/dotnet/console/HANDOVER-RFC0040-IMPLEMENTATION.md
   ```

2. **Create feature branch** (1 min)
   ```bash
   git checkout -b feature/rfc-0040-nuke-components
   ```

3. **Open checklist** (keep visible)
   ```bash
   open build/RFC0040-IMPLEMENTATION-CHECKLIST.md
   ```

4. **Open reference implementation** (for copy-paste)
   ```bash
   open ../../../plate-projects/asset-inout/build/nuke/build/Build.cs
   open ../../../plate-projects/asset-inout/build/nuke/build-config.json
   ```

---

## 📝 Implementation Order

### Phase 1: Path Standardization (30 min) ⭐ START HERE

**What**: Remove `v` prefix from artifact paths

**Files to change**:
- `build/Taskfile.yml` - line ~14: `ARTIFACT_DIR: _artifacts/{{.VERSION}}`
- `build/get-version.sh` - check if it adds `v` (may not need change)

**Verify**:
```bash
cd build
./get-version.sh  # Should output: 0.0.1-XXX (no v)
task build-all    # Should create _artifacts/0.0.1-XXX/
```

**Commit**: `git commit -am "chore: standardize artifact paths (remove v prefix)"`

---

### Phase 2: Component Integration (1-2 hours)

**What**: Create build-config.json and update Build.cs

**Steps**:
1. **Create** `build/nuke/build-config.json`
   - Copy template from RFC-0040 or handover doc
   - Verify paths are relative to `build/nuke/`

2. **Update** `build/nuke/build/_build.csproj`
   - Add 4 package references (Lunar.Build.* packages)
   - Run `dotnet restore`

3. **Update** `build/nuke/build/Build.cs`
   - Add using statements
   - Change: `class Build : NukeBuild` → `partial class Build : INfunReportComponent, ...`
   - Add interface members
   - Add BuildAll target
   - Copy from `asset-inout/Build.cs` as reference

**Verify**:
```bash
cd build/nuke
dotnet build build/_build.csproj  # Should compile
./build.sh BuildAll                # Should work
```

**Commit**: `git commit -am "feat: integrate Nuke build components"`

---

### Phase 3: Test Reporting (2 hours) ⭐ THE MAIN EVENT

**What**: Add Test target and component report generation

**Steps**:
1. **Add Test target** to Build.cs
   - Configure TRX logger
   - Configure HTML logger
   - Enable coverage collection
   - Output to `{GitVersion}/dotnet/test-results/`

2. **Add GenerateComponentReports target** to Build.cs
   - Create ComponentReportCoordinator
   - Register CodeQualityReportProvider
   - Generate reports to `{GitVersion}/reports/`

3. **Add CI target**
   - Chain: BuildAll → Test → GenerateComponentReports

**Verify**:
```bash
./build.sh Test
# Check: _artifacts/0.0.1-*/dotnet/test-results/test-results.trx exists

./build.sh GenerateComponentReports
# Check: _artifacts/0.0.1-*/reports/components/codequality/testing-report.json exists

# View test metrics
cat ../../_artifacts/0.0.1-*/reports/components/codequality/testing-report.json | jq .
```

**Look for**:
```json
{
  "TestsDiscovered": 42,
  "TestsPassed": 40,
  "TestsFailed": 2,
  "TestCoveragePercentage": 85.3
}
```

**Commits**: 
- `git commit -am "feat: add Test target with structured output"`
- `git commit -am "feat: add component report generation"`
- `git commit -am "feat: add CI target"`

---

### Phase 4: Task Integration (30 min)

**What**: Update Taskfile.yml to delegate to Nuke

**Steps**:
1. Add new tasks: `nuke-build`, `nuke-test`, `nuke-reports`, `nuke-ci`
2. Update legacy tasks to delegate

**Verify**:
```bash
cd build
task ci  # Should run full pipeline via Nuke
```

**Commit**: `git commit -am "feat: integrate Nuke targets with Task"`

---

## 📋 Files You'll Edit

### Must Edit
1. ✏️ `build/Taskfile.yml` - Remove `v`, add Nuke tasks
2. ✏️ `build/nuke/build-config.json` - **CREATE NEW**
3. ✏️ `build/nuke/build/_build.csproj` - Add package refs
4. ✏️ `build/nuke/build/Build.cs` - Implement interfaces, add targets

### May Edit
5. 🔍 `build/get-version.sh` - Check if adds `v`

### Reference (Don't Edit, Just Read)
- `plate-projects/asset-inout/build/nuke/build/Build.cs`
- `plate-projects/asset-inout/build/nuke/build-config.json`
- `docs/rfcs/0040-nuke-build-component-integration.md`

---

## 🎯 Success = These Files Exist

After implementation, verify these exist:

```
build/_artifacts/0.0.1-XXX/                    # ← No 'v' prefix
├── dotnet/
│   └── test-results/
│       ├── test-results.trx                   # ← Phase 3
│       ├── test-report.html                   # ← Phase 3
│       └── coverage/coverage.cobertura.xml    # ← Phase 3
└── reports/
    └── components/
        └── codequality/
            ├── component-report.json          # ← Phase 3
            ├── testing-report.json            # ← Phase 3 ⭐
            ├── analysis-report.json           # ← Phase 3
            └── formatting-report.json         # ← Phase 3
```

---

## ⚠️ If Something Goes Wrong

### Can't find component packages?
```bash
# Option 1: Build locally
cd ../../../infra-projects/giantcroissant-lunar-build/build/nuke
./build.sh Pack

# Option 2: Use ProjectReference temporarily
# Change PackageReference to ProjectReference in _build.csproj
```

### Compilation errors in Build.cs?
```bash
# Copy working implementation
cp ../../../plate-projects/asset-inout/build/nuke/build/Build.cs build/nuke/build/Build.cs
# Then adapt paths/names for WingedBean
```

### TRX file not parsed?
```bash
# Verify TRX exists
ls _artifacts/*/dotnet/test-results/*.trx

# Check it's valid XML
xmllint --noout _artifacts/*/dotnet/test-results/*.trx

# Check path matches build-config.json
cat build/nuke/build-config.json | jq .codeQuality.outputDirectory
```

### Zero tests in report?
```bash
# Check tests actually ran
./build.sh Test | grep "Passed.*Failed"

# Check TRX has results
grep -c "TestRun" _artifacts/*/dotnet/test-results/*.trx
```

---

## 📚 Documentation Links

**Full details**:
- RFC-0040: `docs/rfcs/0040-nuke-build-component-integration.md`
- Handover: `development/dotnet/console/HANDOVER-RFC0040-IMPLEMENTATION.md`
- Checklist: `build/RFC0040-IMPLEMENTATION-CHECKLIST.md`

**Quick references**:
- Summary: `development/dotnet/console/SUMMARY-RFC0040-IMPLEMENTATION.md`
- Architecture: `development/dotnet/console/NUKE-REPORTING-ARCHITECTURE-EXPLAINED.md`

**Working example**:
- Asset-InOut: `../../../plate-projects/asset-inout/build/nuke/`

---

## 💡 Key Insights to Remember

1. **"Component Reports" = Reports FROM Nuke Components**
   - CodeQualityReportProvider automatically parses TRX files
   - You don't write the parser, just point it at the files

2. **Path Format Matters**
   - Components use `{GitVersion}` token
   - We use `v{GitVersion}` currently
   - Phase 1 fixes this mismatch

3. **Two Systems Work Together**
   - Task = Multi-language orchestrator
   - Nuke = .NET build + reporting
   - Task delegates to Nuke, Nuke does the work

4. **Copy-Paste is OK**
   - Asset-InOut is the reference
   - Copy their patterns, adapt paths
   - Don't reinvent the wheel

---

## ⏱️ Time Budget

- ☕ **Phase 1**: 30 min (path changes, easy)
- ☕☕ **Phase 2**: 1-2 hours (config + code structure)
- ☕☕☕ **Phase 3**: 2 hours (the meat - test reporting)
- ☕ **Phase 4**: 30 min (Task integration, easy)

**Total**: 4-6 hours

Take breaks between phases! ☕

---

## ✅ Done? Verify Everything Works

```bash
# Clean slate
cd build
task clean

# Full pipeline
task ci

# Check outputs
ls _artifacts/0.0.1-*/reports/components/codequality/testing-report.json
cat _artifacts/0.0.1-*/reports/components/codequality/testing-report.json | jq .

# Open test report in browser
open _artifacts/0.0.1-*/dotnet/test-results/test-report.html
```

If all of above works → **SUCCESS!** 🎉

---

**Ready to start?** 

1. Create branch: `git checkout -b feature/rfc-0040-nuke-components`
2. Open checklist: `open build/RFC0040-IMPLEMENTATION-CHECKLIST.md`
3. Begin Phase 1! 🚀

---

**Good luck!** 💪

Remember: The goal is **automatic test reporting**. Once done, test metrics appear in JSON files without writing any parsers!

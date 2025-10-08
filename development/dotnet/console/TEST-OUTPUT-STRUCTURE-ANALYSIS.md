# Test Output Structure Analysis

**Date**: 2025-01-08  
**Context**: Evaluating test output organization across dotnet, pty, and web components

---

## Current Artifact Structure

```
build/_artifacts/v{VERSION}/
├── _logs/                    # Build logs (all components)
├── dotnet/
│   ├── bin/                  # Compiled .NET binaries
│   ├── logs/                 # Runtime logs
│   └── recordings/           # Runtime recordings (if any)
├── pty/
│   ├── dist/                 # PTY service files
│   └── logs/                 # PTY runtime logs
└── web/
    ├── dist/                 # Built website (Astro docs)
    ├── logs/                 # Web build logs
    ├── recordings/           # Asciinema recordings
    ├── test-reports/         # Playwright HTML reports ⭐
    └── test-results/         # Playwright test artifacts ⭐
```

---

## Test Reports vs Test Results (Playwright Terminology)

### `test-reports/` - Human-Readable Reports
- **Purpose**: HTML reports for viewing test run summaries
- **Content**: 
  - Interactive HTML dashboard (`index.html`)
  - Test execution timeline
  - Pass/fail statistics
  - Aggregated test results
  - Navigation UI for exploring results
- **Audience**: Developers, QA, CI dashboards
- **When**: Generated after all tests complete
- **Config**: `reporter: [['html', { outputFolder: path }]]`

### `test-results/` - Test Artifacts & Evidence
- **Purpose**: Raw test execution artifacts for debugging
- **Content**:
  - Screenshots (on failure or always, depending on config)
  - Videos (on failure: `video: 'retain-on-failure'`)
  - Traces (on failure: `trace: 'retain-on-failure'`)
  - Test-specific folders (one per test)
  - Raw data files
- **Audience**: Developers debugging failed tests
- **When**: Generated during test execution
- **Config**: `outputDir: path`

### Example Structure After Running Tests

```
test-reports/
  ├── index.html              # Main report dashboard
  ├── data/
  │   ├── test-results.json
  │   └── ...
  └── assets/
      ├── styles.css
      └── ...

test-results/
  ├── arrow-keys-chromium/
  │   ├── test-failed-1.png
  │   ├── video.webm
  │   └── trace.zip
  ├── docs-site-chromium/
  │   └── ...
  └── .last-run.json
```

---

## Current Test Coverage by Component

### 1. Web (Playwright E2E Tests) ✅
- **Location**: `development/nodejs/tests/e2e/*.spec.js`
- **Test Types**: 
  - E2E browser tests
  - Visual verification
  - PTY integration (via web interface)
  - Documentation site tests
- **Output**: 
  - ✅ `web/test-reports/` (HTML reports)
  - ✅ `web/test-results/` (screenshots, videos, traces)
- **Runner**: Playwright
- **Command**: `task test-e2e` (from build/)

### 2. .NET (Unit & Integration Tests) ⚠️
- **Location**: 
  - `development/dotnet/console/tests/host/ConsoleDungeon.Host.Tests/`
  - `development/dotnet/console/tests/plugins/*Tests/`
  - `development/dotnet/framework/tests/*/`
- **Test Types**:
  - Unit tests (xUnit)
  - Integration tests
  - Plugin lifecycle tests
- **Output**: 
  - ❌ Currently: Console output only (no structured output to artifacts)
  - ⚠️ No `dotnet/test-reports/`
  - ⚠️ No `dotnet/test-results/`
- **Runner**: `dotnet test`
- **Command**: `task console:test` or direct `dotnet test Console.sln`

### 3. PTY Service (Node.js Tests) ⚠️
- **Location**: `development/nodejs/pty-service/test-*.js`
- **Test Files**:
  - `test-client.js`
  - `test-flow.js`
  - `test-pty.js`
  - `test-recording-manager.js`
- **Test Types**: 
  - Unit tests (ad-hoc)
  - Integration tests
- **Output**:
  - ❌ Currently: Console output only
  - ⚠️ No `pty/test-reports/`
  - ⚠️ No `pty/test-results/`
- **Runner**: Direct `node` execution (not a formal test framework)
- **Command**: None (manual execution)

---

## Should We Add test-reports/test-results to dotnet and pty?

### Analysis

#### For .NET (`dotnet/`)

**YES** - Structured test output is beneficial:

**Reasons**:
1. ✅ **Formal Test Suite Exists**: Multiple test projects with xUnit
2. ✅ **CI Integration**: Needs machine-readable results (e.g., TRX, JUnit XML)
3. ✅ **Test Reports**: xUnit can generate HTML reports with ReportGenerator
4. ✅ **Artifacts**: Coverage reports, test logs belong with artifacts
5. ✅ **Consistency**: Aligns with web testing pattern

**Implementation**:
```yaml
# build/Taskfile.yml
test-dotnet:
  desc: "Run .NET tests with structured output"
  dir: ../development/dotnet/console
  cmds:
    - |
      dotnet test Console.sln \
        --logger "trx;LogFileName={{.ARTIFACT_DIR}}/dotnet/test-results/test-results.trx" \
        --logger "html;LogFileName={{.ARTIFACT_DIR}}/dotnet/test-reports/test-report.html" \
        --results-directory {{.ARTIFACT_DIR}}/dotnet/test-results \
        -- RunConfiguration.CollectSourceInformation=true
```

**Benefits**:
- CI can parse TRX files
- HTML reports for humans
- Coverage reports alongside code
- Historical test data per version

#### For PTY Service (`pty/`)

**MAYBE** - Depends on test formalization:

**Current State**:
- ❌ No formal test framework (Mocha, Jest, Vitest)
- ❌ Tests are ad-hoc scripts
- ❌ No test runner infrastructure
- ✅ Tests are copied to `pty/dist/` during build

**Options**:

##### Option A: Formalize with Test Framework
If we add a proper test framework (e.g., Vitest or Mocha):
```yaml
test-pty:
  desc: "Run PTY service tests"
  dir: {{.ARTIFACT_DIR}}/pty/dist
  cmds:
    - npm test -- --reporter=html --outputFile=../test-reports/index.html
```

**Benefits**:
- Structured test output
- Better test organization
- CI-friendly reports

##### Option B: Keep Ad-Hoc Scripts
Current approach is fine for:
- Manual verification scripts
- Development-time debugging
- Quick smoke tests

**Decision**: 
- **Short term**: Keep as-is (ad-hoc)
- **Long term**: Formalize if PTY tests become critical or frequent

---

## Recommended Structure

### Proposed Artifact Layout

```
build/_artifacts/v{VERSION}/
├── _logs/                    # Build logs for all components
│   ├── dotnet-build.log
│   ├── web-build.log
│   └── pty-build.log
│
├── dotnet/
│   ├── bin/                  # Compiled binaries
│   ├── logs/                 # Runtime logs
│   ├── recordings/           # Runtime recordings
│   ├── test-reports/         # ⭐ NEW: HTML test reports
│   │   └── index.html
│   └── test-results/         # ⭐ NEW: TRX files, coverage
│       ├── test-results.trx
│       └── coverage/
│
├── pty/
│   ├── dist/                 # PTY service files
│   ├── logs/                 # Runtime logs
│   ├── test-reports/         # 💭 FUTURE: If formalized
│   └── test-results/         # 💭 FUTURE: If formalized
│
└── web/
    ├── dist/                 # Built website
    ├── logs/                 # Build logs
    ├── recordings/           # Asciinema recordings
    ├── test-reports/         # ✅ CURRENT: Playwright HTML
    │   └── index.html
    └── test-results/         # ✅ CURRENT: Screenshots, videos, traces
        └── [test-name]/
```

---

## Implementation Priority

### High Priority ⭐
1. **Add `dotnet/test-reports/` and `dotnet/test-results/`**
   - Immediate value for CI integration
   - Multiple test projects already exist
   - Standard .NET tooling support

### Medium Priority 🔶
2. **Update `init-dirs` task to create dotnet test directories**
   ```yaml
   init-dirs:
     cmds:
       - mkdir -p _artifacts/v{{.VERSION}}/dotnet/{bin,recordings,logs,test-reports,test-results}
   ```

3. **Add `test-dotnet` task with structured output**
4. **Update CI pipeline to collect .NET test results**

### Low Priority / Future 💭
5. **Formalize PTY tests** (only if needed)
   - Add test framework
   - Structure test suite
   - Generate reports

---

## Benefits of Consistent Test Output Structure

1. **Unified Test Results Location**: All test outputs in versioned artifacts
2. **CI Integration**: Machine-readable formats (TRX, JUnit XML)
3. **Historical Analysis**: Test trends across versions
4. **Debugging**: Artifacts tied to specific versions
5. **Documentation**: Self-documenting via directory structure
6. **Cleanup**: Easy to delete old test results with old versions

---

## Next Steps

1. ✅ Review this analysis
2. [ ] Update `build/Taskfile.yml` to create dotnet test directories
3. [ ] Configure dotnet test to output TRX and HTML reports
4. [ ] Test the dotnet test output workflow
5. [ ] Update CI pipeline to collect dotnet test results
6. [ ] Document test output conventions
7. [ ] Decide on PTY test formalization (future)

---

## Questions for Discussion

1. **Report Format**: Should we use ReportGenerator for .NET HTML reports, or stick with TRX for CI?
2. **Coverage**: Should we collect code coverage and store it in `test-results/coverage/`?
3. **PTY Tests**: Should we formalize PTY tests now or wait?
4. **Naming**: Is `test-reports` vs `test-results` clear enough? (Reports = human, Results = machine)
5. **Retention**: How many versions of test results should we keep?
6. **CI Artifacts**: Should CI upload test results as GitHub artifacts?

---

## Related Documents

- `TEST-OUTPUT-STRATEGY.md` - Current web testing strategy
- `HANDOVER-TESTING-SESSION.md` - Testing infrastructure session notes
- `build/Taskfile.yml` - Build orchestration
- `development/nodejs/playwright.config.js` - Playwright configuration

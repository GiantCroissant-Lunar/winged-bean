# E2E Tests for ConsoleDungeon.Host

This directory contains comprehensive end-to-end tests for the ConsoleDungeon.Host Terminal.Gui v2 application.

## Test Suites

### 1. Robust State-Based Tests (16 tests)
- **HealthCheckE2ETests** - Query actual system state via HTTP
- **NegativeScenarioE2ETests** - Error handling and recovery
- **BehaviorVerificationE2ETests** - Verify plugins actually work

[See Robust Testing Guide](./README-ROBUST-TESTING.md)

### 2. Visual Regression Tests (5 tests)
- **AsciinemaVisualRegressionTests** - TUI visual baselines with asciinema
- Detects layout changes, color regressions, positioning issues
- Human-reviewable recordings

### 3. Artifact-Based Tests (5 tests)
- **ArtifactBasedE2ETests** - Build convention compliance (R-RUN-020)
- Verifies tests run from `build/_artifacts/v{version}/`
- Ensures flattened plugin layout works

### 4. Legacy Tests (2 test files)
- **HostStartupE2ETests** - Original log-pattern-matching tests
- **MultiModeE2ETests** - Console/PTY/WebSocket mode tests

## Quick Start

### Prerequisites

```bash
# Install tools
brew install asciinema expect

# Build artifacts
cd ../../../../build
task build-all
```

### Run All Tests

```bash
cd WingedBean.Tests.E2E.ConsoleDungeon

# All E2E tests
dotnet test --filter "Category=E2E"

# By type
dotnet test --filter "Type=HealthCheck"     # Health checks
dotnet test --filter "Type=Negative"        # Error scenarios
dotnet test --filter "Type=Behavior"        # Behavior verification
dotnet test --filter "Type=Visual"          # Visual regression
dotnet test --filter "Type=Artifact"        # Artifact compliance
```

### Create Visual Baselines (First Run)

```bash
# First run creates baselines
dotnet test --filter "Type=Visual"

# Output: "⚠️  No baseline. Saved as baseline."
# Baselines saved to: recordings/baselines/
```

### Watch Recordings

```bash
# View a baseline recording
asciinema play recordings/baselines/startup-screen.cast

# View a test run
asciinema play recordings/test-runs/startup-screen-*.cast
```

## Test Categories

| Category | Count | Purpose | Status |
|----------|-------|---------|--------|
| HealthCheck | 3 | Query system state | ✅ Ready |
| Negative | 7 | Error handling | ✅ Ready |
| Behavior | 6 | Plugin functionality | 🚧 Needs endpoints |
| Visual | 5 | TUI regression | ✅ Ready |
| Artifact | 5 | Build conventions | ✅ Ready |
| **Total** | **26** | **Comprehensive E2E** | **Ready** |

## Directory Structure

```
e2e/
├── recordings/                          # Asciinema recordings
│   ├── baselines/                      # Golden recordings
│   │   ├── startup-screen.cast
│   │   ├── menu-navigation.cast
│   │   └── error-dialog.cast
│   └── test-runs/                      # Test recordings (timestamped)
│
├── WingedBean.Tests.E2E.ConsoleDungeon/
│   ├── HealthCheckE2ETests.cs          # HTTP state queries
│   ├── NegativeScenarioE2ETests.cs     # Error scenarios
│   ├── BehaviorVerificationE2ETests.cs # Plugin behavior
│   ├── AsciinemaVisualRegressionTests.cs # TUI visual tests
│   ├── ArtifactBasedE2ETests.cs        # Build conventions
│   ├── HostStartupE2ETests.cs          # Legacy startup tests
│   └── MultiModeE2ETests.cs            # Legacy mode tests
│
├── README.md                            # This file
├── README-ROBUST-TESTING.md            # Robust testing guide
└── README-INTERACTIVE-TESTS.md         # Interactive testing guide
```

## Test Examples

### Health Check Test
```csharp
// Query actual state via HTTP
var health = await httpClient.GetFromJsonAsync<HealthCheckResponse>(
    "http://localhost:5555/health");

Assert.Equal("Healthy", health.Status);
Assert.True(health.PluginsLoaded > 0);
```

### Visual Regression Test
```csharp
// Record TUI session
var recording = await RecordTUISession("startup-screen", ...);

// Compare with baseline
var diff = await CompareAsciinemaRecordings(baseline, recording);

// Assert similarity
Assert.True(diff.SimilarityScore >= 0.90, "Visual regression!");
```

### Artifact Test
```csharp
// Verify runs from versioned artifacts (R-RUN-020)
var startInfo = CreateArtifactBasedProcessStartInfo();
// Path: build/_artifacts/v{version}/dotnet/bin/ConsoleDungeon.Host

Assert.DoesNotContain("dotnet run", output);
```

## Integration Status

### ✅ Ready to Run
- Visual regression tests
- Artifact-based tests
- Legacy startup tests

### 🚧 Requires Integration
- Health check tests (need TestingEndpoints in host)
- Negative scenario tests (need error simulation flags)
- Behavior tests (need behavior endpoints)

[See Integration Guide](../../../../INTEGRATION-CHECKLIST.md)

## CI/CD Integration

### GitHub Actions Example

```yaml
- name: Build Artifacts
  run: |
    cd build
    task build-all

- name: Run E2E Tests
  run: |
    cd development/dotnet/console/tests/e2e/WingedBean.Tests.E2E.ConsoleDungeon
    dotnet test --filter "Category=E2E"
```

## Documentation

- **[Asciinema Visual Testing](../../../../../docs/HANDOVER-2025-10-08-Asciinema-Visual-Testing.md)** - TUI regression testing
- **[Robust E2E Testing](../../../../../docs/HANDOVER-2025-10-08-Robust-E2E-Testing.md)** - State-based testing
- **[Summary](../../../../../docs/SUMMARY-Asciinema-And-Artifacts.md)** - Overview of all improvements

## Troubleshooting

### Issue: Visual tests fail

```bash
# Watch recordings to compare
asciinema play recordings/baselines/test-name.cast
asciinema play recordings/test-runs/test-name-*.cast

# If changes are intentional, update baseline
rm recordings/baselines/test-name.cast
dotnet test --filter "TestName"
```

### Issue: Artifact tests fail

```bash
# Build artifacts first
cd ../../../../build
task build-all

# Check version
./get-version.sh

# Verify artifacts exist
ls -la _artifacts/v*/dotnet/bin/
```

### Issue: Health check tests timeout

```bash
# These require host integration
# See: INTEGRATION-CHECKLIST.md

# For now, skip them
dotnet test --filter "Category=E2E&Type!=HealthCheck&Type!=Behavior"
```

## Benefits

### Robustness
- ✅ State-based assertions (not log parsing)
- ✅ Visual regression detection
- ✅ Error scenario coverage
- ✅ Build convention enforcement

### Coverage
- ✅ 26 comprehensive E2E tests
- ✅ Health checks, behavior, errors, visuals
- ✅ Multiple test approaches
- ✅ Human-reviewable artifacts

### Maintainability
- ✅ Tests won't break on log format changes
- ✅ Visual diffs are easy to review
- ✅ Clear separation of concerns
- ✅ Well documented

## Next Steps

1. Build artifacts: `cd ../../../../build && task build-all`
2. Run tests: `dotnet test`
3. Create visual baselines: `dotnet test --filter "Type=Visual"`
4. Review recordings: `asciinema play recordings/baselines/*.cast`
5. Integrate testing endpoints (optional, for full coverage)

## Questions?

See comprehensive documentation:
- [Asciinema Visual Testing Handover](../../../../../docs/HANDOVER-2025-10-08-Asciinema-Visual-Testing.md)
- [Robust E2E Testing Handover](../../../../../docs/HANDOVER-2025-10-08-Robust-E2E-Testing.md)
- [Integration Checklist](../../../../INTEGRATION-CHECKLIST.md)

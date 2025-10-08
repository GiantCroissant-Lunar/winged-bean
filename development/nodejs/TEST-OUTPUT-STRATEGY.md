# Test Output Strategy

## Overview

Test results and reports should be generated in **versioned artifact directories**, not in the source tree. This ensures:
- Clean source directories
- Versioned test results for comparison
- Proper CI/CD artifact collection
- No git pollution from test runs

## Directory Structure

```
build/_artifacts/
  v{VERSION}/
    web/
      test-reports/         # Playwright HTML reports
      test-results/         # Test artifacts (traces, videos, screenshots)
      dist/                 # Built web assets
      recordings/           # Asciinema recordings
      logs/                 # Build logs
    dotnet/
      bin/                  # .NET binaries
      logs/                 # Runtime logs
    pty/
      dist/                 # PTY service
      logs/                 # PTY logs
```

## Usage

### Development Mode (Default)

For quick iteration during development, tests output to local directories:

```bash
cd development/nodejs
pnpm test:e2e
```

Results go to:
- `development/nodejs/playwright-report/`
- `development/nodejs/test-results/`

These are `.gitignore`d and meant for local use only.

### CI/Production Mode

For CI or formal test runs, use artifact output:

```bash
cd build
task test-e2e              # Runs with ARTIFACT_OUTPUT=1
```

Results go to:
- `build/_artifacts/v{VERSION}/web/test-reports/`
- `build/_artifacts/v{VERSION}/web/test-results/`

### Manual Artifact Output

You can also manually enable artifact output:

```bash
cd development/nodejs
ARTIFACT_OUTPUT=1 pnpm test:e2e
```

## Configuration

The `playwright.config.js` detects the mode automatically:

```javascript
const useArtifacts = process.env.CI || process.env.ARTIFACT_OUTPUT === '1';
```

### Environment Variables

- `CI=true` - Automatically uses artifact output (set by CI systems)
- `ARTIFACT_OUTPUT=1` - Manually enable artifact output
- No flags - Development mode (local directories)

## Benefits

1. **Clean Source Tree**: No test artifacts in version control
2. **Versioned Results**: Each build version has its own test results
3. **Easy Comparison**: Compare test results across versions
4. **CI-Friendly**: Artifact directories can be uploaded/archived
5. **Deterministic**: Test results are tied to specific code versions

## Task Commands

From `build/` directory:

```bash
task test-e2e        # Run E2E tests with artifact output (CI mode)
task test-e2e-dev    # Run E2E tests with local output (dev mode)
task ci              # Full CI pipeline (includes test-e2e)
```

## Git Ignore

Local test directories are ignored:

```gitignore
# Test results and reports (should be in versioned artifacts)
playwright-report/
test-results/
*.log
```

Artifact directories are also ignored as they're build outputs:

```gitignore
# Build artifacts
build/_artifacts/
```

## Migration Notes

If you have existing test results in the source tree:

1. They will be ignored by git after this change
2. Clean them manually: `rm -rf playwright-report test-results`
3. Use `task test-e2e` for future test runs

## Related

- RFC-0010: Multi-language Build Orchestration with Task
- Build Taskfile: `build/Taskfile.yml`
- Playwright Config: `development/nodejs/playwright.config.js`

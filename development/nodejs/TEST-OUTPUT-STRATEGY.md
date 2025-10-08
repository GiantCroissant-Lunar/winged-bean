# Test Output Strategy

## Overview

**All tests run against versioned artifacts** - there is no separate "dev mode" or "CI mode". This ensures:
- Consistency: Same test path for everyone
- Reality check: Test what actually gets deployed
- Simplicity: No mode switching logic
- Reliability: Catch artifact-specific issues early

## Philosophy

> "Test the artifacts, not the source tree"

Whether you're developing locally or running in CI, tests always run against the built artifacts in `build/_artifacts/v{VERSION}/`. This means:

1. Build first, then test
2. Test results are versioned alongside artifacts
3. No confusion about what was tested
4. True end-to-end validation

## Directory Structure

```
build/_artifacts/
  v{VERSION}/
    web/
      test-reports/         # Playwright HTML reports
      test-results/         # Test artifacts (traces, videos, screenshots)
      dist/                 # Built web assets (what we test)
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

### Standard Workflow

```bash
cd build

# Build artifacts first
task build-all

# Test the artifacts
task test-e2e
```

That's it! No flags, no modes, no confusion.

### Quick Iteration

During active development, you can rebuild and test in one command:

```bash
cd build
task build-all && task test-e2e
```

Or use the CI task that does both:

```bash
cd build
task ci    # Builds, then tests
```

### Direct Test Invocation

If artifacts are already built:

```bash
cd development/nodejs
pnpm test:e2e
```

The playwright config automatically detects the version and points to the correct artifact directory.

## How It Works

The `playwright.config.js` always points to versioned artifacts:

```javascript
const version = getVersion();  // e.g., "0.0.1-376"
const artifactBase = path.join(__dirname, '../../build/_artifacts', `v${version}`, 'web');

// Test reports and results go here
const outputDirs = {
  reportDir: path.join(artifactBase, 'test-reports'),
  resultsDir: path.join(artifactBase, 'test-results'),
};
```

No environment variables needed. No mode switching. Just version detection.

## Benefits

1. **Consistency**: Everyone tests the same way
2. **Reliability**: Tests validate actual deployable artifacts
3. **Simplicity**: One path, no conditionals
4. **Traceability**: Test results are versioned with artifacts
5. **Debugging**: Easy to reproduce issues with specific versions
6. **CI-Ready**: No special CI configuration needed

## Task Commands

From `build/` directory:

```bash
task build-all     # Build all artifacts
task test-e2e      # Test the artifacts (requires build-all first)
task ci            # Full pipeline: build + test
```

## What About Source Changes?

If you change source code, you must rebuild before testing:

```bash
# Change some code
cd build
task build-all     # Rebuild artifacts
task test-e2e      # Test new artifacts
```

This might seem slower, but it's the **correct** way because:
- You catch build issues immediately
- You test what actually runs in production
- You avoid "works on my machine" problems
- You ensure the build process is fast enough

## Git Ignore

Test results are ignored since they're in the build artifacts directory:

```gitignore
# Build artifacts (includes test results)
build/_artifacts/
```

The source tree stays clean - no test reports or results checked in.

## Migration Notes

If you have local test directories from before:

```bash
cd development/nodejs
rm -rf playwright-report test-results
```

Then always use `task test-e2e` from the build directory.

## FAQ

**Q: Can I test without building?**  
A: No. Tests run against artifacts. No artifacts = no tests. Use `task build-all` first.

**Q: This seems slower than testing source directly?**  
A: Yes, but it's correct. Make your build fast instead. That benefits everyone.

**Q: What if I'm iterating quickly on a test?**  
A: Still rebuild. A fast build system (< 10s) makes this painless. Fix the build, not the workflow.

**Q: Why not support both modes?**  
A: Complexity kills. One path = less bugs, better reliability, clearer intent.

## Related

- RFC-0010: Multi-language Build Orchestration with Task
- Build Taskfile: `build/Taskfile.yml`
- Playwright Config: `development/nodejs/playwright.config.js`

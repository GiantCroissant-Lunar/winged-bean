# Testing GitHub Workflows Locally with Act

This document explains how to test GitHub workflows locally using [act](https://github.com/nektos/act) before pushing to GitHub.

## Prerequisites

Act is already installed and configured in this repository.

**Verify installation**:
```bash
act --version
```

**Configuration file**: `.actrc` (already configured)

## Quick Start

### Test Recording Workflow

Test the recording workflow that builds and tests both console and desktop apps:

```bash
# Run the recording tests workflow with default settings (10 second recording)
act workflow_dispatch -W .github/workflows/recording-tests.yml

# Run with custom recording duration (5 seconds for faster testing)
act workflow_dispatch -W .github/workflows/recording-tests.yml \
  --input record_duration=5

# Run only console recording test
act workflow_dispatch -W .github/workflows/recording-tests.yml \
  -j build-and-test-console

# Run only desktop recording test  
act workflow_dispatch -W .github/workflows/recording-tests.yml \
  -j build-and-test-desktop
```

### Test Other Workflows

```bash
# List all available workflows
act -l

# Test a specific workflow on push event
act push -W .github/workflows/recording-tests.yml

# Dry run (show what would be executed without running)
act workflow_dispatch -W .github/workflows/recording-tests.yml --dryrun
```

## Understanding the Recording Tests Workflow

The `recording-tests.yml` workflow does the following:

1. **Build Console App** (`build-and-test-console` job)
   - Builds the console app with .NET 8
   - Tests recording with URF format
   - Verifies `.cast` and `.urf.jsonl` files are created
   - Uploads recordings as artifacts

2. **Build Desktop App** (`build-and-test-desktop` job)
   - Builds the desktop app (SadConsole)
   - Tests recording in headless mode
   - Verifies recording files
   - Uploads recordings as artifacts

3. **Summary** (`summary` job)
   - Downloads all artifacts
   - Generates test summary report

## Act Configuration

**File**: `.actrc`

Key settings:
- Uses `catthehacker/ubuntu:act-latest` image for compatibility
- Linux/amd64 architecture for Apple Silicon Macs
- Verbose logging enabled
- Artifact server enabled at `/tmp/act-artifacts`

## Troubleshooting

### Common Issues

**1. Docker not running**
```bash
docker info
# If error, start Docker Desktop
```

**2. Slow first run**
Act needs to download the Docker image on first run (~1-2 GB). This is cached for future runs.

**3. Submodules not available**
If you see errors about missing plate-projects:
```bash
# Initialize submodules first
git submodule update --init --recursive
```

**4. Build failures**
Desktop build may show warnings about missing dependencies in CI. This is expected for headless environments.

### Debug Mode

Run with extra verbosity:
```bash
act workflow_dispatch -W .github/workflows/recording-tests.yml --verbose
```

View container logs:
```bash
act workflow_dispatch -W .github/workflows/recording-tests.yml --container-daemon-socket -
```

## Artifacts

When running with act, artifacts are stored in:
```
/tmp/act-artifacts/
```

View artifacts after a run:
```bash
ls -la /tmp/act-artifacts/
```

## Testing Before Push

**Recommended workflow**:

1. Make changes to console/desktop code
2. Test locally with act:
   ```bash
   act workflow_dispatch -W .github/workflows/recording-tests.yml --input record_duration=5
   ```
3. Review logs and artifacts
4. If tests pass, commit and push
5. GitHub Actions will run the same workflow automatically

## CI/CD Integration

The recording tests workflow runs:
- ✅ On manual trigger (`workflow_dispatch`)
- ✅ On push to `main` branch (if recording-related files changed)
- ✅ On pull requests (if recording-related files changed)

## Workflow Inputs

When using `workflow_dispatch` (manual trigger), you can provide:

- `record_duration`: Recording duration in seconds (default: 10)

**Example with custom input**:
```bash
act workflow_dispatch -W .github/workflows/recording-tests.yml \
  --input record_duration=3
```

## Performance Tips

1. **Use job filters** to run specific jobs:
   ```bash
   # Only run console tests (faster)
   act workflow_dispatch -W .github/workflows/recording-tests.yml -j build-and-test-console
   ```

2. **Reuse containers** between runs (if Docker image is cached)

3. **Shorter recording duration** for quick smoke tests:
   ```bash
   --input record_duration=3
   ```

## Environment Files

**`.env.act`** (if needed for secrets):
Create this file to add environment variables:
```bash
# Example
GITHUB_TOKEN=your_token_here
```

## Documentation

- Act documentation: https://nektosact.com/
- WingedBean workflows: `.github/workflows/`
- Workflow events: `.github/events/`

## Exit Codes

- `0`: All tests passed
- `1`: Tests failed
- `2`: Workflow error

## Next Steps

After testing locally with act:

1. ✅ Verify console recording works
2. ✅ Verify desktop recording works (may have headless limitations)
3. ✅ Check artifact uploads
4. ✅ Push to GitHub
5. ✅ Verify workflow runs on GitHub Actions

## Related Files

- `.actrc` - Act configuration
- `.github/workflows/recording-tests.yml` - Recording tests workflow
- `.github/events/workflow_dispatch.json` - Workflow dispatch event template
- `build/Taskfile.yml` - Build orchestration

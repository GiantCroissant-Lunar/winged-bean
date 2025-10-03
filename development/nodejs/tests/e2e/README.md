# E2E Testing with Versioned Artifacts

## Overview

This test suite captures the complete state of the Winged Bean application at each version, following [RFC-0010](../../../../docs/rfcs/0010-multi-language-build-orchestration-with-task.md)'s versioned artifact strategy.

## Why Versioned Artifacts?

As noted in your observation:
> "As each version evolves, it may show different screen, behavior that the normal spec test is hard to verify"

Traditional spec tests only check if something passes/fails. **Versioned artifacts capture WHAT ACTUALLY HAPPENED** at each version:
- Screenshots showing visual state
- Terminal buffer content
- Logs and verification data
- Asciinema recordings (when available)

This approach is invaluable for:
1. **Debugging** - "What did v0.0.1-alpha show vs v0.0.2-beta?"
2. **Regression tracking** - Visual comparison across versions
3. **Documentation** - Screenshots for each version's release notes
4. **Troubleshooting** - When a user reports "it doesn't work", compare their version's artifacts

## Directory Structure

```
build/_artifacts/v{VERSION}/
├── dotnet/
│   ├── bin/                    # Compiled .NET binaries
│   ├── recordings/             # Asciinema cast files from .NET console
│   └── logs/                   # .NET application logs
├── web/
│   ├── dist/                   # Built web assets
│   ├── screenshots/            # ✨ Playwright visual captures
│   │   ├── dungeon-initial-{timestamp}.png
│   │   ├── dungeon-running-{timestamp}.png
│   │   ├── terminal-only-{timestamp}.png
│   │   └── session-{N}s-{timestamp}.png
│   ├── terminal-captures/      # ✨ Terminal buffer text dumps
│   │   ├── terminal-buffer-{timestamp}.txt
│   │   └── buffer-{N}s-{timestamp}.txt
│   ├── recordings/             # ✨ Browser recordings (future: video/cast)
│   └── logs/                   # ✨ Test verification data
│       └── verification-{timestamp}.json
├── pty/
│   ├── dist/                   # PTY service files
│   └── logs/                   # PTY service logs
└── _logs/                      # Build-time logs
```

## Test Suites

### 1. `capture-versioned-state.spec.js` ⭐ RECOMMENDED

**Purpose**: Capture complete application state at the current version

**What it captures**:
- Initial page load screenshot
- Running state after 10 seconds
- Terminal-only focused screenshot
- Terminal buffer content as text
- Verification JSON with checks
- (Optional) 30-second time-lapse captures

**Usage**:
```bash
cd development/nodejs
pnpm exec playwright test capture-versioned-state.spec.js

# Artifacts saved to:
# build/_artifacts/v{VERSION}/web/screenshots/
# build/_artifacts/v{VERSION}/web/terminal-captures/
# build/_artifacts/v{VERSION}/web/logs/
```

**Example verification JSON**:
```json
{
  "version": "0.0.1-architecture-realignment.1",
  "timestamp": "2025-10-03T03:55:24.429Z",
  "checks": {
    "hasTitle": true,
    "hasGameplay": true,
    "hasStats": true,
    "hasEntities": true,
    "hasGameState": true
  },
  "terminalPreview": "┌┤Console Dungeon - ECS Dungeon Crawler...",
  "url": "http://localhost:4321/demo/",
  "browser": "chromium"
}
```

### 2. `check-dungeon-display.spec.js`

**Purpose**: Quick check of current display (always passes, just captures)

**Usage**: Rapid iteration during development
```bash
pnpm exec playwright test check-dungeon-display.spec.js
```

### 3. `verify-dungeon-gameplay.spec.js`

**Purpose**: Actual spec test with assertions

**Usage**: CI/CD validation
```bash
pnpm exec playwright test verify-dungeon-gameplay.spec.js
```

## Integration with Task Build System

### Task Commands

```bash
# From build/ directory
task dev:status        # Check PM2 services
task dev:logs          # View service logs

# Capture current version state
cd development/nodejs
pnpm exec playwright test capture-versioned-state.spec.js

# Run all E2E tests
pnpm run test:e2e
```

### Recommended Workflow

1. **Development iteration**:
   ```bash
   # Make code changes
   task console:build
   pm2 restart console-dungeon
   
   # Quick check
   pnpm exec playwright test check-dungeon-display.spec.js
   ```

2. **Before committing**:
   ```bash
   # Capture version state
   pnpm exec playwright test capture-versioned-state.spec.js
   
   # Run spec tests
   pnpm exec playwright test verify-dungeon-gameplay.spec.js
   ```

3. **Release/Milestone**:
   ```bash
   # Full build
   cd build
   task build-all
   
   # Capture release state
   cd ../development/nodejs
   pnpm exec playwright test capture-versioned-state.spec.js
   
   # Artifacts ready for archiving at:
   # build/_artifacts/v{VERSION}/
   ```

## Comparing Versions

### Manual Comparison
```bash
# List all captured versions
ls build/_artifacts/

# Compare terminal output between versions
diff \
  build/_artifacts/v0.0.1-alpha/web/terminal-captures/terminal-buffer-*.txt \
  build/_artifacts/v0.0.2-beta/web/terminal-captures/terminal-buffer-*.txt

# View screenshots side-by-side (macOS)
open build/_artifacts/v0.0.1-alpha/web/screenshots/dungeon-running-*.png
open build/_artifacts/v0.0.2-beta/web/screenshots/dungeon-running-*.png
```

### Automated Comparison (Future)
```javascript
// Future enhancement: Visual regression testing
const { compareScreenshots } = require('playwright-test');
const result = compareScreenshots(
  'v0.0.1/web/screenshots/dungeon-running.png',
  'v0.0.2/web/screenshots/dungeon-running.png'
);
```

## Troubleshooting

### "Screenshots are empty/black"
- Ensure services are running: `task dev:status`
- Wait longer for page load: increase `waitForTimeout`
- Check browser console for errors

### "Terminal buffer shows 'Terminal not initialized'"
- xterm.js may not be loaded
- Check network tab for failed resources
- Verify WebSocket connection

### "Version shows as '0.0.1-dev'"
- GitVersion not configured or no tags
- Check: `cd build && ./get-version.sh`
- Fallback to dev version if git version fails

## Future Enhancements

### Asciinema Integration (RFC-0009)
The AsciinemaRecorder plugin (lazy-loaded) can record PTY sessions:
```javascript
// Future: Trigger asciinema recording from test
await page.evaluate(() => {
  window.startRecording?.();
});
await page.waitForTimeout(30000);
await page.evaluate(() => {
  window.stopRecording?.();
});
// Cast file saved to: build/_artifacts/v{VERSION}/dotnet/recordings/
```

### Video Recording
Playwright already records videos on failure. Future enhancement:
```javascript
// Save all videos to versioned artifacts
test.afterEach(async ({}, testInfo) => {
  const video = await testInfo.video();
  await video.saveAs(`build/_artifacts/v${VERSION}/web/recordings/${testInfo.title}.webm`);
});
```

## See Also

- [RFC-0010: Multi-Language Build Orchestration](../../../../docs/rfcs/0010-multi-language-build-orchestration-with-task.md)
- [RFC-0009: Dynamic Asciinema Recording](../../../../docs/rfcs/0009-dynamic-asciinema-recording-in-pty.md)
- [Playwright Documentation](https://playwright.dev/)
- [Task Documentation](https://taskfile.dev/)

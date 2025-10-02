---
title: RFC-0008: Playwright and Asciinema Testing Strategy for Terminal.Gui PTY Integration
---

# RFC-0008: Playwright and Asciinema Testing Strategy for Terminal.Gui PTY Integration

**Status:** ✅ Implemented (CI/CD pending)  
**Date:** 2025-10-01  
**Completed:** 2025-10-01  
**Author:** Development Team  
**Priority:** HIGH (P1)  
**Actual Effort:** 1 day  

---

## Problem Statement

Currently, we can verify that:
- PM2 services are running
- WebSocket connections work
- PTY spawns .NET processes
- Binary data streams correctly

**However, we cannot verify:**
- Terminal.Gui UI is actually **visible** in the browser
- xterm.js is **rendering** the ANSI sequences correctly
- User interactions work as expected
- Development progress over time

## Proposed Solution

Implement a **two-pronged testing and documentation strategy**:

1. **Playwright** for visual E2E testing and verification
2. **Asciinema** for recording and documenting Terminal.Gui sessions

---

## 1. Playwright Visual Testing

### Goals

- ✅ Verify Terminal.Gui UI renders correctly in xterm.js
- ✅ Test keyboard/mouse interactions
- ✅ Capture screenshots for regression testing
- ✅ Run in CI/CD for automated verification
- ✅ Detect visual regressions

### Architecture

```
┌─────────────────────────────────────────────────────────────┐
│                     Playwright Test                          │
│                                                              │
│  1. Launch Browser (Chromium/Firefox/WebKit)                │
│  2. Navigate to http://localhost:4321                       │
│  3. Wait for xterm.js to initialize                         │
│  4. Wait for Terminal.Gui to spawn in PTY                   │
│  5. Verify UI elements visible                              │
│  6. Take screenshots                                         │
│  7. Test keyboard input                                      │
│  8. Compare with baseline screenshots                       │
└─────────────────────────────────────────────────────────────┘
```

### Implementation Plan

#### Phase 1: Setup (High Priority)

**Install Dependencies:**
```bash
cd development/nodejs
pnpm add -D @playwright/test
npx playwright install chromium
```

**Create Playwright Config:**
```javascript
// playwright.config.js
module.exports = {
  testDir: './tests/e2e',
  timeout: 30000,
  use: {
    baseURL: 'http://localhost:4321',
    screenshot: 'only-on-failure',
    video: 'retain-on-failure',
  },
  projects: [
    { name: 'chromium', use: { browserName: 'chromium' } },
  ],
};
```

#### Phase 2: Visual Verification Tests (High Priority)

**Test 1: Terminal.Gui Renders in Browser**
```javascript
test('Terminal.Gui v2 UI is visible in xterm.js', async ({ page }) => {
  await page.goto('/');
  
  // Wait for PTY terminal
  const terminal = page.locator('#pty-terminal');
  await expect(terminal).toBeVisible();
  
  // Wait for Terminal.Gui to spawn (5s)
  await page.waitForTimeout(5000);
  
  // Take screenshot
  await page.screenshot({ path: 'screenshots/terminal-gui-ui.png' });
  
  // Verify Terminal.Gui text is visible
  const content = await terminal.textContent();
  expect(content).toContain('Terminal.Gui v2');
  expect(content).toContain('PTY Demo');
  
  // Verify xterm elements exist
  const xtermRows = terminal.locator('.xterm-rows');
  await expect(xtermRows).toBeVisible();
});
```

**Test 2: Keyboard Interaction**
```javascript
test('Keyboard input works in Terminal.Gui', async ({ page }) => {
  await page.goto('/');
  const terminal = page.locator('#pty-terminal');
  
  // Wait for Terminal.Gui
  await page.waitForTimeout(5000);
  
  // Click on terminal to focus
  await terminal.click();
  
  // Press Tab to navigate
  await page.keyboard.press('Tab');
  await page.waitForTimeout(500);
  
  // Type in text field
  await page.keyboard.type('Hello from Playwright!');
  
  // Take screenshot showing input
  await page.screenshot({ path: 'screenshots/keyboard-input.png' });
});
```

**Test 3: Visual Regression**
```javascript
test('Terminal.Gui UI matches baseline', async ({ page }) => {
  await page.goto('/');
  const terminal = page.locator('#pty-terminal');
  await page.waitForTimeout(5000);
  
  // Compare with baseline screenshot
  await expect(terminal).toHaveScreenshot('terminal-gui-baseline.png', {
    maxDiffPixels: 100, // Allow minor differences
  });
});
```

#### Phase 3: CI/CD Integration (Medium Priority)

**GitHub Actions Workflow:**
```yaml
name: E2E Visual Tests

on: [push, pull_request]

jobs:
  playwright-tests:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v3
      
      - name: Setup Node.js
        uses: actions/setup-node@v3
        with:
          node-version: '18'
      
      - name: Setup .NET
        uses: actions/setup-dotnet@v3
        with:
          dotnet-version: '8.0'
      
      - name: Install dependencies
        run: |
          cd development/nodejs
          pnpm install
          npx playwright install chromium
      
      - name: Build .NET app
        run: |
          cd development/dotnet/console
          dotnet build
      
      - name: Start services
        run: |
          cd development/nodejs
          pm2 start ecosystem.config.js
          sleep 10  # Wait for services to start
      
      - name: Run Playwright tests
        run: |
          cd development/nodejs
          npx playwright test
      
      - name: Upload screenshots
        if: failure()
        uses: actions/upload-artifact@v3
        with:
          name: playwright-screenshots
          path: development/nodejs/tests/e2e/screenshots/
```

---

## 2. Asciinema Recording Strategy

### Goals

- 🎬 Record Terminal.Gui sessions for documentation
- 📊 Track development progress over time
- 📚 Embed recordings in README/docs
- 🔄 Automate recording in CI/CD
- 🎥 Create demo videos for features

### Architecture

```
┌─────────────────────────────────────────────────────────────┐
│                  Asciinema Recording                         │
│                                                              │
│  PTY Process → Asciinema Recorder → .cast file              │
│       ↓                                                      │
│  WebSocket → Browser (xterm.js)                             │
│                                                              │
│  .cast file → asciinema-player → Embedded in docs           │
└─────────────────────────────────────────────────────────────┘
```

### Implementation Plan

#### Phase 1: PTY Service Integration (High Priority)

**Install Asciinema Recorder Package:**
```bash
cd development/nodejs/pty-service
npm install asciinema-recorder
```

**Modify PTY Service to Record:**
```javascript
// server.js
const AsciinemaRecorder = require('asciinema-recorder');

wss.on('connection', (ws) => {
  // Create recorder for this session
  const recorder = new AsciinemaRecorder({
    outputFile: `recordings/${Date.now()}-terminal-gui-session.cast`,
    width: 80,
    height: 24,
    title: 'Terminal.Gui v2 PTY Session',
  });
  
  recorder.start();
  
  ptyProcess.onData((data) => {
    ws.send(data);           // Send to browser
    recorder.write(data);    // Record to .cast file
  });
  
  ws.on('close', () => {
    recorder.stop();
    console.log('Recording saved:', recorder.outputFile);
  });
});
```

#### Phase 2: Manual Recording Script (High Priority)

**Create Recording Script:**
```bash
#!/bin/bash
# scripts/record-terminal-gui-session.sh

RECORDING_DIR="docs/recordings"
TIMESTAMP=$(date +%Y%m%d-%H%M%S)
OUTPUT_FILE="$RECORDING_DIR/terminal-gui-$TIMESTAMP.cast"

mkdir -p "$RECORDING_DIR"

echo "Starting Terminal.Gui recording..."
echo "Recording will be saved to: $OUTPUT_FILE"

# Start recording
asciinema rec "$OUTPUT_FILE" \
  --title "Terminal.Gui v2 Development Progress" \
  --command "cd development/dotnet/console && dotnet run --project src/host/TerminalGui.PtyHost/TerminalGui.PtyHost.csproj"

echo "Recording saved!"
echo "To play: asciinema play $OUTPUT_FILE"
echo "To upload: asciinema upload $OUTPUT_FILE"
```

**Make it executable:**
```bash
chmod +x scripts/record-terminal-gui-session.sh
```

#### Phase 3: GitHub Actions Recording (Medium Priority)

**Workflow to Record on Feature Completion:**
```yaml
name: Record Terminal.Gui Demo

on:
  workflow_dispatch:  # Manual trigger
  push:
    branches: [main]
    paths:
      - 'development/dotnet/console/src/**'
      - 'development/nodejs/pty-service/**'

jobs:
  record-demo:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v3
      
      - name: Install asciinema
        run: |
          sudo apt-get update
          sudo apt-get install -y asciinema
      
      - name: Setup .NET
        uses: actions/setup-dotnet@v3
        with:
          dotnet-version: '8.0'
      
      - name: Build Terminal.Gui app
        run: |
          cd development/dotnet/console
          dotnet build
      
      - name: Record Terminal.Gui session
        run: |
          mkdir -p docs/recordings
          asciinema rec docs/recordings/terminal-gui-$(date +%Y%m%d).cast \
            --title "Terminal.Gui v2 - $(date +%Y-%m-%d)" \
            --command "cd development/dotnet/console && timeout 30s dotnet run --project src/host/TerminalGui.PtyHost/TerminalGui.PtyHost.csproj"
      
      - name: Commit recording
        run: |
          git config user.name "GitHub Actions"
          git config user.email "actions@github.com"
          git add docs/recordings/*.cast
          git commit -m "chore: add Terminal.Gui recording $(date +%Y-%m-%d)" || true
          git push || true
      
      - name: Upload recording artifact
        uses: actions/upload-artifact@v3
        with:
          name: terminal-gui-recording
          path: docs/recordings/*.cast
```

#### Phase 4: Documentation Integration (Low Priority)

**Embed in README:**
```markdown
# Terminal.Gui v2 PTY Integration

## Live Demo

<asciinema-player src="docs/recordings/terminal-gui-latest.cast"></asciinema-player>

## Development Progress

### Latest Features (2025-10-01)
[View Recording](docs/recordings/terminal-gui-20251001.cast)

### Previous Versions
- [2025-09-30: Initial PTY Integration](docs/recordings/terminal-gui-20250930.cast)
- [2025-09-25: Basic UI](docs/recordings/terminal-gui-20250925.cast)
```

**Create Gallery Page:**
```html
<!-- docs/recordings/index.html -->
<!DOCTYPE html>
<html>
<head>
  <title>Terminal.Gui Development Progress</title>
  <link rel="stylesheet" href="asciinema-player.css">
</head>
<body>
  <h1>Terminal.Gui v2 Development Timeline</h1>
  
  <section>
    <h2>Latest Version (2025-10-01)</h2>
    <asciinema-player src="terminal-gui-20251001.cast"></asciinema-player>
  </section>
  
  <section>
    <h2>Previous Versions</h2>
    <!-- List of previous recordings -->
  </section>
  
  <script src="asciinema-player.js"></script>
</body>
</html>

---

## Implementation Status

### ✅ Phase 1: Playwright Setup (COMPLETE)
- ✅ @playwright/test installed
- ✅ playwright.config.js created
- ✅ Chromium browser installed
- ✅ Test directory structure created

### ✅ Phase 2: Visual Verification Tests (COMPLETE)
- ✅ Test 1: Terminal.Gui renders in browser (PASSING)
- ✅ Test 2: UI elements visible (PASSING)
- ✅ Test 3: WebSocket receives data (PASSING)
- ✅ Screenshots generated automatically
- ✅ All 3 E2E tests passing

### ✅ Phase 3: Asciinema Recording (COMPLETE)
- ✅ Recording script created (scripts/record-terminal-gui-session.sh)
- ✅ Manual recording capability working
- ✅ .cast file generation working
- ✅ Dynamic recording via F9/F10 (RFC-0009)

### ⚠️ Phase 4: CI/CD Integration (PENDING)
- ❌ GitHub Actions workflow not yet implemented
- ⚠️ Follow-up task identified

### Verification
- **Tests:** 3/3 E2E tests passing
- **Files Created:** `playwright.config.js`, `terminal-gui-visual-verification.spec.js`
- **Screenshots:** Generated in `tests/e2e/screenshots/`
- **Recordings:** `.cast` files in `docs/recordings/`

### Follow-Up Tasks
1. **Add GitHub Actions CI/CD workflow** for automated Playwright tests
2. **Add asciinema-player** to docs site for embedded playback
3. **Create example recordings** for documentation

---

## Asciinema Benefits
- 🎬 **Progress documentation** - Visual changelog of features
- 📊 **Demo creation** - Easy to share with stakeholders
- 🔄 **Reproducible** - Exact terminal output captured
- 📦 **Lightweight** - JSON format, version-control friendly
- 🌐 **Web embeddable** - Show in docs, README, website

---

## Implementation Timeline

### Week 1: Foundation (High Priority)
- [ ] Install Playwright and configure
- [ ] Create basic visual verification test
- [ ] Install asciinema recorder in PTY service
- [ ] Create manual recording script

### Week 2: Integration (Medium Priority)
- [ ] Add screenshot comparison tests
- [ ] Implement PTY service recording
- [ ] Create GitHub Actions workflow for Playwright
- [ ] Test recording in CI/CD

### Week 3: Documentation (Low Priority)
- [ ] Embed recordings in README
- [ ] Create recording gallery page
- [ ] Document recording workflow
- [ ] Create demo videos for features

---

## Alternatives Considered

### Alternative 1: Manual Testing Only
**Rejected:** Not scalable, error-prone, no regression detection

### Alternative 2: Unit Tests Only
**Rejected:** Cannot verify visual rendering or browser behavior

### Alternative 3: Video Recording (OBS, ffmpeg)
**Rejected:** Large file sizes, not version-control friendly, harder to automate

### Alternative 4: VNC + Screenshot Comparison
**Rejected:** More complex setup, slower than Playwright

---

## Success Metrics

- ✅ Playwright tests run in < 30 seconds
- ✅ 100% of Terminal.Gui UI elements verified visually
- ✅ Asciinema recordings < 1MB each
- ✅ New features recorded within 24 hours
- ✅ Zero visual regressions in production

---

## Open Questions

1. **Recording Storage:** Where to store .cast files long-term? (Git LFS? S3?)
2. **Recording Frequency:** Record on every commit or only on releases?
3. **Screenshot Baselines:** How to handle expected UI changes?
4. **Browser Coverage:** Test all browsers or just Chromium?

---

## References

- [Playwright Documentation](https://playwright.dev/)
- [Asciinema Documentation](https://asciinema.org/)
- [ADR-0001: Use Astro with Asciinema Player](../adr/0001-use-astro-with-asciinema-player.md)
- [Terminal.Gui PTY Integration Handover](../handover/TERMINAL_GUI_PTY_INTEGRATION.md)

---

**Status:** Awaiting approval and prioritization  
**Next Steps:** Review with team, prioritize implementation phases

# Next Steps Analysis: Visual Regression & Asciinema Integration

**Date**: 2025-10-03  
**Status**: Planning

---

## Current State Assessment

### 1. Asciinema Integration Status ✅ ALREADY IMPLEMENTED

**RFC-0009 Status**: ✅ Implemented (2025-10-01)

**What's Already Working**:
```javascript
// PTY Service (server.js) - Line 4, 23-26, 60-88
const RecordingManager = require("./recording-manager");
const recordingsDir = path.resolve(__dirname, "../../../docs/recordings");
const recordingManager = new RecordingManager(recordingsDir);

// Recording is triggered by OSC sequences:
// F9 in Terminal.Gui → \x1b]1337;StartRecording\x07 → recordingManager.startRecording()
// F10 in Terminal.Gui → \x1b]1337;StopRecording\x07 → recordingManager.stopRecording()
```

**Existing Recordings**:
```bash
docs/recordings/
├── terminal-gui-test-20251001-211942.cast      (359B)
├── terminal-gui-pty-output.cast                (23K)
└── session-1-2025-10-01T14-56-27.cast          (17K)
```

**What's Working**:
- ✅ Recording Manager implemented
- ✅ OSC sequence detection
- ✅ Asciinema v2 format
- ✅ Auto-save to `docs/recordings/`
- ✅ Integration with PTY service

**What's NOT Working**:
- ❌ Terminal.Gui app not sending F9/F10 OSC sequences
- ❌ No keyboard shortcuts wired up in ConsoleDungeonApp
- ❌ No automatic recording during tests
- ❌ Recordings not copied to versioned artifacts (`build/_artifacts/`)

---

## Visual Regression Options

### Overview

**Visual Regression Testing** = Comparing screenshots between versions to detect unintended visual changes.

**Three Main Approaches**:

### Option 1: Playwright Built-in Visual Comparison ⭐ RECOMMENDED

**How it works**:
```javascript
// First run - captures baseline
await expect(page).toHaveScreenshot('dungeon-game.png');

// Subsequent runs - compares against baseline
await expect(page).toHaveScreenshot('dungeon-game.png');
// ✅ Pass if matches baseline
// ❌ Fail if differs (shows diff image)
```

**Pros**:
- ✅ Already installed (Playwright 1.55.1)
- ✅ Zero additional dependencies
- ✅ Built-in diff visualization
- ✅ Configurable tolerance (pixel threshold)
- ✅ Cross-platform (uses same rendering engine)
- ✅ Fast execution

**Cons**:
- ⚠️ Terminal text rendering may have slight variations (fonts, antialiasing)
- ⚠️ Timestamps cause false positives (need masking)
- ⚠️ Not ideal for frequently changing UI (like game stats)

**Best for**:
- Static UI elements (window chrome, buttons, labels)
- Layout verification (element positions)
- Detecting accidental UI breaks

**Implementation**:
```javascript
test('visual regression check', async ({ page }) => {
  await page.goto('http://localhost:4321/demo/');
  await page.waitForTimeout(10000);
  
  // Mask dynamic elements (timestamps, stats)
  await expect(page).toHaveScreenshot('dungeon-baseline.png', {
    mask: [page.locator('text=11:49:25')], // Mask timestamps
    maxDiffPixels: 100, // Allow small differences
  });
});
```

**Storage**:
```
tests/e2e/__screenshots__/
├── chromium/
│   ├── dungeon-baseline.png           # Baseline
│   ├── dungeon-baseline-actual.png    # Current
│   └── dungeon-baseline-diff.png      # Diff (on failure)
```

---

### Option 2: Pixelmatch (Pixel-by-Pixel Comparison)

**How it works**:
```javascript
const pixelmatch = require('pixelmatch');
const PNG = require('pngjs').PNG;

const img1 = PNG.sync.read(fs.readFileSync('v0.0.1/screenshot.png'));
const img2 = PNG.sync.read(fs.readFileSync('v0.0.2/screenshot.png'));
const diff = new PNG({ width: img1.width, height: img1.height });

const numDiffPixels = pixelmatch(
  img1.data, img2.data, diff.data, 
  img1.width, img1.height, 
  { threshold: 0.1 } // Sensitivity
);

fs.writeFileSync('diff.png', PNG.sync.write(diff));
console.log(`Difference: ${numDiffPixels} pixels changed`);
```

**Pros**:
- ✅ Very fast (C++ implementation)
- ✅ Granular control (pixel threshold)
- ✅ Works with any PNG images
- ✅ Lightweight (no browser needed)

**Cons**:
- ⚠️ Requires installation: `pnpm add -D pixelmatch pngjs`
- ⚠️ Manual baseline management
- ⚠️ No built-in masking (need to implement)

**Best for**:
- Comparing archived screenshots between versions
- CI/CD pipelines (fast, no browser)
- Custom diff visualization

---

### Option 3: Text-Based Terminal Comparison (For Terminal Output) ⭐ ALSO RECOMMENDED

**How it works**:
```javascript
const diff = require('diff');

const buffer1 = fs.readFileSync('v0.0.1/terminal-buffer.txt', 'utf-8');
const buffer2 = fs.readFileSync('v0.0.2/terminal-buffer.txt', 'utf-8');

const changes = diff.diffLines(buffer1, buffer2);

changes.forEach(part => {
  if (part.added) console.log(`+ ${part.value}`);
  else if (part.removed) console.log(`- ${part.value}`);
});
```

**Pros**:
- ✅ Perfect for terminal text comparison
- ✅ Shows exact text differences
- ✅ Fast and simple
- ✅ Already captured (terminal-buffer-*.txt)
- ✅ No false positives from font rendering

**Cons**:
- ❌ Doesn't show visual layout issues
- ❌ Can't detect color/formatting changes

**Best for**:
- Terminal content verification
- Detecting text changes
- Debugging game state output

---

## Recommended Implementation Strategy

### Phase 1: Fix Game Initialization (FIRST PRIORITY)

**Current Issue**: Stats show "Loading..." forever

**Action**:
```bash
# Debug why game service isn't updating labels
# Add logging to ConsoleDungeonApp timer callbacks
# Verify _gameService.Initialize() completes
# Check Observable subscriptions are working
```

**Expected Result**: Stats should show:
```
HP: 100/100 | MP: 50/50 | Lvl: 1 | XP: 0
Game State: Running | Mode: Play | 11:58:42
Entities in world: 6
```

---

### Phase 2: Integrate Asciinema with Tests

**Goal**: Automatically record .cast files during test runs

**Option A: Programmatic Recording** (Via WebSocket)
```javascript
test('capture with asciinema', async ({ page }) => {
  await page.goto('http://localhost:4321/demo/');
  
  // Send OSC sequence to start recording
  const ptyTerminal = page.locator('#pty-terminal');
  await page.evaluate(() => {
    const ws = window.__ptyWebSocket;
    if (ws) {
      ws.send('\x1b]1337;StartRecording\x07'); // F9 equivalent
    }
  });
  
  // Let recording run...
  await page.waitForTimeout(30000);
  
  // Stop recording
  await page.evaluate(() => {
    const ws = window.__ptyWebSocket;
    if (ws) {
      ws.send('\x1b]1337;StopRecording\x07'); // F10 equivalent
    }
  });
  
  // Recording saved to: docs/recordings/session-*.cast
});
```

**Option B: Copy Existing Recordings** (Simpler)
```javascript
test.afterEach(async () => {
  // Copy any new recordings to versioned artifacts
  const recordings = fs.readdirSync('docs/recordings/');
  const recent = recordings.filter(f => 
    fs.statSync(`docs/recordings/${f}`).mtime > testStartTime
  );
  
  recent.forEach(file => {
    fs.copyFileSync(
      `docs/recordings/${file}`,
      `build/_artifacts/v${VERSION}/dotnet/recordings/${file}`
    );
  });
});
```

**Benefit**: Version-specific recordings for debugging

---

### Phase 3: Playwright Visual Regression (Baseline Capture)

**Step 1: Create Baseline** (After fixing game initialization)
```bash
# Capture baseline at v0.0.1 (or current working version)
cd development/nodejs
pnpm exec playwright test capture-baseline.spec.js --update-snapshots
```

**Step 2: Automated Comparison** (On each version bump)
```javascript
test('visual regression check', async ({ page }) => {
  await page.goto('http://localhost:4321/demo/');
  await page.waitForTimeout(10000);
  
  // Full page
  await expect(page).toHaveScreenshot('full-page.png', {
    maxDiffPixelRatio: 0.01, // Allow 1% difference
  });
  
  // Terminal only
  const terminal = page.locator('#pty-terminal');
  await expect(terminal).toHaveScreenshot('terminal-only.png', {
    mask: [page.locator('text=/\\d{2}:\\d{2}:\\d{2}/')], // Mask time
  });
});
```

**What it catches**:
- Layout breaks (window resized)
- CSS changes (colors, fonts)
- Missing elements (buttons disappeared)
- Content changes (text modified)

**What it doesn't catch**:
- Subtle game logic bugs
- Performance issues
- Internal state problems

---

### Phase 4: Text-Based Terminal Comparison

**For terminal content changes**:
```javascript
const diff = require('diff');

test('compare terminal content across versions', async () => {
  const v1 = fs.readFileSync(
    'build/_artifacts/v0.0.1/web/terminal-captures/terminal-buffer-*.txt', 
    'utf-8'
  );
  const v2 = fs.readFileSync(
    'build/_artifacts/v0.0.2/web/terminal-captures/terminal-buffer-*.txt', 
    'utf-8'
  );
  
  const changes = diff.diffLines(v1, v2);
  const significantChanges = changes.filter(c => c.added || c.removed);
  
  // Log for review
  console.log('Terminal content changes:');
  significantChanges.forEach(change => {
    console.log(change.added ? '+ Added:' : '- Removed:');
    console.log(change.value);
  });
  
  // Optionally assert no unexpected changes
  expect(significantChanges.length).toBeLessThan(5);
});
```

---

## Detailed Implementation Plan

### Week 1: Fix Core Issues

#### Day 1-2: Game Initialization ⚡ URGENT
- [ ] Add debug logging to ConsoleDungeonApp
- [ ] Verify _gameService.Initialize() executes
- [ ] Check timer is firing every 100ms
- [ ] Verify Observable subscriptions
- [ ] Test with: `task capture:quick`
- [ ] Expected: Stats show actual values

#### Day 3: Asciinema Integration
- [ ] Install diff package: `pnpm add -D diff`
- [ ] Create test helper for recording management
- [ ] Test programmatic recording via OSC sequences
- [ ] Copy recordings to versioned artifacts
- [ ] Verify recordings play in browser

### Week 2: Visual Regression

#### Day 1: Baseline Capture
- [ ] Fix game to stable state
- [ ] Run: `pnpm exec playwright test --update-snapshots`
- [ ] Review and commit baseline screenshots
- [ ] Document masking strategy for dynamic content

#### Day 2-3: Regression Tests
- [ ] Create visual regression test suite
- [ ] Configure thresholds (maxDiffPixelRatio)
- [ ] Add to CI pipeline
- [ ] Test with intentional UI change
- [ ] Verify diff images generated

#### Day 4: Text Comparison
- [ ] Implement terminal buffer comparison
- [ ] Create helper for diffing across versions
- [ ] Add to versioned capture test
- [ ] Document expected changes vs regressions

### Week 3: Integration & Documentation

#### Day 1-2: Task Integration
- [ ] Add `task compare:versions --from=v1 --to=v2`
- [ ] Add `task capture:with-recording`
- [ ] Update build/Taskfile.yml
- [ ] Test end-to-end workflow

#### Day 3: CI/CD
- [ ] GitHub Actions workflow
- [ ] Upload artifacts on test failure
- [ ] Comment PR with diff images
- [ ] Archive recordings

#### Day 4-5: Documentation
- [ ] Update README with examples
- [ ] Add troubleshooting guide
- [ ] Create comparison examples
- [ ] Record demo video

---

## Open Questions

### 1. Visual Regression Sensitivity
**Q**: How sensitive should pixel comparison be?
**Options**:
- Strict (maxDiffPixelRatio: 0.001) - Catches everything, many false positives
- Moderate (maxDiffPixelRatio: 0.01) - Balances detection vs noise
- Lenient (maxDiffPixelRatio: 0.05) - Only major changes

**Recommendation**: Start moderate, tune based on false positive rate

### 2. Baseline Management
**Q**: When to update baselines?
**Options**:
- On every version bump (git tag)
- Only on intentional UI changes
- Manual review + approval

**Recommendation**: Intentional changes only, with review process

### 3. Recording Strategy
**Q**: Record every test run or only on demand?
**Options**:
- Always record (large files, useful for debugging)
- Only on failure (saves space, may miss issues)
- Manual trigger (flexible, requires discipline)

**Recommendation**: Start with "only on failure", add manual trigger

### 4. Artifact Retention
**Q**: How long to keep versioned artifacts?
**Options**:
- Forever (large storage, complete history)
- Last 10 versions (reasonable size, recent history)
- Major versions only (minimal storage, gaps in history)

**Recommendation**: Last 10 versions locally, major versions in CI

---

## Risk Assessment

### Low Risk ✅
- Playwright visual regression (native feature)
- Text-based terminal comparison (simple diff)
- Asciinema recording (already implemented)

### Medium Risk ⚠️
- Dynamic content masking (requires tuning)
- False positive rate (may cause alert fatigue)
- Storage growth (versioned artifacts accumulate)

### High Risk 🔴
- None identified for proposed approaches

---

## Success Metrics

After implementation, we should be able to:

1. **Detect Regressions**:
   ```bash
   $ task verify:visual
   ❌ Visual regression detected in terminal display
   See diff: test-results/terminal-diff.png
   Changed pixels: 1247 (5.2%)
   ```

2. **Compare Versions**:
   ```bash
   $ task compare:versions --from=v0.0.1 --to=v0.0.2
   Terminal content changes:
   - Loading game stats...
   + HP: 100/100 | MP: 50/50
   + Entities in world: 6
   ```

3. **Archive State**:
   ```
   build/_artifacts/v0.0.2/
   ├── web/screenshots/          # 5 images
   ├── web/terminal-captures/    # 3 text dumps
   ├── web/recordings/           # 0 video files
   └── dotnet/recordings/        # 1 asciinema cast
   ```

4. **Debug Issues**:
   - Open screenshot from failing version
   - Compare terminal buffer text
   - Watch asciinema playback
   - Review verification JSON

---

## Recommendation Summary

**Priority Order**:
1. 🔴 **Fix game initialization** (blocks everything else)
2. 🟡 **Integrate asciinema** (already 90% done, just copy files)
3. 🟢 **Playwright visual regression** (stable after #1 fixed)
4. 🟢 **Text comparison** (easy, high value)

**Technology Choices**:
- ✅ Playwright built-in: Visual regression
- ✅ diff package: Terminal text comparison
- ✅ Existing RecordingManager: Asciinema
- ❌ NOT pixelmatch: Playwright is sufficient

**Timeline**: 2-3 weeks for full implementation

Would you like me to proceed with Phase 1 (fixing game initialization) first, or would you prefer to start with the visual regression setup?

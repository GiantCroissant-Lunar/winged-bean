# Versioned Testing Strategy

**Date**: 2025-10-03  
**Status**: Implemented  
**Related**: RFC-0010, RFC-0009

---

## Problem Solved

Traditional spec tests only tell you if something **passes or fails**. They don't show you **what actually happened** at each version. As the application evolves:

- UI changes across versions
- Behavior subtly shifts
- Bugs appear and get fixed
- Features get added incrementally

**Question**: "How do I know what v0.0.1 actually showed vs v0.0.2?"  
**Answer**: Versioned artifacts capture the complete state at each version.

---

## Solution: Versioned State Capture

Every test run captures artifacts into `build/_artifacts/v{GitVersion}/`:

```
build/_artifacts/v0.0.1-architecture-realignment.1/
├── dotnet/
│   ├── bin/           # .NET binaries
│   ├── recordings/    # Asciinema cast files
│   └── logs/          # App logs
├── web/
│   ├── screenshots/   # ✨ Playwright captures
│   │   ├── dungeon-initial-{timestamp}.png
│   │   ├── dungeon-running-{timestamp}.png
│   │   └── terminal-only-{timestamp}.png
│   ├── terminal-captures/  # ✨ Buffer text dumps
│   │   └── terminal-buffer-{timestamp}.txt
│   └── logs/          # ✨ Verification JSON
│       └── verification-{timestamp}.json
└── pty/
    ├── dist/          # PTY service
    └── logs/          # PTY logs
```

---

## Real Example from Today

**Version**: `v0.0.1-architecture-realignment.1`  
**Date**: 2025-10-03

### What We Captured

**Terminal Display** (from actual Playwright capture):
```
┌┤Console Dungeon - ECS Dungeon Crawler (Gameplay Active)├──┐
│                                                            │
│ Loading game stats...                                      │
│                                                            │
│ Game initializing...                                       │
│                                                            │
│ Entity count loading...                                    │
│                                                            │
│ Dungeon game is running in the background (ECS active)     │
│                                                            │
│ Watch the stats update in real-time as the game ticks!     │
│                                                            │
│                       [Quit]                               │
└────────────────────────────────────────────────────────────┘
```

**Verification Results**:
```json
{
  "version": "0.0.1-architecture-realignment.1",
  "checks": {
    "hasTitle": true,        ✅ Window shows title
    "hasGameplay": true,     ✅ Gameplay indicators present
    "hasStats": true,        ✅ Stats section exists (but "Loading...")
    "hasEntities": true,     ✅ Entity count section exists
    "hasGameState": true     ✅ Game state section exists
  }
}
```

**Key Finding**: 
- ✅ Game IS running (ECS systems active, 6 entities created)
- ✅ UI IS displaying (Terminal.Gui window visible)
- ❌ BUT labels show "Loading..." instead of actual stats
- 🔍 **Root cause**: Game service not initializing or timer not firing

This is EXACTLY the kind of issue versioned testing helps identify!

---

## Usage

### Quick Commands

```bash
# From project root
cd build

# Show available tasks
task --list | grep capture

# Capture current version state
task capture:state

# Quick check (no assertions)
task capture:quick

# Run verification tests
task verify:visual
```

### Development Workflow

1. **During development**:
   ```bash
   # Make changes
   vim development/dotnet/console/src/plugins/...
   
   # Rebuild
   task console:build
   pm2 restart console-dungeon
   
   # Quick visual check
   task capture:quick
   ```

2. **Before committing**:
   ```bash
   # Capture version state
   task capture:state
   
   # Review artifacts
   open build/_artifacts/v$(./get-version.sh)/web/screenshots/
   ```

3. **Debugging issues**:
   ```bash
   # Compare versions
   diff \
     build/_artifacts/v0.0.1/web/terminal-captures/terminal-buffer-*.txt \
     build/_artifacts/v0.0.2/web/terminal-captures/terminal-buffer-*.txt
   
   # View screenshots side-by-side
   open build/_artifacts/v*/web/screenshots/dungeon-running-*.png
   ```

---

## Benefits Over Traditional Testing

### Traditional Spec Test
```javascript
test('dungeon displays correctly', async ({ page }) => {
  await page.goto('/demo/');
  const title = await page.title();
  expect(title).toContain('Console Dungeon');
});
```
**Result**: ✅ Pass or ❌ Fail  
**Information**: Binary (works or doesn't)

### Versioned State Capture
```javascript
test('capture dungeon state for v0.0.1', async ({ page }) => {
  await page.goto('/demo/');
  await page.screenshot({ path: 'v0.0.1/web/screenshots/dungeon.png' });
  const terminalText = await getTerminalBuffer();
  fs.writeFileSync('v0.0.1/web/terminal-captures/buffer.txt', terminalText);
  const checks = runVerificationChecks(terminalText);
  fs.writeFileSync('v0.0.1/web/logs/verification.json', JSON.stringify(checks));
});
```
**Result**: ✅ Pass + artifacts  
**Information**: 
- Screenshot showing EXACTLY what appeared
- Terminal text showing EXACTLY what was rendered
- Structured verification data for comparison
- Timestamped for troubleshooting

---

## Integration with Existing Tools

### Playwright (Already integrated)
- Screenshots: ✅ Captured to versioned artifacts
- Terminal buffer: ✅ Extracted via xterm.js API
- Videos: ✅ Auto-recorded on failure
- Traces: ✅ Available for debugging

### Asciinema (Plugin available, RFC-0009)
- Plugin: `WingedBean.Plugins.AsciinemaRecorder`
- Status: Lazy-loaded (not currently active)
- Future: Integrate recording trigger from tests
- Cast files → `build/_artifacts/v{VERSION}/dotnet/recordings/`

### PM2 (Runtime management)
- Logs auto-saved to: `development/nodejs/pty-service/logs/`
- Integration needed: Copy logs to versioned artifacts
- Future task: `task dev:archive-logs`

---

## Comparison Example

### Version 0.0.1-alpha (hypothetical)
```
Console Dungeon - Simple Demo
Plugin System Active
Loaded at 10:00:00
[Quit]
```

### Version 0.0.1-architecture-realignment.1 (current)
```
Console Dungeon - ECS Dungeon Crawler (Gameplay Active)
Loading game stats...
Game initializing...
Entity count loading...
Dungeon game is running in the background (ECS systems active)
Watch the stats update in real-time as the game ticks!
[Quit]
```

**Evolution visible**: 
- Title evolved: "Simple Demo" → "ECS Dungeon Crawler"
- Features added: Stats section, entity count, game state
- Architecture: Plugin system → ECS with DungeonGameService
- Issue introduced: Stats show "Loading..." (needs fix)

---

## Future Enhancements

### 1. Automated Visual Regression
```bash
task compare:versions --from=v0.0.1 --to=v0.0.2
# Output: Pixel-by-pixel diff highlighting changes
```

### 2. Video Recordings
```javascript
// Save Playwright videos to artifacts
await testInfo.video().saveAs(`v${VERSION}/web/recordings/test.webm`);
```

### 3. Asciinema Integration
```bash
task capture:asciinema --duration=30s
# Saves to: v{VERSION}/dotnet/recordings/session-{timestamp}.cast
```

### 4. CI Artifact Upload
```yaml
# .github/workflows/test.yml
- name: Capture versioned state
  run: task capture:state
- uses: actions/upload-artifact@v3
  with:
    name: version-artifacts-${{ VERSION }}
    path: build/_artifacts/v*/
```

---

## Troubleshooting

### "No screenshots captured"
**Check**:
1. Services running? `task dev:status`
2. Correct URL? http://localhost:4321/demo/
3. Playwright installed? `pnpm install`

### "Terminal buffer empty"
**Check**:
1. xterm.js loaded? Check browser console
2. WebSocket connected? Check PTY service logs
3. Wait longer: Increase `waitForTimeout`

### "Version shows as 'dev'"
**Check**:
1. GitVersion configured? `cd build && ./get-version.sh`
2. Git tags present? `git tag --list`
3. Fallback is expected during development

---

## See Also

- [RFC-0010: Multi-Language Build Orchestration](../rfcs/0010-multi-language-build-orchestration-with-task.md)
- [RFC-0009: Dynamic Asciinema Recording](../rfcs/0009-dynamic-asciinema-recording-in-pty.md)
- [E2E Testing README](../../development/nodejs/tests/e2e/README.md)
- [Build System README](../../build/README.md)

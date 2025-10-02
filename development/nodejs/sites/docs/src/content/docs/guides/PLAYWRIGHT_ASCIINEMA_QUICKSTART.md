---
title: "Quick Start: Playwright + Asciinema Testing"
---

# Quick Start: Playwright + Asciinema Testing

This guide helps you quickly set up visual testing and recording for Terminal.Gui v2.

---

## Part 1: Playwright Visual Testing (30 minutes)

### Step 1: Install Playwright

```bash
cd development/nodejs

# Install Playwright
pnpm add -D @playwright/test

# Install browser (Chromium only for now)
npx playwright install chromium
```

### Step 2: Create Playwright Config

```bash
# Create config file
cat > playwright.config.js << 'EOF'
module.exports = {
  testDir: './tests/e2e',
  timeout: 30000,
  use: {
    baseURL: 'http://localhost:4321',
    screenshot: 'only-on-failure',
    trace: 'retain-on-failure',
  },
  projects: [
    {
      name: 'chromium',
      use: { 
        browserName: 'chromium',
        viewport: { width: 1280, height: 900 }
      }
    },
  ],
};
EOF
```

### Step 3: Update package.json Scripts

```bash
# Add to package.json scripts section:
npm pkg set scripts.test:e2e="playwright test"
npm pkg set scripts.test:e2e:headed="playwright test --headed"
npm pkg set scripts.test:e2e:debug="playwright test --debug"
```

### Step 4: Run Your First Visual Test

```bash
# Make sure services are running
pm2 status

# If not running, start them
pm2 start ecosystem.config.js

# Run Playwright test (headless)
pnpm test:e2e terminal-gui-visual-verification

# Or run with visible browser
pnpm test:e2e:headed terminal-gui-visual-verification
```

### Step 5: View Screenshots

```bash
# Screenshots are saved to:
ls -lh tests/e2e/screenshots/

# View them:
open tests/e2e/screenshots/terminal-gui-pty-display.png
```

**Expected Result:** You should see a screenshot of the browser showing Terminal.Gui v2 interface in xterm.js.

---

## Part 2: Asciinema Recording (20 minutes)

### Step 1: Install Asciinema

```bash
# macOS
brew install asciinema

# Ubuntu/Debian
sudo apt-get install asciinema

# Verify installation
asciinema --version
```

### Step 2: Record Your First Session

```bash
# Create recordings directory
mkdir -p docs/recordings

# Record a Terminal.Gui session
asciinema rec docs/recordings/terminal-gui-demo.cast \
  --title "Terminal.Gui v2 Demo" \
  --command "cd development/dotnet/console && dotnet run --project src/host/TerminalGui.PtyHost/TerminalGui.PtyHost.csproj"

# Press Ctrl+Q to quit Terminal.Gui and stop recording
```

### Step 3: Play Back the Recording

```bash
# Play in terminal
asciinema play docs/recordings/terminal-gui-demo.cast

# Play at 2x speed
asciinema play -s 2 docs/recordings/terminal-gui-demo.cast
```

### Step 4: Embed in Documentation

```markdown
<!-- In your README.md or docs -->

## Terminal.Gui v2 Demo

<asciinema-player src="docs/recordings/terminal-gui-demo.cast"></asciinema-player>
```

### Step 5: Create Recording Script

```bash
# Create script
cat > scripts/record-session.sh << 'EOF'
#!/bin/bash
TIMESTAMP=$(date +%Y%m%d-%H%M%S)
OUTPUT="docs/recordings/terminal-gui-$TIMESTAMP.cast"

echo "Recording Terminal.Gui session..."
echo "Output: $OUTPUT"

asciinema rec "$OUTPUT" \
  --title "Terminal.Gui v2 - $TIMESTAMP" \
  --command "cd development/dotnet/console && dotnet run --project src/host/TerminalGui.PtyHost/TerminalGui.PtyHost.csproj"

echo "Recording saved to: $OUTPUT"
EOF

chmod +x scripts/record-session.sh

# Use it
./scripts/record-session.sh
```

---

## Part 3: Automated Recording in PTY Service (Advanced)

### Option A: Simple File Recording

Add to `pty-service/server.js`:

```javascript
const fs = require('fs');
const path = require('path');

wss.on('connection', (ws) => {
  // Create recording file
  const recordingDir = path.join(__dirname, '../../../docs/recordings');
  const recordingFile = path.join(recordingDir, `session-${Date.now()}.cast`);
  
  // Ensure directory exists
  fs.mkdirSync(recordingDir, { recursive: true });
  
  // Create cast file header
  const header = {
    version: 2,
    width: 80,
    height: 24,
    timestamp: Math.floor(Date.now() / 1000),
    title: 'Terminal.Gui v2 PTY Session'
  };
  
  fs.writeFileSync(recordingFile, JSON.stringify(header) + '\n');
  const recordingStream = fs.createWriteStream(recordingFile, { flags: 'a' });
  
  const startTime = Date.now();
  
  // Record PTY output
  ptyProcess.onData((data) => {
    ws.send(data);
    
    // Write to cast file
    const timestamp = (Date.now() - startTime) / 1000;
    const event = [timestamp, 'o', data];
    recordingStream.write(JSON.stringify(event) + '\n');
  });
  
  ws.on('close', () => {
    recordingStream.end();
    console.log('Recording saved:', recordingFile);
  });
});
```

### Option B: Use asciinema-recorder Package

```bash
cd development/nodejs/pty-service
npm install asciinema-recorder
```

Then update `server.js`:

```javascript
const AsciinemaRecorder = require('asciinema-recorder');

wss.on('connection', (ws) => {
  const recorder = new AsciinemaRecorder({
    outputFile: `../../../docs/recordings/session-${Date.now()}.cast`,
    width: 80,
    height: 24,
    title: 'Terminal.Gui v2 PTY Session'
  });
  
  recorder.start();
  
  ptyProcess.onData((data) => {
    ws.send(data);
    recorder.write(data);
  });
  
  ws.on('close', () => {
    recorder.stop();
  });
});
```

---

## Quick Reference

### Playwright Commands

```bash
# Run all E2E tests
pnpm test:e2e

# Run specific test
pnpm test:e2e terminal-gui-visual

# Run with visible browser
pnpm test:e2e:headed

# Debug mode (step through)
pnpm test:e2e:debug

# Update screenshots
pnpm test:e2e --update-snapshots
```

### Asciinema Commands

```bash
# Record session
asciinema rec output.cast

# Record with command
asciinema rec output.cast --command "your-command"

# Play recording
asciinema play output.cast

# Upload to asciinema.org
asciinema upload output.cast

# Convert to GIF (requires agg)
agg output.cast output.gif
```

### PM2 Commands

```bash
# Check services
pm2 status

# Start services
pm2 start ecosystem.config.js

# View logs
pm2 logs

# Restart services
pm2 restart all
```

---

## Troubleshooting

### Playwright Issues

**Problem:** Browser not found
```bash
# Solution: Install browsers
npx playwright install
```

**Problem:** Test timeout
```bash
# Solution: Increase timeout in test
test.setTimeout(60000); // 60 seconds
```

**Problem:** Services not running
```bash
# Solution: Start PM2 services
cd development/nodejs
pm2 start ecosystem.config.js
```

### Asciinema Issues

**Problem:** Command not found
```bash
# macOS
brew install asciinema

# Linux
sudo apt-get install asciinema
```

**Problem:** Recording too large
```bash
# Solution: Limit recording time
timeout 30s asciinema rec output.cast
```

**Problem:** Can't play recording
```bash
# Check file format
file output.cast

# Should be: ASCII text
```

---

## Next Steps

1. ✅ **Run Playwright test** to verify Terminal.Gui is visible
2. ✅ **Record a demo session** with asciinema
3. ✅ **Add recordings to docs** for stakeholders
4. ✅ **Set up CI/CD** to run tests automatically
5. ✅ **Create baseline screenshots** for regression testing

---

## Resources

- [Playwright Documentation](https://playwright.dev/)
- [Asciinema Documentation](https://asciinema.org/)
- [RFC-0003: Full Testing Strategy](../rfcs/0003-playwright-and-asciinema-testing-strategy.md)
- [Terminal.Gui PTY Integration](../handover/TERMINAL_GUI_PTY_INTEGRATION.md)

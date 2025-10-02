# RFC-0006 Phase 5 Wave 5.3: xterm.js Integration Tests

## Overview

This directory contains the automated test suite for verifying xterm.js integration after RFC-0006 dynamic plugin loading implementation.

## Test File

**File:** `rfc-0006-phase5-wave5.3-xterm-regression.test.js`

**Purpose:** Critical regression test to ensure that dynamic plugin loading does not break xterm.js/WebSocket/Terminal.Gui integration.

## Prerequisites

### 1. Build .NET Project

```bash
cd development/dotnet/console
dotnet build Console.sln -c Debug
```

This will:
- Build ConsoleDungeon.Host with dynamic plugin loading
- Copy plugins to `bin/Debug/net8.0/plugins/` directory
- Create plugins.json configuration file

### 2. Install Node.js Dependencies

```bash
cd development/nodejs
npm install --legacy-peer-deps
```

### 3. Install Playwright (Optional)

For full browser-based tests:

```bash
cd development/nodejs
npx playwright install chromium
```

**Note:** If Playwright is not available (e.g., in CI without browser support), the WebSocket test script provides equivalent verification.

## Running Tests

### Option 1: Full Test Suite (Requires Playwright)

```bash
cd development/nodejs
npm test -- rfc-0006-phase5-wave5.3-xterm-regression.test.js
```

**Test Cases:**
- ✅ Wave 5.3.1: ConsoleDungeon.Host loads plugins dynamically
- ✅ Wave 5.3.2: Astro page loads successfully
- ✅ Wave 5.3.3: WebSocket connection established
- ✅ Wave 5.3.4: Terminal.Gui interface renders correctly
- ✅ Wave 5.3.5: Dynamic loading does not break xterm.js
- ✅ Wave 5.3.6: Terminal displays plugin-loaded services

### Option 2: WebSocket Test (No Browser Required)

```bash
# Terminal 1: Start host
cd development/dotnet/console/src/host/ConsoleDungeon.Host/bin/Debug/net8.0
dotnet ConsoleDungeon.Host.dll

# Terminal 2: Run test
cd /path/to/winged-bean
NODE_PATH=./development/nodejs/node_modules:$NODE_PATH \
  node ./scripts/verification/test-websocket.js
```

**Expected Output:**
```
========================================
RFC-0005: WebSocket Connection Test
========================================

[1/4] Connecting to WebSocket server at ws://localhost:4040...
  ✓ WebSocket connection established
[2/4] Sending "init" message...
[3/4] Received response from server
  ✓ Server responded to init message
  ✓ Screen content received

========================================
Test Results
========================================
WebSocket Connection:   ✅ PASS
Server Response:        ✅ PASS
Terminal.Gui Elements:  ✅ PASS
========================================

✅ SUCCESS: xterm.js integration is working!
```

## What the Tests Verify

### Dynamic Plugin Loading
- ✅ ConsoleDungeon.Host starts with "Dynamic Plugin Mode"
- ✅ 5 eager plugins load from plugins.json
- ✅ 1 lazy plugin (AsciinemaRecorder) skipped correctly
- ✅ Services auto-register from plugins (IWebSocketService, ITerminalUIService)

### xterm.js Integration
- ✅ WebSocket server starts on port 4040
- ✅ WebSocket connection established successfully
- ✅ Terminal.Gui v2 content renders in xterm.js
- ✅ Screen updates transmitted correctly
- ✅ No regressions from dynamic loading changes

## Test Architecture

```
Test Setup (beforeAll)
├─ Start ConsoleDungeon.Host (from bin/Debug/net8.0 directory)
│  ├─ Load plugins from plugins.json
│  ├─ Register services from plugins
│  └─ Start WebSocket server on port 4040
├─ Start Astro dev server (port 4321)
└─ Launch Playwright browser

Test Execution
├─ Verify plugin loading output
├─ Load Astro page with xterm.js
├─ Check WebSocket connection
├─ Verify Terminal.Gui rendering
└─ Confirm no regressions

Test Cleanup (afterAll)
├─ Close browser
├─ Stop ConsoleDungeon.Host
└─ Stop Astro server
```

## Important Notes

### Working Directory

The test **must** run ConsoleDungeon.Host from the `bin/Debug/net8.0` directory because:
- `plugins.json` uses relative paths to plugin DLLs
- Plugin paths are relative to the execution directory
- Running from project directory will fail to find plugins

### Test Timeout

The test has a 60-second setup timeout to allow for:
- .NET host startup (~5 seconds)
- Plugin loading (~2 seconds)
- Astro dev server startup (~10-15 seconds)
- Browser launch (~3 seconds)

### Port Conflicts

If tests fail with "port already in use":

```bash
# Kill processes on port 4040 (WebSocket)
lsof -ti:4040 | xargs kill -9

# Kill processes on port 4321 (Astro)
lsof -ti:4321 | xargs kill -9
```

## Troubleshooting

### Error: "Plugin assembly not found"

**Cause:** Running from wrong directory or plugins not built.

**Solution:**
```bash
cd development/dotnet/console
dotnet build Console.sln -c Debug
# Verify plugins exist
ls -la src/host/ConsoleDungeon.Host/bin/Debug/net8.0/plugins/
```

### Error: "Executable doesn't exist" (Playwright)

**Cause:** Playwright browsers not installed.

**Solution:**
```bash
cd development/nodejs
npx playwright install chromium
```

**Alternative:** Use WebSocket test script (doesn't require browser).

### Error: "ECONNREFUSED" (WebSocket test)

**Cause:** ConsoleDungeon.Host not running or not listening on port 4040.

**Solution:**
1. Check host is running: `lsof -i:4040`
2. Check host logs for "WebSocket server started"
3. Verify no firewall blocking port 4040

## Success Criteria

All tests pass with:
- ✅ Green checkmarks in test output
- ✅ "SUCCESS" messages
- ✅ Zero exceptions or errors
- ✅ Exit code 0

## Related Documentation

- **Verification Document:** `docs/verification/RFC-0006-Phase5-Wave5.3-xterm-integration-verification.md`
- **Issue #57 Summary:** `docs/testing/rfc-0006-issue-57-summary.md`
- **RFC-0006:** `docs/rfcs/0006-dynamic-plugin-loading.md`
- **WebSocket Test Script:** `scripts/verification/test-websocket.js`

## CI/CD Integration

For environments without browser support:

```yaml
# Example GitHub Actions workflow
- name: Build .NET Project
  run: |
    cd development/dotnet/console
    dotnet build Console.sln -c Debug

- name: Start ConsoleDungeon.Host
  run: |
    cd development/dotnet/console/src/host/ConsoleDungeon.Host/bin/Debug/net8.0
    dotnet ConsoleDungeon.Host.dll &
    sleep 5  # Wait for startup

- name: Test WebSocket Integration
  run: |
    cd development/nodejs
    npm install --legacy-peer-deps
    NODE_PATH=./node_modules:$NODE_PATH \
      node ../../scripts/verification/test-websocket.js
```

## Questions?

See the comprehensive verification document for detailed analysis:
- `docs/verification/RFC-0006-Phase5-Wave5.3-xterm-integration-verification.md`

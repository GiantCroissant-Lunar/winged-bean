# Verification Scripts

This directory contains scripts for verifying RFC implementations and integrations.

## RFC-0005 Phase 5.3: xterm.js Integration Verification

### Scripts

#### 1. `verify-integration.sh`

**Purpose:** Start ConsoleDungeon.Host and Astro frontend for manual verification

**Prerequisites:**
- ConsoleDungeon.Host built (`dotnet build -c Release`)
- Astro dependencies installed (`cd development/nodejs/sites/docs && npm install`)

**Usage:**
```bash
cd scripts/verification
./verify-integration.sh
```

**What it does:**
1. Starts ConsoleDungeon.Host on port 4040 (WebSocket)
2. Starts Astro dev server on port 4321
3. Displays manual verification instructions
4. Keeps services running until Ctrl+C

**Output:**
```
=========================================
RFC-0005 Phase 5.3: xterm.js Verification
=========================================

[1/5] Starting ConsoleDungeon.Host (WebSocket on port 4040)...
  ✓ Started ConsoleDungeon.Host (PID: 1234)
  ✓ Logs: /tmp/xterm-verification/consoledungeon.log

[2/5] Waiting for WebSocket server (port 4040)...
  ✓ WebSocket server is ready on port 4040

[3/5] Starting Astro dev server (port 4321)...
  ✓ Started Astro dev server (PID: 5678)
  ✓ Logs: /tmp/xterm-verification/astro.log

[4/5] Waiting for Astro dev server (port 4321)...
  ✓ Astro dev server is ready on port 4321

=========================================
✅ Both services are running!
=========================================

[5/5] Manual Verification Steps:

1. Open browser to: http://localhost:4321
2. Verify xterm.js terminal loads
3. Verify WebSocket connection message appears
4. Verify Terminal.Gui interface renders
5. Try keyboard input and verify response
```

#### 2. `test-websocket.js`

**Purpose:** Automated WebSocket connection test

**Prerequisites:**
- ConsoleDungeon.Host running on port 4040
- Node.js dependencies installed (`cd development/nodejs && npm install`)

**Usage:**
```bash
cd development/nodejs
NODE_PATH=./node_modules:$NODE_PATH node ../../scripts/verification/test-websocket.js
```

**What it does:**
1. Connects to WebSocket server at ws://localhost:4040
2. Sends "init" message
3. Receives and validates Terminal.Gui screen content
4. Verifies interface elements
5. Reports test results

**Output:**
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
  Screen content preview (first 200 chars):
  Terminal.Gui v2 PTY Demo
  ========================

[4/4] Verifying Terminal.Gui interface elements:
  Console Dungeon title: ✓ Found
  WebSocket info:        ✓ Found

========================================
Test Results
========================================
WebSocket Connection:   ✅ PASS
Server Response:        ✅ PASS
Terminal.Gui Elements:  ✅ PASS
========================================

✅ SUCCESS: xterm.js integration is working!
```

## Complete Verification Workflow

### Quick Test (Automated)

```bash
# Terminal 1: Start ConsoleDungeon.Host
cd development/dotnet/console/src/host/ConsoleDungeon.Host
dotnet run --no-build -c Release

# Terminal 2: Run automated test
cd development/nodejs
npm install
NODE_PATH=./node_modules:$NODE_PATH node ../../scripts/verification/test-websocket.js
```

### Full Manual Verification

```bash
# Run verification script
cd scripts/verification
./verify-integration.sh

# In browser: Open http://localhost:4321
# Verify:
# 1. xterm.js terminal loads
# 2. WebSocket connection message appears
# 3. Terminal.Gui interface renders
# 4. Keyboard input works

# Press Ctrl+C to stop services
```

## Troubleshooting

### Port Already in Use

If port 4040 or 4321 is already in use:

```bash
# Check what's using the port
lsof -i :4040
lsof -i :4321

# Kill processes
kill -9 <PID>
```

### WebSocket Connection Failed

1. Verify ConsoleDungeon.Host is running:
   ```bash
   curl http://localhost:4040
   # Should fail but port should be open
   ```

2. Check logs:
   ```bash
   tail -f /tmp/xterm-verification/consoledungeon.log
   ```

### Astro Server Not Starting

1. Verify dependencies are installed:
   ```bash
   cd development/nodejs/sites/docs
   npm install
   ```

2. Check logs:
   ```bash
   tail -f /tmp/xterm-verification/astro.log
   ```

## Related Documentation

- [RFC-0005 Phase 5.3 Verification](../../docs/verification/RFC-0005-Phase5-Wave5.3-xterm-integration-verification.md)
- [Terminal.Gui PTY Integration](../../docs/handover/TERMINAL_GUI_PTY_INTEGRATION.md)
- [Phase 3 xterm.js Regression Test](../../development/nodejs/tests/integration/phase3-xterm-regression.test.js)

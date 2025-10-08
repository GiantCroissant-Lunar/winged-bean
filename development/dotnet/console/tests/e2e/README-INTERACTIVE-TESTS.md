# Interactive Console Operation Tests

This directory contains interactive tests that verify ConsoleDungeon.Host can be operated via keyboard input in a real terminal environment. Tests use `expect` for automation and `asciinema` for recording and validation.

## 📋 Test Overview

### Test Scripts

| Script | Type | Purpose |
|--------|------|---------|
| `interactive-test-console.exp` | Expect | Tests keyboard input (arrows, WASD, Enter, Space) |
| `record-console-session.exp` | Expect + Asciinema | Records automated session for validation |
| `validate-recording.sh` | Bash | Validates recorded session content |
| `run-interactive-tests.sh` | Bash | Master script to run all interactive tests |

### What Gets Tested

1. **Keyboard Input**
   - Arrow keys (Up, Down, Left, Right)
   - WASD keys (game controls)
   - Enter key
   - Space key
   - Tab key
   - Quit command (q)

2. **Application Behavior**
   - Host starts successfully
   - UI renders in terminal
   - Input is processed
   - Application responds
   - Clean exit

3. **Recording Validation**
   - Host startup messages appear
   - Plugins load successfully
   - No fatal errors
   - Terminal UI active
   - Reasonable content length

## 🚀 Running Interactive Tests

### Quick Start

```bash
# From the e2e directory
./run-interactive-tests.sh
```

This will:
1. Check prerequisites (expect, asciinema, jq)
2. Build ConsoleDungeon.Host
3. Run interactive console test
4. Record asciinema session
5. Validate recording content

### Run Individual Tests

#### Interactive Test Only

```bash
./interactive-test-console.exp
```

This spawns the console application and sends automated keyboard input to test operation.

#### Recording Test Only

```bash
./record-console-session.exp
```

This records an asciinema session with automated input. Output: `recordings/console-operation-test.cast`

#### Validate Existing Recording

```bash
./validate-recording.sh
```

Checks recorded session for expected content patterns.

## 📦 Prerequisites

### Required Tools

```bash
# macOS
brew install expect
brew install asciinema

# Ubuntu/Debian
sudo apt-get install expect
sudo apt-get install asciinema

# Arch Linux
sudo pacman -S expect
sudo pacman -S asciinema
```

### Optional Tools

```bash
# For better recording validation
brew install jq
```

### Check Installation

```bash
expect -version      # Should show: expect version 5.45
asciinema --version  # Should show: asciinema 2.x or 3.x
jq --version         # Should show: jq-1.x
```

## 📊 Expected Output

### Successful Interactive Test

```
=========================================
Interactive Console Test
=========================================
Test directory: /path/to/tests/e2e
Console directory: /path/to/ConsoleDungeon.Host

Starting ConsoleDungeon.Host...
=========================================

✓ Host started successfully
✓ Foundation services initializing
✓ Loading plugins...
✓ Plugins loaded

=========================================
Testing Keyboard Input
=========================================

Sending Right arrow...
Sending Down arrow...
Sending Left arrow...
Sending Up arrow...
Sending 'd' (right)...
Sending 's' (down)...
Sending 'a' (left)...
Sending 'w' (up)...
Sending Enter...
Sending Space...

=========================================
Input Test Complete
=========================================

Sending quit command (q)...

✓ Application exited cleanly
```

### Successful Recording Test

```
=========================================
Asciinema Recording Test
=========================================
Test directory: /path/to/tests/e2e
Console directory: /path/to/ConsoleDungeon.Host
Recording directory: /path/to/recordings

Starting asciinema recording...
=========================================

✓ Recording started, host is starting...
✓ Plugins loading...

=========================================
Performing Automated Operations
=========================================

1. Testing arrow key navigation...
2. Testing WASD controls...
3. Testing interaction keys...
4. Testing menu keys...

=========================================
Operations Complete
=========================================

Exiting application...

✓ Recording completed successfully
✓ Cast file saved to: recordings/console-operation-test.cast

To replay:
  asciinema play recordings/console-operation-test.cast

To upload:
  asciinema upload recordings/console-operation-test.cast
```

### Successful Validation

```
=========================================
Recording Validation Script
=========================================

✓ Found cast file: recordings/console-operation-test.cast

Extracting content from recording...
Content length: 15842 characters

=========================================
Running Validation Checks
=========================================

1. Checking if host started... ✓ PASS
2. Checking foundation services... ✓ PASS
3. Checking plugins loaded... ✓ PASS
4. Checking for fatal errors... ✓ PASS (no fatal errors)
5. Checking terminal UI activity... ✓ PASS
6. Checking recording length... ✓ PASS (15842 bytes)

=========================================
Validation Summary
=========================================
Passed: 6
Failed: 0

✓ All validation checks passed!

To view the recording:
  asciinema play recordings/console-operation-test.cast
```

## 🎬 Working with Recordings

### View Recording

```bash
asciinema play recordings/console-operation-test.cast
```

### Share Recording

```bash
# Upload to asciinema.org
asciinema upload recordings/console-operation-test.cast

# Or convert to GIF/MP4 (requires additional tools)
# Install: npm install -g asciicast2gif
asciicast2gif recordings/console-operation-test.cast recording.gif
```

### Embed in Documentation

After uploading:
```html
<script id="asciicast-XXXXX" src="https://asciinema.org/a/XXXXX.js" async></script>
```

## 🔧 How It Works

### Expect Script Structure

```tcl
#!/usr/bin/expect -f

set timeout 30
cd /path/to/host

# Spawn application
spawn dotnet run --no-build

# Wait for startup
expect {
    "Host starting" { puts "✓ Started" }
    timeout { puts "✗ Timeout"; exit 1 }
}

# Send input
send "\033\[C"  ;# Right arrow
send "w"        ;# W key
send "\r"       ;# Enter

# Quit
send "q"
expect eof
```

### Asciinema Recording

```tcl
# Start recording with command
spawn asciinema rec --quiet --overwrite output.cast -c "dotnet run"

# Perform operations
send "input"
sleep 0.3

# Application exits, asciinema saves recording
expect eof
```

### Validation Script

```bash
# Extract content
CONTENT=$(cat recording.cast | jq -r '.[2]')

# Check patterns
if echo "$CONTENT" | grep -q "Expected Pattern"; then
    echo "✓ PASS"
else
    echo "✗ FAIL"
fi
```

## 🐛 Troubleshooting

### Issue: Expect times out

**Symptoms:**
```
✗ Timeout waiting for host to start
```

**Solutions:**
1. Increase timeout: `set timeout 60`
2. Check if host builds: `dotnet build`
3. Run host manually to see actual output
4. Check for blocking prompts

### Issue: Terminal rendering issues

**Symptoms:**
- Garbled output
- ANSI codes visible
- UI not rendering

**Solutions:**
1. Set proper TERM: `set env(TERM) "xterm-256color"`
2. Check terminal compatibility
3. Use PTY mode if available

### Issue: Recording is empty or corrupt

**Symptoms:**
```
✗ FAIL (only 123 bytes)
```

**Solutions:**
1. Check asciinema version: `asciinema --version`
2. Verify host actually runs
3. Increase wait times in script
4. Check file permissions

### Issue: Application doesn't respond to input

**Symptoms:**
- Input sent but no response
- Application hangs

**Solutions:**
1. Check if Terminal.Gui is initialized
2. Verify input mode is correct
3. Test with manual input first
4. Check for event loop issues

## 📈 Integration with CI/CD

### GitHub Actions Example

```yaml
name: Interactive Tests

on: [push, pull_request]

jobs:
  interactive-tests:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v3
      
      - name: Setup .NET
        uses: actions/setup-dotnet@v3
        with:
          dotnet-version: '8.0.x'
      
      - name: Install Prerequisites
        run: |
          sudo apt-get update
          sudo apt-get install -y expect asciinema jq
      
      - name: Run Interactive Tests
        run: |
          cd yokan-projects/winged-bean/development/dotnet/console/tests/e2e
          chmod +x run-interactive-tests.sh
          ./run-interactive-tests.sh
      
      - name: Upload Recording
        if: always()
        uses: actions/upload-artifact@v3
        with:
          name: console-recording
          path: yokan-projects/winged-bean/development/dotnet/console/tests/e2e/recordings/*.cast
```

### Azure DevOps Example

```yaml
- script: |
    sudo apt-get install -y expect asciinema
    cd yokan-projects/winged-bean/development/dotnet/console/tests/e2e
    chmod +x run-interactive-tests.sh
    ./run-interactive-tests.sh
  displayName: 'Run Interactive Tests'

- task: PublishBuildArtifacts@1
  condition: always()
  inputs:
    PathtoPublish: 'yokan-projects/winged-bean/development/dotnet/console/tests/e2e/recordings'
    ArtifactName: 'console-recordings'
```

## 🎯 Test Scenarios Covered

### Basic Input

✅ Arrow key navigation (Up/Down/Left/Right)
✅ WASD key controls
✅ Enter key (selection/confirmation)
✅ Space key (action)
✅ Tab key (navigation)
✅ Quit command

### Application Lifecycle

✅ Startup (host initialization)
✅ Plugin loading
✅ UI rendering
✅ Input processing
✅ Clean shutdown

### Error Conditions

✅ No fatal errors during operation
✅ No unhandled exceptions
✅ Graceful handling of input
✅ Proper cleanup on exit

## 📚 Related Documentation

- [E2E Tests README](./README-E2E-TESTS.md) - Process-based E2E tests
- [Testing Checklist](../../../docs/TESTING-CHECKLIST-Console-Web-PTY.md) - Manual testing guide
- [Namespace Migration Handover](../../../docs/HANDOVER-2025-01-29-Namespace-Migration-Complete.md)

## 🏁 Summary

These interactive tests provide confidence that:

1. ✅ Console application can be operated via keyboard
2. ✅ Input events are properly processed
3. ✅ UI responds to user actions
4. ✅ Application behaves correctly in real terminal
5. ✅ Recordings can document and validate behavior

**Test Coverage:** Keyboard input, application lifecycle, error handling, terminal compatibility

**Technologies:** Expect (automation), Asciinema (recording), Bash (validation)

**Next Steps:**
- Add Playwright tests for web-based terminal
- Test PTY mode specifically
- Add WebSocket mode interactive tests
- Create visual regression tests

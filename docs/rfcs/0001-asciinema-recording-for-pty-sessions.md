# RFC-0001: Asciinema Recording for PTY Sessions

## Status

Proposed

## Date

2025-09-30

## Summary

Add asciinema `.cast` file recording capability to the existing PTY service to capture both input and output events during Terminal.Gui sessions. This enables creating demos, documentation, bug reports, and automated replay capabilities.

## Motivation

### Problems to Solve

1. **Documentation**: Need to capture Terminal.Gui application behavior for tutorials and demos
2. **Bug Reports**: Ability to reproduce and share exact terminal sessions
3. **Automation**: Record user interactions for automated testing/replay
4. **Presentations**: Create shareable recordings for marketing and training materials

### Current State

- PTY service (projects/nodejs/pty-service/server.js) streams Terminal.Gui output to xterm.js via WebSocket
- No recording capability exists
- Sessions are ephemeral - no way to replay or analyze past interactions

## Proposal

### Architecture

Add recording functionality to the PTY service that captures:
- **Output events** (`"o"`): Terminal output (ANSI sequences) from Terminal.Gui
- **Input events** (`"i"`): User keyboard and mouse input sent to PTY
- **Resize events** (`"r"`): Terminal dimension changes

```
┌─────────────────┐
│  Browser Client │
│   (xterm.js)    │
└────────┬────────┘
         │ WebSocket
         ▼
┌─────────────────────────────────────┐
│     PTY Service (Node.js)           │
│  ┌──────────────────────────────┐   │
│  │  Recording Module            │   │
│  │  - Capture input events      │   │
│  │  - Capture output events     │   │
│  │  - Write .cast file          │   │
│  └──────────────────────────────┘   │
│              │                       │
│              ▼                       │
│  ┌──────────────────────────────┐   │
│  │  PTY Process                 │   │
│  │  (Terminal.Gui App)          │   │
│  └──────────────────────────────┘   │
└─────────────────────────────────────┘
```

### File Format

Use standard [asciicast v2 format](https://docs.asciinema.org/manual/asciicast/v2/):

**Header (JSON):**
```json
{"version": 2, "width": 80, "height": 24, "timestamp": 1696077600, "env": {"TERM": "xterm-256color"}}
```

**Events (JSON array per line):**
```json
[0.123456, "o", "terminal output data"]
[1.234567, "i", "\t"]
[1.235000, "o", "response to tab key"]
[2.100000, "i", "\u001b[<0;10;5M"]
[2.101000, "o", "dialog appears"]
[3.500000, "r", "100x30"]
```

Event types:
- `"o"` - Output from PTY to browser
- `"i"` - Input from browser to PTY (keyboard, mouse)
- `"r"` - Terminal resize (format: `"width x height"`)

### Implementation Details

#### 1. Recording Module (`projects/nodejs/pty-service/recording.js`)

```javascript
class CastRecorder {
  constructor(outputPath, cols, rows) {
    // Initialize file writer
    // Write header with metadata
  }

  recordOutput(data) {
    // Write ["o", timestamp, data]
  }

  recordInput(data) {
    // Write ["i", timestamp, data]
  }

  recordResize(cols, rows) {
    // Write ["r", timestamp, "colsxrows"]
  }

  close() {
    // Flush and close file
  }
}
```

#### 2. Server Integration (modify `server.js`)

**On WebSocket Connection:**
```javascript
// Optional recording via query parameter or config
const shouldRecord = /* config or URL param */;
let recorder = null;

if (shouldRecord) {
  const filename = `session-${Date.now()}.cast`;
  recorder = new CastRecorder(
    path.join(__dirname, 'recordings', filename),
    80, 24
  );
}
```

**Hook into existing data flow:**
```javascript
// Capture output (existing code)
ptyProcess.onData((data) => {
  if (recorder) recorder.recordOutput(data);
  if (ws.readyState === WebSocket.OPEN) {
    ws.send(Buffer.from(data, 'utf8'));
  }
});

// Capture input (modify existing code)
ws.on('message', (message) => {
  try {
    const data = JSON.parse(message.toString());

    if (data.type === 'resize') {
      if (recorder) recorder.recordResize(data.cols, data.rows);
      ptyProcess.resize(data.cols, data.rows);
      return;
    }
  } catch (e) {
    // Raw input
    const input = message.toString();
    if (recorder) recorder.recordInput(input);
    ptyProcess.write(input);
  }
});

// Cleanup
ws.on('close', () => {
  if (recorder) recorder.close();
  if (!ptyProcess.killed) ptyProcess.kill();
});
```

#### 3. Configuration

Add to `package.json` or environment:
```json
{
  "recording": {
    "enabled": false,
    "path": "./recordings",
    "autoStart": false,
    "maxFileSizeMB": 100
  }
}
```

#### 4. API Control (Optional)

Add WebSocket control messages:
```json
{"type": "recording", "action": "start"}
{"type": "recording", "action": "stop"}
{"type": "recording", "action": "pause"}
```

### Testing Strategy

#### Unit Tests (`projects/nodejs/pty-service/__tests__/recording.test.js`)
- CastRecorder class initialization
- Event writing (output, input, resize)
- File format validation
- Timestamp accuracy
- Concurrent write safety

#### Integration Tests (`projects/nodejs/pty-service/__tests__/recording-integration.test.js`)
- Full PTY session recording
- Parse and validate generated .cast file
- Verify event ordering and timing
- Test recording start/stop/pause
- File size limits

#### Manual Tests
- Record a simple Terminal.Gui session
- Play back with `asciinema play session.cast`
- Verify visual output matches original
- Test with asciinema-player in browser
- Verify input events are captured correctly

## Implementation Plan

### Phase 1: Core Recording (Required for MVP)
1. Create `CastRecorder` class with basic output recording
2. Integrate into `server.js` with config flag
3. Add unit tests for recorder
4. Test with simple Terminal.Gui app

### Phase 2: Input Capture (Required for RFC Goal)
1. Add input event recording to `CastRecorder`
2. Hook into WebSocket message handler
3. Add integration tests
4. Manual testing with complex interactions

### Phase 3: Advanced Features (Optional)
1. Recording controls (start/stop/pause via WebSocket)
2. File size limits and rotation
3. Metadata injection (session info, user annotations)
4. Compression support

### Phase 4: Tooling (Future)
1. CLI tool to replay .cast files to PTY (automation)
2. Cast file editor/trimmer
3. Merge multiple recordings
4. Analytics (session duration, interaction patterns)

## Definition of Done

### Acceptance Criteria

#### Must Have ✅
- [ ] `CastRecorder` class implemented in `projects/nodejs/pty-service/recording.js`
- [ ] Records output events (`"o"`) with accurate timestamps
- [ ] Records input events (`"i"`) including keyboard and mouse
- [ ] Records resize events (`"r"`)
- [ ] Generates valid asciicast v2 format files
- [ ] Configuration option to enable/disable recording
- [ ] Recordings saved to `projects/nodejs/pty-service/recordings/` directory
- [ ] Unit tests with >80% coverage for `CastRecorder`
- [ ] Integration test that records a session and validates .cast format
- [ ] Manual test: recorded session plays correctly with `asciinema play`
- [ ] Manual test: recorded session displays correctly in asciinema-player (browser)
- [ ] Documentation: README.md in pty-service explaining how to use recording feature
- [ ] Proper cleanup on disconnection (close file, flush buffers)

#### Nice to Have 🎯
- [ ] Recording control via WebSocket messages (start/stop)
- [ ] File size limits to prevent disk exhaustion
- [ ] Configurable output directory
- [ ] Unique filename generation with timestamp
- [ ] Error handling for disk full, permission errors

#### Out of Scope ❌
- Automated replay system (future RFC)
- Recording editing tools
- Server-side playback API
- Recording encryption
- Multi-session recording (one recorder per connection is sufficient)

### Verification Steps

1. **Install dependencies** (if new packages needed):
   ```bash
   cd projects/nodejs/pty-service
   pnpm install
   ```

2. **Run unit tests**:
   ```bash
   pnpm test
   # All tests should pass
   # Coverage should be >80% for recording.js
   ```

3. **Run integration tests**:
   ```bash
   pnpm test recording-integration
   # Should create valid .cast file
   # Should verify asciicast v2 format
   ```

4. **Manual verification**:
   ```bash
   # Start PTY service with recording enabled
   cd projects/nodejs
   pm2 start ecosystem.config.js

   # Open browser, connect to PTY service, interact with Terminal.Gui
   # Check that .cast file is created in recordings/

   # Play back recording
   asciinema play projects/nodejs/pty-service/recordings/session-TIMESTAMP.cast

   # Verify:
   # - Visual output matches original session
   # - Timing feels natural
   # - All interactions are visible
   ```

5. **Format validation**:
   ```bash
   # Check first line is valid JSON header
   head -n1 recording.cast | jq .

   # Check events are valid JSON arrays
   tail -n+2 recording.cast | while read line; do echo "$line" | jq .; done
   ```

6. **Browser playback test**:
   - Embed recording in Astro site using asciinema-player
   - Verify it plays without errors
   - Test controls (play, pause, seek)

### PM2 Development Environment

**Before starting implementation:**
```bash
cd /Users/apprenticegc/Work/lunar-horse/personal-work/winged-bean/projects/nodejs
pm2 start ecosystem.config.js
pm2 logs
```

The `pty-service` will auto-reload on file changes.

**After implementation:**
```bash
# Verify services are running
pm2 status

# Check pty-service logs for recording messages
pm2 logs pty-service

# Stop services when done
pm2 stop all
pm2 delete all
```

## Dependencies

### Runtime Dependencies
- **node-pty** (already installed): PTY communication
- **ws** (already installed): WebSocket server
- **fs/promises**: File I/O for .cast files

### Development Dependencies
- **jest** (already installed): Testing framework
- **asciinema** (optional, for manual testing): `brew install asciinema`

### System Dependencies
- Node.js >=18.0.0 (already required)
- Disk space for recordings (configurable limit recommended)

## Risks and Mitigations

### Risk: Disk Space Exhaustion
- **Mitigation**: Implement file size limits, automatic cleanup of old recordings
- **Mitigation**: Make recording opt-in, not automatic

### Risk: Performance Impact
- **Mitigation**: Async file writes using buffered streams
- **Mitigation**: Monitor overhead in testing, disable if >5% latency added

### Risk: Sensitive Data in Recordings
- **Mitigation**: Document that recordings capture all input/output
- **Mitigation**: Add config option to exclude input events for privacy
- **Mitigation**: Don't commit recordings to git (add to .gitignore)

### Risk: File Format Changes
- **Mitigation**: Stick to asciicast v2 specification (stable since 2019)
- **Mitigation**: Version check on .cast file header

## Alternatives Considered

### 1. External Screen Recording Tool
**Approach**: Use `script` or `ttyrec` commands
- ❌ Doesn't integrate with WebSocket layer
- ❌ Can't capture browser-side events properly
- ❌ Less control over format and timing

### 2. Custom Binary Format
**Approach**: Design proprietary recording format
- ❌ Not compatible with existing asciinema ecosystem
- ❌ Requires custom playback tools
- ✅ Could be more efficient
- **Decision**: Standard format more valuable than marginal efficiency gains

### 3. Video Recording (Canvas/WebRTC)
**Approach**: Record xterm.js canvas as video
- ❌ Large file sizes
- ❌ Can't extract text from video
- ❌ Not suitable for automation/replay
- ✅ Easier for non-technical viewers
- **Decision**: Use asciinema for technical audience; can add video export later if needed

### 4. Proxy Layer Recording
**Approach**: Record at network layer (WebSocket proxy)
- ❌ More complex architecture
- ❌ Harder to correlate input/output
- ✅ Completely decoupled from PTY service
- **Decision**: Direct integration simpler and more reliable

## Future Enhancements

1. **Automated Replay** (separate RFC): Parse .cast files to replay sessions for testing
2. **Session Analytics**: Extract metrics (duration, key presses, error messages)
3. **Collaborative Features**: Share recordings via URL, embed in documentation
4. **Diff Tool**: Compare recordings to detect regressions in UI behavior
5. **GIF Export**: Convert .cast to animated GIF for social media/READMEs
6. **Streaming Recording**: Upload to cloud storage during session (not just on close)

## References

- [Asciicast v2 Specification](https://docs.asciinema.org/manual/asciicast/v2/)
- [asciinema-player](https://github.com/asciinema/asciinema-player)
- [node-pty Documentation](https://github.com/microsoft/node-pty)
- [ADR-0005: Use PTY for Terminal.Gui Web Integration](../adr/0005-use-pty-for-terminal-gui-web-integration.md)
- [ADR-0001: Use Astro with Asciinema Player](../adr/0001-use-astro-with-asciinema-player.md)

## Notes

- The `"i"` event type is part of the standard asciicast v2 format but is typically ignored by playback tools (they only render output)
- This means recordings will work in all standard players while preserving input data for automation use cases
- Recording adds minimal overhead since it's just appending JSON to a file
- Consider adding a web UI to browse/manage recordings in future iteration

---

**Author**: Claude (AI Assistant)
**Reviewer**: [Pending]
**Implementation**: Assigned to agent
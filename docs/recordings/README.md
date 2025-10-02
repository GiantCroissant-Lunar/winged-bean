# Terminal Recordings

**Purpose**: Terminal session recordings (asciinema format)

---

## Overview

This directory contains terminal session recordings in asciinema format (.cast files). These recordings capture Terminal.Gui sessions, PTY interactions, and development demonstrations for documentation and debugging purposes.

### Key Characteristics
- **Visual documentation** - Shows actual terminal behavior
- **Asciinema format** - Standard .cast file format for terminal recordings
- **Time-based** - Organized by session date/time
- **Auto-archive candidate** - Old recordings should be archived automatically

---

## Current Recordings

### Session Recordings
- `session-1-2025-10-01T14-56-27.cast` - Development session from Oct 1
- `terminal-gui-test-20251001-211942.cast` - Terminal.Gui test session

### PTY Output Captures
- `terminal-gui-pty-output.cast` - PTY session capture
- `terminal-gui-pty-output.txt` - Text output from PTY session

---

## File Types

### .cast Files (Asciinema)
**Format**: Standard asciinema v2 format

**Contains**:
- Terminal session replay data
- Timing information
- ANSI escape sequences
- Interactive session captures

**Playback**:
```bash
# Play recording locally
asciinema play terminal-gui-pty-output.cast

# Embed in documentation (see ADR-0001)
# Use asciinema-player in Astro docs site
```

---

### .txt Files (Text Output)
**Format**: Plain text capture

**Contains**:
- Terminal output as text
- Useful for searching or analysis
- Backup of .cast content

---

## Recording Sessions

### Dynamic Recording (RFC-0009)
**In PTY Sessions**:
- Press `F9` to start recording
- Press `F10` to stop recording
- Files saved automatically to `docs/recordings/`

**Features**:
- Multiple recordings per session
- Automatic timestamped filenames
- OSC sequence protocol for control

---

### Manual Recording
**Using asciinema CLI**:
```bash
# Start recording
asciinema rec docs/recordings/my-session-$(date +%Y%m%d-%H%M%S).cast

# ... perform actions ...

# Stop recording (Ctrl+D or exit)
```

---

## File Naming Convention

### Dynamic Recordings (RFC-0009)
```
session-[N]-YYYY-MM-DDTHH-MM-SS.cast
```

**Examples**:
- `session-1-2025-10-01T14-56-27.cast`
- `session-2-2025-10-01T15-30-45.cast`

### Manual Recordings
```
[descriptive-name]-YYYYMMDD-HHMMSS.cast
```

**Examples**:
- `terminal-gui-test-20251001-211942.cast`
- `plugin-loading-demo-20251002-093000.cast`
- `ecs-performance-test-20251002-140000.cast`

### PTY Output
```
[feature-name]-pty-output.cast
[feature-name]-pty-output.txt
```

---

## Archival Strategy

### Retention Policy
- **Active retention**: 30 days (or until feature documented)
- **Archive location**: `.archive/recordings/YYYY/MM/`
- **Automated cleanup**: Via tooling (Phase 2)

### When to Archive

Archive recordings when:
- **Age-based**: Recordings older than 30 days
- **Feature documented**: Recording no longer needed for active work
- **Superseded**: Newer recording covers same functionality
- **Demo complete**: One-time demonstration recordings

### What to Keep (Don't Archive)
- **Critical baselines**: Regression test baselines
- **Documentation references**: Recordings embedded in docs
- **Milestone recordings**: Important feature demonstrations

---

## Archival Commands

### Manual Archival (Current)

```bash
# Create archive directory structure
mkdir -p .archive/recordings/2025/10

# Move old recordings to archive
mv docs/recordings/session-*.cast .archive/recordings/2025/10/
mv docs/recordings/*-pty-output.* .archive/recordings/2025/10/
```

### Automated Archival (Planned - Phase 2)

Will be implemented as part of documentation tooling automation:
- Automatic detection of files older than 30 days
- Exclude recordings referenced in documentation
- Batch archival with directory structure creation
- Summary report of archived recordings

---

## Integration with Documentation

### Embedding in Docs (ADR-0001)

Recordings can be embedded in Astro documentation site using asciinema-player:

```astro
---
// In .astro file
import AsciinemaPlayer from '@asciinema/player';
---

<AsciinemaPlayer
  src="/recordings/terminal-gui-pty-output.cast"
  cols={120}
  rows={30}
  autoPlay={false}
/>
```

### Linking from RFCs/Guides

Reference recordings in documentation:
```markdown
**Demo**: [Terminal.Gui PTY Session](../recordings/terminal-gui-pty-output.cast)

See recording for live demonstration of the feature.
```

---

## Use Cases

### 1. Feature Demonstrations
Record new features for documentation and stakeholder demos

**Example**: RFC-0009 dynamic recording feature demo

---

### 2. Debugging & Issue Reproduction
Capture problematic behavior for debugging

**Example**: WebSocket connection issues captured via PTY recording

---

### 3. Progress Documentation (RFC-0008)
Record development milestones and progress

**Examples**:
- Baseline recording before major changes
- Post-implementation demonstration
- Performance comparison recordings

---

### 4. Testing & Verification
Visual verification of Terminal.Gui rendering

**Integration**: Works with Playwright E2E tests (RFC-0008)

---

## Technical Details

### Asciinema Format

Recordings use asciinema v2 format:
```json
{
  "version": 2,
  "width": 120,
  "height": 30,
  "timestamp": 1696176987,
  "env": {"SHELL": "/bin/bash", "TERM": "xterm-256color"}
}
[0.5, "o", "Output text"]
[1.0, "o", "More output"]
```

### File Size Considerations
- Text-based format (highly compressible)
- Average session: 100KB - 1MB
- Long sessions: Up to 10MB
- Compress old archives: `gzip .archive/recordings/2025/10/*.cast`

---

## Privacy & Security

### Safe to Record
- ✅ Terminal UI demonstrations
- ✅ Feature showcases
- ✅ Build and test output
- ✅ Debug sessions (after sanitization)

### DO NOT Record
- ❌ Sessions containing credentials or secrets (R-SEC-010)
- ❌ Personal or sensitive data (R-SEC-030)
- ❌ API keys or tokens
- ❌ Database credentials

**Always review recordings before committing to git!**

---

## Maintenance

### Regular Cleanup (Recommended)
- **Weekly**: Review new recordings for sensitive information
- **Monthly**: Archive recordings older than 30 days
- **Quarterly**: Review archive for files that can be deleted

### Storage Management
```bash
# Check total recording storage
du -sh docs/recordings/

# Check archive storage
du -sh .archive/recordings/

# Compress old archives
find .archive/recordings -name "*.cast" -exec gzip {} \;
```

---

## Related Documentation

- [RFC-0008: Playwright and Asciinema Testing Strategy](../rfcs/0008-playwright-and-asciinema-testing-strategy.md)
- [RFC-0009: Dynamic Asciinema Recording in PTY](../rfcs/0009-dynamic-asciinema-recording-in-pty.md)
- [ADR-0001: Use Astro with Asciinema Player](../adr/0001-use-astro-with-asciinema-player.md)
- [Chat History](../chat-history/) - Related conversation logs

---

**Last Updated**: 2025-10-02
**Total Recordings**: 5 files (4 .cast, 1 .txt)
**Retention Policy**: 30 days active, then archive
**Auto-Archive Status**: Planned (Phase 2 tooling)

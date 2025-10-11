# Audio Integration - Session 3 Handover

## Session Date
2025-10-11

## Current Status: Audio Infrastructure Complete, But No Sound in iTerm2

### ✅ What Was Accomplished

#### 1. Audio Plugin Integration Complete
- **Audio plugin enabled** in `plugins.json` (`enabled: true`)
- **Audio plugin loads successfully** in PM2 (confirmed in logs)
- **LibVLC installed**: VLC 3.0.21 with libraries at `/Applications/VLC.app/Contents/MacOS/lib/`
- **Sound file created**: `movement-step.wav` (13KB, 200Hz tone, 0.15s)

#### 2. Code Changes Applied
**File:** `development/dotnet/console/src/plugins/WingedBean.Plugins.ConsoleDungeon/Scene/TerminalGuiSceneProvider.cs`

- Added `using System.IO;`
- Updated `PlayMovementSound()` to construct full file path:
```csharp
var baseDir = AppContext.BaseDirectory;
var soundPath = Path.Combine(baseDir, "..", "..", "..", "..", "assets", "sounds", "movement-step.wav");
var fullPath = Path.GetFullPath(soundPath);
_audioService.Play(fullPath, new AudioPlayOptions { Volume = 0.3f, Loop = false });
```

#### 3. Fixed "v" Prefix Issue (Partially)
**Files Modified:**
- `development/dotnet/console/src/plugins/WingedBean.Plugins.AsciinemaRecorder/GitVersionHelper.cs` - Removed `v` prefix from lines 100, 104, 118, 122
- `development/dotnet/console/tests/e2e/ArtifactBasedE2ETests.cs` - Removed `v` prefix from path construction

**Status:** Still creates `v0.0.1-410` directory at runtime (contains `pty/recordings/` path). This appears to be from a different source than AsciinemaRecorder.

#### 4. Build and Deployment
- **Fresh clean build** completed successfully (0 errors)
- **All 12 plugins** copied to artifacts:
  1. WingedBean.Plugins.ArchECS
  2. WingedBean.Plugins.AsciinemaRecorder
  3. WingedBean.Plugins.Audio ✅
  4. WingedBean.Plugins.Config
  5. WingedBean.Plugins.ConsoleDungeon ✅
  6. WingedBean.Plugins.DungeonGame ✅
  7. WingedBean.Plugins.FigmaSharp.TerminalGui
  8. WingedBean.Plugins.Resilience
  9. WingedBean.Plugins.Resource
  10. WingedBean.Plugins.Resource.NuGet
  11. WingedBean.Plugins.TerminalUI
  12. WingedBean.Plugins.WebSocket

- **Sound file** copied to both:
  - `_artifacts/latest/dotnet/assets/sounds/movement-step.wav`
  - `_artifacts/0.0.1-410/dotnet/assets/sounds/movement-step.wav`

### ⚠️ Issues Identified

#### Issue 1: No Audio in iTerm2 Direct Run
**Problem:** When running the versioned artifact directly in iTerm2:
```bash
/Users/apprenticegc/Work/lunar-horse/personal-work/yokan-projects/winged-bean/build/_artifacts/0.0.1-410/dotnet/bin/ConsoleDungeon.Host
```

**Status:**
- Game loads correctly ✅
- All plugins load (9 plugins confirmed) ✅
- Movement works with arrow keys ✅
- Console log shows `🔊 Moved north/south/east/west` ✅
- **BUT: No sound is heard** ❌

**Possible Causes to Investigate:**
1. LibVLC may not be initializing properly (no initialization messages in logs)
2. Sound file path might be incorrect (relative path calculation may be wrong)
3. LibVLC might need additional configuration for macOS terminal audio
4. Audio output device routing issues
5. LibVLC might fail silently without proper error logging

**Debug Steps for Next Session:**
- Check if LibVLC initialization logs appear
- Verify the full resolved path to the WAV file is correct
- Check if LibVLC is actually attempting to play the file
- Look for LibVLC error messages that might be suppressed
- Test if LibVLC can play a simple sound file independently

#### Issue 2: Plugin Placement and Loading Strategy
**Problem:** Plugins were not automatically copied to versioned artifacts during `task build-all`.

**What Happened:**
1. Initial `task build-all` only copied 4 plugins to `_artifacts/0.0.1-410/dotnet/bin/plugins/`
2. Had to manually run `task console:build` to build plugins properly
3. Had to manually copy plugins from `latest/` to `0.0.1-410/`

**Root Cause:** The build task structure doesn't ensure all plugins are built and copied to versioned artifacts.

**Current Workaround:**
```bash
# Build console to get all plugins
task console:build

# Manually copy plugins to versioned artifacts
cp -r development/dotnet/console/src/host/ConsoleDungeon.Host/bin/Debug/net8.0/plugins/* build/_artifacts/0.0.1-410/dotnet/bin/plugins/
```

**Needs Investigation:**
- Why does `task build-all` not include console plugin building?
- Why are only 4 plugins copied initially?
- Is there a post-build task that should copy plugins to artifacts?
- Should the build system automatically sync `latest/` to versioned artifacts?

## Files Modified This Session

### Core Code Changes
1. `development/dotnet/console/src/plugins/WingedBean.Plugins.ConsoleDungeon/Scene/TerminalGuiSceneProvider.cs`
   - Added `using System.IO;`
   - Updated `PlayMovementSound()` with full file path construction

### Configuration Changes
2. `development/dotnet/console/src/host/ConsoleDungeon.Host/plugins.json`
   - Changed Audio plugin: `"enabled": false` → `"enabled": true`

### Bug Fixes
3. `development/dotnet/console/src/plugins/WingedBean.Plugins.AsciinemaRecorder/GitVersionHelper.cs`
   - Removed `v` prefix from `GetRecordingsDirectory()` and `GetLogsDirectory()`

4. `development/dotnet/console/tests/e2e/ArtifactBasedE2ETests.cs`
   - Removed `v` prefix from path construction in tests

### Assets Created
5. `development/dotnet/console/assets/sounds/movement-step.wav`
   - 13KB WAV file (200Hz, 0.15s, with fade in/out)

## How to Run and Test

### Method 1: Direct Run in iTerm2 (Current Test Method)
```bash
cd /Users/apprenticegc/Work/lunar-horse/personal-work/yokan-projects/winged-bean
./build/_artifacts/0.0.1-410/dotnet/bin/ConsoleDungeon.Host
```

**Expected:** Game loads, plugins work, movement shows 🔊 icons
**Issue:** No audio heard

### Method 2: Via PM2 + Web Browser (Working)
```bash
# Services already running via PM2
pm2 list

# Open browser
open http://localhost:4321/demo/
```

**Status:** Game works in web browser, but audio status unknown (not tested in browser)

## Current PM2 Status

All services running:
```
┌────┬────────────────────┬──────────┬──────┬───────────┬──────────┬──────────┐
│ id │ name               │ mode     │ ↺    │ status    │ cpu      │ memory   │
├────┼────────────────────┼──────────┼──────┼───────────┼──────────┼──────────┤
│ 2  │ console-dungeon    │ fork     │ 2    │ online    │ 0%       │ 55.2mb   │
│ 1  │ docs-site          │ fork     │ 0    │ online    │ 0%       │ 42.3mb   │
│ 0  │ pty-service        │ fork     │ 0    │ online    │ 0%       │ 57.7mb   │
└────┴────────────────────┴──────────┴──────┴───────────┴──────────┴──────────┘
```

Plugins loaded in PM2 console-dungeon:
- 9 plugins total
- Includes Audio, ConsoleDungeon, DungeonGame ✅

## Next Session Priorities

### Priority 1: Debug Audio in iTerm2
**Goal:** Understand why LibVLC isn't playing sound when running directly in terminal

**Tasks:**
1. Add detailed logging to `LibVlcAudioService` initialization
2. Add logging to track the resolved sound file path
3. Check if LibVLC `Core.Initialize()` is being called
4. Verify LibVLC library loading on macOS
5. Test if LibVLC can play sound through terminal audio
6. Check macOS audio permissions/routing
7. Consider testing with a simpler audio library (NAudio, System.Media.SoundPlayer)

**Debug Code to Add:**
```csharp
// In LibVlcAudioService.InitializeLibVlc()
_logger?.LogInformation("Attempting to initialize LibVLC...");
_logger?.LogInformation("LibVLC library path: {Path}", /* get actual path */);

// In PlayMovementSound()
_logger?.LogInformation("Attempting to play sound at: {FullPath}", fullPath);
_logger?.LogInformation("File exists: {Exists}", File.Exists(fullPath));
```

### Priority 2: Fix Plugin Build/Copy Workflow
**Goal:** Ensure plugins are automatically copied to versioned artifacts

**Tasks:**
1. Review `build/Taskfile.yml` - understand the `build-all` task structure
2. Check if `console:build` should be included in `build-all`
3. Verify the MSBuild target that copies plugins
4. Ensure post-build copies plugins to versioned artifacts automatically
5. Document the correct build workflow

**Questions to Answer:**
- Should `task build-all` include console plugin building?
- Should there be a `task copy-artifacts` step?
- How should versioned artifacts (`0.0.1-410/`) stay in sync with `latest/`?

### Priority 3: Investigate Remaining "v" Prefix
**Goal:** Find and fix the source creating `v0.0.1-410/pty/recordings/`

**Tasks:**
1. Search for other places that might add "v" prefix
2. Check PTY service code for version path handling
3. Check recording manager in PTY service
4. Ensure all artifact paths are consistent without "v" prefix

## Verification Commands

### Check Plugin Status
```bash
# List plugins in versioned artifacts
ls build/_artifacts/0.0.1-410/dotnet/bin/plugins/

# Check for all required plugins
ls build/_artifacts/0.0.1-410/dotnet/bin/plugins/ | grep -E "Audio|ConsoleDungeon|DungeonGame"
```

### Check Sound File
```bash
# Verify sound file exists
ls -lh build/_artifacts/0.0.1-410/dotnet/assets/sounds/movement-step.wav

# Play sound file directly (test LibVLC installation)
afplay build/_artifacts/0.0.1-410/dotnet/assets/sounds/movement-step.wav
```

### Check LibVLC
```bash
# Verify LibVLC libraries
ls -la /Applications/VLC.app/Contents/MacOS/lib/libvlc*.dylib
```

### Check for "v" Directories
```bash
# Check if v-prefixed directories are created
ls -d build/_artifacts/v* 2>/dev/null || echo "No v-prefixed directories"
```

### Run and Check Logs
```bash
# Run directly
./build/_artifacts/0.0.1-410/dotnet/bin/ConsoleDungeon.Host

# In another terminal, check PM2 logs
pm2 logs console-dungeon | grep -i "audio\|libvlc\|sound"
```

## Architecture Notes

### Audio Service Flow
1. **Initialization**: `LibVlcAudioService` constructor calls `InitializeLibVlc()`
2. **LibVLC Setup**: `Core.Initialize()` and `new LibVLC("--quiet")`
3. **Play Request**: `PlayMovementSound()` → `_audioService.Play(fullPath, options)`
4. **LibVLC Execution**: Creates `Media` and `MediaPlayer`, calls `Play()`

### File Path Construction
```csharp
// From ConsoleDungeon.Host binary location:
AppContext.BaseDirectory = ".../0.0.1-410/dotnet/bin/"

// Navigate up to assets:
Path.Combine(baseDir, "..", "..", "..", "..", "assets", "sounds", "movement-step.wav")

// Resolves to:
".../0.0.1-410/dotnet/bin/../../../../assets/sounds/movement-step.wav"
// = ".../0.0.1-410/assets/sounds/movement-step.wav" (WRONG - should be dotnet/assets)
```

**⚠️ POTENTIAL BUG FOUND:** The path might be resolving incorrectly!
- Sound file is at: `0.0.1-410/dotnet/assets/sounds/movement-step.wav`
- Code might be looking at: `0.0.1-410/assets/sounds/movement-step.wav`

**Fix Needed:**
```csharp
// Should be only 3 levels up, not 4:
var soundPath = Path.Combine(baseDir, "..", "..", "..", "assets", "sounds", "movement-step.wav");
```

## Tools Installed This Session

1. **VLC (LibVLC)**: `brew install --cask vlc`
   - Version: 3.0.21
   - Libraries: `/Applications/VLC.app/Contents/MacOS/lib/`

2. **ffmpeg**: `brew install ffmpeg`
   - Used to generate `movement-step.wav`
   - Can be used for audio testing/debugging

## References

### Previous Session Documents
- `AUDIO-INTEGRATION-HANDOVER.md` - Initial audio setup (Session 1)
- `AUDIO-SETUP-SUMMARY.md` - Summary of audio setup steps
- `V-PREFIX-FIX-SUMMARY.md` - Documentation of v-prefix fix
- `AUDIO-TEST-READY.md` - Status before testing

### Key Code Files
- `development/dotnet/console/src/plugins/WingedBean.Plugins.ConsoleDungeon/Scene/TerminalGuiSceneProvider.cs` - Movement sound triggering
- `development/dotnet/console/src/plugins/WingedBean.Plugins.Audio/LibVlcAudioService.cs` - Audio service implementation
- `development/dotnet/console/src/host/ConsoleDungeon.Host/plugins.json` - Plugin configuration

### Build System
- `build/Taskfile.yml` - Main build orchestration
- `development/dotnet/console/Taskfile.yml` - Console-specific build tasks

---

## Quick Start for Next Session

```bash
# 1. Check current status
pm2 list
ls build/_artifacts/0.0.1-410/dotnet/bin/plugins/ | wc -l  # Should be 12

# 2. Test sound file directly
afplay build/_artifacts/0.0.1-410/dotnet/assets/sounds/movement-step.wav

# 3. Run the game
./build/_artifacts/0.0.1-410/dotnet/bin/ConsoleDungeon.Host

# 4. Move with arrow keys and check for:
#    - Visual: 🔊 icons in console log
#    - Audio: Beep sound (currently missing)

# 5. Check logs for LibVLC messages
# (Look for initialization, errors, or warnings)
```

---

**Session End Status:** Infrastructure complete, all plugins loading, but audio not playing in iTerm2. Path calculation bug suspected. Ready for debugging session.

**Next Session Focus:** Fix sound file path calculation and debug LibVLC initialization/playback.

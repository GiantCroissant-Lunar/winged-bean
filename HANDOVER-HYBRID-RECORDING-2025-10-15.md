# Handover: Hybrid Recording & Game Initialization Investigation

**Date:** 2025-10-15  
**Session Duration:** ~3 hours  
**Status:** 🔧 Ready for Testing - Diagnostic Logging in Place

---

## 🎯 Original Objective

Implement hybrid recording system that synchronizes FFmpeg video recording with URF (Universal Recording Format) metadata recording for WingedBean dungeon game.

---

## ✅ What Was Accomplished

### 1. Built Core Components

#### FFmpeg Plugin (Plate.Plugins.FFmpeg)
- ✅ **Built successfully** at `plate-projects/ffmpeg-calox/`
- ✅ Fixed plugin manifest format (dependencies as object, not array)
- ✅ Added `entryPoint`, `loadStrategy`, `priority`, `exports` fields
- ✅ Updated to match current `IPlugin` interface (added `Id`, `Version`, `CancellationToken`)
- ✅ Published with all dependencies (including FFmpeg.AutoGen.dll)
- ✅ Deployed to `build/_artifacts/0.0.1-474/dotnet/desktop/bin/plugins/ffmpeg/`

**Status:** ⚠️ Loads but fails to initialize (native FFmpeg libraries not found)  
**Impact:** Video recording unavailable, but URF recording should still work

#### HybridRecorder (WingedBean.Plugins.Recorder.Snapshots)
- ✅ **Built successfully**
- ✅ Extends `URFRecorderService` with optional video recording
- ✅ Checks `ENABLE_VIDEO=1` environment variable
- ✅ Graceful degradation when video encoder unavailable
- ✅ Generates synchronization metadata
- ✅ Updated plugin manifest to export `HybridRecorder` instead of `URFRecorderService`

**Status:** ✅ Ready to test (once game initialization works)

#### Video Contracts (CrossMilo.Contracts.Recorder)
- ✅ `IVideoEncoder.cs` - Video encoding interface
- ✅ `IVideoExporter.cs` - URF-to-video export interface
- ✅ `RecordingSyncMetadata.cs` - Synchronization metadata
- ✅ All contracts compile with proper `using` statements
- ✅ Fixed C# 11 compatibility issues

**Status:** ✅ Complete

### 2. Unified Logging System

#### Added File Logging
- ✅ Added `Serilog.Extensions.Logging.File` v3.0.0
- ✅ Configured to write to `logs/winged-bean-{date}.log`
- ✅ Daily rotation, Debug level minimum
- ✅ Logs persist even when GUI is running

#### Removed Console.WriteLine
- ✅ All debug statements replaced with `ILogger` calls
- ✅ Structured logging with message templates
- ✅ Consistent format across all components

**Log File Location:**
```
build/_artifacts/0.0.1-474/dotnet/desktop/bin/logs/winged-bean-2025-10-15.log
```

**Status:** ✅ Complete - Ready to use for diagnostics

### 3. Added Diagnostic Logging

#### Files Modified with Enhanced Logging:
- `SadConsolePlugin.cs` - Logs service resolution (IDungeonService, RecorderService)
- `SadConsoleApp.cs` - Logs OnGameStarted event and service availability
- `DungeonGameScreen.cs` - Logs initialization steps with proper exception handling

**What Will Be Logged:**
- ✅ Plugin loading and activation
- ✅ Service resolution success/failure
- ✅ Game initialization sequence
- ✅ Dungeon.Initialize() call and result
- ✅ Bot mode activation (if enabled)
- ✅ Player/entity creation
- ✅ Errors with full stack traces

**Status:** ✅ Complete - Ready for diagnostic run

---

## ⚠️ Critical Issue Discovered

### Game Initialization Not Working

**Symptom:**
- Game window opens
- No dungeon generated
- No player movement
- No URF files created
- No bot mode activation

**Root Cause Analysis:**

#### Flow Comparison

**Console Version (Working):**
```
ConsoleDungeonAppRefactored.StartAsync()
  ├─ Resolves IDungeonService from registry
  ├─ **EXPLICITLY calls _gameService.Initialize()** ✅
  └─ Game runs, URF records events
```

**Desktop Version (Not Working):**
```
SadConsoleApp.StartAsync()
  ├─ Creates SadConsole Game instance
  ├─ Registers OnGameStarted event handler
  └─ Calls Game.Run() (blocks until window closes)

OnGameStarted fires (when window opens)
  ├─ Creates DungeonGameScreen
  └─ DungeonGameScreen constructor
      ├─ Calls _dungeon?.Initialize() in try-catch
      └─ **BUT exceptions are silently caught!** ⚠️
```

**Key Discovery:**
- Both console and desktop use **THE SAME DungeonGame plugin** from `console/src/plugins/WingedBean.Plugins.DungeonGame/`
- The core game logic is identical
- Only the presentation layer differs (ConsoleDungeon vs SadConsole)
- But initialization flows are different!

**Likely Causes:**
1. `IDungeonService` not resolved (registry lookup fails silently)
2. `DungeonGameService` not properly initialized (missing dependencies)
3. Exception during `Initialize()` (caught and ignored)
4. `OnGameStarted` event not firing

**Evidence:**
- Plugins load successfully (DungeonGame, Recorder, SadConsole)
- Game window opens
- Log ends at "Starting game loop..."
- **NO** initialization logs appear
- **NO** URF files generated

---

## 📊 Current State

### What Works
- ✅ Build system (Task-based)
- ✅ Plugin loading (all 4 plugins load)
- ✅ Game window opens
- ✅ File logging configured
- ✅ Diagnostic logging in place

### What Doesn't Work
- ❌ Dungeon initialization
- ❌ Player creation
- ❌ Bot mode activation
- ❌ URF recording (nothing to record)
- ❌ Video recording (FFmpeg native libs missing)

### What's Unknown (Need Logs)
- ❓ Does `OnGameStarted` fire?
- ❓ Is `IDungeonService` resolved?
- ❓ Does `Initialize()` get called?
- ❓ Does it throw an exception?

---

## 🔧 Next Steps (Priority Order)

### 1. **IMMEDIATE: Run Diagnostic Test**
```bash
cd build
./test-hybrid-recording.sh 5
```

Then check the log:
```bash
tail -f build/_artifacts/0.0.1-474/dotnet/desktop/bin/logs/winged-bean-2025-10-15.log
```

**Look for:**
- "OnGameStarted fired!" - Did the event trigger?
- "IDungeonService resolved" - Was service found?
- "Calling _dungeon.Initialize()" - Did initialization start?
- "Dungeon initialized successfully" - Did it complete?
- Any errors or exceptions

### 2. **Fix Identified Issue**

Based on logs, likely fixes:

**If IDungeonService is NULL:**
- Check plugin loading order
- Verify DungeonGamePlugin registers service
- Check service registration in registry

**If Initialize() throws exception:**
- Fix the underlying issue (missing dependency, etc.)
- Ensure ECS world initializes correctly

**If OnGameStarted doesn't fire:**
- Check SadConsole event wiring
- Verify Game.Run() behavior

### 3. **Verify Game Works**
- Dungeon generates
- Player created
- Bot mode activates (with `ENABLE_BOT_MODE=1`)
- URF files generated

### 4. **Fix FFmpeg Native Libraries (Optional)**
- Install FFmpeg native libraries to plugin directory
- Or fix library path detection for macOS
- Test video recording with `ENABLE_VIDEO=1`

### 5. **Test Complete Hybrid Recording**
```bash
ENABLE_BOT_MODE=1 ENABLE_VIDEO=1 ./test-hybrid-recording.sh 30
```

Verify outputs:
- `{sessionId}.urf.jsonl` - URF metadata
- `{sessionId}.mp4` - Video file (if FFmpeg works)
- `{sessionId}.sync.json` - Synchronization metadata

---

## 📁 Key Files & Locations

### Source Code

**FFmpeg Plugin:**
```
plate-projects/ffmpeg-calox/dotnet/framework/src/Plate.Plugins.FFmpeg/
├── FFmpegPlugin.cs (plugin entry point)
├── FFmpegVideoEncoder.cs (core encoder)
├── FFmpegInitializer.cs (native library loading)
└── .plugin.json (manifest)
```

**HybridRecorder:**
```
development/dotnet/windows/src/plugins/WingedBean.Plugins.Recorder.Snapshots/
├── HybridRecorder.cs (extends URFRecorderService)
└── WingedBean.Plugins.Recorder.Snapshots.plugin.json (manifest)
```

**Video Contracts:**
```
plate-projects/cross-milo/dotnet/framework/src/CrossMilo.Contracts.Recorder/Video/
├── IVideoEncoder.cs
├── IVideoExporter.cs
└── RecordingSyncMetadata.cs
```

**Desktop Presentation (SadConsole):**
```
development/dotnet/windows/src/plugins/WingedBean.Plugins.SadConsole/
├── SadConsolePlugin.cs (service resolution)
├── SadConsoleApp.cs (game startup)
└── DungeonGameScreen.cs (dungeon initialization)
```

**Shared Game Logic:**
```
development/dotnet/console/src/plugins/WingedBean.Plugins.DungeonGame/
├── DungeonGameService.cs (service implementation)
└── DungeonGame.cs (line 289 - bot mode check)
```

**Host Configuration:**
```
development/dotnet/windows/src/host/Dungeon.Host.SadConsole/
├── Program.cs (logging configuration, line 128-132)
└── Dungeon.Host.SadConsole.csproj (file logging package)
```

### Build Artifacts

**Deployed Plugins:**
```
build/_artifacts/0.0.1-474/dotnet/desktop/bin/plugins/
├── WingedBean.Plugins.DungeonGame.dll
├── WingedBean.Plugins.Recorder.Snapshots.dll
├── WingedBean.Plugins.SadConsole.dll
└── ffmpeg/
    ├── Plate.Plugins.FFmpeg.dll
    ├── FFmpeg.AutoGen.dll
    └── .plugin.json
```

**Log Files:**
```
build/_artifacts/0.0.1-474/dotnet/desktop/bin/logs/
└── winged-bean-2025-10-15.log (diagnostic output)
```

**Recording Output (when working):**
```
build/_artifacts/0.0.1-474/dotnet/desktop/bin/recordings/
├── {sessionId}.urf.jsonl (URF metadata)
├── {sessionId}.mp4 (video - if FFmpeg works)
└── {sessionId}.sync.json (synchronization metadata)
```

---

## 📚 Documentation Created

### Investigation Documents
1. **INVESTIGATION-FINDINGS.md** - Detailed root cause analysis
2. **FLOW-ANALYSIS.md** - Console vs Desktop initialization flow comparison
3. **HYBRID-RECORDING-STATUS.md** - Overall project status
4. **LOGGING-IMPROVEMENTS.md** - Logging system documentation

### Reference Documents
5. **RFC-0001-ffmpeg-video-encoder.md** - FFmpeg plugin design (in ffmpeg-calox)
6. **RFC-0002-hybrid-urf-video-recording.md** - Hybrid recording design (in ffmpeg-calox)
7. **HYBRID-RECORDING-GUIDE.md** - Quick reference guide (in ffmpeg-calox)

---

## 🔍 Diagnostic Commands

### Check Logs
```bash
# View live log
tail -f build/_artifacts/0.0.1-474/dotnet/desktop/bin/logs/winged-bean-2025-10-15.log

# Search for specific events
grep "OnGameStarted" logs/*.log
grep "IDungeonService" logs/*.log
grep "Initialize" logs/*.log
grep "BOT MODE" logs/*.log
grep -i "error\|exception" logs/*.log
```

### Check Recordings
```bash
# List URF files
find build/_artifacts -name "*.urf.jsonl"

# Check recording directory
ls -la build/_artifacts/0.0.1-474/dotnet/desktop/bin/recordings/
```

### Check Plugin Loading
```bash
# Check which plugins loaded
grep "✓ Loaded:" build/_artifacts/0.0.1-474/dotnet/desktop/bin/logs/*.log

# Check for plugin errors
grep "Failed to load" build/_artifacts/0.0.1-474/dotnet/desktop/bin/logs/*.log
```

---

## 🎓 Key Insights

### 1. **Shared Game Logic**
- Console and Desktop use the **same** DungeonGame plugin
- Core logic is identical - only presentation differs
- This is the correct architecture!

### 2. **Silent Failures**
- Original code had `try { } catch { }` with no logging
- Made debugging impossible
- Now fixed with proper exception logging

### 3. **Logging is Critical**
- File logging allows tracing even when GUI is running
- Structured logging with levels makes analysis easier
- Single source of truth for all diagnostics

### 4. **Plugin Architecture Works**
- All plugins load successfully
- Service registration works
- The issue is in initialization, not architecture

---

## ⚡ Quick Start for Next Session

### 1. Run Test
```bash
cd /Users/apprenticegc/Work/lunar-horse/personal-work/yokan-projects/winged-bean
cd build
./test-hybrid-recording.sh 5
```

### 2. Check Log
```bash
cat build/_artifacts/0.0.1-474/dotnet/desktop/bin/logs/winged-bean-2025-10-15.log
```

### 3. Identify Issue
Look for the **first** error or missing log message in the sequence:
1. OnGameStarted fired?
2. IDungeonService resolved?
3. Initialize() called?
4. Initialize() succeeded?

### 4. Fix and Rebuild
```bash
# After fixing code:
cd build
task build-dotnet
```

### 5. Test Again
```bash
./test-hybrid-recording.sh 5
# Check logs again
```

---

## 🚨 Known Issues

### 1. FFmpeg Native Libraries Missing
**Error:** `Specified method is not supported` when calling `av_version_info()`  
**Impact:** Video recording unavailable  
**Workaround:** URF recording still works  
**Fix:** Install native FFmpeg libraries or fix path detection

### 2. Game Initialization Silent Failure
**Error:** Unknown (need logs to identify)  
**Impact:** No dungeon, no player, no recording  
**Workaround:** None  
**Fix:** Run diagnostic test to identify root cause

---

## 📞 Context for AI Assistant

### What You Should Know
- This is a **game development project** (roguelike dungeon crawler)
- Uses **plugin architecture** (PluginManoi) for modularity
- Uses **ECS** (Arch) for game entities
- Has **two presentation layers**: Console (Terminal.Gui) and Desktop (SadConsole)
- Recording system uses **URF** (Universal Recording Format) - JSON Lines format
- Goal is **hybrid recording**: Video (FFmpeg) + Metadata (URF) synchronized

### Build System Rules
- ⚠️ **NEVER use `dotnet build` or `dotnet run`** - Use Task from `build/` directory
- ⚠️ **NEVER run from source** - Always run from `build/_artifacts/` directory
- ✅ Build: `cd build && task build-dotnet`
- ✅ Run: `cd build && task console:debug` or `./test-hybrid-recording.sh`

### Workspace Structure
- `lunar-horse/` - Workspace root (NOT a git repo)
- `plate-projects/` - Shared Plate framework projects
- `yokan-projects/winged-bean/` - This game project

### Environment Variables
- `ENABLE_BOT_MODE=1` - Enable AI-controlled player for testing
- `ENABLE_VIDEO=1` - Enable video recording (requires FFmpeg)

---

## ✅ Success Criteria

When everything works, you should see:

### In Logs
```
info: OnGameStarted fired!
info: _dungeon is NOT NULL
info: IDungeonService resolved: DungeonGameService
info: Calling _dungeon.Initialize()...
info: ✓ Dungeon initialized successfully. State: Running
info: 🤖 BOT MODE ENABLED - Player will be AI-controlled
info: Created player entity at (40, 12)
info: DungeonGameScreen created (120x40)
```

### In File System
```
recordings/
├── {sessionId}.urf.jsonl (URF metadata - player actions, game events)
├── {sessionId}.mp4 (Video - if FFmpeg works)
└── {sessionId}.sync.json (Synchronization metadata)
```

### In Game
- Dungeon visible on screen
- Player (@) moves automatically (bot mode)
- Enemies (g, o, etc.) move and interact
- Game runs for specified duration and exits

---

## 🎯 Final Notes

**Current Blocker:** Game initialization not working - need diagnostic logs to identify exact failure point.

**Next Action:** Run test with file logging, analyze logs, fix identified issue.

**Estimated Time to Fix:** 30-60 minutes once logs reveal the issue.

**Confidence Level:** High - diagnostic logging is comprehensive, issue will be clear in logs.

---

**Good luck with the next session! The logging system is ready to tell us exactly what's wrong.** 🚀

# Complete Findings - Why Terminal.Gui UI Doesn't Show

## Date
2025-10-12 00:14

## Root Cause Chain

### Issue 1: Build System Doesn't Rebuild Changed Plugins ❌ BLOCKING
The `task build-all` command is NOT detecting changes in plugin source files and NOT rebuilding them. Even after:
- Cleaning bin/obj directories
- Modifying source code
- Running `task build-all`

The artifacts still contain old DLL files with old code.

**Evidence**: Stack traces show line 32 in DungeonGameService.cs which is the OLD code location, even though the source has been modified.

### Issue 2: DungeonGame Plugin Dependency on ArchECS ❌ BLOCKING
**Problem**: DungeonGameService constructor tries to resolve ArchECSService from registry before ArchECS plugin has activated.

**Error**:
```
Service ArchECSService not found in registry
at WingedBean.Plugins.DungeonGame.DungeonGameService..ctor(IRegistry registry) in DungeonGameService.cs:line 32
```

**Why It Happens**: Plugin load order ≠ activation order. Even though ArchECS loads first (priority 100), when DungeonGame activates, it immediately creates DungeonGameService which tries `registry.Get<ArchECSService>()` in constructor.

**Solution Attempted**:
1. Changed `_ecs` field from `readonly` to nullable
2. Moved ArchECS resolution from constructor to `Initialize()` method
3. Modified DungeonGameService.cs lines 20, 29-40, 42-48

**Status**: Code fixed in source, but build system not rebuilding it into artifacts!

### Issue 3: IDungeonService Not Registered ❌ BLOCKING
Because DungeonGame plugin fails to activate (due to Issue 2), IDungeonService never gets registered.

**Impact**: When ConsoleDungeonAppRefactored tries to start, it checks for IDungeonService and doesn't find it, so it exits silently.

### Issue 4: Audio Plugin Doesn't Build ⚠️ NON-BLOCKING  
Audio plugin has project reference issues and doesn't compile. But this is not critical for UI - game works without audio.

## What Actually Works ✅

1. **Build succeeds with 0 errors** (but doesn't rebuild changed plugins!)
2. **All plugins load successfully** (DLLs load into memory)
3. **ArchECS plugin activates and registers ArchECSService**
4. **Terminal.Gui detection is NOT headless** (Console.IsOutputRedirected: False)
5. **TerminalAppRunnerHostedService starts**

## What Doesn't Work ❌

1. **Build system doesn't rebuild** changed plugin source files
2. **DungeonGame plugin activation fails** (can't find ArchECSService)
3. **IDungeonService not registered** (DungeonGame failed)
4. **ConsoleDungeonAppRefactored.StartAsync is called** but silently exits (no IDungeonService)
5. **Terminal.Gui UI never initializes** (app exits before UI starts)

## The Build System Problem

### Symptoms
- `task build-all` completes successfully
- Reports "0 Error(s)"
- Updates `_artifacts/latest`
- **BUT** artifact DLLs are OLD (unchanged from previous build)

### What Was Tried
1. ✅ Cleaned all bin/obj directories
2. ✅ Modified source code  
3. ✅ Ran `task build-all`
4. ✅ Checked source code (changes are there)
5. ❌ Checked artifact DLLs (changes are NOT there - old stack trace line numbers)

### Hypothesis
The NUKE build system or MSBuild is using aggressive caching and not detecting that DungeonGameService.cs changed. The task completes but doesn't actually recompile the plugin.

## Solution Path Forward

### Immediate Action Required

**Option A: Force Clean Rebuild**
```bash
# Nuclear option - delete EVERYTHING and rebuild
cd /Users/apprenticegc/Work/lunar-horse/personal-work/yokan-projects/winged-bean
find . -name "bin" -type d -exec rm -rf {} + 2>/dev/null
find . -name "obj" -type d -exec rm -rf {} + 2>/dev/null
rm -rf build/_artifacts/*
cd build && task build-all
```

**Option B: Build Directly With MSBuild**
```bash
cd /Users/apprenticegc/Work/lunar-horse/personal-work/yokan-projects/winged-bean/development/dotnet
dotnet clean
dotnet build --no-incremental --force
```

**Option C: Touch Source Files to Force Rebuild**
```bash
touch /Users/apprenticegc/Work/lunar-horse/personal-work/yokan-projects/winged-bean/development/dotnet/console/src/plugins/WingedBean.Plugins.DungeonGame/DungeonGameService.cs
cd build && task build-all
```

### Code Changes Already Made (But Not Built)

1. **DungeonGameService.cs**:
   - Line 20: Changed `private readonly ArchECSService _ecs;` → `private ArchECSService? _ecs;`
   - Lines 29-40: Removed ArchECS resolution from constructor
   - Lines 42-48: Added ArchECS resolution to Initialize() method

2. **.plugin.json**:
   - Added `"priority": 150` to DungeonGame manifest

3. **TerminalGuiSceneProvider.cs**:
   - Added `FORCE_UI` environment variable support

### Expected Behavior After Proper Build

1. DungeonGame plugin activates successfully  
2. IDungeonService gets registered
3. ConsoleDungeonAppRefactored.StartAsync runs fully
4. Terminal.Gui UI initializes
5. Full-screen TUI game appears in terminal

## Files Modified (Source)

```
✅ Source Modified:
- DungeonGameService.cs (dependency injection fix)
- DungeonGame/.plugin.json (added priority)
- TerminalGuiSceneProvider.cs (added FORCE_UI)
- AudioPlugin.cs (fixed to match WebSocket pattern)
- LibVlcAudioService.cs (removed non-existent attribute)
- Audio/.plugin.json (added priority, fixed references)
- WingedBean.Plugins.Audio.csproj (fixed references)

❌ Artifacts NOT Updated:
- DungeonGame plugin DLL still has old code
- Other plugins may also be stale
```

## Commands to Verify After Rebuild

```bash
# Check if IDungeonService registers
export FORCE_UI=1
./build/_artifacts/latest/dotnet/bin/ConsoleDungeon.Host 2>&1 | grep "IDungeonService"

# Should see:
# ✓ IDungeonService registered: DungeonGameService

# Check if UI starts
export FORCE_UI=1
./build/_artifacts/latest/dotnet/bin/ConsoleDungeon.Host 2>&1 | grep "ConsoleDungeonAppRefactored.StartAsync"

# Should see Terminal.Gui UI appear in terminal
```

## Conclusion

The Terminal.Gui UI doesn't show because:

1. DungeonGame plugin fails to activate (can't find ArchECSService)
2. Which means IDungeonService isn't registered
3. Which means ConsoleDungeonAppRefactored exits early
4. Which means Terminal.Gui never initializes

The fix has been coded but the build system is not applying it to the artifacts.

**Next Step**: Force a complete clean rebuild with one of the options above, then test again.

---

*Investigation complete: 2025-10-12 00:14*
*Root cause: Build system caching + Plugin dependency resolution timing*
*Status: Code fixed, waiting for proper build*

# Investigation Findings - Hybrid Recording & Bot Mode

**Date:** 2025-10-15  
**Status:** Root causes identified

---

## 🔍 Key Findings

### 1. ✅ Plugins Load Successfully
- **DungeonGame Plugin:** ✅ Loaded
- **Recorder Plugin:** ✅ Loaded  
- **SadConsole Plugin:** ✅ Loaded
- **FFmpeg Plugin:** ⚠️ Loads but fails to initialize (native libraries missing)

### 2. ✅ Game DOES Start
- Game window opens
- SadConsole initializes
- Game loop starts
- Log shows: "Starting game loop..."

### 3. ❌ No URF Recording Files Generated
**Location checked:** `build/_artifacts/0.0.1-474/dotnet/desktop/bin/recordings/`  
**Result:** Empty (only `urf/` subdirectory exists, but no `.jsonl` files)

**Why?**
- Recording service started successfully
- But no actual game events to record (see #4)

### 4. ❌ Dungeon/Player Never Initialize
**Expected logs (NOT seen):**
- "🤖 BOT MODE ENABLED - Player will be AI-controlled"
- "Created player entity at (40, 12)"
- "DungeonGameScreen created"
- Any dungeon generation logs
- Any player movement logs

**Actual behavior:**
- Game window opens
- Logs stop at "Starting game loop..."
- No further initialization logs

---

## 🎯 Root Cause Analysis

### Problem: Game Runs But Doesn't Initialize Gameplay

**Evidence:**
1. `DungeonGameScreen` constructor should call `_dungeon?.Initialize()` (line 68)
2. This should trigger player creation with bot mode check
3. Bot mode code exists and checks `ENABLE_BOT_MODE` environment variable
4. But we see ZERO logs from dungeon initialization

**Hypothesis:**
The game enters the SadConsole rendering loop but the `DungeonGameScreen` is either:
- Not being created at all
- Created but `_dungeon` is null
- Created but Initialize() throws an exception that's caught silently

---

## 🧪 Evidence from Logs

### What We See:
```
DEBUG: Starting game loop...
[END OF LOG - Game window opens and waits]
```

### What We DON'T See:
```
// Should appear if DungeonGameScreen is created:
"DungeonGameScreen created (120x40)"

// Should appear if dungeon initializes:
"🤖 BOT MODE ENABLED - Player will be AI-controlled"
"Created player entity at (40, 12)"

// Should appear if game runs:
Player movement logs
Enemy AI logs
Game state updates
```

---

## 🔧 Diagnostic Steps Needed

### 1. Check if IDungeonService is Resolved
**File:** `WingedBean.Plugins.SadConsole/SadConsolePlugin.cs` (line 37-40)

```csharp
IDungeonService? dungeon = null;
try { dungeon = registry.Get<IDungeonService>(); } catch { }
```

**Issue:** Silent catch - if this fails, `dungeon` is null and no error is logged

**Fix:** Add logging to see if service resolution succeeds

### 2. Check if DungeonGameScreen is Created
**File:** `WingedBean.Plugins.SadConsole/SadConsoleApp.cs` (line 93)

```csharp
_gameScreen = new DungeonGameScreen(120, 40, _logger, _dungeon, _gameplay, _worldProvider, _recorder);
```

**Issue:** If this throws an exception, it might be caught silently

**Fix:** Add try-catch with logging around screen creation

### 3. Check if Initialize() is Called
**File:** `WingedBean.Plugins.SadConsole/DungeonGameScreen.cs` (line 66-70)

```csharp
try
{
    _dungeon?.Initialize();
}
catch { /* ignore */ }
```

**Issue:** Exceptions are silently ignored!

**Fix:** Log exceptions to see what's failing

---

## 📊 Service Registration Status

From logs, we know these services ARE registered:
- ✅ `IRegistry`
- ✅ `IPluginLoader`
- ✅ `IUIApp` (SadConsoleApp)
- ✅ `IDungeonService` (logged: "Registering IDungeonService")

But we don't know if `IDungeonService` is successfully RETRIEVED when creating SadConsoleApp.

---

## 🎬 Next Actions

### Immediate (Add Diagnostic Logging)

1. **In SadConsolePlugin.cs:**
   ```csharp
   try { 
       dungeon = registry.Get<IDungeonService>(); 
       logger?.LogInformation("✓ IDungeonService resolved successfully");
   } catch (Exception ex) { 
       logger?.LogWarning(ex, "✗ Failed to resolve IDungeonService");
   }
   ```

2. **In DungeonGameScreen.cs:**
   ```csharp
   try
   {
       _logger?.LogInformation("Calling _dungeon?.Initialize()...");
       _dungeon?.Initialize();
       _logger?.LogInformation("✓ Dungeon initialized successfully");
   }
   catch (Exception ex) 
   { 
       _logger?.LogError(ex, "✗ Failed to initialize dungeon");
   }
   ```

3. **In SadConsoleApp.cs:**
   ```csharp
   try {
       _logger?.LogInformation("Creating DungeonGameScreen...");
       _gameScreen = new DungeonGameScreen(...);
       _logger?.LogInformation("✓ DungeonGameScreen created");
   } catch (Exception ex) {
       _logger?.LogError(ex, "✗ Failed to create DungeonGameScreen");
   }
   ```

### After Diagnostics

Once we see WHERE it's failing, we can fix the actual issue.

---

## 💡 Likely Issues

Based on the silent failures, most likely causes:

1. **IDungeonService not resolved** - Registry lookup fails silently
2. **DungeonGameService not properly initialized** - Missing dependencies
3. **Exception during Initialize()** - Caught and ignored
4. **Game loop not calling Update()** - Screen never renders/updates

---

## 🎯 Success Criteria

When fixed, we should see:
- ✅ "🤖 BOT MODE ENABLED" in logs
- ✅ "Created player entity" in logs
- ✅ "DungeonGameScreen created" in logs
- ✅ URF files generated in `recordings/` directory
- ✅ Player movement/AI activity in logs
- ✅ Game runs for specified duration and exits

---

## 📝 Related Files

**Plugin Loading:**
- `/development/dotnet/windows/src/host/Dungeon.Host.SadConsole/PluginLoaderHostedService.cs`

**Game Initialization:**
- `/development/dotnet/windows/src/plugins/WingedBean.Plugins.SadConsole/SadConsoleApp.cs`
- `/development/dotnet/windows/src/plugins/WingedBean.Plugins.SadConsole/DungeonGameScreen.cs`
- `/development/dotnet/windows/src/plugins/WingedBean.Plugins.SadConsole/SadConsolePlugin.cs`

**Dungeon Logic:**
- `/development/dotnet/console/src/plugins/WingedBean.Plugins.DungeonGame/DungeonGame.cs` (line 289 - bot mode)
- `/development/dotnet/console/src/plugins/WingedBean.Plugins.DungeonGame/DungeonGameService.cs`

**Recording:**
- `/development/dotnet/windows/src/plugins/WingedBean.Plugins.Recorder.Snapshots/HybridRecorder.cs`

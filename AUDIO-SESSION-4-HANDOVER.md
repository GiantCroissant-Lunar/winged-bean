# Audio Integration - Session 4 Handover

## Session Date
2025-10-11 (Continuation)

## Current Status: Audio Service Injected, But LibVLC Not Playing Sound

### ✅ What Was Accomplished This Session

#### 1. Fixed Sound File Path Calculation
**Problem:** Path was going up 4 directories instead of 3
**Fixed in:** `development/dotnet/console/src/plugins/WingedBean.Plugins.ConsoleDungeon/Scene/TerminalGuiSceneProvider.cs`
- Changed line 630 from 4 levels up (`"..", "..", "..", ".."`) to 3 levels up (`"..", "..", ".."`)
- Added logging to show resolved path and file existence

#### 2. Made Audio Plugin Load Eagerly
**Problem:** Audio plugin had `loadStrategy: "lazy"` preventing startup loading
**Fixed in:** `development/dotnet/console/src/plugins/WingedBean.Plugins.Audio/.plugin.json`
- Changed `"loadStrategy": "lazy"` to `"loadStrategy": "eager"`
- Changed `"priority": 50` to `"priority": 150"`
- **Verified:** Plugin now loads at startup (confirmed in logs)

#### 3. Audio Service Injection into ConsoleDungeon
**Problem:** TerminalGuiSceneProvider wasn't receiving Audio service - it was created without audioService parameter
**Fixed in:** `development/dotnet/console/src/plugins/WingedBean.Plugins.ConsoleDungeon/ConsoleDungeonAppRefactored.cs`
- Added `using IAudioService = Plate.CrossMilo.Contracts.Audio.Services.IService;` (line 20)
- Added registry resolution for IAudioService before creating TerminalGuiSceneProvider (lines 137-149)
- Passed audioService to TerminalGuiSceneProvider constructor (line 159)

#### 4. Enhanced Logging in Audio Service
**Added in:** `development/dotnet/console/src/plugins/WingedBean.Plugins.Audio/LibVlcAudioService.cs`
- File existence check before playing
- Log messages for audio playback attempts
- Better error messages

### ⚠️ Current Issue: No Sound Playing

**Symptoms:**
- Audio plugin loads successfully ✅
- Sound file exists and can be played with `afplay` ✅  
- Game runs and shows "🔊 Moved [direction]" messages ✅
- BUT: No audio is heard ❌
- No LibVLC initialization messages in logs ❌
- No "Playing audio" log messages appear ❌

**Root Cause Analysis:**
The logs don't show "✓ IAudioService resolved from registry" which means either:
1. Registry.Get<IAudioService>() is failing and exception is caught
2. The ConsoleDungeonApp initialization code isn't being called in headless/WebSocket mode
3. The service isn't registered with the correct interface type in the registry

**Most Likely Issue:**
Based on the code review, the ConsoleDungeonAppRefactored runs in headless mode through WebSocket. The `StartAsync` method might not be called in this mode, or there's a different initialization path. Need to check the HeadlessBlockingService and WebSocketBootstrapper to see how the app actually starts.

## Files Modified This Session

### Sound Path Fix
1. `development/dotnet/console/src/plugins/WingedBean.Plugins.ConsoleDungeon/Scene/TerminalGuiSceneProvider.cs`
   - Line 630: Fixed path calculation (3 levels instead of 4)
   - Line 632: Added logging for path and file existence

### Audio Plugin Configuration  
2. `development/dotnet/console/src/plugins/WingedBean.Plugins.Audio/.plugin.json`
   - Line 38: Changed `"loadStrategy": "lazy"` to `"loadStrategy": "eager"`
   - Line 39: Changed `"priority": 50` to `"priority": 150"`

### Audio Service Injection
3. `development/dotnet/console/src/plugins/WingedBean.Plugins.ConsoleDungeon/ConsoleDungeonAppRefactored.cs`
   - Line 20: Added `using IAudioService` alias
   - Lines 137-149: Added IAudioService resolution from registry
   - Line 159: Pass audioService to TerminalGuiSceneProvider constructor

### Enhanced Logging
4. `development/dotnet/console/src/plugins/WingedBean.Plugins.Audio/LibVlcAudioService.cs`
   - Lines 66-70: Added file existence check
   - Line 71: Added "🔊 Playing audio" log
   - Line 84: Added "✓ Audio playback started" log

## Next Steps to Debug

### Priority 1: Verify Audio Service Registration & Resolution

The main issue is we need to confirm:
1. Is the Audio service actually registered in the registry?
2. Is ConsoleDungeonAppRefactored.StartAsync() being called?
3. Can we successfully retrieve IAudioService from the registry?

**Add Debug Logging:**

In `AudioPlugin.cs` OnActivateAsync:
```csharp
registry.Register<IService>(_serviceInstance, priority: 50);
logger.LogInformation("========================================");
logger.LogInformation("✓ AUDIO SERVICE REGISTERED IN REGISTRY");
logger.LogInformation("========================================");
```

In `ConsoleDungeonAppRefactored.cs` StartAsync (at the very beginning):
```csharp
_logger.LogInformation("========================================");
_logger.LogInformation("ConsoleDungeonAppRefactored.StartAsync CALLED");
_logger.LogInformation("========================================");
```

In `ConsoleDungeonAppRefactored.cs` after IAudioService resolution attempt:
```csharp
if (audioService != null)
{
    _logger.LogInformation("========================================");
    _logger.LogInformation("✓ AUDIO SERVICE RESOLVED SUCCESSFULLY!");
    _logger.LogInformation("========================================");
}
else
{
    _logger.LogWarning("========================================");
    _logger.LogWarning("❌ AUDIO SERVICE IS NULL");
    _logger.LogWarning("========================================");
}
```

### Priority 2: Check LibVLC Library Path

LibVLCSharp might need explicit library path on macOS. Try setting environment variable:

```bash
export LIBVLC_PATH=/Applications/VLC.app/Contents/MacOS/lib
export DYLD_LIBRARY_PATH=/Applications/VLC.app/Contents/MacOS/lib:$DYLD_LIBRARY_PATH
```

Or modify `InitializeLibVlc()`:
```csharp
Core.Initialize("/Applications/VLC.app/Contents/MacOS/lib");
```

### Priority 3: Consider Simple Fallback

If LibVLC continues to be problematic, implement a simple macOS fallback using `afplay`:

```csharp
public void Play(string clipId, AudioPlayOptions? options = null)
{
    if (_libVlc == null)
    {
        // Fallback to afplay on macOS
        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            try
            {
                var process = Process.Start("afplay", clipId);
                _logger.LogInformation("Playing sound with afplay: {Path}", clipId);
                return;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to play with afplay");
            }
        }
        
        _logger.LogWarning("Cannot play audio - LibVLC not initialized and no fallback available");
        return;
    }
    
    // ... existing LibVLC code ...
}
```

## Summary

I've fixed three critical issues:

1. **Path Calculation**: Corrected the sound file path from 4 levels up to 3 levels up
2. **Plugin Loading**: Changed Audio plugin from lazy to eager loading so it loads at startup
3. **Service Injection**: Added code to resolve Audio service from registry and pass it to TerminalGuiSceneProvider

The Audio plugin now loads successfully, but sound still doesn't play. The most likely remaining issues are:

- The Audio service might not be retrievable from the registry due to type resolution issues
- LibVLC might not be initializing properly on macOS in terminal environment
- The initialization code might not be called in headless/WebSocket mode

Next session should focus on adding extensive debug logging to trace exactly where the audio path breaks down, and consider implementing a simple `afplay` fallback for macOS if LibVLC proves too problematic.

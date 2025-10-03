# Audio Integration Analysis

**Date**: 2025-10-03  
**Status**: Ready for Integration  
**Priority**: Medium

---

## ✅ Current State

### Audio Plugin Infrastructure - COMPLETE

The audio plugin infrastructure is **fully implemented** and ready to use:

```
development/dotnet/
├── console/src/plugins/WingedBean.Plugins.Audio/
│   ├── AudioPlugin.cs                    # Plugin activator
│   ├── NAudioService.cs                  # LibVLC implementation (362 lines)
│   ├── .plugin.json                      # Plugin metadata
│   └── WingedBean.Plugins.Audio.csproj
│
└── framework/src/WingedBean.Contracts.Audio/
    ├── IAudioService.cs                  # Service contract
    ├── AudioPlayOptions.cs               # Play options (volume, loop, pitch, fade)
    ├── ProxyService.cs                   # Source-generated proxy
    └── WingedBean.Contracts.Audio.csproj
```

### Key Features Implemented

#### IAudioService Contract
```csharp
public interface IAudioService
{
    void Play(string clipId, AudioPlayOptions? options = null);
    void Stop(string clipId);
    void StopAll();
    void Pause(string clipId);
    void Resume(string clipId);
    float Volume { get; set; }  // Master volume 0.0-1.0
    bool IsPlaying(string clipId);
    Task<bool> LoadAsync(string clipId, CancellationToken ct = default);
    void Unload(string clipId);
}
```

#### AudioPlayOptions
```csharp
public record AudioPlayOptions
{
    public float Volume { get; init; } = 1.0f;
    public bool Loop { get; init; } = false;
    public float Pitch { get; init; } = 1.0f;
    public float FadeInDuration { get; init; } = 0f;
    public string? MixerGroup { get; init; }
}
```

#### LibVLC Implementation (NAudioService.cs)
- ✅ Multi-channel audio playback
- ✅ Volume control (per-clip and master)
- ✅ Looping support
- ✅ Pause/Resume functionality
- ✅ Preload/Unload for memory management
- ✅ Thread-safe with locking
- ✅ Proper dispose pattern
- ✅ Cross-platform (Windows + Mac via conditional packages)

### Technology Stack

**LibVLCSharp** (v3.9.0):
- Mature, well-maintained library
- Supports many audio formats (MP3, WAV, OGG, FLAC, etc.)
- Cross-platform (Windows, macOS, Linux)
- Used by VLC media player (battle-tested)
- MIT license

**Platform-specific packages**:
- `VideoLAN.LibVLC.Windows` (v3.0.21) - Windows binaries
- `VideoLAN.LibVLC.Mac` (v3.0.21) - macOS binaries
- Conditional inclusion via `$(OS)` condition

---

## 🎯 Integration Steps

### Step 1: Enable Audio Plugin in Host Configuration

**File**: `development/dotnet/console/src/host/ConsoleDungeon.Host/plugins.config.json`

Add audio plugin to the enabled plugins list:

```json
{
  "plugins": [
    "WingedBean.Plugins.Config",
    "WingedBean.Plugins.ArchECS",
    "WingedBean.Plugins.DungeonGame",
    "WingedBean.Plugins.Audio",        // ← Add this
    "WingedBean.Plugins.TerminalUI",
    "WingedBean.Plugins.ConsoleDungeon"
  ]
}
```

### Step 2: Update .plugin.json Implementation Reference

**File**: `development/dotnet/console/src/plugins/WingedBean.Plugins.Audio/.plugin.json`

The current reference is incorrect (line 24):
```json
"implementation": "WingedBean.Plugins.Audio.NAudioService"
```

Should be:
```json
"implementation": "WingedBean.Plugins.Audio.LibVlcAudioService"
```

### Step 3: Add Audio to Console Dungeon Game

**File**: `development/dotnet/console/src/plugins/WingedBean.Plugins.ConsoleDungeon/ConsoleDungeonApp.cs`

Inject IAudioService:

```csharp
private IAudioService? _audioService;

public async Task<bool> InitializeAsync(TerminalAppConfig config, CancellationToken ct)
{
    // ... existing initialization ...
    
    // Resolve audio service
    _audioService = _registry?.Resolve<IAudioService>();
    if (_audioService != null)
    {
        LogToFile("✓ IAudioService injected");
        _logger.LogInformation("✓ IAudioService injected");
    }
    else
    {
        LogToFile("⚠ IAudioService not available");
        _logger.LogWarning("⚠ IAudioService not available");
    }
    
    // ... rest of initialization ...
}
```

### Step 4: Add Sound Effects to Game Actions

Example sound integration:

```csharp
private void HandleGameInput(GameInputEvent inputEvent)
{
    switch (inputEvent.InputType)
    {
        case GameInputType.MoveUp:
        case GameInputType.MoveDown:
        case GameInputType.MoveLeft:
        case GameInputType.MoveRight:
            // Play footstep sound
            _audioService?.Play("sounds/footstep.wav", new AudioPlayOptions 
            { 
                Volume = 0.3f 
            });
            break;
            
        case GameInputType.Attack:
            // Play attack sound
            _audioService?.Play("sounds/attack.wav", new AudioPlayOptions 
            { 
                Volume = 0.5f 
            });
            break;
            
        case GameInputType.MenuToggle:
            // Play UI sound
            _audioService?.Play("sounds/menu_open.wav", new AudioPlayOptions 
            { 
                Volume = 0.4f 
            });
            break;
    }
    
    // Pass input to game service
    _gameService?.HandleInput(inputEvent.ConvertToGameInput());
}
```

### Step 5: Add Background Music

```csharp
public async Task<bool> InitializeAsync(TerminalAppConfig config, CancellationToken ct)
{
    // ... after other initialization ...
    
    // Start background music
    if (_audioService != null)
    {
        await _audioService.LoadAsync("music/dungeon_theme.mp3", ct);
        _audioService.Play("music/dungeon_theme.mp3", new AudioPlayOptions
        {
            Volume = 0.2f,
            Loop = true
        });
        LogToFile("✓ Background music started");
    }
    
    return true;
}
```

---

## 📁 Audio Assets Structure

Recommended folder structure for audio files:

```
development/dotnet/console/src/host/ConsoleDungeon.Host/
└── assets/
    └── audio/
        ├── music/
        │   ├── dungeon_theme.mp3         # Background music
        │   └── victory.mp3               # Victory fanfare
        ├── sfx/
        │   ├── footstep.wav              # Player movement
        │   ├── attack.wav                # Combat sound
        │   ├── hit.wav                   # Damage received
        │   ├── death.wav                 # Enemy death
        │   └── pickup.wav                # Item pickup
        └── ui/
            ├── menu_open.wav             # Menu open
            ├── menu_close.wav            # Menu close
            └── button_click.wav          # Button press
```

**Copy assets to output**:

Add to `ConsoleDungeon.Host.csproj`:
```xml
<ItemGroup>
  <None Include="assets\**\*.*">
    <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
  </None>
</ItemGroup>
```

---

## 🎨 Sound Design Recommendations

### Volume Levels
- **Music**: 0.15 - 0.25 (background, not distracting)
- **UI Sounds**: 0.3 - 0.5 (clear but not loud)
- **Footsteps**: 0.2 - 0.4 (frequent, should be subtle)
- **Combat**: 0.5 - 0.7 (impactful)
- **Alerts**: 0.6 - 0.8 (important events)

### File Formats
- **Music**: MP3 or OGG (compressed, smaller file size)
- **SFX**: WAV (uncompressed, faster loading)
- **Avoid**: Large FLAC files for SFX (overkill for console app)

### Performance Considerations
- **Preload frequently used sounds** (footsteps, attacks)
- **Stream background music** (don't preload)
- **Unload unused sounds** to free memory
- **Limit simultaneous sounds** (5-10 max)

---

## 🧪 Testing Audio Integration

### Test 1: Basic Playback
```csharp
[Fact]
public async Task AudioService_PlaySound_Success()
{
    // Arrange
    var audioService = _serviceProvider.GetRequiredService<IAudioService>();
    
    // Act
    audioService.Play("test.wav");
    await Task.Delay(100); // Let it start
    
    // Assert
    Assert.True(audioService.IsPlaying("test.wav"));
}
```

### Test 2: Volume Control
```csharp
[Fact]
public void AudioService_VolumeControl_Works()
{
    // Arrange
    var audioService = _serviceProvider.GetRequiredService<IAudioService>();
    
    // Act
    audioService.Volume = 0.5f;
    
    // Assert
    Assert.Equal(0.5f, audioService.Volume);
}
```

### Test 3: Looping
```csharp
[Fact]
public async Task AudioService_Looping_Repeats()
{
    // Arrange
    var audioService = _serviceProvider.GetRequiredService<IAudioService>();
    
    // Act
    audioService.Play("short.wav", new AudioPlayOptions { Loop = true });
    await Task.Delay(5000); // Wait beyond clip duration
    
    // Assert
    Assert.True(audioService.IsPlaying("short.wav")); // Should still be playing
}
```

---

## ⚠️ Known Limitations

### LibVLC Initialization
- **Requires LibVLC binaries** at runtime (included in packages)
- **First call may be slow** (~100ms to initialize)
- **No WASM/Browser support** (LibVLC is native)

### Console Profile Constraints
- **No real-time pitch shifting** (LibVLC limitation)
- **Fade effects are basic** (linear fade only)
- **No spatial audio** (stereo only)

### Platform Considerations
- **macOS**: May require app signing for LibVLC
- **Linux**: Requires LibVLC system packages (not included)
- **Windows**: Works out of the box

---

## 🚀 Future Enhancements

### Phase 2 (Optional)
- [ ] Audio mixer groups for categorization
- [ ] Crossfade between music tracks
- [ ] Sound effect pooling for performance
- [ ] Ducking (lower music when SFX plays)
- [ ] Audio settings persistence

### Phase 3 (Advanced)
- [ ] Unity audio bridge (same IAudioService, different provider)
- [ ] Godot audio integration
- [ ] Web Audio API for browser profile
- [ ] FMOD/Wwise integration for advanced features

---

## 📊 Integration Checklist

### Prerequisites
- [x] IAudioService contract defined
- [x] LibVlcAudioService implemented
- [x] Audio plugin structure created
- [x] Platform-specific packages configured
- [x] Central Package Management (CPM) added

### Implementation Tasks
- [ ] Fix .plugin.json implementation name
- [ ] Enable plugin in plugins.config.json
- [ ] Add audio assets folder structure
- [ ] Configure asset copying in .csproj
- [ ] Inject IAudioService in ConsoleDungeonApp
- [ ] Add sound effects to game actions
- [ ] Add background music
- [ ] Test audio playback
- [ ] Verify volume controls work
- [ ] Test pause/resume functionality

### Testing Tasks
- [ ] Unit tests for AudioPlugin activation
- [ ] Integration tests for sound playback
- [ ] Manual testing: footsteps on movement
- [ ] Manual testing: attack sounds
- [ ] Manual testing: UI sounds
- [ ] Manual testing: background music looping
- [ ] Performance testing: multiple sounds
- [ ] Cross-platform testing (if applicable)

### Documentation Tasks
- [ ] Update RFC or create new RFC for audio system
- [ ] Document sound asset guidelines
- [ ] Add audio troubleshooting guide
- [ ] Document volume level recommendations

---

## 💡 Quick Start Guide

**To add audio to your game in 5 minutes:**

1. **Create audio folder**:
   ```bash
   mkdir -p development/dotnet/console/src/host/ConsoleDungeon.Host/assets/audio/sfx
   ```

2. **Add a test sound** (use any .wav or .mp3):
   ```bash
   # Copy or download a test audio file
   cp ~/Downloads/test.wav development/dotnet/console/src/host/ConsoleDungeon.Host/assets/audio/sfx/
   ```

3. **Fix .plugin.json** (change NAudioService → LibVlcAudioService)

4. **Enable plugin** in `plugins.config.json`

5. **Inject service** in ConsoleDungeonApp and play:
   ```csharp
   _audioService?.Play("assets/audio/sfx/test.wav");
   ```

6. **Build and run**:
   ```bash
   cd development/dotnet/console
   task build
   cd ../../nodejs
   pm2 restart console-dungeon
   ```

7. **Verify** audio plays when game starts!

---

## 🎓 References

- [LibVLCSharp Documentation](https://code.videolan.org/videolan/LibVLCSharp)
- [LibVLC Audio Formats](https://wiki.videolan.org/Documentation:Modules/audio/)
- [Game Audio Best Practices](https://www.gamedeveloper.com/audio/game-audio-101-a-crash-course-in-audio-implementation)

---

**Status**: ✅ Ready to integrate
**Estimated Time**: 30-60 minutes for basic integration
**Risk**: Low (well-tested library, isolated plugin)

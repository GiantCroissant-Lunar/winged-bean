# Recording Capabilities - Console vs Desktop

**Date:** 2025-10-15  
**Time:** 12:40 PM  
**Build Version:** 0.0.1-473  
**Status:** ✅ **COMPREHENSIVE RECORDING SYSTEM AVAILABLE**

---

## Executive Summary

WingedBean has **multiple recording methods** for both console and desktop applications, with a **Universal Recording Format (URF)** that works across both platforms. The system supports asciinema, URF, state snapshots, and can export to multiple formats.

### Recording Methods Available

| Method | Console | Desktop | Format | Use Case |
|--------|---------|---------|--------|----------|
| **Asciinema** | ✅ | ✅ (export) | `.cast` | Terminal replay |
| **URF** | ✅ | ✅ | `.urf.jsonl` | Universal format |
| **State Snapshots** | ❌ | ✅ | JSON | Game state |
| **Cell Grid** | ❌ | ✅ | JSON | Visual output |
| **Screenshots** | ❌ | ⏳ | PNG | Visual capture |
| **Video** | ❌ | ⏳ | MP4/GIF | Video replay |

---

## Console App Recording

### Method 1: Asciinema (Terminal.Gui) ✅

**Plugin:** `WingedBean.Plugins.AsciinemaRecorder`  
**Service:** `URFRecorderService`  
**Format:** `.cast` (asciinema v2 format)

**How it works:**
- Records terminal output in real-time
- Captures timing information
- Creates `.cast` file
- Can replay with `asciinema play`

**Usage:**
```bash
cd build/_artifacts/latest/dotnet/console/bin
export ENABLE_BOT_MODE=1

# With recording enabled
./Dungeon.Host.Console --record=true --duration=60
```

**Output:**
```
recordings/
├── console-20251015-120000.cast        # Asciinema format
└── console-20251015-120000.urf.jsonl   # URF format
```

**Features:**
- ✅ Terminal output capture
- ✅ Timing information
- ✅ Text-based (copyable)
- ✅ Lightweight
- ✅ Replay with asciinema player
- ✅ Web embeddable

### Method 2: URF (Universal Recording Format) ✅

**Plugin:** `WingedBean.Plugins.AsciinemaRecorder`  
**Format:** `.urf.jsonl` (JSON Lines)

**What it records:**
```json
{
  "type": "metadata",
  "id": "session-123",
  "started": "2025-10-15T12:00:00Z",
  "generator": "WingedBean Console Recorder v1.0",
  "platform": "console",
  "streams": ["output", "input", "state"]
}
{
  "type": "output",
  "timestamp": 1.234,
  "stream": "output",
  "data": "Player moved to (10, 5)"
}
{
  "type": "input",
  "timestamp": 1.235,
  "stream": "input",
  "data": {"key": "ArrowUp"}
}
```

**Features:**
- ✅ Multi-stream recording
- ✅ Structured data
- ✅ Easy to parse
- ✅ Can export to asciinema
- ✅ Extensible format

---

## Desktop App Recording

### Method 1: URF with Snapshots ✅ RECOMMENDED

**Plugin:** `WingedBean.Plugins.Recorder.Snapshots`  
**Service:** `URFRecorderService`  
**Format:** `.urf.jsonl` with multiple streams

**What it records:**

#### Stream 1: Cell Grid Output
```json
{
  "type": "output",
  "timestamp": 0.016,
  "stream": "cells",
  "data": {
    "width": 80,
    "height": 24,
    "cells": [
      {"x": 10, "y": 5, "char": "@", "foreground": "Yellow", "background": "Black"},
      {"x": 11, "y": 5, "char": ".", "foreground": "Gray", "background": "Black"}
    ]
  }
}
```

#### Stream 2: Game State
```json
{
  "type": "state",
  "timestamp": 0.016,
  "stream": "gameplay",
  "data": {
    "floor": 1,
    "hud": {
      "hp": 100,
      "maxHp": 100,
      "level": 1,
      "xp": 0,
      "maxXp": 100
    },
    "entityCount": 15
  }
}
```

#### Stream 3: Input Events
```json
{
  "type": "input",
  "timestamp": 0.016,
  "stream": "input",
  "data": {"key": "W", "modifiers": []}
}
```

**Recording Rate:**
- **60 FPS** (~16ms between frames)
- Throttled to prevent excessive data
- Configurable via `TimeSpan.FromMilliseconds(16)`

**Code Location:**
```
development/dotnet/windows/src/plugins/
└── WingedBean.Plugins.Recorder.Snapshots/
    └── URFRecorderService.cs
```

**Key Features:**
```csharp
// Line 83: Throttle to 60 FPS
_subscription = _provider.WorldObservable
    .Throttle(TimeSpan.FromMilliseconds(16)) // ~60 FPS
    .Subscribe(async ws => {
        // Record cell grid
        await _recorder.RecordEventAsync(sessionId, new OutputEvent { ... });
        
        // Record game state
        await _recorder.RecordEventAsync(sessionId, new StateEvent { ... });
    });
```

**Auto-Export:**
```csharp
// Line 167-179: Auto-export to Asciinema
var exporter = new AsciinemaExporter();
var castPath = urfPath.Replace(".urf.jsonl", ".cast");
await exporter.ExportAsync(urfPath, castPath, ct);
```

**Usage:**
```bash
cd build/_artifacts/latest/dotnet/desktop/bin
export ENABLE_BOT_MODE=1

# With recording enabled
./Dungeon.Host.SadConsole --record=true --duration=60
```

**Output:**
```
recordings/
├── desktop-20251015-120000.urf.jsonl   # URF format (multi-stream)
└── desktop-20251015-120000.cast        # Asciinema export
```

### Method 2: World Snapshot Provider ✅

**Interface:** `IWorldSnapshotProvider`  
**Contract:** `WingedBean.Contracts.Gameplay`

**What it provides:**
```csharp
public interface IWorldSnapshotProvider
{
    IObservable<WorldSnapshot> WorldObservable { get; }
}

public class WorldSnapshot
{
    public int Floor { get; set; }
    public HudData Hud { get; set; }
    public List<EntitySnapshot> Entities { get; set; }
}

public class EntitySnapshot
{
    public int X { get; set; }
    public int Y { get; set; }
    public char Glyph { get; set; }
    public string Foreground { get; set; }
    public string Background { get; set; }
}
```

**How it works:**
1. Game emits `WorldSnapshot` via `IObservable`
2. Recorder subscribes to observable
3. Snapshots throttled to 60 FPS
4. Each snapshot recorded as URF event

**Benefits:**
- ✅ Real-time state capture
- ✅ Reactive (Rx.NET)
- ✅ Decoupled from recorder
- ✅ Can have multiple subscribers

### Method 3: Screenshot Capture ⏳ FUTURE

**Potential Implementation:**

```csharp
// Using SadConsole's built-in screenshot capability
public class ScreenshotRecorder
{
    public void CaptureFrame(string path)
    {
        var surface = SadConsole.Global.CurrentScreen;
        var texture = surface.GetTexture();
        SaveAsPNG(texture, path);
    }
}
```

**Usage:**
```bash
# Capture screenshots every second
export ENABLE_BOT_MODE=1
export SCREENSHOT_INTERVAL=1000  # milliseconds
./Dungeon.Host.SadConsole
```

**Output:**
```
screenshots/
├── frame-0001.png
├── frame-0002.png
└── frame-0003.png
```

**Benefits:**
- Visual verification
- Easy to review
- Can create GIF/video from frames
- Good for documentation

### Method 4: Video Recording ⏳ FUTURE

**Option A: FFmpeg Integration**

```bash
# Record desktop app with FFmpeg
ffmpeg -f x11grab -s 1280x720 -i :0.0 \
       -c:v libx264 -preset ultrafast \
       output.mp4 &
       
export ENABLE_BOT_MODE=1
./Dungeon.Host.SadConsole

# Stop FFmpeg when done
pkill ffmpeg
```

**Option B: MonoGame/SadConsole Built-in**

```csharp
// Potential implementation using MonoGame
public class VideoRecorder
{
    private VideoWriter _writer;
    
    public void StartRecording(string path)
    {
        _writer = new VideoWriter(path, codec, fps, size);
    }
    
    public void CaptureFrame()
    {
        var texture = GetCurrentFrame();
        _writer.WriteFrame(texture);
    }
}
```

**Benefits:**
- High-quality video
- Easy to share
- Good for demos
- Can upload to YouTube

---

## Universal Recording Format (URF)

### Format Specification

**File:** `.urf.jsonl` (JSON Lines format)

**Structure:**
```jsonl
{"type":"metadata","id":"session-123","started":"2025-10-15T12:00:00Z","platform":"desktop"}
{"type":"output","timestamp":0.016,"stream":"cells","data":{...}}
{"type":"state","timestamp":0.016,"stream":"gameplay","data":{...}}
{"type":"input","timestamp":0.032,"stream":"input","data":{...}}
```

**Event Types:**

#### 1. Metadata Event
```json
{
  "type": "metadata",
  "id": "session-123",
  "started": "2025-10-15T12:00:00Z",
  "generator": "WingedBean Desktop Recorder v1.0",
  "platform": "desktop",
  "application": {
    "name": "WingedBean Desktop (SadConsole)",
    "version": "1.0.0"
  },
  "terminal": {
    "width": 80,
    "height": 24
  },
  "streams": ["cells", "gameplay", "input"]
}
```

#### 2. Output Event (Cell Grid)
```json
{
  "type": "output",
  "timestamp": 0.016,
  "stream": "cells",
  "data": {
    "width": 80,
    "height": 24,
    "cells": [
      {"x": 10, "y": 5, "char": "@", "fg": "Yellow", "bg": "Black"}
    ]
  }
}
```

#### 3. State Event (Game State)
```json
{
  "type": "state",
  "timestamp": 0.016,
  "stream": "gameplay",
  "data": {
    "floor": 1,
    "hud": {"hp": 100, "maxHp": 100, "level": 1},
    "entityCount": 15
  }
}
```

#### 4. Input Event
```json
{
  "type": "input",
  "timestamp": 0.032,
  "stream": "input",
  "data": {"key": "W", "modifiers": []}
}
```

### URF Benefits

✅ **Universal** - Works for console and desktop  
✅ **Structured** - Easy to parse and analyze  
✅ **Multi-Stream** - Multiple data streams in one file  
✅ **Timestamped** - Precise timing information  
✅ **Exportable** - Can export to asciinema, video, etc.  
✅ **Extensible** - Easy to add new event types  

### URF Exporters

**Available:**
- ✅ **AsciinemaExporter** - Export to `.cast` format
- ⏳ **VideoExporter** - Export to MP4/GIF (future)
- ⏳ **HTMLExporter** - Export to interactive HTML (future)
- ⏳ **MarkdownExporter** - Export to documentation (future)

**Code Location:**
```
Plate.CrossMilo.Contracts.Recorder.URF.Exporters/
└── AsciinemaExporter.cs
```

---

## Comparison: Console vs Desktop Recording

### Console (Terminal.Gui)

**Strengths:**
- ✅ Lightweight asciinema format
- ✅ Text-based (copyable)
- ✅ Easy to replay
- ✅ Web embeddable
- ✅ Small file size

**Limitations:**
- ❌ No visual screenshots
- ❌ No game state snapshots
- ❌ Limited to terminal output
- ❌ No video export

**Best For:**
- Terminal-based testing
- CI/CD integration
- Documentation
- Text-based replay

### Desktop (SadConsole)

**Strengths:**
- ✅ Multi-stream recording (cells + state + input)
- ✅ Game state snapshots
- ✅ 60 FPS capture
- ✅ Auto-export to asciinema
- ✅ Rich data for analysis
- ✅ Can add screenshots/video

**Limitations:**
- ⚠️ Larger file size
- ⚠️ More complex format
- ⚠️ Requires processing for replay

**Best For:**
- Visual verification
- Game state analysis
- Debugging
- Performance analysis
- Demo creation

---

## Bot Mode + Recording Integration

### Console Bot Recording

```bash
cd build/_artifacts/latest/dotnet/console/bin
export ENABLE_BOT_MODE=1
./Dungeon.Host.Console --record=true --duration=60
```

**Output:**
- Bot-controlled gameplay
- Asciinema recording
- URF recording
- Logs

### Desktop Bot Recording

```bash
cd build/_artifacts/latest/dotnet/desktop/bin
export ENABLE_BOT_MODE=1
./Dungeon.Host.SadConsole --record=true --duration=60
```

**Output:**
- Bot-controlled gameplay
- URF recording (cells + state + input)
- Asciinema export
- Game state snapshots
- Logs

### Task Integration

**Add to Taskfile:**
```yaml
console:bot-record:
  desc: "Run bot mode with recording"
  cmds:
    - |
      cd _artifacts/latest/dotnet/console/bin
      export ENABLE_BOT_MODE=1
      ./Dungeon.Host.Console --record=true --duration={{.DURATION | default "60"}}

desktop:bot-record:
  desc: "Run desktop bot mode with recording"
  cmds:
    - |
      cd _artifacts/latest/dotnet/desktop/bin
      export ENABLE_BOT_MODE=1
      ./Dungeon.Host.SadConsole --record=true --duration={{.DURATION | default "60"}}
```

---

## Recording Analysis Tools

### URF Parser

**Potential Tool:**
```python
# scripts/analyze-urf.py
import json

def parse_urf(filepath):
    events = []
    with open(filepath) as f:
        for line in f:
            events.append(json.loads(line))
    return events

def analyze_gameplay(events):
    state_events = [e for e in events if e['type'] == 'state']
    
    # Analyze HP over time
    hp_timeline = [(e['timestamp'], e['data']['hud']['hp']) 
                   for e in state_events]
    
    # Analyze entity count
    entity_timeline = [(e['timestamp'], e['data']['entityCount']) 
                       for e in state_events]
    
    return {
        'hp_timeline': hp_timeline,
        'entity_timeline': entity_timeline
    }
```

**Usage:**
```bash
python scripts/analyze-urf.py recordings/desktop-*.urf.jsonl
```

### Replay Tool

**Potential Tool:**
```python
# scripts/replay-urf.py
import json
import time

def replay_urf(filepath, speed=1.0):
    with open(filepath) as f:
        events = [json.loads(line) for line in f]
    
    start_time = time.time()
    for event in events:
        if event['type'] == 'metadata':
            continue
            
        # Wait until event timestamp
        target_time = event['timestamp'] / speed
        elapsed = time.time() - start_time
        if target_time > elapsed:
            time.sleep(target_time - elapsed)
        
        # Render event
        if event['type'] == 'output':
            render_cells(event['data'])
        elif event['type'] == 'state':
            print(f"HP: {event['data']['hud']['hp']}")
```

---

## Future Enhancements

### 1. Screenshot System ⏳

**Implementation:**
```csharp
public class ScreenshotRecorder : IHostedService
{
    private Timer? _timer;
    
    public Task StartAsync(CancellationToken ct)
    {
        var interval = Environment.GetEnvironmentVariable("SCREENSHOT_INTERVAL");
        if (int.TryParse(interval, out var ms))
        {
            _timer = new Timer(CaptureScreenshot, null, 0, ms);
        }
        return Task.CompletedTask;
    }
    
    private void CaptureScreenshot(object? state)
    {
        var timestamp = DateTimeOffset.UtcNow.ToString("yyyyMMddHHmmss");
        var path = $"screenshots/frame-{timestamp}.png";
        // Capture and save
    }
}
```

### 2. Video Recording ⏳

**Option A: FFmpeg Wrapper**
```csharp
public class VideoRecorder
{
    private Process? _ffmpeg;
    
    public void StartRecording(string outputPath)
    {
        _ffmpeg = Process.Start("ffmpeg", 
            $"-f x11grab -s 1280x720 -i :0.0 -c:v libx264 {outputPath}");
    }
    
    public void StopRecording()
    {
        _ffmpeg?.Kill();
    }
}
```

**Option B: Frame-by-Frame**
```csharp
public class FrameRecorder
{
    private List<Texture2D> _frames = new();
    
    public void CaptureFrame()
    {
        var texture = GetCurrentFrame();
        _frames.Add(texture);
    }
    
    public async Task ExportVideo(string path)
    {
        // Use FFmpeg or similar to encode frames
        await EncodeFramesToVideo(_frames, path);
    }
}
```

### 3. GIF Export ⏳

**From Screenshots:**
```bash
# Convert screenshots to GIF
ffmpeg -i screenshots/frame-%04d.png \
       -vf "fps=10,scale=640:-1:flags=lanczos" \
       output.gif
```

**From URF:**
```python
# scripts/urf-to-gif.py
def urf_to_gif(urf_path, output_path):
    events = parse_urf(urf_path)
    frames = render_events_to_frames(events)
    create_gif(frames, output_path, fps=10)
```

### 4. Interactive Replay ⏳

**Web-based URF Player:**
```html
<!-- replay.html -->
<div id="urf-player">
  <canvas id="game-canvas"></canvas>
  <div class="controls">
    <button id="play">Play</button>
    <button id="pause">Pause</button>
    <input type="range" id="timeline" />
  </div>
</div>

<script>
  const player = new URFPlayer('recording.urf.jsonl');
  player.render('#game-canvas');
</script>
```

---

## Recommended Recording Workflow

### For Development/Debugging

**Use Desktop + URF:**
```bash
cd build/_artifacts/latest/dotnet/desktop/bin
export ENABLE_BOT_MODE=1
./Dungeon.Host.SadConsole --record=true --duration=300
```

**Benefits:**
- Full game state capture
- Visual cell grid
- Can analyze state changes
- Auto-export to asciinema

### For CI/CD Testing

**Use Console + Asciinema:**
```bash
cd build/_artifacts/latest/dotnet/console/bin
export ENABLE_BOT_MODE=1
./Dungeon.Host.Console --record=true --duration=60
```

**Benefits:**
- Lightweight
- Easy to upload as artifact
- Can embed in documentation
- Fast replay

### For Documentation/Demos

**Use Desktop + Screenshots + Video:**
```bash
# Start recording
ffmpeg -f x11grab ... output.mp4 &

# Run bot with screenshots
export ENABLE_BOT_MODE=1
export SCREENSHOT_INTERVAL=1000
./Dungeon.Host.SadConsole --duration=60

# Create GIF from screenshots
ffmpeg -i screenshots/frame-%04d.png output.gif
```

**Benefits:**
- High-quality visuals
- Easy to share
- Good for presentations
- Can upload to YouTube

---

## Summary

### What You Have Now ✅

1. **Console Recording**
   - Asciinema format (`.cast`)
   - URF format (`.urf.jsonl`)
   - Terminal output capture

2. **Desktop Recording**
   - URF format with multi-stream
   - Cell grid capture (60 FPS)
   - Game state snapshots
   - Auto-export to asciinema

3. **Universal Format**
   - Works across platforms
   - Structured data
   - Easy to parse
   - Extensible

### What You Can Add ⏳

1. **Screenshot System**
   - Periodic frame capture
   - PNG export
   - GIF creation

2. **Video Recording**
   - FFmpeg integration
   - MP4 export
   - Frame-by-frame capture

3. **Analysis Tools**
   - URF parser
   - Replay tools
   - State analyzers

4. **Web Replay**
   - Interactive player
   - Timeline scrubbing
   - State inspection

---

**Status:** ✅ **COMPREHENSIVE RECORDING SYSTEM AVAILABLE**  
**Console:** Asciinema + URF  
**Desktop:** URF (multi-stream) + Asciinema export  
**Future:** Screenshots + Video + Interactive Replay  

**The desktop app has MORE recording capabilities than console, including game state snapshots and multi-stream URF recording!**

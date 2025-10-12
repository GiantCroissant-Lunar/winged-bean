# Terminal.Gui UI Not Showing - Root Cause & Solution

## Problem
When you run ConsoleDungeon.Host in iTerm2, it doesn't show the Terminal.Gui UI. Instead, it shows "WebSocket server start requested..." and stays in headless mode.

## Root Cause

There are **TWO headless detection points**:

### Detection Point 1: Program.cs (Line 155)
```csharp
headless = Console.IsOutputRedirected || Console.IsInputRedirected ||
           string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("TERM")) ||
           (Environment.GetEnvironmentVariable("DUNGEON_HEADLESS") ?? "").Equals("1", ...);
```

### Detection Point 2: TerminalGuiSceneProvider.cs (Line 119-120) **← THIS IS THE PROBLEM**
```csharp
var redirected = System.Console.IsInputRedirected || System.Console.IsOutputRedirected;
_headlessMode = string.IsNullOrEmpty(term) || redirected;

if (_headlessMode)
{
    _logger?.LogInformation("HEADLESS mode: Skipping Terminal.Gui initialization (no TTY/TERM)");
    return; // ← UI NEVER STARTS!
}
```

Even if Program.cs correctly detects it's NOT headless and starts `TerminalAppRunnerHostedService`, when the app calls `_sceneService.Initialize()`, it checks AGAIN and decides it's headless!

## Why This Happens

Possible reasons:
1. **Daemon/Service mode**: If ConsoleDungeon.Host is launched by PM2 or systemd, file descriptors may be redirected
2. **Double-fork**: If the process is daemonized, stdin/stdout/stderr are redirected to /dev/null
3. **Build artifact issue**: The executable might have been built or configured with specific console settings
4. **Terminal.Gui driver issue**: Terminal.Gui might not be detecting your terminal properly

## Solution 1: Force UI Mode with Environment Variable

Add an environment variable to skip headless detection:

```bash
export FORCE_UI=1
/Users/apprenticegc/Work/lunar-horse/personal-work/yokan-projects/winged-bean/build/_artifacts/0.0.1-422/dotnet/bin/ConsoleDungeon.Host
```

**But this requires code changes** to check for `FORCE_UI` variable.

## Solution 2: Check What's Actually Being Detected

Run this diagnostic in iTerm2:

```bash
cd /Users/apprenticegc/Work/lunar-horse/personal-work/yokan-projects/winged-bean
chmod +x check-console.sh
./check-console.sh
```

This will show you exactly what Console.IsInputRedirected and Console.IsOutputRedirected return in your terminal.

## Solution 3: Patch TerminalGuiSceneProvider (Quick Fix)

Edit: `development/dotnet/console/src/plugins/WingedBean.Plugins.ConsoleDungeon/Scene/TerminalGuiSceneProvider.cs`

Change line 120 from:
```csharp
_headlessMode = string.IsNullOrEmpty(term) || redirected;
```

To:
```csharp
// Force UI mode if FORCE_UI=1 is set
var forceUI = Environment.GetEnvironmentVariable("FORCE_UI") == "1";
_headlessMode = !forceUI && (string.IsNullOrEmpty(term) || redirected);
```

Then rebuild:
```bash
cd /Users/apprenticegc/Work/lunar-horse/personal-work/yokan-projects/winged-bean/build
task build-all
```

Then run with:
```bash
export FORCE_UI=1
/Users/apprenticegc/Work/lunar-horse/personal-work/yokan-projects/winged-bean/build/_artifacts/0.0.1-422/dotnet/bin/ConsoleDungeon.Host
```

## Solution 4: Check How You're Running It

Are you running it:
- Directly in iTerm2? `./ConsoleDungeon.Host`
- Through PM2? `pm2 start`
- Through a script? `./run.sh`
- Through SSH? `ssh host "./ConsoleDungeon.Host"`

If through PM2 or a script, the file descriptors might be redirected.

## Solution 5: Use Task Command

The task system might have proper terminal setup:

```bash
cd /Users/apprenticegc/Work/lunar-horse/personal-work/yokan-projects/winged-bean/build
task console:normal
```

Check `Taskfile.yml` for how `console:normal` is defined - it might set proper environment variables.

## Diagnostic Steps

1. **Check if it's the executable itself**:
   ```bash
   cd /Users/apprenticegc/Work/lunar-horse/personal-work/yokan-projects/winged-bean/build/_artifacts/0.0.1-422/dotnet/bin
   file ConsoleDungeon.Host
   ```

2. **Check how it's being invoked**:
   ```bash
   ps aux | grep ConsoleDungeon.Host
   ```

3. **Check file descriptors**:
   When the app is running, in another terminal:
   ```bash
   lsof -p $(pgrep -f ConsoleDungeon.Host) | grep -E "0u|1u|2u"
   ```
   This shows if stdin(0), stdout(1), stderr(2) are connected to your terminal or redirected.

## Most Likely Cause

Based on the fact that you see "WebSocket server start requested", the app IS running but in headless mode. This means:
- Program.cs detected non-headless (started TerminalAppRunnerHostedService)
- TerminalGuiSceneProvider detected headless (skipped UI initialization)

This inconsistency suggests the console state changed between the two checks, OR Terminal.Gui's detection is more strict.

## Immediate Action

Run this to see what's detected:

```bash
cd /Users/apprenticegc/Work/lunar-horse/personal-work/yokan-projects/winged-bean
./build/_artifacts/0.0.1-422/dotnet/bin/ConsoleDungeon.Host 2>&1 | grep -E "(Headless|HEADLESS|IsOutputRedirected|IsInputRedirected)"
```

This will show you which headless detection triggered.

---

**Would you like me to implement Solution 3 (add FORCE_UI support)?** This is the quickest fix that will definitely work.

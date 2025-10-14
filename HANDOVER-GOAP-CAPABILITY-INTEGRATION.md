# GOAP and Capability Integration - Handover Document

**Date**: 2025-10-14  
**Session**: Checkpoint 4 → Next Session  
**Objective**: Verify GOAP and Capability integration via CrossMilo contracts at the Plate layer

---

## Current Status Summary

### ✅ Completed Work

#### 1. GoblinDemo Fixed
- **File**: `development/dotnet/framework/src/WingedBean.Gameplay.AI/Examples/GoblinDemo.cs`
- **Changes**:
  - Switched from `RulesEngineCapabilityEvaluator` to `RulesEngineCapabilityAdapter`
  - Fixed `CreateActions()` to return proper terminator action
  - Fixed capability workflow return path
- **Status**: ✅ Compiles and builds successfully

#### 2. Plate Plugins Built
Both plugins are successfully built and ready:

**Plate.Plugins.GoapCkpor**:
- Location: `plate-projects/plugins/dotnet/framework/src/Plate.Plugins.GoapCkpor/bin/Debug/net8.0/`
- Fixed issues:
  - Added `using Plate.CrossMilo.Contracts;` for `IRegistry`
  - Pinned `Microsoft.Extensions.Logging.Abstractions` to version `9.0.2`
  - Corrected relative project references (5 levels up: `../../../../../`)
- Key files:
  - `Plate.Plugins.GoapCkpor.dll`
  - `Plate.Plugins.GoapCkpor.deps.json`
  - `.plugin.json`

**Plate.Plugins.CapabilityOrlia**:
- Location: `plate-projects/plugins/dotnet/framework/src/Plate.Plugins.CapabilityOrlia/bin/Debug/net8.0/`
- Fixed issues:
  - Added `using Plate.CrossMilo.Contracts;` for `IRegistry`
  - Pinned `Microsoft.Extensions.Logging.Abstractions` to version `9.0.2`
  - Fixed `CapabilityOrliaEvaluator.cs`:
    - Convert workflows to array: `workflows.ToArray()`
    - Removed `CancellationToken` from underlying evaluator calls
    - Map to `CrossMilo.Contracts.Capability.CapabilityResult` without Score parameter
    - Use LINQ `.Select()` for list mapping
- Key files:
  - `Plate.Plugins.CapabilityOrlia.dll`
  - `Plate.Plugins.CapabilityOrlia.deps.json`
  - `.plugin.json`

#### 3. WingedBean Artifacts Built
- **Command**: `task build-dotnet` (from `build/` directory)
- **Status**: ✅ Build succeeded (665 warnings, 0 errors)
- **Artifacts location**: `build/_artifacts/0.0.1-455/dotnet/`
  - Console: `console/bin/Dungeon.Host.Console`
  - Desktop: `desktop/bin/` (SadConsole version)

#### 4. Taskfile Updated
- **File**: `build/Taskfile.yml`
- **Change**: Added best-effort copy step for Plate plugins in `build-dotnet` task
- **Copy paths**:
  - From: `../../plate-projects/plugins/dotnet/framework/src/Plate.Plugins.{GoapCkpor,CapabilityOrlia}/bin/Debug/net8.0/`
  - To: `_artifacts/<ver>/dotnet/{console,desktop}/bin/plugins/Plate.Plugins.*/`
- **Note**: Copy step runs but plugins are NOT currently in artifacts (see next steps)

---

## ⚠️ Pending Work - CRITICAL

### 1. Copy Plate Plugins to Artifacts
The Plate plugins are built but NOT yet copied to the artifacts directory.

**Required commands** (run from `build/` directory):

```bash
# Set variables
VER="0.0.1-455"
CONSOLE_PLUGINS="_artifacts/$VER/dotnet/console/bin/plugins"
DESKTOP_PLUGINS="_artifacts/$VER/dotnet/desktop/bin/plugins"

# Create plugin directories
mkdir -p "$CONSOLE_PLUGINS/Plate.Plugins.GoapCkpor"
mkdir -p "$CONSOLE_PLUGINS/Plate.Plugins.CapabilityOrlia"
mkdir -p "$DESKTOP_PLUGINS/Plate.Plugins.GoapCkpor"
mkdir -p "$DESKTOP_PLUGINS/Plate.Plugins.CapabilityOrlia"

# Copy GoapCkpor plugin
cp -r ../../plate-projects/plugins/dotnet/framework/src/Plate.Plugins.GoapCkpor/bin/Debug/net8.0/* \
   "$CONSOLE_PLUGINS/Plate.Plugins.GoapCkpor/"
cp -r ../../plate-projects/plugins/dotnet/framework/src/Plate.Plugins.GoapCkpor/bin/Debug/net8.0/* \
   "$DESKTOP_PLUGINS/Plate.Plugins.GoapCkpor/"

# Copy CapabilityOrlia plugin
cp -r ../../plate-projects/plugins/dotnet/framework/src/Plate.Plugins.CapabilityOrlia/bin/Debug/net8.0/* \
   "$CONSOLE_PLUGINS/Plate.Plugins.CapabilityOrlia/"
cp -r ../../plate-projects/plugins/dotnet/framework/src/Plate.Plugins.CapabilityOrlia/bin/Debug/net8.0/* \
   "$DESKTOP_PLUGINS/Plate.Plugins.CapabilityOrlia/"

# Verify
ls -la "$CONSOLE_PLUGINS/"
```

### 2. Verify Plugin Registration in plugins.json
Check if `plugins.json` needs updating to include the Plate plugins:

```bash
# View current plugins.json
cat _artifacts/0.0.1-455/dotnet/console/bin/plugins.json
```

The plugin loader may auto-discover plugins in the `plugins/` directory, but verify this.

### 3. Run Console and Check Logs
After copying plugins, run the console application and verify registry resolution:

```bash
# Option A: Run in debug mode (headless, minimal UI)
cd _artifacts/0.0.1-455/dotnet/console/bin
DEBUG_MINIMAL_UI=1 ./Dungeon.Host.Console

# Option B: Use the test script
cd build/test-tools
RUN_BG=1 ./run-debug-mode.sh
```

**Expected log messages** (grep for these):
- `"IGoapPlannerService resolved from registry"` or similar
- `"ICapabilityEvaluator resolved from registry"` or similar

**Log locations**:
- Application logs: `_artifacts/0.0.1-455/dotnet/console/bin/logs/console-dungeon-*.log`
- Background stdout: `_artifacts/0.0.1-455/dotnet/console/bin/_bg_logs/console.out`

**Grep command**:
```bash
VER="0.0.1-455"
BIN="_artifacts/$VER/dotnet/console/bin"
LOG=$(ls -t "$BIN/logs"/console-dungeon-*.log 2>/dev/null | head -1)

# Check log file
grep -E "IGoapPlannerService|ICapabilityEvaluator" "$LOG"

# Check stdout
grep -E "IGoapPlannerService|ICapabilityEvaluator" "$BIN/_bg_logs/console.out"
```

---

## Key Files Modified

### WingedBean Project
1. `development/dotnet/framework/src/WingedBean.Gameplay.AI/Examples/GoblinDemo.cs`
   - Switched to `RulesEngineCapabilityAdapter`
   - Fixed action creation and workflow return

2. `build/Taskfile.yml`
   - Added Plate plugin copy step (best-effort)

### Plate Projects
3. `plate-projects/plugins/dotnet/framework/src/Plate.Plugins.GoapCkpor/Plate.Plugins.GoapCkpor.csproj`
   - Corrected relative paths (5 levels up)
   - Pinned `Microsoft.Extensions.Logging.Abstractions` to `9.0.2`

4. `plate-projects/plugins/dotnet/framework/src/Plate.Plugins.GoapCkpor/GoapCkporPlugin.cs`
   - Added `using Plate.CrossMilo.Contracts;`

5. `plate-projects/plugins/dotnet/framework/src/Plate.Plugins.CapabilityOrlia/Plate.Plugins.CapabilityOrlia.csproj`
   - Corrected relative paths (5 levels up)
   - Pinned `Microsoft.Extensions.Logging.Abstractions` to `9.0.2`

6. `plate-projects/plugins/dotnet/framework/src/Plate.Plugins.CapabilityOrlia/CapabilityOrliaPlugin.cs`
   - Added `using Plate.CrossMilo.Contracts;`

7. `plate-projects/plugins/dotnet/framework/src/Plate.Plugins.CapabilityOrlia/CapabilityOrliaEvaluator.cs`
   - Added `using System.Linq;`
   - Fixed constructor: `workflows.ToArray()`
   - Removed `CancellationToken` from evaluator calls
   - Fixed `CapabilityResult` mapping (no Score parameter)
   - Used LINQ `.Select()` for list mapping

---

## TODO List

### High Priority
- [ ] **Copy Plate plugins to artifacts** (see commands above)
- [ ] **Run console and verify registry resolution logs**
- [ ] **Grep logs for IGoapPlannerService and ICapabilityEvaluator resolution messages**

### Medium Priority
- [ ] **Optional**: Implement `CapabilityBackedGoapAction` demo
- [ ] **Optional**: Create small planner demo using `IGoapPlannerService.Plan(...)`

### Low Priority
- [ ] **Optional**: Remove direct `Plate.Goap.*` and `Plate.Agent.*` references from `ConsoleDungeon.csproj` if not needed
- [ ] **Optional**: Update Taskfile to make plugin copy step more robust (not best-effort)

---

## Technical Context

### Architecture Overview
- **CrossMilo Contracts**: Shared interfaces for inter-plugin communication
  - `IGoapPlannerService` (in `CrossMilo.Contracts.Goap`)
  - `ICapabilityEvaluator` (in `CrossMilo.Contracts.Capability`)
- **Plate Layer**: Foundational layer with core services and contracts
- **Plugin Registry**: `IRegistry` interface for service registration/resolution at runtime
- **PluginManoi**: Plugin system for WingedBean/Plate

### Key Interfaces
```csharp
// IRegistry (Plate.CrossMilo.Contracts)
public interface IRegistry
{
    void Register<TService>(TService implementation, int priority = 0) where TService : class;
    TService Get<TService>(SelectionMode mode = SelectionMode.HighestPriority) where TService : class;
    // ... other methods
}

// IPlugin (Plate.PluginManoi.Contracts)
public interface IPlugin
{
    string Id { get; }
    string Version { get; }
    Task OnActivateAsync(IRegistry registry, CancellationToken cancellationToken = default);
    Task OnDeactivateAsync(CancellationToken cancellationToken = default);
}
```

### Plugin Structure
Each Plate plugin:
1. Implements `IPlugin` interface
2. Registers services in `OnActivateAsync(IRegistry registry, ...)`
3. Has a `.plugin.json` manifest file
4. Lives in `plugins/Plate.Plugins.*/` directory in artifacts

### Build System
- **Task/BuildArcane**: Orchestrated build system
- **Rule**: ALWAYS use `task build-dotnet` from `build/` directory
- **Rule**: NEVER use `dotnet run` directly; always run from artifacts
- **Artifacts**: Versioned outputs in `build/_artifacts/<version>/dotnet/`

---

## Verification Checklist

When you resume, verify these in order:

1. [ ] Plate plugins are present in artifacts:
   ```bash
   ls -la build/_artifacts/0.0.1-455/dotnet/console/bin/plugins/Plate.Plugins.*/
   ```

2. [ ] Plugin DLLs and dependencies are copied:
   ```bash
   find build/_artifacts/0.0.1-455/dotnet/console/bin/plugins/Plate.Plugins.* -name "*.dll"
   ```

3. [ ] Console application starts without errors:
   ```bash
   cd build/_artifacts/0.0.1-455/dotnet/console/bin
   DEBUG_MINIMAL_UI=1 ./Dungeon.Host.Console
   ```

4. [ ] Registry resolution messages appear in logs:
   ```bash
   grep -r "IGoapPlannerService\|ICapabilityEvaluator" build/_artifacts/0.0.1-455/dotnet/console/bin/logs/
   ```

---

## Known Issues

### RulesEngine Version Warning
Both Plate plugins show this warning (non-blocking):
```
warning NU1603: Plate.Capability.RulesEngine depends on RulesEngine (>= 5.1.2) 
but RulesEngine 5.1.2 was not found. RulesEngine 6.0.0 was resolved instead.
```
This is acceptable; RulesEngine 6.0.0 is compatible.

### Script Path Issue
The `run-debug-mode.sh` script expects the binary at:
```
_artifacts/<version>/dotnet/bin/ConsoleDungeon.Host
```
But actual path is:
```
_artifacts/<version>/dotnet/console/bin/Dungeon.Host.Console
```
**Workaround**: Run directly from the correct path or update the script.

---

## Quick Start Commands (Next Session)

```bash
# 1. Navigate to build directory
cd /Users/apprenticegc/Work/lunar-horse/personal-work/yokan-projects/winged-bean/build

# 2. Copy Plate plugins (see section "1. Copy Plate Plugins to Artifacts" above)

# 3. Run console in debug mode
cd _artifacts/0.0.1-455/dotnet/console/bin
DEBUG_MINIMAL_UI=1 ./Dungeon.Host.Console

# 4. In another terminal, check logs
cd /Users/apprenticegc/Work/lunar-horse/personal-work/yokan-projects/winged-bean/build
LOG=$(ls -t _artifacts/0.0.1-455/dotnet/console/bin/logs/console-dungeon-*.log 2>/dev/null | head -1)
grep -E "IGoapPlannerService|ICapabilityEvaluator" "$LOG"
```

---

## Success Criteria

Integration is verified when:
1. ✅ Both Plate plugins load without errors
2. ✅ `IGoapPlannerService` is resolved from registry (log message present)
3. ✅ `ICapabilityEvaluator` is resolved from registry (log message present)
4. ✅ Console application runs without plugin-related errors

---

## References

### Project Paths
- **WingedBean**: `/Users/apprenticegc/Work/lunar-horse/personal-work/yokan-projects/winged-bean/`
- **Plate Projects**: `/Users/apprenticegc/Work/lunar-horse/personal-work/plate-projects/`
- **Build Directory**: `winged-bean/build/`
- **Artifacts**: `winged-bean/build/_artifacts/0.0.1-455/dotnet/`

### Key Documentation
- Workspace rules: `lunar-horse/.agent/local/overrides.md`
- Project rules: `winged-bean/.agent/local/overrides.md`
- Build rules: `winged-bean/build/.agent/local/overrides.md` (if exists)

---

**End of Handover Document**

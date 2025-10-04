---
id: RFC-0019
title: Remove Legacy ConsoleDungeon Monolithic Host
status: Implemented
category: architecture, cleanup, console
created: 2025-10-03
updated: 2025-10-03
author: Claude (Sonnet 4.5)
---

# RFC-0019: Remove Legacy ConsoleDungeon Monolithic Host

## Summary

Delete the legacy `console/src/host/ConsoleDungeon/` project and consolidate all console game execution through the plugin-based `ConsoleDungeon.Host/` architecture. This removes technical debt, eliminates architectural violations, and simplifies the codebase.

## Motivation

### Current State: Two Overlapping Executables

The codebase currently has **two separate executable projects** that both run the console dungeon game:

1. **`ConsoleDungeon/`** (Legacy Monolithic)
   - Standalone executable with direct Terminal.Gui coupling
   - Uses service registry but bypasses plugin architecture
   - Violates 4-tier architecture (Contracts → Plugins → Host → Platform)
   - Contains ~500 lines of UI code that duplicates plugin functionality
   - **Just migrated to Terminal.Gui v2 today** (wasted effort if deleted)

2. **`ConsoleDungeon.Host/`** (Correct Plugin-Based) ✅
   - Proper plugin host following RFC-0014
   - Loads `WingedBean.Plugins.ConsoleDungeon` (Terminal.Gui v2 implementation)
   - Loads `WingedBean.Plugins.DungeonGame` (game logic)
   - Clean separation of concerns via service contracts

### Historical Context

**Commit `940bb7e` (2025-10-03)**: "refactor: rely on dungeon plugin for gameplay host"
- Deleted **1,785 lines** from `ConsoleDungeon/`:
  - Removed: Components/, Systems/, DungeonGame.cs, tests
  - Moved game logic to `WingedBean.Plugins.DungeonGame`
  - **Intended to complete the migration but left the host executable**

The refactor was incomplete - `ConsoleDungeon/Program.cs` still exists as a simplified launcher but violates architectural principles.

### Problems with Keeping Both

| Issue | Impact | Severity |
|-------|--------|----------|
| **Architectural violation** | `ConsoleDungeon/` bypasses plugin system | High |
| **Tight coupling** | Direct `Terminal.Gui` imports in host | High |
| **Duplicated functionality** | Two ways to run the same game | Medium |
| **Developer confusion** | Which one should new contributors use? | Medium |
| **Maintenance burden** | Must keep both in sync (e.g., Terminal.Gui v2 migration) | High |
| **Build complexity** | Taskfile/Nuke must handle both projects | Low |

### Why `ConsoleDungeon/` Violates Architecture

**4-Tier Service Architecture (RFC-0002):**
```
Platform (Unity/Godot/Console)
    ↓
Host (ConsoleDungeon.Host)  ← plugin loading, orchestration
    ↓
Plugins (*.Plugins.*)       ← Terminal.Gui v2 implementation
    ↓
Contracts (*.Contracts.*)   ← ITerminalUIService, IRenderService
```

**`ConsoleDungeon/` shortcuts this:**
```
Platform (Console)
    ↓
ConsoleDungeon/             ← ❌ Direct Terminal.Gui imports
    ↓
Contracts                   ← Uses registry but not plugins
```

**Violations:**
- Line 9: `using Terminal.Gui;` ← Host should never import platform libraries
- Line 23-24: `private DungeonView? _dungeonView; private Label? _statsLabel;` ← UI widgets in host
- Line 124-165: `new Window() { ... }`, `new Label()`, `new MenuBar()` ← Platform-specific code
- Line 170: `window.KeyDown += (s, e) => { HandleKeyPress(e); }` ← Input handling in host

**Correct approach (`ConsoleDungeon.Host/`):**
- Host only loads plugins from `plugins.json`
- `WingedBean.Plugins.ConsoleDungeon` contains all Terminal.Gui code
- `WingedBean.Plugins.DungeonGame` contains game logic
- Host orchestrates services via registry (no direct dependencies)

## Proposed Solution

### Phase 1: Verify ConsoleDungeon.Host Completeness ✅

**Already complete:**
- ✅ Plugin-based architecture working (RFC-0014, RFC-0017)
- ✅ `WingedBean.Plugins.ConsoleDungeon` has Terminal.Gui v2 implementation
- ✅ `WingedBean.Plugins.DungeonGame` has game logic
- ✅ `ConsoleDungeon.Host/plugins.json` configured correctly

### Phase 2: Remove ConsoleDungeon/ Project

**Delete:**
```bash
console/src/host/ConsoleDungeon/
├── ConsoleDungeon.csproj
├── Program.cs               # ~295 lines (just migrated to v2)
├── DungeonView.cs           # ~96 lines (Terminal.Gui rendering)
└── appsettings.json
```

**Update solution files:**
```bash
# Remove from:
- console/Console.sln
- WingedBean.sln
```

### Phase 3: Update Build Scripts

**Taskfile changes:**
```yaml
# Before
tasks:
  run:
    cmds:
      - cd build && task build-all && task console:normal

# After
tasks:
  run:
    cmds:
      - cd build && task build-all && task console:normal
```

**Nuke changes:**
```csharp
// Update project references in build/nuke/Build.cs
// Remove ConsoleDungeon from compilation targets
```

### Phase 4: Update Documentation

**Update references in:**
- `README.md` - Quick start instructions
- `docs/design/architecture.md` - Architecture diagrams
- `development/dotnet/README.md` - Developer guide

**Add migration note:**
```markdown
## Migration from Legacy ConsoleDungeon

**Before (deprecated):**
```bash
cd build && task build-all && task console:normal
```

**After:**
```bash
cd build && task build-all && task console:normal
```
```

## Implementation Plan

### Step 1: Pre-deletion verification
1. ✅ Verify `ConsoleDungeon.Host/` builds successfully
2. ✅ Verify `ConsoleDungeon.Host/` runs and loads plugins
3. ✅ Verify all game features work (movement, combat, menus)
4. ✅ Check no external scripts depend on `ConsoleDungeon/` path

### Step 2: Delete legacy project
1. Remove `console/src/host/ConsoleDungeon/` directory
2. Update `console/Console.sln` (remove project reference)
3. Update `WingedBean.sln` (remove project reference)

### Step 3: Update build system
1. Update `console/Taskfile.yml` (change run target)
2. Update `build/Taskfile.yml` (if referenced)
3. Update `build/nuke/Build.cs` (remove compilation target)
4. Update `.github/workflows/*.yml` (if CI references old path)

### Step 4: Update documentation
1. Update `README.md` - Quick start section
2. Update `development/dotnet/README.md` - Developer guide
3. Add migration note to `CHANGELOG.md`

### Step 5: Verification
1. Test `task run` from console directory
2. Test `task build-all` from build directory
3. Test CI pipeline (if applicable)
4. Verify no broken links in documentation

## Migration Guide

### For Users

**Old command:**
```bash
cd development/dotnet
cd build && task build-all && task console:normal
```

**New command:**
```bash
cd development/dotnet
cd build && task build-all && task console:normal
```

**Or using Task:**
```bash
cd development/dotnet/console
task run
```

### For Developers

**No code changes needed** - the plugin-based architecture is already implemented.

**Plugin configuration** (`console/src/host/ConsoleDungeon.Host/plugins.json`):
```json
{
  "plugins": [
    {
      "id": "wingedbean.plugins.dungeongame",
      "path": "../../plugins/WingedBean.Plugins.DungeonGame/bin/Debug/net8.0/WingedBean.Plugins.DungeonGame.dll",
      "priority": 1000,
      "enabled": true
    },
    {
      "id": "wingedbean.plugins.consoledungeon",
      "path": "../../plugins/WingedBean.Plugins.ConsoleDungeon/bin/Debug/net8.0/WingedBean.Plugins.ConsoleDungeon.dll",
      "priority": 900,
      "enabled": true
    }
  ]
}
```

## Benefits

1. **✅ Architectural Compliance**
   - 100% plugin-based architecture (no shortcuts)
   - Clean 4-tier separation maintained
   - Host never imports platform libraries

2. **✅ Reduced Maintenance**
   - Single code path for console game execution
   - No need to sync two implementations
   - Terminal.Gui upgrades only affect plugin

3. **✅ Developer Clarity**
   - Clear entry point: `ConsoleDungeon.Host/`
   - Obvious plugin loading mechanism
   - No confusion about which project to use

4. **✅ Code Quality**
   - Removes ~391 lines of redundant code
   - Eliminates architectural violations
   - Improves testability (plugins can be mocked)

## Risks and Mitigations

| Risk | Mitigation |
|------|------------|
| **Breaking existing workflows** | Update all documentation, add migration guide |
| **CI/CD pipeline breaks** | Test pipeline before merging, update workflows |
| **Lost functionality** | Verify feature parity before deletion |
| **Revert complexity** | Git history preserves deleted code if needed |

## Alternatives Considered

### Alternative 1: Keep Both (Current State)
**Rejected** - Technical debt, architectural violations, maintenance burden

### Alternative 2: Refactor ConsoleDungeon/ to Use Plugins
**Rejected** - More work than deletion, still maintains two code paths

### Alternative 3: Make ConsoleDungeon/ a "Simple Example"
**Rejected** - Would need disclaimer about violating best practices, confusing

## Success Criteria

- [x] `console/src/host/ConsoleDungeon/` deleted
- [x] All solution files updated
- [x] Build system verified (no changes needed - already used ConsoleDungeon.Host)
- [x] Documentation updated with migration guide
- [x] No broken references in codebase

## References

- RFC-0002: 4-Tier Service Architecture
- RFC-0014: Reactive Plugin Architecture for Dungeon Game
- RFC-0017: (Same as RFC-0014, renumbered)
- RFC-0018: Render and UI Services for Console Profile
- Commit `940bb7e`: "refactor: rely on dungeon plugin for gameplay host" (deleted 1,785 lines)

## Timeline

- **Phase 1 (Verification)**: Immediate (already complete)
- **Phase 2 (Deletion)**: 10 minutes
- **Phase 3 (Build Scripts)**: 15 minutes
- **Phase 4 (Documentation)**: 20 minutes
- **Total Estimated Time**: ~45 minutes

## Approval

Awaiting approval to proceed with migration.

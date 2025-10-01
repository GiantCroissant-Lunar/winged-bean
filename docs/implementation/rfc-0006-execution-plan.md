# RFC-0006 Execution Plan: Dynamic Plugin Loading

## Overview
This document outlines GitHub issues for implementing RFC-0006: Dynamic Plugin Loading and Runtime Composition.

## Legend
- 🔴 **CRITICAL PATH** - Must be done before next phase
- 🟢 **PARALLEL** - Can be done simultaneously with other issues
- 🔵 **SERIAL** - Must wait for dependencies

---

## Phase 1: Configuration Infrastructure

### Wave 1.1 (Serial - Models)
```
#49 Create plugin configuration models (1 hr)
   └─ Depends on: RFC-0005 complete (#48)
   └─ PluginConfiguration, PluginDescriptor, LoadStrategy classes
   └─ 🔴 BLOCKS all other Phase 1 work
```

### Wave 1.2 (🟢 PARALLEL - Configuration Files)
After #49 completes:

```
┌─ #50 Create plugins.json for ConsoleDungeon.Host (45 min)
│     └─ Define plugin entries for Config, WebSocket, TerminalUI
│     └─ Add copy-to-output in .csproj
│
└─ #51 Create .plugin.json manifests for all plugins (1 hr)
      └─ Config, WebSocket, TerminalUI, PtyService, AsciinemaRecorder
      └─ Add copy-to-output in each .csproj
```

**Parallelization**: 2 agents
**Time savings**: 1.75 hrs → 1 hr

---

## Phase 2: MSBuild Integration

### Wave 2.1 (Serial - Build Targets)
```
#52 Create copy-plugins.targets MSBuild file (1 hr)
   └─ Depends on: #49 (models exist)
   └─ Copy plugin DLLs/PDBs/manifests to output/plugins/
   └─ Import in ConsoleDungeon.Host.csproj
   └─ 🔴 BLOCKS #55 (testing)
```

---

## Phase 3: Remove Static References

### Wave 3.1 (Serial - Host Project Update)
```
#53 Remove static plugin references from ConsoleDungeon.Host (30 min)
   └─ Depends on: #52 (build targets working)
   └─ Remove ProjectReference entries for plugins
   └─ Keep only Registry, PluginLoader, AssemblyContext
   └─ 🔴 CRITICAL: Host must still build
```

---

## Phase 4: Dynamic Loading Implementation

### Wave 4.1 (Serial - Program.cs Refactor)
```
#54 Implement dynamic plugin loading in Program.cs (2 hrs)
   └─ Depends on: #49, #50, #51, #52, #53
   └─ Load plugins.json configuration
   └─ Use ActualPluginLoader to load plugins dynamically
   └─ Auto-register services from plugins
   └─ Service verification logic
   └─ Error handling for plugin failures
   └─ 🔴 BLOCKS #55 (testing)
```

---

## Phase 5: Testing & Verification

### Wave 5.1 (🔵 SERIAL - Build Test)
```
#55 Verify ConsoleDungeon.Host builds with dynamic loading (30 min)
   └─ Depends on: #54
   └─ Clean build, verify plugins/ directory created
   └─ Verify plugins.json copied to output
   └─ 🔴 MUST PASS before #56
```

### Wave 5.2 (🔵 SERIAL - Runtime Test)
```
#56 🔴 CRITICAL: Verify dynamic plugin loading works (1 hr)
   └─ Depends on: #55
   └─ Run ConsoleDungeon.Host
   └─ Verify all plugins load successfully
   └─ Verify services register correctly
   └─ Verify app launches without errors
   └─ 🔴 MUST PASS before #57
```

### Wave 5.3 (🔵 SERIAL - xterm.js Regression Test)
```
#57 🔴 CRITICAL: Verify xterm.js integration still works (30 min)
   └─ Depends on: #56
   └─ Start ConsoleDungeon.Host
   └─ Start Astro frontend
   └─ Connect via xterm.js
   └─ Verify Terminal.Gui renders correctly
   └─ Verify commands work
   └─ 🔴 MUST PASS before #58
```

---

## Phase 6: Configuration Testing

### Wave 6.1 (Serial - Config Scenarios)
```
#58 Test plugin enable/disable functionality (45 min)
   └─ Depends on: #57
   └─ Disable a plugin in plugins.json
   └─ Verify error message if required service missing
   └─ Re-enable plugin, verify works again
```

### Wave 6.2 (Serial - Priority Testing)
```
#59 Test plugin priority and load order (30 min)
   └─ Depends on: #58
   └─ Change plugin priorities
   └─ Verify load order follows priority (high→low)
   └─ Verify highest priority service selected
```

---

## Phase 7: Documentation & Cleanup

### Wave 7.1 (🟢 PARALLEL - Documentation)
After #59 completes:

```
┌─ #60 Create plugin development guide (1 hr)
│     └─ How to create new plugins
│     └─ Manifest format documentation
│     └─ Registration patterns
│
├─ #61 Update architecture documentation (45 min)
│     └─ Dynamic loading architecture diagram
│     └─ Plugin lifecycle documentation
│     └─ Configuration schema
│
└─ #62 Create plugin configuration migration guide (30 min)
      └─ Migrating from static to dynamic loading
      └─ Troubleshooting common issues
      └─ Best practices
```

**Parallelization**: 3 agents
**Time savings**: 2.25 hrs → 1 hr

---

## Dependency Graph

```
RFC-0005 (#48)
   └─→ #49 (configuration models)
        ├─→ #50 (plugins.json) ─────┐
        ├─→ #51 (manifests) ─────────┤
        └─→ #52 (build targets) ──────┼─→ #53 (remove static refs)
                                      │     └─→ #54 (dynamic loading)
                                      │           └─→ #55 (build test)
                                      │                 └─→ #56 🔴 (runtime test)
                                      │                       └─→ #57 🔴 (xterm test)
                                      │                             └─→ #58 (config test)
                                      │                                   └─→ #59 (priority test)
                                      │                                         ├─→ #60 (dev guide)
                                      │                                         ├─→ #61 (arch docs)
                                      │                                         └─→ #62 (migration)
```

---

## Optimal Execution Strategy

### With 3+ Agents
```
Day 1 Morning:
  Agent 1: #49 (models)

Day 1 Afternoon:
  Agent 1: #50 (plugins.json)
  Agent 2: #51 (manifests)
  Agent 3: Wait for #49

Day 1 Evening:
  Agent 1: #52 (build targets)

Day 2 Morning:
  Agent 1: #53 → #54 (remove refs, dynamic loading)

Day 2 Afternoon:
  Agent 1: #55 → #56 → #57 (verification chain)

Day 3 Morning:
  Agent 1: #58 → #59 (config testing)

Day 3 Afternoon:
  Agent 1-3: #60, #61, #62 (parallel docs)
```
**Total time**: ~3 days (~10 hours)

### Single Agent
```
Day 1: #49 → #50 → #51 → #52 → #53
Day 2: #54 → #55 → #56 → #57
Day 3: #58 → #59 → #60 → #61 → #62
```
**Total time**: ~3 days (~11 hours)

---

## Critical Path Analysis

### Longest Serial Chain
```
#49 (1h) → #50 (45m) → #52 (1h) → #53 (30m) → #54 (2h) →
#55 (30m) → #56 (1h) → #57 (30m) → #58 (45m) → #59 (30m) → #60 (1h)
```
**Critical path total**: ~10 hours

### Bottleneck Issues
1. **#54** Dynamic loading implementation (2 hrs) - Complex logic
2. **#56** Runtime verification (1 hr) - May require debugging
3. **#57** xterm.js regression (30 min) - Critical integration test

### Critical Tests
- **#56** Runtime plugin loading - MUST PASS
- **#57** xterm.js integration - MUST PASS

If either fails, STOP and debug before proceeding.

---

## Issue Count & Time Estimates

| Phase | Issues | Serial Time | Parallel Time (3 agents) |
|-------|--------|-------------|--------------------------|
| Phase 1 | 3 (#49-#51) | 2.75 hrs | 1.75 hrs |
| Phase 2 | 1 (#52) | 1 hr | 1 hr |
| Phase 3 | 1 (#53) | 30 min | 30 min |
| Phase 4 | 1 (#54) | 2 hrs | 2 hrs |
| Phase 5 | 3 (#55-#57) | 2 hrs | 2 hrs |
| Phase 6 | 2 (#58-#59) | 1.25 hrs | 1.25 hrs |
| Phase 7 | 3 (#60-#62) | 2.25 hrs | 1 hr |
| **Total** | **14 issues** | **~11.75 hrs** | **~9.5 hrs** |

**Speedup**: ~20% with 3 agents (limited by serial bottlenecks)

---

## Success Criteria

### Phase 1 Complete When:
- ✅ Configuration models implemented and compile
- ✅ plugins.json created with all current plugins
- ✅ All plugin manifests created

### Phase 2 Complete When:
- ✅ MSBuild targets copy plugins to output directory
- ✅ Clean build produces plugins/ folder with DLLs

### Phase 3 Complete When:
- ✅ Host project has no static plugin references
- ✅ Host still builds successfully
- ✅ Only foundation services referenced

### Phase 4 Complete When:
- ✅ Program.cs uses ActualPluginLoader
- ✅ Plugins load from plugins.json
- ✅ Services auto-register from plugins
- ✅ Error handling implemented

### Phase 5 Complete When:
- ✅ Build succeeds with dynamic loading
- ✅ App runs and loads all plugins
- ✅ xterm.js integration verified working
- ✅ No regressions from RFC-0005

### Phase 6 Complete When:
- ✅ Plugin enable/disable works
- ✅ Priority system works correctly
- ✅ Error messages helpful

### RFC-0006 Complete When:
- ✅ All phases complete
- ✅ Documentation published
- ✅ Plugin development guide available
- ✅ Team can proceed to RFC-0007

---

## Risk Mitigation

### Risk: Plugin Load Failures
**Mitigation:**
- Try-catch around each plugin load
- Continue loading other plugins on failure
- Clear error messages indicating which plugin failed

### Risk: Service Registration Issues
**Mitigation:**
- Verify required services after loading
- Fail fast if critical services missing
- Log all registration events

### Risk: xterm.js Regression
**Mitigation:**
- Keep test environment from RFC-0005
- Document exact test procedure
- Automated test script (future)

---

## Notes

- **Dependencies:** RFC-0005 (must be complete)
- **Blocks:** RFC-0007 (Arch ECS should use dynamic loading)
- **Risk Level:** Medium (runtime behavior changes)
- **Rollback Plan:** Revert to static references if critical tests fail

---

**Created:** 2025-10-01
**RFC:** RFC-0006
**Status:** Ready for Implementation (after RFC-0005)
**Total Issues:** 14 (#49-#62)
**Estimated Effort:** 9.5-11.75 hours (3 days)

# RFC-0005 Execution Plan: Target Framework Compliance

## Overview
This document outlines GitHub issues for implementing RFC-0005: Target Framework Compliance for Multi-Platform Support.

## Legend
- 🔴 **CRITICAL PATH** - Must be done before next phase
- 🟢 **PARALLEL** - Can be done simultaneously with other issues
- 🔵 **SERIAL** - Must wait for dependencies

---

## Phase 1: Tier 1 Contracts Migration (.NET Standard 2.1)

### Wave 1.1 (🟢 PARALLEL - Contract Projects)
After RFC-0005 approval, these can run in parallel:

```
┌─ #23 Update WingedBean.Contracts.Core to netstandard2.1 (30 min)
│     └─ Update .csproj, verify no runtime APIs, build & test
│
├─ #24 Update WingedBean.Contracts.Config to netstandard2.1 (20 min)
│     └─ Update .csproj, verify no System.Text.Json, build & test
│
├─ #25 Update WingedBean.Contracts.Audio to netstandard2.1 (15 min)
│     └─ Update .csproj, build & test
│
├─ #26 Update WingedBean.Contracts.Resource to netstandard2.1 (15 min)
│     └─ Update .csproj, build & test
│
├─ #27 Update WingedBean.Contracts.WebSocket to netstandard2.1 (15 min)
│     └─ Update .csproj, build & test
│
├─ #28 Update WingedBean.Contracts.TerminalUI to netstandard2.1 (15 min)
│     └─ Update .csproj, build & test
│
└─ #29 Update WingedBean.Contracts.Pty to netstandard2.1 (15 min)
      └─ Update .csproj, build & test
```

**Parallelization**: 7 coding agents can work simultaneously
**Time savings**: ~2.5 hours → 30 min if fully parallel

---

## Phase 2: Tier 2 Infrastructure Migration (.NET Standard 2.1)

### Wave 2.1 (Serial - Registry)
```
#30 Update WingedBean.Registry to netstandard2.1 (30 min)
   └─ Depends on: #23 (WingedBean.Contracts.Core)
   └─ Update .csproj, verify no runtime APIs, build & test
```

---

## Phase 3: Tier 3/4 Console Migration (.NET 8.0)

### Wave 3.1 (🟢 PARALLEL - Console Infrastructure)
After Wave 1.1 completes:

```
┌─ #31 Update WingedBean.PluginLoader to net8.0 (15 min)
│     └─ Update .csproj, build & test
│
└─ #32 Update WingedBean.Providers.AssemblyContext to net8.0 (15 min)
      └─ Update .csproj, build & test
```

### Wave 3.2 (🟢 PARALLEL - Plugin Projects)
```
┌─ #33 Update WingedBean.Plugins.Config to net8.0 (15 min)
│     └─ Update .csproj, build & test
│
├─ #34 Update WingedBean.Plugins.WebSocket to net8.0 (15 min)
│     └─ Update .csproj, build & test
│
├─ #35 Update WingedBean.Plugins.TerminalUI to net8.0 (15 min)
│     └─ Update .csproj, build & test
│
├─ #36 Update WingedBean.Plugins.PtyService to net8.0 (15 min)
│     └─ Update .csproj, build & test
│
├─ #37 Update WingedBean.Plugins.AsciinemaRecorder to net8.0 (15 min)
│     └─ Update .csproj, build & test
│
└─ #38 Update WingedBean.Plugins.ConsoleDungeon to net8.0 (15 min)
      └─ Update .csproj, build & test
```

### Wave 3.3 (🟢 PARALLEL - Host Projects)
```
┌─ #39 Update ConsoleDungeon to net8.0 (15 min)
│     └─ Update .csproj, build & test
│
├─ #40 Update ConsoleDungeon.Host to net8.0 (15 min)
│     └─ Update .csproj, build & test
│
├─ #41 Update WingedBean.Host.Console to net8.0 (15 min)
│     └─ Update .csproj, build & test
│
├─ #42 Update TerminalGui.PtyHost to net8.0 (15 min)
│     └─ Update .csproj, build & test
│
└─ #43 Update WingedBean.Demo to net8.0 (15 min)
      └─ Update .csproj, build & test
```

**Parallelization**: Up to 13 agents (Wave 3.1-3.3)
**Time savings**: ~3 hours → 15 min if fully parallel

---

## Phase 4: Source Generator Setup

### Wave 4.1 (Serial - New Project)
```
#44 Create WingedBean.Contracts.SourceGen project (1 hr)
   └─ Depends on: #23 (Core contracts exist)
   └─ Create project, add Roslyn packages, placeholder generator
   └─ 🔴 BLOCKS Framework.sln update
```

---

## Phase 5: Verification & Testing

### Wave 5.1 (🔵 SERIAL - Build Verification)
```
#45 Verify Framework.sln builds (30 min)
   └─ Depends on: All Phase 1-4 issues
   └─ Clean build, run all framework tests
   └─ 🔴 MUST PASS before #46
```

### Wave 5.2 (🔵 SERIAL - Console Build Verification)
```
#46 Verify Console.sln builds (30 min)
   └─ Depends on: #45, All Phase 3 issues
   └─ Clean build, run all console tests
   └─ 🔴 MUST PASS before #47
```

### Wave 5.3 (🔵 SERIAL - Integration Test)
```
#47 🔴 CRITICAL: Verify ConsoleDungeon.Host runs (30 min)
   └─ Depends on: #46
   └─ Run app, verify no errors, test xterm.js integration
   └─ 🔴 MUST PASS before declaring RFC-0005 complete
```

---

## Phase 6: Documentation

### Wave 6.1 (Serial - Docs)
```
#48 Update documentation for framework changes (1 hr)
   └─ Depends on: #47 passing
   └─ Update README, architecture docs, migration guide
```

---

## Dependency Graph

```
Wave 1.1 (Parallel):
  ┌─→ #23 (Contracts.Core) ────────┐
  ├─→ #24 (Contracts.Config)       │
  ├─→ #25 (Contracts.Audio)        │
  ├─→ #26 (Contracts.Resource)     ├─→ #30 (Registry)
  ├─→ #27 (Contracts.WebSocket)    │     └─→ #44 (SourceGen)
  ├─→ #28 (Contracts.TerminalUI)   │           └─→ #45 (Framework build)
  └─→ #29 (Contracts.Pty) ─────────┘

Wave 3 (Parallel):
  ┌─→ #31 (PluginLoader) ──────────┐
  ├─→ #32 (AssemblyContext)        │
  ├─→ #33 (Plugins.Config)         │
  ├─→ #34 (Plugins.WebSocket)      │
  ├─→ #35 (Plugins.TerminalUI)     ├─→ #46 (Console build)
  ├─→ #36 (Plugins.PtyService)     │     └─→ #47 🔴 (Run test)
  ├─→ #37 (Plugins.AsciinemaRec)   │           └─→ #48 (Docs)
  ├─→ #38 (Plugins.ConsoleDungeon) │
  ├─→ #39 (ConsoleDungeon)         │
  ├─→ #40 (ConsoleDungeon.Host)    │
  ├─→ #41 (Host.Console)           │
  ├─→ #42 (PtyHost)                │
  └─→ #43 (Demo) ──────────────────┘
```

---

## Optimal Execution Strategy

### With 7+ Agents (Maximum Parallelization)
```
Day 1 Morning:
  Agent 1-7: #23-#29 (parallel contract updates)
  Agent 8: #30 (registry - waits for #23)

Day 1 Afternoon:
  Agent 1-13: #31-#43 (parallel console updates)
  Agent 14: #44 (source gen project)

Day 2:
  Agent 1: #45 → #46 → #47 → #48 (serial verification)
```
**Total time**: ~4-5 hours

### With 2-3 Agents
```
Day 1:
  Agent 1: #23 → #24 → #25 → #26 → #30 → #44
  Agent 2: #27 → #28 → #29 → #31 → #32
  Agent 3: #33 → #34 → #35 → #36 → #37

Day 2:
  Agent 1: #38 → #39 → #40
  Agent 2: #41 → #42 → #43
  Agent 3: Help with verification

Day 2 Afternoon:
  Agent 1: #45 → #46 → #47 → #48
```
**Total time**: ~2 days

### Single Agent (Sequential)
```
Day 1: #23 → #24 → #25 → #26 → #27 → #28 → #29 → #30 → #44
Day 2: #31 → #32 → #33 → #34 → #35 → #36 → #37 → #38 → #39
Day 3: #40 → #41 → #42 → #43 → #45 → #46 → #47 → #48
```
**Total time**: ~3 days

---

## Critical Path Analysis

### Longest Serial Chain
```
#23 (30m) → #30 (30m) → #44 (1h) → #45 (30m) → #46 (30m) → #47 (30m) → #48 (1h)
```
**Critical path total**: ~5 hours

### Bottleneck Issues
1. **#44** Source Generator Setup (1 hr) - New project creation
2. **#45-#47** Verification chain (1.5 hrs) - Must be serial
3. **#48** Documentation (1 hr) - Requires all prior work

### Critical Test
- **#47** ConsoleDungeon.Host run test - MUST PASS

If fails, STOP and debug framework compatibility issues.

---

## Issue Count & Time Estimates

| Phase | Issues | Serial Time | Parallel Time (7+ agents) |
|-------|--------|-------------|---------------------------|
| Phase 1 | 7 (#23-#29) | 2.5 hrs | 30 min |
| Phase 2 | 1 (#30) | 30 min | 30 min |
| Phase 3 | 13 (#31-#43) | 3 hrs | 15 min |
| Phase 4 | 1 (#44) | 1 hr | 1 hr |
| Phase 5 | 3 (#45-#47) | 1.5 hrs | 1.5 hrs |
| Phase 6 | 1 (#48) | 1 hr | 1 hr |
| **Total** | **26 issues** | **~9.5 hrs** | **~4.75 hrs** |

**Maximum speedup**: ~50% with parallel execution

---

## Success Criteria

### Phase 1-2 Complete When:
- ✅ All Tier 1 contracts target `netstandard2.1`
- ✅ Registry targets `netstandard2.1`
- ✅ Framework.sln builds without errors
- ✅ All framework tests pass

### Phase 3 Complete When:
- ✅ All console projects target `net8.0`
- ✅ Console.sln builds without errors
- ✅ All console tests pass

### Phase 4 Complete When:
- ✅ Source generator project exists
- ✅ Targets `netstandard2.0`
- ✅ Builds successfully

### Phase 5 Complete When:
- ✅ Clean builds succeed for all solutions
- ✅ ConsoleDungeon.Host runs without errors
- ✅ xterm.js integration verified working
- ✅ No runtime framework errors

### RFC-0005 Complete When:
- ✅ All phases complete
- ✅ Documentation updated
- ✅ Migration guide published
- ✅ Team can proceed to RFC-0006

---

## Notes

- **Dependencies:** None (can start immediately)
- **Blocks:** RFC-0006, RFC-0007 (both depend on framework compliance)
- **Risk Level:** Low (mostly mechanical changes)
- **Rollback Plan:** Git revert all changes if #47 fails

---

**Created:** 2025-10-01
**RFC:** RFC-0005
**Status:** Ready for Implementation
**Total Issues:** 26 (#23-#48)
**Estimated Effort:** 4-9 hours (depending on parallelization)

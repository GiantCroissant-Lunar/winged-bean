# RFC-0004 Execution Plan: Parallel vs Serial

## Overview
This document shows which GitHub issues can be executed in parallel vs which must be done serially.

## Legend
- 🔴 **CRITICAL PATH** - Must be done before next phase
- 🟢 **PARALLEL** - Can be done simultaneously with other issues
- 🔵 **SERIAL** - Must wait for dependencies

---

## Phase 1: Framework (Tier 1 & 2)

### Wave 1.1 (Serial - Foundation)
```
#1 Create framework directory structure (5 min)
   └─ 🔴 MUST complete before any other Phase 1 work
```

### Wave 1.2 (Serial - Core Contracts)
```
#2 Create WingedBean.Contracts.Core (1-2 hrs)
   └─ 🔴 MUST complete before #3, #4, #5, #6
```

### Wave 1.3 (🟢 PARALLEL - Service Contracts)
After #2 completes, these can run in parallel:
```
┌─ #3 Create WingedBean.Contracts.Config (45 min)
├─ #4 Create WingedBean.Contracts.Audio (30 min)
├─ #5 Create WingedBean.Contracts.Resource (30 min)
└─ #6 Create WingedBean.Registry (3-4 hrs)
```
**Parallelization**: 3-4 coding agents can work simultaneously
**Time savings**: ~3-4 hours → 1 hour if fully parallel

### Wave 1.4 (Serial - Solution)
```
#7 Create Framework.sln (15 min)
   └─ 🔴 MUST complete after #2, #3, #4, #5, #6
   └─ 🔴 BLOCKS Phase 2
```

### Phase 1 Summary
- **Serial steps**: #1 → #2 → {parallel work} → #7
- **Parallel opportunity**: Issues #3, #4, #5, #6 (Wave 1.3)
- **Total time (serial)**: ~7-9 hours
- **Total time (4 agents)**: ~3-4 hours

---

## Phase 2: Console MVP

### Wave 2.1 (Serial - Structure)
```
#8 Create console directory structure (5 min)
   └─ 🔴 MUST complete before #9, #10
```

### Wave 2.2 (🟢 PARALLEL - Projects)
After #8 completes:
```
┌─ #9 Migrate ConsoleDungeon to console/src/ (10 min)
└─ #10 Create ConsoleDungeon.Host wrapper (20 min)
```
**Note**: #10 references #9, but can be created in parallel if empty ConsoleDungeon folder exists

### Wave 2.3 (Serial - Solution)
```
#11 Create Console.sln (10 min)
   └─ Depends on: #9, #10
```

### Wave 2.4 (🔵 SERIAL - Critical Testing)
```
#12 🔴 CRITICAL: Verify xterm.js integration (30 min)
   └─ Depends on: #11
   └─ 🔴 MUST PASS before #13 or Phase 3
```

### Wave 2.5 (Serial - Documentation)
```
#13 Create console README (20 min)
   └─ Depends on: #12 passing
```

### Phase 2 Summary
- **Serial steps**: #8 → [#9, #10] → #11 → #12 → #13
- **Parallel opportunity**: Issues #9 and #10 (Wave 2.2)
- **Total time (serial)**: ~1.5 hours
- **Total time (2 agents)**: ~1.25 hours
- **Critical blocker**: #12 must pass

---

## Phase 3: Plugin Architecture

### Wave 3.1 (Serial - Contracts)
```
#14 Create console service contracts (Tier 1) (1 hr)
   └─ Depends on: #7 (Framework.sln)
   └─ 🔴 BLOCKS #18, #19
```

### Wave 3.2 (🟢 PARALLEL - Infrastructure)
After #2 (WingedBean.Contracts.Core) and #14:
```
┌─ #16 Create WingedBean.Providers.AssemblyContext (Tier 4) (2-3 hrs)
│     └─ Depends on: #2
│
└─ #15 Create WingedBean.PluginLoader (Tier 3) (3-4 hrs)
      └─ Depends on: #2, #16
```
**Note**: #16 must complete before #15 can finish, but can start in parallel

### Wave 3.3 (🟢 PARALLEL - Service Plugins)
After #14 and #6 (Registry):
```
┌─ #17 Create WingedBean.Plugins.Config (2 hrs)
│     └─ Depends on: #3 (WingedBean.Contracts.Config), #6
│
├─ #18 Create WingedBean.Plugins.WebSocket (3 hrs)
│     └─ Depends on: #14 (console contracts), #6
│
└─ #19 Create WingedBean.Plugins.TerminalUI (3 hrs)
      └─ Depends on: #14 (console contracts), #6
```
**Parallelization**: 3 coding agents can work simultaneously
**Time savings**: 3 hours → 1 hour if fully parallel (limited by slowest: #18 or #19)

### Wave 3.4 (🔵 SERIAL - Integration)
After #15, #17, #18, #19 complete:
```
#20 Refactor ConsoleDungeon to use service registry (2-3 hrs)
   └─ Depends on: #6, #17, #18, #19
   └─ 🔴 BLOCKS #21
```

### Wave 3.5 (Serial - Bootstrap)
```
#21 Update ConsoleDungeon.Host with full bootstrap (2 hrs)
   └─ Depends on: #6, #15, #20
   └─ 🔴 BLOCKS #22
```

### Wave 3.6 (🔵 SERIAL - Critical Testing)
```
#22 🔴 CRITICAL: Verify xterm.js regression test (30 min)
   └─ Depends on: #21
   └─ 🔴 MUST PASS before declaring Phase 3 complete
```

### Phase 3 Summary
- **Serial spine**: #14 → #20 → #21 → #22
- **Parallel opportunities**:
  - Wave 3.2: #16 and #15 (partial overlap)
  - Wave 3.3: #17, #18, #19 (3 agents)
- **Total time (serial)**: ~18-21 hours
- **Total time (4 agents)**: ~10-12 hours
- **Critical blocker**: #22 must pass

---

## Optimal Execution Strategy

### Phase 1: 4 Agents
```
Agent 1: #1 → #2 → #3 → (help #7)
Agent 2: (wait for #2) → #4 → (help #7)
Agent 3: (wait for #2) → #5 → (help #7)
Agent 4: (wait for #2) → #6 → #7
```
**Time**: ~3-4 hours (vs 7-9 hours serial)

### Phase 2: 2 Agents
```
Agent 1: #8 → #9 → #11 → #12 → #13
Agent 2: (wait for #8) → #10 → (help #11)
```
**Time**: ~1.25 hours (vs 1.5 hours serial)

### Phase 3: 5 Agents
```
Agent 1: #14 → #20 → #21 → #22
Agent 2: #16 → #15 → (help #21)
Agent 3: (wait for #14) → #17 → (help #20)
Agent 4: (wait for #14) → #18 → (help #20)
Agent 5: (wait for #14) → #19 → (help #20)
```
**Time**: ~10-12 hours (vs 18-21 hours serial)

---

## Critical Path Analysis

### Longest Serial Chain
```
#1 (5m) → #2 (1-2h) → #6 (3-4h) → #7 (15m) →
#8 (5m) → #11 (10m) → #12 (30m) → #13 (20m) →
#14 (1h) → #20 (2-3h) → #21 (2h) → #22 (30m)
```
**Critical path total**: ~11-14 hours

### Bottleneck Issues (Slowest Tasks)
1. **#6** WingedBean.Registry (3-4 hrs) - Blocks Framework.sln
2. **#15** WingedBean.PluginLoader (3-4 hrs) - Complex orchestration
3. **#18** WingedBean.Plugins.WebSocket (3 hrs) - Extract from existing code
4. **#19** WingedBean.Plugins.TerminalUI (3 hrs) - Extract from existing code
5. **#20** Refactor ConsoleDungeon (2-3 hrs) - Integration work

### Critical Tests (Cannot Fail)
- **#12** Phase 2 MVP xterm.js verification
- **#22** Phase 3 xterm.js regression test

If either fails, STOP and fix before proceeding.

---

## Dependency Graph (Visual)

```
Phase 1:
  #1 (foundation)
   └─→ #2 (core contracts)
        ├─→ #3 (config contracts) ─┐
        ├─→ #4 (audio contracts)   ├─→ #7 (Framework.sln)
        ├─→ #5 (resource contracts)│
        └─→ #6 (registry) ─────────┘

Phase 2:
  #7 (Framework.sln)
   └─→ #8 (console structure)
        ├─→ #9 (migrate ConsoleDungeon) ─┐
        └─→ #10 (create Host wrapper)    ├─→ #11 (Console.sln)
                                          │     └─→ #12 🔴 (xterm test)
                                          │          └─→ #13 (README)
Phase 3:
  ┌─→ #14 (console contracts)
  │    ├─→ #18 (WebSocket plugin) ─┐
  │    └─→ #19 (TerminalUI plugin)  │
  │                                  ├─→ #20 (refactor ConsoleDungeon)
  ├─→ #16 (AssemblyContext provider) │     └─→ #21 (update Host bootstrap)
  │    └─→ #15 (PluginLoader)       │          └─→ #22 🔴 (regression test)
  │                                  │
  └─→ #17 (Config plugin) ──────────┘
```

---

## Recommendations

### For Maximum Speed (5 agents available)
1. **Phase 1**: Run #3, #4, #5, #6 in parallel after #2 completes
2. **Phase 2**: Run #9, #10 in parallel after #8 completes
3. **Phase 3**: Run #17, #18, #19 in parallel after #14 completes

**Total time**: ~14-17 hours
**vs Serial**: ~27-31.5 hours
**Speedup**: ~45% faster

### For Limited Resources (2 agents)
1. Prioritize critical path issues first
2. One agent on #6 (Registry), another on #3 → #4 → #5
3. Both agents on testing (#12, #22)

**Total time**: ~22-25 hours
**vs Serial**: ~27-31.5 hours
**Speedup**: ~20% faster

### For Single Agent (Sequential)
Follow the wave order exactly as listed above.

**Total time**: ~27-31.5 hours

---

## Summary Table

| Phase | Serial Time | 2 Agents | 4-5 Agents | Critical Blockers |
|-------|-------------|----------|------------|-------------------|
| Phase 1 | 7-9 hrs | 5-6 hrs | 3-4 hrs | #2, #7 |
| Phase 2 | 1.5 hrs | 1.25 hrs | 1.25 hrs | #12 🔴 |
| Phase 3 | 18-21 hrs | 15-16 hrs | 10-12 hrs | #22 🔴 |
| **Total** | **27-31.5 hrs** | **21-23 hrs** | **14-17 hrs** | - |

**Maximum speedup**: ~45% with 5 agents working in parallel

---

**Last Updated**: 2025-09-30
**Author**: Ray Wang (with Claude AI assistance)

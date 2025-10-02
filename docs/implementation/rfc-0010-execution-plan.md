# RFC-0010 Execution Plan: Multi-Language Build Orchestration with Task

## Overview
This document shows which GitHub issues can be executed in parallel vs which must be done serially for implementing Task-based build orchestration with GitVersion integration.

## Legend
- 🔴 **CRITICAL PATH** - Must be done before next phase
- 🟢 **PARALLEL** - Can be done simultaneously with other issues
- 🔵 **SERIAL** - Must wait for dependencies

---

## Phase 1: Task Setup & Directory Structure

### Wave 1.1 (Serial - Foundation)
```
#TBD-1 Install Task and verify cross-platform compatibility (15 min)
   └─ 🔴 MUST complete before any build orchestration work
```

### Wave 1.2 (Serial - Directory Structure)
```
#TBD-2 Create build artifacts directory structure (10 min)
   └─ 🔴 MUST complete before #3, #4
```

### Wave 1.3 (Serial - GitIgnore)
```
#TBD-3 Update build/.gitignore for Task artifacts (5 min)
   └─ Depends on: #TBD-2
```

### Wave 1.4 (Serial - Base Taskfile)
```
#TBD-4 Create build/Taskfile.yml with GitVersion integration (30 min)
   └─ Depends on: #TBD-1, #TBD-2
   └─ 🔴 BLOCKS Phase 2
```

### Phase 1 Summary
- **Serial steps**: #TBD-1 → #TBD-2 → #TBD-3 → #TBD-4
- **No parallel opportunities** (foundational tasks)
- **Total time**: ~1 hour
- **Critical blocker**: #TBD-4 (base Taskfile must work)

---

## Phase 2: Build Task Integration

### Wave 2.1 (Serial - .NET Integration)
```
#TBD-5 Create build-dotnet task wrapping Nuke (30 min)
   └─ Depends on: #TBD-4
   └─ 🔴 BLOCKS #TBD-8 (build-all)
```

### Wave 2.2 (🟢 PARALLEL - Node.js Integration)
After #TBD-4 completes, these can run in parallel:
```
┌─ #TBD-6 Create build-web task for web builds (20 min)
│     └─ Depends on: #TBD-4
│
└─ #TBD-7 Create build-pty task for PTY service builds (20 min)
      └─ Depends on: #TBD-4
```
**Parallelization**: 2 coding agents can work simultaneously
**Time savings**: 40 min → 20 min if fully parallel

### Wave 2.3 (Serial - Orchestration)
```
#TBD-8 Create build-all orchestration task (15 min)
   └─ Depends on: #TBD-5, #TBD-6, #TBD-7
   └─ 🔴 BLOCKS #TBD-9
```

### Wave 2.4 (🔵 SERIAL - Critical Testing)
```
#TBD-9 🔴 CRITICAL: Verify build-all produces versioned artifacts (30 min)
   └─ Depends on: #TBD-8
   └─ 🔴 MUST PASS before Phase 3

   Definition of Done:
   - Run `task build:build-all`
   - Verify `build/_artifacts/v{GitVersion}/` exists
   - Verify dotnet/bin/ contains binaries
   - Verify web/dist/ contains bundled assets
   - Verify pty/dist/ contains compiled service
   - Verify _logs/ contains build logs
   - GitVersion displays correct semantic version
```

### Phase 2 Summary
- **Serial spine**: #TBD-5 → #TBD-8 → #TBD-9
- **Parallel opportunity**: #TBD-6, #TBD-7 (Wave 2.2)
- **Total time (serial)**: ~2 hours
- **Total time (2 agents)**: ~1.5 hours
- **Critical blocker**: #TBD-9 must pass

---

## Phase 3: Root-Level Orchestration

### Wave 3.1 (Serial - Root Taskfile)
```
#TBD-10 Create root Taskfile.yml with includes (20 min)
   └─ Depends on: #TBD-4 (build/Taskfile.yml)
   └─ 🔴 BLOCKS #TBD-11, #TBD-12
```

### Wave 3.2 (🟢 PARALLEL - Module Taskfiles)
After #TBD-10 completes:
```
┌─ #TBD-11 Create development/nodejs/Taskfile.yml (30 min)
│     └─ Depends on: #TBD-10
│     └─ Tasks: install, build, test, test-e2e, lint, format, clean
│
└─ #TBD-12 Create src/ConsoleDungeon/Taskfile.yml (20 min)
      └─ Depends on: #TBD-10
      └─ Tasks: build, test, run, clean
```
**Parallelization**: 2 coding agents can work simultaneously
**Time savings**: 50 min → 30 min if fully parallel

### Wave 3.3 (Serial - CI Integration)
```
#TBD-13 Create CI task in root Taskfile (15 min)
   └─ Depends on: #TBD-10, #TBD-11, #TBD-12
   └─ 🔴 BLOCKS #TBD-14
```

### Wave 3.4 (🔵 SERIAL - Final Verification)
```
#TBD-14 🔴 CRITICAL: Verify full CI pipeline (30 min)
   └─ Depends on: #TBD-13
   └─ 🔴 MUST PASS before declaring RFC-0010 complete

   Definition of Done:
   - Run `task --list` shows all tasks from all modules
   - Run `task setup` successfully installs dependencies
   - Run `task ci` successfully builds all components
   - Verify cross-platform: Windows, macOS, or Linux
   - All tests pass (dotnet, nodejs unit, e2e)
   - Versioned artifacts created in build/_artifacts/
```

### Phase 3 Summary
- **Serial spine**: #TBD-10 → #TBD-13 → #TBD-14
- **Parallel opportunity**: #TBD-11, #TBD-12 (Wave 3.2)
- **Total time (serial)**: ~2 hours
- **Total time (2 agents)**: ~1.5 hours
- **Critical blocker**: #TBD-14 must pass

---

## Phase 4: Runtime Artifacts Integration (Optional - Future)

### Wave 4.1 (Serial - Recording Integration)
```
#TBD-15 Update console app to output recordings to versioned artifacts (1 hr)
   └─ Depends on: #TBD-14
   └─ Modify ConsoleDungeon.Host to use build/_artifacts/v{GitVersion}/dotnet/recordings/
```

### Wave 4.2 (Serial - Log Integration)
```
#TBD-16 Update logging to output to versioned artifacts (45 min)
   └─ Depends on: #TBD-15
   └─ Configure log paths: build/_artifacts/v{GitVersion}/{component}/logs/
```

### Wave 4.3 (Serial - Verification)
```
#TBD-17 Verify runtime artifacts are properly archived (20 min)
   └─ Depends on: #TBD-16
   └─ Run app, verify recordings/logs appear in correct versioned folders
```

### Phase 4 Summary
- **Serial steps**: #TBD-15 → #TBD-16 → #TBD-17
- **No parallel opportunities** (sequential integration work)
- **Total time**: ~2 hours
- **Optional**: Can be deferred to separate RFC/issue

---

## Optimal Execution Strategy

### Phase 1: 1 Agent (No Parallelization)
```
Agent 1: #TBD-1 → #TBD-2 → #TBD-3 → #TBD-4
```
**Time**: ~1 hour

### Phase 2: 2 Agents
```
Agent 1: #TBD-5 → #TBD-8 → #TBD-9
Agent 2: (wait for #TBD-4) → #TBD-6 → #TBD-7 → (help #TBD-8)
```
**Time**: ~1.5 hours (vs 2 hours serial)

### Phase 3: 2 Agents
```
Agent 1: #TBD-10 → #TBD-11 → #TBD-13 → #TBD-14
Agent 2: (wait for #TBD-10) → #TBD-12 → (help #TBD-13)
```
**Time**: ~1.5 hours (vs 2 hours serial)

### Phase 4 (Optional): 1 Agent
```
Agent 1: #TBD-15 → #TBD-16 → #TBD-17
```
**Time**: ~2 hours

---

## Critical Path Analysis

### Longest Serial Chain (Phases 1-3)
```
#TBD-1 (15m) → #TBD-2 (10m) → #TBD-3 (5m) → #TBD-4 (30m) →
#TBD-5 (30m) → #TBD-8 (15m) → #TBD-9 (30m) →
#TBD-10 (20m) → #TBD-13 (15m) → #TBD-14 (30m)
```
**Critical path total**: ~3.5 hours

### Bottleneck Issues (Slowest Tasks)
1. **#TBD-4** Create build/Taskfile.yml (30 min) - Foundation for all builds
2. **#TBD-5** .NET integration (30 min) - Wrapping Nuke
3. **#TBD-9** Build verification (30 min) - Critical test
4. **#TBD-11** Node.js Taskfile (30 min) - Most complex module
5. **#TBD-14** CI verification (30 min) - Critical test

### Critical Tests (Cannot Fail)
- **#TBD-9** Build-all verification - Ensures artifacts structure works
- **#TBD-14** Full CI pipeline - Ensures end-to-end orchestration works

If either fails, STOP and fix before proceeding.

---

## Dependency Graph (Visual)

```
Phase 1:
  #TBD-1 (install Task)
   └─→ #TBD-2 (create directories)
        └─→ #TBD-3 (update gitignore)
             └─→ #TBD-4 (base Taskfile)

Phase 2:
  #TBD-4 (base Taskfile)
   ├─→ #TBD-5 (.NET task) ────────┐
   ├─→ #TBD-6 (web task) ─────────├─→ #TBD-8 (build-all)
   └─→ #TBD-7 (pty task) ─────────┘     └─→ #TBD-9 🔴 (verify)

Phase 3:
  #TBD-4 (base Taskfile)
   └─→ #TBD-10 (root Taskfile)
        ├─→ #TBD-11 (nodejs Taskfile) ─┐
        └─→ #TBD-12 (game Taskfile) ───├─→ #TBD-13 (CI task)
                                        │     └─→ #TBD-14 🔴 (verify CI)

Phase 4 (Optional):
  #TBD-14 (verify CI)
   └─→ #TBD-15 (recording integration)
        └─→ #TBD-16 (log integration)
             └─→ #TBD-17 (verify runtime artifacts)
```

---

## Recommendations

### For Maximum Speed (2 agents available)
1. **Phase 1**: Single agent (no parallelization)
2. **Phase 2**: Run #TBD-6, #TBD-7 in parallel after #TBD-4
3. **Phase 3**: Run #TBD-11, #TBD-12 in parallel after #TBD-10

**Total time (Phases 1-3)**: ~4 hours
**vs Serial**: ~5 hours
**Speedup**: ~20% faster

### For Single Agent (Sequential)
Follow the wave order exactly as listed above.

**Total time (Phases 1-3)**: ~5 hours

### For Including Phase 4 (Runtime Artifacts)
Add 2 hours for single agent sequential work.

**Total time (Phases 1-4)**: ~6-7 hours

---

## Summary Table

| Phase | Serial Time | 2 Agents | Critical Blockers |
|-------|-------------|----------|-------------------|
| Phase 1 | 1 hr | 1 hr | #TBD-4 |
| Phase 2 | 2 hrs | 1.5 hrs | #TBD-9 🔴 |
| Phase 3 | 2 hrs | 1.5 hrs | #TBD-14 🔴 |
| Phase 4 (opt) | 2 hrs | 2 hrs | #TBD-17 |
| **Total (1-3)** | **5 hrs** | **4 hrs** | - |
| **Total (1-4)** | **7 hrs** | **6 hrs** | - |

**Maximum speedup**: ~20% with 2 agents working in parallel

---

## GitHub Issues (Ready to Create)

### Issue #TBD-1: Install Task and verify cross-platform compatibility
**Labels**: `infra`, `tooling`, `task-orchestration`
**Estimate**: 15 minutes
**Priority**: P1 - Critical Path

**Description:**
Install Task (Taskfile.dev) and verify it works cross-platform.

**Acceptance Criteria:**
- [ ] Install Task via appropriate method for platform (brew/scoop/npm)
- [ ] Run `task --version` successfully
- [ ] Verify Task binary is in PATH
- [ ] Document installation method in issue comments

**Definition of Done:**
- Task installed and executable
- Version verified (v3.x or higher)
- Installation method documented

---

### Issue #TBD-2: Create build artifacts directory structure
**Labels**: `infra`, `tooling`, `task-orchestration`
**Estimate**: 10 minutes
**Priority**: P1 - Critical Path
**Depends on**: #TBD-1

**Description:**
Create the versioned build artifacts directory structure as defined in RFC-0010.

**Acceptance Criteria:**
- [ ] Create `build/_artifacts/` directory
- [ ] Create example version folder: `build/_artifacts/v0.1.0-example/`
- [ ] Create subdirectories: `dotnet/bin/`, `dotnet/recordings/`, `dotnet/logs/`
- [ ] Create subdirectories: `web/dist/`, `web/recordings/`, `web/logs/`
- [ ] Create subdirectories: `pty/dist/`, `pty/logs/`
- [ ] Create subdirectory: `_logs/` for build-time logs
- [ ] Remove example folder after structure validation

**Definition of Done:**
- All directories exist as per RFC-0010 specification
- Structure matches: `build/_artifacts/v{version}/{component}/{type}/`
- Example folder cleaned up

---

### Issue #TBD-3: Update build/.gitignore for Task artifacts
**Labels**: `infra`, `tooling`, `task-orchestration`
**Estimate**: 5 minutes
**Priority**: P2
**Depends on**: #TBD-2

**Description:**
Update `build/.gitignore` to exclude Task cache and build artifacts.

**Acceptance Criteria:**
- [ ] Add `.task/` to `build/.gitignore` (Task checksum cache)
- [ ] Add `_artifacts/*/` to `build/.gitignore` (versioned build outputs)
- [ ] Add comment explaining each ignore pattern
- [ ] Commit the updated `.gitignore`

**Definition of Done:**
- `build/.gitignore` contains `.task/` and `_artifacts/*/`
- Git status shows artifacts are ignored
- Comments explain why each pattern is ignored

**File to modify:**
```
build/.gitignore
```

**Expected content:**
```gitignore
# Task checksum cache
.task/

# Build artifacts (versioned outputs - too large for git)
_artifacts/*/

# Allow selective commits for critical recordings if needed
# !_artifacts/v1.0.0/dotnet/recordings/critical-bug.cast
```

---

### Issue #TBD-4: Create build/Taskfile.yml with GitVersion integration
**Labels**: `infra`, `tooling`, `task-orchestration`, `gitversion`
**Estimate**: 30 minutes
**Priority**: P1 - Critical Path
**Depends on**: #TBD-1, #TBD-2

**Description:**
Create the base `build/Taskfile.yml` with GitVersion integration and directory initialization tasks.

**Acceptance Criteria:**
- [ ] Create `build/Taskfile.yml` with version 3 schema
- [ ] Add `VERSION` variable using `dotnet gitversion /showvariable SemVer`
- [ ] Add `FULL_VERSION` variable using `dotnet gitversion /showvariable FullSemVer`
- [ ] Add `ARTIFACT_DIR` variable: `_artifacts/v{{.VERSION}}`
- [ ] Create `version` task to display GitVersion output
- [ ] Create `init-dirs` task to create versioned artifact directories
- [ ] Create `clean` task to remove `_artifacts/*` and `.task/`
- [ ] Test `task version` displays correct semantic version
- [ ] Test `task init-dirs` creates proper directory structure

**Definition of Done:**
- `build/Taskfile.yml` exists with all required tasks
- `task version` displays GitVersion output
- `task init-dirs` creates `_artifacts/v{GitVersion}/` with all subdirectories
- `task clean` removes artifacts and cache
- GitVersion integration works correctly

**File to create:**
```yaml
# build/Taskfile.yml
version: '3'

vars:
  VERSION:
    sh: dotnet gitversion /showvariable SemVer
  FULL_VERSION:
    sh: dotnet gitversion /showvariable FullSemVer
  ARTIFACT_DIR: _artifacts/v{{.VERSION}}

tasks:
  version:
    desc: "Show current GitVersion"
    cmds:
      - echo "Version: {{.VERSION}}"
      - echo "Full Version: {{.FULL_VERSION}}"

  init-dirs:
    desc: "Initialize build artifact directories"
    cmds:
      - mkdir -p {{.ARTIFACT_DIR}}/dotnet/{bin,recordings,logs}
      - mkdir -p {{.ARTIFACT_DIR}}/web/{dist,recordings,logs}
      - mkdir -p {{.ARTIFACT_DIR}}/pty/{dist,logs}
      - mkdir -p {{.ARTIFACT_DIR}}/_logs

  clean:
    desc: "Clean build artifacts and Task cache"
    cmds:
      - rm -rf _artifacts/* .task/
```

---

### Issue #TBD-5: Create build-dotnet task wrapping Nuke
**Labels**: `infra`, `tooling`, `task-orchestration`, `dotnet`
**Estimate**: 30 minutes
**Priority**: P1 - Critical Path
**Depends on**: #TBD-4

**Description:**
Create a Task task that wraps the Nuke build system for .NET compilation and copies outputs to versioned artifacts.

**Acceptance Criteria:**
- [ ] Add `build-dotnet` task to `build/Taskfile.yml`
- [ ] Task depends on `init-dirs`
- [ ] Task calls `./nuke/build.sh Compile` (or equivalent for platform)
- [ ] Pipe build output to `{{.ARTIFACT_DIR}}/_logs/dotnet-build.log`
- [ ] Copy compiled binaries to `{{.ARTIFACT_DIR}}/dotnet/bin/`
- [ ] Test `task build-dotnet` successfully builds and copies outputs
- [ ] Verify logs are captured in `_logs/dotnet-build.log`

**Definition of Done:**
- `task build-dotnet` builds .NET solution via Nuke
- Binaries copied to `build/_artifacts/v{version}/dotnet/bin/`
- Build logs saved to `build/_artifacts/v{version}/_logs/dotnet-build.log`
- Task works on current platform (Windows/macOS/Linux)

**Code to add to build/Taskfile.yml:**
```yaml
  build-dotnet:
    desc: "Build .NET projects via Nuke"
    deps: [init-dirs]
    cmds:
      - ./nuke/build.sh Compile 2>&1 | tee {{.ARTIFACT_DIR}}/_logs/dotnet-build.log
      # Adjust copy based on actual Nuke output location
      - cp -r ../bin/Release/net8.0/* {{.ARTIFACT_DIR}}/dotnet/bin/ || true
```

---

### Issue #TBD-6: Create build-web task for web builds
**Labels**: `infra`, `tooling`, `task-orchestration`, `nodejs`
**Estimate**: 20 minutes
**Priority**: P2
**Depends on**: #TBD-4

**Description:**
Create a Task task for building web projects and copying outputs to versioned artifacts.

**Acceptance Criteria:**
- [ ] Add `build-web` task to `build/Taskfile.yml`
- [ ] Task depends on `init-dirs`
- [ ] Task changes directory to `../development/nodejs/web` (or equivalent)
- [ ] Task runs `pnpm run build` (or `npm run build`)
- [ ] Pipe build output to `{{.ARTIFACT_DIR}}/_logs/web-build.log`
- [ ] Copy `dist/` to `{{.ARTIFACT_DIR}}/web/dist/`
- [ ] Test `task build-web` successfully builds and copies outputs

**Definition of Done:**
- `task build-web` builds web project
- Bundled assets copied to `build/_artifacts/v{version}/web/dist/`
- Build logs saved to `build/_artifacts/v{version}/_logs/web-build.log`

**Code to add to build/Taskfile.yml:**
```yaml
  build-web:
    desc: "Build web projects"
    deps: [init-dirs]
    dir: ../development/nodejs/web
    cmds:
      - pnpm run build 2>&1 | tee ../../build/{{.ARTIFACT_DIR}}/_logs/web-build.log
      - cp -r dist/* ../../build/{{.ARTIFACT_DIR}}/web/dist/
```

---

### Issue #TBD-7: Create build-pty task for PTY service builds
**Labels**: `infra`, `tooling`, `task-orchestration`, `nodejs`
**Estimate**: 20 minutes
**Priority**: P2
**Depends on**: #TBD-4

**Description:**
Create a Task task for building PTY service and copying outputs to versioned artifacts.

**Acceptance Criteria:**
- [ ] Add `build-pty` task to `build/Taskfile.yml`
- [ ] Task depends on `init-dirs`
- [ ] Task changes directory to `../development/nodejs/pty-service`
- [ ] Task runs `pnpm run build` (or `npm run build`) if build script exists
- [ ] If no build script, copy source files to dist
- [ ] Pipe output to `{{.ARTIFACT_DIR}}/_logs/pty-build.log`
- [ ] Copy `dist/` or `src/` to `{{.ARTIFACT_DIR}}/pty/dist/`
- [ ] Test `task build-pty` successfully builds and copies outputs

**Definition of Done:**
- `task build-pty` builds PTY service
- Service files copied to `build/_artifacts/v{version}/pty/dist/`
- Build logs saved to `build/_artifacts/v{version}/_logs/pty-build.log`

**Code to add to build/Taskfile.yml:**
```yaml
  build-pty:
    desc: "Build PTY service"
    deps: [init-dirs]
    dir: ../development/nodejs/pty-service
    cmds:
      # If build script exists, use it; otherwise copy source
      - pnpm run build 2>&1 | tee ../../../build/{{.ARTIFACT_DIR}}/_logs/pty-build.log || cp -r src/* ../../../build/{{.ARTIFACT_DIR}}/pty/dist/
```

---

### Issue #TBD-8: Create build-all orchestration task
**Labels**: `infra`, `tooling`, `task-orchestration`
**Estimate**: 15 minutes
**Priority**: P1 - Critical Path
**Depends on**: #TBD-5, #TBD-6, #TBD-7

**Description:**
Create a unified `build-all` task that orchestrates building all components.

**Acceptance Criteria:**
- [ ] Add `build-all` task to `build/Taskfile.yml`
- [ ] Task has dependencies: `build-dotnet`, `build-web`, `build-pty`
- [ ] Task executes all builds (can be parallel or sequential)
- [ ] Test `task build-all` builds all components successfully
- [ ] Verify all artifacts are created in correct locations

**Definition of Done:**
- `task build-all` orchestrates all component builds
- All artifacts appear in `build/_artifacts/v{version}/`
- Build completes successfully end-to-end

**Code to add to build/Taskfile.yml:**
```yaml
  build-all:
    desc: "Build all components"
    deps:
      - build-dotnet
      - build-web
      - build-pty
    cmds:
      - echo "All builds completed for version {{.VERSION}}"
```

---

### Issue #TBD-9: 🔴 CRITICAL: Verify build-all produces versioned artifacts
**Labels**: `infra`, `tooling`, `task-orchestration`, `testing`, `critical`
**Estimate**: 30 minutes
**Priority**: P1 - Critical Path
**Depends on**: #TBD-8

**Description:**
Verify that the complete build pipeline produces correctly structured versioned artifacts.

**Acceptance Criteria:**
- [ ] Run `task clean` to start fresh
- [ ] Run `task version` and record the version number
- [ ] Run `task build-all` and verify success
- [ ] Verify `build/_artifacts/v{GitVersion}/` directory exists
- [ ] Verify `dotnet/bin/` contains compiled binaries
- [ ] Verify `web/dist/` contains bundled web assets
- [ ] Verify `pty/dist/` contains PTY service files
- [ ] Verify `_logs/` contains build logs (dotnet-build.log, web-build.log, pty-build.log)
- [ ] Verify GitVersion displays correct semantic version
- [ ] Run `task clean` and verify artifacts are removed
- [ ] Document verification results in issue comments

**Definition of Done:**
- ✅ All artifacts created in correct structure
- ✅ GitVersion integration works
- ✅ Build logs captured
- ✅ Clean task works correctly
- ✅ Screenshot/output of successful build attached to issue

**This is a CRITICAL TEST - if it fails, stop and fix before proceeding to Phase 3**

---

### Issue #TBD-10: Create root Taskfile.yml with includes
**Labels**: `infra`, `tooling`, `task-orchestration`
**Estimate**: 20 minutes
**Priority**: P1 - Critical Path
**Depends on**: #TBD-4

**Description:**
Create the root `Taskfile.yml` that includes build tasks and sets up for future module includes.

**Acceptance Criteria:**
- [ ] Create `Taskfile.yml` at project root
- [ ] Use version 3 schema
- [ ] Include `build/Taskfile.yml` with namespace `build:`
- [ ] Create `default` task that runs `task --list`
- [ ] Create `setup` task that calls `build:version` and `build:init-dirs`
- [ ] Test `task --list` shows tasks from root and build module
- [ ] Test `task setup` successfully initializes project

**Definition of Done:**
- Root `Taskfile.yml` exists
- `task --list` shows namespaced tasks (`build:version`, etc.)
- `task setup` works correctly
- Module inclusion pattern established

**File to create:**
```yaml
# Taskfile.yml (root)
version: '3'

includes:
  build:
    taskfile: ./build/Taskfile.yml
    dir: ./build

tasks:
  default:
    desc: "Show available tasks"
    cmds:
      - task --list

  setup:
    desc: "Initial project setup"
    cmds:
      - task: build:version
      - task: build:init-dirs
      - echo "Setup complete!"
```

---

### Issue #TBD-11: Create development/nodejs/Taskfile.yml
**Labels**: `infra`, `tooling`, `task-orchestration`, `nodejs`
**Estimate**: 30 minutes
**Priority**: P2
**Depends on**: #TBD-10

**Description:**
Create a Taskfile for Node.js development tasks (install, build, test, lint, etc.).

**Acceptance Criteria:**
- [ ] Create `development/nodejs/Taskfile.yml`
- [ ] Add `install` task: `pnpm install` (or `npm install`)
- [ ] Add `build` task: `pnpm run build`
- [ ] Add `test` task: `pnpm test`
- [ ] Add `test-e2e` task: `pnpm run test:e2e`
- [ ] Add `lint` task: `pnpm run lint`
- [ ] Add `format` task: `pnpm run format`
- [ ] Add `dev` task: `pnpm run dev` (development server)
- [ ] Add `clean` task: `rm -rf node_modules/ dist/`
- [ ] Update root Taskfile to include nodejs module
- [ ] Test `task nodejs:install`, `task nodejs:build`, etc. work

**Definition of Done:**
- `development/nodejs/Taskfile.yml` exists with all tasks
- Root Taskfile includes nodejs module
- All nodejs tasks are accessible via `task nodejs:{task}`

**File to create:**
```yaml
# development/nodejs/Taskfile.yml
version: '3'

vars:
  PKG_MANAGER: pnpm

tasks:
  install:
    desc: "Install Node.js dependencies"
    sources:
      - package.json
      - pnpm-lock.yaml
    generates:
      - node_modules/.pnpm/lock.yaml
    cmds:
      - '{{.PKG_MANAGER}} install'

  build:
    desc: "Build Node.js projects"
    deps: [install]
    cmds:
      - '{{.PKG_MANAGER}} run build'

  test:
    desc: "Run Node.js unit tests"
    deps: [install]
    cmds:
      - '{{.PKG_MANAGER}} test'

  test-e2e:
    desc: "Run Playwright E2E tests"
    deps: [install]
    cmds:
      - '{{.PKG_MANAGER}} run test:e2e'

  lint:
    desc: "Lint JavaScript/TypeScript"
    deps: [install]
    cmds:
      - '{{.PKG_MANAGER}} run lint'

  format:
    desc: "Format code with Prettier"
    deps: [install]
    cmds:
      - '{{.PKG_MANAGER}} run format'

  dev:
    desc: "Start development server"
    deps: [install]
    cmds:
      - '{{.PKG_MANAGER}} run dev'

  clean:
    desc: "Clean Node.js artifacts"
    cmds:
      - rm -rf node_modules/ dist/
```

**Update root Taskfile.yml:**
```yaml
includes:
  build:
    taskfile: ./build/Taskfile.yml
    dir: ./build
  nodejs:
    taskfile: ./development/nodejs/Taskfile.yml
    dir: ./development/nodejs
```

---

### Issue #TBD-12: Create src/ConsoleDungeon/Taskfile.yml
**Labels**: `infra`, `tooling`, `task-orchestration`, `dotnet`, `game`
**Estimate**: 20 minutes
**Priority**: P2
**Depends on**: #TBD-10

**Description:**
Create a Taskfile for ConsoleDungeon game-specific tasks.

**Acceptance Criteria:**
- [ ] Create `src/ConsoleDungeon/Taskfile.yml` (adjust path as needed)
- [ ] Add `build` task: `dotnet build`
- [ ] Add `test` task: `dotnet test`
- [ ] Add `run` task: `dotnet run` (if applicable)
- [ ] Add `clean` task: `dotnet clean`
- [ ] Update root Taskfile to include game module
- [ ] Test `task game:build`, `task game:test`, etc. work

**Definition of Done:**
- Game Taskfile exists with all tasks
- Root Taskfile includes game module
- All game tasks are accessible via `task game:{task}`

**File to create:**
```yaml
# src/ConsoleDungeon/Taskfile.yml (adjust path based on actual structure)
version: '3'

tasks:
  build:
    desc: "Build ConsoleDungeon game"
    cmds:
      - dotnet build

  test:
    desc: "Run ConsoleDungeon tests"
    cmds:
      - dotnet test

  run:
    desc: "Run ConsoleDungeon game"
    cmds:
      - dotnet run

  clean:
    desc: "Clean ConsoleDungeon build artifacts"
    cmds:
      - dotnet clean
```

**Update root Taskfile.yml:**
```yaml
includes:
  build:
    taskfile: ./build/Taskfile.yml
    dir: ./build
  nodejs:
    taskfile: ./development/nodejs/Taskfile.yml
    dir: ./development/nodejs
  game:
    taskfile: ./src/ConsoleDungeon/Taskfile.yml
    dir: ./src/ConsoleDungeon
```

---

### Issue #TBD-13: Create CI task in root Taskfile
**Labels**: `infra`, `tooling`, `task-orchestration`, `ci`
**Estimate**: 15 minutes
**Priority**: P1 - Critical Path
**Depends on**: #TBD-10, #TBD-11, #TBD-12

**Description:**
Create a unified CI task that orchestrates the full CI pipeline.

**Acceptance Criteria:**
- [ ] Add `ci` task to root `Taskfile.yml`
- [ ] Task sequence: clean → build → test → test-e2e
- [ ] Task calls: `build:clean`, `build:build-all`, `nodejs:test`, `nodejs:test-e2e`
- [ ] Test `task ci` runs full pipeline successfully
- [ ] Verify all tests pass and artifacts are created

**Definition of Done:**
- `task ci` orchestrates full CI pipeline
- All components built, all tests pass
- Versioned artifacts created

**Code to add to root Taskfile.yml:**
```yaml
  ci:
    desc: "Full CI pipeline"
    cmds:
      - task: build:clean
      - task: build:build-all
      - task: nodejs:test
      - task: nodejs:test-e2e
      - echo "CI pipeline completed successfully!"
```

---

### Issue #TBD-14: 🔴 CRITICAL: Verify full CI pipeline
**Labels**: `infra`, `tooling`, `task-orchestration`, `testing`, `critical`, `ci`
**Estimate**: 30 minutes
**Priority**: P1 - Critical Path
**Depends on**: #TBD-13

**Description:**
Verify the complete CI pipeline works end-to-end across platforms.

**Acceptance Criteria:**
- [ ] Run `task --list` and verify all tasks from all modules are shown
- [ ] Run `task setup` and verify successful dependency installation
- [ ] Run `task ci` and verify full pipeline completes successfully
- [ ] Verify cross-platform compatibility (test on Windows, macOS, or Linux)
- [ ] Verify all .NET tests pass
- [ ] Verify all Node.js unit tests pass
- [ ] Verify Playwright E2E tests pass
- [ ] Verify versioned artifacts created in `build/_artifacts/v{GitVersion}/`
- [ ] Verify all build logs captured in `_logs/`
- [ ] Document verification results with screenshots/output

**Definition of Done:**
- ✅ `task --list` shows all namespaced tasks
- ✅ `task setup` works
- ✅ `task ci` completes successfully
- ✅ All tests pass (dotnet, nodejs unit, e2e)
- ✅ Artifacts created correctly
- ✅ Cross-platform verified (at least one platform)
- ✅ Evidence attached to issue (terminal output, screenshots)

**This is a CRITICAL TEST - if it fails, RFC-0010 is not complete**

---

## Phase 4 Issues (Optional - Can be Deferred)

### Issue #TBD-15: Update console app to output recordings to versioned artifacts
**Labels**: `feature`, `runtime-artifacts`, `asciinema`
**Estimate**: 1 hour
**Priority**: P3 - Enhancement
**Depends on**: #TBD-14

**Description:**
Modify ConsoleDungeon.Host to save asciinema recordings to the versioned artifacts directory.

**Acceptance Criteria:**
- [ ] Modify recording output path to use `build/_artifacts/v{GitVersion}/dotnet/recordings/`
- [ ] Implement GitVersion detection in console app
- [ ] Test recordings are saved to correct versioned folder
- [ ] Verify recordings are playable with asciinema

**Definition of Done:**
- Console app saves recordings to versioned artifacts directory
- Recordings are properly archived per version

---

### Issue #TBD-16: Update logging to output to versioned artifacts
**Labels**: `feature`, `runtime-artifacts`, `logging`
**Estimate**: 45 minutes
**Priority**: P3 - Enhancement
**Depends on**: #TBD-15

**Description:**
Configure application logging to output to versioned artifacts directories.

**Acceptance Criteria:**
- [ ] Configure dotnet logging path: `build/_artifacts/v{GitVersion}/dotnet/logs/`
- [ ] Configure web logging path: `build/_artifacts/v{GitVersion}/web/logs/`
- [ ] Configure pty logging path: `build/_artifacts/v{GitVersion}/pty/logs/`
- [ ] Test logs are created in correct locations during runtime

**Definition of Done:**
- All components log to versioned artifacts directories
- Logs are component-scoped for easy debugging

---

### Issue #TBD-17: Verify runtime artifacts are properly archived
**Labels**: `testing`, `runtime-artifacts`
**Estimate**: 20 minutes
**Priority**: P3 - Enhancement
**Depends on**: #TBD-16

**Description:**
Verify that runtime artifacts (recordings, logs) are properly archived in versioned folders.

**Acceptance Criteria:**
- [ ] Run console app and trigger recording
- [ ] Verify recording appears in `build/_artifacts/v{GitVersion}/dotnet/recordings/`
- [ ] Run app and generate logs
- [ ] Verify logs appear in correct component folders
- [ ] Document verification results

**Definition of Done:**
- ✅ Runtime artifacts properly archived
- ✅ Component-scoped organization verified
- ✅ Version-scoped archiving confirmed

---

**Last Updated**: 2025-10-02
**Author**: Development Team (with Claude Code assistance)

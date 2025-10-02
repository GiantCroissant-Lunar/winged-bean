---
title: RFC-0010: Multi-Language Build Orchestration with Task
---

# RFC-0010: Multi-Language Build Orchestration with Task

**Status:** Proposed
**Date:** 2025-10-02
**Author:** Development Team
**Category:** infra, tooling
**Priority:** HIGH (P1)
**Estimated Effort:** 4-6 hours

---

## Summary

Adopt [Task](https://taskfile.dev) as the multi-language build orchestrator for Winged Bean, wrapping existing build tools (Nuke for .NET, npm/pnpm for Node.js, Docker for containers, Unity CLI for game builds). Establish a versioned build artifacts structure under `build/_artifacts/` that organizes build outputs and runtime byproducts per GitVersion-calculated version.

---

## Motivation

### Current Problems

1. **No unified build orchestration** - .NET uses Nuke, Node.js uses npm scripts, no integration
2. **Manual cross-platform builds** - Different commands for Windows/macOS/Linux
3. **Scattered build outputs** - No consistent location for build artifacts
4. **Runtime artifacts confusion** - Asciinema recordings, logs have no clear home
5. **Version tracking gaps** - No structured way to archive versioned builds with their runtime data

### Why Task?

**Evaluated alternatives:**
- **Make**: Pre-installed but poor Windows support, cryptic syntax
- **Just**: Simple but no incremental builds (always rebuilds everything)
- **Task**: YAML-based, checksum incremental builds, true cross-platform, wraps existing tools

**Key advantages for Winged Bean:**
- Multi-language orchestration (.NET, Node.js, Docker, Unity)
- Checksum-based incremental builds (better than timestamps for game assets)
- Native Windows support (no Git Bash/WSL needed)
- Integrates with GitVersion seamlessly
- Monorepo-friendly include system

---

## Proposal

### 1. Build Orchestration with Task

**Install Task as build orchestrator:**
```bash
# macOS
brew install go-task/tap/go-task

# Windows
scoop install task

# Node.js (cross-platform)
npm install -g @go-task/cli
```

**Task wraps existing build tools (not replacing them):**
- **Nuke** handles .NET complexity (incremental compilation, strong typing)
- **npm/pnpm** handles Node.js tooling (Playwright, PTY service)
- **Docker** handles containerization
- **Unity CLI** handles game builds (future)
- **Task** orchestrates all of the above

### 2. Build Artifacts Directory Structure

```
build/
├── _artifacts/                          # Build outputs (versioned)
│   └── v{GitVersion}/                   # e.g., v1.0.0-alpha.1
│       ├── dotnet/
│       │   ├── bin/                     # Compiled .NET binaries
│       │   ├── recordings/              # Runtime: asciinema cast files
│       │   └── logs/                    # Runtime: console app logs
│       ├── web/
│       │   ├── dist/                    # Bundled web assets
│       │   ├── recordings/              # Runtime: browser recordings
│       │   └── logs/                    # Runtime: web app logs
│       ├── pty/
│       │   ├── dist/                    # Compiled PTY service
│       │   └── logs/                    # Runtime: PTY service logs
│       └── _logs/                       # Build-time logs (Task, compiler)
├── nuke/                                # Existing Nuke build
│   ├── build/
│   │   └── _build.csproj
│   ├── build.sh
│   └── ...
├── Taskfile.yml                         # Task orchestrator
└── .gitignore                           # _artifacts/, .task/
```

**Key design decisions:**

1. **Component-scoped runtime artifacts** - Each component (dotnet/web/pty) has its own recordings/logs for easier debugging
2. **Version-scoped archiving** - All artifacts for a version stored together (critical during unstable development)
3. **Build vs runtime separation** - `_logs/` = build process logs, `{component}/logs/` = runtime application logs
4. **GitVersion integration** - Folder names use semantic version from git (e.g., `v1.0.0-alpha.1+42`)

### 3. Task Integration with GitVersion

```yaml
# build/Taskfile.yml
version: '3'

vars:
  # GitVersion calculates semantic version from git tags/commits
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

  build-dotnet:
    desc: "Build .NET projects via Nuke"
    deps: [init-dirs]
    cmds:
      - ./nuke/build.sh Compile | tee {{.ARTIFACT_DIR}}/_logs/dotnet-build.log
      - cp -r bin/Release/* {{.ARTIFACT_DIR}}/dotnet/bin/

  build-web:
    desc: "Build web projects"
    deps: [init-dirs]
    dir: ../development/nodejs
    cmds:
      - pnpm run build | tee ../../build/{{.ARTIFACT_DIR}}/_logs/web-build.log
      - cp -r dist/* ../../build/{{.ARTIFACT_DIR}}/web/dist/

  build-pty:
    desc: "Build PTY service"
    deps: [init-dirs]
    dir: ../development/nodejs/pty-service
    cmds:
      - pnpm run build | tee ../../../build/{{.ARTIFACT_DIR}}/_logs/pty-build.log
      - cp -r dist/* ../../../build/{{.ARTIFACT_DIR}}/pty/dist/

  build-all:
    desc: "Build all components"
    deps:
      - build-dotnet
      - build-web
      - build-pty

  clean:
    desc: "Clean build artifacts"
    cmds:
      - rm -rf _artifacts/* .task/
      - ./nuke/build.sh Clean
```

### 4. Root-Level Orchestration

```
winged-bean/
├── Taskfile.yml                 # Root orchestrator
├── build/
│   ├── Taskfile.yml             # Build tasks (included by root)
│   └── ...
├── development/
│   └── nodejs/
│       └── Taskfile.yml         # Node.js tasks (included by root)
└── src/
    └── ConsoleDungeon/
        └── Taskfile.yml         # Game-specific tasks
```

**Root `Taskfile.yml`:**
```yaml
version: '3'

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
      - task: nodejs:install

  ci:
    desc: "Full CI pipeline"
    cmds:
      - task: build:clean
      - task: build:build-all
      - task: nodejs:test
      - task: nodejs:test-e2e
```

### 5. GitIgnore Strategy

**`build/.gitignore`:**
```gitignore
# Task cache
.task/

# Build artifacts (too large for git, archived locally)
_artifacts/*/

# Allow selective commits for critical recordings if needed
# !_artifacts/v1.0.0/dotnet/recordings/critical-bug.cast
```

**Why ignore artifacts:**
- Build outputs are reproducible via `task build:build-all`
- Runtime recordings/logs can be large (especially cast files)
- GitVersion ensures version traceability without committing binaries
- Critical recordings can be selectively un-ignored if needed for documentation

---

## Implementation Plan

### Phase 1: Task Setup (1-2 hours)

1. Install Task on development machine(s)
2. Create `build/Taskfile.yml` with GitVersion integration
3. Implement `init-dirs` task to create artifact structure
4. Update `build/.gitignore` to exclude `_artifacts/` and `.task/`

### Phase 2: Build Integration (2-3 hours)

1. Create `build-dotnet` task wrapping Nuke
2. Create `build-web` task for web builds
3. Create `build-pty` task for PTY service builds
4. Test incremental builds with checksum tracking
5. Validate GitVersion calculation in folder names

### Phase 3: Root Orchestration (1 hour)

1. Create root `Taskfile.yml` with includes
2. Create modular Taskfiles for Node.js, game components
3. Test `task --list` shows all available tasks
4. Document common workflows (`task setup`, `task ci`, etc.)

### Phase 4: CI/CD Integration (Future)

1. Update GitHub Actions to use `task ci`
2. Archive versioned artifacts in CI
3. Upload recordings/logs as workflow artifacts

---

## Alternatives Considered

### Alternative 1: Keep Status Quo (Nuke + npm scripts)
- **Pros**: No new tool, team already familiar
- **Cons**: No unified interface, manual coordination, poor Windows cross-compatibility

### Alternative 2: Just (Command Runner)
- **Pros**: Simpler syntax than Task
- **Cons**: No incremental builds (always rebuilds), not suitable for large projects

### Alternative 3: Make
- **Pros**: Pre-installed on Unix systems
- **Cons**: Poor Windows support, tab/space syntax errors, complex for multi-language projects

### Alternative 4: Nuke as Orchestrator
- **Pros**: Already installed, strongly typed C#
- **Cons**: .NET-centric, overkill for Node.js/Docker, team must know C# to modify builds

**Decision: Task** - Best balance of simplicity, cross-platform support, incremental builds, and multi-language orchestration.

---

## Success Criteria

1. ✅ `task --list` shows all available build tasks
2. ✅ `task build:build-all` produces versioned artifacts in `build/_artifacts/v{GitVersion}/`
3. ✅ GitVersion correctly calculates semantic version from git tags/commits
4. ✅ Incremental builds skip unchanged components (checksum-based)
5. ✅ Runtime artifacts (recordings, logs) organized per component
6. ✅ Build-time logs captured in `_logs/` for debugging
7. ✅ Cross-platform: same commands work on Windows, macOS, Linux
8. ✅ CI pipeline uses `task ci` for consistent local/CI builds

---

## Open Questions

1. **Docker image versioning**: Should Docker tags use GitVersion or git commit hash?
2. **Artifact retention**: How long to keep old versioned artifacts locally? (Auto-cleanup policy?)
3. **Unity integration timeline**: When to add Unity build tasks? (Future RFC?)
4. **Parallel builds**: Should Task parallelize `build-dotnet`, `build-web`, `build-pty`? (May cause race conditions in early development)

---

## References

- [Task Documentation](https://taskfile.dev)
- [GitVersion Documentation](https://gitversion.net)
- [RFC-0009: Dynamic Asciinema Recording](./0009-dynamic-asciinema-recording-in-pty.md) - Context for runtime recordings
- [R-DOC-010, R-DOC-020](../.agent/base/20-rules.md) - Documentation rules

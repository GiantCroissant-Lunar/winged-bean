# GitHub Copilot Instructions - Winged Bean

**Project**: Winged Bean  
**Stack**: .NET (multi)

---

## Rule Files to Read

GitHub Copilot: Read these files in order (highest priority first):

### 1. Project Rules (Highest Priority)
**File**: `.agent/local/overrides.md`  
**Contains**: Project-specific rules for Winged Bean

### 2. Workspace Rules
**File**: `.agent/local/workspace-reference.md`  
**Contains**: Rules for ALL projects in lunar-horse workspace

### 3. Workspace Reference
**File**: `.agent/local/workspace-reference.md`  
**Contains**: Explanation of rule hierarchy

---

## Critical Rules Summary

### ⚠️ R-LUNARHORSE-000: NO GIT REPOSITORY
- `/Users/apprenticegc/Work/lunar-horse/` is a WORKSPACE, NOT a git repository
- **NEVER** run `git init` in the workspace root
- Individual projects have their own `.git` directories

### ⚠️ R-LUNARHORSE-030: Use Task Build System
- Use Task or build-arcane for builds
- **NEVER** use `dotnet build` or `dotnet run` directly from source
- **ALWAYS** run from `build/_artifacts/` after building

### ⚠️ R-LUNARHORSE-060: Read Project Rules First
- Check `.agent/local/overrides.md` before making changes
- Ask user if uncertain about project structure

---

## Project-Specific Quick Ref

Game development framework. Use Task build system from build/.

**For full details**: Read `.agent/local/overrides.md`

---

## Build Commands

```bash
# For projects with Task build system
cd build
task build-dotnet        # Build
task console:normal      # Run

# For projects with build-arcane
dotnet lunar-build ci    # Full CI pipeline
dotnet lunar-build compile  # Just build
```

---

## File Organization

```
Winged Bean/
  .agent/
    local/
      overrides.md              ← Project rules (READ THIS)
      workspace-reference.md    ← Rule hierarchy explanation
  .github/
    copilot-instructions.md     ← This file
```

---

## Rule System

**Master Source**: `../plate-projects/rule-ground/`  
**Workspace Rules**: `.agent/local/workspace-reference.md`  
**Cached Base/Stack**: `~/.cache/lunar-rules/v1.0.0/`

**Priority**: Project → Workspace → Stack → Base

---

## For Copilot: Before Making Changes

1. Read `.agent/local/overrides.md` (project rules)
2. Read `.agent/local/workspace-reference.md` (workspace rules)
3. Verify current directory with `pwd`
4. Check project structure if uncertain

---

**Remember**: This project is part of the lunar-horse workspace. Workspace rules apply!

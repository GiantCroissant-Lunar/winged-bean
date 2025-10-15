# Winged Bean - Agent Instructions

**Project**: Winged Bean  
**Stack**: .NET (multi)  
**Date**: 2025-10-14

---

## Quick Start for AI Agents

### Rule Priority (Read in Order)

1. **Workspace Rules** (highest priority) → `.agent/local/workspace-reference.md`
2. **Project Rules** → `.agent/local/overrides.md` 
3. **Stack Rules** (cached) → `~/.cache/lunar-rules/v1.0.0/stacks/{stack}/`
4. **Base Rules** (cached) → `~/.cache/lunar-rules/v1.0.0/base/`

### Where to Find Rules

```
Workspace Rules (ALL projects)
  Reference: .agent/local/workspace-reference.md
  Relative: ../../../.agent/local/overrides.md
  
Project Rules (this project only)
  Location: .agent/local/overrides.md
  Reference: .agent/local/workspace-reference.md
```

---

## Supported AI Systems

### Claude Code
- **File**: `CLAUDE.md` (optional, agent-specific formatting)
- **Rules**: Reads `.agent/local/` automatically
- **Priority**: Workspace rules → Project rules → Stack → Base

### Windsurf/Cascade
- **File**: `AGENTS.md` (this file)
- **Rules**: Reads `.agent/local/` automatically
- **Priority**: Workspace rules → Project rules → Stack → Base

### GitHub Copilot
- **File**: `.github/copilot-instructions.md`
- **Rules**: Needs explicit pointers to .agent files
- **Priority**: Must reference workspace and project rules

---

## Critical Workspace Rules (Summary)

These apply to ALL projects in lunar-horse workspace:

### R-LUNARHORSE-000: NO GIT REPOSITORY ⚠️ CRITICAL
- lunar-horse is a WORKSPACE, not a git repo
- NEVER run `git init` here
- Individual projects have their own .git

### R-LUNARHORSE-030: Use Task Build System ⚠️ IMPORTANT
- Use Task/build-arcane for builds
- NEVER use `dotnet build` or `dotnet run` from source
- ALWAYS run from artifacts

### R-LUNARHORSE-060: Read Project Rules First
- Check `.agent/local/overrides.md` in project
- Then read workspace rules
- Ask if uncertain

**→ See workspace rules for complete list**: `../../../.agent/local/overrides.md`

---

## Project-Specific Quick Reference

Game development framework. Use Task build system from build/.

**→ See project rules for complete details**: `.agent/local/overrides.md`

---

## For AI Agents: First Time in This Project?

**Do this:**

1. ✅ Read workspace rules: `../../../.agent/local/overrides.md`
2. ✅ Read project rules: `.agent/local/overrides.md`
3. ✅ Check current working directory with `pwd`
4. ✅ Check project structure with `ls -la`
5. ✅ If building, use Task from `build/` directory

**Don't do this:**

- ❌ Run `git init` in workspace root
- ❌ Build directly from `development/` or source directories
- ❌ Assume structure without checking
- ❌ Duplicate workspace rules in project

---

## Rule System Architecture

```
rule-ground (git repo)                  ← Master source (version controlled)
    ↓ sync via lunar-rules CLI
~/.cache/lunar-rules/v1.0.0/           ← Cached base/stack rules
    ↓ referenced
lunar-horse/.agent/local/              ← Workspace rules (SSOT)
    ↓ referenced via relative path
{PROJECT}/.agent/local/                ← Project-specific rules only
```

**Key Principle**: Single source of truth, no duplication, flexible references.

---

## Version Information

- **Workspace Rules**: v1.0.0
- **Base Rules**: v1.0.0 (cached)
- **Stack Rules**: v1.0.0 (cached)
- **Last Updated**: 2025-10-14

---

**Need Help?** Read the rules in order listed above. When in doubt, ask!

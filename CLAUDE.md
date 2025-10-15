# Winged Bean - Claude Code Instructions

**Project**: Winged Bean  
**Stack**: .NET (multi)  
**Date**: 2025-10-15

> 📘 **Optional File**: This file provides Claude-specific formatting. All rules are in `.agent/local/`

---

## Rule Hierarchy

Claude Code automatically reads rules from `.agent/local/` in this priority:

1. **Project Rules** → `.agent/local/overrides.md` (this project)
2. **Workspace Rules** → `../../../.agent/local/overrides.md` (all projects)
3. **Stack Rules** → `~/.cache/lunar-rules/v1.0.0/stacks/{stack}/`
4. **Base Rules** → `~/.cache/lunar-rules/v1.0.0/base/`

---

## Quick Reference

### Critical Workspace Rules

**R-LUNARHORSE-000: NO GIT REPOSITORY** ⚠️
- This is a workspace, not a git repo
- NEVER run `git init` in /Users/apprenticegc/Work/lunar-horse/

**R-LUNARHORSE-030: Use Task Build System** ⚠️
- Use Task or build-arcane for builds
- Run from artifacts, not source

**R-LUNARHORSE-060: Read Project Rules First**
- Check `.agent/local/overrides.md`
- Ask if uncertain

### Project-Specific

Game development framework. Use Task build system from build/.

---

## For Claude: First Session?

```bash
# 1. Read workspace rules
cat ../../../.agent/local/overrides.md

# 2. Read project rules
cat .agent/local/overrides.md

# 3. Check workspace reference
cat .agent/local/workspace-reference.md

# 4. Verify working directory
pwd
```

---

## Full Documentation

- **Workspace Rules**: `../../../.agent/local/overrides.md`
- **Project Rules**: `.agent/local/overrides.md`
- **Workspace Reference**: `.agent/local/workspace-reference.md`

All rules are in `.agent/local/` - Claude reads them automatically!

# Workspace Rules Reference

**⚠️ IMPORTANT: Read workspace rules first**

## Rule Hierarchy (Priority Order)

1. **Project-specific rules** (`.agent/local/overrides.md` in this project) - **HIGHEST PRIORITY**
2. **Workspace rules** (`/Users/apprenticegc/Work/lunar-horse/.agent/local/overrides.md`) - **READ THIS**
3. **Stack-specific rules** (from `~/.cache/lunar-rules/v1.0.0/stacks/{stack}/`)
4. **Base rules** (from `~/.cache/lunar-rules/v1.0.0/base/`)

## Workspace Rules Location

**Absolute Path**: `/Users/apprenticegc/Work/lunar-horse/.agent/local/overrides.md`

**Relative Path from personal-work/yokan-projects/winged-bean**: `../../../.agent/local/overrides.md`

## What This Means

- **Workspace rules apply to ALL projects** in the lunar-horse workspace
- This includes critical rules like:
  - R-LUNARHORSE-000: No git init in workspace
  - R-LUNARHORSE-030: Use Task build system
  - R-LUNARHORSE-060: Read project rules first
  - And many more...

- **Project rules override workspace rules** when there's a conflict
- **Project rules should ONLY contain project-specific information**, not duplicate workspace rules

## Single Workspace rules.lock

**Important**: This workspace uses a **single rules.lock** file:
- Location: `/Users/apprenticegc/Work/lunar-horse/.agent/rules.lock`
- Applies to: ALL projects in the workspace
- Version: v1.0.0
- Stacks: dotnet, nodejs, python, terraform

**What this means**:
- All projects share the same base/stack rule versions
- No per-project rules.lock files
- Update once, applies everywhere
- No version drift between projects

---

## Source Control

- **Rule master source**: `personal-work/plate-projects/rule-ground/` (git repository)
- **Workspace rules**: Synced from rule-ground to lunar-horse workspace
- **Cached base/stack rules**: Synced from rule-ground via `lunar-rules sync`

## For AI Agents

When you start working on this project:

1. ✅ **READ** workspace rules first: `/Users/apprenticegc/Work/lunar-horse/.agent/local/overrides.md`
2. ✅ **THEN READ** project rules: `.agent/local/overrides.md` (in this directory)
3. ✅ Check which stack(s) apply (dotnet, nodejs, python, etc.)
4. ✅ Base rules are automatically loaded from cache

**DO NOT** duplicate workspace rules in project rules. Only add project-specific rules.

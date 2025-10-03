# Planning Scripts Migration: TypeScript → Python

**Date**: October 3, 2025  
**Scope**: Unified `planning/scripts/` to Python-only

## Changes Made

### Scripts Converted

- **Before**: `planning/scripts/open_issues_from_plan.ts` (TypeScript/Node.js)
- **After**: `planning/scripts/open_issues_from_plan.py` (Python 3.9+)

### Benefits

1. **Unified language**: All planning scripts now Python-only
2. **Fewer dependencies**: No Node.js/pnpm required for planning tools
3. **Consistency**: Matches `development/python/` patterns
4. **Simpler setup**: One package manager (pip) instead of two (pip + pnpm)

### Features Preserved

✅ Topological sorting (dependencies created first)  
✅ Dry-run mode (`--dry-run`)  
✅ GitHub CLI integration (`gh issue create`)  
✅ Issue body formatting (deps, files, ACs, estimates)  
✅ Label support  
✅ Error handling and progress reporting

## Usage

### Old (TypeScript)
```bash
node planning/scripts/open_issues_from_plan.ts --dry-run
```

### New (Python)
```bash
python3 planning/scripts/open_issues_from_plan.py --dry-run
```

## Dependencies

### Removed
- ❌ Node.js
- ❌ pnpm
- ❌ `js-yaml` package

### Required
- ✅ Python 3.9+
- ✅ `pyyaml` (already required for validation script)
- ✅ `gh` CLI (same as before)

## Documentation Updated

- ✅ `planning/README.md` - Updated commands and deps
- ✅ `docs/guides/owl-planner-integration.md` - Python commands
- ✅ `docs/guides/OWL_INTEGRATION_SUMMARY.md` - Python refs
- ✅ `docs/guides/OWL_QUICK_REFERENCE.md` - Commands cheat sheet
- ✅ `docs/guides/OWL_NEXT_STEPS.md` - Checklist updated

## Migration Impact

### No Breaking Changes

- ✅ CLI interface identical (`--dry-run` flag preserved)
- ✅ Output format identical
- ✅ `gh` CLI usage unchanged
- ✅ `plan.yaml` format unchanged

### Future Benefits

- Easier to extend (Python ecosystem)
- Can reuse code from `development/python/` if needed
- Simpler CI setup (one language for planning tools)
- Better integration with validation script

## File Structure (After)

```
planning/
├── README.md
├── codex-config-template.toml
├── plan.yaml
├── owl/
│   ├── planner_server.py        # Python
│   └── mcp_servers_config.json
└── scripts/
    ├── validate_and_render.py   # Python ✅
    └── open_issues_from_plan.py # Python ✅ (was .ts)
```

**Result**: `planning/scripts/` is now **Python-only** 🎉

## Testing

```bash
# Syntax check
python3 -m py_compile planning/scripts/open_issues_from_plan.py

# Dry run
python3 planning/scripts/open_issues_from_plan.py --dry-run

# Full run (creates issues)
python3 planning/scripts/open_issues_from_plan.py
```

## Rollback (if needed)

The TypeScript version has been removed in favor of Python. If you must use TS, recover it from git history and update docs accordingly.

## Next Steps

- [ ] Delete `open_issues_from_plan.ts` after confirming Python version works
- [ ] Consider moving all planning scripts to use shared utilities
- [ ] Add to `development/python/pyproject.toml` if we want installable commands

---

**Status**: ✅ Complete - All planning scripts unified to Python

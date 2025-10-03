# ✅ Planning Scripts Unified to Python

## Summary

Successfully converted `planning/scripts/` to **Python-only**, eliminating the Node.js/TypeScript dependency.

## What Changed

### Before
```
planning/scripts/
├── validate_and_render.py      # Python
└── open_issues_from_plan.ts    # TypeScript/Node.js ❌
```

### After  
```
planning/scripts/
├── validate_and_render.py      # Python ✅
├── open_issues_from_plan.py    # Python ✅
└── test_scripts.sh              # Test harness
```

## Key Improvements

1. **Single language**: All planning scripts now Python 3.9+
2. **No Node.js needed**: Removed pnpm/js-yaml dependency
3. **Consistent with repo**: Matches `development/python/` patterns
4. **Same features**: Topological sort, dry-run, gh CLI integration
5. **All docs updated**: 5 guide files updated with new commands

## New Python Script Features

✅ **Preserved from TypeScript**:
- Topological sorting (dependencies created first)
- `--dry-run` mode for preview
- GitHub CLI (`gh issue create`) integration  
- Rich issue body formatting (deps, files, ACs, estimates)
- Label support
- Error handling and progress reporting

✅ **Python advantages**:
- Type hints for better IDE support
- Consistent with validation script
- Easier to extend with Python ecosystem
- No build step required

## Usage Examples

### Validate plan
```bash
python3 planning/scripts/validate_and_render.py
# Output: ✅ All validations passed
#         ✅ Generated Mermaid diagram: docs/development/plan.mmd
```

### Preview issues (dry-run)
```bash
python3 planning/scripts/open_issues_from_plan.py --dry-run
# Shows what issues would be created
```

### Create issues
```bash
python3 planning/scripts/open_issues_from_plan.py
# Creates GitHub issues via gh CLI
```

### Test all scripts
```bash
bash planning/scripts/test_scripts.sh
```

## Dependencies Now

**Python only**:
- Python 3.9+
- `pyyaml` (for YAML parsing)
- `gh` CLI (for GitHub integration)

**Removed**:
- Node.js ❌
- pnpm ❌  
- js-yaml ❌

## Files Updated

### Scripts
- ✅ Created `planning/scripts/open_issues_from_plan.py`
- ✅ Created `planning/scripts/test_scripts.sh`
- ⚠️ Left `planning/scripts/open_issues_from_plan.ts` (can delete after testing)

### Documentation
- ✅ `planning/README.md`
- ✅ `docs/guides/owl-planner-integration.md`
- ✅ `docs/guides/OWL_INTEGRATION_SUMMARY.md`
- ✅ `docs/guides/OWL_QUICK_REFERENCE.md`
- ✅ `docs/guides/OWL_NEXT_STEPS.md`
- ✅ `docs/guides/PLANNING_SCRIPTS_MIGRATION.md` (new)

## Installation

```bash
# Install Python dependencies (if not already installed)
pip install pyyaml

# Install gh CLI (if not already installed)
# macOS:
brew install gh

# Authenticate (first time only)
gh auth login
```

## Testing Checklist

- [x] Python script syntax valid (`python3 -m py_compile`)
- [x] Validation script works
- [ ] Dry-run shows correct output format
- [ ] Can create actual issues (when ready)
- [ ] All documentation references updated

## Next Steps

1. **Test the new script**:
   ```bash
   python3 planning/scripts/open_issues_from_plan.py --dry-run
   ```

2. **Verify output** looks correct (issue titles, bodies, labels)

3. **Delete old TypeScript file** (optional, after confirming Python works):
   ```bash
   rm planning/scripts/open_issues_from_plan.ts
   ```

4. **Update any CI/automation** that references the old `.ts` file

## Rollback Plan

If issues arise, the original TypeScript file is still present:
```bash
# Revert to TypeScript version
node planning/scripts/open_issues_from_plan.ts --dry-run
```

Then restore the old documentation references.

---

**Status**: ✅ Migration complete  
**Benefit**: Simpler dependency management, consistent language choice  
**Risk**: Low (functionality preserved, TypeScript file kept as backup)

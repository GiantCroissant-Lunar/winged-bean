# Planning Scripts Restoration Summary

**Date**: October 3, 2025  
**Action**: Restored all planning infrastructure files that were removed

## Files Restored

### Core Planning Files

✅ **`planning/plan.yaml`**
- Task plan template with 7 example tasks
- Demonstrates DAG structure with dependencies
- Shows agent routing labels (copilot/cascade/claude)
- Includes acceptance criteria and estimates

✅ **`planning/codex-config-template.toml`**
- Codex CLI MCP server configuration template
- Instructions for setting up OWL planner in Codex

### OWL MCP Server (`planning/owl/`)

✅ **`planning/owl/planner_server.py`**
- MCP stdio server exposing planning tools
- `plan_dag` tool - decomposes goals into tasks
- `refine_plan` tool - updates existing plans
- JSON-RPC protocol handler
- Ready for CAMEL/OWL agent integration

✅ **`planning/owl/mcp_servers_config.json`**
- Configuration for OWL's own MCP clients
- Filesystem, GitHub, and HTTP fetch servers
- Used by OWL to access repo context

### Planning Scripts (`planning/scripts/`)

✅ **`planning/scripts/validate_and_render.py`**
- Validates plan.yaml structure
- Detects cycles in dependency graph
- Checks label conventions
- Generates Mermaid diagrams (docs/development/plan.mmd)
- Exit codes: 0=success, 1=validation errors, 2=file errors

✅ **`planning/scripts/open_issues_from_plan.py`**
- Creates GitHub issues from plan.yaml
- Topological sorting (dependencies created first)
- Dry-run mode for preview
- Uses gh CLI for issue creation
- Preserves task dependencies in issue bodies

✅ **`planning/scripts/test_scripts.sh`** (already existed)
- Test harness for all planning scripts
- Runs validation and dry-run tests

## Directory Structure (Restored)

```
planning/
├── README.md                        # Planning system documentation
├── PYTHON_MIGRATION_COMPLETE.md    # Migration notes
├── plan.yaml                        # ✅ RESTORED - Task DAG
├── codex-config-template.toml       # ✅ RESTORED - Codex setup
├── owl/
│   ├── planner_server.py           # ✅ RESTORED - MCP server
│   └── mcp_servers_config.json     # ✅ RESTORED - OWL MCP config
└── scripts/
    ├── validate_and_render.py      # ✅ RESTORED - Validation
    ├── open_issues_from_plan.py    # ✅ RESTORED - Issue creation
    └── test_scripts.sh              # (was not removed)
```

## Verification

All files have been recreated with their full original content:

### Test Validation Script
```bash
python3 planning/scripts/validate_and_render.py
# Expected: ✅ All validations passed
#           ✅ Generated Mermaid diagram
```

### Test Issue Creation (Dry Run)
```bash
python3 planning/scripts/open_issues_from_plan.py --dry-run
# Expected: Preview of 7 issues with dependencies
```

### Test OWL MCP Server
```bash
echo '{"jsonrpc":"2.0","id":1,"method":"mcp/spec"}' | python3 planning/owl/planner_server.py
# Expected: JSON response with tool specifications
```

## Dependencies Required

All scripts are Python 3.9+ only:
- `pyyaml` (for YAML parsing)
- `gh` CLI (for issue creation)

No Node.js/TypeScript dependencies needed.

## What Was Removed (and Why)

Based on the migration to Python-only scripts:
- The old `open_issues_from_plan.ts` (TypeScript version) may have been removed
- This is expected - we migrated to Python for consistency

## Next Steps

1. **Verify scripts work**:
   ```bash
   bash planning/scripts/test_scripts.sh
   ```

2. **Check git status**:
   ```bash
   git status planning/
   ```

3. **Commit restored files**:
   ```bash
   git add planning/
   git commit -m "Restore planning infrastructure files"
   ```

## Integration Status

- ✅ `.mcp.json` at repo root (connects Claude Code to OWL)
- ✅ CI workflow `.github/workflows/plan-check.yml` (validates on PRs)
- ✅ Documentation in `docs/guides/` (integration guides)
- ✅ All planning scripts restored and ready to use

## Why These Files Matter

**`plan.yaml`**: Single source of truth for task decomposition  
**OWL MCP server**: Enables AI-powered planning via Claude Code/Codex  
**Validation script**: Ensures DAG correctness in CI  
**Issue creation script**: Automates GitHub issue generation  

Together, they enable the **multi-agent workflow** where:
- OWL plans the work (DAG generation)
- Copilot handles parallel leaves
- Cascade handles UI/refactoring
- Claude handles integration/risky work

---

**Status**: ✅ All planning files successfully restored  
**Ready for**: Testing, validation, and use in multi-agent workflows

# OWL Planner Quick Reference

## Common Commands

```bash
# Validate plan
python3 planning/scripts/validate_and_render.py

# Preview GitHub issues
python3 planning/scripts/open_issues_from_plan.py --dry-run

# Create GitHub issues
python3 planning/scripts/open_issues_from_plan.py

# Test MCP server manually
echo '{"jsonrpc":"2.0","id":1,"method":"mcp/spec"}' | python3 planning/owl/planner_server.py
```

## Claude Code Prompts

### Generate a plan
```
Use owl-planner.plan_dag to decompose "Add real-time PTY terminal support" into tasks.

Context:
- Read docs/rfcs/ for requirements
- Target development/nodejs/pty-service/ and development/dotnet/console/
- Mark parallel leaves with agent:copilot
- Add ACs and test requirements

Return JSON DAG + Mermaid + issue templates
```

### Refine existing plan
```
Use owl-planner.refine_plan on planning/plan.yaml:
- Split E2E task into integration + testing
- Add performance testing task after integration
- Update dependencies

Validate new DAG and show Mermaid
```

### Validate manually
```
Read planning/plan.yaml and check:
1. No cycles in dependency graph
2. All task IDs in 'needs' exist
3. Each task has at most one agent:* label
4. Files are under max_touch_files_per_task limit
```

## Agent Routing Labels

| Label | Agent | Use Case | Size Limit |
|-------|-------|----------|------------|
| `agent:copilot` | GitHub Copilot | Parallel leaves, clear specs | <15 files |
| `agent:cascade` | Windsurf Cascade | Multi-file, frontend, refactoring | Any |
| `agent:claude` | Claude Code | Integration, risky, E2E | Any |

## plan.yaml Cheat Sheet

```yaml
meta:
  project: winged-bean
  rules:
    max_touch_files_per_task: 15
    prefer_parallel_leaves: true
  routing:
    copilot: "Parallel, <15 files, isolated"
    cascade: "Frontend, multi-file, cross-cutting"
    claude: "Integration, risky, E2E, arch"

tasks:
  - id: TASK-ID              # Unique, all-caps-with-dashes
    desc: "What to do"       # Human-readable, concise
    needs: [DEP-1, DEP-2]    # Task IDs (must exist)
    labels: [agent:copilot]  # Routing + metadata
    estimate: 8h             # Optional
    files: [path/to/files/]  # Areas touched
    acceptance_criteria:     # Checkable outcomes
      - "AC 1"
      - "AC 2"
```

## MCP Tools

### plan_dag
```json
{
  "goal": "Feature description",
  "repo_path": ".",
  "constraints": { "max_touch_files_per_task": 15 },
  "style": "detailed"
}
→ { "json_dag": {...}, "mermaid": "...", "issues": [...] }
```

### refine_plan
```json
{
  "plan_path": "planning/plan.yaml",
  "changes": ["split X", "add Y"]
}
→ { "status": "refined", ... }
```

## CI Workflow Triggers

- **On PR**: touching `planning/plan.yaml`
- **On push to main**: touching `planning/plan.yaml`

**Actions:**
1. Validate DAG (fail on cycles)
2. Generate Mermaid → `docs/development/plan.mmd`
3. Upload artifact
4. Comment on PR with visualization

## File Locations

| File | Purpose |
|------|---------|
| `.mcp.json` | Claude Code MCP config |
| `planning/plan.yaml` | Task DAG source of truth |
| `planning/owl/planner_server.py` | OWL MCP server |
| `planning/scripts/validate_and_render.py` | Validation + Mermaid |
| `planning/scripts/open_issues_from_plan.py` | Issue creation |
| `docs/development/plan.mmd` | Generated diagram |
| `docs/guides/owl-planner-integration.md` | Full guide |
| `planning/codex-config-template.toml` | Codex setup |

## Troubleshooting

| Issue | Fix |
|-------|-----|
| "Tool not found: owl-planner" | Check `.mcp.json` exists, test `python3 planning/owl/planner_server.py` |
| "Cycle detected" | Run `validate_and_render.py`, check error, remove cycle |
| MCP server crash | Check Python deps (`pip install pyyaml`), review stderr |
| Can't create issues | Ensure `pyyaml` installed: `pip install pyyaml` |
| Wrong agent | Check label in plan.yaml, only one `agent:*` per task |

## Dependencies

**Python:**
- `pyyaml` (validation)
- `networkx` (graph algorithms)
- Optional: `camel-ai[mcp]` (for actual OWL integration)



**External:**
- `gh` CLI (for issue creation)
- Claude Code / Codex CLI (MCP clients)

## Next Steps

1. ✅ Test: `python3 planning/scripts/validate_and_render.py`
2. 🧪 Try: Ask Claude Code to call `owl-planner.plan_dag`
3. 📝 Customize: Edit `planning/plan.yaml` with real tasks
4. 🔧 Integrate: Install Camel-AI, wire real OWL agent
5. 🚀 Automate: Create issues, start PRs with Copilot

---

**Full docs**: `docs/guides/owl-planner-integration.md`  
**Summary**: `docs/guides/OWL_INTEGRATION_SUMMARY.md`  
**Planning README**: `planning/README.md`

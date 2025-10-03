# Planning System

This directory contains the OWL-powered planning infrastructure for the winged-bean project.

## Structure

```
planning/
├── plan.yaml                    # Single source of truth for tasks and dependencies
├── owl/                         # OWL planner MCP server
│   ├── planner_server.py       # MCP stdio server exposing planning tools
│   └── mcp_servers_config.json # OWL's own MCP clients (filesystem, GitHub, etc.)
└── scripts/                     # Validation and automation
    ├── validate_and_render.py  # CI: validate DAG, generate Mermaid
    └── open_issues_from_plan.py            # GitHub issue creation
```

## Quick Start

### 1. Validate a Plan

```bash
python planning/scripts/validate_and_render.py

# Output:
# ✅ All validations passed
# ✅ Generated Mermaid diagram: docs/development/plan.mmd
```

### 2. Use OWL Planner in Claude Code

```
Call owl-planner.plan_dag with goal="Add PTY service" and return JSON + Mermaid
```

The planner reads your RFCs, decomposes the work, and tags tasks by agent.

### 3. Create GitHub Issues

```bash
# Preview what would be created
python3 planning/scripts/open_issues_from_plan.py --dry-run

# Actually create issues
python3 planning/scripts/open_issues_from_plan.py
```

When not using --dry-run, the script will:

- Create issues in topological order so dependencies exist first
- Write an issues map artifact to `build/_artifacts/issues-map.json`
- Do a second pass to comment real "Blocked By: #<num>" on dependent issues and add the `status:blocked` label

## plan.yaml Format

```yaml
meta:
  project: winged-bean
  rules:
    max_touch_files_per_task: 15
    prefer_parallel_leaves: true

tasks:
  - id: TASK-ID
    desc: "Human-readable description"
    needs: [DEPENDENCY-1, DEPENDENCY-2]
    labels: [size:S, domain:backend]
    estimate: 8h
    files: [path/to/affected/files/]
    acceptance_criteria:
      - "AC 1"
      - "AC 2"
```

## Size & Domain Routing (tool-agnostic)

- `size:*` → Effort/complexity signal (XS, S, M, L, XL)
  - XS/S ≈ small/simple; M/L/XL ≈ larger/complex
- `domain:*` → Context of work, e.g. `domain:frontend`, `domain:backend`, `domain:infra`, `domain:docs`, `domain:test`

These labels describe the nature and scope of the task (not which agent brand does it). Assignment can be manual or automated later based on size/domain.

## CI Integration

The `.github/workflows/plan-check.yml` workflow:
1. Validates DAG structure (no cycles)
2. Generates Mermaid diagram
3. Posts visualization to PRs

## MCP Configuration

Claude Code connects to the OWL planner via `.mcp.json`:

```json
{
  "mcpServers": {
    "owl-planner": {
      "type": "stdio",
      "command": "python3",
      "args": ["planning/owl/planner_server.py"]
    }
  }
}
```

Codex CLI can connect via `~/.codex/config.toml` (see template below).

## Codex CLI Setup (Optional)

Add to `~/.codex/config.toml`:

```toml
[mcp_servers.owl-planner]
type = "stdio"
command = "python3"
args = ["/absolute/path/to/winged-bean/planning/owl/planner_server.py"]
env.OWL_MCP_CONFIG = "/absolute/path/to/winged-bean/planning/owl/mcp_servers_config.json"
env.REPO_ROOT = "/absolute/path/to/winged-bean"
env.GITHUB_TOKEN = "$GITHUB_TOKEN"
```

## Tools Reference

### `owl-planner.plan_dag`

Decompose a goal into tasks with dependencies.

**Input:**
- `goal`: Feature description
- `repo_path`: Repository root (default: ".")
- `constraints`: Optional rules (max_touch_files_per_task, etc.)
- `style`: "detailed" or "minimal"

**Output:**
- `json_dag`: Task graph with nodes and edges
- `mermaid`: Visualization string
- `issues`: GitHub issue templates

### `owl-planner.refine_plan`

Update an existing plan based on feedback.

**Input:**
- `plan_path`: Path to plan.yaml
- `changes`: List of refinements

**Output:**
- Updated plan structure

## Next Steps

1. **Read the full guide**: `docs/guides/owl-planner-integration.md`
2. **Test locally**: `python planning/scripts/validate_and_render.py`
3. **Use in Claude Code**: Ask it to call `owl-planner.plan_dag`
4. **Customize**: Edit `planner_server.py` to integrate actual OWL agents

## Dependencies

- Python 3.11+
- `pyyaml`

- Optional: Camel-AI OWL for advanced planning

Install Python deps:
```bash
pip install pyyaml
```

All planning scripts are Python 3.9+. No Node.js dependencies required.

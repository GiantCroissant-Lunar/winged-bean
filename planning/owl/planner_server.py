#!/usr/bin/env python3
"""
OWL-powered planner exposed as an MCP (Model Context Protocol) stdio server.

This server exposes planning tools that can be called by Claude Code, Codex CLI,
and other MCP clients. It uses Camel-AI OWL to decompose tasks, build DAGs,
and emit both machine-readable JSON and human-readable Mermaid diagrams.

Key tool: plan_dag
  Input: { goal, repo_path, constraints, style }
  Output: { json_dag, mermaid, issues }
"""

import json
import sys
import os
from typing import Dict, Any, List
from pathlib import Path

# TODO: Import actual CAMEL/OWL agent libraries once installed
# from camel.agents import ChatAgent
# from camel.toolkits import MCPToolkit


def plan_dag(args: Dict[str, Any]) -> Dict[str, Any]:
    """
    Build a task DAG from high-level goals and repo context.

    Args:
        goal: High-level feature or RFC description
        repo_path: Path to the repository root
        constraints: Optional dict with keys like max_touch_files_per_task, prefer_parallel_leaves
        style: Optional string "detailed" | "minimal"

    Returns:
        {
          "json_dag": {
            "tasks": [{"id": str, "desc": str, "needs": [str], "labels": [str]}],
            "edges": [[str, str], ...]
          },
          "mermaid": str,
          "issues": [{"title": str, "body": str, "labels": [str], "needs": [str]}]
        }
    """
    goal = args.get("goal", "")
    repo_path = args.get("repo_path", ".")
    constraints = args.get("constraints", {})
    style = args.get("style", "detailed")

    # TODO: Replace this with actual OWL agent planning logic
    # For now, return a demonstration DAG that shows the structure

    # Example: Parse goal and existing plan.yaml if present
    plan_yaml_path = Path(repo_path) / "planning" / "plan.yaml"
    existing_tasks = []

    # Simulated planning logic (replace with OWL agent)
    # This would:
    # 1. Read RFCs from docs/rfcs/
    # 2. Analyze codebase structure
    # 3. Decompose goal into tasks with dependencies
    # 4. Tag parallelizable leaves with agent:copilot
    # 5. Tag risky/integration tasks with agent:claude or agent:cascade

    dag = {
        "tasks": [
            {
                "id": "RFC",
                "desc": "Draft spec & acceptance criteria",
                "needs": [],
                "labels": ["doc", "planner"]
            },
            {
                "id": "API",
                "desc": "Implement service API per spec",
                "needs": ["RFC"],
                "labels": ["agent:copilot", "area:backend", "parallel-safe"]
            },
            {
                "id": "UI",
                "desc": "Create UI shell per spec",
                "needs": ["RFC"],
                "labels": ["agent:cascade", "area:frontend", "parallel-safe"]
            },
            {
                "id": "E2E",
                "desc": "Wire API+UI and add E2E tests",
                "needs": ["API", "UI"],
                "labels": ["agent:claude", "test:e2e", "integration"]
            }
        ],
        "edges": [
            ["RFC", "API"],
            ["RFC", "UI"],
            ["API", "E2E"],
            ["UI", "E2E"]
        ]
    }

    # Generate Mermaid diagram
    mermaid_lines = ["graph TD;"]
    for source, target in dag["edges"]:
        mermaid_lines.append(f"  {source}-->{target};")
    mermaid = "\n".join(mermaid_lines)

    # Generate GitHub issues
    issues = []
    for task in dag["tasks"]:
        needs_text = ""
        if task["needs"]:
            needs_lines = [f"- Blocked by **{n}**" for n in task["needs"]]
            needs_text = "\n\n**Dependencies:**\n" + "\n".join(needs_lines)

        body = f"{task['desc']}{needs_text}\n\n**Labels:** {', '.join(task['labels'])}"

        issues.append({
            "title": f"{task['id']}: {task['desc']}",
            "body": body,
            "labels": task["labels"],
            "needs": task["needs"]
        })

    return {
        "json_dag": dag,
        "mermaid": mermaid,
        "issues": issues,
        "metadata": {
            "goal": goal,
            "constraints": constraints,
            "style": style
        }
    }


def refine_plan(args: Dict[str, Any]) -> Dict[str, Any]:
    """
    Refine an existing plan based on new constraints or PR feedback.

    Args:
        plan_path: Path to existing plan.yaml
        changes: List of change requests, e.g., ["split E2E into two tasks", "add perf testing"]

    Returns:
        Updated plan in same format as plan_dag
    """
    plan_path = args.get("plan_path", "planning/plan.yaml")
    changes = args.get("changes", [])

    # TODO: Implement OWL-based re-planning
    # For now, return a placeholder

    return {
        "status": "refined",
        "changes_applied": changes,
        "message": "Plan refinement not yet implemented - integrate with OWL agent"
    }


# MCP Protocol Handler (minimal JSON-RPC over stdio)
class MCPServer:
    """Minimal MCP stdio server implementation."""

    def __init__(self):
        self.tools = {
            "plan_dag": {
                "description": "Build a task DAG from goals and repo context",
                "parameters": {
                    "goal": "string (required)",
                    "repo_path": "string (optional, default '.')",
                    "constraints": "object (optional)",
                    "style": "string (optional, 'detailed' or 'minimal')"
                },
                "handler": plan_dag
            },
            "refine_plan": {
                "description": "Refine an existing plan based on feedback",
                "parameters": {
                    "plan_path": "string (optional)",
                    "changes": "array of strings"
                },
                "handler": refine_plan
            }
        }

    def handle_request(self, request: Dict[str, Any]) -> Dict[str, Any]:
        """Handle a single JSON-RPC request."""
        method = request.get("method")
        params = request.get("params", {})
        req_id = request.get("id")

        if method == "mcp/spec":
            return {
                "jsonrpc": "2.0",
                "id": req_id,
                "result": {
                    "name": "owl-planner",
                    "version": "0.1.0",
                    "description": "OWL-powered planning and DAG generation",
                    "tools": [
                        {"name": name, **info}
                        for name, info in self.tools.items()
                    ]
                }
            }

        elif method == "tools/call":
            tool_name = params.get("name")
            tool_args = params.get("args", {})

            if tool_name not in self.tools:
                return {
                    "jsonrpc": "2.0",
                    "id": req_id,
                    "error": {
                        "code": -32601,
                        "message": f"Tool not found: {tool_name}"
                    }
                }

            try:
                handler = self.tools[tool_name]["handler"]
                result = handler(tool_args)
                return {
                    "jsonrpc": "2.0",
                    "id": req_id,
                    "result": result
                }
            except Exception as e:
                return {
                    "jsonrpc": "2.0",
                    "id": req_id,
                    "error": {
                        "code": -32000,
                        "message": f"Tool execution failed: {str(e)}"
                    }
                }

        else:
            return {
                "jsonrpc": "2.0",
                "id": req_id,
                "error": {
                    "code": -32601,
                    "message": f"Method not found: {method}"
                }
            }

    def run(self):
        """Main stdio loop."""
        for line in sys.stdin:
            line = line.strip()
            if not line:
                continue

            try:
                request = json.loads(line)
                response = self.handle_request(request)
                print(json.dumps(response), flush=True)
            except json.JSONDecodeError as e:
                error_response = {
                    "jsonrpc": "2.0",
                    "id": None,
                    "error": {
                        "code": -32700,
                        "message": f"Parse error: {str(e)}"
                    }
                }
                print(json.dumps(error_response), flush=True)
            except Exception as e:
                error_response = {
                    "jsonrpc": "2.0",
                    "id": None,
                    "error": {
                        "code": -32603,
                        "message": f"Internal error: {str(e)}"
                    }
                }
                print(json.dumps(error_response), flush=True)


def main():
    """Entry point for the MCP server."""
    # Optional: Load OWL config from environment
    owl_config_path = os.getenv("OWL_MCP_CONFIG")
    if owl_config_path:
        # TODO: Initialize OWL with MCPToolkit using this config
        # mcp_toolkit = MCPToolkit(config_path=owl_config_path)
        # await mcp_toolkit.connect()
        pass

    server = MCPServer()
    server.run()


if __name__ == "__main__":
    main()

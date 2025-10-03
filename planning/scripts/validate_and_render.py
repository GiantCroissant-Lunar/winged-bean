#!/usr/bin/env python3
"""
Validate plan.yaml structure and render Mermaid diagram.

This script is run in CI to ensure:
1. The task DAG is valid (no cycles)
2. All task references in 'needs' exist
3. Labels follow conventions
4. A Mermaid diagram is generated for visualization

Exit codes:
  0 - Success
  1 - Validation errors found
  2 - File not found or parsing error
"""

import sys
import yaml
from pathlib import Path
from typing import Dict, List, Set, Any


def load_plan(plan_path: Path) -> Dict[str, Any]:
    """Load and parse the plan.yaml file."""
    if not plan_path.exists():
        print(f"❌ Error: {plan_path} not found", file=sys.stderr)
        sys.exit(2)

    try:
        with open(plan_path) as f:
            return yaml.safe_load(f)
    except yaml.YAMLError as e:
        print(f"❌ Error parsing YAML: {e}", file=sys.stderr)
        sys.exit(2)


def validate_structure(plan: Dict[str, Any]) -> List[str]:
    """Validate basic plan structure."""
    errors = []

    if "meta" not in plan:
        errors.append("Missing 'meta' section")

    if "tasks" not in plan:
        errors.append("Missing 'tasks' section")
        return errors  # Can't continue without tasks

    if not isinstance(plan["tasks"], list):
        errors.append("'tasks' must be a list")
        return errors

    # Check each task has required fields
    for i, task in enumerate(plan["tasks"]):
        if not isinstance(task, dict):
            errors.append(f"Task {i} is not a dictionary")
            continue

        if "id" not in task:
            errors.append(f"Task {i} missing 'id' field")
        if "desc" not in task:
            errors.append(f"Task {i} missing 'desc' field")

    return errors


def build_dependency_graph(plan: Dict[str, Any]) -> tuple[Dict[str, List[str]], List[str]]:
    """
    Build dependency graph and validate references.

    Returns:
        (graph, errors) where graph is {task_id: [dependent_ids]}
    """
    errors = []
    graph = {}
    task_ids = {task["id"] for task in plan["tasks"] if "id" in task}

    for task in plan["tasks"]:
        if "id" not in task:
            continue

        task_id = task["id"]
        needs = task.get("needs", [])

        if not isinstance(needs, list):
            errors.append(f"Task '{task_id}': 'needs' must be a list")
            continue

        # Validate all dependencies exist
        for dep in needs:
            if dep not in task_ids:
                errors.append(f"Task '{task_id}': unknown dependency '{dep}'")

        graph[task_id] = needs

    return graph, errors


def detect_cycles(graph: Dict[str, List[str]]) -> List[str]:
    """
    Detect cycles in the dependency graph using DFS.

    Returns:
        List of error messages describing any cycles found.
    """
    errors = []
    visited = set()
    rec_stack = set()

    def visit(node: str, path: List[str]) -> bool:
        """Returns True if cycle detected."""
        if node in rec_stack:
            cycle_start = path.index(node)
            cycle = " -> ".join(path[cycle_start:] + [node])
            errors.append(f"Cycle detected: {cycle}")
            return True

        if node in visited:
            return False

        visited.add(node)
        rec_stack.add(node)
        path.append(node)

        for neighbor in graph.get(node, []):
            if visit(neighbor, path.copy()):
                return True

        rec_stack.remove(node)
        return False

    for node in graph:
        if node not in visited:
            visit(node, [])

    return errors


def validate_labels(plan: Dict[str, Any]) -> List[str]:
    """Validate label conventions."""
    errors = []
    valid_agent_labels = {"agent:copilot", "agent:cascade", "agent:claude"}

    for task in plan["tasks"]:
        if "id" not in task:
            continue

        task_id = task["id"]
        labels = task.get("labels", [])

        if not isinstance(labels, list):
            errors.append(f"Task '{task_id}': 'labels' must be a list")
            continue

        # Check for agent assignment
        agent_labels = [l for l in labels if l.startswith("agent:")]
        if len(agent_labels) > 1:
            errors.append(f"Task '{task_id}': multiple agent labels {agent_labels}")

    return errors


def generate_mermaid(plan: Dict[str, Any], output_path: Path) -> None:
    """Generate Mermaid diagram from task dependencies."""
    lines = ["graph TD;"]

    # Add all nodes with descriptions
    for task in plan["tasks"]:
        task_id = task.get("id", "")
        desc = task.get("desc", "")[:50]  # Truncate long descriptions

        # Style by agent type
        labels = task.get("labels", [])
        if "agent:copilot" in labels:
            lines.append(f'  {task_id}["{task_id}<br/>{desc}"]:::copilot')
        elif "agent:cascade" in labels:
            lines.append(f'  {task_id}["{task_id}<br/>{desc}"]:::cascade')
        elif "agent:claude" in labels:
            lines.append(f'  {task_id}["{task_id}<br/>{desc}"]:::claude')
        else:
            lines.append(f'  {task_id}["{task_id}<br/>{desc}"]')

    lines.append("")

    # Add edges
    for task in plan["tasks"]:
        task_id = task.get("id", "")
        for dep in task.get("needs", []):
            lines.append(f"  {dep} --> {task_id};")

    # Add styling
    lines.extend([
        "",
        "classDef copilot fill:#e1f5e1,stroke:#4caf50,stroke-width:2px;",
        "classDef cascade fill:#e3f2fd,stroke:#2196f3,stroke-width:2px;",
        "classDef claude fill:#fce4ec,stroke:#e91e63,stroke-width:2px;"
    ])

    output_path.parent.mkdir(parents=True, exist_ok=True)
    with open(output_path, "w") as f:
        f.write("\n".join(lines))

    print(f"✅ Generated Mermaid diagram: {output_path}")


def main():
    """Main validation entry point."""
    # Determine paths relative to script location
    script_dir = Path(__file__).parent
    repo_root = script_dir.parent.parent
    plan_path = repo_root / "planning" / "plan.yaml"
    output_path = repo_root / "docs" / "development" / "plan.mmd"

    print(f"🔍 Validating {plan_path}...")

    # Load plan
    plan = load_plan(plan_path)

    # Run all validations
    all_errors = []

    all_errors.extend(validate_structure(plan))

    if not all_errors:  # Only continue if structure is valid
        graph, graph_errors = build_dependency_graph(plan)
        all_errors.extend(graph_errors)

        if not graph_errors:  # Only check cycles if graph is valid
            all_errors.extend(detect_cycles(graph))

        all_errors.extend(validate_labels(plan))

    # Report results
    if all_errors:
        print("\n❌ Validation failed with the following errors:\n", file=sys.stderr)
        for error in all_errors:
            print(f"  • {error}", file=sys.stderr)
        sys.exit(1)

    print("✅ All validations passed")

    # Generate Mermaid diagram
    generate_mermaid(plan, output_path)

    # Summary
    task_count = len(plan.get("tasks", []))
    print(f"\n📊 Summary:")
    print(f"  • Tasks: {task_count}")
    print(f"  • Output: {output_path}")
    print(f"\n🎉 Plan validation complete!")


if __name__ == "__main__":
    main()

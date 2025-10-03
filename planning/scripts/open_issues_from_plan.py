#!/usr/bin/env python3
"""
Create GitHub issues from plan.yaml tasks.

This script reads planning/plan.yaml and creates GitHub issues using the gh CLI.
It preserves task dependencies by adding "blocked by" references to issue bodies.

Usage:
    python planning/scripts/open_issues_from_plan.py [--dry-run]

Requirements:
    - Python 3.9+
    - pyyaml
    - gh CLI installed and authenticated
"""

import sys
import argparse
import subprocess
from pathlib import Path
from typing import Dict, List, Any
import json
import re
import yaml


class Task:
    """Represents a single task from plan.yaml."""

    def __init__(self, data: Dict[str, Any]):
        self.id = data.get("id", "")
        self.desc = data.get("desc", "")
        self.needs = data.get("needs", [])
        self.labels = data.get("labels", [])
        self.estimate = data.get("estimate")
        self.files = data.get("files", [])
        self.acceptance_criteria = data.get("acceptance_criteria", [])


class Plan:
    """Represents the entire plan.yaml structure."""

    def __init__(self, data: Dict[str, Any]):
        self.meta = data.get("meta", {})
        self.tasks = [Task(t) for t in data.get("tasks", [])]


def load_plan(plan_path: Path) -> Plan:
    """Load and parse the plan.yaml file."""
    if not plan_path.exists():
        print(f"❌ Error: {plan_path} not found", file=sys.stderr)
        sys.exit(1)

    try:
        with open(plan_path) as f:
            data = yaml.safe_load(f)
            return Plan(data)
    except yaml.YAMLError as e:
        print(f"❌ Error parsing YAML: {e}", file=sys.stderr)
        sys.exit(1)


def build_issue_body(task: Task) -> str:
    """Build the issue body with dependencies, files, and acceptance criteria."""
    parts = []

    # Description
    parts.append(task.desc)
    parts.append("")

    # Dependencies
    if task.needs:
        parts.append("## Dependencies")
        parts.append("")
        parts.append("This task is blocked by:")
        for dep in task.needs:
            parts.append(f"- **{dep}** (search for issue with this ID)")
        parts.append("")

    # Files to touch
    if task.files:
        parts.append("## Files")
        parts.append("")
        for file in task.files:
            parts.append(f"- `{file}`")
        parts.append("")

    # Acceptance criteria
    if task.acceptance_criteria:
        parts.append("## Acceptance Criteria")
        parts.append("")
        for criterion in task.acceptance_criteria:
            parts.append(f"- [ ] {criterion}")
        parts.append("")

    # Estimate
    if task.estimate:
        parts.append(f"**Estimate:** {task.estimate}")
        parts.append("")

    # Labels
    if task.labels:
        parts.append(f"**Labels:** {', '.join(task.labels)}")

    return "\n".join(parts)


def create_issue(task: Task, dry_run: bool) -> bool:
    """
    Create a GitHub issue for the given task.

    Returns:
        True if successful, False otherwise
    """
    title = f"{task.id}: {task.desc}"
    body = build_issue_body(task)
    labels = ",".join(task.labels) if task.labels else ""

    if dry_run:
        print("\n" + "=" * 80)
        print(f"Would create issue: {title}")
        print(f"Labels: {labels or 'none'}")
        print("\nBody:")
        print(body)
        print("=" * 80)
        return True

    # Build gh CLI command
    cmd = ["gh", "issue", "create", "--title", title, "--body", body]
    if labels:
        cmd.extend(["--label", labels])

    try:
        result = subprocess.run(
            cmd,
            capture_output=True,
            text=True,
            check=True
        )
        issue_url = result.stdout.strip()
        print(f"✅ Created issue: {title}")
        print(f"   {issue_url}")
        return True
    except subprocess.CalledProcessError as e:
        print(f"❌ Failed to create issue: {title}", file=sys.stderr)
        print(f"   Error: {e.stderr.strip()}", file=sys.stderr)
        return False
    except FileNotFoundError:
        print("❌ Error: 'gh' CLI not found. Please install it first:", file=sys.stderr)
        print("   https://cli.github.com/", file=sys.stderr)
        sys.exit(1)


def parse_issue_number_from_url(url: str) -> int | None:
    """Extract issue number from GitHub issue URL."""
    # Expected format: https://github.com/<owner>/<repo>/issues/<number>
    m = re.search(r"/issues/(\d+)(?:$|\s|#)", url)
    if m:
        return int(m.group(1))
    # Fallback: last path segment if digits
    try:
        tail = url.rstrip("/\n").split("/")[-1]
        return int(tail) if tail.isdigit() else None
    except Exception:
        return None


def gh_issue_comment(issue_number: int, body: str, dry_run: bool) -> bool:
    """Add a comment to an issue."""
    if dry_run:
        print(f"Would comment on issue #{issue_number}:\n{body}")
        return True
    try:
        subprocess.run(
            ["gh", "issue", "comment", str(issue_number), "--body", body],
            capture_output=True,
            text=True,
            check=True,
        )
        return True
    except subprocess.CalledProcessError as e:
        print(f"❌ Failed to comment on issue #{issue_number}: {e.stderr}", file=sys.stderr)
        return False


def gh_issue_add_label(issue_number: int, label: str, dry_run: bool) -> bool:
    """Add a label to an issue."""
    if dry_run:
        print(f"Would add label '{label}' to issue #{issue_number}")
        return True
    try:
        subprocess.run(
            ["gh", "issue", "edit", str(issue_number), "--add-label", label],
            capture_output=True,
            text=True,
            check=True,
        )
        return True
    except subprocess.CalledProcessError as e:
        print(f"❌ Failed to add label to issue #{issue_number}: {e.stderr}", file=sys.stderr)
        return False


def topological_sort(plan: Plan) -> List[Task]:
    """
    Sort tasks in topological order so dependencies are created first.

    This way, when we reference "blocked by X", issue X already exists.
    Uses Kahn's algorithm for topological sorting.
    """
    # Build adjacency list and in-degree map
    task_map = {task.id: task for task in plan.tasks}
    in_degree = {task.id: 0 for task in plan.tasks}

    # Calculate in-degrees
    for task in plan.tasks:
        # Edge is dep -> task.id, so increment in-degree of the dependent
        for dep in task.needs:
            if task.id in in_degree:
                in_degree[task.id] += 1

    # Find nodes with no incoming edges (dependencies)
    queue = [task_id for task_id, degree in in_degree.items() if degree == 0]
    sorted_tasks = []

    # Process queue
    while queue:
        # Sort queue for deterministic ordering
        queue.sort()
        task_id = queue.pop(0)
        task = task_map.get(task_id)

        if task:
            sorted_tasks.append(task)

            # Reduce in-degree of dependent tasks
            for other_task in plan.tasks:
                if task_id in other_task.needs:
                    in_degree[other_task.id] -= 1
                    if in_degree[other_task.id] == 0:
                        queue.append(other_task.id)

    # If there was a cycle or unknown dependency, some tasks may remain
    if len(sorted_tasks) != len(plan.tasks):
        remaining = set(t.id for t in plan.tasks) - set(t.id for t in sorted_tasks)
        print(
            f"⚠️  Warning: {len(remaining)} task(s) could not be ordered due to cycles or missing deps: {sorted(list(remaining))}",
            file=sys.stderr,
        )

    return sorted_tasks


def main():
    """Main entry point for the script."""
    parser = argparse.ArgumentParser(
        description="Create GitHub issues from planning/plan.yaml"
    )
    parser.add_argument(
        "--dry-run",
        action="store_true",
        help="Preview issues without creating them"
    )
    args = parser.parse_args()

    # Find repo root and plan path
    script_dir = Path(__file__).parent
    repo_root = script_dir.parent.parent
    plan_path = repo_root / "planning" / "plan.yaml"

    print(f"🔍 Loading plan from {plan_path}...")

    plan = load_plan(plan_path)

    print(f"📋 Found {len(plan.tasks)} tasks in plan")

    if args.dry_run:
        print("\n🧪 DRY RUN MODE - No issues will be created\n")
    else:
        print("\n⚠️  This will create GitHub issues. Press Ctrl+C to cancel...")
        print("   Continuing in 3 seconds...\n")
        try:
            import time
            time.sleep(3)
        except KeyboardInterrupt:
            print("\n❌ Cancelled by user")
            sys.exit(0)

    # Sort tasks topologically so dependencies are created first
    sorted_tasks = topological_sort(plan)

    # Create issues
    success_count = 0
    fail_count = 0
    task_to_issue: Dict[str, int] = {}

    for task in sorted_tasks:
        if args.dry_run:
            # Dry-run: simulate creation only
            if create_issue(task, True):
                success_count += 1
            else:
                fail_count += 1
            continue

        # Real creation
        title = f"{task.id}: {task.desc}"
        body = build_issue_body(task)
        labels = ",".join(task.labels) if task.labels else ""
        cmd = ["gh", "issue", "create", "--title", title, "--body", body]
        if labels:
            cmd.extend(["--label", labels])

        try:
            result = subprocess.run(cmd, capture_output=True, text=True, check=True)
            issue_url = result.stdout.strip()
            num = parse_issue_number_from_url(issue_url)
            if num is not None:
                task_to_issue[task.id] = num
            print(f"✅ Created issue: {title}")
            print(f"   {issue_url}")
            success_count += 1
        except subprocess.CalledProcessError as e:
            print(f"❌ Failed to create issue: {title}", file=sys.stderr)
            print(f"   Error: {e.stderr.strip()}", file=sys.stderr)
            fail_count += 1
            print("Continuing with remaining tasks...\n")

    # Write issues map artifact (only when not dry-run)
    if not args.dry_run:
        artifacts_dir = repo_root / "build" / "_artifacts"
        artifacts_dir.mkdir(parents=True, exist_ok=True)
        issues_map_path = artifacts_dir / "issues-map.json"
        with open(issues_map_path, "w") as f:
            json.dump({k: v for k, v in sorted(task_to_issue.items())}, f, indent=2)
        print(f"📝 Wrote issues map: {issues_map_path}")

        # Second pass: add real Blocked By references and status label
        for task in sorted_tasks:
            this_num = task_to_issue.get(task.id)
            if not this_num:
                continue
            if task.needs:
                blocker_nums = [task_to_issue.get(dep) for dep in task.needs if task_to_issue.get(dep)]
                if blocker_nums:
                    comment = "Blocked By: " + ", ".join(f"#{n}" for n in blocker_nums)
                    gh_issue_comment(this_num, comment, dry_run=False)
                    gh_issue_add_label(this_num, "status:blocked", dry_run=False)

    # Summary
    print("\n" + "=" * 80)
    print(f"✅ Successfully processed: {success_count}")
    if fail_count > 0:
        print(f"❌ Failed: {fail_count}")
    print("=" * 80)

    if args.dry_run:
        print("\n💡 Run without --dry-run to actually create issues")

    sys.exit(0 if fail_count == 0 else 1)


if __name__ == "__main__":
    main()

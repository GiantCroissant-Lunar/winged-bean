#!/usr/bin/env python3
"""
Monitor GitHub Actions runner minute usage
Helps track if we're staying under budget (< 500 min/month)
"""
import json
import os
import subprocess
import sys
from datetime import datetime, timedelta


def run_gh(args: list[str]) -> str:
    """Run GitHub CLI command."""
    try:
        result = subprocess.run(
            ["gh"] + args,
            capture_output=True,
            text=True,
            check=True,
            env=os.environ,
        )
        return result.stdout.strip()
    except subprocess.CalledProcessError as e:
        print(f"Error: {e.stderr}", file=sys.stderr)
        raise


def get_workflow_runs(days: int = 7) -> list:
    """Get workflow runs from last N days."""
    since = (datetime.now() - timedelta(days=days)).isoformat()

    # Get runs in JSON format
    runs_json = run_gh([
        "run", "list",
        "--limit", "100",
        "--json", "name,conclusion,createdAt,databaseId,workflowName"
    ])

    runs = json.loads(runs_json)

    # Filter to last N days
    recent_runs = []
    for run in runs:
        created = datetime.fromisoformat(run["createdAt"].replace("Z", "+00:00"))
        if created.timestamp() >= datetime.fromisoformat(since).timestamp():
            recent_runs.append(run)

    return recent_runs


def estimate_usage(runs: list) -> dict:
    """Estimate runner minute usage from runs."""
    # Rough estimates (actual times vary)
    estimated_minutes = {
        "MegaLinter": 3,
        "Qodana": 3,
        "PR Issue Link Enforcement": 0.5,
        "Validate Issue Dependencies": 1,
        "Agent Auto-Retry on Failure": 2,
        "Auto-Label Issues": 0.5,
    }

    usage_by_workflow = {}
    total_minutes = 0

    for run in runs:
        workflow_name = run.get("workflowName", run.get("name", "Unknown"))
        minutes = estimated_minutes.get(workflow_name, 1)  # Default 1 min

        if workflow_name not in usage_by_workflow:
            usage_by_workflow[workflow_name] = {"runs": 0, "minutes": 0}

        usage_by_workflow[workflow_name]["runs"] += 1
        usage_by_workflow[workflow_name]["minutes"] += minutes
        total_minutes += minutes

    return {
        "total_minutes": total_minutes,
        "by_workflow": usage_by_workflow,
        "period_days": len(runs),
    }


def main() -> int:
    """Main monitoring logic."""
    print("GitHub Actions Runner Minute Monitor")
    print("=" * 70)
    print()

    # Get runs from last 7 days
    print("Fetching workflow runs from last 7 days...")
    runs = get_workflow_runs(days=7)
    print(f"Found {len(runs)} runs")
    print()

    # Estimate usage
    usage = estimate_usage(runs)

    print("Estimated Usage (Last 7 Days)")
    print("-" * 70)
    print(f"Total Minutes: {usage['total_minutes']}")
    print()

    print("By Workflow:")
    for workflow, data in sorted(usage["by_workflow"].items(), key=lambda x: x[1]["minutes"], reverse=True):
        print(f"  {workflow:40s} {data['runs']:3d} runs × ~{data['minutes']/data['runs']:.1f} min = {data['minutes']:.0f} min")
    print()

    # Project monthly usage
    monthly_estimate = (usage['total_minutes'] / 7) * 30
    print(f"Projected Monthly Usage: {monthly_estimate:.0f} minutes")
    print()

    # Budget analysis
    budget = 500
    free_tier = 2000

    print("Budget Analysis:")
    print(f"  Target Budget: {budget} min/month")
    print(f"  Free Tier: {free_tier} min/month")
    print(f"  Projected: {monthly_estimate:.0f} min/month")

    if monthly_estimate < budget:
        print(f"  Status: ✓ Under budget ({budget - monthly_estimate:.0f} min remaining)")
    elif monthly_estimate < free_tier:
        print(f"  Status: ⚠️ Over target but within free tier")
    else:
        overage = monthly_estimate - free_tier
        cost = overage * 0.008
        print(f"  Status: ❌ Would exceed free tier")
        print(f"  Overage: {overage:.0f} minutes")
        print(f"  Cost: ${cost:.2f}/month")

    return 0


if __name__ == "__main__":
    sys.exit(main())

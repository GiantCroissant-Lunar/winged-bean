#!/usr/bin/env python3
"""
Workflow script: Handle agent PR failure
Per R-ISS-040: Agent must fix its own failures (3 retry limit)
"""
import json
import os
import re
import subprocess
import sys
from typing import List, Optional


def run_gh(args: List[str]) -> str:
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


def extract_issue_from_pr(pr_number: str) -> Optional[int]:
    """Extract linked issue number from PR."""
    pr_body = run_gh(["pr", "view", pr_number, "--json", "body", "-q", ".body"])
    pattern = r"(?:close[sd]?|fixe?[sd]?|resolve[sd]?)\s+#(\d+)"
    match = re.search(pattern, pr_body, re.IGNORECASE)
    return int(match.group(1)) if match else None


def get_retry_count(issue_number: int) -> int:
    """Get current retry count from issue labels."""
    labels_json = run_gh(["issue", "view", str(issue_number), "--json", "labels"])
    labels_data = json.loads(labels_json)
    label_names = [label["name"] for label in labels_data.get("labels", [])]

    for i in range(3, 0, -1):  # Check 3, 2, 1
        if f"ci-failed-retry-{i}" in label_names:
            return i
    return 0


def get_failure_logs(run_id: str) -> str:
    """Get failure logs from workflow run."""
    try:
        # Get failed job logs
        result = subprocess.run(
            ["gh", "run", "view", run_id, "--log-failed"],
            capture_output=True,
            text=True,
            timeout=30,
            env=os.environ,
        )
        # Get last 100 lines
        lines = result.stdout.strip().split("\n")
        return "\n".join(lines[-100:])
    except Exception as e:
        return f"Could not extract logs: {e}"


def main() -> int:
    """Main failure handling logic."""
    run_id = os.environ.get("RUN_ID")
    pr_number = os.environ.get("PR_NUMBER")
    workflow_name = os.environ.get("WORKFLOW_NAME", "CI")
    repo = os.environ.get("GITHUB_REPOSITORY")

    if not pr_number or not run_id:
        print("No PR or run ID found, skipping")
        return 0

    print(f"Handling failure for PR #{pr_number}, workflow run {run_id}")

    # Extract linked issue
    issue_number = extract_issue_from_pr(pr_number)
    if not issue_number:
        print("No linked issue found, skipping auto-retry")
        return 0

    print(f"Linked issue: #{issue_number}")

    # Check retry count
    retry_count = get_retry_count(issue_number)
    print(f"Current retry count: {retry_count}")

    if retry_count >= 3:
        print("Maximum retries (3) reached, escalating to human")

        # Add escalation comment
        comment = f"""## ⚠️ ESCALATION: Agent Failed After 3 Attempts

The assigned agent has failed to resolve this issue after 3 attempts.
**Human intervention is required.**

### Failed Attempts
Check the issue history for all 3 attempt logs and errors.

### Recommended Actions
1. **Review failure patterns** - Are all 3 failures related to the same root cause?
2. **Assess issue clarity** - Does the issue need clearer requirements or more context?
3. **Consider splitting** - Is this issue too complex and needs to be broken down?
4. **Evaluate agent selection** - Should this be assigned to a different agent?
5. **Check dependencies** - Are there hidden blockers not captured in metadata?

### Next Steps
- [ ] Human reviews all 3 failure attempts
- [ ] Determine root cause of repeated failures
- [ ] Update issue with clearer guidance OR split into smaller tasks
- [ ] Re-assign to agent or take over manually

**Per R-ISS-040:** After 3 failures, escalation to human is required.
"""
        run_gh(["issue", "comment", str(issue_number), "--body", comment])

        # Add escalation labels
        run_gh(["issue", "edit", str(issue_number), "--add-label", "ci-failed-escalate,needs-human,priority:high"])

        # Close the PR
        run_gh(["pr", "close", pr_number, "--comment", "Auto-closed after 3 failed attempts. Issue has been escalated to human review."])

        return 0

    # Increment retry count
    new_retry = retry_count + 1
    print(f"Adding retry attempt {new_retry}/3")

    # Get failure logs
    print("Extracting failure logs...")
    failure_logs = get_failure_logs(run_id)

    # Add comment to issue
    comment = f"""## 🔴 PR #{pr_number} Failed CI (Attempt {new_retry}/3)

**Workflow:** {workflow_name}
**Run ID:** {run_id}
**Run URL:** https://github.com/{repo}/actions/runs/{run_id}
**Conclusion:** failure

### Error Summary (Last 100 Lines)
```
{failure_logs}
```

### Next Steps
Per **R-ISS-040**: The agent that created this PR must analyze and fix the failure.

**Action Required:**
1. Review the error logs above
2. Analyze what went wrong
3. Fix the issue in your code
4. Create a new PR addressing this failure
5. Reference this issue with `Closes #{issue_number}`

**Retry Attempt:** {new_retry} of 3 allowed

The previous PR has been closed. Please create a new PR with your fixes.
"""

    run_gh(["issue", "comment", str(issue_number), "--body", comment])

    # Update labels
    run_gh(["issue", "edit", str(issue_number), "--add-label", f"ci-failed-retry-{new_retry}"])

    # Remove old retry label
    if retry_count > 0:
        try:
            run_gh(["issue", "edit", str(issue_number), "--remove-label", f"ci-failed-retry-{retry_count}"])
        except:
            pass

    # Close the PR
    run_gh(["pr", "close", pr_number, "--comment",
            f"Auto-closed due to CI failure. See issue #{issue_number} for failure details and retry instructions.\n\n"
            f"Per R-ISS-040, the agent will analyze the failure and create a new PR with fixes."])

    # Delete the branch
    try:
        branch = run_gh(["pr", "view", pr_number, "--json", "headRefName", "-q", ".headRefName"])
        subprocess.run(["git", "push", "origin", "--delete", branch], check=False)
        print(f"Deleted branch: {branch}")
    except:
        print("Could not delete branch")

    print(f"✓ Failure handled, retry {new_retry}/3 initiated")
    return 0


if __name__ == "__main__":
    sys.exit(main())

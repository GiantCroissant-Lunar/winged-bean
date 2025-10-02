#!/usr/bin/env python3
"""
Workflow script: Validate issue dependencies
Per R-ISS-030: Verify all blockers are closed before allowing work
"""
import json
import os
import re
import subprocess
import sys
import time
from typing import List


def run_gh(args: List[str], max_retries: int = 3) -> str:
    """Run GitHub CLI command with retry logic for transient failures."""
    for attempt in range(max_retries):
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
            # Check if it's a label-not-found error
            if "not found" in e.stderr and attempt < max_retries - 1:
                print(f"⏳ Retry {attempt + 1}/{max_retries - 1}: {e.stderr.strip()}", file=sys.stderr)
                time.sleep(2 ** attempt)  # Exponential backoff: 1s, 2s
                continue
            print(f"Error running gh command: {e.stderr}", file=sys.stderr)
            raise
    return ""


def extract_blockers(issue_body: str) -> List[int]:
    """Extract blocker issue numbers from issue body."""
    # Match "Blocked By:" with optional markdown bold markers (**) around both parts
    pattern = r"(?:\*\*)?Blocked By:?(?:\*\*)?\s*(.*)$"
    match = re.search(pattern, issue_body, re.IGNORECASE | re.MULTILINE)
    if not match:
        return []

    blocked_line = match.group(1).strip()

    # Check for explicit "None"
    if re.search(r"\bNone\b", blocked_line, re.IGNORECASE):
        return []

    # Extract issue numbers (with or without #)
    issue_pattern = r"#?(\d+)"
    blockers = [int(m.group(1)) for m in re.finditer(issue_pattern, blocked_line)]
    return blockers


def main() -> int:
    """Main validation logic."""
    issue_number = os.environ.get("ISSUE_NUMBER")
    if not issue_number:
        print("Error: ISSUE_NUMBER environment variable not set")
        return 1

    print(f"Validating dependencies for issue #{issue_number}...")
    print("Per R-ISS-030: Verify all blockers are closed before starting work")
    print()

    # Get issue body
    issue_body = run_gh(["issue", "view", issue_number, "--json", "body", "-q", ".body"])

    # Extract blockers
    blockers = extract_blockers(issue_body)
    if not blockers:
        print(f"✓ Issue #{issue_number} has no blockers")

        # Remove blocked status
        try:
            run_gh(["issue", "edit", issue_number, "--remove-label", "status:blocked"])
        except:
            pass  # Label might not exist

        run_gh(["issue", "edit", issue_number, "--add-label", "status:ready"])
        return 0

    blockers_str = ", ".join(f"#{b}" for b in blockers)
    print(f"Issue #{issue_number} has blockers: {blockers_str}")

    # Check each blocker
    open_blockers = []
    for blocker in blockers:
        state = run_gh(["issue", "view", str(blocker), "--json", "state", "-q", ".state"])
        if state == "closed":
            print(f"   ✓ Issue #{blocker} is closed")
        else:
            print(f"   ❌ Blocker #{blocker} is {state}")
            open_blockers.append(blocker)

    if open_blockers:
        print()
        print("━" * 70)
        print(f"⚠️  Issue #{issue_number} is BLOCKED")
        print("━" * 70)

        # Mark as blocked
        run_gh(["issue", "edit", issue_number, "--add-label", "status:blocked"])
        try:
            run_gh(["issue", "edit", issue_number, "--remove-label", "status:ready"])
        except:
            pass

        # Add comment
        open_blockers_str = ", ".join(f"#{b}" for b in open_blockers)
        comment = f"""## ⚠️ Cannot Start Work - Issue is Blocked

This issue cannot be worked on until the following blockers are resolved:

{open_blockers_str}

**Action Required:**
- Complete and close the blocker issues first
- Or: Update this issue's description to remove invalid blockers

**Per R-ISS-030:** Agents must verify all blockers are closed before starting work.
"""
        run_gh(["issue", "comment", issue_number, "--body", comment])

        print()
        print("Added 'status:blocked' label and comment to issue")
        return 0  # Don't fail workflow, just mark as blocked

    print()
    print("✓ All blockers are resolved")
    print(f"✓ Issue #{issue_number} is ready for work")

    # Mark as ready
    run_gh(["issue", "edit", issue_number, "--add-label", "status:ready"])
    try:
        run_gh(["issue", "edit", issue_number, "--remove-label", "status:blocked"])
    except:
        pass

    return 0


if __name__ == "__main__":
    sys.exit(main())

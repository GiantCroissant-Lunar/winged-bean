#!/usr/bin/env python3
"""
Pre-commit hook: Validate issue dependencies
Ensures commits don't reference blocked issues (per R-ISS-030)
"""
import re
import subprocess
import sys
from typing import List, Optional


def run_gh_command(args: List[str]) -> Optional[str]:
    """Run GitHub CLI command and return output."""
    try:
        result = subprocess.run(
            ["gh"] + args,
            capture_output=True,
            text=True,
            check=True,
        )
        return result.stdout.strip()
    except subprocess.CalledProcessError:
        return None
    except FileNotFoundError:
        return None


def extract_issue_number(commit_msg: str) -> Optional[int]:
    """Extract issue number from commit message."""
    # Look for "Refs #XX", "Closes #XX", "Fixes #XX", "Resolves #XX"
    pattern = r"(?:refs?|closes?|fixe?[sd]?|resolve[sd]?)\s+#(\d+)"
    match = re.search(pattern, commit_msg, re.IGNORECASE)
    return int(match.group(1)) if match else None


def extract_blockers(issue_body: str) -> List[int]:
    """Extract blocker issue numbers from issue body."""
    # Look for "Blocked By: #XX, #YY" or "Blocked By: None"
    pattern = r"Blocked By:.*"
    match = re.search(pattern, issue_body, re.IGNORECASE)
    if not match:
        return []

    blocked_line = match.group(0)

    # Check for explicit "None"
    if re.search(r"\bNone\b", blocked_line, re.IGNORECASE):
        return []

    # Extract issue numbers
    issue_pattern = r"#(\d+)"
    blockers = [int(m.group(1)) for m in re.finditer(issue_pattern, blocked_line)]
    return blockers


def check_issue_state(issue_num: int) -> Optional[str]:
    """Check if an issue is open or closed."""
    output = run_gh_command(["issue", "view", str(issue_num), "--json", "state", "-q", ".state"])
    return output


def main() -> int:
    """Main validation logic."""
    if len(sys.argv) < 2:
        print("Usage: validate_issue_dependencies.py <commit-msg-file>")
        return 1

    commit_msg_file = sys.argv[1]

    # Read commit message
    try:
        with open(commit_msg_file, "r", encoding="utf-8") as f:
            commit_msg = f.read()
    except Exception as e:
        print(f"Error reading commit message: {e}")
        return 1

    # Extract issue number
    issue_num = extract_issue_number(commit_msg)
    if not issue_num:
        print("⚠️  Warning: No issue reference found in commit message")
        print("   Consider adding 'Refs #<issue>' or 'Closes #<issue>' to link work to an issue")
        print()
        return 0  # Warning only

    print(f"✓ Found issue reference: #{issue_num}")

    # Check if gh CLI is available
    if not run_gh_command(["--version"]):
        print("⚠️  GitHub CLI (gh) not found - skipping dependency validation")
        print("   Install: brew install gh")
        return 0

    # Check if authenticated
    if run_gh_command(["auth", "status"]) is None:
        print("⚠️  Not authenticated with GitHub CLI - skipping dependency validation")
        print("   Run: gh auth login")
        return 0

    print(f"Checking dependencies for issue #{issue_num}...")

    # Get issue body
    issue_body = run_gh_command(["issue", "view", str(issue_num), "--json", "body", "-q", ".body"])
    if not issue_body:
        print(f"⚠️  Could not fetch issue #{issue_num} (may not exist or no access)")
        return 0

    # Extract blockers
    blockers = extract_blockers(issue_body)
    if not blockers:
        print(f"✓ Issue #{issue_num} has no blockers")
        return 0

    blockers_str = ", ".join(f"#{b}" for b in blockers)
    print(f"Issue #{issue_num} is blocked by: {blockers_str}")

    # Check each blocker
    open_blockers = []
    for blocker in blockers:
        state = check_issue_state(blocker)
        if state == "closed":
            print(f"   ✓ Issue #{blocker} is closed")
        else:
            print(f"   ❌ Blocker #{blocker} is {state or 'unknown'}")
            open_blockers.append(blocker)

    # If any blocker is open, block the commit
    if open_blockers:
        print()
        print("━" * 70)
        print(f"❌ COMMIT BLOCKED: Issue #{issue_num} has unresolved dependencies")
        print("━" * 70)
        print()
        print(f"You are trying to commit work on issue #{issue_num},")
        print("but it has dependencies that must be resolved first.")
        print()
        print("Open blockers:")
        for blocker in open_blockers:
            print(f"  • #{blocker}")
        print()
        print("Next steps:")
        print("1. Complete and close the blocker issues first")
        print("2. Or: Remove the issue reference from your commit message")
        print(f"3. Or: Update issue #{issue_num} to remove invalid blockers")
        print()
        print("Per R-ISS-030: Verify all blockers are closed before starting work")
        print()
        return 1

    print("✓ All blockers are resolved - commit allowed")
    return 0


if __name__ == "__main__":
    sys.exit(main())

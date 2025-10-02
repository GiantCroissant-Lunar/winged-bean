#!/usr/bin/env python3
"""
Workflow script: Validate PR has issue link
Per R-GIT-010: All PRs must link to an issue
"""
import os
import re
import sys


def extract_issue_number(text: str) -> int | None:
    """Extract issue number from text."""
    pattern = r"(?:close[sd]?|fixe?[sd]?|resolve[sd]?)\s+#(\d+)"
    match = re.search(pattern, text, re.IGNORECASE)
    return int(match.group(1)) if match else None


def main() -> int:
    """Main validation logic."""
    pr_body = os.environ.get("PR_BODY", "")
    pr_title = os.environ.get("PR_TITLE", "")

    print("Validating PR for issue link...")
    print("Per R-GIT-010: PRs must link to an issue")
    print()

    # Check body first
    issue_num = extract_issue_number(pr_body)
    if issue_num:
        print(f"✓ Issue link found in body: #{issue_num}")
        return 0

    # Check title as fallback
    issue_num = extract_issue_number(pr_title)
    if issue_num:
        print(f"✓ Issue link found in title: #{issue_num}")
        return 0

    # No link found - fail
    print("━" * 70)
    print("❌ ERROR: PR must contain 'Closes #<issue>' or 'Fixes #<issue>'")
    print("━" * 70)
    print()
    print("This PR does not link to an issue. Every PR must be tied to an issue.")
    print()
    print("Add one of these to your PR description:")
    print("  • Closes #<issue-number>")
    print("  • Fixes #<issue-number>")
    print("  • Resolves #<issue-number>")
    print()
    print("Example:")
    print("  Closes #45")
    print()
    print("  Implements IWorld interface for ECS Tier 1 contracts.")
    print()
    print("Per R-GIT-010: All PRs must link to an issue for traceability")
    print()
    return 1


if __name__ == "__main__":
    sys.exit(main())

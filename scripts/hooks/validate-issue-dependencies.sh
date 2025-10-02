#!/usr/bin/env bash
# Pre-commit hook: Validate issue dependencies
# Ensures commits don't reference blocked issues

set -euo pipefail

COMMIT_MSG_FILE="${1:-}"

if [ -z "$COMMIT_MSG_FILE" ]; then
  echo "Usage: $0 <commit-msg-file>"
  exit 1
fi

COMMIT_MSG=$(cat "$COMMIT_MSG_FILE")

# Extract issue number from commit message (Refs #XX, Closes #XX, etc.)
ISSUE_NUM=$(echo "$COMMIT_MSG" | grep -oiE '(refs?|closes?|fixe?[sd]?|resolve[sd]?) #[0-9]+' | head -1 | grep -oE '[0-9]+' || echo "")

if [ -z "$ISSUE_NUM" ]; then
  echo "⚠️  Warning: No issue reference found in commit message"
  echo "   Consider adding 'Refs #<issue>' or 'Closes #<issue>' to link work to an issue"
  echo ""
  exit 0  # Warning only for commits without issue reference
fi

echo "✓ Found issue reference: #$ISSUE_NUM"

# Check if gh CLI is available
if ! command -v gh &> /dev/null; then
  echo "⚠️  GitHub CLI (gh) not found - skipping dependency validation"
  echo "   Install: brew install gh"
  exit 0
fi

# Check if we're authenticated
if ! gh auth status &> /dev/null; then
  echo "⚠️  Not authenticated with GitHub CLI - skipping dependency validation"
  echo "   Run: gh auth login"
  exit 0
fi

echo "Checking dependencies for issue #$ISSUE_NUM..."

# Get issue body
ISSUE_BODY=$(gh issue view "$ISSUE_NUM" --json body -q .body 2>/dev/null || echo "")

if [ -z "$ISSUE_BODY" ]; then
  echo "⚠️  Could not fetch issue #$ISSUE_NUM (may not exist or no access)"
  exit 0
fi

# Extract "Blocked By: #XX, #YY"
BLOCKED_BY=$(echo "$ISSUE_BODY" | grep -oiE 'Blocked By:.*' | grep -oE '#[0-9]+' | tr -d '#' || echo "")

if [ -z "$BLOCKED_BY" ]; then
  echo "✓ Issue #$ISSUE_NUM has no blockers"
  exit 0
fi

echo "Issue #$ISSUE_NUM is blocked by: $(echo $BLOCKED_BY | tr '\n' ',' | sed 's/,/, #/g' | sed 's/^/#/')"

# Check each blocker
BLOCKED=0
for blocker in $BLOCKED_BY; do
  STATE=$(gh issue view "$blocker" --json state -q .state 2>/dev/null || echo "unknown")

  if [ "$STATE" != "closed" ]; then
    echo "❌ ERROR: Blocker issue #$blocker is still $STATE"
    BLOCKED=1
  else
    echo "   ✓ Issue #$blocker is closed"
  fi
done

if [ $BLOCKED -eq 1 ]; then
  echo ""
  echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"
  echo "❌ COMMIT BLOCKED: Issue #$ISSUE_NUM has unresolved dependencies"
  echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"
  echo ""
  echo "You are trying to commit work on issue #$ISSUE_NUM,"
  echo "but it has dependencies that must be resolved first."
  echo ""
  echo "Next steps:"
  echo "1. Complete and close the blocker issues first"
  echo "2. Or: Remove the issue reference from your commit message"
  echo "3. Or: Update issue #$ISSUE_NUM to remove invalid blockers"
  echo ""
  echo "Per R-ISS-030: Verify all blockers are closed before starting work"
  echo ""
  exit 1
fi

echo "✓ All blockers are resolved - commit allowed"
exit 0

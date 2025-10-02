# GitHub Automation - Revised Approach (Agent-First)

**Date:** 2025-10-02  
**Status:** Proposed  
**Author:** GitHub Copilot  
**Project:** WingedBean  

---

## Executive Summary

Based on team discussion, the automation approach has been **fundamentally revised** to align with three key principles:

1. **Local-First Testing** - Use `act` to test workflows locally before deploying
2. **Pre-Commit Validation** - Catch dependency issues before GitHub sees them
3. **Agent-Driven Recovery** - Let agents fix their own failures, not humans

**Key Insight:** Since agents (Copilot, Claude, Windsurf) write most code, they should also **resolve their own failures** rather than requiring human intervention.

---

## Problem with Previous Approach

### What Was Proposed (Too Human-Centric)

```yaml
# Stalled PR Recovery (OLD APPROACH)
steps:
  - Close PR
  - Delete branch
  - Keep issue open
  - Remove assignee           # ❌ Requires human to manually reassign
  - Add investigation labels  # ❌ Requires human to investigate
  - Wait for human to fix     # ❌ Agent wrote bad code, human must fix?
```

**Problem:** This assumes humans will investigate and fix agent failures. But if an agent wrote the code, **the agent should fix it**.

### What Should Happen (Agent-Driven)

```yaml
# Stalled PR Recovery (NEW APPROACH)
steps:
  - Close PR with failure details
  - Delete branch
  - Keep issue open
  - Add comment: "PR failed at step X. Reassigning to agent for retry."
  - Re-assign to SAME agent      # ✅ Agent must learn from failure
  - Agent reads failure logs
  - Agent fixes code
  - Agent creates new PR
  - If fails 3x → escalate to human
```

**Philosophy:** Agents should iterate on their own work until success (with safety limits).

---

## Principle #1: Local-First Testing with `act`

### Current Problem

**You mentioned:** "Previously, the event-driven [workflows] rarely succeeded."

**Root Cause:** Testing workflows requires:
1. Push to GitHub
2. Wait for runner
3. Check logs
4. Fix workflow
5. Repeat (costs runner minutes each time)

**Cost per iteration:** ~2-5 runner minutes × $0.008 = $0.016-$0.04

**After 50 iterations:** 100-250 minutes = $0.80-$2.00 wasted

### Solution: `act` for Local Testing

**Tool:** https://github.com/nektos/act  
**Status:** ✅ Already installed (`/opt/homebrew/bin/act`)

**Workflow:**

```bash
# Test workflow locally (FREE, instant feedback)
act pull_request -W .github/workflows/pr-enforce-issue-link.yml

# Test with specific event
act -j validate-dependencies --eventpath test-event.json

# Test all workflows
act -l  # List jobs
act     # Run all workflows
```

**Benefits:**
- ✅ **Zero cost** - No GitHub runner minutes
- ✅ **Instant feedback** - No waiting for cloud runners
- ✅ **Iterate quickly** - Fix → test → repeat in seconds
- ✅ **Offline testing** - No internet required

### Local Testing Strategy

**Phase 1: Setup `act` Testing Environment**

```bash
# Create test events for common scenarios
mkdir -p .github/workflows/test-events

# PR opened event
cat > .github/workflows/test-events/pr-opened.json << 'EOF'
{
  "pull_request": {
    "number": 123,
    "title": "Test PR",
    "body": "Closes #45",
    "user": {
      "login": "apprenticegc"
    }
  }
}
EOF

# Issue assigned event
cat > .github/workflows/test-events/issue-assigned.json << 'EOF'
{
  "issue": {
    "number": 45,
    "title": "RFC-0007-01: Create ECS contracts",
    "body": "**Blocked By:** #40\n**Agent:** GitHub Copilot",
    "labels": [
      {"name": "agent:copilot"},
      {"name": "rfc-0007"}
    ]
  }
}
EOF
```

**Phase 2: Test Workflows Locally**

```bash
# Test PR issue link validation
act pull_request \
  --eventpath .github/workflows/test-events/pr-opened.json \
  -W .github/workflows/pr-enforce-issue-link.yml

# Test dependency validation
act issues \
  --eventpath .github/workflows/test-events/issue-assigned.json \
  -W .github/workflows/validate-dependencies.yml

# If test passes locally → deploy to GitHub
# If test fails → fix and retry (no cost!)
```

**Phase 3: CI/CD Pipeline with Local Testing**

```yaml
# .github/workflows/validate-workflows.yml
name: Validate Workflows

on:
  pull_request:
    paths:
      - '.github/workflows/**'

jobs:
  test-with-act:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
      
      - name: Install act
        run: |
          curl -sSL https://raw.githubusercontent.com/nektos/act/master/install.sh | bash
          
      - name: Test all workflows locally
        run: |
          # Test each workflow with act before deploying
          for workflow in .github/workflows/*.yml; do
            echo "Testing $workflow..."
            act -W "$workflow" --dryrun || exit 1
          done
```

**Estimated Savings:**
- Without `act`: 50 iterations × 3 min = 150 min = $1.20
- With `act`: 50 iterations × 0 min = 0 min = **$0**
- **Savings:** $1.20 per workflow development cycle

---

## Principle #2: Pre-Commit Hook Validation

### Current Problem

**You mentioned:** "We can customize our pre-commit hook to assist."

**Current State:**
- ✅ Pre-commit hook exists (`.git/hooks/pre-commit`)
- ✅ Uses `pre-commit` framework
- ❌ No issue dependency validation
- ❌ No agent assignment validation

### Solution: Dependency Validation Pre-Commit Hook

**Goal:** Catch dependency issues **before** they reach GitHub

**Implementation:**

```yaml
# .pre-commit-config.yaml (ADD NEW HOOK)
repos:
  - repo: local
    hooks:
      # Existing hooks...
      
      - id: validate-issue-dependencies
        name: Validate Issue Dependencies
        entry: scripts/hooks/validate-issue-dependencies.sh
        language: script
        pass_filenames: false
        stages: [commit-msg]
        
      - id: validate-commit-issue-link
        name: Validate Commit Links to Issue
        entry: scripts/hooks/validate-commit-issue-link.sh
        language: script
        stages: [commit-msg]
```

**Hook Script:**

```bash
#!/usr/bin/env bash
# scripts/hooks/validate-issue-dependencies.sh

set -euo pipefail

COMMIT_MSG_FILE="$1"
COMMIT_MSG=$(cat "$COMMIT_MSG_FILE")

# Extract issue number from commit message
ISSUE_NUM=$(echo "$COMMIT_MSG" | grep -oE '#[0-9]+' | head -1 | tr -d '#')

if [ -z "$ISSUE_NUM" ]; then
  echo "⚠️  Warning: No issue reference found in commit message"
  echo "   Add 'Refs #<issue>' or 'Closes #<issue>' to link to issue"
  exit 0  # Warning only, don't block
fi

echo "✓ Found issue reference: #$ISSUE_NUM"

# Check if issue has unresolved blockers (requires gh CLI)
if command -v gh &> /dev/null; then
  echo "Checking dependencies for issue #$ISSUE_NUM..."
  
  ISSUE_BODY=$(gh issue view "$ISSUE_NUM" --json body -q .body 2>/dev/null || echo "")
  
  if [ -n "$ISSUE_BODY" ]; then
    # Extract "Blocked By: #XX, #YY"
    BLOCKED_BY=$(echo "$ISSUE_BODY" | grep -oE 'Blocked By:.*' | grep -oE '#[0-9]+' | tr -d '#')
    
    if [ -n "$BLOCKED_BY" ]; then
      echo "Issue #$ISSUE_NUM is blocked by: $BLOCKED_BY"
      
      for blocker in $BLOCKED_BY; do
        STATE=$(gh issue view "$blocker" --json state -q .state 2>/dev/null)
        
        if [ "$STATE" != "closed" ]; then
          echo "❌ ERROR: Issue #$blocker must be closed before working on #$ISSUE_NUM"
          echo "   Current state: $STATE"
          exit 1
        fi
      done
      
      echo "✓ All blockers are resolved"
    fi
  fi
fi

echo "✓ Issue dependency validation passed"
```

**Benefits:**
- ✅ Catches blocked issues **before commit**
- ✅ Prevents wasted work on blocked tasks
- ✅ Fast (runs locally, no network delay)
- ✅ Optional (warning-only mode if `gh` not available)

### Validation Workflow

```
Developer → git commit
  ↓
Pre-commit hook validates issue dependencies
  ↓
If blocked → reject commit with clear error
If clear → allow commit
  ↓
Push to GitHub (only if validation passed)
```

---

## Principle #3: Agent-Driven Recovery

### Current Problem

**You mentioned:** "Agent should resolve the PR unsuccessful problems instead of human"

**Agreed!** If an agent wrote code that failed CI, **that agent should fix it**.

### Solution: Agent Auto-Retry with Learning

**Recovery Flow:**

```
PR fails CI
  ↓
Workflow captures:
  • Failure reason (test failures, build errors, lint errors)
  • Failed step (compile, test, lint)
  • Error logs (last 50 lines)
  ↓
Add comment to issue:
  "PR #123 failed at step 'dotnet test'
   
   Error:
   ```
   Test 'SaveManagerTests.SaveWorldState' failed
   Expected: true, Actual: false
   ```
   
   @copilot Please review failure and create new PR."
  ↓
Keep assignee (don't remove)
Add label: "ci-failed-retry-1"
  ↓
Agent (Copilot/Claude/Windsurf):
  • Reads failure logs in issue comment
  • Analyzes what went wrong
  • Fixes code
  • Creates new PR
  ↓
If PR succeeds → close issue
If PR fails again → "ci-failed-retry-2"
If fails 3x → "ci-failed-escalate" + notify human
```

**Implementation:**

```yaml
# .github/workflows/agent-auto-retry.yml
name: Agent Auto-Retry on Failure

on:
  workflow_run:
    workflows: ["CI"]
    types: [completed]

jobs:
  handle-failure:
    if: ${{ github.event.workflow_run.conclusion == 'failure' }}
    runs-on: ubuntu-latest
    permissions:
      issues: write
      pull-requests: write
      actions: read
    steps:
      - name: Get PR and Issue info
        id: info
        run: |
          # Extract PR number from workflow run
          PR_NUM=$(gh api repos/${{ github.repository }}/actions/runs/${{ github.event.workflow_run.id }} \
            --jq '.pull_requests[0].number')
          echo "pr_number=$PR_NUM" >> $GITHUB_OUTPUT
          
          # Extract linked issue
          PR_BODY=$(gh pr view "$PR_NUM" --json body -q .body)
          ISSUE_NUM=$(echo "$PR_BODY" | grep -oE 'Closes #[0-9]+' | grep -oE '[0-9]+')
          echo "issue_number=$ISSUE_NUM" >> $GITHUB_OUTPUT
          
      - name: Get failure logs
        id: logs
        run: |
          # Get last 50 lines of failed job logs
          gh run view ${{ github.event.workflow_run.id }} --log-failed > failure.log || true
          
          # Extract relevant error (last 50 lines)
          tail -50 failure.log > failure-summary.txt
          
      - name: Check retry count
        id: retry
        run: |
          # Check existing labels
          LABELS=$(gh issue view ${{ steps.info.outputs.issue_number }} --json labels -q '.labels[].name')
          
          RETRY_COUNT=0
          if echo "$LABELS" | grep -q "ci-failed-retry-1"; then
            RETRY_COUNT=1
          fi
          if echo "$LABELS" | grep -q "ci-failed-retry-2"; then
            RETRY_COUNT=2
          fi
          if echo "$LABELS" | grep -q "ci-failed-retry-3"; then
            RETRY_COUNT=3
          fi
          
          echo "retry_count=$RETRY_COUNT" >> $GITHUB_OUTPUT
          
      - name: Add failure comment for agent
        if: steps.retry.outputs.retry_count < 3
        run: |
          RETRY_NUM=$((steps.retry.outputs.retry_count + 1))
          
          gh issue comment ${{ steps.info.outputs.issue_number }} --body "
          ## 🔴 PR #${{ steps.info.outputs.pr_number }} Failed (Attempt $RETRY_NUM/3)
          
          **Workflow:** ${{ github.event.workflow_run.name }}
          **Run ID:** ${{ github.event.workflow_run.id }}
          **Conclusion:** ${{ github.event.workflow_run.conclusion }}
          
          ### Error Summary
          \`\`\`
          $(cat failure-summary.txt)
          \`\`\`
          
          ### Next Steps
          @${{ github.event.workflow_run.triggering_actor.login }} (Agent):
          1. Review the error logs above
          2. Analyze what went wrong
          3. Fix the issue
          4. Create a new PR addressing this failure
          
          **Retry:** $RETRY_NUM of 3 allowed
          "
          
          # Update label
          gh issue edit ${{ steps.info.outputs.issue_number }} \
            --add-label "ci-failed-retry-$RETRY_NUM"
            
      - name: Close failed PR
        if: steps.retry.outputs.retry_count < 3
        run: |
          gh pr close ${{ steps.info.outputs.pr_number }} \
            --comment "Auto-closed due to CI failure. See issue for details and retry."
          
          # Delete branch
          BRANCH=$(gh pr view ${{ steps.info.outputs.pr_number }} --json headRefName -q .headRefName)
          git push origin --delete "$BRANCH" || true
          
      - name: Escalate to human
        if: steps.retry.outputs.retry_count >= 3
        run: |
          gh issue comment ${{ steps.info.outputs.issue_number }} --body "
          ## ⚠️ ESCALATION: Agent failed 3 times
          
          The assigned agent has failed to resolve this issue after 3 attempts.
          Human intervention is required.
          
          **Failed PRs:**
          - Check issue history for all attempts
          
          **Action Required:**
          - Review failure patterns
          - Determine if issue needs to be split
          - Provide clearer requirements
          - Or: Assign to different agent
          "
          
          gh issue edit ${{ steps.info.outputs.issue_number }} \
            --add-label "ci-failed-escalate" \
            --add-label "needs-human"
```

**Key Features:**

1. **Automatic retry** - Agent gets 3 attempts
2. **Failure context** - Full error logs in issue comment
3. **No human intervention** - Agent reads logs and fixes
4. **Escalation path** - After 3 failures, notify human
5. **Learning opportunity** - Agent must analyze its own mistakes

### Benefits

- ✅ **Agents learn** - Must fix their own mistakes
- ✅ **Humans focus on hard problems** - Only escalated after 3 failures
- ✅ **Fast iteration** - Agent can retry immediately
- ✅ **Cost effective** - No manual investigation overhead

---

## Principle #4: Agent Rules in `.agent/` Folder

### Current Problem

**You mentioned:** "We may need to update the rule to .agent folder so that when any agent is making issues, it follows."

**Current State:**
- ✅ `.agent/base/20-rules.md` exists with 28 rules
- ❌ No rules about issue creation
- ❌ No rules about dependency management
- ❌ No rules about agent assignment

### Solution: Add Issue Management Rules

**New Rules to Add:**

```markdown
# .agent/base/20-rules.md (ADD THESE)

## Issue Management
R-ISS-010: When creating issues, always specify dependencies in the issue body.
  - Format: `**Blocked By:** #XX, #YY` (comma-separated issue numbers)
  - Format: `**Blocks:** #ZZ` (issues that depend on this one)
  
R-ISS-020: When creating issues, specify the intended agent.
  - Use labels: `agent:copilot`, `agent:claude-code`, or `agent:windsurf`
  - If unsure, use `agent:unassigned` and let human decide
  
R-ISS-030: Before starting work on an issue, verify all blockers are closed.
  - Check `**Blocked By:**` field in issue body
  - Query each blocker's status
  - If any blocker is open, do not start work
  
R-ISS-040: When a PR fails CI, the agent that created it must fix it.
  - Read failure logs in issue comments
  - Analyze what went wrong
  - Create new PR with fixes
  - Do not require human intervention unless 3 attempts fail
  
R-ISS-050: Issue titles must follow naming convention.
  - Format: `RFC-XXXX-YY: Short description` (for RFC-related work)
  - Format: `[COMPONENT] Short description` (for general work)
  - Examples: `RFC-0007-01: Create ECS contracts`, `[CI] Fix MegaLinter timeout`
```

**Update `.agent/adapters/copilot.md`:**

```markdown
# .agent/adapters/copilot.md (ADD SECTION)

## Issue Creation (per R-ISS-010, R-ISS-020, R-ISS-050)

When creating issues:
1. Specify dependencies: `**Blocked By:** #40` if work depends on other issues
2. Add agent label: `agent:copilot` (GitHub CLI will do this automatically)
3. Follow naming: `RFC-XXXX-YY: Description` for RFC work
4. Include acceptance criteria checklist

Example:
```
Issue #45: RFC-0007-01: Create ECS contracts

**Blocked By:** None
**Blocks:** #46, #47
**Agent:** GitHub Copilot

## Summary
Create Tier 1 ECS contracts (IWorld, IEntity, IComponent)

## Acceptance Criteria
- [ ] IWorld interface defined
- [ ] IEntity interface defined
- [ ] IComponent interface defined
- [ ] XML documentation complete
- [ ] Follows netstandard2.1 targeting (R-CODE-050)
```

## Dependency Validation (per R-ISS-030)

Before starting work:
```bash
# Check if issue has blockers
gh issue view 45 --json body | grep "Blocked By"

# If blockers found, verify they're all closed
for blocker in $(echo "$BLOCKED_BY" | grep -oE '[0-9]+'); do
  gh issue view $blocker --json state
done
```

If any blocker is open, comment on issue:
"Cannot start work on this issue. Blocked by #XX (status: open)"

## Failure Recovery (per R-ISS-040)

When your PR fails CI:
1. Read failure logs in issue comment
2. Identify root cause
3. Fix the issue
4. Create new PR (reference same issue)
5. Do not wait for human to investigate

Example recovery flow:
```
PR #123 failed: "Test SaveManagerTests.SaveWorldState failed"
  ↓
Read test failure details
  ↓
Find bug: SaveManager.Save() not handling null world
  ↓
Fix: Add null check in SaveManager.Save()
  ↓
Create new PR #124 with fix
```
```

**Benefits:**
- ✅ All agents follow same rules
- ✅ Rules are version-controlled
- ✅ Easy to reference: "Per R-ISS-030, check blockers"
- ✅ Consistent across Copilot, Claude, Windsurf

---

## Revised Architecture: Local-First, Agent-Driven

### Phase 0: Setup (2-3 hours)

**0.1: Setup `act` Testing Environment**

```bash
# Create test event fixtures
mkdir -p .github/workflows/test-events

# Add act configuration
cat > .actrc << 'EOF'
-P ubuntu-latest=catthehacker/ubuntu:act-latest
--container-daemon-socket -
EOF

# Add to .gitignore
echo ".actrc" >> .gitignore
echo ".github/workflows/test-events/*.json" >> .gitignore
```

**Effort:** 30 minutes

---

**0.2: Create Pre-Commit Hooks**

```bash
# Add dependency validation hook
cat > scripts/hooks/validate-issue-dependencies.sh << 'EOF'
#!/usr/bin/env bash
# (Implementation from Principle #2)
EOF
chmod +x scripts/hooks/validate-issue-dependencies.sh

# Update .pre-commit-config.yaml
# (Add hooks from Principle #2)
```

**Effort:** 1-2 hours

---

**0.3: Update Agent Rules**

```bash
# Add issue management rules to .agent/base/20-rules.md
# (R-ISS-010 through R-ISS-050)

# Update adapter docs
# - .agent/adapters/copilot.md
# - .agent/adapters/claude.md
# - .agent/adapters/windsurf.md
```

**Effort:** 1 hour

---

### Phase 1: Core Workflows (4-6 hours)

**1.1: PR Issue Link Enforcement**

```yaml
# .github/workflows/pr-enforce-issue-link.yml
name: Enforce PR Issue Link

on:
  pull_request:
    types: [opened, edited, synchronize]

jobs:
  validate:
    runs-on: ubuntu-latest
    steps:
      - name: Check for issue link
        run: |
          PR_BODY="${{ github.event.pull_request.body }}"
          
          if ! echo "$PR_BODY" | grep -qiE 'close[sd]? #[0-9]+|fixe?[sd]? #[0-9]+|resolve[sd]? #[0-9]+'; then
            echo "❌ ERROR: PR body must contain 'Closes #<issue>' or 'Fixes #<issue>'"
            exit 1
          fi
          
          echo "✓ Issue link found"
```

**Test Locally:**

```bash
act pull_request \
  --eventpath .github/workflows/test-events/pr-opened.json \
  -W .github/workflows/pr-enforce-issue-link.yml
```

**Effort:** 1 hour (including local testing)

---

**1.2: Dependency Validation Workflow**

```yaml
# .github/workflows/validate-dependencies.yml
name: Validate Issue Dependencies

on:
  issues:
    types: [assigned, labeled]

jobs:
  validate:
    runs-on: ubuntu-latest
    permissions:
      issues: write
    steps:
      - name: Check if issue is blocked
        run: |
          ISSUE_BODY=$(gh issue view ${{ github.event.issue.number }} --json body -q .body)
          BLOCKED_BY=$(echo "$ISSUE_BODY" | grep -oE 'Blocked By:.*' | grep -oE '#[0-9]+' | tr -d '#')
          
          if [ -n "$BLOCKED_BY" ]; then
            echo "Issue has blockers: $BLOCKED_BY"
            
            for blocker in $BLOCKED_BY; do
              STATE=$(gh issue view "$blocker" --json state -q .state)
              
              if [ "$STATE" != "closed" ]; then
                echo "❌ Blocker #$blocker is still $STATE"
                gh issue edit ${{ github.event.issue.number }} \
                  --add-label "status:blocked" \
                  --remove-label "status:ready"
                gh issue comment ${{ github.event.issue.number }} \
                  --body "Cannot start work. Blocked by #$blocker (status: $STATE)"
                exit 1
              fi
            done
            
            echo "✓ All blockers resolved"
          fi
          
          gh issue edit ${{ github.event.issue.number }} \
            --add-label "status:ready" \
            --remove-label "status:blocked"
```

**Test Locally:**

```bash
act issues \
  --eventpath .github/workflows/test-events/issue-assigned.json \
  -W .github/workflows/validate-dependencies.yml
```

**Effort:** 2-3 hours (including local testing)

---

**1.3: Agent Auto-Retry on Failure**

```yaml
# .github/workflows/agent-auto-retry.yml
# (Full implementation from Principle #3)
```

**Test Locally:**

```bash
act workflow_run \
  --eventpath .github/workflows/test-events/ci-failed.json \
  -W .github/workflows/agent-auto-retry.yml
```

**Effort:** 2-3 hours (including local testing)

---

### Phase 2: Issue Templates (1-2 hours)

**2.1: Agent Task Template**

```yaml
# .github/ISSUE_TEMPLATE/agent-task.yml
name: Agent Task
description: Create a task for an AI coding agent
title: "[COMPONENT] Short description"
labels: ["agent"]
body:
  - type: input
    id: blocked_by
    attributes:
      label: Blocked By
      description: Issue numbers that must be closed first (comma-separated)
      placeholder: "40, 45"
      
  - type: input
    id: blocks
    attributes:
      label: Blocks
      description: Issue numbers that depend on this one (comma-separated)
      placeholder: "47, 48"
      
  - type: dropdown
    id: agent
    attributes:
      label: Assigned Agent
      description: Which agent should work on this?
      options:
        - GitHub Copilot
        - Claude Code
        - Windsurf
        - Human
        - Unassigned
      default: 4
      
  - type: textarea
    id: summary
    attributes:
      label: Summary
      description: One-paragraph description of the task
    validations:
      required: true
      
  - type: textarea
    id: acceptance
    attributes:
      label: Acceptance Criteria
      description: Checklist the agent must satisfy
      value: |
        - [ ] Code compiles without errors
        - [ ] Tests pass (if applicable)
        - [ ] Follows .editorconfig
        - [ ] PR links to this issue
    validations:
      required: true
      
  - type: textarea
    id: scope
    attributes:
      label: Scope Boundaries
      description: What is IN scope and OUT of scope?
      
  - type: textarea
    id: references
    attributes:
      label: References
      description: Links to RFCs, ADRs, or related docs
```

**Auto-Label Workflow:**

```yaml
# .github/workflows/auto-label-issues.yml
name: Auto-Label Issues

on:
  issues:
    types: [opened, edited]

jobs:
  label:
    runs-on: ubuntu-latest
    permissions:
      issues: write
    steps:
      - name: Add agent label
        run: |
          ISSUE_BODY=$(gh issue view ${{ github.event.issue.number }} --json body -q .body)
          
          if echo "$ISSUE_BODY" | grep -q "Assigned Agent.*GitHub Copilot"; then
            gh issue edit ${{ github.event.issue.number }} --add-label "agent:copilot"
          elif echo "$ISSUE_BODY" | grep -q "Assigned Agent.*Claude Code"; then
            gh issue edit ${{ github.event.issue.number }} --add-label "agent:claude-code"
          elif echo "$ISSUE_BODY" | grep -q "Assigned Agent.*Windsurf"; then
            gh issue edit ${{ github.event.issue.number }} --add-label "agent:windsurf"
          fi
          
      - name: Parse and add dependency labels
        run: |
          ISSUE_BODY=$(gh issue view ${{ github.event.issue.number }} --json body -q .body)
          BLOCKED_BY=$(echo "$ISSUE_BODY" | grep -oE 'Blocked By:.*' | grep -oE '#[0-9]+' | tr -d '#')
          
          if [ -n "$BLOCKED_BY" ]; then
            gh issue edit ${{ github.event.issue.number }} --add-label "has-blockers"
          fi
```

**Effort:** 1-2 hours

---

## Runner Minute Budget (Revised)

### Estimated Monthly Consumption

| Workflow | Trigger | Runs/Month | Min/Run | Total |
|----------|---------|------------|---------|-------|
| `pr-enforce-issue-link.yml` | PR opened/sync | 50 | 0.5 | 25 |
| `validate-dependencies.yml` | Issue assigned | 30 | 1 | 30 |
| `agent-auto-retry.yml` | CI failure | 10 | 2 | 20 |
| `auto-label-issues.yml` | Issue created | 30 | 0.5 | 15 |
| CI/CD (existing) | PR/push | 100 | 3 | 300 |
| **Total** | | | | **390** |

**Buffer:** 110 minutes/month  
**Cost:** $0 (within free tier 2,000 min/month)

### Development Savings with `act`

**Without `act`:**
- 50 workflow iterations × 3 min = 150 min
- Cost: $1.20 per development cycle

**With `act`:**
- 50 workflow iterations × 0 min = 0 min (local)
- Cost: $0

**Savings:** $1.20 per workflow × 4 workflows = **$4.80 saved during development**

---

## Comparison: Manual vs. Agent-Driven Recovery

### Manual Recovery (Old Approach)

```
PR fails CI
  ↓
Human investigates (30 min)
  ↓
Human fixes code (1-2 hours)
  ↓
Create new PR
  ↓
Total time: 1.5-2.5 hours human time
```

### Agent-Driven Recovery (New Approach)

```
PR fails CI
  ↓
Agent reads logs (instant)
  ↓
Agent fixes code (5-10 min)
  ↓
Create new PR (instant)
  ↓
Total time: 5-10 min, zero human time
```

**Savings:** 1.5-2.5 hours per failure × $50/hour = **$75-125 saved per failure**

---

## Decision Matrix: What to Build

| Component | Local `act` Testing | Pre-Commit Hook | GitHub Workflow | Priority |
|-----------|---------------------|-----------------|-----------------|----------|
| Issue dependency validation | ✅ Yes | ✅ Yes | ✅ Yes | P0 |
| PR issue link enforcement | ✅ Yes | ❌ No | ✅ Yes | P0 |
| Agent auto-retry | ✅ Yes | ❌ No | ✅ Yes | P1 |
| Issue auto-labeling | ✅ Yes | ❌ No | ✅ Yes | P2 |

---

## Implementation Checklist

### Week 1: Foundation

- [ ] Setup `act` test environment (30 min)
- [ ] Create test event fixtures (30 min)
- [ ] Add pre-commit dependency validation hook (1-2h)
- [ ] Update agent rules in `.agent/base/20-rules.md` (1h)
- [ ] Test pre-commit hook locally (30 min)

**Total:** 3-4 hours

### Week 2: Core Workflows

- [ ] Create `pr-enforce-issue-link.yml` (1h)
- [ ] Test with `act` (30 min)
- [ ] Create `validate-dependencies.yml` (2h)
- [ ] Test with `act` (30 min)
- [ ] Create `agent-auto-retry.yml` (2h)
- [ ] Test with `act` (1h)
- [ ] Deploy to GitHub (test with real PRs) (1h)

**Total:** 8 hours

### Week 3: Issue Templates

- [ ] Create `agent-task.yml` template (1h)
- [ ] Create `auto-label-issues.yml` (1h)
- [ ] Test with real issues (30 min)
- [ ] Document workflow in PLAYBOOK.md (1h)

**Total:** 3.5 hours

### Week 4: Iteration

- [ ] Monitor workflow success rates
- [ ] Adjust retry limits if needed
- [ ] Refine error messages
- [ ] Add more test cases

---

## Open Questions

### Q1: Agent Retry Limit

**Options:**
- A) 3 attempts (balanced)
- B) 5 attempts (generous)
- C) 1 attempt (strict, escalate quickly)

**Recommendation:** Start with 3, adjust based on data

---

### Q2: Pre-Commit Hook Strictness

**Options:**
- A) Hard block (fail commit if blockers exist)
- B) Warning only (allow commit with warning)

**Recommendation:** Warning only (developers may need to work ahead)

---

### Q3: Agent Failure Notification

**Options:**
- A) Only notify human after 3 failures
- B) Notify on every failure (noisy)
- C) Daily digest of failures

**Recommendation:** Option A (only after 3 failures)

---

## Success Metrics

### Developer Productivity

- **Target:** 90% of PR failures resolved by agents (not humans)
- **Measure:** Count of `ci-failed-escalate` labels (should be < 10%)

### Runner Minutes

- **Target:** < 500 min/month
- **Measure:** GitHub Actions usage dashboard
- **Alert:** If > 400 min/month, review workflows

### Dependency Violations

- **Target:** Zero work started on blocked issues
- **Measure:** Count of "blocked by" comments on PRs
- **Alert:** If > 0, improve pre-commit validation

---

## Next Steps

1. **This Week:**
   - [ ] Team reviews this approach
   - [ ] Approve use of `act` for local testing
   - [ ] Approve agent-driven recovery (vs. manual)
   - [ ] Answer Q1-Q3

2. **Week 1-2:**
   - [ ] Implement Phase 0 (foundation)
   - [ ] Test all workflows with `act`

3. **Week 3-4:**
   - [ ] Deploy to GitHub
   - [ ] Monitor agent retry rates
   - [ ] Iterate based on data

---

## References

- `act` documentation: https://github.com/nektos/act
- Pre-commit framework: https://pre-commit.com
- Previous analysis: `github-automation-critical-issues-analysis.md`
- Agent rules: `.agent/base/20-rules.md`

---

**Status:** Awaiting team approval  
**Priority:** P0 (Foundation for all automation)  
**Estimated Effort:** 14-18 hours total  
**Cost Savings:** $330/month (vs. ref-project) + $75-125 per agent failure

---

**Last Updated:** 2025-10-02

# GitHub Automation - Critical Issues Analysis

**Date:** 2025-10-02  
**Status:** Discussion  
**Author:** GitHub Copilot  
**Project:** WingedBean  

---

## Executive Summary

After reviewing the `ithome-ironman-2025` workflows and discussing with the team, **three critical blockers** prevent adoption:

1. 🔴 **BLOCKER #1:** Runner minute consumption (~43,230 min/month = $330/month cost)
2. 🔴 **BLOCKER #2:** No cross-agent issue dependency management
3. 🔴 **BLOCKER #3:** Stalled PR cleanup doesn't handle linked issues properly

**Revised Recommendation:** **DO NOT adopt any workflows** until these foundational problems are solved.

---

## Problem #1: Runner Minute Explosion 💸

### Current State in `ithome-ironman-2025`

The reference project uses **13 cron-based workflows** with aggressive polling:

| Workflow | Frequency | Runs/Day | Est. Min/Day | Monthly Cost |
|----------|-----------|----------|--------------|--------------|
| `auto-approve-workflows.yml` | Every 2 min | 720 | 360 | $86.40 |
| `flow-monitor-shadow.yml` | Every 5 min | 288 | 288 | $69.12 |
| `pr-flow-monitor.yml` | Every 5 min | 288 | 288 | $69.12 |
| `chain-health-scan.yml` | Every 10 min | 144 | 144 | $34.56 |
| `rfc-assign-cron.yml` | Every 10 min | 144 | 144 | $34.56 |
| `rfc-cleanup-duplicates.yml` | Every 10 min | 144 | 144 | $34.56 |
| `test-diagnostic-workflow.yml` | Every 30 min | 48 | 48 | $11.52 |
| Other daily jobs | Various | — | 25 | $6.00 |

**Total Monthly Cost:** ~$329.84 (41,230 overage minutes @ $0.008/min)

### Why This Happens

❌ **Anti-Pattern:** Polling instead of event-driven workflows

```yaml
# BAD: Polls every 2 minutes looking for work
on:
  schedule:
    - cron: '*/2 * * * *'  # 720 runs/day!
```

```yaml
# GOOD: Only runs when actual event occurs
on:
  pull_request:
    types: [opened, synchronize]
```

### Impact on WingedBean

If we adopted these workflows as-is:

- **Free Tier (2,000 min/month):** Overrun by Day 2 (~1,441 min/day)
- **Team Plan (3,000 min/month):** Overrun by Day 3
- **Enterprise (50,000 min/month):** Overrun by Day 35

**Conclusion:** Cron-based polling is **financially unsustainable** for any tier.

---

## Problem #2: Cross-Agent Issue Dependency Management 🔗

### Current State

**WingedBean has multiple agents:**
- GitHub Copilot (CLI-based, PR-driven)
- Claude Code (deep analysis, architectural work)
- Windsurf (context-aware editing)

**Problem:** When different agents create issues, dependencies are not tracked.

**Example Scenario:**

```
RFC-0012: Save/Load System
├─ Issue #45: Design state serialization (Claude Code)
│  └─ Depends on: RFC-0007 (ECS implementation)
├─ Issue #46: Implement save manager (Copilot)
│  └─ Depends on: Issue #45 (design must be done first)
└─ Issue #47: Add load UI (Windsurf)
   └─ Depends on: Issue #46 (save manager must exist)
```

**Current Reality:**
- ❌ Issue #46 gets assigned to Copilot before #45 is done
- ❌ Copilot starts work without design doc
- ❌ PR fails CI because ECS contracts don't exist
- ❌ Wasted runner minutes, wasted agent time

### What `ithome-ironman-2025` Does (Inadequate)

1. **Micro-issue numbering** (RFC-014-01, RFC-014-02, etc.)
   - Assumes strict sequential execution
   - No cross-RFC dependencies
   - Breaks when issues are created out of order

2. **Assignment mutex** (`rfc_assignment_mutex.py`)
   - Only prevents concurrent assignment in same RFC series
   - Doesn't check if dependencies are complete
   - No validation of readiness

3. **No dependency graph**
   - No way to express "Issue A blocks Issue B"
   - No way to prevent premature assignment

### What We Need

A **dependency-aware issue management system** that:

✅ Tracks cross-issue dependencies (not just RFC sequences)
✅ Validates all blockers are resolved before assignment
✅ Works across different agents (Copilot, Claude, Windsurf)
✅ Supports parallel work when dependencies allow
✅ Prevents wasted work on blocked issues

### GitHub Native Options

**Option A: Task Lists (GitHub Projects Beta)**

```markdown
## Issue #45: Design state serialization

**Dependencies:**
- #40 RFC-0007 ECS implementation

**Blocks:**
- #46 Implement save manager
- #47 Add load UI
```

**Pros:**
- Native GitHub feature
- Visual dependency graph
- No custom code needed

**Cons:**
- Requires GitHub Projects (manual setup)
- No automated enforcement
- Can't prevent assignment if blockers exist

---

**Option B: Custom Labels + Workflow Validation**

```yaml
# Issue labels
dependencies: blocked-by:#45
status: blocked
```

```yaml
# Workflow validation
- name: Check dependencies before assignment
  run: |
    ISSUE_BODY=$(gh issue view $ISSUE_NUM --json body -q .body)
    BLOCKED_BY=$(echo "$ISSUE_BODY" | grep -oP 'blocked-by:#\K\d+' || echo "")
    
    if [ -n "$BLOCKED_BY" ]; then
      STATUS=$(gh issue view "$BLOCKED_BY" --json state -q .state)
      if [ "$STATUS" != "closed" ]; then
        echo "ERROR: Issue #$BLOCKED_BY must be closed first"
        exit 1
      fi
    fi
```

**Pros:**
- Automated enforcement
- Works with any agent
- Prevents premature assignment

**Cons:**
- Requires custom workflow logic
- Label parsing is brittle
- Doesn't visualize dependency graph

---

**Option C: Issue Templates with Dependency Field**

```yaml
# .github/ISSUE_TEMPLATE/agent-task.yml
- type: input
  id: blocked_by
  attributes:
    label: Blocked By
    description: Issue numbers that must be closed first (comma-separated)
    placeholder: "45, 46"

- type: input
  id: blocks
  attributes:
    label: Blocks
    description: Issue numbers that depend on this (comma-separated)
    placeholder: "47, 48"
```

**Pros:**
- Structured data entry
- Easy to parse
- No manual label management

**Cons:**
- Users can still skip/misenter data
- Needs validation workflow
- No automatic graph generation

---

**Option D: External Dependency Tracker (JSON file)**

```json
// .github/issue-dependencies.json
{
  "dependencies": {
    "45": [],
    "46": ["45"],
    "47": ["46"],
    "48": ["45", "46"]
  }
}
```

```yaml
# Workflow validates against this file
- name: Validate dependencies
  run: python scripts/validate_issue_dependencies.py $ISSUE_NUM
```

**Pros:**
- Central source of truth
- Easy to query/visualize
- Version controlled

**Cons:**
- Manual file updates (error-prone)
- Gets out of sync with issues
- Requires custom tooling

---

### Recommended Solution: Hybrid Approach

**Phase 1: Issue Template + Label-Based Blocking**

1. Add dependency fields to issue templates
2. Bot auto-adds `blocked-by:#XX` labels from template
3. Workflow validates blockers before assignment
4. Workflow adds `status:blocked` label if blockers exist

**Phase 2: Automated Dependency Extraction**

1. Parse issue body for `Blocked By: #45, #46`
2. Create bidirectional links in issue comments
3. Bot updates `status:blocked` → `status:ready` when blockers close

**Phase 3: Visual Dependency Graph**

1. Generate dependency graph from issue metadata
2. Commit to `docs/status/issue-dependencies.dot`
3. Render as SVG in README

**Estimated Effort:** 
- Phase 1: 4-6 hours
- Phase 2: 8-10 hours
- Phase 3: 4-6 hours

---

## Problem #3: Stalled PR Cleanup Doesn't Handle Issues ⚠️

### Current Behavior in `cleanup-stalled-prs.yml`

```yaml
if [ -n "$ISSUE_NUM" ]; then
  # Calls cleanup_recreate_issue.py
  # 1. Closes the PR
  # 2. Deletes the branch
  # 3. CLOSES THE ORIGINAL ISSUE  ❌
  # 4. CREATES A NEW ISSUE        ❌
  # 5. Re-assigns to Copilot
else
  # Just closes PR and deletes branch
  # Leaves original issue OPEN with no PR  ❌
fi
```

### Problems

**Scenario A: Issue Found**
1. ❌ **Closes original issue** (loses history, comments, context)
2. ❌ **Creates duplicate issue** (breaks dependency tracking)
3. ❌ **Re-assigns to Copilot** (might fail again for same reason)

**Scenario B: No Issue Found**
1. ❌ **Orphaned work** (PR closed, but issue still open)
2. ❌ **Status desync** (issue shows "In Progress" but PR is gone)
3. ❌ **Manual cleanup required** (someone has to close the issue)

### What Should Happen

**Scenario A: Issue Found (Preferred Flow)**

1. ✅ Close the stalled PR
2. ✅ Delete the branch
3. ✅ **KEEP the original issue open**
4. ✅ **Add comment to issue:**
   ```
   Previous PR #123 was closed due to inactivity (48h stalled).
   
   Branch: copilot/rfc-0007-ecs-integration
   Reason: CI failed, no updates for 48 hours
   
   This issue remains open. Next steps:
   - Review failure logs: [link]
   - Consider if issue needs splitting
   - Re-assign to agent when ready
   ```
5. ✅ **Remove assignee** (don't auto-reassign)
6. ✅ **Add label:** `stalled-pr-cleanup`, `needs-investigation`

**Scenario B: No Issue Found**

1. ✅ Close the stalled PR
2. ✅ Delete the branch
3. ✅ Add comment:
   ```
   Auto-closed: No linked issue found, stalled for 48h.
   
   If this PR should be reopened, please:
   1. Create or link an issue
   2. Add `Closes #<issue>` to PR body
   3. Investigate why PR stalled
   ```

### Why Don't Recreate Issues?

**Problems with recreation:**

1. **Loses context** - Comments, discussion, history gone
2. **Breaks dependencies** - Issue #46 blocked by #45, but #45 is now #52
3. **Confuses agents** - Different issue numbers, same task
4. **Audit trail loss** - Can't track why work failed
5. **GitHub limitations** - PRs can't be deleted, only closed

**Better approach:** Keep issues open, learn from failures

---

## Problem #4: Multi-Agent Coordination (Hidden Issue)

### The Real Problem

WingedBean uses **three different agents** with different strengths:

| Agent | Best For | Workflow |
|-------|----------|----------|
| **GitHub Copilot** | Small, incremental tasks | Issue → PR → Merge |
| **Claude Code** | Deep analysis, refactoring | Interactive, multi-file |
| **Windsurf** | Context-aware editing | Real-time assistance |

**Current workflow assumptions:**
- ❌ Only one agent works on the repo
- ❌ All work is PR-based
- ❌ Sequential task execution

**Reality:**
- ✅ Multiple agents work in parallel
- ✅ Claude/Windsurf don't always use PRs
- ✅ Tasks have complex dependencies

### Example: RFC-0007 (Arch ECS Integration)

**Ideal work distribution:**

```
┌─────────────────────────────────────────────────────────┐
│ RFC-0007: Arch ECS Integration                          │
├─────────────────────────────────────────────────────────┤
│ Issue #40: Create ECS contracts (Tier 1)                │
│   Agent: Claude Code (architectural design)             │
│   Output: Interface definitions, no PR needed           │
├─────────────────────────────────────────────────────────┤
│ Issue #41: Implement Arch plugin (Tier 3)               │
│   Agent: GitHub Copilot (incremental implementation)    │
│   Depends: #40 (needs contracts)                        │
│   Output: PR with tests                                 │
├─────────────────────────────────────────────────────────┤
│ Issue #42: Create ECS game systems                      │
│   Agent: GitHub Copilot (system scaffolding)            │
│   Depends: #41 (needs plugin)                           │
│   Output: PR with movement/combat systems               │
├─────────────────────────────────────────────────────────┤
│ Issue #43: Performance tuning                           │
│   Agent: Windsurf (interactive profiling)               │
│   Depends: #42 (needs systems to profile)               │
│   Output: Direct commits or PR                          │
└─────────────────────────────────────────────────────────┘
```

**Problems with current workflows:**
1. ❌ `assign-copilot-to-issue.yml` assumes only Copilot works
2. ❌ `auto-advance-micro.yml` assumes sequential Copilot PRs
3. ❌ No way to indicate "Issue #40 is for Claude, not Copilot"

### Solution: Agent-Aware Issue Management

**Option A: Issue Labels**

```yaml
labels:
  - rfc-0007
  - agent:claude-code      # Explicitly assign agent type
  - priority:high
  - status:ready
```

```yaml
# Workflow only assigns to Copilot if label matches
- name: Check agent assignment
  run: |
    LABELS=$(gh issue view $ISSUE_NUM --json labels -q '.labels[].name')
    if ! echo "$LABELS" | grep -q "agent:copilot"; then
      echo "Issue is not assigned to Copilot agent"
      exit 1
    fi
```

**Option B: Issue Templates by Agent**

```
.github/ISSUE_TEMPLATE/
├── copilot-task.yml          # For incremental PRs
├── claude-code-task.yml      # For architectural work
└── windsurf-task.yml         # For interactive editing
```

**Option C: Assignment Prefix**

```
Issue #40: [CLAUDE] Create ECS contracts
Issue #41: [COPILOT] Implement Arch plugin
Issue #42: [COPILOT] Create ECS game systems
Issue #43: [WINDSURF] Performance tuning
```

---

## Revised Recommendation: Foundational Work Required

### Phase 0: Prerequisites (Before Any Workflow Adoption)

**Must complete these foundational tasks:**

#### 1. Issue Dependency System (CRITICAL)

**Goal:** Prevent agents from working on blocked issues

**Tasks:**
- [ ] Create issue template with dependency fields
- [ ] Create `validate-issue-dependencies.yml` workflow
- [ ] Add `blocked-by:#XX` label automation
- [ ] Document dependency conventions in PLAYBOOK.md

**Deliverables:**
- `.github/ISSUE_TEMPLATE/agent-task.yml`
- `.github/workflows/validate-dependencies.yml`
- `docs/playbook/issue-dependency-management.md`

**Effort:** 6-8 hours  
**Priority:** P0 (CRITICAL)  
**Blocks:** All automation workflows

---

#### 2. Multi-Agent Issue Assignment (CRITICAL)

**Goal:** Different agents can work on appropriate tasks

**Tasks:**
- [ ] Define agent labels: `agent:copilot`, `agent:claude-code`, `agent:windsurf`
- [ ] Update issue templates with agent selection
- [ ] Modify `assign-copilot-to-issue.yml` to check labels
- [ ] Document agent selection criteria

**Deliverables:**
- Updated issue templates
- `docs/playbook/agent-selection-guide.md`
- Modified assignment workflows

**Effort:** 4-6 hours  
**Priority:** P0 (CRITICAL)  
**Blocks:** Any auto-assignment workflows

---

#### 3. Event-Driven Workflows Only (CRITICAL)

**Goal:** Zero cron jobs to minimize runner minutes

**Tasks:**
- [ ] Audit all proposed workflows for cron usage
- [ ] Convert polling to event-driven triggers
- [ ] Document event-driven patterns
- [ ] Set up runner minute monitoring

**Deliverables:**
- `docs/playbook/event-driven-workflows.md`
- Runner usage dashboard/badge

**Effort:** 2-3 hours  
**Priority:** P0 (CRITICAL)  
**Blocks:** Any workflow adoption

---

#### 4. Stalled PR Recovery (HIGH)

**Goal:** Clean up stalled PRs without losing issue context

**Tasks:**
- [ ] Create `stalled-pr-recovery.yml` workflow
- [ ] Never recreate issues (keep original)
- [ ] Add detailed failure comments
- [ ] Remove assignee, add investigation labels
- [ ] Document manual recovery process

**Deliverables:**
- `.github/workflows/stalled-pr-recovery.yml`
- `docs/playbook/stalled-pr-recovery.md`

**Effort:** 4-5 hours  
**Priority:** P1 (HIGH)  
**Depends:** Phase 0.1, 0.2

---

### Phase 1: Minimal Safe Automation (After Phase 0)

Only adopt workflows that:
- ✅ Are event-driven (no cron)
- ✅ Support multi-agent coordination
- ✅ Respect issue dependencies
- ✅ Don't recreate/delete issues

**Candidate Workflows:**

1. ✅ `pr-enforce-issue-link.yml` (SAFE - no dependencies)
2. ⚠️ `assign-copilot-to-issue.yml` (NEEDS REFACTORING - must check dependencies and agent labels)
3. ❌ `cleanup-stalled-prs.yml` (NEEDS COMPLETE REWRITE - see Phase 0.4)

---

### Phase 2: Advanced Automation (Future)

**Only after:**
- Phase 0 complete (all prerequisites)
- Phase 1 validated (no issues for 2+ weeks)
- Runner minute usage < 500 min/month
- Dependency system proven effective

---

## Runner Minute Budget Planning

### Conservative Approach

**Goal:** Stay under 500 minutes/month

| Workflow | Trigger | Est. Runs/Month | Min/Run | Total |
|----------|---------|-----------------|---------|-------|
| `pr-enforce-issue-link.yml` | PR opened/sync | ~50 | 0.5 | 25 |
| `validate-dependencies.yml` | Issue assigned | ~30 | 1 | 30 |
| `stalled-pr-recovery.yml` | Daily (manual) | ~4 | 2 | 8 |
| CI/CD (existing) | PR/push | ~100 | 3 | 300 |
| **Total** | | | | **363** |

**Buffer:** 137 minutes/month for unexpected runs

**Cost:** $0 (within free tier 2,000 min/month)

---

## Comparison: Event-Driven vs. Polling

### Anti-Pattern (ithome-ironman-2025)

```yaml
# Runs every 5 minutes looking for work
on:
  schedule:
    - cron: '*/5 * * * *'

jobs:
  check_for_work:
    runs-on: ubuntu-latest
    steps:
      - name: Check if there's anything to do
        run: |
          # 99% of the time, this finds nothing
          # But still costs 1 minute of runner time
```

**Cost:** 288 runs/day × 1 min = 8,640 min/month = $69/month (per workflow!)

---

### Correct Pattern (WingedBean)

```yaml
# Only runs when actual event occurs
on:
  issues:
    types: [assigned]
  pull_request:
    types: [opened, synchronize]

jobs:
  handle_event:
    runs-on: ubuntu-latest
    steps:
      - name: Process the event
        run: |
          # Only runs when needed
```

**Cost:** ~30 runs/month × 1 min = 30 min/month = $0 (free tier)

**Savings:** $69/month per workflow

---

## Decision Matrix: What to Build vs. What to Adopt

| Component | Build Custom | Adopt from Ref | Rationale |
|-----------|--------------|----------------|-----------|
| Issue dependency validation | ✅ Build | ❌ Not in ref | Core requirement, ref doesn't have |
| Multi-agent assignment | ✅ Build | ❌ Not in ref | WingedBean-specific need |
| PR issue link enforcement | ⚠️ Adapt | ✅ Can adopt | Simple, but needs minor tweaks |
| Stalled PR recovery | ✅ Build | ❌ Ref is broken | Ref recreates issues (bad design) |
| Auto-advance workflow | ❌ Don't need | ❌ Don't adopt | Assumes micro-issues |
| RFC automation | ❌ Don't need | ❌ Don't adopt | Notion-specific |
| Auto-merge | ❌ Never | ❌ Never | Security violation |

---

## Open Questions for Team Discussion

### Question 1: Issue Numbering Strategy

**Context:** If we adopt micro-issues (RFC-0007-01, RFC-0007-02), dependencies are clearer but creates more issues.

**Options:**
- **A:** Keep monolithic RFCs (current approach)
  - Pros: Fewer issues, less overhead
  - Cons: Harder to assign incremental work
  
- **B:** Adopt micro-issues
  - Pros: Better granularity, clearer dependencies
  - Cons: More issues to manage, needs tooling

**Decision Needed:** Which approach for WingedBean?

---

### Question 2: GitHub Projects vs. Labels

**Context:** Dependencies can be tracked in GitHub Projects (visual graph) or labels (automation-friendly).

**Options:**
- **A:** GitHub Projects
  - Pros: Visual, native GitHub feature
  - Cons: Manual setup, no automated enforcement
  
- **B:** Label-based system
  - Pros: Automatable, version controlled
  - Cons: No visual graph, brittle parsing

**Decision Needed:** Which system to use?

---

### Question 3: Stalled PR Timeout

**Context:** How long should a PR be inactive before cleanup?

**Options:**
- **A:** 24 hours (aggressive)
- **B:** 48 hours (balanced)
- **C:** 72 hours (conservative)

**Recommendation:** Start with 72h, adjust based on data

---

### Question 4: Agent Assignment Automation

**Context:** Should assignment be fully automated or require human approval?

**Options:**
- **A:** Fully automated (if dependencies satisfied)
- **B:** Human approval required
- **C:** Hybrid (auto for P2/P3, manual for P0/P1)

**Recommendation:** Option C (hybrid approach)

---

## Next Steps

### Immediate Actions (This Week)

1. ✅ **Review this analysis** with team
2. ✅ **Decide on:** Issue numbering strategy (Q1)
3. ✅ **Decide on:** Dependency tracking approach (Q2)
4. ⏸️ **Pause all workflow adoption** until Phase 0 complete

### Phase 0 Implementation (Week 1-2)

1. Implement issue dependency system
2. Implement multi-agent assignment
3. Document event-driven patterns
4. Create stalled PR recovery workflow

**Estimated Total Effort:** 16-22 hours

### Phase 1 Adoption (Week 3-4)

1. Adopt `pr-enforce-issue-link.yml`
2. Test dependency validation
3. Monitor runner minute usage
4. Iterate based on feedback

---

## Appendix A: Runner Minute Cost Calculator

```python
# Calculate monthly cost for a cron job
def monthly_cost(cron_frequency_minutes, job_duration_minutes):
    runs_per_day = (24 * 60) / cron_frequency_minutes
    runs_per_month = runs_per_day * 30
    total_minutes = runs_per_month * job_duration_minutes
    
    free_tier = 2000  # GitHub Free
    overage = max(0, total_minutes - free_tier)
    cost = overage * 0.008  # $0.008 per minute
    
    return {
        'runs_per_month': runs_per_month,
        'total_minutes': total_minutes,
        'overage_minutes': overage,
        'monthly_cost': cost
    }

# Example: Every 5 minutes, 1 minute job
result = monthly_cost(5, 1)
# Result: 8,640 runs, 8,640 minutes, 6,640 overage, $53.12/month
```

---

## Appendix B: Issue Dependency Example

```yaml
# .github/ISSUE_TEMPLATE/agent-task.yml
name: Agent Task
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
      description: Issue numbers that depend on this (comma-separated)
      placeholder: "47, 48"
      
  - type: dropdown
    id: agent
    attributes:
      label: Assigned Agent
      options:
        - GitHub Copilot
        - Claude Code
        - Windsurf
        - Human
      default: 0
```

**Resulting Issue:**

```markdown
## Issue #46: Implement save manager

**Blocked By:** #45 (ECS state serialization design)
**Blocks:** #47 (Load UI), #48 (Save UI)
**Agent:** GitHub Copilot

### Acceptance Criteria
- [ ] SaveManager class implements ISaveService
- [ ] Tests pass (95%+ coverage)
- [ ] Integration with ECS world state
```

**Validation Workflow:**

```yaml
- name: Validate dependencies
  run: |
    BLOCKED_BY=$(gh issue view $ISSUE_NUM --json body -q .body | grep -oP 'Blocked By:.*?#\K\d+')
    
    for dep in $BLOCKED_BY; do
      STATE=$(gh issue view $dep --json state -q .state)
      if [ "$STATE" != "closed" ]; then
        echo "ERROR: Issue #$dep must be closed before #$ISSUE_NUM can be worked"
        gh issue edit $ISSUE_NUM --add-label "status:blocked"
        exit 1
      fi
    done
    
    gh issue edit $ISSUE_NUM --remove-label "status:blocked"
    gh issue edit $ISSUE_NUM --add-label "status:ready"
```

---

## Appendix C: Multi-Agent Coordination Example

```yaml
# .github/workflows/assign-agent-to-issue.yml
name: Assign Agent to Issue

on:
  workflow_dispatch:
    inputs:
      issue_number:
        required: true

jobs:
  assign:
    runs-on: ubuntu-latest
    steps:
      - name: Determine agent from labels
        id: agent
        run: |
          LABELS=$(gh issue view ${{ inputs.issue_number }} --json labels -q '.labels[].name')
          
          if echo "$LABELS" | grep -q "agent:copilot"; then
            echo "agent=copilot" >> $GITHUB_OUTPUT
          elif echo "$LABELS" | grep -q "agent:claude-code"; then
            echo "agent=claude" >> $GITHUB_OUTPUT
          elif echo "$LABELS" | grep -q "agent:windsurf"; then
            echo "agent=windsurf" >> $GITHUB_OUTPUT
          else
            echo "ERROR: No agent label found"
            exit 1
          fi
          
      - name: Validate dependencies
        run: |
          # Check if all blockers are closed
          python scripts/validate_dependencies.py ${{ inputs.issue_number }}
          
      - name: Assign to Copilot
        if: steps.agent.outputs.agent == 'copilot'
        run: |
          gh issue edit ${{ inputs.issue_number }} --add-assignee @me
          
      - name: Notify Claude Code
        if: steps.agent.outputs.agent == 'claude'
        run: |
          gh issue comment ${{ inputs.issue_number }} \
            --body "@apprenticegc This issue is ready for Claude Code"
          
      - name: Notify Windsurf
        if: steps.agent.outputs.agent == 'windsurf'
        run: |
          gh issue comment ${{ inputs.issue_number }} \
            --body "@apprenticegc This issue is ready for Windsurf"
```

---

## References

- [GitHub Actions Pricing](https://docs.github.com/en/billing/managing-billing-for-github-actions/about-billing-for-github-actions)
- [Event-Driven Workflow Best Practices](https://docs.github.com/en/actions/using-workflows/events-that-trigger-workflows)
- [GitHub Issues Best Practices](https://docs.github.com/en/issues/tracking-your-work-with-issues/about-issues)
- Previous analysis: `github-automation-workflows-adoption-plan.md`

---

**Status:** Awaiting team decision on Phase 0 implementation  
**Priority:** P0 (CRITICAL - blocks all automation)  
**Estimated Total Effort:** 16-22 hours for Phase 0  
**Next Review:** After team discussion

---

**Last Updated:** 2025-10-02

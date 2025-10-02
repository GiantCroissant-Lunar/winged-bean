---
id: RFC-0015
title: Agent GitHub Automation with Dependency Tracking
status: Draft
category: tooling
created: 2025-10-02
updated: 2025-10-02
---

# RFC-0015: Agent GitHub Automation with Dependency Tracking

## Summary

Establish a runner-minute-conscious, event-driven GitHub automation system for agent-assisted development with robust issue dependency tracking, local testing support, and agent rule enforcement.

## Motivation

The ref-project `ithome-ironman-2025` contains 40+ GitHub workflows for agent automation, but has critical limitations:

1. **Runner minute consumption** - Multiple workflows trigger frequently, consuming limited GitHub Actions minutes
2. **Issue dependency gaps** - Dependencies exist (e.g., #85 depends on #48, #62) but aren't programmatically enforced
3. **Workflow reliability** - Event-driven workflows often fail without local testing capability
4. **Stalled PR handling** - Closing PRs doesn't address linked issues, creating orphaned work
5. **Agent coordination** - Multiple agents (Copilot, Claude, Windsurf) create issues without consistent metadata

## Goals

1. **Minimize runner minutes** through efficient event-driven architecture
2. **Enforce issue dependencies** via pre-commit hooks and metadata validation
3. **Enable local testing** using `nektos/act` before GitHub deployment
4. **Automate agent recovery** with bounded retry logic (3 attempts max)
5. **Unify agent behavior** via `.agent` rules for issue creation

## Non-Goals

- Replacing existing `.agent` rule system
- Supporting non-agent human workflows (out of scope)
- Implementing full project management features (labels, milestones, etc.)

## Design

### Architecture: Event-Driven Hub-and-Spoke

```
┌─────────────────────────────────────────────────────────┐
│                   Event Sources                          │
│  - issue_opened                                          │
│  - pull_request (opened/closed/synchronize)             │
│  - workflow_run (completed/failed)                      │
└──────────────────┬──────────────────────────────────────┘
                   │
                   ▼
          ┌────────────────┐
          │  Event Router  │  (single workflow)
          │  validates &   │
          │  dispatches    │
          └────────┬───────┘
                   │
         ┌─────────┼─────────┐
         ▼         ▼         ▼
    ┌────────┐ ┌─────────┐ ┌──────────┐
    │ Assign │ │ Cleanup │ │ Watchdog │
    │ Agent  │ │ Stalled │ │ Retry    │
    └────────┘ └─────────┘ └──────────┘
```

**Key principle:** One event router, multiple specialized handlers (no redundant polling)

### Phase 1: Issue Dependency Infrastructure

#### 1.1 Issue Metadata Schema

Every agent-created issue MUST include frontmatter:

```yaml
---
rfc: RFC-0007
phase: 1
wave: 1.1
depends_on: [48, 62]
blocks: [86, 87]
estimate_minutes: 30
priority: critical  # critical|high|medium|low
agent_assignable: true
retry_count: 0
max_retries: 3
---
```

#### 1.2 Pre-Commit Hook (`development/python/src/hooks/pre_commit_issue_validator.py`)

**Triggers:** On any commit modifying `.github/ISSUE_TEMPLATE/*.yml` or docs containing issue references

**Actions:**
1. Parse issue metadata from template/docs
2. Validate schema (required fields, valid dependencies)
3. Check circular dependencies
4. Verify referenced issues exist (if online)
5. **HARD BLOCK** on validation failure

**Implementation:**
```python
# development/python/src/hooks/pre_commit_issue_validator.py
import re
import sys
from pathlib import Path
from typing import List, Set, Dict
import yaml

def validate_issue_metadata(content: str) -> Dict:
    """Extract and validate issue frontmatter."""
    # Extract YAML frontmatter
    # Validate schema
    # Check dependencies
    pass

def detect_circular_deps(deps_graph: Dict[int, List[int]]) -> List[List[int]]:
    """Detect circular dependencies using DFS."""
    pass

if __name__ == "__main__":
    # Hook entry point
    sys.exit(main())
```

#### 1.3 Agent Rules Update (`.agent/base/20-rules.md`)

Add new rule **R-ISS-010**:

> When creating issues programmatically (via API or templates), agents MUST include:
> - `rfc`: RFC identifier (e.g., RFC-0007)
> - `depends_on`: List of blocking issue numbers (empty array if none)
> - `priority`: One of critical|high|medium|low
> - `agent_assignable`: Boolean (default true)
> - `retry_count`: Integer (default 0)
> - `max_retries`: Integer (default 3, per user preference)

### Phase 2: Event-Driven Workflow System

#### 2.1 Event Router (`.github/workflows/agent-event-router.yml`)

**Trigger:** `issues`, `pull_request`, `workflow_run` events

**Responsibilities:**
1. Validate event payload
2. Extract issue metadata
3. Dispatch to specialized handlers
4. Log event for audit trail

**Sample workflow:**
```yaml
name: agent-event-router

on:
  issues:
    types: [opened, closed, reopened]
  pull_request:
    types: [opened, closed, synchronize, ready_for_review]
  workflow_run:
    workflows: ["ci"]
    types: [completed]

jobs:
  route:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
      
      - name: Route event
        run: |
          python development/python/src/workflows/event_router.py \
            --event-type "${{ github.event_name }}" \
            --payload "${{ github.event.path }}"
```

#### 2.2 Assign Agent Workflow (`.github/workflows/agent-assign.yml`)

**Trigger:** Repository dispatch from event router

**Logic:**
1. Check `agent_assignable: true`
2. Verify dependencies resolved (all `depends_on` issues closed)
3. Check `retry_count < max_retries`
4. Assign to Copilot via comment trigger
5. Update issue metadata (`retry_count++`)

#### 2.3 Cleanup Stalled PR Workflow (`.github/workflows/agent-cleanup-stalled-pr.yml`)

**Trigger:** Scheduled (6h) OR repository dispatch

**Logic:**
1. Find PRs inactive > 24h
2. Extract linked issue from PR body
3. **Close PR AND reopen linked issue** with:
   - Reset `retry_count` (or increment depending on failure cause)
   - Add `stalled-recovery` label
   - Comment with failure reason
4. Delete stale branch

#### 2.4 Watchdog Retry Workflow (`.github/workflows/agent-watchdog.yml`)

**Trigger:** `workflow_run` with `conclusion: failure`

**Logic:**
1. Extract PR/issue from failed run
2. Check `retry_count < max_retries`
3. If retries available:
   - Close failed PR
   - Reopen issue with `retry_count++`
   - Add comment: "Auto-retry attempt X/3"
4. If max retries exceeded:
   - Label issue `agent-failed-max-retries`
   - Notify via discussion (NOT issue comment to reduce noise)
   - **Do NOT close issue** (requires human investigation)

### Phase 3: Local Testing with `act`

#### 3.1 Setup Script (`development/python/src/testing/setup_act.py`)

Automates `act` installation and configuration:

```python
# development/python/src/testing/setup_act.py
def setup_act():
    """Install act and configure secrets/vars."""
    # Check if act is installed
    # If not, provide installation instructions
    # Create .actrc with platform mapping
    # Setup .secrets for local testing
```

#### 3.2 Workflow Testing Harness

**Location:** `development/python/src/testing/test_workflows.py`

**Capabilities:**
- Simulate issue events with metadata
- Mock GitHub API responses
- Validate workflow outputs
- Generate test reports

**Usage:**
```bash
# Test single workflow locally
python development/python/src/testing/test_workflows.py \
  --workflow .github/workflows/agent-assign.yml \
  --event-type issues \
  --event-payload tests/fixtures/issue_opened.json

# Run full workflow test suite
python development/python/src/testing/test_workflows.py --all
```

### Phase 4: Documentation & Rules

#### 4.1 Agent Rules Addition

Update `.agent/base/20-rules.md`:

**R-ISS-010: Issue Metadata** (described above)

**R-ISS-020: Script Complexity Threshold**
> When workflow inline scripts exceed 50 lines OR use complex logic (loops, conditionals, regex), extract to Python module in `development/python/src/workflows/`. NO embedded Bash scripts with Perl/awk.

**R-ISS-030: Workflow Testing**
> Before pushing new/modified workflows to GitHub, MUST test locally using `act` or workflow testing harness. Run `make test-workflows` or equivalent.

#### 4.2 Developer Guide

Create `docs/guides/agent-automation.md` documenting:
- Issue metadata schema
- How to create agent-assignable issues
- Local workflow testing procedure
- Debugging failed workflows
- Runner minute optimization tips

## Implementation Plan

### Phase 1: Dependency Infrastructure (Week 1)
- [ ] Create `development/python/src/hooks/pre_commit_issue_validator.py`
- [ ] Write validation logic with tests
- [ ] Install pre-commit hook
- [ ] Update `.agent/base/20-rules.md` with R-ISS-010, R-ISS-020, R-ISS-030
- [ ] Test hook with sample commits

### Phase 2: Core Workflows (Week 2)
- [ ] Implement event router workflow
- [ ] Port assign-copilot workflow with dependency checks
- [ ] Implement cleanup-stalled-pr with issue reopening
- [ ] Implement watchdog with bounded retries
- [ ] Add workflow testing to CI

### Phase 3: Local Testing Infrastructure (Week 3)
- [ ] Setup `act` installation script
- [ ] Create workflow testing harness
- [ ] Write test fixtures for common events
- [ ] Document testing procedure
- [ ] Validate all workflows locally

### Phase 4: Documentation & Migration (Week 4)
- [ ] Write `docs/guides/agent-automation.md`
- [ ] Update issue templates with metadata schema
- [ ] Migrate existing issues (add metadata)
- [ ] Run parallel testing (ref-project vs new system)
- [ ] Cutover and archive ref-project workflows

## Security Considerations

1. **Token scoping** - Use minimal GitHub token permissions
2. **Secret handling** - Never log tokens, use `<REDACTED>` (per R-SEC-010)
3. **Workflow permissions** - Explicit `permissions:` block per workflow
4. **Act secrets** - `.secrets` file in `.gitignore`, documented setup

## Alternatives Considered

### Polling-based system
**Rejected** - High runner minute consumption, poor scalability

### Human-driven issue tracking
**Rejected** - Doesn't scale for agent-heavy development

### Third-party project management tools (Jira, Linear)
**Rejected** - Adds external dependencies, GitHub Issues sufficient

## Success Metrics

1. **Runner minutes** - Reduce by 60% compared to ref-project baseline
2. **Issue dependency violations** - Zero (enforced by pre-commit hook)
3. **Agent retry success rate** - 70% of issues resolved within 3 retries
4. **Workflow test coverage** - 100% of workflows tested with `act` before deployment
5. **Stalled PR recovery time** - < 24h from stall detection to issue reassignment

## Open Questions

1. How to handle cross-repository dependencies? (Future RFC)
2. Should we implement automatic dependency resolution (topological sort)? (Phase 5?)
3. Notification strategy for max-retry failures? (Discussion post vs email?)

## References

- [nektos/act](https://github.com/nektos/act) - Local GitHub Actions testing
- [GitHub Actions Events](https://docs.github.com/en/actions/using-workflows/events-that-trigger-workflows)
- ref-project: `ref-projects/ithome-ironman-2025/.github/workflows/`
- Related: RFC-0012 (Agent-driven GitHub automation)
- Related: RFC-0013 (Documentation automation tooling)

## Changelog

- 2025-10-02: Initial draft (per user requirements)

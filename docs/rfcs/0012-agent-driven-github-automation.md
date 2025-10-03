---
id: RFC-0012
title: Agent-Driven GitHub Automation
status: Draft
category: infra, tooling, automation
created: 2025-10-02
updated: 2025-10-02
---

# RFC-0012: Agent-Driven GitHub Automation

## Summary

Implement event-driven GitHub Actions workflows and pre-commit hooks to automate issue dependency validation, PR hygiene enforcement, and agent failure recovery. This RFC establishes a foundation for multi-agent collaboration (GitHub Copilot, Claude Code, Windsurf) with zero recurring costs by avoiding polling-based workflows.

**Key Principles:**
- Event-driven only (no cron polling)
- Python for complex logic (R-PRC-050)
- Local testing with `act` before deployment
- Agent-driven recovery (agents fix their own failures)
- Hard block on dependency violations

## Motivation

### Current Problems

1. **No Dependency Management**
   - Agents can work on blocked issues
   - Wasted work when dependencies aren't met
   - No validation before commits/PRs

2. **High Runner Minute Costs**
   - Reference project (ithome-ironman-2025) uses ~43,230 min/month ($329.84)
   - Aggressive cron polling (every 2-10 minutes)
   - Financially unsustainable

3. **Manual Failure Recovery**
   - When agent PRs fail CI, humans must investigate
   - Agent wrote the code, but human must fix
   - Wastes 1.5-2.5 hours per failure

4. **No Multi-Agent Coordination**
   - Three agents (Copilot, Claude, Windsurf) work independently
   - No way to indicate "this issue is for Claude, not Copilot"
   - Risk of assignment conflicts

5. **Fragile Shell Scripts**
   - Complex bash in workflow YAML
   - Hard to test, maintain, debug
   - No type safety

### Why Now?

- Active development with multiple agents contributing
- Need to scale automation without scaling costs
- Foundation required before adopting more complex workflows
- Reference project provides lessons learned (what NOT to do)

## Proposal

### Overview

Implement a **three-phase automation system**:

**Phase 0: Foundation (This RFC)**
- Issue dependency validation (pre-commit + workflow)
- PR issue link enforcement
- Python-based scripts in proper project structure
- Local testing with `act`
- Agent rules codification

**Phase 1: Core Workflows (Future)**
- Agent auto-retry on CI failure (3 attempts)
- Issue auto-labeling by agent type
- Stalled PR recovery

**Phase 2: Advanced (Future)**
- Multi-agent assignment coordination
- Dependency graph visualization
- Runner minute monitoring

### Detailed Design

#### 1. Issue Dependency System

**Issue Template Fields:**
```yaml
# .github/ISSUE_TEMPLATE/agent-task.yml
- type: input
  id: blocked_by
  label: Blocked By
  description: Issue numbers that must be closed first
  placeholder: "40, 45"

- type: input
  id: blocks
  label: Blocks
  description: Issue numbers that depend on this
  placeholder: "47, 48"

- type: dropdown
  id: agent
  label: Assigned Agent
  options: [GitHub Copilot, Claude Code, Windsurf, Human, Unassigned]
```

**Pre-Commit Hook:**
```python
# development/python/src/scripts/validate_issue_dependencies.py
def main():
    issue_num = extract_issue_number(commit_msg)
    blockers = extract_blockers(issue_body)
    
    for blocker in blockers:
        if check_issue_state(blocker) != "closed":
            print(f"❌ COMMIT BLOCKED: Issue #{blocker} must be closed first")
            return 1  # Hard block
    
    return 0
```

**Workflow Validation:**
```yaml
# .github/workflows/validate-dependencies.yml
on:
  issues:
    types: [assigned, labeled, edited]

jobs:
  validate:
    runs-on: ubuntu-latest
    steps:
      - name: Validate dependencies
        run: python -m scripts.workflows.validate_issue_dependencies
        working-directory: ./development/python/src
```

#### 2. PR Issue Link Enforcement

**Workflow:**
```yaml
# .github/workflows/pr-enforce-issue-link.yml
on:
  pull_request:
    types: [opened, edited, synchronize]

jobs:
  validate:
    steps:
      - name: Check for issue link
        run: python -m scripts.workflows.validate_pr_issue_link
```

**Python Script:**
```python
def main():
    issue_num = extract_issue_number(pr_body)
    if not issue_num:
        print("❌ ERROR: PR must contain 'Closes #<issue>'")
        return 1
    return 0
```

#### 3. Agent Auto-Retry System

**Workflow:**
```yaml
# .github/workflows/agent-auto-retry.yml
on:
  workflow_run:
    workflows: ["MegaLinter", "Qodana"]
    types: [completed]

jobs:
  handle-failure:
    if: ${{ github.event.workflow_run.conclusion == 'failure' }}
    steps:
      - name: Handle agent failure
        run: python -m scripts.workflows.handle_agent_failure
```

**Retry Logic:**
```python
retry_count = get_retry_count(issue_number)

if retry_count >= 3:
    escalate_to_human(issue_number)
else:
    post_failure_logs_to_issue(issue_number)
    close_pr_and_delete_branch()
    add_label(f"ci-failed-retry-{retry_count + 1}")
```

#### 4. Python Script Structure

**Location:** `development/python/src/scripts/`

```
scripts/
├── __init__.py
├── validate_issue_dependencies.py     # Pre-commit hook
└── workflows/
    ├── __init__.py
    ├── validate_pr_issue_link.py      # PR validation
    ├── validate_issue_dependencies.py # Issue validation
    └── handle_agent_failure.py        # Failure recovery
```

**Why This Structure:**
- ✅ Proper Python package (not ad-hoc scripts)
- ✅ Can use `python -m scripts.workflows.module_name`
- ✅ Easy to test (`pytest development/python/`)
- ✅ Console entry points via `pyproject.toml`
- ✅ Type hints, linting, static analysis

#### 5. Local Testing with `act`

**Setup:**
```bash
# Install act (already installed)
brew install act

# Create test events
.github/workflows/test-events/
├── pr-opened-valid.json
├── pr-opened-invalid.json
├── issue-assigned.json
└── issue-blocked.json
```

**Usage:**
```bash
# Test workflow locally (FREE, instant feedback)
act pull_request \
  --eventpath .github/workflows/test-events/pr-opened-valid.json \
  -W .github/workflows/pr-enforce-issue-link.yml

# If passes locally → deploy to GitHub
# If fails → fix and retry (no cost!)
```

**Benefits:**
- Zero GitHub runner minutes during development
- Instant feedback (no waiting for cloud runners)
- Can test offline
- Iterate quickly

#### 6. New Agent Rules

**R-PRC-050: Use Python for complex logic**
```markdown
R-PRC-050: Use Python instead of embedded shell scripts for complex logic.
  - Shell acceptable for < 10 lines
  - Python required for: JSON parsing, complex strings, error handling, API calls
  - Place in development/python/src/scripts/
```

**R-ISS-010 through R-ISS-050: Issue Management**
```markdown
R-ISS-010: Specify dependencies in issue body ("Blocked By: #XX, #YY")
R-ISS-020: Specify intended agent (agent:copilot, agent:claude-code, etc.)
R-ISS-030: Verify blockers closed before starting work (pre-commit enforces)
R-ISS-040: Agent must fix own failures (3 retry limit)
R-ISS-050: Follow issue naming conventions (RFC-XXXX-YY: Description)
```

### Migration Plan

**Phase 0: Foundation (This RFC) - Week 1**

1. Create Python scripts (✅ Done)
2. Update `pyproject.toml` (✅ Done)
3. Create test event fixtures (✅ Done)
4. Update agent rules (✅ Done)
5. Create PR validation workflow (✅ Done)
6. Install and test pre-commit hook
7. Create issue dependency validation workflow
8. Create agent auto-retry workflow
9. Test all workflows with `act`
10. Deploy to GitHub

**Phase 1: Adoption - Week 2**

1. Create issue template
2. Test with real issues/PRs
3. Monitor workflow success rates
4. Adjust retry limits if needed
5. Document in PLAYBOOK.md

**Phase 2: Advanced - Future**

1. Add dependency graph visualization
2. Add runner minute monitoring
3. Add multi-agent coordination

**No Breaking Changes:**
- Workflows are additive (don't modify existing CI)
- Pre-commit hook is optional (warnings if `gh` not available)
- Can roll back by removing workflows

## Benefits

### Cost Savings

| Item | Savings | Calculation |
|------|---------|-------------|
| **vs. Reference Project** | $329.84/month | Zero cron polling |
| **Development Testing** | $1-2 per workflow | Use `act` instead of GitHub |
| **Agent Failure Recovery** | $75-125 per failure | Agent fixes own code |
| **Prevented Wasted Work** | Unknown | Hard block on dependencies |

**Total Estimated Savings:** $400-500/month

### Developer Productivity

- **Faster feedback:** `act` testing in seconds vs. minutes
- **Less context switching:** Agents fix their own failures
- **Prevented mistakes:** Pre-commit catches blocked issues
- **Better coordination:** Agent labels prevent conflicts

### Code Quality

- **Maintainable:** Python with type hints
- **Testable:** Standard Python testing
- **Reliable:** No fragile shell parsing
- **Observable:** Clear error messages

## Risks and Mitigations

### Risk 1: Pre-Commit Hook Too Strict

**Risk:** Hard block prevents commits even when legitimate

**Mitigation:**
- Hook is graceful if `gh` not available (warning only)
- Can use `git commit --no-verify` to skip hook
- Clear error messages explain how to unblock

**Likelihood:** Low  
**Impact:** Medium

### Risk 2: Agent Gets Stuck in Retry Loop

**Risk:** Agent fails 3 times on same issue

**Mitigation:**
- After 3 failures, escalate to human
- Human reviews failure patterns
- Can split issue or provide clearer guidance

**Likelihood:** Medium  
**Impact:** Low (human review is the safety net)

### Risk 3: Workflow Complexity

**Risk:** Python scripts become unmaintainable

**Mitigation:**
- Keep scripts focused (single responsibility)
- Type hints + linting
- Unit tests
- Good documentation

**Likelihood:** Low  
**Impact:** Medium

### Risk 4: `act` Diverges from GitHub Actions

**Risk:** Workflow passes locally but fails on GitHub

**Mitigation:**
- Test in GitHub after local testing
- Use official GitHub action images
- Document known differences

**Likelihood:** Low  
**Impact:** Low

## Definition of Done

**Phase 0 (This RFC):**
- [x] Python scripts created in `development/python/src/scripts/`
- [x] `pyproject.toml` updated with console scripts
- [x] Agent rules updated (R-ISS-010 through R-ISS-050, R-PRC-050)
- [x] Test event fixtures created
- [x] `act` configured
- [x] PR issue link enforcement workflow created
- [ ] Pre-commit hook installed and tested
- [ ] Issue dependency validation workflow created
- [ ] Agent auto-retry workflow created
- [ ] All workflows tested with `act`
- [ ] All workflows deployed to GitHub
- [ ] Documentation updated (PLAYBOOK.md)

**Success Criteria:**
- ✅ All Python scripts compile without errors
- ✅ Workflows pass `act` local tests
- ✅ Pre-commit hook blocks commits on blocked issues
- ✅ PR validation rejects PRs without issue links
- ✅ Agent auto-retry works for at least 1 failed PR
- ✅ Zero cron-based workflows
- ✅ Runner minute usage < 500 min/month

## Implementation Phases

### Phase 0: Foundation (1-2 weeks) ← **THIS RFC**

**Effort:** 10-14 hours  
**Cost:** $0 (using `act` for testing)

**Deliverables:**
1. ✅ Python scripts (4 files)
2. ✅ Agent rules (6 new rules)
3. ✅ Test fixtures (4 files)
4. ✅ PR validation workflow
5. ⏳ Issue dependency validation workflow
6. ⏳ Agent auto-retry workflow
7. ⏳ Pre-commit hook installed
8. ⏳ Documentation

### Phase 1: Adoption (2-4 weeks)

**Effort:** 4-6 hours  
**Cost:** ~50-100 runner minutes ($0-0.80)

**Deliverables:**
1. Issue template
2. Real-world testing
3. Monitoring dashboard
4. Iteration based on feedback

### Phase 2: Advanced (Future)

**Effort:** TBD  
**Cost:** TBD

**Deliverables:**
1. Dependency graph visualization
2. Runner minute monitoring
3. Multi-agent coordination enhancements

## Dependencies

**Required:**
- ✅ Python 3.9+ (available)
- ✅ `act` installed (available)
- ✅ GitHub CLI (`gh`) for pre-commit hook (available)
- ✅ `pre-commit` framework (available)

**Optional:**
- GitHub Projects (for dependency visualization) - Future

**Blocks:**
- None (this is foundational work)

**Blocked By:**
- None

## Alternatives Considered

### Alternative 1: Adopt Reference Project Workflows As-Is

**Pros:**
- Pre-built workflows
- Proven in one project

**Cons:**
- ❌ $329.84/month cost (cron polling)
- ❌ Recreates issues (loses context)
- ❌ Notion-specific (not applicable)
- ❌ No multi-agent support
- ❌ Fragile bash scripts

**Decision:** Rejected - too costly, wrong assumptions

### Alternative 2: Manual Process (No Automation)

**Pros:**
- Zero runner minutes
- Simple to understand

**Cons:**
- ❌ Humans forget to check dependencies
- ❌ Wasted work on blocked issues
- ❌ Manual PR review overhead
- ❌ Manual failure investigation

**Decision:** Rejected - doesn't scale

### Alternative 3: GitHub Projects + Manual Labels

**Pros:**
- Visual dependency graph
- Native GitHub feature

**Cons:**
- ❌ No automated enforcement
- ❌ Manual label management
- ❌ Can't prevent commits on blocked issues

**Decision:** Rejected - no automation, error-prone

### Alternative 4: External Service (Linear, Jira)

**Pros:**
- Rich features
- Professional tools

**Cons:**
- ❌ Monthly cost ($10-20/user)
- ❌ Requires integration setup
- ❌ Data outside GitHub
- ❌ Overkill for this project

**Decision:** Rejected - too heavy, unnecessary cost

## Open Questions

### Q1: Should pre-commit hook be mandatory?

**Options:**
- A) Warning only (current)
- B) Hard block always
- C) Configurable per-developer

**Decision:** **B) Hard block** - Prevents wasted work  
**Rationale:** Can use `--no-verify` if needed, but default should be safe

### Q2: Should we track dependencies in separate file?

**Options:**
- A) Issue body text (current)
- B) JSON file (`.github/issue-dependencies.json`)
- C) GitHub Projects

**Decision:** **A) Issue body** - Simple, no extra files  
**Rationale:** Easy to parse, visible in UI, no sync issues

### Q3: Should agents be able to override retry limit?

**Options:**
- A) Fixed 3 retries (current)
- B) Configurable via issue label
- C) Unlimited with manual stop

**Decision:** **A) Fixed 3 retries** - Simple, clear escalation  
**Rationale:** 3 is balanced, prevents infinite loops

## References

- [ithome-ironman-2025 workflows](../../ref-projects/ithome-ironman-2025/.github/workflows/) - Reference implementation (what NOT to do)
- [act documentation](https://github.com/nektos/act) - Local workflow testing
- [GitHub Actions pricing](https://docs.github.com/en/billing/managing-billing-for-github-actions/about-billing-for-github-actions)
- [Pre-commit framework](https://pre-commit.com/)
- Related Design Docs:
  - `docs/design/github-automation-critical-issues-analysis.md`
  - `docs/design/github-automation-revised-approach.md`
  - `docs/design/github-automation-workflows-adoption-plan.md`

## Changelog

| Date | Change | Author |
|------|--------|--------|
| 2025-10-02 | Initial draft | GitHub Copilot |
| 2025-10-02 | Added R-PRC-050 decision | GitHub Copilot |
| 2025-10-02 | Confirmed decisions (3 retries, hard block, Python) | GitHub Copilot |

---

**Status:** Draft → Ready for Review  
**Priority:** P0 (Foundation for all automation)  
**Target Date:** 2025-10-15  
**Estimated Effort:** 10-14 hours (Phase 0)  
**Cost Impact:** -$329.84/month (savings vs. reference approach)  
**Author:** GitHub Copilot  
**Reviewers:** TBD

---

**Related RFCs:**
- None (foundational work)

**Related ADRs:**
- None yet (will create ADR after approval)

**Implementation Plan:**
- See Phase 0 checklist in Definition of Done
- See `docs/design/github-automation-revised-approach.md` for detailed plan

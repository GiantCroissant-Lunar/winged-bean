# GitHub Automation Workflows - Adoption Plan

**Date:** 2025-10-02  
**Status:** Proposed  
**Author:** GitHub Copilot  
**Project:** WingedBean  

---

## ⚠️ CRITICAL UPDATE: ADOPTION PLAN SUPERSEDED

**Date:** 2025-10-02  
**Status:** ❌ **DO NOT IMPLEMENT** - Critical blockers identified

After detailed discussion with the team, **three critical blockers** were identified that prevent any workflow adoption:

1. 🔴 **Runner minute explosion** - Ref project uses ~43,230 min/month ($330/month cost) due to aggressive cron polling
2. 🔴 **No cross-agent dependency management** - Cannot prevent agents from working on blocked issues
3. 🔴 **Stalled PR cleanup destroys context** - Recreates issues, breaks dependency tracking

**New Analysis:** See `github-automation-critical-issues-analysis.md` for complete details.

---

## Executive Summary (OUTDATED - See New Analysis)

⚠️ **WARNING:** This section represents the initial analysis before critical issues were discovered. Do not follow these recommendations without first addressing the blockers in `github-automation-critical-issues-analysis.md`.

The `ithome-ironman-2025` reference project contains **46 GitHub Actions workflows** focused on automating GitHub Copilot coding agent interactions. After initial analysis, **8 workflows were considered for adoption**, but further investigation revealed fundamental issues.

### Revised Recommendation (2025-10-02)

**Phase 0 Required (16-22 hours):**
1. 🔴 Build issue dependency system (CRITICAL)
2. 🔴 Build multi-agent coordination (CRITICAL)
3. 🔴 Enforce event-driven workflows only (CRITICAL)
4. 🔴 Build stalled PR recovery (HIGH)

**Phase 1 (After Phase 0):**
1. ✅ `ensure-closes-link.yml` - Enforce PR→Issue linking (SAFE)
2. ⚠️ `assign-copilot-to-issue.yml` - Needs major refactoring
3. ❌ `cleanup-stalled-prs.yml` - Needs complete rewrite

**Do NOT Adopt (High Risk/Complexity):**
- ❌ RFC automation workflows (Notion-specific, overly complex)
- ❌ Auto-merge/auto-approve workflows (dangerous without proper controls)
- ❌ Project board sync workflows (unnecessary complexity)
- ❌ Any cron-based workflows (runner minute explosion)

---

## Problem Analysis

### Current State: WingedBean

**Existing Workflows:**
- `mega-linter.yml` - Code quality checks
- `qodana_code_quality.yml` - JetBrains code analysis

**Missing Automation:**
- No PR→Issue linking enforcement
- No stalled PR cleanup
- No automated Copilot assignment
- No failed workflow recovery

**Current Process:**
- RFCs are manually created in `docs/rfcs/`
- No micro-issue workflow (monolithic RFCs)
- Manual issue assignment
- Manual PR review and merge

### Reference Project State: ithome-ironman-2025

**Workflow Categories:**

| Category | Count | Purpose | Complexity | Quality |
|----------|-------|---------|------------|---------|
| Copilot Assignment | 5 | Auto-assign issues to Copilot | Medium | Poor* |
| RFC Automation | 8 | Sync Notion→GitHub issues | High | Poor* |
| Auto-Merge | 6 | Auto-approve and merge PRs | High | Dangerous* |
| PR Monitoring | 4 | Track PR status, ensure links | Low | Good |
| Cleanup | 3 | Remove stalled PRs/branches | Medium | Good |
| Testing | 5 | Diagnostic/test workflows | Low | Good |
| Project Boards | 6 | Sync GitHub Projects | Medium | Poor* |
| Misc | 9 | Badges, events, cron jobs | Low | Mixed |

**Quality Issues:**
- ⚠️ Hardcoded repository names
- ⚠️ Missing error handling
- ⚠️ Inconsistent token usage (`GITHUB_TOKEN` vs `AUTO_APPROVE_TOKEN`)
- ⚠️ Complex Python scripts with no tests
- ⚠️ Notion API dependencies (external coupling)
- ⚠️ Overly aggressive auto-merge (bypasses review)

---

## Workflows: Detailed Analysis

### ✅ Category 1: Safe to Adopt (with minor changes)

#### 1. `ensure-closes-link.yml`

**Purpose:** Enforce that all PRs contain `Closes #<issue>` links

**Quality:** ⭐⭐⭐⭐ Good  
**Complexity:** Low  
**Risk:** Low  

**Why Adopt:**
- Aligns with R-GIT-010 (commit body standards)
- Prevents orphaned PRs
- Simple script, easy to maintain

**Required Changes:**
```yaml
# Change from:
on: pull_request_target  # Dangerous for untrusted contributors
# To:
on: pull_request         # Safer, runs on PR branch
```

**Dependencies:**
- Python script: `ensure_closes_link.py` (90 lines, no external deps)

**Adoption Effort:** 1-2 hours

---

#### 2. `assign-copilot-to-issue.yml`

**Purpose:** Manually assign GitHub Copilot to an issue via workflow_dispatch

**Quality:** ⭐⭐⭐ Fair  
**Complexity:** Medium  
**Risk:** Low (manual trigger only)  

**Why Adopt:**
- Useful for kickstarting Copilot work
- Manual control (workflow_dispatch)
- Can be simplified

**Required Changes:**
1. Remove Notion/RFC-specific logic
2. Remove event bus emission
3. Simplify token selection (use GITHUB_TOKEN only)
4. Remove mutex/locking logic (overly complex)

**Simplified Version:**
```yaml
name: Assign Copilot to Issue
on:
  workflow_dispatch:
    inputs:
      issue_number:
        required: true
jobs:
  assign:
    runs-on: ubuntu-latest
    permissions:
      issues: write
    steps:
      - name: Assign issue to Copilot
        env:
          GH_TOKEN: ${{ secrets.GITHUB_TOKEN }}
        run: |
          gh issue edit ${{ inputs.issue_number }} \
            --add-assignee @me \
            --repo ${{ github.repository }}
```

**Adoption Effort:** 2-3 hours (with simplification)

---

#### 3. `cleanup-stalled-prs.yml`

**Purpose:** Auto-close PRs that are inactive for 24+ hours

**Quality:** ⭐⭐⭐ Fair  
**Complexity:** Medium  
**Risk:** Medium (deletes branches)  

**Why Adopt:**
- Prevents branch/PR clutter
- Useful for Copilot-generated PRs that fail
- Follows stale-PR best practices

**Required Changes:**
1. Remove `cleanup_recreate_issue.py` dependency (WingedBean doesn't use micro-issues)
2. Simplify to just close + delete branch
3. Add safety check: only close PRs from `copilot/*` branches
4. Make timeout configurable (default 48h, not 24h)

**Simplified Version:**
```yaml
- name: Close stalled Copilot PRs
  run: |
    CUTOFF=$(date -u -d "48 hours ago" +%Y-%m-%dT%H:%M:%SZ)
    STALLED=$(gh pr list --author app/copilot-swe-agent \
      --json number,updatedAt,headRefName \
      --jq "map(select(.updatedAt < \"$CUTOFF\" and (.headRefName | startswith(\"copilot/\")))) | .[].number")
    for pr in $STALLED; do
      gh pr close $pr --comment "Auto-closed: inactive for 48h"
      BRANCH=$(gh pr view $pr --json headRefName -q .headRefName)
      git push origin --delete "$BRANCH" || true
    done
```

**Adoption Effort:** 2-3 hours

---

### ⚠️ Category 2: Adopt with Caution (needs refactoring)

#### 4. `auto-advance-micro.yml`

**Purpose:** When a PR merges, auto-assign the next issue in the RFC sequence

**Quality:** ⭐⭐ Poor  
**Complexity:** High  
**Risk:** High (auto-assignment without validation)  

**Why Consider:**
- Useful for sequential work (e.g., RFC-0006-01 → RFC-0006-02)
- Reduces manual overhead

**Why Not Adopt As-Is:**
- Tightly coupled to micro-issue workflow (not used in WingedBean)
- Hardcoded PAT token requirements
- No validation of next issue readiness
- Complex Python script with RFC-specific logic

**Recommendation:** **Defer** until WingedBean adopts micro-issue workflow

**Alternative:** Create a simpler workflow that:
1. Detects `Closes #<issue>` in merged PR
2. Checks if issue has `next-issue: #<num>` label
3. Assigns Copilot to next issue if label exists

**Adoption Effort:** 1 day (if needed in future)

---

#### 5. `agent-watchdog.yml`

**Purpose:** When CI fails, close PR, delete branch, recreate issue, and reassign Copilot

**Quality:** ⭐⭐ Poor  
**Complexity:** Very High  
**Risk:** Very High (destructive)  

**Why Consider:**
- Useful for automatic recovery from Copilot failures
- Prevents "stuck" PRs

**Why Not Adopt As-Is:**
- **Dangerous:** Deletes branches without confirmation
- Recreates issues (assumes micro-issue workflow)
- Complex derivation logic (regex scraping)
- No rollback mechanism

**Recommendation:** **Do NOT adopt** without significant safety controls

**Safer Alternative:**
1. Just comment on failed PR (don't delete)
2. Add `needs-attention` label
3. Notify in Slack/email instead of auto-recreating

**Adoption Effort:** N/A (not recommended)

---

### ❌ Category 3: Do NOT Adopt

#### 6. `rfc-automation.yml` (and 7 related workflows)

**Purpose:** Sync Notion Implementation RFCs to GitHub issues

**Quality:** ⭐ Very Poor  
**Complexity:** Extreme  
**Risk:** N/A (not applicable)  

**Why NOT Adopt:**
- ❌ Requires Notion API token
- ❌ Tightly coupled to `ithome-ironman-2025` Notion workspace
- ❌ 500+ lines of unmaintained Python code
- ❌ WingedBean uses Markdown RFCs, not Notion
- ❌ Overly complex duplicate detection
- ❌ Uses SQLite tracking database

**Alternative:** Keep RFCs in `docs/rfcs/*.md` (current approach is better)

---

#### 7. `auto-approve-merge.yml` (and 5 related workflows)

**Purpose:** Auto-approve and merge PRs from Copilot

**Quality:** ⭐ Very Poor  
**Complexity:** High  
**Risk:** Extremely High (bypasses code review)  

**Why NOT Adopt:**
- ❌ **Bypasses code review** (violates R-CODE-010)
- ❌ Merges without human verification
- ❌ No rollback on failure
- ❌ Requires elevated PAT token
- ❌ Dangerous for production repos

**Recommendation:** **Never adopt** - code review is non-negotiable

---

#### 8. Project Board Sync Workflows (6 workflows)

**Purpose:** Sync issue status to GitHub Projects v2

**Quality:** ⭐⭐ Poor  
**Complexity:** Medium-High  
**Risk:** Low (read-only impact)  

**Why NOT Adopt:**
- ❌ WingedBean doesn't use GitHub Projects
- ❌ Overly complex for simple status tracking
- ❌ GraphQL API complexity
- ❌ Adds maintenance burden

**Alternative:** Use GitHub issue labels instead

---

## Recommended Adoption Plan

### Phase 1: Foundation (Week 1)

**Goal:** Establish basic PR hygiene and manual Copilot workflow

**Tasks:**
1. ✅ Adopt `ensure-closes-link.yml`
   - Create `.github/workflows/pr-enforce-issue-link.yml`
   - Copy and simplify `scripts/ensure_closes_link.py`
   - Test with sample PR

2. ✅ Adopt `assign-copilot-to-issue.yml` (simplified)
   - Create `.github/workflows/copilot-manual-assign.yml`
   - Remove RFC/Notion logic
   - Test manual assignment

3. ✅ Add PR template
   - Create `.github/pull_request_template.md`
   - Include "Closes #<issue>" section
   - Link to PLAYBOOK.md checklist

**Deliverables:**
- 2 new workflows
- 1 Python script (~50 lines)
- 1 PR template

**Effort:** 4-6 hours

---

### Phase 2: Cleanup (Week 2)

**Goal:** Auto-clean stalled PRs

**Tasks:**
1. ✅ Adopt `cleanup-stalled-prs.yml` (simplified)
   - Create `.github/workflows/cleanup-stalled-prs.yml`
   - Simplify to only close + delete branch
   - Configure 48h timeout
   - Test with stale PR

2. ✅ Create issue template for Copilot work
   - Create `.github/ISSUE_TEMPLATE/copilot-task.yml`
   - Include RFC reference
   - Include acceptance criteria

**Deliverables:**
- 1 new workflow
- 1 issue template

**Effort:** 3-4 hours

---

### Phase 3: Advanced (Future)

**Goal:** Auto-advance workflow (only if needed)

**Tasks:**
1. ⚠️ Evaluate micro-issue workflow
   - Decide if WingedBean needs sequential task splitting
   - If yes, adapt `auto-advance-micro.yml`
   - If no, skip this phase

2. ⚠️ Create "next-issue" labeling convention
   - Define label format: `next-issue: #<num>`
   - Document in PLAYBOOK.md

**Deliverables:**
- TBD (depends on evaluation)

**Effort:** 1-2 days (if needed)

---

## Implementation Details

### Required Files

```
.github/
├── workflows/
│   ├── pr-enforce-issue-link.yml       # Phase 1 (NEW)
│   ├── copilot-manual-assign.yml       # Phase 1 (NEW)
│   └── cleanup-stalled-prs.yml         # Phase 2 (NEW)
├── ISSUE_TEMPLATE/
│   └── copilot-task.yml                # Phase 2 (NEW)
├── pull_request_template.md            # Phase 1 (NEW)
└── scripts/
    └── python/
        └── ensure_closes_link.py       # Phase 1 (NEW)
```

### Required Secrets

**Phase 1-2:** None (use `GITHUB_TOKEN` only)

**Phase 3 (if adopted):** `AUTO_APPROVE_TOKEN` (PAT with `repo` scope)

---

## Risk Assessment

### Low Risk Workflows

| Workflow | Risk | Mitigation |
|----------|------|------------|
| `ensure-closes-link.yml` | Low | Read-only PR check |
| `assign-copilot-to-issue.yml` | Low | Manual trigger only |

### Medium Risk Workflows

| Workflow | Risk | Mitigation |
|----------|------|------------|
| `cleanup-stalled-prs.yml` | Medium | Only target `copilot/*` branches, 48h timeout |

### High Risk Workflows (NOT RECOMMENDED)

| Workflow | Risk | Why Avoid |
|----------|------|-----------|
| `agent-watchdog.yml` | Very High | Destructive, no rollback |
| `auto-advance-micro.yml` | High | Auto-assignment without validation |
| `auto-approve-merge.yml` | Extreme | Bypasses code review |

---

## Alignment with Agent Rules

### Rule Compliance

| Rule | Workflow | Compliance | Notes |
|------|----------|------------|-------|
| R-GIT-010 | `ensure-closes-link.yml` | ✅ Pass | Enforces `Closes #<issue>` |
| R-CODE-010 | All | ✅ Pass | Prefer editing over creating |
| R-CODE-020 | All | ✅ Pass | No fabricated code |
| R-SEC-010 | All | ✅ Pass | No secrets in code |
| R-PRC-010 | `assign-copilot-to-issue.yml` | ✅ Pass | Manual decision points |

### Rule Violations (in reference project)

| Rule | Workflow | Violation | Fix |
|------|----------|-----------|-----|
| R-CODE-020 | `auto-approve-merge.yml` | No human review | Remove workflow |
| R-SEC-010 | `rfc-automation.yml` | `NOTION_TOKEN` in logs | Remove workflow |

---

## Success Metrics

### Phase 1 Metrics

- ✅ 100% of PRs have `Closes #<issue>` link
- ✅ 0 orphaned PRs
- ✅ Manual Copilot assignment takes <1 minute

### Phase 2 Metrics

- ✅ Stalled PRs reduced to <5 at any time
- ✅ Average PR lifetime: <72 hours
- ✅ 0 lingering `copilot/*` branches

---

## Alternative Approaches

### Option A: Minimal Adoption (Recommended)

**Adopt:** `ensure-closes-link.yml` only

**Rationale:**
- Lowest risk
- Highest value (enforces PR→Issue linking)
- No complex scripts

**Effort:** 2 hours

---

### Option B: Moderate Adoption (Proposed)

**Adopt:** Phases 1 + 2

**Rationale:**
- Balances automation with safety
- Useful for Copilot-heavy workflows
- Manageable maintenance burden

**Effort:** 8-10 hours

---

### Option C: Full Adoption (NOT RECOMMENDED)

**Adopt:** All workflows

**Rationale:**
- Maximum automation
- High risk of bugs/security issues
- Heavy maintenance burden
- Not aligned with WingedBean's workflow

**Effort:** 2-3 weeks + ongoing maintenance

---

## Dependencies and Prerequisites

### Required Tools

- ✅ GitHub CLI (`gh`) - Already available in GitHub Actions
- ✅ Python 3.11+ - Already available in GitHub Actions
- ✅ `jq` - Need to install in workflows

### Required Permissions

**For Phase 1-2:**
```yaml
permissions:
  contents: read
  issues: write
  pull-requests: write
```

**For Phase 3 (if adopted):**
```yaml
permissions:
  contents: write  # To delete branches
  actions: write   # To re-run workflows
```

---

## Maintenance Plan

### Weekly Tasks

- Review stalled PR cleanup logs
- Verify `Closes #<issue>` enforcement

### Monthly Tasks

- Review Copilot assignment patterns
- Adjust timeout thresholds if needed

### Quarterly Tasks

- Audit workflow effectiveness
- Consider adopting Phase 3 if patterns emerge

---

## Open Questions

1. **Q:** Should we adopt micro-issue workflow?  
   **A:** Defer decision until RFC volume increases. Current monolithic RFCs are manageable.

2. **Q:** Should we auto-assign Copilot to issues?  
   **A:** No, keep manual control for now. Auto-assignment in Phase 3 if needed.

3. **Q:** Should we use GitHub Projects for tracking?  
   **A:** No, issue labels are sufficient. Avoid complexity.

4. **Q:** Should we auto-approve/merge Copilot PRs?  
   **A:** **Absolutely not.** Code review is non-negotiable (per R-CODE-020).

---

## References

### Source Material

- Reference workflows: `ref-projects/ithome-ironman-2025/.github/workflows/`
- Reference scripts: `ref-projects/ithome-ironman-2025/scripts/python/production/`

### Related Documentation

- [Agent Rules](../../.agent/base/20-rules.md) - R-GIT-010, R-CODE-010, etc.
- [RFC-0011: Starlight Documentation](../rfcs/0011-starlight-documentation-integration.md)
- [GitHub Actions Documentation](https://docs.github.com/en/actions)

---

## Appendix A: Workflow Quality Matrix

| Workflow | Lines | Complexity | Test Coverage | Maintainability | Score |
|----------|-------|------------|---------------|-----------------|-------|
| `ensure-closes-link.yml` | 30 | Low | None | High | ⭐⭐⭐⭐ |
| `assign-copilot-to-issue.yml` | 95 | Medium | None | Medium | ⭐⭐⭐ |
| `cleanup-stalled-prs.yml` | 106 | Medium | None | Medium | ⭐⭐⭐ |
| `auto-advance-micro.yml` | 50 | High | None | Low | ⭐⭐ |
| `agent-watchdog.yml` | 78 | Very High | None | Very Low | ⭐ |
| `rfc-automation.yml` | 185 | Extreme | None | Very Low | ⭐ |
| `auto-approve-merge.yml` | 40 | High | None | Low | ⭐ |

**Scoring Criteria:**
- ⭐⭐⭐⭐ - Production-ready, minimal changes needed
- ⭐⭐⭐ - Good, needs cleanup
- ⭐⭐ - Usable, needs refactoring
- ⭐ - Prototype quality, major issues

---

## Appendix B: Python Scripts Analysis

| Script | Lines | Dependencies | Test Coverage | Recommendation |
|--------|-------|--------------|---------------|----------------|
| `ensure_closes_link.py` | 90 | None | 0% | ✅ Adopt as-is |
| `assign_issue_to_copilot.py` | 120 | `event_bus.py` | 0% | ⚠️ Simplify first |
| `cleanup_recreate_issue.py` | 150 | Many | 0% | ❌ Too complex |
| `auto_approve_or_dispatch.py` | 400 | Many | 0% | ❌ Security risk |

---

## Decision

**Recommendation:** Adopt **Option B** (Moderate Adoption - Phases 1 + 2)

**Rationale:**
- Low risk, high value
- Aligns with agent rules
- Manageable maintenance
- Useful for Copilot workflows
- No external dependencies

**Next Steps:**
1. Review this document with team
2. Approve Phase 1 implementation
3. Create tracking issue for implementation
4. Schedule 2-week implementation window

---

**Status:** Awaiting approval  
**Priority:** P2 (Medium)  
**Target Date:** 2025-10-15  
**Estimated Effort:** 8-10 hours  
**Author:** GitHub Copilot  
**Reviewers:** TBD  

---

**Last Updated:** 2025-10-02

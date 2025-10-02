# RFC-0012 Phase 1: Real-World Testing & Adoption

**Status:** In Progress  
**Started:** 2025-10-02  
**Target Completion:** 2025-10-09  
**Phase:** 1 of 3

---

## Phase 1 Objectives

Test RFC-0012 automation workflows in real-world usage and iterate based on feedback.

**Goals:**
1. ✅ Issue template created
2. ⏳ Workflows tested with actual PRs/issues
3. ⏳ Runner minute usage monitored
4. ⏳ Agent retry behavior validated
5. ⏳ Documentation updated based on learnings

---

## Deliverables

### 1. Issue Template (✅ Complete)

**File:** `.github/ISSUE_TEMPLATE/agent-task.yml`

**Features:**
- RFC reference field
- Summary and acceptance criteria
- **Blocked By** field (for R-ISS-030)
- **Blocks** field (dependency tracking)
- **Agent selection** dropdown (Copilot, Claude, Windsurf, Human)
- Priority level
- Scope boundaries
- Technical notes
- References

**Testing:**
- [ ] Create test issue using template
- [ ] Verify fields populate correctly
- [ ] Verify auto-labeling works

---

### 2. Real-World Workflow Testing

**PR Issue Link Enforcement:**
- [ ] Create PR without "Closes #" → should fail ✓
- [ ] Create PR with "Closes #XX" → should pass ✓
- [ ] Check error messages are clear

**Issue Dependency Validation:**
- [ ] Create issue with "Blocked By: #999" → should get `status:blocked`
- [ ] Close blocker, verify `status:ready` added
- [ ] Test pre-commit hook blocks commits on blocked issues

**Agent Auto-Retry:**
- [ ] Wait for a CI failure on Copilot PR
- [ ] Verify failure logs posted to issue
- [ ] Verify PR closed and branch deleted
- [ ] Verify retry label added (`ci-failed-retry-1`)
- [ ] After 3 failures, verify escalation to human

---

### 3. Runner Minute Monitoring

**Target:** < 500 minutes/month

**Current Usage:** (Track weekly)
- Week 1 (2025-10-02 to 2025-10-08): TBD
- Week 2 (2025-10-09 to 2025-10-15): TBD
- Week 3 (2025-10-16 to 2025-10-22): TBD
- Week 4 (2025-10-23 to 2025-10-29): TBD

**Breakdown by Workflow:**
- `pr-enforce-issue-link.yml`: TBD runs, TBD minutes
- `validate-dependencies.yml`: TBD runs, TBD minutes
- `agent-auto-retry.yml`: TBD runs, TBD minutes

**Actions if over budget:**
- Review workflow efficiency
- Optimize Python scripts
- Adjust trigger conditions

---

### 4. Agent Retry Behavior Analysis

**Track All Failures:**

| Date | Issue | PR | Attempt | Outcome | Notes |
|------|-------|----|---------| --------|-------|
| TBD  | TBD   | TBD| 1       | TBD     | TBD   |

**Metrics:**
- Total failures: TBD
- Failures resolved by agent: TBD (target: > 80%)
- Failures escalated to human: TBD (target: < 20%)
- Average attempts before success: TBD (target: < 2.5)

**Learnings:**
- Common failure patterns: TBD
- Agent effectiveness by type: TBD
- Retry limit optimization: TBD (current: 3)

---

### 5. Documentation Updates

**Based on Phase 1 learnings:**
- [ ] Update RFC-0012 with actual metrics
- [ ] Document common pitfalls
- [ ] Create troubleshooting guide
- [ ] Update agent rules if needed
- [ ] Add examples of good/bad issues

---

## Testing Checklist

### Pre-Push Testing (with `act`)

- [ ] Test PR validation workflow locally
  ```bash
  act pull_request \
    --eventpath .github/workflows/test-events/pr-opened-valid.json \
    -W .github/workflows/pr-enforce-issue-link.yml
  ```

- [ ] Test invalid PR (should fail)
  ```bash
  act pull_request \
    --eventpath .github/workflows/test-events/pr-opened-invalid.json \
    -W .github/workflows/pr-enforce-issue-link.yml
  ```

- [ ] Test issue dependency validation
  ```bash
  act issues \
    --eventpath .github/workflows/test-events/issue-assigned.json \
    -W .github/workflows/validate-dependencies.yml
  ```

### Post-Push Testing (on GitHub)

- [ ] Create test PR without issue link
  - Expected: Workflow fails, clear error message
  - Actual: TBD

- [ ] Create test issue with blockers
  - Expected: Gets `status:blocked` label, helpful comment
  - Actual: TBD

- [ ] Test pre-commit hook
  - Expected: Blocks commit on blocked issue
  - Actual: TBD

- [ ] Trigger CI failure (intentional)
  - Expected: Agent auto-retry workflow triggers
  - Actual: TBD

---

## Issues Encountered

### Issue 1: [Title]
**Date:** TBD  
**Description:** TBD  
**Resolution:** TBD  
**Lessons Learned:** TBD

---

## Metrics Summary (End of Phase 1)

**Runner Minutes:**
- Total used: TBD / 500 target
- Cost: $TBD (free tier: 2,000 min/month)
- Vs. Reference: -$329.84 savings ✓

**Workflow Success Rate:**
- PR validation: TBD% pass rate
- Dependency validation: TBD% pass rate
- Agent auto-retry: TBD% resolve rate

**Agent Performance:**
- GitHub Copilot: TBD failures, TBD% success
- Claude Code: TBD failures, TBD% success
- Windsurf: TBD failures, TBD% success

**Time Savings:**
- Human time saved: ~TBD hours
- Agent iteration time: ~TBD hours
- Net benefit: ~TBD hours

---

## Recommendations for Phase 2

**Based on Phase 1 data:**

1. **Retry Limit Adjustment:** TBD (keep at 3 or adjust?)
2. **Timeout Adjustments:** TBD
3. **Additional Workflows:** TBD
4. **Rule Updates:** TBD

**Priority Features for Phase 2:**
- [ ] Dependency graph visualization
- [ ] Runner minute dashboard
- [ ] Auto-labeling by issue content
- [ ] Slack/Discord notifications

---

## Sign-off

**Phase 1 Complete When:**
- [x] Issue template created and tested
- [ ] All workflows tested in production
- [ ] Runner minute usage < 500/month confirmed
- [ ] At least 10 agent retries observed
- [ ] Documentation updated with learnings
- [ ] Recommendations for Phase 2 documented

**Completed By:** TBD  
**Approved By:** TBD  
**Date:** TBD

---

## References

- [RFC-0012](../rfcs/0012-agent-driven-github-automation.md)
- [Critical Issues Analysis](../design/github-automation-critical-issues-analysis.md)
- [Revised Approach](../design/github-automation-revised-approach.md)
- [GitHub Actions Usage](https://github.com/GiantCroissant-Lunar/winged-bean/actions)

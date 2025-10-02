# RFC-0012 Manual Test Execution Plan

**Date:** 2025-10-02  
**Tester:** User + GitHub Copilot  
**Status:** Ready to Execute  

---

## Pre-Test Checklist

- [x] Labels created (11 labels)
- [x] Workflows deployed to GitHub
- [x] Issue template available
- [x] Pre-commit hook installed
- [x] Git working directory clean
- [ ] Ready to create test issues/PRs

---

## Test 1: Issue Template Creation

**Test ID:** MT-001  
**Objective:** Verify agent-task template creates properly formatted issues

### Procedure

1. Navigate to: https://github.com/GiantCroissant-Lunar/winged-bean/issues/new/choose
2. Select "Agent Task" template
3. Fill in all fields (see test data below)
4. Submit issue
5. Record issue number

### Test Data

```yaml
Title: RFC-0012-02: Implement Phase 2 Advanced Features
RFC Reference: RFC-0012
Summary: Implement Phase 2 advanced features...
Acceptance Criteria: [6 checkboxes]
Blocked By: None
Blocks: TBD
Assigned Agent: Claude Code (deep analysis, architecture)
Priority: P1 (High - important feature)
```

### Expected Results

- ✓ Issue created successfully
- ✓ All fields appear in issue body
- ✓ Formatting is clean and readable
- ✓ Issue number assigned (e.g., #186)

### Actual Results

**Issue Number:** #______  
**Status:** ⏳ PENDING  
**Notes:**

---

## Test 2: Auto-Labeling Workflow

**Test ID:** MT-002  
**Objective:** Verify auto-label-issues.yml extracts and applies labels

### Procedure

1. After creating issue in MT-001, wait 1 minute
2. Go to: https://github.com/GiantCroissant-Lunar/winged-bean/actions
3. Find "Auto-Label Issues" workflow run
4. Wait for completion (~30 seconds)
5. Return to issue
6. Check applied labels

### Expected Results

Labels automatically added:
- ✓ `agent:claude-code` (from "Assigned Agent: Claude Code")
- ✓ `priority:high` (from "Priority: P1")
- ✓ `rfc-0012` (from "RFC Reference: RFC-0012")
- ✓ NO `has-blockers` (because "Blocked By: None")

### Actual Results

**Workflow Run:** https://github.com/GiantCroissant-Lunar/winged-bean/actions/runs/______  
**Labels Applied:** _______________  
**Status:** ⏳ PENDING  
**Notes:**

---

## Test 3: PR Validation - Reject PR Without Issue Link

**Test ID:** MT-003  
**Objective:** Verify pr-enforce-issue-link.yml blocks PRs without issue reference

### Procedure

1. Create test branch: `test/rfc-0012-pr-validation`
2. Make trivial change (add line to README)
3. Commit and push
4. Create PR with title: "Test: PR without issue link"
5. PR description: "This is a test PR without issue link"
6. Submit PR
7. Wait for workflow to run

### Expected Results

- ✓ Workflow: pr-enforce-issue-link.yml triggers
- ✓ Workflow FAILS with clear error message
- ✓ PR shows red X status check
- ✓ Error message: "PR must include 'Closes #' or 'Fixes #'"

### Actual Results

**PR Number:** #______  
**Workflow Status:** _______________  
**Error Message:** _______________  
**Status:** ⏳ PENDING  
**Notes:**

---

## Test 4: PR Validation - Accept PR With Issue Link

**Test ID:** MT-004  
**Objective:** Verify pr-enforce-issue-link.yml accepts PRs with issue reference

### Procedure

1. Edit PR from MT-003
2. Update description: "Test PR for validation\n\nCloses #[ISSUE_FROM_MT-001]"
3. Save changes
4. Wait for workflow to re-run

### Expected Results

- ✓ Workflow: pr-enforce-issue-link.yml triggers
- ✓ Workflow PASSES with success
- ✓ PR shows green ✓ status check
- ✓ PR is valid and can be merged (or closed)

### Actual Results

**Workflow Status:** _______________  
**Status:** ⏳ PENDING  
**Notes:**

---

## Test 5: Dependency Validation - Detect Blockers

**Test ID:** MT-005  
**Objective:** Verify validate-dependencies.yml detects and labels blocked issues

### Procedure

1. Create new issue via template
2. Title: "Test: Issue with Blocker"
3. Summary: "Test issue to verify dependency validation"
4. **Blocked By:** `999` (non-existent issue)
5. Assigned Agent: Copilot
6. Priority: P3
7. Submit issue
8. Wait for workflow to run (~30 seconds)

### Expected Results

- ✓ Workflow: validate-dependencies.yml triggers
- ✓ Issue gets `status:blocked` label
- ✓ Comment posted explaining blockers
- ✓ Comment lists blocker issues (with links)
- ✓ Comment shows which blockers are open

### Actual Results

**Issue Number:** #______  
**Labels Applied:** _______________  
**Comment Posted:** Yes / No  
**Status:** ⏳ PENDING  
**Notes:**

---

## Test 6: Pre-Commit Hook - Block Commit on Blocked Issue

**Test ID:** MT-006  
**Objective:** Verify pre-commit hook blocks commits referencing blocked issues

### Procedure

1. Use issue from MT-005 (blocked issue)
2. Create test branch: `test/blocked-issue-commit`
3. Make trivial change (e.g., add comment to code)
4. Attempt to commit with message: `test: verify pre-commit hook

Refs #[ISSUE_FROM_MT-005]`
5. Observe pre-commit hook output

### Expected Results

- ✓ Pre-commit hook runs
- ✓ Hook executes validate_issue_dependencies.py
- ✓ Detects issue #XXX is blocked by #999
- ✓ Commit is BLOCKED with error message
- ✓ Error lists which blockers must be closed first
- ✓ Error suggests removing issue reference or closing blockers

### Actual Results

**Hook Output:**
```
[Paste output here]
```

**Commit Blocked:** Yes / No  
**Status:** ⏳ PENDING  
**Notes:**

---

## Test 7: Pre-Commit Hook - Allow Commit on Ready Issue

**Test ID:** MT-007  
**Objective:** Verify pre-commit hook allows commits to ready issues

### Procedure

1. Use issue from MT-001 (no blockers, should be ready)
2. Stay on same test branch
3. Make another trivial change
4. Commit with message: `test: verify pre-commit hook allows ready issues

Refs #[ISSUE_FROM_MT-001]`
5. Observe pre-commit hook output

### Expected Results

- ✓ Pre-commit hook runs
- ✓ Hook executes validate_issue_dependencies.py
- ✓ Detects issue has no blockers (or blockers are closed)
- ✓ Commit is ALLOWED
- ✓ Commit completes successfully

### Actual Results

**Hook Output:**
```
[Paste output here]
```

**Commit Allowed:** Yes / No  
**Status:** ⏳ PENDING  
**Notes:**

---

## Test 8: Agent Auto-Retry (Observation Only)

**Test ID:** MT-008  
**Objective:** Verify agent-auto-retry.yml triggers on CI failure

**Note:** This test requires a natural CI failure or intentional failure. We'll observe in production.

### Procedure (When Failure Occurs)

1. Wait for a natural CI failure (MegaLinter, Qodana, etc.)
2. Observe agent-auto-retry workflow
3. Check if:
   - Workflow detects failure
   - Posts logs to linked issue
   - Adds retry label (ci-failed-retry-1)
   - Closes PR
   - Deletes branch (optional)

### Expected Results

- ✓ Workflow triggers within 1 minute of failure
- ✓ Failure logs extracted and posted to issue
- ✓ Issue comment includes:
  - Which workflow failed
  - Error summary
  - Link to failed run
  - Retry count
- ✓ PR closed automatically
- ✓ Retry label added (ci-failed-retry-1, -2, or -3)
- ✓ After 3 failures: escalate to human

### Actual Results

**Will observe in production over next 1-2 weeks**

**Status:** ⏳ DEFERRED (requires CI failure)  
**Notes:**

---

## Summary Checklist

| Test | ID | Status | Pass/Fail | Time |
|------|----|--------|-----------|------|
| Issue Template | MT-001 | ⏳ PENDING | - | - |
| Auto-Labeling | MT-002 | ⏳ PENDING | - | - |
| PR Reject (no link) | MT-003 | ⏳ PENDING | - | - |
| PR Accept (with link) | MT-004 | ⏳ PENDING | - | - |
| Dependency Validation | MT-005 | ⏳ PENDING | - | - |
| Pre-Commit Block | MT-006 | ⏳ PENDING | - | - |
| Pre-Commit Allow | MT-007 | ⏳ PENDING | - | - |
| Agent Auto-Retry | MT-008 | ⏳ DEFERRED | - | - |

**Progress:** 0/8 complete (0%)  
**Estimated Time:** 25-35 minutes  
**Start Time:** _______  
**End Time:** _______  

---

## Test Execution Order

**Phase 1: GitHub UI Tests (10 min)**
1. MT-001: Create issue with template
2. MT-002: Verify auto-labeling (wait 1 min)
3. MT-005: Create blocked issue

**Phase 2: PR Tests (5 min)**
4. MT-003: Create PR without issue link
5. MT-004: Edit PR to add issue link

**Phase 3: Local Tests (10 min)**
6. MT-006: Test pre-commit hook blocks commit
7. MT-007: Test pre-commit hook allows commit
8. Cleanup: Close test PR, clean branches

**Phase 4: Observation (ongoing)**
9. MT-008: Wait for natural CI failure in production

---

## Cleanup Tasks

After testing:
- [ ] Close test PR (MT-003)
- [ ] Close test issue (MT-005 - blocked issue)
- [ ] Keep Phase 2 issue (MT-001) as real tracking issue
- [ ] Delete test branches
- [ ] Update validation report with results
- [ ] Commit test plan with results

---

## Success Criteria

**All tests pass when:**
- Issue template works correctly
- Auto-labeling applies correct labels
- PR validation enforces issue links
- Dependency validation detects blockers
- Pre-commit hook blocks/allows appropriately
- Agent auto-retry observed (or tested manually)

**Ready to execute!** 🚀

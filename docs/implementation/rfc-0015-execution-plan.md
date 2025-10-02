# RFC-0015 Execution Plan: Agent GitHub Automation with Dependency Tracking

**RFC**: [RFC-0015](../rfcs/0015-agent-github-automation-with-dependency-tracking.md)
**Status**: In Progress
**Started**: 2025-10-02
**Last Updated**: 2025-10-02

## Overview

Implementing agent-driven GitHub automation with issue dependency tracking, local testing, and bounded retry logic.

## User Preferences (from discussion)

- **Retry limit**: 3 attempts
- **Pre-commit strictness**: Hard block
- **Failure notification**: Only after 3x failures (avoid noise)
- **Script complexity**: Use Python when Bash > 50 lines or complex logic
- **Local testing**: Prefer `act` for workflow validation

## Progress Tracking

### ✅ Phase 1: Local Testing Infrastructure (COMPLETED)

**Completed**: 2025-10-02

- [x] Create `.github/workflows/TESTING.md` guide
- [x] Implement `development/python/test_scripts.sh` for fast validation
- [x] Document `act` usage patterns
- [x] Commit Phase 1 deliverables

**Artifacts**:
- `.github/workflows/TESTING.md` - Comprehensive testing guide
- `development/python/test_scripts.sh` - Fast Python script validator

**Commit**: `74f9bf79` - "docs: add local workflow testing guide and script"

---

### 🚧 Phase 2: Issue Dependency Infrastructure (IN PROGRESS)

**Target**: 2025-10-02

#### 2.1 Pre-Commit Hook - Issue Metadata Validator

- [ ] Create `development/python/src/hooks/pre_commit_issue_validator.py`
  - [ ] YAML frontmatter parser
  - [ ] Schema validator (required fields)
  - [ ] Circular dependency detector (DFS)
  - [ ] Optional GitHub API check for dependency existence
- [ ] Write unit tests (`development/python/tests/test_issue_validator.py`)
- [ ] Install hook in `.git/hooks/pre-commit` (or via pre-commit framework)
- [ ] Manual test with sample commits

**Deliverables**:
- `development/python/src/hooks/pre_commit_issue_validator.py`
- `development/python/tests/test_issue_validator.py`
- Pre-commit configuration

#### 2.2 Agent Rules Update

- [ ] Update `.agent/base/20-rules.md` with:
  - [ ] **R-ISS-010**: Issue metadata schema requirements
  - [ ] **R-ISS-020**: Script complexity threshold (50 lines → Python)
  - [ ] **R-ISS-030**: Mandatory workflow local testing

**Schema to enforce**:
```yaml
---
rfc: RFC-XXXX
phase: N
wave: N.N
depends_on: [issue_numbers]
blocks: [issue_numbers]
estimate_minutes: NN
priority: critical|high|medium|low
agent_assignable: true
retry_count: 0
max_retries: 3
---
```

#### 2.3 Testing & Validation

- [ ] Create test fixtures (valid/invalid issue metadata)
- [ ] Test with `act` locally
- [ ] Verify pre-commit hook blocks invalid metadata

---

### ⏳ Phase 3: Event-Driven Workflow System (PENDING)

**Target**: Week 2

#### 3.1 Event Router

- [ ] Create `.github/workflows/agent-event-router.yml`
- [ ] Implement `development/python/src/workflows/event_router.py`
- [ ] Event validation and dispatching logic
- [ ] Audit logging

#### 3.2 Assign Agent Workflow

- [ ] Create `.github/workflows/agent-assign.yml`
- [ ] Dependency resolution check
- [ ] Retry count validation
- [ ] Copilot assignment via comment trigger

#### 3.3 Cleanup Stalled PR Workflow

- [ ] Create `.github/workflows/agent-cleanup-stalled-pr.yml`
- [ ] PR inactivity detection (24h threshold)
- [ ] Linked issue extraction
- [ ] **Issue reopening logic** (critical: don't orphan issues)
- [ ] Stale branch cleanup

#### 3.4 Watchdog Retry Workflow

- [ ] Create `.github/workflows/agent-watchdog.yml`
- [ ] Failure detection from `workflow_run` events
- [ ] Bounded retry logic (max 3 attempts)
- [ ] Discussion notification (not issue comment)

---

### ⏳ Phase 4: Documentation & Migration (PENDING)

**Target**: Week 3-4

- [ ] Create `docs/guides/agent-automation.md`
- [ ] Update issue templates with metadata schema
- [ ] Migrate existing issues (add frontmatter)
- [ ] Parallel testing (ref vs new system)
- [ ] Cutover and archive ref-project workflows

---

## Technical Decisions

### Decision 1: Python over Bash for complex scripts
**Rationale**: Per user requirement and R-ISS-020, when scripts exceed 50 lines or use complex logic, extract to Python modules in `development/python/src/workflows/`.

**Impact**:
- ✅ Better testability (unit tests)
- ✅ Easier debugging
- ✅ Type safety (with type hints)
- ⚠️ Need Python runtime in workflows (acceptable, already used)

### Decision 2: Hard block pre-commit hook
**Rationale**: Per user preference, validation failures MUST block commits.

**Impact**:
- ✅ Zero invalid metadata in repo
- ✅ Forces agents to follow schema
- ⚠️ May frustrate quick fixes (provide `--no-verify` escape hatch in docs)

### Decision 3: 3-attempt retry limit
**Rationale**: Per user preference, balance between automation and runner minutes.

**Impact**:
- ✅ Gives agents multiple chances to succeed
- ✅ Limits runner minute waste
- ⚠️ Requires human intervention after 3x failures (acceptable per user)

### Decision 4: `act` for local testing
**Rationale**: Per user requirement and Phase 1 work, test workflows locally before GitHub deployment.

**Impact**:
- ✅ Saves runner minutes dramatically
- ✅ Faster iteration cycle
- ⚠️ Some actions don't work in `act` (document workarounds)

---

## Risks & Mitigations

### Risk 1: Circular dependencies in issues
**Mitigation**: Pre-commit hook detects cycles using DFS algorithm (Phase 2.1)

### Risk 2: High runner minute consumption
**Mitigation**: Event-driven architecture + local testing + bounded retries

### Risk 3: Stalled PRs orphaning issues
**Mitigation**: Cleanup workflow MUST reopen linked issue (Phase 3.3)

### Risk 4: Agent rule compliance
**Mitigation**: Hard-blocking pre-commit hook enforces schema (Phase 2.1)

---

## Success Criteria

- [ ] **Zero** invalid issue metadata reaches GitHub (enforced by hook)
- [ ] **60%** reduction in runner minutes vs ref-project
- [ ] **70%** agent retry success rate within 3 attempts
- [ ] **100%** workflow test coverage with `act` before deployment
- [ ] **< 24h** stalled PR recovery time

---

## Open Issues & Questions

1. **Q**: Should pre-commit hook require network access to verify `depends_on` issues exist?
   **A**: Make optional (fail gracefully if offline), document in R-ISS-010

2. **Q**: How to handle cross-repo dependencies (e.g., winged-bean depends on external library)?
   **A**: Out of scope for this RFC, defer to future work

3. **Q**: Notification strategy for max-retry failures?
   **A**: GitHub Discussion post (not issue comment to reduce noise) - per user preference

---

## Next Steps

1. ✅ Complete Phase 1 (local testing infrastructure) - DONE
2. 🚧 Implement Phase 2.1 (pre-commit hook validator) - IN PROGRESS
3. ⏳ Implement Phase 2.2 (agent rules update)
4. ⏳ Test Phase 2 with sample commits
5. ⏳ Commit Phase 2 deliverables
6. ⏳ Begin Phase 3 (event-driven workflows)

---

## Related Documents

- **RFC**: [RFC-0015](../rfcs/0015-agent-github-automation-with-dependency-tracking.md)
- **Testing Guide**: [.github/workflows/TESTING.md](../../.github/workflows/TESTING.md)
- **Agent Rules**: [.agent/base/20-rules.md](../../.agent/base/20-rules.md)
- **Ref Project**: `ref-projects/ithome-ironman-2025/.github/workflows/`

---

## Change Log

- **2025-10-02 20:30**: Created execution plan, Phase 1 completed
- **2025-10-02 20:35**: Starting Phase 2 implementation

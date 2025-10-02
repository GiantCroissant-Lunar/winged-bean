---
id: RFC-0013
title: Documentation Automation Tooling
status: Draft
created: 2025-10-02
updated: 2025-10-02
category: tooling, documentation
priority: P1
effort: 2-3 days
---

# RFC-0013: Documentation Automation Tooling

## Status

**Draft** - Under review

## Metadata

- **ID**: RFC-0013
- **Created**: 2025-10-02
- **Updated**: 2025-10-02
- **Category**: tooling, documentation
- **Priority**: P1 (High)
- **Estimated Effort**: 2-3 days
- **Dependencies**: None (Phase 1 complete)
- **Blocks**: None

---

## Summary

Implement Python-based automation tooling for documentation management to address growing documentation sprawl. This RFC defines Phase 2 of the documentation management strategy, building on the Enhanced Organization (Phase 1) completed in commit 4335330.

**Key Capabilities**:
1. Frontmatter validation for RFCs and ADRs
2. Orphaned file detection
3. Automated archival of old chat-history and recordings
4. Table of contents generation for index files

---

## Motivation

### Current Problem

As of 2025-10-02, the docs folder contains:
- **59 markdown files** across 13 subdirectories (2.3MB total)
- **No automated validation** of RFC/ADR frontmatter requirements
- **Manual archival** of time-based files (chat-history, recordings)
- **Manual maintenance** of index files and cross-references
- **No detection** of orphaned/unreferenced documents

**Pain Points**:
- Documentation continues to grow without automated management
- Risk of inconsistent frontmatter in RFCs/ADRs (violates R-DOC-020)
- Old chat-history and recordings accumulate (archival policy defined but not enforced)
- Orphaned files may exist without detection
- Index files become stale as docs are added/removed

### Why Now?

1. **Phase 1 Complete**: Enhanced Organization provides foundation and clear structure
2. **Archival Policies Defined**: Retention periods established (30-day, 90-day)
3. **Growth Trajectory**: 59 files will continue growing; automation needed before it becomes unwieldy
4. **Compliance**: R-DOC-020 requires frontmatter validation for RFCs/ADRs
5. **Existing Infrastructure**: Python tooling already in place (`development/python/`)

---

## Proposal

### Overview

Build lightweight Python scripts integrated with pre-commit hooks and CI/CD to automate documentation management tasks.

**Architecture**:
```
development/python/src/scripts/docs/
├── __init__.py
├── validate_frontmatter.py    # Frontmatter validation
├── detect_orphans.py           # Orphaned file detection
├── archive_old_files.py        # Time-based archival
└── generate_toc.py             # TOC generation (future)
```

**Integration Points**:
- Pre-commit hooks (frontmatter validation)
- GitHub Actions workflow (orphan detection, archival reporting)
- Manual CLI (archival execution, TOC generation)

---

### Detailed Design

#### 1. Frontmatter Validator (`validate_frontmatter.py`)

**Purpose**: Enforce R-DOC-020 frontmatter requirements for RFCs and ADRs

**Required Fields**:
- **RFCs**: `id`, `title`, `status`, `category`, `created`, `updated`
- **ADRs**: `id`, `title`, `status`, `date`, `authors`

**Validation Rules**:
```python
RFC_REQUIRED = ['id', 'title', 'status', 'category', 'created', 'updated']
RFC_STATUSES = ['Draft', 'Proposed', 'Accepted', 'Implemented', 'Superseded']
RFC_ID_PATTERN = r'^RFC-\d{4}$'

ADR_REQUIRED = ['id', 'title', 'status', 'date', 'authors']
ADR_STATUSES = ['Accepted', 'Superseded']
ADR_ID_PATTERN = r'^ADR-\d{4}$'
```

**Features**:
- Parse YAML frontmatter from Markdown files
- Validate required fields exist
- Validate field formats (dates, IDs, status values)
- Report missing/invalid fields with file:line references
- Exit code 1 on validation failure (for pre-commit integration)

**Usage**:
```bash
# Pre-commit hook
python development/python/src/scripts/docs/validate_frontmatter.py docs/rfcs/*.md docs/adr/*.md

# CI/CD
python development/python/src/scripts/docs/validate_frontmatter.py --check-all
```

---

#### 2. Orphan Detector (`detect_orphans.py`)

**Purpose**: Find documents not referenced from any index or other document

**Detection Logic**:
1. Scan all `.md` files in `docs/`
2. Extract all Markdown links `[text](path)` and wiki links `[[path]]`
3. Build reference graph
4. Report files with zero incoming references (excluding index files)

**Exclusions**:
- `*/README.md`, `*/INDEX.md` (are indexes themselves)
- Files in `.archive/` (expected to be unreferenced)
- Root `docs/INDEX.md` (root index)

**Output Formats**:
- **Console**: Human-readable list with suggestions
- **JSON**: Machine-readable for CI/CD (`--format=json`)
- **Markdown**: Report for issues/PRs (`--format=markdown`)

**Example Output**:
```
Orphaned Files Detected (3):

  docs/design/old-analysis.md
    → No references found
    → Suggestion: Archive or link from docs/design/README.md

  docs/rfcs/draft-feature.md
    → Status: Draft (no references expected)
    → Suggestion: Complete or remove draft

  docs/guides/deprecated-guide.md
    → Last modified: 2025-08-15 (48 days ago)
    → Suggestion: Archive to .archive/guides/2025/08/
```

**Usage**:
```bash
# CLI check
python development/python/src/scripts/docs/detect_orphans.py

# CI/CD (fail on orphans)
python development/python/src/scripts/docs/detect_orphans.py --strict --format=json

# Generate report for issue
python development/python/src/scripts/docs/detect_orphans.py --format=markdown > orphan-report.md
```

---

#### 3. Auto-Archiver (`archive_old_files.py`)

**Purpose**: Enforce retention policies by archiving old files

**Archival Rules** (from Phase 1):
```python
ARCHIVAL_RULES = {
    'docs/chat-history/*.txt': {'age_days': 30, 'archive_to': '.archive/chat-history/{year}/{month}/'},
    'docs/recordings/*.cast': {'age_days': 30, 'archive_to': '.archive/recordings/{year}/{month}/', 'exclude_pattern': 'baseline-*'},
    'docs/recordings/*.txt': {'age_days': 30, 'archive_to': '.archive/recordings/{year}/{month}/'},
    'docs/test-results/**/*.md': {'age_days': 90, 'archive_to': '.archive/test-results/{year}/{month}/', 'exclude_pattern': 'phase*'},
}
```

**Features**:
- Scan directories for files matching patterns
- Check file modification time against age threshold
- Skip files matching exclusion patterns (e.g., `baseline-*`, `phase*`)
- Create archive directory structure (`YYYY/MM/`)
- Move files to archive location
- Dry-run mode (preview without moving)
- Summary report of archived files

**Safety Features**:
- **Dry-run by default**: Requires `--execute` flag to actually move files
- **Confirmation prompt**: Interactive mode asks before archiving
- **Backup log**: Record all moved files in `.archive/archival-log.json`

**Usage**:
```bash
# Preview what would be archived (dry-run)
python development/python/src/scripts/docs/archive_old_files.py

# Execute archival with confirmation
python development/python/src/scripts/docs/archive_old_files.py --execute

# Auto-confirm (for CI/CD)
python development/python/src/scripts/docs/archive_old_files.py --execute --yes

# Archive specific category only
python development/python/src/scripts/docs/archive_old_files.py --category=recordings --execute
```

**Example Output**:
```
Documentation Archival Report
=============================

Chat History (30-day retention):
  ✓ 2025-09-01-chat-history-consolidated.txt → .archive/chat-history/2025/09/
  ✓ 2025-09-02-session-notes.txt → .archive/chat-history/2025/09/

Recordings (30-day retention):
  ✓ session-1-2025-09-01T14-30-00.cast → .archive/recordings/2025/09/
  ⊘ baseline-pty-session.cast (excluded: baseline)

Test Results (90-day retention):
  ⊘ phase3-9-xterm-regression-test.md (excluded: phase*)

Summary:
  Archived: 3 files
  Skipped: 2 files (exclusions)
  Space saved: 1.2 MB
```

---

#### 4. TOC Generator (`generate_toc.py`) - Future

**Purpose**: Auto-generate table of contents for index files

**Scope**: Future iteration (not part of Phase 2)

**Rationale**: Index files currently maintained manually; automation adds complexity without clear ROI yet. Revisit after 6 months of usage.

---

### Integration with Existing Workflows

#### Pre-commit Hooks

Add to `.pre-commit-config.yaml`:

```yaml
- repo: local
  hooks:
    - id: validate-doc-frontmatter
      name: Validate RFC/ADR Frontmatter (R-DOC-020)
      entry: python development/python/src/scripts/docs/validate_frontmatter.py
      language: python
      files: ^docs/(rfcs|adr)/.*\.md$
      pass_filenames: true
```

#### GitHub Actions Workflow

Create `.github/workflows/docs-maintenance.yml`:

```yaml
name: Documentation Maintenance

on:
  schedule:
    - cron: '0 2 * * 1'  # Weekly on Monday 2 AM
  workflow_dispatch:

jobs:
  check-orphans:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
      - uses: actions/setup-python@v5
        with:
          python-version: '3.11'
      - name: Detect orphaned files
        run: |
          python development/python/src/scripts/docs/detect_orphans.py --format=markdown > orphan-report.md
          cat orphan-report.md >> $GITHUB_STEP_SUMMARY

  archival-report:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
      - uses: actions/setup-python@v5
        with:
          python-version: '3.11'
      - name: Generate archival preview
        run: |
          python development/python/src/scripts/docs/archive_old_files.py > archival-preview.txt
          cat archival-preview.txt >> $GITHUB_STEP_SUMMARY
```

**Note**: Actual archival requires manual execution or separate workflow with write permissions.

---

### Migration Plan

#### Phase 2.1: Frontmatter Validation (Day 1)
1. Implement `validate_frontmatter.py`
2. Add unit tests (pytest)
3. Add pre-commit hook
4. Run validation on existing RFCs/ADRs, fix any issues
5. Document in `docs/guides/documentation-frontmatter-guide.md`

#### Phase 2.2: Orphan Detection (Day 1-2)
1. Implement `detect_orphans.py`
2. Add unit tests
3. Add GitHub Actions workflow
4. Run initial scan, document/fix orphans
5. Document in `docs/guides/documentation-maintenance-guide.md`

#### Phase 2.3: Auto-Archival (Day 2-3)
1. Implement `archive_old_files.py`
2. Add unit tests
3. Add manual CLI documentation
4. Add optional GitHub Actions workflow (manual trigger)
5. Perform initial archival (dry-run → execute)

---

## Benefits

### Immediate
1. ✅ **Compliance**: Automated R-DOC-020 enforcement (frontmatter validation)
2. ✅ **Quality**: Detect orphaned/unreferenced documentation
3. ✅ **Organization**: Automated archival reduces clutter
4. ✅ **Efficiency**: Reduce manual maintenance burden

### Long-term
1. ✅ **Scalability**: Documentation can grow without becoming unmanageable
2. ✅ **Discoverability**: Orphan detection ensures all docs are linked/findable
3. ✅ **Historical Context**: Archive preserves old docs while keeping active docs clean
4. ✅ **Developer Experience**: Clear structure and automated checks improve doc quality

---

## Risks and Mitigations

### Risk 1: False Positives in Orphan Detection
**Impact**: Valid files flagged as orphans (e.g., intentional standalone docs)

**Mitigation**:
- Exclusion patterns for known standalone files
- Allow `.orphan-ok` marker file to explicitly mark intentional orphans
- Manual review of orphan reports before taking action

### Risk 2: Accidental Archival of Important Files
**Impact**: Critical files moved to archive by mistake

**Mitigation**:
- Dry-run mode by default (requires `--execute`)
- Exclusion patterns (e.g., `baseline-*`, `phase*`)
- Archival log for recovery (`.archive/archival-log.json`)
- Files not deleted, just moved (can be restored)

### Risk 3: Maintenance Burden of New Scripts
**Impact**: Scripts become unmaintained or outdated

**Mitigation**:
- Comprehensive unit tests (pytest)
- Simple, focused implementations
- Clear documentation in `docs/guides/`
- CI/CD integration ensures scripts keep working

### Risk 4: Overhead in Pre-commit Hooks
**Impact**: Slow commits due to frontmatter validation

**Mitigation**:
- Only validate modified RFC/ADR files (not all docs)
- Lightweight YAML parsing (use `pyyaml`)
- Cache validation results if needed

---

## Definition of Done

### Implementation
- [x] Phase 1 (Enhanced Organization) complete ✅
- [ ] `validate_frontmatter.py` implemented with tests
- [ ] `detect_orphans.py` implemented with tests
- [ ] `archive_old_files.py` implemented with tests
- [ ] Pre-commit hook configured
- [ ] GitHub Actions workflow created

### Documentation
- [ ] `docs/guides/documentation-frontmatter-guide.md` created
- [ ] `docs/guides/documentation-maintenance-guide.md` created
- [ ] Scripts have `--help` documentation
- [ ] README.md in `development/python/src/scripts/docs/`

### Validation
- [ ] All existing RFCs/ADRs pass frontmatter validation
- [ ] Initial orphan scan completed, results documented
- [ ] Initial archival preview generated
- [ ] Pre-commit hook tested
- [ ] CI/CD workflow tested

### Success Criteria
1. ✅ Frontmatter validation catches invalid RFCs/ADRs before commit
2. ✅ Orphan detection runs weekly, reports available in Actions
3. ✅ Archival can be executed manually with dry-run safety
4. ✅ Documentation guides enable team to maintain system
5. ✅ All scripts have ≥80% test coverage

---

## Dependencies

### Internal
- **Phase 1 Complete**: Enhanced Organization (commit 4335330) ✅
- **Python Infrastructure**: `development/python/` structure ✅
- **Pre-commit Framework**: Already configured ✅

### External
- **Python 3.11+**: Already in use ✅
- **Libraries**:
  - `pyyaml` - YAML frontmatter parsing (lightweight)
  - `pytest` - Testing framework (already in use)
  - `pathlib` - File operations (stdlib)

---

## Alternatives Considered

### Alternative 1: Use Existing Tools (MkDocs + Plugins)

**Pros**:
- Battle-tested solutions
- Rich plugin ecosystem

**Cons**:
- Heavyweight for our needs (we use Astro, not MkDocs)
- Still need custom scripts for archival
- Integration complexity

**Decision**: Rejected - over-engineered for current needs

### Alternative 2: Front Matter CMS (VS Code Extension)

**Pros**:
- GUI for managing docs
- Built-in frontmatter validation

**Cons**:
- Requires VS Code (not CI/CD friendly)
- Doesn't solve archival/orphan detection
- Team may use different editors

**Decision**: Rejected - not automatable for CI/CD

### Alternative 3: GitHub Actions Marketplace Actions

**Pros**:
- Pre-built actions available
- No custom code

**Cons**:
- Limited to GitHub Actions (no pre-commit)
- May not match our exact requirements
- Less control over behavior

**Decision**: Rejected - custom scripts provide better fit and control

---

## References

### Related RFCs/ADRs
- [Phase 1: Enhanced Organization](../INDEX.md) - commit 4335330
- [R-DOC-020](.agent/base/20-rules.md) - RFC frontmatter requirements

### External Resources
- [Front Matter CMS](https://frontmatter.codes/docs) - Inspiration for frontmatter validation
- [Orphaned File Detection](https://medium.com/@MrJamesFisher/orphaned-file-detection-de307d96d5e1) - Detection heuristics
- [Python-Markdown](https://python-markdown.github.io/) - Markdown parsing in Python

### Implementation Guides
- [Frontmatter Validation Guide](../guides/documentation-frontmatter-guide.md) - TBD
- [Maintenance Guide](../guides/documentation-maintenance-guide.md) - TBD

---

## Appendix

### Example Frontmatter Validation Output

```
Validating RFC frontmatter...

❌ docs/rfcs/0014-new-feature.md:
  - Missing required field: 'category'
  - Invalid status: 'In Progress' (must be one of: Draft, Proposed, Accepted, Implemented, Superseded)
  - Invalid ID format: 'RFC-14' (expected: RFC-XXXX with 4 digits)

❌ docs/adr/0007-decision.md:
  - Missing required field: 'authors'
  - Invalid date format: '2025-Oct-02' (expected: YYYY-MM-DD)

✅ docs/rfcs/0013-documentation-automation-tooling.md
✅ docs/adr/0001-use-astro-with-asciinema-player.md

Summary: 2 files passed, 2 files failed
```

### Example Archival Log Format

```json
{
  "archival_runs": [
    {
      "timestamp": "2025-10-02T10:30:00Z",
      "files_archived": [
        {
          "source": "docs/chat-history/2025-09-01-chat-history-consolidated.txt",
          "destination": ".archive/chat-history/2025/09/2025-09-01-chat-history-consolidated.txt",
          "size_bytes": 524288,
          "age_days": 31
        }
      ],
      "files_skipped": [
        {
          "path": "docs/recordings/baseline-pty-session.cast",
          "reason": "exclusion_pattern",
          "pattern": "baseline-*"
        }
      ],
      "summary": {
        "total_archived": 3,
        "total_skipped": 2,
        "space_saved_bytes": 1258291
      }
    }
  ]
}
```

---

**Author**: Claude Code
**Reviewers**: TBD
**Status**: Draft
**Priority**: P1 (High)
**Estimated Effort**: 2-3 days
**Target Date**: 2025-10-05

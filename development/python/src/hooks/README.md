# Git Hooks for Winged Bean

This directory contains Git hooks for enforcing project standards and quality checks.

## Available Hooks

### pre-commit: Issue Metadata Validator

**Script**: `pre_commit_issue_validator.py`

**Purpose**: Validates issue metadata schema in committed files (per R-ISS-010).

**What it checks**:
- YAML frontmatter in markdown files (`.github/ISSUE_TEMPLATE/`, `docs/`)
- Required fields: `rfc`, `depends_on`, `priority`, `agent_assignable`, `retry_count`, `max_retries`
- Valid priority values: `critical`, `high`, `medium`, `low`
- Circular dependency detection
- RFC format validation (`RFC-XXXX`)

**Behavior**:
- ✅ **Hard block** on validation failures (per user preference)
- ⚠️ **Warnings** for non-standard configurations (e.g., `max_retries != 3`)
- ℹ️ **Info** for files without issue metadata (ignored)

## Installation

### Option 1: Manual Installation (Recommended)

Install the hook in your local repository:

```bash
cd /path/to/winged-bean

# Create symlink to pre-commit hook
ln -sf ../../development/python/src/hooks/pre_commit_issue_validator.py .git/hooks/pre-commit

# Verify installation
.git/hooks/pre-commit --help
```

### Option 2: Using pre-commit Framework (Future)

If we adopt the `pre-commit` framework (https://pre-commit.com/):

```yaml
# .pre-commit-config.yaml
repos:
  - repo: local
    hooks:
      - id: issue-metadata-validator
        name: Validate issue metadata
        entry: development/python/src/hooks/pre_commit_issue_validator.py
        language: script
        files: \.(md|yml|yaml)$
```

Then run:
```bash
pre-commit install
```

## Usage

### Normal Workflow

Hooks run automatically on `git commit`:

```bash
git add .github/ISSUE_TEMPLATE/feature.yml
git commit -m "Add feature issue template"

# Hook runs and validates metadata
# ✓ All issue metadata is valid
# [main abc1234] Add feature issue template
```

### Bypass Hook (Emergency Only)

If you need to bypass validation (NOT RECOMMENDED):

```bash
git commit --no-verify -m "Emergency fix"
```

**Warning**: Bypassing the hook violates R-ISS-010 and may cause workflow failures.

## Testing

### Run hook manually on staged files

```bash
# Stage files you want to test
git add path/to/file.md

# Run hook
./development/python/src/hooks/pre_commit_issue_validator.py
```

### Run unit tests

```bash
cd development/python
python -m pytest tests/test_issue_validator.py -v
```

### Test with sample files

Create a test commit to verify hook installation:

```bash
# Create test file with valid metadata
cat > /tmp/test-issue.md << 'EOF'
---
rfc: RFC-0007
depends_on: [48, 62]
priority: high
agent_assignable: true
retry_count: 0
max_retries: 3
---

# Test Issue

This is a test issue with valid metadata.
EOF

# Try to commit (should succeed)
git add /tmp/test-issue.md
git commit -m "Test: valid issue metadata"

# Clean up
git reset HEAD~1
```

## Required Metadata Schema

When creating issues with frontmatter, use this schema:

```yaml
---
rfc: RFC-XXXX                    # Required: RFC identifier
phase: N                         # Optional: Phase number
wave: N.N                        # Optional: Wave number
depends_on: [issue_numbers]      # Required: List of blockers (empty [] if none)
blocks: [issue_numbers]          # Optional: Issues blocked by this one
estimate_minutes: NN             # Optional: Estimated completion time
priority: critical|high|medium|low  # Required: Priority level
agent_assignable: true           # Required: Can agents work on this?
retry_count: 0                   # Required: Current retry attempt (default 0)
max_retries: 3                   # Required: Max retry attempts (default 3)
---
```

### Example: Feature Issue

```yaml
---
rfc: RFC-0007
phase: 1
wave: 1.2
depends_on: [48]
blocks: [86, 87]
estimate_minutes: 45
priority: high
agent_assignable: true
retry_count: 0
max_retries: 3
---

# Add Entity Component System

Implement ECS pattern using Arch library...
```

### Example: Standalone Issue (No Dependencies)

```yaml
---
rfc: RFC-0012
depends_on: []
priority: medium
agent_assignable: true
retry_count: 0
max_retries: 3
---

# Update documentation

Refresh getting started guide...
```

## Troubleshooting

### Error: "Missing required field"

Add the missing field to your frontmatter. See schema above.

### Error: "Invalid priority"

Use one of: `critical`, `high`, `medium`, `low`

### Error: "Field 'depends_on' must be a list"

Change from `depends_on: 48` to `depends_on: [48]`

### Error: "Invalid RFC format"

Use 4-digit format: `RFC-0007` not `RFC-7` or `rfc-0007`

### Warning: "max_retries is X, but user preference is 3"

Non-blocking warning. Update to `max_retries: 3` or ignore.

### Hook doesn't run

Check installation:
```bash
ls -la .git/hooks/pre-commit
# Should show symlink or executable file

# Verify it's executable
chmod +x .git/hooks/pre-commit
```

## Related Documentation

- **RFC-0015**: [Agent GitHub Automation with Dependency Tracking](../../../../docs/rfcs/0015-agent-github-automation-with-dependency-tracking.md)
- **Agent Rules**: [R-ISS-010 through R-ISS-070](../../../../.agent/base/20-rules.md)
- **Workflow Testing**: [.github/workflows/TESTING.md](../../../../.github/workflows/TESTING.md)

## Maintenance

### Adding New Validations

1. Update `IssueMetadataValidator` class in `pre_commit_issue_validator.py`
2. Add corresponding tests in `tests/test_issue_validator.py`
3. Run tests: `pytest tests/test_issue_validator.py -v`
4. Update this README with new validation rules

### Modifying Schema

1. Update `REQUIRED_FIELDS` and validation logic
2. Update RFC-0015 with schema changes
3. Update agent rules (R-ISS-010)
4. Add migration guide if breaking changes
5. Test with existing issues/templates

---

**Last Updated**: 2025-10-02
**Version**: 1.0.0 (Initial implementation per RFC-0015 Phase 2)

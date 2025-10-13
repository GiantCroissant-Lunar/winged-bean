# Local Agent Rules

This directory is for project-specific customizations.

## Files

- `overrides.md` - Override or disable specific rules
- `custom-rules.md` - Add project-specific rules

## Example overrides.md

```markdown
# Project Overrides

## Disabled Rules
- R-CODE-010: disabled (we prefer creating new files)

## Modified Rules
- R-TEST-020: Use xUnit instead of NUnit (project standard)

## Custom Settings
- Max line length: 120 (instead of default 100)
```

## Priority

Rules are loaded in this order (later overrides earlier):
1. Base rules (from cache)
2. Stack rules (from cache)
3. **Local overrides (this directory)** ← Highest priority

These files should be committed to version control.

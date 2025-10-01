# Versioning & Migration Guide

## Version Format
Agent rules follow [Semantic Versioning](https://semver.org/): `MAJOR.MINOR.PATCH`

- **MAJOR**: Breaking changes (rule removal, semantic shift, incompatible adapter changes)
- **MINOR**: Additive changes (new rules, clarifications, new adapters)
- **PATCH**: Bug fixes, typos, formatting improvements

## Version Sync Protocol

### For Adapter Authors
1. Every adapter MUST declare `Base-Version-Expected: X.Y.Z` at the top
2. If base version doesn't match, adapter should fail closed (prompt for upgrade)
3. Adapters should reference rule IDs (e.g., R-SEC-010) not copy rule text

### For Rule Maintainers
1. Update `Version:` in `.agent/base/00-index.md` when making changes
2. Document changes in `.agent/meta/changelog.md`
3. Increment version according to semver rules above

## Rule ID Management

### Immutable IDs
- Once a rule ID is published (e.g., R-SEC-010), it is **immutable**
- Never renumber or repurpose an existing rule ID
- This ensures adapters and documentation references remain stable

### Adding Rules
```markdown
## New Category
R-NEW-010: Description of the new rule.
R-NEW-020: Another rule in this category.
```

### Deprecating Rules
```markdown
## Security
R-SEC-010: [DEPRECATED as of v2.0.0 - see R-SEC-015] Never log secrets.
R-SEC-015: Never log, echo, or invent secrets. Use `<REDACTED>` in examples. (Replaces R-SEC-010)
```

### Modifying Rules
- For semantic changes: deprecate old rule, create new rule with new ID
- For clarifications: update in place, bump PATCH or MINOR version

## Migration Guide Template

When bumping MAJOR version, create `migration-vX-to-vY.md`:

```markdown
# Migration Guide: v1.0.0 → v2.0.0

## Breaking Changes
- [List what changed and why]

## Rule Changes
- R-XXX-YYY: [REMOVED] - reason
- R-XXX-ZZZ: [MODIFIED] - old behavior → new behavior

## Adapter Updates Required
- [What adapters need to change]

## Checklist
- [ ] Update adapter `Base-Version-Expected`
- [ ] Review deprecated rules
- [ ] Update custom workflows referencing changed rules
```

## Current Version
**1.0.0** - Initial release (2025-10-01)

## Version History
| Version | Date       | Changes                    |
|---------|------------|----------------------------|
| 1.0.0   | 2025-10-01 | Initial agent rules system |

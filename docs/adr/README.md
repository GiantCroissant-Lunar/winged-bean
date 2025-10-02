# Architecture Decision Records (ADRs)

**Purpose**: Immutable historical record of architectural decisions for Winged Bean

---

## What are ADRs?

Architecture Decision Records capture important architectural decisions along with their context and consequences. Unlike RFCs which evolve through discussion and implementation, ADRs are **immutable historical records** that document the "what" and "why" of decisions made.

---

## Key Principles

### Immutability
- **Never modified** after creation (historical record)
- **Never deprecated** or archived
- Always remain accessible for future reference

### Content
- **Context**: Why was this decision needed?
- **Decision**: What did we decide?
- **Consequences**: What are the trade-offs and implications?
- **Alternatives**: What other options were considered?

---

## ADR Index

### ADR-0001: Use Astro with Asciinema Player
**Date**: 2025-09-29

**Decision**: Use Astro static site generator with asciinema-player for documentation site

**Context**: Need to display terminal recordings (.cast files) in documentation

**Consequences**:
- ✅ Fast static site generation
- ✅ Native asciinema-player integration
- ⚠️ Requires JavaScript for playback

[Read Full ADR →](./0001-use-astro-with-asciinema-player.md)

---

### ADR-0002: Use Native Tools for Pre-commit Hooks
**Date**: 2025-09-30

**Decision**: Use native tool implementations in pre-commit hooks instead of shell wrappers

**Context**: Initial pre-commit config used shell scripts which caused permission and maintenance issues

**Consequences**:
- ✅ Better cross-platform compatibility
- ✅ Reduced maintenance burden
- ✅ Official hook repositories

[Read Full ADR →](./0002-use-native-tools-for-pre-commit-hooks.md)

---

### ADR-0003: Implement Security and Quality Pre-commit Hooks
**Date**: 2025-09-30

**Decision**: Add comprehensive pre-commit hooks for security scanning and code quality

**Context**: Need automated checks for secrets, security issues, and code quality

**Consequences**:
- ✅ Prevents secret commits
- ✅ Catches common security issues
- ✅ Enforces code formatting
- ⚠️ Slightly slower commit process

[Read Full ADR →](./0003-implement-security-and-quality-pre-commit-hooks.md)

---

### ADR-0004: [Title TBD]
**Status**: Reserved for future use

---

### ADR-0005: [Title TBD]
**Status**: Reserved for future use

---

### ADR-0006: [Title TBD]
**Status**: Reserved for future use

---

## Creating a New ADR

### Naming Convention
- Format: `XXXX-title.md` (4-digit ID, lowercase slug)
- Sequential numbering (never reuse IDs)
- Example: `0007-use-hybrid-clr-for-unity-plugins.md`

### Template

```markdown
# ADR-XXXX: [Title]

**Status**: Accepted
**Date**: YYYY-MM-DD
**Authors**: [Name(s)]

## Context

What is the issue we're facing? What factors influenced this decision?

## Decision

What did we decide to do? Be specific and clear.

## Rationale

Why did we make this decision? What are the key factors?

## Consequences

### Positive
- What benefits does this bring?

### Negative
- What trade-offs or limitations exist?

### Neutral
- What other impacts should be noted?

## Alternatives Considered

1. **Alternative 1**: Brief description and why it was rejected
2. **Alternative 2**: Brief description and why it was rejected

## Related Decisions

- Links to related ADRs or RFCs

---

**Author**: [Name]
**Reviewers**: [Names]
**Status**: Accepted
**Date**: [YYYY-MM-DD]
```

---

## ADR vs RFC

| Aspect | ADR | RFC |
|--------|-----|-----|
| **Purpose** | Record final decisions | Propose and discuss changes |
| **Lifecycle** | Immutable | Draft → Proposed → Accepted → Implemented |
| **When Created** | After decision is made | Before implementation begins |
| **Updates** | Never modified | Updated during discussion |
| **Scope** | Specific decisions | Broad designs and implementations |

---

## Related Documentation

- [RFCs](../rfcs/) - Design proposals and discussions
- [Design Docs](../design/) - Free-form design exploration
- [Implementation Plans](../implementation/) - Execution tracking

---

**Last Updated**: 2025-10-02
**Total ADRs**: 6 (3 active, 3 reserved)

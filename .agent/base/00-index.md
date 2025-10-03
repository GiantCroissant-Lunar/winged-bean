# Agent Instruction Base
Version: 1.1.0
Source of Truth for all automated assistant behavior in Winged Bean project.

## Project Context
- **Type**: Game development framework (.NET/C#, Unity)
- **Phase**: Active development with rapid iteration
- **Focus**: Pragmatic rules that enhance quality without blocking progress

## Composition
- 10-principles.md: Core philosophy
- 20-rules.md: Normative, enforceable rules (ID-based)
- 30-glossary.md: Domain terms

Adapters (in ../adapters) must reference **rule IDs** instead of copying rule text.

## Adapter Sync & Versioning
- Adapters MUST declare `Base-Version-Expected:`. If it doesn't match this file's `Version`, adapters should **fail closed** (ask for upgrade).
- Pointer files (e.g., CLAUDE.md) should redirect agents to this canon and the agent-specific adapter.

All adapters must enforce documentation conventions (R-DOC-010/020).

## Naming Conventions (Documents)
- **RFCs**
  - Location: `docs/rfcs/` (lowercase, plural).
  - Filename: `XXXX-title.md` (4-digit zero-padded number, lowercase slug).
  - Frontmatter REQUIRED (see R-DOC-020): `id`, `title`, `status`, `category`.
  - Cite in text as "RFC 0042". IDs are immutable once published.
- **ADRs**
  - Location: `docs/adr/` (lowercase, singular).
  - Filename: `XXXX-title.md` (4-digit zero-padded number, lowercase slug).
  - Cite as "ADR 0001".
- **Execution Plans**
  - Location: `docs/implementation/`
  - Filename: `rfc-XXXX-execution-plan.md` (tied to RFC number).

Rules:
- Lowercase on disk for portability; use uppercase acronym (RFC/ADR) in prose.
- Once published, numbers/IDs are immutable; create a new document to supersede.

## Change Policy
- **Add rule**: append with a new unique ID; never repurpose IDs.
- **Deprecate rule**: mark "DEPRECATED" but keep the ID (do not delete).
- **Major version bump** if any backward-incompatible change (removal or semantics shift). Minor bump for additive rules or clarifications.

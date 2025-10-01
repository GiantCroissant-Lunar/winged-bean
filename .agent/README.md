# Agent Instructions System

Multi-agent instruction system for Winged Bean project.

## Structure

```
.agent/
├── README.md              # This file
├── base/                  # Canonical rules (source of truth)
│   ├── 00-index.md       # Version 1.0.0, structure, naming conventions
│   ├── 10-principles.md  # 7 core principles
│   ├── 20-rules.md       # 28 normative rules with IDs
│   └── 30-glossary.md    # Domain terminology
├── adapters/             # Agent-specific implementations
│   ├── claude.md         # Claude Code
│   ├── copilot.md        # GitHub Copilot
│   └── windsurf.md       # Windsurf/Codeium
└── meta/                 # Versioning and governance
    ├── changelog.md      # Version history
    ├── versioning.md     # Version sync protocol
    └── adapter-template.md # Template for new adapters
```

## Pointer Files

Agents discover rules via pointer files:
- `/CLAUDE.md` → `.agent/adapters/claude.md`
- `/.github/copilot-instructions.md` → `.agent/adapters/copilot.md`
- `/.windsurf/rules.md` → `.agent/adapters/windsurf.md`

## Rule Categories (28 rules)

- **R-CODE-xxx**: Code quality & architecture (5 rules)
- **R-SEC-xxx**: Security guidelines (4 rules)
- **R-TST-xxx**: Testing standards (3 rules)
- **R-DOC-xxx**: Documentation conventions (5 rules)
- **R-GIT-xxx**: Git workflow (2 rules)
- **R-PRC-xxx**: Process guidelines (4 rules)

## Version Sync

All adapters declare `Base-Version-Expected: 1.0.0` and must sync with base version in `base/00-index.md`.

## Adding a New Agent

1. Copy `.agent/meta/adapter-template.md`
2. Fill in agent-specific details
3. Save to `.agent/adapters/{agent-name}.md`
4. Create pointer file in appropriate location
5. Update `Base-Version-Expected` to current version

## Modifying Rules

1. Update rule in `.agent/base/20-rules.md`
2. Update version in `.agent/base/00-index.md` (following semver)
3. Document change in `.agent/meta/changelog.md`
4. Never reuse or renumber existing rule IDs

## Design Philosophy

**Pragmatic over Perfect**: Rules focus on security, quality, and maintainability without blocking rapid development.

**Version Stability**: Rule IDs are immutable. Deprecated rules are marked but never removed.

**Adapter Composability**: Adapters reference rule IDs rather than duplicating text, ensuring consistency.

## Current Version

**1.0.0** - Initial release (2025-10-01)

See `.agent/meta/changelog.md` for version history.

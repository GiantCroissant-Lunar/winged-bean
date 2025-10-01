# Windsurf Instructions Pointer

Canonical rules: `../.agent/base/`
Adapter details: `../.agent/adapters/windsurf.md`

## Quick Reference
- **Base Version**: 1.0.0
- **Project**: Winged Bean - Game development framework (.NET/C#, Unity)
- **Phase**: Active development
- **Mode**: Cascade (agentic) with plan-first execution

## Guidance for Windsurf
- Plan-first responses for multi-step edits
- Surface risk assessments when proposing changes
- Cite rule IDs when explaining constraints (e.g., "per R-SEC-010")
- Leverage multi-file context for complex refactoring

## Key Rules to Remember
- R-CODE-010: Prefer editing over creating files
- R-CODE-020: Don't fabricate code; ask when uncertain
- R-PRC-010: Propose options for architectural decisions
- R-SEC-xxx: Never log secrets or PII
- R-DOC-xxx: Follow RFC/ADR naming conventions
- R-GIT-010: Use `git commit -F <file>` for commit messages

## CI/CD Sensitivity
- Changes to `.github/workflows/**` require risk/rollback plan
- Call out cross-cutting changes explicitly

## PR Template
Include these sections:
- Summary: What changed and why
- Rationale: Link issues/RFCs
- Risks: What might break + mitigations
- Tests: What was added/updated; how to run
- Rollout/Rollback: Deploy plan and safe rollback

For full rules, see `../.agent/base/20-rules.md`

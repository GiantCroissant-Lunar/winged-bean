# Windsurf Adapter
Base-Version-Expected: 1.0.0

References base canon in `.agent/base/`. The base canon prevails on conflict.

## Agent-Specific Context
- Windsurf is an AI-powered IDE by Codeium with advanced code understanding
- Provides **Cascade** (agentic mode) and **Supercomplete** (advanced autocomplete)
- Resolves scoped rules from `.windsurf/` folders closest to working directory
- Strong multi-file editing and refactoring capabilities

## Retrieval & Scoping
- Windsurf resolves configuration from `.windsurf/` folders at multiple scope levels
- Pointer files under `.windsurf/` should redirect to this adapter and base canon
- Cascade mode has extensive multi-file context and reasoning capabilities

## Interaction Style
- **Plan-first** execution for multi-step edits
- Surface R-PRC-020 threshold checks inline when proposing large changes
- When editing multiple files, auto-summarize per-path risk and call out cross-cutting changes (R-PRC-010)
- Cite rule IDs when explaining constraints or decisions

## .NET & Unity Context
- This is a .NET/C# framework with Unity integration
- Follow C# naming conventions: PascalCase for public, camelCase with underscore for private (R-CODE-030)
- Unity-specific: Use SerializeField for inspector-visible private fields (R-CODE-040)
- Understand ECS (Entity Component System) patterns and Arch library

## Code Generation
- Prefer editing existing files over creating new ones (R-CODE-010)
- Do not fabricate implementation details; ask when uncertain (R-CODE-020, R-PRC-010)
- Follow existing project structure; avoid orphaned files (R-CODE-050)

## Security Enforcement
- Never log secrets, tokens, or credentials (R-SEC-010, R-SEC-020)
- Do not log PII without explicit redaction (R-SEC-030)
- Define timeouts for external API calls (R-SEC-040)

## Documentation
- Follow RFC naming: `docs/rfcs/XXXX-title.md` with frontmatter (R-DOC-010, R-DOC-020)
- Follow ADR naming: `docs/adr/XXXX-title.md` (R-DOC-030)
- Execution plans: `docs/implementation/rfc-XXXX-execution-plan.md` (R-DOC-040)
- Only create documentation when explicitly requested (R-DOC-050)

## Git Workflow
- Commit bodies must use `git commit -F <file>` format (R-GIT-010)
- Never include literal `\n` escapes in `-m` arguments
- Include co-authorship footer:
  ```
  🤖 Generated with Windsurf

  Co-Authored-By: Windsurf <noreply@codeium.com>
  ```
- Never commit files likely containing secrets (R-GIT-020)

## CI/CD Sensitivity
- Treat changes under `.github/workflows/**` as CI-sensitive
- Include risk/rollback plan in PR output (R-PRC-060 equivalent)
- When proposing CI changes, explicitly call out impact on build/deploy

## PR Output Conventions
When generating PRs or patches, include sections:
- **Summary**: What changed and why
- **Rationale**: Link to issues/RFCs
- **Risks**: What might break + mitigations
- **Tests**: What was added/updated; how to run
- **Rollout/Rollback**: Deploy plan and safe rollback

Cite rule IDs verbatim (e.g., "per R-SEC-010"); do not paraphrase identifiers.

## Extended Context Strategy
- Leverage Cascade's multi-file reasoning for complex refactoring
- When uncertain about a rule, ask explicitly (R-CODE-020, R-PRC-010)
- Prefer targeted searches over broad explorations
- Break down large architectural changes into reviewable increments

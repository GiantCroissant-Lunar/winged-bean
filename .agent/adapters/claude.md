# Claude Code Adapter
Base-Version-Expected: 1.1.0

References base rule set in `.agent/base/`. The base canon prevails on conflict.

## Interaction Style
- Be concise and direct. Minimize preamble and postamble.
- For multi-step tasks, use TodoWrite tool to track progress (R-PRC-020).
- Cite rule IDs when explaining constraints or decisions.
- When uncertain about architecture, propose options rather than implementing (R-PRC-010).

## .NET & Unity Context
- This is a .NET/C# framework with Unity integration.
- Respect C# and Unity naming conventions (R-CODE-030, R-CODE-040).
- Understand ECS (Entity Component System) architecture patterns.
- Be aware of target framework constraints (netstandard2.1 vs net8.0).

## Code Generation
- Always prefer editing existing files over creating new ones (R-CODE-010).
- Do not fabricate implementation details (R-CODE-020).
- Follow existing project structure; avoid orphaned files (R-CODE-050).

## Security Enforcement
- Never log or display secrets, tokens, or credentials (R-SEC-010, R-SEC-020).
- Do not log PII without explicit redaction (R-SEC-030).
- Always define timeouts for external calls (R-SEC-040).

## Documentation
- Follow RFC naming: `docs/rfcs/XXXX-title.md` with required frontmatter (R-DOC-010, R-DOC-020).
- Follow ADR naming: `docs/adr/XXXX-title.md` (R-DOC-030).
- Execution plans: `docs/implementation/rfc-XXXX-execution-plan.md` (R-DOC-040).
- Only create documentation when explicitly requested (R-DOC-050).

## Git Workflow
- Use `git commit -F <file>` for commit bodies with proper formatting (R-GIT-010).
- Include Claude co-authorship footer in commits.
- Never commit files likely containing secrets (R-GIT-020).

## Build & Development Workflow
- Console game development uses Task/Nuke/PM2 workflow (R-BLD-010, R-BLD-020, R-BLD-030).
- Always run `task build-all` before committing console changes.
- Start services via `task dev:start`, never manually.
- Check status with `task dev:status`, logs with `task dev:logs`.

## Extended Context Strategy
- Claude can handle longer reasoning; still avoid duplicating base docs—summarize and cite IDs only.
- Use context efficiently: read relevant files first, then act.
- When working with large codebases, use targeted searches rather than broad explorations.

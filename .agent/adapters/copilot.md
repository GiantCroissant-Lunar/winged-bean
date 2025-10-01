# GitHub Copilot Adapter
Base-Version-Expected: 1.0.0

Canonical rules live in `.agent/base/`. The base canon prevails on conflict.

## Retrieval Emphasis
- Copilot prioritizes `.github/` paths; pointer file `.github/copilot-instructions.md` references this adapter.
- GitHub Copilot also reads: `**/AGENTS.md`, `/CLAUDE.md`, `/GEMINI.md`

## Agent-Specific Context
- GitHub Copilot provides inline code suggestions and chat assistance
- Integrated directly into VS Code, Visual Studio, and GitHub web interface
- Best for incremental code completion and quick refactoring
- Limited multi-file context compared to Claude Code

## Interaction Style
- Prefer concise, actionable responses
- For multi-step changes, provide clear plan with rule citations
- Ask when specifications are ambiguous (R-PRC-010)
- Avoid large multi-file rewrites unless explicitly requested

## .NET & Unity Context
- This is a .NET/C# framework with Unity integration
- Follow C# naming conventions (R-CODE-030)
- Respect Unity patterns like SerializeField for private fields (R-CODE-040)
- Understand ECS architecture and Arch library usage

## Code Generation
- Prefer editing existing files over creating new ones (R-CODE-010)
- Do not fabricate implementation details; ask when uncertain (R-CODE-020)
- Follow existing project structure (R-CODE-050)

## Security Enforcement
- Never log secrets, tokens, or credentials (R-SEC-010, R-SEC-020)
- Do not log PII (R-SEC-030)
- Define timeouts for external API calls (R-SEC-040)

## Documentation
- Follow RFC naming: `docs/rfcs/XXXX-title.md` (R-DOC-010, R-DOC-020)
- Follow ADR naming: `docs/adr/XXXX-title.md` (R-DOC-030)
- Only create documentation when explicitly requested (R-DOC-050)

## Git Workflow
- Commit bodies must use `git commit -F <file>` format (R-GIT-010)
- Never include literal `\n` escapes in `-m` arguments
- Include co-authorship footer:
  ```
  Co-Authored-By: GitHub Copilot <noreply@github.com>
  ```
- Never commit files likely containing secrets (R-GIT-020)

## PR Output Conventions
When generating PR descriptions, include:
- **Summary**: What changed and why
- **Rationale**: Link to issues/RFCs
- **Risks**: What might break and mitigations
- **Tests**: What was added/updated and how to run
- **Rollout/Rollback**: Deployment plan and safe rollback

Cite rule IDs verbatim (e.g., "per R-SEC-010"); do not paraphrase identifiers.

## Limitations
- Shorter context window than Claude Code
- Focus on single-file or small multi-file changes
- For complex architectural work, consider escalating to Claude Code

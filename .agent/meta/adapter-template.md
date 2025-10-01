# Adapter Template

Use this template when creating a new agent adapter.

```markdown
# [Agent Name] Adapter
Base-Version-Expected: 1.0.0

References base rule set in `.agent/base/`. The base canon prevails on conflict.

## Agent-Specific Context
- [Brief description of the agent's capabilities]
- [Any unique features or limitations]
- [How this agent is typically used in the workflow]

## Interaction Style
- [How should the agent communicate? Verbose or concise?]
- [Should it provide plans first or act immediately?]
- [How should it cite rules?]

## .NET & Unity Context
- [Any agent-specific considerations for C#/.NET]
- [Unity-specific tooling or integration notes]
- [ECS or framework-specific guidance]

## Code Generation
- Prefer editing existing files over creating new ones (R-CODE-010)
- Do not fabricate implementation details (R-CODE-020)
- Follow .NET and Unity conventions (R-CODE-030, R-CODE-040)

## Security Enforcement
- Never log secrets or credentials (R-SEC-010, R-SEC-020)
- Do not log PII (R-SEC-030)
- Define timeouts for external calls (R-SEC-040)

## Documentation
- Follow RFC naming conventions (R-DOC-010, R-DOC-020)
- Follow ADR naming conventions (R-DOC-030)
- Only create docs when requested (R-DOC-050)

## Git Workflow
- Use proper commit message formatting (R-GIT-010)
- [Agent-specific commit signing or co-authorship details]
- Never commit secrets (R-GIT-020)

## Tool-Specific Features
- [List any unique tools or commands this agent supports]
- [Integration with IDE, CI/CD, or other systems]
- [Limitations compared to other agents]

## Extended Context Strategy
- [How should this agent handle large codebases?]
- [Search strategies, context window management]
- [When to ask for clarification vs. proceeding]
```

## Example Usage

See existing adapters:
- `.agent/adapters/claude.md` - Claude Code
- `.agent/adapters/copilot.md` - GitHub Copilot
- `.agent/adapters/windsurf.md` - Windsurf

## Required Sections
1. **Base-Version-Expected** header (must match base version)
2. **Security Enforcement** (must reference R-SEC rules)
3. **Git Workflow** (must reference R-GIT rules)
4. **Documentation** (must reference R-DOC rules)

## Optional Sections
- Agent-specific tooling or commands
- IDE integrations
- Workflow customizations
- Performance considerations

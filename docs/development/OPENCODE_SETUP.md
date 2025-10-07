# OpenCode Setup Guide

This guide explains how to set up and use OpenCode AI agents in the Winged Bean repository.

## Overview

OpenCode provides AI-powered code assistance directly in GitHub issues and pull requests. This project has 6 specialized sub-agents configured to handle different tasks.

## Prerequisites

1. **GitHub App Installation**
   - Visit: https://github.com/apps/opencode-agent
   - Click "Install"
   - Select the `winged-bean` repository

2. **API Key Setup**
   - Go to repository Settings → Secrets and variables → Actions
   - Add a new secret: `OPENCODE_API_KEY`
   - Value: Your OpenCode Zen API key (from https://opencode.ai)

## Available Sub-Agents

### 1. `@rfc-implementer`
**Purpose:** Implements RFCs following Winged Bean conventions

**Rules enforced:**
- R-CODE-010: Prefer editing existing files
- R-CODE-030: .NET naming conventions
- R-SEC-010: Never log secrets
- R-TST-010: xUnit test conventions
- R-BLD-010: Task/Nuke/PM2 workflow

**Usage:**
```
/opencode @rfc-implementer implement RFC-0042
/opencode @rfc-implementer review execution plan for RFC-0038
```

### 2. `@test-writer`
**Purpose:** Writes xUnit tests for .NET projects

**Rules enforced:**
- R-TST-010: xUnit conventions
- R-TST-020: Unity Test Framework
- R-TST-030: Quarantine flaky tests
- Arrange-Act-Assert pattern
- >80% code coverage goal

**Usage:**
```
/opencode @test-writer add tests for PluginLoader class
/opencode @test-writer write integration tests for ECS system
```

### 3. `@security-auditor`
**Purpose:** Audits code for security issues

**Rules enforced:**
- R-SEC-010: No secret logging
- R-SEC-020: No hardcoded credentials
- R-SEC-030: No PII logging
- R-SEC-040: Timeout/retry policies
- R-GIT-020: No secret files in git

**Usage:**
```
/opencode @security-auditor scan this PR for security issues
/opencode @security-auditor check for hardcoded credentials
```

**Note:** Read-only tools for safety

### 4. `@doc-writer`
**Purpose:** Creates and updates documentation

**Rules enforced:**
- R-DOC-010: RFC naming (docs/rfcs/XXXX-title.md)
- R-DOC-020: RFC frontmatter requirements
- R-DOC-030: ADR naming conventions
- R-DOC-040: Execution plan format
- R-DOC-050: Only create when requested

**Usage:**
```
/opencode @doc-writer create RFC for plugin hot-reload feature
/opencode @doc-writer update README with new build instructions
```

### 5. `@build-helper`
**Purpose:** Assists with build pipeline and service management

**Rules enforced:**
- R-BLD-010: Use Task/Nuke/PM2 workflow
- R-BLD-020: Clean build verification
- R-BLD-030: Service management via Task

**Usage:**
```
/opencode @build-helper diagnose build failure
/opencode @build-helper explain Task workflow
```

### 6. `@issue-manager`
**Purpose:** Creates and manages GitHub issues

**Rules enforced:**
- R-ISS-010: YAML frontmatter requirements
- Required fields: rfc, depends_on, priority, agent_assignable, retry_count, max_retries
- Proper issue linking and dependencies

**Usage:**
```
/opencode @issue-manager create issues for RFC-0043
/opencode @issue-manager update frontmatter for proper dependency tracking
```

## Workflow Triggers

### Basic OpenCode Commands
Triggers: `.github/workflows/opencode.yml`

```
/oc <command>
/opencode <command>
```

### Agent-Specific Commands
Triggers: `.github/workflows/opencode-agent-commands.yml`

```
/opencode @<agent-name> <task>
```

## Configuration Files

### `opencode.json`
Project-level configuration at repository root:
- Default model: `opencode/grok-code` (free tier)
- Temperature: 0.1-0.3 (precise, deterministic)
- Tool permissions per agent
- Agent prompts with rule references

### Agent Rules
Agents follow rules defined in:
- `.agent/base/20-rules.md` - All normative rules
- `CLAUDE.md` - Project instructions

## Examples

### Implementing an RFC
```
# In an issue or PR comment:
/opencode @rfc-implementer implement RFC-0044

# With specific instructions:
/opencode @rfc-implementer implement RFC-0044 focusing on Phase 1 tasks only
```

### Writing Tests
```
# In a PR comment:
/opencode @test-writer add unit tests for the new PluginRegistry methods

# With coverage requirement:
/opencode @test-writer ensure >90% coverage for PluginLoader class
```

### Security Audit
```
# In a PR comment:
/opencode @security-auditor perform full security audit on this PR

# Specific check:
/opencode @security-auditor verify no secrets are logged in the new logging system
```

### Documentation
```
# In an issue:
/opencode @doc-writer create RFC for multi-world ECS support

# Update existing doc:
/opencode @doc-writer update build guide with new Task commands
```

## Workflow Integration

### Auto-Labeling
When agents are invoked, workflows automatically add relevant labels:
- `@rfc-implementer` → `rfc`, `implementation`
- `@test-writer` → `tests`
- `@security-auditor` → `security`, `audit`
- `@doc-writer` → `documentation`
- `@build-helper` → `build`, `infrastructure`
- `@issue-manager` → `project-management`

### Concurrency Control
- One OpenCode execution per issue/PR at a time
- Cancel in-progress runs if new command issued
- Prevents conflicts and wasted API credits

## Troubleshooting

### Agent Not Responding
1. Check if GitHub App is installed for the repository
2. Verify `OPENCODE_API_KEY` secret is set
3. Check workflow logs in Actions tab
4. Ensure comment contains `/opencode` or `/oc`

### Wrong Agent Behavior
1. Check agent configuration in `opencode.json`
2. Verify agent prompt includes correct rules
3. Review `.agent/base/20-rules.md` for rule updates
4. Check temperature setting (lower = more deterministic)

### API Quota Issues
- Grok Code model is free (limited time)
- Monitor usage at https://opencode.ai dashboard
- Consider switching to own Anthropic API key if needed

## Cost Management

**Free tier (current):**
- Model: `opencode/grok-code`
- Limited time promotional offer
- Monitor for quota exhaustion

**Paid options:**
- OpenCode Zen pay-as-you-go
- Bring your own API key (Anthropic, OpenAI)
- Update workflow `env.ANTHROPIC_API_KEY` if using own key

## Security Considerations

### Agent Permissions
- Most agents have `write`, `edit`, `bash` permissions
- `@security-auditor` is **read-only** for safety
- All agents respect R-SEC-* security rules

### Secret Management
- Never commit API keys to repository
- Use GitHub Secrets for all credentials
- Agents automatically redact secrets with `<REDACTED>`

### Code Review
- Always review agent-generated code before merging
- Agents follow conventions but may need adjustments
- Run local tests before accepting changes

## Best Practices

1. **Be specific** - Clear instructions get better results
2. **Reference RFCs** - Agents understand RFC-XXXX format
3. **One task per command** - Don't chain multiple requests
4. **Review outputs** - Agents are assistants, not replacements
5. **Cite rules** - Mention R-* rule IDs when relevant
6. **Check logs** - Workflow logs show agent reasoning

## Related Documentation

- [Agent Rules](.agent/base/20-rules.md) - All normative rules
- [Project Instructions](../../CLAUDE.md) - Quick reference
- [OpenCode Docs](https://opencode.ai/docs/) - Official documentation
- [GitHub Workflows](../../.github/workflows/) - Workflow configurations

## Support

- **OpenCode issues**: https://github.com/sst/opencode/issues
- **Project issues**: https://github.com/GiantCroissant-Lunar/winged-bean/issues
- **OpenCode docs**: https://opencode.ai/docs/

---

**Version:** 1.0.0
**Last Updated:** 2025-10-07
**Maintainer:** Winged Bean Team

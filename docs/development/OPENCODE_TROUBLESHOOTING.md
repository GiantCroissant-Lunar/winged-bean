# OpenCode Integration Troubleshooting Guide

**Last Updated:** 2025-10-07
**Status:** Investigation in Progress

This document captures findings from integrating OpenCode AI agents into the Winged Bean repository, including issues encountered and solutions attempted.

---

## Table of Contents

1. [Overview](#overview)
2. [Configuration Issues Encountered](#configuration-issues-encountered)
3. [Authentication & API Keys](#authentication--api-keys)
4. [Model Availability](#model-availability)
5. [Troubleshooting Steps Taken](#troubleshooting-steps-taken)
6. [Current Status](#current-status)
7. [Next Steps](#next-steps)
8. [Resources](#resources)

---

## Overview

OpenCode is an AI-powered coding assistant that integrates with GitHub issues and pull requests. This document tracks our attempt to integrate it with specialized sub-agents for RFC implementation, testing, security auditing, and more.

**Goal:** Enable AI assistance via `/opencode @agent-name` commands in GitHub issues/PRs.

**Implementation:**
- Configuration: `opencode.json`
- Workflows: `.github/workflows/opencode.yml` and `opencode-agent-commands.yml`
- Documentation: `docs/development/OPENCODE_SETUP.md`

---

## Configuration Issues Encountered

### Issue 1: Invalid Config Schema (RESOLVED ✅)

**Error:**
```
ConfigInvalidError
```

**Cause:**
- Original config included unsupported fields:
  - `temperature` at agent level
  - `prompt` field in JSON config (only supported in Markdown config)
  - Invalid tool names: `glob`, `grep`

**Solution:**
- Added `$schema: "https://opencode.ai/config.json"`
- Removed unsupported fields
- Limited tools to valid names: `write`, `edit`, `bash`, `read`

**Commit:** `84ad285` - "fix: Simplify opencode.json to resolve ConfigInvalidError"

---

### Issue 2: Workflow Trigger Configuration (RESOLVED ✅)

**Problem:**
Workflows didn't trigger when command was in issue body (during creation).

**Cause:**
Workflows trigger on `issue_comment` event (comments only), not `issues` event (issue creation).

**Solution:**
Commands must be posted as **comments** on issues, not in the initial issue body.

**Usage:**
```bash
# Correct: Add as comment
gh issue comment 215 --body "/opencode @test-writer add tests"

# Incorrect: In issue body during creation (won't trigger)
gh issue create --body "/opencode @test-writer add tests"
```

---

### Issue 3: Model Configuration Mismatch (RESOLVED ✅)

**Problem:**
Workflow files had hardcoded model names that differed from `opencode.json`.

**Cause:**
- Workflow: `model: opencode/grok-code`
- Config: Various attempts at different models
- Workflows override config file settings

**Solution:**
Synchronized model configuration across all files:
- `opencode.json`
- `.github/workflows/opencode.yml`
- `.github/workflows/opencode-agent-commands.yml`

**Commit:** `436dc2f` - "fix: Update workflow files to use claude-sonnet-4 model"

---

## Authentication & API Keys

### OPENCODE_API_KEY vs Provider-Specific Keys

OpenCode supports two authentication approaches:

#### Option 1: OpenCode Zen (Hosted Service)

**Environment Variable:** `OPENCODE_API_KEY`

**Configuration:**
```yaml
env:
  OPENCODE_API_KEY: ${{ secrets.OPENCODE_API_KEY }}
with:
  model: opencode/model-name
```

**Setup:**
1. Visit https://opencode.ai/auth
2. Sign in and add billing details
3. Copy API key
4. Run `opencode auth login` locally OR add to GitHub Secrets

**Model Format:** `opencode/model-name`

#### Option 2: Bring Your Own Key (BYOK)

**Environment Variable:** Provider-specific (e.g., `ANTHROPIC_API_KEY`)

**Configuration:**
```yaml
env:
  ANTHROPIC_API_KEY: ${{ secrets.ANTHROPIC_API_KEY }}
with:
  model: anthropic/claude-sonnet-4-20250514
```

**Model Format:** `provider/model-name-version`

**Supported Providers:**
- Anthropic: `anthropic/model-name`
- OpenAI: `openai/model-name`
- Others per OpenCode documentation

---

## Model Availability

### OpenCode Zen Models (via `opencode/` prefix)

According to [OpenCode Zen documentation](https://opencode.ai/docs/zen):

| Model ID | Provider | Status | Pricing |
|----------|----------|--------|---------|
| `opencode/gpt-5` | OpenAI | Available | $1.25 / $10.00 per 1M tokens |
| `opencode/gpt-5-codex` | OpenAI | Available | Pricing varies |
| `opencode/claude-sonnet-4` | Anthropic | Available | $0.80 / $4.00 per 1M tokens |
| `opencode/claude-3-5-haiku` | Anthropic | Available | Lower cost |
| `opencode/claude-opus-4-1` | Anthropic | Available | $15.00 / $75.00 per 1M tokens |
| `opencode/qwen3-coder` | Alibaba | Available | Pricing varies |
| `opencode/grok-code` | xAI | **Free (Limited Time)** | Currently free |
| `opencode/kimi-k2` | Moonshot | Available | Pricing varies |

**Note:** Free models (Grok Code Fast 1, Code Supernova) may collect feedback for improvement.

---

## Troubleshooting Steps Taken

### Attempt 1: Use Free Grok Code Model

**Configuration:**
```json
{
  "model": "opencode/grok-code"
}
```

**Result:** ❌ `ProviderModelNotFoundError`

**Error Details:**
```
ProviderModelNotFoundError: ProviderModelNotFoundError
  data: {
    providerID: "opencode",
    modelID: "grok-code"
  }
```

**Workflow Run:** [18313610699](https://github.com/GiantCroissant-Lunar/winged-bean/actions/runs/18313610699)

---

### Attempt 2: Switch to Claude Sonnet 4

**Configuration:**
```json
{
  "model": "opencode/claude-sonnet-4"
}
```

**Result:** ❌ `ProviderModelNotFoundError`

**Error Details:**
```
ProviderModelNotFoundError: ProviderModelNotFoundError
  data: {
    providerID: "opencode",
    modelID: "claude-sonnet-4"
  }
```

**Workflow Run:** [18313785474](https://github.com/GiantCroissant-Lunar/winged-bean/actions/runs/18313785474)

**Commit:** `43951c3` - "fix: Switch OpenCode model from grok-code to claude-sonnet-4"

---

## Current Status

### ✅ Completed

- [x] Created `opencode.json` with 6 specialized sub-agents
- [x] Created GitHub workflows for OpenCode integration
- [x] Fixed config validation issues
- [x] Synchronized model configuration across files
- [x] Secret `OPENCODE_API_KEY` is configured in repository
- [x] Workflows trigger correctly on issue comments

### ❌ Blocked

- [ ] Model resolution failing for all OpenCode Zen models
- [ ] Unable to verify API key has proper access/credits
- [ ] No successful OpenCode execution

### 🔍 Root Cause Analysis

**Hypothesis 1: Billing/Credits Required**
- OpenCode Zen requires "adding billing details"
- API key may not have credits or active billing

**Hypothesis 2: API Key Scope Mismatch**
- `OPENCODE_API_KEY` may be for GitHub App authentication only
- May need separate OpenCode Zen API key

**Hypothesis 3: Model Access Restrictions**
- Account may not have access to requested models
- Free models may have limited availability

**Hypothesis 4: Service Issues**
- Models may be unavailable in certain regions
- Service may have changed model IDs since documentation

---

## Next Steps

### Immediate Actions

1. **Verify OpenCode Account Status**
   - Visit https://opencode.ai/auth
   - Check billing status
   - Verify available credits
   - Confirm which models are accessible
   - Review API key type and scope

2. **Test Alternative Authentication**
   - Option A: Try with Anthropic API key directly:
     ```yaml
     env:
       ANTHROPIC_API_KEY: ${{ secrets.ANTHROPIC_API_KEY }}
     with:
       model: anthropic/claude-sonnet-4-20250514
     ```
   - Option B: Try different OpenCode Zen model (e.g., `opencode/gpt-5`)

3. **Contact OpenCode Support**
   - Report model not found errors
   - Verify model availability
   - Confirm API key configuration

### Alternative Approaches

#### Option 1: Use Anthropic Direct

**Pros:**
- Direct control over API usage
- No intermediary service dependencies
- Well-documented Claude models

**Cons:**
- Requires separate Anthropic API key
- Pay Anthropic directly (not OpenCode Zen rates)

**Implementation:**
```yaml
# .github/workflows/opencode.yml
env:
  ANTHROPIC_API_KEY: ${{ secrets.ANTHROPIC_API_KEY }}
with:
  model: anthropic/claude-sonnet-4-20250514
```

#### Option 2: Disable OpenCode Integration

If integration proves too complex, remove workflows and rely on:
- Claude Code CLI (current workflow)
- GitHub Copilot
- Manual code reviews

---

## Resources

### Official Documentation

- **OpenCode Docs:** https://opencode.ai/docs/
- **OpenCode GitHub Integration:** https://opencode.ai/docs/github/
- **OpenCode Zen Models:** https://opencode.ai/docs/zen
- **OpenCode Agents:** https://opencode.ai/docs/agents/

### GitHub Resources

- **OpenCode GitHub App:** https://github.com/apps/opencode-agent
- **OpenCode Issues:** https://github.com/sst/opencode/issues

### Project Files

- **Configuration:** `opencode.json`
- **Setup Guide:** `docs/development/OPENCODE_SETUP.md`
- **Workflows:**
  - `.github/workflows/opencode.yml`
  - `.github/workflows/opencode-agent-commands.yml`
- **Test Issue:** https://github.com/GiantCroissant-Lunar/winged-bean/issues/215

---

## Error Reference

### ConfigInvalidError

**Symptoms:** OpenCode fails to load configuration file

**Causes:**
- Unsupported fields in `opencode.json`
- Invalid tool names
- Incorrect schema format

**Solution:**
- Follow schema: https://opencode.ai/config.json
- Use only valid tool names: `write`, `edit`, `bash`, `read`
- Remove unsupported fields: `temperature` (at agent level), `prompt` (in JSON)

---

### ProviderModelNotFoundError

**Symptoms:** OpenCode cannot find specified model

**Error Format:**
```
ProviderModelNotFoundError
  data: {
    providerID: "opencode",
    modelID: "model-name"
  }
```

**Possible Causes:**
1. Model ID doesn't exist or is misspelled
2. API key lacks access to the model
3. Billing not configured / no credits
4. Model discontinued or region-restricted

**Debugging Steps:**
1. Verify model ID in OpenCode Zen docs
2. Check OpenCode dashboard for available models
3. Verify billing and credits
4. Try alternative model
5. Switch to BYOK approach with provider API key

**Workaround:**
Use provider-specific API keys instead of OpenCode Zen:
```yaml
env:
  ANTHROPIC_API_KEY: ${{ secrets.ANTHROPIC_API_KEY }}
with:
  model: anthropic/claude-sonnet-4-20250514
```

---

## Configuration Examples

### Working Config Template (Untested)

```json
{
  "$schema": "https://opencode.ai/config.json",
  "model": "opencode/claude-sonnet-4",
  "agent": {
    "test-writer": {
      "description": "Writes xUnit tests for .NET projects",
      "mode": "subagent",
      "model": "opencode/claude-sonnet-4",
      "tools": {
        "write": true,
        "edit": true,
        "read": true
      }
    }
  }
}
```

### Alternative: Anthropic Direct

```json
{
  "$schema": "https://opencode.ai/config.json",
  "model": "anthropic/claude-sonnet-4-20250514",
  "agent": {
    "test-writer": {
      "description": "Writes xUnit tests for .NET projects",
      "mode": "subagent",
      "model": "anthropic/claude-sonnet-4-20250514",
      "tools": {
        "write": true,
        "edit": true,
        "read": true
      }
    }
  }
}
```

**Workflow:**
```yaml
env:
  ANTHROPIC_API_KEY: ${{ secrets.ANTHROPIC_API_KEY }}
with:
  model: anthropic/claude-sonnet-4-20250514
```

---

## Lessons Learned

1. **Config Validation is Strict**
   - OpenCode validates against a specific schema
   - Unsupported fields cause immediate failure
   - Always include `$schema` for validation

2. **Workflow vs Config Priority**
   - Workflow `with.model` overrides `opencode.json`
   - Must synchronize model across all files

3. **Trigger Events Matter**
   - Commands in issue body ≠ issue comments
   - Use `issue_comment` event for slash commands

4. **Model Availability is Complex**
   - Documentation may not reflect current availability
   - Account-specific access restrictions
   - Billing requirements not always clear upfront

5. **Two-Tier Architecture**
   - OpenCode (framework) vs OpenCode Zen (hosted models)
   - Can mix BYOK and hosted models
   - Authentication differs by approach

---

## Version History

| Date | Version | Changes |
|------|---------|---------|
| 2025-10-07 | 1.0 | Initial troubleshooting document created |

---

**Maintainer:** Winged Bean Team
**Related Issues:** #215
**Status:** Investigation Ongoing

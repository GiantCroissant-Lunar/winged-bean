# Local Workflow Testing Guide

This guide explains how to test GitHub workflows locally using `act` to **save runner minutes** and iterate faster.

## Prerequisites

```bash
# Install act (macOS)
brew install act

# Verify installation
act --version
```

## Quick Start

```bash
# List all workflows
act -l

# Test a specific workflow with a sample event
act -W .github/workflows/validate-dependencies.yml \
    -e .github/workflows/test-events/issue-assigned.json

# Test PR validation
act pull_request -W .github/workflows/pr-enforce-issue-link.yml \
    -e .github/workflows/test-events/pr-opened-valid.json
```

## Test Event Files

Sample event payloads are stored in `.github/workflows/test-events/`:

- `issue-assigned.json` - Valid issue with dependencies
- `issue-blocked.json` - Issue with blocking dependencies
- `pr-opened-valid.json` - PR with proper issue link
- `pr-opened-invalid.json` - PR missing issue link

## Testing Scenarios

### 1. Test Dependency Validation

```bash
# Should pass - valid dependencies
act workflow_dispatch \
    -W .github/workflows/validate-dependencies.yml \
    -e .github/workflows/test-events/issue-assigned.json \
    --secret GITHUB_TOKEN="${GITHUB_TOKEN}"

# Should fail - blocked dependencies
act workflow_dispatch \
    -W .github/workflows/validate-dependencies.yml \
    -e .github/workflows/test-events/issue-blocked.json \
    --secret GITHUB_TOKEN="${GITHUB_TOKEN}"
```

### 2. Test PR Issue Link Enforcement

```bash
# Should pass
act pull_request \
    -W .github/workflows/pr-enforce-issue-link.yml \
    -e .github/workflows/test-events/pr-opened-valid.json

# Should fail
act pull_request \
    -W .github/workflows/pr-enforce-issue-link.yml \
    -e .github/workflows/test-events/pr-opened-invalid.json
```

### 3. Test Auto-Retry Workflow

```bash
act workflow_run \
    -W .github/workflows/agent-auto-retry.yml \
    -e .github/workflows/test-events/pr-check-failed.json \
    --secret GITHUB_TOKEN="${GITHUB_TOKEN}"
```

## Creating Test Events

To capture real event payloads for testing:

```bash
# Method 1: From workflow run logs (requires gh CLI)
gh run view <run-id> --log > workflow.log
# Extract the event JSON from logs

# Method 2: Use GitHub's webhook payload examples
# https://docs.github.com/en/webhooks/webhook-events-and-payloads
```

## Debugging

```bash
# Verbose output
act -v -W .github/workflows/validate-dependencies.yml

# Use specific runner image (faster)
act -P ubuntu-latest=catthehacker/ubuntu:act-latest

# Dry run (show what would execute)
act --dryrun -W .github/workflows/validate-dependencies.yml
```

## CI/CD Integration

Before pushing workflows to GitHub:

1. **Test locally first** with `act`
2. **Validate with multiple event scenarios**
3. **Check resource usage** (act shows step timing)
4. **Only then push** to save runner minutes

## Limitations

- Some GitHub-specific features don't work locally (e.g., GITHUB_TOKEN permissions)
- Matrix builds may behave differently
- Secrets must be provided manually via `--secret` flag
- GitHub actions that clone repositories may fail without proper authentication
  - **Workaround**: Use `--action-offline-mode` or pull actions first with `act --pull=false`

## Best Practices

1. ✅ **Always test with `act` before committing workflow changes**
2. ✅ **Create test events for common scenarios (valid/invalid/edge cases)**
3. ✅ **Use `--dryrun` for quick validation**
4. ✅ **Document expected behavior in test event filenames**
5. ⚠️ **Never commit real secrets in test event files**

## Related

- [act documentation](https://github.com/nektos/act)
- [GitHub event types](https://docs.github.com/en/actions/using-workflows/events-that-trigger-workflows)
- RFC-0003: Agent-Driven Development Workflows

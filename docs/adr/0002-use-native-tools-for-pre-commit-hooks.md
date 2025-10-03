---
id: ADR-0002
title: Use Native Tools for Pre-commit Hooks
status: Accepted
date: 2025-09-29
---

# ADR-0002: Use Native Tools for Pre-commit Hooks

## Status

Accepted

## Context

We need to implement pre-commit hooks for a multi-language project containing C#, Python, Node.js, YAML, and Terraform. The main considerations are:

- Fast execution time to maintain developer productivity
- Comprehensive linting across multiple programming languages
- Consistent code quality enforcement before commits
- Good developer experience to encourage adoption
- Integration with CI/CD pipeline for comprehensive checks

## Decision

We will use native tools (not Docker containers) for pre-commit hooks, while using megalint with Docker containers in CI/CD for comprehensive validation.

## Rationale

### Native Tools for Pre-commit Benefits

- **Fast Execution**: Native tools start immediately without Docker container overhead (2-5+ seconds saved per hook)
- **Better Developer Experience**: Quick feedback encourages developers to use hooks rather than bypass them
- **Resource Efficiency**: No Docker daemon requirements or container resource overhead
- **Simplified Setup**: Developers can install tools directly via package managers (npm, pip, etc.)
- **IDE Integration**: Native tools work seamlessly with language servers and IDE linting

### Hybrid Approach Benefits

- **Local Speed**: Fast pre-commit validation for immediate feedback
- **CI Consistency**: Docker-based megalint ensures consistent, comprehensive validation across environments
- **Language Coverage**: Pre-commit handles common issues, CI handles comprehensive analysis
- **Flexibility**: Can adjust local vs CI validation independently

## Consequences

### Positive

- Developers will experience fast, responsive pre-commit validation
- Higher adoption rate of pre-commit hooks due to speed
- Seamless integration with developer IDEs and editors
- Clear separation between local validation (speed) and CI validation (comprehensive)
- M4 Mini performance will make native tools nearly instantaneous

### Negative

- Need to maintain two sets of linting configurations (pre-commit + CI)
- Potential version drift between local tools and CI environment
- Developers must install language-specific tools locally
- Risk of inconsistency between local and CI validation results

## Implementation Notes

### Pre-commit Configuration

```yaml
# .pre-commit-config.yaml
repos:
  - repo: https://github.com/pre-commit/pre-commit-hooks
    rev: v4.4.0
    hooks:
      - id: trailing-whitespace
      - id: end-of-file-fixer
      - id: check-yaml

  - repo: https://github.com/charliermarsh/ruff-pre-commit
    rev: v0.1.0
    hooks:
      - id: ruff
      - id: ruff-format

  - repo: https://github.com/pre-commit/mirrors-eslint
    rev: v8.50.0
    hooks:
      - id: eslint
      - id: prettier

  - repo: https://github.com/pre-commit/mirrors-terraform
    rev: v1.3.0
    hooks:
      - id: terraform-fmt
      - id: terraform-validate
```

### CI Configuration

- Use megalint with Docker for comprehensive validation
- Configure megalint to handle all languages: C#, Python, Node.js, YAML, Terraform
- Ensure CI catches any issues missed by local pre-commit hooks

### Developer Setup

1. Install pre-commit: `pip install pre-commit`
2. Install hooks: `pre-commit install`
3. Install required language tools (ruff, eslint, terraform, etc.)

## Date

2025-09-29

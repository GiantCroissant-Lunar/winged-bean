---
id: ADR-0003
title: Implement Security and Quality Pre-commit Hooks
status: Accepted
date: 2025-09-29
---

# ADR-0003: Implement Security and Quality Pre-commit Hooks

## Status

Accepted

## Context

We need to implement comprehensive security and code quality checks as part of our pre-commit workflow. Key requirements include:

- Prevent accidental commits of secrets, credentials, and sensitive data
- Enforce consistent code quality and formatting across multiple languages (C#, Python, Node.js, YAML, Terraform)
- Detect potential security vulnerabilities before they reach the repository
- Maintain fast execution times to preserve developer experience
- Provide immediate feedback to developers during the commit process

## Decision

We will implement a comprehensive pre-commit hook strategy that includes:

1. **Linting** for code quality across all languages
2. **GitLeaks** for secret detection and credential scanning
3. **Additional security tools** for vulnerability detection
4. **Code formatting** for consistency

## Rationale

### Security Benefits

- **Secret Prevention**: GitLeaks prevents credentials, API keys, and tokens from being committed
- **Early Detection**: Catches security issues before code review, reducing remediation costs
- **Compliance**: Helps meet security compliance requirements for sensitive projects
- **Developer Education**: Immediate feedback teaches developers about security best practices

### Code Quality Benefits

- **Consistency**: Automated formatting ensures uniform code style across the team
- **Error Prevention**: Linting catches common bugs and anti-patterns early
- **Multi-language Support**: Comprehensive coverage for our technology stack
- **Maintainability**: Cleaner code is easier to maintain and review

### Performance Considerations

- **Native Tools**: Fast execution without Docker overhead
- **Incremental Scanning**: Only scan changed files for speed
- **Parallel Execution**: Multiple hooks can run concurrently
- **Selective Hooks**: Different hooks for different file types

## Consequences

### Positive

- Significant reduction in security incidents and credential leaks
- Improved code quality and consistency across the codebase
- Faster code review cycles due to automated quality checks
- Enhanced security posture with multiple layers of protection
- Educational value for developers learning security best practices

### Negative

- Initial setup complexity with multiple tools and configurations
- Potential for false positives requiring allowlist management
- Additional time added to commit process (though minimal with native tools)
- Need for team training on new tools and workflows
- Maintenance overhead for keeping tools and rules updated

## Implementation Notes

### Pre-commit Configuration

```yaml
# .pre-commit-config.yaml
repos:
  # Basic file hygiene
  - repo: https://github.com/pre-commit/pre-commit-hooks
    rev: v4.4.0
    hooks:
      - id: trailing-whitespace
      - id: end-of-file-fixer
      - id: check-yaml
      - id: check-json
      - id: check-xml
      - id: check-merge-conflict
      - id: check-case-conflict
      - id: mixed-line-ending

  # Secret detection
  - repo: https://github.com/gitleaks/gitleaks
    rev: v8.18.0
    hooks:
      - id: gitleaks

  # Python linting and formatting
  - repo: https://github.com/charliermarsh/ruff-pre-commit
    rev: v0.1.0
    hooks:
      - id: ruff
        args: [--fix, --exit-non-zero-on-fix]
      - id: ruff-format

  # JavaScript/TypeScript linting and formatting
  - repo: https://github.com/pre-commit/mirrors-eslint
    rev: v8.50.0
    hooks:
      - id: eslint
        files: \.(js|ts|jsx|tsx)$
        additional_dependencies:
          - eslint@8.50.0
          - "@typescript-eslint/parser@6.7.0"
          - "@typescript-eslint/eslint-plugin@6.7.0"

  - repo: https://github.com/pre-commit/mirrors-prettier
    rev: v3.0.3
    hooks:
      - id: prettier
        files: \.(js|ts|jsx|tsx|json|yaml|yml|md)$

  # Terraform
  - repo: https://github.com/antonbabenko/pre-commit-terraform
    rev: v1.83.5
    hooks:
      - id: terraform_fmt
      - id: terraform_validate
      - id: terraform_tflint

  # YAML linting
  - repo: https://github.com/adrienverge/yamllint
    rev: v1.32.0
    hooks:
      - id: yamllint
        args: [-d, relaxed]

  # Security scanning for dependencies
  - repo: https://github.com/PyCQA/safety
    rev: 2.3.4
    hooks:
      - id: safety
        files: requirements.*\.txt$

  # Additional security checks
  - repo: https://github.com/Yelp/detect-secrets
    rev: v1.4.0
    hooks:
      - id: detect-secrets
        args: ['--baseline', '.secrets.baseline']
```

### GitLeaks Configuration

```toml
# .gitleaks.toml
title = "Gitleaks Config"

[extend]
useDefault = true

[[rules]]
description = "AWS Access Key"
id = "aws-access-key"
regex = '''AKIA[0-9A-Z]{16}'''

[[rules]]
description = "Private Key"
id = "private-key"
regex = '''-----BEGIN (RSA|OPENSSH|DSA|EC|PGP) PRIVATE KEY-----'''

[allowlist]
description = "Allowlisted files"
files = [
    '''(.*)?\.md$''',
    '''(.*)?\.txt$''',
]
paths = [
    '''tests/fixtures/''',
    '''docs/examples/''',
]
```

### Tool Installation

```bash
# Install pre-commit
pip install pre-commit

# Install language-specific tools
npm install -g eslint prettier
pip install ruff safety
brew install gitleaks yamllint terraform tflint

# Install hooks
pre-commit install
```

### Security Baseline Management

- Use `.secrets.baseline` for detect-secrets to manage known false positives
- Regular updates to security rules and allowlists
- Team training on handling security alerts and exceptions

### CI Integration

- Run same checks in CI with megalint for consistency
- Fail builds on security violations
- Generate security reports for compliance

## Date

2025-09-29

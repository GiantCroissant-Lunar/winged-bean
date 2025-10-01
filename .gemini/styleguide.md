# Winged Bean Repository Review Guide

This style guide augments Gemini's default review focus areas with project-specific expectations for the winged-bean multi-language, plugin-based development platform.

## Project Context

**Winged Bean** is a modular development platform featuring:
- **Plugin Architecture** - Extensible .NET plugin system with hot-reload support (AssemblyLoadContext)
- **Multi-Language Support** - .NET, Node.js/TypeScript, Python, Unity
- **Infrastructure as Code** - Terraform with Terraform Cloud automation
- **CI/CD Pipeline** - MegaLinter, pre-commit hooks, Act for local testing
- **Security First** - SOPS encryption, GitLeaks, secret management

## Languages & Stacks

- **.NET 8.0+** (Plugin architecture, host implementations, contracts)
- **Node.js/TypeScript** (PTY service, documentation sites, tooling)
- **Python 3.8+** (Utilities, validation scripts, tooling)
- **PowerShell** (Infrastructure automation, Terraform Cloud scripts)
- **Terraform** (Infrastructure as code for GitHub, GCP)
- **Unity / C#** (Game engine plugin host - planned)

## General Principles

1. **Security & secret hygiene first** - No exposed credentials, proper SOPS usage
2. **Plugin architecture integrity** - Maintain profile-agnostic design
3. **Fail fast** - Small, actionable findings over broad refactors
4. **Consistency across languages** - Naming, error handling, explicit exits
5. **Documentation accuracy** - Keep docs in sync with implementation
6. **Minimize noise** - Focus on high-value, actionable feedback

## .NET / C# Guidelines

### Plugin Architecture Specific
- **Profile-Agnostic Design**: Plugin interfaces must not assume a specific runtime (Console, Unity, Godot, Web)
- **ALC (AssemblyLoadContext)**: Ensure collectible contexts for hot-reload support
- **Dependency Resolution**: Validate topological sort logic for plugin dependencies
- **Manifest Validation**: Check `.plugin.json` structure matches `PluginManifest` schema
- **Service Registration**: Plugins should use DI container properly via `IPluginActivator`

### General C# Standards
- Follow Microsoft C# coding conventions
- Use nullable reference types where appropriate
- Prefer `async/await` over `Task.Result` or `.Wait()`
- Use `CancellationToken` for long-running operations
- Validate plugin lifecycle: `ActivateAsync` → `DeactivateAsync`

### What to Skip
- Formatting issues (handled by `dotnet format`)
- Generated Unity files (`Library/`, `Temp/`, `Obj/`, `Build/`)
- Style preferences already enforced by EditorConfig

## Node.js / TypeScript Guidelines

### Project Standards
- Use **pnpm** as package manager (workspace configured)
- TypeScript strict mode enabled
- ESLint + Prettier for linting and formatting

### PTY Service Specific
- Validate proper PTY lifecycle management
- Check for memory leaks in long-running processes
- Ensure proper cleanup on process termination

### What to Review
- Async/await usage and error handling
- WebSocket connection management
- Process spawning and cleanup
- Type safety and null checks

### What to Skip
- Formatting (handled by Prettier)
- Linting issues already caught by ESLint

## Python Guidelines

### Project Standards
- Python 3.8+ compatibility
- Use **Ruff** for linting and formatting (line length 88)
- Type hints with **mypy** for type checking
- **pytest** for testing with coverage

### What to Review
- Security issues (Bandit findings)
- Type safety (mypy compliance)
- Error handling and explicit exceptions
- Secret scanning logic (`validate_secrets.py`)

### What to Skip
- Style changes already covered by Ruff configuration
- Formatting (handled by `ruff format`)
- Issues already flagged by pre-commit hooks

## PowerShell Guidelines

### Infrastructure Scripts
- Scripts should use `Set-StrictMode -Version Latest`
- Use `$ErrorActionPreference = 'Stop'` for critical scripts
- Functions must have `[CmdletBinding()]` and `param()` blocks
- Validate required parameters with `[Parameter(Mandatory)]`

### Terraform Cloud Scripts Specific
- Check API error handling (TFC API calls)
- Validate SOPS decryption logic
- Ensure proper token validation (`$env:TFC_TOKEN`)
- Check file path resolution (cross-platform compatibility)

### What to Review
- Error handling completeness
- Parameter validation
- Security (token handling, secret management)
- Cross-platform compatibility

## Terraform Guidelines

### Versioning & Providers
- Every provider in `required_providers` must declare version constraint
- Prefer `~> X.Y` (pessimistic) for stability
- Review `.terraform.lock.hcl` changes carefully
- Validate `required_version` is specified

### GitHub Provider Specific
- Check branch protection configurations
- Validate secret management (no plaintext secrets)
- Review repository settings and access controls

### What to Review
- Logical drift risks (unpinned providers, missing validation)
- Input variable validation blocks for critical variables
- State management and backend configuration
- Security implications of resource changes

### What to Skip
- Formatting (handled by `terraform fmt`)
- Minor style preferences

## Secret Hygiene

### Classification
- **CRITICAL**: Private keys, Age secret keys, SSH keys
- **HIGH**: Cloud/API tokens, GitHub PAT, TFC tokens
- **MEDIUM**: JWT tokens, high-entropy strings
- **LOW**: Informational, placeholders

### Detection Rules
- Fail threshold: HIGH (configurable via `SECRET_VALIDATOR_FAIL_LEVEL`)
- Encrypted artifacts (SOPS/Age headers) should not trigger warnings
- Placeholders (`<...>`, `CHANGEME`, `example`) are benign in templates
- High-entropy heuristic uses adjustable threshold

### What to Review
- Actual secrets in code or configs
- Improper SOPS usage or unencrypted sensitive files
- Secrets in commit messages or PR descriptions
- Age key exposure (should be in `.gitignore`)

### What to Skip
- Findings already reported by GitLeaks or detect-secrets
- Encrypted file contents (`.encrypted`, `.sops.json`)
- Test fixtures with dummy credentials

## Infrastructure as Code

### Terraform Cloud Scripts
- Validate API authentication and error handling
- Check SOPS encryption/decryption logic
- Ensure proper secret file auto-detection
- Review variable upsert logic (create vs update)

### SOPS & Age Encryption
- Verify age key generation and storage
- Check `.sops.yaml` configuration
- Validate encryption/decryption workflows
- Ensure proper key rotation procedures

## Documentation

### ADRs (Architecture Decision Records)
- Check for outdated decisions
- Validate status (proposed, accepted, deprecated, superseded)
- Ensure consequences section is complete

### README Files
- Verify badges are accurate and functional
- Check installation instructions are current
- Validate links to documentation
- Ensure examples are runnable

### Code Comments
- Flag misleading or outdated comments
- Encourage comments for complex plugin logic
- Document public API contracts

## Commit & PR Standards

### Commit Messages
- Follow Conventional Commits format (enforced by commitizen)
- Types: `feat`, `fix`, `docs`, `chore`, `refactor`, `test`, `ci`
- Only flag if breaking convention or obscuring intent

### Pull Requests
- Check PR description completeness
- Validate linked issues
- Review breaking changes documentation

## CI/CD & Testing

### MegaLinter
- Don't duplicate findings from MegaLinter
- Focus on logic issues not caught by linters

### Pre-commit Hooks
- Validate hook configuration is appropriate
- Check for hooks that should be enabled
- Don't recommend removed hooks without justification

### Act (Local Testing)
- Validate workflow compatibility with Act
- Check for Act-specific issues in workflows

## Noise Reduction

### Avoid Commenting On
- Formatting already auto-managed by tools
- Linting issues caught by pre-commit hooks
- Build artifacts in ignored directories
- Reference projects (`ref-projects/`)
- Lock files and generated code
- Redundant security warnings about excluded artifacts

### Focus On
- Security vulnerabilities and secret exposure
- Functional correctness and reliability
- Plugin architecture integrity
- Breaking changes to public APIs
- Misleading or outdated documentation
- Missing error handling or validation

## Review Priorities (Descending Order)

1. **Security** - Secrets exposure, authentication, authorization
2. **Plugin Architecture** - ALC integrity, profile-agnostic design, lifecycle
3. **Functional Correctness** - Logic errors, edge cases, error handling
4. **Infrastructure** - Terraform drift, SOPS usage, TFC automation
5. **Documentation** - Accuracy, completeness, broken links
6. **Maintainability** - Dead code, duplication, missing validation
7. **Performance** - Only if non-trivial or likely impact
8. **Style** - Only if causes ambiguity or error risk

## Plugin Architecture Specific Reviews

### Core Interfaces
- `IPluginLoader` - Profile-specific loading logic
- `IPluginActivator` - Plugin entry point for service registration
- `ILoadedPlugin` - Loaded plugin representation
- `PluginManifest` - Metadata structure validation

### Discovery & Loading
- `PluginDiscovery` - Manifest scanning logic
- `PluginDependencyResolver` - Topological sort correctness
- `AlcPluginLoader` - ALC creation and cleanup
- `HostBootstrap` - Orchestration and lifecycle

### What to Validate
- Circular dependency detection
- Plugin unload/reload correctness
- Service resolution from plugins
- Manifest schema compliance
- Cross-profile compatibility

## When Unsure

Prefer asking a clarifying question rather than proposing speculative changes. Consider:
- Is this a real issue or a style preference?
- Is this already caught by automated tooling?
- Does this affect security, correctness, or maintainability?
- Is the suggested change aligned with project architecture?

## Special Considerations

### Multi-Language Workspace
- Respect language-specific conventions
- Don't apply one language's patterns to another
- Consider cross-language integration points

### Plugin System
- Maintain profile-agnostic abstractions
- Validate plugin isolation and hot-reload
- Check for proper resource cleanup

### Infrastructure Automation
- Validate idempotency of scripts
- Check error handling in automation
- Ensure proper secret management

### Security-First Approach
- Prioritize security findings
- Validate encryption usage
- Check for proper authentication/authorization

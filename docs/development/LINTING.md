# Linting Strategy - Hybrid Approach

This project uses a **hybrid linting approach** combining fast local validation with comprehensive CI checks.

## Overview

- **Local Development**: Pre-commit hooks with native tools (fast feedback)
- **CI/CD Pipeline**: MegaLinter with Docker containers (comprehensive validation)

## Local Setup (Pre-commit Hooks)

### Installation

```bash
# Install pre-commit
pip install pre-commit

# Install language-specific tools
pip install ruff bandit safety
npm install -g eslint prettier
brew install gitleaks yamllint terraform tflint shellcheck

# Install .NET SDK (if not already installed)
# brew install --cask dotnet

# Install JetBrains CLI (optional, for enhanced C# analysis)
# Download from: https://www.jetbrains.com/toolbox-app/

# Install the hooks
pre-commit install
```

### What Runs Locally

Pre-commit hooks provide fast feedback on:

- **Security**: GitLeaks, detect-secrets, bandit
- **Code Quality**: Ruff (Python), ESLint/Prettier (JS/TS)
- **Infrastructure**: Terraform fmt/validate
- **General**: YAML lint, trailing whitespace, large files

### Usage

```bash
# Hooks run automatically on commit
git commit -m "your message"

# Run manually on all files
pre-commit run --all-files

# Run specific hook
pre-commit run ruff

# Skip hooks (not recommended)
git commit --no-verify -m "skip hooks"
```

## CI Pipeline (MegaLinter)

### What Runs in CI

MegaLinter provides comprehensive validation:

- **All languages**: C#, Python, Node.js, YAML, Terraform
- **Security scanning**: Credentials, secrets, vulnerabilities
- **Code quality**: Formatting, linting, best practices
- **Documentation**: Markdown, comments, structure

### GitHub Actions (Optimized)

The CI workflow (`.github/workflows/mega-linter.yml`) is optimized for maximum performance:

- **Speed**: ~1-2 minutes (vs 3-4 minutes for standard workflows)
- **Features**:
  - Uses lightweight "cupcake" MegaLinter flavor
  - Docker layer caching for faster subsequent runs
  - Parallel execution of tools
  - Background image pulling while installing native tools
  - Quick native pre-checks for immediate feedback

The workflow runs on:

- All pushes to any branch
- Pull requests to main/master
- Validates only changed files (except main branch)

### Configuration Files

- **MegaLinter**: `.mega-linter.yml`
- **GitLeaks**: `.gitleaks.toml`
- **YAML Lint**: `.yamllint.yml`
- **Secrets Baseline**: `.secrets.baseline`

## Project Structure

```
winged-bean/
├── .pre-commit-config.yaml     # Local pre-commit hooks
├── .mega-linter.yml           # CI linting configuration
├── .gitleaks.toml             # Secret detection rules
├── .yamllint.yml              # YAML linting rules
├── .secrets.baseline          # Known false positives
├── .github/workflows/
│   └── mega-linter.yml        # CI workflow
└── projects/
    ├── python/                # Python projects
    │   ├── pyproject.toml     # Python config
    │   └── requirements*.txt
    ├── nodejs/                # Node.js projects
    │   ├── .eslintrc.js       # ESLint config
    │   └── .prettierrc        # Prettier config
    ├── dotnet/                # .NET projects
    └── infra/terraform/       # Terraform configs
```

## Language-Specific Configuration

### Python (`projects/python/`)

- **Linter**: Ruff (replaces flake8, black, isort)
- **Type Checking**: MyPy
- **Security**: Bandit, Safety
- **Config**: `pyproject.toml`

### JavaScript/TypeScript (`projects/nodejs/`)

- **Linter**: ESLint with TypeScript support
- **Formatter**: Prettier
- **Config**: `.eslintrc.js`, `.prettierrc`

### C# (`projects/dotnet/`)

- **Formatter**: dotnet format (whitespace, style)
- **Code Quality**: JetBrains CLI (cleanup, inspection)
- **Config**: Built-in .NET formatting rules + JetBrains profiles
- **Requirements**: .NET SDK, optional JetBrains CLI (`jb`)

### Terraform (`infra/terraform/`)

- **Formatter**: terraform fmt
- **Linter**: tflint
- **Validator**: terraform validate

### YAML (All `.yml`/`.yaml` files)

- **Linter**: yamllint
- **Config**: `.yamllint.yml`

## Security Features

### Secret Detection

- **GitLeaks**: Comprehensive secret scanning
- **detect-secrets**: Additional secret detection with baseline
- **Custom Rules**: AWS, GitHub, Azure, JWT tokens

### Vulnerability Scanning

- **Python**: Safety for dependency vulnerabilities
- **Node.js**: npm audit (via ESLint plugins)
- **General**: Bandit for Python security issues

## Troubleshooting

### Pre-commit Issues

```bash
# Update hooks to latest versions
pre-commit autoupdate

# Clear cache and reinstall
pre-commit clean
pre-commit install

# Debug specific hook
pre-commit run ruff --verbose
```

### False Positives

1. **Secrets**: Add to `.secrets.baseline`
2. **GitLeaks**: Update `.gitleaks.toml` allowlist
3. **Code Quality**: Use inline disable comments

### Performance Tips

- Pre-commit hooks run only on changed files
- Use `--no-verify` sparingly for emergency commits
- Run `pre-commit run --all-files` before major commits

## Benefits

### Local Development

✅ **Fast**: Native tools start immediately
✅ **Immediate Feedback**: Catch issues before commit
✅ **IDE Integration**: Works with language servers
✅ **Selective**: Only runs on changed files

### CI Pipeline

✅ **Comprehensive**: Full project validation
✅ **Consistent**: Docker ensures same environment
✅ **Reporting**: Detailed reports and GitHub integration
✅ **Security**: Advanced vulnerability scanning

## Migration Guide

### From Existing Projects

1. **Backup existing configs**: Save current lint configurations
2. **Install tools**: Follow installation steps above
3. **Test incrementally**: Run on small changesets first
4. **Update CI**: Replace existing CI linting with MegaLinter
5. **Train team**: Share this documentation with developers

### Adding New Languages

1. **Pre-commit**: Add language-specific hooks to `.pre-commit-config.yaml`
2. **MegaLinter**: Enable language in `.mega-linter.yml`
3. **File patterns**: Update file filters for new extensions
4. **Documentation**: Update this guide with new language info

## Support

- **Pre-commit**: <https://pre-commit.com/>
- **MegaLinter**: <https://megalinter.io/>
- **Project Issues**: Create GitHub issues for project-specific problems

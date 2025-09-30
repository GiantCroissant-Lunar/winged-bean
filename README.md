Terraform fmt........................................(no files to check)Skipped
Terraform validate...................................(no files to check)Skipped
Terraform validate with tflint.......................(no files to check)Skipped# Winged Bean

A multi-language project with comprehensive CI/CD pipeline and local development tools.

## Project Structure

- `projects/nodejs/` - Node.js/TypeScript projects
- `projects/python/` - Python projects
- `docs/` - Documentation and ADRs
- `infra/` - Infrastructure as code (Terraform)

## Development Setup

### Prerequisites

- Docker
- Node.js (for JavaScript/TypeScript projects)
- Python 3.8+ (for Python projects)
- Terraform (for infrastructure)

### Local Workflow Testing with Act

This project uses [act](https://github.com/nektos/act) for local GitHub Actions testing.

#### Quick Start

```bash
# Install act (macOS)
brew install act

# Test workflows locally
act -l                    # List available workflows
act --dryrun             # Dry run all workflows
act -j megalinter        # Run MegaLinter workflow
```

#### Documentation

- [Complete Act Usage Guide](docs/development/ACT_USAGE.md)
- [Architecture Decision Record](docs/adr/0004-adopt-act-for-local-github-actions-testing.md)

### CI/CD Pipeline

The project uses MegaLinter for comprehensive code quality and security checks across all languages:

- **Languages**: Python, JavaScript/TypeScript, C#, Terraform, YAML, JSON, Markdown
- **Security**: GitLeaks, Bandit, Safety
- **Quality**: Ruff, ESLint, Prettier, terraform fmt
- **Local Testing**: Act for GitHub Actions simulation

## Contributing

1. Install development dependencies
2. Set up pre-commit hooks: `pre-commit install`
3. Test workflows locally with act before committing
4. Follow existing code style and conventions

## Documentation

- [Architecture Decision Records](docs/adr/)
- [Development Documentation](docs/development/)
- [Linting Configuration](docs/development/LINTING.md)

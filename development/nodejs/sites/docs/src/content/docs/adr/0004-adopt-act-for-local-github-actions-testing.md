# ADR-0004: Adopt act for Local GitHub Actions Testing

## Status

Accepted

## Context

We need to improve our GitHub Actions development workflow efficiency. Current challenges include:

- **Slow feedback loops**: Testing GitHub Actions changes requires pushing commits and waiting for CI runs
- **Expensive CI minutes**: Our complex MegaLinter workflow pulls large Docker images and runs comprehensive multi-language linting
- **Complex workflow debugging**: Our current workflow includes Docker layer caching, parallel tool installation, and sophisticated orchestration that's difficult to debug remotely
- **Multi-language project complexity**: Supporting Python, JavaScript/TypeScript, C#, and Terraform across different project directories requires reliable workflow testing

Our current MegaLinter workflow (`.github/workflows/mega-linter.yml`) is particularly complex with:

- Docker image pre-pulling and layer caching
- Parallel native tool installation
- Multi-step environment setup with 20+ environment variables
- Comprehensive linting across 5+ languages with custom configurations

## Decision

We will adopt `act` (<https://github.com/nektos/act>) to enable local testing of GitHub Actions workflows.

## Rationale

### Development Efficiency Benefits

- **Fast Feedback**: Test workflow changes locally without committing and waiting for CI
- **Cost Reduction**: Avoid consuming GitHub Actions minutes during development and debugging
- **Improved Debugging**: Debug complex workflow orchestration steps locally with full control
- **Iterative Development**: Rapidly iterate on workflow optimizations and configurations

### Technical Alignment

- **Docker-based**: Our existing workflow already uses Docker heavily, making act a natural fit
- **Environment Simulation**: act closely mimics GitHub's runner environment using Docker containers
- **Configuration Testing**: Perfect for validating our complex MegaLinter configuration changes
- **Multi-language Support**: Handles our diverse technology stack effectively

### Project-Specific Benefits

- **MegaLinter Optimization**: Test Docker layer caching and image pulling strategies locally
- **Configuration Validation**: Verify `.mega-linter.yml` changes across multiple project directories
- **Environment Variable Testing**: Debug complex environment setup without remote CI runs
- **Workflow Performance**: Optimize parallel execution and caching strategies

## Consequences

### Positive

- Significant reduction in development cycle time for CI/CD changes
- Lower GitHub Actions usage costs during development
- Enhanced ability to debug and optimize complex workflows
- Better developer experience with immediate feedback
- Reduced risk of broken workflows reaching main branch
- Easier onboarding for team members working on CI/CD

### Negative

- Additional tool installation and setup required for development machines
- Initial Docker image downloads will be substantial (similar to current CI)
- Some GitHub-specific features may not be perfectly replicated locally
- Requires team training on act usage and best practices
- Potential for environment differences between local act and GitHub Actions

### Neutral

- Docker dependency (already required for our current workflow)
- Learning curve offset by improved productivity
- Storage requirements manageable with Docker image cleanup

## Implementation Notes

### Installation

```bash
# macOS
brew install act

# Other platforms
# Download from https://github.com/nektos/act/releases
```

### Basic Usage

```bash
# List available workflows
act -l

# Run the MegaLinter workflow
act -j megalinter

# Run with specific event
act push

# Dry run to see what would execute
act --dryrun
```

### Configuration

- Create `.actrc` file for project-specific act configuration
- Configure Docker image preferences to match GitHub runners
- Set up environment variable files for sensitive data (excluded from git)
- Document team workflow for using act in development

### Integration with Existing Workflow

- Use act primarily for development and testing workflow changes
- Keep existing CI/CD pipeline unchanged for production use
- Consider act usage in PR review process for workflow changes
- Document act usage in development guidelines

### Team Adoption Strategy

1. Install act on development machines
2. Create team documentation for common act usage patterns
3. Use act for all workflow modifications before PR creation
4. Share Docker image caching strategies to optimize performance

## Date

2025-09-29

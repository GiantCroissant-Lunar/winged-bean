# Act Usage Guide

This document describes how to use `act` to test GitHub Actions workflows locally in the winged-bean project.

## Overview

[Act](https://github.com/nektos/act) allows you to run GitHub Actions workflows locally using Docker, providing fast feedback during CI/CD development.

## Prerequisites

- Docker installed and running
- Act installed (see Installation section)
- Sufficient disk space (~2GB for runner image + workflow images)

## Installation

### macOS
```bash
brew install act
```

### Other Platforms
Download from [act releases](https://github.com/nektos/act/releases)

## Configuration

The project includes pre-configured files for act:

- `.actrc` - Main configuration file with sensible defaults
- `.env.act` - Environment variables for local testing (excluded from git)
- `.github/events/push.json` - Sample event payload for testing

## Basic Usage

### List Available Workflows
```bash
act -l
```

### Run the MegaLinter Workflow
```bash
# Dry run (shows what would execute)
act --dryrun

# Run the full workflow
act

# Run specific job
act -j megalinter

# Run with specific event
act push
act pull_request
```

### Common Options
```bash
# Run with verbose output
act --verbose

# Run specific workflow file
act -W .github/workflows/mega-linter.yml

# Run with custom environment
act --env CUSTOM_VAR=value

# Skip specific steps (useful for debugging)
act --skip-steps="Archive reports"
```

## Development Workflow

### 1. Modify Workflows
Make changes to `.github/workflows/mega-linter.yml` or `.mega-linter.yml`

### 2. Test Locally
```bash
# Quick validation
act --dryrun

# Full test run
act -j megalinter
```

### 3. Debug Issues
```bash
# Run with verbose logging
act --verbose

# Access the runner container for debugging
act --container-daemon-socket /var/run/docker.sock
```

### 4. Commit Changes
Once local testing passes, commit your workflow changes.

## Troubleshooting

### Docker Issues
If you encounter Docker permission issues:
```bash
# Add your user to docker group (Linux)
sudo usermod -aG docker $USER

# Or run with sudo (not recommended)
sudo act
```

### Container Architecture Issues (Apple Silicon)
The `.actrc` file includes `--container-architecture linux/amd64` for compatibility.

### Large Image Downloads
First run will download ~500MB runner image. This is cached for future runs.

### Memory Issues
If workflows fail due to memory constraints:
```bash
# Run with more memory
act --container-options="--memory=4g"
```

## Environment Variables

The `.env.act` file contains default environment variables. You can override these:

```bash
# Override specific variables
act --env MEGALINTER_FLAVOR=javascript

# Use different env file
act --env-file .env.production
```

## Advanced Usage

### Custom Runner Images
Modify `.actrc` to use different runner images:
```bash
-P ubuntu-latest=ghcr.io/catthehacker/ubuntu:full-latest
```

### Workflow Secrets
For workflows requiring secrets, add them to `.env.act`:
```bash
GITHUB_TOKEN=your_token_here
CUSTOM_SECRET=secret_value
```

### Selective Step Execution
Skip time-consuming steps during development:
```bash
# Skip artifact upload
act --skip-steps="Archive reports"

# Skip Docker pulls
act --skip-steps="Pre-pull MegaLinter image"
```

## Performance Tips

1. **Use Docker Layer Caching**: Keep Docker running to maintain image cache
2. **Selective Testing**: Use `--job` flag to run specific jobs only
3. **Skip Non-Essential Steps**: Use `--skip-steps` for faster iterations
4. **Parallelize Development**: Run act while developing to catch issues early

## Integration with Development

### Pre-commit Testing
Add act testing to your development workflow:
```bash
# Before committing workflow changes
act --dryrun && git add . && git commit -m "Update workflow"
```

### CI/CD Development
Use act for iterative CI/CD development:
1. Modify workflow files
2. Test with `act --dryrun`
3. Run full test with `act`
4. Refine and repeat
5. Commit when satisfied

## Limitations

- Some GitHub-specific features may not work identically
- Actions that depend on GitHub's infrastructure may fail
- File system permissions may differ from GitHub runners
- Some composite actions might behave differently

## Support

For act-specific issues:
- [Act GitHub Repository](https://github.com/nektos/act)
- [Act Documentation](https://nektosact.com/)

For project-specific workflow issues:
- Review workflow files in `.github/workflows/`
- Check MegaLinter configuration in `.mega-linter.yml`
- Consult team documentation
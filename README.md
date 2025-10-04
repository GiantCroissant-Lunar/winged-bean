# Winged Bean

[![MegaLinter](https://github.com/GiantCroissant-Lunar/winged-bean/workflows/MegaLinter/badge.svg?branch=main)](https://github.com/GiantCroissant-Lunar/winged-bean/actions?query=workflow%3AMegaLinter+branch%3Amain)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)
[![Pre-commit](https://img.shields.io/badge/pre--commit-enabled-brightgreen?logo=pre-commit)](https://github.com/pre-commit/pre-commit)
[![Terraform](https://img.shields.io/badge/Terraform-1.0+-623CE4?logo=terraform)](https://www.terraform.io/)
[![.NET](https://img.shields.io/badge/.NET-8.0-512BD4?logo=dotnet)](https://dotnet.microsoft.com/)
[![Node.js](https://img.shields.io/badge/Node.js-20+-339933?logo=node.js)](https://nodejs.org/)
[![Python](https://img.shields.io/badge/Python-3.8+-3776AB?logo=python)](https://www.python.org/)

A multi-language, plugin-based development platform with comprehensive CI/CD pipeline, infrastructure automation, and local development tools.

## 🎯 Project Overview

**Winged Bean** is a modular development platform featuring:

- **Plugin Architecture** - Extensible .NET plugin system with hot-reload support
- **Multi-Language Support** - .NET, Node.js/TypeScript, Python, Unity
- **Infrastructure as Code** - Terraform configurations with Terraform Cloud automation
- **CI/CD Pipeline** - MegaLinter integration with local testing via Act
- **Security First** - SOPS encryption, pre-commit hooks, GitLeaks scanning

## 📁 Project Structure

```
winged-bean/
├── projects/
│   ├── dotnet/          # .NET plugin architecture & host implementations
│   ├── nodejs/          # Node.js/TypeScript projects (PTY service, sites)
│   ├── python/          # Python utilities and tools
│   └── unity/           # Unity plugin host (planned)
├── infra/
│   ├── terraform/       # Infrastructure as Code
│   │   ├── github/      # GitHub repository management
│   │   ├── gcp/         # Google Cloud Platform resources
│   │   └── scripts/     # Terraform Cloud automation scripts
│   ├── ansible/         # Configuration management (planned)
│   └── docker/          # Container configurations
├── docs/
│   ├── adr/             # Architecture Decision Records
│   ├── design/          # Design documents
│   └── development/     # Development guides
└── .github/
    └── workflows/       # GitHub Actions CI/CD
```

## 🚀 Key Features

### Plugin Architecture (.NET)

✅ **Multi-Platform Ready** - Framework targeting for Unity/Godot compatibility:

- **AssemblyLoadContext (ALC)** based plugin loading with hot-reload
- **Dependency Resolution** - Topological sort with circular dependency detection
- **Profile-Agnostic Design** - Console, Unity, Godot, and Web profiles
- **Service Integration** - Full dependency injection support
- **Manifest-Based Discovery** - JSON-based plugin metadata
- **Framework Targeting** - netstandard2.1 (Tier 1/2), net8.0 (Tier 3/4)

**Framework Compliance:**
- ✅ Contracts: `.NET Standard 2.1` for Unity/Godot compatibility
- ✅ Infrastructure: `.NET Standard 2.1` for portability
- ✅ Console: `.NET 8.0` LTS for modern features
- ✅ Source Generators: `.NET Standard 2.0` for Roslyn compatibility

[Learn more →](development/dotnet/README.md) | [Framework Guide →](docs/guides/framework-targeting-guide.md)

### Infrastructure Automation

- **Terraform Cloud Scripts** - PowerShell automation for TFC workflows
- **SOPS Encryption** - Secure secret management with age encryption
- **GitHub Management** - Automated repository configuration
- **Multi-Cloud Ready** - GCP and GitHub provider configurations

[Learn more →](infra/terraform/scripts/README.md)

### CI/CD Pipeline

- **MegaLinter** - Comprehensive linting across all languages
- **Pre-commit Hooks** - Security and quality checks before commit
- **Local Testing** - Act for GitHub Actions simulation
- **Parallel Execution** - Optimized workflow performance

## 🛠️ Development Setup

### Prerequisites

| Tool | Version | Purpose |
|------|---------|---------|
| **Docker** | Latest | Container runtime for Act and services |
| **.NET SDK** | 8.0+ | Plugin architecture and C# projects |
| **Node.js** | 20+ | TypeScript projects and tooling |
| **Python** | 3.8+ | Python utilities and pre-commit |
| **Terraform** | 1.0+ | Infrastructure provisioning |
| **SOPS** | Latest | Secret encryption/decryption |
| **Act** | Latest | Local GitHub Actions testing |

### Quick Start

```bash
# Clone repository
git clone https://github.com/GiantCroissant-Lunar/winged-bean.git
cd winged-bean

# Install pre-commit hooks
pip install pre-commit
pre-commit install

# Install Act for local workflow testing (macOS)
brew install act

# Build via Task (artifact-first policy)
cd build
task build-all

# Run ConsoleDungeon.Host from versioned artifacts
task console:normal   # full UI
# or
task console:debug    # blank UI for isolating input handling
```

### Local Workflow Testing with Act

Test GitHub Actions workflows locally before pushing:

```bash
# List available workflows
act -l

# Dry run all workflows
act --dryrun

# Run MegaLinter workflow
act -j megalinter

# Run specific event
act pull_request
```

📖 [Complete Act Usage Guide](docs/development/ACT_USAGE.md)

## 🔒 Security & Quality

### Pre-commit Hooks

Automated checks before every commit:

- **GitLeaks** - Secret detection
- **Bandit** - Python security scanning
- **Safety** - Python dependency vulnerability checks
- **Terraform** - Format and validation
- **YAML/JSON** - Syntax validation

### Secret Management

Secrets are encrypted using **SOPS** with **age** encryption:

```bash
# Generate age key pair
./infra/terraform/scripts/New-AgeKeyPair.ps1

# Encrypt secrets
./infra/terraform/scripts/Encrypt-Secrets.ps1 -Force

# Apply to Terraform Cloud
./infra/terraform/scripts/Apply-SecretsJson.ps1 \
  -Organization giantcroissant-lunar \
  -Workspace winged-bean-github
```

## 📚 Documentation

### Architecture Decision Records (ADRs)

- [ADR-0001: Use Astro with Asciinema Player](docs/adr/0001-use-astro-with-asciinema-player.md)
- [ADR-0002: Use Native Tools for Pre-commit Hooks](docs/adr/0002-use-native-tools-for-pre-commit-hooks.md)
- [ADR-0003: Implement Security and Quality Pre-commit Hooks](docs/adr/0003-implement-security-and-quality-pre-commit-hooks.md)
- [ADR-0004: Adopt Act for Local GitHub Actions Testing](docs/adr/0004-adopt-act-for-local-github-actions-testing.md)

### Development Guides

- [Linting Configuration](docs/development/LINTING.md)
- [Act Usage Guide](docs/development/ACT_USAGE.md)
- [Advanced Plugin Features](docs/development/ADVANCED_PLUGIN_FEATURES.md)

### Design Documents

- [Console MVP Migration Plan](docs/design/console-mvp-migration-plan.md)
- [Tier 1 Core Contracts](docs/design/tier1-core-contracts.md)

## 🤝 Contributing

1. **Fork the repository**
2. **Create a feature branch**: `git checkout -b feature/amazing-feature`
3. **Install pre-commit hooks**: `pre-commit install`
4. **Test locally with Act**: `act pull_request`
5. **Commit changes**: `git commit -m 'Add amazing feature'`
6. **Push to branch**: `git push origin feature/amazing-feature`
7. **Open a Pull Request**

### Code Style

- **.NET**: Follow Microsoft C# coding conventions
- **TypeScript**: ESLint + Prettier configuration
- **Python**: Ruff for linting and formatting
- **Terraform**: `terraform fmt` for consistent formatting

## 📋 Roadmap

### Current Phase: Plugin Architecture Foundation ✅

- [x] Core plugin interfaces and abstractions
- [x] ALC-based plugin loader for console
- [x] Dependency resolution with topological sort
- [x] Hot-reload support
- [x] Sample Asciinema recorder plugin
- [ ] Unit tests (>80% coverage) - **In Progress**

### Next Phase: Multi-Profile Support

- [ ] Unity profile with HybridCLR
- [ ] Godot profile implementation
- [ ] Web profile with ES modules
- [ ] Cross-profile plugin compatibility

### Future Enhancements

- [ ] Plugin marketplace
- [ ] Plugin signing and sandboxing
- [ ] Multi-version plugin support
- [ ] Ansible playbooks for runner provisioning

## 📄 License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## 🙏 Acknowledgments

- **MegaLinter** - Comprehensive linting solution
- **Act** - Local GitHub Actions testing
- **SOPS** - Secure secret management
- **Terraform Cloud** - Infrastructure automation platform

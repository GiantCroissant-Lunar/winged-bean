# Winged Bean Project Overrides

**Project**: Winged Bean  
**Primary Stack**: .NET (multi-language: .NET, Node.js, Python, Unity)  
**Date**: 2025-10-13

## Project Purpose

Multi-language, plugin-based development platform with comprehensive CI/CD pipeline, infrastructure automation, and local development tools. Features AssemblyLoadContext-based plugin loading with hot-reload support for Console, Unity, and Godot profiles.

## Project-Specific Rules

### R-WINGEDBEAN-010: Multi-Language Architecture
- **What**: Support .NET, Node.js/TypeScript, Python, and Unity
- **Why**: Different components use different tech stacks
- **Structure**:
  ```
  development/
    dotnet/         # .NET plugin architecture & hosts
    nodejs/         # Node.js/TypeScript projects (PTY service, sites)
    python/         # Python utilities and tools
    unity/          # Unity plugin host (planned)
  ```

### R-WINGEDBEAN-020: Plugin Architecture (ALC-Based)
- **What**: AssemblyLoadContext (ALC) based plugin loading
- **Why**: Hot-reload support, isolation, multi-profile compatibility
- **Features**:
  - Dependency resolution with topological sort
  - Circular dependency detection
  - Profile-agnostic design (Console, Unity, Godot, Web)
  - Service integration with DI

### R-WINGEDBEAN-030: Framework Targeting Strategy
- **What**: Different framework targets for different tiers
- **Why**: Unity/Godot compatibility (netstandard2.1) vs modern features (net8.0)
- **Targets**:
  - Contracts: `.NET Standard 2.1` (Unity/Godot compatibility)
  - Infrastructure: `.NET Standard 2.1` (portability)
  - Console: `.NET 8.0` LTS (modern features)
  - Source Generators: `.NET Standard 2.0` (Roslyn compatibility)

### R-WINGEDBEAN-040: Artifact-First Build Policy ⭐ IMPORTANT
- **What**: ALWAYS build via Task system, run from versioned artifacts in `build/_artifacts/{version}/`
- **Why**: Consistent deployment, version tracking, reproducible builds
- **NEVER**: Run `dotnet build` or `dotnet run` directly from `development/` directories
- **ALWAYS**: Use Task commands from `build/` directory
- **Commands**:
  ```bash
  cd build
  
  # Build both console and desktop to versioned artifacts
  task build-dotnet
  
  # Build everything (dotnet, web, pty)
  task build-all
  
  # Run console from artifacts
  task console:normal
  task console:debug
  
  # Run with PM2 (for web UI + PTY + console)
  task pm2:start-artifacts
  task pm2:restart-artifacts
  task pm2:stop-artifacts
  
  # Sync from source Debug builds to artifacts (quick rebuild)
  task artifacts:sync-from-source
  ```
- **Artifact Locations**:
  - Console: `build/_artifacts/{version}/dotnet/console/bin/`
  - Desktop: `build/_artifacts/{version}/dotnet/desktop/bin/`
  - Latest: `build/_artifacts/latest/` (symlinked to current version)
- **Testing with Recording**:
  ```bash
  # Console with 30-second recording
  cd build/_artifacts/latest/dotnet/console/bin
  ./Dungeon.Host.Console --record --record-duration=30 --record-session=test-console
  
  # Desktop with 30-second recording (headless)
  cd build/_artifacts/latest/dotnet/desktop/bin
  ./Dungeon.Host.SadConsole --record --record-duration=30 --record-session=test-desktop
  ```

## Modified Base Rules

### R-DOTNET-STRUCT-010: Solution Organization (Modified)
```
winged-bean/
  development/
    dotnet/
      console/
        src/
          host/
            Dungeon.Host.Console/
          plugins/
            WingedBean.Plugins.*/
      windows/
        src/
          host/
            Dungeon.Host.SadConsole/
    nodejs/
      pty-service/
      sites/
    python/
      utilities/
    unity/
      (planned)
  infra/
    terraform/
      github/
      gcp/
      scripts/
    ansible/
    docker/
  docs/
    adr/
    design/
    development/
  build/
    nuke/
  .github/
    workflows/
```

### R-DOTNET-010: Namespace Conventions (Modified)
- **Pattern**: `WingedBean.{Component}.{Feature}`
- **Examples**:
  - `WingedBean.Plugins.DungeonGame`
  - `WingedBean.Plugins.SadConsole`
  - `Dungeon.Host.Console`

## Build System Rules

### R-WINGEDBEAN-BUILD-010: Lunar Build Integration
- **What**: Uses `giantcroissant-lunar-build` (similar to build-arcane)
- **Config**: `build-config.json` at root
- **Components**: ProjectReference to lunar-build components
- **Path**: `../../infra-projects/giantcroissant-lunar-build/`

### R-WINGEDBEAN-BUILD-020: Task-Based Workflow
- **What**: Task runner for build automation
- **Location**: `build/` directory
- **Commands**:
  - `task build-all` - Build all projects
  - `task console:normal` - Run console host
  - `task console:debug` - Run with debug UI

### R-WINGEDBEAN-BUILD-030: NUKE Build System
- **What**: NUKE build components for advanced builds
- **Location**: `build/nuke/`
- **Components**:
  - Configuration
  - CoreAbstractions
  - CodeQuality
  - Reporting (from giantcroissant-lunar-report)

## CI/CD Rules

### R-WINGEDBEAN-CI-010: MegaLinter Integration
- **What**: Comprehensive linting across all languages
- **Config**: `.mega-linter.yml`
- **Workflow**: `.github/workflows/`
- **Languages**: .NET, TypeScript, Python, Terraform, YAML

### R-WINGEDBEAN-CI-020: Act for Local Testing
- **What**: Test GitHub Actions workflows locally
- **Tool**: Act (via Docker)
- **Commands**:
  ```bash
  act -l              # List workflows
  act --dryrun        # Dry run
  act -j megalinter   # Run MegaLinter
  ```

### R-WINGEDBEAN-CI-030: Pre-commit Hooks
- **What**: Automated checks before commit
- **Config**: `.pre-commit-config.yaml`
- **Hooks**:
  - GitLeaks (secret detection)
  - Ruff (Python)
  - Terraform fmt/validate
  - dotnet format (via hook-validator)
  - YAML/JSON validation

## Hook Validator Integration

### R-WINGEDBEAN-HOOKVAL-010: .NET Formatting
- **What**: Uses hook-validator for .NET formatting
- **Path**: `../../plate-projects/hook-validator/`
- **Hook**: `format-dotnet` (dotnet format)
- **Files**: `\.cs$`

### R-WINGEDBEAN-HOOKVAL-020: Local Repository Reference
- **What**: References hook-validator from local path
- **Why**: Development/testing of hook-validator
- **Pattern**: `repo: local` in `.pre-commit-config.yaml`

## Security Rules

### R-WINGEDBEAN-SEC-010: SOPS Encryption
- **What**: Secure secret management with SOPS + age
- **Tools**: 
  - `New-AgeKeyPair.ps1` - Generate keys
  - `Encrypt-Secrets.ps1` - Encrypt secrets
  - `Apply-SecretsJson.ps1` - Apply to Terraform Cloud
- **Location**: `infra/terraform/scripts/`

### R-WINGEDBEAN-SEC-020: GitLeaks Scanning
- **What**: Secret detection in commits
- **Config**: `.gitleaks.toml`
- **Integration**: Pre-commit hook + CI/CD

### R-WINGEDBEAN-SEC-030: Secrets Baseline
- **What**: Baseline for detect-secrets
- **File**: `.secrets.baseline`
- **Purpose**: Filter known false positives

## Infrastructure Rules

### R-WINGEDBEAN-INFRA-010: Terraform Cloud
- **What**: Infrastructure as Code with Terraform Cloud
- **Organization**: `giantcroissant-lunar`
- **Workspaces**: 
  - `winged-bean-github` - GitHub management
  - `winged-bean-gcp` - GCP resources
- **Scripts**: PowerShell automation in `infra/terraform/scripts/`

### R-WINGEDBEAN-INFRA-020: Multi-Cloud Ready
- **What**: Support for multiple cloud providers
- **Providers**:
  - GitHub (repository management)
  - GCP (cloud resources)
  - (Future: AWS, Azure)

## Plugin System Rules

### R-WINGEDBEAN-PLUGIN-010: Manifest-Based Discovery
- **What**: JSON-based plugin metadata
- **Why**: Declarative plugin configuration
- **Fields**: Name, version, dependencies, entry point

### R-WINGEDBEAN-PLUGIN-020: Hot-Reload Support
- **What**: Reload plugins without restarting host
- **Why**: Faster development iteration
- **Implementation**: ALC unload + reload

### R-WINGEDBEAN-PLUGIN-030: Dependency Resolution
- **What**: Topological sort with circular detection
- **Why**: Correct plugin load order
- **Algorithm**: Kahn's algorithm for topological sort

### R-WINGEDBEAN-PLUGIN-040: Multi-Profile Support
- **What**: Plugins work across Console, Unity, Godot, Web
- **Why**: Write once, run anywhere
- **Tiers**:
  - Tier 1/2: netstandard2.1 (Unity/Godot)
  - Tier 3/4: net8.0 (Console/Web)

## Documentation Rules

### R-WINGEDBEAN-DOC-010: Architecture Decision Records (ADRs)
- **What**: Document significant architectural decisions
- **Location**: `docs/adr/`
- **Format**: Markdown with standard template
- **Examples**:
  - ADR-0001: Use Astro with Asciinema Player
  - ADR-0002: Use Native Tools for Pre-commit Hooks
  - ADR-0003: Implement Security and Quality Pre-commit Hooks
  - ADR-0004: Adopt Act for Local GitHub Actions Testing

### R-WINGEDBEAN-DOC-020: Development Guides
- **What**: Comprehensive development documentation
- **Location**: `docs/development/`
- **Topics**:
  - Linting configuration
  - Act usage
  - Advanced plugin features

### R-WINGEDBEAN-DOC-030: Design Documents
- **What**: Design documents for major features
- **Location**: `docs/design/`
- **Examples**:
  - Console MVP migration plan
  - Tier 1 core contracts

## Testing Strategy

### R-WINGEDBEAN-TEST-010: Unit Test Coverage
- **What**: >80% coverage target
- **Status**: In progress
- **Framework**: xUnit for .NET

### R-WINGEDBEAN-TEST-020: Local CI Testing
- **What**: Test CI/CD locally before push
- **Tool**: Act
- **Why**: Catch issues early, faster iteration

### R-WINGEDBEAN-TEST-030: URF Recording & Automated Testing
- **What**: Universal Recording Format (URF) for gameplay recording and testing
- **Framework**: `CrossMilo.Contracts.Recorder/URF/`
- **Status**: Steps 1-3 complete (50% progress)
- **Capabilities**:
  - Desktop app: URF recorder with F9/F10 hotkeys OR `--record` flag
  - Console app: URF recorder with `--record` flag
  - Asciinema export for standard playback
  - Golden recording comparison for regression detection
  - Automated test framework (32 tests passing)
- **Command-Line Recording**:
  ```bash
  # Console app (from artifacts)
  cd build/_artifacts/latest/dotnet/console/bin
  ./Dungeon.Host.Console --record --record-duration=30 --record-session=test-run
  
  # Desktop app (from artifacts, headless)
  cd build/_artifacts/latest/dotnet/desktop/bin
  ./Dungeon.Host.SadConsole --record --record-duration=30 --record-session=test-run
  ```
- **Recording Locations**:
  - URF files: `recordings/urf/*.urf.jsonl`
  - Asciinema export: `recordings/urf/*.cast`
- **Test Location**: `plate-projects/cross-milo/dotnet/tests/CrossMilo.Tests.URF/`
- **Documentation**: See `docs/URF-*.md` and `HANDOVER-2025-10-13-URF-Testing-Implementation.md`
- **Next Steps**: Test harness (Step 4), CI/CD integration (Step 5), Performance tracking (Step 6)

## Code Style Rules

### R-WINGEDBEAN-STYLE-010: .NET Conventions
- **What**: Microsoft C# coding conventions
- **Tool**: dotnet format (via hook-validator)
- **Config**: `.editorconfig`

### R-WINGEDBEAN-STYLE-020: TypeScript/Node.js
- **What**: ESLint + Prettier
- **Config**: ESLint configuration in nodejs projects

### R-WINGEDBEAN-STYLE-030: Python
- **What**: Ruff for linting and formatting
- **Config**: Ruff configuration in `.pre-commit-config.yaml`

### R-WINGEDBEAN-STYLE-040: Terraform
- **What**: `terraform fmt` for formatting
- **Integration**: Pre-commit hook

## Multi-Language Coordination

### R-WINGEDBEAN-MULTI-010: Language Isolation
- **What**: Each language in separate directory
- **Why**: Clear boundaries, independent tooling
- **Structure**: `development/{language}/`

### R-WINGEDBEAN-MULTI-020: Shared Infrastructure
- **What**: Common CI/CD, security, documentation
- **Why**: Consistent quality across languages
- **Location**: Root-level configs

### R-WINGEDBEAN-MULTI-030: Cross-Language Communication
- **What**: PTY service for Node.js ↔ .NET communication
- **Why**: Leverage strengths of each language
- **Example**: Node.js PTY service for terminal emulation

## Relationship to Plate Projects

### R-WINGEDBEAN-PLATE-010: Uses giantcroissant-lunar-build
- **What**: Similar to build-arcane but different implementation
- **Path**: `../../infra-projects/giantcroissant-lunar-build/`
- **Components**: Configuration, CoreAbstractions, CodeQuality

### R-WINGEDBEAN-PLATE-020: Uses hook-validator
- **What**: .NET formatting via hook-validator
- **Path**: `../../plate-projects/hook-validator/`
- **Hook**: format-dotnet

### R-WINGEDBEAN-PLATE-030: Now Uses rule-ground
- **What**: Centralized agent rules (just applied)
- **Stack**: .NET (primary)
- **Version**: v1.0.0

## Roadmap Awareness

### Current Phase: Plugin Architecture Foundation ✅
- [x] Core plugin interfaces
- [x] ALC-based plugin loader
- [x] Dependency resolution
- [x] Hot-reload support
- [x] Sample plugins
- [ ] Unit tests (>80% coverage) - In Progress

### Next Phase: Multi-Profile Support
- [ ] Unity profile with HybridCLR
- [ ] Godot profile implementation
- [ ] Web profile with ES modules
- [ ] Cross-profile plugin compatibility

### Future Enhancements
- [ ] Plugin marketplace
- [ ] Plugin signing and sandboxing
- [ ] Multi-version plugin support
- [ ] Ansible playbooks

## Known Patterns

### Pattern 1: Console Host with Plugins
```csharp
// Dungeon.Host.Console
// Loads plugins via ALC
// Runs game loop
// Supports hot-reload
```

### Pattern 2: Plugin Development
```csharp
// WingedBean.Plugins.DungeonGame
// Implements plugin interfaces
// Declares dependencies in manifest
// Targets netstandard2.1 or net8.0
```

### Pattern 3: Infrastructure Automation
```powershell
# Generate age keys
./infra/terraform/scripts/New-AgeKeyPair.ps1

# Encrypt secrets
./infra/terraform/scripts/Encrypt-Secrets.ps1 -Force

# Apply to Terraform Cloud
./infra/terraform/scripts/Apply-SecretsJson.ps1
```

## Special Considerations

### R-WINGEDBEAN-SPECIAL-010: Unity Compatibility
- **What**: Ensure plugins work in Unity
- **How**: Target netstandard2.1, avoid unsupported APIs
- **Test**: HybridCLR for IL2CPP compatibility

### R-WINGEDBEAN-SPECIAL-020: Godot Compatibility
- **What**: Ensure plugins work in Godot
- **How**: Target netstandard2.1, use Godot-compatible APIs
- **Test**: Godot 4.x with .NET support

### R-WINGEDBEAN-SPECIAL-030: Cross-Repository Dependencies
- **What**: References to infra-projects and plate-projects
- **Why**: Shared infrastructure and tooling
- **Pattern**: Relative paths `../../{repo}/`

## Notes

- This is a yokan-project (different from plate-projects)
- Multi-language architecture (not just .NET)
- Plugin-based with hot-reload
- Comprehensive CI/CD with MegaLinter + Act
- Security-first with SOPS, GitLeaks, pre-commit hooks
- Infrastructure as Code with Terraform Cloud
- Uses similar tools to plate-projects (lunar-build, hook-validator, rule-ground)
- Status: 🚧 In Development - Plugin Architecture Foundation phase

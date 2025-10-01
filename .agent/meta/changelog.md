# Agent Rules Changelog

All notable changes to the agent instruction base will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [1.0.0] - 2025-10-01

### Added
- Initial agent rules system adapted from pinto-bean reference project
- Base rules structure with versioning system
- Security rules (R-SEC-010 through R-SEC-040)
- Git workflow rules (R-GIT-010, R-GIT-020)
- Documentation conventions (R-DOC-010 through R-DOC-050)
- Code quality rules (R-CODE-010 through R-CODE-050)
- Testing guidelines (R-TST-010 through R-TST-030)
- Process rules (R-PRC-010 through R-PRC-040)
- Core principles (P-1 through P-7)
- Domain glossary for .NET/Unity/ECS terms
- Claude Code adapter
- GitHub Copilot adapter
- Windsurf adapter

### Design Decisions
- Focused on pragmatic rules suitable for active development phase
- Excluded heavy-handed thresholds (large change confirmations, dependency approval gates)
- Emphasized security, documentation standards, and code quality over process bureaucracy
- Tailored for .NET/C# and Unity development

### Notes
- Base version syncing enabled across all adapters
- Rule IDs are immutable once published
- Adapters reference rule IDs rather than duplicating text

## [Unreleased]

### Planned
- Performance rules when optimization phase begins
- CI/CD specific rules as pipeline matures
- Stricter testing requirements for production-ready code

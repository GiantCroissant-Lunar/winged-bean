# Development Guides

**Purpose**: How-to guides, best practices, and framework usage documentation

---

## Overview

This directory contains practical guides for developers working with the Winged Bean framework. These documents focus on "how to" rather than "why" or "what", providing concrete instructions and examples.

### Key Characteristics
- **Practical focus** - Step-by-step instructions
- **Maintained actively** - Updated as framework evolves
- **Example-driven** - Code samples and demonstrations
- **Developer-friendly** - Clear, accessible language

---

## Available Guides

### Framework & Architecture

- [Architecture Overview](./architecture-overview.md) - High-level architecture guide
- [Plugin Development Guide](./plugin-development-guide.md) - Complete guide for creating .NET plugins

#### Framework Targeting Guide
**File**: `framework-targeting-guide.md`

**Covers**:
- Target framework selection (.NET Standard 2.1, .NET 8.0)
- Unity and Godot compatibility
- Multi-platform considerations
- Best practices for framework targeting

**Use When**: Setting up new projects or updating framework versions

---

#### Source Generator Usage Guide
**File**: `source-generator-usage.md`

**Covers**:
- Creating source generators for Winged Bean
- .NET Standard 2.0 requirements
- Integration with build process
- Debugging source generators

**Use When**: Creating code generation tools or analyzers

---

#### Plugin Development Guide
**File**: `plugin-development-guide.md`

**Covers**:
- Creating new plugins from scratch
- Plugin manifest (.plugin.json) format
- Service registration patterns
- Plugin lifecycle and activation
- Host configuration (plugins.json)
- Best practices and troubleshooting

**Use When**: Creating new plugins or understanding the plugin system

---

#### Plugin Configuration Migration Guide
**File**: `plugin-configuration-migration-guide.md`

**Covers**:
- Migrating from static to dynamic plugin loading
- Troubleshooting common plugin loading issues
- Best practices for plugin configuration
- Priority and load strategy guidelines
- Testing plugin configurations

**Use When**: Migrating existing hosts to dynamic loading or troubleshooting plugin issues

---

## Guide Categories

### 🏗️ Framework & Architecture
Guides covering core framework concepts and architectural patterns

### 🎮 Unity Integration
Unity-specific guides and best practices
- See also: [Unity Documentation](../unity/)

### 🔧 Development Tools
Tool setup, configuration, and usage guides

### 🧪 Testing
Testing strategies, frameworks, and best practices

- [Playwright Asciinema Quickstart](./PLAYWRIGHT_ASCIINEMA_QUICKSTART.md) - Quick start guide for E2E testing

### 📦 Deployment
Build, packaging, and deployment guides

---

## Creating New Guides

### When to Create a Guide

Create a guide when:
1. **Common task** - Developers frequently need to perform this task
2. **Complex process** - Multiple steps or configuration required
3. **Best practices** - Document recommended approaches
4. **Tool usage** - Explain how to use specific tools or features
5. **Migration** - Help developers migrate to new patterns

---

### Guide Template

```markdown
# [Task/Topic] Guide

**Purpose**: Brief description of what this guide covers
**Audience**: Who should read this guide
**Prerequisites**: What you need before starting

---

## Overview

High-level explanation of the task or topic.

## Prerequisites

- Prerequisite 1
- Prerequisite 2

## Step-by-Step Instructions

### Step 1: [Action]

Description of what to do.

**Code Example**:
```csharp
// Example code
```

**Explanation**: What this code does and why.

---

### Step 2: [Action]

Continue with next step...

## Common Issues

### Issue 1: [Problem]
**Symptom**: How you know you have this problem
**Solution**: How to fix it

## Best Practices

1. **Practice 1**: Description
2. **Practice 2**: Description

## Related Documentation

- Link to related guides
- Link to relevant RFCs

---

**Last Updated**: YYYY-MM-DD
**Author**: [Name]
```

---

### Naming Conventions

Use **descriptive kebab-case** names that clearly indicate the guide's purpose:

**Good Examples**:
- ✅ `framework-targeting-guide.md`
- ✅ `unity-plugin-development-guide.md`
- ✅ `ecs-system-creation-guide.md`
- ✅ `testing-with-playwright-guide.md`

**Poor Examples**:
- ❌ `guide1.md`
- ❌ `how-to.md`
- ❌ `setup.md`

---

## Guide vs Other Docs

| Aspect | Guide | RFC | ADR | Design Doc |
|--------|-------|-----|-----|------------|
| **Purpose** | How to do X | Propose system change | Record decision | Explore options |
| **Focus** | Practical steps | Design & rationale | Context & choice | Analysis |
| **Audience** | Developers (hands-on) | Team (review) | Future devs (history) | Team (planning) |
| **Updates** | As framework evolves | During proposal | Never | During exploration |
| **Format** | Step-by-step | Structured proposal | Decision record | Freeform |

---

## Maintenance

### Regular Updates
- **Framework changes** - Update when APIs or patterns change
- **Tool updates** - Reflect new tool versions or features
- **Feedback** - Incorporate developer feedback and clarifications
- **Deprecations** - Mark outdated approaches and provide alternatives

### Deprecation
When a guide becomes outdated:
1. Add deprecation notice at the top
2. Link to updated guide or replacement
3. Move to `.archive/guides/` after 1 release cycle
4. Update index to remove from active list

---

## Contributing

### Adding a New Guide
1. Follow the template structure
2. Include practical code examples
3. Test all instructions
4. Add to this index
5. Link from related documentation

### Updating Existing Guides
1. Maintain version compatibility notes
2. Preserve examples that still work
3. Add migration notes when changing approaches
4. Update "Last Updated" date

---

## Related Documentation

- [RFCs](../rfcs/) - Architectural proposals
- [Unity Documentation](../unity/) - Unity-specific deep dives
- [Implementation Plans](../implementation/) - RFC execution details
- [Design Docs](../design/) - Technical exploration

---

**Last Updated**: 2025-10-02
**Total Guides**: 7
**Categories**: Framework & Architecture (4), Testing (1)

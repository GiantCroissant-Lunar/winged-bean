---
title: Existing WingedBean.Host Analysis
description: "Documentation for Existing WingedBean.Host Analysis"
---

# Existing WingedBean.Host Analysis

## Overview

This document analyzes the current `WingedBean.Host` implementation to understand what exists and how it maps to our new tiered architecture design.

## Current Structure

### Location
`/projects/dotnet/WingedBean.Host/`

### Dependencies
```xml
<PackageReference Include="Microsoft.Extensions.DependencyInjection" Version="8.0.0" />
<PackageReference Include="Microsoft.Extensions.Logging" Version="8.0.0" />
<PackageReference Include="Microsoft.Extensions.Hosting" Version="8.0.0" />
<PackageReference Include="System.Text.Json" Version="8.0.5" />
```

### Key Files

1. **IPluginLoader.cs** - Plugin loading interface
2. **IPluginActivator.cs** - Plugin activation interface
3. **ILoadedPlugin.cs** - Loaded plugin abstraction
4. **PluginManifest.cs** - Plugin metadata (JSON-based)
5. **PluginRegistry.cs** - Plugin registry (IPluginRegistry + FilePluginRegistry)
6. **PluginDiscovery.cs** - Plugin discovery logic
7. **PluginDependencyResolver.cs** - Dependency resolution
8. **PluginState.cs** - Plugin lifecycle states
9. **PluginSecurity.cs** - Plugin security/signing
10. **PluginUpdateManager.cs** - Plugin updates
11. **SemanticVersion.cs** - Semantic versioning
12. **HostBootstrap.cs** - Host bootstrapping

## Mapping to New Architecture

### What Exists (RFC-0003 Implementation)

The current `WingedBean.Host` is a **comprehensive implementation of RFC-0003** (Plugin Architecture). It includes:

#### ✅ Plugin Loading System
- `IPluginLoader` - Profile-agnostic plugin loading
- `ILoadedPlugin` - Loaded plugin abstraction
- `PluginState` - Lifecycle management

#### ✅ Plugin Metadata
- `PluginManifest` - Comprehensive JSON-based metadata
  - Entry points per profile (dotnet, nodejs, unity, godot)
  - Dependencies, exports, capabilities
  - Security settings, update info
  - Unity-specific settings (persist, packages, prefabs, etc.)

#### ✅ Plugin Registry
- `IPluginRegistry` - Registry interface
- `FilePluginRegistry` - File-based implementation
- Search, filtering, versioning
- Statistics and analytics

#### ✅ Advanced Features
- `PluginDiscovery` - Scan directories for `.plugin.json`
- `PluginDependencyResolver` - Topological sort
- `PluginSecurity` - Digital signatures, permissions
- `PluginUpdateManager` - Hot updates, rollback
- `SemanticVersion` - Full SemVer support

#### ✅ Host Bootstrap
- `HostBootstrap` - Orchestrates plugin lifecycle

### What's Missing (Tiered Architecture)

The current implementation is **plugin-focused but NOT service-oriented** as required by our tiered architecture discussions. Missing:

#### ❌ Service Registry (IRegistry)
- No `IRegistry` interface for **service registration and selection**
- The current `IPluginRegistry` is about **plugin metadata**, not service instances
- Need: Service registry with selection strategies (One, HighestPriority, All)

#### ❌ Tier 1 Contracts
- No separation of Tier 1 contracts
- No `IConfigService`, `IAudioService`, `IResourceService`
- No proxy services with `[RealizeService]` attributes

#### ❌ Source Code Generation
- No source generators for proxy services
- No `[RealizeService]` or `[SelectionStrategy]` attributes
- Need: `WingedBean.Contracts.SourceGen` project

#### ❌ Platform Providers (Tier 4)
- No `AssemblyLoadContext` provider for Console
- No `HybridCLR` provider for Unity
- Plugin loading is abstracted but not implemented per-platform

#### ❌ Tier 2 Registry
- No actual registry implementation that manages **service instances**
- Current `PluginRegistry` manages **plugin metadata**, not services

## Key Differences

### Current Architecture (RFC-0003)
```
Host Bootstrap
  ↓
Plugin Discovery (scan .plugin.json files)
  ↓
Plugin Dependency Resolution
  ↓
Plugin Loading (IPluginLoader, profile-specific)
  ↓
Plugin Activation (IPluginActivator)
  ↓
Service Registration (via DI container)
```

Focus: **Plugin-centric**. Everything is about loading plugins and their metadata.

### Target Architecture (RFC-0002 + RFC-0004)
```
Tier 1: Contracts (IRegistry, IConfigService, IAudioService, etc.)
  ↓
Tier 2: Proxy Services (source-gen) + ActualRegistry
  ↓
Tier 3: Plugin Implementations (ActualConfig, ActualAudio, etc.)
  ↓
Tier 4: Platform Providers (AssemblyLoadContext, HybridCLR)
```

Focus: **Service-oriented**. Plugins provide services; registry manages service selection.

## Integration Strategy

### What to Keep

1. **Plugin Manifest System** - Already comprehensive, supports multi-platform
2. **Plugin Discovery** - Works well, scan directories for `.plugin.json`
3. **Plugin Dependency Resolution** - Topological sort is solid
4. **Plugin Security** - Signing, permissions already implemented
5. **Semantic Versioning** - Full SemVer support
6. **Plugin Update Manager** - Hot updates, rollback

### What to Refactor

1. **IPluginRegistry → Keep for metadata**
   - Rename to `IPluginMetadataRegistry` for clarity?
   - Manages plugin manifests, discovery, search
   - Lives at Tier 3 (Console-specific implementation)

2. **Create IRegistry (NEW)**
   - Service instance registry (not metadata)
   - Selection strategies (One, HighestPriority, All)
   - Lives at Tier 1 (interface) and Tier 2 (implementation)

3. **IPluginLoader → Move to Tier 3**
   - Current: Tier 1-ish (in WingedBean.Host)
   - Target: Tier 3 (platform-aware orchestration)
   - Depends on Tier 4 providers (AssemblyLoadContext, HybridCLR)

4. **HostBootstrap → Tier 3**
   - Platform-specific bootstrap logic
   - Console: `ConsoleDungeon.Host`
   - Unity: `Unity.Host`

### What to Create

1. **Tier 1 Contracts** (NEW)
   - `WingedBean.Contracts.Core/` - IRegistry, IPlugin
   - `WingedBean.Contracts.Config/` - IConfigService + ProxyService
   - `WingedBean.Contracts.Audio/` - IAudioService + ProxyService
   - `WingedBean.Contracts.Resource/` - IResourceService + ProxyService

2. **Tier 2 Registry** (NEW)
   - `WingedBean.Registry/` - ActualRegistry implementation
   - Pure C#, platform-agnostic service instance management

3. **Tier 2 Source Gen** (NEW)
   - `WingedBean.Contracts.SourceGen/` - Roslyn generators
   - Generate proxy service methods
   - Generate selection logic

4. **Tier 3 Plugin Loader** (NEW)
   - `WingedBean.PluginLoader/` - Orchestrates loading (uses existing logic)
   - Wraps/refactors existing `IPluginLoader` implementation

5. **Tier 4 Providers** (NEW)
   - `WingedBean.Providers.AssemblyContext/` - Console provider
   - `WingedBean.Providers.HybridCLR/` - Unity provider

## Migration Path

### Phase 1: Create Tier 1 & 2 (Framework)
1. Create `framework/` directory
2. Implement Tier 1 contracts (IRegistry, IPluginLoader, IConfigService, etc.)
3. Implement Tier 2 Registry (ActualRegistry)
4. Create source generators (WingedBean.Contracts.SourceGen)
5. **Do NOT touch existing WingedBean.Host yet**

### Phase 2: Refactor WingedBean.Host
1. Move plugin-related logic to `console/src/`
2. Split concerns:
   - Plugin metadata registry → `WingedBean.Plugins.MetadataRegistry` (Tier 3)
   - Plugin loader → `WingedBean.PluginLoader` (Tier 3)
   - Bootstrap → `ConsoleDungeon.Host` (Tier 3)
3. Keep existing logic, just reorganize

### Phase 3: Implement Tier 3 Services
1. Create service plugins (Config, Audio, Resource)
2. Integrate with Tier 2 Registry
3. Test service registration and selection

### Phase 4: Create Tier 4 Providers
1. Implement `AssemblyContextProvider`
2. Integrate with `PluginLoader`
3. Test plugin loading via provider

### Phase 5: End-to-End Integration
1. Wire everything together
2. Bootstrap flow: Registry → PluginLoader → Config plugin → Other plugins
3. Test complete system

## Naming Clarification

### Current Confusion
- `IPluginRegistry` manages **plugin metadata** (manifests, versions, search)
- We need `IRegistry` for **service instances** (registration, selection)

### Proposed Naming
- **IPluginMetadataRegistry** - Plugin manifest management (current `IPluginRegistry`)
- **IRegistry** - Service instance management (NEW, foundational)
- **IPluginLoader** - Plugin loading orchestration (existing, move to Tier 3)

## Conclusion

The existing `WingedBean.Host` is a **solid RFC-0003 implementation** with comprehensive plugin features. However, it's **plugin-centric, not service-centric**.

To align with our tiered architecture:
1. **Keep** the plugin system (metadata, discovery, security, updates)
2. **Add** service registry system (IRegistry, selection strategies)
3. **Refactor** existing code into proper tiers
4. **Create** missing pieces (source gen, providers, Tier 1 contracts)

The existing code is valuable and will be integrated into the new structure, not thrown away.

---

**Status**: Analysis Complete
**Next Steps**: Begin Phase 1 - Create Tier 1 & 2 Framework
**Author**: Ray Wang (with Claude AI assistance)
**Date**: 2025-09-30

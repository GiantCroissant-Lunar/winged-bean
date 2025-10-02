---
title: WingedBean Architecture Overview
---

# WingedBean Architecture Overview

This document provides a comprehensive overview of the WingedBean architecture, including the 4-tier structure, framework targeting strategy, and plugin system.

## Table of Contents

- [Architecture Principles](#architecture-principles)
- [4-Tier Architecture](#4-tier-architecture)
- [Framework Targeting Strategy](#framework-targeting-strategy)
- [Plugin System Architecture](#plugin-system-architecture)
- [Service Registry Pattern](#service-registry-pattern)
- [Source Generator Architecture](#source-generator-architecture)

## Architecture Principles

WingedBean follows these core architectural principles:

1. **Separation of Concerns** - Clear boundaries between contracts, infrastructure, implementations, and providers
2. **Platform Agnostic Contracts** - Tier 1 contracts work across Unity, Godot, and .NET platforms
3. **Dependency Inversion** - High-level modules don't depend on low-level modules; both depend on abstractions
4. **Plugin Architecture** - All features loaded dynamically via plugin system
5. **Service Registry Pattern** - Centralized service resolution with priority-based selection

## 4-Tier Architecture

WingedBean uses a strict 4-tier architecture to ensure portability and maintainability:

```
┌─────────────────────────────────────────────────────────────────┐
│                    Tier 1: Contracts                             │
│                  (.NET Standard 2.1)                             │
│                                                                  │
│  Pure interfaces, no implementation, no platform dependencies    │
│  ┌──────────────────────────────────────────────────────────┐  │
│  │ IConfigService, IAudioService, IWebSocketService, etc.   │  │
│  └──────────────────────────────────────────────────────────┘  │
└─────────────────────────────────────────────────────────────────┘
                              ↑
                              │ implements & uses
                              ↓
┌─────────────────────────────────────────────────────────────────┐
│                 Tier 2: Infrastructure                           │
│                  (.NET Standard 2.1)                             │
│                                                                  │
│  Core framework logic (Registry, Plugin Loading, etc.)          │
│  ┌──────────────────────────────────────────────────────────┐  │
│  │ ActualRegistry, ServiceRegistry, PluginLoader           │  │
│  └──────────────────────────────────────────────────────────┘  │
└─────────────────────────────────────────────────────────────────┘
                              ↑
                              │ implements
                              ↓
┌─────────────────────────────────────────────────────────────────┐
│              Tier 3: Implementations (Plugins)                   │
│                       (.NET 8.0)                                 │
│                                                                  │
│  Platform-specific service implementations                       │
│  ┌──────────────────────────────────────────────────────────┐  │
│  │ ConfigService, SuperSocketWebSocketService,             │  │
│  │ TerminalGuiService, PtyService                          │  │
│  └──────────────────────────────────────────────────────────┘  │
└─────────────────────────────────────────────────────────────────┘
                              ↑
                              │ uses
                              ↓
┌─────────────────────────────────────────────────────────────────┐
│                 Tier 4: Providers                                │
│                       (.NET 8.0)                                 │
│                                                                  │
│  Low-level platform integration and system APIs                  │
│  ┌──────────────────────────────────────────────────────────┐  │
│  │ AssemblyLoadContext, File System, Network APIs          │  │
│  └──────────────────────────────────────────────────────────┘  │
└─────────────────────────────────────────────────────────────────┘
```

### Tier 1: Contracts

**Purpose:** Platform-agnostic service contracts

**Framework:** `.NET Standard 2.1`

**Examples:**
- `WingedBean.Contracts.Core` - Core service interfaces
- `WingedBean.Contracts.Config` - Configuration services
- `WingedBean.Contracts.Audio` - Audio playback services
- `WingedBean.Contracts.WebSocket` - WebSocket communication
- `WingedBean.Contracts.TerminalUI` - Terminal UI services
- `WingedBean.Contracts.Pty` - Pseudo-terminal services

**Guidelines:**
- ✅ Only interfaces, enums, and data classes
- ✅ No implementation logic
- ✅ No platform-specific APIs
- ❌ No System.Text.Json
- ❌ No IAsyncEnumerable<T>
- ❌ No Span<T> in public APIs

### Tier 2: Infrastructure

**Purpose:** Core framework components

**Framework:** `.NET Standard 2.1`

**Examples:**
- `WingedBean.Registry` - Service registry implementation

**Guidelines:**
- ✅ Framework-level abstractions
- ✅ Can depend on Tier 1 contracts
- ❌ No platform-specific code
- ❌ No UI or I/O operations

### Tier 3: Implementations

**Purpose:** Platform-specific service implementations

**Framework:** `.NET 8.0` (Console), Unity runtime (Unity), Godot runtime (Godot)

**Examples:**
- `WingedBean.Plugins.Config` - Configuration service
- `WingedBean.Plugins.WebSocket` - SuperSocket-based WebSocket
- `WingedBean.Plugins.TerminalUI` - Terminal.Gui integration
- `WingedBean.Plugins.PtyService` - Node-PTY service wrapper

**Guidelines:**
- ✅ Platform-specific implementations
- ✅ Can use modern C# features (in .NET 8.0)
- ✅ Can depend on Tier 1 and Tier 2
- ❌ Don't reference other Tier 3 plugins directly

### Tier 4: Providers

**Purpose:** Low-level platform integration

**Framework:** `.NET 8.0` (Console), Unity APIs (Unity), Godot APIs (Godot)

**Examples:**
- `WingedBean.Providers.AssemblyContext` - ALC management
- File system providers
- Network providers

**Guidelines:**
- ✅ Direct platform API usage
- ✅ Thin wrappers around system APIs
- ✅ Can depend on Tier 1, 2, and 3
- ❌ No business logic

## Framework Targeting Strategy

WingedBean uses a strategic framework targeting approach to ensure maximum compatibility:

```
┌────────────────────────────────────────────────────────────────┐
│                   Platform Compatibility                        │
├────────────────────────────────────────────────────────────────┤
│                                                                │
│  Unity 2021+  ←────┐                                          │
│                     │                                          │
│  Godot C#    ←──────┼─── .NET Standard 2.1 ───→ Tier 1 & 2   │
│                     │                                          │
│  .NET Core   ←────┘                                          │
│                                                                │
│  Modern .NET ←───── .NET 8.0 (LTS) ──────────→ Tier 3 & 4   │
│                                                                │
└────────────────────────────────────────────────────────────────┘
```

### Why .NET Standard 2.1?

- ✅ Unity 2021+ compatible (il2cpp and Mono)
- ✅ Godot 4.x C# support
- ✅ .NET Core 3.0+ compatible
- ✅ Maximum portability

### Why .NET 8.0?

- ✅ Long Term Support (LTS) until November 2026
- ✅ Modern C# 12 features
- ✅ Performance optimizations
- ✅ Production-ready stability

### Why .NET Standard 2.0 for Source Generators?

- ✅ Required by Roslyn SDK
- ✅ Works in all IDEs
- ✅ Compatible with all .NET SDK versions

## Plugin System Architecture

WingedBean's plugin system enables dynamic loading and hot-reload:

```
┌─────────────────────────────────────────────────────────────┐
│                        Host Application                      │
│                    (ConsoleDungeon.Host)                     │
│                                                              │
│  ┌───────────────────────────────────────────────────────┐ │
│  │              HostBootstrap                            │ │
│  │  1. Discover plugins (scan directories)              │ │
│  │  2. Resolve dependencies (topological sort)          │ │
│  │  3. Load plugins (via AlcPluginLoader)               │ │
│  │  4. Activate plugins (register services)             │ │
│  └───────────────────────────────────────────────────────┘ │
└─────────────────────────────────────────────────────────────┘
                            ↓
        ┌───────────────────┴───────────────────┐
        ↓                   ↓                   ↓
┌───────────────┐  ┌───────────────┐  ┌───────────────┐
│ Config Plugin │  │WebSocket Plugin│  │TermUI Plugin  │
│               │  │                │  │               │
│ ConfigService │  │SuperSocketSvc  │  │TerminalGuiSvc │
│               │  │                │  │               │
│ Priority: 100 │  │ Priority: 100  │  │ Priority: 100 │
└───────────────┘  └───────────────┘  └───────────────┘
        ↓                   ↓                   ↓
┌─────────────────────────────────────────────────────────────┐
│                    Service Registry                          │
│            (ActualRegistry implements IServiceRegistry)      │
│                                                              │
│  ┌────────────────────────────────────────────────────────┐ │
│  │ IConfigService     → ConfigService (priority: 100)     │ │
│  │ IWebSocketService  → SuperSocketService (priority: 100)│ │
│  │ ITerminalUIService → TerminalGuiService (priority: 100)│ │
│  └────────────────────────────────────────────────────────┘ │
└─────────────────────────────────────────────────────────────┘
```

### Plugin Lifecycle

1. **Discovery** - Scan directories for `.plugin.json` manifests
2. **Dependency Resolution** - Sort plugins by dependencies (topological)
3. **Loading** - Load assemblies into AssemblyLoadContext
4. **Activation** - Call `IPluginActivator.ActivateAsync()`
5. **Registration** - Plugins register services with the registry
6. **Ready** - Services available for consumption

### Plugin Manifest Format

```json
{
  "id": "wingedbean.plugins.config",
  "version": "1.0.0",
  "name": "Config Service",
  "description": "Configuration management service",
  
  "entryPoint": {
    "dotnet": "./WingedBean.Plugins.Config.dll",
    "unity": "./WingedBean.Plugins.Config.dll",
    "godot": "./WingedBean.Plugins.Config.dll"
  },
  
  "dependencies": {
    "wingedbean.contracts.core": "^1.0.0"
  },
  
  "exports": {
    "services": [
      {
        "interface": "IConfigService",
        "implementation": "ConfigService",
        "lifecycle": "singleton"
      }
    ]
  },
  
  "supportedProfiles": ["console", "unity", "godot"],
  "loadStrategy": "eager"
}
```

## Service Registry Pattern

The service registry is the central hub for service resolution:

```
┌─────────────────────────────────────────────────────────────┐
│                   Application Code                           │
│                                                              │
│  var config = registry.Get<IConfigService>();               │
│  var websocket = registry.Get<IWebSocketService>();         │
└─────────────────────────────────────────────────────────────┘
                            ↓
┌─────────────────────────────────────────────────────────────┐
│                  IServiceRegistry                            │
│                                                              │
│  + Register<T>(impl, priority)                              │
│  + Get<T>(selectionMode)                                    │
│  + GetAll<T>()                                              │
│  + IsRegistered<T>()                                        │
└─────────────────────────────────────────────────────────────┘
                            ↓
┌─────────────────────────────────────────────────────────────┐
│                  ActualRegistry                              │
│                                                              │
│  Dictionary<Type, List<(object impl, int priority)>>        │
│                                                              │
│  Selection Modes:                                           │
│  • One - Return any implementation                          │
│  • HighestPriority - Return highest priority impl           │
│  • All - Return all implementations                         │
└─────────────────────────────────────────────────────────────┘
```

### Priority System

Services can be registered with different priorities:

```csharp
// Register multiple implementations
registry.Register<ILogger>(new ConsoleLogger(), priority: 50);
registry.Register<ILogger>(new FileLogger(), priority: 100);  // Higher priority
registry.Register<ILogger>(new CloudLogger(), priority: 75);

// Get highest priority implementation
var logger = registry.Get<ILogger>(SelectionMode.HighestPriority);
// Returns FileLogger (priority: 100)

// Get all implementations
var allLoggers = registry.GetAll<ILogger>();
// Returns [FileLogger, CloudLogger, ConsoleLogger] (sorted by priority desc)
```

## Source Generator Architecture

WingedBean uses Roslyn source generators to eliminate boilerplate:

```
┌─────────────────────────────────────────────────────────────┐
│                   Compilation Time                           │
│                                                              │
│  Developer writes:                                           │
│  ┌────────────────────────────────────────────────────────┐ │
│  │ [RealizeService(typeof(IConfigService))]               │ │
│  │ public partial class ConfigProxy : IConfigService      │ │
│  │ {                                                      │ │
│  │     private readonly IServiceRegistry _registry;       │ │
│  │                                                        │ │
│  │     public ConfigProxy(IServiceRegistry registry)      │ │
│  │         => _registry = registry;                       │ │
│  │                                                        │ │
│  │     // No implementation needed!                       │ │
│  │ }                                                      │ │
│  └────────────────────────────────────────────────────────┘ │
└─────────────────────────────────────────────────────────────┘
                            ↓
┌─────────────────────────────────────────────────────────────┐
│         ProxyServiceGenerator (ISourceGenerator)             │
│                                                              │
│  1. Find classes with [RealizeService] attribute            │
│  2. Analyze service interface (IConfigService)              │
│  3. Generate proxy implementation:                          │
│     - Methods: delegate to registry.Get<T>()                │
│     - Properties: delegate get/set                          │
│     - Events: delegate add/remove                           │
│  4. Emit ConfigProxy.g.cs                                   │
└─────────────────────────────────────────────────────────────┘
                            ↓
┌─────────────────────────────────────────────────────────────┐
│                 Generated Code (ConfigProxy.g.cs)            │
│                                                              │
│  public partial class ConfigProxy                           │
│  {                                                          │
│      public string GetValue(string key)                     │
│      {                                                      │
│          var service = _registry.Get<IConfigService>(       │
│              SelectionMode.HighestPriority);                │
│          return service.GetValue(key);                      │
│      }                                                      │
│                                                             │
│      public void SetValue(string key, string value)         │
│      {                                                      │
│          var service = _registry.Get<IConfigService>(       │
│              SelectionMode.HighestPriority);                │
│          service.SetValue(key, value);                      │
│      }                                                      │
│  }                                                          │
└─────────────────────────────────────────────────────────────┘
```

### Benefits

- ✅ **Type-Safe** - Compiler verifies all interfaces
- ✅ **Zero Runtime Overhead** - Code generated at compile time
- ✅ **No Reflection** - Direct method calls
- ✅ **IntelliSense Support** - Full IDE integration
- ✅ **Maintainable** - Less boilerplate code

## Project Structure

```
development/dotnet/
├── framework/              # Tier 1 & 2 (netstandard2.1)
│   ├── src/
│   │   ├── WingedBean.Contracts.Core/         # Service interfaces
│   │   ├── WingedBean.Contracts.Config/       # Config interfaces
│   │   ├── WingedBean.Contracts.Audio/        # Audio interfaces
│   │   ├── WingedBean.Contracts.WebSocket/    # WebSocket interfaces
│   │   ├── WingedBean.Contracts.TerminalUI/   # Terminal UI interfaces
│   │   ├── WingedBean.Contracts.Pty/          # PTY interfaces
│   │   ├── WingedBean.Registry/               # Service registry
│   │   └── WingedBean.SourceGenerators.Proxy/ # Source generator (netstandard2.0)
│   └── tests/
│       ├── WingedBean.Registry.Tests/
│       └── WingedBean.SourceGenerators.Proxy.Tests/
│
├── console/                # Tier 3 & 4 (net8.0)
│   ├── src/
│   │   ├── host/
│   │   │   └── ConsoleDungeon.Host/           # Main application
│   │   ├── plugins/
│   │   │   ├── WingedBean.Plugins.Config/     # Config implementation
│   │   │   ├── WingedBean.Plugins.WebSocket/  # WebSocket implementation
│   │   │   ├── WingedBean.Plugins.TerminalUI/ # Terminal UI implementation
│   │   │   └── WingedBean.Plugins.PtyService/ # PTY implementation
│   │   ├── providers/
│   │   │   └── WingedBean.Providers.AssemblyContext/  # ALC management
│   │   └── shared/
│   │       └── WingedBean.PluginLoader/       # Plugin loading logic
│   └── tests/
│       ├── plugins/
│       ├── providers/
│       └── shared/
│
└── WingedBean.sln          # Solution file (all projects)
```

## Build Process

```bash
# Clean build
dotnet clean

# Restore dependencies
dotnet restore

# Build all projects
dotnet build WingedBean.sln --configuration Release

# Run tests
dotnet test WingedBean.sln --configuration Release

# Run application
cd console/src/host/ConsoleDungeon.Host
dotnet run --configuration Release
```

## Related Documentation

- [Framework Targeting Guide](./framework-targeting-guide.md)
- [Source Generator Usage Guide](./source-generator-usage.md)
- [RFC-0002: 4-Tier Architecture](../rfcs/0002-service-platform-core-4-tier-architecture.md)
- [RFC-0003: Plugin Architecture](../rfcs/0003-plugin-architecture-foundation.md)
- [RFC-0005: Framework Compliance](../rfcs/0005-target-framework-compliance.md)

## Summary

WingedBean's architecture provides:

1. **Multi-Platform Support** - Unity, Godot, and .NET compatibility via netstandard2.1
2. **Plugin System** - Dynamic loading with hot-reload support
3. **Service Registry** - Priority-based service resolution
4. **Source Generators** - Eliminate boilerplate with type-safe code generation
5. **4-Tier Structure** - Clear separation of concerns and dependency rules
6. **Modern .NET** - LTS stability with .NET 8.0 for implementations

This architecture enables building complex, multi-platform applications while maintaining clean code and excellent developer experience.

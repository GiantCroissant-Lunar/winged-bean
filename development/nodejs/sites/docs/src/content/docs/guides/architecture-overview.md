---
title: WingedBean Architecture Overview
description: "Documentation for WingedBean Architecture Overview"
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

WingedBean's plugin system enables dynamic loading and hot-reload through a configuration-driven approach.

### Dynamic Loading Architecture

The plugin system uses a multi-stage loading process coordinated by the host application:

```
┌────────────────────────────────────────────────────────────────────┐
│                      Host Application (Program.cs)                  │
│                                                                     │
│  [1] Foundation Services                                           │
│  ┌──────────────────────────────────────────────────────────────┐ │
│  │ ActualRegistry (IRegistry)                                    │ │
│  │ AssemblyContextProvider (IALCProvider)                        │ │
│  │ ActualPluginLoader (IPluginLoader)                            │ │
│  └──────────────────────────────────────────────────────────────┘ │
│                                                                     │
│  [2] Configuration Loading                                         │
│  ┌──────────────────────────────────────────────────────────────┐ │
│  │ Load plugins.json                                             │ │
│  │   → Parse PluginConfiguration                                 │ │
│  │   → Filter enabled plugins                                    │ │
│  │   → Sort by priority (high → low)                            │ │
│  └──────────────────────────────────────────────────────────────┘ │
│                         ↓                                          │
│  [3] Plugin Loading Loop (for each enabled plugin)                │
│  ┌──────────────────────────────────────────────────────────────┐ │
│  │ Check LoadStrategy (Eager/Lazy/Explicit)                      │ │
│  │   → If Eager: Load immediately                               │ │
│  │   → If Lazy: Skip (load on first use)                        │ │
│  │   → If Explicit: Skip (load on demand)                       │ │
│  │                                                               │ │
│  │ ActualPluginLoader.LoadAsync(path)                            │ │
│  │   → Create isolated AssemblyLoadContext                       │ │
│  │   → Load plugin assembly + dependencies                       │ │
│  │   → Discover types with [Plugin] attribute                    │ │
│  │   → Create ILoadedPlugin wrapper                             │ │
│  │                                                               │ │
│  │ Activate Plugin (if implements IPlugin)                       │ │
│  │   → Call OnActivateAsync(registry)                           │ │
│  │   → Plugin performs initialization                            │ │
│  │                                                               │ │
│  │ Register Services                                             │ │
│  │   → Discover service types from plugin                        │ │
│  │   → Find contract interfaces (WingedBean.Contracts.*)        │ │
│  │   → Register: registry.Register<TContract>(impl, priority)   │ │
│  └──────────────────────────────────────────────────────────────┘ │
│                         ↓                                          │
│  [4] Service Verification                                          │
│  ┌──────────────────────────────────────────────────────────────┐ │
│  │ Verify required services registered                           │ │
│  │   → IConfigService                                            │ │
│  │   → IWebSocketService                                         │ │
│  │   → ITerminalUIService                                        │ │
│  │   → Fail fast if critical service missing                    │ │
│  └──────────────────────────────────────────────────────────────┘ │
│                         ↓                                          │
│  [5] Application Launch                                            │
│  ┌──────────────────────────────────────────────────────────────┐ │
│  │ Start main application logic                                  │ │
│  │ Services available via registry.Get<TService>()               │ │
│  └──────────────────────────────────────────────────────────────┘ │
└────────────────────────────────────────────────────────────────────┘

         Plugin Files on Disk                    Loaded Plugins in Memory
         ═══════════════════════                 ═══════════════════════════
    
    plugins/                                    AssemblyLoadContext 1
    ├── Config.dll                              ┌─────────────────────────┐
    ├── Config.pdb                              │  ConfigService          │
    ├── WebSocket.dll                           │  Priority: 1000         │
    ├── WebSocket.pdb                           │  Implements:            │
    ├── TerminalUI.dll                          │  - IConfigService       │
    ├── TerminalUI.pdb                          └─────────────────────────┘
    └── [dependencies...]                       
                                                AssemblyLoadContext 2
                ↓                               ┌─────────────────────────┐
                                                │  WebSocketService       │
         ActualPluginLoader                     │  Priority: 100          │
         ═════════════════                      │  Implements:            │
                                                │  - IWebSocketService    │
    ILoadedPlugin LoadAsync(path)               └─────────────────────────┘
              ↓                                 
    1. Create ALC                               AssemblyLoadContext 3
    2. Load assembly                            ┌─────────────────────────┐
    3. Scan for [Plugin]                        │  TerminalUIService      │
    4. Wrap as ILoadedPlugin                    │  Priority: 100          │
                                                │  Implements:            │
                                                │  - ITerminalUIService   │
                ↓                               └─────────────────────────┘
                                                          ↓
         Service Registry                       All services registered
         ═══════════════                        ═══════════════════════
                                                
    IRegistry.Register<T>(impl, priority)       registry.Get<IConfigService>()
              ↓                                          ↓
    Dictionary<Type, List<(impl, priority)>>    Returns highest priority impl
              ↓
    SelectionMode:
    • One             → Any implementation
    • HighestPriority → Highest priority impl
    • All             → All implementations
```

### Plugin Lifecycle States

Plugins transition through well-defined states during their lifetime:

```
Discovered → Loading → Loaded → Activating → Activated → Ready
                ↓                               ↓           ↓
              Failed ← ─────────────────────────┴───────────┘
                                                    ↓
                                            Deactivating → Deactivated
                                                    ↓
                                             Unloading → Unloaded
```

#### State Descriptions

1. **Discovered**: Plugin entry found in `plugins.json`, file path validated
   - Host has discovered the plugin but not yet loaded it
   - Plugin metadata (id, priority, loadStrategy) available
   - No assembly loaded yet

2. **Loading**: Assembly being loaded into AssemblyLoadContext
   - Assembly file being read from disk
   - Dependencies being resolved
   - Type scanning in progress
   - Can fail here if DLL is corrupted or dependencies missing

3. **Loaded**: Assembly loaded, types discovered
   - Assembly successfully loaded into isolated context
   - Types with [Plugin] attribute identified
   - ILoadedPlugin wrapper created
   - Not yet activated

4. **Activating**: Calling plugin lifecycle hooks
   - If plugin implements IPlugin, calling OnActivateAsync()
   - Plugin performing initialization
   - May connect to external services, validate config
   - Can fail here if initialization fails

5. **Activated**: Plugin initialized successfully
   - OnActivateAsync() completed without errors
   - Plugin ready for service registration
   - Internal state initialized

6. **Ready**: Services registered with registry
   - All services from plugin registered with IRegistry
   - Services available for consumption via registry.Get<T>()
   - Plugin fully operational

7. **Failed**: Plugin failed to load or activate
   - Error occurred during Loading, Activating, or service registration
   - Plugin not available for use
   - Error logged with details
   - Other plugins continue loading (unless critical priority >= 1000)

8. **Deactivating**: Calling cleanup hooks
   - OnDeactivateAsync() being called
   - Plugin closing connections, releasing resources
   - Saving state if needed

9. **Deactivated**: Cleanup complete
   - Resources released
   - Services unregistered
   - Ready for unload

10. **Unloading**: Assembly being unloaded from ALC
    - AssemblyLoadContext.Unload() called
    - Waiting for GC to collect plugin assemblies

11. **Unloaded**: Assembly unloaded from memory
    - ALC collected by GC
    - Memory freed
    - Plugin fully removed

### Plugin Lifecycle Hooks

Plugins can implement the `IPlugin` interface to receive lifecycle notifications:

```csharp
public interface IPlugin
{
    string Id { get; }
    string Version { get; }
    
    Task OnActivateAsync(IRegistry registry, CancellationToken cancellationToken = default);
    Task OnDeactivateAsync(CancellationToken cancellationToken = default);
}
```

#### OnActivateAsync Hook

Called after assembly is loaded but before services are registered. Use for:

- **Initialization**: Set up internal state, create resources
- **Validation**: Verify configuration, check dependencies
- **Connection**: Connect to external services (databases, APIs)
- **Registration**: Register event handlers, subscribe to events

**Example:**
```csharp
public async Task OnActivateAsync(IRegistry registry, CancellationToken cancellationToken)
{
    Console.WriteLine($"[{Id}] Activating plugin...");
    
    // Validate configuration
    var config = registry.Get<IConfigService>();
    if (!ValidateConfiguration(config))
        throw new InvalidOperationException("Invalid configuration");
    
    // Initialize resources
    await InitializeDatabaseConnectionAsync(cancellationToken);
    
    // Register handlers
    registry.Get<IEventBus>()?.Subscribe("app.shutdown", OnShutdown);
    
    Console.WriteLine($"[{Id}] Plugin activated");
}
```

#### OnDeactivateAsync Hook

Called before plugin is unloaded. Use for:

- **Cleanup**: Release resources, close connections
- **Persistence**: Save state, flush buffers
- **Unregistration**: Remove event handlers, unsubscribe
- **Graceful Shutdown**: Finish pending work, notify dependents

**Example:**
```csharp
public async Task OnDeactivateAsync(CancellationToken cancellationToken)
{
    Console.WriteLine($"[{Id}] Deactivating plugin...");
    
    // Unregister handlers
    registry.Get<IEventBus>()?.Unsubscribe("app.shutdown", OnShutdown);
    
    // Save state
    await SaveStateAsync(cancellationToken);
    
    // Close connections
    await _databaseConnection?.DisposeAsync();
    
    Console.WriteLine($"[{Id}] Plugin deactivated");
}
```

### Configuration-Driven Loading

The plugin system is driven by `plugins.json`, which controls all aspects of plugin loading:

#### plugins.json Structure

```json
{
  "version": "1.0",
  "pluginDirectory": "plugins",
  "plugins": [
    {
      "id": "wingedbean.plugins.config",
      "path": "plugins/WingedBean.Plugins.Config.dll",
      "priority": 1000,
      "loadStrategy": "Eager",
      "enabled": true,
      "metadata": {
        "description": "Configuration service",
        "author": "WingedBean",
        "version": "1.0.0"
      }
    }
  ]
}
```

#### Configuration Schema

##### Root Configuration (PluginConfiguration)

| Field | Type | Required | Default | Description |
|-------|------|----------|---------|-------------|
| `version` | string | ✅ | - | Configuration format version (currently "1.0") |
| `pluginDirectory` | string | ✅ | "plugins" | Base directory for plugin DLLs (relative to host) |
| `plugins` | PluginDescriptor[] | ✅ | [] | Array of plugin descriptors |

##### Plugin Descriptor (PluginDescriptor)

| Field | Type | Required | Default | Description |
|-------|------|----------|---------|-------------|
| `id` | string | ✅ | - | Unique plugin identifier (lowercase, dot-separated) |
| `path` | string | ✅ | - | Path to plugin DLL (relative to pluginDirectory) |
| `priority` | number | ⬜ | 0 | Loading priority (0-1000+, higher loads first) |
| `loadStrategy` | LoadStrategy | ⬜ | Eager | When to load: Eager, Lazy, or Explicit |
| `enabled` | boolean | ⬜ | true | Whether plugin is enabled (false = skip loading) |
| `metadata` | object | ⬜ | null | Custom metadata (description, author, version, etc.) |
| `dependencies` | string[] | ⬜ | [] | Required plugin IDs (for dependency resolution) |

##### Load Strategy (LoadStrategy enum)

```csharp
public enum LoadStrategy
{
    Eager,    // Load immediately at application startup
    Lazy,     // Load on first use (when service is first requested)
    Explicit  // Load only when explicitly requested via API
}
```

| Strategy | When Loaded | Use Cases | Example Plugins |
|----------|-------------|-----------|-----------------|
| **Eager** | Startup (in Program.cs) | Critical services, infrastructure | Config, Registry, WebSocket |
| **Lazy** | First service request | Optional features, heavy plugins | Recording, Analytics, ECS |
| **Explicit** | Manual LoadAsync() call | Dev tools, admin plugins | Debugger, Profiler, Test Harness |

#### Priority System

Plugins are loaded in priority order (highest to lowest). Priority also determines service selection when multiple plugins provide the same contract:

| Range | Purpose | Criticality | Example Plugins |
|-------|---------|-------------|-----------------|
| **1000+** | Critical infrastructure | App won't start without these | Configuration, Logging, Registry |
| **500-999** | Core services | Essential for main functionality | Database, Authentication, Authorization |
| **100-499** | Standard services | Important features | WebSocket, Terminal UI, PTY |
| **50-99** | Optional features | Nice-to-have functionality | Recording, Metrics, Notifications |
| **0-49** | Low priority | Experimental, dev-only | Debug tools, Test plugins |

**Critical Plugin Behavior**: Plugins with priority >= 1000 are considered critical. If they fail to load, the host application aborts startup with an error message.

### Plugin Manifest Format (.plugin.json)

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

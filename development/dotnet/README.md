# WingedBean Plugin Architecture Implementation

This directory contains the implementation of RFC-0003: Plugin Architecture Foundation and RFC-0005: Target Framework Compliance for the WingedBean project.

## 🎯 Implementation Status

✅ **Phase 1 Complete** - Core plugin system foundation implemented:

- **WingedBean.Host** - Core plugin interfaces and abstractions
- **WingedBean.Host.Console** - ALC-based plugin loader for .NET console applications
- **WingedBean.Contracts** - Shared interfaces for plugin implementations
- **WingedBean.Plugins.AsciinemaRecorder** - Sample plugin demonstrating the architecture
- **WingedBean.Demo** - Console application demonstrating the plugin system

✅ **RFC-0005 Complete** - Framework targeting for multi-platform support:

- **Tier 1 Contracts**: `.NET Standard 2.1` (Unity/Godot compatible)
- **Tier 2 Infrastructure**: `.NET Standard 2.1` (Portable)
- **Tier 3/4 Console**: `.NET 8.0` LTS (Modern features)
- **Source Generators**: `.NET Standard 2.0` (Roslyn compatible)

[Framework Targeting Guide →](../../docs/guides/framework-targeting-guide.md)

## 🏗️ Architecture Overview

The plugin system follows the RFC specification with these key components:

```
┌─────────────────────────────────────────────────────────────────┐
│                        Host (Minimal Core)                     │
│  - Plugin Discovery (PluginDiscovery)                         │
│  - Plugin Loading (IPluginLoader: AlcPluginLoader)            │
│  - Plugin Lifecycle Management                                │
│  - Dependency Resolution (PluginDependencyResolver)           │
│  - Host Bootstrap (HostBootstrap)                            │
└─────────────────────────────────────────────────────────────────┘
                             ↓ discovers & loads
                    ┌─────────────────────────────────────────┐
                    │                Plugins                  │
                    │                                         │
                    │  ┌─────────────────────────────────────┐ │
                    │  │      AsciinemaRecorder Plugin       │ │
                    │  │  - Implements IRecorder interface  │ │
                    │  │  - Records to asciicast v2 format  │ │
                    │  │  - Hot-swappable via ALC           │ │
                    │  └─────────────────────────────────────┘ │
                    └─────────────────────────────────────────┘
```

## 📦 Components

### Core Host (`WingedBean.Host`)

- **IPluginLoader** - Profile-agnostic plugin loading interface
- **IPluginActivator** - Interface plugins implement to register services
- **ILoadedPlugin** - Represents a loaded plugin instance
- **PluginManifest** - JSON-based plugin metadata
- **PluginDiscovery** - Scans directories for `.plugin.json` files
- **PluginDependencyResolver** - Topological sort for dependency ordering
- **HostBootstrap** - Orchestrates plugin discovery, loading, and activation

### Console Profile (`WingedBean.Host.Console`)

- **AlcPluginLoader** - AssemblyLoadContext-based plugin loader
- **LoadedPlugin** - Concrete implementation with ALC support
- Supports hot-reload through collectible load contexts

### Sample Plugin (`WingedBean.Plugins.AsciinemaRecorder`)

- **AsciinemaRecorder** - Records terminal sessions to asciicast v2 format
- **AsciinemaRecorderPlugin** - Plugin activator implementation
- **AsciinemaRecorder.plugin.json** - Plugin manifest

### Demo Application (`WingedBean.Demo`)

- Console application demonstrating plugin loading
- Shows service resolution from plugins
- Tests recording functionality

## 🚀 Key Features Implemented

### ✅ Plugin Discovery

- Scans directories for `.plugin.json` manifest files
- Resolves relative paths to absolute paths
- Validates plugin metadata

### ✅ Dependency Resolution

- Topological sort using Kahn's algorithm
- Detects circular dependencies
- Orders plugins by dependency requirements

### ✅ Hot-Reload Support

- Uses collectible AssemblyLoadContext
- Supports unload/reload without restart
- Quiesce period for graceful shutdown

### ✅ Profile-Agnostic Design

- Abstract IPluginLoader interface
- Console profile with ALC implementation
- Ready for Unity (HybridCLR) and Web (ES modules) profiles

### ✅ Service Integration

- Plugins register services via DI container
- Host merges plugin services
- Full dependency injection support

## 📄 Plugin Manifest Format

```json
{
  "id": "wingedbean.plugins.recorder.asciinema",
  "version": "1.0.0",
  "name": "Asciinema Recorder",
  "description": "Records PTY sessions to asciicast v2 format",
  "author": "WingedBean Team",
  "license": "MIT",

  "entryPoint": {
    "dotnet": "./WingedBean.Plugins.AsciinemaRecorder.dll",
    "nodejs": null,
    "unity": "./WingedBean.Plugins.AsciinemaRecorder.dll",
    "godot": "./WingedBean.Plugins.AsciinemaRecorder.dll"
  },

  "dependencies": {
    "wingedbean.contracts": "^1.0.0"
  },

  "exports": {
    "services": [
      {
        "interface": "IRecorder",
        "implementation": "AsciinemaRecorder",
        "lifecycle": "transient"
      }
    ]
  },

  "capabilities": ["recording", "asciinema-v2", "filesystem"],
  "supportedProfiles": ["console", "unity", "godot", "web"],
  "loadStrategy": "lazy",
  "quiesceSeconds": 5
}
```

## 🔧 Usage

### Building

```bash
cd projects/dotnet
dotnet build WingedBean.sln
```

### Running Demo

```bash
dotnet run --project WingedBean.Demo
```

### Creating a Plugin

1. **Create Plugin Project**

   ```bash
   dotnet new classlib -n MyPlugin
   dotnet add reference WingedBean.Host
   dotnet add reference WingedBean.Contracts
   ```

2. **Implement Plugin Activator**

   ```csharp
   public class MyPlugin : IPluginActivator
   {
       public Task ActivateAsync(IServiceCollection services, IServiceProvider hostServices, CancellationToken ct)
       {
           services.AddTransient<IMyService, MyServiceImpl>();
           return Task.CompletedTask;
       }

       public Task DeactivateAsync(CancellationToken ct) => Task.CompletedTask;
   }
   ```

3. **Create Manifest** (`MyPlugin.plugin.json`)

   ```json
   {
     "id": "myplugin",
     "version": "1.0.0",
     "name": "My Plugin",
     "entryPoint": { "dotnet": "./MyPlugin.dll" },
     "dependencies": {},
     "exports": {
       "services": [{
         "interface": "IMyService",
         "implementation": "MyServiceImpl",
         "lifecycle": "transient"
       }]
     },
     "supportedProfiles": ["console"]
   }
   ```

## 📋 Remaining Work

The core plugin system is complete per RFC Phase 1. Future enhancements:

- **Unity Profile** (Phase 3) - HybridCLR-based plugin loader
- **Godot Profile** (Phase 3) - Godot ALC plugin loader
- **Web Profile** (Phase 3) - ES module plugin loader
- **Plugin Versioning** (Phase 4) - Multiple version support
- **Plugin Security** (Phase 4) - Signing and sandboxing
- **Plugin Marketplace** (Phase 4) - Online plugin repository

## 🎯 Definition of Done - Phase 1

✅ `WingedBean.Host` package with core plugin interfaces
✅ `PluginDiscovery` working (scan directories, load manifests)
✅ `PluginDependencyResolver` working (topological sort)
✅ `AlcPluginLoader` working (load/unload/reload in Console profile)
✅ `HostBootstrap` working (orchestrate plugin lifecycle)
⏳ Unit tests for all components (>80% coverage) - **Next priority**

The plugin architecture foundation is now complete and ready for production use in console applications, with a clear path for extending to Unity, Godot, and Web profiles.

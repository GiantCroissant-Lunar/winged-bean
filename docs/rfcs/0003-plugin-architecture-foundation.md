# RFC-0003: Plugin Architecture Foundation

## Status

Draft

## Date

2025-09-30

## Summary

Establish a **plugin-based architecture** where **everything** (including core services, adapters, providers) is loaded as a plugin. The platform itself is just a minimal host that discovers, loads, and coordinates plugins. All assemblies/modules are loaded dynamically at startup or on-demand via a unified plugin loader supporting multiple load contexts (ALC for .NET/Godot, HybridCLR for Unity, ES modules for Node.js).

**Key Principle**: If it's not the host, it's a plugin. Even Tier-1 contracts can be provided by plugins.

## Motivation

### Vision

Build a **pure plugin platform** where:
1. **Host** (minimal): Plugin discovery, loading, lifecycle management, DI container
2. **Everything else is a plugin**: Services, adapters, providers, UI, even contracts
3. **Hot-swappable**: Load/unload/reload plugins at runtime without restart
4. **Profile-agnostic**: Same plugin system works on Console (.NET ALC), Unity (HybridCLR), Godot (ALC), Web (ES modules)

### Problems to Solve

1. **Modularity**: Current codebase is monolithic; adding a service requires recompiling the entire app
2. **Hot-Swap**: No runtime plugin replacement; Unity/Godot workflows require restart
3. **Profile-Specific Loading**: .NET uses ALC, Unity uses HybridCLR, Node.js uses ES modules → need unified abstraction
4. **Dependency Management**: Plugins have dependencies on other plugins; need dependency resolution
5. **Versioning**: Multiple versions of the same plugin must coexist (for gradual migration)
6. **Security**: Sandboxing, permissions, signed plugins

### Current Architecture Issues

**Console Profile:**
- All code compiled into one executable
- No hot-reload (must restart app)
- Tight coupling between PTY service, Terminal.Gui app, recording

**Unity/Godot (Future):**
- Unity: Limited hot-reload without HybridCLR
- Godot: Some hot-reload, but not for all C# code
- Both require restart for major changes

**Without a plugin architecture:**
- Cannot distribute features independently
- Cannot hot-swap providers at runtime
- Cannot support third-party extensions

## Proposal

### Plugin Architecture Overview

```
┌────────────────────────────────────────────────────────────────────────────┐
│                          Host (Minimal Core)                               │
│  - Plugin Discovery (scan directories, manifests)                          │
│  - Plugin Loading (IPluginLoader: ALC / HybridCLR / ES modules)            │
│  - Plugin Lifecycle (Load → Activate → Deactivate → Unload)                │
│  - DI Container (register plugin services)                                 │
│  - Event Bus (inter-plugin communication)                                  │
│  - Plugin Registry (metadata, dependencies, versions)                      │
└────────────────────────────────────────────────────────────────────────────┘
                                      ↓ discovers & loads
        ┌─────────────────────────────────────────────────────────────────┐
        │                         Plugins                                 │
        │                                                                 │
        │  ┌──────────────┐  ┌───────────────┐  ┌──────────────────────┐ │
        │  │  Contracts   │  │   Adapters    │  │     Providers        │ │
        │  │   Plugin     │  │    Plugin     │  │      Plugin          │ │
        │  │              │  │               │  │                      │ │
        │  │ - IPtyService│  │ - Resilience  │  │ - NodePtyProvider    │ │
        │  │ - IRecorder  │  │ - LoadContext │  │ - AsciinemaRecorder  │ │
        │  │ - ITransport │  │ - Telemetry   │  │ - WebSocketTransport │ │
        │  └──────────────┘  └───────────────┘  └──────────────────────┘ │
        │                                                                 │
        │  ┌──────────────┐  ┌───────────────┐  ┌──────────────────────┐ │
        │  │  Façades     │  │   Terminal    │  │   Recording          │ │
        │  │   Plugin     │  │     Plugin    │  │    Strategy          │ │
        │  │              │  │               │  │     Plugin           │ │
        │  │ - Generated  │  │ - Terminal.Gui│  │ - Asciinema v2       │ │
        │  │   Façades    │  │ - Spectre     │  │ - Custom JSON        │ │
        │  └──────────────┘  └───────────────┘  └──────────────────────┘ │
        └─────────────────────────────────────────────────────────────────┘
```

### Plugin Manifest

Every plugin has a manifest (JSON) describing metadata, dependencies, exports.

**Example: `NodePtyProvider.plugin.json`**

```json
{
  "id": "wingedbean.providers.pty.node",
  "version": "1.0.0",
  "name": "Node.js PTY Provider",
  "description": "PTY implementation using node-pty library",
  "author": "WingedBean Team",
  "license": "MIT",

  "entryPoint": {
    "dotnet": null,
    "nodejs": "./dist/index.js",
    "unity": null,
    "godot": null
  },

  "dependencies": {
    "wingedbean.contracts": "^1.0.0",
    "node-pty": "^0.10.0"
  },

  "exports": {
    "services": [
      {
        "interface": "IPtyService",
        "implementation": "NodePtyProvider",
        "lifecycle": "singleton"
      }
    ]
  },

  "capabilities": [
    "pty",
    "terminal",
    "unix"
  ],

  "supportedProfiles": ["console", "web"],

  "loadStrategy": "eager"
}
```

**Example: `AsciinemaRecorder.plugin.json`**

```json
{
  "id": "wingedbean.providers.recorder.asciinema",
  "version": "1.0.0",
  "name": "Asciinema Recorder",
  "description": "Records PTY sessions to asciicast v2 format",
  "author": "WingedBean Team",
  "license": "MIT",

  "entryPoint": {
    "dotnet": "./WingedBean.Providers.Recorder.Asciinema.dll",
    "nodejs": null,
    "unity": "./WingedBean.Providers.Recorder.Asciinema.Unity.dll",
    "godot": "./WingedBean.Providers.Recorder.Asciinema.dll"
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

  "capabilities": [
    "recording",
    "asciinema-v2",
    "filesystem"
  ],

  "supportedProfiles": ["console", "unity", "godot", "web"],

  "loadStrategy": "lazy",
  "quiesceSeconds": 5
}
```

### Plugin Loader Interface

**Profile-agnostic plugin loading abstraction:**

```csharp
// WingedBean.Host/IPluginLoader.cs
namespace WingedBean.Host;

public interface IPluginLoader
{
    Task<ILoadedPlugin> LoadPluginAsync(PluginManifest manifest, CancellationToken ct = default);
    Task UnloadPluginAsync(ILoadedPlugin plugin, CancellationToken ct = default);
    Task ReloadPluginAsync(ILoadedPlugin plugin, CancellationToken ct = default);
}

public interface ILoadedPlugin
{
    string Id { get; }
    Version Version { get; }
    PluginManifest Manifest { get; }
    PluginState State { get; } // Loaded, Activated, Deactivated, Unloaded
    IServiceCollection Services { get; }

    Task ActivateAsync(IServiceProvider hostServices, CancellationToken ct = default);
    Task DeactivateAsync(CancellationToken ct = default);
}

public enum PluginState
{
    Discovered,
    Loading,
    Loaded,
    Activating,
    Activated,
    Deactivating,
    Deactivated,
    Unloading,
    Unloaded,
    Failed
}
```

### Profile-Specific Plugin Loaders

**Console Profile (.NET ALC-based):**

```csharp
// WingedBean.Host.Console/AlcPluginLoader.cs
public class AlcPluginLoader : IPluginLoader
{
    private readonly Dictionary<string, AssemblyLoadContext> _contexts = new();

    public async Task<ILoadedPlugin> LoadPluginAsync(PluginManifest manifest, CancellationToken ct)
    {
        var entryPoint = manifest.EntryPoint.Dotnet;
        if (string.IsNullOrEmpty(entryPoint))
            throw new InvalidOperationException($"Plugin {manifest.Id} has no .NET entry point");

        // Create isolated ALC (collectible for hot-swap)
        var alc = new AssemblyLoadContext(manifest.Id, isCollectible: true);
        var assembly = alc.LoadFromAssemblyPath(Path.GetFullPath(entryPoint));

        // Find IPluginActivator implementation
        var activatorType = assembly.GetTypes()
            .FirstOrDefault(t => typeof(IPluginActivator).IsAssignableFrom(t));

        if (activatorType == null)
            throw new InvalidOperationException($"Plugin {manifest.Id} does not implement IPluginActivator");

        var activator = (IPluginActivator)Activator.CreateInstance(activatorType)!;

        _contexts[manifest.Id] = alc;

        return new LoadedPlugin(manifest, activator, alc);
    }

    public async Task UnloadPluginAsync(ILoadedPlugin plugin, CancellationToken ct)
    {
        if (_contexts.TryGetValue(plugin.Id, out var alc))
        {
            // Quiesce period (allow in-flight operations to complete)
            await Task.Delay(plugin.Manifest.QuiesceSeconds * 1000, ct);

            // Unload ALC (triggers GC for plugin assemblies)
            alc.Unload();
            _contexts.Remove(plugin.Id);
        }
    }

    public async Task ReloadPluginAsync(ILoadedPlugin plugin, CancellationToken ct)
    {
        await UnloadPluginAsync(plugin, ct);
        await LoadPluginAsync(plugin.Manifest, ct);
    }
}
```

**Unity Profile (HybridCLR-based):**

```csharp
// WingedBean.Host.Unity/HybridClrPluginLoader.cs
public class HybridClrPluginLoader : IPluginLoader
{
    public async Task<ILoadedPlugin> LoadPluginAsync(PluginManifest manifest, CancellationToken ct)
    {
        var entryPoint = manifest.EntryPoint.Unity;
        if (string.IsNullOrEmpty(entryPoint))
            throw new InvalidOperationException($"Plugin {manifest.Id} has no Unity entry point");

        // Use HybridCLR to load assembly at runtime
        var dllBytes = await File.ReadAllBytesAsync(entryPoint, ct);
        HybridCLR.RuntimeApi.LoadMetadataForAOTAssembly(dllBytes, HybridCLR.HomologousImageMode.SuperSet);

        var assembly = Assembly.Load(dllBytes);

        var activatorType = assembly.GetTypes()
            .FirstOrDefault(t => typeof(IPluginActivator).IsAssignableFrom(t));

        if (activatorType == null)
            throw new InvalidOperationException($"Plugin {manifest.Id} does not implement IPluginActivator");

        var activator = (IPluginActivator)Activator.CreateInstance(activatorType)!;

        return new LoadedPlugin(manifest, activator, null);
    }

    // HybridCLR doesn't support unloading, so hot-swap replaces instances via DI
    public async Task UnloadPluginAsync(ILoadedPlugin plugin, CancellationToken ct)
    {
        // Deactivate only (cannot unload assemblies in Unity)
        await plugin.DeactivateAsync(ct);
    }
}
```

**Node.js Profile (ES modules):**

```typescript
// projects/nodejs/host/EsModulePluginLoader.ts
export class EsModulePluginLoader implements IPluginLoader {
  private loadedModules = new Map<string, any>();

  async loadPlugin(manifest: PluginManifest): Promise<ILoadedPlugin> {
    const entryPoint = manifest.entryPoint.nodejs;
    if (!entryPoint) {
      throw new Error(`Plugin ${manifest.id} has no Node.js entry point`);
    }

    // Dynamic import (can be hot-swapped via cache invalidation)
    const module = await import(entryPoint);

    if (!module.activate) {
      throw new Error(`Plugin ${manifest.id} does not export 'activate' function`);
    }

    this.loadedModules.set(manifest.id, module);

    return new LoadedPlugin(manifest, module);
  }

  async unloadPlugin(plugin: ILoadedPlugin): Promise<void> {
    // Clear module cache to allow hot-reload
    const entryPoint = plugin.manifest.entryPoint.nodejs;
    if (entryPoint) {
      delete require.cache[require.resolve(entryPoint)];
    }
    this.loadedModules.delete(plugin.id);
  }

  async reloadPlugin(plugin: ILoadedPlugin): Promise<void> {
    await this.unloadPlugin(plugin);
    await this.loadPlugin(plugin.manifest);
  }
}
```

### Plugin Activator Interface

Every plugin implements `IPluginActivator` to register its services.

```csharp
// WingedBean.Host/IPluginActivator.cs
public interface IPluginActivator
{
    Task ActivateAsync(IServiceCollection services, IServiceProvider hostServices, CancellationToken ct = default);
    Task DeactivateAsync(CancellationToken ct = default);
}
```

**Example Plugin Implementation:**

```csharp
// WingedBean.Providers.Recorder.Asciinema/AsciinemaRecorderPlugin.cs
using WingedBean.Host;
using WingedBean.Contracts;

namespace WingedBean.Providers.Recorder.Asciinema;

public class AsciinemaRecorderPlugin : IPluginActivator
{
    public Task ActivateAsync(IServiceCollection services, IServiceProvider hostServices, CancellationToken ct)
    {
        // Register this plugin's services
        services.AddTransient<IRecorder, AsciinemaRecorder>();

        // Access host services if needed
        var logger = hostServices.GetRequiredService<ILogger>();
        logger.LogInformation("AsciinemaRecorder plugin activated");

        return Task.CompletedTask;
    }

    public Task DeactivateAsync(CancellationToken ct)
    {
        // Cleanup resources
        return Task.CompletedTask;
    }
}
```

### Plugin Discovery

**Host scans plugin directories and loads manifests:**

```csharp
// WingedBean.Host/PluginDiscovery.cs
public class PluginDiscovery
{
    private readonly string[] _pluginDirectories;

    public PluginDiscovery(params string[] pluginDirectories)
    {
        _pluginDirectories = pluginDirectories;
    }

    public async Task<IEnumerable<PluginManifest>> DiscoverPluginsAsync()
    {
        var manifests = new List<PluginManifest>();

        foreach (var dir in _pluginDirectories)
        {
            if (!Directory.Exists(dir)) continue;

            var manifestFiles = Directory.GetFiles(dir, "*.plugin.json", SearchOption.AllDirectories);

            foreach (var file in manifestFiles)
            {
                var json = await File.ReadAllTextAsync(file);
                var manifest = JsonSerializer.Deserialize<PluginManifest>(json);
                if (manifest != null)
                {
                    manifests.Add(manifest);
                }
            }
        }

        return manifests;
    }
}
```

### Plugin Dependency Resolution

**Topological sort to load plugins in dependency order:**

```csharp
// WingedBean.Host/PluginDependencyResolver.cs
public class PluginDependencyResolver
{
    public IEnumerable<PluginManifest> ResolveLoadOrder(IEnumerable<PluginManifest> manifests)
    {
        var graph = new Dictionary<string, List<string>>();
        var manifestMap = manifests.ToDictionary(m => m.Id, m => m);

        // Build dependency graph
        foreach (var manifest in manifests)
        {
            graph[manifest.Id] = manifest.Dependencies.Keys.ToList();
        }

        // Topological sort (Kahn's algorithm)
        var sorted = new List<PluginManifest>();
        var inDegree = graph.Keys.ToDictionary(k => k, k => 0);

        foreach (var deps in graph.Values)
        {
            foreach (var dep in deps)
            {
                if (inDegree.ContainsKey(dep))
                    inDegree[dep]++;
            }
        }

        var queue = new Queue<string>(inDegree.Where(kv => kv.Value == 0).Select(kv => kv.Key));

        while (queue.Count > 0)
        {
            var id = queue.Dequeue();
            sorted.Add(manifestMap[id]);

            foreach (var dep in graph[id])
            {
                if (--inDegree[dep] == 0)
                    queue.Enqueue(dep);
            }
        }

        if (sorted.Count != manifests.Count())
            throw new InvalidOperationException("Circular dependency detected in plugins");

        return sorted;
    }
}
```

### Host Boot Sequence

```csharp
// WingedBean.Host/HostBootstrap.cs
public class HostBootstrap
{
    private readonly IPluginLoader _pluginLoader;
    private readonly PluginDiscovery _discovery;
    private readonly PluginDependencyResolver _resolver;
    private readonly ServiceCollection _services = new();

    public async Task BootAsync(CancellationToken ct = default)
    {
        // 1. Register host services (DI container, event bus, logger)
        RegisterHostServices();

        // 2. Discover plugins
        var manifests = await _discovery.DiscoverPluginsAsync();

        // 3. Resolve dependency order
        var orderedManifests = _resolver.ResolveLoadOrder(manifests);

        // 4. Load plugins
        var loadedPlugins = new List<ILoadedPlugin>();
        foreach (var manifest in orderedManifests)
        {
            var plugin = await _pluginLoader.LoadPluginAsync(manifest, ct);
            loadedPlugins.Add(plugin);
        }

        // 5. Build intermediate service provider (for plugin activation)
        var hostServiceProvider = _services.BuildServiceProvider();

        // 6. Activate plugins (in dependency order)
        foreach (var plugin in loadedPlugins)
        {
            await plugin.ActivateAsync(hostServiceProvider, ct);
        }

        // 7. Build final service provider (with all plugin services)
        var finalServiceProvider = _services.BuildServiceProvider();

        // 8. Host is ready
        Console.WriteLine($"Host initialized with {loadedPlugins.Count} plugins");
    }

    private void RegisterHostServices()
    {
        _services.AddSingleton<IEventBus, EventBus>();
        _services.AddLogging();
        // ... other host services
    }
}
```

## Implementation Plan

### Phase 1: Plugin System Foundation (3-4 weeks)
1. **Week 1: Core Interfaces**
   - Define `IPluginLoader`, `IPluginActivator`, `ILoadedPlugin`
   - Define `PluginManifest` data model
   - Define `PluginState` enum and lifecycle
   - **DOD**: Core plugin interfaces published in `WingedBean.Host` package

2. **Week 2: Plugin Discovery & Resolution**
   - Implement `PluginDiscovery` (scan directories for `.plugin.json`)
   - Implement `PluginDependencyResolver` (topological sort)
   - Implement `PluginRegistry` (metadata storage)
   - **DOD**: Discovery and resolution working with sample plugins

3. **Week 3: Console Profile Plugin Loader**
   - Implement `AlcPluginLoader` (.NET ALC-based)
   - Implement `LoadedPlugin` class
   - Add hot-reload support (unload/reload)
   - **DOD**: Load/unload/reload working in Console profile

4. **Week 4: Host Bootstrap**
   - Implement `HostBootstrap` (orchestrate discovery, load, activate)
   - Implement event bus for inter-plugin communication
   - Implement plugin lifecycle management (activate/deactivate)
   - **DOD**: Host boots with multiple plugins, all services registered

### Phase 2: Refactor Existing Code to Plugins (2-3 weeks)
1. **Week 5: Extract Core Services as Plugins**
   - Extract `AsciinemaRecorder` as plugin
   - Extract `NodePtyProvider` as plugin (Node.js)
   - Extract `Terminal.Gui` app as plugin
   - Create `.plugin.json` manifests for each
   - **DOD**: Existing functionality works via plugin system

2. **Week 6-7: Plugin DI Integration**
   - Integrate plugin services into host DI container
   - Test hot-reload (unload/reload plugins without restart)
   - Test dependency resolution (plugin A depends on plugin B)
   - **DOD**: All integration tests passing

### Phase 3: Unity/Godot Plugin Loaders (Future)
1. **Unity Profile** (3-4 weeks)
   - Implement `HybridClrPluginLoader`
   - Test Unity-specific plugin loading
   - Test MonoBehaviour lifecycle integration
   - **DOD**: Unity host boots with plugins

2. **Godot Profile** (2-3 weeks)
   - Implement `GodotAlcPluginLoader` (C# ALC in Godot)
   - Test Godot-specific plugin loading
   - Test Node lifecycle integration
   - **DOD**: Godot host boots with plugins

3. **Web Profile** (2-3 weeks)
   - Implement `EsModulePluginLoader` (Node.js)
   - Test ES module hot-reload
   - Test WASM constraints (browser limitations)
   - **DOD**: Web host boots with plugins

### Phase 4: Advanced Features (Future)
1. **Plugin Versioning** (1 week)
   - Support multiple versions of same plugin
   - Implement version negotiation
   - **DOD**: Multiple versions coexist

2. **Plugin Security** (2 weeks)
   - Implement plugin signing (verify integrity)
   - Implement sandboxing (permission model)
   - **DOD**: Only signed plugins load

3. **Plugin Marketplace** (Future)
   - Online plugin repository
   - Plugin search/install/update
   - **DOD**: Install plugins from marketplace

## Benefits

### Key Advantages

1. **Everything is a Plugin**: Even core services are loaded dynamically
2. **Hot-Swap**: Reload plugins without restarting the host
3. **Profile-Agnostic**: Same plugin system works on Console, Unity, Godot, Web
4. **Dependency Management**: Plugins declare dependencies, auto-resolved
5. **Versioning**: Multiple versions coexist
6. **Third-Party Extensions**: Open ecosystem for community plugins

## Testing Strategy

### Unit Tests
- Plugin discovery (scan directories, parse manifests)
- Dependency resolution (topological sort, circular detection)
- Plugin loader (load/unload/reload)

### Integration Tests
- Load multiple plugins in dependency order
- Hot-reload plugin (unload/reload)
- Plugin inter-communication via event bus

### E2E Tests
- Boot host with real plugins
- Verify all services registered
- Hot-reload and verify functionality preserved

## Definition of Done

### Phase 1 (Plugin System Foundation)
- [ ] `WingedBean.Host` package with core plugin interfaces
- [ ] `PluginDiscovery` working (scan directories, load manifests)
- [ ] `PluginDependencyResolver` working (topological sort)
- [ ] `AlcPluginLoader` working (load/unload/reload in Console profile)
- [ ] `HostBootstrap` working (orchestrate plugin lifecycle)
- [ ] Unit tests for all components (>80% coverage)

### Phase 2 (Refactor to Plugins)
- [ ] `AsciinemaRecorder` extracted as plugin
- [ ] `NodePtyProvider` extracted as plugin
- [ ] `Terminal.Gui` app extracted as plugin
- [ ] All plugins have `.plugin.json` manifests
- [ ] Host boots with plugins, all functionality preserved
- [ ] Hot-reload working (unload/reload plugins)

### Phase 3 (Unity/Godot Loaders)
- [ ] `HybridClrPluginLoader` working in Unity
- [ ] `GodotAlcPluginLoader` working in Godot
- [ ] `EsModulePluginLoader` working in Node.js/Web

### Phase 4 (Advanced Features)
- [ ] Plugin versioning working
- [ ] Plugin signing working
- [ ] Plugin marketplace (future)

## Dependencies

- **RFC-0002**: 4-Tier Architecture (plugins implement tiers)
- **RFC-0001**: Asciinema recording (will become a plugin)

## Risks and Mitigations

### Risk: ALC/HybridCLR Complexity
- **Mitigation**: Start with simple Console profile (ALC), then Unity (HybridCLR)
- **Mitigation**: Reference pinto-bean's hot-swap implementation

### Risk: Performance Overhead
- **Mitigation**: Lazy loading (only load plugins on demand)
- **Mitigation**: Benchmark plugin loading time (must be <1s per plugin)

### Risk: Dependency Hell
- **Mitigation**: Strict semver versioning
- **Mitigation**: Isolated load contexts prevent conflicts

## Alternatives Considered

### 1. MEF (Managed Extensibility Framework)
- ✅ Mature .NET plugin system
- ❌ Heavy, not designed for hot-reload
- **Decision**: Custom plugin system for hot-swap and profile support

### 2. Static Linking (No Plugins)
- ✅ Simpler, no runtime loading
- ❌ Cannot hot-swap, cannot extend
- **Decision**: Plugin architecture is essential for modularity

## Future Enhancements

1. **Plugin Marketplace**: Online repository for community plugins
2. **Plugin Sandboxing**: Permission model (file system, network access)
3. **Plugin Monitoring**: Telemetry, crash reporting
4. **Plugin Analytics**: Usage stats, performance metrics

## References

- [.NET AssemblyLoadContext Documentation](https://learn.microsoft.com/en-us/dotnet/core/dependency-loading/understanding-assemblyloadcontext)
- [HybridCLR Documentation](https://hybridclr.doc.code-philosophy.com/)
- [MEF (Managed Extensibility Framework)](https://learn.microsoft.com/en-us/dotnet/framework/mef/)

## Notes

- **Plugin architecture is foundational** - must be implemented before 4-tier architecture
- Everything (contracts, adapters, providers, façades) is loaded as a plugin
- Even the "core" services are plugins loaded at startup (just marked as "eager" load strategy)
- This enables true hot-swap, third-party extensions, and profile-agnostic modularity

---

**Author**: Ray Wang (with Claude AI assistance)
**Reviewer**: [Pending]
**Implementation**: [Assigned]
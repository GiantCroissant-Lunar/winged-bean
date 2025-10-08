# Plugin Loading Flow Chart

## Visual Overview

```
┌─────────────────────────────────────────────────────────────────────┐
│                    Plugin Loading Orchestration                      │
└─────────────────────────────────────────────────────────────────────┘
                                  │
                                  ▼
┌─────────────────────────────────────────────────────────────────────┐
│                     PHASE 0: BOOTSTRAP                               │
│                   (Critical Infrastructure)                          │
├─────────────────────────────────────────────────────────────────────┤
│                                                                       │
│  1. Discover all .plugin.json manifests                             │
│     └─→ Scan plugins/ directory recursively                         │
│                                                                       │
│  2. Identify Resource plugin                                         │
│     └─→ Find manifest with id="wingedbean.plugins.resource"         │
│                                                                       │
│  3. Load Resource plugin assembly                                    │
│     ├─→ SKIP NuGet dependency loading (not available yet!)          │
│     ├─→ Load WingedBean.Plugins.Resource.dll                        │
│     └─→ Contains: FileSystemResourceService + NuGetResourceProvider │
│                                                                       │
│  4. Activate Resource plugin                                         │
│     └─→ Call plugin.ActivateAsync()                                 │
│                                                                       │
│  5. Register services in DI                                          │
│     └─→ Register IResourceService → FileSystemResourceService       │
│                                                                       │
│  6. Verify IResourceService available                                │
│     ├─→ Get IResourceService from registry                          │
│     └─→ ✓ Success: Can now load NuGet packages!                     │
│                                                                       │
│  Result: IResourceService + NuGet provider available                 │
│                                                                       │
└─────────────────────────────────────────────────────────────────────┘
                                  │
                                  ▼
┌─────────────────────────────────────────────────────────────────────┐
│               PHASE 1: RESOURCE PLUGIN DEPENDENCIES                  │
│              (Optional - for future extensibility)                   │
├─────────────────────────────────────────────────────────────────────┤
│                                                                       │
│  Currently: Resource plugin has NO NuGet dependencies                │
│             (Design constraint - self-contained)                     │
│                                                                       │
│  If Resource plugin had NuGet deps in future:                        │
│  1. Read Resource plugin manifest                                    │
│  2. For each NuGet dependency:                                       │
│     ├─→ Build URI: "nuget:PackageId/Version"                        │
│     ├─→ Call resourceService.LoadAsync<NuGetPackageResource>()      │
│     └─→ Assemblies loaded into AppDomain                            │
│  3. Reload or patch Resource plugin if needed                        │
│                                                                       │
│  Current Status: ⊘ Skipped (no deps)                                │
│                                                                       │
└─────────────────────────────────────────────────────────────────────┘
                                  │
                                  ▼
┌─────────────────────────────────────────────────────────────────────┐
│            PHASE 2: DEPENDENCY-ORDERED PLUGIN LOADING                │
│                  (All other plugins)                                 │
├─────────────────────────────────────────────────────────────────────┤
│                                                                       │
│  Step 1: Build Dependency Graph                                      │
│  ┌─────────────────────────────────────────────────────┐            │
│  │  For each manifest (except Resource):               │            │
│  │    - Extract plugin dependencies                     │            │
│  │    - Build PluginNode                                │            │
│  │    - Add edges for dependencies                      │            │
│  └─────────────────────────────────────────────────────┘            │
│                                                                       │
│  Step 2: Assign Dependency Tiers                                     │
│  ┌─────────────────────────────────────────────────────┐            │
│  │  Tier 0: No dependencies                             │            │
│  │  Tier 1: Depends only on Tier 0                      │            │
│  │  Tier 2: Depends on Tier 0 or Tier 1                │            │
│  │  Tier N: Depends on Tier 0..N-1                      │            │
│  │                                                       │            │
│  │  Detect circular dependencies → FAIL                 │            │
│  └─────────────────────────────────────────────────────┘            │
│                                                                       │
│  Step 3: Sort Within Tiers by Priority                               │
│  ┌─────────────────────────────────────────────────────┐            │
│  │  Within each tier:                                   │            │
│  │    Sort by priority (descending)                     │            │
│  │    Higher priority = earlier load                    │            │
│  └─────────────────────────────────────────────────────┘            │
│                                                                       │
│  Step 4: Load Plugins in Order                                       │
│  ┌─────────────────────────────────────────────────────┐            │
│  │  For each plugin in sorted order:                   │            │
│  │                                                       │            │
│  │    A. Verify Dependencies Loaded                     │            │
│  │       ├─→ Check all plugin deps in loadedPlugins     │            │
│  │       └─→ If missing → FAIL with error              │            │
│  │                                                       │            │
│  │    B. Load NuGet Dependencies                         │            │
│  │       ├─→ For each NuGet dep in manifest:            │            │
│  │       │   ├─→ Build URI: "nuget:PackageId/Version"  │            │
│  │       │   ├─→ Call resourceService.LoadAsync()       │            │
│  │       │   ├─→ Package downloaded (if not cached)     │            │
│  │       │   ├─→ Assemblies loaded                      │            │
│  │       │   └─→ ✓ Package available                    │            │
│  │       └─→ All NuGet deps loaded                      │            │
│  │                                                       │            │
│  │    C. Load Plugin Assembly                            │            │
│  │       ├─→ Resolve entry point path                   │            │
│  │       ├─→ Load via PluginLoader (ALC isolation)      │            │
│  │       └─→ Plugin types available                     │            │
│  │                                                       │            │
│  │    D. Activate Plugin                                 │            │
│  │       └─→ Call plugin.ActivateAsync()                │            │
│  │                                                       │            │
│  │    E. Register Services                               │            │
│  │       ├─→ Scan for service implementations           │            │
│  │       ├─→ Register in DI container/Registry          │            │
│  │       └─→ ✓ Services available to other plugins      │            │
│  │                                                       │            │
│  │    F. Mark as Loaded                                  │            │
│  │       └─→ Add to loadedPlugins set                   │            │
│  │                                                       │            │
│  └─────────────────────────────────────────────────────┘            │
│                                                                       │
│  Result: All plugins loaded in correct order                         │
│                                                                       │
└─────────────────────────────────────────────────────────────────────┘
                                  │
                                  ▼
┌─────────────────────────────────────────────────────────────────────┐
│                      VERIFICATION PHASE                              │
├─────────────────────────────────────────────────────────────────────┤
│                                                                       │
│  1. Verify required services registered                              │
│     ├─→ Check IResourceService                                      │
│     ├─→ Check ITerminalApp                                          │
│     └─→ Check other critical services                               │
│                                                                       │
│  2. Log summary                                                      │
│     ├─→ Total plugins loaded                                        │
│     ├─→ Total services registered                                   │
│     └─→ Any warnings or issues                                      │
│                                                                       │
│  3. Ready for application start                                      │
│                                                                       │
└─────────────────────────────────────────────────────────────────────┘
```

## Example: Real-World Plugin Loading

### Scenario Setup

```
Plugins in directory:
├── WingedBean.Plugins.Resource/
│   └── .plugin.json (id: resource, priority: 100, deps: [])
├── WingedBean.Plugins.Config/
│   └── .plugin.json (id: config, priority: 100, deps: [resource])
├── WingedBean.Plugins.DungeonGame/
│   └── .plugin.json (id: dungeon, priority: 50, deps: [resource, config])
│       NuGet: [Newtonsoft.Json@13.0.3]
└── WingedBean.Plugins.Audio/
    └── .plugin.json (id: audio, priority: 50, deps: [resource])
        NuGet: [NAudio@2.2.1]
```

### Execution Trace

```
[Phase 0] Bootstrapping Resource plugin...
  → Discovered 4 plugin manifests
  → Identified Resource plugin: wingedbean.plugins.resource
  → Loading Resource plugin assembly (SKIP NuGet deps)
    ├─→ Load: plugins/WingedBean.Plugins.Resource/WingedBean.Plugins.Resource.dll
    ├─→ Activate plugin
    └─→ Register IResourceService → FileSystemResourceService
  → Verifying IResourceService available
    └─→ ✓ IResourceService registered
  → Verifying NuGet provider available
    └─→ ✓ NuGetResourceProvider loaded (part of Resource plugin)
✓ Resource plugin bootstrapped

[Phase 1] Loading Resource plugin dependencies...
  → Resource plugin has 0 NuGet dependencies
  ⊘ Skipped (no dependencies)
✓ Resource plugin fully initialized

[Phase 2] Loading remaining plugins...
  → Building dependency graph for 3 plugins
  → Assigning dependency tiers:
    ├─→ Tier 0: config, audio (no deps except Resource)
    └─→ Tier 1: dungeon (deps: config)
  → Sorting within tiers by priority:
    ├─→ Tier 0: config (100), audio (50)
    └─→ Tier 1: dungeon (50)
  → Final load order: config → audio → dungeon

  [1/3] Loading: wingedbean.plugins.config
    → Verifying dependencies: [resource] ✓
    → Loading 0 NuGet dependencies
    ⊘ No NuGet dependencies
    → Loading plugin assembly: plugins/WingedBean.Plugins.Config/WingedBean.Plugins.Config.dll
    → Activating plugin
    → Registering services
      └─→ IConfigService → ConfigurationService
    ✓ Loaded: config v1.0.0

  [2/3] Loading: wingedbean.plugins.audio
    → Verifying dependencies: [resource] ✓
    → Loading 1 NuGet dependencies...
      → Loading NuGet: NAudio latest
        └─→ Calling: resourceService.LoadAsync<NuGetPackageResource>("nuget:NAudio/2.2.1")
        └─→ NuGet provider downloading package...
        └─→ Package extracted to: ~/.wingedbean/packages/naudio/2.2.1/
        └─→ Assemblies loaded: NAudio.dll, NAudio.Core.dll
        └─→ ✓ Loaded: NAudio v2.2.1
    ✓ NuGet dependencies loaded
    → Loading plugin assembly: plugins/WingedBean.Plugins.Audio/WingedBean.Plugins.Audio.dll
    → Activating plugin
    → Registering services
      └─→ IAudioService → VlcAudioService
    ✓ Loaded: audio v1.0.0

  [3/3] Loading: wingedbean.plugins.dungeongame
    → Verifying dependencies: [resource, config] ✓
    → Loading 1 NuGet dependencies...
      → Loading NuGet: Newtonsoft.Json 13.0.3
        └─→ Calling: resourceService.LoadAsync<NuGetPackageResource>("nuget:Newtonsoft.Json/13.0.3")
        └─→ Package already in cache
        └─→ Assemblies loaded: Newtonsoft.Json.dll
        └─→ ✓ Loaded: Newtonsoft.Json v13.0.3 (from cache)
    ✓ NuGet dependencies loaded
    → Loading plugin assembly: plugins/WingedBean.Plugins.DungeonGame/WingedBean.Plugins.DungeonGame.dll
    → Activating plugin
    → Registering services
      └─→ IDungeonGameService → DungeonGameEngine
    ✓ Loaded: dungeon v1.0.0

✓ 3 plugins loaded successfully (+ 1 bootstrap)

[Verification] Checking required services...
  ✓ IResourceService registered: FileSystemResourceService
  ✓ IConfigService registered: ConfigurationService
  ✓ IDungeonGameService registered: DungeonGameEngine
  ✓ IAudioService registered: VlcAudioService
✓ All required services registered

========================================
Ready to start application
Total plugins: 4
Total NuGet packages: 2
Load time: 3.5 seconds
========================================
```

## Dependency Graph Visualization

```
                    ┌──────────────┐
                    │   Resource   │ ◄── Phase 0 Bootstrap
                    │ (priority100)│
                    └──────┬───────┘
                           │
              ┌────────────┼────────────┐
              │            │            │
              ▼            ▼            ▼
         ┌────────┐   ┌────────┐   ┌────────┐
         │ Config │   │ Audio  │   │ Other  │ ◄── Tier 0
         │  (100) │   │  (50)  │   │  (80)  │
         └────┬───┘   └────────┘   └────────┘
              │
              │ depends on
              │
              ▼
         ┌─────────┐
         │ Dungeon │ ◄── Tier 1
         │   (50)  │
         └─────────┘

Load Order (with priority):
1. Resource (Phase 0 - Bootstrap)
2. Config (Tier 0, priority 100)
3. Other (Tier 0, priority 80)
4. Audio (Tier 0, priority 50)
5. Dungeon (Tier 1, priority 50)
```

## Error Scenarios

### Error 1: Circular Dependency

```
Plugin A → Plugin B → Plugin C → Plugin A

[Phase 2] Loading remaining plugins...
  → Building dependency graph
  → Assigning dependency tiers
  ✗ ERROR: Circular dependency detected:
    plugin-a → plugin-b → plugin-c → plugin-a
  
  Cannot determine load order.
  Please review plugin dependencies in manifests:
    - plugins/PluginA/.plugin.json
    - plugins/PluginB/.plugin.json
    - plugins/PluginC/.plugin.json

FATAL: Plugin loading failed
```

### Error 2: Missing Dependency

```
Plugin B depends on Plugin A (not found)

[Phase 2] Loading remaining plugins...
  [1/2] Loading: plugin-b
    → Verifying dependencies: [plugin-a]
    ✗ ERROR: Dependency 'plugin-a' not loaded
      Possible causes:
        - Plugin A not found in plugins directory
        - Plugin A failed to load
        - Plugin A disabled in configuration
      
      Required by: plugin-b
      Location: plugins/PluginB/.plugin.json

  ⊘ Skipping plugin-b (dependency failed)

WARN: 1 plugin failed to load due to missing dependencies
```

### Error 3: NuGet Package Not Found

```
Plugin needs Newtonsoft.Json 99.0.0 (doesn't exist)

[Phase 2] Loading remaining plugins...
  [1/1] Loading: plugin-a
    → Verifying dependencies: [resource] ✓
    → Loading 1 NuGet dependencies...
      → Loading NuGet: Newtonsoft.Json 99.0.0
        ✗ ERROR: Package not found: Newtonsoft.Json v99.0.0
          Feed: https://api.nuget.org/v3/index.json
          
      ✗ Failed to load required NuGet package
      
  ✗ Failed to load plugin-a: NuGet dependency failed

ERROR: 1 plugin failed to load
```

## Implementation Checklist

- [ ] Extract bootstrap logic into `BootstrapResourcePluginAsync()`
- [ ] Implement `BuildDependencyGraph()`
- [ ] Implement `AssignTiers()` with cycle detection
- [ ] Implement tier-based sorting with priority
- [ ] Update `LoadNuGetDependenciesAsync()` to require IResourceService
- [ ] Add comprehensive error messages
- [ ] Add logging at each phase
- [ ] Test with real plugin scenarios
- [ ] Document constraint: Resource plugin should be self-contained
- [ ] Update all plugin manifests to declare Resource dependency

## Benefits of This Approach

✅ **Predictable** - Always loads Resource plugin first  
✅ **Ordered** - Respects dependency graph  
✅ **Prioritized** - Uses priority within tiers  
✅ **Safe** - Detects circular dependencies  
✅ **Clear** - Detailed logging and error messages  
✅ **Extensible** - Easy to add new plugins  
✅ **Testable** - Each phase can be tested independently  

## See Also

- PLUGIN-LOADING-FLOW-WITH-RESOURCE-SERVICE.md (Detailed design doc)
- PLUGIN-MANIFEST-NUGET-DEPENDENCIES.md (Manifest schema)
- RFC-0039: NuGet Package Resource Provider

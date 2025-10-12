# Plugin Loading Flow with Resource Service

## Problem Statement

**Circular Dependency Problem:**
- Plugins can have NuGet dependencies
- NuGet dependencies require IResourceService
- IResourceService is provided by Resource plugin
- Resource plugin might have NuGet dependencies
- ❌ **Current Issue:** Plugins load in discovery order, not dependency order

## Current Flow (Problematic)

```
1. Discover all .plugin.json files
2. For each manifest (in discovery order):
   ├─→ Load NuGet dependencies (requires IResourceService)
   │   ├─→ Get IResourceService from registry
   │   └─→ ❌ FAILS if Resource plugin not loaded yet!
   ├─→ Load plugin assembly
   └─→ Register services
```

**Problem Scenarios:**

### Scenario 1: Plugin A needs NuGet, loaded before Resource plugin
```
1. Plugin A discovered first
2. Try to load NuGet dependency for Plugin A
3. ❌ IResourceService not in registry → Skip NuGet loading
4. Load Plugin A (without NuGet packages!)
5. Resource plugin discovered
6. Load Resource plugin
7. Register IResourceService
```

### Scenario 2: Resource plugin needs NuGet (bootstrap problem)
```
1. Resource plugin discovered
2. Try to load NuGet dependency for Resource plugin
3. ❌ IResourceService not in registry (it's in the plugin being loaded!)
4. Chicken and egg problem
```

## Solution: Multi-Phase Loading with Dependency Ordering

### Phase-Based Approach

```
┌─────────────────────────────────────────────────────────────┐
│ Phase 0: Bootstrap (Critical Infrastructure)                 │
├─────────────────────────────────────────────────────────────┤
│ • Load Resource plugin WITHOUT NuGet dependencies            │
│ • Register IResourceService                                  │
│ • Load NuGet provider                                        │
│ • Now IResourceService + NuGet provider available            │
└─────────────────────────────────────────────────────────────┘
                            ↓
┌─────────────────────────────────────────────────────────────┐
│ Phase 1: Load Resource Plugin's NuGet Dependencies (if any) │
├─────────────────────────────────────────────────────────────┤
│ • IResourceService now available                             │
│ • Load Resource plugin's NuGet packages                      │
│ • Reload/patch Resource plugin if needed                     │
└─────────────────────────────────────────────────────────────┘
                            ↓
┌─────────────────────────────────────────────────────────────┐
│ Phase 2: Dependency-Ordered Plugin Loading                   │
├─────────────────────────────────────────────────────────────┤
│ • Build dependency graph                                     │
│ • Topological sort                                           │
│ • Load in order:                                             │
│   1. Plugins with no dependencies                            │
│   2. Plugins depending on (1)                                │
│   3. Plugins depending on (2)                                │
│   etc.                                                        │
│ • Each plugin:                                               │
│   ├─→ Load NuGet dependencies (Resource service available)  │
│   ├─→ Load plugin assembly                                   │
│   └─→ Register services                                      │
└─────────────────────────────────────────────────────────────┘
```

## Detailed Flow

### Phase 0: Bootstrap

```typescript
async function Phase0_Bootstrap() {
  // Step 1: Discover all manifests
  manifests = DiscoverAllManifests()
  
  // Step 2: Identify Resource plugin
  resourcePlugin = manifests.find(m => m.id == "wingedbean.plugins.resource")
  
  if (!resourcePlugin) {
    throw "CRITICAL: Resource plugin not found!"
  }
  
  // Step 3: Load Resource plugin (SKIP NuGet dependencies for now)
  logger.Info("Phase 0: Bootstrapping Resource plugin...")
  plugin = LoadPluginAssembly(resourcePlugin.entryPoint)
  RegisterServices(plugin)
  
  // Step 4: Verify IResourceService registered
  resourceService = registry.Get<IResourceService>()
  logger.Info("✓ IResourceService available")
  
  // Step 5: Verify NuGet provider loaded (part of Resource plugin)
  logger.Info("✓ NuGet provider available")
  
  return resourceService
}
```

### Phase 1: Load Resource Plugin's Own Dependencies

```typescript
async function Phase1_ResourcePluginDependencies(resourceService) {
  resourcePlugin = manifests.find(m => m.id == "wingedbean.plugins.resource")
  
  if (resourcePlugin.dependencies.nuget.length == 0) {
    logger.Info("Phase 1: Resource plugin has no NuGet dependencies, skipping")
    return
  }
  
  logger.Info("Phase 1: Loading Resource plugin's NuGet dependencies...")
  
  // Now we CAN load NuGet packages because IResourceService exists
  foreach (dep in resourcePlugin.dependencies.nuget) {
    package = await resourceService.LoadAsync<NuGetPackageResource>(
      $"nuget:{dep.packageId}/{dep.version}"
    )
    logger.Info($"  ✓ Loaded: {dep.packageId} v{package.Version}")
  }
  
  logger.Info("✓ Resource plugin fully initialized with all dependencies")
}
```

### Phase 2: Dependency-Ordered Loading

```typescript
async function Phase2_LoadRemainingPlugins(resourceService) {
  logger.Info("Phase 2: Loading remaining plugins in dependency order...")
  
  // Step 1: Get all non-Resource plugins
  remainingPlugins = manifests.filter(m => m.id != "wingedbean.plugins.resource")
  
  // Step 2: Build dependency graph
  graph = BuildDependencyGraph(remainingPlugins)
  
  // Step 3: Topological sort (dependency order)
  loadOrder = TopologicalSort(graph)
  
  if (loadOrder.hasCycle) {
    throw "CRITICAL: Circular dependency detected!"
  }
  
  // Step 4: Load in order
  foreach (pluginId in loadOrder) {
    manifest = manifests.find(m => m.id == pluginId)
    
    logger.Info($"→ Loading: {pluginId}")
    
    // Check plugin dependencies loaded
    foreach (depId in manifest.dependencies.plugins) {
      if (!loadedPlugins.contains(depId)) {
        throw $"Dependency '{depId}' not loaded for '{pluginId}'"
      }
    }
    
    // Load NuGet dependencies (Resource service available!)
    await LoadNuGetDependencies(manifest, resourceService)
    
    // Load plugin assembly
    plugin = await LoadPluginAssembly(manifest.entryPoint)
    loadedPlugins[pluginId] = plugin
    
    // Register services
    await RegisterServices(plugin)
    
    logger.Info($"  ✓ Loaded: {pluginId} v{manifest.version}")
  }
  
  logger.Info($"✓ {loadedPlugins.Count} plugins loaded")
}
```

## Dependency Graph Example

```
Resource Plugin (priority: 100, no deps)
    ↓
├── Config Plugin (priority: 100, deps: [resource])
│   ├── NuGet: None
│   └── Services: IConfigService
│
├── Plugin A (priority: 50, deps: [resource])
│   ├── NuGet: [Newtonsoft.Json]
│   └── Services: IPluginAService
│
└── Plugin B (priority: 50, deps: [resource, plugin-a])
    ├── NuGet: [Polly]
    └── Services: IPluginBService

Load Order:
1. Resource (Phase 0 - Bootstrap)
2. Config, Plugin A (Phase 2 - Tier 1, parallel possible)
3. Plugin B (Phase 2 - Tier 2, depends on A)
```

## Priority vs. Dependency Order

**Current Problem:**
- Priority determines load order
- But doesn't consider dependencies

**Solution:**
```typescript
function DetermineFinalLoadOrder(manifests) {
  // Step 1: Group by dependency tier
  tier0 = [Resource]  // Bootstrap
  tier1 = manifests with no plugin deps (except Resource)
  tier2 = manifests depending only on tier0 + tier1
  tier3 = manifests depending on tier0 + tier1 + tier2
  // etc.
  
  // Step 2: Within each tier, sort by priority
  foreach (tier in [tier1, tier2, tier3, ...]) {
    tier.sortBy(m => -m.priority)  // Descending
  }
  
  // Step 3: Final load order
  return [tier0, tier1, tier2, tier3, ...]
}
```

**Example:**
```
Manifests:
- Resource (priority: 100, deps: [])
- Config (priority: 100, deps: [resource])
- Plugin A (priority: 80, deps: [resource])
- Plugin B (priority: 90, deps: [resource, plugin-a])
- Plugin C (priority: 70, deps: [resource])

Load Order:
1. Resource (Tier 0 - bootstrap)
2. Config (Tier 1, priority 100)
3. Plugin A (Tier 1, priority 80)
4. Plugin C (Tier 1, priority 70)
5. Plugin B (Tier 2, priority 90, depends on A)
```

## Implementation Changes Needed

### 1. Add Phase-Based Loading to PluginLoaderHostedService

```csharp
private async Task LoadPluginsAsync(CancellationToken cancellationToken)
{
    _logger.LogInformation("========================================");
    _logger.LogInformation("ConsoleDungeon.Host - Plugin Loading");
    _logger.LogInformation("========================================");
    
    try
    {
        // Phase 0: Bootstrap Resource plugin
        _logger.LogInformation("[Phase 0] Bootstrapping Resource plugin...");
        var resourceService = await BootstrapResourcePluginAsync(cancellationToken);
        _logger.LogInformation("✓ Resource plugin bootstrapped");
        
        // Phase 1: Load Resource plugin's NuGet dependencies (if any)
        _logger.LogInformation("[Phase 1] Loading Resource plugin dependencies...");
        await LoadResourcePluginDependenciesAsync(resourceService, cancellationToken);
        _logger.LogInformation("✓ Resource plugin fully initialized");
        
        // Phase 2: Load remaining plugins in dependency order
        _logger.LogInformation("[Phase 2] Loading remaining plugins...");
        await LoadRemainingPluginsAsync(resourceService, cancellationToken);
        _logger.LogInformation("✓ All plugins loaded");
    }
    catch (Exception ex)
    {
        _logger.LogCritical(ex, "FATAL: Plugin loading failed");
        throw;
    }
}
```

### 2. Add Dependency Graph Builder

```csharp
private class PluginNode
{
    public string Id { get; set; }
    public PluginManifest Manifest { get; set; }
    public List<string> Dependencies { get; set; }
    public int Tier { get; set; } = -1;
}

private List<PluginNode> BuildDependencyGraph(List<PluginManifest> manifests)
{
    var nodes = manifests.Select(m => new PluginNode
    {
        Id = m.Id,
        Manifest = m,
        Dependencies = ExtractPluginDependencies(m)
    }).ToList();
    
    // Assign tiers
    AssignTiers(nodes);
    
    return nodes;
}

private void AssignTiers(List<PluginNode> nodes)
{
    // Tier 0: No dependencies (except Resource, which is bootstrap)
    foreach (var node in nodes.Where(n => n.Dependencies.Count == 0))
    {
        node.Tier = 0;
    }
    
    // Tier N: Depends only on Tier 0..N-1
    int currentTier = 1;
    while (nodes.Any(n => n.Tier == -1))
    {
        var nodesInCurrentTier = nodes
            .Where(n => n.Tier == -1)
            .Where(n => n.Dependencies.All(dep => 
                nodes.First(x => x.Id == dep).Tier < currentTier
            ))
            .ToList();
        
        if (!nodesInCurrentTier.Any())
        {
            // Circular dependency detected
            var remaining = nodes.Where(n => n.Tier == -1).Select(n => n.Id);
            throw new InvalidOperationException(
                $"Circular dependency detected among: {string.Join(", ", remaining)}"
            );
        }
        
        foreach (var node in nodesInCurrentTier)
        {
            node.Tier = currentTier;
        }
        
        currentTier++;
    }
}
```

### 3. Update LoadNuGetDependenciesAsync

```csharp
private async Task LoadNuGetDependenciesAsync(
    string manifestPath,
    string pluginId,
    IResourceService resourceService,  // Now required parameter
    CancellationToken cancellationToken)
{
    // Remove the try/catch for getting IResourceService
    // It's now passed as parameter, guaranteed to exist
    
    // Rest of implementation stays the same
}
```

## Resource Plugin Special Handling

### Option 1: Resource Plugin Never Has NuGet Dependencies

**Pros:**
- Simplest solution
- No bootstrap problem
- Clear separation of concerns

**Cons:**
- Resource plugin can't use NuGet packages
- Limitation for future extensions

**Implementation:**
```json
// WingedBean.Plugins.Resource/.plugin.json
{
  "id": "wingedbean.plugins.resource",
  "dependencies": {
    "plugins": [],
    "nuget": []  // ✅ Always empty
  }
}
```

### Option 2: Two-Phase Resource Plugin Loading

**Pros:**
- Resource plugin CAN use NuGet packages
- Flexible for future needs

**Cons:**
- More complex bootstrap
- Potential for circular issues

**Implementation:**
1. Load Resource plugin without NuGet deps
2. Register IResourceService
3. Load Resource plugin's NuGet deps
4. Reload/patch if needed

### Option 3: Embed NuGet Provider in Resource Plugin DLL

**Pros:**
- NuGet provider always available
- No external dependencies

**Cons:**
- Tighter coupling
- Already implemented this way!

**Current Status:** ✅ We're already doing this!

## Recommended Implementation

### Strategy: Hybrid Approach

```
1. Bootstrap Phase (Phase 0):
   - Load Resource plugin assembly
   - Resource plugin contains NuGet provider
   - Register IResourceService
   - NuGet support now available
   
2. Dependency Phase (Phase 1):
   - Resource plugin itself should NOT have NuGet deps
   - Document this as a design constraint
   
3. Plugin Loading Phase (Phase 2):
   - Build dependency graph
   - Sort by tiers + priority
   - Load in order with NuGet support
```

### Why This Works:

1. **Resource plugin is self-contained** - Contains NuGet provider, no external NuGet deps
2. **Clear bootstrap** - Resource plugin loads first (Phase 0)
3. **Dependency ordering** - Other plugins load in dependency order (Phase 2)
4. **NuGet always available** - After Phase 0, all plugins can use NuGet

## Configuration

### Explicit Bootstrap Order

```json
// appsettings.json
{
  "PluginLoader": {
    "BootstrapPlugins": [
      "wingedbean.plugins.resource"  // Always first
    ],
    "EnableDependencyOrdering": true,
    "FailOnCircularDependencies": true
  }
}
```

### Manifest Priority vs Tier

```json
{
  "id": "my-plugin",
  "priority": 100,  // Within-tier priority
  "dependencies": {
    "plugins": ["resource"]  // Determines tier
  }
}
```

## Error Handling

### Missing Dependency

```
✗ Failed to load plugin 'plugin-b': Dependency 'plugin-a' not loaded
Possible causes:
  - plugin-a failed to load
  - plugin-a not found in plugins directory
  - plugin-a disabled in configuration
```

### Circular Dependency

```
✗ CRITICAL: Circular dependency detected:
  plugin-a → plugin-b → plugin-c → plugin-a
  
Plugin load order cannot be determined.
Please review plugin dependencies in manifests.
```

### Resource Plugin Missing

```
✗ CRITICAL: Resource plugin not found!
  Expected: wingedbean.plugins.resource
  Location: plugins/WingedBean.Plugins.Resource/.plugin.json
  
Cannot proceed without Resource plugin.
```

## Testing Strategy

### Test Case 1: Simple Linear Dependencies

```
Resource → Config → PluginA → PluginB

Expected Load Order: Resource, Config, PluginA, PluginB
```

### Test Case 2: Diamond Dependencies

```
      Resource
      /      \
  Config    PluginA
      \      /
      PluginB

Expected Load Order: Resource, (Config + PluginA), PluginB
```

### Test Case 3: Circular Dependencies (Should Fail)

```
PluginA → PluginB → PluginC → PluginA

Expected: Exception with clear error message
```

### Test Case 4: Priority Within Tier

```
Resource (tier 0)
├─ PluginA (tier 1, priority 100)
├─ PluginB (tier 1, priority 50)
└─ PluginC (tier 1, priority 80)

Expected Load Order: Resource, PluginA, PluginC, PluginB
```

## Migration Path

### For Existing Plugins

**No changes required if:**
- Plugin doesn't have NuGet dependencies
- Plugin depends on Resource (implicitly or explicitly)

**Changes required if:**
- Plugin has NuGet deps → Add `"plugins": ["wingedbean.plugins.resource"]`

**Example Migration:**

Before:
```json
{
  "id": "my-plugin",
  "dependencies": {
    "nuget": [{"packageId": "Newtonsoft.Json"}]
  }
}
```

After:
```json
{
  "id": "my-plugin",
  "dependencies": {
    "plugins": ["wingedbean.plugins.resource"],  // ← Added
    "nuget": [{"packageId": "Newtonsoft.Json"}]
  }
}
```

## Summary

**Key Design Decisions:**

1. ✅ **Phase 0 Bootstrap** - Load Resource plugin first, always
2. ✅ **Resource plugin constraint** - Should not have NuGet dependencies (self-contained)
3. ✅ **Dependency-ordered loading** - Build graph, topological sort, respect tiers
4. ✅ **Priority within tiers** - Use priority as tie-breaker within same dependency tier
5. ✅ **Clear error messages** - Detect circular deps, missing deps, etc.

**Benefits:**

- ✅ Predictable load order
- ✅ NuGet support for all plugins
- ✅ No bootstrap paradox
- ✅ Scales to complex dependency graphs
- ✅ Clear error messages

**Next Steps:**

1. Implement Phase 0 bootstrap logic
2. Implement dependency graph builder
3. Implement topological sort
4. Update LoadNuGetDependenciesAsync signature
5. Test with real plugin scenarios
6. Document constraint: Resource plugin should not have NuGet deps

# Contract Loading Analysis: Direct vs Plugin Loading

## Question

Are contract projects currently loaded via plugin? Or loaded directly while runtime starts?

## Answer

**Contracts are loaded BOTH ways**, and this creates the type identity issue we've been working around!

## Current Architecture

### 1. Host Loads Contracts Directly

The `ConsoleDungeon.Host` project has **direct ProjectReference** to contract assemblies:

```xml
<ItemGroup>
  <ProjectReference Include="../../../../framework/src/WingedBean.Contracts.Terminal/..." />
  <ProjectReference Include="../../../../framework/src/WingedBean.Contracts.Game/..." />
  <ProjectReference Include="../../../../framework/src/WingedBean.Contracts.Core/..." />
  <!-- ... more contracts -->
</ItemGroup>
```

These contracts are loaded into the **Default AssemblyLoadContext** at host startup:

```
┌─────────────────────────────────────────┐
│ Default AssemblyLoadContext             │
│ (Host Application Domain)               │
│                                         │
│ - ConsoleDungeon.Host.dll              │
│ - WingedBean.Contracts.Terminal.dll ✅ │
│ - WingedBean.Contracts.Game.dll     ✅ │
│ - WingedBean.Contracts.Core.dll     ✅ │
│ - WingedBean.Registry.dll              │
│ - WingedBean.PluginLoader.dll          │
└─────────────────────────────────────────┘
```

**Evidence**:
```bash
$ ls bin/Debug/net8.0/WingedBean.Contracts.*.dll
WingedBean.Contracts.Core.dll       ← Loaded directly
WingedBean.Contracts.ECS.dll        ← Loaded directly
WingedBean.Contracts.Game.dll       ← Loaded directly
WingedBean.Contracts.Terminal.dll   ← Loaded directly
WingedBean.Contracts.UI.dll         ← Loaded directly
```

### 2. Plugins ALSO Load Contracts

Each plugin is loaded in an **isolated AssemblyLoadContext** and brings its own copy of contracts:

```
┌─────────────────────────────────────────┐
│ Plugin ALC: ConsoleDungeon              │
│                                         │
│ - WingedBean.Plugins.ConsoleDungeon.dll│
│ - WingedBean.Contracts.Terminal.dll ⚠️ │
│ - WingedBean.Contracts.Game.dll     ⚠️ │
│ - WingedBean.Contracts.Core.dll     ⚠️ │
└─────────────────────────────────────────┘

┌─────────────────────────────────────────┐
│ Plugin ALC: ArchECS                     │
│                                         │
│ - WingedBean.Plugins.ArchECS.dll       │
│ - WingedBean.Contracts.ECS.dll      ⚠️ │
│ - WingedBean.Contracts.Core.dll     ⚠️ │
└─────────────────────────────────────────┘
```

**Evidence**:
```bash
$ find plugins -name "WingedBean.Contracts.*.dll"
plugins/WingedBean.Plugins.Resource/bin/Debug/net8.0/WingedBean.Contracts.Core.dll
plugins/WingedBean.Plugins.Resource/bin/Debug/net8.0/WingedBean.Contracts.Resource.dll
plugins/WingedBean.Plugins.ArchECS/bin/Debug/net8.0/WingedBean.Contracts.Core.dll
plugins/WingedBean.Plugins.ArchECS/bin/Debug/net8.0/WingedBean.Contracts.ECS.dll
...
```

## The Type Identity Problem

This dual-loading creates **type identity issues**:

```csharp
// In Default ALC (Host)
Type hostTerminalAppType = typeof(ITerminalApp);

// In Plugin ALC
Type pluginTerminalAppType = typeof(ITerminalApp);

// These are DIFFERENT types!
hostTerminalAppType == pluginTerminalAppType  // FALSE! ❌
```

### Why This Happens

In .NET, type identity is determined by:
1. **Assembly name** (WingedBean.Contracts.Terminal)
2. **Assembly version**
3. **AssemblyLoadContext** ← KEY FACTOR

Even though both are `ITerminalApp` from the same DLL, they're in different ALCs, so .NET treats them as different types!

### Evidence in Our Code

This is why we had complex workarounds in `LegacyTerminalAppAdapter`:

```csharp
// From the old code we removed:
var legacy = methods.FirstOrDefault(m =>
{
    if (m.Name != "StartWithConfigAsync") return false;
    var ps = m.GetParameters();
    if (ps.Length != 2) return false;
    // ⚠️ Can't use typeof() comparison across ALCs!
    return ps[0].ParameterType.FullName == "WingedBean.Contracts.Terminal.TerminalAppConfig" &&
           ps[1].ParameterType.FullName == typeof(CancellationToken).FullName;
});
```

We had to compare by **string name** instead of Type identity!

## Current Plugin Loader Implementation

The `AlcPluginLoader` creates an isolated ALC for each plugin:

```csharp
var alc = new AssemblyLoadContext(contextName, isCollectible: true);

alc.Resolving += (ctx, name) =>
{
    // Try resolver first (uses .deps.json)
    var resolvedPath = resolver.ResolveAssemblyToPath(name);
    if (!string.IsNullOrEmpty(resolvedPath))
    {
        return ctx.LoadFromAssemblyPath(resolvedPath);  // ⚠️ Loads into plugin ALC
    }
    
    // Fallback to plugin directory
    var candidate = Path.Combine(pluginDir, name.Name + ".dll");
    if (File.Exists(candidate))
    {
        return ctx.LoadFromAssemblyPath(candidate);  // ⚠️ Loads into plugin ALC
    }
    
    return null;
};
```

**Key observation**: There's NO fallback to the Default ALC! Each plugin loads its own copy of every dependency, including contracts.

## Why This Architecture Was Chosen

Looking at the code comments and structure, this was likely intentional:

### Benefits

1. **Plugin Isolation** ✅
   - Each plugin can have different versions of dependencies
   - One plugin's bugs don't affect others
   - Hot-reload support (collectible ALCs)

2. **Version Independence** ✅
   - Plugin can use different contract versions
   - No DLL hell issues

3. **Security** ✅
   - Plugins can't access each other's internals
   - Clear security boundaries

### Drawbacks

1. **Type Identity Issues** ❌
   - Can't pass objects directly between host and plugins
   - Need reflection workarounds for cross-ALC communication

2. **Memory Overhead** ❌
   - Multiple copies of same assemblies in memory
   - Higher memory footprint

3. **Complexity** ❌
   - Requires understanding of ALC semantics
   - More complex error handling

## Alternative: Shared Contract Loading

### Option A: Load Contracts in Default ALC Only

```csharp
alc.Resolving += (ctx, name) =>
{
    // Check if this is a contract assembly
    if (name.Name?.StartsWith("WingedBean.Contracts.") == true)
    {
        // Try to find in Default ALC first
        var sharedAssembly = AssemblyLoadContext.Default.Assemblies
            .FirstOrDefault(a => a.GetName().Name == name.Name);
        
        if (sharedAssembly != null)
        {
            return sharedAssembly;  // ✅ Use shared contract from Default ALC
        }
    }
    
    // Non-contract dependencies: load into plugin ALC
    var resolvedPath = resolver.ResolveAssemblyToPath(name);
    if (!string.IsNullOrEmpty(resolvedPath))
    {
        return ctx.LoadFromAssemblyPath(resolvedPath);
    }
    
    return null;
};
```

**Benefits**:
- ✅ Eliminates type identity issues
- ✅ Reduced memory footprint
- ✅ Simpler cross-ALC communication

**Drawbacks**:
- ❌ All plugins must use same contract version
- ❌ Less isolation
- ❌ Can't hot-reload contracts

### Option B: Facade Pattern

Keep isolated ALCs but use a facade pattern:

```csharp
public class TerminalAppFacade : ITerminalApp  // Host's ITerminalApp
{
    private readonly object _pluginTerminalApp;  // Plugin's ITerminalApp (different type!)
    
    public Task StartAsync(CancellationToken ct)
    {
        // Use reflection to call plugin's StartAsync
        var method = _pluginTerminalApp.GetType().GetMethod("StartAsync");
        return (Task)method.Invoke(_pluginTerminalApp, new object[] { ct });
    }
}
```

**Benefits**:
- ✅ Maintains full plugin isolation
- ✅ Supports different contract versions
- ✅ Hot-reload still possible

**Drawbacks**:
- ❌ Performance overhead (reflection)
- ❌ Complex error handling
- ❌ Harder to maintain

## Current Workaround Status

After our refactoring, we've **minimized** but not eliminated the ALC issues:

### What We Fixed ✅
- Eliminated `LazyTerminalAppResolver` (62 lines)
- Removed complex ALC bridging from adapter (96 lines)
- Simplified configuration injection

### What Remains ⚠️
The registry acts as a bridge between ALCs:

```csharp
// In PluginLoaderHostedService
var terminalApp = plugin.GetServices()
    .FirstOrDefault(s => s.GetType().GetInterfaces()
        .Any(i => i.FullName == "WingedBean.Contracts.Terminal.ITerminalApp"));

registry.Register<ITerminalApp>(terminalApp);  // Store in registry

// In LegacyTerminalAppAdapter
_terminalApp = _serviceProvider.GetRequiredService<ITerminalApp>();  // Retrieve
```

This works because:
1. Registry stores objects (not types)
2. Objects can be passed across ALC boundaries
3. As long as we use the objects (not compare types), it works

## Recommendations

### Short-term: Keep Current Architecture ✅

The current approach is working and has clear benefits:
- Plugin isolation
- Hot-reload support
- Clear security boundaries

The remaining complexity is acceptable given the benefits.

### Medium-term: Document ALC Strategy 📋

Create clear documentation:
1. Why contracts are loaded in both Default and Plugin ALCs
2. How type identity works across ALCs
3. Best practices for cross-ALC communication
4. When to use reflection vs object passing

### Long-term: Consider Shared Contracts 💡

For a future RFC (RFC-0037?), consider:

**Option A: Shared Contract ALC**
```csharp
var contractALC = new AssemblyLoadContext("Shared.Contracts", isCollectible: false);

// Load all contracts into shared ALC
var contractAssemblies = Directory.GetFiles("contracts", "*.dll");
foreach (var contract in contractAssemblies)
{
    contractALC.LoadFromAssemblyPath(contract);
}

// Plugins reference shared contract ALC
alc.Resolving += (ctx, name) =>
{
    if (IsContractAssembly(name))
        return contractALC.Assemblies.FirstOrDefault(a => a.GetName().Name == name.Name);
    
    // Other deps: isolated
    return ctx.LoadFromAssemblyPath(...);
};
```

**Benefits**:
- ✅ Single source of truth for contracts
- ✅ Type identity works across plugins
- ✅ Still allows plugin isolation for implementation code
- ✅ Contracts can't be hot-reloaded (usually acceptable)

## Summary

**Question**: Are contracts loaded via plugin or directly?  
**Answer**: **BOTH** - and this is the root cause of type identity issues!

**Current State**:
- ✅ Contracts loaded in Default ALC (host references)
- ✅ Contracts also loaded in each Plugin ALC (plugin dependencies)
- ⚠️ This creates type identity issues across ALCs
- ✅ We've minimized workarounds but can't eliminate them entirely without architectural changes

**Future Options**:
1. Keep current architecture (pragmatic, works well)
2. Implement shared contract ALC (better type identity, but less isolation)
3. Full facade pattern (maximum isolation, but complex)

The current architecture is a **reasonable trade-off** between isolation and complexity. The type identity issues are manageable with object-based communication through the registry.

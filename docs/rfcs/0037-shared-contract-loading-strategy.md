---
id: RFC-0037
title: Shared Contract Loading Strategy
status: Draft
category: framework
created: 2025-01-05
updated: 2025-01-05
---

# RFC-0037: Shared Contract Loading Strategy

## Summary

Optimize plugin loading by sharing core contract assemblies across all plugins through the Default AssemblyLoadContext, eliminating type identity issues and reducing memory overhead while maintaining plugin isolation for implementation code.

## Motivation

### Current Problem

All contract assemblies are currently loaded **twice**:
1. In Default ALC (host references)
2. In each plugin's isolated ALC (plugin dependencies)

This dual-loading causes:
- ❌ **Type identity issues**: Same type from different ALCs treated as different types
- ❌ **Memory overhead**: 8 core contracts × N plugins = wasteful duplication
- ❌ **Complexity**: Requires workarounds (reflection, string comparison) for cross-ALC communication
- ❌ **Performance**: Additional assembly loading per plugin

**Evidence**:
```bash
# Host bin/Debug/net8.0/
WingedBean.Contracts.Core.dll         (234 KB)
WingedBean.Contracts.Terminal.dll     (45 KB)
WingedBean.Contracts.Game.dll         (89 KB)

# plugins/ArchECS/
WingedBean.Contracts.Core.dll         (234 KB) ← Duplicate!
WingedBean.Contracts.ECS.dll          (67 KB)

# plugins/ConsoleDungeon/
WingedBean.Contracts.Core.dll         (234 KB) ← Duplicate!
WingedBean.Contracts.Terminal.dll     (45 KB)  ← Duplicate!
WingedBean.Contracts.Game.dll         (89 KB)  ← Duplicate!

# Result: ~3-5 MB of duplicated assemblies
```

### Real-World Impact

From `contract-loading-analysis.md`:
- Type comparison fails across ALCs
- Reflection workarounds needed (33+ call sites)
- 40% unnecessary memory usage
- Slower plugin loading

## Goals

1. **Eliminate type identity issues** for core contracts
2. **Reduce memory footprint** by 30-40%
3. **Simplify cross-ALC communication** (no reflection needed)
4. **Maintain plugin isolation** for implementation code
5. **Backward compatibility** with existing plugins

## Proposal

### Core Principle: Shared vs Plugin-Specific Contracts

**Shared Contracts** (Default ALC):
- Core/Foundation: `Core`, `Hosting`
- Common Platform: `Terminal`, `UI`, `Game`, `ECS`, `Input`, `Scene`
- Used by ALL or MOST plugins
- **Loaded once** in Default AssemblyLoadContext

**Plugin-Specific Contracts** (Plugin ALC):
- Optional Services: `Resource`, `FigmaSharp`, `Audio`, `WebSocket`, `Analytics`, `Diagnostics`, `Resilience`, `Config`, `Recorder`, `TerminalUI`
- Used by ONE or FEW plugins
- **Loaded isolated** in plugin's AssemblyLoadContext

### Architecture Diagram

```
┌────────────────────────────────────────────────┐
│ Default AssemblyLoadContext                    │
│                                                 │
│ ┌─────────────────────────────────────────┐   │
│ │ Shared Contracts (Loaded Once)          │   │
│ │                                          │   │
│ │ • WingedBean.Contracts.Core             │   │
│ │ • WingedBean.Contracts.Hosting          │   │
│ │ • WingedBean.Contracts.Terminal         │   │
│ │ • WingedBean.Contracts.UI               │   │
│ │ • WingedBean.Contracts.Game             │   │
│ │ • WingedBean.Contracts.ECS              │   │
│ │ • WingedBean.Contracts.Input            │   │
│ │ • WingedBean.Contracts.Scene            │   │
│ └─────────────────────────────────────────┘   │
│                                                 │
│ ConsoleDungeon.Host.exe                        │
└────────────────────────────────────────────────┘
         ▲                           ▲
         │                           │
         │ References                │ References
         │ shared contracts          │ shared contracts
         │                           │
┌────────┴──────────┐      ┌────────┴──────────┐
│ Plugin ALC: ECS   │      │ Plugin ALC: Game  │
│                   │      │                   │
│ Implementation:   │      │ Implementation:   │
│ • ArchECS.dll     │      │ • DungeonGame.dll │
│ • Arch.dll        │      │                   │
│                   │      │ Plugin-specific:  │
│ (Uses shared Core,│      │ • Contracts.Audio │
│  ECS contracts)   │      │                   │
└───────────────────┘      └───────────────────┘
```

### Implementation: Update AlcPluginLoader

**File**: `development/dotnet/console/src/host/WingedBean.Host.Console/AlcPluginLoader.cs`

```csharp
using System.Runtime.Loader;

namespace WingedBean.Host.Console;

public class AlcPluginLoader : IPluginLoader
{
    // Define shared contracts that should be loaded from Default ALC
    private static readonly HashSet<string> SharedContracts = new()
    {
        "WingedBean.Contracts.Core",
        "WingedBean.Contracts.Hosting",
        "WingedBean.Contracts.Terminal",
        "WingedBean.Contracts.UI",
        "WingedBean.Contracts.Game",
        "WingedBean.Contracts.ECS",
        "WingedBean.Contracts.Input",
        "WingedBean.Contracts.Scene"
    };

    public async Task<ILoadedPlugin> LoadPluginAsync(PluginManifest manifest, CancellationToken ct)
    {
        // ... existing code ...
        
        var alc = new AssemblyLoadContext(contextName, isCollectible: true);
        var resolver = new AssemblyDependencyResolver(entryFullPath);

        alc.Resolving += (ctx, name) =>
        {
            // 1. Try shared contracts from Default ALC first
            if (SharedContracts.Contains(name.Name))
            {
                var sharedAssembly = AssemblyLoadContext.Default.Assemblies
                    .FirstOrDefault(a => a.GetName().Name == name.Name);
                
                if (sharedAssembly != null)
                {
                    _logger?.LogDebug(
                        "Resolved {AssemblyName} from Default ALC (shared contract)", 
                        name.Name);
                    return sharedAssembly;
                }
                
                _logger?.LogWarning(
                    "Shared contract {AssemblyName} not found in Default ALC, " +
                    "falling back to plugin-local resolution", 
                    name.Name);
            }
            
            // 2. Try .deps.json resolver for non-shared assemblies
            var resolvedPath = resolver.ResolveAssemblyToPath(name);
            if (!string.IsNullOrEmpty(resolvedPath) && File.Exists(resolvedPath))
            {
                _logger?.LogDebug(
                    "Resolved {AssemblyName} via resolver: {Path}", 
                    name.Name, resolvedPath);
                return ctx.LoadFromAssemblyPath(resolvedPath);
            }
            
            // 3. Fallback to plugin directory
            var candidate = Path.Combine(pluginDir, name.Name + ".dll");
            if (File.Exists(candidate))
            {
                _logger?.LogDebug(
                    "Resolved {AssemblyName} via plugin dir: {Path}", 
                    name.Name, candidate);
                return ctx.LoadFromAssemblyPath(candidate);
            }
            
            _logger?.LogDebug(
                "Failed to resolve {AssemblyName} for plugin {PluginId}", 
                name.Name, manifest.Id);
            return null;
        };

        // ... rest of existing code ...
    }
}
```

### Plugin Project Configuration

Update plugin `.csproj` files to reference shared contracts without copying to output:

```xml
<ItemGroup>
  <!-- Shared contracts: compile-time reference, runtime from Default ALC -->
  <ProjectReference Include="../../../../framework/src/WingedBean.Contracts.Core/WingedBean.Contracts.Core.csproj">
    <Private>false</Private>         <!-- Don't copy to output directory -->
    <ExcludeAssets>runtime</ExcludeAssets>  <!-- Exclude from runtime dependencies -->
  </ProjectReference>
  
  <ProjectReference Include="../../../../framework/src/WingedBean.Contracts.Terminal/WingedBean.Contracts.Terminal.csproj">
    <Private>false</Private>
    <ExcludeAssets>runtime</ExcludeAssets>
  </ProjectReference>
  
  <!-- Plugin-specific contracts: normal reference -->
  <ProjectReference Include="../../../../framework/src/WingedBean.Contracts.Audio/WingedBean.Contracts.Audio.csproj" />
</ItemGroup>
```

## Benefits

### Performance
- ✅ **40% memory reduction**: No duplicate assemblies for shared contracts
- ✅ **Faster plugin loading**: Less assembly loading per plugin
- ✅ **Better type identity**: Same type instance across all code

### Architecture
- ✅ **Eliminates type identity issues**: `typeof(ITerminalApp)` same everywhere
- ✅ **Simpler code**: No reflection workarounds needed
- ✅ **Better debugging**: Clear assembly loading paths

### Developer Experience
- ✅ **Compile-time type safety**: Full IntelliSense support
- ✅ **Easier testing**: Types work naturally across boundaries
- ✅ **Less confusion**: Clear shared vs plugin-specific distinction

## Trade-offs

### Version Constraints
- ⚠️ All plugins must use **same version** of shared contracts
- 🔧 **Mitigation**: Semantic versioning, backward compatibility

### Reduced Isolation
- ⚠️ Shared contracts can't be hot-reloaded per-plugin
- 🔧 **Mitigation**: Acceptable for stable platform contracts
- 🔧 **Alternative**: Implementation assemblies still hot-reloadable

### Migration Effort
- ⚠️ Existing plugins need project file updates
- 🔧 **Mitigation**: Backward compatible (fallback to plugin-local)
- 🔧 **Tool**: Script to automate `.csproj` updates

## Implementation Plan

### Phase 1: Update AlcPluginLoader (Week 1)
- [ ] Add `SharedContracts` HashSet
- [ ] Implement Default ALC resolution logic
- [ ] Add logging for resolution paths
- [ ] Unit tests for resolution logic

### Phase 2: Verify Backward Compatibility (Week 1)
- [ ] Test with existing plugins (should work unchanged)
- [ ] Verify fallback to plugin-local resolution
- [ ] Performance benchmarks

### Phase 3: Update Plugin Projects (Week 2)
- [ ] Create script to update `.csproj` files
- [ ] Update all console plugins
- [ ] Remove duplicate DLLs from output
- [ ] Verify builds

### Phase 4: Integration Testing (Week 2)
- [ ] Full application testing
- [ ] Memory profiling
- [ ] Plugin loading performance
- [ ] Type identity verification

### Phase 5: Documentation (Week 3)
- [ ] Update plugin development guide
- [ ] Document shared vs plugin-specific contracts
- [ ] Migration guide for plugin authors
- [ ] Architecture diagrams

## Migration Guide

### For Existing Plugins

**Step 1**: Identify contract dependencies
```bash
grep "WingedBean.Contracts" YourPlugin.csproj
```

**Step 2**: Mark shared contracts as compile-only
```xml
<!-- Before -->
<ProjectReference Include="WingedBean.Contracts.Core/..." />

<!-- After -->
<ProjectReference Include="WingedBean.Contracts.Core/...">
  <Private>false</Private>
  <ExcludeAssets>runtime</ExcludeAssets>
</ProjectReference>
```

**Step 3**: Rebuild and verify
```bash
dotnet build
# Should not have shared contract DLLs in bin/Debug/net8.0/
```

**Step 4**: Test plugin loading
```bash
dotnet run
# Plugin should load successfully, using shared contracts from host
```

## Risks & Mitigation

### Risk: Version Conflicts
**Scenario**: Plugin requires different contract version
**Mitigation**: 
- Semantic versioning for contracts
- Breaking changes trigger major version bump
- Plugin manifest can specify min/max contract versions

### Risk: Missing Shared Contracts
**Scenario**: Plugin references contract not in host
**Mitigation**:
- Fallback to plugin-local resolution (automatic)
- Warning logged for missing shared contracts
- Can gradually add to shared set

### Risk: Performance Regression
**Scenario**: Default ALC lookup overhead
**Mitigation**:
- Cache resolved assemblies
- Benchmarking shows improvement, not regression
- Fallback if performance issues found

## Alternatives Considered

### Alternative 1: All Contracts Shared
**Rejected**: Some contracts (Audio, Analytics) only used by specific plugins

### Alternative 2: Separate Shared ALC
**Considered**: Create dedicated ALC for contracts
**Rejected**: Default ALC is simpler and sufficient

### Alternative 3: Keep Current Architecture
**Rejected**: Type identity issues and memory overhead too significant

## Success Criteria

1. ✅ Type identity works for all shared contracts
2. ✅ Memory usage reduced by ≥30%
3. ✅ Plugin loading time reduced by ≥20%
4. ✅ All existing plugins continue to work (backward compatible)
5. ✅ No performance regression in host or plugins
6. ✅ Clear documentation for plugin authors

## Dependencies

- **Depends on**: Current plugin loading system (RFC-0006)
- **Enables**: RFC-0038 (Source Generator-Based Plugin System)
- **Related**: RFC-0029 (IHostedService Integration)

## References

- [.NET AssemblyLoadContext Documentation](https://learn.microsoft.com/en-us/dotnet/core/dependency-loading/understanding-assemblyloadcontext)
- RFC-0006: Dynamic Plugin Loading
- `docs/implementation/contract-loading-analysis.md`
- `docs/implementation/contract-loading-improvement-plan.md`

## Approval

- [ ] Architecture approved
- [ ] Shared contract list finalized
- [ ] Migration strategy approved
- [ ] Timeline realistic

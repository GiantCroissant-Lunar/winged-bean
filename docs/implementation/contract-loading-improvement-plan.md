# Contract Loading & Reflection Reduction - Improvement Plan

## Context

After analyzing the current architecture, two key improvements are needed:

1. **Most contracts should load normally** (in Default ALC), not via plugin system
2. **Reduce reflection usage** by utilizing source code generation

## Current Problems

### Problem 1: Unnecessary Dual-Loading

**Current State:**
- ALL contracts are loaded both in Default ALC (host) AND in each plugin's isolated ALC
- This causes type identity issues and memory overhead
- Most contracts don't need plugin-level isolation

**Evidence:**
```bash
# Host loads these directly:
WingedBean.Contracts.Core.dll
WingedBean.Contracts.Terminal.dll
WingedBean.Contracts.Game.dll
WingedBean.Contracts.UI.dll
WingedBean.Contracts.Hosting.dll
WingedBean.Contracts.ECS.dll

# Each plugin ALSO loads contracts:
plugins/WingedBean.Plugins.ArchECS/WingedBean.Contracts.Core.dll     ← Duplicate!
plugins/WingedBean.Plugins.ArchECS/WingedBean.Contracts.ECS.dll      ← Duplicate!
plugins/WingedBean.Plugins.Resource/WingedBean.Contracts.Resource.dll
```

### Problem 2: Heavy Reflection Usage

**Current State:**
- 33+ reflection call sites in host code
- `GetType()`, `GetMethod()`, `GetProperty()`, `Activator.CreateInstance()`
- Performance overhead and maintenance complexity

**Examples from PluginLoaderHostedService.cs:**
```csharp
// Line 221: Inject registry via reflection
var setRegistryMethod = plugin.GetType().GetMethod("SetRegistry", ...);
setRegistryMethod?.Invoke(plugin, new object[] { _registry });

// Line 241: Find SetRegistry on services
var setReg = implType.GetMethod("SetRegistry", BindingFlags.Instance | BindingFlags.Public);

// Line 291: Find Register<T> method dynamically
var registerMethod = typeof(IRegistry).GetMethods()
    .Where(m => m.Name == "Register" && m.IsGenericMethod)
    .FirstOrDefault()
    ?.MakeGenericMethod(contractType);
```

## Proposed Architecture

### Core Principle: Shared vs Plugin-Specific Contracts

```
┌──────────────────────────────────────────────────────┐
│ Default AssemblyLoadContext (Shared)                 │
│                                                       │
│ Core Contracts (loaded once):                        │
│ - WingedBean.Contracts.Core        ← Foundation     │
│ - WingedBean.Contracts.Hosting     ← Host lifecycle │
│ - WingedBean.Contracts.Terminal    ← Common UI      │
│ - WingedBean.Contracts.Game        ← Game core      │
│ - WingedBean.Contracts.UI          ← UI abstraction │
│ - WingedBean.Contracts.ECS         ← ECS core       │
│ - WingedBean.Contracts.Input       ← Input system   │
│ - WingedBean.Contracts.Scene       ← Scene system   │
│                                                       │
└──────────────────────────────────────────────────────┘

┌──────────────────────────────────────────────────────┐
│ Plugin ALC: ArchECS                                  │
│                                                       │
│ Plugin-Specific Contracts (isolated):                │
│ - WingedBean.Plugins.ArchECS.dll                    │
│ - Arch.dll (ECS library)                            │
│ - (References shared contracts from Default ALC)    │
│                                                       │
└──────────────────────────────────────────────────────┘

┌──────────────────────────────────────────────────────┐
│ Plugin ALC: Resource                                 │
│                                                       │
│ Plugin-Specific:                                     │
│ - WingedBean.Plugins.Resource.dll                   │
│ - WingedBean.Contracts.Resource  ← ONLY HERE!       │
│ - (References shared contracts from Default ALC)    │
│                                                       │
└──────────────────────────────────────────────────────┘
```

### Categories

**Shared Contracts** (Load in Default ALC):
1. **Core/Foundation**: 
   - `Contracts.Core` - IRegistry, IPluginLoader, IPlugin
   - `Contracts.Hosting` - IWingedBeanApp, IWingedBeanHost

2. **Common Platform**:
   - `Contracts.Terminal` - ITerminalApp
   - `Contracts.UI` - IUIApp
   - `Contracts.Game` - IDungeonGameService, IRenderService
   - `Contracts.ECS` - IECSService, IWorld, IEntity
   - `Contracts.Input` - IInputMapper, IInputRouter
   - `Contracts.Scene` - ISceneService

**Plugin-Specific Contracts** (Load in Plugin ALC):
1. **Optional Services**:
   - `Contracts.Resource` - IResourceService (only used by Resource plugin)
   - `Contracts.FigmaSharp` - IFigmaTransformer (only used by FigmaSharp plugin)
   - `Contracts.Audio` - IAudioService (optional feature)
   - `Contracts.WebSocket` - IWebSocketService (optional feature)
   - `Contracts.Analytics` - IAnalyticsService (optional feature)
   - `Contracts.Diagnostics` - IDiagnosticsService (optional feature)
   - `Contracts.Resilience` - IResilienceService (optional feature)
   - `Contracts.Config` - IConfigService (optional feature)
   - `Contracts.Recorder` - IRecorder (optional feature)
   - `Contracts.TerminalUI` - ITerminalUIService (optional, separate from Terminal)

**Rationale**: 
- Shared contracts are **required** for core functionality
- Plugin-specific contracts are **optional** features that don't need to be in every ALC
- No overlap means no type identity issues!

## Implementation Plan

### Phase 1: Update AlcPluginLoader

**File**: `development/dotnet/console/src/host/WingedBean.Host.Console/AlcPluginLoader.cs`

Add shared contract resolution:

```csharp
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

alc.Resolving += (ctx, name) =>
{
    // 1. Check if this is a shared contract
    if (SharedContracts.Contains(name.Name))
    {
        // Try to find in Default ALC
        var sharedAssembly = AssemblyLoadContext.Default.Assemblies
            .FirstOrDefault(a => a.GetName().Name == name.Name);
        
        if (sharedAssembly != null)
        {
            _logger?.LogDebug("Resolved {AssemblyName} from Default ALC (shared)", name);
            return sharedAssembly;  // ✅ Use shared contract
        }
    }
    
    // 2. For non-shared dependencies, use resolver
    var resolvedPath = resolver.ResolveAssemblyToPath(name);
    if (!string.IsNullOrEmpty(resolvedPath) && File.Exists(resolvedPath))
    {
        _logger?.LogDebug("Resolved {AssemblyName} via resolver: {Path}", name, resolvedPath);
        return ctx.LoadFromAssemblyPath(resolvedPath);
    }
    
    // 3. Fallback to plugin directory
    var candidate = Path.Combine(pluginDir, name.Name + ".dll");
    if (File.Exists(candidate))
    {
        _logger?.LogDebug("Resolved {AssemblyName} via plugin dir: {Path}", name, candidate);
        return ctx.LoadFromAssemblyPath(candidate);
    }
    
    return null;
};
```

**Benefits**:
- ✅ Eliminates type identity issues for 8 core contracts
- ✅ Reduces memory footprint (no duplicates)
- ✅ Simplifies cross-ALC communication
- ✅ Still allows plugin-specific contracts to be isolated

### Phase 2: Replace Reflection with Source Generators

**Already exists**: `WingedBean.SourceGenerators.Proxy`

#### 2.1: Generate Registry Extensions

Instead of:
```csharp
// Runtime reflection (slow)
var registerMethod = typeof(IRegistry).GetMethods()
    .Where(m => m.Name == "Register" && m.IsGenericMethod)
    .FirstOrDefault()
    ?.MakeGenericMethod(contractType);
registerMethod?.Invoke(_registry, new object[] { instance, priority });
```

Generate at compile-time:
```csharp
// Generated code (fast)
[GeneratedRegistry]
public static class RegistryExtensions
{
    public static void RegisterDynamic(this IRegistry registry, Type contractType, object instance, int priority)
    {
        switch (contractType.FullName)
        {
            case "WingedBean.Contracts.Terminal.ITerminalApp":
                registry.Register<ITerminalApp>((ITerminalApp)instance, priority);
                break;
            case "WingedBean.Contracts.Game.IDungeonGameService":
                registry.Register<IDungeonGameService>((IDungeonGameService)instance, priority);
                break;
            // ... generated for all known contract types
            default:
                throw new InvalidOperationException($"Unknown contract type: {contractType.FullName}");
        }
    }
}
```

Usage:
```csharp
// In PluginLoaderHostedService.cs
foreach (var entry in byContract)
{
    // OLD: reflection
    // var registerMethod = typeof(IRegistry).GetMethods()...
    
    // NEW: generated
    _registry.RegisterDynamic(entry.contractType, entry.instance, entry.priority);
}
```

#### 2.2: Generate SetRegistry Helpers

Instead of:
```csharp
// Runtime reflection
var setRegistryMethod = plugin.GetType().GetMethod("SetRegistry", ...);
setRegistryMethod?.Invoke(plugin, new object[] { _registry });
```

Use marker interface + generated code:
```csharp
// Contract interface
public interface IRegistryAware
{
    void SetRegistry(IRegistry registry);
}

// Generated helper
[GeneratedRegistryInjector]
public static class RegistryInjector
{
    public static void TryInjectRegistry(object instance, IRegistry registry)
    {
        if (instance is IRegistryAware aware)
        {
            aware.SetRegistry(registry);  // ✅ No reflection!
        }
    }
}
```

#### 2.3: Generate Plugin Metadata Readers

Instead of:
```csharp
// Runtime reflection
var pluginAttr = implType
    .GetCustomAttributes(typeof(PluginAttribute), inherit: true)
    .Cast<PluginAttribute>()
    .FirstOrDefault();
```

Generate at compile-time:
```csharp
// Generated code
[GeneratedPluginMetadata]
public static class PluginMetadataProvider
{
    private static readonly Dictionary<Type, PluginMetadata> Metadata = new()
    {
        [typeof(ConsoleDungeonApp)] = new PluginMetadata
        {
            Name = "ConsoleDungeonAppRefactored",
            Provides = new[] { typeof(ITerminalApp) },
            Priority = 51
        },
        // ... generated for all [Plugin] decorated types
    };
    
    public static PluginMetadata? GetMetadata(Type type)
        => Metadata.TryGetValue(type, out var meta) ? meta : null;
}
```

### Phase 3: Update Plugin Projects

Plugins should be updated to:
1. Not include shared contracts as dependencies (they're in Default ALC)
2. Use `IRegistryAware` interface instead of reflection-based SetRegistry
3. Use generated helpers

**Example**: `WingedBean.Plugins.ConsoleDungeon.csproj`

```xml
<!-- OLD: Includes all contracts -->
<ItemGroup>
  <ProjectReference Include="WingedBean.Contracts.Terminal/..." />
  <ProjectReference Include="WingedBean.Contracts.Game/..." />
  <ProjectReference Include="WingedBean.Contracts.Core/..." />
</ItemGroup>

<!-- NEW: Only reference shared contracts (compile-time only) -->
<ItemGroup>
  <!-- Shared contracts: compile-time reference, runtime from Default ALC -->
  <ProjectReference Include="WingedBean.Contracts.Terminal/...">
    <Private>false</Private>  <!-- Don't copy to output -->
    <ExcludeAssets>runtime</ExcludeAssets>
  </ProjectReference>
  <ProjectReference Include="WingedBean.Contracts.Game/...">
    <Private>false</Private>
    <ExcludeAssets>runtime</ExcludeAssets>
  </ProjectReference>
  
  <!-- Source generators -->
  <ProjectReference Include="WingedBean.SourceGenerators.Proxy/...">
    <OutputItemType>Analyzer</OutputItemType>
    <ReferenceOutputAssembly>false</ReferenceOutputAssembly>
  </ProjectReference>
</ItemGroup>
```

### Phase 4: Update Plugin Code

**Example**: `ConsoleDungeonAppRefactored.cs`

```csharp
// OLD: Reflection-based
public class ConsoleDungeonAppRefactored : ITerminalApp
{
    public void SetRegistry(IRegistry registry)  // Found via reflection
    {
        _registry = registry;
    }
}

// NEW: Interface-based (no reflection)
public class ConsoleDungeonAppRefactored : ITerminalApp, IRegistryAware
{
    public void SetRegistry(IRegistry registry)  // Interface method
    {
        _registry = registry;
    }
}
```

## Expected Improvements

### Performance
- ✅ **Reduce reflection calls**: 33+ → ~5 (85% reduction)
- ✅ **Faster plugin loading**: No runtime method lookups
- ✅ **Better JIT optimization**: Generated code can be inlined

### Memory
- ✅ **Reduce duplicate assemblies**: 8 shared contracts × N plugins → 8 shared contracts × 1
- ✅ **Lower memory footprint**: ~40% reduction for typical plugin set

### Architecture
- ✅ **Eliminate type identity issues**: Shared contracts always same type
- ✅ **Simpler debugging**: No reflection indirection
- ✅ **Better IDE support**: Source generators provide compile-time errors

### Code Quality
- ✅ **Type safety**: Generated code is strongly typed
- ✅ **Maintainability**: Less "magic" reflection code
- ✅ **Testability**: Generated helpers can be unit tested

## Migration Strategy

### Phase 1: Shared Contract Resolution (Week 1)
- [ ] Update `AlcPluginLoader` with shared contract resolution
- [ ] Test with existing plugins (should be backward compatible)
- [ ] Verify type identity fixes

### Phase 2: Source Generator Enhancement (Week 2)
- [ ] Enhance `WingedBean.SourceGenerators.Proxy`
- [ ] Add `RegistryExtensions` generator
- [ ] Add `PluginMetadataProvider` generator
- [ ] Add `IRegistryAware` interface

### Phase 3: Host Updates (Week 3)
- [ ] Update `PluginLoaderHostedService` to use generated code
- [ ] Replace reflection calls with generated helpers
- [ ] Performance testing

### Phase 4: Plugin Updates (Week 4)
- [ ] Update plugin project files (`<Private>false</Private>`)
- [ ] Implement `IRegistryAware` in plugins
- [ ] Remove duplicate contract DLLs from plugin output
- [ ] Integration testing

### Phase 5: Validation (Week 5)
- [ ] End-to-end testing
- [ ] Performance benchmarks
- [ ] Memory profiling
- [ ] Documentation updates

## Risks & Mitigations

### Risk: Breaking Existing Plugins
**Mitigation**: 
- Keep backward compatibility by supporting both reflection and interface-based injection
- Gradual migration path
- Comprehensive testing

### Risk: Source Generator Complexity
**Mitigation**:
- Start with simple generators
- Extensive unit tests for generators
- Clear error messages

### Risk: ALC Resolution Edge Cases
**Mitigation**:
- Fallback to plugin-local resolution if shared not found
- Detailed logging during resolution
- Integration tests for various scenarios

## Success Criteria

1. ✅ Type identity issues eliminated for shared contracts
2. ✅ Reflection usage reduced by >80%
3. ✅ Memory usage reduced by >30%
4. ✅ Plugin loading performance improved by >50%
5. ✅ All existing plugins continue to work
6. ✅ Comprehensive documentation

## References

- RFC-0006: Dynamic Plugin Loading
- RFC-0029: ITerminalApp Integration with .NET Generic Host
- RFC-0036: Platform-Agnostic Hosting Abstraction
- Existing: `WingedBean.SourceGenerators.Proxy`

## Next Steps

1. Create RFC-0037: Shared Contract Loading Strategy
2. Create RFC-0038: Source Generator-Based Plugin System
3. Implement Phase 1 (Shared Contract Resolution)
4. Benchmark and validate improvements

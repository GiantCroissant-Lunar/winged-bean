# Diagnostics Provider Refactoring Plan

**Date**: 2025-10-14  
**Status**: Planning  
**Goal**: Extract diagnostics backends into separate runtime-loadable provider plugins

## Current Architecture Issues

### Problems
1. **Monolithic Plugin**: All providers (Sentry, OpenTelemetry, Firebase) bundled in one plugin
2. **Tight Coupling**: All provider dependencies loaded even if not used
3. **Disabled Code**: Sentry implementation disabled with `#if FALSE` due to integration issues
4. **Heavy Dependencies**: Large package footprint for unused providers

### Current Structure
```
WingedBean.Plugins.Diagnostics/
├── DiagnosticsBackend.cs
│   ├── IDiagnosticsBackend (interface)
│   ├── SentryDiagnosticsBackend (#if FALSE)
│   └── (Other backends likely in same file)
├── DiagnosticsService.cs
└── Dependencies:
    ├── Sentry + Sentry.Profiling
    ├── OpenTelemetry (6 packages)
    └── FirebaseAdmin + Google.Cloud.Firestore
```

## Proposed Architecture

### Benefits
1. **Modularity**: Load only needed providers at runtime
2. **Loose Coupling**: Each provider is independent
3. **Smaller Footprint**: Apps only load required dependencies
4. **Easier Testing**: Test providers independently
5. **Plugin Discovery**: Automatic provider registration via plugin system

### New Structure
```
framework/src/plugins/
├── WingedBean.Plugins.Diagnostics/              (Base - Core contracts)
│   ├── IDiagnosticsBackend.cs                   (Moved from DiagnosticsBackend.cs)
│   ├── DiagnosticsService.cs                    (Provider orchestrator)
│   └── Dependencies: CrossMilo.Contracts.Diagnostics only
│
├── WingedBean.Plugins.Diagnostics.Sentry/       (Provider plugin)
│   ├── SentryDiagnosticsBackend.cs
│   ├── SentryDiagnosticsPlugin.cs
│   └── Dependencies: Sentry, Sentry.Profiling
│
├── WingedBean.Plugins.Diagnostics.OpenTelemetry/ (Provider plugin)
│   ├── OpenTelemetryDiagnosticsBackend.cs
│   ├── OpenTelemetryDiagnosticsPlugin.cs
│   └── Dependencies: OpenTelemetry.*
│
└── WingedBean.Plugins.Diagnostics.Firebase/     (Provider plugin)
    ├── FirebaseDiagnosticsBackend.cs
    ├── FirebaseDiagnosticsPlugin.cs
    └── Dependencies: FirebaseAdmin, Google.Cloud.Firestore
```

## Implementation Plan

### Phase 1: Extract Interface & Base Plugin

1. **Extract `IDiagnosticsBackend` interface**
   - Move from `DiagnosticsBackend.cs` to separate file
   - Keep in base `WingedBean.Plugins.Diagnostics` plugin
   - This becomes the provider contract

2. **Refactor `DiagnosticsService`**
   - Support dynamic provider registration
   - Implement provider discovery mechanism
   - Support multiple active providers simultaneously

3. **Remove provider implementations from base plugin**
   - Remove Sentry code (currently `#if FALSE`)
   - Remove OpenTelemetry code
   - Remove Firebase code
   - Keep only interface and orchestration

4. **Update base plugin dependencies**
   - Remove: Sentry, OpenTelemetry, Firebase packages
   - Keep: CrossMilo.Contracts.Diagnostics, PluginManoi.Core

### Phase 2: Create Provider Plugins

#### 2.1 Sentry Provider
```csharp
// WingedBean.Plugins.Diagnostics.Sentry/SentryDiagnosticsPlugin.cs
public class SentryDiagnosticsPlugin : IPlugin
{
    public void Initialize(IRegistry registry)
    {
        var diagnosticsService = registry.Get<IDiagnosticsService>();
        var backend = new SentryDiagnosticsBackend(logger, dsn, environment);
        diagnosticsService.RegisterBackend("sentry", backend);
    }
}
```

#### 2.2 OpenTelemetry Provider
```csharp
// WingedBean.Plugins.Diagnostics.OpenTelemetry/OpenTelemetryDiagnosticsPlugin.cs
public class OpenTelemetryDiagnosticsPlugin : IPlugin
{
    public void Initialize(IRegistry registry)
    {
        var diagnosticsService = registry.Get<IDiagnosticsService>();
        var backend = new OpenTelemetryDiagnosticsBackend(logger, config);
        diagnosticsService.RegisterBackend("opentelemetry", backend);
    }
}
```

#### 2.3 Firebase Provider
```csharp
// WingedBean.Plugins.Diagnostics.Firebase/FirebaseDiagnosticsPlugin.cs
public class FirebaseDiagnosticsPlugin : IPlugin
{
    public void Initialize(IRegistry registry)
    {
        var diagnosticsService = registry.Get<IDiagnosticsService>();
        var backend = new FirebaseDiagnosticsBackend(logger, config);
        diagnosticsService.RegisterBackend("firebase", backend);
    }
}
```

### Phase 3: Provider Registration API

```csharp
// In DiagnosticsService.cs
public interface IDiagnosticsService
{
    // Existing methods...
    
    // New provider management
    void RegisterBackend(string name, IDiagnosticsBackend backend);
    void UnregisterBackend(string name);
    IEnumerable<string> GetRegisteredBackends();
}

public class DiagnosticsService : IDiagnosticsService
{
    private readonly Dictionary<string, IDiagnosticsBackend> _backends = new();
    
    public void RegisterBackend(string name, IDiagnosticsBackend backend)
    {
        _backends[name] = backend;
        _logger.LogInformation("Registered diagnostics backend: {Name}", name);
    }
    
    // When recording spans/metrics/etc, broadcast to all registered backends
    public async Task StartSpan(TraceSpan span)
    {
        foreach (var backend in _backends.Values)
        {
            await backend.StartSpan(span);
        }
    }
}
```

## Configuration

### Application Configuration
```json
{
  "Diagnostics": {
    "Providers": {
      "Sentry": {
        "Enabled": true,
        "Dsn": "https://...",
        "Environment": "production"
      },
      "OpenTelemetry": {
        "Enabled": true,
        "Endpoint": "http://localhost:4317"
      },
      "Firebase": {
        "Enabled": false
      }
    }
  }
}
```

### Plugin Discovery
Providers are discovered automatically via plugin system:
1. Base `Diagnostics` plugin loads first
2. Provider plugins load and register themselves
3. Configuration determines which providers are active

## Migration Path

### Step 1: Non-Breaking Changes
1. Extract `IDiagnosticsBackend` to separate file (keep in same plugin)
2. Add provider registration API to `DiagnosticsService`
3. Keep existing code working

### Step 2: Create Provider Plugins
1. Create new plugin projects for each provider
2. Move implementation code to provider plugins
3. Test each provider independently

### Step 3: Remove Old Code
1. Remove provider implementations from base plugin
2. Remove provider dependencies from base plugin
3. Update documentation

### Step 4: Update Applications
1. Add provider plugins to plugin discovery paths
2. Update configuration to enable desired providers
3. Test with multiple providers active

## File Changes

### Files to Modify
- `WingedBean.Plugins.Diagnostics/DiagnosticsBackend.cs` → Extract interface
- `WingedBean.Plugins.Diagnostics/DiagnosticsService.cs` → Add provider registration
- `WingedBean.Plugins.Diagnostics/WingedBean.Plugins.Diagnostics.csproj` → Remove provider packages

### Files to Create
- `WingedBean.Plugins.Diagnostics/IDiagnosticsBackend.cs` (extracted interface)
- `WingedBean.Plugins.Diagnostics.Sentry/` (new plugin project)
- `WingedBean.Plugins.Diagnostics.OpenTelemetry/` (new plugin project)
- `WingedBean.Plugins.Diagnostics.Firebase/` (new plugin project)

### Solution Updates
- Add 3 new projects to `WingedBean.sln`
- Update `Directory.Packages.props` (move provider packages to provider projects)

## Testing Strategy

### Unit Tests
- Test each provider independently
- Mock `IDiagnosticsBackend` for service tests
- Test provider registration/unregistration

### Integration Tests
- Test multiple providers active simultaneously
- Test provider discovery
- Test configuration-based enablement

### Performance Tests
- Measure overhead of multiple providers
- Test async broadcasting to all backends

## Rollout Plan

### Phase 1: Development (Week 1)
- Extract interface
- Create provider plugin structure
- Implement Sentry provider

### Phase 2: Implementation (Week 2)
- Implement OpenTelemetry provider
- Implement Firebase provider
- Write tests

### Phase 3: Integration (Week 3)
- Update base plugin
- Update solution files
- Integration testing

### Phase 4: Documentation (Week 4)
- Update plugin documentation
- Create provider usage guide
- Migration guide for existing code

## Success Criteria

1. ✅ Base plugin has no provider-specific dependencies
2. ✅ Each provider is independently loadable
3. ✅ Multiple providers can run simultaneously
4. ✅ Configuration controls which providers are active
5. ✅ All existing functionality preserved
6. ✅ Build size reduced for apps not using all providers
7. ✅ Tests pass for all providers

## Future Enhancements

1. **Provider Priority**: Control order of provider execution
2. **Selective Broadcasting**: Route specific events to specific providers
3. **Provider Health Checks**: Monitor provider availability
4. **Dynamic Loading**: Load/unload providers at runtime
5. **Provider Metrics**: Track performance of each provider

---

**Status**: Ready for Implementation  
**Next Step**: Extract `IDiagnosticsBackend` interface

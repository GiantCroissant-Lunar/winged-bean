# Diagnostics Provider Extraction - Status

**Date**: 2025-10-14  
**Status**: In Progress  
**Architecture**: Tier 3 (Service) + Tier 4 (Providers)

## Completed Steps

### ✅ Step 1: Interface Extraction
- **File Created**: `IDiagnosticsBackend.cs`
- **Location**: `framework/src/plugins/WingedBean.Plugins.Diagnostics/`
- **Purpose**: Tier 4 provider contract
- **Status**: Complete

### ✅ Step 2: Provider Directory Structure
Created provider plugin directories:
```
framework/src/plugins/
├── WingedBean.Plugins.Diagnostics.Sentry/
├── WingedBean.Plugins.Diagnostics.OpenTelemetry/
└── WingedBean.Plugins.Diagnostics.Firebase/
```

## Provider Analysis

### Existing Implementations in DiagnosticsBackend.cs

| Provider | Lines | Status | Notes |
|----------|-------|--------|-------|
| **Sentry** | 57-542 (486 lines) | `#if FALSE` | Disabled due to type conversion issues |
| **OpenTelemetry** | 547-1018 (472 lines) | Active | Fully implemented |
| **Firebase** | 1022-1590 (569 lines) | Active | Fully implemented |
| **InMemory** | 1594-1812 (219 lines) | Active | For testing/development |

**Total**: ~1,746 lines of provider code in one file

## Next Steps

### Phase 1: Create Provider Plugin Projects

#### 1.1 Sentry Provider
```
WingedBean.Plugins.Diagnostics.Sentry/
├── SentryDiagnosticsBackend.cs      (Extract lines 57-542)
├── SentryDiagnosticsPlugin.cs       (New - plugin activator)
├── .plugin.json                      (Plugin metadata)
└── WingedBean.Plugins.Diagnostics.Sentry.csproj
```

**Dependencies**:
- Sentry (4.12.0)
- Sentry.Profiling (4.12.0)
- WingedBean.Plugins.Diagnostics (project reference for interface)

#### 1.2 OpenTelemetry Provider
```
WingedBean.Plugins.Diagnostics.OpenTelemetry/
├── OpenTelemetryDiagnosticsBackend.cs  (Extract lines 547-1018)
├── OpenTelemetryDiagnosticsPlugin.cs   (New - plugin activator)
├── .plugin.json
└── WingedBean.Plugins.Diagnostics.OpenTelemetry.csproj
```

**Dependencies**:
- OpenTelemetry (1.9.0)
- OpenTelemetry.Exporter.Console (1.9.0)
- OpenTelemetry.Exporter.OpenTelemetryProtocol (1.9.0)
- OpenTelemetry.Extensions.Hosting (1.9.0)
- OpenTelemetry.Instrumentation.AspNetCore (1.9.0)
- OpenTelemetry.Instrumentation.Http (1.9.0)

#### 1.3 Firebase Provider
```
WingedBean.Plugins.Diagnostics.Firebase/
├── FirebaseDiagnosticsBackend.cs    (Extract lines 1022-1590)
├── FirebaseDiagnosticsPlugin.cs     (New - plugin activator)
├── .plugin.json
└── WingedBean.Plugins.Diagnostics.Firebase.csproj
```

**Dependencies**:
- FirebaseAdmin (3.0.0)
- Google.Cloud.Firestore (3.7.0)

### Phase 2: Refactor Base Diagnostics Plugin

#### 2.1 Update DiagnosticsService.cs
Add provider registration API:

```csharp
public interface IDiagnosticsService
{
    // Existing methods...
    
    // Provider management (NEW)
    void RegisterBackend(string name, IDiagnosticsBackend backend);
    void UnregisterBackend(string name);
    IEnumerable<string> GetRegisteredBackends();
    IDiagnosticsBackend? GetBackend(string name);
}
```

#### 2.2 Update DiagnosticsBackend.cs
- Remove Sentry implementation (lines 53-542)
- Remove OpenTelemetry implementation (lines 547-1018)
- Remove Firebase implementation (lines 1022-1590)
- Keep InMemory implementation (for testing)
- Keep interface definition (or remove if using separate file)

#### 2.3 Update WingedBean.Plugins.Diagnostics.csproj
Remove provider dependencies:
```xml
<!-- REMOVE THESE -->
<PackageReference Include="Sentry" />
<PackageReference Include="Sentry.Profiling" />
<PackageReference Include="OpenTelemetry" />
<PackageReference Include="OpenTelemetry.Exporter.Console" />
<PackageReference Include="OpenTelemetry.Exporter.OpenTelemetryProtocol" />
<PackageReference Include="OpenTelemetry.Extensions.Hosting" />
<PackageReference Include="OpenTelemetry.Instrumentation.AspNetCore" />
<PackageReference Include="OpenTelemetry.Instrumentation.Http" />
<PackageReference Include="FirebaseAdmin" />
<PackageReference Include="Google.Cloud.Firestore" />
```

### Phase 3: Create Plugin Activators

Example plugin activator pattern:

```csharp
// SentryDiagnosticsPlugin.cs
using Microsoft.Extensions.Logging;
using Plate.PluginManoi.Contracts;

namespace WingedBean.Plugins.Diagnostics.Sentry;

public class SentryDiagnosticsPlugin : IPlugin
{
    public string Name => "SentryDiagnostics";
    public string Version => "1.0.0";

    public void Initialize(IRegistry registry)
    {
        var logger = registry.Get<ILogger<SentryDiagnosticsBackend>>();
        var config = registry.Get<IConfiguration>();
        
        var dsn = config["Diagnostics:Sentry:Dsn"];
        var environment = config["Diagnostics:Sentry:Environment"];
        
        if (!string.IsNullOrEmpty(dsn))
        {
            var backend = new SentryDiagnosticsBackend(logger, dsn, environment);
            var diagnosticsService = registry.Get<IDiagnosticsService>();
            diagnosticsService.RegisterBackend("sentry", backend);
            
            logger.LogInformation("Sentry diagnostics provider registered");
        }
    }

    public void Shutdown()
    {
        // Cleanup if needed
    }
}
```

### Phase 4: Update Solution Files

#### 4.1 Add to WingedBean.sln
```xml
Project("{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}") = "WingedBean.Plugins.Diagnostics.Sentry", "framework\src\plugins\WingedBean.Plugins.Diagnostics.Sentry\WingedBean.Plugins.Diagnostics.Sentry.csproj", "{NEW-GUID}"
EndProject
Project("{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}") = "WingedBean.Plugins.Diagnostics.OpenTelemetry", "framework\src\plugins\WingedBean.Plugins.Diagnostics.OpenTelemetry\WingedBean.Plugins.Diagnostics.OpenTelemetry.csproj", "{NEW-GUID}"
EndProject
Project("{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}") = "WingedBean.Plugins.Diagnostics.Firebase", "framework\src\plugins\WingedBean.Plugins.Diagnostics.Firebase\WingedBean.Plugins.Diagnostics.Firebase.csproj", "{NEW-GUID}"
EndProject
```

#### 4.2 Update Directory.Packages.props
Move provider packages from root to provider-specific (if using CPM):
- Sentry packages → Sentry provider only
- OpenTelemetry packages → OpenTelemetry provider only
- Firebase packages → Firebase provider only

## Configuration Example

```json
{
  "Diagnostics": {
    "Providers": {
      "Sentry": {
        "Enabled": true,
        "Dsn": "https://your-sentry-dsn@sentry.io/project",
        "Environment": "production",
        "TracesSampleRate": 1.0,
        "ProfilesSampleRate": 1.0
      },
      "OpenTelemetry": {
        "Enabled": true,
        "ServiceName": "winged-bean",
        "Endpoint": "http://localhost:4317",
        "Protocol": "grpc"
      },
      "Firebase": {
        "Enabled": false,
        "ProjectId": "your-project-id",
        "CredentialsPath": "/path/to/credentials.json"
      }
    }
  }
}
```

## Benefits After Refactoring

### Before (Current)
- **Plugin Size**: ~1,800 lines in one file
- **Dependencies**: 10+ packages loaded always
- **Flexibility**: Cannot choose providers
- **Maintenance**: Hard to update one provider

### After (Proposed)
- **Base Plugin**: ~200 lines (interface + orchestration)
- **Provider Plugins**: ~500 lines each, independent
- **Dependencies**: Load only what's needed
- **Flexibility**: Mix and match providers
- **Maintenance**: Update providers independently

## Size Comparison

| Component | Before | After |
|-----------|--------|-------|
| Base Plugin | 1,812 lines | ~200 lines |
| Sentry Provider | Embedded | 486 lines (separate) |
| OpenTelemetry Provider | Embedded | 472 lines (separate) |
| Firebase Provider | Embedded | 569 lines (separate) |
| InMemory Provider | Embedded | 219 lines (stays in base) |

## Rollout Strategy

### Option A: Big Bang (Recommended for greenfield)
1. Create all provider plugins
2. Refactor base plugin
3. Update solution
4. Test all together
5. Deploy

### Option B: Incremental (Safer for production)
1. Extract interface (✅ Done)
2. Add provider registration API (keep old code)
3. Create Sentry provider (test alongside old code)
4. Create OpenTelemetry provider
5. Create Firebase provider
6. Remove old implementations
7. Update dependencies

## Testing Plan

### Unit Tests
- Test each provider independently
- Mock IDiagnosticsBackend for service tests
- Test provider registration/unregistration

### Integration Tests
- Test multiple providers simultaneously
- Test provider discovery
- Test configuration-based activation

### Performance Tests
- Measure overhead of provider abstraction
- Test async broadcasting to multiple backends

## Known Issues to Fix

### Sentry Provider
- **Current Status**: Disabled with `#if FALSE`
- **Issue**: Type conversion errors with BreadcrumbLevel, ActivityTagsCollection
- **Fix Needed**: Update type mappings for Sentry SDK compatibility
- **Priority**: High (provider is currently unusable)

## Timeline Estimate

| Phase | Effort | Duration |
|-------|--------|----------|
| Create provider projects | Medium | 2-3 hours |
| Extract implementations | Low | 1 hour |
| Create plugin activators | Medium | 2 hours |
| Refactor base plugin | Medium | 2-3 hours |
| Update solution files | Low | 30 min |
| Testing | High | 4-6 hours |
| Documentation | Medium | 2 hours |
| **Total** | | **14-17 hours** |

## Decision Point

**Question**: Should we proceed with full implementation now, or create a proof-of-concept with one provider first?

**Recommendation**: Create Sentry provider as POC to validate the architecture, then proceed with others.

---

**Status**: Ready for implementation  
**Next Action**: Create Sentry provider plugin as proof-of-concept

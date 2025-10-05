---
id: RFC-0032
title: Migrate to NuGet.Versioning for Semantic Version Management
status: Proposed
category: plugin-system, versioning, dependency-management
created: 2025-10-05
updated: 2025-10-05
---

# RFC-0032: Migrate to NuGet.Versioning for Semantic Version Management

## Status

Proposed

## Date

2025-10-05

## Summary

Replace the custom `SemanticVersion` and `VersionRange` implementations in `WingedBean.PluginSystem` with Microsoft's battle-tested `NuGet.Versioning` library. This will reduce maintenance burden, improve reliability, and provide access to advanced version range features while maintaining backward compatibility during the transition.

## Motivation

### Problems with Current Implementation

1. **Maintenance Burden**: Custom version parsing and comparison logic requires ongoing maintenance
2. **Limited Testing**: Custom implementation lacks the extensive testing of a production-grade library
3. **Feature Gaps**: Missing advanced version range features (interval notation, floating ranges)
4. **Duplication**: Reinventing functionality that exists in well-maintained libraries
5. **Trust Issues**: Users may question the reliability of home-grown versioning logic

### Benefits of NuGet.Versioning

1. **Battle-Tested**: Used by the entire .NET ecosystem for package management
2. **Microsoft-Maintained**: Regular updates, security patches, and bug fixes
3. **SemVer 2.0 Compliant**: Full semantic versioning support with pre-release and metadata
4. **Advanced Features**: 
   - Interval notation: `[1.0.0, 2.0.0)`, `(1.0.0, ]`
   - Floating ranges: `1.*`, `1.0.*`
   - npm-style ranges via `FloatRange`
5. **Well-Documented**: Extensive documentation and community knowledge
6. **Zero Maintenance**: No need to maintain version parsing logic

## Current State

### Custom Implementation

**Location**: `WingedBean.PluginSystem/SemanticVersion.cs`

**Classes**:
- `SemanticVersion`: Parses and compares semantic versions with pre-release and build metadata
- `VersionRange`: Supports npm-style version ranges (`^1.2.3`, `~1.2.3`, exact versions)

**Usage Points**:
- `PluginManifest.cs`: Version parsing and host compatibility checking
- `PluginDependencyResolver.cs`: Dependency resolution and version validation
- `PluginUpdateManager.cs`: Version comparison for updates
- `HostBootstrap.cs`: Host version parsing

### Dependencies

Currently, `WingedBean.PluginSystem.csproj` has no versioning-related dependencies.

## Proposal

### Phase 1: Add NuGet.Versioning & Deprecation

#### 1.1 Add NuGet Package

Update `WingedBean.PluginSystem.csproj`:

```xml
<ItemGroup>
  <PackageReference Include="NuGet.Versioning" Version="6.11.1" />
</ItemGroup>
```

#### 1.2 Mark Custom Classes as Obsolete

Add `[Obsolete]` attributes to existing classes:

```csharp
[Obsolete("Use NuGet.Versioning.NuGetVersion instead. This class will be removed in version 2.0.0.")]
public class SemanticVersion : IComparable<SemanticVersion>, IEquatable<SemanticVersion>
{
    // Existing implementation unchanged
}

[Obsolete("Use NuGet.Versioning.VersionRange instead. This class will be removed in version 2.0.0.")]
public class VersionRange
{
    // Existing implementation unchanged
}
```

#### 1.3 Create Extension Methods

Add `VersionExtensions.cs` for compatibility:

```csharp
namespace WingedBean.PluginSystem;

public static class VersionExtensions
{
    // Convert custom to NuGet
    public static NuGetVersion ToNuGetVersion(this SemanticVersion version)
    {
        return NuGetVersion.Parse(version.ToString());
    }
    
    // Convert NuGet to custom (for backward compatibility)
    public static SemanticVersion ToSemanticVersion(this NuGetVersion version)
    {
        return SemanticVersion.Parse(version.ToString());
    }
    
    // Parsing helper with better error messages
    public static NuGetVersion ParseVersion(string version)
    {
        if (NuGetVersion.TryParse(version, out var result))
            return result;
        
        throw new ArgumentException($"Invalid semantic version: {version}. Must follow SemVer 2.0 format (e.g., 1.2.3, 1.2.3-beta.1, 1.2.3+build.123)");
    }
    
    // Version range parsing with npm-style support
    public static VersionRange ParseVersionRange(string range)
    {
        // Handle npm-style ranges
        if (range.StartsWith("^"))
        {
            var version = ParseVersion(range[1..]);
            return new VersionRange(
                minVersion: version,
                includeMinVersion: true,
                maxVersion: new NuGetVersion(version.Major + 1, 0, 0),
                includeMaxVersion: false);
        }
        
        if (range.StartsWith("~"))
        {
            var version = ParseVersion(range[1..]);
            return new VersionRange(
                minVersion: version,
                includeMinVersion: true,
                maxVersion: new NuGetVersion(version.Major, version.Minor + 1, 0),
                includeMaxVersion: false);
        }
        
        // Try standard NuGet range notation
        if (NuGet.Versioning.VersionRange.TryParse(range, out var result))
            return result;
        
        // Treat as exact version
        var exactVersion = ParseVersion(range);
        return new VersionRange(exactVersion, true, exactVersion, true);
    }
}
```

### Phase 2: Update Internal Implementation

#### 2.1 Update PluginManifest.cs

Replace custom `SemanticVersion` with `NuGetVersion`:

```csharp
using NuGet.Versioning;

public class PluginManifest
{
    // ... existing properties ...
    
    /// <summary>Parse semantic version from version string</summary>
    [JsonIgnore]
    public NuGetVersion SemanticVersion => VersionExtensions.ParseVersion(Version);
    
    /// <summary>Check if this plugin is compatible with a host version</summary>
    public bool IsCompatibleWith(NuGetVersion hostVersion)
    {
        if (Compatibility.MinHostVersion == null)
            return true;
            
        var minVersion = VersionExtensions.ParseVersion(Compatibility.MinHostVersion);
        return hostVersion >= minVersion;
    }
}
```

#### 2.2 Update PluginDependencyResolver.cs

Replace all `SemanticVersion` and `VersionRange` usage:

```csharp
using NuGet.Versioning;

public class PluginDependencyResolver
{
    public IEnumerable<PluginManifest> ResolveLoadOrder(
        IEnumerable<PluginManifest> manifests, 
        NuGetVersion? hostVersion = null)
    {
        // ... existing logic with NuGetVersion instead of SemanticVersion
    }
    
    private void ValidateVersionDependencies(List<PluginManifest> manifests)
    {
        var availableVersions = manifests.ToDictionary(m => m.Id, m => m.SemanticVersion);
        
        foreach (var manifest in manifests)
        {
            foreach (var (dependencyId, versionRangeString) in manifest.Dependencies)
            {
                if (!availableVersions.TryGetValue(dependencyId, out var availableVersion))
                    continue;
                
                var versionRange = VersionExtensions.ParseVersionRange(versionRangeString);
                
                if (!versionRange.Satisfies(availableVersion))
                {
                    throw new InvalidOperationException(
                        $"Plugin {manifest.Id} requires {dependencyId} {versionRangeString}, " +
                        $"but version {availableVersion} is available");
                }
            }
        }
    }
    
    public List<NuGetVersion> FindAvailableVersions(IEnumerable<PluginManifest> manifests, string pluginId)
    {
        return manifests
            .Where(m => m.Id == pluginId)
            .Select(m => m.SemanticVersion)
            .OrderByDescending(v => v)
            .ToList();
    }
}
```

#### 2.3 Update PluginUpdateManager.cs

Replace version comparison logic with `NuGetVersion`.

#### 2.4 Update HostBootstrap.cs

Replace version parsing:

```csharp
using NuGet.Versioning;

public class HostBootstrap
{
    private readonly NuGetVersion _hostVersion;
    
    public HostBootstrap(string hostVersion, /* ... */)
    {
        _hostVersion = VersionExtensions.ParseVersion(hostVersion);
        // ...
    }
}
```

### Phase 3: Remove Custom Implementation (Breaking Change)

#### 3.1 Remove Obsolete Classes

Delete `SemanticVersion.cs` entirely (or move to a separate legacy compatibility package).

#### 3.2 Update Version to 2.0.0

Since this is a breaking API change, bump the major version:
- `WingedBean.PluginSystem` version: `1.x.x` → `2.0.0`

#### 3.3 Migration Guide

Create a migration guide document:

```markdown
# Migration Guide: SemanticVersion → NuGet.Versioning

## Breaking Changes in v2.0.0

### Package Reference Required

Add to your `.csproj`:
```xml
<PackageReference Include="NuGet.Versioning" Version="6.11.1" />
```

### Type Changes

| Old Type | New Type | Namespace |
|----------|----------|-----------|
| `WingedBean.PluginSystem.SemanticVersion` | `NuGet.Versioning.NuGetVersion` | `NuGet.Versioning` |
| `WingedBean.PluginSystem.VersionRange` | `NuGet.Versioning.VersionRange` | `NuGet.Versioning` |

### API Changes

```csharp
// Before
using WingedBean.PluginSystem;
var version = SemanticVersion.Parse("1.2.3-beta.1");
var range = VersionRange.Parse("^1.2.0");
bool satisfies = range.Satisfies(version);

// After
using NuGet.Versioning;
using WingedBean.PluginSystem; // for helper extensions
var version = VersionExtensions.ParseVersion("1.2.3-beta.1");
var range = VersionExtensions.ParseVersionRange("^1.2.0");
bool satisfies = range.Satisfies(version);

// Or use NuGet types directly
var version = NuGetVersion.Parse("1.2.3-beta.1");
var range = new VersionRange(minVersion: new NuGetVersion(1, 2, 0));
bool satisfies = range.Satisfies(version);
```
```

## Migration Timeline

### v1.1.0 (Immediate)
- ✅ Add `NuGet.Versioning` package
- ✅ Mark custom classes as `[Obsolete]`
- ✅ Add extension methods for compatibility
- ✅ Update internal implementations
- ⚠️ Custom classes still work but emit compiler warnings

### v1.2.0 (Testing Period - 1 month)
- ✅ All WingedBean projects migrated internally
- ✅ Documentation updated
- ✅ Migration guide published
- ⚠️ Custom classes still available

### v2.0.0 (Breaking Release - 2 months)
- ❌ Remove custom `SemanticVersion` and `VersionRange` classes
- ❌ Only `NuGet.Versioning` types supported
- ✅ Clean API surface
- ✅ Full NuGet ecosystem compatibility

## Testing Strategy

### Unit Tests

Create comprehensive tests for `VersionExtensions`:

```csharp
[Fact]
public void ParseVersion_ValidSemVer_Success()
{
    var version = VersionExtensions.ParseVersion("1.2.3-beta.1+build.123");
    Assert.Equal(1, version.Major);
    Assert.Equal(2, version.Minor);
    Assert.Equal(3, version.Patch);
    Assert.True(version.IsPrerelease);
    Assert.Equal("beta.1", version.Release);
}

[Fact]
public void ParseVersionRange_CaretOperator_CorrectRange()
{
    var range = VersionExtensions.ParseVersionRange("^1.2.3");
    var version123 = new NuGetVersion(1, 2, 3);
    var version130 = new NuGetVersion(1, 3, 0);
    var version200 = new NuGetVersion(2, 0, 0);
    
    Assert.True(range.Satisfies(version123));
    Assert.True(range.Satisfies(version130));
    Assert.False(range.Satisfies(version200));
}

[Fact]
public void ParseVersionRange_TildeOperator_CorrectRange()
{
    var range = VersionExtensions.ParseVersionRange("~1.2.3");
    var version123 = new NuGetVersion(1, 2, 3);
    var version124 = new NuGetVersion(1, 2, 4);
    var version130 = new NuGetVersion(1, 3, 0);
    
    Assert.True(range.Satisfies(version123));
    Assert.True(range.Satisfies(version124));
    Assert.False(range.Satisfies(version130));
}
```

### Integration Tests

Test plugin dependency resolution with real plugin manifests:

```csharp
[Fact]
public void ResolveLoadOrder_WithNuGetVersioning_Success()
{
    var manifests = new[]
    {
        new PluginManifest { Id = "plugin-a", Version = "1.0.0", Dependencies = new() },
        new PluginManifest { Id = "plugin-b", Version = "2.0.0", Dependencies = new() { ["plugin-a"] = "^1.0.0" } }
    };
    
    var resolver = new PluginDependencyResolver();
    var ordered = resolver.ResolveLoadOrder(manifests);
    
    Assert.Equal("plugin-a", ordered.First().Id);
    Assert.Equal("plugin-b", ordered.Last().Id);
}
```

### Backward Compatibility Tests

Ensure obsolete APIs still work in v1.x:

```csharp
#pragma warning disable CS0618 // Type or member is obsolete
[Fact]
public void ObsoleteSemanticVersion_StillWorks()
{
    var version = SemanticVersion.Parse("1.2.3");
    Assert.Equal(1, version.Major);
}
#pragma warning restore CS0618
```

## Documentation Updates

### Files to Update

1. **README.md**: Update examples to use `NuGet.Versioning`
2. **Plugin Developer Guide**: Show new version syntax
3. **API Documentation**: Update all version-related docs
4. **Migration Guide**: Create comprehensive guide (see above)

### Example Updates

**Before**:
```csharp
// Plugin manifest
{
  "id": "my-plugin",
  "version": "1.2.3-beta.1",
  "dependencies": {
    "other-plugin": "^1.0.0"
  }
}
```

**After**: (No change to JSON, only code)
```csharp
using NuGet.Versioning;
using WingedBean.PluginSystem;

// Parse plugin version
var version = VersionExtensions.ParseVersion(manifest.Version);

// Check dependency
var range = VersionExtensions.ParseVersionRange("^1.0.0");
if (!range.Satisfies(otherPluginVersion))
{
    throw new Exception("Dependency version mismatch");
}
```

## Risks and Mitigations

### Risk 1: Breaking Changes for External Plugins

**Mitigation**: 
- Use obsolete attributes with clear messages
- Provide 2-month deprecation period
- Publish migration guide immediately
- Maintain compatibility layer in v1.x

### Risk 2: Different Version Parsing Behavior

**Mitigation**:
- Comprehensive test suite comparing old vs new behavior
- Document any differences in migration guide
- `NuGet.Versioning` is more permissive and standard-compliant

### Risk 3: Performance Regression

**Mitigation**:
- Benchmark version parsing and comparison
- `NuGet.Versioning` is highly optimized (used in NuGet client)
- Unlikely to be slower than custom implementation

### Risk 4: NPM-Style Range Support

**Mitigation**:
- Implement `ParseVersionRange` helper for `^` and `~` operators
- Map to NuGet's interval notation internally
- Document range syntax clearly

## Alternatives Considered

### 1. Keep Custom Implementation

**Pros**: No breaking changes, full control
**Cons**: Maintenance burden, reinventing the wheel, trust issues

**Decision**: ❌ Not recommended

### 2. Use Semver NuGet Package

**Pros**: Lightweight, SemVer-focused
**Cons**: Less feature-rich, not NuGet ecosystem standard

**Decision**: ❌ `NuGet.Versioning` is more complete

### 3. Use System.Version

**Pros**: Built-in, no dependencies
**Cons**: No pre-release support, no build metadata, not SemVer compliant

**Decision**: ❌ Insufficient for plugin versioning

### 4. Gradual Migration with Facade Pattern

**Pros**: Smoother transition, no breaking changes initially
**Cons**: More complex, two implementations running simultaneously

**Decision**: ✅ **Selected** - Use Phase 1-3 approach

## Success Criteria

1. ✅ All `WingedBean.PluginSystem` code uses `NuGet.Versioning` internally
2. ✅ Zero regressions in plugin dependency resolution
3. ✅ All existing tests pass with new implementation
4. ✅ Migration guide published and validated
5. ✅ No performance degradation (< 5% acceptable)
6. ✅ External plugins can migrate within deprecation period

## References

- [NuGet.Versioning Documentation](https://learn.microsoft.com/en-us/nuget/concepts/package-versioning)
- [NuGet.Versioning GitHub](https://github.com/NuGet/NuGet.Client)
- [Semantic Versioning 2.0.0](https://semver.org/)
- [RFC-0003: Plugin Architecture Foundation](./0003-plugin-architecture-foundation.md)
- [RFC-0006: Dynamic Plugin Loading](./0006-dynamic-plugin-loading.md)

## Implementation Tasks

- [ ] Phase 1.1: Add `NuGet.Versioning` package to `WingedBean.PluginSystem.csproj`
- [ ] Phase 1.2: Mark custom classes as `[Obsolete]`
- [ ] Phase 1.3: Create `VersionExtensions.cs` with helper methods
- [ ] Phase 2.1: Update `PluginManifest.cs`
- [ ] Phase 2.2: Update `PluginDependencyResolver.cs`
- [ ] Phase 2.3: Update `PluginUpdateManager.cs`
- [ ] Phase 2.4: Update `HostBootstrap.cs`
- [ ] Phase 2.5: Add comprehensive unit tests
- [ ] Phase 2.6: Add integration tests
- [ ] Phase 2.7: Update documentation
- [ ] Phase 2.8: Create migration guide
- [ ] Phase 2.9: Release v1.1.0
- [ ] Phase 3.1: Remove obsolete classes
- [ ] Phase 3.2: Release v2.0.0

---

**Author**: GitHub Copilot  
**Reviewers**: TBD  
**Implementation**: In Progress

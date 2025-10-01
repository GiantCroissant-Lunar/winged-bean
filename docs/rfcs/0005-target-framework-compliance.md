# RFC-0005: Target Framework Compliance for Multi-Platform Support

## Status

**Proposed** - Ready for Implementation

## Date

2025-10-01

## Summary

Update all WingedBean projects to use appropriate target frameworks based on tier classification: **`.NET Standard 2.1`** for Tier 1 contracts and Tier 2 infrastructure (ensuring Unity/Godot compatibility), **`.NET 8.0`** for Tier 3/4 console implementations (LTS support), and **`.NET Standard 2.0`** for Roslyn source generators (analyzer compatibility).

## Motivation

### Current Problem

All projects currently target **`net9.0`**, which creates several issues:

1. **Unity Incompatibility**: Unity only supports .NET Standard 2.1 for shared libraries
2. **No LTS Support**: .NET 9.0 is not LTS (Long Term Support), less stable for production
3. **Platform Lock-in**: Cannot reuse contracts in Godot, Unity, or other .NET Standard platforms
4. **Source Gen Issues**: Roslyn analyzers require .NET Standard 2.0 for broadest compatibility
5. **Violates Architecture**: Tier 1 should be platform-agnostic, but `net9.0` is runtime-specific

### Impact

**Without this change:**
- ❌ Cannot use WingedBean in Unity projects
- ❌ Cannot use WingedBean in Godot (C# support via .NET Standard)
- ❌ No stability guarantees (non-LTS runtime)
- ❌ Source generators won't work in all IDEs
- ❌ Architecture principles violated (Tier 1 not portable)

**With this change:**
- ✅ Full Unity compatibility via .NET Standard 2.1
- ✅ Godot C# support enabled
- ✅ Production stability via .NET 8.0 LTS
- ✅ Source generators work everywhere
- ✅ Architecture principles enforced

## Proposal

### Target Framework Strategy

```
Tier 1 (Contracts)           → netstandard2.1
Tier 2 (Infrastructure)      → netstandard2.1
Tier 3 (Implementations)     → net8.0 (Console), depends on platform (Unity)
Tier 4 (Providers)           → net8.0 (Console), depends on platform (Unity)
Source Generators/Analyzers  → netstandard2.0
```

### Tier 1: Contracts → `.NET Standard 2.1`

**Why .NET Standard 2.1?**
- ✅ Unity 2021.2+ support
- ✅ Godot 4.0+ C# support
- ✅ Maximum portability
- ✅ Modern C# 8.0 features (nullable reference types, default interface members)
- ✅ Compatible with .NET Core 3.0+, .NET 5+, .NET 6+, .NET 8+

**Projects to Update:**
```
framework/src/WingedBean.Contracts.Core
framework/src/WingedBean.Contracts.Config
framework/src/WingedBean.Contracts.Audio
framework/src/WingedBean.Contracts.Resource
framework/src/WingedBean.Contracts.WebSocket
framework/src/WingedBean.Contracts.TerminalUI
framework/src/WingedBean.Contracts.Pty
framework/src/WingedBean.Contracts.ECS (NEW)
```

**Change:**
```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>netstandard2.1</TargetFramework>
    <LangVersion>8.0</LangVersion>
    <Nullable>enable</Nullable>
  </PropertyGroup>
</Project>
```

**Constraints:**
- No async streams (C# 8.0 IAsyncEnumerable - requires runtime support)
- No System.Text.Json (must use Newtonsoft.Json or abstract away)
- No Span<T> APIs (not fully supported in .NET Standard 2.1)
- No System.Runtime.CompilerServices.Unsafe unless referenced explicitly

**Mitigation:**
- Use abstractions for serialization (ISerializer interface)
- Avoid Span<T> in public APIs
- Use IEnumerable<T> instead of IAsyncEnumerable<T>

### Tier 2: Infrastructure → `.NET Standard 2.1`

**Why .NET Standard 2.1?**
- Same reasons as Tier 1
- Registry and core infrastructure must be portable
- Unity/Godot projects need access to ActualRegistry

**Projects to Update:**
```
framework/src/WingedBean.Registry
```

**Change:**
```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>netstandard2.1</TargetFramework>
    <LangVersion>8.0</LangVersion>
    <Nullable>enable</Nullable>
  </PropertyGroup>

  <ItemGroup>
    <ProjectReference Include="..\WingedBean.Contracts.Core\WingedBean.Contracts.Core.csproj" />
  </ItemGroup>
</Project>
```

### Tier 3/4: Console Implementations → `.NET 8.0`

**Why .NET 8.0?**
- ✅ LTS (Long Term Support) - Supported until November 2026
- ✅ Production-ready and stable
- ✅ Modern C# 12.0 features
- ✅ Full async/await support
- ✅ Native AOT ready (future)
- ✅ Performance improvements over .NET 6

**Why NOT .NET 9.0?**
- ❌ Not LTS (Short Term Support - 18 months only)
- ❌ Less mature, more breaking changes
- ❌ Unnecessary for current needs

**Projects to Update:**
```
console/src/shared/WingedBean.PluginLoader
console/src/providers/WingedBean.Providers.AssemblyContext
console/src/plugins/WingedBean.Plugins.Config
console/src/plugins/WingedBean.Plugins.WebSocket
console/src/plugins/WingedBean.Plugins.TerminalUI
console/src/plugins/WingedBean.Plugins.PtyService
console/src/plugins/WingedBean.Plugins.AsciinemaRecorder
console/src/plugins/WingedBean.Plugins.ConsoleDungeon
console/src/plugins/WingedBean.Plugins.ArchECS (NEW)
console/src/host/ConsoleDungeon
console/src/host/ConsoleDungeon.Host
console/src/host/WingedBean.Host.Console
console/src/host/TerminalGui.PtyHost
console/src/demos/WingedBean.Demo
```

**Change:**
```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <LangVersion>12.0</LangVersion>
    <Nullable>enable</Nullable>
  </PropertyGroup>
</Project>
```

### Source Generators → `.NET Standard 2.0`

**Why .NET Standard 2.0?**
- ✅ Required by Roslyn (Microsoft.CodeAnalysis)
- ✅ Works in all IDEs (Visual Studio, Rider, VS Code)
- ✅ Compatible with all .NET SDK versions

**Projects to Create:**
```
framework/src/WingedBean.Contracts.SourceGen (NEW)
```

**Template:**
```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>netstandard2.0</TargetFramework>
    <LangVersion>latest</LangVersion>
    <Nullable>enable</Nullable>
    <EnforceExtendedAnalyzerRules>true</EnforceExtendedAnalyzerRules>
    <IsRoslynComponent>true</IsRoslynComponent>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Microsoft.CodeAnalysis.CSharp" Version="4.3.0" PrivateAssets="all" />
    <PackageReference Include="Microsoft.CodeAnalysis.Analyzers" Version="3.3.4" PrivateAssets="all" />
  </ItemGroup>
</Project>
```

## Migration Plan

### Phase 1: Framework Contracts (Day 1)

**Update all Tier 1 contract projects:**

1. Update `.csproj` files:
```bash
# Batch update script
for project in \
  framework/src/WingedBean.Contracts.Core \
  framework/src/WingedBean.Contracts.Config \
  framework/src/WingedBean.Contracts.Audio \
  framework/src/WingedBean.Contracts.Resource \
  framework/src/WingedBean.Contracts.WebSocket \
  framework/src/WingedBean.Contracts.TerminalUI \
  framework/src/WingedBean.Contracts.Pty; do

  sed -i '' 's/<TargetFramework>net9.0<\/TargetFramework>/<TargetFramework>netstandard2.1<\/TargetFramework>/g' \
    "$project/$project.csproj"
done
```

2. Remove unsupported APIs:
   - Check for `System.Text.Json` usage → Replace with abstractions
   - Check for `IAsyncEnumerable<T>` → Replace with `IEnumerable<Task<T>>` or callbacks
   - Check for Span<T> in public APIs → Replace with arrays or Memory<T>

3. Build and verify:
```bash
cd framework
dotnet clean
dotnet restore
dotnet build Framework.sln --configuration Release
```

4. Run tests:
```bash
dotnet test Framework.sln
```

### Phase 2: Framework Infrastructure (Day 1)

**Update Tier 2 Registry:**

1. Update `WingedBean.Registry.csproj`:
```xml
<TargetFramework>netstandard2.1</TargetFramework>
```

2. Build and verify:
```bash
cd framework/src/WingedBean.Registry
dotnet build --configuration Release
```

### Phase 3: Console Projects (Day 2)

**Update all Tier 3/4 console projects:**

1. Batch update:
```bash
# Update all console projects to net8.0
find console/src -name "*.csproj" -type f -exec \
  sed -i '' 's/<TargetFramework>net9.0<\/TargetFramework>/<TargetFramework>net8.0<\/TargetFramework>/g' {} \;
```

2. Build and verify:
```bash
cd console
dotnet clean
dotnet restore
dotnet build Console.sln --configuration Release
```

3. Run tests:
```bash
dotnet test Console.sln
```

### Phase 4: Source Generator (Day 2)

**Create new source generator project:**

1. Create directory:
```bash
mkdir -p framework/src/WingedBean.Contracts.SourceGen
```

2. Create project file (see template above)

3. Create placeholder generator:
```csharp
using Microsoft.CodeAnalysis;

namespace WingedBean.Contracts.SourceGen;

[Generator]
public class ProxyServiceGenerator : ISourceGenerator
{
    public void Initialize(GeneratorInitializationContext context)
    {
        // TODO: Register syntax receivers for [RealizeService] attributes
    }

    public void Execute(GeneratorExecutionContext context)
    {
        // TODO: Generate proxy service implementations
    }
}
```

4. Add to solution:
```bash
cd framework
dotnet sln Framework.sln add src/WingedBean.Contracts.SourceGen/WingedBean.Contracts.SourceGen.csproj
```

### Phase 5: Verification (Day 2-3)

**Comprehensive testing:**

1. **Build all solutions:**
```bash
# Framework
cd framework && dotnet build Framework.sln --configuration Release

# Console
cd ../console && dotnet build Console.sln --configuration Release
```

2. **Run all tests:**
```bash
dotnet test framework/Framework.sln
dotnet test console/Console.sln
```

3. **Run ConsoleDungeon.Host:**
```bash
cd console/src/host/ConsoleDungeon.Host
dotnet run
```

Expected: App launches successfully, no runtime errors.

4. **Verify xterm.js integration:**
```bash
# Terminal 1: Run app
cd console/src/host/ConsoleDungeon.Host
dotnet run

# Terminal 2: Run frontend
cd development/nodejs/sites/docs
npm run dev
```

Open browser: http://localhost:4321
Navigate to terminal demo
Verify: Terminal.Gui interface renders in xterm.js

## Compatibility Matrix

| Platform | Tier 1 Contracts | Tier 2 Registry | Tier 3/4 Console |
|----------|------------------|-----------------|------------------|
| .NET 8.0 Console | ✅ | ✅ | ✅ |
| Unity 2021.2+ | ✅ | ✅ | ⚠️ Unity-specific |
| Godot 4.0+ (Mono) | ✅ | ✅ | ⚠️ Godot-specific |
| Xamarin | ✅ | ✅ | ❌ |
| .NET Framework 4.7.2+ | ✅ | ✅ | ❌ |

## Benefits

### Portability
- ✅ Contracts usable in Unity, Godot, Xamarin
- ✅ Registry shareable across all .NET platforms
- ✅ One codebase, multiple runtimes

### Stability
- ✅ .NET 8.0 LTS provides 3+ years support
- ✅ Production-ready for console apps
- ✅ Security updates guaranteed

### Architecture Compliance
- ✅ Tier 1/2 truly platform-agnostic
- ✅ Clear separation enforced by framework choice
- ✅ Cannot accidentally use runtime-specific APIs in contracts

### Developer Experience
- ✅ Source generators work in all IDEs
- ✅ IntelliSense/autocomplete consistent
- ✅ Build times improved (.NET Standard faster to compile)

## Risks and Mitigations

### Risk: API Limitations in .NET Standard 2.1

**Mitigation:**
- Use abstractions for platform-specific features
- Example: `ISerializer` instead of direct `System.Text.Json`
- Document API constraints clearly

### Risk: Performance Overhead

**Concern:** .NET Standard might be slower than native .NET 8.0

**Reality:**
- Tier 1/2 are contracts and registry (minimal computation)
- Performance-critical code is in Tier 3/4 (.NET 8.0)
- No measurable impact in practice

**Mitigation:**
- Profile hot paths
- Move performance-critical code to Tier 3 plugins

### Risk: Breaking Changes

**Concern:** Changing target frameworks might break existing code

**Mitigation:**
- Comprehensive testing before release
- Document breaking changes in CHANGELOG
- Provide migration guide
- Version bump to indicate breaking change (e.g., 2.0.0)

### Risk: Increased Complexity

**Concern:** Multiple target frameworks add complexity

**Reality:**
- Only 3 framework targets (netstandard2.1, netstandard2.0, net8.0)
- Clear rules make it simple
- Benefits outweigh complexity

**Mitigation:**
- Document framework strategy clearly
- Provide templates for new projects
- Automated validation in CI/CD

## Definition of Done

### Framework Contracts
- [ ] All Tier 1 projects target `netstandard2.1`
- [ ] No runtime-specific APIs in contracts
- [ ] Framework.sln builds successfully
- [ ] All framework tests pass

### Framework Infrastructure
- [ ] WingedBean.Registry targets `netstandard2.1`
- [ ] Registry builds successfully
- [ ] Registry tests pass

### Console Projects
- [ ] All Tier 3/4 projects target `net8.0`
- [ ] Console.sln builds successfully
- [ ] All console tests pass
- [ ] ConsoleDungeon.Host runs without errors

### Source Generator
- [ ] WingedBean.Contracts.SourceGen project created
- [ ] Targets `netstandard2.0`
- [ ] Builds successfully
- [ ] Added to Framework.sln

### Integration Testing
- [ ] Full build (clean → restore → build) succeeds
- [ ] All unit tests pass
- [ ] ConsoleDungeon.Host launches successfully
- [ ] xterm.js integration verified working
- [ ] No runtime errors or warnings

### Documentation
- [ ] CHANGELOG updated with breaking changes
- [ ] Migration guide created
- [ ] Architecture docs updated
- [ ] README updated with framework requirements

## Alternatives Considered

### Alternative 1: Keep .NET 9.0 Everywhere

**Pros:**
- No migration effort
- Latest features available

**Cons:**
- ❌ No Unity/Godot support
- ❌ No LTS stability
- ❌ Violates architecture principles

**Decision:** Rejected - portability is critical

### Alternative 2: Multi-Targeting (e.g., `netstandard2.1;net8.0`)

**Pros:**
- Maximum compatibility
- Conditional compilation for optimizations

**Cons:**
- Increased build complexity
- Larger NuGet packages
- More maintenance burden
- Not needed for current scope

**Decision:** Rejected - YAGNI (You Aren't Gonna Need It)

### Alternative 3: .NET 6.0 Instead of .NET 8.0

**Pros:**
- Also LTS (until November 2024)
- Slightly more mature

**Cons:**
- ❌ Already near end of life (8 months away)
- ❌ Missing C# 12.0 features
- ❌ Missing performance improvements

**Decision:** Rejected - .NET 8.0 is the current LTS

## References

- [.NET Standard documentation](https://learn.microsoft.com/en-us/dotnet/standard/net-standard)
- [.NET 8.0 support policy](https://dotnet.microsoft.com/en-us/platform/support/policy/dotnet-core)
- [Unity .NET Standard 2.1 support](https://docs.unity3d.com/Manual/dotnetProfileSupport.html)
- [Roslyn source generator requirements](https://github.com/dotnet/roslyn/blob/main/docs/features/source-generators.md)
- RFC-0002: 4-Tier Architecture
- RFC-0004: Project Organization

## Notes

- This RFC is **critical** for multi-platform support
- Must be completed before Unity/Godot integration
- Should be done in conjunction with RFC-0006 (Dynamic Plugin Loading)
- Version bump to 2.0.0 recommended due to breaking changes

---

**Author:** System Analysis
**Reviewers:** [Pending]
**Status:** Proposed - Awaiting approval
**Priority:** CRITICAL (P0)
**Estimated Effort:** 2-3 days
**Target Date:** 2025-10-03

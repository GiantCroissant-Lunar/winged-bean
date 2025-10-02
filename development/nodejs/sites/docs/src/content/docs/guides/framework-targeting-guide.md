---
title: Framework Targeting Guide
---

# Framework Targeting Guide

This guide explains the framework targeting strategy for WingedBean projects and how to correctly target frameworks for new projects.

## Overview

WingedBean uses a **tiered framework targeting strategy** to ensure multi-platform compatibility while maintaining modern .NET features where appropriate:

- **Tier 1 (Contracts)**: `.NET Standard 2.1` - For Unity/Godot compatibility
- **Tier 2 (Infrastructure)**: `.NET Standard 2.1` - For Unity/Godot compatibility
- **Tier 3/4 (Implementations)**: `.NET 8.0` - For LTS stability and modern features
- **Source Generators**: `.NET Standard 2.0` - For Roslyn compatibility

## Framework Targeting Strategy

### Tier 1: Contracts → `.NET Standard 2.1`

**Why .NET Standard 2.1?**
- ✅ Unity 2021+ compatible
- ✅ Godot C# compatible
- ✅ Maximum portability
- ✅ No runtime-specific APIs

**Projects:**
```
framework/src/WingedBean.Contracts.Core
framework/src/WingedBean.Contracts.Config
framework/src/WingedBean.Contracts.Audio
framework/src/WingedBean.Contracts.Resource
framework/src/WingedBean.Contracts.WebSocket
framework/src/WingedBean.Contracts.TerminalUI
framework/src/WingedBean.Contracts.Pty
```

**Project File Template:**
```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>netstandard2.1</TargetFramework>
    <LangVersion>latest</LangVersion>
    <Nullable>enable</Nullable>
  </PropertyGroup>
</Project>
```

**Guidelines:**
- ❌ No `System.Text.Json` (use abstractions instead)
- ❌ No `IAsyncEnumerable<T>` (use `Task<IEnumerable<T>>` or callbacks)
- ❌ No `Span<T>` in public APIs (use arrays or `Memory<T>`)
- ✅ Only platform-agnostic APIs
- ✅ Pure contracts and interfaces

### Tier 2: Infrastructure → `.NET Standard 2.1`

**Why .NET Standard 2.1?**
- ✅ Same Unity/Godot compatibility as Tier 1
- ✅ Can be loaded in Unity/Godot alongside contracts
- ✅ Minimal runtime dependencies

**Projects:**
```
framework/src/WingedBean.Registry
```

**Project File Template:**
```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>netstandard2.1</TargetFramework>
    <LangVersion>latest</LangVersion>
    <Nullable>enable</Nullable>
  </PropertyGroup>

  <ItemGroup>
    <ProjectReference Include="../WingedBean.Contracts.Core/WingedBean.Contracts.Core.csproj" />
  </ItemGroup>
</Project>
```

### Tier 3/4: Console Implementations → `.NET 8.0`

**Why .NET 8.0?**
- ✅ Long Term Support (LTS) until November 2026
- ✅ Modern C# features and performance optimizations
- ✅ Production-ready stability
- ✅ Full runtime capabilities

**Projects:**
```
console/src/host/ConsoleDungeon.Host
console/src/plugins/WingedBean.Plugins.*
console/src/providers/WingedBean.Providers.*
console/src/shared/WingedBean.PluginLoader
console-dungeon/ConsoleDungeon
```

**Project File Template:**
```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <LangVersion>latest</LangVersion>
    <Nullable>enable</Nullable>
  </PropertyGroup>

  <ItemGroup>
    <!-- Can reference both netstandard2.1 and net8.0 projects -->
    <ProjectReference Include="../../framework/src/WingedBean.Contracts.Core/WingedBean.Contracts.Core.csproj" />
  </ItemGroup>
</Project>
```

**Guidelines:**
- ✅ Use modern C# features freely
- ✅ Use `System.Text.Json`
- ✅ Use `IAsyncEnumerable<T>`
- ✅ Use `Span<T>` and `Memory<T>`
- ✅ Full .NET 8.0 BCL available

### Source Generators → `.NET Standard 2.0`

**Why .NET Standard 2.0?**
- ✅ Required by Roslyn (Microsoft.CodeAnalysis)
- ✅ Works in all IDEs (Visual Studio, Rider, VS Code)
- ✅ Compatible with all .NET SDK versions

**Projects:**
```
framework/src/WingedBean.SourceGenerators.Proxy
```

**Project File Template:**
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

**Guidelines:**
- ✅ Implement `ISourceGenerator` or `IIncrementalGenerator`
- ✅ Use `PrivateAssets="all"` for Roslyn packages
- ✅ Mark as `IsRoslynComponent`
- ❌ No runtime dependencies (source generators run at compile time)

### Test Projects → `.NET 8.0`

**Why .NET 8.0?**
- ✅ Tests run on the development machine, not in Unity/Godot
- ✅ Modern test frameworks and tooling
- ✅ Can test both netstandard2.1 and net8.0 projects

**Project File Template:**
```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <LangVersion>latest</LangVersion>
    <Nullable>enable</Nullable>
    <IsPackable>false</IsPackable>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.11.1" />
    <PackageReference Include="xunit" Version="2.9.2" />
    <PackageReference Include="xunit.runner.visualstudio" Version="2.8.2" />
  </ItemGroup>
</Project>
```

## Compatibility Matrix

| Source Project | Can Reference | Cannot Reference |
|---------------|---------------|------------------|
| netstandard2.1 (Contracts) | netstandard2.0, netstandard2.1 | net8.0 |
| netstandard2.1 (Registry) | netstandard2.0, netstandard2.1 | net8.0 |
| net8.0 (Console) | netstandard2.0, netstandard2.1, net8.0 | (none) |
| netstandard2.0 (SourceGen) | netstandard2.0 | netstandard2.1, net8.0 |

## Creating New Projects

### New Contract Project

1. **Create project:**
   ```bash
   cd development/dotnet/framework/src
   dotnet new classlib -n WingedBean.Contracts.NewFeature
   ```

2. **Update .csproj:**
   ```xml
   <TargetFramework>netstandard2.1</TargetFramework>
   ```

3. **Add to solution:**
   ```bash
   cd ../..
   dotnet sln Framework.sln add src/WingedBean.Contracts.NewFeature/WingedBean.Contracts.NewFeature.csproj
   ```

4. **Verify build:**
   ```bash
   dotnet build Framework.sln
   ```

### New Console Plugin

1. **Create project:**
   ```bash
   cd development/dotnet/console/src/plugins
   dotnet new classlib -n WingedBean.Plugins.NewPlugin
   ```

2. **Update .csproj:**
   ```xml
   <TargetFramework>net8.0</TargetFramework>
   ```

3. **Add contract reference:**
   ```xml
   <ItemGroup>
     <ProjectReference Include="../../../framework/src/WingedBean.Contracts.Core/WingedBean.Contracts.Core.csproj" />
   </ItemGroup>
   ```

4. **Add to solution:**
   ```bash
   cd ../../../..
   dotnet sln WingedBean.sln add console/src/plugins/WingedBean.Plugins.NewPlugin/WingedBean.Plugins.NewPlugin.csproj
   ```

### New Source Generator

1. **Create project:**
   ```bash
   cd development/dotnet/framework/src
   dotnet new classlib -n WingedBean.SourceGenerators.NewGenerator
   ```

2. **Update .csproj to netstandard2.0 with Roslyn packages:**
   ```xml
   <TargetFramework>netstandard2.0</TargetFramework>
   <IsRoslynComponent>true</IsRoslynComponent>
   ```

3. **Add Roslyn dependencies:**
   ```xml
   <ItemGroup>
     <PackageReference Include="Microsoft.CodeAnalysis.CSharp" Version="4.3.0" PrivateAssets="all" />
     <PackageReference Include="Microsoft.CodeAnalysis.Analyzers" Version="3.3.4" PrivateAssets="all" />
   </ItemGroup>
   ```

## Verification

### Build Verification

Build each solution to verify framework targeting:

```bash
# Framework solution (netstandard2.1 + netstandard2.0)
cd development/dotnet/framework
dotnet clean
dotnet build Framework.sln --configuration Release

# Console solution (net8.0)
cd ../console
dotnet clean
dotnet build Console.sln --configuration Release

# Full solution
cd ..
dotnet clean
dotnet build WingedBean.sln --configuration Release
```

### Test Verification

Run tests to ensure compatibility:

```bash
cd development/dotnet
dotnet test WingedBean.sln --configuration Release
```

Expected: All tests pass (95+ tests)

### Runtime Verification

Verify console application runs:

```bash
cd console/src/host/ConsoleDungeon.Host
dotnet run --configuration Release
```

Expected: Application starts, all services load, no errors

## Troubleshooting

### Error: Project targets net8.0 but references netstandard2.1

**Problem**: Trying to reference a net8.0 project from a netstandard2.1 project.

**Solution**: Reverse the dependency. Tier 1/2 cannot depend on Tier 3/4.

### Error: API not available in .NET Standard 2.1

**Problem**: Using a .NET 8.0 API in a netstandard2.1 project.

**Solutions:**
- Use an abstraction/interface in the contract
- Move implementation to Tier 3/4
- Use a polyfill NuGet package

### Warning: Source generator not running

**Problem**: Generator not targeting netstandard2.0 or missing `IsRoslynComponent`.

**Solution**:
```xml
<TargetFramework>netstandard2.0</TargetFramework>
<IsRoslynComponent>true</IsRoslynComponent>
```

## Related Documentation

- [RFC-0005: Target Framework Compliance](../rfcs/0005-target-framework-compliance.md)
- [Source Generator Usage Guide](./source-generator-usage.md)
- [Architecture Documentation](../rfcs/0002-service-platform-core-4-tier-architecture.md)
- [.NET README](../../development/dotnet/README.md)

## References

- [.NET Standard 2.1 Specification](https://learn.microsoft.com/en-us/dotnet/standard/net-standard?tabs=net-standard-2-1)
- [Unity C# and .NET Support](https://docs.unity3d.com/Manual/CSharpCompiler.html)
- [.NET 8.0 LTS](https://dotnet.microsoft.com/platform/support/policy/dotnet-core)
- [Roslyn Source Generators](https://learn.microsoft.com/en-us/dotnet/csharp/roslyn-sdk/source-generators-overview)

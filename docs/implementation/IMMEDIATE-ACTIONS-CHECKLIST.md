# Immediate Actions Checklist

**Date:** 2025-10-01
**Goal:** Fix framework compliance and prepare for Arch ECS integration
**Timeline:** Week 1 (5 days)

---

## 🔴 CRITICAL: Framework Compliance (Days 1-2)

### Task 1: Update Tier 1 Contracts → .NET Standard 2.1

**WHY:** Ensure contracts are portable to Unity, Godot, and other platforms.

**Files to Update:**

- [ ] `framework/src/WingedBean.Contracts.Core/WingedBean.Contracts.Core.csproj`
- [ ] `framework/src/WingedBean.Contracts.Config/WingedBean.Contracts.Config.csproj`
- [ ] `framework/src/WingedBean.Contracts.Audio/WingedBean.Contracts.Audio.csproj`
- [ ] `framework/src/WingedBean.Contracts.Resource/WingedBean.Contracts.Resource.csproj`
- [ ] `framework/src/WingedBean.Contracts.WebSocket/WingedBean.Contracts.WebSocket.csproj`
- [ ] `framework/src/WingedBean.Contracts.TerminalUI/WingedBean.Contracts.TerminalUI.csproj`
- [ ] `framework/src/WingedBean.Contracts.Pty/WingedBean.Contracts.Pty.csproj`

**Change:**
```xml
<!-- FROM: -->
<TargetFramework>net9.0</TargetFramework>

<!-- TO: -->
<TargetFramework>netstandard2.1</TargetFramework>
```

**Command:**
```bash
cd framework
dotnet build Framework.sln
# Should succeed with no errors
```

---

### Task 2: Update Tier 2 Registry → .NET Standard 2.1

- [ ] `framework/src/WingedBean.Registry/WingedBean.Registry.csproj`

**Same change as above.**

---

### Task 3: Update Tier 3/4 Console Projects → .NET 8.0

**WHY:** .NET 8 is LTS (Long Term Support), more stable than .NET 9.

**Files to Update:**

- [ ] `console/src/shared/WingedBean.PluginLoader/WingedBean.PluginLoader.csproj`
- [ ] `console/src/providers/WingedBean.Providers.AssemblyContext/WingedBean.Providers.AssemblyContext.csproj`
- [ ] `console/src/plugins/WingedBean.Plugins.Config/WingedBean.Plugins.Config.csproj`
- [ ] `console/src/plugins/WingedBean.Plugins.WebSocket/WingedBean.Plugins.WebSocket.csproj`
- [ ] `console/src/plugins/WingedBean.Plugins.TerminalUI/WingedBean.Plugins.TerminalUI.csproj`
- [ ] `console/src/plugins/WingedBean.Plugins.PtyService/WingedBean.Plugins.PtyService.csproj`
- [ ] `console/src/plugins/WingedBean.Plugins.AsciinemaRecorder/WingedBean.Plugins.AsciinemaRecorder.csproj`
- [ ] `console/src/plugins/WingedBean.Plugins.ConsoleDungeon/WingedBean.Plugins.ConsoleDungeon.csproj`
- [ ] `console/src/host/ConsoleDungeon/ConsoleDungeon.csproj`
- [ ] `console/src/host/ConsoleDungeon.Host/ConsoleDungeon.Host.csproj`

**Change:**
```xml
<!-- FROM: -->
<TargetFramework>net9.0</TargetFramework>

<!-- TO: -->
<TargetFramework>net8.0</TargetFramework>
```

**Command:**
```bash
cd console
dotnet build Console.sln
# Should succeed with no errors
```

---

### Task 4: Create Source Generator Project

- [ ] Create directory: `framework/src/WingedBean.Contracts.SourceGen/`
- [ ] Create project file: `WingedBean.Contracts.SourceGen.csproj`

**Content:**
```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>netstandard2.0</TargetFramework>
    <EnforceExtendedAnalyzerRules>true</EnforceExtendedAnalyzerRules>
    <IsRoslynComponent>true</IsRoslynComponent>
    <LangVersion>latest</LangVersion>
    <Nullable>enable</Nullable>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Microsoft.CodeAnalysis.CSharp" Version="4.3.0" PrivateAssets="all" />
    <PackageReference Include="Microsoft.CodeAnalysis.Analyzers" Version="3.3.4" PrivateAssets="all" />
  </ItemGroup>
</Project>
```

- [ ] Create placeholder file: `ProxyServiceGenerator.cs`

**Content:**
```csharp
using Microsoft.CodeAnalysis;

namespace WingedBean.Contracts.SourceGen;

[Generator]
public class ProxyServiceGenerator : ISourceGenerator
{
    public void Initialize(GeneratorInitializationContext context)
    {
        // TODO: Register syntax receivers
    }

    public void Execute(GeneratorExecutionContext context)
    {
        // TODO: Generate proxy service implementations
    }
}
```

- [ ] Add to solution:
```bash
cd framework
dotnet sln Framework.sln add src/WingedBean.Contracts.SourceGen/WingedBean.Contracts.SourceGen.csproj
```

---

### Task 5: Verify Build

- [ ] Build framework:
```bash
cd framework
dotnet clean
dotnet build Framework.sln --configuration Release
```

- [ ] Build console:
```bash
cd console
dotnet clean
dotnet build Console.sln --configuration Release
```

- [ ] Run ConsoleDungeon.Host:
```bash
cd console/src/host/ConsoleDungeon.Host
dotnet run
```

**Expected:** App launches successfully, no errors.

---

## 🟡 HIGH PRIORITY: Dynamic Plugin Loading (Days 3-4)

### Task 6: Create Plugin Configuration

- [ ] Create file: `console/src/host/ConsoleDungeon.Host/plugins.json`

**Content:**
```json
{
  "plugins": [
    {
      "id": "wingedbean.plugins.config",
      "path": "plugins/WingedBean.Plugins.Config.dll",
      "priority": 1000,
      "loadStrategy": "Eager"
    },
    {
      "id": "wingedbean.plugins.websocket",
      "path": "plugins/WingedBean.Plugins.WebSocket.dll",
      "priority": 100,
      "loadStrategy": "Eager"
    },
    {
      "id": "wingedbean.plugins.terminalui",
      "path": "plugins/WingedBean.Plugins.TerminalUI.dll",
      "priority": 100,
      "loadStrategy": "Eager"
    }
  ]
}
```

- [ ] Update `.csproj` to copy file:
```xml
<ItemGroup>
  <None Update="plugins.json">
    <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
  </None>
</ItemGroup>
```

---

### Task 7: Remove Static Plugin References

- [ ] Edit: `console/src/host/ConsoleDungeon.Host/ConsoleDungeon.Host.csproj`

**Remove these lines:**
```xml
<ProjectReference Include="../WingedBean.Plugins.WebSocket/..." />
<ProjectReference Include="../WingedBean.Plugins.TerminalUI/..." />
<ProjectReference Include="../WingedBean.Plugins.Config/..." />
```

**Keep only:**
```xml
<ProjectReference Include="../ConsoleDungeon/ConsoleDungeon.csproj" />
<ProjectReference Include="../../../framework/src/WingedBean.Registry/WingedBean.Registry.csproj" />
<ProjectReference Include="../../shared/WingedBean.PluginLoader/WingedBean.PluginLoader.csproj" />
<ProjectReference Include="../../providers/WingedBean.Providers.AssemblyContext/WingedBean.Providers.AssemblyContext.csproj" />
```

---

### Task 8: Create Plugin Copy Build Target

- [ ] Create file: `console/src/host/ConsoleDungeon.Host/build/copy-plugins.targets`

**Content:**
```xml
<Project>
  <Target Name="CopyPlugins" AfterTargets="Build">
    <Message Importance="high" Text="Copying plugin assemblies to output directory..." />

    <ItemGroup>
      <!-- Get all plugin DLLs -->
      <PluginFiles Include="../../plugins/**/bin/$(Configuration)/$(TargetFramework)/*.dll" />
      <PluginFiles Include="../../plugins/**/bin/$(Configuration)/$(TargetFramework)/*.json" />
    </ItemGroup>

    <!-- Create plugins directory -->
    <MakeDir Directories="$(OutDir)plugins/" />

    <!-- Copy plugins -->
    <Copy SourceFiles="@(PluginFiles)" DestinationFolder="$(OutDir)plugins/" SkipUnchangedFiles="true" />

    <Message Importance="high" Text="Copied @(PluginFiles->Count()) plugin files" />
  </Target>
</Project>
```

- [ ] Import in `.csproj`:
```xml
<Import Project="build/copy-plugins.targets" />
```

---

### Task 9: Implement Dynamic Loading in Program.cs

- [ ] Edit: `console/src/host/ConsoleDungeon.Host/Program.cs`

**Replace with:**
```csharp
using System.Text.Json;
using WingedBean.Contracts.Core;
using WingedBean.Registry;
using WingedBean.PluginLoader;
using WingedBean.Providers.AssemblyContext;

namespace ConsoleDungeon.Host;

public class Program
{
    public static async Task Main(string[] args)
    {
        Console.WriteLine("========================================");
        Console.WriteLine("ConsoleDungeon.Host - Dynamic Plugin Mode");
        Console.WriteLine("========================================\n");

        // Step 1: Create foundation services
        Console.WriteLine("[1/4] Initializing foundation services...");
        var registry = new ActualRegistry();
        var contextProvider = new AssemblyContextProvider();
        var pluginLoader = new ActualPluginLoader(contextProvider);

        registry.Register<IRegistry>(registry);
        registry.Register<IPluginLoader>(pluginLoader);
        Console.WriteLine("✓ Foundation services ready\n");

        // Step 2: Load plugin configuration
        Console.WriteLine("[2/4] Loading plugin configuration...");
        var config = LoadPluginConfiguration("plugins.json");
        Console.WriteLine($"✓ Found {config.Plugins.Length} plugins to load\n");

        // Step 3: Load plugins dynamically
        Console.WriteLine("[3/4] Loading plugins...");
        foreach (var pluginInfo in config.Plugins.OrderByDescending(p => p.Priority))
        {
            Console.WriteLine($"  Loading: {pluginInfo.Id} (priority: {pluginInfo.Priority})");
            try
            {
                var plugin = await pluginLoader.LoadAsync(pluginInfo.Path);
                Console.WriteLine($"  ✓ Loaded: {plugin.Manifest.Id} v{plugin.Manifest.Version}");

                // Auto-register services
                foreach (var service in plugin.GetServices())
                {
                    var serviceType = FindContractInterface(service.GetType());
                    if (serviceType != null)
                    {
                        registry.Register(serviceType, service, pluginInfo.Priority);
                        Console.WriteLine($"    → Registered: {serviceType.Name}");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"  ✗ Failed: {ex.Message}");
            }
        }
        Console.WriteLine();

        // Step 4: Launch game
        Console.WriteLine("[4/4] Launching ConsoleDungeon...\n");
        var app = new ConsoleDungeon.Program(registry);
        await app.RunAsync();
    }

    private static PluginConfiguration LoadPluginConfiguration(string path)
    {
        var json = File.ReadAllText(path);
        return JsonSerializer.Deserialize<PluginConfiguration>(json)
            ?? throw new InvalidOperationException("Failed to load plugin configuration");
    }

    private static Type? FindContractInterface(Type type)
    {
        return type.GetInterfaces()
            .FirstOrDefault(i => i.Namespace?.StartsWith("WingedBean.Contracts") == true);
    }
}

public class PluginConfiguration
{
    public PluginInfo[] Plugins { get; set; } = Array.Empty<PluginInfo>();
}

public class PluginInfo
{
    public string Id { get; set; } = "";
    public string Path { get; set; } = "";
    public int Priority { get; set; }
    public string LoadStrategy { get; set; } = "Eager";
}
```

---

### Task 10: Test Dynamic Loading

- [ ] Build:
```bash
cd console
dotnet build Console.sln
```

- [ ] Run:
```bash
cd src/host/ConsoleDungeon.Host
dotnet run
```

**Expected Output:**
```
========================================
ConsoleDungeon.Host - Dynamic Plugin Mode
========================================

[1/4] Initializing foundation services...
✓ Foundation services ready

[2/4] Loading plugin configuration...
✓ Found 3 plugins to load

[3/4] Loading plugins...
  Loading: wingedbean.plugins.config (priority: 1000)
  ✓ Loaded: wingedbean.plugins.config v1.0.0
    → Registered: IConfigService
  Loading: wingedbean.plugins.websocket (priority: 100)
  ✓ Loaded: wingedbean.plugins.websocket v1.0.0
    → Registered: IWebSocketService
  Loading: wingedbean.plugins.terminalui (priority: 100)
  ✓ Loaded: wingedbean.plugins.terminalui v1.0.0
    → Registered: ITerminalUIService

[4/4] Launching ConsoleDungeon...
```

---

## 🟢 MEDIUM PRIORITY: Documentation (Day 5)

### Task 11: Update Architecture Documentation

- [ ] Create: `development/dotnet/ARCHITECTURE.md`

**Content:** Map actual structure to RFC-0004, document changes.

---

### Task 12: Create Migration Notes

- [ ] Create: `development/dotnet/MIGRATION-NOTES.md`

Document:
- Target framework changes
- Breaking changes (if any)
- Plugin loading changes
- How to update custom plugins

---

### Task 13: Update README

- [ ] Update: `development/dotnet/README.md`

Add:
- Current target frameworks
- Plugin development guide
- Build instructions

---

## Verification Checklist

Before proceeding to Week 2 (Arch ECS), verify:

- [ ] All contracts use `netstandard2.1`
- [ ] All console projects use `net8.0`
- [ ] Source generator project exists (even if empty)
- [ ] Plugins load dynamically from `plugins.json`
- [ ] No static plugin references in Host
- [ ] Build succeeds on clean workspace
- [ ] ConsoleDungeon.Host runs without errors
- [ ] All services register correctly
- [ ] Terminal.Gui interface appears
- [ ] xterm.js integration still works (if testing)

---

## Week 2 Preview: Arch ECS Integration

Once Week 1 is complete, Week 2 will focus on:

1. Create `WingedBean.Contracts.ECS` (Tier 1)
2. Create `WingedBean.Plugins.ArchECS` (Tier 3)
3. Define game components (Position, Stats, Renderable, etc.)
4. Implement core systems (Movement, Combat, Render)
5. Integrate with game loop

See: `docs/design/dungeon-crawler-ecs-roadmap.md` for full details.

---

**Status:** Ready to Start
**Estimated Time:** 5 days (40 hours)
**Blockers:** None
**Next Review:** End of Day 2 (framework compliance complete)

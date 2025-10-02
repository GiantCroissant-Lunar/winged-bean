---
title: RFC-0006: Dynamic Plugin Loading and Runtime Composition
---

# RFC-0006: Dynamic Plugin Loading and Runtime Composition

## Status

**Proposed** - Ready for Implementation

## Date

2025-10-01

## Summary

Replace static plugin references in `ConsoleDungeon.Host` with dynamic plugin loading based on configuration files. Plugins should be loaded at runtime via `ActualPluginLoader` and `AssemblyContextProvider`, enabling runtime composition, hot-reload support, and plugin discovery without recompilation.

## Motivation

### Current Problem

`ConsoleDungeon.Host` currently has **static compile-time references** to plugin assemblies:

```xml
<!-- ConsoleDungeon.Host.csproj -->
<ItemGroup>
  <ProjectReference Include="../WingedBean.Plugins.WebSocket/..." />
  <ProjectReference Include="../WingedBean.Plugins.TerminalUI/..." />
  <ProjectReference Include="../WingedBean.Plugins.Config/..." />
</ItemGroup>
```

And loads them statically in code:

```csharp
// Program.cs
var webSocketService = new SuperSocketWebSocketService();
var terminalUIService = new TerminalGuiService();
var configService = new ConfigService();

registry.Register<IWebSocketService>(webSocketService, priority: 100);
registry.Register<ITerminalUIService>(terminalUIService, priority: 100);
registry.Register<IConfigService>(configService, priority: 100);
```

### Problems with Static Loading

1. **Violates Plugin Architecture**: Defeats the purpose of having `IPluginLoader`
2. **No Runtime Composition**: Cannot change plugins without recompiling
3. **No Hot Reload**: Cannot update plugins while app is running
4. **Tight Coupling**: Host has compile-time dependency on all plugins
5. **No Discovery**: Cannot add new plugins without modifying Host project
6. **Configuration Ignored**: Plugin configuration system exists but unused
7. **Testing Difficulty**: Cannot easily mock or replace plugins for testing

### Benefits of Dynamic Loading

1. ✅ **True Plugin Architecture**: Host doesn't know about specific plugins
2. ✅ **Runtime Composition**: Change plugins via config file
3. ✅ **Hot Reload Ready**: Foundation for plugin updates without restart
4. ✅ **Loose Coupling**: Host only depends on contracts and plugin loader
5. ✅ **Easy Discovery**: Drop DLL in folder, add to config, done
6. ✅ **Configuration-Driven**: Control loading order, priorities, strategies
7. ✅ **Better Testing**: Easily substitute mock plugins

## Proposal

### Plugin Configuration File Format

Create `plugins.json` in `ConsoleDungeon.Host` project:

```json
{
  "version": "1.0",
  "pluginDirectory": "plugins",
  "plugins": [
    {
      "id": "wingedbean.plugins.config",
      "path": "plugins/WingedBean.Plugins.Config.dll",
      "priority": 1000,
      "loadStrategy": "Eager",
      "enabled": true,
      "metadata": {
        "description": "Configuration service using Microsoft.Extensions.Configuration",
        "author": "WingedBean",
        "version": "1.0.0"
      }
    },
    {
      "id": "wingedbean.plugins.websocket",
      "path": "plugins/WingedBean.Plugins.WebSocket.dll",
      "priority": 100,
      "loadStrategy": "Eager",
      "enabled": true,
      "metadata": {
        "description": "WebSocket service using SuperSocket",
        "author": "WingedBean",
        "version": "1.0.0"
      }
    },
    {
      "id": "wingedbean.plugins.terminalui",
      "path": "plugins/WingedBean.Plugins.TerminalUI.dll",
      "priority": 100,
      "loadStrategy": "Eager",
      "enabled": true,
      "metadata": {
        "description": "Terminal UI service using Terminal.Gui v2",
        "author": "WingedBean",
        "version": "1.0.0"
      }
    },
    {
      "id": "wingedbean.plugins.ecs",
      "path": "plugins/WingedBean.Plugins.ArchECS.dll",
      "priority": 50,
      "loadStrategy": "Lazy",
      "enabled": true,
      "metadata": {
        "description": "Entity Component System using Arch",
        "author": "WingedBean",
        "version": "1.0.0"
      }
    }
  ]
}
```

### Configuration Schema

```csharp
public class PluginConfiguration
{
    public string Version { get; set; } = "1.0";
    public string PluginDirectory { get; set; } = "plugins";
    public List<PluginDescriptor> Plugins { get; set; } = new();
}

public class PluginDescriptor
{
    public string Id { get; set; } = "";
    public string Path { get; set; } = "";
    public int Priority { get; set; } = 0;
    public LoadStrategy LoadStrategy { get; set; } = LoadStrategy.Eager;
    public bool Enabled { get; set; } = true;
    public Dictionary<string, string>? Metadata { get; set; }
    public List<string>? Dependencies { get; set; }
}

public enum LoadStrategy
{
    Eager,   // Load immediately at startup
    Lazy,    // Load on first use
    Explicit // Load only when explicitly requested
}
```

### Updated Program.cs Implementation

**Remove static plugin references:**

```xml
<!-- ConsoleDungeon.Host.csproj - BEFORE -->
<ItemGroup>
  <ProjectReference Include="../ConsoleDungeon/ConsoleDungeon.csproj" />
  <ProjectReference Include="../../../framework/src/WingedBean.Registry/WingedBean.Registry.csproj" />
  <ProjectReference Include="../WingedBean.Plugins.WebSocket/WingedBean.Plugins.WebSocket.csproj" />
  <ProjectReference Include="../WingedBean.Plugins.TerminalUI/WingedBean.Plugins.TerminalUI.csproj" />
  <ProjectReference Include="../WingedBean.Plugins.Config/WingedBean.Plugins.Config.csproj" />
</ItemGroup>
```

```xml
<!-- ConsoleDungeon.Host.csproj - AFTER -->
<ItemGroup>
  <ProjectReference Include="../ConsoleDungeon/ConsoleDungeon.csproj" />
  <ProjectReference Include="../../../framework/src/WingedBean.Registry/WingedBean.Registry.csproj" />
  <ProjectReference Include="../../shared/WingedBean.PluginLoader/WingedBean.PluginLoader.csproj" />
  <ProjectReference Include="../../providers/WingedBean.Providers.AssemblyContext/WingedBean.Providers.AssemblyContext.csproj" />
</ItemGroup>

<ItemGroup>
  <None Update="plugins.json">
    <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
  </None>
</ItemGroup>
```

**New Program.cs with dynamic loading:**

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

        try
        {
            // Step 1: Create foundation services
            Console.WriteLine("[1/5] Initializing foundation services...");
            var registry = new ActualRegistry();
            var contextProvider = new AssemblyContextProvider();
            var pluginLoader = new ActualPluginLoader(contextProvider);

            registry.Register<IRegistry>(registry);
            registry.Register<IPluginLoader>(pluginLoader);
            Console.WriteLine("✓ Foundation services initialized\n");

            // Step 2: Load plugin configuration
            Console.WriteLine("[2/5] Loading plugin configuration...");
            var config = await LoadPluginConfigurationAsync("plugins.json");
            var enabledPlugins = config.Plugins
                .Where(p => p.Enabled)
                .OrderByDescending(p => p.Priority)
                .ToList();
            Console.WriteLine($"✓ Found {enabledPlugins.Count} enabled plugins\n");

            // Step 3: Load plugins dynamically
            Console.WriteLine("[3/5] Loading plugins...");
            var loadedPlugins = new Dictionary<string, ILoadedPlugin>();

            foreach (var descriptor in enabledPlugins)
            {
                if (descriptor.LoadStrategy != LoadStrategy.Eager)
                {
                    Console.WriteLine($"  ⊘ Skipping {descriptor.Id} (strategy: {descriptor.LoadStrategy})");
                    continue;
                }

                Console.WriteLine($"  → Loading: {descriptor.Id} (priority: {descriptor.Priority})");

                try
                {
                    var plugin = await pluginLoader.LoadAsync(descriptor.Path);
                    loadedPlugins[descriptor.Id] = plugin;

                    Console.WriteLine($"    ✓ Loaded: {plugin.Manifest.Id} v{plugin.Manifest.Version}");

                    // Auto-register services from plugin
                    await RegisterPluginServicesAsync(registry, plugin, descriptor.Priority);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"    ✗ Failed to load {descriptor.Id}: {ex.Message}");

                    // Check if plugin is critical
                    if (descriptor.Priority >= 1000)
                    {
                        Console.WriteLine($"    CRITICAL: Plugin {descriptor.Id} failed to load. Aborting.");
                        return;
                    }
                }
            }

            Console.WriteLine($"✓ {loadedPlugins.Count} plugins loaded successfully\n");

            // Step 4: Verify required services
            Console.WriteLine("[4/5] Verifying service registry...");
            VerifyRequiredServices(registry);
            Console.WriteLine("✓ All required services registered\n");

            // Step 5: Launch application
            Console.WriteLine("[5/5] Launching ConsoleDungeon...\n");
            var app = new ConsoleDungeon.Program(registry);
            await app.RunAsync();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"\n❌ FATAL ERROR: {ex.Message}");
            Console.WriteLine($"Stack trace:\n{ex.StackTrace}");
            Environment.Exit(1);
        }
    }

    private static async Task<PluginConfiguration> LoadPluginConfigurationAsync(string path)
    {
        if (!File.Exists(path))
        {
            throw new FileNotFoundException($"Plugin configuration file not found: {path}");
        }

        var json = await File.ReadAllTextAsync(path);
        var config = JsonSerializer.Deserialize<PluginConfiguration>(json, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            ReadCommentHandling = JsonCommentHandling.Skip
        });

        return config ?? throw new InvalidOperationException("Failed to parse plugin configuration");
    }

    private static async Task RegisterPluginServicesAsync(
        IRegistry registry,
        ILoadedPlugin plugin,
        int priority)
    {
        // Activate plugin (if it implements IPlugin)
        await plugin.ActivateAsync();

        // Get all services from plugin
        var services = plugin.GetServices();

        foreach (var service in services)
        {
            // Find the contract interface (in WingedBean.Contracts.* namespace)
            var contractType = FindContractInterface(service.GetType());

            if (contractType != null)
            {
                registry.Register(contractType, service, priority);
                Console.WriteLine($"      → Registered: {contractType.Name} (priority: {priority})");
            }
            else
            {
                Console.WriteLine($"      ⚠ Warning: Service {service.GetType().Name} has no contract interface");
            }
        }
    }

    private static Type? FindContractInterface(Type implementationType)
    {
        // Find interfaces from WingedBean.Contracts.* namespaces
        return implementationType.GetInterfaces()
            .FirstOrDefault(i => i.Namespace?.StartsWith("WingedBean.Contracts") == true);
    }

    private static void VerifyRequiredServices(IRegistry registry)
    {
        var requiredServices = new[]
        {
            typeof(IConfigService),
            typeof(IWebSocketService),
            typeof(ITerminalUIService)
        };

        foreach (var serviceType in requiredServices)
        {
            if (!registry.IsRegistered(serviceType))
            {
                throw new InvalidOperationException(
                    $"Required service {serviceType.Name} is not registered. " +
                    "Check plugin configuration and ensure required plugins are enabled.");
            }

            Console.WriteLine($"  ✓ {serviceType.Name} registered");
        }
    }
}
```

### Plugin Assembly Copy Mechanism

**Create MSBuild target to copy plugins:**

File: `console/src/host/ConsoleDungeon.Host/build/copy-plugins.targets`

```xml
<Project>
  <Target Name="CopyPlugins" AfterTargets="Build">
    <Message Importance="high" Text="========================================" />
    <Message Importance="high" Text="Copying plugin assemblies..." />
    <Message Importance="high" Text="========================================" />

    <ItemGroup>
      <!-- Get all plugin DLLs and dependencies -->
      <PluginFiles Include="../../plugins/**/bin/$(Configuration)/$(TargetFramework)/*.dll" />
      <PluginFiles Include="../../plugins/**/bin/$(Configuration)/$(TargetFramework)/*.pdb" />
      <PluginFiles Include="../../plugins/**/bin/$(Configuration)/$(TargetFramework)/.plugin.json" />
    </ItemGroup>

    <!-- Create plugins directory -->
    <MakeDir Directories="$(OutDir)plugins/" Condition="!Exists('$(OutDir)plugins/')" />

    <!-- Copy plugins -->
    <Copy
      SourceFiles="@(PluginFiles)"
      DestinationFolder="$(OutDir)plugins/"
      SkipUnchangedFiles="true"
      OverwriteReadOnlyFiles="true" />

    <Message Importance="high" Text="Copied @(PluginFiles->Count()) plugin files to $(OutDir)plugins/" />
  </Target>

  <Target Name="CleanPlugins" AfterTargets="Clean">
    <Message Importance="high" Text="Cleaning plugin directory..." />
    <RemoveDir Directories="$(OutDir)plugins/" Condition="Exists('$(OutDir)plugins/')" />
  </Target>
</Project>
```

Import in `ConsoleDungeon.Host.csproj`:

```xml
<Import Project="build/copy-plugins.targets" />
```

### Plugin Manifest Enhancement

Each plugin should have a `.plugin.json` manifest:

```json
{
  "id": "wingedbean.plugins.websocket",
  "version": "1.0.0",
  "name": "WebSocket Service",
  "description": "WebSocket server using SuperSocket",
  "author": "WingedBean Team",
  "provides": [
    "WingedBean.Contracts.WebSocket.IWebSocketService"
  ],
  "dependencies": [],
  "loadStrategy": "Eager",
  "entryPoint": "WingedBean.Plugins.WebSocket.SuperSocketWebSocketService"
}
```

Update plugin projects to copy manifest:

```xml
<ItemGroup>
  <None Include=".plugin.json">
    <CopyToOutputDirectory>Always</CopyToOutputDirectory>
  </None>
</ItemGroup>
```

## Migration Plan

### Phase 1: Create Configuration Infrastructure (Day 1)

1. **Create `PluginConfiguration` models:**
   - `PluginConfiguration.cs`
   - `PluginDescriptor.cs`
   - `LoadStrategy.cs`

2. **Create `plugins.json` in ConsoleDungeon.Host**

3. **Create `copy-plugins.targets` MSBuild file**

4. **Test configuration loading:**
```bash
cd console/src/host/ConsoleDungeon.Host
dotnet build
# Verify plugins.json copied to bin/Debug/net8.0/
# Verify plugins/ directory created
```

### Phase 2: Update Program.cs (Day 2)

1. **Remove static `using` statements for plugin namespaces**

2. **Implement dynamic loading logic** (see full implementation above)

3. **Test loading without running:**
```bash
dotnet build
# Should compile without errors
# Should not reference plugin types directly
```

### Phase 3: Remove Static References (Day 2)

1. **Update ConsoleDungeon.Host.csproj:**
   - Remove `<ProjectReference>` to plugin projects
   - Keep only foundation services (Registry, PluginLoader, AssemblyContext)

2. **Build and test:**
```bash
dotnet clean
dotnet build
# Should build successfully
# Plugins should copy to output directory
```

### Phase 4: Update Plugin Manifests (Day 3)

1. **Create `.plugin.json` for each plugin:**
   - WebSocket plugin
   - TerminalUI plugin
   - Config plugin
   - PtyService plugin
   - AsciinemaRecorder plugin

2. **Update plugin `.csproj` files to copy manifests**

3. **Verify manifests copy to output:**
```bash
dotnet build console/Console.sln
ls -la console/src/host/ConsoleDungeon.Host/bin/Debug/net8.0/plugins/
# Should see *.dll and .plugin.json files
```

### Phase 5: End-to-End Testing (Day 3)

1. **Run ConsoleDungeon.Host:**
```bash
cd console/src/host/ConsoleDungeon.Host
dotnet run
```

Expected output:
```
========================================
ConsoleDungeon.Host - Dynamic Plugin Mode
========================================

[1/5] Initializing foundation services...
✓ Foundation services initialized

[2/5] Loading plugin configuration...
✓ Found 3 enabled plugins

[3/5] Loading plugins...
  → Loading: wingedbean.plugins.config (priority: 1000)
    ✓ Loaded: wingedbean.plugins.config v1.0.0
      → Registered: IConfigService (priority: 1000)
  → Loading: wingedbean.plugins.websocket (priority: 100)
    ✓ Loaded: wingedbean.plugins.websocket v1.0.0
      → Registered: IWebSocketService (priority: 100)
  → Loading: wingedbean.plugins.terminalui (priority: 100)
    ✓ Loaded: wingedbean.plugins.terminalui v1.0.0
      → Registered: ITerminalUIService (priority: 100)
✓ 3 plugins loaded successfully

[4/5] Verifying service registry...
  ✓ IConfigService registered
  ✓ IWebSocketService registered
  ✓ ITerminalUIService registered
✓ All required services registered

[5/5] Launching ConsoleDungeon...
```

2. **Test xterm.js integration:**
```bash
# Terminal 1: Run app
cd console/src/host/ConsoleDungeon.Host
dotnet run

# Terminal 2: Run frontend
cd development/nodejs/sites/docs
npm run dev
```

Open browser, verify Terminal.Gui renders in xterm.js.

3. **Test plugin disable:**

Edit `plugins.json`:
```json
{
  "id": "wingedbean.plugins.websocket",
  "enabled": false,  // Disable WebSocket
  ...
}
```

Run app, verify error:
```
❌ FATAL ERROR: Required service IWebSocketService is not registered.
```

Re-enable, verify it works again.

## Benefits

### Architecture

- ✅ True plugin architecture realized
- ✅ Host decoupled from plugin implementations
- ✅ IPluginLoader actually used
- ✅ Configuration-driven composition

### Development

- ✅ Add plugins without touching Host code
- ✅ Test with mock plugins easily
- ✅ Debug specific plugin combinations
- ✅ Parallel plugin development

### Operations

- ✅ Change plugins without recompiling
- ✅ A/B test different plugin implementations
- ✅ Hot-reload foundation (future)
- ✅ Plugin versioning support

### User Experience

- ✅ Customize plugin set via config
- ✅ Enable/disable features easily
- ✅ Better error messages (which plugin failed)
- ✅ Plugin discovery UI (future)

## Risks and Mitigations

### Risk: Plugin Not Found

**Scenario:** Plugin DLL missing from `plugins/` directory

**Mitigation:**
- Check `plugins.json` paths are correct
- Verify `copy-plugins.targets` working
- Clear error messages indicating missing file
- Fallback to default plugins (future)

### Risk: Plugin Load Failure

**Scenario:** Plugin throws exception during load

**Mitigation:**
- Try-catch around each plugin load
- Log detailed error messages
- Mark plugin as failed, continue loading others
- Critical plugins (priority >= 1000) abort startup

### Risk: Circular Dependencies

**Scenario:** Plugin A depends on B, B depends on A

**Mitigation:**
- Document: plugins should not depend on each other
- Plugins only depend on contracts
- Dependency resolution algorithm (future)
- Validation tool to detect cycles (future)

### Risk: Performance Overhead

**Scenario:** Dynamic loading slower than static

**Reality:**
- Load happens once at startup
- Millisecond difference (negligible)
- Benefits far outweigh tiny performance cost

**Mitigation:**
- Profile startup time
- Cache loaded plugins
- Lazy loading for non-critical plugins

## Future Enhancements

### Hot Reload Support

```csharp
public class PluginWatcher
{
    private readonly FileSystemWatcher _watcher;

    public PluginWatcher(string pluginDirectory)
    {
        _watcher = new FileSystemWatcher(pluginDirectory, "*.dll");
        _watcher.Changed += OnPluginChanged;
    }

    private async void OnPluginChanged(object sender, FileSystemEventArgs e)
    {
        // Unload old plugin
        await _pluginLoader.UnloadAsync(oldPlugin);

        // Load new plugin
        var newPlugin = await _pluginLoader.LoadAsync(e.FullPath);

        // Re-register services
        // ...
    }
}
```

### Plugin Discovery UI

```
Available Plugins:
  [✓] WebSocket Service    v1.0.0  (enabled)
  [✓] Terminal UI         v1.0.0  (enabled)
  [✓] Config Service      v1.0.0  (enabled)
  [ ] Arch ECS            v1.0.0  (disabled)
  [ ] Audio Service       v1.0.0  (not installed)

Press 'p' to manage plugins...
```

### Plugin Marketplace

- Download plugins from central repository
- Automatic updates
- Community plugins
- Rating/review system

## Definition of Done

### Configuration
- [ ] `PluginConfiguration` models created
- [ ] `plugins.json` created with all current plugins
- [ ] JSON schema documented

### Build System
- [ ] `copy-plugins.targets` created and working
- [ ] Plugins copy to output directory automatically
- [ ] Clean target removes plugins

### Host Implementation
- [ ] Static plugin references removed from `.csproj`
- [ ] Program.cs uses dynamic loading
- [ ] Error handling for plugin failures
- [ ] Service verification logic implemented

### Plugin Manifests
- [ ] All plugins have `.plugin.json` files
- [ ] Manifests include all required metadata
- [ ] Manifests copy to output directory

### Testing
- [ ] Host builds successfully
- [ ] All plugins load dynamically
- [ ] Services register correctly
- [ ] ConsoleDungeon.Host runs without errors
- [ ] xterm.js integration still works
- [ ] Plugin disable/enable works correctly

### Documentation
- [ ] Plugin configuration format documented
- [ ] Plugin development guide updated
- [ ] Migration guide for existing plugins
- [ ] Example plugin manifest provided

## Dependencies

- RFC-0005: Target Framework Compliance (should be done first)
- Existing `ActualPluginLoader` implementation
- Existing `AssemblyContextProvider` implementation

## References

- RFC-0003: Plugin Architecture Foundation
- `WingedBean.PluginLoader` implementation
- `WingedBean.Providers.AssemblyContext` implementation
- Plugin pattern documentation

## Notes

- This RFC completes the plugin architecture vision
- Enables true modular architecture
- Foundation for hot reload, plugin marketplace
- Should be completed before adding more plugins (e.g., Arch ECS)

---

**Author:** System Analysis
**Reviewers:** [Pending]
**Status:** Proposed - Awaiting approval
**Priority:** HIGH (P1)
**Estimated Effort:** 3 days
**Target Date:** 2025-10-06
**Dependencies:** RFC-0005

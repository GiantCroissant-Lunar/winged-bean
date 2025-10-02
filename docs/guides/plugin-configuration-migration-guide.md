# Plugin Configuration Migration Guide

**Purpose**: Guide for migrating from static plugin references to dynamic plugin loading  
**Audience**: Developers maintaining or extending the Winged Bean plugin system  
**Prerequisites**: Understanding of .NET project structure and basic plugin concepts

---

## Overview

This guide helps you migrate from **static compile-time plugin references** to **dynamic runtime plugin loading** using the configuration-driven system introduced in RFC-0006.

### Benefits of Dynamic Loading

- ✅ **No recompilation required** - Change plugins via configuration file
- ✅ **Loose coupling** - Host doesn't depend on specific plugin implementations
- ✅ **Easy plugin discovery** - Drop DLL in folder, add to config
- ✅ **Runtime composition** - Control loading order, priorities, and strategies
- ✅ **Better error handling** - Graceful degradation when plugins fail
- ✅ **Hot reload ready** - Foundation for plugin updates without restart

---

## Migration from Static to Dynamic Loading

### Step 1: Understand Your Current Setup

**Before (Static Loading):**

Your host project likely has static references to plugins:

```xml
<!-- ConsoleDungeon.Host.csproj - BEFORE -->
<ItemGroup>
  <ProjectReference Include="../ConsoleDungeon/ConsoleDungeon.csproj" />
  <ProjectReference Include="../WingedBean.Plugins.WebSocket/WingedBean.Plugins.WebSocket.csproj" />
  <ProjectReference Include="../WingedBean.Plugins.TerminalUI/WingedBean.Plugins.TerminalUI.csproj" />
  <ProjectReference Include="../WingedBean.Plugins.Config/WingedBean.Plugins.Config.csproj" />
</ItemGroup>
```

And manual instantiation in code:

```csharp
// Program.cs - BEFORE
var webSocketService = new SuperSocketWebSocketService();
var terminalUIService = new TerminalGuiService();
var configService = new ConfigService();

registry.Register<IWebSocketService>(webSocketService, priority: 100);
registry.Register<ITerminalUIService>(terminalUIService, priority: 100);
registry.Register<IConfigService>(configService, priority: 100);
```

### Step 2: Create Plugin Configuration File

Create a `plugins.json` file in your host project root:

```json
{
  "version": "1.0",
  "pluginDirectory": "plugins",
  "plugins": [
    {
      "id": "wingedbean.plugins.config",
      "path": "plugins/WingedBean.Plugins.Config/bin/Debug/net8.0/WingedBean.Plugins.Config.dll",
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
      "path": "plugins/WingedBean.Plugins.WebSocket/bin/Debug/net8.0/WingedBean.Plugins.WebSocket.dll",
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
      "path": "plugins/WingedBean.Plugins.TerminalUI/bin/Debug/net8.0/WingedBean.Plugins.TerminalUI.dll",
      "priority": 100,
      "loadStrategy": "Eager",
      "enabled": true,
      "metadata": {
        "description": "Terminal UI service using Terminal.Gui v2",
        "author": "WingedBean",
        "version": "1.0.0"
      }
    }
  ]
}
```

**Key Configuration Fields:**

| Field | Type | Description |
|-------|------|-------------|
| `id` | string | Unique plugin identifier (lowercase, dot-separated) |
| `path` | string | Relative path to plugin DLL from host output directory |
| `priority` | number | Loading priority (1000+ = critical, 100-999 = core, 0-99 = optional) |
| `loadStrategy` | string | `"Eager"` (load at startup), `"Lazy"` (load on demand), `"Explicit"` |
| `enabled` | boolean | Whether to load this plugin |
| `metadata` | object | Additional plugin information (description, author, version) |

### Step 3: Add Configuration Models to Host Project

If not already present, add these classes to your host project:

```csharp
// PluginConfiguration.cs
public class PluginConfiguration
{
    public string Version { get; set; } = "1.0";
    public string PluginDirectory { get; set; } = "plugins";
    public List<PluginDescriptor> Plugins { get; set; } = new();
}

// PluginDescriptor.cs
public class PluginDescriptor
{
    public string Id { get; set; } = "";
    public string Path { get; set; } = "";
    public int Priority { get; set; } = 0;
    public LoadStrategy LoadStrategy { get; set; } = LoadStrategy.Eager;
    public bool Enabled { get; set; } = true;
    public Dictionary<string, string>? Metadata { get; set; }
}

// LoadStrategy.cs
public enum LoadStrategy
{
    Eager,    // Load at startup
    Lazy,     // Load on first use
    Explicit  // Load only when explicitly requested
}
```

### Step 4: Update Host Project References

Remove plugin project references and add loader dependencies:

```xml
<!-- ConsoleDungeon.Host.csproj - AFTER -->
<ItemGroup>
  <!-- Keep foundation services -->
  <ProjectReference Include="../ConsoleDungeon/ConsoleDungeon.csproj" />
  <ProjectReference Include="../../../framework/src/WingedBean.Registry/WingedBean.Registry.csproj" />
  
  <!-- Add plugin loading infrastructure -->
  <ProjectReference Include="../../shared/WingedBean.PluginLoader/WingedBean.PluginLoader.csproj" />
  <ProjectReference Include="../../providers/WingedBean.Providers.AssemblyContext/WingedBean.Providers.AssemblyContext.csproj" />
</ItemGroup>

<!-- Ensure plugins.json is copied to output -->
<ItemGroup>
  <None Update="plugins.json">
    <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
  </None>
</ItemGroup>
```

### Step 5: Create MSBuild Targets to Copy Plugins

Create `build/copy-plugins.targets` in your host project directory:

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

Import in your host `.csproj`:

```xml
<Import Project="build/copy-plugins.targets" />
```

### Step 6: Update Program.cs for Dynamic Loading

Replace static instantiation with dynamic loading:

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
        // List your required service types
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

### Step 7: Build and Test

Build your project to verify the migration:

```bash
# Clean previous build
dotnet clean

# Build with new configuration
dotnet build

# Verify plugins directory created
ls -la bin/Debug/net8.0/plugins/

# Run application
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

---

## Troubleshooting Guide

### Issue 1: Plugin Assembly Not Found

**Symptom:**

```
  → Loading: wingedbean.plugins.config (priority: 1000)
    ✗ Failed to load wingedbean.plugins.config: Plugin assembly not found: plugins/WingedBean.Plugins.Config.dll
```

**Possible Causes:**

1. **Incorrect path in `plugins.json`**
   - Path should be relative to host output directory
   - Must include subdirectory structure if plugins are organized by name

2. **MSBuild targets not copying plugins**
   - Verify `copy-plugins.targets` imported in `.csproj`
   - Check plugin projects are building before host

3. **Plugin project not building**
   - Ensure plugin projects are in solution
   - Verify plugin `.csproj` has valid configuration

**Solutions:**

1. **Verify plugin path:**
   ```bash
   # From host output directory
   ls -la plugins/
   # Should see plugin DLLs
   ```

2. **Check MSBuild output:**
   ```bash
   dotnet build -v detailed | grep -i "copying plugin"
   ```

3. **Update path in plugins.json:**
   ```json
   {
     "path": "plugins/WingedBean.Plugins.Config/bin/Debug/net8.0/WingedBean.Plugins.Config.dll"
   }
   ```

### Issue 2: Dependency Assembly Not Found

**Symptom:**

```
✗ Failed to load wingedbean.plugins.config: Could not load file or assembly 
'WingedBean.Contracts.Config, Version=1.0.0.0'
```

**Possible Causes:**

1. **Plugin dependencies not copied to output**
2. **Missing `CopyLocalLockFileAssemblies` setting**
3. **Assembly version mismatch**

**Solutions:**

1. **Update plugin `.csproj`:**
   ```xml
   <PropertyGroup>
     <CopyLocalLockFileAssemblies>true</CopyLocalLockFileAssemblies>
   </PropertyGroup>
   ```

2. **Rebuild plugin project:**
   ```bash
   dotnet clean
   dotnet build
   ```

3. **Verify dependencies copied:**
   ```bash
   ls -la plugins/WingedBean.Plugins.Config/bin/Debug/net8.0/
   # Should see all dependency DLLs
   ```

### Issue 3: Service Not Registered

**Symptom:**

```
❌ FATAL ERROR: Required service IConfigService is not registered.
```

**Possible Causes:**

1. **Plugin disabled in configuration**
2. **Plugin failed to load (check earlier output)**
3. **Service class doesn't implement expected interface**
4. **Interface not in WingedBean.Contracts namespace**

**Solutions:**

1. **Check plugin enabled:**
   ```json
   {
     "id": "wingedbean.plugins.config",
     "enabled": true  // Must be true
   }
   ```

2. **Check plugin load output:**
   ```
   [3/5] Loading plugins...
     → Loading: wingedbean.plugins.config (priority: 1000)
       ✓ Loaded: wingedbean.plugins.config v1.0.0
         → Registered: IConfigService (priority: 1000)
   ```

3. **Verify service implements interface:**
   ```csharp
   // Service must implement contract interface
   public class ConfigService : IConfigService
   {
       // Implementation
   }
   ```

### Issue 4: Wrong Service Implementation Used

**Symptom:**
- Multiple plugins provide the same service
- Wrong implementation is being used

**Possible Causes:**

1. **Priority misconfiguration**
2. **Using wrong selection mode**

**Solutions:**

1. **Check priorities in `plugins.json`:**
   ```json
   {
     "id": "wingedbean.plugins.config.production",
     "priority": 1000  // Higher priority = preferred
   },
   {
     "id": "wingedbean.plugins.config.mock",
     "priority": 10    // Lower priority = fallback
   }
   ```

2. **Use appropriate selection mode:**
   ```csharp
   // Get highest priority implementation
   var config = registry.Get<IConfigService>(SelectionMode.HighestPriority);
   
   // Get all implementations
   var allConfigs = registry.GetAll<IConfigService>();
   ```

### Issue 5: Critical Plugin Failure Aborts Startup

**Symptom:**

```
  → Loading: wingedbean.plugins.config (priority: 1000)
    ✗ Failed to load wingedbean.plugins.config: [error message]
    CRITICAL: Plugin wingedbean.plugins.config failed to load. Aborting.
```

**Explanation:**
- Plugins with priority >= 1000 are considered **critical**
- If a critical plugin fails, the application aborts to prevent undefined behavior

**Solutions:**

1. **Fix the plugin error** (see other troubleshooting sections)

2. **Lower priority if plugin is not truly critical:**
   ```json
   {
     "priority": 100  // Non-critical, will not abort on failure
   }
   ```

3. **Disable plugin temporarily:**
   ```json
   {
     "enabled": false  // Skip loading this plugin
   }
   ```

### Issue 6: Lazy Plugin Not Loading

**Symptom:**
- Plugin marked as "Lazy" never loads
- Service from lazy plugin not available

**Explanation:**
- Lazy plugins skip initial loading
- Must be loaded explicitly when needed

**Expected Behavior:**

```
[3/5] Loading plugins...
  ⊘ Skipping wingedbean.plugins.asciinemarecorder (strategy: Lazy)
```

**Solutions:**

1. **Change to Eager if needed at startup:**
   ```json
   {
     "loadStrategy": "Eager"  // Load immediately
   }
   ```

2. **Load explicitly in code:**
   ```csharp
   // Load lazy plugin when needed
   var pluginLoader = registry.Get<IPluginLoader>();
   var plugin = await pluginLoader.LoadAsync("path/to/plugin.dll");
   ```

---

## Best Practices

### 1. Plugin Priority Guidelines

Use appropriate priority ranges based on plugin criticality:

| Priority Range | Category | Behavior on Failure | Use For |
|----------------|----------|---------------------|---------|
| 1000+ | **Critical Infrastructure** | Abort startup | Configuration, Registry, Core Services |
| 500-999 | **Core Services** | Continue with warning | Database, Authentication, Logging |
| 100-499 | **Standard Services** | Continue silently | WebSocket, Terminal UI, Networking |
| 50-99 | **Optional Features** | Continue silently | Recording, Metrics, Analytics |
| 0-49 | **Experimental** | Continue silently | Development tools, Debug features |

**Example Configuration:**

```json
{
  "plugins": [
    {
      "id": "wingedbean.plugins.config",
      "priority": 1000,  // Critical - required for app to function
      "enabled": true
    },
    {
      "id": "wingedbean.plugins.websocket",
      "priority": 100,   // Standard - important but not critical
      "enabled": true
    },
    {
      "id": "wingedbean.plugins.asciinemarecorder",
      "priority": 80,    // Optional - nice to have
      "enabled": true
    }
  ]
}
```

### 2. Load Strategy Selection

Choose the appropriate load strategy for each plugin:

**Eager Loading:**
```json
{
  "loadStrategy": "Eager"
}
```
- Use for: Critical services, frequently used features
- Pros: Immediate availability, fail fast
- Cons: Slower startup, higher initial memory

**Lazy Loading:**
```json
{
  "loadStrategy": "Lazy"
}
```
- Use for: Optional features, rarely used services
- Pros: Faster startup, lower initial memory
- Cons: First-use delay, deferred failure detection

**Explicit Loading:**
```json
{
  "loadStrategy": "Explicit"
}
```
- Use for: Development tools, admin features, on-demand functionality
- Pros: Maximum control, minimal overhead
- Cons: Manual load management required

### 3. Configuration Organization

**Separate environments:**

```json
{
  "version": "1.0",
  "pluginDirectory": "plugins",
  "plugins": [
    {
      "id": "wingedbean.plugins.config",
      "enabled": true,
      "priority": 1000
    }
  ],
  "environments": {
    "development": {
      "debugPlugins": true,
      "loadMockServices": true
    },
    "production": {
      "debugPlugins": false,
      "loadMockServices": false
    }
  }
}
```

**Use comments for documentation:**

```json
{
  "plugins": [
    {
      // Core configuration service - DO NOT DISABLE
      "id": "wingedbean.plugins.config",
      "priority": 1000,
      "enabled": true
    },
    {
      // Optional recording feature - can be disabled to improve performance
      "id": "wingedbean.plugins.asciinemarecorder",
      "priority": 80,
      "loadStrategy": "Lazy",
      "enabled": true
    }
  ]
}
```

### 4. Error Handling Best Practices

**Always wrap plugin operations in try-catch:**

```csharp
try
{
    var plugin = await pluginLoader.LoadAsync(descriptor.Path);
    await RegisterPluginServicesAsync(registry, plugin, descriptor.Priority);
}
catch (FileNotFoundException ex)
{
    Console.WriteLine($"    ✗ Plugin not found: {ex.Message}");
}
catch (TypeLoadException ex)
{
    Console.WriteLine($"    ✗ Type load failed: {ex.Message}");
}
catch (Exception ex)
{
    Console.WriteLine($"    ✗ Unexpected error: {ex.Message}");
}
```

**Log detailed information:**

```csharp
Console.WriteLine($"  → Loading: {descriptor.Id}");
Console.WriteLine($"    Path: {descriptor.Path}");
Console.WriteLine($"    Priority: {descriptor.Priority}");
Console.WriteLine($"    Strategy: {descriptor.LoadStrategy}");
```

### 5. Testing Plugin Configuration

**Create test configurations:**

```json
// plugins.test.json - minimal config for testing
{
  "version": "1.0",
  "pluginDirectory": "plugins",
  "plugins": [
    {
      "id": "wingedbean.plugins.config",
      "path": "plugins/WingedBean.Plugins.Config.dll",
      "priority": 1000,
      "loadStrategy": "Eager",
      "enabled": true
    }
  ]
}
```

**Test plugin enable/disable:**

```bash
# Disable a plugin
# Edit plugins.json: "enabled": false

dotnet run
# Verify error message for missing service

# Re-enable plugin
# Edit plugins.json: "enabled": true

dotnet run
# Verify application works again
```

**Test priority ordering:**

```bash
# Change plugin priorities in plugins.json
# Higher priority loads first and is preferred by registry

dotnet run
# Check load order in output
```

### 6. Documentation and Metadata

Always include helpful metadata:

```json
{
  "id": "wingedbean.plugins.custom",
  "path": "plugins/WingedBean.Plugins.Custom.dll",
  "priority": 100,
  "loadStrategy": "Eager",
  "enabled": true,
  "metadata": {
    "description": "Custom functionality for feature X",
    "author": "Team Name",
    "version": "1.0.0",
    "homepage": "https://example.com/docs/custom-plugin",
    "lastUpdated": "2025-10-01"
  }
}
```

---

## Related Documentation

- [Plugin Development Guide](plugin-development-guide.md) - Creating new plugins
- [Architecture Overview](architecture-overview.md) - System architecture
- [RFC-0006: Dynamic Plugin Loading](../rfcs/0006-dynamic-plugin-loading.md) - Original proposal

---

**Last Updated**: 2025-10-02  
**Version**: 1.0.0  
**RFC**: RFC-0006  
**Status**: ✅ Complete

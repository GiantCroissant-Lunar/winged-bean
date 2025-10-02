# Plugin Development Guide

This guide walks you through creating .NET plugins for the WingedBean plugin system, from basic setup to advanced patterns.

## Table of Contents

1. [Quick Start](#quick-start)
2. [Plugin Manifest Format](#plugin-manifest-format)
3. [Service Registration Patterns](#service-registration-patterns)
4. [Plugin Lifecycle](#plugin-lifecycle)
5. [Configuration Schema](#configuration-schema)
6. [Best Practices](#best-practices)
7. [Troubleshooting](#troubleshooting)

## Quick Start

### Prerequisites

- .NET 8.0 SDK or later
- Understanding of dependency injection concepts
- Familiarity with C# interfaces and attributes

### Step 1: Create a Plugin Project

Create a new class library targeting .NET 8.0:

```bash
cd development/dotnet/console/src/plugins
dotnet new classlib -n WingedBean.Plugins.MyPlugin -f net8.0
cd WingedBean.Plugins.MyPlugin
```

### Step 2: Configure Project File

Update your `.csproj` file with required settings and references:

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <LangVersion>latest</LangVersion>
    <AssemblyTitle>My Plugin</AssemblyTitle>
    <AssemblyDescription>A sample WingedBean plugin</AssemblyDescription>
    <AssemblyVersion>1.0.0.0</AssemblyVersion>
  </PropertyGroup>

  <!-- Ensure all dependencies are copied to output -->
  <PropertyGroup>
    <CopyLocalLockFileAssemblies>true</CopyLocalLockFileAssemblies>
  </PropertyGroup>

  <!-- Reference contract interfaces -->
  <ItemGroup>
    <ProjectReference Include="../../../../framework/src/WingedBean.Contracts.Core/WingedBean.Contracts.Core.csproj" />
    <!-- Add references to specific contract projects you need -->
  </ItemGroup>

  <!-- Copy plugin manifest to output -->
  <ItemGroup>
    <None Include=".plugin.json">
      <CopyToOutputDirectory>Always</CopyToOutputDirectory>
    </None>
  </ItemGroup>

</Project>
```

### Step 3: Create Plugin Manifest

Create a `.plugin.json` file in your project root:

```json
{
  "id": "wingedbean.plugins.myplugin",
  "name": "My Plugin",
  "version": "1.0.0",
  "description": "A sample plugin demonstrating basic patterns",
  "author": "Your Name",
  "provides": [
    "WingedBean.Contracts.MyNamespace.IMyService"
  ],
  "dependencies": [],
  "priority": 50
}
```

**Key Fields:**

- `id`: Unique identifier (use lowercase, dot-separated notation)
- `name`: Human-readable plugin name
- `version`: Semantic version (major.minor.patch)
- `provides`: Array of contract interfaces your plugin implements
- `dependencies`: Array of required plugin IDs (empty if none)
- `priority`: Loading priority (0-1000+, higher = loads first)

### Step 4: Define Your Contract Interface

If you're creating a new service type, first define its contract interface in the appropriate contracts project:

```csharp
// In WingedBean.Contracts.MyNamespace/IMyService.cs
namespace WingedBean.Contracts.MyNamespace;

/// <summary>
/// Contract for my custom service.
/// </summary>
public interface IMyService
{
    /// <summary>
    /// Perform the primary operation.
    /// </summary>
    Task<string> DoWorkAsync(string input);

    /// <summary>
    /// Event raised when work is completed.
    /// </summary>
    event Action<string>? WorkCompleted;
}
```

### Step 5: Implement Your Service

Create the service implementation in your plugin project:

```csharp
// In WingedBean.Plugins.MyPlugin/MyService.cs
using WingedBean.Contracts.Core;
using WingedBean.Contracts.MyNamespace;

namespace WingedBean.Plugins.MyPlugin;

/// <summary>
/// Implementation of IMyService.
/// </summary>
[Plugin(
    Name = "MyService.Default",
    Provides = new[] { typeof(IMyService) },
    Priority = 50
)]
public class MyService : IMyService
{
    private readonly ILogger<MyService>? _logger;

    public event Action<string>? WorkCompleted;

    public MyService()
    {
        // Default constructor required for plugin loading
    }

    public MyService(ILogger<MyService> logger)
    {
        _logger = logger;
    }

    public async Task<string> DoWorkAsync(string input)
    {
        _logger?.LogInformation($"Processing input: {input}");
        
        // Simulate async work
        await Task.Delay(100);
        
        var result = $"Processed: {input}";
        
        // Raise completion event
        WorkCompleted?.Invoke(result);
        
        _logger?.LogInformation($"Work completed: {result}");
        return result;
    }
}
```

### Step 6: Build Your Plugin

Build the plugin to verify everything compiles:

```bash
dotnet build
```

The output should include:
- `WingedBean.Plugins.MyPlugin.dll` - Your plugin assembly
- `.plugin.json` - Plugin manifest
- All dependency DLLs

### Step 7: Register Plugin in Host Configuration

Add your plugin to the host's `plugins.json` file:

```json
{
  "version": "1.0",
  "pluginDirectory": "plugins",
  "plugins": [
    {
      "id": "wingedbean.plugins.myplugin",
      "path": "plugins/WingedBean.Plugins.MyPlugin/bin/Debug/net8.0/WingedBean.Plugins.MyPlugin.dll",
      "priority": 50,
      "loadStrategy": "Eager",
      "enabled": true,
      "metadata": {
        "description": "My custom plugin",
        "author": "Your Name",
        "version": "1.0.0"
      }
    }
  ]
}
```

### Step 8: Test Your Plugin

Run the host application to verify your plugin loads:

```bash
cd ../../host/ConsoleDungeon.Host
dotnet run
```

Look for output indicating your plugin was loaded:

```
[3/5] Loading plugins...
  → Loading: wingedbean.plugins.myplugin (priority: 50)
    ✓ Loaded: wingedbean.plugins.myplugin v1.0.0
      → Registered: IMyService (priority: 50)
```

## Plugin Manifest Format

The `.plugin.json` file describes your plugin's metadata, dependencies, and capabilities.

### Basic Structure

```json
{
  "id": "wingedbean.plugins.example",
  "version": "1.0.0",
  "name": "Example Plugin",
  "description": "Detailed description of plugin functionality",
  "author": "Your Name or Organization",
  "license": "MIT",
  
  "provides": [
    "WingedBean.Contracts.Example.IExampleService"
  ],
  
  "dependencies": [],
  
  "priority": 100
}
```

### Advanced Manifest Fields

For more complex plugins, use these additional fields:

```json
{
  "id": "wingedbean.plugins.advanced",
  "version": "2.1.0",
  "name": "Advanced Plugin",
  "description": "Plugin with advanced configuration",
  "author": "WingedBean Team",
  "license": "MIT",
  
  "provides": [
    "WingedBean.Contracts.Database.IDatabaseService",
    "WingedBean.Contracts.Cache.ICacheService"
  ],
  
  "dependencies": [
    {
      "pluginId": "wingedbean.plugins.config",
      "versionRange": "^1.0.0",
      "optional": false
    }
  ],
  
  "entryPoint": {
    "dotnet": "./WingedBean.Plugins.Advanced.dll",
    "unity": "./WingedBean.Plugins.Advanced.dll"
  },
  
  "capabilities": [
    "database",
    "caching",
    "persistent-storage"
  ],
  
  "supportedProfiles": ["console", "web", "unity"],
  
  "loadStrategy": "eager",
  "priority": 150,
  
  "metadata": {
    "homepage": "https://github.com/example/plugin",
    "repository": "https://github.com/example/plugin.git",
    "tags": ["database", "storage", "cache"]
  }
}
```

### Manifest Field Reference

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| `id` | string | ✅ | Unique plugin identifier (lowercase, dot-separated) |
| `version` | string | ✅ | Semantic version (e.g., "1.0.0") |
| `name` | string | ✅ | Human-readable plugin name |
| `description` | string | ⬜ | Detailed description of functionality |
| `author` | string | ⬜ | Author name or organization |
| `license` | string | ⬜ | License identifier (e.g., "MIT", "Apache-2.0") |
| `provides` | string[] | ⬜ | Contract interfaces implemented by this plugin |
| `dependencies` | object[] | ⬜ | Required plugin dependencies |
| `entryPoint` | object | ⬜ | Platform-specific entry points |
| `capabilities` | string[] | ⬜ | Tags describing plugin capabilities |
| `supportedProfiles` | string[] | ⬜ | Runtime profiles this plugin supports |
| `loadStrategy` | string | ⬜ | When to load: "eager" or "lazy" (default: "lazy") |
| `priority` | number | ⬜ | Loading priority (0-1000+, default: 0) |
| `metadata` | object | ⬜ | Additional custom metadata |

### Dependency Specification

Dependencies use semantic versioning ranges:

```json
{
  "dependencies": [
    {
      "pluginId": "wingedbean.plugins.config",
      "versionRange": "^1.0.0",
      "optional": false
    },
    {
      "pluginId": "wingedbean.plugins.logging",
      "versionRange": "~2.1.0",
      "optional": true
    }
  ]
}
```

**Version Range Syntax:**

- `^1.0.0` - Compatible with 1.x.x (>= 1.0.0, < 2.0.0)
- `~1.2.0` - Approximately 1.2.x (>= 1.2.0, < 1.3.0)
- `1.0.0` - Exact version match
- `>=1.0.0 <2.0.0` - Explicit range

## Service Registration Patterns

### Basic Service Implementation

Implement a contract interface directly:

```csharp
using WingedBean.Contracts.Core;
using WingedBean.Contracts.Example;

namespace WingedBean.Plugins.Example;

[Plugin(
    Name = "Example.BasicService",
    Provides = new[] { typeof(IExampleService) },
    Priority = 10
)]
public class BasicExampleService : IExampleService
{
    public string GetValue() => "Hello from plugin!";
}
```

### Service with Dependency Injection

Accept dependencies through constructor injection:

```csharp
using Microsoft.Extensions.Logging;
using WingedBean.Contracts.Core;
using WingedBean.Contracts.Config;
using WingedBean.Contracts.Example;

namespace WingedBean.Plugins.Example;

[Plugin(
    Name = "Example.AdvancedService",
    Provides = new[] { typeof(IExampleService) },
    Dependencies = new[] { typeof(IConfigService) },
    Priority = 20
)]
public class AdvancedExampleService : IExampleService
{
    private readonly IConfigService _config;
    private readonly ILogger<AdvancedExampleService>? _logger;

    public AdvancedExampleService(
        IConfigService config,
        ILogger<AdvancedExampleService>? logger = null)
    {
        _config = config ?? throw new ArgumentNullException(nameof(config));
        _logger = logger;
    }

    public string GetValue()
    {
        var value = _config.GetValue<string>("Example:Setting") ?? "default";
        _logger?.LogInformation($"Retrieved value: {value}");
        return value;
    }
}
```

**Note:** Services are instantiated via reflection with default constructors. For DI support, implement `IPlugin` interface.

### Multiple Service Implementations

One plugin can provide multiple service implementations:

```csharp
using WingedBean.Contracts.Core;
using WingedBean.Contracts.Storage;

namespace WingedBean.Plugins.Storage;

[Plugin(
    Name = "Storage.FileSystem",
    Provides = new[] { 
        typeof(IStorageService),
        typeof(ICacheService)
    },
    Priority = 30
)]
public class FileSystemStorageService : IStorageService, ICacheService
{
    // Implement both interfaces
    public Task<byte[]> ReadAsync(string path) { /* ... */ }
    public Task WriteAsync(string path, byte[] data) { /* ... */ }
    public Task<T?> GetAsync<T>(string key) { /* ... */ }
    public Task SetAsync<T>(string key, T value) { /* ... */ }
}
```

### Service Priority

Use priority to control which implementation is selected when multiple plugins provide the same service:

```csharp
// Low priority fallback implementation
[Plugin(
    Name = "Cache.InMemory",
    Provides = new[] { typeof(ICacheService) },
    Priority = 10
)]
public class InMemoryCacheService : ICacheService
{
    // Simple in-memory implementation
}

// High priority production implementation
[Plugin(
    Name = "Cache.Redis",
    Provides = new[] { typeof(ICacheService) },
    Priority = 100
)]
public class RedisCacheService : ICacheService
{
    // Redis-backed implementation
}
```

The host's registry will prefer the higher priority implementation (Redis) unless explicitly requested otherwise.

## Plugin Lifecycle

### Plugin States

Plugins go through several lifecycle states:

```
Discovered → Loading → Loaded → Activating → Activated
                ↓                              ↓
              Failed ← ─────────────── Deactivating → Deactivated → Unloading → Unloaded
```

### Implementing IPlugin Interface

For lifecycle control, implement the `IPlugin` interface:

```csharp
using System.Threading;
using System.Threading.Tasks;
using WingedBean.Contracts.Core;

namespace WingedBean.Plugins.Example;

[Plugin(
    Name = "Example.Lifecycle",
    Provides = new[] { typeof(IExampleService) },
    Priority = 50
)]
public class LifecycleAwarePlugin : IPlugin, IExampleService
{
    private IRegistry? _registry;
    private bool _isActivated;

    public string Id => "wingedbean.plugins.example";
    public string Version => "1.0.0";

    public async Task OnActivateAsync(
        IRegistry registry, 
        CancellationToken cancellationToken = default)
    {
        _registry = registry;
        
        // Perform initialization
        Console.WriteLine($"[{Id}] Activating plugin...");
        
        // Simulate async initialization
        await Task.Delay(100, cancellationToken);
        
        // Register additional services or resources
        _isActivated = true;
        
        Console.WriteLine($"[{Id}] Plugin activated successfully");
    }

    public async Task OnDeactivateAsync(CancellationToken cancellationToken = default)
    {
        Console.WriteLine($"[{Id}] Deactivating plugin...");
        
        // Cleanup resources
        _isActivated = false;
        
        await Task.CompletedTask;
        
        Console.WriteLine($"[{Id}] Plugin deactivated");
    }

    // IExampleService implementation
    public string GetValue()
    {
        if (!_isActivated)
            throw new InvalidOperationException("Plugin not activated");
            
        return "Value from activated plugin";
    }
}
```

### Lifecycle Hooks

The plugin loader calls lifecycle methods at specific points:

1. **OnActivateAsync**: Called after plugin is loaded but before services are registered
   - Initialize resources
   - Validate configuration
   - Register event handlers
   - Connect to external services

2. **OnDeactivateAsync**: Called before plugin is unloaded
   - Close connections
   - Release resources
   - Save state
   - Unregister handlers

### Error Handling

Implement proper error handling in lifecycle methods:

```csharp
public async Task OnActivateAsync(
    IRegistry registry, 
    CancellationToken cancellationToken = default)
{
    try
    {
        Console.WriteLine($"[{Id}] Starting activation...");
        
        // Critical initialization
        await InitializeCriticalResourcesAsync(cancellationToken);
        
        Console.WriteLine($"[{Id}] Activation complete");
    }
    catch (OperationCanceledException)
    {
        Console.WriteLine($"[{Id}] Activation cancelled");
        throw;
    }
    catch (Exception ex)
    {
        Console.WriteLine($"[{Id}] Activation failed: {ex.Message}");
        
        // Cleanup partial initialization
        await CleanupAsync();
        
        throw new PluginActivationException(
            $"Failed to activate plugin {Id}", ex);
    }
}

private async Task CleanupAsync()
{
    // Safe cleanup that won't throw
    try
    {
        // Release resources
    }
    catch
    {
        // Log but don't throw
    }
}
```

## Configuration Schema

### Host Configuration (plugins.json)

The host application uses `plugins.json` to configure which plugins to load:

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
        "description": "Configuration service",
        "author": "WingedBean",
        "version": "1.0.0"
      }
    }
  ]
}
```

### Configuration Models

The host uses these C# models to parse configuration:

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
}

public enum LoadStrategy
{
    Eager,    // Load at startup
    Lazy,     // Load on first use
    Explicit  // Load only when explicitly requested
}
```

### Priority Guidelines

Choose appropriate priority values based on plugin criticality:

| Range | Purpose | Example Plugins |
|-------|---------|----------------|
| 1000+ | Critical infrastructure | Configuration, logging |
| 500-999 | Core services | Database, authentication |
| 100-499 | Standard services | WebSocket, terminal UI |
| 50-99 | Optional features | Recording, metrics |
| 0-49 | Low priority | Experimental features |

### Load Strategies

- **Eager**: Load immediately during application startup
  - Use for: Core services, critical plugins
  - Example: Configuration, registry, foundational services

- **Lazy**: Load on first use
  - Use for: Optional features, performance optimization
  - Example: Recording services, analytics

- **Explicit**: Load only via explicit API call
  - Use for: Development tools, admin plugins
  - Example: Debugging utilities, test harnesses

## Best Practices

### Plugin Design

1. **Single Responsibility**: Each plugin should focus on one well-defined capability
   ```csharp
   // ✅ Good - focused responsibility
   public class WebSocketService : IWebSocketService { }
   
   // ❌ Bad - multiple unrelated responsibilities
   public class EverythingService : IWebSocketService, IDatabaseService, ILoggingService { }
   ```

2. **Interface Segregation**: Prefer small, focused contract interfaces
   ```csharp
   // ✅ Good - focused interfaces
   public interface IWebSocketService { }
   public interface IWebSocketMetrics { }
   
   // ❌ Bad - monolithic interface
   public interface IWebSocketEverything { }
   ```

3. **Dependency Management**: Declare all plugin dependencies explicitly
   ```json
   {
     "dependencies": [
       {
         "pluginId": "wingedbean.plugins.config",
         "versionRange": "^1.0.0",
         "optional": false
       }
     ]
   }
   ```

4. **Version Stability**: Follow semantic versioning strictly
   - MAJOR: Breaking changes
   - MINOR: New features, backward compatible
   - PATCH: Bug fixes, backward compatible

### Error Handling

1. **Graceful Degradation**: Handle missing optional dependencies
   ```csharp
   public MyService(IOptionalService? optionalService = null)
   {
       _optionalService = optionalService;
   }
   
   public void DoWork()
   {
       // Use optional service if available
       _optionalService?.PerformOptionalWork();
       
       // Continue with core functionality
       PerformCoreWork();
   }
   ```

2. **Informative Exceptions**: Throw exceptions with clear messages
   ```csharp
   if (configuration == null)
   {
       throw new InvalidOperationException(
           $"Plugin {Id} requires configuration section 'MyPlugin' " +
           "but it was not found in configuration file.");
   }
   ```

3. **Logging**: Use structured logging for diagnostics
   ```csharp
   _logger?.LogInformation(
       "Plugin {PluginId} processing request {RequestId}",
       Id, requestId);
   ```

### Performance

1. **Lazy Initialization**: Defer expensive initialization until needed
   ```csharp
   private IExpensiveResource? _resource;
   
   private IExpensiveResource GetResource()
   {
       if (_resource == null)
       {
           _resource = InitializeExpensiveResource();
       }
       return _resource;
   }
   ```

2. **Resource Cleanup**: Always implement IDisposable for managed resources
   ```csharp
   public class MyService : IMyService, IDisposable
   {
       private readonly HttpClient _httpClient = new();
       
       public void Dispose()
       {
           _httpClient?.Dispose();
           GC.SuppressFinalize(this);
       }
   }
   ```

3. **Async Operations**: Use async/await for I/O operations
   ```csharp
   public async Task<string> LoadDataAsync()
   {
       // ✅ Good - truly asynchronous
       return await File.ReadAllTextAsync(filePath);
       
       // ❌ Bad - blocking on async
       // return File.ReadAllTextAsync(filePath).Result;
   }
   ```

### Testing

1. **Unit Tests**: Test services in isolation
   ```csharp
   [Fact]
   public async Task MyService_ProcessesDataCorrectly()
   {
       // Arrange
       var service = new MyService();
       
       // Act
       var result = await service.ProcessAsync("test");
       
       // Assert
       Assert.Equal("processed: test", result);
   }
   ```

2. **Integration Tests**: Test plugin loading and registration
   ```csharp
   [Fact]
   public async Task Plugin_LoadsAndRegistersServices()
   {
       // Arrange
       var loader = new ActualPluginLoader(new AssemblyContextProvider());
       
       // Act
       var plugin = await loader.LoadAsync("path/to/plugin.dll");
       var services = plugin.GetServices();
       
       // Assert
       Assert.NotEmpty(services);
       Assert.Contains(services, s => s is IMyService);
   }
   ```

3. **Mock Dependencies**: Use interfaces to enable testing
   ```csharp
   public class MyService
   {
       private readonly IConfigService _config;
       
       // Constructor injection enables mocking
       public MyService(IConfigService config)
       {
           _config = config;
       }
   }
   
   // In tests
   var mockConfig = new Mock<IConfigService>();
   var service = new MyService(mockConfig.Object);
   ```

## Troubleshooting

### Plugin Not Loading

**Symptom**: Plugin doesn't appear in loaded plugins list

**Possible Causes:**

1. **Path incorrect in plugins.json**
   ```json
   // ❌ Wrong
   "path": "plugins/MyPlugin.dll"
   
   // ✅ Correct
   "path": "plugins/WingedBean.Plugins.MyPlugin/bin/Debug/net8.0/WingedBean.Plugins.MyPlugin.dll"
   ```

2. **Plugin disabled**
   ```json
   {
     "enabled": false  // Should be true
   }
   ```

3. **Missing dependencies**
   - Check that all referenced DLLs are copied to output
   - Verify `CopyLocalLockFileAssemblies` is set to `true`

**Solution**: Enable verbose logging to see detailed error messages:
```bash
export DOTNET_LOG_LEVEL=Debug
dotnet run
```

### Service Not Registered

**Symptom**: `ServiceNotFoundException` when trying to retrieve service

**Possible Causes:**

1. **Contract interface not in WingedBean.Contracts namespace**
   ```csharp
   // ❌ Wrong namespace
   namespace MyApp.Services;
   public interface IMyService { }
   
   // ✅ Correct namespace
   namespace WingedBean.Contracts.MyNamespace;
   public interface IMyService { }
   ```

2. **Service doesn't implement expected interface**
   ```csharp
   // ❌ Wrong - doesn't implement IMyService
   public class MyService { }
   
   // ✅ Correct
   public class MyService : IMyService { }
   ```

3. **Plugin attribute missing or incorrect**
   ```csharp
   // ✅ Add Plugin attribute
   [Plugin(
       Name = "MyService",
       Provides = new[] { typeof(IMyService) },
       Priority = 10
   )]
   public class MyService : IMyService { }
   ```

### Version Conflicts

**Symptom**: Type load exceptions or missing method exceptions

**Possible Causes:**

1. **Mismatched dependency versions**
   - Plugin built against different version of contracts
   - Shared dependencies with version conflicts

**Solution**: Ensure consistent versions across all projects:
```xml
<ItemGroup>
  <PackageReference Include="Microsoft.Extensions.Logging" Version="8.0.0" />
</ItemGroup>
```

2. **Use central package management** (recommended):
   ```xml
   <!-- Directory.Packages.props at solution root -->
   <Project>
     <PropertyGroup>
       <ManagePackageVersionsCentrally>true</ManagePackageVersionsCentrally>
     </PropertyGroup>
     <ItemGroup>
       <PackageVersion Include="Microsoft.Extensions.Logging" Version="8.0.0" />
     </ItemGroup>
   </Project>
   ```

### Priority Not Working

**Symptom**: Wrong service implementation selected by registry

**Possible Causes:**

1. **Priority in manifest doesn't match attribute**
   ```csharp
   // Attribute priority
   [Plugin(Priority = 10)]
   
   // Manifest priority (this one is used by host)
   { "priority": 100 }
   ```

2. **Using SelectionMode.One instead of HighestPriority**
   ```csharp
   // ❌ Gets first registered, ignores priority
   var service = registry.Get<IMyService>(SelectionMode.One);
   
   // ✅ Gets highest priority
   var service = registry.Get<IMyService>(SelectionMode.HighestPriority);
   ```

**Solution**: Always use consistent priorities and appropriate selection mode.

### Memory Leaks

**Symptom**: Memory usage grows over time, especially with plugin reload

**Possible Causes:**

1. **Event handlers not unsubscribed**
   ```csharp
   public class MyService : IMyService
   {
       public MyService()
       {
           SomeEvent += HandleEvent;  // Memory leak!
       }
       
       // ✅ Add cleanup
       public void Dispose()
       {
           SomeEvent -= HandleEvent;
       }
   }
   ```

2. **Static references to plugin types**
   ```csharp
   // ❌ Static reference prevents plugin unload
   private static IMyService _service;
   
   // ✅ Instance reference
   private IMyService _service;
   ```

**Solution**: Always implement IDisposable and clean up resources in OnDeactivateAsync.

### Getting Help

If you're still experiencing issues:

1. **Check logs**: Enable debug logging and review error messages
2. **Review samples**: Look at existing plugins in `src/plugins/`
3. **Ask for help**: Create an issue with:
   - Plugin manifest
   - Service implementation
   - Error messages
   - Steps to reproduce

## Next Steps

- Review [Advanced Plugin Features](../development/ADVANCED_PLUGIN_FEATURES.md) for security, hot updates, and more
- Study [Architecture Overview](architecture-overview.md) to understand the plugin system design
- Explore existing plugins in `development/dotnet/console/src/plugins/` for real-world examples
- Read RFC-0006 for the dynamic loading implementation details

---

**Last Updated**: 2025-10-01  
**Version**: 1.0.0  
**Applies to**: WingedBean Plugin System v1.0+

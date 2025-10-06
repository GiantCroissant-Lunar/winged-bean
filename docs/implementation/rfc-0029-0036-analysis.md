# RFC-0029 & RFC-0036 Implementation Analysis

## Current State

The console app currently runs, but several workarounds were added that deviate from RFC-0029 and RFC-0036:

### Key Issues Identified

1. **LazyTerminalAppResolver Workaround** (Program.cs)
   - Added a `LazyTerminalAppResolver` wrapper class to defer resolution of `ITerminalApp` from the registry
   - This is necessary because plugins are loaded asynchronously by `PluginLoaderHostedService`
   - The RFC design assumed synchronous plugin loading during service configuration

2. **StartWithConfigAsync Legacy Method** (ConsoleDungeonAppRefactored.cs)
   - Added a `StartWithConfigAsync(TerminalAppConfig, CancellationToken)` method to maintain compatibility
   - This breaks the RFC-0029 design where `StartAsync(CancellationToken)` is the only entry point
   - Configuration should be injected via constructor, not passed to StartAsync

3. **Assembly Load Context (ALC) Bridging** (LegacyTerminalAppAdapter.cs)
   - Complex reflection code to handle cross-ALC type mismatches for `TerminalAppConfig`
   - Creates config instances dynamically and copies properties by name
   - This is a symptom of the plugin architecture not properly integrating with DI

4. **Missing WingedBeanHost Usage** (Program.cs)
   - Currently calls `WingedBeanHost.CreateConsoleBuilder(args)` but doesn't properly use it
   - Immediately falls back to manual configuration instead of leveraging the abstraction

5. **Registry and DI Coexistence Issues**
   - Plugins register services in custom `IRegistry`
   - Host uses MS DI (`IServiceProvider`)
   - The bridge between these two is fragile and requires workarounds

## Root Causes

### 1. Plugin Loading Timing
The fundamental issue is that plugins need to be loaded **before** the terminal app can be registered in DI, but the Generic Host builder pattern expects all services to be registered synchronously during configuration.

**RFC-0029 Assumption** (incorrect):
```csharp
.ConfigureServices((context, services) =>
{
    // Assumes plugins are already loaded
    services.AddSingleton<ITerminalApp>(sp =>
    {
        var registry = sp.GetRequiredService<IRegistry>();
        return registry.Get<ITerminalApp>(); // ❌ Registry is empty here!
    });
})
```

**Current Workaround**:
```csharp
// Defer resolution with LazyTerminalAppResolver
services.AddSingleton<ITerminalApp>(sp => 
    new LazyTerminalAppResolver(sp.GetRequiredService<IRegistry>()));
```

### 2. Configuration Injection Pattern
RFC-0029 specifies that configuration should be injected via constructor using `IOptions<TerminalAppConfig>`:

```csharp
public ConsoleDungeonAppRefactored(
    IOptions<TerminalAppConfig> config,
    // other deps
)
{
    _config = config.Value;
}
```

However, plugins are instantiated by the plugin loader, not by DI, so they don't receive injected dependencies.

### 3. Assembly Load Context Isolation
Plugins are loaded in separate ALCs for isolation, but this causes type identity issues:
- Host's `TerminalAppConfig` type ≠ Plugin's `TerminalAppConfig` type (different ALCs)
- Reflection workarounds are needed to bridge these types

## Solution Approach

We need to fix the architecture to properly implement RFC-0029 and RFC-0036 without workarounds.

### Option A: Plugin-First Bootstrap (Recommended)

Load plugins **before** building the Generic Host, then register their services in DI:

```csharp
// 1. Load plugins first (outside of Generic Host)
var registry = new ActualRegistry();
var pluginLoader = new ActualPluginLoader(registry);
await pluginLoader.LoadPluginsAsync("plugins.json");

// 2. Build Generic Host with plugin services
var host = WingedBeanHost.CreateConsoleBuilder(args)
    .ConfigureAppConfiguration(config => { /* ... */ })
    .ConfigureServices((context, services) =>
    {
        // Register registry (already populated with plugins)
        services.AddSingleton<IRegistry>(registry);
        
        // Bridge plugin services to DI
        BridgePluginServicesToDI(registry, services);
        
        // Now ITerminalApp can be resolved directly
        services.AddSingleton<ITerminalApp>(sp =>
            registry.Get<ITerminalApp>());
        
        // Register as hosted service
        services.AddHostedService<LegacyTerminalAppAdapter>();
    })
    .Build();

await host.RunAsync();
```

**Pros:**
- Plugins loaded synchronously before host configuration
- No lazy resolution needed
- Clean separation of concerns

**Cons:**
- Plugin loading happens outside Generic Host lifecycle
- Can't use IHostedService for plugin loading

### Option B: Two-Phase Host Bootstrap

Use a preliminary host just for plugin loading, then rebuild the main host:

```csharp
// Phase 1: Plugin loader host
var loaderHost = WingedBeanHost.CreateConsoleBuilder(args)
    .ConfigureServices(services =>
    {
        services.AddSingleton<IRegistry, ActualRegistry>();
        services.AddSingleton<IPluginLoader, ActualPluginLoader>();
        services.AddHostedService<PluginLoaderHostedService>();
    })
    .Build();

await loaderHost.StartAsync(); // Load plugins
var registry = loaderHost.Services.GetRequiredService<IRegistry>();

// Phase 2: Main host with plugins
var mainHost = WingedBeanHost.CreateConsoleBuilder(args)
    .ConfigureServices(services =>
    {
        services.AddSingleton<IRegistry>(registry); // Reuse registry
        services.AddSingleton<ITerminalApp>(sp =>
            registry.Get<ITerminalApp>());
        services.AddHostedService<LegacyTerminalAppAdapter>();
    })
    .Build();

await mainHost.RunAsync();
```

**Pros:**
- Plugin loading uses IHostedService
- Clean separation between loading and running

**Cons:**
- Two host instances (overhead)
- Complex lifecycle management

### Option C: DI-Native Plugin System (Future)

Redesign plugin system to fully integrate with MS DI:

```csharp
public class ConsoleDungeonPlugin : IPlugin
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton<ITerminalApp, ConsoleDungeonAppRefactored>();
        services.AddSingleton<IDungeonGameService, DungeonGame>();
        // ...
    }
}
```

**Pros:**
- Native DI integration
- No registry bridge needed
- Proper dependency injection

**Cons:**
- Major refactoring required
- Breaks existing plugin API

## Recommended Implementation: Option A (Enhanced)

Implement Option A with proper configuration injection:

### Step 1: Refactor Program.cs

```csharp
Console.WriteLine("ConsoleDungeon.Host starting...");

// Build configuration first
var configBuilder = new ConfigurationBuilder()
    .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
    .AddEnvironmentVariables(prefix: "DUNGEON_")
    .AddCommandLine(args);
var configuration = configBuilder.Build();

// Load plugins before building host
Console.WriteLine("Loading plugins...");
var registry = new ActualRegistry();
var pluginLoader = new ActualPluginLoader(registry);

// Register configuration in registry so plugins can access it
var terminalConfig = configuration.GetSection("Terminal").Get<TerminalAppConfig>()
    ?? new TerminalAppConfig();
registry.Register<TerminalAppConfig>(terminalConfig);

// Load plugins
await pluginLoader.LoadPluginsAsync("plugins.json");
Console.WriteLine("Plugins loaded successfully");

// Build Generic Host with loaded plugins
var host = WingedBeanHost.CreateConsoleBuilder(args)
    .ConfigureAppConfiguration(config =>
    {
        config.AddConfiguration(configuration); // Reuse existing config
    })
    .ConfigureServices(services =>
    {
        // Register configuration
        services.Configure<TerminalAppConfig>(
            configuration.GetSection("Terminal"));
        
        // Register registry (already populated)
        services.AddSingleton<IRegistry>(registry);
        
        // Register terminal app from registry
        services.AddSingleton<ITerminalApp>(sp =>
        {
            var terminalApp = registry.Get<ITerminalApp>();
            
            // Inject registry if plugin supports it
            TryInjectRegistry(terminalApp, registry);
            
            return terminalApp;
        });
        
        // Register as hosted service
        services.AddHostedService<LegacyTerminalAppAdapter>();
        
        // Bridge IHostApplicationLifetime to registry
        services.AddSingleton<IHostedService>(sp =>
        {
            var lifetime = sp.GetRequiredService<IHostApplicationLifetime>();
            registry.Register<IHostApplicationLifetime>(lifetime);
            return new NoOpHostedService();
        });
    })
    .ConfigureLogging(logging =>
    {
        logging.AddConfiguration(configuration.GetSection("Logging"));
    })
    .Build();

await host.RunAsync();

static void TryInjectRegistry(object instance, IRegistry registry)
{
    var setRegistryMethod = instance.GetType().GetMethod("SetRegistry");
    setRegistryMethod?.Invoke(instance, new object[] { registry });
}

class NoOpHostedService : IHostedService
{
    public Task StartAsync(CancellationToken ct) => Task.CompletedTask;
    public Task StopAsync(CancellationToken ct) => Task.CompletedTask;
}
```

### Step 2: Remove StartWithConfigAsync from ConsoleDungeonAppRefactored

The plugin should only implement the RFC-0029 interface:

```csharp
public class ConsoleDungeonAppRefactored : ITerminalApp, IDisposable
{
    // Remove StartWithConfigAsync method
    
    // Keep IHostedService.StartAsync
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        // Configuration is already available via _config field
        // which was set by SetRegistry or constructor injection
    }
}
```

### Step 3: Simplify LegacyTerminalAppAdapter

Remove complex ALC bridging since config is now injected directly:

```csharp
public class LegacyTerminalAppAdapter : IHostedService
{
    private readonly ITerminalApp _terminalApp;
    
    public LegacyTerminalAppAdapter(ITerminalApp terminalApp)
    {
        _terminalApp = terminalApp;
    }
    
    public Task StartAsync(CancellationToken cancellationToken)
    {
        // Just delegate to the terminal app
        return _terminalApp.StartAsync(cancellationToken);
    }
    
    public Task StopAsync(CancellationToken cancellationToken)
    {
        return _terminalApp.StopAsync(cancellationToken);
    }
}
```

### Step 4: Update Plugin Constructor

Add a constructor that accepts configuration from registry:

```csharp
public ConsoleDungeonAppRefactored()
{
    // Default empty constructor for plugin loader
}

public void SetRegistry(IRegistry registry)
{
    _registry = registry;
    
    // Resolve dependencies from registry
    _config = registry.TryGet<TerminalAppConfig>()
        ?? new TerminalAppConfig { Name = "Console Dungeon", Cols = 80, Rows = 24 };
    
    _gameService = registry.TryGet<IDungeonGameService>();
    
    // Create logger (fallback if not in registry)
    _logger = registry.TryGet<ILogger<ConsoleDungeonAppRefactored>>()
        ?? new LoggerFactory().CreateLogger<ConsoleDungeonAppRefactored>();
}
```

## Testing Plan

1. **Build and run console app** - verify no exceptions
2. **Test graceful shutdown** - press Ctrl+C, verify clean exit
3. **Test configuration loading** - modify appsettings.json, verify changes applied
4. **Test plugin isolation** - verify plugins load in separate ALCs
5. **Remove LazyTerminalAppResolver** - verify direct resolution works

## Benefits of This Approach

1. ✅ **Proper RFC-0029 compliance** - `StartAsync(CancellationToken)` only
2. ✅ **No lazy resolution** - services available immediately
3. ✅ **Clean configuration injection** - through registry or Options pattern
4. ✅ **Simplified adapter** - no complex reflection needed
5. ✅ **Maintains plugin isolation** - ALCs still separate, but config bridged properly
6. ✅ **Graceful degradation** - fallback to defaults if config unavailable

## Next Steps

1. Implement refactored Program.cs (Option A)
2. Update ConsoleDungeonAppRefactored to remove StartWithConfigAsync
3. Simplify LegacyTerminalAppAdapter
4. Test end-to-end
5. Remove workaround code
6. Update RFC-0029 execution plan with lessons learned

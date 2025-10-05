---
id: RFC-0029
title: ITerminalApp Integration with .NET Generic Host
status: Draft
category: architecture
created: 2025-10-05
updated: 2025-10-05
depends-on: RFC-0028
---

# RFC-0029: ITerminalApp Integration with .NET Generic Host

## Summary

Modernize `ITerminalApp` to extend `IHostedService`, enabling integration with .NET Generic Host for graceful shutdown, configuration management, dependency injection, and standard hosting patterns.

## Motivation

### Current Problems

**Current implementation** (`ConsoleDungeon.Host/Program.cs`):
```csharp
// Manual plugin loading and service registration
var registry = new ActualRegistry();
var terminalApp = registry.Get<ITerminalApp>();

// Manual blocking call - no hosting framework
await terminalApp.StartAsync(appConfig); // Blocks until exit
```

**Issues:**
1. ❌ **No graceful shutdown**: Process termination is abrupt
2. ❌ **No configuration system**: Hardcoded values, no `appsettings.json`
3. ❌ **Custom registry**: Parallel DI system instead of MS DI
4. ❌ **No health checks**: Can't integrate with monitoring
5. ❌ **No lifecycle management**: Missing `IHostApplicationLifetime` events
6. ❌ **Not cloud-native**: Incompatible with modern .NET hosting (Kubernetes, Docker, etc.)

### What We're Missing

Modern .NET applications using `IHost` get:
- ✅ Graceful shutdown with `SIGTERM` handling
- ✅ Configuration via `appsettings.json`, environment variables, command-line
- ✅ Structured logging with `ILogger<T>` from DI
- ✅ Health checks for monitoring
- ✅ Metrics and telemetry
- ✅ Background services coordination
- ✅ Application lifetime events

## Goals

1. **Make `ITerminalApp` extend `IHostedService`** for .NET ecosystem compatibility
2. **Refactor `ConsoleDungeon.Host` to use `IHost`** instead of manual lifecycle management
3. **Maintain backward compatibility** where possible via adapter pattern
4. **Enable modern hosting features**: configuration, health checks, graceful shutdown

## Proposal

### Phase 1: Update ITerminalApp Interface

**Current interface:**
```csharp
namespace WingedBean.Contracts.Terminal;

public interface ITerminalApp
{
    Task StartAsync(TerminalAppConfig config, CancellationToken ct = default);
    Task StopAsync(CancellationToken ct = default);
    Task SendInputAsync(byte[] data, CancellationToken ct = default);
    Task ResizeAsync(int cols, int rows, CancellationToken ct = default);

    event EventHandler<TerminalOutputEventArgs> OutputReceived;
    event EventHandler<TerminalExitEventArgs> Exited;
}
```

**New interface (extends IHostedService):**
```csharp
using Microsoft.Extensions.Hosting;

namespace WingedBean.Contracts.Terminal;

/// <summary>
/// Terminal application that integrates with .NET Generic Host.
/// Extends IHostedService for lifecycle management while adding terminal-specific operations.
/// </summary>
public interface ITerminalApp : IHostedService
{
    // IHostedService provides:
    // - Task StartAsync(CancellationToken cancellationToken)
    // - Task StopAsync(CancellationToken cancellationToken)

    // Terminal-specific operations
    Task SendInputAsync(byte[] data, CancellationToken ct = default);
    Task ResizeAsync(int cols, int rows, CancellationToken ct = default);

    // Terminal-specific events
    event EventHandler<TerminalOutputEventArgs> OutputReceived;
    event EventHandler<TerminalExitEventArgs> Exited;
}

/// <summary>
/// Configuration for terminal applications.
/// Injected via DI or IOptions pattern.
/// </summary>
public class TerminalAppConfig
{
    public string Name { get; set; } = string.Empty;
    public int Cols { get; set; } = 80;
    public int Rows { get; set; } = 24;
    public string? WorkingDirectory { get; set; }
    public Dictionary<string, string> Environment { get; set; } = new();
    public Dictionary<string, object> Parameters { get; set; } = new();
}
```

**Breaking changes:**
- ⚠️ `StartAsync` signature changes from `(TerminalAppConfig, CancellationToken)` to `(CancellationToken)` (IHostedService contract)
- ⚠️ Configuration must be injected via constructor or `IOptions<TerminalAppConfig>`

### Phase 2: Backward Compatibility Adapter

For legacy plugins that don't extend IHostedService yet:

```csharp
namespace WingedBean.Hosting;

/// <summary>
/// Adapter that wraps a legacy ITerminalApp (pre-IHostedService)
/// to work with .NET Generic Host.
/// </summary>
public class LegacyTerminalAppAdapter : IHostedService
{
    private readonly ITerminalApp _terminalApp;
    private readonly TerminalAppConfig _config;

    public LegacyTerminalAppAdapter(ITerminalApp terminalApp, TerminalAppConfig config)
    {
        _terminalApp = terminalApp;
        _config = config;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        // Call legacy StartAsync(config, ct) signature
        var legacyStart = _terminalApp.GetType().GetMethod("StartAsync",
            new[] { typeof(TerminalAppConfig), typeof(CancellationToken) });

        if (legacyStart != null)
        {
            return (Task)legacyStart.Invoke(_terminalApp, new object[] { _config, cancellationToken })!;
        }

        // Fallback to new signature
        return _terminalApp.StartAsync(cancellationToken);
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return _terminalApp.StopAsync(cancellationToken);
    }
}
```

### Phase 3: Refactor ConsoleDungeon.Host

**Current approach:**
```csharp
// Manual lifecycle management
var terminalApp = registry.Get<ITerminalApp>();
await terminalApp.StartAsync(appConfig); // Blocks
```

**New approach using IHost:**
```csharp
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;

// Build .NET Generic Host
var host = Host.CreateDefaultBuilder(args)
    .ConfigureAppConfiguration((context, config) =>
    {
        config.AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);
        config.AddEnvironmentVariables(prefix: "DUNGEON_");
        config.AddCommandLine(args);
    })
    .ConfigureServices((context, services) =>
    {
        // Register configuration
        services.Configure<TerminalAppConfig>(context.Configuration.GetSection("Terminal"));

        // Load plugins and register services
        var registry = new ActualRegistry();
        var pluginLoader = LoadPlugins(registry); // Existing plugin loading logic

        // Bridge custom registry to MS DI
        var terminalApp = registry.Get<ITerminalApp>();
        services.AddSingleton(terminalApp);

        // Register as hosted service
        services.AddHostedService<LegacyTerminalAppAdapter>(sp =>
        {
            var app = sp.GetRequiredService<ITerminalApp>();
            var config = sp.GetRequiredService<IOptions<TerminalAppConfig>>().Value;
            return new LegacyTerminalAppAdapter(app, config);
        });

        // Optional: Add health checks
        services.AddHealthChecks()
            .AddCheck<TerminalAppHealthCheck>("terminal-app");
    })
    .ConfigureLogging((context, logging) =>
    {
        logging.AddConsole();
        logging.AddConfiguration(context.Configuration.GetSection("Logging"));
    })
    .UseConsoleLifetime() // Graceful shutdown on Ctrl+C
    .Build();

// Run with proper lifecycle management
await host.RunAsync();
```

**Benefits:**
- ✅ Configuration from `appsettings.json`:
  ```json
  {
    "Terminal": {
      "Name": "Console Dungeon",
      "Cols": 120,
      "Rows": 40
    },
    "Logging": {
      "LogLevel": {
        "Default": "Information"
      }
    }
  }
  ```
- ✅ Graceful shutdown on `SIGTERM`/`SIGINT`
- ✅ Structured logging via `ILogger<T>`
- ✅ Health checks for monitoring
- ✅ Standard .NET hosting patterns

### Phase 4: Update Plugin Implementations

**ConsoleDungeonAppRefactored.cs** - Update to IHostedService pattern:

```csharp
[Plugin(Name = "ConsoleDungeonAppRefactored", Provides = new[] { typeof(ITerminalApp) })]
public class ConsoleDungeonAppRefactored : ITerminalApp, IDisposable
{
    private readonly ILogger<ConsoleDungeonAppRefactored> _logger;
    private readonly TerminalAppConfig _config;
    private readonly IDungeonGameService _gameService;

    // Constructor injection for configuration
    public ConsoleDungeonAppRefactored(
        ILogger<ConsoleDungeonAppRefactored> logger,
        IOptions<TerminalAppConfig> config,
        IDungeonGameService gameService)
    {
        _logger = logger;
        _config = config.Value;
        _gameService = gameService;
    }

    // IHostedService.StartAsync - no config parameter
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting Console Dungeon: {Name} ({Cols}x{Rows})",
            _config.Name, _config.Cols, _config.Rows);

        // Existing startup logic...
    }

    // IHostedService.StopAsync
    public async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Stopping Console Dungeon gracefully");

        // Cleanup logic...
    }

    // Terminal-specific operations remain unchanged
    public Task SendInputAsync(byte[] data, CancellationToken ct = default) { /* ... */ }
    public Task ResizeAsync(int cols, int rows, CancellationToken ct = default) { /* ... */ }

    public event EventHandler<TerminalOutputEventArgs>? OutputReceived;
    public event EventHandler<TerminalExitEventArgs>? Exited;
}
```

### Phase 5: Optional Enhancements

#### 5.1 Health Checks

```csharp
public class TerminalAppHealthCheck : IHealthCheck
{
    private readonly ITerminalApp _terminalApp;

    public Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken ct = default)
    {
        // Check if terminal app is running
        var isHealthy = _terminalApp != null; // Add actual health logic

        return Task.FromResult(isHealthy
            ? HealthCheckResult.Healthy("Terminal app is running")
            : HealthCheckResult.Unhealthy("Terminal app is not responding"));
    }
}
```

#### 5.2 Application Lifetime Events

```csharp
public class TerminalAppLifecycleHandler : IHostedService
{
    private readonly IHostApplicationLifetime _lifetime;
    private readonly ILogger<TerminalAppLifecycleHandler> _logger;

    public TerminalAppLifecycleHandler(
        IHostApplicationLifetime lifetime,
        ILogger<TerminalAppLifecycleHandler> logger)
    {
        _lifetime = lifetime;
        _logger = logger;

        _lifetime.ApplicationStarted.Register(OnStarted);
        _lifetime.ApplicationStopping.Register(OnStopping);
        _lifetime.ApplicationStopped.Register(OnStopped);
    }

    private void OnStarted() => _logger.LogInformation("Application started");
    private void OnStopping() => _logger.LogInformation("Application stopping - saving state...");
    private void OnStopped() => _logger.LogInformation("Application stopped");

    public Task StartAsync(CancellationToken ct) => Task.CompletedTask;
    public Task StopAsync(CancellationToken ct) => Task.CompletedTask;
}
```

#### 5.3 Metrics and Telemetry

```csharp
// OpenTelemetry integration
services.AddOpenTelemetry()
    .WithMetrics(metrics =>
    {
        metrics.AddMeter("WingedBean.Terminal");
    });

// Custom metrics
var meter = new Meter("WingedBean.Terminal");
var activeSessionsCounter = meter.CreateCounter<int>("terminal.sessions.active");
var inputEventsCounter = meter.CreateCounter<long>("terminal.input.events");
```

## Implementation Plan

### Timeline

1. **Week 1**: Update `ITerminalApp` interface, create adapter
2. **Week 2**: Refactor `ConsoleDungeon.Host` to use `IHost`
3. **Week 3**: Update plugin implementations (`ConsoleDungeonAppRefactored`)
4. **Week 4**: Add health checks, metrics, documentation

### Rollout Strategy

**Option A: Big Bang (Breaking Change)**
- Update all plugins at once
- Single PR with all changes
- Fast but risky

**Option B: Incremental (Adapter Pattern)**
- ✅ Phase 1: Add adapter for legacy plugins
- ✅ Phase 2: Migrate host to use `IHost`
- ✅ Phase 3: Migrate plugins one-by-one
- ✅ Phase 4: Remove adapter after all plugins migrated

**Recommendation**: **Option B (Incremental)** for safer migration

## Breaking Changes

### For Plugin Authors

**Before (Legacy):**
```csharp
public class MyTerminalApp : ITerminalApp
{
    public Task StartAsync(TerminalAppConfig config, CancellationToken ct)
    {
        // Use config parameter
        _config = config;
    }
}
```

**After (IHostedService):**
```csharp
public class MyTerminalApp : ITerminalApp // Now extends IHostedService
{
    private readonly TerminalAppConfig _config;

    public MyTerminalApp(IOptions<TerminalAppConfig> config)
    {
        _config = config.Value; // Inject via DI
    }

    public Task StartAsync(CancellationToken ct) // No config parameter
    {
        // Use injected _config
    }
}
```

### Migration Checklist

- [ ] Update `ITerminalApp` implementations to inject `IOptions<TerminalAppConfig>`
- [ ] Remove `config` parameter from `StartAsync` method
- [ ] Add `appsettings.json` for configuration
- [ ] Update host to use `IHost.RunAsync()`
- [ ] Test graceful shutdown (Ctrl+C)
- [ ] Verify configuration loading

## Alternatives Considered

### Alternative 1: Keep custom lifecycle (no IHostedService)

**Rejected because:**
- Misses modern .NET hosting benefits
- Cannot integrate with cloud-native tooling
- Requires maintaining custom lifecycle code

### Alternative 2: Create separate IHostedTerminalApp interface

**Rejected because:**
- Confusing to have two terminal app interfaces
- Would need migration path anyway
- Better to standardize on `IHostedService`

### Alternative 3: Use BackgroundService base class

```csharp
public abstract class TerminalAppBase : BackgroundService, ITerminalApp
{
    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        // Default implementation
        await StartAsync(ct);
    }
}
```

**Consideration:**
- ✅ Simpler than implementing `IHostedService` directly
- ✅ `ExecuteAsync` runs in background automatically
- ⚠️ May conflict with existing `StartAsync` semantics
- **Decision**: Offer as optional base class, not required

## Impact

### Benefits

- ✅ **Standard .NET patterns**: Uses `IHost`, `IHostedService`, `IOptions`
- ✅ **Graceful shutdown**: Proper `SIGTERM` handling
- ✅ **Configuration**: `appsettings.json`, environment variables, CLI args
- ✅ **Health checks**: Monitoring and alerting integration
- ✅ **Cloud-native**: Compatible with Kubernetes, Docker, Azure
- ✅ **Testability**: Easier to mock and test with DI
- ✅ **Observability**: Logging, metrics, telemetry out-of-the-box

### Risks

- ⚠️ **Breaking change**: Signature change to `StartAsync`
- ⚠️ **Migration effort**: All plugins must be updated
- ⚠️ **Complexity**: More boilerplate for simple scenarios
- ⚠️ **Learning curve**: Developers must understand `IHost` patterns

### Mitigation

- ✅ Provide **adapter** for legacy plugins
- ✅ Create **migration guide** with examples
- ✅ **Incremental rollout** to reduce risk
- ✅ **Comprehensive testing** before removing adapter

## Dependencies

- **Depends on**: RFC-0028 (Contract Reorganization)
- **Package dependencies**:
  - `Microsoft.Extensions.Hosting` (already referenced)
  - `Microsoft.Extensions.Options` (already referenced)
  - `Microsoft.Extensions.HealthChecks` (optional)

## Questions

1. **Should we support both signatures during transition?**
   - **Answer**: Yes, via adapter pattern (Phase 2)

2. **What about plugins that can't inject IOptions?**
   - **Answer**: Adapter handles legacy plugins, or use `IOptionsSnapshot` for dynamic config

3. **Should configuration be required or optional?**
   - **Answer**: Optional with defaults, can override via DI

4. **How do we handle registry vs MS DI coexistence?**
   - **Answer**: Bridge pattern - load plugins into registry, then expose to MS DI

## References

- RFC-0028: Contract Reorganization - Terminal and Recorder Separation
- MS Docs: [Generic Host](https://learn.microsoft.com/en-us/dotnet/core/extensions/generic-host)
- MS Docs: [Hosted Services](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/host/hosted-services)
- MS Docs: [Configuration](https://learn.microsoft.com/en-us/dotnet/core/extensions/configuration)
- MS Docs: [Health Checks](https://learn.microsoft.com/en-us/aspnet/core/host-and-deploy/health-checks)

## Approval

- [ ] Architecture approved
- [ ] Breaking changes acceptable
- [ ] Migration strategy approved
- [ ] Timeline realistic

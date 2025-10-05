---
id: RFC-0036
title: Platform-Agnostic Hosting and UI Abstraction
status: Draft
category: architecture
created: 2025-10-05
updated: 2025-10-05
depends-on: RFC-0029
---

# RFC-0036: Platform-Agnostic Hosting and UI Abstraction

## Summary

Generalize the hosting pattern from RFC-0029 to support multiple platforms (Console, Unity, Godot) through a unified `IWingedBeanApp` abstraction and platform-specific host implementations that bridge .NET Generic Host with native platform lifecycles.

## Motivation

### Current State (Post RFC-0029)

RFC-0029 introduces `ITerminalApp : IHostedService` for console/terminal applications, enabling .NET Generic Host integration. However:

1. ❌ **Terminal-only**: Only works for console apps, not Unity/Godot
2. ❌ **UI coupling**: `ITerminalApp` mixes lifecycle with rendering concerns
3. ❌ **No abstraction**: Can't write platform-agnostic game logic
4. ❌ **Duplicated patterns**: Each platform will reinvent hosting integration

### Vision

```
┌─────────────────────────────────────────────────┐
│         IWingedBeanApp (Lifecycle)              │
│         (extends IHostedService)                │
└────────────────┬────────────────────────────────┘
                 │
    ┌────────────┴────────────┬──────────────────┐
    │                         │                  │
┌───▼────────┐       ┌────────▼──────┐   ┌───────▼──────┐
│  IUIApp    │       │  IUnityApp    │   │  IGodotApp   │
│ (Abstract) │       │ (Unity-spec)  │   │ (Godot-spec) │
└─────┬──────┘       └───────────────┘   └──────────────┘
      │
┌─────▼──────────┐
│ ITerminalApp   │
│ (Terminal-spec)│
└────────────────┘

Platform Hosts (implementations):
• WingedBean.Hosting.Console  → wraps IHost
• WingedBean.Hosting.Unity    → bridges Unity lifecycle
• WingedBean.Hosting.Godot    → bridges Godot lifecycle
```

## Goals

1. **Unified lifecycle abstraction** across all platforms
2. **Separate UI concerns** from application lifecycle
3. **Platform-specific hosts** that adapt native lifecycles to `IHostedService`
4. **Backward compatibility** with RFC-0029 terminal implementation
5. **Reusable game logic** that doesn't depend on platform

## Proposal

### Phase 1: Core Hosting Contracts

#### 1.1 WingedBean.Contracts.Hosting

```csharp
using Microsoft.Extensions.Hosting;

namespace WingedBean.Contracts.Hosting;

/// <summary>
/// Base interface for all Winged Bean applications.
/// Provides standard lifecycle management across platforms.
/// </summary>
public interface IWingedBeanApp : IHostedService
{
    /// <summary>
    /// Application name (for logging, diagnostics).
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Current application state.
    /// </summary>
    AppState State { get; }

    /// <summary>
    /// Fired when application state changes.
    /// </summary>
    event EventHandler<AppStateChangedEventArgs>? StateChanged;
}

/// <summary>
/// Application lifecycle states.
/// </summary>
public enum AppState
{
    NotStarted,
    Starting,
    Running,
    Stopping,
    Stopped,
    Faulted
}

public class AppStateChangedEventArgs : EventArgs
{
    public AppState PreviousState { get; init; }
    public AppState NewState { get; init; }
    public Exception? Error { get; init; }
}
```

#### 1.2 WingedBean.Contracts.Hosting - Host Interface

```csharp
namespace WingedBean.Contracts.Hosting;

/// <summary>
/// Abstraction for platform-specific hosts.
/// Wraps .NET Generic Host or native platform lifecycle.
/// </summary>
public interface IWingedBeanHost
{
    /// <summary>
    /// Run the host and block until shutdown.
    /// </summary>
    Task RunAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Start the host without blocking.
    /// </summary>
    Task StartAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Stop the host gracefully.
    /// </summary>
    Task StopAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Access to DI container.
    /// </summary>
    IServiceProvider Services { get; }
}

/// <summary>
/// Builder for configuring Winged Bean hosts.
/// </summary>
public interface IWingedBeanHostBuilder
{
    IWingedBeanHostBuilder ConfigureServices(Action<IServiceCollection> configure);
    IWingedBeanHostBuilder ConfigureAppConfiguration(Action<IConfigurationBuilder> configure);
    IWingedBeanHostBuilder ConfigureLogging(Action<ILoggingBuilder> configure);
    IWingedBeanHost Build();
}
```

### Phase 2: UI Abstraction Layer

#### 2.1 WingedBean.Contracts.UI

```csharp
namespace WingedBean.Contracts.UI;

/// <summary>
/// Platform-agnostic UI application.
/// Extends IWingedBeanApp with rendering capabilities.
/// </summary>
public interface IUIApp : IWingedBeanApp
{
    /// <summary>
    /// Render the current frame.
    /// Called by platform-specific host's render loop.
    /// </summary>
    Task RenderAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Handle user input (platform-agnostic).
    /// </summary>
    Task HandleInputAsync(InputEvent input, CancellationToken cancellationToken = default);

    /// <summary>
    /// Resize/reconfigure the UI surface.
    /// </summary>
    Task ResizeAsync(int width, int height, CancellationToken cancellationToken = default);

    /// <summary>
    /// UI-specific events.
    /// </summary>
    event EventHandler<UIEventArgs>? UIEvent;
}

/// <summary>
/// Platform-agnostic input event.
/// </summary>
public abstract record InputEvent
{
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
}

public record KeyInputEvent : InputEvent
{
    public required string Key { get; init; }
    public bool Ctrl { get; init; }
    public bool Alt { get; init; }
    public bool Shift { get; init; }
}

public record MouseInputEvent : InputEvent
{
    public required int X { get; init; }
    public required int Y { get; init; }
    public MouseButton Button { get; init; }
    public MouseEventType EventType { get; init; }
}

public enum MouseButton { Left, Right, Middle, None }
public enum MouseEventType { Click, Move, Scroll }

public class UIEventArgs : EventArgs
{
    public required string EventType { get; init; }
    public object? Data { get; init; }
}
```

#### 2.2 Terminal-Specific UI (Extends RFC-0029)

```csharp
namespace WingedBean.Contracts.TerminalUI;

/// <summary>
/// Terminal-specific UI application.
/// Extends IUIApp with terminal-specific capabilities (ANSI, PTY, etc.).
/// </summary>
public interface ITerminalApp : IUIApp
{
    // Terminal-specific operations
    Task SendRawInputAsync(byte[] data, CancellationToken ct = default);
    Task SetCursorPositionAsync(int x, int y, CancellationToken ct = default);
    Task WriteAnsiAsync(string ansiSequence, CancellationToken ct = default);

    // Terminal-specific events
    event EventHandler<TerminalOutputEventArgs>? OutputReceived;
    event EventHandler<TerminalExitEventArgs>? Exited;
}

// Configuration (from RFC-0029, now under TerminalUI namespace)
public class TerminalAppConfig
{
    public string Name { get; set; } = string.Empty;
    public int Cols { get; set; } = 80;
    public int Rows { get; set; } = 24;
    public string? WorkingDirectory { get; set; }
    public Dictionary<string, string> Environment { get; set; } = new();
}
```

### Phase 3: Platform-Specific Hosts

#### 3.1 Console Host (RFC-0029 Implementation)

```csharp
namespace WingedBean.Hosting.Console;

/// <summary>
/// Console/terminal host using .NET Generic Host.
/// Direct wrapper around Microsoft.Extensions.Hosting.IHost.
/// </summary>
public class ConsoleWingedBeanHost : IWingedBeanHost
{
    private readonly IHost _host;

    public ConsoleWingedBeanHost(IHost host)
    {
        _host = host;
    }

    public Task RunAsync(CancellationToken cancellationToken = default)
        => _host.RunAsync(cancellationToken);

    public Task StartAsync(CancellationToken cancellationToken = default)
        => _host.StartAsync(cancellationToken);

    public Task StopAsync(CancellationToken cancellationToken = default)
        => _host.StopAsync(cancellationToken);

    public IServiceProvider Services => _host.Services;
}

/// <summary>
/// Builder for console hosts.
/// </summary>
public class ConsoleWingedBeanHostBuilder : IWingedBeanHostBuilder
{
    private readonly IHostBuilder _hostBuilder;

    public ConsoleWingedBeanHostBuilder(string[] args)
    {
        _hostBuilder = Host.CreateDefaultBuilder(args)
            .UseConsoleLifetime(); // Graceful shutdown on Ctrl+C
    }

    public IWingedBeanHostBuilder ConfigureServices(Action<IServiceCollection> configure)
    {
        _hostBuilder.ConfigureServices((context, services) => configure(services));
        return this;
    }

    public IWingedBeanHostBuilder ConfigureAppConfiguration(Action<IConfigurationBuilder> configure)
    {
        _hostBuilder.ConfigureAppConfiguration((context, config) => configure(config));
        return this;
    }

    public IWingedBeanHostBuilder ConfigureLogging(Action<ILoggingBuilder> configure)
    {
        _hostBuilder.ConfigureLogging((context, logging) => configure(logging));
        return this;
    }

    public IWingedBeanHost Build()
    {
        var host = _hostBuilder.Build();
        return new ConsoleWingedBeanHost(host);
    }
}
```

#### 3.2 Unity Host (Bridges Unity Lifecycle)

```csharp
namespace WingedBean.Hosting.Unity;

/// <summary>
/// Unity host that bridges Unity MonoBehaviour lifecycle to IWingedBeanApp.
/// Unity's lifecycle is authoritative (Update, OnDestroy, etc.).
/// </summary>
public class UnityWingedBeanHost : MonoBehaviour, IWingedBeanHost
{
    private IServiceProvider? _services;
    private IWingedBeanApp? _app;
    private CancellationTokenSource? _cts;

    public IServiceProvider Services => _services
        ?? throw new InvalidOperationException("Host not started");

    private void Awake()
    {
        // Build service provider (Unity doesn't use Generic Host)
        var services = new ServiceCollection();

        // Configure services (set by builder)
        _configureServices?.Invoke(services);

        _services = services.BuildServiceProvider();
        _app = _services.GetRequiredService<IWingedBeanApp>();
    }

    private async void Start()
    {
        _cts = new CancellationTokenSource();
        await StartAsync(_cts.Token);
    }

    private void Update()
    {
        // Call RenderAsync if app is IUIApp
        if (_app is IUIApp uiApp)
        {
            // Unity Update is sync, so we can't await
            // Either use async void or queue render for next frame
            _ = uiApp.RenderAsync(_cts?.Token ?? default);
        }
    }

    private async void OnDestroy()
    {
        await StopAsync(default);
        _cts?.Dispose();
    }

    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
        if (_app != null)
            await _app.StartAsync(cancellationToken);
    }

    public async Task StopAsync(CancellationToken cancellationToken = default)
    {
        if (_app != null)
            await _app.StopAsync(cancellationToken);
    }

    public Task RunAsync(CancellationToken cancellationToken = default)
    {
        // Unity controls the run loop, so this is a no-op
        // The MonoBehaviour lifecycle keeps the app running
        return Task.CompletedTask;
    }

    // Builder state (set before GameObject is created)
    private Action<IServiceCollection>? _configureServices;

    public static UnityWingedBeanHostBuilder CreateBuilder()
        => new UnityWingedBeanHostBuilder();
}

public class UnityWingedBeanHostBuilder : IWingedBeanHostBuilder
{
    private Action<IServiceCollection>? _configureServices;
    private Action<IConfigurationBuilder>? _configureConfig;

    public IWingedBeanHostBuilder ConfigureServices(Action<IServiceCollection> configure)
    {
        _configureServices += configure;
        return this;
    }

    public IWingedBeanHostBuilder ConfigureAppConfiguration(Action<IConfigurationBuilder> configure)
    {
        _configureConfig += configure;
        return this;
    }

    public IWingedBeanHostBuilder ConfigureLogging(Action<ILoggingBuilder> configure)
    {
        // Unity uses its own logging - could bridge to ILogger
        return this;
    }

    public IWingedBeanHost Build()
    {
        // Create Unity GameObject with host component
        var hostObject = new GameObject("WingedBeanHost");
        var host = hostObject.AddComponent<UnityWingedBeanHost>();

        // Pass configuration to host
        // (Need to store in host before Awake runs)
        // This requires some refactoring - possibly use ScriptableObject

        return host;
    }
}
```

#### 3.3 Godot Host (Bridges Godot Lifecycle)

```csharp
namespace WingedBean.Hosting.Godot;

/// <summary>
/// Godot host that bridges Godot Node lifecycle to IWingedBeanApp.
/// Godot's lifecycle is authoritative (_Ready, _Process, etc.).
/// </summary>
public partial class GodotWingedBeanHost : Node, IWingedBeanHost
{
    private IServiceProvider? _services;
    private IWingedBeanApp? _app;
    private CancellationTokenSource? _cts;

    public IServiceProvider Services => _services
        ?? throw new InvalidOperationException("Host not started");

    public override void _Ready()
    {
        // Build service provider
        var services = new ServiceCollection();
        _configureServices?.Invoke(services);

        _services = services.BuildServiceProvider();
        _app = _services.GetRequiredService<IWingedBeanApp>();

        _cts = new CancellationTokenSource();
        _ = StartAsync(_cts.Token);
    }

    public override void _Process(double delta)
    {
        // Call RenderAsync if app is IUIApp
        if (_app is IUIApp uiApp)
        {
            _ = uiApp.RenderAsync(_cts?.Token ?? default);
        }
    }

    public override void _ExitTree()
    {
        _ = StopAsync(default);
        _cts?.Dispose();
    }

    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
        if (_app != null)
            await _app.StartAsync(cancellationToken);
    }

    public async Task StopAsync(CancellationToken cancellationToken = default)
    {
        if (_app != null)
            await _app.StopAsync(cancellationToken);
    }

    public Task RunAsync(CancellationToken cancellationToken = default)
    {
        // Godot controls the run loop
        return Task.CompletedTask;
    }

    // Builder state
    private Action<IServiceCollection>? _configureServices;

    public static GodotWingedBeanHostBuilder CreateBuilder()
        => new GodotWingedBeanHostBuilder();
}

public class GodotWingedBeanHostBuilder : IWingedBeanHostBuilder
{
    private Action<IServiceCollection>? _configureServices;

    public IWingedBeanHostBuilder ConfigureServices(Action<IServiceCollection> configure)
    {
        _configureServices += configure;
        return this;
    }

    public IWingedBeanHostBuilder ConfigureAppConfiguration(Action<IConfigurationBuilder> configure)
    {
        // Godot config integration
        return this;
    }

    public IWingedBeanHostBuilder ConfigureLogging(Action<ILoggingBuilder> configure)
    {
        // Godot logging integration
        return this;
    }

    public IWingedBeanHost Build()
    {
        // Create Godot node with host
        var host = new GodotWingedBeanHost();
        // Pass configuration before _Ready
        return host;
    }
}
```

### Phase 4: Unified Host Factory

```csharp
namespace WingedBean.Hosting;

/// <summary>
/// Factory for creating platform-appropriate hosts.
/// Auto-detects platform or allows explicit selection.
/// </summary>
public static class WingedBeanHost
{
    /// <summary>
    /// Create a host builder for the current platform.
    /// </summary>
    public static IWingedBeanHostBuilder CreateDefaultBuilder(string[] args)
    {
        // Auto-detect platform
        if (IsUnityRuntime())
            return new UnityWingedBeanHostBuilder();

        if (IsGodotRuntime())
            return new GodotWingedBeanHostBuilder();

        // Default to console
        return new ConsoleWingedBeanHostBuilder(args);
    }

    /// <summary>
    /// Create a console host builder explicitly.
    /// </summary>
    public static IWingedBeanHostBuilder CreateConsoleBuilder(string[] args)
        => new ConsoleWingedBeanHostBuilder(args);

    /// <summary>
    /// Create a Unity host builder explicitly.
    /// </summary>
    public static IWingedBeanHostBuilder CreateUnityBuilder()
        => new UnityWingedBeanHostBuilder();

    /// <summary>
    /// Create a Godot host builder explicitly.
    /// </summary>
    public static IWingedBeanHostBuilder CreateGodotBuilder()
        => new GodotWingedBeanHostBuilder();

    private static bool IsUnityRuntime()
        => AppDomain.CurrentDomain.GetAssemblies()
            .Any(a => a.GetName().Name == "UnityEngine");

    private static bool IsGodotRuntime()
        => AppDomain.CurrentDomain.GetAssemblies()
            .Any(a => a.GetName().Name == "GodotSharp");
}
```

### Phase 5: Example Usage

#### 5.1 Console Application (RFC-0029 Compatible)

```csharp
// ConsoleDungeon.Host/Program.cs
using WingedBean.Hosting;

var host = WingedBeanHost.CreateConsoleBuilder(args)
    .ConfigureAppConfiguration(config =>
    {
        config.AddJsonFile("appsettings.json", optional: true);
        config.AddEnvironmentVariables("DUNGEON_");
    })
    .ConfigureServices(services =>
    {
        services.Configure<TerminalAppConfig>(
            context.Configuration.GetSection("Terminal"));

        // Register terminal app as hosted service
        services.AddSingleton<ITerminalApp, ConsoleDungeonApp>();
        services.AddHostedService<ITerminalApp>(sp =>
            sp.GetRequiredService<ITerminalApp>());
    })
    .Build();

await host.RunAsync();
```

#### 5.2 Unity Application

```csharp
// UnityBootstrap.cs (attached to scene GameObject)
using UnityEngine;
using WingedBean.Hosting;

public class UnityBootstrap : MonoBehaviour
{
    void Start()
    {
        var host = WingedBeanHost.CreateUnityBuilder()
            .ConfigureServices(services =>
            {
                services.AddSingleton<IUIApp, MyUnityGame>();
            })
            .Build();

        // Host lifecycle is managed by Unity MonoBehaviour
    }
}
```

#### 5.3 Godot Application

```csharp
// GodotBootstrap.cs (attached to scene)
using Godot;
using WingedBean.Hosting;

public partial class GodotBootstrap : Node
{
    public override void _Ready()
    {
        var host = WingedBeanHost.CreateGodotBuilder()
            .ConfigureServices(services =>
            {
                services.AddSingleton<IUIApp, MyGodotGame>();
            })
            .Build();

        AddChild((Node)host); // Add host as child node
    }
}
```

#### 5.4 Platform-Agnostic Game Logic

```csharp
// Shared game implementation - works on all platforms!
public class DungeonGame : IUIApp
{
    private readonly ILogger<DungeonGame> _logger;
    private readonly GameConfig _config;
    private AppState _state = AppState.NotStarted;

    public string Name => "Dungeon Game";
    public AppState State => _state;
    public event EventHandler<AppStateChangedEventArgs>? StateChanged;
    public event EventHandler<UIEventArgs>? UIEvent;

    public DungeonGame(ILogger<DungeonGame> logger, IOptions<GameConfig> config)
    {
        _logger = logger;
        _config = config.Value;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting {GameName}", Name);
        SetState(AppState.Starting);

        // Initialize game systems
        await InitializeGameSystemsAsync(cancellationToken);

        SetState(AppState.Running);
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Stopping {GameName}", Name);
        SetState(AppState.Stopping);

        // Cleanup
        await ShutdownGameSystemsAsync(cancellationToken);

        SetState(AppState.Stopped);
    }

    public Task RenderAsync(CancellationToken cancellationToken)
    {
        // Platform-agnostic render logic
        // Platform-specific renderer will handle actual output
        return Task.CompletedTask;
    }

    public Task HandleInputAsync(InputEvent input, CancellationToken cancellationToken)
    {
        _logger.LogDebug("Input: {InputType}", input.GetType().Name);

        return input switch
        {
            KeyInputEvent key => HandleKeyInput(key, cancellationToken),
            MouseInputEvent mouse => HandleMouseInput(mouse, cancellationToken),
            _ => Task.CompletedTask
        };
    }

    public Task ResizeAsync(int width, int height, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Resize: {Width}x{Height}", width, height);
        return Task.CompletedTask;
    }

    private void SetState(AppState newState)
    {
        var previous = _state;
        _state = newState;
        StateChanged?.Invoke(this, new AppStateChangedEventArgs
        {
            PreviousState = previous,
            NewState = newState
        });
    }

    private Task HandleKeyInput(KeyInputEvent key, CancellationToken ct)
    {
        // Game input handling
        return Task.CompletedTask;
    }

    private Task HandleMouseInput(MouseInputEvent mouse, CancellationToken ct)
    {
        // Game input handling
        return Task.CompletedTask;
    }

    private Task InitializeGameSystemsAsync(CancellationToken ct)
    {
        // ECS setup, asset loading, etc.
        return Task.CompletedTask;
    }

    private Task ShutdownGameSystemsAsync(CancellationToken ct)
    {
        // Save state, dispose resources
        return Task.CompletedTask;
    }
}
```

## Migration Path from RFC-0029

### Step 1: Keep ITerminalApp Compatible

```csharp
// OLD (RFC-0029): ITerminalApp : IHostedService
// NEW (RFC-0033): ITerminalApp : IUIApp : IWingedBeanApp : IHostedService

// Existing terminal apps automatically gain:
// - IWingedBeanApp.Name, State, StateChanged
// - IUIApp.RenderAsync, HandleInputAsync, ResizeAsync
```

### Step 2: No Breaking Changes for RFC-0029 Code

```csharp
// ConsoleDungeonApp from RFC-0029 still works!
public class ConsoleDungeonApp : ITerminalApp
{
    // Implement IHostedService (from RFC-0029) ✅
    public Task StartAsync(CancellationToken ct) { }
    public Task StopAsync(CancellationToken ct) { }

    // Implement IWingedBeanApp (new) ✅
    public string Name => "Console Dungeon";
    public AppState State => _state;
    public event EventHandler<AppStateChangedEventArgs>? StateChanged;

    // Implement IUIApp (new) ✅
    public Task RenderAsync(CancellationToken ct) { }
    public Task HandleInputAsync(InputEvent input, CancellationToken ct) { }
    public Task ResizeAsync(int width, int height, CancellationToken ct) { }
    public event EventHandler<UIEventArgs>? UIEvent;

    // Implement ITerminalApp (from RFC-0029) ✅
    public Task SendRawInputAsync(byte[] data, CancellationToken ct) { }
    // ... other terminal methods
}
```

### Step 3: Optional - Extract to Platform-Agnostic Base

```csharp
// ConsoleDungeonApp.cs - Terminal-specific wrapper
public class ConsoleDungeonApp : ITerminalApp
{
    private readonly DungeonGame _game; // Platform-agnostic core

    public ConsoleDungeonApp(DungeonGame game)
    {
        _game = game;
    }

    // Delegate lifecycle to core game
    public Task StartAsync(CancellationToken ct) => _game.StartAsync(ct);
    public Task StopAsync(CancellationToken ct) => _game.StopAsync(ct);

    // Implement terminal-specific methods
    public Task SendRawInputAsync(byte[] data, CancellationToken ct)
    {
        // Convert bytes to InputEvent, delegate to game
        var inputEvent = ConvertToInputEvent(data);
        return _game.HandleInputAsync(inputEvent, ct);
    }

    // ... other terminal-specific adapters
}

// DungeonGame.cs - Platform-agnostic (can run on Unity/Godot too!)
public class DungeonGame : IUIApp { /* ... */ }
```

## Project Structure

```
WingedBean/
├── framework/
│   ├── WingedBean.Contracts.Hosting/        # NEW
│   │   ├── IWingedBeanApp.cs
│   │   ├── IWingedBeanHost.cs
│   │   ├── IWingedBeanHostBuilder.cs
│   │   └── AppState.cs
│   │
│   ├── WingedBean.Contracts.UI/              # NEW
│   │   ├── IUIApp.cs
│   │   ├── InputEvent.cs
│   │   └── UIEventArgs.cs
│   │
│   ├── WingedBean.Contracts.TerminalUI/      # NEW (renamed from .Terminal)
│   │   ├── ITerminalApp.cs                   # NOW: extends IUIApp
│   │   ├── TerminalAppConfig.cs
│   │   └── TerminalEventArgs.cs
│   │
│   ├── WingedBean.Hosting/                   # NEW - Core hosting
│   │   └── WingedBeanHost.cs                 # Factory
│   │
│   ├── WingedBean.Hosting.Console/           # NEW
│   │   ├── ConsoleWingedBeanHost.cs
│   │   └── ConsoleWingedBeanHostBuilder.cs
│   │
│   ├── WingedBean.Hosting.Unity/             # NEW
│   │   ├── UnityWingedBeanHost.cs
│   │   └── UnityWingedBeanHostBuilder.cs
│   │
│   └── WingedBean.Hosting.Godot/             # NEW
│       ├── GodotWingedBeanHost.cs
│       └── GodotWingedBeanHostBuilder.cs
│
└── console/
    ├── ConsoleDungeon.Host/
    │   └── Program.cs                         # UPDATED: Use WingedBeanHost.CreateConsoleBuilder()
    │
    └── plugins/
        └── WingedBean.Plugins.ConsoleDungeon/
            ├── ConsoleDungeonApp.cs           # UPDATED: Implement new interfaces
            └── DungeonGame.cs                 # NEW: Platform-agnostic core
```

## Implementation Plan

### Phase 1: Foundation (Week 1)
- [ ] Create `WingedBean.Contracts.Hosting` project
- [ ] Create `WingedBean.Contracts.UI` project
- [ ] Define `IWingedBeanApp`, `IWingedBeanHost`, `IWingedBeanHostBuilder`
- [ ] Define `IUIApp` and input abstractions

### Phase 2: Console Host (Week 2)
- [ ] Create `WingedBean.Hosting` project (factory)
- [ ] Create `WingedBean.Hosting.Console` project
- [ ] Implement `ConsoleWingedBeanHost` and builder
- [ ] Update `ConsoleDungeon.Host` to use new hosting

### Phase 3: Terminal Contract Migration (Week 3)
- [ ] Rename `WingedBean.Contracts.Terminal` → `WingedBean.Contracts.TerminalUI`
- [ ] Update `ITerminalApp` to extend `IUIApp`
- [ ] Update `ConsoleDungeonApp` to implement new interfaces
- [ ] Verify RFC-0029 compatibility

### Phase 4: Unity/Godot Hosts (Week 4-5)
- [ ] Create `WingedBean.Hosting.Unity` project
- [ ] Implement Unity host and builder
- [ ] Create `WingedBean.Hosting.Godot` project
- [ ] Implement Godot host and builder
- [ ] Create sample Unity/Godot apps

### Phase 5: Documentation & Testing (Week 6)
- [ ] Migration guide for RFC-0029 apps
- [ ] Platform-specific hosting guides
- [ ] Integration tests for all platforms
- [ ] Update RFC-0029 to reference RFC-0033

## Breaking Changes

### For RFC-0029 Users

**Minimal breaking changes:**
1. ⚠️ `WingedBean.Contracts.Terminal` → `WingedBean.Contracts.TerminalUI` (namespace change)
2. ✅ `ITerminalApp` now extends `IUIApp` (additive)
3. ✅ Must implement new properties/methods from `IWingedBeanApp` and `IUIApp`

**Mitigation:**
- Default implementations via base class `TerminalAppBase : ITerminalApp`
- Adapter pattern for legacy apps

### For New Platform Support

**Unity/Godot:**
- Must understand async/await patterns (not always idiomatic in game engines)
- DI integration differs from .NET Generic Host

**Mitigation:**
- Provide base classes for common scenarios
- Document async patterns for game engines

## Alternatives Considered

### Alternative 1: Separate Hosting Per Platform (No Abstraction)

```
IConsoleApp (uses IHost)
IUnityApp (uses Unity lifecycle)
IGodotApp (uses Godot lifecycle)
```

**Rejected because:**
- Can't share game logic across platforms
- Duplicated hosting patterns
- No unified lifecycle

### Alternative 2: Platform Detection at Runtime

```csharp
public class UniversalApp : IWingedBeanApp
{
    public Task StartAsync(CancellationToken ct)
    {
        if (IsUnity())
            return StartUnityAsync(ct);
        else if (IsGodot())
            return StartGodotAsync(ct);
        // ...
    }
}
```

**Rejected because:**
- Messy conditional logic
- Tight coupling to all platforms
- Hard to test

### Alternative 3: Use BackgroundService Base Class

**Considered for console apps:**
```csharp
public abstract class WingedBeanAppBase : BackgroundService, IWingedBeanApp
{
    protected override Task ExecuteAsync(CancellationToken ct)
        => RunAsync(ct);

    protected abstract Task RunAsync(CancellationToken ct);
}
```

**Decision:** Offer as optional base class, not required (same as RFC-0029)

## Open Questions

1. **Unity DI Container**: Unity has its own DI (Zenject, VContainer). Do we:
   - Use MS DI and ignore Unity DI?
   - Bridge between the two?
   - **Proposed**: Use MS DI, provide bridge utilities

2. **Async in Unity**: Unity's `Update()` is synchronous. Do we:
   - Fire-and-forget async calls?
   - Use coroutines?
   - Queue async work for next frame?
   - **Proposed**: Fire-and-forget with error handling

3. **Configuration in Unity/Godot**: These use their own config systems. Do we:
   - Only use `IConfiguration` for console apps?
   - Bridge to Unity/Godot config formats?
   - **Proposed**: Platform-specific config providers

4. **Logging Integration**: Unity uses `Debug.Log`, Godot uses `GD.Print`. Do we:
   - Bridge `ILogger` to platform logging?
   - Use `ILogger` only?
   - **Proposed**: Create platform-specific `ILoggerProvider`

## Benefits

- ✅ **Unified lifecycle** across all platforms
- ✅ **Platform-agnostic game logic** (write once, run anywhere)
- ✅ **Familiar .NET patterns** (IHostedService, IOptions, ILogger)
- ✅ **Backward compatible** with RFC-0029
- ✅ **Future-proof** for new platforms (web, mobile, etc.)
- ✅ **Testable** - mock platform-specific behaviors
- ✅ **Separation of concerns** - UI vs. lifecycle vs. platform

## Risks

- ⚠️ **Complexity**: More abstractions to understand
- ⚠️ **Async overhead**: May not be idiomatic for Unity/Godot
- ⚠️ **Unity/Godot integration**: Lifecycle bridging is non-trivial
- ⚠️ **Learning curve**: Teams must understand hosting patterns

## Dependencies

- **Depends on**: RFC-0029 (ITerminalApp + IHostedService)
- **Package dependencies**:
  - `Microsoft.Extensions.Hosting` (console only)
  - `Microsoft.Extensions.DependencyInjection` (all platforms)
  - Unity/Godot SDKs (platform-specific)

## References

- RFC-0029: ITerminalApp Integration with .NET Generic Host
- [.NET Generic Host](https://learn.microsoft.com/en-us/dotnet/core/extensions/generic-host)
- [Unity Dependency Injection](https://docs.unity3d.com/Packages/com.unity.netcode.gameobjects@latest/manual/advanced-topics/ways-synchronize.html)
- [Godot C# Guide](https://docs.godotengine.org/en/stable/tutorials/scripting/c_sharp/index.html)

## Approval

- [ ] Architecture approved
- [ ] Abstraction layers acceptable
- [ ] Unity/Godot integration feasible
- [ ] Migration path from RFC-0029 acceptable
- [ ] Timeline realistic

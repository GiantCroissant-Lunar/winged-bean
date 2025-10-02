---
title: "RFC-0002: Service Platform Core (4-Tier, Multi-Profile Architecture)"
---

# RFC-0002: Service Platform Core (4-Tier, Multi-Profile Architecture)

## Status

Draft

## Date

2025-09-30

## Summary

Establish a **full-fledged 4-tier service platform** for winged-bean (inspired by pinto-bean), supporting **multiple execution profiles** (Console/Terminal.Gui, Unity, Godot, Web). The architecture uses:

- **Tier-1**: Pure contracts (profile-agnostic interfaces and DTOs)
- **Tier-2**: Source-generated façades with `[RealizeService]` attribute
- **Tier-3**: Profile-aware adapters (resilience, load contexts, telemetry, schedulers, transport)
- **Tier-4**: Profile-specific providers (Terminal.Gui + PTY for Console, Unity APIs, Godot APIs, Browser APIs)

**The Console profile is the first implementation** to validate the architecture, with Unity and Godot profiles planned as future iterations. Source generators are **essential** as the service count will grow significantly across profiles.

## Motivation

### Vision

Build a **cross-profile service platform** where:

1. Services are defined once (Tier-1 contracts)
2. Automatically realized across profiles (Tier-2 source-generated façades)
3. Cross-cutting concerns (resilience, telemetry, hot-swap) work uniformly (Tier-3 adapters)
4. Profile-specific implementations plug in cleanly (Tier-4 providers)

### Problems to Solve

1. **Multi-Profile Support**: Console (Terminal.Gui), Unity, Godot, and Web environments with shared service contracts
2. **Service Realization at Scale**: As service count grows (PTY, Recording, Session, Analytics, AI, Resources, etc.), hand-written façades become unmaintainable → **source generators required**
3. **Provider Selection**: PickOne, FanOut, and Sharded strategies for dynamic provider routing
4. **Hot-Swap**: Runtime provider replacement (Unity HybridCLR, Godot hot-reload, .NET ALC)
5. **Cross-Cutting Concerns**: Resilience, telemetry, main-thread scheduling must work uniformly across profiles
6. **Current State**: Console app (PTY + Terminal.Gui) has tight coupling; must refactor to scalable platform

### Current Architecture Issues

**Console Profile (Current):**

- `server.js` (Node.js PTY service) + `TerminalGuiApp.cs` (C#) tightly coupled
- No abstraction for recording, transport, or PTY implementation
- Hard to test, hard to extend, impossible to reuse for Unity/Godot

**Future Profiles Need:**

- **Unity**: MonoBehaviour lifecycle, HybridCLR hot-swap, Unity Netcode transport, IL2CPP AOT
- **Godot**: Node lifecycle, GodotSharp hot-reload, ENet/WebRTC transport, native C#
- **Web**: Browser environment, WebAssembly constraints, HTTP/WebSocket transport, no filesystem

**Without a platform architecture, each profile requires a complete rewrite.**

## Proposal

### 4-Tier Service Platform Architecture

Like pinto-bean, winged-bean is a **cross-profile service platform** where services are defined once (Tier-1 contracts) and realized across multiple execution profiles via source-generated façades (Tier-2), profile-specific adapters (Tier-3), and providers (Tier-4).

```
┌────────────────────────────────────────────────────────────────────────────────────┐
│ Tier 1: Contracts (Pure .NET/TypeScript Interfaces, Profile-Agnostic)             │
│ - Service Interfaces: IPtyService, IRecorder, ISessionManager, ITransport         │
│ - Pure DTOs: PtyConfig, RecordingConfig, SessionState, TransportMessage           │
│ - NO Unity/Godot/Console dependencies - pure abstraction                          │
│ - NuGet Package: WingedBean.Contracts (shared across all profiles)                │
└────────────────────────────────────────────────────────────────────────────────────┘
                                          ↓
┌────────────────────────────────────────────────────────────────────────────────────┐
│ Tier 2: Source-Generated Façades (Profile-Specific Service Realization)           │
│ - [RealizeService(typeof(IPtyService))] → Generates PtyServiceFacade              │
│ - [RealizeService(typeof(IRecorder))] → Generates RecorderFacade                  │
│ - Façades delegate to Tier-3 adapters via typed registries                        │
│ - Selection Strategies: PickOne (default), FanOut, Sharded                        │
│ - NuGet Package: WingedBean.Facades (generated at build time)                     │
└────────────────────────────────────────────────────────────────────────────────────┘
                                          ↓
┌────────────────────────────────────────────────────────────────────────────────────┐
│ Tier 3: Adapters (Cross-Cutting Concerns, Profile-Aware)                          │
│ - Resilience: IResilienceExecutor (Polly-based retry, circuit breaker, timeout)   │
│ - Load Context: ILoadContext (ALC on .NET/Godot, HybridCLR on Unity, N/A on Web)  │
│ - Telemetry: IAspectRuntime (OpenTelemetry traces, metrics, logs)                 │
│ - Schedulers: IMainThreadScheduler (Unity: MonoBehaviour, Godot: SceneTree, etc.) │
│ - Transport: ITransport (WebSocket, Stdio, HTTP, SignalR depending on profile)    │
│ - NuGet Packages: WingedBean.Adapters.* (profile-specific, e.g., .Console, .Unity)│
└────────────────────────────────────────────────────────────────────────────────────┘
                                          ↓
┌────────────────────────────────────────────────────────────────────────────────────┐
│ Tier 4: Providers (Profile-Specific Implementations)                              │
│ - Console: node-pty (PTY), Terminal.Gui (UI), ws (WebSocket), fs (file I/O)       │
│ - Unity: HybridCLR (hot-swap), UnityEngine.* APIs, Unity Netcode (transport)      │
│ - Godot: Godot.* APIs, GodotSharp hot-reload, ENet/WebRTC (transport)             │
│ - Web: Browser APIs (WebAssembly), fetch/WebSocket (transport), IndexedDB (storage)│
└────────────────────────────────────────────────────────────────────────────────────┘

                              ╔═══════════════════════════════════════╗
                              ║  Execution Profiles (Composition Root) ║
                              ╚═══════════════════════════════════════╝
                  ┌──────────────┬──────────────┬──────────────┬──────────────┐
                  │              │              │              │              │
            ┌─────▼─────┐  ┌────▼────┐  ┌──────▼──────┐  ┌───▼──────────┐
            │  Console  │  │  Unity  │  │    Godot    │  │     Web      │
            │  Profile  │  │ Profile │  │   Profile   │  │   Profile    │
            └───────────┘  └─────────┘  └─────────────┘  └──────────────┘
            │ Terminal.Gui│ MonoBehaviour│  Node-based  │  Blazor/WASM  │
            │   + PTY     │ + HybridCLR  │  + C# Godot  │  + Browser    │
            └─────────────┴──────────────┴──────────────┴───────────────┘
```

### Tier Responsibilities

#### Tier 1: Contracts (Pure .NET/TypeScript Interfaces)

**Profile-agnostic interfaces and DTOs only. No implementation, no profile dependencies.**

**C# Contracts (`src/WingedBean.Contracts/`):**

```csharp
// Contracts/IPtyService.cs
namespace WingedBean.Contracts;

public interface IPtyService
{
    Task<IPtyProcess> SpawnAsync(PtyConfig config, CancellationToken ct = default);
    Task KillAsync(string processId, CancellationToken ct = default);
}

public interface IPtyProcess
{
    int Pid { get; }
    event EventHandler<string> DataReceived;
    event EventHandler<(int ExitCode, string? Signal)> Exited;
    Task WriteAsync(string data, CancellationToken ct = default);
    Task ResizeAsync(int cols, int rows, CancellationToken ct = default);
    Task KillAsync();
}

// Contracts/IRecorder.cs
public interface IRecorder
{
    Task StartAsync(RecordingConfig config, CancellationToken ct = default);
    void RecordOutput(string data);
    void RecordInput(string data);
    void RecordResize(int cols, int rows);
    Task<string> StopAsync(); // Returns file path
}

// Contracts/ISessionManager.cs
public interface ISessionManager
{
    Task<ISession> CreateSessionAsync(SessionConfig config, CancellationToken ct = default);
    Task<ISession?> GetSessionAsync(string sessionId, CancellationToken ct = default);
    Task CloseSessionAsync(string sessionId, CancellationToken ct = default);
}

// Contracts/ITransport.cs
public interface ITransport
{
    Task SendAsync(byte[] data, CancellationToken ct = default);
    event EventHandler<byte[]> DataReceived;
    event EventHandler<(int? Code, string? Reason)> Closed;
    Task CloseAsync();
}

// Contracts/Models/PtyConfig.cs
public record PtyConfig(
    string Command,
    string[] Args,
    int Cols,
    int Rows,
    string? Cwd = null,
    IReadOnlyDictionary<string, string>? Env = null
);

public record RecordingConfig(
    string OutputPath,
    int Cols,
    int Rows,
    bool CaptureInput,
    bool CaptureResize
);

public record SessionConfig(
    string SessionId,
    PtyConfig PtyConfig,
    RecordingConfig? RecordingConfig = null,
    TransportConfig? TransportConfig = null
);
```

**TypeScript Contracts (for Node.js PTY service - `projects/nodejs/pty-service/contracts/`):**

```typescript
// contracts/IPtyService.ts
export interface IPtyService {
  spawn(config: PtyConfig): IPtyProcess;
  kill(processId: string): Promise<void>;
}

export interface IPtyProcess {
  readonly pid: number;
  onData(handler: (data: string) => void): void;
  onExit(handler: (code: number, signal?: string) => void): void;
  write(data: string): void;
  resize(cols: number, rows: number): void;
  kill(): void;
}

// contracts/models.ts
export interface PtyConfig {
  command: string;
  args: string[];
  cols: number;
  rows: number;
  cwd?: string;
  env?: Record<string, string>;
}
```

**Key Point**: Tier-1 has **ZERO** dependencies on Unity/Godot/Terminal.Gui/Browser. Pure abstraction.

#### Tier 2: Source-Generated Façades

**Façades are auto-generated at build time using Roslyn source generators.**

**Source Generator Attributes:**

```csharp
// WingedBean.SourceGen/RealizeServiceAttribute.cs
namespace WingedBean.SourceGen;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public class RealizeServiceAttribute : Attribute
{
    public RealizeServiceAttribute(Type contractType) { }
}

[AttributeUsage(AttributeTargets.Interface)]
public class GenerateRegistryAttribute : Attribute
{
    public GenerateRegistryAttribute() { }
}
```

**Usage (Developer writes this):**

```csharp
// WingedBean.Facades/PtyService.cs
using WingedBean.Contracts;
using WingedBean.SourceGen;

namespace WingedBean.Facades;

[RealizeService(typeof(IPtyService))]
public partial class PtyService : IPtyService
{
    // Source generator fills in implementation
    // Delegates to IServiceRegistry<IPtyService>
}
```

**Generated Code (by source generator):**

```csharp
// WingedBean.Facades/PtyService.g.cs (generated)
namespace WingedBean.Facades;

public partial class PtyService : IPtyService
{
    private readonly IServiceRegistry<IPtyService> _registry;
    private readonly IResilienceExecutor _resilience;
    private readonly IAspectRuntime _telemetry;

    public PtyService(
        IServiceRegistry<IPtyService> registry,
        IResilienceExecutor resilience,
        IAspectRuntime telemetry)
    {
        _registry = registry;
        _resilience = resilience;
        _telemetry = telemetry;
    }

    public async Task<IPtyProcess> SpawnAsync(PtyConfig config, CancellationToken ct = default)
    {
        using var activity = _telemetry.StartActivity("PtyService.SpawnAsync");
        return await _resilience.ExecuteAsync(async () =>
        {
            var provider = _registry.SelectProvider(); // Uses PickOne strategy by default
            return await provider.SpawnAsync(config, ct);
        }, ct);
    }

    public async Task KillAsync(string processId, CancellationToken ct = default)
    {
        using var activity = _telemetry.StartActivity("PtyService.KillAsync");
        await _resilience.ExecuteAsync(async () =>
        {
            var provider = _registry.SelectProvider();
            await provider.KillAsync(processId, ct);
        }, ct);
    }
}
```

**Why Source Generators?**

- Service count will grow: PTY, Recording, Session, Analytics, AI, Resources, SceneFlow, etc.
- Hand-writing façades for each service × each method × resilience + telemetry = unmaintainable
- Generated code is consistent, tested once, works for all services

#### Tier 3: Adapters (Cross-Cutting Concerns)

**Profile-aware adapters that wrap Tier-4 providers.**

**Resilience Adapter:**

```csharp
// WingedBean.Adapters/IResilienceExecutor.cs
public interface IResilienceExecutor
{
    Task<T> ExecuteAsync<T>(Func<Task<T>> operation, CancellationToken ct = default);
    Task ExecuteAsync(Func<Task> operation, CancellationToken ct = default);
}

// WingedBean.Adapters.Console/PollyResilienceExecutor.cs
public class PollyResilienceExecutor : IResilienceExecutor
{
    private readonly ResiliencePipeline _pipeline;

    public PollyResilienceExecutor()
    {
        _pipeline = new ResiliencePipelineBuilder()
            .AddRetry(new RetryStrategyOptions { MaxRetryAttempts = 3 })
            .AddTimeout(TimeSpan.FromSeconds(30))
            .Build();
    }

    public async Task<T> ExecuteAsync<T>(Func<Task<T>> operation, CancellationToken ct = default)
        => await _pipeline.ExecuteAsync(async _ => await operation(), ct);
}
```

**Load Context Adapter (Hot-Swap):**

```csharp
// WingedBean.Adapters/ILoadContext.cs
public interface ILoadContext
{
    Task<ILoadedProvider> LoadProviderAsync(string assemblyPath, string typeName, CancellationToken ct = default);
    Task UnloadProviderAsync(ILoadedProvider provider, CancellationToken ct = default);
}

// WingedBean.Adapters.Console/AlcLoadContext.cs (.NET ALC-based)
public class AlcLoadContext : ILoadContext
{
    private readonly Dictionary<string, AssemblyLoadContext> _contexts = new();

    public async Task<ILoadedProvider> LoadProviderAsync(string assemblyPath, string typeName, CancellationToken ct)
    {
        var alc = new AssemblyLoadContext(assemblyPath, isCollectible: true);
        var assembly = alc.LoadFromAssemblyPath(assemblyPath);
        var type = assembly.GetType(typeName);
        var instance = Activator.CreateInstance(type);
        _contexts[assemblyPath] = alc;
        return new LoadedProvider(instance, assemblyPath);
    }

    public async Task UnloadProviderAsync(ILoadedProvider provider, CancellationToken ct)
    {
        if (_contexts.TryGetValue(provider.AssemblyPath, out var alc))
        {
            alc.Unload();
            _contexts.Remove(provider.AssemblyPath);
        }
    }
}

// WingedBean.Adapters.Unity/HybridClrLoadContext.cs (Unity HybridCLR-based)
public class HybridClrLoadContext : ILoadContext
{
    // Unity-specific hot-swap via HybridCLR
    public async Task<ILoadedProvider> LoadProviderAsync(string assemblyPath, string typeName, CancellationToken ct)
    {
        // Use HybridCLR.RuntimeApi.LoadMetadataForAOTAssembly
        throw new NotImplementedException("Unity HybridCLR implementation");
    }
}
```

**Telemetry Adapter:**

```csharp
// WingedBean.Adapters/IAspectRuntime.cs
public interface IAspectRuntime
{
    IDisposable StartActivity(string name);
    void RecordMetric(string name, double value);
    void Log(LogLevel level, string message);
}

// WingedBean.Adapters.Console/OpenTelemetryAspectRuntime.cs
public class OpenTelemetryAspectRuntime : IAspectRuntime
{
    private readonly ActivitySource _activitySource = new("WingedBean");

    public IDisposable StartActivity(string name)
        => _activitySource.StartActivity(name) ?? NullActivity.Instance;

    public void RecordMetric(string name, double value)
    {
        // OpenTelemetry metrics
    }

    public void Log(LogLevel level, string message)
    {
        // OpenTelemetry logging
    }
}
```

**Main-Thread Scheduler Adapter:**

```csharp
// WingedBean.Adapters/IMainThreadScheduler.cs
public interface IMainThreadScheduler
{
    Task RunOnMainThreadAsync(Action action);
    Task<T> RunOnMainThreadAsync<T>(Func<T> func);
}

// WingedBean.Adapters.Unity/UnityMainThreadScheduler.cs
public class UnityMainThreadScheduler : MonoBehaviour, IMainThreadScheduler
{
    private readonly ConcurrentQueue<Action> _actions = new();

    void Update()
    {
        while (_actions.TryDequeue(out var action))
            action();
    }

    public Task RunOnMainThreadAsync(Action action)
    {
        var tcs = new TaskCompletionSource();
        _actions.Enqueue(() => { action(); tcs.SetResult(); });
        return tcs.Task;
    }
}
```

**Transport Adapters:**

```typescript
// projects/nodejs/pty-service/adapters/WebSocketTransport.ts
export class WebSocketTransport implements ITransport {
  constructor(private ws: WebSocket) {}

  async send(data: Buffer | string): Promise<void> {
    if (this.ws.readyState === WebSocket.OPEN) {
      this.ws.send(data);
    }
  }

  onReceive(handler: (data: Buffer | string) => void): void {
    this.ws.on('message', handler);
  }

  onClose(handler: (code?: number, reason?: string) => void): void {
    this.ws.on('close', handler);
  }

  async close(): Promise<void> {
    this.ws.close();
  }
}

// adapters/StdioTransport.ts (CLI mode)
export class StdioTransport implements ITransport {
  async send(data: Buffer | string): Promise<void> {
    process.stdout.write(data);
  }

  onReceive(handler: (data: Buffer | string) => void): void {
    process.stdin.setRawMode(true);
    process.stdin.on('data', handler);
  }

  onClose(handler: () => void): void {
    process.on('SIGINT', handler);
    process.on('SIGTERM', handler);
  }

  async close(): Promise<void> {
    process.exit(0);
  }
}
```

#### Tier 4: Providers (Profile-Specific Implementations)

**Concrete implementations that realize Tier-1 contracts for a specific profile.**

**Console Profile Providers:**

```typescript
// Node.js PTY Provider
// projects/nodejs/pty-service/providers/NodePtyProvider.ts
import * as pty from 'node-pty';

export class NodePtyProvider implements IPtyService {
  spawn(config: PtyConfig): IPtyProcess {
    const ptyProcess = pty.spawn(config.command, config.args, {
      name: 'xterm-256color',
      cols: config.cols,
      rows: config.rows,
      cwd: config.cwd,
      env: { ...process.env, ...config.env }
    });

    return new NodePtyProcess(ptyProcess);
  }

  async kill(processId: string): Promise<void> {
    // Implementation
  }
}
```

```csharp
// C# Recording Provider
// WingedBean.Providers.Console/AsciinemaRecorder.cs
public class AsciinemaRecorder : IRecorder
{
    private FileStream? _fileStream;
    private DateTime _startTime;

    public async Task StartAsync(RecordingConfig config, CancellationToken ct)
    {
        _startTime = DateTime.UtcNow;
        _fileStream = File.Create(config.OutputPath);

        var header = new { version = 2, width = config.Cols, height = config.Rows, timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds() };
        await _fileStream.WriteAsync(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(header) + "\n"), ct);
    }

    public void RecordOutput(string data)
    {
        var timestamp = (DateTime.UtcNow - _startTime).TotalSeconds;
        var evt = JsonSerializer.Serialize(new object[] { timestamp, "o", data }) + "\n";
        _fileStream?.Write(Encoding.UTF8.GetBytes(evt));
    }

    // ... RecordInput, RecordResize, StopAsync
}
```

**Unity Profile Providers (Future):**

```csharp
// WingedBean.Providers.Unity/UnityTransport.cs
public class UnityTransport : ITransport
{
    private NetworkManager _networkManager;

    public async Task SendAsync(byte[] data, CancellationToken ct)
    {
        // Use Unity Netcode for transport
        _networkManager.SendToServer(data);
    }

    // ...
}
```

### Provider Selection Strategies (Typed Registry)

**PickOne (Default):**

```csharp
// WingedBean.Registry/IServiceRegistry.cs
public interface IServiceRegistry<TService>
{
    TService SelectProvider(SelectionCriteria? criteria = null);
    IEnumerable<TService> GetAllProviders();
}

// PickOne implementation
public class PickOneRegistry<TService> : IServiceRegistry<TService>
{
    private readonly List<(TService Provider, ProviderMetadata Metadata)> _providers = new();

    public TService SelectProvider(SelectionCriteria? criteria = null)
    {
        // Filter by capability, platform, priority
        var filtered = _providers
            .Where(p => criteria == null || p.Metadata.Matches(criteria))
            .OrderByDescending(p => p.Metadata.Priority)
            .ThenBy(p => p.Metadata.Name.GetStableHashCode())
            .FirstOrDefault();

        return filtered.Provider ?? throw new InvalidOperationException("No provider available");
    }
}
```

**FanOut (for analytics, telemetry):**

```csharp
public class FanOutRegistry<TService> : IServiceRegistry<TService>
{
    public TService SelectProvider(SelectionCriteria? criteria = null)
    {
        // Returns a proxy that fans out to all providers
        return (TService)FanOutProxy.Create<TService>(_providers.Select(p => p.Provider).ToList());
    }
}
```

**Sharded (for analytics by event name prefix):**

```csharp
public class ShardedRegistry<TService> : IServiceRegistry<TService>
{
    private readonly Dictionary<string, TService> _explicitMap = new();
    private readonly ConsistentHash<TService> _consistentHash = new();

    public TService SelectProvider(SelectionCriteria? criteria = null)
    {
        var shardKey = criteria?.ShardKey ?? throw new ArgumentNullException(nameof(criteria.ShardKey));

        // Check explicit map first
        if (_explicitMap.TryGetValue(shardKey, out var provider))
            return provider;

        // Fallback to consistent hashing
        return _consistentHash.GetNode(shardKey);
    }
}
```

### Dependency Injection & Composition Root

**Console Profile DI Setup:**

```csharp
// WingedBean.Profiles.Console/Program.cs
var services = new ServiceCollection();

// Tier-3 Adapters
services.AddSingleton<IResilienceExecutor, PollyResilienceExecutor>();
services.AddSingleton<ILoadContext, AlcLoadContext>();
services.AddSingleton<IAspectRuntime, OpenTelemetryAspectRuntime>();

// Tier-4 Providers
services.AddSingleton<IRecorder, AsciinemaRecorder>();

// Tier-2 Façades (generated)
services.AddSingleton<IPtyService, PtyService>();

// Registries (generated by source generator)
services.AddSingleton<IServiceRegistry<IPtyService>, PickOneRegistry<IPtyService>>();

var serviceProvider = services.BuildServiceProvider();
var ptyService = serviceProvider.GetRequiredService<IPtyService>();

await ptyService.SpawnAsync(new PtyConfig("dotnet", new[] { "run" }, 80, 24));
```

**Unity Profile DI Setup (Future):**

```csharp
// UnityBootstrap.cs
public class UnityBootstrap : MonoBehaviour
{
    void Start()
    {
        var services = new ServiceCollection();

        // Unity-specific adapters
        services.AddSingleton<IMainThreadScheduler>(this.gameObject.AddComponent<UnityMainThreadScheduler>());
        services.AddSingleton<ILoadContext, HybridClrLoadContext>();
        services.AddSingleton<ITransport, UnityTransport>();

        // Same Tier-2 façades work!
        services.AddSingleton<IPtyService, PtyService>();

        // ...
    }
}
```

## Implementation Plan

### Phase 1: Platform Foundation (4-6 weeks)

1. **Week 1-2: Tier-1 Contracts**
   - Define `WingedBean.Contracts` NuGet package
   - Interfaces: `IPtyService`, `IRecorder`, `ISessionManager`, `ITransport`
   - DTOs: `PtyConfig`, `RecordingConfig`, `SessionConfig`
   - TypeScript contracts mirror for Node.js
   - **DOD**: Contracts package published to local NuGet feed

2. **Week 3-4: Source Generator (Tier-2)**
   - Create `WingedBean.SourceGen` Roslyn analyzer project
   - Implement `[RealizeService]` attribute
   - Generate façade classes with resilience + telemetry wrappers
   - Generate typed registries (`IServiceRegistry<T>`)
   - Unit tests for generator (golden file tests)
   - **DOD**: Source generator produces valid façade code for sample service

3. **Week 5-6: Tier-3 Adapters (Console Profile)**
   - Implement `PollyResilienceExecutor`
   - Implement `AlcLoadContext` (hot-swap for .NET)
   - Implement `OpenTelemetryAspectRuntime`
   - Implement transport adapters: `WebSocketTransport`, `StdioTransport`
   - **DOD**: Adapters packaged in `WingedBean.Adapters.Console`

### Phase 2: Console Profile Implementation (3-4 weeks)

1. **Week 7-8: Tier-4 Providers (Console)**
   - `NodePtyProvider` (TypeScript wrapper for `node-pty`)
   - `AsciinemaRecorder` (C# implementation)
   - Refactor `server.js` to use adapters
   - **DOD**: Console profile runs with new architecture

2. **Week 9-10: Registry & DI**
   - Implement `PickOneRegistry<T>`
   - Implement DI setup for Console profile
   - Refactor `TerminalGuiApp.cs` to use DI
   - **DOD**: Console app fully refactored, all tests pass

### Phase 3: Testing & Documentation (2 weeks)

1. **Week 11: Tests**
   - Unit tests for all adapters
   - Integration tests for Console profile
   - E2E test: Browser → xterm.js → WebSocket → PTY → Terminal.Gui
   - **DOD**: >80% code coverage

2. **Week 12: Documentation**
   - Architecture diagrams
   - Developer guide: "How to add a new service"
   - Developer guide: "How to add a new profile (Unity/Godot)"
   - **DOD**: Complete documentation published

### Phase 4: Future Profiles (Future Iterations)

1. **Unity Profile** (8-10 weeks)
   - Unity-specific adapters (`UnityMainThreadScheduler`, `HybridClrLoadContext`)
   - Unity transport (`Unity Netcode`)
   - Unity providers
   - Unity DI bootstrap

2. **Godot Profile** (6-8 weeks)
   - Godot-specific adapters
   - Godot transport (ENet/WebRTC)
   - Godot providers
   - Godot DI bootstrap

3. **Web Profile** (4-6 weeks)
   - Browser-specific adapters
   - Blazor/WebAssembly transport
   - Browser providers
   - WASM DI bootstrap

## Benefits

### vs. Pinto-Bean

| Aspect | Pinto-Bean | Winged-Bean (This RFC) |
|--------|------------|------------------------|
| **Tier-1** | Pure .NET contracts, engine-agnostic | Pure .NET/TypeScript contracts, profile-agnostic |
| **Tier-2** | Source-generated façades with `[RealizeService]` | **Same** - source-generated façades |
| **Tier-3** | Resilience, load contexts, telemetry, schedulers | **Same** - profile-specific implementations |
| **Tier-4** | Unity/Godot/custom engine SDKs | Console (PTY/Terminal.Gui), Unity, Godot, Web |
| **Selection Strategies** | PickOne, FanOut, Sharded | **Same** - typed registries |
| **Hot-Swap** | ALC-based plugin loading | **Same** - profile-aware (ALC, HybridCLR, etc.) |
| **Primary Use Case** | Cross-engine game services | Cross-profile application platform |

### Key Advantages

1. **Write Once, Run Anywhere**: Define `IPtyService` once, works in Console, Unity, Godot, Web
2. **Source Generators Scale**: As service count grows, no manual boilerplate
3. **Testability**: Mock any tier in isolation
4. **Hot-Swap Ready**: Load/unload providers at runtime (Unity plugins, Godot modules)
5. **Uniform Cross-Cutting**: Resilience, telemetry work the same across all profiles

## Testing Strategy

### Unit Tests

- **Tier-3 Adapters**: Mock Tier-4 providers (e.g., mock `node-pty`, mock `fs`)
- **Tier-2 Façades**: Mock Tier-3 adapters (e.g., mock `IRecorder`, mock `ITransport`)
- **Source Generator**: Golden file tests (input → expected output)

### Integration Tests

- **CLI Mode**: Spawn real PTY, verify recording file format
- **Web Mode**: Start WebSocket server, send messages, verify PTY interaction

### E2E Tests

- Full stack: Browser → xterm.js → WebSocket → PTY → Terminal.Gui
- Verify asciinema playback of recorded sessions

## Definition of Done

### Phase 1 (Platform Foundation)

- [ ] `WingedBean.Contracts` NuGet package created and published
- [ ] `WingedBean.SourceGen` Roslyn analyzer generates valid façade code
- [ ] `WingedBean.Adapters.Console` package with resilience, load context, telemetry
- [ ] Unit tests for source generator (>90% coverage)

### Phase 2 (Console Profile)

- [ ] `NodePtyProvider` implemented in TypeScript
- [ ] `AsciinemaRecorder` implemented in C#
- [ ] `server.js` refactored to use adapters
- [ ] `TerminalGuiApp.cs` refactored to use DI
- [ ] DI setup for Console profile complete
- [ ] All existing functionality preserved (no regressions)

### Phase 3 (Testing & Documentation)

- [ ] Unit tests for all adapters (>80% coverage)
- [ ] Integration tests for CLI and Web modes
- [ ] E2E test passing
- [ ] Architecture diagrams published
- [ ] Developer guide: "How to add a new service"
- [ ] Developer guide: "How to add a new profile"

### Phase 4 (Future Profiles)

- [ ] Unity profile implemented and tested
- [ ] Godot profile implemented and tested
- [ ] Web profile implemented and tested

## Dependencies

- **RFC-0001**: Asciinema recording (provides recording requirements)
- **ADR-0005**: PTY service architecture (establishes PTY as integration point)
- **ADR-0006**: PM2 development workflow (establishes dev environment)

## Risks and Mitigations

### Risk: Source Generator Complexity

- **Mitigation**: Start with simple façade generation, iterate
- **Mitigation**: Golden file tests ensure correctness
- **Mitigation**: Reference pinto-bean generator implementation

### Risk: Performance Overhead

- **Mitigation**: Adapters are thin wrappers (minimal indirection)
- **Mitigation**: Benchmark PTY data forwarding (must be <5% overhead)

### Risk: Unity/Godot Integration Challenges

- **Mitigation**: Console profile validates architecture first
- **Mitigation**: IL2CPP/AOT constraints addressed via source generators (no runtime reflection)

### Risk: Learning Curve

- **Mitigation**: Clear documentation and examples
- **Mitigation**: Gradual migration (keep old code working during transition)

## Alternatives Considered

### 1. Keep Existing Flat Architecture

- ✅ Simpler, less code
- ❌ Cannot scale to Unity/Godot without complete rewrite
- **Decision**: Platform architecture is essential for multi-profile support

### 2. No Source Generators (Hand-Written Façades)

- ✅ Simpler tooling
- ❌ Unmaintainable as service count grows
- **Decision**: Source generators are essential for scalability

### 3. Microservices Architecture

- ✅ Maximum isolation
- ❌ Way overkill for local console/Unity/Godot apps
- **Decision**: Monolithic with clean boundaries is sufficient

## Future Enhancements

1. **Additional Selection Strategies**: WeightedRandom, HealthWeighted
2. **Plugin Marketplace**: Hot-loadable provider ecosystem
3. **Distributed Tracing**: OpenTelemetry across profiles
4. **Code Coverage Reporting**: Per-tier coverage metrics
5. **Performance Profiling**: Profile adapter overhead

## References

- [Pinto-Bean RFC-0001: Service Platform Core](../../ref-projects/pinto-bean/docs/rfcs/rfc-0001-service-platform-core.md)
- [Pinto-Bean RFC-0002: Code Generation & Consumption](../../ref-projects/pinto-bean/docs/rfcs/rfc-0002-codegen-and-consumption.md)
- [Pinto-Bean RFC-0003: Selection Strategies](../../ref-projects/pinto-bean/docs/rfcs/rfc-0003-selection-strategies.md)
- [Clean Architecture by Robert C. Martin](https://blog.cleancoder.com/uncle-bob/2012/08/13/the-clean-architecture.html)

## Notes

- This RFC establishes winged-bean as a **full-fledged service platform**, not just a console app
- Console profile is the **first implementation profile** to validate the architecture
- Unity and Godot profiles follow the same pattern, reusing Tier-1 contracts and Tier-2 façades
- Source generators are **essential** for scalability across profiles and services
- The architecture is designed for **long-term growth** across multiple execution environments

---

**Author**: Ray Wang (with Claude AI assistance)
**Reviewer**: [Pending]
**Implementation**: [Assigned]

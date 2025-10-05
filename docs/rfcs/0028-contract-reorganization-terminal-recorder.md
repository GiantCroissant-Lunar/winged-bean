---
id: RFC-0028
title: Contract Reorganization - Terminal and Recorder Separation
status: Draft
category: architecture
created: 2025-10-05
updated: 2025-10-05
---

# RFC-0028: Contract Reorganization - Terminal and Recorder Separation

## Summary

Reorganize `WingedBean.Contracts.Pty` into two focused contracts: `WingedBean.Contracts.Terminal` for terminal application lifecycle and `WingedBean.Contracts.Recorder` for session recording. Remove unused `IPtyService` interface that was a stub for Node.js PTY integration.

## Motivation

### Current Problems

1. **Misleading naming**: `WingedBean.Contracts.Pty` implies PTY (pseudo-terminal) functionality, but:
   - The contract doesn't provide real PTY capabilities
   - C# cannot directly create PTYs without P/Invoke or native interop
   - Actual PTY functionality is provided by Node.js `node-pty` service

2. **Mixed concerns**: The contract contains three unrelated interfaces:
   - `IPtyService` - Unused stub for process spawning (never properly implemented)
   - `ITerminalApp` - Application lifecycle interface (actively used by ConsoleDungeon)
   - `IRecorder` - Session recording interface (actively used by AsciinemaRecorder)

3. **Unused code**: `IPtyService` and `NodePtyService` implementation are:
   - Not real PTY implementations (just `System.Diagnostics.Process` with stdio redirection)
   - Comments admit: "In a real implementation, this would use the Node.js PTY service"
   - Never integrated with actual Node.js PTY server in `/development/nodejs/pty-service/`

### Actual Architecture

**Node.js PTY Service** (`/nodejs/pty-service/`):
```
Node.js (node-pty) → spawns → .NET Console App
         ↑                           ↓
    WebSocket ←─────── output ───────┘
```

- Node.js handles **real PTY** using `node-pty` library
- .NET Console App runs **inside** the PTY as a child process
- Communication happens via standard I/O through the PTY
- C# side doesn't need `IPtyService` - it's already running in a PTY!

## Proposal

### Contract Structure

**Current (incorrect):**
```
WingedBean.Contracts.Pty/
  ├── IPtyService.cs (unused - remove)
  ├── ITerminalApp.cs (keep - rename namespace)
  └── IRecorder.cs (move out)

WingedBean.Plugins.PtyService/ (remove entire plugin)
```

**Proposed (correct):**
```
WingedBean.Contracts.Terminal/
  └── ITerminalApp.cs (application lifecycle)

WingedBean.Contracts.Recorder/
  └── IRecorder.cs (session recording/replay)
```

### Interface Analysis

#### ITerminalApp - Application Lifecycle

```csharp
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

**Purpose**: Defines lifecycle for terminal-based applications (game loop, rendering, input handling)

**NOT**:
- Process spawning (handled by Node.js PTY externally)
- PTY management (handled by Node.js)
- Generic hosted service (specialized for terminal apps)

**Comparison to MS Extensions Hosting:**

| Feature | `IHostedService` | `ITerminalApp` |
|---------|------------------|----------------|
| Purpose | Generic background service | Terminal-specific application |
| Lifecycle | Start/Stop | Start/Stop + Input/Resize |
| Events | None | OutputReceived, Exited |
| Input | N/A | SendInputAsync |
| Terminal-specific | No | Yes (cols/rows, output events) |
| Use Case | Long-running services | Interactive terminal applications |

**Why not use `IHostedService`?**

1. **Terminal-specific contract**: Requires `SendInputAsync`, `ResizeAsync`, terminal dimensions
2. **Event-driven**: Needs `OutputReceived` and `Exited` events for real-time interaction
3. **Simpler**: `ITerminalApp` is focused on terminal apps, not generic services
4. **Plugin-friendly**: Works with custom registry, not tied to MS DI container lifecycle
5. **Game-specific**: Designed for game loop, input routing, scene management

**Could we use `IHostedService`?** Yes, but it would require:
- Additional abstraction layer for terminal-specific features
- More boilerplate for event handling
- Integration complexity with custom registry system
- Loss of type safety for terminal operations

**Decision**: Keep `ITerminalApp` as a specialized contract for terminal applications. It's simpler, more focused, and better suited to the domain.

#### IRecorder - Session Recording

```csharp
public interface IRecorder
{
    Task StartRecordingAsync(string sessionId, SessionMetadata metadata, CancellationToken ct = default);
    Task RecordDataAsync(string sessionId, byte[] data, DateTimeOffset timestamp, CancellationToken ct = default);
    Task<string> StopRecordingAsync(string sessionId, CancellationToken ct = default);
}
```

**Purpose**: Records terminal sessions to Asciinema format for replay

**Orthogonal concern**: Recording is independent of application lifecycle

## Implementation Plan

### Phase 1: Create New Contracts

1. **Create `WingedBean.Contracts.Recorder`**
   - Move `IRecorder` interface
   - Move `SessionMetadata` class
   - Update namespace to `WingedBean.Contracts.Recorder`

2. **Rename `WingedBean.Contracts.Pty` → `WingedBean.Contracts.Terminal`**
   - Rename directory and `.csproj`
   - Keep `ITerminalApp` interface
   - Update namespace to `WingedBean.Contracts.Terminal`
   - Remove `IPtyService` and `IRecorder`

### Phase 2: Update References

1. **AsciinemaRecorder plugin**
   - Update project reference: `WingedBean.Contracts.Pty` → `WingedBean.Contracts.Recorder`
   - Update using: `WingedBean.Contracts` → `WingedBean.Contracts.Recorder`

2. **ConsoleDungeon plugins**
   - Update project reference: `WingedBean.Contracts.Pty` → `WingedBean.Contracts.Terminal`
   - Update using: `WingedBean.Contracts` → `WingedBean.Contracts.Terminal`

3. **Host projects**
   - Update `ConsoleDungeon.Host` references
   - Update `WingedBean.Demo` references

### Phase 3: Remove Unused Code

1. **Delete `WingedBean.Plugins.PtyService`** (entire plugin)
   - `NodePtyService.cs` - fake PTY implementation
   - `PtyServicePlugin.cs` - plugin registration
   - Associated tests

2. **Remove from solution files**
   - Remove project references
   - Clean build artifacts

### Phase 4: Verification

1. Build all projects
2. Run tests
3. Verify `ConsoleDungeon.Host` still launches correctly
4. Verify AsciinemaRecorder plugin works

### Phase 5: .NET Generic Host Integration (Optional Enhancement)

**Goal**: Make `ITerminalApp` compatible with modern .NET hosting patterns

1. **Make `ITerminalApp` inherit from `IHostedService`**
   ```csharp
   public interface ITerminalApp : IHostedService
   {
       // Signature changes:
       // Old: Task StartAsync(TerminalAppConfig config, CancellationToken ct)
       // New: StartAsync(CancellationToken ct) from IHostedService
       //      + configure via constructor injection or factory

       Task SendInputAsync(byte[] data, CancellationToken ct = default);
       Task ResizeAsync(int cols, int rows, CancellationToken ct = default);
       event EventHandler<TerminalOutputEventArgs> OutputReceived;
       event EventHandler<TerminalExitEventArgs> Exited;
   }
   ```

2. **Create adapter for backward compatibility**
   ```csharp
   public class TerminalAppAdapter : IHostedService
   {
       private readonly ITerminalApp _app;
       private readonly TerminalAppConfig _config;

       public Task StartAsync(CancellationToken ct) => _app.StartAsync(_config, ct);
       public Task StopAsync(CancellationToken ct) => _app.StopAsync(ct);
   }
   ```

3. **Refactor `ConsoleDungeon.Host` to use `IHost`**
   - Replace manual `await terminalApp.StartAsync()` with `Host.RunAsync()`
   - Use `IHostApplicationLifetime` for graceful shutdown
   - Integrate with MS DI container
   - Support configuration via `appsettings.json`

**Benefits:**
- ✅ Standard .NET hosting patterns
- ✅ Graceful shutdown with `IHostApplicationLifetime`
- ✅ Configuration system integration
- ✅ Logging, health checks, metrics
- ✅ Compatible with modern .NET ecosystem

**Breaking Changes:**
- ⚠️ `ITerminalApp.StartAsync` signature change (config via DI, not parameter)
- ⚠️ Implementations must update to match `IHostedService` contract

**Decision**: Implement in **separate RFC** (RFC-0029) after this refactoring

## Migration Guide

### For Plugin Authors

**Before:**
```csharp
using WingedBean.Contracts; // ITerminalApp
using WingedBean.Contracts; // IRecorder
```

**After:**
```csharp
using WingedBean.Contracts.Terminal; // ITerminalApp
using WingedBean.Contracts.Recorder;  // IRecorder
```

**Project References:**

Before:
```xml
<ProjectReference Include="../WingedBean.Contracts.Pty/..." />
```

After:
```xml
<ProjectReference Include="../WingedBean.Contracts.Terminal/..." />
<!-- OR -->
<ProjectReference Include="../WingedBean.Contracts.Recorder/..." />
```

## Alternatives Considered

### Alternative 1: Keep `WingedBean.Contracts.Pty` as-is

**Rejected because:**
- Misleading name (no real PTY functionality)
- Mixed concerns (application lifecycle + recording)
- Contains unused `IPtyService` stub

### Alternative 2: Implement real PTY in C#

**Rejected because:**
- Complex P/Invoke required (platform-specific)
- Already have working Node.js PTY service
- C# app runs **inside** Node.js PTY, doesn't need to create one
- Would be architectural overengineering

### Alternative 3: Use MS Extensions Hosting Only (No ITerminalApp)

**Rejected because:**
- `IHostedService` is too generic for terminal applications
- Requires additional abstraction for terminal-specific features
- Loss of domain-specific API (SendInput, Resize, terminal events)
- Would need custom events/interfaces anyway

### Alternative 4: Make ITerminalApp inherit from IHostedService

**✅ RECOMMENDED - Implement in Phase 5:**

```csharp
namespace WingedBean.Contracts.Terminal;

// Extend IHostedService with terminal-specific features
public interface ITerminalApp : IHostedService
{
    // Terminal-specific operations (beyond Start/Stop)
    Task SendInputAsync(byte[] data, CancellationToken ct = default);
    Task ResizeAsync(int cols, int rows, CancellationToken ct = default);

    // Terminal-specific events
    event EventHandler<TerminalOutputEventArgs> OutputReceived;
    event EventHandler<TerminalExitEventArgs> Exited;
}

// Adapter for .NET Generic Host integration
public class TerminalAppHostedService : IHostedService
{
    private readonly ITerminalApp _terminalApp;
    private readonly TerminalAppConfig _config;

    public TerminalAppHostedService(ITerminalApp app, TerminalAppConfig config)
    {
        _terminalApp = app;
        _config = config;
    }

    public Task StartAsync(CancellationToken ct) => _terminalApp.StartAsync(_config, ct);
    public Task StopAsync(CancellationToken ct) => _terminalApp.StopAsync(ct);
}
```

**Benefits:**
- ✅ Compatible with .NET Generic Host ecosystem
- ✅ Can use `IHostApplicationLifetime`, graceful shutdown
- ✅ Integrates with MS DI container
- ✅ Keeps terminal-specific features
- ✅ Adapter pattern for legacy compatibility

**Current Reality Check:**

The current `ConsoleDungeon.Host` implementation **does NOT use** `IHost`:
```csharp
// Current: Manual blocking call, no hosting framework
var terminalApp = registry.Get<ITerminalApp>();
await terminalApp.StartAsync(appConfig); // Blocks until exit
```

**Should be:**
```csharp
// Better: Use .NET Generic Host
var host = Host.CreateDefaultBuilder(args)
    .ConfigureServices(services =>
    {
        services.AddSingleton<ITerminalApp>(sp => registry.Get<ITerminalApp>());
        services.AddSingleton(appConfig);
        services.AddHostedService<TerminalAppHostedService>();
    })
    .Build();

await host.RunAsync(); // Proper hosting with lifecycle management
```

### Alternative 5: Single `WingedBean.Contracts.Application`

**Rejected because:**
- `IRecorder` is orthogonal to application lifecycle
- Mixing recording with application lifecycle would couple unrelated concerns
- Harder to version independently

## Impact

### Breaking Changes

- ✅ Namespace changes: `WingedBean.Contracts` → `WingedBean.Contracts.Terminal` / `.Recorder`
- ✅ Project reference changes: `Contracts.Pty` → `Contracts.Terminal` / `Contracts.Recorder`
- ✅ Removal of `IPtyService` (unused, no impact)
- ✅ Removal of `WingedBean.Plugins.PtyService` (unused, no impact)

### Benefits

- ✅ Clearer naming (Terminal, not Pty)
- ✅ Separation of concerns (lifecycle vs recording)
- ✅ Reduced confusion about PTY responsibilities
- ✅ Removal of misleading/unused code
- ✅ Better alignment with actual architecture

### Risks

- ⚠️ Multiple namespace updates across codebase
- ⚠️ Requires careful testing after refactoring
- ⚠️ Could break downstream plugins (if any exist)

## Questions

1. **Should we version contracts separately?**
   - Currently all contracts share same version
   - Could enable independent evolution
   - **Decision**: Keep unified versioning for now

2. **Should we add `IHostedService` compatibility layer?**
   - Could wrap `ITerminalApp` as `IHostedService` adapter
   - Useful for generic hosting scenarios
   - **Decision**: Not needed now, add if use case emerges

3. **Should we add replay support to `IRecorder`?**
   - Currently only recording (write)
   - Replay (read) could be separate interface: `IRecordingPlayer`
   - **Decision**: Future enhancement, not in scope

## References

- Node.js PTY Service: `/development/nodejs/pty-service/`
- Current Contracts: `/framework/src/WingedBean.Contracts.Pty/`
- AsciinemaRecorder Plugin: `/console/src/plugins/WingedBean.Plugins.AsciinemaRecorder/`
- ConsoleDungeon Host: `/console/src/host/ConsoleDungeon.Host/`
- MS Extensions Hosting: https://learn.microsoft.com/en-us/dotnet/core/extensions/hosted-services

## Approval

- [ ] Architecture approved
- [ ] Implementation plan reviewed
- [ ] Breaking changes acceptable
- [ ] Migration guide complete

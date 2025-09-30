# RFC-0004: Project Organization and Folder Structure

## Status

Draft

## Date

2025-09-30

## Summary

Reorganize the `/projects/dotnet` directory structure to support the **4-tier service-oriented architecture** with clear separation between framework (Tier 1 & 2), console implementation (Tier 3 & 4), and Unity implementation (Tier 3 & 4). This structure emphasizes **strict tier dependency rules**, **source code generation**, and **service-oriented design** where everything is a service (including Registry, PluginLoader, and Config).

## Motivation

### Current Structure Issues

The current `/projects/dotnet` structure is flat and doesn't clearly express:
1. **Tier boundaries** (Tier 1, 2, 3, 4)
2. **Platform separation** (Framework vs Console vs Unity)
3. **Test organization** (tests scattered or missing)
4. **Service orientation** (unclear which components are services)

Current structure:
```
projects/dotnet/
├── WingedBean.Contracts/
├── WingedBean.Host/
├── WingedBean.Host.Console/
├── WingedBean.Host.Unity/
├── WingedBean.Demo/
├── WingedBean.Plugins.*/
├── console-dungeon/
└── WingedBean.sln
```

### Problems

1. **No clear tier separation**: Can't tell what's Tier 1 vs Tier 2 vs Tier 3
2. **Mixed concerns**: Framework code mixed with platform-specific code
3. **Unclear dependencies**: Easy to accidentally violate tier dependency rules
4. **Test organization**: Only one test project exists
5. **Plugin confusion**: Current "plugins" need to be mapped to new architecture

## Proposal

### Folder Structure

```
projects/dotnet/
├── framework/
│   ├── src/
│   │   ├── WingedBean.Contracts.Core/           # Tier 1: IRegistry, IPluginLoader, IPlugin
│   │   ├── WingedBean.Contracts.Config/         # Tier 1: IConfigService + Proxy (partial)
│   │   ├── WingedBean.Contracts.Audio/          # Tier 1: IAudioService + Proxy (partial)
│   │   ├── WingedBean.Contracts.Resource/       # Tier 1: IResourceService + Proxy (partial)
│   │   ├── WingedBean.Contracts.SourceGen/      # Tier 1: Roslyn generators for proxies
│   │   │
│   │   └── WingedBean.Registry/                 # Tier 2: ActualRegistry (pure C#)
│   │
│   ├── tests/
│   │   ├── WingedBean.Registry.Tests/
│   │   └── WingedBean.Contracts.Tests/
│   │
│   └── Framework.sln
│
├── console/
│   ├── src/
│   │   ├── ConsoleDungeon.SourceGen/            # Tier 3: Console-specific source gen
│   │   ├── ConsoleDungeon.Host/                 # Tier 3: Console bootstrap entry point
│   │   │
│   │   ├── WingedBean.PluginLoader/             # Tier 3: Plugin loading orchestration
│   │   ├── WingedBean.Providers.AssemblyContext/ # Tier 4: AssemblyLoadContext provider
│   │   │
│   │   ├── WingedBean.Plugins.Config/           # Tier 3: Config (wraps MS.Extensions.Configuration)
│   │   ├── WingedBean.Plugins.Audio/            # Tier 3: Audio service (NAudio)
│   │   ├── WingedBean.Plugins.Resource/         # Tier 3: Resource loading
│   │   ├── WingedBean.Plugins.PtyService/       # Tier 3: PTY service (existing)
│   │   ├── WingedBean.Plugins.AsciinemaRecorder/ # Tier 3: Recording (existing)
│   │   │
│   │   └── ConsoleDungeon/                      # The TUI application
│   │
│   ├── tests/
│   │   ├── ConsoleDungeon.Tests/
│   │   ├── WingedBean.PluginLoader.Tests/
│   │   └── WingedBean.Plugins.*.Tests/
│   │
│   └── Console.sln
│
├── unity/
│   ├── Assets/                                  # Unity project structure
│   │   ├── Scripts/
│   │   │   ├── Unity.Host/                      # Tier 3: Unity bootstrap (MonoBehaviour)
│   │   │   └── Unity.SourceGen/                 # Tier 3: Unity-specific source gen
│   │   │
│   │   └── Plugins/                             # Tier 3 & 4 assemblies
│   │       ├── WingedBean.PluginLoader.Unity/
│   │       ├── WingedBean.Providers.HybridCLR/
│   │       └── WingedBean.Plugins.*/
│   │
│   ├── Packages/
│   │   └── com.wingedbean.framework/            # UPM package (framework reference)
│   │
│   ├── ProjectSettings/
│   └── Tests/
│       └── PlayMode/
│           └── Unity.Host.Tests/
│
└── samples/
    └── WingedBean.Demo/                         # Framework usage examples
```

### Tier Dependency Rules

The architecture enforces strict one-way dependencies:

```
Tier 1 ← Tier 2 (sees Tier 1, no knowledge of Tier 3/4)
Tier 1 ← Tier 3 (sees Tier 1, no knowledge of Tier 2)
Tier 1 ← Tier 4 (can see Tier 1)
Tier 3 ← Tier 4 (can see Tier 3)
```

**Key Rules:**
- Tier 2 has NO knowledge of Tier 3/4
- Tier 3 has NO knowledge of Tier 2
- Tier 1 never references any other tier
- Tier 4 can reference Tier 1 or Tier 3

### Service-Oriented Design Principles

**Foundation Services** (not loaded as plugins):
- **Registry** (Tier 2): Manages service registration and selection
- **PluginLoader** (Tier 3): Orchestrates plugin loading/unloading

**Service Plugins** (dynamically loaded):
- **Config** (Tier 3): Multiple implementations (File, InMemory, Remote, etc.)
- **Audio** (Tier 3): Platform-specific (NAudio for Console, Unity AudioSource, etc.)
- **Resource** (Tier 3): Platform-specific (File system, Addressables, YooAsset, etc.)
- **All other services** (Tier 3)

### Project Naming Conventions

**Tier 1 Contracts:**
- Pattern: `WingedBean.Contracts.<ServiceGroup>`
- Examples: `WingedBean.Contracts.Core`, `WingedBean.Contracts.Audio`
- Each contains: Interface + Proxy (partial class with source gen attributes)

**Tier 2 Infrastructure:**
- Pattern: `WingedBean.<Component>`
- Examples: `WingedBean.Registry`, `WingedBean.Contracts.SourceGen`
- Pure C#, no platform-specific code

**Tier 3 Implementations:**
- Pattern: `WingedBean.Plugins.<Service>` or `<Platform>.<Component>`
- Examples: `WingedBean.Plugins.Audio`, `ConsoleDungeon.Host`, `Unity.Host`
- Platform-specific types allowed

**Tier 4 Providers:**
- Pattern: `WingedBean.Providers.<Technology>`
- Examples: `WingedBean.Providers.AssemblyContext`, `WingedBean.Providers.HybridCLR`
- Platform/technology-specific implementations

### Source Code Generation Strategy

**Tier 1 Source Gen** (`WingedBean.Contracts.SourceGen`):
- Generates proxy service method implementations
- Attributes: `[RealizeService(typeof(IService))]`, `[SelectionStrategy(SelectionMode.X)]`
- Output: Fills in partial proxy classes with delegation code

**Tier 3 Source Gen** (per platform):
- Console: `ConsoleDungeon.SourceGen` - generates host bootstrap logic
- Unity: `Unity.SourceGen` - generates Unity-specific patterns
- Attributes: `[GenerateHostBootstrap]`, platform-specific attributes

### Contract Project Structure

Each Tier 1 contract project contains both interface and proxy:

```csharp
// WingedBean.Contracts.Audio/IAudioService.cs
namespace WingedBean.Contracts.Audio;

public interface IAudioService
{
    void Play(string clip);
    void Stop();
    float Volume { get; set; }
}

// WingedBean.Contracts.Audio/Audio.ProxyService.cs
namespace WingedBean.Contracts.Audio;

[RealizeService(typeof(IAudioService))]
[SelectionStrategy(SelectionMode.HighestPriority)]
public partial class ProxyService : IAudioService
{
    // Source gen fills in:
    // - Play() -> _registry.Get<IAudioService>().Play(clip)
    // - Stop() -> _registry.Get<IAudioService>().Stop()
    // - Volume getter/setter -> delegate to registry
}
```

### Plugin Metadata

Plugins use attributes for metadata (not JSON manifests initially):

```csharp
// WingedBean.Plugins.Audio/NAudioService.cs
[Plugin(
    Name = "NAudio.AudioService",
    Provides = new[] { typeof(IAudioService) },
    Dependencies = new[] { typeof(IConfigService) },
    Priority = 10
)]
public class NAudioService : IAudioService
{
    // Implementation using NAudio library
}
```

Source gen can read these attributes to generate registration code.

## Bootstrap Sequence

### Console Bootstrap Flow

```csharp
// ConsoleDungeon.Host/Program.cs
[GenerateHostBootstrap]  // Source gen creates bootstrap logic
public partial class ConsoleHost
{
    public static async Task Main(string[] args)
    {
        // 1. Create foundation services (manual instantiation)
        var registry = new ActualRegistry();
        var pluginLoader = new ActualPluginLoader(
            new AssemblyContextProvider()
        );

        // 2. Register foundation services
        registry.Register<IRegistry>(registry);
        registry.Register<IPluginLoader>(pluginLoader);

        // 3. Load bootstrap plugin (Config - first plugin loaded)
        var configPlugin = await pluginLoader.LoadAsync(
            "WingedBean.Plugins.Config.dll"
        );
        registry.Register<IConfigService>(configPlugin.GetService<IConfigService>());

        // 4. Use config to determine what other plugins to load
        var configService = registry.Get<IConfigService>();
        var pluginsToLoad = configService.Get<string[]>("Plugins:Load");

        // 5. Load remaining plugins (Audio, Resource, etc.)
        foreach (var pluginPath in pluginsToLoad)
        {
            await pluginLoader.LoadAsync(pluginPath);
        }

        // 6. Application ready - start ConsoleDungeon TUI
        var app = new ConsoleDungeonApp(registry);
        await app.RunAsync();
    }
}
```

### Unity Bootstrap Flow

```csharp
// Unity.Host/UnityBootstrap.cs
using UnityEngine;

[GenerateHostBootstrap]  // Source gen creates bootstrap logic
public partial class UnityBootstrap : MonoBehaviour
{
    private void Awake()
    {
        // Similar flow to Console, but Unity-specific:
        // - Uses HybridCLR instead of AssemblyLoadContext
        // - MonoBehaviour lifecycle integration
        // - Different plugin loading paths
    }
}
```

## Migration Plan

### Phase 1: Framework Structure (Week 1-2)

1. Create `framework/` directory structure
2. Move/refactor existing contracts to `WingedBean.Contracts.*`
3. Create `WingedBean.Registry` (Tier 2)
4. Create `WingedBean.Contracts.SourceGen` (Tier 1)
5. Set up `Framework.sln`

### Phase 2: Console MVP - Preserve Existing Functionality (Week 2-3)

**Goal**: Migrate `console-dungeon` to new `console/` structure while preserving **exact** xterm.js functionality.

#### Current Console-Dungeon Setup

The existing `console-dungeon` app demonstrates:
- **Terminal.Gui v2** TUI interface (`TerminalGuiApp.cs`)
- **SuperSocket WebSocket** server (port 4040)
- **xterm.js integration** via WebSocket (Astro frontend)
- **Real-time communication** between C# console and web browser

**Critical requirement**: New `console/*` structure must maintain this working demo.

#### MVP Deliverables

1. **Create minimal console/ structure**
   ```
   console/
   ├── src/
   │   ├── ConsoleDungeon/                    # Main TUI app (migrate existing)
   │   │   ├── Program.cs                     # WebSocket + Terminal.Gui
   │   │   ├── TerminalGuiApp.cs              # Terminal.Gui v2 interface
   │   │   └── ConsoleDungeon.csproj
   │   │
   │   └── ConsoleDungeon.Host/               # Bootstrap wrapper (NEW - minimal)
   │       ├── Program.cs                     # Entry point, prepares for future plugin system
   │       └── ConsoleDungeon.Host.csproj
   │
   ├── tests/
   │   └── ConsoleDungeon.Tests/
   │
   └── Console.sln
   ```

2. **ConsoleDungeon.Host responsibilities (MVP)**
   - **Minimal bootstrap**: Just launch `ConsoleDungeon` TUI app
   - **No plugin loading yet**: Direct instantiation for MVP
   - **Prepare structure**: Set up for future Registry/PluginLoader integration

   ```csharp
   // ConsoleDungeon.Host/Program.cs (MVP)
   namespace ConsoleDungeon.Host;

   public class Program
   {
       public static async Task Main(string[] args)
       {
           Console.WriteLine("ConsoleDungeon.Host starting...");

           // MVP: Direct launch (no plugins yet)
           // TODO: Phase 3 will add Registry, PluginLoader, Config
           await ConsoleDungeon.TerminalGuiApp.Main(args);
       }
   }
   ```

3. **Preserve existing functionality**
   - ✅ Terminal.Gui v2 interface renders correctly
   - ✅ WebSocket server on port 4040
   - ✅ xterm.js can connect and display TUI
   - ✅ Commands work (help, echo, time, status, quit)
   - ✅ Real-time updates to browser

4. **Migration steps**
   ```bash
   # Step 1: Create console/ structure
   mkdir -p projects/dotnet/console/src
   mkdir -p projects/dotnet/console/tests

   # Step 2: Copy existing ConsoleDungeon
   cp -r projects/dotnet/console-dungeon/ConsoleDungeon \
         projects/dotnet/console/src/ConsoleDungeon

   # Step 3: Create minimal Host wrapper
   # (new project with simple bootstrap)

   # Step 4: Update solution
   cd projects/dotnet/console
   dotnet new sln -n Console
   dotnet sln add src/ConsoleDungeon/ConsoleDungeon.csproj
   dotnet sln add src/ConsoleDungeon.Host/ConsoleDungeon.Host.csproj

   # Step 5: Test
   dotnet build
   dotnet run --project src/ConsoleDungeon.Host/ConsoleDungeon.Host.csproj
   # Verify: Open browser, connect to xterm, see Terminal.Gui interface
   ```

5. **Definition of Done (MVP)**
   - [ ] `console/` directory structure created
   - [ ] Existing ConsoleDungeon code migrated (no changes to logic)
   - [ ] ConsoleDungeon.Host wrapper created (minimal bootstrap)
   - [ ] Console.sln compiles successfully
   - [ ] **Web browser can connect to xterm and see Terminal.Gui interface**
   - [ ] All existing commands work (help, echo, time, etc.)
   - [ ] No regressions from original `console-dungeon` behavior

#### Phase 2b: Prepare for Plugins (Optional)

If time permits in Phase 2, add structure (but don't require it for MVP):

```
console/src/
├── WingedBean.Plugins.Stub/              # Stub plugin (no-op, just structure)
│   └── StubPlugin.cs
└── WingedBean.PluginLoader.Stub/         # Stub loader (no-op, just structure)
    └── StubLoader.cs
```

These stubs do nothing but establish folder structure for Phase 3.

#### Critical: PTY Integration

The current `console-dungeon` works by:
1. **C# Console App** (ConsoleDungeon) runs Terminal.Gui v2
2. **WebSocket Server** (SuperSocket, port 4040) embedded in C# app
3. **Astro Frontend** with xterm.js connects via WebSocket
4. **Direct WebSocket protocol**: Screen updates sent as `screen:<content>`

**Important**: This is **NOT** using node-pty for PTY sessions. It's a simpler WebSocket-based approach.

For the full PTY-based architecture (future enhancement):
1. **C# Console App** outputs to stdout/TTY
2. **Node.js PTY Service** (`node-pty`) wraps C# process
3. **WebSocket Bridge** relays PTY output to xterm.js
4. **xterm.js** displays real terminal output

The MVP preserves the current WebSocket approach. PTY integration is a future enhancement (possibly in Phase 3 or 4).

### Console Architecture Evolution

#### Current State (console-dungeon)
```
ConsoleDungeon App (Monolithic)
├── TerminalGuiApp.cs         # Terminal.Gui v2 interface
├── Program.cs                # WebSocket server (SuperSocket)
└── Hardcoded dependencies    # Terminal.Gui, SuperSocket, MS.Extensions
```

**Flow**: Browser → WebSocket (port 4040) → C# Console → Terminal.Gui → WebSocket → Browser

#### MVP State (Phase 2)
```
console/
├── src/
│   ├── ConsoleDungeon/          # Same app, new location
│   └── ConsoleDungeon.Host/     # Thin wrapper (bootstrap)
└── Console.sln
```

**Change**: Folder reorganization only, no architectural changes.

#### Target State (Phase 3+)
```
console/
├── src/
│   ├── ConsoleDungeon/                    # TUI app (depends on services)
│   ├── ConsoleDungeon.Host/               # Bootstrap with Registry + PluginLoader
│   ├── WingedBean.PluginLoader/           # Tier 3: Plugin orchestration
│   ├── WingedBean.Providers.AssemblyContext/  # Tier 4: ALC provider
│   ├── WingedBean.Plugins.Config/         # Tier 3: Config service
│   ├── WingedBean.Plugins.WebSocket/      # Tier 3: WebSocket service (SuperSocket)
│   ├── WingedBean.Plugins.TerminalUI/     # Tier 3: Terminal.Gui service
│   └── WingedBean.Plugins.PtyService/     # Tier 3: PTY service (future)
```

**Flow**:
1. ConsoleDungeon.Host bootstraps
2. Creates Registry + PluginLoader
3. Loads Config plugin (determines what else to load)
4. Loads WebSocket, TerminalUI plugins
5. ConsoleDungeon app requests services via Registry
6. Browser connects to WebSocket service → TerminalUI service → xterm.js

#### Service Contracts for Console

**Phase 3 will define**:
```csharp
// Tier 1
interface IWebSocketService {
    void Start(int port);
    void Broadcast(string message);
    event Action<string> MessageReceived;
}

interface ITerminalUIService {
    void Initialize();
    void Run();
    string GetScreenContent();
}

interface IPtyService {  // Future
    Task<PtySession> CreateSessionAsync(string command);
}
```

### Phase 3: Plugin Migration (Week 3-4)

**Now with plugin-based architecture**:

1. **Refactor ConsoleDungeon to use services**:
   - Replace direct SuperSocket usage with `IWebSocketService`
   - Replace direct Terminal.Gui usage with `ITerminalUIService`
   - Get services from Registry instead of direct instantiation

2. **Create service plugins**:
   - `WingedBean.Plugins.Config` (new - MS.Extensions.Configuration wrapper)
   - `WingedBean.Plugins.WebSocket` (new - SuperSocket wrapper)
   - `WingedBean.Plugins.TerminalUI` (new - Terminal.Gui wrapper)
   - `WingedBean.Plugins.PtyService` (migrate existing - node-pty integration)
   - `WingedBean.Plugins.AsciinemaRecorder` (migrate existing - recording service)

3. **Add plugin metadata attributes**
   ```csharp
   [Plugin(
       Name = "WebSocket.SuperSocket",
       Provides = new[] { typeof(IWebSocketService) },
       Priority = 10
   )]
   public class SuperSocketWebSocketService : IWebSocketService { }
   ```

4. **Implement Tier 3 PluginLoader and Tier 4 Provider**
   - `WingedBean.PluginLoader` - Orchestrates plugin loading
   - `WingedBean.Providers.AssemblyContext` - ALC-based loading

5. **Update ConsoleDungeon.Host bootstrap**
   ```csharp
   // Phase 3 ConsoleDungeon.Host/Program.cs
   var registry = new ActualRegistry();
   var pluginLoader = new ActualPluginLoader(new AssemblyContextProvider());

   registry.Register<IRegistry>(registry);
   registry.Register<IPluginLoader>(pluginLoader);

   // Load Config plugin first
   var configPlugin = await pluginLoader.LoadAsync("WingedBean.Plugins.Config.dll");
   var config = configPlugin.GetService<IConfigService>();
   registry.Register<IConfigService>(config);

   // Load other plugins based on config
   await pluginLoader.LoadAsync("WingedBean.Plugins.WebSocket.dll");
   await pluginLoader.LoadAsync("WingedBean.Plugins.TerminalUI.dll");

   // Launch ConsoleDungeon app (now uses services from registry)
   var app = new ConsoleDungeonApp(registry);
   await app.RunAsync();
   ```

6. **Test plugin loading/registration**
   - Verify all plugins load successfully
   - Verify services registered in Registry
   - **Verify xterm.js still works** (critical regression test)

### Phase 4: Unity Structure (Week 4-5)

1. Create `unity/` directory with Unity project structure
2. Create `Unity.Host` with MonoBehaviour bootstrap
3. Create `WingedBean.Providers.HybridCLR` (Tier 4)
4. Create Unity-specific plugin implementations
5. Test Unity plugin loading

### Phase 5: Testing & Documentation (Week 5-6)

1. Create test projects for all tiers
2. Integration tests for plugin loading
3. E2E tests for complete bootstrap flow
4. Update documentation with new structure

## Benefits

### Organizational Clarity

1. **Clear tier boundaries**: Framework vs Console vs Unity
2. **Explicit dependencies**: Tier rules enforced by structure
3. **Separation of concerns**: Contracts, infrastructure, implementations clearly separated
4. **Test organization**: Tests colocated with source

### Maintainability

1. **Scalable**: Easy to add new services (just add contract + plugins)
2. **Modular**: Each service is independent, can be developed/tested separately
3. **Reusable**: Framework shared across Console, Unity, and future platforms
4. **Source gen**: Eliminates boilerplate for proxy services

### Platform Support

1. **Console**: Full support with AssemblyLoadContext
2. **Unity**: Clear path with HybridCLR
3. **Future platforms**: Structure supports Godot, Web, etc.

## Definition of Done

### Phase 1 (Framework)
- [ ] `framework/` structure created
- [ ] All Tier 1 contracts migrated
- [ ] `WingedBean.Registry` implemented (Tier 2)
- [ ] `WingedBean.Contracts.SourceGen` implemented
- [ ] `Framework.sln` compiles successfully

### Phase 2 (Console MVP)
- [ ] `console/` structure created
- [ ] Existing `ConsoleDungeon` code migrated unchanged
- [ ] `ConsoleDungeon.Host` minimal wrapper created
- [ ] `Console.sln` compiles successfully
- [ ] **xterm.js integration still works** (browser can connect and see Terminal.Gui)
- [ ] All existing commands functional (help, echo, time, status)
- [ ] No architectural changes (monolithic app preserved for MVP)

### Phase 3 (Plugins + Service Architecture)
- [ ] Tier 1 contracts created for console services (IWebSocketService, ITerminalUIService)
- [ ] Service plugins created (WebSocket, TerminalUI, Config)
- [ ] `WingedBean.PluginLoader` implemented (Tier 3)
- [ ] `WingedBean.Providers.AssemblyContext` implemented (Tier 4)
- [ ] ConsoleDungeon app refactored to use services from Registry
- [ ] Plugin metadata attributes defined and applied
- [ ] Plugin loading working end-to-end
- [ ] **xterm.js integration still works** (critical regression test)

### Phase 4 (Unity)
- [ ] `unity/` Unity project created
- [ ] `Unity.Host` with MonoBehaviour bootstrap
- [ ] `WingedBean.Providers.HybridCLR` implemented
- [ ] Unity plugins implemented
- [ ] Unity bootstrap working

### Phase 5 (Testing)
- [ ] Test projects for all components
- [ ] Unit tests (>80% coverage)
- [ ] Integration tests passing
- [ ] E2E tests passing
- [ ] Documentation updated

## Dependencies

- **RFC-0002**: 4-Tier Architecture (this RFC organizes the structure for it)
- **RFC-0003**: Plugin Architecture (this RFC provides the folder structure for plugins)

## Risks and Mitigations

### Risk: Large Migration Effort

- **Mitigation**: Phased approach, framework first, then console, then Unity
- **Mitigation**: Existing code continues to work during migration
- **Mitigation**: Each phase can be tested independently

### Risk: Breaking Changes

- **Mitigation**: Keep existing structure until new structure is validated
- **Mitigation**: Create new folders alongside existing, migrate incrementally
- **Mitigation**: Comprehensive testing at each phase

### Risk: Tier Boundary Violations

- **Mitigation**: Clear naming conventions make violations obvious
- **Mitigation**: Consider tooling to enforce tier dependencies (Roslyn analyzer)
- **Mitigation**: Code review focus on tier dependencies

## Alternatives Considered

### 1. Monolithic Structure (Keep Current)

- ✅ No migration effort
- ❌ Doesn't scale, unclear boundaries, violates architecture principles
- **Decision**: Structure must support tiered architecture

### 2. Separate Repositories (Framework, Console, Unity)

- ✅ Clearest separation
- ❌ Complicates development, versioning, and integration testing
- **Decision**: Monorepo with clear folder structure is better

### 3. Flat Structure with Naming Conventions

- ✅ Simpler migration
- ❌ Doesn't enforce tier boundaries, easy to violate rules
- **Decision**: Nested structure with explicit tier folders is clearer

## Future Enhancements

1. **Godot support**: Add `godot/` directory with similar structure
2. **Web support**: Add `web/` directory for browser/WASM
3. **Shared utilities**: `shared/` directory for cross-platform helpers
4. **NuGet packaging**: Publish framework packages to NuGet
5. **Build automation**: Scripts to build all projects, run tests, package

## References

- RFC-0002: 4-Tier Architecture
- RFC-0003: Plugin Architecture
- [Pinto-bean reference project](../../ref-projects/pinto-bean/)
- [.NET Solution Structure Best Practices](https://learn.microsoft.com/en-us/dotnet/core/porting/project-structure)

## Notes

- This RFC focuses on **structure and organization**, not implementation details
- Implementation details are covered in RFC-0002 (architecture) and RFC-0003 (plugins)
- The structure is designed to support gradual evolution and expansion
- Source code generation is a first-class concern, with dedicated projects per tier

---

**Author**: Ray Wang (with Claude AI assistance)
**Reviewer**: [Pending]
**Status**: Draft - Open for Discussion

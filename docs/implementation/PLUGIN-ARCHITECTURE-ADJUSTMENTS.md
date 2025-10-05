# Plugin Architecture Adjustments (Console Profile)

Date: 2025-10-05

This document summarizes the current plugin architecture for the Console profile, recent changes, and how to extend it. It is intended for all agents working on the console host and plugins.

## Goals

- Default to manifest-driven plugin discovery and loading (AssemblyLoadContext/ALC).
- Keep plugin registration declarative via an activator (IPluginActivator) and support automatic discovery of additional services with Scrutor.
- Use the central `IRegistry` for selection and priorities when resolving services at runtime.
- Deprecate legacy `plugins.json` while keeping a temporary escape hatch.

## Key Changes (What’s New)

- ALC discovery is now the default in the console host. Legacy `plugins.json` is disabled by default.
  - Host: `development/dotnet/console/src/host/ConsoleDungeon.Host/Program.cs:44`
  - ALC loader: `development/dotnet/console/src/host/WingedBean.Host.Console/AlcPluginLoader.cs:40`

- Plugins use `IPluginActivator` to register services into their own `IServiceCollection` (constructor injection friendly):
  - Config: `.../plugins/WingedBean.Plugins.Config/ConfigPluginActivator.cs:1`
  - TerminalUI: `.../plugins/WingedBean.Plugins.TerminalUI/TerminalUIPluginActivator.cs:1`
  - ArchECS: `.../plugins/WingedBean.Plugins.ArchECS/ArchECSPluginActivator.cs:1`
  - DungeonGame: `.../plugins/WingedBean.Plugins.DungeonGame/DungeonGamePluginActivator.cs:1`

- Scrutor assists DI wiring within each plugin by scanning the plugin assembly for classes that implement `WingedBean.Contracts.*` and adding them to DI.
  - Host bridge: `development/dotnet/console/src/host/ConsoleDungeon.Host/Program.cs:73`

- The refactored `ConsoleDungeonAppRefactored` is the only ITerminalApp. The old `ConsoleDungeonApp` was removed.
  - App: `.../plugins/WingedBean.Plugins.ConsoleDungeon/ConsoleDungeonAppRefactored.cs:1`

## Core Concepts

- `IPluginActivator` (per plugin): declaratively registers services into the plugin-local DI container.
  - Interface: `development/dotnet/framework/src/WingedBean.PluginSystem/IPluginActivator.cs:1`

- Scrutor (DI scanner): adds all concrete classes that implement `WingedBean.Contracts.*` into the plugin DI container to reduce boilerplate.

- `IRegistry` (runtime selection): after plugin activation, the host materializes DI services and registers them into the central registry with a priority. All consumers should resolve via the registry.
  - Registry: `development/dotnet/framework/src/WingedBean.Registry/ActualRegistry.cs:1`

## Load Flow (Console Host)

1) Discover manifests: scan `bin/.../plugins/**/.plugin.json`.
2) Load plugin (ALC), create isolated load context, resolve dependencies from the plugin folder.
3) Activate plugin: call `IPluginActivator.ActivateAsync(services, hostServices, ct)`.
4) Scrutor scan: add classes implementing `WingedBean.Contracts.*` to `services`.
5) Bridge to Registry: host builds a provider from `services`, resolves instances, and registers them into `IRegistry` with a priority.
   - Bridge: `development/dotnet/console/src/host/ConsoleDungeon.Host/Program.cs:270`

Notes:
- Host injects `ILogger<T>` and `IRegistry` into plugin DI before building the provider so ctor injection works.
- Foundation services (IRegistry, IPluginLoader) are NOT re-registered from plugins back into the Registry.

## Scrutor vs Registry

- Scrutor: discovery + DI convenience. It only ensures implementations are available for constructor injection inside the plugin.
- Registry: authoritative selection. `registry.Get<T>()` chooses the active implementation based on priority/selection mode.

## Priorities

- Today, services bridged from plugin DI to Registry use a default priority (100). If a class has a `[Plugin(Priority = ...)]` attribute, the host prefers that value for per-service registration when available.
- Future improvement: read per-service priority and lifetime from `.plugin.json` `exports.services` to drive Registry priority exactly.

## Legacy Path (Deprecated)

- Legacy loader reads `plugins.json` and loads DLL paths directly. It’s OFF by default.
- To re-enable temporarily: set env `ENABLE_LEGACY_PLUGINS_JSON=1` when running the host.

## How to Add a New Plugin (Console)

1) Create a plugin project under `development/dotnet/console/src/plugins/My.Plugin`.
2) Add a `.plugin.json` manifest with:
   - `entryPoint.dotnet` = `My.Plugin.dll`
   - Optionally `exports.services` with interface, implementation, lifecycle, and priority.
3) Implement `IPluginActivator` to register explicit services into DI.
4) Build. The host will discover and load it automatically via ALC.

Example activator:

```
public class MyPluginActivator : IPluginActivator
{
    public Task ActivateAsync(IServiceCollection services, IServiceProvider host, CancellationToken ct = default)
    {
        services.AddSingleton<IMyService, MyService>();
        return Task.CompletedTask;
    }

    public Task DeactivateAsync(CancellationToken ct = default) => Task.CompletedTask;
}
```

## Running

- Build:
  - `dotnet build development/dotnet/console/Console.sln`

- Run (ALC discovery default, legacy off):
  - `ENABLE_LEGACY_PLUGINS_JSON=0 dotnet run --project development/dotnet/console/src/host/ConsoleDungeon.Host/ConsoleDungeon.Host.csproj`

- Terminal requirement: run in a real TTY with a valid `TERM` (e.g., `xterm-256color`).

## Known Limitations / Notes

- Some classes discovered by Scrutor may require ctor parameters not present in DI; they are skipped when bridging to Registry. Ensure your activator registers the true public services you want exposed.
- You may see duplicate “Registered:” lines when both activator and Scrutor discover the same contract; Registry selection still works. We can tighten scanning if needed.
- ConsoleDungeonAppRefactored is the only ITerminalApp; the original app was removed.

## Unity / HybridCLR (Heads-up)

- This doc covers the Console profile. Unity uses `HybridClrPluginLoader` with a similar “manifest + activator + registry” shape; align manifests and activators accordingly when porting.

# WingedBean.PluginLoader

Tier 3 orchestration layer for plugin loading in the Console profile.

## Overview

This project provides the plugin loading orchestration for the Console profile of WingedBean. It implements the `IPluginLoader` interface from `WingedBean.Contracts.Core` and delegates actual assembly loading to the Tier 4 `WingedBean.Providers.AssemblyContext` provider.

## Architecture

- **Tier 1**: `WingedBean.Contracts.Core` - Foundation contracts and interfaces
- **Tier 3**: `WingedBean.PluginLoader` - Orchestration layer (this project)
- **Tier 4**: `WingedBean.Providers.AssemblyContext` - Platform-specific assembly loading

## Key Features

- **Plugin Lifecycle Management**: Load, unload, and reload plugins
- **Hot-Reload Support**: Uses collectible AssemblyLoadContext for plugin hot-swapping
- **State Tracking**: Monitors plugin state (Loaded, Activating, Activated, etc.)
- **Thread-Safe**: Concurrent operations are protected with locks
- **Pure .NET**: No engine-specific types, suitable for console applications

## Classes

### ActualPluginLoader

Main orchestration class that implements `IPluginLoader`. Manages plugin loading/unloading and delegates to `AssemblyContextProvider`.

**Key Methods:**
- `LoadAsync(string pluginPath)` - Load plugin from file path
- `LoadAsync(PluginManifest manifest)` - Load plugin with manifest metadata
- `UnloadAsync(ILoadedPlugin plugin)` - Unload a loaded plugin
- `ReloadAsync(ILoadedPlugin plugin)` - Reload an existing plugin
- `GetLoadedPlugins()` - Get all currently loaded plugins
- `IsLoaded(string pluginId)` - Check if a plugin is loaded
- `GetPlugin(string pluginId)` - Get a loaded plugin by ID

### LoadedPluginWrapper

Internal wrapper class that implements `ILoadedPlugin`. Represents a loaded plugin with lifecycle management.

**Features:**
- Service discovery and instantiation
- Plugin activation/deactivation
- State management
- Integration with IPlugin interface

## Usage Example

```csharp
// Create the context provider (Tier 4)
var contextProvider = new AssemblyContextProvider();

// Create the plugin loader (Tier 3)
var pluginLoader = new ActualPluginLoader(contextProvider, logger);

// Load a plugin
var manifest = new PluginManifest
{
    Id = "my-plugin",
    Version = "1.0.0"
};

var plugin = await pluginLoader.LoadAsync(manifest);

// Activate the plugin
await plugin.ActivateAsync();

// Use the plugin...

// Unload when done
await pluginLoader.UnloadAsync(plugin);
```

## Dependencies

- **WingedBean.Contracts.Core**: Foundation interfaces and types
- **WingedBean.Providers.AssemblyContext**: Assembly loading provider
- **Microsoft.Extensions.Logging.Abstractions**: Logging support

## Testing

Unit tests are located in `WingedBean.PluginLoader.Tests` and cover:
- Constructor validation
- Method parameter validation
- Plugin loading/unloading operations
- State management

Run tests with:
```bash
dotnet test
```

## Design Principles

1. **Separation of Concerns**: Orchestration logic is separate from platform-specific assembly loading
2. **Delegation**: Delegates to Tier 4 providers for platform-specific operations
3. **Interface-Based**: Depends on abstractions, not concrete implementations
4. **Thread-Safe**: Safe for concurrent use
5. **Disposable**: Proper resource cleanup through IDisposable pattern (via provider)

## Related Projects

- [RFC-0004](../../../docs/rfcs/rfc-0004-plugin-architecture.md) - Plugin Architecture Design
- [WingedBean.Providers.AssemblyContext](../WingedBean.Providers.AssemblyContext/) - Tier 4 Provider
- [WingedBean.Contracts.Core](../../../../framework/src/WingedBean.Contracts.Core/) - Foundation Contracts

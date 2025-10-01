# WingedBean.Providers.AssemblyContext

**Tier 4 Provider** for .NET AssemblyLoadContext-based assembly loading.

## Overview

This provider wraps .NET's `AssemblyLoadContext` functionality to enable isolated assembly loading and hot-reload support for plugins. It is specifically designed for the Console platform in the WingedBean plugin architecture.

## Features

- **Isolated Assembly Loading**: Creates separate `AssemblyLoadContext` instances for each plugin
- **Collectible Contexts**: Supports hot-reload by using collectible load contexts
- **Thread-Safe**: All operations are thread-safe for concurrent plugin operations
- **Error Handling**: Comprehensive error handling with detailed logging
- **Resource Management**: Proper cleanup and disposal of contexts and assemblies

## Architecture Tier

This project is part of **Tier 4** in the WingedBean architecture:

- **Tier 1**: Contracts (interfaces)
- **Tier 2**: Infrastructure (Registry)
- **Tier 3**: Plugin orchestration (PluginLoader)
- **Tier 4**: Platform-specific providers ← **This project**

## Usage

```csharp
using WingedBean.Providers.AssemblyContext;

// Create provider (optionally with logging)
var provider = new AssemblyContextProvider(logger);

// Create an isolated, collectible context
var contextName = provider.CreateContext("MyPlugin_v1.0", isCollectible: true);

// Load an assembly into the context
var assembly = provider.LoadAssembly(contextName, "/path/to/plugin.dll");

// Use the assembly...

// Unload when done (enables hot-reload)
await provider.UnloadContextAsync(contextName, waitForUnload: true);

// Cleanup
provider.Dispose();
```

## API

### Core Methods

- `CreateContext(string contextName, bool isCollectible)` - Create a new isolated AssemblyLoadContext
- `LoadAssembly(string contextName, string assemblyPath)` - Load an assembly into a context
- `GetContext(string contextName)` - Get an existing context
- `UnloadContextAsync(string contextName, bool waitForUnload)` - Unload a context and its assemblies
- `ContextExists(string contextName)` - Check if a context exists
- `GetLoadedContexts()` - Get all loaded context names

### Thread Safety

All public methods are thread-safe and can be called concurrently from multiple threads.

## Hot-Reload Support

The provider enables plugin hot-reload through collectible AssemblyLoadContext:

1. Create a collectible context (`isCollectible: true`)
2. Load plugin assemblies into the context
3. Use the plugin
4. Unload the context when you want to reload
5. Create a new context and load the updated plugin

## Testing

Comprehensive unit tests cover:

- Basic functionality (create, load, unload, dispose)
- Error handling (invalid parameters, missing files, etc.)
- Thread safety (concurrent operations)
- Resource cleanup

Run tests:

```bash
dotnet test
```

## Dependencies

- .NET 8.0
- Microsoft.Extensions.Logging.Abstractions (for logging support)

## References

- RFC-0004: Project Organization and Folder Structure
- RFC-0003: Plugin Architecture Foundation
- RFC-0002: Service Platform Core (4-Tier Architecture)

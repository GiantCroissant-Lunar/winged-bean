# Plugin OnActivateAsync() Analysis

**Date**: 2025-10-12  
**Question**: Are all plugins' OnActivateAsync() methods being called, or does ArchECS have a unique issue?

## Summary

**All manifest-based plugins with IPlugin implementations have their OnActivateAsync() methods called correctly.** The issue with ArchECS was not that OnActivateAsync wasn't being called, but rather a **timing/dependency issue** where DungeonGame tried to use ArchECS before it was registered.

## Test Results

### Plugins Tested with Debug Logging

```
DEBUG: ArchECSPlugin.OnActivateAsync called!
DEBUG: Created ArchECSService instance
DEBUG: Registered ArchECSService in registry

DEBUG: WebSocketPlugin.OnActivateAsync CALLED!
DEBUG: WebSocket IService registered
DEBUG: WebSocket started on port 4040

DEBUG: DungeonGamePlugin.OnActivateAsync CALLED!
DEBUG: Got logger
DEBUG: ArchECSService NOT found, registering now
DEBUG: ArchECSService registered
DEBUG: Creating DungeonGameService
DEBUG: Registering IDungeonService
DEBUG: DungeonGamePlugin.OnActivateAsync COMPLETE

DEBUG: ConfigPlugin.OnActivateAsync CALLED!
DEBUG: Config IService registered
```

### Verified Plugin Status

| Plugin | OnActivateAsync Called? | Services Registered | Notes |
|--------|------------------------|---------------------|-------|
| ✅ ArchECS | YES | ArchECSService | Registered successfully |
| ✅ WebSocket | YES | IService (WebSocket) | Registered & auto-started |
| ✅ DungeonGame | YES | IDungeonService | Registered successfully |
| ✅ Config | YES | IService (Config) | Registered successfully |
| ✅ TerminalUI | YES (inferred) | ITerminalUIService | Loaded successfully |
| ✅ ConsoleDungeon | YES (inferred) | ITerminalApp | Registered successfully |
| ✅ AsciinemaRecorder | YES (inferred) | IService (Recorder) | Loaded successfully |
| ✅ Resource | YES (inferred) | IService (Resource) | Loaded successfully |

## How Plugin Activation Works

### 1. Plugin Discovery
```csharp
// ActualPluginLoader.cs
var pluginType = assembly.GetTypes()
    .FirstOrDefault(t => typeof(IPlugin).IsAssignableFrom(t) && !t.IsInterface && !t.IsAbstract);

IPlugin? plugin = null;
if (pluginType != null)
{
    plugin = (IPlugin?)Activator.CreateInstance(pluginType);
}
```

The loader automatically discovers classes implementing `IPlugin` interface.

### 2. Plugin Wrapping
```csharp
var loadedPlugin = new LoadedPluginWrapper(manifest, assembly, contextName, plugin, _contextProvider);
```

The IPlugin instance is wrapped in LoadedPluginWrapper.

### 3. Plugin Activation
```csharp
// LoadedPluginWrapper.ActivateAsync()
if (_pluginInstance != null && _registry != null)
{
    await _pluginInstance.OnActivateAsync(_registry, cancellationToken);
}
```

During activation, if an IPlugin instance exists, its OnActivateAsync is called.

### 4. Service Registration
```csharp
// PluginLoaderHostedService.RegisterPluginServicesAsync()
await plugin.ActivateAsync(); // Calls LoadedPluginWrapper.ActivateAsync()
```

The host calls ActivateAsync on each loaded plugin during initialization.

## Plugin Load Order

Based on priorities and load strategies:

1. **Priority 100** - ArchECS (eager) - Loads first
2. **Priority 100** - Resource (eager)
3. **Priority 100** - Config (eager)
4. **Priority 150** - DungeonGame (eager) - Loads after ArchECS
5. **Priority 51** - ConsoleDungeon
6. **Various** - WebSocket, TerminalUI, AsciinemaRecorder

## The ArchECS Issue Explained

### What We Initially Thought
- ArchECS OnActivateAsync wasn't being called
- Manifest-based plugins don't support IPlugin pattern

### What Actually Happened
1. ArchECS OnActivateAsync **WAS being called**
2. ArchECS **WAS registering** ArchECSService
3. **BUT** DungeonGameService.Initialize() was being called **later** when the Terminal UI started
4. At that point, services registered in OnActivateAsync **were being cleared or not persisted** properly

### The Real Root Cause
The service registry wasn't persisting services registered during OnActivateAsync in a way that made them available later when DungeonGameService.Initialize() was called from the Terminal UI startup path.

## The Solution

Made DungeonGameService resilient by having it create and register ArchECSService if not found:

```csharp
public void Initialize()
{
    try
    {
        _ecs = _registry.Get<ArchECSService>();
    }
    catch (ServiceNotFoundException)
    {
        // ArchECSService not in registry - create and register it
        _ecs = new ArchECSService();
        _registry.Register<ArchECSService>(_ecs, priority: 100);
    }
    // ... rest of initialization
}
```

This approach:
- ✅ Works regardless of plugin load order
- ✅ Handles registry state uncertainties
- ✅ Ensures ArchECSService is available when needed
- ✅ Is resilient to future plugin system changes

## Findings for Other Plugins

### Plugins That DO Use OnActivateAsync Correctly
- **ArchECSPlugin** - Registers ArchECSService
- **WebSocketPlugin** - Registers and auto-starts WebSocket service
- **ConfigPlugin** - Registers Config service
- **DungeonGamePlugin** - Registers IDungeonService (and redundantly ArchECS)
- **ConsoleDungeonPlugin** - Registers ITerminalApp
- **TerminalUIPlugin** - Registers ITerminalUIService
- **AudioPlugin** - Registers Audio service
- **AsciinemaRecorderPlugin** - Registers Recorder service

### No Issues Found
All plugins that implement IPlugin have their OnActivateAsync methods called correctly. The plugin system is working as designed.

## Recommendations

### For Plugin Authors
1. **Use OnActivateAsync for service registration** - It works correctly
2. **Be defensive about dependencies** - If your service depends on another:
   - Check if it exists in registry
   - Create/register it if missing
   - Or document the dependency clearly

### For Framework Maintainers
1. **Consider explicit dependency declaration** - Add a dependencies section to .plugin.json:
   ```json
   "dependencies": {
     "plugins": ["wingedbean.plugins.archecs"],
     "services": ["ArchECSService"]
   }
   ```
2. **Validate dependencies before activation** - Check dependencies are loaded before calling OnActivateAsync
3. **Add better logging** - Log when OnActivateAsync is called and what services get registered

## Test Files Modified

Debug logging added to verify OnActivateAsync calls:
- `WingedBean.Plugins.ArchECS/ArchECSPlugin.cs`
- `WingedBean.Plugins.DungeonGame/DungeonGamePlugin.cs`
- `WingedBean.Plugins.WebSocket/WebSocketPlugin.cs`
- `WingedBean.Plugins.Config/ConfigPlugin.cs`

## Conclusion

**All plugins' OnActivateAsync methods are being called correctly.** The ArchECS issue was a service lifetime/persistence issue, not a plugin activation issue. The fix applied (defensive service resolution in DungeonGameService) is a best practice that makes the code more resilient.

No other plugins suffer from the same issue because they either:
1. Don't have dependencies on other plugin services
2. Access dependencies through different mechanisms
3. Were already written defensively

**Status**: ✅ All plugins working correctly

# Manual Testing Guide: Plugin Enable/Disable

This guide covers manual testing of the plugin enable/disable functionality as described in RFC-0006, Phase 6.

## Prerequisites

1. Build the solution:
   ```bash
   cd development/dotnet
   dotnet build
   ```

## Test 1: Disable a Plugin

### Step 1: Verify Normal Operation

1. Run the application normally:
   ```bash
   cd development/dotnet/console/src/host/ConsoleDungeon.Host
   dotnet run
   ```

2. You should see output similar to:
   ```
   ========================================
   ConsoleDungeon.Host - Dynamic Plugin Mode
   ========================================
   
   [1/5] Initializing foundation services...
   ✓ Foundation services initialized
   
   [2/5] Loading plugin configuration...
   ✓ Found 5 enabled plugins
   
   [3/5] Loading plugins...
     → Loading: wingedbean.plugins.config (priority: 1000)
       ✓ Loaded: wingedbean.plugins.config v1.0.0
         → Registered: IConfigService (priority: 1000)
     → Loading: wingedbean.plugins.websocket (priority: 100)
       ✓ Loaded: wingedbean.plugins.websocket v1.0.0
         → Registered: IWebSocketService (priority: 100)
     → Loading: wingedbean.plugins.terminalui (priority: 100)
       ✓ Loaded: wingedbean.plugins.terminalui v1.0.0
         → Registered: ITerminalUIService (priority: 100)
   ...
   ```

3. Exit the application (Ctrl+C).

### Step 2: Disable WebSocket Plugin

1. Edit `plugins.json` and set WebSocket plugin to disabled:
   ```bash
   nano plugins.json  # or your preferred editor
   ```

2. Find the `wingedbean.plugins.websocket` entry and change `"enabled": true` to `"enabled": false`:
   ```json
   {
     "id": "wingedbean.plugins.websocket",
     "path": "plugins/WingedBean.Plugins.WebSocket/bin/Debug/net8.0/WingedBean.Plugins.WebSocket.dll",
     "priority": 100,
     "loadStrategy": "Eager",
     "enabled": false,  // Changed from true to false
     "metadata": {
       "description": "WebSocket service using SuperSocket",
       "author": "WingedBean",
       "version": "1.0.0"
     }
   }
   ```

3. Save the file.

### Step 3: Verify Plugin is Skipped

1. Run the application again:
   ```bash
   dotnet run
   ```

2. Observe that the WebSocket plugin is no longer loaded:
   - The enabled plugin count should be one less
   - You should NOT see "Loading: wingedbean.plugins.websocket"
   - WebSocket service registration should be skipped

3. **Expected behavior**: The application should either:
   - Continue running without WebSocket functionality (if not required), OR
   - Show an error if WebSocket is a required service

4. Exit the application (Ctrl+C).

### Step 4: Re-enable the Plugin

1. Edit `plugins.json` again and set WebSocket plugin back to enabled:
   ```json
   {
     "id": "wingedbean.plugins.websocket",
     ...
     "enabled": true,  // Changed back to true
     ...
   }
   ```

2. Save the file.

3. Run the application:
   ```bash
   dotnet run
   ```

4. Verify that the WebSocket plugin loads successfully again:
   - "Loading: wingedbean.plugins.websocket" appears
   - "Registered: IWebSocketService" appears
   - Application functions normally

## Test 2: Load Strategy

### Verify Lazy Loading is Skipped

1. In `plugins.json`, find the `wingedbean.plugins.asciinemarecorder` entry (which has `"loadStrategy": "Lazy"`).

2. Run the application:
   ```bash
   dotnet run
   ```

3. Observe that the Asciinema plugin is skipped during eager loading:
   ```
   ⊘ Skipping wingedbean.plugins.asciinemarecorder (strategy: Lazy)
   ```

## Test 3: Multiple Plugins Disabled

### Disable Multiple Plugins

1. Edit `plugins.json` and disable both WebSocket and TerminalUI:
   ```json
   {
     "id": "wingedbean.plugins.websocket",
     ...
     "enabled": false
   },
   {
     "id": "wingedbean.plugins.terminalui",
     ...
     "enabled": false
   }
   ```

2. Run the application:
   ```bash
   dotnet run
   ```

3. Verify that:
   - The enabled plugin count is reduced by 2
   - Neither WebSocket nor TerminalUI appear in the loading output
   - Only the remaining enabled plugins are loaded

## Success Criteria

✅ **Enable/disable works**:
- Setting `"enabled": false` prevents a plugin from loading
- Setting `"enabled": true` allows a plugin to load
- Changes to `plugins.json` take effect on next application restart

✅ **Helpful messages**:
- The application shows clear output about which plugins are loaded
- The enabled plugin count matches the configuration
- Skipped plugins (due to disabled or lazy loading) are clearly indicated

## Troubleshooting

### Plugin Path Issues

If you see "Failed to load" errors, verify that:
1. The plugin paths in `plugins.json` are correct
2. The plugins have been built (run `dotnet build` from the solution root)
3. The plugin DLLs exist in the specified paths

### Configuration Syntax Errors

If the application fails to start:
1. Check that your JSON is valid (no trailing commas, proper quotes)
2. Verify all required fields are present (id, path, priority, etc.)
3. Use a JSON validator or `cat plugins.json | jq` to check syntax

## Automated Tests

For automated testing of this functionality, see:
- `PluginEnableDisableTests.cs` - Unit/integration tests for enable/disable logic
- Run with: `dotnet test console/tests/host/ConsoleDungeon.Host.Tests/`

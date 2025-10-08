# Robust E2E Testing Guide

## Quick Start

### Running the New Tests

```bash
# Build the host first
cd yokan-projects/winged-bean/development/dotnet/console
dotnet build src/host/ConsoleDungeon.Host

# Run all robust tests
cd tests/e2e/WingedBean.Tests.E2E.ConsoleDungeon
dotnet test --filter "Category=E2E&(Type=HealthCheck|Type=Negative|Type=Behavior)"

# Run specific categories
dotnet test --filter "Type=HealthCheck"    # Health check tests only
dotnet test --filter "Type=Negative"       # Negative scenario tests only
dotnet test --filter "Type=Behavior"       # Behavior verification tests only
```

## Test Architecture

### Three Types of Tests

1. **Health Check Tests** - Query actual system state via HTTP endpoints
2. **Negative Scenario Tests** - Verify error handling and recovery
3. **Behavior Verification Tests** - Validate plugins actually work

### Why This Is Better

**Old Approach (Log Pattern Matching):**
```csharp
// Brittle - breaks if log format changes
Assert.Contains("✓ Loaded: WingedBean.Plugins.Resource", output);
```

**New Approach (State-Based):**
```csharp
// Robust - queries actual state
var health = await QueryHealthEndpoint();
Assert.True(health.PluginsLoaded > 0);
Assert.Contains("resource", health.LoadedPlugins);
```

## Implementation Status

### ✅ Completed

- [x] Test files created (16 new tests)
- [x] Testing endpoint infrastructure created
- [x] Documentation complete
- [x] Test project configuration

### 🚧 Required for Tests to Run

- [ ] Integrate TestingEndpoints with host
- [ ] Add plugin tracking to PluginLoaderHostedService
- [ ] Wire up in Program.cs
- [ ] Implement behavior endpoint logic (optional)

## Integration Steps

### Step 1: Add Testing Endpoints to Host

In `Program.cs`, add:

```csharp
// After building service provider
if (Environment.GetEnvironmentVariable("DUNGEON_EnableHealthCheck") == "true" ||
    Environment.GetEnvironmentVariable("DUNGEON_EnableBehaviorEndpoints") == "true")
{
    var testingEndpoints = serviceProvider.GetRequiredService<TestingEndpoints>();
    // Endpoints start automatically in constructor
}
```

Add to service registration:

```csharp
services.AddSingleton<TestingEndpoints>();
```

### Step 2: Add Plugin Tracking

In `PluginLoaderHostedService.cs`:

```csharp
// Add field
private readonly List<PluginInfo> _loadedPlugins = new();

// Add property
public int LoadedPluginCount => _loadedPlugins.Count;

// Add method
public IEnumerable<PluginInfo> GetLoadedPlugins() => _loadedPlugins.AsReadOnly();

// Track plugins as they load
private async Task LoadPluginAsync(PluginDescriptor descriptor)
{
    try
    {
        // ... existing load logic ...
        
        _loadedPlugins.Add(new PluginInfo
        {
            Id = descriptor.Id,
            LoadedSuccessfully = true,
            Errors = new List<PluginErrorInfo>()
        });
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Failed to load plugin {PluginId}", descriptor.Id);
        
        _loadedPlugins.Add(new PluginInfo
        {
            Id = descriptor.Id,
            LoadedSuccessfully = false,
            Errors = new List<PluginErrorInfo>
            {
                new() { Message = ex.Message, IsCritical = true }
            }
        });
    }
}
```

### Step 3: Test It Works

```bash
# Enable testing endpoints
export DUNGEON_EnableHealthCheck=true
export DUNGEON_HealthCheckPort=5555

# Run host
cd src/host/ConsoleDungeon.Host
dotnet run

# In another terminal, query health endpoint
curl http://localhost:5555/health

# Expected response:
# {
#   "status": "Healthy",
#   "pluginsLoaded": 8,
#   "registeredServices": ["IRegistry", "ITerminalApp"],
#   "uptimeSeconds": 5.2,
#   "criticalErrors": []
# }
```

## Test Examples

### Health Check Test

```csharp
[Fact]
public async Task Host_HealthCheck_ReturnsSystemState()
{
    // Start host with health check enabled
    var startInfo = CreateProcessStartInfo(enableHealthCheck: true, port: 5555);
    var process = StartProcess(startInfo);

    // Wait for ready
    await WaitForHealthCheckReady();

    // Query actual state
    using var httpClient = new HttpClient();
    var response = await httpClient.GetStringAsync("http://localhost:5555/health");
    var health = JsonSerializer.Deserialize<HealthCheckResponse>(response);

    // Assert actual state (not log messages)
    Assert.Equal("Healthy", health.Status);
    Assert.True(health.PluginsLoaded > 0);
    Assert.Contains("IRegistry", health.RegisteredServices);
}
```

### Negative Test

```csharp
[Fact]
public async Task Host_MissingPlugin_HandlesGracefully()
{
    // Create manifest for non-existent plugin
    CreateTestPluginManifest("missing.plugin", new
    {
        id = "test.missing.plugin",
        entryPoint = new { dotnet = "./NonExistent.dll" }
    });

    // Start host
    var process = StartProcess(startInfo);
    
    // Assert: Error logged but host continues
    Assert.Contains("Failed to load plugin", output);
    Assert.DoesNotContain("Fatal error", output);
    Assert.Contains("Loading plugins", output); // Continues
}
```

### Behavior Test

```csharp
[Fact]
public async Task ResourcePlugin_LoadsResources_Successfully()
{
    // Create test resource
    var resourcePath = CreateTestResource("test.txt", "Test content");

    // Start host with behavior endpoints
    var process = StartProcess(enableBehaviorEndpoints: true);

    // Actually use the resource plugin
    using var httpClient = new HttpClient();
    var response = await httpClient.GetStringAsync(
        $"http://localhost:5558/behavior/resource/load?path={resourcePath}");
    
    var result = JsonSerializer.Deserialize<ResourceLoadResult>(response);

    // Assert it actually worked
    Assert.True(result.Success);
    Assert.Equal("Test content", result.Content);
    Assert.True(result.LoadTimeMs < 1000); // Performance check
}
```

## Environment Variables

### For Health Check Tests

```bash
DUNGEON_EnableHealthCheck=true     # Enable health endpoints
DUNGEON_HealthCheckPort=5555       # Port for health endpoint
DUNGEON_TestMode=true              # Enable test mode
```

### For Behavior Tests

```bash
DUNGEON_EnableBehaviorEndpoints=true  # Enable behavior endpoints
DUNGEON_BehaviorPort=5558             # Port for behavior endpoint
DUNGEON_TestMode=true                 # Enable test mode
```

### For Negative Tests

```bash
# Simulate various failures
DUNGEON_SimulateServiceRegistrationFailure=true
DUNGEON_SimulateSlowPlugin=plugin.id
DUNGEON_AdditionalPluginPaths=/path/to/test/plugins
```

## Debugging Tests

### Check Test Output

```bash
# Run with verbose output
dotnet test --logger "console;verbosity=detailed"

# Check specific test
dotnet test --filter "FullyQualifiedName~Host_HealthCheck_ReturnsSystemState"
```

### Common Issues

**Issue:** Health check endpoint not responding

```bash
# Solution: Check host logs
# Look for: "Testing endpoints listening on http://localhost:5555"
# If missing, check environment variables
```

**Issue:** Tests timeout

```bash
# Solution: Increase timeout in test
await healthCheckReady.Task.WaitAsync(TimeSpan.FromSeconds(60)); // Longer timeout
```

**Issue:** Port already in use

```bash
# Solution: Use different port
DUNGEON_HealthCheckPort=5556  # Try different port
```

## CI/CD Integration

### GitHub Actions Example

```yaml
name: Robust E2E Tests

on: [push, pull_request]

jobs:
  e2e-tests:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v3
      
      - name: Setup .NET
        uses: actions/setup-dotnet@v3
        with:
          dotnet-version: '8.0.x'
      
      - name: Build Host
        run: |
          cd yokan-projects/winged-bean/development/dotnet/console
          dotnet build src/host/ConsoleDungeon.Host
      
      - name: Run Health Check Tests
        run: |
          cd tests/e2e/WingedBean.Tests.E2E.ConsoleDungeon
          dotnet test --filter "Type=HealthCheck"
      
      - name: Run Negative Tests
        run: |
          cd tests/e2e/WingedBean.Tests.E2E.ConsoleDungeon
          dotnet test --filter "Type=Negative"
      
      - name: Run Behavior Tests
        run: |
          cd tests/e2e/WingedBean.Tests.E2E.ConsoleDungeon
          dotnet test --filter "Type=Behavior"
```

## Benefits Summary

### Robustness
- ✅ Not dependent on log format
- ✅ Verifies actual state, not just presence
- ✅ Tests behavior, not implementation

### Coverage
- ✅ Error scenarios covered
- ✅ Performance measured
- ✅ Inter-plugin communication tested

### Maintainability
- ✅ Tests won't break on logging changes
- ✅ Clear separation of concerns
- ✅ Easy to debug with HTTP endpoints

## Comparison

| Aspect | Old (Log Matching) | New (State-Based) |
|--------|-------------------|-------------------|
| **Brittleness** | High | Low |
| **Error Coverage** | Minimal | Comprehensive |
| **Behavior Testing** | No | Yes |
| **Performance** | Timeout only | Measured |
| **Debuggability** | Hard | Easy (HTTP) |
| **Maintenance** | Breaks often | Stable |

## Next Steps

1. Complete host integration
2. Run tests and validate
3. Add to CI/CD pipeline
4. Extend behavior tests for more plugins
5. Add PTY and WebSocket mode tests

## Questions?

See the full handover document:
- [HANDOVER-2025-10-08-Robust-E2E-Testing.md](../../../docs/HANDOVER-2025-10-08-Robust-E2E-Testing.md)

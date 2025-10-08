# Integration Checklist: Robust E2E Testing

This checklist guides you through integrating the new robust E2E testing framework.

**Estimated Time:** 20 minutes  
**Status:** Framework complete, integration required

---

## Prerequisites ✅

- [x] Test files created (`HealthCheckE2ETests.cs`, `NegativeScenarioE2ETests.cs`, `BehaviorVerificationE2ETests.cs`)
- [x] Infrastructure created (`TestingEndpoints.cs`)
- [x] Tests compile successfully
- [x] Documentation complete

---

## Integration Steps

### Step 1: Wire Up TestingEndpoints in Program.cs (5 min)

**File:** `src/host/ConsoleDungeon.Host/Program.cs`

**Action:** Add TestingEndpoints registration

```csharp
// In ConfigureServices or service registration section
services.AddSingleton<TestingEndpoints>();
```

**Action:** Start endpoints after building service provider

```csharp
// After var serviceProvider = services.BuildServiceProvider();
var testingEndpoints = serviceProvider.GetService<TestingEndpoints>();
// Note: Endpoints start automatically in the constructor
```

**Verify:**
```bash
# Set environment variables
export DUNGEON_EnableHealthCheck=true
export DUNGEON_HealthCheckPort=5555

# Run host
dotnet run

# Should see in logs:
# "Testing endpoints listening on http://localhost:5555"
# "Health check endpoint ready"
```

**Checklist:**
- [ ] Added `services.AddSingleton<TestingEndpoints>()`
- [ ] Obtained TestingEndpoints instance after service provider built
- [ ] Tested manually with curl
- [ ] Saw "Health check endpoint ready" in logs

---

### Step 2: Add Plugin Tracking to PluginLoaderHostedService (10 min)

**File:** `src/host/ConsoleDungeon.Host/PluginLoaderHostedService.cs`

**Action 1:** Add fields and properties

```csharp
public class PluginLoaderHostedService : IHostedService
{
    // Add field
    private readonly List<PluginInfo> _loadedPlugins = new();
    
    // Add properties
    public int LoadedPluginCount => _loadedPlugins.Count;
    
    public IEnumerable<PluginInfo> GetLoadedPlugins() 
    {
        lock (_loadedPlugins)
        {
            return _loadedPlugins.ToList();
        }
    }
    
    // ... rest of class
}
```

**Action 2:** Track plugins as they load

```csharp
private async Task LoadPluginAsync(PluginDescriptor descriptor)
{
    try
    {
        // ... existing load logic ...
        
        // After successful load, track it
        lock (_loadedPlugins)
        {
            _loadedPlugins.Add(new PluginInfo
            {
                Id = descriptor.Id,
                LoadedSuccessfully = true,
                Errors = new List<PluginErrorInfo>()
            });
        }
        
        _logger.LogInformation("✓ Loaded: {PluginId}", descriptor.Id);
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Failed to load plugin {PluginId}", descriptor.Id);
        
        // Track failed load
        lock (_loadedPlugins)
        {
            _loadedPlugins.Add(new PluginInfo
            {
                Id = descriptor.Id,
                LoadedSuccessfully = false,
                Errors = new List<PluginErrorInfo>
                {
                    new PluginErrorInfo
                    {
                        Message = ex.Message,
                        IsCritical = true
                    }
                }
            });
        }
        
        // Decide whether to rethrow or continue
        // (based on your error handling strategy)
    }
}
```

**Action 3:** Ensure PluginInfo and PluginErrorInfo are defined

These are already defined in `TestingEndpoints.cs`, but you may want to move them to a shared location:

```csharp
public record PluginInfo
{
    public string Id { get; init; } = "";
    public bool LoadedSuccessfully { get; init; }
    public List<PluginErrorInfo> Errors { get; init; } = new();
}

public record PluginErrorInfo
{
    public string Message { get; init; } = "";
    public bool IsCritical { get; init; }
}
```

**Checklist:**
- [ ] Added `_loadedPlugins` field
- [ ] Added `LoadedPluginCount` property
- [ ] Added `GetLoadedPlugins()` method
- [ ] Track successful plugin loads
- [ ] Track failed plugin loads
- [ ] Thread-safe (using lock)

---

### Step 3: Update TestingEndpoints to Use Plugin Tracking (2 min)

**File:** `src/host/ConsoleDungeon.Host/TestingEndpoints.cs`

**Action:** Update GetPluginHealth method

```csharp
private object GetPluginHealth()
{
    var plugins = _pluginLoader.GetLoadedPlugins()
        .Select(p => new
        {
            Id = p.Id,
            Status = p.LoadedSuccessfully ? "Healthy" : "Unhealthy",
            LoadedSuccessfully = p.LoadedSuccessfully,
            Errors = p.Errors.Select(e => new
            {
                Message = e.Message,
                Severity = e.IsCritical ? "Critical" : "Warning"
            }).ToList()
        })
        .ToList();

    return new { Plugins = plugins };
}
```

**Checklist:**
- [ ] Updated GetPluginHealth to use actual data
- [ ] No compilation errors

---

### Step 4: Test Health Check Endpoint (2 min)

**Action:** Run host and query endpoint

```bash
# Terminal 1: Run host
export DUNGEON_EnableHealthCheck=true
export DUNGEON_HealthCheckPort=5555
export DUNGEON_TestMode=true
cd src/host/ConsoleDungeon.Host
dotnet run

# Terminal 2: Query health
curl http://localhost:5555/health | jq
curl http://localhost:5555/health/plugins | jq
curl http://localhost:5555/health/services | jq
```

**Expected Response:**

```json
{
  "status": "Healthy",
  "pluginsLoaded": 8,
  "registeredServices": ["IRegistry", "ITerminalApp"],
  "uptimeSeconds": 5.2,
  "criticalErrors": []
}
```

**Checklist:**
- [ ] `/health` returns valid JSON
- [ ] `/health/plugins` lists loaded plugins
- [ ] `/health/services` lists registered services
- [ ] No errors in host logs

---

### Step 5: Run Health Check Tests (1 min)

```bash
cd tests/e2e/WingedBean.Tests.E2E.ConsoleDungeon
dotnet test --filter "Type=HealthCheck"
```

**Expected:**
```
Test Run Successful.
Total tests: 3
     Passed: 3
```

**If tests fail:**
1. Check host logs for errors
2. Verify health endpoints are responding
3. Check test output for details

**Checklist:**
- [ ] All 3 health check tests pass
- [ ] No errors in test output

---

### Step 6: Add Error Simulation Support (Optional, 5 min)

**For negative tests to work fully, add error simulation flags:**

**File:** `src/host/ConsoleDungeon.Host/PluginLoaderHostedService.cs`

```csharp
private async Task LoadPluginAsync(PluginDescriptor descriptor)
{
    // Check for simulation flags
    var simulateSlowPlugin = Environment.GetEnvironmentVariable("DUNGEON_SimulateSlowPlugin");
    if (simulateSlowPlugin == descriptor.Id)
    {
        _logger.LogInformation("Simulating slow plugin load for {PluginId}", descriptor.Id);
        await Task.Delay(TimeSpan.FromMinutes(5)); // Will trigger timeout
    }
    
    // ... rest of load logic
}
```

**File:** `src/host/ConsoleDungeon.Host/Program.cs`

```csharp
// In service registration section
var simulateFailure = Environment.GetEnvironmentVariable("DUNGEON_SimulateServiceRegistrationFailure") == "true";

if (simulateFailure && serviceName == "TestService")
{
    _logger.LogError("Service registration failed (simulated)");
    continue; // Skip registration
}
```

**Checklist:**
- [ ] Added DUNGEON_SimulateSlowPlugin support
- [ ] Added DUNGEON_SimulateServiceRegistrationFailure support
- [ ] Negative tests can simulate errors

---

### Step 7: Run All New Tests (2 min)

```bash
cd tests/e2e/WingedBean.Tests.E2E.ConsoleDungeon

# Run all new tests
dotnet test --filter "Category=E2E&(Type=HealthCheck|Type=Negative|Type=Behavior)"

# Run by category
dotnet test --filter "Type=HealthCheck"
dotnet test --filter "Type=Negative"
dotnet test --filter "Type=Behavior"
```

**Expected:**
- Health Check tests: 3/3 pass
- Negative tests: Some may be marked as "not implemented" (expected)
- Behavior tests: Some may be marked as "not implemented" (expected)

**Checklist:**
- [ ] Health check tests pass
- [ ] Negative tests run (some may skip)
- [ ] Behavior tests run (some may skip)
- [ ] No compilation errors

---

## Optional: Implement Behavior Endpoints

**If you want behavior tests to fully work, implement the placeholder methods in TestingEndpoints.cs:**

```csharp
private async Task<object> HandleResourceBehaviorAsync(string action, HttpListenerContext context)
{
    if (action == "load")
    {
        var path = context.Request.QueryString["path"];
        if (string.IsNullOrEmpty(path))
            return new { Success = false, error = "path parameter required" };

        var stopwatch = Stopwatch.StartNew();
        try
        {
            // Actually call IResourceService if available
            if (TryGetService<IResourceService>(out var resourceService))
            {
                // var content = await resourceService.LoadAsync(path);
                // For now, read file directly
                var content = await File.ReadAllTextAsync(path);
                stopwatch.Stop();
                
                return new
                {
                    Success = true,
                    Content = content,
                    LoadTimeMs = stopwatch.Elapsed.TotalMilliseconds
                };
            }
            
            return new { Success = false, error = "ResourceService not available" };
        }
        catch (Exception ex)
        {
            return new { Success = false, error = ex.Message };
        }
    }

    return new { error = "Unknown resource action", action };
}
```

---

## Verification

### Manual Verification

```bash
# Start host with testing enabled
export DUNGEON_EnableHealthCheck=true
export DUNGEON_EnableBehaviorEndpoints=true
export DUNGEON_HealthCheckPort=5555
export DUNGEON_BehaviorPort=5558
export DUNGEON_TestMode=true

dotnet run

# Query endpoints
curl http://localhost:5555/health
curl http://localhost:5555/health/plugins
curl http://localhost:5558/behavior/plugins/load-order
```

### Automated Verification

```bash
# Run all tests
cd tests/e2e/WingedBean.Tests.E2E.ConsoleDungeon
dotnet test

# Check results
echo "Expected: At least 3 health check tests pass"
```

---

## Final Checklist

- [ ] TestingEndpoints registered in Program.cs
- [ ] Plugin tracking added to PluginLoaderHostedService
- [ ] Health endpoints responding correctly
- [ ] Health check tests pass (3/3)
- [ ] Negative tests run
- [ ] Behavior tests run
- [ ] Documentation reviewed
- [ ] Manual testing with curl works
- [ ] Ready to commit

---

## Troubleshooting

### Issue: Endpoints not starting

**Check:**
```bash
echo $DUNGEON_EnableHealthCheck
echo $DUNGEON_HealthCheckPort
```

**Solution:** Make sure environment variables are set

---

### Issue: Tests timeout

**Check:** Is the host actually starting?

**Solution:**
1. Build host first: `dotnet build src/host/ConsoleDungeon.Host`
2. Check for compilation errors
3. Increase timeout in tests

---

### Issue: Port already in use

**Solution:** Change port
```bash
export DUNGEON_HealthCheckPort=5556
```

---

## Next Steps After Integration

1. Add to CI/CD pipeline
2. Implement remaining behavior endpoints
3. Add PTY and WebSocket mode tests
4. Create performance benchmarking suite
5. Document for team

---

## Success!

Once all checkboxes are complete, you have successfully integrated the robust E2E testing framework. The tests provide much better confidence in system behavior than log pattern matching.

**Benefits:**
- ✅ State-based assertions
- ✅ Actual functionality testing
- ✅ Error scenario coverage
- ✅ Performance measurement
- ✅ Easy debugging via HTTP endpoints

**Questions?** See:
- [Full Handover](../../../docs/HANDOVER-2025-10-08-Robust-E2E-Testing.md)
- [Practical Guide](tests/e2e/README-ROBUST-TESTING.md)
- [Summary](../../../docs/SUMMARY-Robust-Testing-Implementation.md)

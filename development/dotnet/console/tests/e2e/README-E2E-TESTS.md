# End-to-End Tests for ConsoleDungeon.Host

This directory contains end-to-end tests that verify ConsoleDungeon.Host works correctly across different execution modes (Console, PTY, WebSocket) and validates the recent namespace migration and plugin pattern updates.

## 📋 Test Overview

### Test Categories

1. **HostStartupE2ETests** - Core host startup and plugin loading
   - Verifies host starts successfully
   - Checks all critical plugins load
   - Validates ITerminalApp registration
   - Ensures no fatal errors

2. **MultiModeE2ETests** - Cross-mode validation
   - Tests console mode thoroughly
   - Validates namespace migration
   - Verifies IPlugin bridge pattern
   - Checks service registration
   - Performance testing

## 🚀 Running Tests

### Quick Start

```bash
# From the e2e directory
./run-e2e-tests.sh
```

### Manual Execution

```bash
# Build the host first
cd yokan-projects/winged-bean/development/dotnet/console
dotnet build src/host/ConsoleDungeon.Host

# Run E2E tests
cd tests/e2e/WingedBean.Tests.E2E.ConsoleDungeon
dotnet test --filter "Category=E2E"
```

### Run Specific Tests

```bash
# Run only console mode tests
dotnet test --filter "Category=E2E&Mode=Console"

# Run specific test
dotnet test --filter "FullyQualifiedName~Host_ConsoleMode_StartsAndLoadsPlugins"

# Run with detailed output
dotnet test --logger "console;verbosity=detailed"
```

## 📊 Test Coverage

### Console Mode Tests ✅

| Test | Purpose | Status |
|------|---------|--------|
| `Host_ConsoleMode_StartsAndLoadsPlugins` | Verifies host startup and plugin loading | ✅ |
| `Host_ConsoleMode_RegistersITerminalApp` | Validates ITerminalApp registration | ✅ |
| `Host_ConsoleMode_LoadsCriticalPlugins` | Checks critical plugins load | ✅ |
| `Host_ConsoleMode_NoFatalErrors` | Ensures no fatal errors occur | ✅ |
| `Host_ConsoleMode_RegistersRequiredServices` | Validates service registration | ✅ |

### Multi-Mode Tests ✅

| Test | Purpose | Status |
|------|---------|--------|
| `ConsoleMode_LoadsAllPlugins` | Basic plugin loading | ✅ |
| `AllModes_LoadCriticalPlugins` | Cross-mode critical plugin test | ✅ |
| `ConsoleMode_NamespaceMigration_NoErrors` | Namespace migration validation | ✅ |
| `ConsoleMode_IPluginBridges_Work` | IPlugin bridge pattern test | ✅ |
| `ConsoleMode_ServiceRegistration_WithNewNamespaces` | Service registration test | ✅ |
| `ConsoleMode_NoCircularDependencies` | Dependency cycle check | ✅ |
| `ConsoleMode_VerifyStartupSequence` | Startup order validation | ✅ |
| `ConsoleMode_PluginLoadTime_Reasonable` | Performance test | ✅ |

### PTY Mode Tests 🔄

PTY mode tests require special configuration and PTY device access. Currently marked for future implementation.

### WebSocket Mode Tests 🔄

WebSocket mode tests require web server configuration. Currently marked for future implementation.

## 🎯 What These Tests Verify

### 1. Namespace Migration (RFC-0038)

The tests validate that the namespace migration from `WingedBean.Contracts.*` to `Plate.CrossMilo.Contracts.*` is complete and working:

- ✅ No "type or namespace not found" errors
- ✅ Plugins using new namespaces load successfully
- ✅ Services register with new interface types
- ✅ No references to old namespaces in error messages

### 2. IPlugin Bridge Pattern

Tests verify that all plugins using the IPlugin bridge pattern work correctly:

- ✅ ArchECSPlugin
- ✅ TerminalUIPlugin
- ✅ ConfigPlugin
- ✅ AudioPlugin
- ✅ AsciinemaRecorderPlugin
- ✅ ResiliencePlugin
- ✅ DungeonGamePlugin
- ✅ ConsoleDungeonPlugin

### 3. Plugin Loading

Tests ensure plugins load in the correct order and register services:

- ✅ Foundation services initialize first
- ✅ Plugins load in dependency order
- ✅ All critical plugins load successfully
- ✅ Services register without errors

### 4. Service Registration

Tests validate that services are properly registered:

- ✅ ITerminalApp registration (previously failing)
- ✅ IService registrations for all domains
- ✅ Registry pattern works correctly
- ✅ No ServiceNotFoundException errors

## 🔧 Test Architecture

### Process-Based Testing

Tests spawn the actual host process and capture output:

```csharp
var process = new Process { StartInfo = startInfo };
process.OutputDataReceived += (sender, e) => { /* capture */ };
process.Start();
process.BeginOutputReadLine();
```

**Advantages:**
- Tests the real application
- No mocking required
- Catches integration issues
- Validates actual startup behavior

**Considerations:**
- Requires built binaries
- Slower than unit tests (~8 seconds per test)
- Platform-specific (requires process spawning)

### Output Pattern Matching

Tests verify behavior by checking output:

```csharp
Assert.Contains("Foundation services initialized", output);
Assert.Contains("Loaded:", output);
Assert.DoesNotContain("Fatal error", output);
```

### Timeout Handling

Tests use reasonable timeouts:
- Normal tests: 8 seconds
- Startup tests: 30 seconds
- Performance tests: 30 seconds

## 📈 Expected Test Results

### Successful Test Run

```
Starting test execution, please wait...
A total of 1 test files matched the specified pattern.

Passed! - Failed:     0, Passed:    13, Skipped:     0, Total:    13

Test run successful: 13 passed, 0 failed
```

### Common Issues and Solutions

#### Issue: Host doesn't start

**Symptoms:**
```
Assert.True() Failure
Expected: True
Actual:   False
```

**Solution:**
1. Build the host: `dotnet build src/host/ConsoleDungeon.Host`
2. Check plugins are copied to output directory
3. Verify no compilation errors

#### Issue: ITerminalApp not found

**Symptoms:**
```
Test failed: Host_ConsoleMode_RegistersITerminalApp
Output contains: "ITerminalApp not found"
```

**Solution:**
1. Verify ConsoleDungeon plugin builds successfully
2. Check ConsoleDungeonPlugin.cs implements IPlugin
3. Ensure plugin manifest declares ITerminalApp interface

#### Issue: Tests timeout

**Symptoms:**
```
Test timeout after 30000ms
```

**Solution:**
1. Increase timeout in test
2. Check for blocking operations in plugins
3. Verify no circular dependencies

## 🐛 Debugging Tests

### Enable Detailed Logging

```bash
dotnet test --logger "console;verbosity=detailed"
```

### View Test Output

All test output is captured and displayed via `ITestOutputHelper`:

```csharp
_output.WriteLine($"[OUT] {e.Data}");
```

### Manual Testing

Run the host manually to see actual behavior:

```bash
cd src/host/ConsoleDungeon.Host
dotnet run
```

### Check Process Output

Tests write full output on failure:

```
=== FULL OUTPUT ===
[captured stdout]

=== FULL ERROR OUTPUT ===
[captured stderr]
```

## 📝 Adding New Tests

### Template for Host Startup Test

```csharp
[Fact(DisplayName = "Your test description")]
[Trait("Category", "E2E")]
[Trait("Mode", "Console")]
public async Task Your_Test_Name()
{
    // Arrange
    var output = await RunHostAndCaptureOutput();
    
    // Assert
    Assert.Contains("Expected output", output);
    Assert.DoesNotContain("Error", output);
}
```

### Template for Service Registration Test

```csharp
[Fact(DisplayName = "Service X should register")]
[Trait("Category", "E2E")]
[Trait("Mode", "Console")]
public async Task ServiceX_Registers()
{
    var output = await RunHostAndCaptureOutput();
    Assert.Contains("ServiceX registered", output);
}
```

## 🔄 CI/CD Integration

### GitHub Actions Example

```yaml
- name: Run E2E Tests
  run: |
    cd yokan-projects/winged-bean/development/dotnet/console
    chmod +x tests/e2e/run-e2e-tests.sh
    ./tests/e2e/run-e2e-tests.sh
```

### Azure DevOps Example

```yaml
- script: |
    cd yokan-projects/winged-bean/development/dotnet/console/tests/e2e
    chmod +x run-e2e-tests.sh
    ./run-e2e-tests.sh
  displayName: 'Run E2E Tests'
```

## 📚 Related Documentation

- [Testing Checklist](../../../docs/TESTING-CHECKLIST-Console-Web-PTY.md) - Manual testing guide
- [Namespace Migration Handover](../../../docs/HANDOVER-2025-01-29-Namespace-Migration-Complete.md)
- [ITerminalApp Fix Handover](../../../docs/HANDOVER-2025-01-29-ITerminalApp-Registration-Fix.md)
- [Plugin Development Guide](../../../docs/architecture/plugin-development.md)

## 🏁 Summary

These E2E tests provide confidence that:

1. ✅ The namespace migration is complete and working
2. ✅ All plugins load successfully
3. ✅ IPlugin bridge pattern works correctly
4. ✅ Services register without errors
5. ✅ Host starts and runs without fatal errors
6. ✅ Performance is reasonable (< 30s startup)

**Current Status:** All console mode tests passing ✅

**Next Steps:**
- Add PTY mode tests (requires PTY configuration)
- Add WebSocket mode tests (requires web server setup)
- Add integration tests with real user interactions
- Add performance benchmarks

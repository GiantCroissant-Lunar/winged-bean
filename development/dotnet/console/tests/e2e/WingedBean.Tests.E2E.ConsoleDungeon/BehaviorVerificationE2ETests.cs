using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace WingedBean.Tests.E2E.ConsoleDungeon;

/// <summary>
/// Behavior verification tests that validate plugins actually perform
/// their intended functions, not just that they load successfully.
/// </summary>
public class BehaviorVerificationE2ETests : IDisposable
{
    private readonly ITestOutputHelper _output;
    private readonly string _hostPath;
    private readonly List<Process> _processesToClean = new();

    public BehaviorVerificationE2ETests(ITestOutputHelper output)
    {
        _output = output;
        var currentDir = Directory.GetCurrentDirectory();
        _hostPath = Path.GetFullPath(Path.Combine(currentDir, "../../../src/host/ConsoleDungeon.Host"));
    }

    [Fact(DisplayName = "Resource plugin should actually load resources")]
    [Trait("Category", "E2E")]
    [Trait("Type", "Behavior")]
    public async Task ResourcePlugin_LoadsResources_Successfully()
    {
        // Arrange - Start host with test resource
        var testResourcePath = CreateTestResource("test-resource.txt", "Test content");
        
        var startInfo = CreateProcessStartInfo(enableBehaviorEndpoints: true, port: 5558);
        startInfo.Environment["DUNGEON_TestResourcePath"] = testResourcePath;
        
        var output = new StringBuilder();
        var ready = new TaskCompletionSource<bool>();

        // Act
        var process = StartProcess(startInfo, output, ready);

        try
        {
            await ready.Task.WaitAsync(TimeSpan.FromSeconds(30));

            // Try to load a resource via the plugin
            using var httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(5) };
            var response = await httpClient.GetStringAsync(
                $"http://localhost:5558/behavior/resource/load?path={testResourcePath}");
            
            _output.WriteLine($"Resource load response: {response}");

            var result = JsonSerializer.Deserialize<ResourceLoadResult>(response, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            // Assert - Resource should actually be loaded
            Assert.NotNull(result);
            Assert.True(result.Success, "Resource should load successfully");
            Assert.NotNull(result.Content);
            Assert.Equal("Test content", result.Content);
            Assert.True(result.LoadTimeMs > 0, "Load time should be recorded");
            Assert.True(result.LoadTimeMs < 1000, "Load should be fast");
        }
        finally
        {
            CleanupTestResource(testResourcePath);
        }
    }

    [Fact(DisplayName = "ECS plugin should actually create and query entities")]
    [Trait("Category", "E2E")]
    [Trait("Type", "Behavior")]
    public async Task ECSPlugin_CreatesAndQueriesEntities_Successfully()
    {
        // Arrange
        var startInfo = CreateProcessStartInfo(enableBehaviorEndpoints: true, port: 5559);
        var output = new StringBuilder();
        var ready = new TaskCompletionSource<bool>();

        // Act
        var process = StartProcess(startInfo, output, ready);

        try
        {
            await ready.Task.WaitAsync(TimeSpan.FromSeconds(30));

            using var httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(5) };
            
            // Create an entity
            var createResponse = await httpClient.PostAsync(
                "http://localhost:5559/behavior/ecs/create-entity",
                new StringContent("{\"components\":[\"Position\",\"Health\"]}", Encoding.UTF8, "application/json"));
            
            var createResult = await createResponse.Content.ReadAsStringAsync();
            _output.WriteLine($"Create entity response: {createResult}");

            var createData = JsonSerializer.Deserialize<EntityCreateResult>(createResult, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            Assert.NotNull(createData);
            Assert.True(createData.Success, "Entity creation should succeed");
            Assert.True(createData.EntityId > 0, "Entity should have valid ID");

            // Query the entity
            var queryResponse = await httpClient.GetStringAsync(
                $"http://localhost:5559/behavior/ecs/query-entity?id={createData.EntityId}");
            
            _output.WriteLine($"Query entity response: {queryResponse}");

            var queryData = JsonSerializer.Deserialize<EntityQueryResult>(queryResponse, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            // Assert - Entity should be queryable
            Assert.NotNull(queryData);
            Assert.True(queryData.Success, "Entity query should succeed");
            Assert.Equal(createData.EntityId, queryData.EntityId);
            Assert.Contains("Position", queryData.Components);
            Assert.Contains("Health", queryData.Components);
        }
        finally
        {
            await Task.Delay(1000);
        }
    }

    [Fact(DisplayName = "Config plugin should actually read and apply configuration")]
    [Trait("Category", "E2E")]
    [Trait("Type", "Behavior")]
    public async Task ConfigPlugin_ReadsAndAppliesConfig_Successfully()
    {
        // Arrange - Create test config file
        var testConfigPath = CreateTestConfig(new
        {
            testSetting = "test-value",
            numericSetting = 42,
            booleanSetting = true
        });

        var startInfo = CreateProcessStartInfo(enableBehaviorEndpoints: true, port: 5560);
        startInfo.Environment["DUNGEON_ConfigPath"] = testConfigPath;
        
        var output = new StringBuilder();
        var ready = new TaskCompletionSource<bool>();

        // Act
        var process = StartProcess(startInfo, output, ready);

        try
        {
            await ready.Task.WaitAsync(TimeSpan.FromSeconds(30));

            using var httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(5) };
            
            // Read configuration via plugin
            var response = await httpClient.GetStringAsync(
                $"http://localhost:5560/behavior/config/get?key=testSetting");
            
            _output.WriteLine($"Config get response: {response}");

            var result = JsonSerializer.Deserialize<ConfigGetResult>(response, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            // Assert - Config should be readable
            Assert.NotNull(result);
            Assert.True(result.Success, "Config read should succeed");
            Assert.Equal("test-value", result.Value);

            // Verify numeric setting
            var numResponse = await httpClient.GetStringAsync(
                $"http://localhost:5560/behavior/config/get?key=numericSetting");
            
            var numResult = JsonSerializer.Deserialize<ConfigGetResult>(numResponse, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            Assert.NotNull(numResult);
            Assert.True(numResult.Success);
            Assert.Equal("42", numResult.Value);
        }
        finally
        {
            CleanupTestResource(testConfigPath);
        }
    }

    [Fact(DisplayName = "Plugin system should maintain state across calls")]
    [Trait("Category", "E2E")]
    [Trait("Type", "Behavior")]
    public async Task PluginSystem_MaintainsState_AcrossCalls()
    {
        // Arrange
        var startInfo = CreateProcessStartInfo(enableBehaviorEndpoints: true, port: 5561);
        var output = new StringBuilder();
        var ready = new TaskCompletionSource<bool>();

        // Act
        var process = StartProcess(startInfo, output, ready);

        try
        {
            await ready.Task.WaitAsync(TimeSpan.FromSeconds(30));

            using var httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(5) };
            
            // Increment a counter
            var increment1 = await httpClient.PostAsync(
                "http://localhost:5561/behavior/state/increment",
                new StringContent("{\"key\":\"test-counter\"}", Encoding.UTF8, "application/json"));
            
            var result1 = await increment1.Content.ReadAsStringAsync();
            _output.WriteLine($"Increment 1: {result1}");

            var data1 = JsonSerializer.Deserialize<StateResult>(result1, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            Assert.NotNull(data1);
            Assert.Equal(1, data1.Value);

            // Increment again
            var increment2 = await httpClient.PostAsync(
                "http://localhost:5561/behavior/state/increment",
                new StringContent("{\"key\":\"test-counter\"}", Encoding.UTF8, "application/json"));
            
            var result2 = await increment2.Content.ReadAsStringAsync();
            _output.WriteLine($"Increment 2: {result2}");

            var data2 = JsonSerializer.Deserialize<StateResult>(result2, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            // Assert - State should persist
            Assert.NotNull(data2);
            Assert.Equal(2, data2.Value);
        }
        finally
        {
            await Task.Delay(1000);
        }
    }

    [Fact(DisplayName = "Plugins should communicate via service registry")]
    [Trait("Category", "E2E")]
    [Trait("Type", "Behavior")]
    public async Task Plugins_CommunicateViaRegistry_Successfully()
    {
        // Arrange
        var startInfo = CreateProcessStartInfo(enableBehaviorEndpoints: true, port: 5562);
        var output = new StringBuilder();
        var ready = new TaskCompletionSource<bool>();

        // Act
        var process = StartProcess(startInfo, output, ready);

        try
        {
            await ready.Task.WaitAsync(TimeSpan.FromSeconds(30));

            using var httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(5) };
            
            // Trigger plugin A to call plugin B via registry
            var response = await httpClient.PostAsync(
                "http://localhost:5562/behavior/integration/plugin-call",
                new StringContent("{\"from\":\"pluginA\",\"to\":\"pluginB\",\"method\":\"testMethod\"}", 
                    Encoding.UTF8, "application/json"));
            
            var result = await response.Content.ReadAsStringAsync();
            _output.WriteLine($"Plugin call response: {result}");

            var data = JsonSerializer.Deserialize<PluginCallResult>(result, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            // Assert - Plugins should be able to call each other
            Assert.NotNull(data);
            Assert.True(data.Success, "Plugin-to-plugin call should succeed");
            Assert.True(data.CallCompleted, "Call should complete");
            Assert.True(data.ResponseReceived, "Response should be received");
            Assert.True(data.RoundTripMs > 0, "Round trip time should be recorded");
            Assert.True(data.RoundTripMs < 100, "Round trip should be fast");
        }
        finally
        {
            await Task.Delay(1000);
        }
    }

    [Fact(DisplayName = "Plugin load order should respect dependencies")]
    [Trait("Category", "E2E")]
    [Trait("Type", "Behavior")]
    public async Task PluginSystem_RespectsLoadOrder_ForDependencies()
    {
        // Arrange
        var startInfo = CreateProcessStartInfo(enableBehaviorEndpoints: true, port: 5563);
        var output = new StringBuilder();
        var ready = new TaskCompletionSource<bool>();

        // Act
        var process = StartProcess(startInfo, output, ready);

        try
        {
            await ready.Task.WaitAsync(TimeSpan.FromSeconds(30));

            using var httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(5) };
            
            // Get plugin load order
            var response = await httpClient.GetStringAsync(
                "http://localhost:5563/behavior/plugins/load-order");
            
            _output.WriteLine($"Load order response: {response}");

            var result = JsonSerializer.Deserialize<LoadOrderResult>(response, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            // Assert - Dependencies should load before dependents
            Assert.NotNull(result);
            Assert.NotEmpty(result.LoadOrder);

            // Resource plugin should load before plugins that depend on it
            var resourceIndex = result.LoadOrder.FindIndex(p => 
                p.Contains("resource", StringComparison.OrdinalIgnoreCase));
            var dependentIndex = result.LoadOrder.FindIndex(p => 
                p.Contains("dungeon", StringComparison.OrdinalIgnoreCase));

            if (resourceIndex >= 0 && dependentIndex >= 0)
            {
                Assert.True(resourceIndex < dependentIndex, 
                    "Resource plugin should load before dependent plugins");
            }

            _output.WriteLine($"Resource loaded at index {resourceIndex}");
            _output.WriteLine($"Dependent loaded at index {dependentIndex}");
        }
        finally
        {
            await Task.Delay(1000);
        }
    }

    private ProcessStartInfo CreateProcessStartInfo(bool enableBehaviorEndpoints = false, int port = 5558)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = "dotnet",
            Arguments = "run --no-build",
            WorkingDirectory = _hostPath,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            RedirectStandardInput = true,
            UseShellExecute = false,
            CreateNoWindow = true,
        };

        startInfo.Environment["DOTNET_ENVIRONMENT"] = "Testing";
        startInfo.Environment["DUNGEON_TestMode"] = "true";
        
        if (enableBehaviorEndpoints)
        {
            startInfo.Environment["DUNGEON_EnableBehaviorEndpoints"] = "true";
            startInfo.Environment["DUNGEON_BehaviorPort"] = port.ToString();
        }

        return startInfo;
    }

    private Process StartProcess(ProcessStartInfo startInfo, StringBuilder output, TaskCompletionSource<bool> readySignal)
    {
        var process = new Process { StartInfo = startInfo };
        _processesToClean.Add(process);

        process.OutputDataReceived += (sender, e) =>
        {
            if (e.Data != null)
            {
                output.AppendLine(e.Data);
                _output.WriteLine($"[OUT] {e.Data}");

                if (e.Data.Contains("Behavior endpoints listening") || 
                    e.Data.Contains("behavior endpoint ready") ||
                    e.Data.Contains("Test endpoints enabled"))
                {
                    readySignal.TrySetResult(true);
                }
            }
        };

        process.ErrorDataReceived += (sender, e) =>
        {
            if (e.Data != null)
            {
                output.AppendLine(e.Data);
                _output.WriteLine($"[ERR] {e.Data}");
            }
        };

        process.Start();
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        return process;
    }

    private string CreateTestResource(string filename, string content)
    {
        var tempPath = Path.Combine(Path.GetTempPath(), "wingedbean-tests", filename);
        Directory.CreateDirectory(Path.GetDirectoryName(tempPath)!);
        File.WriteAllText(tempPath, content);
        _output.WriteLine($"Created test resource: {tempPath}");
        return tempPath;
    }

    private string CreateTestConfig(object config)
    {
        var tempPath = Path.Combine(Path.GetTempPath(), "wingedbean-tests", "test-config.json");
        Directory.CreateDirectory(Path.GetDirectoryName(tempPath)!);
        var json = JsonSerializer.Serialize(config, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(tempPath, json);
        _output.WriteLine($"Created test config: {tempPath}");
        return tempPath;
    }

    private void CleanupTestResource(string path)
    {
        try
        {
            if (File.Exists(path))
            {
                File.Delete(path);
                _output.WriteLine($"Cleaned up test resource: {path}");
            }
        }
        catch (Exception ex)
        {
            _output.WriteLine($"Error cleaning up test resource: {ex.Message}");
        }
    }

    public void Dispose()
    {
        foreach (var process in _processesToClean)
        {
            try
            {
                if (!process.HasExited)
                {
                    process.Kill(entireProcessTree: true);
                    process.WaitForExit(5000);
                }
                process.Dispose();
            }
            catch (Exception ex)
            {
                _output.WriteLine($"Error cleaning up process: {ex.Message}");
            }
        }

        // Cleanup test resources directory
        try
        {
            var testDir = Path.Combine(Path.GetTempPath(), "wingedbean-tests");
            if (Directory.Exists(testDir))
            {
                Directory.Delete(testDir, recursive: true);
            }
        }
        catch (Exception ex)
        {
            _output.WriteLine($"Error cleaning up test directory: {ex.Message}");
        }
    }
}

// DTOs for behavior verification responses
public record ResourceLoadResult
{
    public bool Success { get; init; }
    public string? Content { get; init; }
    public double LoadTimeMs { get; init; }
}

public record EntityCreateResult
{
    public bool Success { get; init; }
    public int EntityId { get; init; }
}

public record EntityQueryResult
{
    public bool Success { get; init; }
    public int EntityId { get; init; }
    public List<string> Components { get; init; } = new();
}

public record ConfigGetResult
{
    public bool Success { get; init; }
    public string? Value { get; init; }
}

public record StateResult
{
    public int Value { get; init; }
}

public record PluginCallResult
{
    public bool Success { get; init; }
    public bool CallCompleted { get; init; }
    public bool ResponseReceived { get; init; }
    public double RoundTripMs { get; init; }
}

public record LoadOrderResult
{
    public List<string> LoadOrder { get; init; } = new();
}

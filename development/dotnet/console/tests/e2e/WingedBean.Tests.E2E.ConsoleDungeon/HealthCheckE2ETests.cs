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
/// Health check-based E2E tests that verify actual system state
/// instead of relying on log pattern matching.
/// </summary>
public class HealthCheckE2ETests : IDisposable
{
    private readonly ITestOutputHelper _output;
    private readonly string _hostPath;
    private readonly List<Process> _processesToClean = new();

    public HealthCheckE2ETests(ITestOutputHelper output)
    {
        _output = output;
        var currentDir = Directory.GetCurrentDirectory();
        _hostPath = Path.GetFullPath(Path.Combine(currentDir, "../../../src/host/ConsoleDungeon.Host"));
        _output.WriteLine($"Host directory: {_hostPath}");
    }

    [Fact(DisplayName = "Host health check endpoint should return system state")]
    [Trait("Category", "E2E")]
    [Trait("Type", "HealthCheck")]
    public async Task Host_HealthCheck_ReturnsSystemState()
    {
        // Arrange
        var startInfo = CreateProcessStartInfo(enableHealthCheck: true, port: 5555);
        var healthCheckUrl = "http://localhost:5555/health";
        
        var output = new StringBuilder();
        var healthCheckReady = new TaskCompletionSource<bool>();

        // Act
        var process = StartProcess(startInfo, output, healthCheckReady);

        try
        {
            // Wait for health check endpoint to be ready (max 30 seconds)
            var readyTask = await Task.WhenAny(
                healthCheckReady.Task,
                Task.Delay(TimeSpan.FromSeconds(30))
            );

            if (readyTask != healthCheckReady.Task)
            {
                _output.WriteLine("Health check endpoint did not become ready in time");
                _output.WriteLine($"Output so far:\n{output}");
                Assert.Fail("Health check endpoint did not start within 30 seconds");
            }

            // Query health check endpoint
            using var httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(5) };
            var response = await httpClient.GetStringAsync(healthCheckUrl);
            
            _output.WriteLine($"Health check response:\n{response}");

            // Parse and verify health check data
            var health = JsonSerializer.Deserialize<HealthCheckResponse>(response, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            // Assert - Verify actual state, not log messages
            Assert.NotNull(health);
            Assert.Equal("Healthy", health.Status);
            Assert.True(health.PluginsLoaded > 0, "At least one plugin should be loaded");
            Assert.Contains("IRegistry", health.RegisteredServices);
            Assert.True(health.UptimeSeconds > 0, "Uptime should be positive");
            Assert.Empty(health.CriticalErrors);
        }
        finally
        {
            await Task.Delay(1000); // Give process time to shutdown gracefully
        }
    }

    [Fact(DisplayName = "Plugin health check should verify each plugin is functional")]
    [Trait("Category", "E2E")]
    [Trait("Type", "HealthCheck")]
    public async Task Host_PluginHealthCheck_VerifiesPluginFunctionality()
    {
        // Arrange
        var startInfo = CreateProcessStartInfo(enableHealthCheck: true, port: 5556);
        var healthCheckUrl = "http://localhost:5556/health/plugins";
        
        var output = new StringBuilder();
        var healthCheckReady = new TaskCompletionSource<bool>();

        // Act
        var process = StartProcess(startInfo, output, healthCheckReady);

        try
        {
            await healthCheckReady.Task.WaitAsync(TimeSpan.FromSeconds(30));

            using var httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(5) };
            var response = await httpClient.GetStringAsync(healthCheckUrl);
            
            _output.WriteLine($"Plugin health check response:\n{response}");

            var pluginHealth = JsonSerializer.Deserialize<PluginHealthResponse>(response, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            // Assert - Verify each plugin's actual functionality
            Assert.NotNull(pluginHealth);
            Assert.NotEmpty(pluginHealth.Plugins);

            foreach (var plugin in pluginHealth.Plugins)
            {
                _output.WriteLine($"Plugin: {plugin.Id} - Status: {plugin.Status}");
                
                // Each plugin should be operational
                Assert.Contains(plugin.Status, new[] { "Healthy", "Degraded" });
                
                // Should have loaded successfully
                Assert.True(plugin.LoadedSuccessfully, $"Plugin {plugin.Id} should have loaded successfully");
                
                // Should not have critical errors
                Assert.Empty(plugin.Errors.Where(e => e.Severity == "Critical"));
            }

            // Verify critical plugins are present and healthy
            var criticalPlugins = new[] { "archecs", "resource", "config" };
            foreach (var pluginId in criticalPlugins)
            {
                var plugin = pluginHealth.Plugins.FirstOrDefault(p => 
                    p.Id.Contains(pluginId, StringComparison.OrdinalIgnoreCase));
                    
                Assert.NotNull(plugin);
                Assert.Equal("Healthy", plugin.Status);
            }
        }
        finally
        {
            await Task.Delay(1000);
        }
    }

    [Fact(DisplayName = "Service health check should verify services are actually callable")]
    [Trait("Category", "E2E")]
    [Trait("Type", "HealthCheck")]
    public async Task Host_ServiceHealthCheck_VerifiesServicesCallable()
    {
        // Arrange
        var startInfo = CreateProcessStartInfo(enableHealthCheck: true, port: 5557);
        var healthCheckUrl = "http://localhost:5557/health/services";
        
        var output = new StringBuilder();
        var healthCheckReady = new TaskCompletionSource<bool>();

        // Act
        var process = StartProcess(startInfo, output, healthCheckReady);

        try
        {
            await healthCheckReady.Task.WaitAsync(TimeSpan.FromSeconds(30));

            using var httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(5) };
            var response = await httpClient.GetStringAsync(healthCheckUrl);
            
            _output.WriteLine($"Service health check response:\n{response}");

            var serviceHealth = JsonSerializer.Deserialize<ServiceHealthResponse>(response, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            // Assert - Verify services are actually working
            Assert.NotNull(serviceHealth);
            Assert.NotEmpty(serviceHealth.Services);

            foreach (var service in serviceHealth.Services)
            {
                _output.WriteLine($"Service: {service.InterfaceName} - Callable: {service.IsCallable}");
                
                // Service should be callable
                Assert.True(service.IsCallable, $"Service {service.InterfaceName} should be callable");
                
                // Service instance should not be null
                Assert.True(service.InstanceExists, $"Service {service.InterfaceName} instance should exist");
                
                // If there are health check methods, they should pass
                if (service.HealthCheckPassed.HasValue)
                {
                    Assert.True(service.HealthCheckPassed.Value, 
                        $"Service {service.InterfaceName} health check should pass");
                }
            }

            // Verify IRegistry is callable
            var registry = serviceHealth.Services.FirstOrDefault(s => s.InterfaceName.Contains("IRegistry"));
            Assert.NotNull(registry);
            Assert.True(registry.IsCallable);
        }
        finally
        {
            await Task.Delay(1000);
        }
    }

    private ProcessStartInfo CreateProcessStartInfo(bool enableHealthCheck = false, int port = 5555)
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
        
        if (enableHealthCheck)
        {
            startInfo.Environment["DUNGEON_EnableHealthCheck"] = "true";
            startInfo.Environment["DUNGEON_HealthCheckPort"] = port.ToString();
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

                if (e.Data.Contains("Health check endpoint listening") || 
                    e.Data.Contains("health endpoint ready"))
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
    }
}

// DTOs for health check responses
public record HealthCheckResponse
{
    public string Status { get; init; } = "Unknown";
    public int PluginsLoaded { get; init; }
    public List<string> RegisteredServices { get; init; } = new();
    public double UptimeSeconds { get; init; }
    public List<string> CriticalErrors { get; init; } = new();
}

public record PluginHealthResponse
{
    public List<PluginHealth> Plugins { get; init; } = new();
}

public record PluginHealth
{
    public string Id { get; init; } = "";
    public string Status { get; init; } = "Unknown";
    public bool LoadedSuccessfully { get; init; }
    public List<PluginError> Errors { get; init; } = new();
}

public record PluginError
{
    public string Message { get; init; } = "";
    public string Severity { get; init; } = "Info";
}

public record ServiceHealthResponse
{
    public List<ServiceHealth> Services { get; init; } = new();
}

public record ServiceHealth
{
    public string InterfaceName { get; init; } = "";
    public bool IsCallable { get; init; }
    public bool InstanceExists { get; init; }
    public bool? HealthCheckPassed { get; init; }
}

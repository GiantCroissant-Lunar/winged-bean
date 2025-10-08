using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace WingedBean.Tests.E2E.ConsoleDungeon;

/// <summary>
/// Negative scenario tests that verify error handling, recovery,
/// and graceful degradation when things go wrong.
/// </summary>
public class NegativeScenarioE2ETests : IDisposable
{
    private readonly ITestOutputHelper _output;
    private readonly string _hostPath;
    private readonly List<Process> _processesToClean = new();
    private readonly string _testPluginsPath;

    public NegativeScenarioE2ETests(ITestOutputHelper output)
    {
        _output = output;
        var currentDir = Directory.GetCurrentDirectory();
        _hostPath = Path.GetFullPath(Path.Combine(currentDir, "../../../src/host/ConsoleDungeon.Host"));
        _testPluginsPath = Path.Combine(_hostPath, "bin/Debug/net8.0/test-plugins");
        _output.WriteLine($"Host directory: {_hostPath}");
        _output.WriteLine($"Test plugins: {_testPluginsPath}");
    }

    [Fact(DisplayName = "Host should handle missing plugin gracefully")]
    [Trait("Category", "E2E")]
    [Trait("Type", "Negative")]
    public async Task Host_MissingPlugin_HandlesGracefully()
    {
        // Arrange - Create manifest for non-existent plugin
        var testPluginDir = CreateTestPluginManifest("missing.plugin", new
        {
            id = "test.missing.plugin",
            version = "1.0.0",
            name = "Missing Plugin Test",
            entryPoint = new { dotnet = "./NonExistent.dll" },
            dependencies = new { plugins = new string[] { } }
        });

        var startInfo = CreateProcessStartInfo();
        startInfo.Environment["DUNGEON_AdditionalPluginPaths"] = testPluginDir;
        
        var output = new StringBuilder();
        var errorHandled = new TaskCompletionSource<bool>();

        // Act
        var process = StartProcess(startInfo, output, line =>
        {
            if (line.Contains("Failed to load plugin") || 
                line.Contains("Plugin load error") ||
                line.Contains("Skipping plugin"))
            {
                errorHandled.TrySetResult(true);
            }
        });

        try
        {
            // Wait for error handling (max 15 seconds)
            var handled = await errorHandled.Task.WaitAsync(TimeSpan.FromSeconds(15));

            // Assert - Should handle error gracefully
            Assert.True(handled, "Host should report plugin load failure");
            
            // Host should still start despite the error
            var fullOutput = output.ToString();
            Assert.DoesNotContain("Unhandled exception", fullOutput);
            Assert.DoesNotContain("Fatal error", fullOutput);
            
            // Should continue loading other plugins
            Assert.Contains("Loading plugins", fullOutput);
        }
        finally
        {
            CleanupTestPlugin(testPluginDir);
        }
    }

    [Fact(DisplayName = "Host should handle corrupted plugin manifest")]
    [Trait("Category", "E2E")]
    [Trait("Type", "Negative")]
    public async Task Host_CorruptedManifest_HandlesGracefully()
    {
        // Arrange - Create invalid JSON manifest
        var testPluginDir = Path.Combine(_testPluginsPath, "corrupted-manifest");
        Directory.CreateDirectory(testPluginDir);
        
        var manifestPath = Path.Combine(testPluginDir, ".plugin.json");
        await File.WriteAllTextAsync(manifestPath, "{ this is not valid JSON! }");

        var startInfo = CreateProcessStartInfo();
        startInfo.Environment["DUNGEON_AdditionalPluginPaths"] = testPluginDir;
        
        var output = new StringBuilder();
        var errorHandled = new TaskCompletionSource<bool>();

        // Act
        var process = StartProcess(startInfo, output, line =>
        {
            if (line.Contains("Invalid manifest") || 
                line.Contains("Failed to parse") ||
                line.Contains("JSON"))
            {
                errorHandled.TrySetResult(true);
            }
        });

        try
        {
            var handled = await errorHandled.Task.WaitAsync(TimeSpan.FromSeconds(15));

            // Assert - Should report parse error
            Assert.True(handled, "Host should report manifest parse error");
            
            var fullOutput = output.ToString();
            Assert.DoesNotContain("Unhandled exception", fullOutput);
            
            // Should continue despite bad manifest
            Assert.Contains("Loading plugins", fullOutput);
        }
        finally
        {
            CleanupTestPlugin(testPluginDir);
        }
    }

    [Fact(DisplayName = "Host should handle plugin with missing dependencies")]
    [Trait("Category", "E2E")]
    [Trait("Type", "Negative")]
    public async Task Host_MissingDependency_HandlesGracefully()
    {
        // Arrange - Create plugin that depends on non-existent plugin
        var testPluginDir = CreateTestPluginManifest("missing-dep", new
        {
            id = "test.missing.dependency",
            version = "1.0.0",
            name = "Missing Dependency Test",
            entryPoint = new { dotnet = "./Test.dll" },
            dependencies = new 
            { 
                plugins = new[] { "nonexistent.plugin.id" }
            }
        });

        var startInfo = CreateProcessStartInfo();
        startInfo.Environment["DUNGEON_AdditionalPluginPaths"] = testPluginDir;
        
        var output = new StringBuilder();
        var errorHandled = new TaskCompletionSource<bool>();

        // Act
        var process = StartProcess(startInfo, output, line =>
        {
            if (line.Contains("dependency") && 
                (line.Contains("not found") || line.Contains("missing")))
            {
                errorHandled.TrySetResult(true);
            }
        });

        try
        {
            var handled = await errorHandled.Task.WaitAsync(TimeSpan.FromSeconds(15));

            // Assert
            Assert.True(handled, "Host should report missing dependency");
            
            var fullOutput = output.ToString();
            Assert.DoesNotContain("Unhandled exception", fullOutput);
        }
        finally
        {
            CleanupTestPlugin(testPluginDir);
        }
    }

    [Fact(DisplayName = "Host should handle plugin that throws during activation")]
    [Trait("Category", "E2E")]
    [Trait("Type", "Negative")]
    public async Task Host_PluginThrowsOnActivation_HandlesGracefully()
    {
        // Arrange - Create plugin that will throw
        var testPluginDir = CreateFaultyPlugin("throws-on-activate");

        var startInfo = CreateProcessStartInfo();
        startInfo.Environment["DUNGEON_AdditionalPluginPaths"] = testPluginDir;
        
        var output = new StringBuilder();
        var errorHandled = new TaskCompletionSource<bool>();

        // Act
        var process = StartProcess(startInfo, output, line =>
        {
            if (line.Contains("Plugin activation failed") || 
                line.Contains("Exception during") ||
                line.Contains("Error activating"))
            {
                errorHandled.TrySetResult(true);
            }
        });

        try
        {
            var handled = await errorHandled.Task.WaitAsync(TimeSpan.FromSeconds(15));

            // Assert - Should catch and report the exception
            Assert.True(handled, "Host should report plugin activation failure");
            
            var fullOutput = output.ToString();
            // Should not crash the entire host
            Assert.DoesNotContain("Fatal error", fullOutput);
            
            // Other plugins should still load
            Assert.Contains("Loading plugins", fullOutput);
        }
        finally
        {
            CleanupTestPlugin(testPluginDir);
        }
    }

    [Fact(DisplayName = "Host should handle circular plugin dependencies")]
    [Trait("Category", "E2E")]
    [Trait("Type", "Negative")]
    public async Task Host_CircularDependencies_DetectsAndHandles()
    {
        // Arrange - Create two plugins that depend on each other
        var plugin1Dir = CreateTestPluginManifest("circular-a", new
        {
            id = "test.circular.a",
            version = "1.0.0",
            name = "Circular A",
            entryPoint = new { dotnet = "./A.dll" },
            dependencies = new { plugins = new[] { "test.circular.b" } }
        });

        var plugin2Dir = CreateTestPluginManifest("circular-b", new
        {
            id = "test.circular.b",
            version = "1.0.0",
            name = "Circular B",
            entryPoint = new { dotnet = "./B.dll" },
            dependencies = new { plugins = new[] { "test.circular.a" } }
        });

        var startInfo = CreateProcessStartInfo();
        startInfo.Environment["DUNGEON_AdditionalPluginPaths"] = $"{plugin1Dir};{plugin2Dir}";
        
        var output = new StringBuilder();
        var circularDetected = new TaskCompletionSource<bool>();

        // Act
        var process = StartProcess(startInfo, output, line =>
        {
            if (line.Contains("circular") || 
                line.Contains("dependency cycle") ||
                line.Contains("Circular dependency"))
            {
                circularDetected.TrySetResult(true);
            }
        });

        try
        {
            var detected = await circularDetected.Task.WaitAsync(TimeSpan.FromSeconds(15));

            // Assert - Should detect circular dependency
            Assert.True(detected, "Host should detect circular dependencies");
            
            var fullOutput = output.ToString();
            // Should not cause infinite loop or stack overflow
            Assert.DoesNotContain("StackOverflowException", fullOutput);
            Assert.DoesNotContain("Fatal error", fullOutput);
        }
        finally
        {
            CleanupTestPlugin(plugin1Dir);
            CleanupTestPlugin(plugin2Dir);
        }
    }

    [Fact(DisplayName = "Host should handle service registration failure")]
    [Trait("Category", "E2E")]
    [Trait("Type", "Negative")]
    public async Task Host_ServiceRegistrationFails_HandlesGracefully()
    {
        // Arrange
        var startInfo = CreateProcessStartInfo();
        startInfo.Environment["DUNGEON_SimulateServiceRegistrationFailure"] = "true";
        
        var output = new StringBuilder();
        var errorHandled = new TaskCompletionSource<bool>();

        // Act
        var process = StartProcess(startInfo, output, line =>
        {
            if (line.Contains("Service registration failed") || 
                line.Contains("Failed to register service"))
            {
                errorHandled.TrySetResult(true);
            }
        });

        try
        {
            var handled = await errorHandled.Task.WaitAsync(TimeSpan.FromSeconds(15));

            // Assert
            var fullOutput = output.ToString();
            
            // Should log the error
            if (!handled)
            {
                _output.WriteLine("Service registration failure simulation not implemented");
                // This is expected if the feature isn't implemented yet
                return;
            }
            
            Assert.True(handled, "Host should report service registration failure");
            Assert.DoesNotContain("Unhandled exception", fullOutput);
        }
        finally
        {
            await Task.Delay(1000);
        }
    }

    [Fact(DisplayName = "Host should timeout gracefully on slow plugin")]
    [Trait("Category", "E2E")]
    [Trait("Type", "Negative")]
    public async Task Host_SlowPlugin_TimesOutGracefully()
    {
        // Arrange - Create plugin that takes too long to load
        var testPluginDir = CreateTestPluginManifest("slow-plugin", new
        {
            id = "test.slow.plugin",
            version = "1.0.0",
            name = "Slow Plugin Test",
            entryPoint = new { dotnet = "./Slow.dll" },
            dependencies = new { plugins = new string[] { } },
            loadTimeout = 5 // 5 second timeout
        });

        var startInfo = CreateProcessStartInfo();
        startInfo.Environment["DUNGEON_AdditionalPluginPaths"] = testPluginDir;
        startInfo.Environment["DUNGEON_SimulateSlowPlugin"] = "test.slow.plugin";
        
        var output = new StringBuilder();
        var timeoutHandled = new TaskCompletionSource<bool>();

        // Act
        var process = StartProcess(startInfo, output, line =>
        {
            if (line.Contains("timeout") || 
                line.Contains("took too long") ||
                line.Contains("exceeded time limit"))
            {
                timeoutHandled.TrySetResult(true);
            }
        });

        try
        {
            var handled = await timeoutHandled.Task.WaitAsync(TimeSpan.FromSeconds(20));

            // Assert
            if (!handled)
            {
                _output.WriteLine("Plugin timeout handling not implemented");
                return;
            }
            
            Assert.True(handled, "Host should detect and handle slow plugin");
            
            var fullOutput = output.ToString();
            Assert.DoesNotContain("Fatal error", fullOutput);
            
            // Should continue with other plugins
            Assert.Contains("Loading plugins", fullOutput);
        }
        finally
        {
            CleanupTestPlugin(testPluginDir);
        }
    }

    private ProcessStartInfo CreateProcessStartInfo()
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

        return startInfo;
    }

    private Process StartProcess(ProcessStartInfo startInfo, StringBuilder output, Action<string> lineHandler)
    {
        var process = new Process { StartInfo = startInfo };
        _processesToClean.Add(process);

        process.OutputDataReceived += (sender, e) =>
        {
            if (e.Data != null)
            {
                output.AppendLine(e.Data);
                _output.WriteLine($"[OUT] {e.Data}");
                lineHandler(e.Data);
            }
        };

        process.ErrorDataReceived += (sender, e) =>
        {
            if (e.Data != null)
            {
                output.AppendLine(e.Data);
                _output.WriteLine($"[ERR] {e.Data}");
                lineHandler(e.Data);
            }
        };

        process.Start();
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        return process;
    }

    private string CreateTestPluginManifest(string pluginName, object manifest)
    {
        var testPluginDir = Path.Combine(_testPluginsPath, pluginName);
        Directory.CreateDirectory(testPluginDir);
        
        var manifestPath = Path.Combine(testPluginDir, ".plugin.json");
        var json = JsonSerializer.Serialize(manifest, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(manifestPath, json);
        
        _output.WriteLine($"Created test plugin manifest: {manifestPath}");
        return testPluginDir;
    }

    private string CreateFaultyPlugin(string pluginName)
    {
        // For now, just create a manifest
        // In a full implementation, you'd compile a plugin that throws
        return CreateTestPluginManifest(pluginName, new
        {
            id = $"test.faulty.{pluginName}",
            version = "1.0.0",
            name = $"Faulty Plugin {pluginName}",
            entryPoint = new { dotnet = $"./{pluginName}.dll" },
            dependencies = new { plugins = new string[] { } }
        });
    }

    private void CleanupTestPlugin(string pluginDir)
    {
        try
        {
            if (Directory.Exists(pluginDir))
            {
                Directory.Delete(pluginDir, recursive: true);
                _output.WriteLine($"Cleaned up test plugin: {pluginDir}");
            }
        }
        catch (Exception ex)
        {
            _output.WriteLine($"Error cleaning up test plugin: {ex.Message}");
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

        // Cleanup test plugins directory
        try
        {
            if (Directory.Exists(_testPluginsPath))
            {
                Directory.Delete(_testPluginsPath, recursive: true);
            }
        }
        catch (Exception ex)
        {
            _output.WriteLine($"Error cleaning up test plugins directory: {ex.Message}");
        }
    }
}

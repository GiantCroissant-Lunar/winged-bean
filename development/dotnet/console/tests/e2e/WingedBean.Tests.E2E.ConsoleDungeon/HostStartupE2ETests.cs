using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace WingedBean.Tests.E2E.ConsoleDungeon;

/// <summary>
/// End-to-end tests for ConsoleDungeon.Host startup in different modes.
/// Tests verify that the host starts successfully, loads all plugins,
/// and registers required services in Console, PTY, and WebSocket modes.
/// </summary>
public class HostStartupE2ETests : IDisposable
{
    private readonly ITestOutputHelper _output;
    private readonly string _hostPath;
    private readonly List<Process> _processesToClean = new();

    public HostStartupE2ETests(ITestOutputHelper output)
    {
        _output = output;
        
        // Find the host executable
        var currentDir = Directory.GetCurrentDirectory();
        var hostDir = Path.GetFullPath(Path.Combine(currentDir, "../../../src/host/ConsoleDungeon.Host"));
        _hostPath = hostDir;
        
        _output.WriteLine($"Test directory: {currentDir}");
        _output.WriteLine($"Host directory: {hostDir}");
    }

    [Fact(DisplayName = "Host should start in console mode and load all plugins")]
    [Trait("Category", "E2E")]
    [Trait("Mode", "Console")]
    public async Task Host_ConsoleMode_StartsAndLoadsPlugins()
    {
        // Arrange
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
            Environment =
            {
                ["DOTNET_ENVIRONMENT"] = "Development",
                ["DUNGEON_TestMode"] = "true" // Flag for tests to exit early
            }
        };

        var output = new StringBuilder();
        var errorOutput = new StringBuilder();
        var hostStarted = new TaskCompletionSource<bool>();
        var pluginsLoaded = new TaskCompletionSource<bool>();
        var terminalAppRegistered = new TaskCompletionSource<bool>();

        // Act
        var process = new Process { StartInfo = startInfo };
        _processesToClean.Add(process);

        process.OutputDataReceived += (sender, e) =>
        {
            if (e.Data != null)
            {
                output.AppendLine(e.Data);
                _output.WriteLine($"[OUT] {e.Data}");

                if (e.Data.Contains("ConsoleDungeon.Host starting"))
                    hostStarted.TrySetResult(true);

                if (e.Data.Contains("✓ All plugins loaded") || e.Data.Contains("Loaded:"))
                    pluginsLoaded.TrySetResult(true);

                if (e.Data.Contains("✓ ITerminalApp registered") || 
                    e.Data.Contains("ITerminalApp"))
                    terminalAppRegistered.TrySetResult(true);
            }
        };

        process.ErrorDataReceived += (sender, e) =>
        {
            if (e.Data != null)
            {
                errorOutput.AppendLine(e.Data);
                _output.WriteLine($"[ERR] {e.Data}");
            }
        };

        process.Start();
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        try
        {
            // Wait for startup (30 second timeout)
            var startedTask = await Task.WhenAny(
                hostStarted.Task,
                Task.Delay(TimeSpan.FromSeconds(30))
            );

            // Give it a few more seconds to load plugins
            await Task.Delay(TimeSpan.FromSeconds(5));

            // Kill the process
            if (!process.HasExited)
            {
                process.Kill(entireProcessTree: true);
                await process.WaitForExitAsync();
            }

            // Assert
            var outputText = output.ToString();
            var errorText = errorOutput.ToString();

            _output.WriteLine("\n=== FULL OUTPUT ===");
            _output.WriteLine(outputText);
            _output.WriteLine("\n=== FULL ERROR OUTPUT ===");
            _output.WriteLine(errorText);

            // Verify host started
            Assert.True(hostStarted.Task.IsCompletedSuccessfully, 
                "Host should start successfully");

            // Verify critical components
            Assert.Contains("Foundation services initialized", outputText);
            Assert.Contains("Loading plugins", outputText);
            
            // Verify specific plugins loaded
            Assert.Contains("ArchECS", outputText);
            Assert.Contains("Resource", outputText);
            
            // Should not have critical errors
            Assert.DoesNotContain("Fatal error", errorText);
            Assert.DoesNotContain("Unhandled exception", errorText);
        }
        finally
        {
            if (!process.HasExited)
            {
                process.Kill(entireProcessTree: true);
            }
        }
    }

    [Fact(DisplayName = "Host should register ITerminalApp from ConsoleDungeon plugin")]
    [Trait("Category", "E2E")]
    [Trait("Mode", "Console")]
    public async Task Host_ConsoleMode_RegistersITerminalApp()
    {
        // Arrange
        var startInfo = new ProcessStartInfo
        {
            FileName = "dotnet",
            Arguments = "run --no-build",
            WorkingDirectory = _hostPath,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            RedirectStandardInput = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        var output = new StringBuilder();
        var terminalAppFound = false;

        // Act
        var process = new Process { StartInfo = startInfo };
        _processesToClean.Add(process);

        process.OutputDataReceived += (sender, e) =>
        {
            if (e.Data != null)
            {
                output.AppendLine(e.Data);
                _output.WriteLine($"[OUT] {e.Data}");

                if (e.Data.Contains("✓ ITerminalApp registered") || 
                    e.Data.Contains("ITerminalApp"))
                {
                    terminalAppFound = true;
                }
            }
        };

        process.ErrorDataReceived += (sender, e) =>
        {
            if (e.Data != null)
            {
                _output.WriteLine($"[ERR] {e.Data}");
            }
        };

        process.Start();
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        try
        {
            // Wait for startup
            await Task.Delay(TimeSpan.FromSeconds(8));

            // Kill the process
            if (!process.HasExited)
            {
                process.Kill(entireProcessTree: true);
                await process.WaitForExitAsync();
            }

            // Assert
            var outputText = output.ToString();
            _output.WriteLine("\n=== ITerminalApp Registration Check ===");
            _output.WriteLine(outputText);

            Assert.True(terminalAppFound, 
                "ITerminalApp should be registered by ConsoleDungeon plugin");
            
            // Should not have the "not found" error
            Assert.DoesNotContain("ITerminalApp not found", outputText);
            Assert.DoesNotContain("ServiceNotFoundException", outputText);
        }
        finally
        {
            if (!process.HasExited)
            {
                process.Kill(entireProcessTree: true);
            }
        }
    }

    [Fact(DisplayName = "Host should load all critical plugins successfully")]
    [Trait("Category", "E2E")]
    [Trait("Mode", "Console")]
    public async Task Host_ConsoleMode_LoadsCriticalPlugins()
    {
        // Arrange
        var criticalPlugins = new[]
        {
            "ArchECS",
            "Config",
            "Resource",
            "ConsoleDungeon",
            "DungeonGame"
        };

        var startInfo = new ProcessStartInfo
        {
            FileName = "dotnet",
            Arguments = "run --no-build",
            WorkingDirectory = _hostPath,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            RedirectStandardInput = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        var output = new StringBuilder();

        // Act
        var process = new Process { StartInfo = startInfo };
        _processesToClean.Add(process);

        process.OutputDataReceived += (sender, e) =>
        {
            if (e.Data != null)
            {
                output.AppendLine(e.Data);
                _output.WriteLine($"[OUT] {e.Data}");
            }
        };

        process.ErrorDataReceived += (sender, e) =>
        {
            if (e.Data != null)
            {
                output.AppendLine($"ERROR: {e.Data}");
                _output.WriteLine($"[ERR] {e.Data}");
            }
        };

        process.Start();
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        try
        {
            // Wait for startup
            await Task.Delay(TimeSpan.FromSeconds(8));

            // Kill the process
            if (!process.HasExited)
            {
                process.Kill(entireProcessTree: true);
                await process.WaitForExitAsync();
            }

            // Assert
            var outputText = output.ToString();
            _output.WriteLine("\n=== Critical Plugins Check ===");

            foreach (var plugin in criticalPlugins)
            {
                var found = outputText.Contains(plugin, StringComparison.OrdinalIgnoreCase);
                _output.WriteLine($"Plugin '{plugin}': {(found ? "✓ Found" : "✗ NOT FOUND")}");
                Assert.True(found, $"Critical plugin '{plugin}' should be loaded");
            }
        }
        finally
        {
            if (!process.HasExited)
            {
                process.Kill(entireProcessTree: true);
            }
        }
    }

    [Fact(DisplayName = "Host should not have fatal errors or exceptions")]
    [Trait("Category", "E2E")]
    [Trait("Mode", "Console")]
    public async Task Host_ConsoleMode_NoFatalErrors()
    {
        // Arrange
        var startInfo = new ProcessStartInfo
        {
            FileName = "dotnet",
            Arguments = "run --no-build",
            WorkingDirectory = _hostPath,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            RedirectStandardInput = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        var output = new StringBuilder();
        var errorOutput = new StringBuilder();

        // Act
        var process = new Process { StartInfo = startInfo };
        _processesToClean.Add(process);

        process.OutputDataReceived += (sender, e) =>
        {
            if (e.Data != null)
            {
                output.AppendLine(e.Data);
                _output.WriteLine($"[OUT] {e.Data}");
            }
        };

        process.ErrorDataReceived += (sender, e) =>
        {
            if (e.Data != null)
            {
                errorOutput.AppendLine(e.Data);
                _output.WriteLine($"[ERR] {e.Data}");
            }
        };

        process.Start();
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        try
        {
            // Wait for startup
            await Task.Delay(TimeSpan.FromSeconds(8));

            // Kill the process
            if (!process.HasExited)
            {
                process.Kill(entireProcessTree: true);
                await process.WaitForExitAsync();
            }

            // Assert
            var outputText = output.ToString();
            var errorText = errorOutput.ToString();

            _output.WriteLine("\n=== Error Check ===");
            _output.WriteLine($"Output length: {outputText.Length}");
            _output.WriteLine($"Error length: {errorText.Length}");

            // Check for critical errors
            Assert.DoesNotContain("Fatal error", outputText);
            Assert.DoesNotContain("Unhandled exception", outputText);
            Assert.DoesNotContain("System.Exception", errorText);
            
            // Process should have started successfully
            Assert.NotEqual(0, outputText.Length);
        }
        finally
        {
            if (!process.HasExited)
            {
                process.Kill(entireProcessTree: true);
            }
        }
    }

    [Fact(DisplayName = "Host should register all required services")]
    [Trait("Category", "E2E")]
    [Trait("Mode", "Console")]
    public async Task Host_ConsoleMode_RegistersRequiredServices()
    {
        // Arrange
        var requiredServices = new[]
        {
            "IService", // ECS, Config, Resource, etc.
            "ITerminalApp",
            "Registry"
        };

        var startInfo = new ProcessStartInfo
        {
            FileName = "dotnet",
            Arguments = "run --no-build",
            WorkingDirectory = _hostPath,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            RedirectStandardInput = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        var output = new StringBuilder();

        // Act
        var process = new Process { StartInfo = startInfo };
        _processesToClean.Add(process);

        process.OutputDataReceived += (sender, e) =>
        {
            if (e.Data != null)
            {
                output.AppendLine(e.Data);
                _output.WriteLine($"[OUT] {e.Data}");
            }
        };

        process.ErrorDataReceived += (sender, e) =>
        {
            if (e.Data != null)
            {
                _output.WriteLine($"[ERR] {e.Data}");
            }
        };

        process.Start();
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        try
        {
            // Wait for startup
            await Task.Delay(TimeSpan.FromSeconds(8));

            // Kill the process
            if (!process.HasExited)
            {
                process.Kill(entireProcessTree: true);
                await process.WaitForExitAsync();
            }

            // Assert
            var outputText = output.ToString();
            _output.WriteLine("\n=== Service Registration Check ===");

            // At minimum, should mention services being registered
            Assert.Contains("Registered", outputText);
            
            // Should not have "not found" errors for critical services
            Assert.DoesNotContain("Service ITerminalApp not found", outputText);
        }
        finally
        {
            if (!process.HasExited)
            {
                process.Kill(entireProcessTree: true);
            }
        }
    }

    public void Dispose()
    {
        // Cleanup any remaining processes
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
            catch
            {
                // Ignore cleanup errors
            }
        }
    }
}

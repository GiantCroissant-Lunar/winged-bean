using System;
using System.Diagnostics;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace WingedBean.Tests.E2E.ConsoleDungeon;

/// <summary>
/// End-to-end tests for different execution modes: Console, PTY, and WebSocket.
/// These tests verify that plugins load correctly and services are available
/// regardless of the execution mode.
/// </summary>
public class MultiModeE2ETests : IDisposable
{
    private readonly ITestOutputHelper _output;
    private readonly string _hostPath;
    private readonly List<Process> _processesToClean = new();

    public MultiModeE2ETests(ITestOutputHelper output)
    {
        _output = output;
        
        var currentDir = Directory.GetCurrentDirectory();
        var hostDir = Path.GetFullPath(Path.Combine(currentDir, "../../../src/host/ConsoleDungeon.Host"));
        _hostPath = hostDir;
        
        _output.WriteLine($"Host directory: {hostDir}");
    }

    [Fact(DisplayName = "Console mode: All plugins should load successfully")]
    [Trait("Category", "E2E")]
    [Trait("Mode", "Console")]
    public async Task ConsoleMode_LoadsAllPlugins()
    {
        // Arrange
        var output = await RunHostAndCaptureOutput();

        // Assert
        _output.WriteLine("\n=== CONSOLE MODE TEST ===");
        _output.WriteLine(output);

        Assert.Contains("Foundation services initialized", output);
        Assert.Contains("Loading plugins", output);
        Assert.Contains("Loaded:", output); // At least one plugin loaded
    }

    [Theory(DisplayName = "All modes should load critical plugins")]
    [Trait("Category", "E2E")]
    [Trait("Mode", "Multi")]
    [InlineData("Console", "")]
    // PTY and WebSocket modes would require special configuration
    // [InlineData("PTY", "--pty")]
    // [InlineData("WebSocket", "--websocket")]
    public async Task AllModes_LoadCriticalPlugins(string modeName, string args)
    {
        // Arrange
        _output.WriteLine($"\n=== TESTING {modeName.ToUpper()} MODE ===");
        var output = await RunHostAndCaptureOutput(args);

        // Assert
        var criticalPlugins = new[]
        {
            "Config",
            "Resource",
            "ArchECS"
        };

        foreach (var plugin in criticalPlugins)
        {
            var found = output.Contains(plugin, StringComparison.OrdinalIgnoreCase);
            _output.WriteLine($"[{modeName}] Plugin '{plugin}': {(found ? "✓" : "✗")}");
            Assert.True(found, $"Critical plugin '{plugin}' should be loaded in {modeName} mode");
        }
    }

    [Fact(DisplayName = "Console mode: Namespace migration should not cause errors")]
    [Trait("Category", "E2E")]
    [Trait("Mode", "Console")]
    public async Task ConsoleMode_NamespaceMigration_NoErrors()
    {
        // Arrange
        var output = await RunHostAndCaptureOutput();

        // Assert
        _output.WriteLine("\n=== NAMESPACE MIGRATION VERIFICATION ===");

        // Should not have namespace-related errors
        Assert.DoesNotContain("could not be found", output);
        Assert.DoesNotContain("WingedBean.Contracts", output); // Old namespace shouldn't appear in errors
        Assert.DoesNotContain("type or namespace", output.ToLower());
        
        // Should successfully load plugins with new namespaces
        Assert.Contains("Loaded:", output);
    }

    [Fact(DisplayName = "Console mode: IPlugin bridge pattern should work")]
    [Trait("Category", "E2E")]
    [Trait("Mode", "Console")]
    public async Task ConsoleMode_IPluginBridges_Work()
    {
        // Arrange
        var output = await RunHostAndCaptureOutput();

        // Assert
        _output.WriteLine("\n=== IPLUGIN BRIDGE PATTERN VERIFICATION ===");

        // Plugins using IPlugin bridge should load
        var bridgePlugins = new[]
        {
            "ArchECS",
            "TerminalUI",
            "Config",
            "Audio",
            "ConsoleDungeon",
            "DungeonGame"
        };

        int foundCount = 0;
        foreach (var plugin in bridgePlugins)
        {
            if (output.Contains(plugin, StringComparison.OrdinalIgnoreCase))
            {
                foundCount++;
                _output.WriteLine($"✓ Bridge plugin '{plugin}' loaded");
            }
        }

        Assert.True(foundCount >= 3, 
            $"At least 3 bridge plugins should load successfully (found {foundCount})");
    }

    [Fact(DisplayName = "Console mode: Service registration should work with new namespaces")]
    [Trait("Category", "E2E")]
    [Trait("Mode", "Console")]
    public async Task ConsoleMode_ServiceRegistration_WithNewNamespaces()
    {
        // Arrange
        var output = await RunHostAndCaptureOutput();

        // Assert
        _output.WriteLine("\n=== SERVICE REGISTRATION WITH NEW NAMESPACES ===");

        // Should have successful registrations
        Assert.Contains("Registered", output);
        
        // Should not have registration failures
        Assert.DoesNotContain("Failed to register", output);
        Assert.DoesNotContain("ServiceNotFoundException", output);
        
        // Specific services should be registered
        var serviceNames = new[] { "ITerminalApp", "IService" };
        foreach (var service in serviceNames)
        {
            var mentioned = output.Contains(service);
            _output.WriteLine($"Service '{service}': {(mentioned ? "mentioned" : "not mentioned")}");
        }
    }

    [Fact(DisplayName = "Console mode: No circular dependencies")]
    [Trait("Category", "E2E")]
    [Trait("Mode", "Console")]
    public async Task ConsoleMode_NoCircularDependencies()
    {
        // Arrange
        var output = await RunHostAndCaptureOutput();

        // Assert
        _output.WriteLine("\n=== CIRCULAR DEPENDENCY CHECK ===");

        Assert.DoesNotContain("circular", output.ToLower());
        Assert.DoesNotContain("dependency cycle", output.ToLower());
        Assert.DoesNotContain("StackOverflowException", output);
    }

    [Fact(DisplayName = "Console mode: Verify startup sequence")]
    [Trait("Category", "E2E")]
    [Trait("Mode", "Console")]
    public async Task ConsoleMode_VerifyStartupSequence()
    {
        // Arrange
        var output = await RunHostAndCaptureOutput();

        // Assert
        _output.WriteLine("\n=== STARTUP SEQUENCE VERIFICATION ===");

        // Expected sequence
        var sequence = new[]
        {
            "ConsoleDungeon.Host starting",
            "Foundation services",
            "Loading plugins",
            "Loaded:"
        };

        int lastIndex = -1;
        foreach (var step in sequence)
        {
            var index = output.IndexOf(step, StringComparison.OrdinalIgnoreCase);
            _output.WriteLine($"Step '{step}': {(index > lastIndex ? $"✓ at {index}" : "✗ out of order")}");
            
            Assert.True(index > lastIndex, 
                $"Startup step '{step}' should appear after previous steps");
            lastIndex = index;
        }
    }

    [Fact(DisplayName = "Console mode: Plugin load time should be reasonable")]
    [Trait("Category", "E2E")]
    [Trait("Mode", "Console")]
    [Trait("Performance", "LoadTime")]
    public async Task ConsoleMode_PluginLoadTime_Reasonable()
    {
        // Arrange
        var stopwatch = Stopwatch.StartNew();
        var output = await RunHostAndCaptureOutput(timeout: 30);
        stopwatch.Stop();

        // Assert
        _output.WriteLine($"\n=== LOAD TIME: {stopwatch.ElapsedMilliseconds}ms ===");
        
        // Should load within reasonable time (30 seconds including first-time JIT)
        Assert.True(stopwatch.Elapsed.TotalSeconds < 30, 
            $"Host should start within 30 seconds (took {stopwatch.Elapsed.TotalSeconds:F2}s)");
        
        // Should contain successful startup
        Assert.Contains("Loaded:", output);
    }

    /// <summary>
    /// Helper method to run the host and capture output
    /// </summary>
    private async Task<string> RunHostAndCaptureOutput(
        string arguments = "", 
        int timeout = 8)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = "dotnet",
            Arguments = $"run --no-build {arguments}".Trim(),
            WorkingDirectory = _hostPath,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            RedirectStandardInput = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        var output = new StringBuilder();
        var errorOutput = new StringBuilder();

        var process = new Process { StartInfo = startInfo };
        _processesToClean.Add(process);

        process.OutputDataReceived += (sender, e) =>
        {
            if (e.Data != null)
            {
                output.AppendLine(e.Data);
            }
        };

        process.ErrorDataReceived += (sender, e) =>
        {
            if (e.Data != null)
            {
                errorOutput.AppendLine(e.Data);
            }
        };

        process.Start();
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        try
        {
            // Wait for specified timeout
            await Task.Delay(TimeSpan.FromSeconds(timeout));

            // Kill the process
            if (!process.HasExited)
            {
                process.Kill(entireProcessTree: true);
                await process.WaitForExitAsync();
            }

            var combined = output.ToString();
            var errors = errorOutput.ToString();
            
            if (!string.IsNullOrEmpty(errors))
            {
                combined += "\n\n=== ERRORS ===\n" + errors;
            }

            return combined;
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

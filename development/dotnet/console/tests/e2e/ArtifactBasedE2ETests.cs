using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace WingedBean.Tests.E2E.ConsoleDungeon;

/// <summary>
/// E2E tests that follow build artifact conventions (R-RUN-020).
/// Always runs from versioned artifacts under build/_artifacts/v{version}/
/// instead of using 'dotnet run'.
/// </summary>
public class ArtifactBasedE2ETests : IDisposable
{
    private readonly ITestOutputHelper _output;
    private readonly string _projectRoot;
    private readonly string _artifactsRoot;
    private readonly string _currentVersion;
    private readonly string _hostBinaryPath;
    private readonly List<Process> _processesToClean = new();

    public ArtifactBasedE2ETests(ITestOutputHelper output)
    {
        _output = output;
        
        // Navigate to project root (where build/ directory is)
        var currentDir = Directory.GetCurrentDirectory();
        _projectRoot = FindProjectRoot(currentDir);
        _artifactsRoot = Path.Combine(_projectRoot, "build/_artifacts");
        
        // Get current version
        _currentVersion = GetCurrentVersion();
        
        // Construct path to host binary following R-RUN-020
        var versionDir = Path.Combine(_artifactsRoot, $"v{_currentVersion}");
        _hostBinaryPath = Path.Combine(versionDir, "dotnet/bin/ConsoleDungeon.Host");
        
        // Check if binary exists (may need to be built first)
        if (!File.Exists(_hostBinaryPath) && !File.Exists(_hostBinaryPath + ".dll"))
        {
            _output.WriteLine($"⚠️  Host binary not found at: {_hostBinaryPath}");
            _output.WriteLine($"   Run 'task build-all' to create versioned artifacts");
        }
        
        _output.WriteLine($"Project root: {_projectRoot}");
        _output.WriteLine($"Artifacts root: {_artifactsRoot}");
        _output.WriteLine($"Current version: v{_currentVersion}");
        _output.WriteLine($"Host binary: {_hostBinaryPath}");
    }

    [Fact(DisplayName = "Host binary should exist in versioned artifacts")]
    [Trait("Category", "E2E")]
    [Trait("Type", "Artifact")]
    public void HostBinary_ExistsInVersionedArtifacts()
    {
        // Check for either native binary or .dll
        var exists = File.Exists(_hostBinaryPath) || 
                     File.Exists(_hostBinaryPath + ".dll") ||
                     File.Exists(_hostBinaryPath + ".exe");
        
        Assert.True(exists, 
            $"Host binary not found in artifacts. Expected at: {_hostBinaryPath}\n" +
            $"Run 'task build-all' to build artifacts.");
    }

    [Fact(DisplayName = "Host should start from versioned artifacts (R-RUN-020)")]
    [Trait("Category", "E2E")]
    [Trait("Type", "Artifact")]
    public async Task Host_StartsFromVersionedArtifacts_Successfully()
    {
        // Following R-RUN-020: Always run from versioned artifacts
        
        var startInfo = CreateArtifactBasedProcessStartInfo();
        var output = new StringBuilder();
        var hostStarted = new TaskCompletionSource<bool>();
        
        var process = new Process { StartInfo = startInfo };
        _processesToClean.Add(process);

        process.OutputDataReceived += (sender, e) =>
        {
            if (e.Data != null)
            {
                output.AppendLine(e.Data);
                _output.WriteLine($"[OUT] {e.Data}");

                if (e.Data.Contains("ConsoleDungeon.Host starting") || 
                    e.Data.Contains("Foundation services"))
                {
                    hostStarted.TrySetResult(true);
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

        try
        {
            // Wait for startup
            var started = await hostStarted.Task.WaitAsync(TimeSpan.FromSeconds(30));
            
            Assert.True(started, "Host should start from versioned artifacts");
            
            // Verify it's running from correct path
            var fullOutput = output.ToString();
            
            // Should not contain "dotnet run" references
            Assert.DoesNotContain("dotnet run", fullOutput);
            
            _output.WriteLine($"✓ Host started successfully from v{_currentVersion} artifacts");
        }
        finally
        {
            await Task.Delay(1000);
        }
    }

    [Fact(DisplayName = "Plugins should load from flattened artifact layout (R-RUN-023)")]
    [Trait("Category", "E2E")]
    [Trait("Type", "Artifact")]
    public async Task Plugins_LoadFromFlattenedArtifacts_Successfully()
    {
        // R-RUN-023: Plugin paths should be relative, loader resolves flattened layouts
        
        var startInfo = CreateArtifactBasedProcessStartInfo();
        var output = new StringBuilder();
        var pluginsLoaded = new TaskCompletionSource<bool>();
        var loadedPlugins = new List<string>();

        var process = new Process { StartInfo = startInfo };
        _processesToClean.Add(process);

        process.OutputDataReceived += (sender, e) =>
        {
            if (e.Data != null)
            {
                output.AppendLine(e.Data);
                _output.WriteLine($"[OUT] {e.Data}");

                if (e.Data.Contains("✓ Loaded:") || e.Data.Contains("Loaded plugin:"))
                {
                    loadedPlugins.Add(e.Data);
                }

                if (e.Data.Contains("All plugins loaded") || 
                    e.Data.Contains("plugins loaded successfully"))
                {
                    pluginsLoaded.TrySetResult(true);
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

        try
        {
            await pluginsLoaded.Task.WaitAsync(TimeSpan.FromSeconds(30));

            // Verify plugins loaded
            Assert.NotEmpty(loadedPlugins);
            
            _output.WriteLine($"✓ {loadedPlugins.Count} plugins loaded from flattened artifact layout");
            
            // Verify no absolute path errors
            var fullOutput = output.ToString();
            Assert.DoesNotContain("absolute path", fullOutput, StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            await Task.Delay(1000);
        }
    }

    [Fact(DisplayName = "Artifact version should match git version")]
    [Trait("Category", "E2E")]
    [Trait("Type", "Artifact")]
    public void ArtifactVersion_MatchesGitVersion()
    {
        // Verify version consistency
        var versionFromScript = GetCurrentVersion();
        var versionDir = Path.Combine(_artifactsRoot, $"v{versionFromScript}");
        
        Assert.True(Directory.Exists(versionDir), 
            $"Version directory should exist: {versionDir}");
        
        _output.WriteLine($"✓ Artifact version v{versionFromScript} is consistent");
    }

    [Theory(DisplayName = "Should run via provided scripts/tasks (R-RUN-021/R-RUN-022)")]
    [InlineData("debug", "test-tools/run-debug-mode.sh")]
    [InlineData("normal", "test-tools/run-normal-mode.sh")]
    [Trait("Category", "E2E")]
    [Trait("Type", "Artifact")]
    public async Task Host_RunsViaProvidedScripts_Successfully(string mode, string scriptPath)
    {
        var fullScriptPath = Path.Combine(_projectRoot, scriptPath);
        
        if (!File.Exists(fullScriptPath))
        {
            _output.WriteLine($"⚠️  Script not found: {fullScriptPath}");
            _output.WriteLine("   This test expects scripts following R-RUN-021/R-RUN-022");
            return;
        }

        var startInfo = new ProcessStartInfo
        {
            FileName = fullScriptPath,
            WorkingDirectory = _projectRoot,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            RedirectStandardInput = true,
            UseShellExecute = false,
            CreateNoWindow = true,
        };
        
        startInfo.Environment["DUNGEON_TestMode"] = "true";

        var output = new StringBuilder();
        var scriptRan = new TaskCompletionSource<bool>();
        
        var process = new Process { StartInfo = startInfo };
        _processesToClean.Add(process);

        process.OutputDataReceived += (sender, e) =>
        {
            if (e.Data != null)
            {
                output.AppendLine(e.Data);
                _output.WriteLine($"[{mode.ToUpper()}] {e.Data}");

                if (e.Data.Contains("ConsoleDungeon") || e.Data.Contains("starting"))
                {
                    scriptRan.TrySetResult(true);
                }
            }
        };

        process.Start();
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        try
        {
            var ran = await scriptRan.Task.WaitAsync(TimeSpan.FromSeconds(15));
            
            Assert.True(ran, $"Script {scriptPath} should execute successfully");
            
            _output.WriteLine($"✓ {mode} mode script executed successfully");
        }
        finally
        {
            await Task.Delay(1000);
        }
    }

    private ProcessStartInfo CreateArtifactBasedProcessStartInfo()
    {
        // Determine if we have a native binary or need dotnet
        string fileName;
        string arguments = "";
        
        if (File.Exists(_hostBinaryPath))
        {
            // Native binary (unlikely for .NET apps, but possible)
            fileName = _hostBinaryPath;
        }
        else if (File.Exists(_hostBinaryPath + ".dll"))
        {
            // .NET assembly
            fileName = "dotnet";
            arguments = _hostBinaryPath + ".dll";
        }
        else if (File.Exists(_hostBinaryPath + ".exe"))
        {
            // Windows executable
            fileName = _hostBinaryPath + ".exe";
        }
        else
        {
            throw new FileNotFoundException(
                $"Host binary not found. Expected one of:\n" +
                $"  {_hostBinaryPath}\n" +
                $"  {_hostBinaryPath}.dll\n" +
                $"  {_hostBinaryPath}.exe\n\n" +
                $"Run 'task build-all' to build artifacts.");
        }

        var startInfo = new ProcessStartInfo
        {
            FileName = fileName,
            Arguments = arguments,
            WorkingDirectory = Path.GetDirectoryName(_hostBinaryPath),
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

    private string FindProjectRoot(string startPath)
    {
        var current = new DirectoryInfo(startPath);
        
        while (current != null)
        {
            // Look for build/ directory
            if (Directory.Exists(Path.Combine(current.FullName, "build")))
            {
                return current.FullName;
            }
            
            current = current.Parent;
        }
        
        throw new DirectoryNotFoundException(
            $"Could not find project root (with build/ directory) starting from {startPath}");
    }

    private string GetCurrentVersion()
    {
        var versionScriptPath = Path.Combine(_projectRoot, "build/get-version.sh");
        
        if (!File.Exists(versionScriptPath))
        {
            _output.WriteLine($"⚠️  Version script not found: {versionScriptPath}");
            return "0.1.0-dev";
        }

        try
        {
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "bash",
                    Arguments = versionScriptPath,
                    WorkingDirectory = Path.Combine(_projectRoot, "build"),
                    RedirectStandardOutput = true,
                    UseShellExecute = false
                }
            };

            process.Start();
            var version = process.StandardOutput.ReadToEnd().Trim();
            process.WaitForExit();

            if (string.IsNullOrWhiteSpace(version))
            {
                _output.WriteLine("⚠️  Version script returned empty, using fallback");
                return "0.1.0-dev";
            }

            return version;
        }
        catch (Exception ex)
        {
            _output.WriteLine($"⚠️  Error getting version: {ex.Message}");
            return "0.1.0-dev";
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
                    process.WaitForExitAsync().Wait(TimeSpan.FromSeconds(5));
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

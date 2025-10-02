using System.Diagnostics;

namespace WingedBean.Plugins.AsciinemaRecorder;

/// <summary>
/// Helper class to retrieve GitVersion for artifact paths
/// </summary>
public static class GitVersionHelper
{
    private static string? _cachedVersion;

    /// <summary>
    /// Gets the current semantic version from GitVersion with fallback
    /// </summary>
    public static string GetVersion()
    {
        if (_cachedVersion != null)
        {
            return _cachedVersion;
        }

        try
        {
            // Try to use GitVersion
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "dotnet",
                    Arguments = "gitversion /nofetch /showvariable SemVer",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };

            process.Start();
            var output = process.StandardOutput.ReadToEnd().Trim();
            process.WaitForExit();

            if (process.ExitCode == 0 && !string.IsNullOrWhiteSpace(output))
            {
                _cachedVersion = output;
                return _cachedVersion;
            }
        }
        catch
        {
            // Fall through to fallback
        }

        // Fallback: use git commit hash
        try
        {
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "git",
                    Arguments = "rev-parse --short HEAD",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };

            process.Start();
            var commitHash = process.StandardOutput.ReadToEnd().Trim();
            process.WaitForExit();

            if (process.ExitCode == 0 && !string.IsNullOrWhiteSpace(commitHash))
            {
                _cachedVersion = $"0.1.0-dev+{commitHash}";
                return _cachedVersion;
            }
        }
        catch
        {
            // Fall through to final fallback
        }

        // Final fallback
        _cachedVersion = "0.1.0-dev+unknown";
        return _cachedVersion;
    }

    /// <summary>
    /// Gets the versioned artifacts recordings directory path
    /// </summary>
    public static string GetRecordingsDirectory()
    {
        var version = GetVersion();
        
        // Find repository root by looking for .git directory
        var repoRoot = FindRepositoryRoot();
        if (repoRoot != null)
        {
            return Path.Combine(repoRoot, "build", "_artifacts", $"v{version}", "dotnet", "recordings");
        }

        // Fallback: use a relative path from current directory
        return Path.Combine(Directory.GetCurrentDirectory(), "build", "_artifacts", $"v{version}", "dotnet", "recordings");
    }

    /// <summary>
    /// Gets the versioned artifacts logs directory path
    /// </summary>
    public static string GetLogsDirectory()
    {
        var version = GetVersion();
        
        // Find repository root by looking for .git directory
        var repoRoot = FindRepositoryRoot();
        if (repoRoot != null)
        {
            return Path.Combine(repoRoot, "build", "_artifacts", $"v{version}", "dotnet", "logs");
        }

        // Fallback: use a relative path from current directory
        return Path.Combine(Directory.GetCurrentDirectory(), "build", "_artifacts", $"v{version}", "dotnet", "logs");
    }

    /// <summary>
    /// Finds the repository root by walking up the directory tree looking for .git
    /// </summary>
    private static string? FindRepositoryRoot()
    {
        var currentDir = Directory.GetCurrentDirectory();
        
        while (currentDir != null)
        {
            if (Directory.Exists(Path.Combine(currentDir, ".git")))
            {
                return currentDir;
            }

            var parent = Directory.GetParent(currentDir);
            currentDir = parent?.FullName;
        }

        return null;
    }
}

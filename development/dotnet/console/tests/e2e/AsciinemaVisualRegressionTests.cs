using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace WingedBean.Tests.E2E.ConsoleDungeon;

/// <summary>
/// Visual regression tests using asciinema recordings.
/// Records TUI interactions and compares against baseline "golden" recordings
/// to detect visual regressions in Terminal.Gui v2 applications.
/// </summary>
public class AsciinemaVisualRegressionTests : IDisposable
{
    private readonly ITestOutputHelper _output;
    private readonly string _hostPath;
    private readonly string _recordingsPath;
    private readonly string _baselinesPath;
    private readonly List<Process> _processesToClean = new();

    public AsciinemaVisualRegressionTests(ITestOutputHelper output)
    {
        _output = output;
        var currentDir = Directory.GetCurrentDirectory();
        _hostPath = Path.GetFullPath(Path.Combine(currentDir, "../../../src/host/ConsoleDungeon.Host"));
        _recordingsPath = Path.Combine(currentDir, "../recordings/test-runs");
        _baselinesPath = Path.Combine(currentDir, "../recordings/baselines");
        
        Directory.CreateDirectory(_recordingsPath);
        Directory.CreateDirectory(_baselinesPath);
        
        _output.WriteLine($"Host directory: {_hostPath}");
        _output.WriteLine($"Recordings: {_recordingsPath}");
        _output.WriteLine($"Baselines: {_baselinesPath}");
    }

    [Fact(DisplayName = "Startup screen should match baseline")]
    [Trait("Category", "E2E")]
    [Trait("Type", "Visual")]
    [Trait("TUI", "Regression")]
    public async Task TUI_StartupScreen_MatchesBaseline()
    {
        var testName = "startup-screen";
        var recordingPath = await RecordTUISession(testName, async (expectScript) =>
        {
            // Wait for startup
            expectScript.AppendLine("expect \"ConsoleDungeon\" { send_log \"✓ Startup screen displayed\\n\" }");
            expectScript.AppendLine("sleep 2");
            expectScript.AppendLine("send \"q\"");
        });

        // Compare with baseline
        var baselinePath = Path.Combine(_baselinesPath, $"{testName}.cast");
        
        if (!File.Exists(baselinePath))
        {
            // First run - save as baseline
            File.Copy(recordingPath, baselinePath);
            _output.WriteLine($"⚠️  No baseline found. Saved current recording as baseline: {baselinePath}");
            _output.WriteLine("Run test again to perform actual comparison.");
            return;
        }

        // Compare recordings
        var diff = await CompareAsciinemaRecordings(baselinePath, recordingPath);
        
        _output.WriteLine($"Visual diff score: {diff.SimilarityScore:P2}");
        _output.WriteLine($"Frame differences: {diff.DifferentFrames}/{diff.TotalFrames}");
        
        if (diff.DifferentFrames > 0)
        {
            _output.WriteLine("\nDifferences found:");
            foreach (var frameDiff in diff.FrameDifferences.Take(5))
            {
                _output.WriteLine($"  Frame {frameDiff.FrameNumber}: {frameDiff.Description}");
            }
        }

        // Assert similarity threshold (90% similar is acceptable for TUI tests)
        Assert.True(diff.SimilarityScore >= 0.90, 
            $"Visual regression detected! Similarity: {diff.SimilarityScore:P2} (expected >= 90%)");
    }

    [Fact(DisplayName = "Menu navigation should render correctly")]
    [Trait("Category", "E2E")]
    [Trait("Type", "Visual")]
    [Trait("TUI", "Regression")]
    public async Task TUI_MenuNavigation_MatchesBaseline()
    {
        var testName = "menu-navigation";
        var recordingPath = await RecordTUISession(testName, async (expectScript) =>
        {
            // Navigate menus
            expectScript.AppendLine("expect \"ConsoleDungeon\"");
            expectScript.AppendLine("sleep 1");
            expectScript.AppendLine("send \"\\t\"");  // Tab to menu
            expectScript.AppendLine("sleep 0.5");
            expectScript.AppendLine("send \"\\033\\[B\"");  // Down arrow
            expectScript.AppendLine("sleep 0.5");
            expectScript.AppendLine("send \"\\033\\[B\"");  // Down arrow
            expectScript.AppendLine("sleep 0.5");
            expectScript.AppendLine("send \"\\r\"");  // Enter
            expectScript.AppendLine("sleep 1");
            expectScript.AppendLine("send \"q\"");  // Quit
        });

        var baselinePath = Path.Combine(_baselinesPath, $"{testName}.cast");
        
        if (!File.Exists(baselinePath))
        {
            File.Copy(recordingPath, baselinePath);
            _output.WriteLine($"⚠️  No baseline found. Saved as baseline: {baselinePath}");
            return;
        }

        var diff = await CompareAsciinemaRecordings(baselinePath, recordingPath);
        
        _output.WriteLine($"Menu navigation visual diff: {diff.SimilarityScore:P2}");
        
        Assert.True(diff.SimilarityScore >= 0.90, 
            $"Menu rendering changed! Similarity: {diff.SimilarityScore:P2}");
    }

    [Fact(DisplayName = "Error dialog should display correctly")]
    [Trait("Category", "E2E")]
    [Trait("Type", "Visual")]
    [Trait("TUI", "Regression")]
    public async Task TUI_ErrorDialog_MatchesBaseline()
    {
        var testName = "error-dialog";
        var recordingPath = await RecordTUISession(testName, async (expectScript) =>
        {
            // Trigger error (if supported)
            expectScript.AppendLine("expect \"ConsoleDungeon\"");
            expectScript.AppendLine("sleep 1");
            expectScript.AppendLine("send \"e\"");  // Trigger error (if mapped)
            expectScript.AppendLine("sleep 1");
            expectScript.AppendLine("send \"\\r\"");  // Dismiss
            expectScript.AppendLine("sleep 0.5");
            expectScript.AppendLine("send \"q\"");
        });

        var baselinePath = Path.Combine(_baselinesPath, $"{testName}.cast");
        
        if (!File.Exists(baselinePath))
        {
            File.Copy(recordingPath, baselinePath);
            _output.WriteLine($"⚠️  No baseline found. Saved as baseline: {baselinePath}");
            return;
        }

        var diff = await CompareAsciinemaRecordings(baselinePath, recordingPath);
        
        _output.WriteLine($"Error dialog visual diff: {diff.SimilarityScore:P2}");
        
        Assert.True(diff.SimilarityScore >= 0.85, 
            $"Error dialog rendering changed! Similarity: {diff.SimilarityScore:P2}");
    }

    [Theory(DisplayName = "Key input scenarios should match baselines")]
    [InlineData("arrow-keys", "Arrow key navigation")]
    [InlineData("wasd-keys", "WASD movement")]
    [InlineData("function-keys", "Function key actions")]
    [Trait("Category", "E2E")]
    [Trait("Type", "Visual")]
    [Trait("TUI", "Regression")]
    public async Task TUI_KeyInputScenarios_MatchBaselines(string scenarioName, string description)
    {
        _output.WriteLine($"Testing scenario: {description}");
        
        var testName = scenarioName;
        var recordingPath = await RecordTUISession(testName, async (expectScript) =>
        {
            expectScript.AppendLine("expect \"ConsoleDungeon\"");
            expectScript.AppendLine("sleep 1");
            
            // Different key sequences per scenario
            switch (scenarioName)
            {
                case "arrow-keys":
                    expectScript.AppendLine("send \"\\033\\[C\"");  // Right
                    expectScript.AppendLine("sleep 0.3");
                    expectScript.AppendLine("send \"\\033\\[B\"");  // Down
                    expectScript.AppendLine("sleep 0.3");
                    expectScript.AppendLine("send \"\\033\\[D\"");  // Left
                    expectScript.AppendLine("sleep 0.3");
                    expectScript.AppendLine("send \"\\033\\[A\"");  // Up
                    break;
                    
                case "wasd-keys":
                    expectScript.AppendLine("send \"d\"");
                    expectScript.AppendLine("sleep 0.3");
                    expectScript.AppendLine("send \"s\"");
                    expectScript.AppendLine("sleep 0.3");
                    expectScript.AppendLine("send \"a\"");
                    expectScript.AppendLine("sleep 0.3");
                    expectScript.AppendLine("send \"w\"");
                    break;
                    
                case "function-keys":
                    expectScript.AppendLine("send \"\\033OP\"");  // F1
                    expectScript.AppendLine("sleep 0.5");
                    expectScript.AppendLine("send \"\\r\"");  // Dismiss if dialog
                    break;
            }
            
            expectScript.AppendLine("sleep 1");
            expectScript.AppendLine("send \"q\"");
        });

        var baselinePath = Path.Combine(_baselinesPath, $"{testName}.cast");
        
        if (!File.Exists(baselinePath))
        {
            File.Copy(recordingPath, baselinePath);
            _output.WriteLine($"⚠️  No baseline found. Saved as baseline: {baselinePath}");
            return;
        }

        var diff = await CompareAsciinemaRecordings(baselinePath, recordingPath);
        
        _output.WriteLine($"{description} visual diff: {diff.SimilarityScore:P2}");
        
        Assert.True(diff.SimilarityScore >= 0.90, 
            $"{description} rendering changed! Similarity: {diff.SimilarityScore:P2}");
    }

    private async Task<string> RecordTUISession(string testName, Func<StringBuilder, Task> scriptBuilder)
    {
        var recordingFile = Path.Combine(_recordingsPath, $"{testName}-{DateTime.Now:yyyyMMdd-HHmmss}.cast");
        var expectScriptPath = Path.Combine(_recordingsPath, $"{testName}.exp");
        
        // Build expect script
        var expectScript = new StringBuilder();
        expectScript.AppendLine("#!/usr/bin/expect -f");
        expectScript.AppendLine("set timeout 30");
        expectScript.AppendLine($"spawn asciinema rec {recordingFile} -c \"dotnet run --no-build\"");
        expectScript.AppendLine("log_user 0");
        
        await scriptBuilder(expectScript);
        
        expectScript.AppendLine("expect eof");
        
        await File.WriteAllTextAsync(expectScriptPath, expectScript.ToString());
        
        // Make executable
        var chmod = Process.Start("chmod", $"+x {expectScriptPath}");
        await chmod.WaitForExitAsync();
        
        // Run expect script
        var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = expectScriptPath,
                WorkingDirectory = _hostPath,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false
            }
        };
        
        _processesToClean.Add(process);
        
        var output = new StringBuilder();
        process.OutputDataReceived += (s, e) =>
        {
            if (e.Data != null)
            {
                output.AppendLine(e.Data);
                _output.WriteLine($"[REC] {e.Data}");
            }
        };
        
        process.ErrorDataReceived += (s, e) =>
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
        
        await process.WaitForExitAsync();
        
        if (!File.Exists(recordingFile))
        {
            throw new Exception($"Recording failed: {recordingFile} not created\n{output}");
        }
        
        _output.WriteLine($"✓ Recording saved: {recordingFile}");
        return recordingFile;
    }

    private async Task<AsciinemaComparison> CompareAsciinemaRecordings(string baselinePath, string recordingPath)
    {
        // Parse both cast files
        var baseline = await ParseCastFile(baselinePath);
        var recording = await ParseCastFile(recordingPath);
        
        var result = new AsciinemaComparison
        {
            TotalFrames = Math.Max(baseline.Frames.Count, recording.Frames.Count)
        };
        
        // Compare frames
        var minFrames = Math.Min(baseline.Frames.Count, recording.Frames.Count);
        var matchingFrames = 0;
        
        for (int i = 0; i < minFrames; i++)
        {
            var baselineFrame = baseline.Frames[i];
            var recordingFrame = recording.Frames[i];
            
            // Normalize output (strip timing variations, normalize whitespace)
            var baselineOutput = NormalizeOutput(baselineFrame.Output);
            var recordingOutput = NormalizeOutput(recordingFrame.Output);
            
            if (baselineOutput == recordingOutput)
            {
                matchingFrames++;
            }
            else
            {
                result.DifferentFrames++;
                
                // Calculate similarity for this frame
                var similarity = CalculateSimilarity(baselineOutput, recordingOutput);
                
                if (similarity < 0.95) // Only report significant differences
                {
                    result.FrameDifferences.Add(new FrameDifference
                    {
                        FrameNumber = i,
                        Description = $"Content differs (similarity: {similarity:P2})",
                        BaselineContent = baselineOutput.Length > 100 
                            ? baselineOutput.Substring(0, 100) + "..." 
                            : baselineOutput,
                        RecordingContent = recordingOutput.Length > 100 
                            ? recordingOutput.Substring(0, 100) + "..." 
                            : recordingOutput
                    });
                }
            }
        }
        
        // Handle different frame counts
        if (baseline.Frames.Count != recording.Frames.Count)
        {
            result.DifferentFrames += Math.Abs(baseline.Frames.Count - recording.Frames.Count);
            result.FrameDifferences.Add(new FrameDifference
            {
                FrameNumber = minFrames,
                Description = $"Frame count differs: baseline={baseline.Frames.Count}, recording={recording.Frames.Count}"
            });
        }
        
        result.SimilarityScore = result.TotalFrames > 0 
            ? (double)matchingFrames / result.TotalFrames 
            : 0.0;
        
        return result;
    }

    private async Task<CastFile> ParseCastFile(string path)
    {
        var lines = await File.ReadAllLinesAsync(path);
        var cast = new CastFile();
        
        foreach (var line in lines)
        {
            if (line.StartsWith("{\"version\":"))
            {
                // Header line
                var header = JsonSerializer.Deserialize<CastHeader>(line);
                cast.Header = header ?? new CastHeader();
            }
            else if (line.StartsWith("["))
            {
                // Frame line: [timestamp, "o", "output"]
                var frame = JsonSerializer.Deserialize<JsonElement>(line);
                if (frame.ValueKind == JsonValueKind.Array && frame.GetArrayLength() >= 3)
                {
                    cast.Frames.Add(new CastFrame
                    {
                        Timestamp = frame[0].GetDouble(),
                        Type = frame[1].GetString() ?? "",
                        Output = frame[2].GetString() ?? ""
                    });
                }
            }
        }
        
        return cast;
    }

    private string NormalizeOutput(string output)
    {
        // Strip ANSI color codes
        output = Regex.Replace(output, @"\x1B\[[0-9;]*[mGKH]", "");
        
        // Normalize whitespace
        output = Regex.Replace(output, @"\s+", " ");
        
        // Remove timing-dependent content (timestamps, etc.)
        output = Regex.Replace(output, @"\d{2}:\d{2}:\d{2}", "HH:MM:SS");
        output = Regex.Replace(output, @"\d{4}-\d{2}-\d{2}", "YYYY-MM-DD");
        
        return output.Trim();
    }

    private double CalculateSimilarity(string text1, string text2)
    {
        // Simple Levenshtein distance-based similarity
        var maxLen = Math.Max(text1.Length, text2.Length);
        if (maxLen == 0) return 1.0;
        
        var distance = LevenshteinDistance(text1, text2);
        return 1.0 - ((double)distance / maxLen);
    }

    private int LevenshteinDistance(string s1, string s2)
    {
        var len1 = s1.Length;
        var len2 = s2.Length;
        var matrix = new int[len1 + 1, len2 + 1];
        
        for (int i = 0; i <= len1; i++) matrix[i, 0] = i;
        for (int j = 0; j <= len2; j++) matrix[0, j] = j;
        
        for (int i = 1; i <= len1; i++)
        {
            for (int j = 1; j <= len2; j++)
            {
                var cost = s1[i - 1] == s2[j - 1] ? 0 : 1;
                matrix[i, j] = Math.Min(
                    Math.Min(matrix[i - 1, j] + 1, matrix[i, j - 1] + 1),
                    matrix[i - 1, j - 1] + cost
                );
            }
        }
        
        return matrix[len1, len2];
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

// DTOs for asciinema cast file parsing
public class CastFile
{
    public CastHeader Header { get; set; } = new();
    public List<CastFrame> Frames { get; set; } = new();
}

public class CastHeader
{
    public int Version { get; set; } = 2;
    public int Width { get; set; } = 80;
    public int Height { get; set; } = 24;
}

public class CastFrame
{
    public double Timestamp { get; set; }
    public string Type { get; set; } = "";
    public string Output { get; set; } = "";
}

public class AsciinemaComparison
{
    public int TotalFrames { get; set; }
    public int DifferentFrames { get; set; }
    public double SimilarityScore { get; set; }
    public List<FrameDifference> FrameDifferences { get; set; } = new();
}

public class FrameDifference
{
    public int FrameNumber { get; set; }
    public string Description { get; set; } = "";
    public string? BaselineContent { get; set; }
    public string? RecordingContent { get; set; }
}

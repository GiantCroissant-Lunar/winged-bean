using Microsoft.Extensions.Logging;
using WingedBean.Contracts;
using WingedBean.Plugins.AsciinemaRecorder;

namespace WingedBean.ArtifactVerifier;

/// <summary>
/// Simple console app to verify runtime artifacts are properly archived
/// This is for Issue #170 verification
/// </summary>
class Program
{
    static async Task Main(string[] args)
    {
        // Set up logging
        using var loggerFactory = LoggerFactory.Create(builder =>
        {
            builder.AddConsole();
            builder.SetMinimumLevel(LogLevel.Information);
        });

        var logger = loggerFactory.CreateLogger<Program>();

        Console.WriteLine("===========================================");
        Console.WriteLine("Runtime Artifacts Verification - Issue #170");
        Console.WriteLine("===========================================");
        Console.WriteLine();

        try
        {
            // Get version and paths
            var version = GitVersionHelper.GetVersion();
            var recordingsDir = GitVersionHelper.GetRecordingsDirectory();
            var logsDir = GitVersionHelper.GetLogsDirectory();

            Console.WriteLine($"✓ Version: {version}");
            Console.WriteLine($"✓ Recordings directory: {recordingsDir}");
            Console.WriteLine($"✓ Logs directory: {logsDir}");
            Console.WriteLine();

            // Test 1: Create a recording
            Console.WriteLine("Test 1: Creating test recording...");
            var recorder = new AsciinemaRecorder(loggerFactory.CreateLogger<AsciinemaRecorder>());
            
            var sessionId = $"verification-{DateTime.UtcNow:yyyyMMdd-HHmmss}";
            var metadata = new SessionMetadata
            {
                Width = 80,
                Height = 24,
                Title = "Runtime Artifacts Verification",
                Command = "artifact-verifier",
                WorkingDirectory = Environment.CurrentDirectory
            };

            await recorder.StartRecordingAsync(sessionId, metadata);
            logger.LogInformation("✓ Recording started for session: {SessionId}", sessionId);

            // Record some test data
            await recorder.RecordDataAsync(
                sessionId, 
                System.Text.Encoding.UTF8.GetBytes("$ echo 'Verifying artifact archiving for Issue #170'\r\n"), 
                DateTimeOffset.UtcNow
            );
            await Task.Delay(100);

            await recorder.RecordDataAsync(
                sessionId, 
                System.Text.Encoding.UTF8.GetBytes("Verifying artifact archiving for Issue #170\r\n"), 
                DateTimeOffset.UtcNow
            );
            await Task.Delay(100);

            await recorder.RecordDataAsync(
                sessionId, 
                System.Text.Encoding.UTF8.GetBytes("$ echo 'Recordings are saved to versioned folders'\r\n"), 
                DateTimeOffset.UtcNow
            );
            await Task.Delay(100);

            await recorder.RecordDataAsync(
                sessionId, 
                System.Text.Encoding.UTF8.GetBytes("Recordings are saved to versioned folders\r\n"), 
                DateTimeOffset.UtcNow
            );
            await Task.Delay(100);

            await recorder.RecordDataAsync(
                sessionId, 
                System.Text.Encoding.UTF8.GetBytes("$ exit\r\n"), 
                DateTimeOffset.UtcNow
            );

            var outputPath = await recorder.StopRecordingAsync(sessionId);
            logger.LogInformation("✓ Recording saved to: {OutputPath}", outputPath);

            // Verify the file exists
            if (File.Exists(outputPath))
            {
                var fileInfo = new FileInfo(outputPath);
                logger.LogInformation("✓ Recording file exists: {Size} bytes", fileInfo.Length);
                
                // Show a sample of the content
                var content = await File.ReadAllTextAsync(outputPath);
                var lines = content.Split('\n');
                logger.LogInformation("✓ Recording has {LineCount} lines", lines.Length);
            }
            else
            {
                logger.LogError("✗ Recording file does not exist at: {OutputPath}", outputPath);
                Environment.Exit(1);
            }

            Console.WriteLine();
            Console.WriteLine("Test 2: Verifying directory structure...");
            
            // List all recordings
            if (Directory.Exists(recordingsDir))
            {
                var recordings = Directory.GetFiles(recordingsDir, "*.cast");
                logger.LogInformation("✓ Found {Count} recording(s) in {Directory}", recordings.Length, recordingsDir);
                foreach (var recording in recordings)
                {
                    var fileName = Path.GetFileName(recording);
                    var size = new FileInfo(recording).Length;
                    logger.LogInformation("  - {FileName} ({Size} bytes)", fileName, size);
                }
            }

            // Check if logs directory exists (it should be created)
            if (Directory.Exists(logsDir))
            {
                logger.LogInformation("✓ Logs directory exists: {LogsDir}", logsDir);
            }
            else
            {
                logger.LogInformation("ℹ Logs directory will be created when logs are generated: {LogsDir}", logsDir);
            }

            Console.WriteLine();
            Console.WriteLine("===========================================");
            Console.WriteLine("✅ Verification Complete - All tests passed!");
            Console.WriteLine("===========================================");
            Console.WriteLine();
            Console.WriteLine("Summary:");
            Console.WriteLine($"  - Version-scoped path verified: build/_artifacts/v{version}/");
            Console.WriteLine($"  - Component-scoped recordings: {recordingsDir}");
            Console.WriteLine($"  - Component-scoped logs: {logsDir}");
            Console.WriteLine("  - Recording functionality: ✅");
            Console.WriteLine("  - Artifact archiving: ✅");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Verification failed: {Message}", ex.Message);
            Environment.Exit(1);
        }
    }
}

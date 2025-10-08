using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Plate.CrossMilo.Contracts.Recorder;
using Xunit;
using IRecorder = Plate.CrossMilo.Contracts.Recorder.Services.IService;

namespace WingedBean.Plugins.AsciinemaRecorder.Tests;

/// <summary>
/// Unit tests for AsciinemaRecorder.
/// </summary>
public class AsciinemaRecorderTests
{
    [Fact]
    public void Constructor_CreatesRecordingsDirectory()
    {
        // Arrange & Act
        var logger = NullLogger<AsciinemaRecorder>.Instance;
        var recorder = new AsciinemaRecorder(logger);

        // Assert
        recorder.Should().NotBeNull();
        recorder.Should().BeAssignableTo<IRecorder>();
        
        // Verify that recordings directory is created
        var version = GitVersionHelper.GetVersion();
        var recordingsDir = GitVersionHelper.GetRecordingsDirectory();
        
        recordingsDir.Should().Contain("build/_artifacts");
        recordingsDir.Should().Contain($"v{version}");
        recordingsDir.Should().Contain("dotnet/recordings");
        Directory.Exists(recordingsDir).Should().BeTrue();
    }

    [Fact]
    public async Task StartRecordingAsync_CreatesRecordingFile()
    {
        // Arrange
        var logger = NullLogger<AsciinemaRecorder>.Instance;
        var recorder = new AsciinemaRecorder(logger);
        var sessionId = $"test-{Guid.NewGuid()}";
        var metadata = new SessionMetadata
        {
            Width = 80,
            Height = 24,
            Title = "Test Recording"
        };

        // Act
        await recorder.StartRecordingAsync(sessionId, metadata);

        // Assert - file should exist in versioned artifacts directory
        var recordingsDir = GitVersionHelper.GetRecordingsDirectory();
        var files = Directory.GetFiles(recordingsDir, $"{sessionId}_*.cast");
        files.Should().NotBeEmpty();

        // Cleanup
        await recorder.StopRecordingAsync(sessionId);
        foreach (var file in files)
        {
            File.Delete(file);
        }
    }

    [Fact]
    public async Task RecordDataAsync_AppendsToRecordingFile()
    {
        // Arrange
        var logger = NullLogger<AsciinemaRecorder>.Instance;
        var recorder = new AsciinemaRecorder(logger);
        var sessionId = $"test-{Guid.NewGuid()}";
        var metadata = new SessionMetadata
        {
            Width = 80,
            Height = 24,
            Title = "Test Recording"
        };

        await recorder.StartRecordingAsync(sessionId, metadata);

        // Act
        var testData = System.Text.Encoding.UTF8.GetBytes("Hello, World!\n");
        await recorder.RecordDataAsync(sessionId, testData, DateTimeOffset.UtcNow);

        // Assert
        var outputPath = await recorder.StopRecordingAsync(sessionId);
        outputPath.Should().Contain(GitVersionHelper.GetRecordingsDirectory());
        
        var content = await File.ReadAllTextAsync(outputPath);
        content.Should().Contain("Hello, World!");

        // Cleanup
        File.Delete(outputPath);
    }

    [Fact]
    public async Task StopRecordingAsync_ReturnsOutputPath()
    {
        // Arrange
        var logger = NullLogger<AsciinemaRecorder>.Instance;
        var recorder = new AsciinemaRecorder(logger);
        var sessionId = $"test-{Guid.NewGuid()}";
        var metadata = new SessionMetadata
        {
            Width = 80,
            Height = 24,
            Title = "Test Recording"
        };

        await recorder.StartRecordingAsync(sessionId, metadata);

        // Act
        var outputPath = await recorder.StopRecordingAsync(sessionId);

        // Assert
        outputPath.Should().NotBeNullOrEmpty();
        outputPath.Should().Contain(GitVersionHelper.GetRecordingsDirectory());
        outputPath.Should().EndWith(".cast");
        File.Exists(outputPath).Should().BeTrue();

        // Cleanup
        File.Delete(outputPath);
    }

    [Fact]
    public void GetRecordingsDirectory_UsesVersionedArtifactsPath()
    {
        // Act
        var recordingsDir = GitVersionHelper.GetRecordingsDirectory();

        // Assert
        recordingsDir.Should().Contain("build/_artifacts");
        recordingsDir.Should().Contain("/dotnet/recordings");
        recordingsDir.Should().Match(pattern => pattern.Contains("v0.") || pattern.Contains("v1."));
    }

    [Fact]
    public void GetVersion_ReturnsValidSemanticVersion()
    {
        // Act
        var version = GitVersionHelper.GetVersion();

        // Assert
        version.Should().NotBeNullOrEmpty();
        version.Should().MatchRegex(@"^\d+\.\d+\.\d+");
    }
}

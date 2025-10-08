using System.IO.Compression;
using System.Text.Json;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Plate.CrossMilo.Contracts.Resource;
using IResourceService = Plate.CrossMilo.Contracts.Resource.Services.IService;
using Xunit;

namespace WingedBean.Plugins.Resource.Tests;

/// <summary>
/// Tests for resource bundle functionality.
/// </summary>
public class ResourceBundleTests : IDisposable
{
    private readonly string _testPath;
    private readonly string _bundlePath;
    private readonly ILogger<FileSystemResourceService> _logger;

    public ResourceBundleTests()
    {
        _testPath = Path.Combine(Path.GetTempPath(), $"wingedbean-bundle-test-{Guid.NewGuid()}");
        Directory.CreateDirectory(_testPath);
        _bundlePath = Path.Combine(_testPath, "test.wbundle");
        _logger = NullLogger<FileSystemResourceService>.Instance;
    }

    public void Dispose()
    {
        if (Directory.Exists(_testPath))
        {
            Directory.Delete(_testPath, recursive: true);
        }
    }

    [Fact]
    public async Task BuildBundle_WithResources_CreatesValidBundle()
    {
        // Arrange
        var dataDir = Path.Combine(_testPath, "data");
        Directory.CreateDirectory(dataDir);
        
        var testFile = Path.Combine(dataDir, "test.json");
        await File.WriteAllTextAsync(testFile, "{\"name\":\"Test\",\"value\":42}");

        // Act
        var builder = new ResourceBundleBuilder("test-bundle", "1.0.0")
            .WithMetadata(
                name: "Test Bundle",
                description: "A test bundle",
                author: "Test Author"
            )
            .AddResource(testFile, "data/test", type: "data", tags: new[] { "test" });

        await builder.BuildAsync(_bundlePath);

        // Assert
        File.Exists(_bundlePath).Should().BeTrue();

        // Verify bundle structure
        using var archive = ZipFile.OpenRead(_bundlePath);
        archive.GetEntry("manifest.json").Should().NotBeNull();
        archive.GetEntry("resources/data/test").Should().NotBeNull();
    }

    [Fact]
    public async Task BuildBundle_WithDirectory_AddsAllFiles()
    {
        // Arrange
        var dataDir = Path.Combine(_testPath, "data");
        Directory.CreateDirectory(dataDir);
        
        await File.WriteAllTextAsync(Path.Combine(dataDir, "file1.json"), "{}");
        await File.WriteAllTextAsync(Path.Combine(dataDir, "file2.json"), "{}");

        // Act
        var builder = new ResourceBundleBuilder("test-bundle")
            .AddDirectory(dataDir, resourcePrefix: "data", filePatterns: new[] { "*.json" });

        await builder.BuildAsync(_bundlePath);

        // Assert
        using var archive = ZipFile.OpenRead(_bundlePath);
        archive.Entries.Count.Should().BeGreaterThan(2); // manifest + 2 files
    }

    [Fact]
    public async Task FileSystemResourceService_LoadsFromBundle()
    {
        // Arrange
        await CreateTestBundle();
        
        var resourcesDir = Path.Combine(_testPath, "resources");
        Directory.CreateDirectory(resourcesDir);
        File.Move(_bundlePath, Path.Combine(resourcesDir, "test.wbundle"));

        var service = new FileSystemResourceService(_logger, _testPath);

        // Act
        var result = await service.LoadAsync<TestData>("data/test");

        // Assert
        result.Should().NotBeNull();
        result!.Name.Should().Be("Test");
        result.Value.Should().Be(42);
    }

    [Fact]
    public async Task FileSystemResourceService_FallsBackToFile_WhenNotInBundle()
    {
        // Arrange
        await CreateTestBundle();
        
        var resourcesDir = Path.Combine(_testPath, "resources");
        Directory.CreateDirectory(resourcesDir);
        File.Move(_bundlePath, Path.Combine(resourcesDir, "test.wbundle"));

        // Create an individual file (not in bundle)
        var individualFile = Path.Combine(_testPath, "individual.json");
        await File.WriteAllTextAsync(individualFile, "{\"name\":\"Individual\",\"value\":99}");

        var service = new FileSystemResourceService(_logger, _testPath);

        // Act
        var result = await service.LoadAsync<TestData>("individual.json");

        // Assert
        result.Should().NotBeNull();
        result!.Name.Should().Be("Individual");
        result.Value.Should().Be(99);
    }

    [Fact]
    public async Task FileSystemResourceService_PrioritizesBundleOverFile()
    {
        // Arrange
        await CreateTestBundle();
        
        var resourcesDir = Path.Combine(_testPath, "resources");
        Directory.CreateDirectory(resourcesDir);
        File.Move(_bundlePath, Path.Combine(resourcesDir, "test.wbundle"));

        // Create an individual file with different data
        var individualFile = Path.Combine(_testPath, "data", "test.json");
        Directory.CreateDirectory(Path.GetDirectoryName(individualFile)!);
        await File.WriteAllTextAsync(individualFile, "{\"name\":\"File\",\"value\":999}");

        var service = new FileSystemResourceService(_logger, _testPath);

        // Act - should load from bundle, not file
        var result = await service.LoadAsync<TestData>("data/test");

        // Assert
        result.Should().NotBeNull();
        result!.Name.Should().Be("Test"); // Bundle value
        result.Value.Should().Be(42); // Bundle value
    }

    private async Task CreateTestBundle()
    {
        var dataDir = Path.Combine(_testPath, "temp-data");
        Directory.CreateDirectory(dataDir);
        
        var testFile = Path.Combine(dataDir, "test.json");
        await File.WriteAllTextAsync(testFile, "{\"name\":\"Test\",\"value\":42}");

        var builder = new ResourceBundleBuilder("test-bundle", "1.0.0")
            .WithMetadata(name: "Test Bundle", description: "Test")
            .AddResource(testFile, "data/test", type: "data");

        await builder.BuildAsync(_bundlePath);

        // Clean up temp data directory
        Directory.Delete(dataDir, recursive: true);
    }

    private class TestData
    {
        public string Name { get; set; } = string.Empty;
        public int Value { get; set; }
    }
}

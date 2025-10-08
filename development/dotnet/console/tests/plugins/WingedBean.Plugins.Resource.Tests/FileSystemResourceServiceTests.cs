using System.Text.Json;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Plate.CrossMilo.Contracts.Resource;
using IResourceService = Plate.CrossMilo.Contracts.Resource.Services.IService;
using Xunit;

namespace WingedBean.Plugins.Resource.Tests;

/// <summary>
/// Unit tests for FileSystemResourceService.
/// </summary>
public class FileSystemResourceServiceTests : IDisposable
{
    private readonly string _testResourcePath;
    private readonly ILogger<FileSystemResourceService> _logger;
    private readonly FileSystemResourceService _service;

    public FileSystemResourceServiceTests()
    {
        // Create a temporary directory for test resources
        _testResourcePath = Path.Combine(Path.GetTempPath(), $"wingedbean-test-{Guid.NewGuid()}");
        Directory.CreateDirectory(_testResourcePath);

        _logger = NullLogger<FileSystemResourceService>.Instance;
        _service = new FileSystemResourceService(_logger, _testResourcePath);
    }

    public void Dispose()
    {
        // Clean up test directory
        if (Directory.Exists(_testResourcePath))
        {
            Directory.Delete(_testResourcePath, recursive: true);
        }
    }

    [Fact]
    public void Constructor_Default_CreatesInstance()
    {
        // Act
        var service = new FileSystemResourceService(_logger);

        // Assert
        service.Should().NotBeNull();
        service.Should().BeAssignableTo<IResourceService>();
    }

    [Fact]
    public void Constructor_CreatesBaseDirectory()
    {
        // Assert
        Directory.Exists(_testResourcePath).Should().BeTrue();
    }

    [Fact]
    public async Task LoadAsync_ValidJsonResource_ReturnsDeserialized()
    {
        // Arrange
        var testData = new TestData { Name = "Test", Value = 42 };
        var jsonPath = Path.Combine(_testResourcePath, "test.json");
        await File.WriteAllTextAsync(jsonPath, JsonSerializer.Serialize(testData));

        // Act
        var result = await _service.LoadAsync<TestData>("test.json");

        // Assert
        result.Should().NotBeNull();
        result!.Name.Should().Be("Test");
        result.Value.Should().Be(42);
    }

    [Fact]
    public async Task LoadAsync_ValidTextResource_ReturnsString()
    {
        // Arrange
        var testContent = "Hello, World!";
        var textPath = Path.Combine(_testResourcePath, "test.txt");
        await File.WriteAllTextAsync(textPath, testContent);

        // Act
        var result = await _service.LoadAsync<string>("test.txt");

        // Assert
        result.Should().NotBeNull();
        result.Should().Be(testContent);
    }

    [Fact]
    public async Task LoadAsync_NonExistentResource_ReturnsNull()
    {
        // Act
        var result = await _service.LoadAsync<TestData>("nonexistent.json");

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task LoadAsync_CachedResource_UsesCache()
    {
        // Arrange
        var testData = new TestData { Name = "Cached", Value = 99 };
        var jsonPath = Path.Combine(_testResourcePath, "cached.json");
        await File.WriteAllTextAsync(jsonPath, JsonSerializer.Serialize(testData));

        // Act
        var first = await _service.LoadAsync<TestData>("cached.json");
        var second = await _service.LoadAsync<TestData>("cached.json");

        // Assert
        first.Should().NotBeNull();
        second.Should().NotBeNull();
        first.Should().BeSameAs(second); // Should be same instance from cache
    }

    [Fact]
    public async Task LoadAllAsync_WithPattern_ReturnsMatchingResources()
    {
        // Arrange
        var dataDir = Path.Combine(_testResourcePath, "data");
        Directory.CreateDirectory(dataDir);

        var testData1 = new TestData { Name = "Item1", Value = 1 };
        var testData2 = new TestData { Name = "Item2", Value = 2 };
        
        await File.WriteAllTextAsync(
            Path.Combine(dataDir, "item1.json"), 
            JsonSerializer.Serialize(testData1)
        );
        await File.WriteAllTextAsync(
            Path.Combine(dataDir, "item2.json"), 
            JsonSerializer.Serialize(testData2)
        );

        // Act
        var results = await _service.LoadAllAsync<TestData>("data/*.json");

        // Assert
        results.Should().NotBeNull();
        results.Should().HaveCount(2);
        results.Should().Contain(x => x.Name == "Item1");
        results.Should().Contain(x => x.Name == "Item2");
    }

    [Fact]
    public async Task LoadAllAsync_NonExistentDirectory_ReturnsEmpty()
    {
        // Act
        var results = await _service.LoadAllAsync<TestData>("nonexistent/*.json");

        // Assert
        results.Should().NotBeNull();
        results.Should().BeEmpty();
    }

    [Fact]
    public async Task IsLoaded_CachedResource_ReturnsTrue()
    {
        // Arrange
        var testData = new TestData { Name = "Test", Value = 1 };
        var jsonPath = Path.Combine(_testResourcePath, "test.json");
        await File.WriteAllTextAsync(jsonPath, JsonSerializer.Serialize(testData));
        await _service.LoadAsync<TestData>("test.json");

        // Act
        var result = _service.IsLoaded("test.json");

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void IsLoaded_NotCachedResource_ReturnsFalse()
    {
        // Act
        var result = _service.IsLoaded("not-loaded.json");

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task Unload_ExistingResource_RemovesFromCache()
    {
        // Arrange
        var testData = new TestData { Name = "Test", Value = 1 };
        var jsonPath = Path.Combine(_testResourcePath, "test.json");
        await File.WriteAllTextAsync(jsonPath, JsonSerializer.Serialize(testData));
        await _service.LoadAsync<TestData>("test.json");

        // Act
        _service.Unload("test.json");

        // Assert
        _service.IsLoaded("test.json").Should().BeFalse();
    }

    [Fact]
    public async Task UnloadAll_RemovesAllResourcesOfType()
    {
        // Arrange
        var testData1 = new TestData { Name = "Test1", Value = 1 };
        var testData2 = new TestData { Name = "Test2", Value = 2 };
        
        await File.WriteAllTextAsync(
            Path.Combine(_testResourcePath, "test1.json"), 
            JsonSerializer.Serialize(testData1)
        );
        await File.WriteAllTextAsync(
            Path.Combine(_testResourcePath, "test2.json"), 
            JsonSerializer.Serialize(testData2)
        );

        await _service.LoadAsync<TestData>("test1.json");
        await _service.LoadAsync<TestData>("test2.json");

        // Act
        _service.UnloadAll<TestData>();

        // Assert
        _service.IsLoaded("test1.json").Should().BeFalse();
        _service.IsLoaded("test2.json").Should().BeFalse();
    }

    [Fact]
    public async Task GetMetadataAsync_ValidResource_ReturnsMetadata()
    {
        // Arrange
        var testContent = "Test content";
        var textPath = Path.Combine(_testResourcePath, "test.txt");
        await File.WriteAllTextAsync(textPath, testContent);

        // Act
        var metadata = await _service.GetMetadataAsync("test.txt");

        // Assert
        metadata.Should().NotBeNull();
        metadata!.Id.Should().Be("test.txt");
        metadata.Name.Should().Be("test");
        metadata.Format.Should().Be("TXT");
        metadata.Type.Should().Be("text");
        metadata.Size.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task GetMetadataAsync_NonExistentResource_ReturnsNull()
    {
        // Act
        var metadata = await _service.GetMetadataAsync("nonexistent.txt");

        // Assert
        metadata.Should().BeNull();
    }

    [Fact]
    public async Task PreloadAsync_MultipleResources_LoadsAll()
    {
        // Arrange
        var testData1 = new TestData { Name = "Test1", Value = 1 };
        var testData2 = new TestData { Name = "Test2", Value = 2 };
        
        await File.WriteAllTextAsync(
            Path.Combine(_testResourcePath, "test1.json"), 
            JsonSerializer.Serialize(testData1)
        );
        await File.WriteAllTextAsync(
            Path.Combine(_testResourcePath, "test2.json"), 
            JsonSerializer.Serialize(testData2)
        );

        var resourceIds = new[] { "test1.json", "test2.json" };

        // Act
        await _service.PreloadAsync(resourceIds);

        // Assert
        _service.IsLoaded("test1.json").Should().BeTrue();
        _service.IsLoaded("test2.json").Should().BeTrue();
    }

    [Fact]
    public async Task LoadAsync_JsonAsString_ReturnsRawJson()
    {
        // Arrange
        var jsonContent = "{\"name\":\"Test\",\"value\":42}";
        var jsonPath = Path.Combine(_testResourcePath, "test.json");
        await File.WriteAllTextAsync(jsonPath, jsonContent);

        // Act
        var result = await _service.LoadAsync<string>("test.json");

        // Assert
        result.Should().NotBeNull();
        result.Should().Be(jsonContent);
    }

    [Fact]
    public async Task LoadAsync_BinaryFile_ReturnsBytes()
    {
        // Arrange
        var binaryData = new byte[] { 0x01, 0x02, 0x03, 0x04 };
        var binPath = Path.Combine(_testResourcePath, "test.bin");
        await File.WriteAllBytesAsync(binPath, binaryData);

        // Act
        var result = await _service.LoadAsync<byte[]>("test.bin");

        // Assert
        result.Should().NotBeNull();
        result.Should().Equal(binaryData);
    }

    // Test data class
    private class TestData
    {
        public string Name { get; set; } = string.Empty;
        public int Value { get; set; }
    }
}

using FluentAssertions;
using Xunit;

namespace WingedBean.Plugins.Resource.Tests;

/// <summary>
/// Unit tests for ResourceCache.
/// </summary>
public class ResourceCacheTests
{
    [Fact]
    public void TryGet_NonExistentResource_ReturnsFalse()
    {
        // Arrange
        var cache = new ResourceCache();

        // Act
        var result = cache.TryGet<string>("nonexistent", out var resource);

        // Assert
        result.Should().BeFalse();
        resource.Should().BeNull();
    }

    [Fact]
    public void Set_ThenTryGet_ReturnsTrue()
    {
        // Arrange
        var cache = new ResourceCache();
        var testData = "Test Resource";

        // Act
        cache.Set("test-id", testData);
        var result = cache.TryGet<string>("test-id", out var resource);

        // Assert
        result.Should().BeTrue();
        resource.Should().Be(testData);
    }

    [Fact]
    public void Set_SameInstance_ReturnsSameInstance()
    {
        // Arrange
        var cache = new ResourceCache();
        var testData = new TestClass { Value = "Test" };

        // Act
        cache.Set("test-id", testData);
        cache.TryGet<TestClass>("test-id", out var resource);

        // Assert
        resource.Should().BeSameAs(testData);
    }

    [Fact]
    public void TryGet_WrongType_ReturnsFalse()
    {
        // Arrange
        var cache = new ResourceCache();
        cache.Set("test-id", "string value");

        // Act
        var result = cache.TryGet<TestClass>("test-id", out var resource);

        // Assert
        result.Should().BeFalse();
        resource.Should().BeNull();
    }

    [Fact]
    public void Remove_ExistingResource_ReturnsTrue()
    {
        // Arrange
        var cache = new ResourceCache();
        cache.Set("test-id", "Test Resource");

        // Act
        var result = cache.Remove("test-id");

        // Assert
        result.Should().BeTrue();
        cache.Contains("test-id").Should().BeFalse();
    }

    [Fact]
    public void Remove_NonExistentResource_ReturnsFalse()
    {
        // Arrange
        var cache = new ResourceCache();

        // Act
        var result = cache.Remove("nonexistent");

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void RemoveAll_ByType_RemovesOnlyMatchingType()
    {
        // Arrange
        var cache = new ResourceCache();
        cache.Set("string1", "Test 1");
        cache.Set("string2", "Test 2");
        cache.Set("testclass1", new TestClass { Value = "Test" });

        // Act
        var count = cache.RemoveAll<string>();

        // Assert
        count.Should().Be(2);
        cache.Contains("string1").Should().BeFalse();
        cache.Contains("string2").Should().BeFalse();
        cache.Contains("testclass1").Should().BeTrue();
    }

    [Fact]
    public void Contains_ExistingResource_ReturnsTrue()
    {
        // Arrange
        var cache = new ResourceCache();
        cache.Set("test-id", "Test Resource");

        // Act
        var result = cache.Contains("test-id");

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void Contains_NonExistentResource_ReturnsFalse()
    {
        // Arrange
        var cache = new ResourceCache();

        // Act
        var result = cache.Contains("nonexistent");

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void Clear_RemovesAllResources()
    {
        // Arrange
        var cache = new ResourceCache();
        cache.Set("test1", "Resource 1");
        cache.Set("test2", "Resource 2");
        cache.Set("test3", "Resource 3");

        // Act
        cache.Clear();

        // Assert
        cache.Count.Should().Be(0);
        cache.Contains("test1").Should().BeFalse();
        cache.Contains("test2").Should().BeFalse();
        cache.Contains("test3").Should().BeFalse();
    }

    [Fact]
    public void Count_ReturnsCorrectCount()
    {
        // Arrange
        var cache = new ResourceCache();
        cache.Set("test1", "Resource 1");
        cache.Set("test2", "Resource 2");

        // Act
        var count = cache.Count;

        // Assert
        count.Should().Be(2);
    }

    [Fact]
    public void Set_OverwriteExisting_UpdatesResource()
    {
        // Arrange
        var cache = new ResourceCache();
        cache.Set("test-id", "Original");

        // Act
        cache.Set("test-id", "Updated");
        cache.TryGet<string>("test-id", out var resource);

        // Assert
        resource.Should().Be("Updated");
    }

    [Fact]
    public async Task ThreadSafety_ConcurrentAccess_WorksCorrectly()
    {
        // Arrange
        var cache = new ResourceCache();
        var tasks = new List<Task>();

        // Act
        for (int i = 0; i < 100; i++)
        {
            var index = i;
            tasks.Add(Task.Run(() =>
            {
                cache.Set($"resource-{index}", $"Value {index}");
            }));
        }

        await Task.WhenAll(tasks);

        // Assert
        cache.Count.Should().Be(100);
        for (int i = 0; i < 100; i++)
        {
            cache.Contains($"resource-{i}").Should().BeTrue();
        }
    }

    private class TestClass
    {
        public string Value { get; set; } = string.Empty;
    }
}

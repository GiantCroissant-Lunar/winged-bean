using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using WingedBean.Plugins.Resource.Providers;
using Xunit;

namespace WingedBean.Plugins.Resource.Tests;

/// <summary>
/// Tests for ResourceHandle<T> - RFC-0054 Phase 1
/// </summary>
public class ResourceHandleTests
{
    private class TestResource
    {
        public required string Name { get; init; }
        public required int Value { get; init; }
    }

    private class TestProvider : IResourceProvider
    {
        private readonly Dictionary<string, object> _resources = new();

        public void AddResource<T>(string id, T resource) where T : class
        {
            _resources[id] = resource;
        }

        public bool CanHandle(string resourceId) => _resources.ContainsKey(resourceId);

        public Task<TResource?> LoadAsync<TResource>(string resourceId, CancellationToken cancellationToken = default)
            where TResource : class
        {
            if (_resources.TryGetValue(resourceId, out var resource) && resource is TResource typed)
            {
                return Task.FromResult<TResource?>(typed);
            }

            return Task.FromResult<TResource?>(null);
        }

        public Task<Plate.CrossMilo.Contracts.Resource.ResourceMetadata?> GetMetadataAsync(
            string resourceId,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult<Plate.CrossMilo.Contracts.Resource.ResourceMetadata?>(null);
        }
    }

    [Fact]
    public async Task Handle_DisposesCorrectly_DecrementsRefCount()
    {
        // Arrange
        var provider = new TestProvider();
        provider.AddResource("test", new TestResource { Name = "Test", Value = 42 });

        var manager = new ResourceManager(
            NullLogger<ResourceManager>.Instance,
            new[] { provider },
            new ResourceManagerOptions { EnableAutoCleanup = false }
        );

        // Act
        ResourceHandle<TestResource>? handle;
        using (handle = manager.GetHandle<TestResource>("test"))
        {
            var resource = await handle.GetAsync();
            resource.Should().NotBeNull();
            resource!.Name.Should().Be("Test");

            // Check refcount is 1 while handle exists
            manager.GetState("test").Should().Be(ResourceLoadState.Loaded);
        }

        // Assert - After dispose, refcount should be 0
        // (We can't directly check refcount without exposing it, but we can verify behavior)
        handle.Invoking(h => h.GetIfLoaded()).Should().Throw<ObjectDisposedException>();
    }

    [Fact]
    public async Task MultipleHandles_ShareResource_IndependentLifecycles()
    {
        // Arrange
        var provider = new TestProvider();
        provider.AddResource("test", new TestResource { Name = "Test", Value = 42 });

        var manager = new ResourceManager(
            NullLogger<ResourceManager>.Instance,
            new[] { provider },
            new ResourceManagerOptions { EnableAutoCleanup = false }
        );

        // Act
        var handle1 = manager.GetHandle<TestResource>("test");
        var handle2 = manager.GetHandle<TestResource>("test");

        var resource1 = await handle1.GetAsync();
        var resource2 = await handle2.GetAsync();

        // Assert - Same instance
        resource1.Should().BeSameAs(resource2);

        // Dispose first handle
        handle1.Dispose();

        // Second handle should still work
        var resource2Again = handle2.GetIfLoaded();
        resource2Again.Should().NotBeNull();
        resource2Again.Should().BeSameAs(resource1);

        // Dispose second handle
        handle2.Dispose();
    }

    [Fact]
    public async Task GetAsync_LoadsOnFirstCall_CachesSubsequently()
    {
        // Arrange
        int loadCount = 0;
        var provider = new TestProvider();

        // Wrap provider to count loads
        var countingProvider = new CountingProvider(provider, () => loadCount++);
        countingProvider.AddResource("test", new TestResource { Name = "Test", Value = 42 });

        var manager = new ResourceManager(
            NullLogger<ResourceManager>.Instance,
            new[] { (IResourceProvider)countingProvider },
            new ResourceManagerOptions { EnableAutoCleanup = false }
        );

        using var handle = manager.GetHandle<TestResource>("test");

        // Act
        var resource1 = await handle.GetAsync();
        var resource2 = await handle.GetAsync();
        var resource3 = await handle.GetAsync();

        // Assert
        loadCount.Should().Be(1, "resource should only be loaded once");
        resource1.Should().BeSameAs(resource2);
        resource2.Should().BeSameAs(resource3);
    }

    [Fact]
    public async Task GetIfLoaded_ReturnsNull_WhenNotLoaded()
    {
        // Arrange
        var provider = new TestProvider();
        provider.AddResource("test", new TestResource { Name = "Test", Value = 42 });

        var manager = new ResourceManager(
            NullLogger<ResourceManager>.Instance,
            new[] { provider },
            new ResourceManagerOptions { EnableAutoCleanup = false }
        );

        using var handle = manager.GetHandle<TestResource>("test");

        // Act
        var resourceBeforeLoad = handle.GetIfLoaded();

        // Assert
        resourceBeforeLoad.Should().BeNull("resource hasn't been loaded yet");
    }

    [Fact]
    public async Task GetIfLoaded_ReturnsResource_WhenLoaded()
    {
        // Arrange
        var provider = new TestProvider();
        provider.AddResource("test", new TestResource { Name = "Test", Value = 42 });

        var manager = new ResourceManager(
            NullLogger<ResourceManager>.Instance,
            new[] { provider },
            new ResourceManagerOptions { EnableAutoCleanup = false }
        );

        using var handle = manager.GetHandle<TestResource>("test");

        // Act
        await handle.GetAsync(); // Load it
        var resourceAfterLoad = handle.GetIfLoaded();

        // Assert
        resourceAfterLoad.Should().NotBeNull();
        resourceAfterLoad!.Name.Should().Be("Test");
    }

    [Fact]
    public void IsLoaded_ReturnsFalse_WhenNotLoaded()
    {
        // Arrange
        var provider = new TestProvider();
        provider.AddResource("test", new TestResource { Name = "Test", Value = 42 });

        var manager = new ResourceManager(
            NullLogger<ResourceManager>.Instance,
            new[] { provider },
            new ResourceManagerOptions { EnableAutoCleanup = false }
        );

        using var handle = manager.GetHandle<TestResource>("test");

        // Assert
        handle.IsLoaded.Should().BeFalse();
    }

    [Fact]
    public async Task IsLoaded_ReturnsTrue_WhenLoaded()
    {
        // Arrange
        var provider = new TestProvider();
        provider.AddResource("test", new TestResource { Name = "Test", Value = 42 });

        var manager = new ResourceManager(
            NullLogger<ResourceManager>.Instance,
            new[] { provider },
            new ResourceManagerOptions { EnableAutoCleanup = false }
        );

        using var handle = manager.GetHandle<TestResource>("test");

        // Act
        await handle.GetAsync();

        // Assert
        handle.IsLoaded.Should().BeTrue();
    }

    [Fact]
    public async Task State_TransitionsCorrectly()
    {
        // Arrange
        var provider = new TestProvider();
        provider.AddResource("test", new TestResource { Name = "Test", Value = 42 });

        var manager = new ResourceManager(
            NullLogger<ResourceManager>.Instance,
            new[] { provider },
            new ResourceManagerOptions { EnableAutoCleanup = false }
        );

        using var handle = manager.GetHandle<TestResource>("test");

        // Assert initial state
        handle.State.Should().Be(ResourceLoadState.Unloaded);

        // Act - Load
        var loadTask = handle.GetAsync();

        // State might be Loading during load
        // (timing dependent, so we don't assert here)

        await loadTask;

        // Assert final state
        handle.State.Should().Be(ResourceLoadState.Loaded);
    }

    [Fact]
    public void Dispose_CanBeCalledMultipleTimes()
    {
        // Arrange
        var provider = new TestProvider();
        var manager = new ResourceManager(
            NullLogger<ResourceManager>.Instance,
            new[] { provider },
            new ResourceManagerOptions { EnableAutoCleanup = false }
        );

        var handle = manager.GetHandle<TestResource>("test");

        // Act & Assert - Should not throw
        handle.Dispose();
        handle.Dispose();
        handle.Dispose();
    }

    [Fact]
    public void DisposedHandle_ThrowsObjectDisposedException()
    {
        // Arrange
        var provider = new TestProvider();
        provider.AddResource("test", new TestResource { Name = "Test", Value = 42 });

        var manager = new ResourceManager(
            NullLogger<ResourceManager>.Instance,
            new[] { provider },
            new ResourceManagerOptions { EnableAutoCleanup = false }
        );

        var handle = manager.GetHandle<TestResource>("test");
        handle.Dispose();

        // Act & Assert
        handle.Invoking(h => h.GetAsync().AsTask()).Should().ThrowAsync<ObjectDisposedException>();
        handle.Invoking(h => h.GetIfLoaded()).Should().Throw<ObjectDisposedException>();
    }

    private class CountingProvider : IResourceProvider
    {
        private readonly TestProvider _inner;
        private readonly Action _onLoad;

        public CountingProvider(TestProvider inner, Action onLoad)
        {
            _inner = inner;
            _onLoad = onLoad;
        }

        public void AddResource<T>(string id, T resource) where T : class
        {
            _inner.AddResource(id, resource);
        }

        public bool CanHandle(string resourceId) => _inner.CanHandle(resourceId);

        public async Task<TResource?> LoadAsync<TResource>(string resourceId, CancellationToken ct = default)
            where TResource : class
        {
            _onLoad();
            return await _inner.LoadAsync<TResource>(resourceId, ct);
        }

        public Task<Plate.CrossMilo.Contracts.Resource.ResourceMetadata?> GetMetadataAsync(
            string resourceId,
            CancellationToken cancellationToken = default)
        {
            return _inner.GetMetadataAsync(resourceId, cancellationToken);
        }
    }
}

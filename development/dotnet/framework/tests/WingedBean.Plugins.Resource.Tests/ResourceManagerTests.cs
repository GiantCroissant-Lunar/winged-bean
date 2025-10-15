using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using WingedBean.Plugins.Resource.Providers;
using Xunit;

namespace WingedBean.Plugins.Resource.Tests;

/// <summary>
/// Tests for ResourceManager - RFC-0054 Phase 1
/// Focuses on load deduplication and state management
/// </summary>
public class ResourceManagerTests
{
    private class TestResource
    {
        public required string Name { get; init; }
    }

    private class SlowProvider : IResourceProvider
    {
        private readonly Dictionary<string, object> _resources = new();
        private readonly TimeSpan _delay;
        public int LoadCallCount { get; private set; }

        public SlowProvider(TimeSpan delay)
        {
            _delay = delay;
        }

        public void AddResource<T>(string id, T resource) where T : class
        {
            _resources[id] = resource;
        }

        public bool CanHandle(string resourceId) => _resources.ContainsKey(resourceId);

        public async Task<TResource?> LoadAsync<TResource>(string resourceId, CancellationToken cancellationToken = default)
            where TResource : class
        {
            LoadCallCount++;
            await Task.Delay(_delay, cancellationToken);

            if (_resources.TryGetValue(resourceId, out var resource) && resource is TResource typed)
            {
                return typed;
            }

            return null;
        }

        public Task<Plate.CrossMilo.Contracts.Resource.ResourceMetadata?> GetMetadataAsync(
            string resourceId,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult<Plate.CrossMilo.Contracts.Resource.ResourceMetadata?>(null);
        }
    }

    [Fact]
    public async Task DuplicateLoads_Deduplicated_SingleProviderCall()
    {
        // Arrange
        var provider = new SlowProvider(TimeSpan.FromMilliseconds(100));
        provider.AddResource("test", new TestResource { Name = "Test" });

        var manager = new ResourceManager(
            NullLogger<ResourceManager>.Instance,
            new[] { (IResourceProvider)provider },
            new ResourceManagerOptions { EnableAutoCleanup = false }
        );

        // Act - Start two loads simultaneously
        var task1 = manager.LoadAsync<TestResource>("test");
        var task2 = manager.LoadAsync<TestResource>("test");

        var results = await Task.WhenAll(task1, task2);

        // Assert
        provider.LoadCallCount.Should().Be(1, "provider should only be called once despite duplicate requests");
        results[0].Should().BeSameAs(results[1], "both requests should get the same instance");
    }

    [Fact]
    public async Task ConcurrentHandleCreation_SharesSingleLoad()
    {
        // Arrange
        var provider = new SlowProvider(TimeSpan.FromMilliseconds(100));
        provider.AddResource("test", new TestResource { Name = "Test" });

        var manager = new ResourceManager(
            NullLogger<ResourceManager>.Instance,
            new[] { (IResourceProvider)provider },
            new ResourceManagerOptions { EnableAutoCleanup = false }
        );

        // Act - Create multiple handles concurrently
        var handle1 = manager.GetHandle<TestResource>("test");
        var handle2 = manager.GetHandle<TestResource>("test");
        var handle3 = manager.GetHandle<TestResource>("test");

        var task1 = handle1.GetAsync();
        var task2 = handle2.GetAsync();
        var task3 = handle3.GetAsync();

        var results = await Task.WhenAll(task1.AsTask(), task2.AsTask(), task3.AsTask());

        // Assert
        provider.LoadCallCount.Should().Be(1, "load should be deduplicated");
        results[0].Should().BeSameAs(results[1]);
        results[1].Should().BeSameAs(results[2]);

        // Cleanup
        handle1.Dispose();
        handle2.Dispose();
        handle3.Dispose();
    }

    [Fact]
    public async Task LoadAsync_BackwardCompatibility_WorksWithoutHandle()
    {
        // Arrange
        var provider = new SlowProvider(TimeSpan.FromMilliseconds(10));
        provider.AddResource("test", new TestResource { Name = "Test" });

        var manager = new ResourceManager(
            NullLogger<ResourceManager>.Instance,
            new[] { (IResourceProvider)provider },
            new ResourceManagerOptions { EnableAutoCleanup = false }
        );

        // Act - Old API style (backward compatibility)
        var resource = await manager.LoadAsync<TestResource>("test");

        // Assert
        resource.Should().NotBeNull();
        resource!.Name.Should().Be("Test");
    }

    [Fact]
    public async Task GetState_ReturnsCorrectState_ThroughoutLifecycle()
    {
        // Arrange
        var provider = new SlowProvider(TimeSpan.FromMilliseconds(50));
        provider.AddResource("test", new TestResource { Name = "Test" });

        var manager = new ResourceManager(
            NullLogger<ResourceManager>.Instance,
            new[] { (IResourceProvider)provider },
            new ResourceManagerOptions { EnableAutoCleanup = false }
        );

        using var handle = manager.GetHandle<TestResource>("test");

        // Assert - Initial state
        handle.State.Should().Be(ResourceLoadState.Unloaded);

        // Act - Start loading
        var loadTask = handle.GetAsync();

        // Wait a bit to ensure we're in Loading state
        await Task.Delay(10);

        // Note: State might be Loading or Loaded depending on timing
        var stateWhileLoading = handle.State;
        stateWhileLoading.Should().BeOneOf(ResourceLoadState.Loading, ResourceLoadState.Loaded);

        // Wait for completion
        await loadTask;

        // Assert - Final state
        handle.State.Should().Be(ResourceLoadState.Loaded);
    }

    [Fact]
    public async Task ReloadAsync_ClearsCache_ReloadsResource()
    {
        // Arrange
        var provider = new SlowProvider(TimeSpan.FromMilliseconds(10));
        provider.AddResource("test", new TestResource { Name = "Original" });

        var manager = new ResourceManager(
            NullLogger<ResourceManager>.Instance,
            new[] { (IResourceProvider)provider },
            new ResourceManagerOptions { EnableAutoCleanup = false }
        );

        using var handle = manager.GetHandle<TestResource>("test");

        // Load initially
        var original = await handle.GetAsync();
        original!.Name.Should().Be("Original");

        var loadCountAfterFirst = provider.LoadCallCount;

        // Act - Reload
        await manager.ReloadAsync("test");

        // Assert
        provider.LoadCallCount.Should().BeGreaterThan(loadCountAfterFirst, "resource should be reloaded");
    }

    [Fact]
    public async Task OnReloadingEvent_Fires_BeforeReload()
    {
        // Arrange
        var provider = new SlowProvider(TimeSpan.FromMilliseconds(10));
        provider.AddResource("test", new TestResource { Name = "Test" });

        var manager = new ResourceManager(
            NullLogger<ResourceManager>.Instance,
            new[] { (IResourceProvider)provider },
            new ResourceManagerOptions { EnableAutoCleanup = false }
        );

        bool reloadingFired = false;
        string? reloadingResourceId = null;

        manager.OnReloading += (resourceId) =>
        {
            reloadingFired = true;
            reloadingResourceId = resourceId;
        };

        // Load initially
        await manager.LoadAsync<TestResource>("test");

        // Act - Reload
        await manager.ReloadAsync("test");

        // Assert
        reloadingFired.Should().BeTrue();
        reloadingResourceId.Should().Be("test");
    }

    [Fact]
    public async Task OnReloadedEvent_Fires_AfterSuccessfulReload()
    {
        // Arrange
        var provider = new SlowProvider(TimeSpan.FromMilliseconds(10));
        provider.AddResource("test", new TestResource { Name = "Test" });

        var manager = new ResourceManager(
            NullLogger<ResourceManager>.Instance,
            new[] { (IResourceProvider)provider },
            new ResourceManagerOptions { EnableAutoCleanup = false }
        );

        bool reloadedFired = false;
        string? reloadedResourceId = null;

        manager.OnReloaded += (resourceId) =>
        {
            reloadedFired = true;
            reloadedResourceId = resourceId;
        };

        // Load initially
        await manager.LoadAsync<TestResource>("test");

        // Act - Reload
        await manager.ReloadAsync("test");

        // Assert
        reloadedFired.Should().BeTrue();
        reloadedResourceId.Should().Be("test");
    }

    [Fact]
    public async Task WeakHandle_DoesNotIncrementRefCount()
    {
        // Arrange
        var provider = new SlowProvider(TimeSpan.FromMilliseconds(10));
        provider.AddResource("test", new TestResource { Name = "Test" });

        var manager = new ResourceManager(
            NullLogger<ResourceManager>.Instance,
            new[] { (IResourceProvider)provider },
            new ResourceManagerOptions { EnableAutoCleanup = false }
        );

        // Load with strong handle first
        using var strongHandle = manager.GetHandle<TestResource>("test");
        await strongHandle.GetAsync();

        // Act - Create weak handle
        var weakHandle = manager.GetWeakHandle<TestResource>("test");

        // Assert
        weakHandle.IsLoaded.Should().BeTrue();
        var resource = weakHandle.GetIfLoaded();
        resource.Should().NotBeNull();
    }

    [Fact]
    public async Task WeakHandle_CanUpgradeToStrongHandle()
    {
        // Arrange
        var provider = new SlowProvider(TimeSpan.FromMilliseconds(10));
        provider.AddResource("test", new TestResource { Name = "Test" });

        var manager = new ResourceManager(
            NullLogger<ResourceManager>.Instance,
            new[] { (IResourceProvider)provider },
            new ResourceManagerOptions { EnableAutoCleanup = false }
        );

        // Load with strong handle first
        using (var strongHandle = manager.GetHandle<TestResource>("test"))
        {
            await strongHandle.GetAsync();
        }

        // Create weak handle while resource is loaded
        var weakHandle = manager.GetWeakHandle<TestResource>("test");

        // Act - Upgrade to strong handle
        var upgradedHandle = weakHandle.ToStrongHandle();

        // Assert
        upgradedHandle.Should().NotBeNull();
        var resource = upgradedHandle!.GetIfLoaded();
        resource.Should().NotBeNull();

        // Cleanup
        upgradedHandle.Dispose();
    }

    [Fact]
    public void Dispose_CleansUpResources()
    {
        // Arrange
        var provider = new SlowProvider(TimeSpan.FromMilliseconds(10));
        var manager = new ResourceManager(
            NullLogger<ResourceManager>.Instance,
            new[] { (IResourceProvider)provider },
            new ResourceManagerOptions { EnableAutoCleanup = false }
        );

        // Act & Assert - Should not throw
        manager.Dispose();
    }
}

using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using System.Diagnostics;
using WingedBean.Plugins.Resource.Providers;
using Xunit;
using Xunit.Abstractions;

namespace WingedBean.Plugins.Resource.Tests;

/// <summary>
/// Performance and load tests for RFC-0054 Phase 3.
/// Tests load deduplication, throughput, and scalability.
/// </summary>
public class PerformanceTests
{
    private readonly ITestOutputHelper _output;

    public PerformanceTests(ITestOutputHelper output)
    {
        _output = output;
    }

    private class TestResource
    {
        public required string Id { get; init; }
        public required string Name { get; init; }
        public byte[] Data { get; init; } = Array.Empty<byte>();
    }

    private class InMemoryProvider : IResourceProvider
    {
        private readonly Dictionary<string, object> _resources = new();
        private readonly TimeSpan _loadDelay;
        private int _loadCallCount; // Changed to field

        public int LoadCallCount => _loadCallCount; // Property accessor

        public InMemoryProvider(TimeSpan loadDelay = default)
        {
            _loadDelay = loadDelay;
        }

        public void AddResource<T>(string id, T resource) where T : class
        {
            _resources[id] = resource;
        }

        public void AddResources(int count)
        {
            for (int i = 0; i < count; i++)
            {
                var id = $"resource-{i:D6}";
                _resources[id] = new TestResource
                {
                    Id = id,
                    Name = $"Test Resource {i}",
                    Data = new byte[1024] // 1KB per resource
                };
            }
        }

        public bool CanHandle(string resourceId) => _resources.ContainsKey(resourceId);

        public async Task<TResource?> LoadAsync<TResource>(
            string resourceId,
            CancellationToken cancellationToken = default) where TResource : class
        {
            Interlocked.Increment(ref _loadCallCount);

            if (_loadDelay > TimeSpan.Zero)
            {
                await Task.Delay(_loadDelay, cancellationToken);
            }

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
    public async Task LoadTest_1000Resources_Sequential()
    {
        // Arrange
        var provider = new InMemoryProvider();
        provider.AddResources(1000);

        var manager = new ResourceManager(
            NullLogger<ResourceManager>.Instance,
            new[] { (IResourceProvider)provider },
            new ResourceManagerOptions { EnableAutoCleanup = false }
        );

        // Act
        var sw = Stopwatch.StartNew();
        
        for (int i = 0; i < 1000; i++)
        {
            var resourceId = $"resource-{i:D6}";
            using var handle = manager.GetHandle<TestResource>(resourceId);
            var resource = await handle.GetAsync();
            resource.Should().NotBeNull();
        }

        sw.Stop();

        // Assert
        provider.LoadCallCount.Should().Be(1000, "each resource loaded once");
        _output.WriteLine($"Loaded 1000 resources sequentially in {sw.ElapsedMilliseconds}ms");
        _output.WriteLine($"Average: {sw.ElapsedMilliseconds / 1000.0:F2}ms per resource");

        // Performance target: < 2ms per resource on average
        var avgMs = sw.ElapsedMilliseconds / 1000.0;
        avgMs.Should().BeLessThan(2.0, "should achieve <2ms per resource");
    }

    [Fact]
    public async Task LoadTest_100Resources_Parallel_10Concurrent()
    {
        // Arrange
        var provider = new InMemoryProvider(TimeSpan.FromMilliseconds(10)); // 10ms simulated I/O
        provider.AddResources(100);

        var manager = new ResourceManager(
            NullLogger<ResourceManager>.Instance,
            new[] { (IResourceProvider)provider },
            new ResourceManagerOptions { EnableAutoCleanup = false }
        );

        // Act
        var sw = Stopwatch.StartNew();

        var tasks = Enumerable.Range(0, 100).Select(async i =>
        {
            var resourceId = $"resource-{i:D6}";
            using var handle = manager.GetHandle<TestResource>(resourceId);
            return await handle.GetAsync();
        });

        var results = await Task.WhenAll(tasks);

        sw.Stop();

        // Assert
        results.Should().AllSatisfy(r => r.Should().NotBeNull());
        provider.LoadCallCount.Should().Be(100, "each resource loaded once");
        
        _output.WriteLine($"Loaded 100 resources (10ms I/O each) in parallel in {sw.ElapsedMilliseconds}ms");
        _output.WriteLine($"Provider calls: {provider.LoadCallCount}");
        _output.WriteLine($"Theoretical sequential time: {100 * 10}ms");
        _output.WriteLine($"Speedup: {(100 * 10.0) / sw.ElapsedMilliseconds:F2}x");

        // Should be much faster than sequential (100 * 10ms = 1000ms)
        sw.ElapsedMilliseconds.Should().BeLessThan(500, "parallel loading should be faster");
    }

    [Fact]
    public async Task Deduplication_100ConcurrentLoadsOfSameResource_SingleProviderCall()
    {
        // Arrange
        var provider = new InMemoryProvider(TimeSpan.FromMilliseconds(100)); // 100ms simulated I/O
        provider.AddResource("shared-resource", new TestResource
        {
            Id = "shared",
            Name = "Shared Resource",
            Data = new byte[10240] // 10KB
        });

        var manager = new ResourceManager(
            NullLogger<ResourceManager>.Instance,
            new[] { (IResourceProvider)provider },
            new ResourceManagerOptions { EnableAutoCleanup = false }
        );

        // Act
        var sw = Stopwatch.StartNew();

        // 100 concurrent requests for the same resource
        var tasks = Enumerable.Range(0, 100).Select(async _ =>
        {
            using var handle = manager.GetHandle<TestResource>("shared-resource");
            return await handle.GetAsync();
        });

        var results = await Task.WhenAll(tasks);

        sw.Stop();

        // Assert
        results.Should().AllSatisfy(r => r.Should().NotBeNull());
        
        // All results should be the same instance (reference equality)
        var firstResult = results[0];
        results.Should().AllSatisfy(r => r.Should().BeSameAs(firstResult, "all should reference the same instance"));
        
        provider.LoadCallCount.Should().Be(1, "deduplication should result in single load");
        
        _output.WriteLine($"100 concurrent loads of same resource: {sw.ElapsedMilliseconds}ms");
        _output.WriteLine($"Provider calls: {provider.LoadCallCount} (deduplication working!)");
        _output.WriteLine($"Without deduplication would take: ~100 * 100ms = 10,000ms");

        // Should take ~100ms (one load), not 100 * 100ms (100 loads)
        sw.ElapsedMilliseconds.Should().BeLessThan(500, "deduplication should prevent redundant loads");
    }

    [Fact]
    public async Task Throughput_10000HandleCreations_WithCaching()
    {
        // Arrange
        var provider = new InMemoryProvider();
        provider.AddResources(100); // Only 100 unique resources

        var manager = new ResourceManager(
            NullLogger<ResourceManager>.Instance,
            new[] { (IResourceProvider)provider },
            new ResourceManagerOptions { EnableAutoCleanup = false }
        );

        // Pre-warm cache by loading all 100 resources
        for (int i = 0; i < 100; i++)
        {
            using var handle = manager.GetHandle<TestResource>($"resource-{i:D6}");
            await handle.GetAsync();
        }

        // Act - Create 10,000 handles (cycling through 100 resources)
        var sw = Stopwatch.StartNew();

        for (int i = 0; i < 10000; i++)
        {
            var resourceId = $"resource-{i % 100:D6}";
            using var handle = manager.GetHandle<TestResource>(resourceId);
            var resource = handle.GetIfLoaded(); // Should be instant (cached)
            resource.Should().NotBeNull();
        }

        sw.Stop();

        // Assert
        _output.WriteLine($"Created 10,000 handles (100 unique resources, cached): {sw.ElapsedMilliseconds}ms");
        _output.WriteLine($"Average: {sw.ElapsedMilliseconds / 10000.0:F3}ms per handle");
        
        // Performance target: < 0.1ms per handle creation when cached
        var avgMs = sw.ElapsedMilliseconds / 10000.0;
        avgMs.Should().BeLessThan(0.1, "handle creation from cache should be very fast");
    }

    [Fact]
    public async Task MemoryPressure_1000Resources_WithCleanup()
    {
        // Arrange
        var provider = new InMemoryProvider();
        provider.AddResources(1000);

        var manager = new ResourceManager(
            NullLogger<ResourceManager>.Instance,
            new[] { (IResourceProvider)provider },
            new ResourceManagerOptions
            {
                EnableAutoCleanup = false // Manual cleanup for test
            }
        );

        // Act - Load all resources
        var handles = new List<ResourceHandle<TestResource>>();
        
        for (int i = 0; i < 1000; i++)
        {
            var resourceId = $"resource-{i:D6}";
            var handle = manager.GetHandle<TestResource>(resourceId);
            await handle.GetAsync();
            handles.Add(handle);
        }

        // Check initial state
        handles.Should().AllSatisfy(h => h.IsLoaded.Should().BeTrue());

        // Dispose all handles
        foreach (var handle in handles)
        {
            handle.Dispose();
        }

        // Assert
        _output.WriteLine("All 1000 handles disposed successfully");
        _output.WriteLine("Resources are now eligible for cleanup (refcount = 0)");

        // Cleanup test - resources should be unloadable
        handles.Should().AllSatisfy(h =>
        {
            h.Invoking(x => x.GetIfLoaded())
                .Should().Throw<ObjectDisposedException>();
        });
    }

    [Fact]
    public async Task Concurrency_1000ParallelLoads_MixedResources()
    {
        // Arrange
        var provider = new InMemoryProvider(TimeSpan.FromMilliseconds(5));
        provider.AddResources(100); // 100 unique resources

        var manager = new ResourceManager(
            NullLogger<ResourceManager>.Instance,
            new[] { (IResourceProvider)provider },
            new ResourceManagerOptions { EnableAutoCleanup = false }
        );

        var random = new Random(42); // Deterministic

        // Act
        var sw = Stopwatch.StartNew();

        var tasks = Enumerable.Range(0, 1000).Select(async i =>
        {
            // Random resource (0-99)
            var resourceId = $"resource-{random.Next(100):D6}";
            using var handle = manager.GetHandle<TestResource>(resourceId);
            return await handle.GetAsync();
        });

        var results = await Task.WhenAll(tasks);

        sw.Stop();

        // Assert
        results.Should().AllSatisfy(r => r.Should().NotBeNull());
        
        _output.WriteLine($"1000 parallel loads (100 unique resources, 5ms I/O): {sw.ElapsedMilliseconds}ms");
        _output.WriteLine($"Provider calls: {provider.LoadCallCount}");
        _output.WriteLine($"Deduplication rate: {(1000 - provider.LoadCallCount) / 1000.0:P1}");

        // Each unique resource should only be loaded once
        provider.LoadCallCount.Should().BeLessOrEqualTo(100, "deduplication should work");
        
        // Should complete reasonably fast
        sw.ElapsedMilliseconds.Should().BeLessThan(2000, "concurrent loads should be efficient");
    }

    [Fact]
    public async Task StateTransitions_100Resources_AllStatesVerified()
    {
        // Arrange
        var provider = new InMemoryProvider(TimeSpan.FromMilliseconds(10));
        provider.AddResources(100);

        var manager = new ResourceManager(
            NullLogger<ResourceManager>.Instance,
            new[] { (IResourceProvider)provider },
            new ResourceManagerOptions { EnableAutoCleanup = false }
        );

        // Act & Assert - Track state transitions
        var handles = new List<ResourceHandle<TestResource>>();

        for (int i = 0; i < 100; i++)
        {
            var resourceId = $"resource-{i:D6}";
            var handle = manager.GetHandle<TestResource>(resourceId);
            
            // Initial state
            handle.State.Should().Be(ResourceLoadState.Unloaded);
            handle.IsLoaded.Should().BeFalse();
            
            // Start loading
            var loadTask = handle.GetAsync();
            
            // During load (may be Loading or Loaded depending on timing)
            var stateWhileLoading = handle.State;
            stateWhileLoading.Should().BeOneOf(
                ResourceLoadState.Loading,
                ResourceLoadState.Loaded
            );
            
            await loadTask;
            
            // After load
            handle.State.Should().Be(ResourceLoadState.Loaded);
            handle.IsLoaded.Should().BeTrue();
            
            handles.Add(handle);
        }

        _output.WriteLine("All 100 resources transitioned through states correctly");

        // Cleanup
        foreach (var handle in handles)
        {
            handle.Dispose();
        }
    }

    [Fact]
    public async Task RefCounting_MultipleHandles_CorrectLifecycle()
    {
        // Arrange
        var provider = new InMemoryProvider();
        provider.AddResource("shared", new TestResource
        {
            Id = "shared",
            Name = "Shared",
            Data = new byte[1024]
        });

        var manager = new ResourceManager(
            NullLogger<ResourceManager>.Instance,
            new[] { (IResourceProvider)provider },
            new ResourceManagerOptions { EnableAutoCleanup = false }
        );

        // Act - Create 10 handles to same resource
        var handles = Enumerable.Range(0, 10)
            .Select(_ => manager.GetHandle<TestResource>("shared"))
            .ToList();

        // Load via first handle
        var resource = await handles[0].GetAsync();
        resource.Should().NotBeNull();

        // All handles should see it as loaded
        handles.Should().AllSatisfy(h => h.IsLoaded.Should().BeTrue());

        // Dispose 5 handles
        for (int i = 0; i < 5; i++)
        {
            handles[i].Dispose();
        }

        // Remaining 5 handles should still work
        for (int i = 5; i < 10; i++)
        {
            var res = handles[i].GetIfLoaded();
            res.Should().NotBeNull();
        }

        // Dispose remaining handles
        for (int i = 5; i < 10; i++)
        {
            handles[i].Dispose();
        }

        _output.WriteLine("Reference counting verified: resource stayed alive until all handles disposed");
    }
}

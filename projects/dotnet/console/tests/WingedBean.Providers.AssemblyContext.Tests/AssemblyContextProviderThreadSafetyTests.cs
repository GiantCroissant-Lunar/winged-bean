using System.Reflection;
using FluentAssertions;
using Xunit;

namespace WingedBean.Providers.AssemblyContext.Tests;

/// <summary>
/// Thread-safety tests for AssemblyContextProvider.
/// </summary>
public class AssemblyContextProviderThreadSafetyTests : IDisposable
{
    private readonly AssemblyContextProvider _provider;

    public AssemblyContextProviderThreadSafetyTests()
    {
        _provider = new AssemblyContextProvider();
    }

    public void Dispose()
    {
        _provider?.Dispose();
    }

    [Fact]
    public void CreateContext_ConcurrentCreations_ShouldHandleCorrectly()
    {
        // Arrange
        const int concurrentCount = 50;
        var tasks = new List<Task<string>>();

        // Act: Perform concurrent context creations
        for (int i = 0; i < concurrentCount; i++)
        {
            var contextName = $"TestContext_{i}_{Guid.NewGuid():N}";
            tasks.Add(Task.Run(() => _provider.CreateContext(contextName)));
        }

        Task.WaitAll(tasks.Cast<Task>().ToArray());

        // Assert: All contexts should be created successfully
        foreach (var task in tasks)
        {
            task.Result.Should().NotBeNullOrEmpty();
            _provider.ContextExists(task.Result).Should().BeTrue();
        }

        _provider.GetLoadedContexts().Should().HaveCount(concurrentCount);
    }

    [Fact]
    public void LoadAssembly_ConcurrentLoads_ShouldHandleCorrectly()
    {
        // Arrange
        const int concurrentCount = 20;
        var tasks = new List<Task<Assembly>>();
        var testAssemblyPath = Assembly.GetExecutingAssembly().Location;

        // Create contexts first
        var contextNames = new List<string>();
        for (int i = 0; i < concurrentCount; i++)
        {
            var contextName = $"TestContext_{i}_{Guid.NewGuid():N}";
            _provider.CreateContext(contextName);
            contextNames.Add(contextName);
        }

        // Act: Perform concurrent assembly loads
        foreach (var contextName in contextNames)
        {
            tasks.Add(Task.Run(() => _provider.LoadAssembly(contextName, testAssemblyPath)));
        }

        Task.WaitAll(tasks.Cast<Task>().ToArray());

        // Assert: All assemblies should be loaded successfully
        foreach (var task in tasks)
        {
            task.Result.Should().NotBeNull();
            task.Result.Location.Should().Be(testAssemblyPath);
        }
    }

    [Fact]
    public void GetContext_ConcurrentGets_ShouldHandleCorrectly()
    {
        // Arrange
        var contextName = $"TestContext_{Guid.NewGuid():N}";
        _provider.CreateContext(contextName);

        const int concurrentCount = 100;
        var tasks = new List<Task<System.Runtime.Loader.AssemblyLoadContext?>>();

        // Act: Perform concurrent reads
        for (int i = 0; i < concurrentCount; i++)
        {
            tasks.Add(Task.Run(() => _provider.GetContext(contextName)));
        }

        Task.WaitAll(tasks.Cast<Task>().ToArray());

        // Assert: All reads should return the same context
        var firstContext = tasks[0].Result;
        foreach (var task in tasks)
        {
            task.Result.Should().BeSameAs(firstContext);
        }
    }

    [Fact]
    public async Task UnloadContext_ConcurrentUnloads_ShouldHandleCorrectly()
    {
        // Arrange
        const int concurrentCount = 20;
        var contextNames = new List<string>();

        // Create contexts
        for (int i = 0; i < concurrentCount; i++)
        {
            var contextName = $"TestContext_{i}_{Guid.NewGuid():N}";
            _provider.CreateContext(contextName);
            contextNames.Add(contextName);
        }

        // Act: Perform concurrent unloads
        var tasks = contextNames.Select(name =>
            _provider.UnloadContextAsync(name, waitForUnload: false)).ToList();

        await Task.WhenAll(tasks);

        // Assert: All contexts should be unloaded
        foreach (var contextName in contextNames)
        {
            _provider.ContextExists(contextName).Should().BeFalse();
        }

        _provider.GetLoadedContexts().Should().BeEmpty();
    }

    [Fact]
    public void MixedOperations_ConcurrentMixedOperations_ShouldHandleCorrectly()
    {
        // Arrange
        const int operationsCount = 30;
        var tasks = new List<Task>();
        var testAssemblyPath = Assembly.GetExecutingAssembly().Location;

        // Act: Perform concurrent mixed operations
        for (int i = 0; i < operationsCount; i++)
        {
            var index = i;
            var contextName = $"TestContext_{index}_{Guid.NewGuid():N}";

            // Mix different operations
            if (index % 3 == 0)
            {
                // Create context
                tasks.Add(Task.Run(() => _provider.CreateContext(contextName)));
            }
            else if (index % 3 == 1)
            {
                // Create and load
                tasks.Add(Task.Run(() =>
                {
                    _provider.CreateContext(contextName);
                    _provider.LoadAssembly(contextName, testAssemblyPath);
                }));
            }
            else
            {
                // Create and check
                tasks.Add(Task.Run(() =>
                {
                    _provider.CreateContext(contextName);
                    return _provider.ContextExists(contextName);
                }));
            }
        }

        Task.WaitAll(tasks.ToArray());

        // Assert: Should have created multiple contexts without errors
        _provider.GetLoadedContexts().Should().NotBeEmpty();
    }

    [Fact]
    public void ContextExists_ConcurrentChecks_ShouldHandleCorrectly()
    {
        // Arrange
        var contextName = $"TestContext_{Guid.NewGuid():N}";
        _provider.CreateContext(contextName);

        const int concurrentCount = 100;
        var tasks = new List<Task<bool>>();

        // Act: Perform concurrent existence checks
        for (int i = 0; i < concurrentCount; i++)
        {
            tasks.Add(Task.Run(() => _provider.ContextExists(contextName)));
        }

        Task.WaitAll(tasks.Cast<Task>().ToArray());

        // Assert: All checks should return true
        foreach (var task in tasks)
        {
            task.Result.Should().BeTrue();
        }
    }

    [Fact]
    public void GetLoadedContexts_ConcurrentGets_ShouldHandleCorrectly()
    {
        // Arrange
        // Create some contexts
        for (int i = 0; i < 10; i++)
        {
            var contextName = $"TestContext_{i}_{Guid.NewGuid():N}";
            _provider.CreateContext(contextName);
        }

        const int concurrentCount = 50;
        var tasks = new List<Task<IEnumerable<string>>>();

        // Act: Perform concurrent GetLoadedContexts calls
        for (int i = 0; i < concurrentCount; i++)
        {
            tasks.Add(Task.Run(() => _provider.GetLoadedContexts()));
        }

        Task.WaitAll(tasks.Cast<Task>().ToArray());

        // Assert: All calls should return consistent results
        var firstCount = tasks[0].Result.Count();
        foreach (var task in tasks)
        {
            task.Result.Should().HaveCount(firstCount);
        }
    }
}

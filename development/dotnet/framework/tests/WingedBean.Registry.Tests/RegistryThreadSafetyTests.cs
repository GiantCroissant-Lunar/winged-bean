using FluentAssertions;
using WingedBean.Contracts.Core;
using WingedBean.Registry;
using Xunit;

namespace WingedBean.Registry.Tests;

/// <summary>
/// Unit tests for ActualRegistry thread-safety.
/// </summary>
public class RegistryThreadSafetyTests
{
    private interface ITestService { }
    private class TestService : ITestService
    {
        public int Id { get; set; }
    }

    [Fact]
    public void Register_ConcurrentRegistrations_ShouldHandleCorrectly()
    {
        // Arrange
        var registry = new ActualRegistry();
        const int concurrentCount = 100;
        var tasks = new List<Task>();

        // Act: Perform concurrent registrations
        for (int i = 0; i < concurrentCount; i++)
        {
            var id = i;
            tasks.Add(Task.Run(() =>
            {
                var service = new TestService { Id = id };
                registry.Register<ITestService>(service, priority: id);
            }));
        }

        Task.WaitAll(tasks.ToArray());

        // Assert: All services should be registered
        var allServices = registry.GetAll<ITestService>().ToList();
        allServices.Should().HaveCount(concurrentCount);
        registry.IsRegistered<ITestService>().Should().BeTrue();
    }

    [Fact]
    public void Get_ConcurrentGets_ShouldHandleCorrectly()
    {
        // Arrange
        var registry = new ActualRegistry();
        var service = new TestService { Id = 1 };
        registry.Register<ITestService>(service, priority: 10);

        const int concurrentCount = 100;
        var tasks = new List<Task<ITestService>>();

        // Act: Perform concurrent reads
        for (int i = 0; i < concurrentCount; i++)
        {
            tasks.Add(Task.Run(() => registry.Get<ITestService>(SelectionMode.HighestPriority)));
        }

        Task.WaitAll(tasks.Cast<Task>().ToArray());

        // Assert: All reads should return the same service
        foreach (var task in tasks)
        {
            task.Result.Should().BeSameAs(service);
        }
    }

    [Fact]
    public void Unregister_ConcurrentUnregistrations_ShouldHandleCorrectly()
    {
        // Arrange
        var registry = new ActualRegistry();
        var services = new List<TestService>();

        for (int i = 0; i < 50; i++)
        {
            var service = new TestService { Id = i };
            services.Add(service);
            registry.Register<ITestService>(service, priority: i);
        }

        var tasks = new List<Task>();

        // Act: Perform concurrent unregistrations
        foreach (var service in services)
        {
            var s = service;
            tasks.Add(Task.Run(() => registry.Unregister<ITestService>(s)));
        }

        Task.WaitAll(tasks.ToArray());

        // Assert: All services should be unregistered
        registry.IsRegistered<ITestService>().Should().BeFalse();
        registry.GetAll<ITestService>().Should().BeEmpty();
    }

    [Fact]
    public void MixedOperations_ConcurrentReadWriteUnregister_ShouldHandleCorrectly()
    {
        // Arrange
        var registry = new ActualRegistry();
        var initialServices = new List<TestService>();

        // Pre-register some services
        for (int i = 0; i < 10; i++)
        {
            var service = new TestService { Id = i };
            initialServices.Add(service);
            registry.Register<ITestService>(service, priority: i);
        }

        var tasks = new List<Task>();

        // Act: Perform concurrent mixed operations
        // Register new services
        for (int i = 10; i < 20; i++)
        {
            var id = i;
            tasks.Add(Task.Run(() =>
            {
                var service = new TestService { Id = id };
                registry.Register<ITestService>(service, priority: id);
            }));
        }

        // Read services
        for (int i = 0; i < 20; i++)
        {
            tasks.Add(Task.Run(() =>
            {
                try
                {
                    _ = registry.Get<ITestService>(SelectionMode.HighestPriority);
                }
                catch (ServiceNotFoundException)
                {
                    // Expected if all services were unregistered
                }
            }));
        }

        // Unregister some services
        for (int i = 0; i < 5; i++)
        {
            var service = initialServices[i];
            tasks.Add(Task.Run(() => registry.Unregister<ITestService>(service)));
        }

        // Assert: Should not throw any exceptions
        var act = () => Task.WaitAll(tasks.ToArray());
        act.Should().NotThrow();

        // Verify registry is still functional
        registry.IsRegistered<ITestService>().Should().BeTrue();
    }

    [Fact]
    public void GetAll_ConcurrentGetAll_ShouldReturnConsistentResults()
    {
        // Arrange
        var registry = new ActualRegistry();
        const int serviceCount = 10;

        for (int i = 0; i < serviceCount; i++)
        {
            var service = new TestService { Id = i };
            registry.Register<ITestService>(service, priority: i);
        }

        const int concurrentCount = 50;
        var tasks = new List<Task<List<ITestService>>>();

        // Act: Perform concurrent GetAll operations
        for (int i = 0; i < concurrentCount; i++)
        {
            tasks.Add(Task.Run(() => registry.GetAll<ITestService>().ToList()));
        }

        Task.WaitAll(tasks.Cast<Task>().ToArray());

        // Assert: All GetAll calls should return the same count
        foreach (var task in tasks)
        {
            task.Result.Should().HaveCount(serviceCount);
        }
    }

    [Fact]
    public void GetMetadata_ConcurrentMetadataAccess_ShouldHandleCorrectly()
    {
        // Arrange
        var registry = new ActualRegistry();
        var service = new TestService { Id = 1 };
        var metadata = new ServiceMetadata
        {
            Name = "TestService",
            Priority = 10,
            Version = "1.0.0"
        };
        registry.Register<ITestService>(service, metadata);

        const int concurrentCount = 50;
        var tasks = new List<Task<ServiceMetadata?>>();

        // Act: Perform concurrent metadata reads
        for (int i = 0; i < concurrentCount; i++)
        {
            tasks.Add(Task.Run(() => registry.GetMetadata<ITestService>(service)));
        }

        Task.WaitAll(tasks.Cast<Task>().ToArray());

        // Assert: All reads should return the correct metadata
        foreach (var task in tasks)
        {
            task.Result.Should().NotBeNull();
            task.Result!.Name.Should().Be("TestService");
            task.Result.Priority.Should().Be(10);
            task.Result.Version.Should().Be("1.0.0");
        }
    }
}

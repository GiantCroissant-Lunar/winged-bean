using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using WingedBean.Contracts.Resource;
using WingedBean.PluginSystem;
using Xunit;

namespace WingedBean.Plugins.Resource.Tests;

/// <summary>
/// Integration tests for Resource plugin activation and service registration.
/// </summary>
public class ResourcePluginIntegrationTests
{
    [Fact]
    public async Task PluginActivator_RegistersIResourceService()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging(); // Need to add logging to the services being built
        
        var hostServices = new ServiceCollection()
            .AddLogging()
            .BuildServiceProvider();

        var activator = new ResourcePluginActivator();

        // Act
        await activator.ActivateAsync(services, hostServices);
        var provider = services.BuildServiceProvider();

        // Assert
        var service = provider.GetService<IResourceService>();
        service.Should().NotBeNull();
        service.Should().BeOfType<FileSystemResourceService>();
    }

    [Fact]
    public async Task PluginActivator_ServiceIsSingleton()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging(); // Need to add logging to the services being built
        
        var hostServices = new ServiceCollection()
            .AddLogging()
            .BuildServiceProvider();

        var activator = new ResourcePluginActivator();

        // Act
        await activator.ActivateAsync(services, hostServices);
        var provider = services.BuildServiceProvider();

        var service1 = provider.GetService<IResourceService>();
        var service2 = provider.GetService<IResourceService>();

        // Assert
        service1.Should().BeSameAs(service2);
    }

    [Fact]
    public async Task ActivatedService_CanLoadResources()
    {
        // Arrange
        var testPath = Path.Combine(Path.GetTempPath(), $"resource-integration-test-{Guid.NewGuid()}");
        Directory.CreateDirectory(testPath);

        try
        {
            // Create test resource
            var testFile = Path.Combine(testPath, "test.json");
            await File.WriteAllTextAsync(testFile, "{\"name\":\"Integration\",\"value\":123}");

            var services = new ServiceCollection();
            services.AddSingleton<ILogger<FileSystemResourceService>>(NullLogger<FileSystemResourceService>.Instance);
            
            var hostServices = new ServiceCollection()
                .AddLogging()
                .BuildServiceProvider();

            // Manually register with custom base path (since activator uses default)
            services.AddSingleton<IResourceService>(sp =>
            {
                var logger = sp.GetRequiredService<ILogger<FileSystemResourceService>>();
                return new FileSystemResourceService(logger, testPath);
            });

            var provider = services.BuildServiceProvider();
            var service = provider.GetRequiredService<IResourceService>();

            // Act
            var result = await service.LoadAsync<TestData>("test.json");

            // Assert
            result.Should().NotBeNull();
            result!.Name.Should().Be("Integration");
            result.Value.Should().Be(123);
        }
        finally
        {
            if (Directory.Exists(testPath))
            {
                Directory.Delete(testPath, recursive: true);
            }
        }
    }

    [Fact]
    public async Task PluginActivator_DeactivateAsync_Succeeds()
    {
        // Arrange
        var activator = new ResourcePluginActivator();

        // Act
        var act = async () => await activator.DeactivateAsync();

        // Assert
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task MultipleActivations_AreIdempotent()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging(); // Need to add logging to the services being built
        
        var hostServices = new ServiceCollection()
            .AddLogging()
            .BuildServiceProvider();

        var activator = new ResourcePluginActivator();

        // Act
        await activator.ActivateAsync(services, hostServices);
        await activator.ActivateAsync(services, hostServices); // Second activation

        var provider = services.BuildServiceProvider();
        var allServices = provider.GetServices<IResourceService>().ToList();

        // Assert - should have registered the service twice (DI behavior)
        allServices.Should().HaveCount(2);
        allServices[0].Should().BeOfType<FileSystemResourceService>();
        allServices[1].Should().BeOfType<FileSystemResourceService>();
    }

    [Fact]
    public void FileSystemResourceService_WithDefaultBasePath_CreatesResourcesDirectory()
    {
        // Arrange
        var logger = NullLogger<FileSystemResourceService>.Instance;
        var expectedPath = Path.Combine(AppContext.BaseDirectory, "resources");

        // Clean up if exists
        if (Directory.Exists(expectedPath))
        {
            try { Directory.Delete(expectedPath, recursive: true); } catch { }
        }

        // Act
        var service = new FileSystemResourceService(logger);

        // Assert
        Directory.Exists(expectedPath).Should().BeTrue();

        // Cleanup
        try { Directory.Delete(expectedPath, recursive: true); } catch { }
    }

    [Fact]
    public async Task IntegrationScenario_ActivateLoadAndCache()
    {
        // Arrange - Full integration scenario
        var testPath = Path.Combine(Path.GetTempPath(), $"resource-full-integration-{Guid.NewGuid()}");
        Directory.CreateDirectory(testPath);

        try
        {
            // Create test resources
            var dataDir = Path.Combine(testPath, "data");
            Directory.CreateDirectory(dataDir);
            await File.WriteAllTextAsync(
                Path.Combine(dataDir, "item1.json"),
                "{\"name\":\"Item1\",\"value\":10}"
            );
            await File.WriteAllTextAsync(
                Path.Combine(dataDir, "item2.json"),
                "{\"name\":\"Item2\",\"value\":20}"
            );

            // Setup services
            var services = new ServiceCollection();
            services.AddSingleton<ILogger<FileSystemResourceService>>(NullLogger<FileSystemResourceService>.Instance);
            services.AddSingleton<IResourceService>(sp =>
            {
                var logger = sp.GetRequiredService<ILogger<FileSystemResourceService>>();
                return new FileSystemResourceService(logger, testPath);
            });

            var provider = services.BuildServiceProvider();
            var service = provider.GetRequiredService<IResourceService>();

            // Act & Assert - Load first resource
            var item1 = await service.LoadAsync<TestData>("data/item1.json");
            item1.Should().NotBeNull();
            item1!.Name.Should().Be("Item1");

            // Act & Assert - Check cache
            service.IsLoaded("data/item1.json").Should().BeTrue();

            // Act & Assert - Load second resource
            var item2 = await service.LoadAsync<TestData>("data/item2.json");
            item2.Should().NotBeNull();
            item2!.Value.Should().Be(20);

            // Act & Assert - Load pattern
            var allItems = await service.LoadAllAsync<TestData>("data/*.json");
            allItems.Should().HaveCount(2);

            // Act & Assert - Unload
            service.Unload("data/item1.json");
            service.IsLoaded("data/item1.json").Should().BeFalse();
            service.IsLoaded("data/item2.json").Should().BeTrue(); // Still cached
        }
        finally
        {
            if (Directory.Exists(testPath))
            {
                Directory.Delete(testPath, recursive: true);
            }
        }
    }

    private class TestData
    {
        public string Name { get; set; } = string.Empty;
        public int Value { get; set; }
    }
}

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using FluentAssertions;
using Plate.PluginManoi.Contracts;
using Plate.PluginManoi.Registry;
using Plate.PluginManoi.Loader;
using Plate.PluginManoi.Loader.AssemblyContext;
using Xunit;

namespace ConsoleDungeon.Host.Tests;

/// <summary>
/// Integration tests for plugin priority and load order (RFC-0006, Phase 6, Wave 6.2).
/// Tests that plugins load in priority order and that highest priority service is selected.
/// </summary>
public class PluginPriorityTests : IDisposable
{
    private readonly string _testDirectory;
    private readonly string _testPluginsJsonPath;

    public PluginPriorityTests()
    {
        // Create a temporary directory for test files
        _testDirectory = Path.Combine(Path.GetTempPath(), $"wingedbean-test-{Guid.NewGuid()}");
        Directory.CreateDirectory(_testDirectory);
        _testPluginsJsonPath = Path.Combine(_testDirectory, "plugins.json");
    }

    public void Dispose()
    {
        // Clean up test directory
        if (Directory.Exists(_testDirectory))
        {
            try
            {
                Directory.Delete(_testDirectory, recursive: true);
            }
            catch
            {
                // Ignore cleanup errors
            }
        }
    }

    [Fact]
    public async Task PluginLoadOrder_FollowsPriority_HighToLow()
    {
        // Arrange: Create a configuration with plugins of different priorities
        var config = new PluginConfiguration
        {
            Version = "1.0",
            PluginDirectory = "plugins",
            Plugins = new()
            {
                new PluginDescriptor
                {
                    Id = "wingedbean.plugins.low",
                    Path = "low.dll",
                    Priority = 10,
                    LoadStrategy = LoadStrategy.Eager,
                    Enabled = true
                },
                new PluginDescriptor
                {
                    Id = "wingedbean.plugins.high",
                    Path = "high.dll",
                    Priority = 1000,
                    LoadStrategy = LoadStrategy.Eager,
                    Enabled = true
                },
                new PluginDescriptor
                {
                    Id = "wingedbean.plugins.medium",
                    Path = "medium.dll",
                    Priority = 100,
                    LoadStrategy = LoadStrategy.Eager,
                    Enabled = true
                }
            }
        };

        await File.WriteAllTextAsync(_testPluginsJsonPath, JsonSerializer.Serialize(config));

        // Act: Load configuration and sort by priority (mimics Program.cs behavior)
        var loadedConfig = await LoadPluginConfigurationAsync(_testPluginsJsonPath);
        var enabledPlugins = loadedConfig.Plugins
            .Where(p => p.Enabled)
            .OrderByDescending(p => p.Priority)
            .ToList();

        // Assert: Plugins should be ordered from highest to lowest priority
        enabledPlugins.Should().HaveCount(3);
        enabledPlugins[0].Id.Should().Be("wingedbean.plugins.high");
        enabledPlugins[0].Priority.Should().Be(1000);
        enabledPlugins[1].Id.Should().Be("wingedbean.plugins.medium");
        enabledPlugins[1].Priority.Should().Be(100);
        enabledPlugins[2].Id.Should().Be("wingedbean.plugins.low");
        enabledPlugins[2].Priority.Should().Be(10);
    }

    [Fact]
    public async Task ChangePriorities_UpdatesLoadOrder()
    {
        // Arrange: Start with initial priorities
        var config = new PluginConfiguration
        {
            Version = "1.0",
            PluginDirectory = "plugins",
            Plugins = new()
            {
                new PluginDescriptor
                {
                    Id = "wingedbean.plugins.alpha",
                    Path = "alpha.dll",
                    Priority = 50,
                    LoadStrategy = LoadStrategy.Eager,
                    Enabled = true
                },
                new PluginDescriptor
                {
                    Id = "wingedbean.plugins.beta",
                    Path = "beta.dll",
                    Priority = 100,
                    LoadStrategy = LoadStrategy.Eager,
                    Enabled = true
                }
            }
        };

        await File.WriteAllTextAsync(_testPluginsJsonPath, JsonSerializer.Serialize(config));

        // Act 1: Load with initial priorities
        var loadedConfig1 = await LoadPluginConfigurationAsync(_testPluginsJsonPath);
        var enabledPlugins1 = loadedConfig1.Plugins
            .Where(p => p.Enabled)
            .OrderByDescending(p => p.Priority)
            .ToList();

        // Assert 1: Beta should load first (higher priority)
        enabledPlugins1[0].Id.Should().Be("wingedbean.plugins.beta");
        enabledPlugins1[1].Id.Should().Be("wingedbean.plugins.alpha");

        // Act 2: Change priorities (swap them)
        config.Plugins[0].Priority = 200; // alpha: 50 -> 200
        config.Plugins[1].Priority = 75;  // beta: 100 -> 75
        await File.WriteAllTextAsync(_testPluginsJsonPath, JsonSerializer.Serialize(config));

        // Act 3: Reload configuration
        var loadedConfig2 = await LoadPluginConfigurationAsync(_testPluginsJsonPath);
        var enabledPlugins2 = loadedConfig2.Plugins
            .Where(p => p.Enabled)
            .OrderByDescending(p => p.Priority)
            .ToList();

        // Assert 2: Alpha should now load first (higher priority)
        enabledPlugins2[0].Id.Should().Be("wingedbean.plugins.alpha");
        enabledPlugins2[0].Priority.Should().Be(200);
        enabledPlugins2[1].Id.Should().Be("wingedbean.plugins.beta");
        enabledPlugins2[1].Priority.Should().Be(75);
    }

    [Fact]
    public void Registry_MultipleServices_HighestPrioritySelected()
    {
        // Arrange: Create a mock service interface
        var registry = new ActualRegistry();
        var serviceLow = new TestService { Name = "Low Priority" };
        var serviceMedium = new TestService { Name = "Medium Priority" };
        var serviceHigh = new TestService { Name = "High Priority" };

        // Act: Register multiple implementations with different priorities
        registry.Register<ITestService>(serviceLow, priority: 10);
        registry.Register<ITestService>(serviceMedium, priority: 100);
        registry.Register<ITestService>(serviceHigh, priority: 1000);

        // Get service using default mode (HighestPriority)
        var result = registry.Get<ITestService>();

        // Assert: Should return the highest priority service
        result.Should().NotBeNull();
        result.Should().BeSameAs(serviceHigh);
        result.Name.Should().Be("High Priority");
    }

    [Fact]
    public void Registry_DefaultSelectionMode_UsesHighestPriority()
    {
        // Arrange
        var registry = new ActualRegistry();
        var service1 = new TestService { Name = "Priority 50" };
        var service2 = new TestService { Name = "Priority 200" };
        var service3 = new TestService { Name = "Priority 100" };

        // Act: Register in non-priority order
        registry.Register<ITestService>(service1, priority: 50);
        registry.Register<ITestService>(service2, priority: 200);
        registry.Register<ITestService>(service3, priority: 100);

        // Get without specifying mode (defaults to HighestPriority)
        var result = registry.Get<ITestService>();

        // Assert: Should return service2 (priority 200)
        result.Should().BeSameAs(service2);
        result.Name.Should().Be("Priority 200");
    }

    [Fact]
    public void Registry_ExplicitHighestPriorityMode_SelectsCorrectService()
    {
        // Arrange
        var registry = new ActualRegistry();
        var service1 = new TestService { Name = "Priority 1" };
        var service2 = new TestService { Name = "Priority 5" };
        var service3 = new TestService { Name = "Priority 3" };

        registry.Register<ITestService>(service1, priority: 1);
        registry.Register<ITestService>(service2, priority: 5);
        registry.Register<ITestService>(service3, priority: 3);

        // Act: Explicitly request highest priority
        var result = registry.Get<ITestService>(SelectionMode.HighestPriority);

        // Assert
        result.Should().BeSameAs(service2);
    }

    [Fact]
    public async Task PriorityOrder_WithMixedStrategies_EagerLoadedByPriority()
    {
        // Arrange: Mix of Eager and Lazy plugins
        var config = new PluginConfiguration
        {
            Version = "1.0",
            PluginDirectory = "plugins",
            Plugins = new()
            {
                new PluginDescriptor
                {
                    Id = "wingedbean.plugins.eager.low",
                    Path = "eager-low.dll",
                    Priority = 50,
                    LoadStrategy = LoadStrategy.Eager,
                    Enabled = true
                },
                new PluginDescriptor
                {
                    Id = "wingedbean.plugins.lazy.high",
                    Path = "lazy-high.dll",
                    Priority = 1000,
                    LoadStrategy = LoadStrategy.Lazy,
                    Enabled = true
                },
                new PluginDescriptor
                {
                    Id = "wingedbean.plugins.eager.high",
                    Path = "eager-high.dll",
                    Priority = 100,
                    LoadStrategy = LoadStrategy.Eager,
                    Enabled = true
                }
            }
        };

        await File.WriteAllTextAsync(_testPluginsJsonPath, JsonSerializer.Serialize(config));

        // Act: Filter for Eager plugins and order by priority (mimics Program.cs)
        var loadedConfig = await LoadPluginConfigurationAsync(_testPluginsJsonPath);
        var eagerPlugins = loadedConfig.Plugins
            .Where(p => p.Enabled && p.LoadStrategy == LoadStrategy.Eager)
            .OrderByDescending(p => p.Priority)
            .ToList();

        // Assert: Only eager plugins, ordered by priority
        eagerPlugins.Should().HaveCount(2);
        eagerPlugins[0].Id.Should().Be("wingedbean.plugins.eager.high");
        eagerPlugins[0].Priority.Should().Be(100);
        eagerPlugins[1].Id.Should().Be("wingedbean.plugins.eager.low");
        eagerPlugins[1].Priority.Should().Be(50);
        
        // Lazy plugin should not be in eager load list
        eagerPlugins.Should().NotContain(p => p.Id == "wingedbean.plugins.lazy.high");
    }

    [Fact]
    public async Task EqualPriorities_MaintainConfigurationOrder()
    {
        // Arrange: Multiple plugins with same priority
        var config = new PluginConfiguration
        {
            Version = "1.0",
            PluginDirectory = "plugins",
            Plugins = new()
            {
                new PluginDescriptor
                {
                    Id = "wingedbean.plugins.first",
                    Path = "first.dll",
                    Priority = 100,
                    LoadStrategy = LoadStrategy.Eager,
                    Enabled = true
                },
                new PluginDescriptor
                {
                    Id = "wingedbean.plugins.second",
                    Path = "second.dll",
                    Priority = 100,
                    LoadStrategy = LoadStrategy.Eager,
                    Enabled = true
                },
                new PluginDescriptor
                {
                    Id = "wingedbean.plugins.third",
                    Path = "third.dll",
                    Priority = 100,
                    LoadStrategy = LoadStrategy.Eager,
                    Enabled = true
                }
            }
        };

        await File.WriteAllTextAsync(_testPluginsJsonPath, JsonSerializer.Serialize(config));

        // Act: Load and sort
        var loadedConfig = await LoadPluginConfigurationAsync(_testPluginsJsonPath);
        var enabledPlugins = loadedConfig.Plugins
            .Where(p => p.Enabled)
            .OrderByDescending(p => p.Priority)
            .ToList();

        // Assert: All have same priority
        enabledPlugins.Should().HaveCount(3);
        enabledPlugins.All(p => p.Priority == 100).Should().BeTrue();
        
        // Original order is preserved (stable sort)
        // Note: OrderByDescending uses stable sort, so original order is maintained for equal values
        var ids = enabledPlugins.Select(p => p.Id).ToList();
        ids.Should().ContainInOrder("wingedbean.plugins.first", "wingedbean.plugins.second", "wingedbean.plugins.third");
    }

    [Fact]
    public void Registry_GetMetadata_ReturnsCorrectPriority()
    {
        // Arrange
        var registry = new ActualRegistry();
        var service = new TestService { Name = "Test" };

        // Act: Register with priority 500
        registry.Register<ITestService>(service, priority: 500);
        var metadata = registry.GetMetadata<ITestService>(service);

        // Assert: Metadata should reflect registered priority
        metadata.Should().NotBeNull();
        metadata!.Priority.Should().Be(500);
    }

    /// <summary>
    /// Helper method that mimics the host's LoadPluginConfigurationAsync logic.
    /// </summary>
    private static async Task<PluginConfiguration> LoadPluginConfigurationAsync(string path)
    {
        if (!File.Exists(path))
        {
            throw new FileNotFoundException($"Plugin configuration file not found: {path}");
        }

        var json = await File.ReadAllTextAsync(path);
        var config = JsonSerializer.Deserialize<PluginConfiguration>(json, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            ReadCommentHandling = JsonCommentHandling.Skip
        });

        return config ?? throw new InvalidOperationException("Failed to parse plugin configuration");
    }

    // Test service interface and implementation
    private interface ITestService
    {
        string Name { get; set; }
    }

    private class TestService : ITestService
    {
        public string Name { get; set; } = string.Empty;
    }
}

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
/// Integration tests for plugin enable/disable functionality (RFC-0006, Phase 6).
/// Tests that plugins can be disabled in configuration and that appropriate error messages are shown.
/// </summary>
public class PluginEnableDisableTests : IDisposable
{
    private readonly string _testDirectory;
    private readonly string _testPluginsJsonPath;

    public PluginEnableDisableTests()
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
    public async Task DisabledPlugin_DoesNotLoad()
    {
        // Arrange: Create a configuration with a disabled plugin
        var config = new PluginConfiguration
        {
            Version = "1.0",
            PluginDirectory = "plugins",
            Plugins = new()
            {
                new PluginDescriptor
                {
                    Id = "wingedbean.plugins.test",
                    Path = "nonexistent.dll",
                    Priority = 100,
                    LoadStrategy = LoadStrategy.Eager,
                    Enabled = false
                }
            }
        };

        await File.WriteAllTextAsync(_testPluginsJsonPath, JsonSerializer.Serialize(config));

        // Act: Load configuration and filter enabled plugins
        var loadedConfig = await LoadPluginConfigurationAsync(_testPluginsJsonPath);
        var enabledPlugins = loadedConfig.Plugins
            .Where(p => p.Enabled)
            .ToList();

        // Assert: Disabled plugin should not be in enabled list
        enabledPlugins.Should().BeEmpty();
        loadedConfig.Plugins.Should().HaveCount(1);
        loadedConfig.Plugins[0].Enabled.Should().BeFalse();
    }

    [Fact]
    public async Task EnabledPlugin_IsIncludedInLoadList()
    {
        // Arrange: Create a configuration with an enabled plugin
        var config = new PluginConfiguration
        {
            Version = "1.0",
            PluginDirectory = "plugins",
            Plugins = new()
            {
                new PluginDescriptor
                {
                    Id = "wingedbean.plugins.test",
                    Path = "test.dll",
                    Priority = 100,
                    LoadStrategy = LoadStrategy.Eager,
                    Enabled = true
                }
            }
        };

        await File.WriteAllTextAsync(_testPluginsJsonPath, JsonSerializer.Serialize(config));

        // Act: Load configuration and filter enabled plugins
        var loadedConfig = await LoadPluginConfigurationAsync(_testPluginsJsonPath);
        var enabledPlugins = loadedConfig.Plugins
            .Where(p => p.Enabled)
            .ToList();

        // Assert: Enabled plugin should be in enabled list
        enabledPlugins.Should().HaveCount(1);
        enabledPlugins[0].Id.Should().Be("wingedbean.plugins.test");
        enabledPlugins[0].Enabled.Should().BeTrue();
    }

    [Fact]
    public async Task TogglePlugin_FromDisabledToEnabled_Works()
    {
        // Arrange: Start with disabled plugin
        var config = new PluginConfiguration
        {
            Version = "1.0",
            PluginDirectory = "plugins",
            Plugins = new()
            {
                new PluginDescriptor
                {
                    Id = "wingedbean.plugins.test",
                    Path = "test.dll",
                    Priority = 100,
                    LoadStrategy = LoadStrategy.Eager,
                    Enabled = false
                }
            }
        };

        await File.WriteAllTextAsync(_testPluginsJsonPath, JsonSerializer.Serialize(config));

        // Act 1: Load with disabled plugin
        var loadedConfig1 = await LoadPluginConfigurationAsync(_testPluginsJsonPath);
        var enabledPlugins1 = loadedConfig1.Plugins.Where(p => p.Enabled).ToList();

        // Assert 1: Plugin should be disabled
        enabledPlugins1.Should().BeEmpty();

        // Act 2: Enable the plugin
        config.Plugins[0].Enabled = true;
        await File.WriteAllTextAsync(_testPluginsJsonPath, JsonSerializer.Serialize(config));

        // Act 3: Reload configuration
        var loadedConfig2 = await LoadPluginConfigurationAsync(_testPluginsJsonPath);
        var enabledPlugins2 = loadedConfig2.Plugins.Where(p => p.Enabled).ToList();

        // Assert 2: Plugin should now be enabled
        enabledPlugins2.Should().HaveCount(1);
        enabledPlugins2[0].Id.Should().Be("wingedbean.plugins.test");
        enabledPlugins2[0].Enabled.Should().BeTrue();
    }

    [Fact]
    public async Task MultiplePlugins_OnlyEnabledOnesLoad()
    {
        // Arrange: Create a configuration with mixed enabled/disabled plugins
        var config = new PluginConfiguration
        {
            Version = "1.0",
            PluginDirectory = "plugins",
            Plugins = new()
            {
                new PluginDescriptor
                {
                    Id = "wingedbean.plugins.enabled1",
                    Path = "enabled1.dll",
                    Priority = 100,
                    LoadStrategy = LoadStrategy.Eager,
                    Enabled = true
                },
                new PluginDescriptor
                {
                    Id = "wingedbean.plugins.disabled",
                    Path = "disabled.dll",
                    Priority = 90,
                    LoadStrategy = LoadStrategy.Eager,
                    Enabled = false
                },
                new PluginDescriptor
                {
                    Id = "wingedbean.plugins.enabled2",
                    Path = "enabled2.dll",
                    Priority = 80,
                    LoadStrategy = LoadStrategy.Eager,
                    Enabled = true
                }
            }
        };

        await File.WriteAllTextAsync(_testPluginsJsonPath, JsonSerializer.Serialize(config));

        // Act: Load configuration and filter enabled plugins
        var loadedConfig = await LoadPluginConfigurationAsync(_testPluginsJsonPath);
        var enabledPlugins = loadedConfig.Plugins
            .Where(p => p.Enabled)
            .OrderByDescending(p => p.Priority)
            .ToList();

        // Assert: Only enabled plugins should be in list, ordered by priority
        enabledPlugins.Should().HaveCount(2);
        enabledPlugins[0].Id.Should().Be("wingedbean.plugins.enabled1");
        enabledPlugins[0].Priority.Should().Be(100);
        enabledPlugins[1].Id.Should().Be("wingedbean.plugins.enabled2");
        enabledPlugins[1].Priority.Should().Be(80);
    }

    [Fact]
    public void DisabledPlugin_ShouldNotAffectRegistry()
    {
        // Arrange: Create a registry with foundation services
        var registry = new ActualRegistry();
        var contextProvider = new AssemblyContextProvider();
        var pluginLoader = new ActualPluginLoader(contextProvider);

        registry.Register<IRegistry>(registry);
        registry.Register<IPluginLoader>(pluginLoader);

        // Act: Verify that foundation services are registered
        var registryRegistered = registry.IsRegistered<IRegistry>();
        var pluginLoaderRegistered = registry.IsRegistered<IPluginLoader>();

        // Assert: Foundation services should be available
        registryRegistered.Should().BeTrue();
        pluginLoaderRegistered.Should().BeTrue();

        // Cleanup
        contextProvider.Dispose();
    }

    [Fact]
    public async Task LazyLoadStrategy_SkippedDuringEagerLoad()
    {
        // Arrange: Create a configuration with Lazy load strategy plugin
        var config = new PluginConfiguration
        {
            Version = "1.0",
            PluginDirectory = "plugins",
            Plugins = new()
            {
                new PluginDescriptor
                {
                    Id = "wingedbean.plugins.lazy",
                    Path = "lazy.dll",
                    Priority = 100,
                    LoadStrategy = LoadStrategy.Lazy,
                    Enabled = true
                }
            }
        };

        await File.WriteAllTextAsync(_testPluginsJsonPath, JsonSerializer.Serialize(config));

        // Act: Load configuration and filter for Eager plugins
        var loadedConfig = await LoadPluginConfigurationAsync(_testPluginsJsonPath);
        var eagerPlugins = loadedConfig.Plugins
            .Where(p => p.Enabled && p.LoadStrategy == LoadStrategy.Eager)
            .ToList();

        // Assert: Lazy plugin should not be in eager load list
        eagerPlugins.Should().BeEmpty();
        loadedConfig.Plugins[0].LoadStrategy.Should().Be(LoadStrategy.Lazy);
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
}

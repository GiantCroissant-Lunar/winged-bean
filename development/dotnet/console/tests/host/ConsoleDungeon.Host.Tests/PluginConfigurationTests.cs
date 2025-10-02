using System.Text.Json;
using FluentAssertions;
using Xunit;

namespace ConsoleDungeon.Host.Tests;

/// <summary>
/// Unit tests for PluginConfiguration JSON serialization.
/// </summary>
public class PluginConfigurationTests
{
    [Fact]
    public void Constructor_Default_CreatesInstance()
    {
        // Act
        var config = new PluginConfiguration();

        // Assert
        config.Should().NotBeNull();
        config.Version.Should().Be("1.0");
        config.PluginDirectory.Should().Be("plugins");
        config.Plugins.Should().NotBeNull();
        config.Plugins.Should().BeEmpty();
    }

    [Fact]
    public void Deserialize_ValidJson_ReturnsConfiguration()
    {
        // Arrange
        var json = """
        {
          "version": "1.0",
          "pluginDirectory": "plugins",
          "plugins": [
            {
              "id": "wingedbean.plugins.config",
              "path": "plugins/WingedBean.Plugins.Config.dll",
              "priority": 1000,
              "loadStrategy": "Eager",
              "enabled": true,
              "metadata": {
                "description": "Configuration service",
                "author": "WingedBean",
                "version": "1.0.0"
              },
              "dependencies": ["wingedbean.contracts.core"]
            },
            {
              "id": "wingedbean.plugins.websocket",
              "path": "plugins/WingedBean.Plugins.WebSocket.dll",
              "priority": 100,
              "loadStrategy": "Lazy",
              "enabled": false
            }
          ]
        }
        """;

        // Act
        var config = JsonSerializer.Deserialize<PluginConfiguration>(json);

        // Assert
        config.Should().NotBeNull();
        config!.Version.Should().Be("1.0");
        config.PluginDirectory.Should().Be("plugins");
        config.Plugins.Should().HaveCount(2);

        var plugin1 = config.Plugins[0];
        plugin1.Id.Should().Be("wingedbean.plugins.config");
        plugin1.Path.Should().Be("plugins/WingedBean.Plugins.Config.dll");
        plugin1.Priority.Should().Be(1000);
        plugin1.LoadStrategy.Should().Be(LoadStrategy.Eager);
        plugin1.Enabled.Should().BeTrue();
        plugin1.Metadata.Should().NotBeNull();
        plugin1.Metadata!["description"].Should().Be("Configuration service");
        plugin1.Metadata["author"].Should().Be("WingedBean");
        plugin1.Metadata["version"].Should().Be("1.0.0");
        plugin1.Dependencies.Should().NotBeNull();
        plugin1.Dependencies.Should().Contain("wingedbean.contracts.core");

        var plugin2 = config.Plugins[1];
        plugin2.Id.Should().Be("wingedbean.plugins.websocket");
        plugin2.Path.Should().Be("plugins/WingedBean.Plugins.WebSocket.dll");
        plugin2.Priority.Should().Be(100);
        plugin2.LoadStrategy.Should().Be(LoadStrategy.Lazy);
        plugin2.Enabled.Should().BeFalse();
        plugin2.Metadata.Should().BeNull();
        plugin2.Dependencies.Should().BeNull();
    }

    [Fact]
    public void Serialize_Configuration_ReturnsValidJson()
    {
        // Arrange
        var config = new PluginConfiguration
        {
            Version = "1.0",
            PluginDirectory = "plugins",
            Plugins = new()
            {
                new PluginDescriptor
                {
                    Id = "test.plugin",
                    Path = "plugins/Test.dll",
                    Priority = 50,
                    LoadStrategy = LoadStrategy.Explicit,
                    Enabled = true,
                    Metadata = new Dictionary<string, string>
                    {
                        { "description", "Test plugin" }
                    },
                    Dependencies = new List<string> { "dep1" }
                }
            }
        };

        // Act
        var json = JsonSerializer.Serialize(config, new JsonSerializerOptions 
        { 
            WriteIndented = true 
        });

        // Assert
        json.Should().NotBeNullOrEmpty();
        json.Should().Contain("\"version\": \"1.0\"");
        json.Should().Contain("\"pluginDirectory\": \"plugins\"");
        json.Should().Contain("\"id\": \"test.plugin\"");
        json.Should().Contain("\"path\": \"plugins/Test.dll\"");
        json.Should().Contain("\"priority\": 50");
        json.Should().Contain("\"loadStrategy\": \"Explicit\"");
        json.Should().Contain("\"enabled\": true");
    }

    [Fact]
    public void RoundTrip_SerializeAndDeserialize_PreservesData()
    {
        // Arrange
        var original = new PluginConfiguration
        {
            Version = "1.0",
            PluginDirectory = "custom-plugins",
            Plugins = new()
            {
                new PluginDescriptor
                {
                    Id = "plugin.one",
                    Path = "path/to/plugin1.dll",
                    Priority = 200,
                    LoadStrategy = LoadStrategy.Eager,
                    Enabled = true,
                    Metadata = new Dictionary<string, string>
                    {
                        { "key1", "value1" },
                        { "key2", "value2" }
                    },
                    Dependencies = new List<string> { "dep1", "dep2" }
                },
                new PluginDescriptor
                {
                    Id = "plugin.two",
                    Path = "path/to/plugin2.dll",
                    Priority = 100,
                    LoadStrategy = LoadStrategy.Lazy,
                    Enabled = false
                }
            }
        };

        // Act
        var json = JsonSerializer.Serialize(original);
        var deserialized = JsonSerializer.Deserialize<PluginConfiguration>(json);

        // Assert
        deserialized.Should().NotBeNull();
        deserialized!.Version.Should().Be(original.Version);
        deserialized.PluginDirectory.Should().Be(original.PluginDirectory);
        deserialized.Plugins.Should().HaveCount(2);
        
        deserialized.Plugins[0].Id.Should().Be(original.Plugins[0].Id);
        deserialized.Plugins[0].Path.Should().Be(original.Plugins[0].Path);
        deserialized.Plugins[0].Priority.Should().Be(original.Plugins[0].Priority);
        deserialized.Plugins[0].LoadStrategy.Should().Be(original.Plugins[0].LoadStrategy);
        deserialized.Plugins[0].Enabled.Should().Be(original.Plugins[0].Enabled);
        deserialized.Plugins[0].Metadata.Should().BeEquivalentTo(original.Plugins[0].Metadata);
        deserialized.Plugins[0].Dependencies.Should().BeEquivalentTo(original.Plugins[0].Dependencies);

        deserialized.Plugins[1].Id.Should().Be(original.Plugins[1].Id);
        deserialized.Plugins[1].Path.Should().Be(original.Plugins[1].Path);
        deserialized.Plugins[1].Priority.Should().Be(original.Plugins[1].Priority);
        deserialized.Plugins[1].LoadStrategy.Should().Be(original.Plugins[1].LoadStrategy);
        deserialized.Plugins[1].Enabled.Should().Be(original.Plugins[1].Enabled);
        deserialized.Plugins[1].Metadata.Should().BeNull();
        deserialized.Plugins[1].Dependencies.Should().BeNull();
    }

    [Fact]
    public void Deserialize_MinimalJson_UsesDefaultValues()
    {
        // Arrange
        var json = """
        {
          "plugins": []
        }
        """;

        // Act
        var config = JsonSerializer.Deserialize<PluginConfiguration>(json);

        // Assert
        config.Should().NotBeNull();
        config!.Version.Should().Be("1.0");
        config.PluginDirectory.Should().Be("plugins");
        config.Plugins.Should().NotBeNull();
        config.Plugins.Should().BeEmpty();
    }

    [Fact]
    public void PluginDescriptor_DefaultValues_AreCorrect()
    {
        // Act
        var descriptor = new PluginDescriptor();

        // Assert
        descriptor.Id.Should().Be("");
        descriptor.Path.Should().Be("");
        descriptor.Priority.Should().Be(0);
        descriptor.LoadStrategy.Should().Be(LoadStrategy.Eager);
        descriptor.Enabled.Should().BeTrue();
        descriptor.Metadata.Should().BeNull();
        descriptor.Dependencies.Should().BeNull();
    }

    [Fact]
    public void LoadStrategy_AllValues_CanBeSerializedAndDeserialized()
    {
        // Arrange
        var strategies = new[] { LoadStrategy.Eager, LoadStrategy.Lazy, LoadStrategy.Explicit };

        foreach (var strategy in strategies)
        {
            var descriptor = new PluginDescriptor
            {
                Id = "test",
                LoadStrategy = strategy
            };

            // Act
            var json = JsonSerializer.Serialize(descriptor);
            var deserialized = JsonSerializer.Deserialize<PluginDescriptor>(json);

            // Assert
            deserialized.Should().NotBeNull();
            deserialized!.LoadStrategy.Should().Be(strategy);
        }
    }
}

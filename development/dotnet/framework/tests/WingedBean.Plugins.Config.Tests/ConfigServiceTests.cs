using FluentAssertions;
using Microsoft.Extensions.Configuration;
using WingedBean.Plugins.Config;
using Xunit;

namespace WingedBean.Plugins.Config.Tests;

/// <summary>
/// Tests for the Config service.
/// </summary>
public class ConfigServiceTests
{
    [Fact]
    public void GetValue_WithExistingKey_ShouldReturnValue()
    {
        // Arrange
        var configData = new Dictionary<string, string?>
        {
            { "TestKey", "TestValue" },
            { "Nested:Key", "NestedValue" }
        };
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(configData)
            .Build();
        var service = new ConfigService(configuration);

        // Act
        var value = service.GetValue<string>("TestKey");

        // Assert
        value.Should().Be("TestValue");
    }

    [Fact]
    public void GetValue_WithNonExistentKey_ShouldReturnDefault()
    {
        // Arrange
        var configuration = new ConfigurationBuilder().Build();
        var service = new ConfigService(configuration);

        // Act
        var value = service.GetValue<string>("NonExistent");

        // Assert
        value.Should().BeNull();
    }

    [Fact]
    public void GetValue_WithDefaultValue_ShouldReturnDefaultWhenMissing()
    {
        // Arrange
        var configuration = new ConfigurationBuilder().Build();
        var service = new ConfigService(configuration);

        // Act
        var value = service.GetValue("NonExistent", "DefaultValue");

        // Assert
        value.Should().Be("DefaultValue");
    }

    [Fact]
    public void GetSection_ShouldReturnConfigurationSection()
    {
        // Arrange
        var configData = new Dictionary<string, string?>
        {
            { "Section:Key1", "Value1" },
            { "Section:Key2", "Value2" }
        };
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(configData)
            .Build();
        var service = new ConfigService(configuration);

        // Act
        var section = service.GetSection("Section");

        // Assert
        section.Should().NotBeNull();
        section["Key1"].Should().Be("Value1");
        section["Key2"].Should().Be("Value2");
    }

    [Fact]
    public void Bind_ShouldPopulateObject()
    {
        // Arrange
        var configData = new Dictionary<string, string?>
        {
            { "TestConfig:Name", "Test" },
            { "TestConfig:Value", "42" }
        };
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(configData)
            .Build();
        var service = new ConfigService(configuration);
        var testConfig = new TestConfig();

        // Act
        service.Bind("TestConfig", testConfig);

        // Assert
        testConfig.Name.Should().Be("Test");
        testConfig.Value.Should().Be(42);
    }

    [Fact]
    public void GetRequiredValue_WithMissingKey_ShouldThrow()
    {
        // Arrange
        var configuration = new ConfigurationBuilder().Build();
        var service = new ConfigService(configuration);

        // Act
        Action act = () => service.GetRequiredValue<string>("MissingKey");

        // Assert
        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Reload_ShouldRefreshConfiguration()
    {
        // Arrange
        var configData = new Dictionary<string, string?>
        {
            { "DynamicKey", "InitialValue" }
        };
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(configData)
            .Build();
        var service = new ConfigService(configuration);

        // Act
        service.Reload();
        var value = service.GetValue<string>("DynamicKey");

        // Assert
        value.Should().Be("InitialValue");
    }
}

public class TestConfig
{
    public string Name { get; set; } = string.Empty;
    public int Value { get; set; }
}

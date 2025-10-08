using FluentAssertions;
using Microsoft.Extensions.Configuration;
using WingedBean.Contracts.Config;
using Xunit;

namespace WingedBean.Plugins.Config.Tests;

/// <summary>
/// Unit tests for ConfigService.
/// </summary>
public class ConfigServiceTests
{
    [Fact]
    public void Constructor_Default_CreatesInstance()
    {
        // Act
        var service = new ConfigService();

        // Assert
        service.Should().NotBeNull();
        service.Should().BeAssignableTo<IConfigService>();
    }

    [Fact]
    public void Constructor_WithConfiguration_CreatesInstance()
    {
        // Arrange
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                { "TestKey", "TestValue" }
            })
            .Build();

        // Act
        var service = new ConfigService(configuration);

        // Assert
        service.Should().NotBeNull();
        service.Get("TestKey").Should().Be("TestValue");
    }

    [Fact]
    public void Get_WithValidKey_ReturnsValue()
    {
        // Arrange
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                { "Key1", "Value1" }
            })
            .Build();
        var service = new ConfigService(configuration);

        // Act
        var result = service.Get("Key1");

        // Assert
        result.Should().Be("Value1");
    }

    [Fact]
    public void Get_WithInvalidKey_ReturnsNull()
    {
        // Arrange
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>())
            .Build();
        var service = new ConfigService(configuration);

        // Act
        var result = service.Get("NonExistentKey");

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void Get_WithNullKey_ThrowsArgumentException()
    {
        // Arrange
        var service = new ConfigService();

        // Act & Assert
        Assert.Throws<ArgumentException>(() => service.Get(null!));
    }

    [Fact]
    public void Get_WithEmptyKey_ThrowsArgumentException()
    {
        // Arrange
        var service = new ConfigService();

        // Act & Assert
        Assert.Throws<ArgumentException>(() => service.Get(string.Empty));
    }

    [Fact]
    public void GetTyped_WithInt_ReturnsTypedValue()
    {
        // Arrange
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                { "Port", "8080" }
            })
            .Build();
        var service = new ConfigService(configuration);

        // Act
        var result = service.Get<int>("Port");

        // Assert
        result.Should().Be(8080);
    }

    [Fact]
    public void GetTyped_WithBool_ReturnsTypedValue()
    {
        // Arrange
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                { "Enabled", "true" }
            })
            .Build();
        var service = new ConfigService(configuration);

        // Act
        var result = service.Get<bool>("Enabled");

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void GetTyped_WithInvalidConversion_ReturnsDefault()
    {
        // Arrange
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                { "Invalid", "not-a-number" }
            })
            .Build();
        var service = new ConfigService(configuration);

        // Act
        var result = service.Get<int>("Invalid");

        // Assert
        result.Should().Be(0); // default(int)
    }

    [Fact]
    public void GetSection_WithValidKey_ReturnsSection()
    {
        // Arrange
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                { "Database:Host", "localhost" },
                { "Database:Port", "5432" }
            })
            .Build();
        var service = new ConfigService(configuration);

        // Act
        var section = service.GetSection("Database");

        // Assert
        section.Should().NotBeNull();
        section.Key.Should().Be("Database");
    }

    [Fact]
    public void GetSection_WithNestedKeys_CanAccessChildren()
    {
        // Arrange
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                { "Database:Host", "localhost" },
                { "Database:Port", "5432" }
            })
            .Build();
        var service = new ConfigService(configuration);

        // Act
        var section = service.GetSection("Database");
        var host = section.GetSection("Host");

        // Assert
        host.Value.Should().Be("localhost");
    }

    [Fact]
    public void Set_WithValidKey_UpdatesValue()
    {
        // Arrange
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                { "Key1", "OldValue" }
            })
            .Build();
        var service = new ConfigService(configuration);

        // Act
        service.Set("Key1", "NewValue");

        // Assert
        service.Get("Key1").Should().Be("NewValue");
    }

    [Fact]
    public void Set_WithNewKey_AddsValue()
    {
        // Arrange
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>())
            .Build();
        var service = new ConfigService(configuration);

        // Act
        service.Set("NewKey", "NewValue");

        // Assert
        service.Get("NewKey").Should().Be("NewValue");
    }

    [Fact]
    public void Set_RaisesConfigChangedEvent()
    {
        // Arrange
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                { "Key1", "OldValue" }
            })
            .Build();
        var service = new ConfigService(configuration);
        ConfigChangedEventArgs? eventArgs = null;
        service.ConfigChanged += (sender, e) => eventArgs = e;

        // Act
        service.Set("Key1", "NewValue");

        // Assert
        eventArgs.Should().NotBeNull();
        eventArgs!.Key.Should().Be("Key1");
        eventArgs.OldValue.Should().Be("OldValue");
        eventArgs.NewValue.Should().Be("NewValue");
    }

    [Fact]
    public void Exists_WithExistingKey_ReturnsTrue()
    {
        // Arrange
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                { "ExistingKey", "Value" }
            })
            .Build();
        var service = new ConfigService(configuration);

        // Act
        var result = service.Exists("ExistingKey");

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void Exists_WithNonExistingKey_ReturnsFalse()
    {
        // Arrange
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>())
            .Build();
        var service = new ConfigService(configuration);

        // Act
        var result = service.Exists("NonExistingKey");

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void Exists_WithNullKey_ReturnsFalse()
    {
        // Arrange
        var service = new ConfigService();

        // Act
        var result = service.Exists(null!);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task ReloadAsync_CompletesSuccessfully()
    {
        // Arrange
        var service = new ConfigService();

        // Act
        var task = service.ReloadAsync();
        await task;

        // Assert
        task.IsCompleted.Should().BeTrue();
    }

    [Fact]
    public void ConfigService_HasPluginAttribute()
    {
        // Arrange
        var type = typeof(ConfigService);

        // Act
        var attribute = type.GetCustomAttributes(typeof(Plate.PluginManoi.Contracts.PluginAttribute), false)
            .FirstOrDefault() as Plate.PluginManoi.Contracts.PluginAttribute;

        // Assert
        attribute.Should().NotBeNull();
        attribute!.Name.Should().Be("ConfigService");
        attribute.Provides.Should().Contain(typeof(IConfigService));
        attribute.Priority.Should().Be(100);
    }

    [Fact]
    public void GetSection_GetChildren_ReturnsChildSections()
    {
        // Arrange
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                { "Database:Primary:Host", "localhost" },
                { "Database:Primary:Port", "5432" },
                { "Database:Secondary:Host", "backup" },
                { "Database:Secondary:Port", "5433" }
            })
            .Build();
        var service = new ConfigService(configuration);

        // Act
        var section = service.GetSection("Database");
        var children = section.GetChildren().ToList();

        // Assert
        children.Should().HaveCount(2);
        children.Should().Contain(c => c.Key == "Primary");
        children.Should().Contain(c => c.Key == "Secondary");
    }

    [Fact]
    public void GetSection_Bind_BindsToObject()
    {
        // Arrange
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                { "Database:Host", "localhost" },
                { "Database:Port", "5432" }
            })
            .Build();
        var service = new ConfigService(configuration);
        var options = new DatabaseOptions();

        // Act
        var section = service.GetSection("Database");
        section.Bind(options);

        // Assert
        options.Host.Should().Be("localhost");
        options.Port.Should().Be(5432);
    }

    [Fact]
    public void GetSection_GetTyped_ReturnsTypedObject()
    {
        // Arrange
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                { "Database:Host", "localhost" },
                { "Database:Port", "5432" }
            })
            .Build();
        var service = new ConfigService(configuration);

        // Act
        var section = service.GetSection("Database");
        var options = section.Get<DatabaseOptions>();

        // Assert
        options.Should().NotBeNull();
        options!.Host.Should().Be("localhost");
        options.Port.Should().Be(5432);
    }

    [Fact]
    public void Get_WithNestedKey_ReturnsValue()
    {
        // Arrange
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                { "Database:Host", "localhost" }
            })
            .Build();
        var service = new ConfigService(configuration);

        // Act
        var result = service.Get("Database:Host");

        // Assert
        result.Should().Be("localhost");
    }

    // Test helper class
    private class DatabaseOptions
    {
        public string Host { get; set; } = string.Empty;
        public int Port { get; set; }
    }
}

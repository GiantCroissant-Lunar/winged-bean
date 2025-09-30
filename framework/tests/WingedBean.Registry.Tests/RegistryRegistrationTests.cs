using FluentAssertions;
using WingedBean.Contracts.Core;
using WingedBean.Registry;
using Xunit;

namespace WingedBean.Registry.Tests;

/// <summary>
/// Unit tests for ActualRegistry service registration functionality.
/// </summary>
public class RegistryRegistrationTests
{
    private interface ITestService { }
    private class TestServiceA : ITestService { }
    private class TestServiceB : ITestService { }

    [Fact]
    public void Register_WithPriority_ShouldRegisterService()
    {
        // Arrange
        var registry = new ActualRegistry();
        var service = new TestServiceA();

        // Act
        registry.Register<ITestService>(service, priority: 10);

        // Assert
        registry.IsRegistered<ITestService>().Should().BeTrue();
    }

    [Fact]
    public void Register_WithMetadata_ShouldRegisterService()
    {
        // Arrange
        var registry = new ActualRegistry();
        var service = new TestServiceA();
        var metadata = new ServiceMetadata
        {
            Name = "TestService",
            Priority = 5,
            Version = "1.0.0",
            Platform = "All"
        };

        // Act
        registry.Register<ITestService>(service, metadata);

        // Assert
        registry.IsRegistered<ITestService>().Should().BeTrue();
    }

    [Fact]
    public void Register_NullImplementation_ShouldThrowArgumentNullException()
    {
        // Arrange
        var registry = new ActualRegistry();

        // Act & Assert
        var act = () => registry.Register<ITestService>(null!, priority: 0);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Register_NullMetadata_ShouldThrowArgumentNullException()
    {
        // Arrange
        var registry = new ActualRegistry();
        var service = new TestServiceA();

        // Act & Assert
        var act = () => registry.Register<ITestService>(service, null!);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Register_MultipleServices_ShouldRegisterAll()
    {
        // Arrange
        var registry = new ActualRegistry();
        var serviceA = new TestServiceA();
        var serviceB = new TestServiceB();

        // Act
        registry.Register<ITestService>(serviceA, priority: 1);
        registry.Register<ITestService>(serviceB, priority: 2);

        // Assert
        var all = registry.GetAll<ITestService>().ToList();
        all.Should().HaveCount(2);
        all.Should().Contain(serviceA);
        all.Should().Contain(serviceB);
    }

    [Fact]
    public void IsRegistered_NoServicesRegistered_ShouldReturnFalse()
    {
        // Arrange
        var registry = new ActualRegistry();

        // Act & Assert
        registry.IsRegistered<ITestService>().Should().BeFalse();
    }

    [Fact]
    public void GetMetadata_RegisteredService_ShouldReturnMetadata()
    {
        // Arrange
        var registry = new ActualRegistry();
        var service = new TestServiceA();
        var metadata = new ServiceMetadata
        {
            Name = "TestService",
            Priority = 10,
            Version = "1.0.0",
            Platform = "Windows"
        };

        // Act
        registry.Register<ITestService>(service, metadata);
        var retrievedMetadata = registry.GetMetadata<ITestService>(service);

        // Assert
        retrievedMetadata.Should().NotBeNull();
        retrievedMetadata!.Name.Should().Be("TestService");
        retrievedMetadata.Priority.Should().Be(10);
        retrievedMetadata.Version.Should().Be("1.0.0");
        retrievedMetadata.Platform.Should().Be("Windows");
    }

    [Fact]
    public void GetMetadata_UnregisteredService_ShouldReturnNull()
    {
        // Arrange
        var registry = new ActualRegistry();
        var service = new TestServiceA();

        // Act
        var metadata = registry.GetMetadata<ITestService>(service);

        // Assert
        metadata.Should().BeNull();
    }

    [Fact]
    public void GetMetadata_NullImplementation_ShouldThrowArgumentNullException()
    {
        // Arrange
        var registry = new ActualRegistry();

        // Act & Assert
        var act = () => registry.GetMetadata<ITestService>(null!);
        act.Should().Throw<ArgumentNullException>();
    }
}

using FluentAssertions;
using WingedBean.Contracts.Core;
using WingedBean.Registry;
using Xunit;

namespace WingedBean.Registry.Tests;

/// <summary>
/// Unit tests for ActualRegistry unregistration functionality.
/// </summary>
public class RegistryUnregistrationTests
{
    private interface ITestService { }
    private class TestServiceA : ITestService { }
    private class TestServiceB : ITestService { }

    [Fact]
    public void Unregister_RegisteredService_ShouldRemoveService()
    {
        // Arrange
        var registry = new ActualRegistry();
        var service = new TestServiceA();
        registry.Register<ITestService>(service);

        // Act
        var result = registry.Unregister<ITestService>(service);

        // Assert
        result.Should().BeTrue();
        registry.IsRegistered<ITestService>().Should().BeFalse();
    }

    [Fact]
    public void Unregister_UnregisteredService_ShouldReturnFalse()
    {
        // Arrange
        var registry = new ActualRegistry();
        var service = new TestServiceA();

        // Act
        var result = registry.Unregister<ITestService>(service);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void Unregister_NullImplementation_ShouldThrowArgumentNullException()
    {
        // Arrange
        var registry = new ActualRegistry();

        // Act & Assert
        var act = () => registry.Unregister<ITestService>(null!);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Unregister_SpecificService_ShouldOnlyRemoveThatService()
    {
        // Arrange
        var registry = new ActualRegistry();
        var serviceA = new TestServiceA();
        var serviceB = new TestServiceB();
        registry.Register<ITestService>(serviceA);
        registry.Register<ITestService>(serviceB);

        // Act
        var result = registry.Unregister<ITestService>(serviceA);

        // Assert
        result.Should().BeTrue();
        registry.IsRegistered<ITestService>().Should().BeTrue();
        var remaining = registry.GetAll<ITestService>().ToList();
        remaining.Should().HaveCount(1);
        remaining.Should().Contain(serviceB);
        remaining.Should().NotContain(serviceA);
    }

    [Fact]
    public void UnregisterAll_MultipleServices_ShouldRemoveAllServices()
    {
        // Arrange
        var registry = new ActualRegistry();
        var serviceA = new TestServiceA();
        var serviceB = new TestServiceB();
        registry.Register<ITestService>(serviceA);
        registry.Register<ITestService>(serviceB);

        // Act
        registry.UnregisterAll<ITestService>();

        // Assert
        registry.IsRegistered<ITestService>().Should().BeFalse();
        registry.GetAll<ITestService>().Should().BeEmpty();
    }

    [Fact]
    public void UnregisterAll_NoServicesRegistered_ShouldNotThrow()
    {
        // Arrange
        var registry = new ActualRegistry();

        // Act & Assert
        var act = () => registry.UnregisterAll<ITestService>();
        act.Should().NotThrow();
        registry.IsRegistered<ITestService>().Should().BeFalse();
    }

    [Fact]
    public void Unregister_AfterUnregisterAll_ShouldReturnFalse()
    {
        // Arrange
        var registry = new ActualRegistry();
        var service = new TestServiceA();
        registry.Register<ITestService>(service);
        registry.UnregisterAll<ITestService>();

        // Act
        var result = registry.Unregister<ITestService>(service);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void GetMetadata_AfterUnregister_ShouldReturnNull()
    {
        // Arrange
        var registry = new ActualRegistry();
        var service = new TestServiceA();
        var metadata = new ServiceMetadata { Priority = 10 };
        registry.Register<ITestService>(service, metadata);
        registry.Unregister<ITestService>(service);

        // Act
        var retrievedMetadata = registry.GetMetadata<ITestService>(service);

        // Assert
        retrievedMetadata.Should().BeNull();
    }

    [Fact]
    public void Register_AfterUnregister_ShouldRegisterServiceAgain()
    {
        // Arrange
        var registry = new ActualRegistry();
        var service = new TestServiceA();
        registry.Register<ITestService>(service, priority: 5);
        registry.Unregister<ITestService>(service);

        // Act
        registry.Register<ITestService>(service, priority: 10);

        // Assert
        registry.IsRegistered<ITestService>().Should().BeTrue();
        var retrievedMetadata = registry.GetMetadata<ITestService>(service);
        retrievedMetadata.Should().NotBeNull();
        retrievedMetadata!.Priority.Should().Be(10);
    }
}

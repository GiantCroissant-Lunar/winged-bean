using FluentAssertions;
using WingedBean.Contracts.Core;
using WingedBean.Registry;
using Xunit;

namespace WingedBean.Registry.Tests;

/// <summary>
/// Unit tests for ActualRegistry selection strategies (One, HighestPriority, All).
/// </summary>
public class RegistrySelectionTests
{
    private interface ITestService { }
    private class TestServiceA : ITestService { }
    private class TestServiceB : ITestService { }
    private class TestServiceC : ITestService { }

    [Fact]
    public void Get_SelectionModeOne_SingleService_ShouldReturnService()
    {
        // Arrange
        var registry = new ActualRegistry();
        var service = new TestServiceA();
        registry.Register<ITestService>(service);

        // Act
        var result = registry.Get<ITestService>(SelectionMode.One);

        // Assert
        result.Should().BeSameAs(service);
    }

    [Fact]
    public void Get_SelectionModeOne_MultipleServices_ShouldThrowMultipleServicesException()
    {
        // Arrange
        var registry = new ActualRegistry();
        var serviceA = new TestServiceA();
        var serviceB = new TestServiceB();
        registry.Register<ITestService>(serviceA);
        registry.Register<ITestService>(serviceB);

        // Act & Assert
        var act = () => registry.Get<ITestService>(SelectionMode.One);
        act.Should().Throw<MultipleServicesException>()
            .Which.Count.Should().Be(2);
    }

    [Fact]
    public void Get_SelectionModeHighestPriority_ShouldReturnHighestPriorityService()
    {
        // Arrange
        var registry = new ActualRegistry();
        var serviceA = new TestServiceA();
        var serviceB = new TestServiceB();
        var serviceC = new TestServiceC();

        registry.Register<ITestService>(serviceA, priority: 5);
        registry.Register<ITestService>(serviceB, priority: 10);
        registry.Register<ITestService>(serviceC, priority: 3);

        // Act
        var result = registry.Get<ITestService>(SelectionMode.HighestPriority);

        // Assert
        result.Should().BeSameAs(serviceB);
    }

    [Fact]
    public void Get_SelectionModeHighestPriority_DefaultPriority_ShouldReturnFirstService()
    {
        // Arrange
        var registry = new ActualRegistry();
        var serviceA = new TestServiceA();
        var serviceB = new TestServiceB();

        registry.Register<ITestService>(serviceA); // Default priority 0
        registry.Register<ITestService>(serviceB); // Default priority 0

        // Act
        var result = registry.Get<ITestService>(SelectionMode.HighestPriority);

        // Assert - Should return the first one added (serviceA) since priorities are equal
        result.Should().BeSameAs(serviceA);
    }

    [Fact]
    public void Get_SelectionModeAll_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var registry = new ActualRegistry();
        var service = new TestServiceA();
        registry.Register<ITestService>(service);

        // Act & Assert
        var act = () => registry.Get<ITestService>(SelectionMode.All);
        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Get_NoServicesRegistered_ShouldThrowServiceNotFoundException()
    {
        // Arrange
        var registry = new ActualRegistry();

        // Act & Assert
        var act = () => registry.Get<ITestService>();
        act.Should().Throw<ServiceNotFoundException>()
            .Which.ServiceType.Should().Be(typeof(ITestService));
    }

    [Fact]
    public void Get_DefaultSelectionMode_ShouldUseHighestPriority()
    {
        // Arrange
        var registry = new ActualRegistry();
        var serviceA = new TestServiceA();
        var serviceB = new TestServiceB();

        registry.Register<ITestService>(serviceA, priority: 1);
        registry.Register<ITestService>(serviceB, priority: 5);

        // Act
        var result = registry.Get<ITestService>(); // No mode specified, should use HighestPriority

        // Assert
        result.Should().BeSameAs(serviceB);
    }

    [Fact]
    public void GetAll_MultipleServices_ShouldReturnAllServices()
    {
        // Arrange
        var registry = new ActualRegistry();
        var serviceA = new TestServiceA();
        var serviceB = new TestServiceB();
        var serviceC = new TestServiceC();

        registry.Register<ITestService>(serviceA, priority: 1);
        registry.Register<ITestService>(serviceB, priority: 2);
        registry.Register<ITestService>(serviceC, priority: 3);

        // Act
        var result = registry.GetAll<ITestService>().ToList();

        // Assert
        result.Should().HaveCount(3);
        result.Should().Contain(serviceA);
        result.Should().Contain(serviceB);
        result.Should().Contain(serviceC);
    }

    [Fact]
    public void GetAll_NoServicesRegistered_ShouldReturnEmptyCollection()
    {
        // Arrange
        var registry = new ActualRegistry();

        // Act
        var result = registry.GetAll<ITestService>();

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public void GetAll_SingleService_ShouldReturnSingleServiceCollection()
    {
        // Arrange
        var registry = new ActualRegistry();
        var service = new TestServiceA();
        registry.Register<ITestService>(service);

        // Act
        var result = registry.GetAll<ITestService>().ToList();

        // Assert
        result.Should().HaveCount(1);
        result.Should().Contain(service);
    }
}

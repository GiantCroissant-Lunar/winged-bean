using FluentAssertions;
using WingedBean.Plugins.WebSocket;
using WingedBean.Contracts.WebSocket;
using Xunit;

namespace WingedBean.Plugins.WebSocket.Tests;

/// <summary>
/// Tests for SuperSocketWebSocketService
/// </summary>
public class SuperSocketWebSocketServiceTests
{
    [Fact]
    public void SuperSocketWebSocketService_ImplementsIWebSocketService()
    {
        // Arrange & Act
        var service = new SuperSocketWebSocketService();

        // Assert
        service.Should().BeAssignableTo<IWebSocketService>();
    }

    [Fact]
    public void Start_ConfiguresServerWithPort()
    {
        // Arrange
        var service = new SuperSocketWebSocketService();
        int testPort = 4050; // Use a different port to avoid conflicts

        // Act
        Action act = () => service.Start(testPort);

        // Assert - Should not throw
        act.Should().NotThrow();

        // Give server time to start
        Thread.Sleep(1000);
    }

    [Fact]
    public void MessageReceived_Event_CanBeSubscribed()
    {
        // Arrange
        var service = new SuperSocketWebSocketService();
        bool subscribed = false;

        // Act
        Action act = () =>
        {
            service.MessageReceived += (msg) => { };
            subscribed = true;
        };

        // Assert - Event subscription should work without throwing
        act.Should().NotThrow();
        subscribed.Should().BeTrue();
    }

    [Fact]
    public void Broadcast_ThrowsException_WhenNotStarted()
    {
        // Arrange
        var service = new SuperSocketWebSocketService();

        // Act
        Action act = () => service.Broadcast("test message");

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*not been started*");
    }

    [Fact]
    public void Broadcast_DoesNotThrow_AfterStart()
    {
        // Arrange
        var service = new SuperSocketWebSocketService();
        service.Start(4051); // Use a different port

        // Give server time to start
        Thread.Sleep(1000);

        // Act
        Action act = () => service.Broadcast("test message");

        // Assert - Should not throw even with no clients
        act.Should().NotThrow();
    }

    [Fact]
    public void Service_HasPluginAttribute()
    {
        // Arrange
        var serviceType = typeof(SuperSocketWebSocketService);

        // Act
        var pluginAttributes = serviceType.GetCustomAttributes(typeof(WingedBean.Contracts.Core.PluginAttribute), false);

        // Assert
        pluginAttributes.Should().NotBeEmpty();
        pluginAttributes.Should().HaveCount(1);
    }
}

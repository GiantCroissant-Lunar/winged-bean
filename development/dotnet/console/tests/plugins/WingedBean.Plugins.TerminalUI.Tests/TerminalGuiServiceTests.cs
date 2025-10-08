using FluentAssertions;
using Plate.CrossMilo.Contracts.TerminalUI;
using ITerminalUIService = Plate.CrossMilo.Contracts.TerminalUI.Services.IService;
using WingedBean.Plugins.TerminalUI;
using Xunit;

namespace WingedBean.Plugins.TerminalUI.Tests;

/// <summary>
/// Unit tests for TerminalGuiService.
/// Note: These tests verify the interface contract but cannot fully test UI rendering
/// without a real terminal. Full integration testing requires a PTY environment.
/// </summary>
public class TerminalGuiServiceTests
{
    [Fact]
    public void Service_ImplementsInterface()
    {
        // Arrange & Act
        var service = new TerminalGuiService();

        // Assert
        service.Should().BeAssignableTo<ITerminalUIService>();
    }

    [Fact]
    public void Initialize_CanBeCalledMultipleTimes()
    {
        // Arrange
        var service = new TerminalGuiService();

        // Act - calling Initialize multiple times should be safe
        Action act = () =>
        {
            service.Initialize();
            service.Initialize(); // Second call should be idempotent
        };

        // Assert - Should not throw
        act.Should().NotThrow();
    }

    [Fact]
    public void GetScreenContent_BeforeInitialize_ReturnsMessage()
    {
        // Arrange
        var service = new TerminalGuiService();

        // Act
        var content = service.GetScreenContent();

        // Assert
        content.Should().NotBeNullOrEmpty();
        content.Should().Contain("not initialized");
    }

    [Fact]
    public void GetScreenContent_AfterInitialize_ReturnsContent()
    {
        // Arrange
        var service = new TerminalGuiService();

        // Act
        service.Initialize();
        var content = service.GetScreenContent();

        // Assert
        content.Should().NotBeNullOrEmpty();
        content.Should().Contain("Terminal.Gui");
    }

    [Fact]
    public void Run_WithoutInitialize_ThrowsException()
    {
        // Arrange
        var service = new TerminalGuiService();

        // Act
        Action act = () => service.Run();

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Initialize*");
    }

    [Fact]
    public void PluginAttribute_IsApplied()
    {
        // Arrange
        var serviceType = typeof(TerminalGuiService);

        // Act
        var pluginAttribute = serviceType.GetCustomAttributes(typeof(Plate.PluginManoi.Contracts.PluginAttribute), false)
            .FirstOrDefault() as Plate.PluginManoi.Contracts.PluginAttribute;

        // Assert
        pluginAttribute.Should().NotBeNull();
        pluginAttribute!.Name.Should().Be("TerminalGuiService");
        pluginAttribute.Provides.Should().Contain(typeof(ITerminalUIService));
        pluginAttribute.Priority.Should().Be(100);
    }
}

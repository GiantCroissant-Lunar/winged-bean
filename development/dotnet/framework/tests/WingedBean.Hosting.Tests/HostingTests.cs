using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using WingedBean.Contracts.Hosting;
using WingedBean.Contracts.UI;
using WingedBean.Hosting;
using Xunit;

namespace WingedBean.Hosting.Tests;

public class WingedBeanHostTests
{
    [Fact]
    public void CreateDefaultBuilder_ReturnsConsoleBuilder()
    {
        // Act
        var builder = WingedBeanHost.CreateDefaultBuilder([]);

        // Assert
        builder.Should().NotBeNull();
        builder.Should().BeOfType<WingedBean.Hosting.Console.ConsoleWingedBeanHostBuilder>();
    }

    [Fact]
    public void CreateConsoleBuilder_ReturnsConsoleBuilder()
    {
        // Act
        var builder = WingedBeanHost.CreateConsoleBuilder([]);

        // Assert
        builder.Should().NotBeNull();
        builder.Should().BeOfType<WingedBean.Hosting.Console.ConsoleWingedBeanHostBuilder>();
    }

    [Fact]
    public void CreateUnityBuilder_ReturnsUnityBuilder()
    {
        // Act
        var builder = WingedBeanHost.CreateUnityBuilder();

        // Assert
        builder.Should().NotBeNull();
        builder.Should().BeOfType<WingedBean.Hosting.Unity.UnityWingedBeanHostBuilder>();
    }

    [Fact]
    public void CreateGodotBuilder_ReturnsGodotBuilder()
    {
        // Act
        var builder = WingedBeanHost.CreateGodotBuilder();

        // Assert
        builder.Should().NotBeNull();
        builder.Should().BeOfType<WingedBean.Hosting.Godot.GodotWingedBeanHostBuilder>();
    }
}

public class ConsoleHostBuilderTests
{
    [Fact]
    public void ConfigureServices_AddsServiceToContainer()
    {
        // Arrange
        var builder = WingedBeanHost.CreateConsoleBuilder([])
            .ConfigureServices(services =>
            {
                services.AddSingleton<ITestService, TestService>();
            });

        // Act
        var host = builder.Build();

        // Assert
        var service = host.Services.GetService<ITestService>();
        service.Should().NotBeNull();
        service.Should().BeOfType<TestService>();
    }

    [Fact]
    public void Build_CreatesHostWithConfiguredServices()
    {
        // Arrange
        var builder = WingedBeanHost.CreateConsoleBuilder([])
            .ConfigureServices(services =>
            {
                services.AddSingleton<IWingedBeanApp, TestApp>();
            });

        // Act
        var host = builder.Build();

        // Assert
        host.Should().NotBeNull();
        host.Services.GetService<IWingedBeanApp>().Should().NotBeNull();
    }
}

public class AppStateTests
{
    [Fact]
    public void AppState_EnumsHaveCorrectValues()
    {
        // Assert
        AppState.NotStarted.Should().Be(AppState.NotStarted);
        AppState.Starting.Should().Be(AppState.Starting);
        AppState.Running.Should().Be(AppState.Running);
        AppState.Stopping.Should().Be(AppState.Stopping);
        AppState.Stopped.Should().Be(AppState.Stopped);
        AppState.Faulted.Should().Be(AppState.Faulted);
    }
}

// Test implementations
public interface ITestService { }

public class TestService : ITestService { }

public class TestApp : IWingedBeanApp
{
    public string Name => "Test App";
    public AppState State { get; private set; } = AppState.NotStarted;

    public event EventHandler<AppStateChangedEventArgs>? StateChanged;

    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
        State = AppState.Running;
        StateChanged?.Invoke(this, new AppStateChangedEventArgs
        {
            PreviousState = AppState.NotStarted,
            NewState = AppState.Running
        });
    }

    public async Task StopAsync(CancellationToken cancellationToken = default)
    {
        State = AppState.Stopped;
        StateChanged?.Invoke(this, new AppStateChangedEventArgs
        {
            PreviousState = AppState.Running,
            NewState = AppState.Stopped
        });
    }
}

public class TestUIApp : IUIApp
{
    public string Name => "Test UI App";
    public AppState State { get; private set; } = AppState.NotStarted;

    public event EventHandler<AppStateChangedEventArgs>? StateChanged;
    public event EventHandler<UIEventArgs>? UIEvent;

    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
        State = AppState.Running;
    }

    public async Task StopAsync(CancellationToken cancellationToken = default)
    {
        State = AppState.Stopped;
    }

    public Task RenderAsync(CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }

    public Task HandleInputAsync(InputEvent input, CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }

    public Task ResizeAsync(int width, int height, CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }
}

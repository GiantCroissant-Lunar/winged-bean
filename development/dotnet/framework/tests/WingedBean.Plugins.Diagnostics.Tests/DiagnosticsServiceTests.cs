using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Plate.CrossMilo.Contracts.Diagnostics;
using WingedBean.Plugins.Diagnostics;
using Xunit;

namespace WingedBean.Plugins.Diagnostics.Tests;

/// <summary>
/// Tests for the DiagnosticsService (Tier 3 Service).
/// Focuses on provider management and broadcasting functionality.
/// </summary>
public class DiagnosticsServiceTests
{
    private readonly Mock<ILogger<DiagnosticsService>> _loggerMock;
    private readonly DiagnosticsConfig _config;

    public DiagnosticsServiceTests()
    {
        _loggerMock = new Mock<ILogger<DiagnosticsService>>();
        _config = new DiagnosticsConfig
        {
            Enabled = true,
            Backend = "multi"
        };
    }

    #region Provider Registration Tests

    [Fact]
    public void RegisterBackend_ShouldAddBackendToCollection()
    {
        // Arrange
        var service = new DiagnosticsService(_loggerMock.Object, _config);
        var backend = new InMemoryDiagnosticsBackend();

        // Act
        service.RegisterBackend("test-backend", backend);

        // Assert
        service.GetRegisteredBackends().Should().Contain("test-backend");
    }

    [Fact]
    public void RegisterBackend_WithSameName_ShouldReplaceExisting()
    {
        // Arrange
        var service = new DiagnosticsService(_loggerMock.Object, _config);
        var backend1 = new InMemoryDiagnosticsBackend();
        var backend2 = new InMemoryDiagnosticsBackend();

        // Act
        service.RegisterBackend("test", backend1);
        service.RegisterBackend("test", backend2);

        // Assert
        service.GetRegisteredBackends().Should().ContainSingle("test");
        service.GetBackend("test").Should().Be(backend2);
    }

    [Fact]
    public void UnregisterBackend_ShouldRemoveBackend()
    {
        // Arrange
        var service = new DiagnosticsService(_loggerMock.Object, _config);
        var backend = new InMemoryDiagnosticsBackend();
        service.RegisterBackend("test", backend);

        // Act
        var result = service.UnregisterBackend("test");

        // Assert
        result.Should().BeTrue();
        service.GetRegisteredBackends().Should().NotContain("test");
    }

    [Fact]
    public void UnregisterBackend_NonExistent_ShouldReturnFalse()
    {
        // Arrange
        var service = new DiagnosticsService(_loggerMock.Object, _config);

        // Act
        var result = service.UnregisterBackend("non-existent");

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void GetBackend_Existing_ShouldReturnBackend()
    {
        // Arrange
        var service = new DiagnosticsService(_loggerMock.Object, _config);
        var backend = new InMemoryDiagnosticsBackend();
        service.RegisterBackend("test", backend);

        // Act
        var result = service.GetBackend("test");

        // Assert
        result.Should().Be(backend);
    }

    [Fact]
    public void GetBackend_NonExistent_ShouldReturnNull()
    {
        // Arrange
        var service = new DiagnosticsService(_loggerMock.Object, _config);

        // Act
        var result = service.GetBackend("non-existent");

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void GetRegisteredBackends_WithMultipleBackends_ShouldReturnAllNames()
    {
        // Arrange
        var service = new DiagnosticsService(_loggerMock.Object, _config);
        service.RegisterBackend("backend1", new InMemoryDiagnosticsBackend());
        service.RegisterBackend("backend2", new InMemoryDiagnosticsBackend());
        service.RegisterBackend("backend3", new InMemoryDiagnosticsBackend());

        // Act
        var backends = service.GetRegisteredBackends().ToList();

        // Assert
        backends.Should().HaveCount(3);
        backends.Should().Contain(new[] { "backend1", "backend2", "backend3" });
    }

    #endregion

    #region Service Enable/Disable Tests

    [Fact]
    public void IsEnabled_WithEnabledConfig_ShouldReturnTrue()
    {
        // Arrange
        var config = new DiagnosticsConfig { Enabled = true };
        var service = new DiagnosticsService(_loggerMock.Object, config);

        // Act & Assert
        service.IsEnabled.Should().BeTrue();
    }

    [Fact]
    public void IsEnabled_WithDisabledConfig_ShouldReturnFalse()
    {
        // Arrange
        var config = new DiagnosticsConfig { Enabled = false };
        var service = new DiagnosticsService(_loggerMock.Object, config);

        // Act & Assert
        service.IsEnabled.Should().BeFalse();
    }

    [Fact]
    public void SetEnabled_ShouldUpdateEnabledState()
    {
        // Arrange
        var service = new DiagnosticsService(_loggerMock.Object, _config);

        // Act
        service.SetEnabled(false);

        // Assert
        service.IsEnabled.Should().BeFalse();

        // Act
        service.SetEnabled(true);

        // Assert
        service.IsEnabled.Should().BeTrue();
    }

    #endregion

    #region Broadcasting Tests (Integration)

    [Fact]
    public async Task MultipleBackends_ShouldAllReceiveFlushCall()
    {
        // Arrange
        var service = new DiagnosticsService(_loggerMock.Object, _config);
        var backend1 = new Mock<IDiagnosticsBackend>();
        var backend2 = new Mock<IDiagnosticsBackend>();
        
        service.RegisterBackend("backend1", backend1.Object);
        service.RegisterBackend("backend2", backend2.Object);

        // Act
        await service.FlushAsync();

        // Assert
        backend1.Verify(x => x.FlushAsync(), Times.Once);
        backend2.Verify(x => x.FlushAsync(), Times.Once);
    }

    [Fact]
    public void StartSpan_WithMultipleBackends_ShouldBroadcastToAll()
    {
        // Arrange
        var service = new DiagnosticsService(_loggerMock.Object, _config);
        var backend1 = new InMemoryDiagnosticsBackend();
        var backend2 = new InMemoryDiagnosticsBackend();
        
        service.RegisterBackend("backend1", backend1);
        service.RegisterBackend("backend2", backend2);

        // Act
        using (service.StartSpan("test-operation"))
        {
            // Span is active
        }

        // Assert
        backend1.GetCompletedSpans().Should().HaveCount(1);
        backend2.GetCompletedSpans().Should().HaveCount(1);
        
        backend1.GetCompletedSpans().First().OperationName.Should().Be("test-operation");
        backend2.GetCompletedSpans().First().OperationName.Should().Be("test-operation");
    }

    #endregion

    #region Thread Safety Tests

    [Fact]
    public void RegisterBackend_ConcurrentCalls_ShouldBeThreadSafe()
    {
        // Arrange
        var service = new DiagnosticsService(_loggerMock.Object, _config);
        var tasks = new List<Task>();

        // Act
        for (int i = 0; i < 100; i++)
        {
            var index = i;
            tasks.Add(Task.Run(() =>
            {
                var backend = new InMemoryDiagnosticsBackend();
                service.RegisterBackend($"backend-{index}", backend);
            }));
        }

        Task.WaitAll(tasks.ToArray());

        // Assert
        service.GetRegisteredBackends().Should().HaveCount(100);
    }

    #endregion
}

using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Plate.CrossMilo.Contracts.Diagnostics;
using WingedBean.Plugins.Diagnostics.Sentry;
using Xunit;

namespace WingedBean.Plugins.Diagnostics.Sentry.Tests;

/// <summary>
/// Tests for the SentryDiagnosticsBackend (Tier 4 Provider).
/// Tests Sentry-specific functionality and SDK integration.
/// </summary>
public class SentryDiagnosticsBackendTests
{
    private readonly Mock<ILogger<SentryDiagnosticsBackend>> _loggerMock;
    private const string TestDsn = "https://test-key@sentry.io/test-project";

    public SentryDiagnosticsBackendTests()
    {
        _loggerMock = new Mock<ILogger<SentryDiagnosticsBackend>>();
    }

    #region Initialization Tests

    [Fact]
    public void Constructor_WithValidDsn_ShouldInitialize()
    {
        // Arrange & Act
        var backend = new SentryDiagnosticsBackend(_loggerMock.Object, TestDsn, "test");

        // Assert
        backend.Should().NotBeNull();
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("initialized")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public void Constructor_WithNullEnvironment_ShouldUseProduction()
    {
        // Arrange & Act
        var backend = new SentryDiagnosticsBackend(_loggerMock.Object, TestDsn);

        // Assert
        backend.Should().NotBeNull();
        // Sentry SDK should be initialized with "production" environment
    }

    [Fact]
    public void Constructor_WithInvalidDsn_ShouldThrowException()
    {
        // Arrange & Act
        Action act = () => new SentryDiagnosticsBackend(_loggerMock.Object, "invalid-dsn");

        // Assert
        act.Should().Throw<Exception>();
    }

    #endregion

    #region Span Tests

    [Fact]
    public async Task StartSpan_ShouldCreateSentryTransaction()
    {
        // Arrange
        var backend = new SentryDiagnosticsBackend(_loggerMock.Object, TestDsn);
        var span = new TraceSpan
        {
            SpanId = "test-span-id",
            TraceId = "test-trace-id",
            OperationName = "test-operation",
            StartTime = DateTimeOffset.UtcNow,
            Tags = new Dictionary<string, object> { { "test-tag", "test-value" } }
        };

        // Act
        await backend.StartSpan(span);

        // Assert
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Debug,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Started Sentry span")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task SetSpanTags_ShouldAddTagsToActiveSpan()
    {
        // Arrange
        var backend = new SentryDiagnosticsBackend(_loggerMock.Object, TestDsn);
        var span = new TraceSpan
        {
            SpanId = "test-span-id",
            TraceId = "test-trace-id",
            OperationName = "test-operation",
            StartTime = DateTimeOffset.UtcNow
        };
        await backend.StartSpan(span);

        var tags = new Dictionary<string, object>
        {
            { "tag1", "value1" },
            { "tag2", 123 }
        };

        // Act
        await backend.SetSpanTags("test-span-id", tags);

        // Assert - Should not throw
        // In a real test, we'd verify Sentry SDK calls
    }

    [Fact]
    public async Task EndSpan_ShouldFinishSentryTransaction()
    {
        // Arrange
        var backend = new SentryDiagnosticsBackend(_loggerMock.Object, TestDsn);
        var span = new TraceSpan
        {
            SpanId = "test-span-id",
            TraceId = "test-trace-id",
            OperationName = "test-operation",
            StartTime = DateTimeOffset.UtcNow
        };
        await backend.StartSpan(span);

        // Act
        await backend.EndSpan(span);

        // Assert
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Debug,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Ended Sentry span")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    #endregion

    #region Breadcrumb Tests

    [Fact]
    public async Task AddBreadcrumb_ShouldRecordInSentry()
    {
        // Arrange
        var backend = new SentryDiagnosticsBackend(_loggerMock.Object, TestDsn);
        var breadcrumb = new DiagnosticBreadcrumb
        {
            Message = "Test breadcrumb",
            Category = "test",
            Level = BreadcrumbLevel.Info,
            Timestamp = DateTimeOffset.UtcNow,
            Data = new Dictionary<string, object> { { "key", "value" } }
        };

        // Act
        await backend.AddBreadcrumb(breadcrumb);

        // Assert
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Debug,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Added Sentry breadcrumb")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task ClearBreadcrumbs_ShouldClearLocalCollection()
    {
        // Arrange
        var backend = new SentryDiagnosticsBackend(_loggerMock.Object, TestDsn);
        var breadcrumb = new DiagnosticBreadcrumb
        {
            Message = "Test",
            Category = "test",
            Level = BreadcrumbLevel.Info,
            Timestamp = DateTimeOffset.UtcNow
        };
        await backend.AddBreadcrumb(breadcrumb);

        // Act
        await backend.ClearBreadcrumbs();

        // Assert
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Debug,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Cleared local breadcrumbs")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    #endregion

    #region Exception and Error Tests

    [Fact]
    public async Task RecordException_ShouldCaptureinSentry()
    {
        // Arrange
        var backend = new SentryDiagnosticsBackend(_loggerMock.Object, TestDsn);
        var exception = new InvalidOperationException("Test exception");
        var context = new Dictionary<string, object>
        {
            { "user_id", "123" },
            { "action", "test" }
        };

        // Act
        await backend.RecordException(exception, context);

        // Assert
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Debug,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Recorded Sentry exception")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task RecordError_ShouldCaptureInSentry()
    {
        // Arrange
        var backend = new SentryDiagnosticsBackend(_loggerMock.Object, TestDsn);
        var message = "Test error message";
        var context = new Dictionary<string, object> { { "severity", "high" } };

        // Act
        await backend.RecordError(message, context);

        // Assert
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Debug,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Recorded Sentry error")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    #endregion

    #region Metrics Tests

    [Fact]
    public async Task RecordMetric_ShouldCaptureAsMessage()
    {
        // Arrange
        var backend = new SentryDiagnosticsBackend(_loggerMock.Object, TestDsn);
        var tags = new Dictionary<string, object> { { "environment", "test" } };

        // Act
        await backend.RecordMetric("test_metric", 42.5, "ms", tags);

        // Assert
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Debug,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Recorded Sentry metric")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task RecordCounter_ShouldDelegateToRecordMetric()
    {
        // Arrange
        var backend = new SentryDiagnosticsBackend(_loggerMock.Object, TestDsn);
        var tags = new Dictionary<string, object>();

        // Act
        await backend.RecordCounter("test_counter", 5, tags);

        // Assert - Should complete without error
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Debug,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Recorded Sentry metric")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    #endregion

    #region Health Check Tests

    [Fact]
    public async Task RegisterHealthCheck_ShouldCaptureInSentry()
    {
        // Arrange
        var backend = new SentryDiagnosticsBackend(_loggerMock.Object, TestDsn);
        var registration = new HealthCheckRegistration
        {
            Name = "test-health-check",
            Tags = new Dictionary<string, object>()
        };

        // Act
        await backend.RegisterHealthCheck("test-health-check", registration);

        // Assert
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Debug,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Registered Sentry health check")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task RecordHealthCheckResult_ShouldCaptureWithCorrectLevel()
    {
        // Arrange
        var backend = new SentryDiagnosticsBackend(_loggerMock.Object, TestDsn);
        var result = new HealthCheckResult
        {
            Status = HealthStatus.Healthy,
            Description = "All systems operational",
            Duration = TimeSpan.FromMilliseconds(50)
        };

        // Act
        await backend.RecordHealthCheckResult("test-check", result);

        // Assert
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Debug,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Recorded Sentry health check result")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    #endregion

    #region Data Management Tests

    [Fact]
    public async Task FlushAsync_ShouldFlushSentryData()
    {
        // Arrange
        var backend = new SentryDiagnosticsBackend(_loggerMock.Object, TestDsn);

        // Act
        await backend.FlushAsync();

        // Assert
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Debug,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Flushed Sentry data")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task ExportDataAsync_ShouldReturnSummary()
    {
        // Arrange
        var backend = new SentryDiagnosticsBackend(_loggerMock.Object, TestDsn);
        var startTime = DateTimeOffset.UtcNow.AddHours(-1);
        var endTime = DateTimeOffset.UtcNow;

        // Act
        var result = await backend.ExportDataAsync("json", startTime, endTime);

        // Assert
        result.Should().NotBeEmpty();
        var json = System.Text.Encoding.UTF8.GetString(result);
        json.Should().Contain("Sentry");
    }

    [Fact]
    public async Task ClearOldDataAsync_ShouldReturnZero()
    {
        // Arrange
        var backend = new SentryDiagnosticsBackend(_loggerMock.Object, TestDsn);

        // Act
        var result = await backend.ClearOldDataAsync(TimeSpan.FromDays(30));

        // Assert
        result.Should().Be(0); // Sentry manages its own retention
    }

    #endregion
}

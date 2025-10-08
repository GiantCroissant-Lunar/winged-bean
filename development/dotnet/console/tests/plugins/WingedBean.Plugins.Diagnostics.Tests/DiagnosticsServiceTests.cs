using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Plate.CrossMilo.Contracts.Diagnostics;
using Plate.CrossMilo.Contracts.Diagnostics.Services;
using BreadcrumbLevel = Plate.CrossMilo.Contracts.Diagnostics.BreadcrumbLevel;
using Xunit;

namespace WingedBean.Plugins.Diagnostics.Tests;

/// <summary>
/// Unit tests for DiagnosticsService.
/// </summary>
public class DiagnosticsServiceTests
{
    private readonly DiagnosticsService _service;
    private readonly InMemoryDiagnosticsBackend _backend;

    public DiagnosticsServiceTests()
    {
        var config = new DiagnosticsConfig
        {
            Enabled = true,
            Backend = DiagnosticsBackend.InMemory,
            SamplingRate = 1.0,
            MaxBreadcrumbs = 10
        };

        _backend = new InMemoryDiagnosticsBackend();
        var logger = NullLogger<DiagnosticsService>.Instance;
        _service = new DiagnosticsService(logger, config, _backend);
    }

    [Fact]
    public void Constructor_InitializesCorrectly()
    {
        _service.IsEnabled.Should().BeTrue();
    }

    [Fact]
    public void SetEnabled_ChangesState()
    {
        _service.SetEnabled(false);
        _service.IsEnabled.Should().BeFalse();

        _service.SetEnabled(true);
        _service.IsEnabled.Should().BeTrue();
    }

    [Fact]
    public void StartSpan_CreatesSpan()
    {
        using var span = _service.StartSpan("test_operation");

        span.Should().NotBeNull();

        // Check backend has the span
        var completedSpans = _backend.GetCompletedSpans();
        completedSpans.Should().HaveCount(1);
        completedSpans[0].OperationName.Should().Be("test_operation");
        completedSpans[0].EndTime.Should().NotBeNull();
    }

    [Fact]
    public void AddBreadcrumb_RecordsBreadcrumb()
    {
        _service.AddBreadcrumb("Test message", "test", BreadcrumbLevel.Info);

        var breadcrumbs = _backend.GetBreadcrumbs();
        breadcrumbs.Should().HaveCount(1);
        breadcrumbs[0].Message.Should().Be("Test message");
        breadcrumbs[0].Category.Should().Be("test");
        breadcrumbs[0].Level.Should().Be(BreadcrumbLevel.Info);
    }

    [Fact]
    public void AddBreadcrumb_WithException_RecordsCorrectly()
    {
        var exception = new InvalidOperationException("Test exception");
        _service.AddBreadcrumb(exception, "error");

        var breadcrumbs = _backend.GetBreadcrumbs();
        breadcrumbs.Should().HaveCount(1);
        breadcrumbs[0].Category.Should().Be("error");
        breadcrumbs[0].Level.Should().Be(BreadcrumbLevel.Error);
        breadcrumbs[0].Data.Should().ContainKey("exceptionType")
            .WhoseValue.Should().Be("System.InvalidOperationException");
    }

    [Fact]
    public void RecordException_RecordsCorrectly()
    {
        var exception = new ArgumentNullException("param");

        _service.RecordException(exception);

        var exceptions = _backend.GetExceptions();
        exceptions.Should().HaveCount(1);
        exceptions[0].Should().Be(exception);

        var breadcrumbs = _backend.GetBreadcrumbs();
        breadcrumbs.Should().HaveCount(1); // Should also add breadcrumb
    }

    [Fact]
    public void RecordError_RecordsCorrectly()
    {
        _service.RecordError("Test error message", "CustomError");

        var errors = _backend.GetErrors();
        errors.Should().HaveCount(1);
        errors[0].Should().Be("Test error message");

        var breadcrumbs = _backend.GetBreadcrumbs();
        breadcrumbs.Should().HaveCount(1);
    }

    [Fact]
    public void RecordMetric_RecordsCorrectly()
    {
        _service.RecordMetric("response_time", 150.5, "ms");

        var metrics = _backend.GetMetrics();
        metrics.Should().HaveCount(1);
        metrics[0].Name.Should().Be("response_time");
        metrics[0].Value.Should().Be(150.5);
        metrics[0].Unit.Should().Be("ms");
    }

    [Fact]
    public void RecordCounter_IncrementsCorrectly()
    {
        _service.RecordCounter("requests_total", 5);

        var metrics = _backend.GetMetrics();
        metrics.Should().HaveCount(1);
        metrics[0].Name.Should().Be("requests_total");
        metrics[0].Value.Should().Be(5.0);
        metrics[0].Unit.Should().Be("count");
    }

    [Fact]
    public void RecordGauge_RecordsCorrectly()
    {
        _service.RecordGauge("memory_usage", 1024.0);

        var metrics = _backend.GetMetrics();
        metrics.Should().HaveCount(1);
        metrics[0].Name.Should().Be("memory_usage");
        metrics[0].Value.Should().Be(1024.0);
        metrics[0].Unit.Should().Be("gauge");
    }

    [Fact]
    public void RecordHistogram_RecordsCorrectly()
    {
        _service.RecordHistogram("request_duration", 0.5);

        var metrics = _backend.GetMetrics();
        metrics.Should().HaveCount(1);
        metrics[0].Name.Should().Be("request_duration");
        metrics[0].Value.Should().Be(0.5);
        metrics[0].Unit.Should().Be("histogram");
    }

    [Fact]
    public async Task RegisterHealthCheck_AddsCheck()
    {
        var checkExecuted = false;
        var check = new Func<HealthCheckResult>(() =>
        {
            checkExecuted = true;
            return new HealthCheckResult
            {
                Status = HealthStatus.Healthy,
                Description = "Test check",
                Duration = TimeSpan.FromMilliseconds(10),
                Timestamp = DateTimeOffset.UtcNow
            };
        });

        _service.RegisterHealthCheck("test_check", check);

        var results = await _service.RunHealthChecksAsync();
        results.Should().ContainKey("test_check");
        checkExecuted.Should().BeTrue();
    }

    [Fact]
    public async Task RunHealthCheckAsync_ExecutesSpecificCheck()
    {
        var checkExecuted = false;
        var check = new Func<HealthCheckResult>(() =>
        {
            checkExecuted = true;
            return new HealthCheckResult
            {
                Status = HealthStatus.Healthy,
                Description = "Specific check",
                Duration = TimeSpan.FromMilliseconds(5),
                Timestamp = DateTimeOffset.UtcNow
            };
        });

        _service.RegisterHealthCheck("specific_check", check);

        var result = await _service.RunHealthCheckAsync("specific_check");

        result.Status.Should().Be(HealthStatus.Healthy);
        result.Description.Should().Be("Specific check");
        checkExecuted.Should().BeTrue();
    }

    [Fact]
    public async Task GetHealthStatusAsync_AggregatesResults()
    {
        // Register healthy check
        _service.RegisterHealthCheck("healthy", () => new HealthCheckResult
        {
            Status = HealthStatus.Healthy,
            Timestamp = DateTimeOffset.UtcNow
        });

        // Register degraded check
        _service.RegisterHealthCheck("degraded", () => new HealthCheckResult
        {
            Status = HealthStatus.Degraded,
            Timestamp = DateTimeOffset.UtcNow
        });

        var status = await _service.GetHealthStatusAsync();
        status.Should().Be(HealthStatus.Degraded);
    }

    [Fact]
    public void GetSystemMetrics_ReturnsMetrics()
    {
        var metrics = _service.GetSystemMetrics();

        metrics.Should().NotBeNull();
        metrics.Timestamp.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(5));
        metrics.ProcessId.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task CaptureSnapshotAsync_IncludesSystemMetrics()
    {
        var snapshot = await _service.CaptureSnapshotAsync(includeHeapDump: false, includeThreadDump: false);

        snapshot.Should().NotBeNull();
        snapshot.SystemMetrics.Should().NotBeNull();
        snapshot.HealthChecks.Should().NotBeNull();
        snapshot.Breadcrumbs.Should().NotBeNull();
        snapshot.ActiveSpans.Should().NotBeNull();
        snapshot.Timestamp.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void SetupAlert_AddsAlertRule()
    {
        var rule = new AlertRule
        {
            Name = "test_alert",
            MetricName = "error_rate",
            Condition = AlertCondition.GreaterThan,
            Threshold = 0.05,
            Severity = AlertSeverity.Warning
        };

        _service.SetupAlert(rule);

        var rules = _service.GetAlertRules();
        rules.Should().ContainSingle(r => r.Name == "test_alert");
    }

    [Fact]
    public void GetActiveAlerts_ReturnsEmptyInitially()
    {
        var alerts = _service.GetActiveAlerts();
        alerts.Should().BeEmpty();
    }

    [Fact]
    public async Task FlushAsync_CompletesSuccessfully()
    {
        await _service.FlushAsync();
        // Should not throw
    }

    [Fact]
    public async Task ExportDataAsync_ReturnsData()
    {
        // Add some test data
        _service.AddBreadcrumb("Test breadcrumb");
        _service.RecordMetric("test_metric", 42.0);

        var startTime = DateTimeOffset.UtcNow.AddMinutes(-1);
        var endTime = DateTimeOffset.UtcNow.AddMinutes(1);

        var data = await _service.ExportDataAsync("json", startTime, endTime);

        data.Should().NotBeNullOrEmpty();
        // Should contain JSON data
        var jsonString = System.Text.Encoding.UTF8.GetString(data);
        jsonString.Should().Contain("Spans");
        jsonString.Should().Contain("Breadcrumbs");
        jsonString.Should().Contain("Metrics");
    }

    [Fact]
    public void GetStats_ReturnsStatistics()
    {
        var stats = _service.GetStats();

        stats.Should().NotBeNull();
        stats.TotalSpans.Should().BeGreaterThanOrEqualTo(0);
        stats.BreadcrumbCount.Should().BeGreaterThanOrEqualTo(0);
        stats.HealthCheckCount.Should().BeGreaterThanOrEqualTo(0);
    }

    [Fact]
    public void GetStatus_ReturnsStatus()
    {
        var status = _service.GetStatus();

        status.Should().NotBeNull();
        status.IsEnabled.Should().BeTrue();
        status.Backend.Should().Be(DiagnosticsBackend.InMemory);
        status.LastUpdate.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void StartProfiling_ReturnsProfiler()
    {
        var profiler = _service.StartProfiling("test_operation");

        profiler.Should().NotBeNull();
        profiler.Should().BeAssignableTo<IOperationProfiler>();
    }

    [Fact]
    public void RecordTiming_RecordsCorrectly()
    {
        var duration = TimeSpan.FromMilliseconds(150);
        _service.RecordTiming("test_operation", duration);

        var metrics = _backend.GetMetrics();
        metrics.Should().Contain(m => m.Name == "test_operation" && m.Value == 150.0 && m.Unit == "ms");
    }

    [Fact]
    public void ClearBreadcrumbs_RemovesAll()
    {
        _service.AddBreadcrumb("Test 1");
        _service.AddBreadcrumb("Test 2");

        _service.ClearBreadcrumbs();

        var breadcrumbs = _service.GetBreadcrumbs();
        breadcrumbs.Should().BeEmpty();
    }

    [Fact]
    public async Task RunHealthChecksAsync_WithMultipleChecks_ReturnsAllResults()
    {
        _service.RegisterHealthCheck("check1", () => new HealthCheckResult
        {
            Status = HealthStatus.Healthy,
            Timestamp = DateTimeOffset.UtcNow
        });

        _service.RegisterHealthCheck("check2", () => new HealthCheckResult
        {
            Status = HealthStatus.Unhealthy,
            Timestamp = DateTimeOffset.UtcNow
        });

        var results = await _service.RunHealthChecksAsync();

        results.Should().HaveCount(2);
        results.Should().ContainKey("check1");
        results.Should().ContainKey("check2");
        results["check1"].Status.Should().Be(HealthStatus.Healthy);
        results["check2"].Status.Should().Be(HealthStatus.Unhealthy);
    }
}

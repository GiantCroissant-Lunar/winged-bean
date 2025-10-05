using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace WingedBean.Contracts.Diagnostics;

/// <summary>
/// Diagnostics service for system health monitoring, performance profiling, and troubleshooting.
/// </summary>
/// <remarks>
/// <para><strong>Implementation Strategy (RFC-0033):</strong></para>
/// <para>This contract is implemented as a <strong>thin wrapper</strong> over industry-standard SDKs:</para>
/// <list type="bullet">
///   <item><description><see cref="RecordException"/> → Delegates to Sentry SDK (RFC-0034) for error tracking</description></item>
///   <item><description><see cref="StartSpan"/> → Wraps OpenTelemetry Activity API (RFC-0035) for distributed tracing</description></item>
///   <item><description>Health checks → Custom WingedBean implementation (not part of OTEL/Sentry)</description></item>
/// </list>
/// <para><strong>Separation of Concerns:</strong></para>
/// <list type="bullet">
///   <item><description><strong>Diagnostics (this service):</strong> System observability (errors, traces, health)</description></item>
///   <item><description><strong>Analytics (IAnalyticsService):</strong> Product/business metrics (user behavior, engagement)</description></item>
/// </list>
/// <para>See RFC-0031 for contract specification, RFC-0033/0034/0035 for implementation details.</para>
/// </remarks>
public interface IDiagnosticsService
{
    /// <summary>
    /// Gets whether diagnostics are currently enabled.
    /// </summary>
    bool IsEnabled { get; }

    /// <summary>
    /// Enables or disables diagnostics collection.
    /// </summary>
    /// <param name="enabled">Whether to enable diagnostics.</param>
    void SetEnabled(bool enabled);

    // Distributed Tracing

    /// <summary>
    /// Starts a new trace span.
    /// </summary>
    /// <param name="operationName">The operation name.</param>
    /// <param name="kind">The span kind.</param>
    /// <param name="tags">Optional span tags.</param>
    /// <returns>A disposable trace span.</returns>
    IDisposable StartSpan(string operationName, SpanKind kind = SpanKind.Internal, Dictionary<string, object>? tags = null);

    /// <summary>
    /// Starts a new trace span with a parent context.
    /// </summary>
    /// <param name="operationName">The operation name.</param>
    /// <param name="parentSpan">The parent span.</param>
    /// <param name="kind">The span kind.</param>
    /// <param name="tags">Optional span tags.</param>
    /// <returns>A disposable trace span.</returns>
    IDisposable StartSpan(string operationName, IDisposable parentSpan, SpanKind kind = SpanKind.Internal, Dictionary<string, object>? tags = null);

    /// <summary>
    /// Gets the current trace ID.
    /// </summary>
    /// <returns>The current trace ID, or null if no active span.</returns>
    string? GetCurrentTraceId();

    /// <summary>
    /// Gets the current span ID.
    /// </summary>
    /// <returns>The current span ID, or null if no active span.</returns>
    string? GetCurrentSpanId();

    /// <summary>
    /// Sets tags on the current span.
    /// </summary>
    /// <param name="tags">The tags to set.</param>
    void SetSpanTags(Dictionary<string, object> tags);

    /// <summary>
    /// Sets an error on the current span.
    /// </summary>
    /// <param name="error">The error message.</param>
    /// <param name="tags">Optional error tags.</param>
    void SetSpanError(string error, Dictionary<string, object>? tags = null);

    // Breadcrumbs and Context

    /// <summary>
    /// Adds a breadcrumb to the current trace.
    /// </summary>
    /// <param name="message">The breadcrumb message.</param>
    /// <param name="category">The breadcrumb category.</param>
    /// <param name="level">The breadcrumb level.</param>
    /// <param name="data">Optional breadcrumb data.</param>
    void AddBreadcrumb(string message, string category = "default", BreadcrumbLevel level = BreadcrumbLevel.Info, Dictionary<string, object>? data = null);

    /// <summary>
    /// Adds a breadcrumb with exception details.
    /// </summary>
    /// <param name="exception">The exception to add as breadcrumb.</param>
    /// <param name="category">The breadcrumb category.</param>
    void AddBreadcrumb(Exception exception, string category = "error");

    /// <summary>
    /// Clears all breadcrumbs.
    /// </summary>
    void ClearBreadcrumbs();

    /// <summary>
    /// Gets the current breadcrumbs.
    /// </summary>
    /// <returns>List of current breadcrumbs.</returns>
    IEnumerable<DiagnosticBreadcrumb> GetBreadcrumbs();

    // Error Tracking

    /// <summary>
    /// Records an exception with context.
    /// </summary>
    /// <param name="exception">The exception to record.</param>
    /// <param name="tags">Optional tags for the error.</param>
    void RecordException(Exception exception, Dictionary<string, object>? tags = null);

    /// <summary>
    /// Records an error message.
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <param name="type">The error type.</param>
    /// <param name="tags">Optional error tags.</param>
    void RecordError(string message, string? type = null, Dictionary<string, object>? tags = null);

    /// <summary>
    /// Records a handled exception.
    /// </summary>
    /// <param name="exception">The handled exception.</param>
    /// <param name="context">Additional context information.</param>
    void RecordHandledException(Exception exception, Dictionary<string, object>? context = null);

    // Performance Profiling

    /// <summary>
    /// Records a performance metric.
    /// </summary>
    /// <param name="name">The metric name.</param>
    /// <param name="value">The metric value.</param>
    /// <param name="unit">The metric unit.</param>
    /// <param name="tags">Optional metric tags.</param>
    void RecordMetric(string name, double value, string unit = "ms", Dictionary<string, object>? tags = null);

    /// <summary>
    /// Records a counter metric.
    /// </summary>
    /// <param name="name">The counter name.</param>
    /// <param name="increment">The increment value.</param>
    /// <param name="tags">Optional counter tags.</param>
    void RecordCounter(string name, long increment = 1, Dictionary<string, object>? tags = null);

    /// <summary>
    /// Records a gauge metric.
    /// </summary>
    /// <param name="name">The gauge name.</param>
    /// <param name="value">The gauge value.</param>
    /// <param name="tags">Optional gauge tags.</param>
    void RecordGauge(string name, double value, Dictionary<string, object>? tags = null);

    /// <summary>
    /// Records a histogram sample.
    /// </summary>
    /// <param name="name">The histogram name.</param>
    /// <param name="value">The sample value.</param>
    /// <param name="tags">Optional histogram tags.</param>
    void RecordHistogram(string name, double value, Dictionary<string, object>? tags = null);

    /// <summary>
    /// Starts performance profiling for a specific operation.
    /// </summary>
    /// <param name="operationName">The operation name.</param>
    /// <returns>A profiler instance.</returns>
    IOperationProfiler StartProfiling(string operationName);

    /// <summary>
    /// Starts performance profiling with custom configuration.
    /// </summary>
    /// <param name="operationName">The operation name.</param>
    /// <param name="config">The profiling configuration.</param>
    /// <returns>A profiler instance.</returns>
    IOperationProfiler StartProfiling(string operationName, ProfilingConfig config);

    /// <summary>
    /// Records timing for a completed operation.
    /// </summary>
    /// <param name="operationName">The operation name.</param>
    /// <param name="duration">The operation duration.</param>
    /// <param name="tags">Optional timing tags.</param>
    void RecordTiming(string operationName, TimeSpan duration, Dictionary<string, object>? tags = null);

    // Health Checks

    /// <summary>
    /// Registers a health check.
    /// </summary>
    /// <param name="name">The health check name.</param>
    /// <param name="check">The health check function.</param>
    /// <param name="tags">Optional health check tags.</param>
    void RegisterHealthCheck(string name, Func<Task<HealthCheckResult>> check, Dictionary<string, object>? tags = null);

    /// <summary>
    /// Registers a synchronous health check.
    /// </summary>
    /// <param name="name">The health check name.</param>
    /// <param name="check">The health check function.</param>
    /// <param name="tags">Optional health check tags.</param>
    void RegisterHealthCheck(string name, Func<HealthCheckResult> check, Dictionary<string, object>? tags = null);

    /// <summary>
    /// Registers a health check with custom configuration.
    /// </summary>
    /// <param name="name">The health check name.</param>
    /// <param name="config">The health check configuration.</param>
    void RegisterHealthCheck(string name, HealthCheckConfig config);

    /// <summary>
    /// Unregisters a health check.
    /// </summary>
    /// <param name="name">The health check name.</param>
    void UnregisterHealthCheck(string name);

    /// <summary>
    /// Runs all registered health checks.
    /// </summary>
    /// <returns>A dictionary of health check results.</returns>
    Task<Dictionary<string, HealthCheckResult>> RunHealthChecksAsync();

    /// <summary>
    /// Runs a specific health check.
    /// </summary>
    /// <param name="name">The health check name.</param>
    /// <returns>The health check result.</returns>
    Task<HealthCheckResult> RunHealthCheckAsync(string name);

    /// <summary>
    /// Gets the current health status.
    /// </summary>
    /// <returns>The overall health status.</returns>
    Task<HealthStatus> GetHealthStatusAsync();

    /// <summary>
    /// Gets health check results with filtering.
    /// </summary>
    /// <param name="tags">Optional tags to filter by.</param>
    /// <returns>Filtered health check results.</returns>
    Task<Dictionary<string, HealthCheckResult>> GetHealthCheckResultsAsync(Dictionary<string, object>? tags = null);

    // System Monitoring

    /// <summary>
    /// Gets current system resource metrics.
    /// </summary>
    /// <returns>Current system metrics.</returns>
    SystemMetrics GetSystemMetrics();

    /// <summary>
    /// Gets system metrics history.
    /// </summary>
    /// <param name="duration">The time window for history.</param>
    /// <returns>Historical system metrics.</returns>
    IEnumerable<SystemMetrics> GetSystemMetricsHistory(TimeSpan duration);

    /// <summary>
    /// Records custom system metrics.
    /// </summary>
    /// <param name="metrics">The custom metrics to record.</param>
    void RecordSystemMetrics(Dictionary<string, object> metrics);

    // Diagnostic Snapshots

    /// <summary>
    /// Captures a diagnostic snapshot.
    /// </summary>
    /// <param name="includeHeapDump">Whether to include heap dump.</param>
    /// <param name="includeThreadDump">Whether to include thread dump.</param>
    /// <returns>A diagnostic snapshot.</returns>
    Task<DiagnosticSnapshot> CaptureSnapshotAsync(bool includeHeapDump = false, bool includeThreadDump = true);

    /// <summary>
    /// Captures a snapshot with custom configuration.
    /// </summary>
    /// <param name="config">The snapshot configuration.</param>
    /// <returns>A diagnostic snapshot.</returns>
    Task<DiagnosticSnapshot> CaptureSnapshotAsync(SnapshotConfig config);

    /// <summary>
    /// Gets recent diagnostic snapshots.
    /// </summary>
    /// <param name="count">The number of snapshots to retrieve.</param>
    /// <returns>Recent diagnostic snapshots.</returns>
    IEnumerable<DiagnosticSnapshot> GetRecentSnapshots(int count = 10);

    // Alerting System

    /// <summary>
    /// Sets up an alert rule.
    /// </summary>
    /// <param name="rule">The alert rule configuration.</param>
    void SetupAlert(AlertRule rule);

    /// <summary>
    /// Updates an existing alert rule.
    /// </summary>
    /// <param name="ruleName">The alert rule name.</param>
    /// <param name="rule">The updated alert rule configuration.</param>
    void UpdateAlert(string ruleName, AlertRule rule);

    /// <summary>
    /// Removes an alert rule.
    /// </summary>
    /// <param name="ruleName">The alert rule name.</param>
    void RemoveAlert(string ruleName);

    /// <summary>
    /// Enables or disables an alert rule.
    /// </summary>
    /// <param name="ruleName">The alert rule name.</param>
    /// <param name="enabled">Whether to enable the alert.</param>
    void SetAlertEnabled(string ruleName, bool enabled);

    /// <summary>
    /// Gets active alerts.
    /// </summary>
    /// <returns>List of active alerts.</returns>
    IEnumerable<Alert> GetActiveAlerts();

    /// <summary>
    /// Gets all alert rules.
    /// </summary>
    /// <returns>List of all alert rules.</returns>
    IEnumerable<AlertRule> GetAlertRules();

    /// <summary>
    /// Acknowledges an alert.
    /// </summary>
    /// <param name="alertId">The alert ID.</param>
    /// <param name="user">The user acknowledging the alert.</param>
    /// <param name="comment">Optional acknowledgment comment.</param>
    void AcknowledgeAlert(string alertId, string user, string? comment = null);

    // Data Management

    /// <summary>
    /// Flushes any pending diagnostics data.
    /// </summary>
    /// <returns>A task representing the flush operation.</returns>
    Task FlushAsync();

    /// <summary>
    /// Exports diagnostics data.
    /// </summary>
    /// <param name="format">The export format.</param>
    /// <param name="startTime">The start time for data export.</param>
    /// <param name="endTime">The end time for data export.</param>
    /// <returns>The exported data.</returns>
    Task<byte[]> ExportDataAsync(string format, DateTimeOffset startTime, DateTimeOffset endTime);

    /// <summary>
    /// Clears diagnostics data older than the specified duration.
    /// </summary>
    /// <param name="retentionPeriod">The retention period.</param>
    /// <returns>The number of items cleared.</returns>
    Task<long> ClearOldDataAsync(TimeSpan retentionPeriod);

    // Configuration & Management

    /// <summary>
    /// Gets the current diagnostics configuration.
    /// </summary>
    /// <returns>The diagnostics configuration.</returns>
    DiagnosticsConfig GetConfig();

    /// <summary>
    /// Updates the diagnostics configuration.
    /// </summary>
    /// <param name="config">The new configuration.</param>
    void UpdateConfig(DiagnosticsConfig config);

    /// <summary>
    /// Gets diagnostics statistics.
    /// </summary>
    /// <returns>Diagnostics statistics.</returns>
    DiagnosticsStats GetStats();

    /// <summary>
    /// Resets diagnostics statistics.
    /// </summary>
    void ResetStats();

    /// <summary>
    /// Gets current diagnostics status.
    /// </summary>
    /// <returns>The diagnostics status.</returns>
    DiagnosticsStatus GetStatus();
}

/// <summary>
/// Interface for operation profiling.
/// </summary>
public interface IOperationProfiler : IDisposable
{
    /// <summary>
    /// Adds a profiling step/milestone.
    /// </summary>
    /// <param name="stepName">The step name.</param>
    /// <param name="tags">Optional step tags.</param>
    void AddStep(string stepName, Dictionary<string, object>? tags = null);

    /// <summary>
    /// Records a custom metric for this operation.
    /// </summary>
    /// <param name="name">The metric name.</param>
    /// <param name="value">The metric value.</param>
    /// <param name="unit">The metric unit.</param>
    void RecordMetric(string name, double value, string unit = "ms");

    /// <summary>
    /// Gets the profiling results.
    /// </summary>
    /// <returns>The profiling results.</returns>
    ProfilingResult GetResult();
}

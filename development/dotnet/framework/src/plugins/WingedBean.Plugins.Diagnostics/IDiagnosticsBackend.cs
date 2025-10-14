using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Plate.CrossMilo.Contracts.Diagnostics;

namespace WingedBean.Plugins.Diagnostics;

/// <summary>
/// Interface for diagnostics backend providers.
/// This is the Tier 4 provider contract that allows different diagnostics implementations
/// (Sentry, OpenTelemetry, Firebase, etc.) to be loaded as separate plugins at runtime.
/// </summary>
public interface IDiagnosticsBackend
{
    /// <summary>
    /// Starts a new trace span.
    /// </summary>
    Task StartSpan(TraceSpan span);

    /// <summary>
    /// Sets tags on an active span.
    /// </summary>
    Task SetSpanTags(string spanId, Dictionary<string, object> tags);

    /// <summary>
    /// Records an error on an active span.
    /// </summary>
    Task SetSpanError(string spanId, string error, Dictionary<string, object>? tags = null);

    /// <summary>
    /// Ends an active trace span.
    /// </summary>
    Task EndSpan(TraceSpan span);

    /// <summary>
    /// Adds a breadcrumb for debugging context.
    /// </summary>
    Task AddBreadcrumb(DiagnosticBreadcrumb breadcrumb);

    /// <summary>
    /// Clears all breadcrumbs.
    /// </summary>
    Task ClearBreadcrumbs();

    /// <summary>
    /// Records an exception with context.
    /// </summary>
    Task RecordException(Exception exception, Dictionary<string, object> context);

    /// <summary>
    /// Records an error message with context.
    /// </summary>
    Task RecordError(string message, Dictionary<string, object> context);

    /// <summary>
    /// Records a metric value.
    /// </summary>
    Task RecordMetric(string name, double value, string unit, Dictionary<string, object> tags);

    /// <summary>
    /// Records a counter increment.
    /// </summary>
    Task RecordCounter(string name, long increment, Dictionary<string, object> tags);

    /// <summary>
    /// Records a gauge value.
    /// </summary>
    Task RecordGauge(string name, double value, Dictionary<string, object> tags);

    /// <summary>
    /// Records a histogram value.
    /// </summary>
    Task RecordHistogram(string name, double value, Dictionary<string, object> tags);

    /// <summary>
    /// Starts profiling for an operation.
    /// </summary>
    Task StartProfiling(string operationName);

    /// <summary>
    /// Ends profiling and records the session data.
    /// </summary>
    Task EndProfiling(string operationName, ProfilingSession session);

    /// <summary>
    /// Registers a health check.
    /// </summary>
    Task RegisterHealthCheck(string name, HealthCheckRegistration registration);

    /// <summary>
    /// Unregisters a health check.
    /// </summary>
    Task UnregisterHealthCheck(string name);

    /// <summary>
    /// Records the result of a health check execution.
    /// </summary>
    Task RecordHealthCheckResult(string name, HealthCheckResult result);

    /// <summary>
    /// Records system-level metrics (CPU, memory, etc.).
    /// </summary>
    Task RecordSystemMetrics(Dictionary<string, object> metrics);

    /// <summary>
    /// Records a diagnostic snapshot.
    /// </summary>
    Task RecordSnapshot(DiagnosticSnapshot snapshot);

    /// <summary>
    /// Sets up an alert rule.
    /// </summary>
    Task SetupAlert(AlertRule rule);

    /// <summary>
    /// Updates an existing alert rule.
    /// </summary>
    Task UpdateAlert(string ruleName, AlertRule rule);

    /// <summary>
    /// Removes an alert rule.
    /// </summary>
    Task RemoveAlert(string ruleName);

    /// <summary>
    /// Acknowledges an alert.
    /// </summary>
    Task AcknowledgeAlert(string alertId, string user, string? comment = null);

    /// <summary>
    /// Flushes any buffered data to the backend.
    /// </summary>
    Task FlushAsync();

    /// <summary>
    /// Exports diagnostic data in the specified format.
    /// </summary>
    Task<byte[]> ExportDataAsync(string format, DateTimeOffset startTime, DateTimeOffset endTime);

    /// <summary>
    /// Clears old diagnostic data based on retention period.
    /// </summary>
    Task<long> ClearOldDataAsync(TimeSpan retentionPeriod);
}

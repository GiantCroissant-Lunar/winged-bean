using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace WingedBean.Contracts.Diagnostics;

/// <summary>
/// Configuration for the diagnostics service.
/// </summary>
public class DiagnosticsConfig
{
    /// <summary>
    /// Whether diagnostics is enabled by default.
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// The diagnostics backend to use.
    /// </summary>
    public DiagnosticsBackend Backend { get; set; } = DiagnosticsBackend.InMemory;

    /// <summary>
    /// Backend-specific configuration.
    /// </summary>
    public Dictionary<string, object> BackendConfig { get; set; } = new();

    /// <summary>
    /// Sampling rate for traces (0.0 to 1.0).
    /// </summary>
    public double SamplingRate { get; set; } = 1.0;

    /// <summary>
    /// Maximum number of breadcrumbs to keep.
    /// </summary>
    public int MaxBreadcrumbs { get; set; } = 100;

    /// <summary>
    /// Health check interval in seconds.
    /// </summary>
    public int HealthCheckIntervalSeconds { get; set; } = 60;

    /// <summary>
    /// Alert evaluation interval in seconds.
    /// </summary>
    public int AlertEvaluationIntervalSeconds { get; set; } = 30;

    /// <summary>
    /// Data retention period in days.
    /// </summary>
    public int RetentionDays { get; set; } = 7;

    /// <summary>
    /// Flush interval in seconds.
    /// </summary>
    public int FlushIntervalSeconds { get; set; } = 30;

    /// <summary>
    /// Whether to capture thread dumps in snapshots.
    /// </summary>
    public bool CaptureThreadDumps { get; set; } = true;

    /// <summary>
    /// Whether to capture heap dumps in snapshots.
    /// </summary>
    public bool CaptureHeapDumps { get; set; } = false;

    /// <summary>
    /// Maximum number of snapshots to keep.
    /// </summary>
    public int MaxSnapshots { get; set; } = 50;
}

/// <summary>
/// Diagnostics backend types.
/// </summary>
/// <remarks>
/// <para><strong>Recommended Production Configuration (RFC-0033/0034/0035):</strong></para>
/// <list type="bullet">
///   <item><description><strong>Sentry:</strong> Error tracking and crash reporting (RFC-0034)</description></item>
///   <item><description><strong>OpenTelemetry:</strong> Distributed tracing and performance monitoring (RFC-0035)</description></item>
/// </list>
/// <para>IDiagnosticsService implementation acts as a thin wrapper coordinating both backends.</para>
/// </remarks>
public enum DiagnosticsBackend
{
    /// <summary>
    /// In-memory storage (for testing/debugging).
    /// </summary>
    InMemory,

    /// <summary>
    /// File-based storage.
    /// </summary>
    File,

    /// <summary>
    /// Application Insights.
    /// </summary>
    /// <remarks>Legacy option. Consider using Sentry + OpenTelemetry for modern observability.</remarks>
    ApplicationInsights,

    /// <summary>
    /// Sentry error tracking (RFC-0034).
    /// </summary>
    /// <remarks>Recommended for error tracking, crash reporting, and exception monitoring.</remarks>
    Sentry,

    /// <summary>
    /// OpenTelemetry export (RFC-0035).
    /// </summary>
    /// <remarks>Recommended for distributed tracing, spans, and performance monitoring.</remarks>
    OpenTelemetry,

    /// <summary>
    /// Zipkin distributed tracing.
    /// </summary>
    Zipkin,

    /// <summary>
    /// Custom backend implementation.
    /// </summary>
    Custom
}

/// <summary>
/// Span kinds for distributed tracing.
/// </summary>
public enum SpanKind
{
    /// <summary>
    /// Internal span.
    /// </summary>
    Internal,

    /// <summary>
    /// Server-side span.
    /// </summary>
    Server,

    /// <summary>
    /// Client-side span.
    /// </summary>
    Client,

    /// <summary>
    /// Producer span (message sending).
    /// </summary>
    Producer,

    /// <summary>
    /// Consumer span (message receiving).
    /// </summary>
    Consumer
}

/// <summary>
/// Breadcrumb severity levels.
/// </summary>
public enum BreadcrumbLevel
{
    Debug,
    Info,
    Warning,
    Error,
    Critical
}

/// <summary>
/// Profiling configuration.
/// </summary>
public class ProfilingConfig
{
    /// <summary>
    /// Whether to enable memory profiling.
    /// </summary>
    public bool EnableMemoryProfiling { get; set; } = true;

    /// <summary>
    /// Whether to enable CPU profiling.
    /// </summary>
    public bool EnableCpuProfiling { get; set; } = true;

    /// <summary>
    /// Sampling interval for profiling.
    /// </summary>
    public TimeSpan SamplingInterval { get; set; } = TimeSpan.FromMilliseconds(10);

    /// <summary>
    /// Maximum profiling duration.
    /// </summary>
    public TimeSpan MaxDuration { get; set; } = TimeSpan.FromMinutes(5);

    /// <summary>
    /// Additional profiling tags.
    /// </summary>
    public Dictionary<string, object> Tags { get; set; } = new();
}

/// <summary>
/// Health check configuration.
/// </summary>
public class HealthCheckConfig
{
    /// <summary>
    /// The health check name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// The health check function.
    /// </summary>
    public Func<Task<HealthCheckResult>>? Check { get; set; }

    /// <summary>
    /// Synchronous health check function.
    /// </summary>
    public Func<HealthCheckResult>? SyncCheck { get; set; }

    /// <summary>
    /// Health check timeout.
    /// </summary>
    public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(30);

    /// <summary>
    /// Health check tags.
    /// </summary>
    public Dictionary<string, object> Tags { get; set; } = new();

    /// <summary>
    /// Failure threshold (consecutive failures before marking unhealthy).
    /// </summary>
    public int FailureThreshold { get; set; } = 1;

    /// <summary>
    /// Success threshold (consecutive successes before marking healthy).
    /// </summary>
    public int SuccessThreshold { get; set; } = 1;

    /// <summary>
    /// Whether the health check is enabled.
    /// </summary>
    public bool Enabled { get; set; } = true;
}

/// <summary>
/// Health check result.
/// </summary>
public class HealthCheckResult
{
    /// <summary>
    /// The health status.
    /// </summary>
    public HealthStatus Status { get; set; } = HealthStatus.Healthy;

    /// <summary>
    /// The health check description.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// The health check duration.
    /// </summary>
    public TimeSpan Duration { get; set; }

    /// <summary>
    /// Additional health check data.
    /// </summary>
    public Dictionary<string, object> Data { get; set; } = new();

    /// <summary>
    /// Exception if the health check failed.
    /// </summary>
    public Exception? Exception { get; set; }

    /// <summary>
    /// Tags associated with this health check.
    /// </summary>
    public Dictionary<string, object> Tags { get; set; } = new();

    /// <summary>
    /// Timestamp when the health check was performed.
    /// </summary>
    public DateTimeOffset Timestamp { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Number of consecutive failures.
    /// </summary>
    public int ConsecutiveFailures { get; set; }

    /// <summary>
    /// Number of consecutive successes.
    /// </summary>
    public int ConsecutiveSuccesses { get; set; }
}

/// <summary>
/// Overall health status.
/// </summary>
public enum HealthStatus
{
    /// <summary>
    /// System is healthy.
    /// </summary>
    Healthy,

    /// <summary>
    /// System is degraded but operational.
    /// </summary>
    Degraded,

    /// <summary>
    /// System is unhealthy.
    /// </summary>
    Unhealthy,

    /// <summary>
    /// Health status is unknown.
    /// </summary>
    Unknown
}

/// <summary>
/// Snapshot configuration.
/// </summary>
public class SnapshotConfig
{
    /// <summary>
    /// Whether to include heap dump.
    /// </summary>
    public bool IncludeHeapDump { get; set; }

    /// <summary>
    /// Whether to include thread dump.
    /// </summary>
    public bool IncludeThreadDump { get; set; } = true;

    /// <summary>
    /// Whether to include system metrics.
    /// </summary>
    public bool IncludeSystemMetrics { get; set; } = true;

    /// <summary>
    /// Whether to include active spans.
    /// </summary>
    public bool IncludeActiveSpans { get; set; } = true;

    /// <summary>
    /// Whether to include breadcrumbs.
    /// </summary>
    public bool IncludeBreadcrumbs { get; set; } = true;

    /// <summary>
    /// Whether to include health check results.
    /// </summary>
    public bool IncludeHealthChecks { get; set; } = true;

    /// <summary>
    /// Maximum snapshot duration.
    /// </summary>
    public TimeSpan MaxDuration { get; set; } = TimeSpan.FromSeconds(30);

    /// <summary>
    /// Additional snapshot tags.
    /// </summary>
    public Dictionary<string, object> Tags { get; set; } = new();
}

/// <summary>
/// Diagnostic snapshot containing system state.
/// </summary>
public class DiagnosticSnapshot
{
    /// <summary>
    /// The snapshot ID.
    /// </summary>
    public string Id { get; set; } = Guid.NewGuid().ToString();

    /// <summary>
    /// The snapshot timestamp.
    /// </summary>
    public DateTimeOffset Timestamp { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// System metrics at snapshot time.
    /// </summary>
    public SystemMetrics SystemMetrics { get; set; } = new();

    /// <summary>
    /// Current health check results.
    /// </summary>
    public Dictionary<string, HealthCheckResult> HealthChecks { get; set; } = new();

    /// <summary>
    /// Active trace spans.
    /// </summary>
    public List<TraceSpan> ActiveSpans { get; set; } = new();

    /// <summary>
    /// Recent breadcrumbs.
    /// </summary>
    public List<DiagnosticBreadcrumb> Breadcrumbs { get; set; } = new();

    /// <summary>
    /// Thread dump (if captured).
    /// </summary>
    public ThreadDump? ThreadDump { get; set; }

    /// <summary>
    /// Heap dump summary (if captured).
    /// </summary>
    public HeapDump? HeapDump { get; set; }

    /// <summary>
    /// Additional snapshot data.
    /// </summary>
    public Dictionary<string, object> AdditionalData { get; set; } = new();

    /// <summary>
    /// Snapshot tags.
    /// </summary>
    public Dictionary<string, object> Tags { get; set; } = new();
}

/// <summary>
/// System resource metrics.
/// </summary>
public class SystemMetrics
{
    /// <summary>
    /// CPU usage percentage.
    /// </summary>
    public double CpuUsagePercent { get; set; }

    /// <summary>
    /// Memory usage in bytes.
    /// </summary>
    public long MemoryUsageBytes { get; set; }

    /// <summary>
    /// Total memory in bytes.
    /// </summary>
    public long TotalMemoryBytes { get; set; }

    /// <summary>
    /// Available memory in bytes.
    /// </summary>
    public long AvailableMemoryBytes { get; set; }

    /// <summary>
    /// Disk usage percentage.
    /// </summary>
    public double DiskUsagePercent { get; set; }

    /// <summary>
    /// Thread count.
    /// </summary>
    public int ThreadCount { get; set; }

    /// <summary>
    /// Active thread pool threads.
    /// </summary>
    public int ActiveThreadPoolThreads { get; set; }

    /// <summary>
    /// Process ID.
    /// </summary>
    public int ProcessId { get; set; }

    /// <summary>
    /// Process start time.
    /// </summary>
    public DateTimeOffset ProcessStartTime { get; set; }

    /// <summary>
    /// Timestamp when metrics were collected.
    /// </summary>
    public DateTimeOffset Timestamp { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Additional custom metrics.
    /// </summary>
    public Dictionary<string, object> CustomMetrics { get; set; } = new();
}

/// <summary>
/// Trace span information.
/// </summary>
public class TraceSpan
{
    /// <summary>
    /// The trace ID.
    /// </summary>
    public string TraceId { get; set; } = string.Empty;

    /// <summary>
    /// The span ID.
    /// </summary>
    public string SpanId { get; set; } = string.Empty;

    /// <summary>
    /// The parent span ID.
    /// </summary>
    public string? ParentSpanId { get; set; }

    /// <summary>
    /// The operation name.
    /// </summary>
    public string OperationName { get; set; } = string.Empty;

    /// <summary>
    /// The span kind.
    /// </summary>
    public SpanKind Kind { get; set; } = SpanKind.Internal;

    /// <summary>
    /// The span start time.
    /// </summary>
    public DateTimeOffset StartTime { get; set; }

    /// <summary>
    /// The span end time (null if still active).
    /// </summary>
    public DateTimeOffset? EndTime { get; set; }

    /// <summary>
    /// The span duration (calculated).
    /// </summary>
    public TimeSpan? Duration => EndTime.HasValue ? EndTime - StartTime : null;

    /// <summary>
    /// Span tags/attributes.
    /// </summary>
    public Dictionary<string, object> Tags { get; set; } = new();

    /// <summary>
    /// Whether the span represents an error.
    /// </summary>
    public bool IsError { get; set; }

    /// <summary>
    /// Error message if the span failed.
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Stack trace if the span failed.
    /// </summary>
    public string? StackTrace { get; set; }
}

/// <summary>
/// Diagnostic breadcrumb.
/// </summary>
public class DiagnosticBreadcrumb
{
    /// <summary>
    /// The breadcrumb message.
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// The breadcrumb category.
    /// </summary>
    public string Category { get; set; } = string.Empty;

    /// <summary>
    /// The breadcrumb level.
    /// </summary>
    public BreadcrumbLevel Level { get; set; } = BreadcrumbLevel.Info;

    /// <summary>
    /// Additional breadcrumb data.
    /// </summary>
    public Dictionary<string, object> Data { get; set; } = new();

    /// <summary>
    /// Timestamp when the breadcrumb was created.
    /// </summary>
    public DateTimeOffset Timestamp { get; set; } = DateTimeOffset.UtcNow;
}

/// <summary>
/// Thread dump information.
/// </summary>
public class ThreadDump
{
    /// <summary>
    /// List of thread information.
    /// </summary>
    public List<ThreadInfo> Threads { get; set; } = new();

    /// <summary>
    /// Timestamp when the dump was captured.
    /// </summary>
    public DateTimeOffset Timestamp { get; set; } = DateTimeOffset.UtcNow;
}

/// <summary>
/// Thread information.
/// </summary>
public class ThreadInfo
{
    /// <summary>
    /// The thread ID.
    /// </summary>
    public int ThreadId { get; set; }

    /// <summary>
    /// The thread name.
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// The thread state.
    /// </summary>
    public ThreadState State { get; set; }

    /// <summary>
    /// Whether the thread is a background thread.
    /// </summary>
    public bool IsBackground { get; set; }

    /// <summary>
    /// The thread priority.
    /// </summary>
    public ThreadPriority Priority { get; set; }

    /// <summary>
    /// The current stack trace.
    /// </summary>
    public string? StackTrace { get; set; }

    /// <summary>
    /// CPU time used by the thread.
    /// </summary>
    public TimeSpan? CpuTime { get; set; }

    /// <summary>
    /// Whether the thread is alive.
    /// </summary>
    public bool IsAlive { get; set; }
}

/// <summary>
/// Heap dump summary.
/// </summary>
public class HeapDump
{
    /// <summary>
    /// Total heap size in bytes.
    /// </summary>
    public long TotalHeapSizeBytes { get; set; }

    /// <summary>
    /// Generation sizes in bytes.
    /// </summary>
    public Dictionary<int, long> GenerationSizes { get; set; } = new();

    /// <summary>
    /// Large object heap size in bytes.
    /// </summary>
    public long LargeObjectHeapSizeBytes { get; set; }

    /// <summary>
    /// Top memory-consuming types.
    /// </summary>
    public List<TypeMemoryInfo> TopTypes { get; set; } = new();

    /// <summary>
    /// Number of garbage collections by generation.
    /// </summary>
    public Dictionary<int, long> GarbageCollections { get; set; } = new();

    /// <summary>
    /// Timestamp when the dump was captured.
    /// </summary>
    public DateTimeOffset Timestamp { get; set; } = DateTimeOffset.UtcNow;
}

/// <summary>
/// Memory information for a type.
/// </summary>
public class TypeMemoryInfo
{
    /// <summary>
    /// The type name.
    /// </summary>
    public string TypeName { get; set; } = string.Empty;

    /// <summary>
    /// Number of instances.
    /// </summary>
    public long InstanceCount { get; set; }

    /// <summary>
    /// Total memory used in bytes.
    /// </summary>
    public long TotalSizeBytes { get; set; }

    /// <summary>
    /// Average size per instance in bytes.
    /// </summary>
    public long AverageSizeBytes { get; set; }
}

/// <summary>
/// Profiling result for an operation.
/// </summary>
public class ProfilingResult
{
    /// <summary>
    /// The operation name.
    /// </summary>
    public string OperationName { get; set; } = string.Empty;

    /// <summary>
    /// The total operation duration.
    /// </summary>
    public TimeSpan TotalDuration { get; set; }

    /// <summary>
    /// Individual profiling steps.
    /// </summary>
    public List<ProfilingStep> Steps { get; set; } = new();

    /// <summary>
    /// Custom metrics recorded during profiling.
    /// </summary>
    public List<ProfilingMetric> Metrics { get; set; } = new();

    /// <summary>
    /// Memory usage information.
    /// </summary>
    public MemoryUsageInfo? MemoryUsage { get; set; }

    /// <summary>
    /// CPU usage information.
    /// </summary>
    public CpuUsageInfo? CpuUsage { get; set; }

    /// <summary>
    /// Timestamp when profiling started.
    /// </summary>
    public DateTimeOffset StartTime { get; set; }

    /// <summary>
    /// Timestamp when profiling ended.
    /// </summary>
    public DateTimeOffset EndTime { get; set; }
}

/// <summary>
/// Memory usage information.
/// </summary>
public class MemoryUsageInfo
{
    /// <summary>
    /// Memory usage at start in bytes.
    /// </summary>
    public long StartMemoryBytes { get; set; }

    /// <summary>
    /// Memory usage at end in bytes.
    /// </summary>
    public long EndMemoryBytes { get; set; }

    /// <summary>
    /// Peak memory usage in bytes.
    /// </summary>
    public long PeakMemoryBytes { get; set; }

    /// <summary>
    /// Memory allocated during operation in bytes.
    /// </summary>
    public long AllocatedBytes { get; set; }
}

/// <summary>
/// CPU usage information.
/// </summary>
public class CpuUsageInfo
{
    /// <summary>
    /// CPU time used in milliseconds.
    /// </summary>
    public long CpuTimeMs { get; set; }

    /// <summary>
    /// Average CPU usage percentage.
    /// </summary>
    public double AverageCpuPercent { get; set; }

    /// <summary>
    /// Peak CPU usage percentage.
    /// </summary>
    public double PeakCpuPercent { get; set; }
}

/// <summary>
/// Profiling step information.
/// </summary>
public class ProfilingStep
{
    /// <summary>
    /// The step name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// The step start time relative to operation start.
    /// </summary>
    public TimeSpan StartOffset { get; set; }

    /// <summary>
    /// The step duration.
    /// </summary>
    public TimeSpan Duration { get; set; }

    /// <summary>
    /// Additional step data.
    /// </summary>
    public Dictionary<string, object> Data { get; set; } = new();
}

/// <summary>
/// Profiling metric.
/// </summary>
public class ProfilingMetric
{
    /// <summary>
    /// The metric name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// The metric value.
    /// </summary>
    public double Value { get; set; }

    /// <summary>
    /// The metric unit.
    /// </summary>
    public string Unit { get; set; } = string.Empty;

    /// <summary>
    /// Timestamp when the metric was recorded.
    /// </summary>
    public DateTimeOffset Timestamp { get; set; }
}

/// <summary>
/// Alert rule configuration.
/// </summary>
public class AlertRule
{
    /// <summary>
    /// The alert rule name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// The alert rule description.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// The metric name to monitor.
    /// </summary>
    public string MetricName { get; set; } = string.Empty;

    /// <summary>
    /// The alert condition.
    /// </summary>
    public AlertCondition Condition { get; set; } = AlertCondition.GreaterThan;

    /// <summary>
    /// The threshold value.
    /// </summary>
    public double Threshold { get; set; }

    /// <summary>
    /// The evaluation window duration.
    /// </summary>
    public TimeSpan EvaluationWindow { get; set; } = TimeSpan.FromMinutes(5);

    /// <summary>
    /// The alert severity.
    /// </summary>
    public AlertSeverity Severity { get; set; } = AlertSeverity.Warning;

    /// <summary>
    /// Additional alert tags.
    /// </summary>
    public Dictionary<string, object> Tags { get; set; } = new();

    /// <summary>
    /// Whether the alert is enabled.
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Cooldown period between alerts.
    /// </summary>
    public TimeSpan CooldownPeriod { get; set; } = TimeSpan.FromMinutes(5);

    /// <summary>
    /// Auto-resolve after duration (null for manual resolution).
    /// </summary>
    public TimeSpan? AutoResolveAfter { get; set; }
}

/// <summary>
/// Alert condition types.
/// </summary>
public enum AlertCondition
{
    /// <summary>
    /// Trigger when value is greater than threshold.
    /// </summary>
    GreaterThan,

    /// <summary>
    /// Trigger when value is less than threshold.
    /// </summary>
    LessThan,

    /// <summary>
    /// Trigger when value equals threshold.
    /// </summary>
    Equals,

    /// <summary>
    /// Trigger when value does not equal threshold.
    /// </summary>
    NotEquals,

    /// <summary>
    /// Trigger when value is greater than or equal to threshold.
    /// </summary>
    GreaterThanOrEqual,

    /// <summary>
    /// Trigger when value is less than or equal to threshold.
    /// </summary>
    LessThanOrEqual,

    /// <summary>
    /// Trigger when rate of change exceeds threshold.
    /// </summary>
    RateOfChange
}

/// <summary>
/// Alert severity levels.
/// </summary>
public enum AlertSeverity
{
    /// <summary>
    /// Informational alert.
    /// </summary>
    Info,

    /// <summary>
    /// Warning alert.
    /// </summary>
    Warning,

    /// <summary>
    /// Error alert.
    /// </summary>
    Error,

    /// <summary>
    /// Critical alert.
    /// </summary>
    Critical
}

/// <summary>
/// Active alert information.
/// </summary>
public class Alert
{
    /// <summary>
    /// The alert ID.
    /// </summary>
    public string Id { get; set; } = Guid.NewGuid().ToString();

    /// <summary>
    /// The alert rule name.
    /// </summary>
    public string RuleName { get; set; } = string.Empty;

    /// <summary>
    /// The alert message.
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// The alert severity.
    /// </summary>
    public AlertSeverity Severity { get; set; }

    /// <summary>
    /// The metric value that triggered the alert.
    /// </summary>
    public double TriggerValue { get; set; }

    /// <summary>
    /// The threshold value.
    /// </summary>
    public double Threshold { get; set; }

    /// <summary>
    /// Additional alert data.
    /// </summary>
    public Dictionary<string, object> Data { get; set; } = new();

    /// <summary>
    /// Timestamp when the alert was triggered.
    /// </summary>
    public DateTimeOffset TriggeredAt { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Timestamp when the alert was resolved (null if still active).
    /// </summary>
    public DateTimeOffset? ResolvedAt { get; set; }

    /// <summary>
    /// User who acknowledged the alert.
    /// </summary>
    public string? AcknowledgedBy { get; set; }

    /// <summary>
    /// Acknowledgment comment.
    /// </summary>
    public string? AcknowledgmentComment { get; set; }

    /// <summary>
    /// Timestamp when the alert was acknowledged.
    /// </summary>
    public DateTimeOffset? AcknowledgedAt { get; set; }
}

/// <summary>
/// Diagnostics statistics.
/// </summary>
public class DiagnosticsStats
{
    /// <summary>
    /// Total number of spans created.
    /// </summary>
    public long TotalSpans { get; set; }

    /// <summary>
    /// Number of active spans.
    /// </summary>
    public long ActiveSpans { get; set; }

    /// <summary>
    /// Number of errors recorded.
    /// </summary>
    public long ErrorCount { get; set; }

    /// <summary>
    /// Number of breadcrumbs recorded.
    /// </summary>
    public long BreadcrumbCount { get; set; }

    /// <summary>
    /// Number of health checks registered.
    /// </summary>
    public long HealthCheckCount { get; set; }

    /// <summary>
    /// Number of alerts configured.
    /// </summary>
    public long AlertRuleCount { get; set; }

    /// <summary>
    /// Number of active alerts.
    /// </summary>
    public long ActiveAlertCount { get; set; }

    /// <summary>
    /// Number of snapshots captured.
    /// </summary>
    public long SnapshotCount { get; set; }

    /// <summary>
    /// Total metrics recorded.
    /// </summary>
    public long MetricCount { get; set; }

    /// <summary>
    /// Service uptime.
    /// </summary>
    public TimeSpan Uptime { get; set; }

    /// <summary>
    /// Last flush timestamp.
    /// </summary>
    public DateTimeOffset? LastFlush { get; set; }

    /// <summary>
    /// Additional statistics.
    /// </summary>
    public Dictionary<string, object> AdditionalStats { get; set; } = new();
}

/// <summary>
/// Diagnostics status information.
/// </summary>
public class DiagnosticsStatus
{
    /// <summary>
    /// Whether diagnostics is enabled.
    /// </summary>
    public bool IsEnabled { get; set; }

    /// <summary>
    /// Current backend being used.
    /// </summary>
    public DiagnosticsBackend Backend { get; set; }

    /// <summary>
    /// Whether the backend is healthy.
    /// </summary>
    public bool BackendHealthy { get; set; }

    /// <summary>
    /// Current health status.
    /// </summary>
    public HealthStatus HealthStatus { get; set; }

    /// <summary>
    /// Number of pending operations.
    /// </summary>
    public long PendingOperations { get; set; }

    /// <summary>
    /// Memory usage in bytes.
    /// </summary>
    public long MemoryUsageBytes { get; set; }

    /// <summary>
    /// Last status update timestamp.
    /// </summary>
    public DateTimeOffset LastUpdate { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Status messages.
    /// </summary>
    public List<string> Messages { get; set; } = new();
}

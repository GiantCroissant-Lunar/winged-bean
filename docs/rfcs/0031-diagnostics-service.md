---
id: RFC-0031
title: Diagnostics Service for System Health and Troubleshooting
status: Draft
category: architecture
created: 2025-10-05
updated: 2025-10-05
related-rfcs: RFC-0033, RFC-0034, RFC-0035
---

# RFC-0031: Diagnostics Service for System Health and Troubleshooting

> **⚠️ Implementation Note**: This RFC defines the **contract** for `IDiagnosticsService`. The **implementation strategy** is defined in:
> - **RFC-0033**: Observability Strategy Foundation (architecture)
> - **RFC-0034**: Sentry Integration (error tracking implementation)
> - **RFC-0035**: OpenTelemetry Integration (distributed tracing implementation)
>
> `IDiagnosticsService` is implemented as a **thin wrapper** over Sentry SDK (errors) + OpenTelemetry SDK (traces) + custom health checks.

## Summary

Introduce a comprehensive Diagnostics service (`IDiagnosticsService`) for system health monitoring, performance profiling, error tracking, and troubleshooting in the WingedBean framework. The service will provide real-time diagnostics, distributed tracing, health checks, and diagnostic snapshots for debugging production issues.

**Architecture**: This RFC defines the application-facing contract. The underlying implementation uses industry-standard SDKs (Sentry, OpenTelemetry) as defined in RFC-0033/0034/0035.

## Motivation

### Current State

The WingedBean framework currently lacks systematic diagnostics capabilities:
- ❌ No centralized health monitoring
- ❌ No distributed tracing for plugin interactions
- ❌ Limited error context for debugging
- ❌ No performance profiling tools
- ❌ No system resource monitoring
- ❌ Ad-hoc diagnostic logging scattered across codebase

### Problems to Solve

1. **Debugging Difficulty**: Hard to reproduce and diagnose production issues
2. **Performance Bottlenecks**: Cannot identify slow operations or resource leaks
3. **Health Visibility**: No real-time system health dashboard
4. **Error Context**: Stack traces without sufficient context
5. **Integration Issues**: Plugin interactions not traceable
6. **Resource Monitoring**: Memory leaks and CPU spikes go undetected

### Goals

1. ✅ **Health Monitoring**: Real-time system health checks and status
2. ✅ **Distributed Tracing**: Track operations across plugins and services
3. ✅ **Performance Profiling**: Identify bottlenecks and slow operations
4. ✅ **Error Tracking**: Rich error context with breadcrumbs
5. ✅ **Resource Monitoring**: CPU, memory, thread pool metrics
6. ✅ **Diagnostic Snapshots**: Capture system state for offline analysis
7. ✅ **Correlation**: Link logs, traces, and metrics with correlation IDs
8. ✅ **Alerting**: Threshold-based alerts for critical issues

## Proposal

### Service Contract

```csharp
namespace WingedBean.Contracts.Diagnostics;

/// <summary>
/// Diagnostics service for system health monitoring and troubleshooting.
/// Provides tracing, profiling, health checks, and diagnostic snapshots.
/// </summary>
public interface IDiagnosticsService
{
    // ===== Health Checks =====

    /// <summary>
    /// Register a health check.
    /// </summary>
    void RegisterHealthCheck(string name, Func<CancellationToken, Task<HealthCheckResult>> check);

    /// <summary>
    /// Get all health check results.
    /// </summary>
    Task<HealthReport> GetHealthAsync(CancellationToken ct = default);

    /// <summary>
    /// Get health status (Healthy, Degraded, Unhealthy).
    /// </summary>
    Task<HealthStatus> GetHealthStatusAsync(CancellationToken ct = default);

    // ===== Distributed Tracing =====

    /// <summary>
    /// Start a new trace span (activity).
    /// </summary>
    IDisposable StartTrace(string operationName, IDictionary<string, object>? tags = null);

    /// <summary>
    /// Add a tag to the current trace span.
    /// </summary>
    void AddTraceTag(string key, object value);

    /// <summary>
    /// Add an event to the current trace span.
    /// </summary>
    void AddTraceEvent(string eventName, IDictionary<string, object>? attributes = null);

    /// <summary>
    /// Get the current trace/correlation ID.
    /// </summary>
    string? GetCurrentTraceId();

    // ===== Performance Profiling =====

    /// <summary>
    /// Start a performance measurement.
    /// </summary>
    IDisposable MeasurePerformance(string operationName, IDictionary<string, object>? tags = null);

    /// <summary>
    /// Record a custom metric.
    /// </summary>
    void RecordMetric(string name, double value, IDictionary<string, object>? tags = null);

    /// <summary>
    /// Get performance statistics for an operation.
    /// </summary>
    PerformanceStatistics? GetPerformanceStats(string operationName);

    // ===== Error Tracking =====

    /// <summary>
    /// Track an error/exception with context.
    /// </summary>
    Task TrackErrorAsync(Exception exception, ErrorSeverity severity = ErrorSeverity.Error, IDictionary<string, object>? context = null, CancellationToken ct = default);

    /// <summary>
    /// Add a breadcrumb (for error context).
    /// </summary>
    void AddBreadcrumb(string message, BreadcrumbLevel level = BreadcrumbLevel.Info, IDictionary<string, object>? data = null);

    /// <summary>
    /// Set user context for error tracking.
    /// </summary>
    void SetUserContext(string userId, string? username = null, string? email = null);

    // ===== System Monitoring =====

    /// <summary>
    /// Get current system resource usage.
    /// </summary>
    SystemResourceMetrics GetSystemMetrics();

    /// <summary>
    /// Start continuous resource monitoring.
    /// </summary>
    Task StartResourceMonitoringAsync(TimeSpan interval, CancellationToken ct = default);

    /// <summary>
    /// Stop resource monitoring.
    /// </summary>
    void StopResourceMonitoring();

    // ===== Diagnostic Snapshots =====

    /// <summary>
    /// Capture a diagnostic snapshot (heap dump, thread dump, etc.).
    /// </summary>
    Task<DiagnosticSnapshot> CaptureSnapshotAsync(SnapshotOptions options, CancellationToken ct = default);

    /// <summary>
    /// Export diagnostics to file.
    /// </summary>
    Task ExportDiagnosticsAsync(string filePath, DiagnosticExportFormat format = DiagnosticExportFormat.Json, CancellationToken ct = default);

    // ===== Alerting =====

    /// <summary>
    /// Register a diagnostic alert (threshold-based).
    /// </summary>
    void RegisterAlert(string name, Func<DiagnosticContext, bool> condition, Action<DiagnosticAlert> handler);

    /// <summary>
    /// Event raised when an alert fires.
    /// </summary>
    event EventHandler<DiagnosticAlert>? AlertTriggered;
}
```

### Data Models

```csharp
/// <summary>
/// Health check result.
/// </summary>
public class HealthCheckResult
{
    public HealthStatus Status { get; set; }
    public string Description { get; set; } = string.Empty;
    public TimeSpan Duration { get; set; }
    public Exception? Exception { get; set; }
    public Dictionary<string, object> Data { get; set; } = new();
}

/// <summary>
/// Health status enum.
/// </summary>
public enum HealthStatus
{
    Healthy,
    Degraded,
    Unhealthy
}

/// <summary>
/// Overall health report.
/// </summary>
public class HealthReport
{
    public HealthStatus Status { get; set; }
    public Dictionary<string, HealthCheckResult> Checks { get; set; } = new();
    public TimeSpan TotalDuration { get; set; }
}

/// <summary>
/// Performance statistics.
/// </summary>
public class PerformanceStatistics
{
    public string OperationName { get; set; } = string.Empty;
    public long ExecutionCount { get; set; }
    public TimeSpan TotalDuration { get; set; }
    public TimeSpan AverageDuration { get; set; }
    public TimeSpan MinDuration { get; set; }
    public TimeSpan MaxDuration { get; set; }
    public TimeSpan P50Duration { get; set; } // Median
    public TimeSpan P95Duration { get; set; }
    public TimeSpan P99Duration { get; set; }
}

/// <summary>
/// Error severity levels.
/// </summary>
public enum ErrorSeverity
{
    Debug,
    Info,
    Warning,
    Error,
    Critical
}

/// <summary>
/// Breadcrumb for error context.
/// </summary>
public class Breadcrumb
{
    public DateTimeOffset Timestamp { get; set; } = DateTimeOffset.UtcNow;
    public string Message { get; set; } = string.Empty;
    public BreadcrumbLevel Level { get; set; }
    public Dictionary<string, object> Data { get; set; } = new();
}

/// <summary>
/// Breadcrumb level.
/// </summary>
public enum BreadcrumbLevel
{
    Debug,
    Info,
    Warning,
    Error
}

/// <summary>
/// System resource metrics.
/// </summary>
public class SystemResourceMetrics
{
    public DateTimeOffset Timestamp { get; set; } = DateTimeOffset.UtcNow;

    // CPU
    public double CpuUsagePercent { get; set; }
    public int ProcessorCount { get; set; }

    // Memory
    public long WorkingSetBytes { get; set; }
    public long PrivateMemoryBytes { get; set; }
    public long GCTotalMemoryBytes { get; set; }
    public int Gen0Collections { get; set; }
    public int Gen1Collections { get; set; }
    public int Gen2Collections { get; set; }

    // Threads
    public int ThreadCount { get; set; }
    public int ThreadPoolThreads { get; set; }
    public int ThreadPoolCompletionPortThreads { get; set; }

    // I/O
    public long DiskReadBytes { get; set; }
    public long DiskWriteBytes { get; set; }
}

/// <summary>
/// Diagnostic snapshot.
/// </summary>
public class DiagnosticSnapshot
{
    public DateTimeOffset Timestamp { get; set; } = DateTimeOffset.UtcNow;
    public string SnapshotId { get; set; } = Guid.NewGuid().ToString();
    public SystemResourceMetrics SystemMetrics { get; set; } = new();
    public List<ThreadInfo> Threads { get; set; } = new();
    public Dictionary<string, HealthCheckResult> HealthChecks { get; set; } = new();
    public Dictionary<string, PerformanceStatistics> PerformanceStats { get; set; } = new();
    public List<Breadcrumb> RecentBreadcrumbs { get; set; } = new();
    public byte[]? HeapDump { get; set; }
}

/// <summary>
/// Thread information.
/// </summary>
public class ThreadInfo
{
    public int ThreadId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty;
    public string[] StackTrace { get; set; } = Array.Empty<string>();
}

/// <summary>
/// Snapshot options.
/// </summary>
public class SnapshotOptions
{
    public bool IncludeHeapDump { get; set; } = false;
    public bool IncludeThreadDump { get; set; } = true;
    public bool IncludeHealthChecks { get; set; } = true;
    public bool IncludePerformanceStats { get; set; } = true;
    public int BreadcrumbLimit { get; set; } = 50;
}

/// <summary>
/// Diagnostic alert.
/// </summary>
public class DiagnosticAlert
{
    public string Name { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public ErrorSeverity Severity { get; set; }
    public DateTimeOffset Timestamp { get; set; } = DateTimeOffset.UtcNow;
    public Dictionary<string, object> Context { get; set; } = new();
}

/// <summary>
/// Diagnostic export format.
/// </summary>
public enum DiagnosticExportFormat
{
    Json,
    Xml,
    Binary
}
```

## Architecture

### Component Diagram

```
┌─────────────────────────────────────────────────────────────┐
│                   Application Layer                          │
│  ┌──────────┐  ┌──────────┐  ┌──────────┐  ┌──────────┐    │
│  │ Plugins  │  │ Services │  │   Host   │  │   Game   │    │
│  └────┬─────┘  └────┬─────┘  └────┬─────┘  └────┬─────┘    │
│       │             │              │             │          │
│       └─────────────┴──────────────┴─────────────┘          │
│                           ↓                                 │
│               ┌──────────────────────┐                      │
│               │ IDiagnosticsService  │ (Contract)           │
│               └──────────┬───────────┘                      │
└───────────────────────────┼──────────────────────────────────┘
                            ↓
                ┌───────────────────────────┐
                │   DiagnosticsService      │
                │                           │
                │  ┌─────────────────────┐  │
                │  │ Health Manager      │  │
                │  ├─────────────────────┤  │
                │  │ Trace Manager       │  │
                │  ├─────────────────────┤  │
                │  │ Performance Monitor │  │
                │  ├─────────────────────┤  │
                │  │ Error Tracker       │  │
                │  ├─────────────────────┤  │
                │  │ Resource Monitor    │  │
                │  ├─────────────────────┤  │
                │  │ Snapshot Generator  │  │
                │  ├─────────────────────┤  │
                │  │ Alert Engine        │  │
                │  └─────────────────────┘  │
                └───────────┬───────────────┘
                            ↓
            ┌───────────────┴──────────────┐
            ↓                              ↓
    ┌──────────────────┐          ┌──────────────────┐
    │  Exporters       │          │  Event Sinks     │
    │                  │          │                  │
    │ • JSON           │          │ • Console        │
    │ • XML            │          │ • File           │
    │ • OpenTelemetry  │          │ • Application    │
    │ • Zipkin         │          │   Insights       │
    └──────────────────┘          │ • Sentry         │
                                  └──────────────────┘
```

### Diagnostic Flow

1. **Health Checks**: Periodic health check execution → aggregate status
2. **Tracing**: Start span → operations → add tags/events → end span
3. **Performance**: Measure operation → record metrics → calculate statistics
4. **Error Tracking**: Catch exception → add breadcrumbs → send to Sentry/App Insights
5. **Resource Monitoring**: Poll system metrics → alert on thresholds
6. **Snapshots**: Capture state → serialize → export to file/storage

## Implementation Plan

### Phase 1: Core Diagnostics (Week 1)

**Deliverables:**
- ✅ `WingedBean.Contracts.Diagnostics` - Interface contracts
- ✅ `WingedBean.Plugins.Diagnostics` - Base implementation:
  - Health check registry and executor
  - Distributed tracing (Activity API)
  - Performance measurement
  - Error tracking with breadcrumbs
- ✅ Console exporter

**Files:**
```
framework/src/WingedBean.Contracts.Diagnostics/
  ├── IDiagnosticsService.cs
  ├── HealthCheckResult.cs
  ├── PerformanceStatistics.cs
  ├── DiagnosticSnapshot.cs
  └── SystemResourceMetrics.cs

console/src/plugins/WingedBean.Plugins.Diagnostics/
  ├── DiagnosticsService.cs
  ├── HealthCheckManager.cs
  ├── TraceManager.cs
  ├── PerformanceMonitor.cs
  └── ErrorTracker.cs
```

### Phase 2: Resource Monitoring (Week 2)

**Deliverables:**
- ✅ System resource metrics (CPU, memory, threads)
- ✅ Continuous monitoring with intervals
- ✅ Threshold-based alerts
- ✅ Memory leak detection

**Files:**
```
console/src/plugins/WingedBean.Plugins.Diagnostics/
  ├── ResourceMonitor.cs
  ├── MemoryAnalyzer.cs
  └── AlertEngine.cs
```

### Phase 3: Diagnostic Snapshots (Week 3)

**Deliverables:**
- ✅ Snapshot capture (thread dump, heap dump)
- ✅ Export formats (JSON, XML, Binary)
- ✅ Snapshot analysis tools
- ✅ OpenTelemetry integration

**Files:**
```
console/src/plugins/WingedBean.Plugins.Diagnostics/
  ├── SnapshotGenerator.cs
  ├── HeapDumpAnalyzer.cs
  └── Exporters/
      ├── JsonExporter.cs
      ├── XmlExporter.cs
      └── OpenTelemetryExporter.cs
```

### Phase 4: Integration & Tools (Week 4)

**Deliverables:**
- ✅ Application Insights integration
- ✅ Sentry integration (error tracking)
- ✅ Diagnostic dashboard (Terminal.Gui)
- ✅ CLI tools for snapshot analysis

## Standard Health Checks

### System Health Checks
- `database_connection` - Database connectivity
- `plugin_loader` - Plugin loading status
- `memory_usage` - Memory threshold check
- `thread_pool` - Thread pool exhaustion check
- `disk_space` - Available disk space

### Service Health Checks
- `registry_health` - Service registry status
- `resilience_health` - Resilience service status
- `analytics_health` - Analytics backend connectivity

### Custom Health Checks
```csharp
diagnostics.RegisterHealthCheck("game_state", async ct =>
{
    var gameService = registry.Get<IDungeonGameService>();
    var isHealthy = gameService.IsRunning && !gameService.HasErrors;

    return new HealthCheckResult
    {
        Status = isHealthy ? HealthStatus.Healthy : HealthStatus.Degraded,
        Description = isHealthy ? "Game running normally" : "Game has errors",
        Data = { ["player_count"] = gameService.GetPlayerCount() }
    };
});
```

## Distributed Tracing Example

```csharp
// Trace a plugin operation
using (diagnostics.StartTrace("plugin_load", new Dictionary<string, object>
{
    ["plugin_id"] = "wingedbean.plugins.dungeongame",
    ["load_strategy"] = "eager"
}))
{
    diagnostics.AddTraceTag("plugin_version", "1.0.0");

    // Load plugin
    var plugin = await pluginLoader.LoadAsync(pluginPath);

    diagnostics.AddTraceEvent("plugin_activated", new Dictionary<string, object>
    {
        ["service_count"] = plugin.GetServices().Count()
    });

    // Trace automatically ends when disposed
}
```

## Performance Profiling Example

```csharp
// Measure operation performance
using (diagnostics.MeasurePerformance("level_generation", new Dictionary<string, object>
{
    ["level_size"] = "large",
    ["difficulty"] = "hard"
}))
{
    await GenerateLevelAsync();
}

// Get statistics
var stats = diagnostics.GetPerformanceStats("level_generation");
Console.WriteLine($"Avg: {stats.AverageDuration.TotalMilliseconds}ms");
Console.WriteLine($"P95: {stats.P95Duration.TotalMilliseconds}ms");
```

## Error Tracking Example

```csharp
try
{
    // Add breadcrumbs for context
    diagnostics.AddBreadcrumb("Loading player data", BreadcrumbLevel.Info, new Dictionary<string, object>
    {
        ["player_id"] = playerId
    });

    await LoadPlayerDataAsync(playerId);
}
catch (Exception ex)
{
    // Track error with full context
    await diagnostics.TrackErrorAsync(ex, ErrorSeverity.Error, new Dictionary<string, object>
    {
        ["player_id"] = playerId,
        ["operation"] = "load_player_data",
        ["trace_id"] = diagnostics.GetCurrentTraceId()
    });

    throw;
}
```

## Resource Monitoring Example

```csharp
// Start continuous monitoring
await diagnostics.StartResourceMonitoringAsync(TimeSpan.FromSeconds(5));

// Register alert for high memory usage
diagnostics.RegisterAlert("high_memory", ctx =>
{
    var metrics = diagnostics.GetSystemMetrics();
    return metrics.WorkingSetBytes > 500_000_000; // 500MB threshold
}, alert =>
{
    logger.LogWarning("High memory usage detected: {WorkingSet}MB",
        alert.Context["working_set_mb"]);
});
```

## Integration with Existing Services

### Analytics Service (RFC-0030)
Send diagnostic events to analytics:
```csharp
analytics.TrackEvent("diagnostic_alert", new Dictionary<string, object>
{
    ["alert_name"] = alert.Name,
    ["severity"] = alert.Severity.ToString()
});
```

### Resilience Service
Track resilience events as diagnostic breadcrumbs:
```csharp
resilience.ResilienceEventOccurred += (sender, e) =>
{
    diagnostics.AddBreadcrumb($"Resilience: {e.EventType}", BreadcrumbLevel.Warning, new Dictionary<string, object>
    {
        ["strategy"] = e.StrategyName,
        ["attempt"] = e.AttemptNumber
    });
};
```

### Logging
Correlate logs with trace IDs:
```csharp
logger.LogInformation("Processing request {TraceId}", diagnostics.GetCurrentTraceId());
```

## OpenTelemetry Integration

```csharp
// Export to OpenTelemetry collector
var tracerProvider = Sdk.CreateTracerProviderBuilder()
    .AddSource("WingedBean")
    .AddOtlpExporter(options =>
    {
        options.Endpoint = new Uri("http://localhost:4317");
    })
    .Build();

// Use Activity API for tracing
using var activity = new Activity("operation_name").Start();
activity.SetTag("plugin.id", "dungeongame");
```

## Testing Strategy

### Unit Tests
- Health check execution
- Performance statistics calculation
- Breadcrumb collection
- Alert triggering logic

### Integration Tests
- Application Insights export
- Sentry error tracking
- OpenTelemetry tracing
- Snapshot serialization

### Load Tests
- Resource monitoring under load
- Performance measurement overhead
- Alert storm prevention

## Metrics & Monitoring

### Service Metrics
- Health check execution count
- Trace span count
- Error tracking rate
- Alert trigger frequency
- Snapshot capture duration

### System Metrics
- CPU usage (avg, max)
- Memory usage (avg, max, GC pressure)
- Thread count (avg, max)
- Disk I/O (read/write bytes)

## Alternatives Considered

### Alternative 1: Use Application Insights Directly

**Rejected because:**
- Tight coupling to Azure
- Cannot switch to Sentry, Datadog, etc.
- Less control over diagnostic data
- No unified contract for testing

### Alternative 2: Custom Logging Only

**Rejected because:**
- Logs are not structured for diagnostics
- No health checks or tracing
- No performance profiling
- No resource monitoring

### Alternative 3: Third-party APM (New Relic, Datadog)

**Partially Adopted:**
- Use adapters for third-party APM tools
- Keep WingedBean contract as abstraction
- Allows multi-backend support

## Dependencies

- **System.Diagnostics.DiagnosticSource** - Activity API for tracing
- **Microsoft.ApplicationInsights** (optional) - Application Insights backend
- **Sentry** (optional) - Error tracking backend
- **OpenTelemetry** (optional) - OTLP export

## Security Considerations

1. **Sensitive Data**: Redact secrets/credentials from snapshots
2. **Snapshot Size**: Limit heap dump size to prevent DoS
3. **Export Access**: Secure diagnostic export endpoints
4. **PII Protection**: Scrub user data from error context
5. **Alert Rate Limiting**: Prevent alert spam

## Performance Considerations

1. **Low Overhead**: < 2% CPU for monitoring
2. **Async Operations**: All I/O is async
3. **Sampling**: Support trace sampling (e.g., 10%)
4. **Buffer Limits**: Cap breadcrumb/event buffers
5. **Background Processing**: Export on background thread

## Success Criteria

- ✅ Health check latency < 50ms (p95)
- ✅ Trace overhead < 1ms per span
- ✅ Memory overhead < 20MB
- ✅ Alert latency < 100ms
- ✅ Snapshot capture < 5 seconds
- ✅ Zero production crashes from diagnostics

## Questions

1. **Which APM backend should be default?**
   - **Recommendation**: Application Insights (Azure native)
   - **Alternative**: OpenTelemetry (vendor-neutral)

2. **Should diagnostic data be stored locally?**
   - **Recommendation**: Yes, SQLite for offline diagnostics

3. **How long to retain diagnostic data?**
   - **Recommendation**: 7 days (configurable)

4. **Should health checks run automatically?**
   - **Recommendation**: Yes, every 30 seconds (configurable)

## References

- [OpenTelemetry](https://opentelemetry.io/)
- [Application Insights](https://learn.microsoft.com/en-us/azure/azure-monitor/app/app-insights-overview)
- [Sentry Error Tracking](https://docs.sentry.io/)
- [.NET Diagnostics](https://learn.microsoft.com/en-us/dotnet/core/diagnostics/)
- [Activity API](https://learn.microsoft.com/en-us/dotnet/api/system.diagnostics.activity)

## Approval

- [ ] Architecture approved
- [ ] Performance benchmarks met
- [ ] Security review completed
- [ ] Integration plan validated

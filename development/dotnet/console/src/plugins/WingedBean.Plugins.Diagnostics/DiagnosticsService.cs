using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using WingedBean.Contracts.Diagnostics;

namespace WingedBean.Plugins.Diagnostics;

/// <summary>
/// Comprehensive diagnostics service implementation.
/// </summary>
public class DiagnosticsService : IDiagnosticsService
{
    private readonly ILogger<DiagnosticsService> _logger;
    private readonly DiagnosticsConfig _config;
    private readonly IDiagnosticsBackend _backend;
    private readonly object _lock = new();

    // Current state
    private bool _isEnabled = true;
    private readonly Dictionary<string, TraceSpan> _activeSpans = new();
    private readonly Dictionary<string, HealthCheckRegistration> _healthChecks = new();
    private readonly Dictionary<string, AlertRule> _alertRules = new();
    private readonly List<DiagnosticBreadcrumb> _breadcrumbs = new();
    private readonly List<Alert> _activeAlerts = new();

    // Sampling and performance tracking
    private readonly Random _random = new();
    private readonly Dictionary<string, ProfilingSession> _activeProfilings = new();

    public DiagnosticsService(
        ILogger<DiagnosticsService> logger,
        DiagnosticsConfig config,
        IDiagnosticsBackend backend)
    {
        _logger = logger;
        _config = config;
        _backend = backend;
        _isEnabled = config.Enabled;

        _logger.LogInformation("Diagnostics service initialized with backend: {Backend}",
            config.Backend);
    }

    public bool IsEnabled => _isEnabled;

    public void SetEnabled(bool enabled)
    {
        lock (_lock)
        {
            _isEnabled = enabled;
            _logger.LogInformation("Diagnostics {State}", enabled ? "enabled" : "disabled");
        }
    }

    // Distributed Tracing

    public IDisposable StartSpan(string operationName, SpanKind kind = SpanKind.Internal, Dictionary<string, object>? tags = null)
    {
        if (!CheckEnabled()) return new NoOpDisposable();

        var spanId = Guid.NewGuid().ToString("N");
        var traceId = GetCurrentTraceId() ?? Guid.NewGuid().ToString("N");

        var span = new TraceSpan
        {
            TraceId = traceId,
            SpanId = spanId,
            ParentSpanId = GetCurrentSpanId(),
            OperationName = operationName,
            Kind = kind,
            StartTime = DateTimeOffset.UtcNow,
            Tags = tags ?? new Dictionary<string, object>()
        };

        lock (_lock)
        {
            _activeSpans[spanId] = span;
        }

        _backend.StartSpan(span);

        return new SpanDisposable(spanId, this);
    }

    public IDisposable StartSpan(string operationName, IDisposable parentSpan, SpanKind kind = SpanKind.Internal, Dictionary<string, object>? tags = null)
    {
        // For simplicity, just create a new span - in a real implementation,
        // we'd extract the parent span ID from the parentSpan
        return StartSpan(operationName, kind, tags);
    }

    public string? GetCurrentTraceId()
    {
        // In a real implementation, this would use AsyncLocal or similar
        // For now, return the most recent active span's trace ID
        lock (_lock)
        {
            return _activeSpans.Values
                .OrderByDescending(s => s.StartTime)
                .FirstOrDefault()?.TraceId;
        }
    }

    public string? GetCurrentSpanId()
    {
        // In a real implementation, this would use AsyncLocal or similar
        // For now, return the most recent active span's ID
        lock (_lock)
        {
            return _activeSpans.Keys
                .OrderByDescending(k => _activeSpans[k].StartTime)
                .FirstOrDefault();
        }
    }

    public void SetSpanTags(Dictionary<string, object> tags)
    {
        if (!CheckEnabled()) return;

        var spanId = GetCurrentSpanId();
        if (spanId == null) return;

        lock (_lock)
        {
            if (_activeSpans.TryGetValue(spanId, out var span))
            {
                foreach (var kvp in tags)
                {
                    span.Tags[kvp.Key] = kvp.Value;
                }
                _backend.SetSpanTags(spanId, tags);
            }
        }
    }

    public void SetSpanError(string error, Dictionary<string, object>? tags = null)
    {
        if (!CheckEnabled()) return;

        var spanId = GetCurrentSpanId();
        if (spanId == null) return;

        lock (_lock)
        {
            if (_activeSpans.TryGetValue(spanId, out var span))
            {
                span.IsError = true;
                span.ErrorMessage = error;
                if (tags != null)
                {
                    foreach (var kvp in tags)
                    {
                        span.Tags[kvp.Key] = kvp.Value;
                    }
                }
                _backend.SetSpanError(spanId, error, tags);
            }
        }
    }

    private void EndSpan(string spanId)
    {
        TraceSpan span;
        lock (_lock)
        {
            if (!_activeSpans.TryGetValue(spanId, out span))
                return;

            span.EndTime = DateTimeOffset.UtcNow;
            _activeSpans.Remove(spanId);
        }

        _backend.EndSpan(span);
    }

    // Breadcrumbs and Context

    public void AddBreadcrumb(string message, string category = "default", BreadcrumbLevel level = BreadcrumbLevel.Info, Dictionary<string, object>? data = null)
    {
        if (!CheckEnabled()) return;

        var breadcrumb = new DiagnosticBreadcrumb
        {
            Message = message,
            Category = category,
            Level = level,
            Data = data ?? new Dictionary<string, object>(),
            Timestamp = DateTimeOffset.UtcNow
        };

        lock (_lock)
        {
            _breadcrumbs.Add(breadcrumb);

            // Keep breadcrumbs within limit
            while (_breadcrumbs.Count > _config.MaxBreadcrumbs)
            {
                _breadcrumbs.RemoveAt(0);
            }
        }

        _backend.AddBreadcrumb(breadcrumb);
    }

    public void AddBreadcrumb(Exception exception, string category = "error")
    {
        AddBreadcrumb(exception.Message, category, BreadcrumbLevel.Error,
            new Dictionary<string, object>
            {
                ["exceptionType"] = exception.GetType().FullName ?? "Unknown",
                ["stackTrace"] = exception.StackTrace ?? ""
            });
    }

    public void ClearBreadcrumbs()
    {
        lock (_lock)
        {
            _breadcrumbs.Clear();
        }
        _backend.ClearBreadcrumbs();
    }

    public IEnumerable<DiagnosticBreadcrumb> GetBreadcrumbs()
    {
        lock (_lock)
        {
            return _breadcrumbs.ToList();
        }
    }

    // Error Tracking

    public void RecordException(Exception exception, Dictionary<string, object>? tags = null)
    {
        if (!CheckEnabled()) return;

        var contextTags = tags ?? new Dictionary<string, object>();
        contextTags["exceptionType"] = exception.GetType().FullName ?? "Unknown";
        contextTags["message"] = exception.Message;
        contextTags["stackTrace"] = exception.StackTrace ?? "";
        contextTags["source"] = exception.Source ?? "";

        _backend.RecordException(exception, contextTags);

        // Add breadcrumb for the exception
        AddBreadcrumb(exception, "exception");

        _logger.LogError(exception, "Exception recorded");
    }

    public void RecordError(string message, string? type = null, Dictionary<string, object>? tags = null)
    {
        if (!CheckEnabled()) return;

        var contextTags = tags ?? new Dictionary<string, object>();
        if (type != null) contextTags["errorType"] = type;

        _backend.RecordError(message, contextTags);

        // Add breadcrumb for the error
        AddBreadcrumb(message, "error", BreadcrumbLevel.Error, contextTags);

        _logger.LogError("Error recorded: {Message}", message);
    }

    public void RecordHandledException(Exception exception, Dictionary<string, object>? context = null)
    {
        if (!CheckEnabled()) return;

        var contextTags = context ?? new Dictionary<string, object>();
        contextTags["handled"] = true;

        RecordException(exception, contextTags);
    }

    // Performance Profiling

    public void RecordMetric(string name, double value, string unit = "ms", Dictionary<string, object>? tags = null)
    {
        if (!CheckEnabled()) return;

        _backend.RecordMetric(name, value, unit, tags ?? new Dictionary<string, object>());
    }

    public void RecordCounter(string name, long increment = 1, Dictionary<string, object>? tags = null)
    {
        if (!CheckEnabled()) return;

        _backend.RecordCounter(name, increment, tags ?? new Dictionary<string, object>());
    }

    public void RecordGauge(string name, double value, Dictionary<string, object>? tags = null)
    {
        if (!CheckEnabled()) return;

        _backend.RecordGauge(name, value, tags ?? new Dictionary<string, object>());
    }

    public void RecordHistogram(string name, double value, Dictionary<string, object>? tags = null)
    {
        if (!CheckEnabled()) return;

        _backend.RecordHistogram(name, value, tags ?? new Dictionary<string, object>());
    }

    public IOperationProfiler StartProfiling(string operationName)
    {
        if (!CheckEnabled()) return new NoOpProfiler();

        var session = new ProfilingSession(operationName);
        lock (_lock)
        {
            _activeProfilings[operationName] = session;
        }

        _backend.StartProfiling(operationName);
        return new OperationProfiler(operationName, this);
    }

    public IOperationProfiler StartProfiling(string operationName, ProfilingConfig config)
    {
        // For simplicity, ignore config for now
        return StartProfiling(operationName);
    }

    public void RecordTiming(string operationName, TimeSpan duration, Dictionary<string, object>? tags = null)
    {
        if (!CheckEnabled()) return;

        RecordMetric(operationName, duration.TotalMilliseconds, "ms", tags);
    }

    private ProfilingResult EndProfiling(string operationName)
    {
        ProfilingSession session;
        lock (_lock)
        {
            if (!_activeProfilings.TryGetValue(operationName, out session))
            {
                session = new ProfilingSession(operationName);
            }
            else
            {
                _activeProfilings.Remove(operationName);
            }
        }

        session.EndTime = DateTimeOffset.UtcNow;
        _backend.EndProfiling(operationName, session);
        return session.ToResult();
    }

    // Health Checks

    public void RegisterHealthCheck(string name, Func<Task<HealthCheckResult>> check, Dictionary<string, object>? tags = null)
    {
        var registration = new HealthCheckRegistration
        {
            Name = name,
            Check = check,
            Tags = tags ?? new Dictionary<string, object>(),
            IsAsync = true
        };

        lock (_lock)
        {
            _healthChecks[name] = registration;
        }

        _backend.RegisterHealthCheck(name, registration);
    }

    public void RegisterHealthCheck(string name, Func<HealthCheckResult> check, Dictionary<string, object>? tags = null)
    {
        var registration = new HealthCheckRegistration
        {
            Name = name,
            SyncCheck = check,
            Tags = tags ?? new Dictionary<string, object>(),
            IsAsync = false
        };

        lock (_lock)
        {
            _healthChecks[name] = registration;
        }

        _backend.RegisterHealthCheck(name, registration);
    }

    public void RegisterHealthCheck(string name, HealthCheckConfig config)
    {
        // For simplicity, create a wrapper
        if (config.Check != null)
        {
            RegisterHealthCheck(name, config.Check, config.Tags);
        }
        else if (config.SyncCheck != null)
        {
            RegisterHealthCheck(name, config.SyncCheck, config.Tags);
        }
    }

    public void UnregisterHealthCheck(string name)
    {
        lock (_lock)
        {
            _healthChecks.Remove(name);
        }
        _backend.UnregisterHealthCheck(name);
    }

    public async Task<Dictionary<string, HealthCheckResult>> RunHealthChecksAsync()
    {
        var results = new Dictionary<string, HealthCheckResult>();

        List<HealthCheckRegistration> checksToRun;
        lock (_lock)
        {
            checksToRun = _healthChecks.Values.ToList();
        }

        foreach (var registration in checksToRun)
        {
            try
            {
                var result = registration.IsAsync
                    ? await registration.Check()
                    : registration.SyncCheck();

                results[registration.Name] = result;
                _backend.RecordHealthCheckResult(registration.Name, result);
            }
            catch (Exception ex)
            {
                var errorResult = new HealthCheckResult
                {
                    Status = HealthStatus.Unhealthy,
                    Description = $"Health check failed: {ex.Message}",
                    Duration = TimeSpan.Zero,
                    Exception = ex,
                    Timestamp = DateTimeOffset.UtcNow
                };

                results[registration.Name] = errorResult;
                _backend.RecordHealthCheckResult(registration.Name, errorResult);
            }
        }

        return results;
    }

    public async Task<HealthCheckResult> RunHealthCheckAsync(string name)
    {
        HealthCheckRegistration registration;
        lock (_lock)
        {
            if (!_healthChecks.TryGetValue(name, out registration))
            {
                return new HealthCheckResult
                {
                    Status = HealthStatus.Unhealthy,
                    Description = $"Health check '{name}' not found",
                    Duration = TimeSpan.Zero,
                    Timestamp = DateTimeOffset.UtcNow
                };
            }
        }

        try
        {
            var result = registration.IsAsync
                ? await registration.Check()
                : registration.SyncCheck();

            _backend.RecordHealthCheckResult(name, result);
            return result;
        }
        catch (Exception ex)
        {
            var errorResult = new HealthCheckResult
            {
                Status = HealthStatus.Unhealthy,
                Description = $"Health check failed: {ex.Message}",
                Duration = TimeSpan.Zero,
                Exception = ex,
                Timestamp = DateTimeOffset.UtcNow
            };

            _backend.RecordHealthCheckResult(name, errorResult);
            return errorResult;
        }
    }

    public async Task<HealthStatus> GetHealthStatusAsync()
    {
        var results = await RunHealthChecksAsync();

        if (results.Values.Any(r => r.Status == HealthStatus.Unhealthy))
            return HealthStatus.Unhealthy;

        if (results.Values.Any(r => r.Status == HealthStatus.Degraded))
            return HealthStatus.Degraded;

        return results.Any() ? HealthStatus.Healthy : HealthStatus.Unknown;
    }

    public async Task<Dictionary<string, HealthCheckResult>> GetHealthCheckResultsAsync(Dictionary<string, object>? tags = null)
    {
        var allResults = await RunHealthChecksAsync();

        if (tags == null || !tags.Any())
            return allResults;

        // Filter by tags
        return allResults.Where(kvp =>
        {
            lock (_lock)
            {
                return _healthChecks.TryGetValue(kvp.Key, out var registration) &&
                       tags.All(tag => registration.Tags.Contains(tag));
            }
        }).ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
    }

    // System Monitoring

    public SystemMetrics GetSystemMetrics()
    {
        var process = Process.GetCurrentProcess();

        return new SystemMetrics
        {
            CpuUsagePercent = 0, // Would need platform-specific implementation
            MemoryUsageBytes = process.WorkingSet64,
            TotalMemoryBytes = process.VirtualMemorySize64,
            AvailableMemoryBytes = 0, // Would need platform-specific implementation
            ThreadCount = process.Threads.Count,
            ActiveThreadPoolThreads = 0, // Would need ThreadPool.GetAvailableThreads()
            ProcessId = process.Id,
            ProcessStartTime = process.StartTime,
            Timestamp = DateTimeOffset.UtcNow
        };
    }

    public IEnumerable<SystemMetrics> GetSystemMetricsHistory(TimeSpan duration)
    {
        // For simplicity, just return current metrics
        // In a real implementation, this would return historical data
        yield return GetSystemMetrics();
    }

    public void RecordSystemMetrics(Dictionary<string, object> metrics)
    {
        if (!CheckEnabled()) return;

        _backend.RecordSystemMetrics(metrics);
    }

    // Diagnostic Snapshots

    public async Task<DiagnosticSnapshot> CaptureSnapshotAsync(bool includeHeapDump = false, bool includeThreadDump = true)
    {
        var snapshot = new DiagnosticSnapshot
        {
            Id = Guid.NewGuid().ToString(),
            Timestamp = DateTimeOffset.UtcNow,
            SystemMetrics = GetSystemMetrics(),
            Breadcrumbs = GetBreadcrumbs().ToList()
        };

        // Get active spans
        lock (_lock)
        {
            snapshot.ActiveSpans = _activeSpans.Values.ToList();
        }

        // Get health checks
        snapshot.HealthChecks = await RunHealthChecksAsync();

        // Thread dump if requested
        if (includeThreadDump)
        {
            snapshot.ThreadDump = new ThreadDump
            {
                Threads = Process.GetCurrentProcess().Threads
                    .Cast<ProcessThread>()
                    .Select(t => new ThreadInfo
                    {
                        ThreadId = t.Id,
                        Name = t.ThreadState.ToString(), // Simplified
                        State = ThreadState.Running, // Simplified
                        IsBackground = false, // Simplified
                        Priority = ThreadPriority.Normal, // Simplified
                        IsAlive = true
                    }).ToList(),
                Timestamp = DateTimeOffset.UtcNow
            };
        }

        // Heap dump if requested (simplified)
        if (includeHeapDump)
        {
            snapshot.HeapDump = new HeapDump
            {
                TotalHeapSizeBytes = GC.GetTotalMemory(false),
                Timestamp = DateTimeOffset.UtcNow
            };
        }

        _backend.RecordSnapshot(snapshot);
        return snapshot;
    }

    public async Task<DiagnosticSnapshot> CaptureSnapshotAsync(SnapshotConfig config)
    {
        return await CaptureSnapshotAsync(config.IncludeHeapDump, config.IncludeThreadDump);
    }

    public IEnumerable<DiagnosticSnapshot> GetRecentSnapshots(int count = 10)
    {
        // In a real implementation, this would return stored snapshots
        return Enumerable.Empty<DiagnosticSnapshot>();
    }

    // Alerting System

    public void SetupAlert(AlertRule rule)
    {
        lock (_lock)
        {
            _alertRules[rule.Name] = rule;
        }
        _backend.SetupAlert(rule);
        _logger.LogInformation("Alert rule setup: {RuleName}", rule.Name);
    }

    public void UpdateAlert(string ruleName, AlertRule rule)
    {
        lock (_lock)
        {
            _alertRules[ruleName] = rule;
        }
        _backend.UpdateAlert(ruleName, rule);
    }

    public void RemoveAlert(string ruleName)
    {
        lock (_lock)
        {
            _alertRules.Remove(ruleName);
        }
        _backend.RemoveAlert(ruleName);
        _logger.LogInformation("Alert rule removed: {RuleName}", ruleName);
    }

    public void SetAlertEnabled(string ruleName, bool enabled)
    {
        lock (_lock)
        {
            if (_alertRules.TryGetValue(ruleName, out var rule))
            {
                rule.Enabled = enabled;
                _backend.UpdateAlert(ruleName, rule);
            }
        }
    }

    public IEnumerable<Alert> GetActiveAlerts()
    {
        lock (_lock)
        {
            return _activeAlerts.Where(a => a.ResolvedAt == null).ToList();
        }
    }

    public IEnumerable<AlertRule> GetAlertRules()
    {
        lock (_lock)
        {
            return _alertRules.Values.ToList();
        }
    }

    public void AcknowledgeAlert(string alertId, string user, string? comment = null)
    {
        lock (_lock)
        {
            var alert = _activeAlerts.FirstOrDefault(a => a.Id == alertId);
            if (alert != null)
            {
                alert.AcknowledgedBy = user;
                alert.AcknowledgmentComment = comment;
                alert.AcknowledgedAt = DateTimeOffset.UtcNow;
                _backend.AcknowledgeAlert(alertId, user, comment);
            }
        }
    }

    // Data Management

    public async Task FlushAsync()
    {
        await _backend.FlushAsync();
        _logger.LogDebug("Diagnostics flushed");
    }

    public async Task<byte[]> ExportDataAsync(string format, DateTimeOffset startTime, DateTimeOffset endTime)
    {
        return await _backend.ExportDataAsync(format, startTime, endTime);
    }

    public async Task<long> ClearOldDataAsync(TimeSpan retentionPeriod)
    {
        return await _backend.ClearOldDataAsync(retentionPeriod);
    }

    // Configuration & Management

    public DiagnosticsConfig GetConfig()
    {
        return _config;
    }

    public void UpdateConfig(DiagnosticsConfig config)
    {
        // Note: In a real implementation, this might restart the service
        _logger.LogWarning("Config update requested but not implemented");
    }

    public DiagnosticsStats GetStats()
    {
        lock (_lock)
        {
            return new DiagnosticsStats
            {
                TotalSpans = 0, // Would track in backend
                ActiveSpans = _activeSpans.Count,
                ErrorCount = 0, // Would track in backend
                BreadcrumbCount = _breadcrumbs.Count,
                HealthCheckCount = _healthChecks.Count,
                AlertRuleCount = _alertRules.Count,
                ActiveAlertCount = _activeAlerts.Count,
                SnapshotCount = 0, // Would track in backend
                MetricCount = 0, // Would track in backend
                Uptime = TimeSpan.Zero, // Would calculate from service start
                LastFlush = null
            };
        }
    }

    public void ResetStats()
    {
        // Would reset counters in backend
        _logger.LogInformation("Diagnostics stats reset");
    }

    public DiagnosticsStatus GetStatus()
    {
        return new DiagnosticsStatus
        {
            IsEnabled = _isEnabled,
            Backend = _config.Backend,
            BackendHealthy = true, // Would check backend health
            HealthStatus = HealthStatus.Healthy, // Would aggregate health
            PendingOperations = 0,
            MemoryUsageBytes = GetSystemMetrics().MemoryUsageBytes,
            LastUpdate = DateTimeOffset.UtcNow
        };
    }

    private bool CheckEnabled()
    {
        if (!_isEnabled)
        {
            _logger.LogTrace("Diagnostics disabled - skipping operation");
            return false;
        }
        return true;
    }

    // Helper classes

    private class SpanDisposable : IDisposable
    {
        private readonly string _spanId;
        private readonly DiagnosticsService _service;

        public SpanDisposable(string spanId, DiagnosticsService service)
        {
            _spanId = spanId;
            _service = service;
        }

        public void Dispose()
        {
            _service.EndSpan(_spanId);
        }
    }

    private class NoOpDisposable : IDisposable
    {
        public void Dispose() { }
    }

    private class NoOpProfiler : IOperationProfiler
    {
        public void AddStep(string name, TimeSpan duration, Dictionary<string, object>? data = null) { }
        public void Dispose() { }
    }

    private class OperationProfiler : IOperationProfiler
    {
        private readonly string _operationName;
        private readonly DiagnosticsService _service;

        public OperationProfiler(string operationName, DiagnosticsService service)
        {
            _operationName = operationName;
            _service = service;
        }

        public void AddStep(string name, TimeSpan duration, Dictionary<string, object>? data = null)
        {
            _service.RecordTiming($"{_operationName}.{name}", duration, data);
        }

        public void Dispose()
        {
            _service.EndProfiling(_operationName);
        }
    }
}

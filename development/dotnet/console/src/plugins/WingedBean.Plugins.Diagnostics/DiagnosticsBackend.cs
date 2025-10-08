using Plate.CrossMilo.Contracts.Diagnostics;
using System.Collections.Generic;
using System.Threading.Tasks;
using Sentry;
using Sentry.Protocol;
using Microsoft.Extensions.Logging;
using System;
using BreadcrumbLevel = Plate.CrossMilo.Contracts.Diagnostics.BreadcrumbLevel;

namespace WingedBean.Plugins.Diagnostics;

/// <summary>
/// Interface for diagnostics backends.
/// </summary>
public interface IDiagnosticsBackend
{
    Task StartSpan(TraceSpan span);
    Task SetSpanTags(string spanId, Dictionary<string, object> tags);
    Task SetSpanError(string spanId, string error, Dictionary<string, object>? tags = null);
    Task EndSpan(TraceSpan span);

    Task AddBreadcrumb(DiagnosticBreadcrumb breadcrumb);
    Task ClearBreadcrumbs();

    Task RecordException(Exception exception, Dictionary<string, object> context);
    Task RecordError(string message, Dictionary<string, object> context);

    Task RecordMetric(string name, double value, string unit, Dictionary<string, object> tags);
    Task RecordCounter(string name, long increment, Dictionary<string, object> tags);
    Task RecordGauge(string name, double value, Dictionary<string, object> tags);
    Task RecordHistogram(string name, double value, Dictionary<string, object> tags);

    Task StartProfiling(string operationName);
    Task EndProfiling(string operationName, ProfilingSession session);

    Task RegisterHealthCheck(string name, HealthCheckRegistration registration);
    Task UnregisterHealthCheck(string name);
    Task RecordHealthCheckResult(string name, HealthCheckResult result);

    Task RecordSystemMetrics(Dictionary<string, object> metrics);
    Task RecordSnapshot(DiagnosticSnapshot snapshot);

    Task SetupAlert(AlertRule rule);
    Task UpdateAlert(string ruleName, AlertRule rule);
    Task RemoveAlert(string ruleName);
    Task AcknowledgeAlert(string alertId, string user, string? comment = null);

    Task FlushAsync();
    Task<byte[]> ExportDataAsync(string format, DateTimeOffset startTime, DateTimeOffset endTime);
    Task<long> ClearOldDataAsync(TimeSpan retentionPeriod);
}

#if FALSE // TODO: Fix Sentry SDK integration issues - type conversion errors with BreadcrumbLevel, ActivityTagsCollection, etc.
/// <summary>
/// Sentry diagnostics backend implementation.
/// </summary>
public class SentryDiagnosticsBackend : IDiagnosticsBackend
{
    private readonly ILogger<SentryDiagnosticsBackend> _logger;
    private readonly Dictionary<string, ISpan> _activeSpans = new();
    private readonly List<DiagnosticBreadcrumb> _breadcrumbs = new();

    public SentryDiagnosticsBackend(ILogger<SentryDiagnosticsBackend> logger, string dsn, string? environment = null)
    {
        _logger = logger;

        try
        {
            SentrySdk.Init(options =>
            {
                options.Dsn = dsn;
                options.Environment = environment ?? "production";
                options.Debug = false; // Set to true for debugging
                options.TracesSampleRate = 1.0; // Capture 100% of transactions
                options.ProfilesSampleRate = 1.0; // Capture 100% of profiles
                options.DiagnosticLevel = SentryLevel.Info;

                // Configure integrations
                options.AddIntegration(new ProfilingIntegration());
            });

            _logger.LogInformation("Sentry Diagnostics backend initialized with DSN: {Dsn}", dsn);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize Sentry Diagnostics backend");
            throw;
        }
    }

    public async Task StartSpan(TraceSpan span)
    {
        try
        {
            var sentrySpan = SentrySdk.StartTransaction(span.OperationName, span.OperationName);
            sentrySpan.SetTag("span_id", span.SpanId);
            sentrySpan.SetTag("parent_span_id", span.ParentSpanId);

            foreach (var tag in span.Tags)
            {
                sentrySpan.SetTag(tag.Key, tag.Value?.ToString() ?? string.Empty);
            }

            _activeSpans[span.SpanId] = sentrySpan;

            _logger.LogDebug("Started Sentry span: {SpanId} - {OperationName}", span.SpanId, span.OperationName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to start Sentry span: {SpanId}", span.SpanId);
        }
    }

    public async Task SetSpanTags(string spanId, Dictionary<string, object> tags)
    {
        try
        {
            if (_activeSpans.TryGetValue(spanId, out var sentrySpan))
            {
                foreach (var tag in tags)
                {
                    sentrySpan.SetTag(tag.Key, tag.Value?.ToString() ?? string.Empty);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to set Sentry span tags for span: {SpanId}", spanId);
        }
    }

    public async Task SetSpanError(string spanId, string error, Dictionary<string, object>? tags = null)
    {
        try
        {
            if (_activeSpans.TryGetValue(spanId, out var sentrySpan))
            {
                sentrySpan.Status = SpanStatus.InternalError;

                var sentryException = new SentryException
                {
                    Value = error,
                    Type = "SpanError"
                };

                if (tags != null)
                {
                    foreach (var tag in tags)
                    {
                        sentrySpan.SetTag($"error_{tag.Key}", tag.Value?.ToString() ?? string.Empty);
                    }
                }

                SentrySdk.CaptureException(new Exception(error), scope =>
                {
                    scope.Contexts.Trace.SpanId = sentrySpan.SpanId;
                    scope.Contexts.Trace.TraceId = sentrySpan.TraceId;
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to set Sentry span error for span: {SpanId}", spanId);
        }
    }

    public async Task EndSpan(TraceSpan span)
    {
        try
        {
            if (_activeSpans.TryGetValue(span.SpanId, out var sentrySpan))
            {
                sentrySpan.Finish();
                _activeSpans.Remove(span.SpanId);
            }

            _logger.LogDebug("Ended Sentry span: {SpanId}", span.SpanId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to end Sentry span: {SpanId}", span.SpanId);
        }
    }

    public async Task AddBreadcrumb(DiagnosticBreadcrumb breadcrumb)
    {
        try
        {
            _breadcrumbs.Add(breadcrumb);

            var sentryBreadcrumb = new Breadcrumb(
                message: breadcrumb.Message,
                type: breadcrumb.Category,
                level: breadcrumb.Level switch
                {
                    BreadcrumbLevel.Debug => SentryLevel.Debug,
                    BreadcrumbLevel.Info => SentryLevel.Info,
                    BreadcrumbLevel.Warning => SentryLevel.Warning,
                    BreadcrumbLevel.Error => SentryLevel.Error,
                    BreadcrumbLevel.Fatal => SentryLevel.Fatal,
                    _ => SentryLevel.Info
                },
                category: breadcrumb.Category,
                data: breadcrumb.Data
            );

            SentrySdk.AddBreadcrumb(sentryBreadcrumb);

            _logger.LogDebug("Added Sentry breadcrumb: {Message}", breadcrumb.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to add Sentry breadcrumb");
        }
    }

    public async Task ClearBreadcrumbs()
    {
        try
        {
            _breadcrumbs.Clear();
            // Note: Sentry doesn't provide a way to clear breadcrumbs from the current scope
            _logger.LogDebug("Cleared local breadcrumbs");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to clear breadcrumbs");
        }
    }

    public async Task RecordException(Exception exception, Dictionary<string, object> context)
    {
        try
        {
            SentrySdk.CaptureException(exception, scope =>
            {
                foreach (var kvp in context)
                {
                    scope.SetTag(kvp.Key, kvp.Value?.ToString() ?? string.Empty);
                }
            });

            _logger.LogDebug("Recorded Sentry exception: {ExceptionType}", exception.GetType().Name);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to record Sentry exception");
        }
    }

    public async Task RecordError(string message, Dictionary<string, object> context)
    {
        try
        {
            var exception = new Exception(message);
            await RecordException(exception, context);

            _logger.LogDebug("Recorded Sentry error: {Message}", message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to record Sentry error");
        }
    }

    public async Task RecordMetric(string name, double value, string unit, Dictionary<string, object> tags)
    {
        try
        {
            // Sentry doesn't have built-in metrics, but we can capture as custom events
            SentrySdk.CaptureMessage($"Metric: {name} = {value} {unit}", SentryLevel.Info, scope =>
            {
                scope.SetTag("metric_name", name);
                scope.SetTag("metric_value", value.ToString());
                scope.SetTag("metric_unit", unit);

                foreach (var tag in tags)
                {
                    scope.SetTag($"metric_{tag.Key}", tag.Value?.ToString() ?? string.Empty);
                }
            });

            _logger.LogDebug("Recorded Sentry metric: {Name} = {Value} {Unit}", name, value, unit);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to record Sentry metric: {Name}", name);
        }
    }

    public async Task RecordCounter(string name, long increment, Dictionary<string, object> tags)
    {
        await RecordMetric(name, increment, "count", tags);
    }

    public async Task RecordGauge(string name, double value, Dictionary<string, object> tags)
    {
        await RecordMetric(name, value, "gauge", tags);
    }

    public async Task RecordHistogram(string name, double value, Dictionary<string, object> tags)
    {
        await RecordMetric(name, value, "histogram", tags);
    }

    public async Task StartProfiling(string operationName)
    {
        try
        {
            // Sentry profiling is automatically handled by the SDK
            _logger.LogDebug("Started Sentry profiling for: {OperationName}", operationName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to start Sentry profiling");
        }
    }

    public async Task EndProfiling(string operationName, ProfilingSession session)
    {
        try
        {
            // Profiling data is automatically sent by Sentry SDK
            _logger.LogDebug("Ended Sentry profiling for: {OperationName}", operationName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to end Sentry profiling");
        }
    }

    public async Task RegisterHealthCheck(string name, HealthCheckRegistration registration)
    {
        try
        {
            // Health checks could be implemented as custom Sentry events or metrics
            SentrySdk.CaptureMessage($"Health check registered: {name}", SentryLevel.Info);
            _logger.LogDebug("Registered Sentry health check: {Name}", name);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to register Sentry health check: {Name}", name);
        }
    }

    public async Task UnregisterHealthCheck(string name)
    {
        try
        {
            SentrySdk.CaptureMessage($"Health check unregistered: {name}", SentryLevel.Info);
            _logger.LogDebug("Unregistered Sentry health check: {Name}", name);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to unregister Sentry health check: {Name}", name);
        }
    }

    public async Task RecordHealthCheckResult(string name, HealthCheckResult result)
    {
        try
        {
            var level = result.Status switch
            {
                HealthStatus.Healthy => SentryLevel.Info,
                HealthStatus.Degraded => SentryLevel.Warning,
                HealthStatus.Unhealthy => SentryLevel.Error,
                _ => SentryLevel.Info
            };

            SentrySdk.CaptureMessage($"Health check {name}: {result.Status}", level, scope =>
            {
                scope.SetTag("health_check_name", name);
                scope.SetTag("health_check_status", result.Status.ToString());
                scope.SetTag("health_check_duration", result.Duration.TotalMilliseconds.ToString());
                scope.SetTag("health_check_description", result.Description);
            });

            _logger.LogDebug("Recorded Sentry health check result: {Name} - {Status}", name, result.Status);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to record Sentry health check result: {Name}", name);
        }
    }

    public async Task RecordSystemMetrics(Dictionary<string, object> metrics)
    {
        try
        {
            foreach (var metric in metrics)
            {
                SentrySdk.CaptureMessage($"System metric: {metric.Key} = {metric.Value}", SentryLevel.Info, scope =>
                {
                    scope.SetTag("system_metric", metric.Key);
                    scope.SetTag("system_metric_value", metric.Value?.ToString() ?? string.Empty);
                });
            }

            _logger.LogDebug("Recorded Sentry system metrics: {Count} metrics", metrics.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to record Sentry system metrics");
        }
    }

    public async Task RecordSnapshot(DiagnosticSnapshot snapshot)
    {
        try
        {
            SentrySdk.CaptureMessage($"Diagnostic snapshot captured", SentryLevel.Info, scope =>
            {
                scope.SetTag("snapshot_timestamp", snapshot.Timestamp.ToString());
                scope.SetTag("snapshot_breadcrumbs", snapshot.Breadcrumbs.Count.ToString());
                scope.SetTag("snapshot_spans", snapshot.ActiveSpans.Count.ToString());
                scope.SetTag("snapshot_health_checks", snapshot.HealthChecks.Count.ToString());
            });

            _logger.LogDebug("Recorded Sentry diagnostic snapshot");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to record Sentry diagnostic snapshot");
        }
    }

    public async Task SetupAlert(AlertRule rule)
    {
        try
        {
            SentrySdk.CaptureMessage($"Alert rule setup: {rule.Name}", SentryLevel.Info, scope =>
            {
                scope.SetTag("alert_rule_name", rule.Name);
                scope.SetTag("alert_rule_metric", rule.MetricName);
                scope.SetTag("alert_rule_threshold", rule.Threshold.ToString());
                scope.SetTag("alert_rule_severity", rule.Severity.ToString());
            });

            _logger.LogDebug("Setup Sentry alert rule: {Name}", rule.Name);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to setup Sentry alert rule: {Name}", rule.Name);
        }
    }

    public async Task UpdateAlert(string ruleName, AlertRule rule)
    {
        await SetupAlert(rule); // Reuse the same logic
    }

    public async Task RemoveAlert(string ruleName)
    {
        try
        {
            SentrySdk.CaptureMessage($"Alert rule removed: {ruleName}", SentryLevel.Info, scope =>
            {
                scope.SetTag("alert_rule_name", ruleName);
            });

            _logger.LogDebug("Removed Sentry alert rule: {Name}", ruleName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to remove Sentry alert rule: {Name}", ruleName);
        }
    }

    public async Task AcknowledgeAlert(string alertId, string user, string? comment = null)
    {
        try
        {
            SentrySdk.CaptureMessage($"Alert acknowledged: {alertId}", SentryLevel.Info, scope =>
            {
                scope.SetTag("alert_id", alertId);
                scope.SetTag("alert_acknowledged_by", user);
                if (comment != null)
                {
                    scope.SetTag("alert_acknowledgement_comment", comment);
                }
            });

            _logger.LogDebug("Acknowledged Sentry alert: {AlertId} by {User}", alertId, user);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to acknowledge Sentry alert: {AlertId}", alertId);
        }
    }

    public async Task FlushAsync()
    {
        try
        {
            await SentrySdk.FlushAsync(TimeSpan.FromSeconds(5));
            _logger.LogDebug("Flushed Sentry data");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to flush Sentry data");
        }
    }

    public async Task<byte[]> ExportDataAsync(string format, DateTimeOffset startTime, DateTimeOffset endTime)
    {
        try
        {
            // Sentry doesn't provide direct data export, so we'll return a summary
            var summary = new
            {
                Provider = "Sentry",
                TimeRange = $"{startTime} to {endTime}",
                Breadcrumbs = _breadcrumbs.Count,
                ActiveSpans = _activeSpans.Count,
                Note = "Data is stored in Sentry, use Sentry dashboard for detailed export"
            };

            return System.Text.Json.JsonSerializer.SerializeToUtf8Bytes(summary);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to export Sentry data");
            return System.Text.Json.JsonSerializer.SerializeToUtf8Bytes(new { Error = ex.Message });
        }
    }

    public async Task<long> ClearOldDataAsync(TimeSpan retentionPeriod)
    {
        try
        {
            // Sentry manages its own data retention, we can't clear it programmatically
            _logger.LogInformation("Sentry data retention is managed by Sentry, not cleared locally");
            return 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to clear old Sentry data");
            return 0;
        }
    }
}

/// <summary>
/// OpenTelemetry diagnostics backend implementation.
/// </summary>
public class OpenTelemetryDiagnosticsBackend : IDiagnosticsBackend
{
    private readonly ILogger<OpenTelemetryDiagnosticsBackend> _logger;
    private readonly System.Diagnostics.ActivitySource _activitySource;
    private readonly Dictionary<string, System.Diagnostics.Activity> _activeActivities = new();
    private readonly List<DiagnosticBreadcrumb> _breadcrumbs = new();

    public OpenTelemetryDiagnosticsBackend(ILogger<OpenTelemetryDiagnosticsBackend> logger, string serviceName, string serviceVersion = "1.0.0")
    {
        _logger = logger;
        _activitySource = new System.Diagnostics.ActivitySource(serviceName, serviceVersion);

        _logger.LogInformation("OpenTelemetry Diagnostics backend initialized for service: {ServiceName} v{ServiceVersion}", serviceName, serviceVersion);
    }

    public async Task StartSpan(TraceSpan span)
    {
        try
        {
            var activity = _activitySource.StartActivity(span.OperationName, System.Diagnostics.ActivityKind.Internal);

            if (activity != null)
            {
                activity.SetTag("span_id", span.SpanId);
                activity.SetTag("parent_span_id", span.ParentSpanId);

                foreach (var tag in span.Tags)
                {
                    activity.SetTag(tag.Key, tag.Value);
                }

                _activeActivities[span.SpanId] = activity;
            }

            _logger.LogDebug("Started OpenTelemetry span: {SpanId} - {OperationName}", span.SpanId, span.OperationName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to start OpenTelemetry span: {SpanId}", span.SpanId);
        }
    }

    public async Task SetSpanTags(string spanId, Dictionary<string, object> tags)
    {
        try
        {
            if (_activeActivities.TryGetValue(spanId, out var activity))
            {
                foreach (var tag in tags)
                {
                    activity.SetTag(tag.Key, tag.Value);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to set OpenTelemetry span tags for span: {SpanId}", spanId);
        }
    }

    public async Task SetSpanError(string spanId, string error, Dictionary<string, object>? tags = null)
    {
        try
        {
            if (_activeActivities.TryGetValue(spanId, out var activity))
            {
                activity.SetStatus(System.Diagnostics.ActivityStatusCode.Error, error);

                if (tags != null)
                {
                    foreach (var tag in tags)
                    {
                        activity.SetTag($"error_{tag.Key}", tag.Value);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to set OpenTelemetry span error for span: {SpanId}", spanId);
        }
    }

    public async Task EndSpan(TraceSpan span)
    {
        try
        {
            if (_activeActivities.TryGetValue(span.SpanId, out var activity))
            {
                activity.Stop();
                _activeActivities.Remove(span.SpanId);
            }

            _logger.LogDebug("Ended OpenTelemetry span: {SpanId}", span.SpanId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to end OpenTelemetry span: {SpanId}", span.SpanId);
        }
    }

    public async Task AddBreadcrumb(DiagnosticBreadcrumb breadcrumb)
    {
        try
        {
            _breadcrumbs.Add(breadcrumb);

            // OpenTelemetry doesn't have breadcrumbs, so we'll add as span events
            foreach (var activity in _activeActivities.Values)
            {
                activity.AddEvent(new System.Diagnostics.ActivityEvent(breadcrumb.Message,
                    tags: new System.Collections.Generic.KeyValuePair<string, object?>[]
                    {
                        new("breadcrumb_category", breadcrumb.Category),
                        new("breadcrumb_level", breadcrumb.Level.ToString()),
                        new("breadcrumb_data", breadcrumb.Data)
                    }));
            }

            _logger.LogDebug("Added OpenTelemetry breadcrumb: {Message}", breadcrumb.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to add OpenTelemetry breadcrumb");
        }
    }

    public async Task ClearBreadcrumbs()
    {
        try
        {
            _breadcrumbs.Clear();
            _logger.LogDebug("Cleared breadcrumbs");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to clear breadcrumbs");
        }
    }

    public async Task RecordException(Exception exception, Dictionary<string, object> context)
    {
        try
        {
            foreach (var activity in _activeActivities.Values)
            {
                activity.RecordException(exception, new System.Collections.Generic.KeyValuePair<string, object?>[]
                {
                    new("exception_context", context)
                });
            }

            _logger.LogDebug("Recorded OpenTelemetry exception: {ExceptionType}", exception.GetType().Name);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to record OpenTelemetry exception");
        }
    }

    public async Task RecordError(string message, Dictionary<string, object> context)
    {
        try
        {
            var exception = new Exception(message);
            await RecordException(exception, context);

            _logger.LogDebug("Recorded OpenTelemetry error: {Message}", message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to record OpenTelemetry error");
        }
    }

    public async Task RecordMetric(string name, double value, string unit, Dictionary<string, object> tags)
    {
        try
        {
            // OpenTelemetry metrics would require a Meter, but for now we'll use span events
            foreach (var activity in _activeActivities.Values)
            {
                activity.AddEvent(new System.Diagnostics.ActivityEvent($"Metric: {name}",
                    tags: new System.Collections.Generic.KeyValuePair<string, object?>[]
                    {
                        new("metric_name", name),
                        new("metric_value", value),
                        new("metric_unit", unit),
                        new("metric_tags", tags)
                    }));
            }

            _logger.LogDebug("Recorded OpenTelemetry metric: {Name} = {Value} {Unit}", name, value, unit);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to record OpenTelemetry metric: {Name}", name);
        }
    }

    public async Task RecordCounter(string name, long increment, Dictionary<string, object> tags)
    {
        await RecordMetric(name, increment, "count", tags);
    }

    public async Task RecordGauge(string name, double value, Dictionary<string, object> tags)
    {
        await RecordMetric(name, value, "gauge", tags);
    }

    public async Task RecordHistogram(string name, double value, Dictionary<string, object> tags)
    {
        await RecordMetric(name, value, "histogram", tags);
    }

    public async Task StartProfiling(string operationName)
    {
        try
        {
            // OpenTelemetry profiling support is limited in .NET
            _logger.LogDebug("Started OpenTelemetry profiling for: {OperationName}", operationName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to start OpenTelemetry profiling");
        }
    }

    public async Task EndProfiling(string operationName, ProfilingSession session)
    {
        try
        {
            _logger.LogDebug("Ended OpenTelemetry profiling for: {OperationName}", operationName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to end OpenTelemetry profiling");
        }
    }

    public async Task RegisterHealthCheck(string name, HealthCheckRegistration registration)
    {
        try
        {
            foreach (var activity in _activeActivities.Values)
            {
                activity.AddEvent(new System.Diagnostics.ActivityEvent($"Health check registered: {name}"));
            }

            _logger.LogDebug("Registered OpenTelemetry health check: {Name}", name);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to register OpenTelemetry health check: {Name}", name);
        }
    }

    public async Task UnregisterHealthCheck(string name)
    {
        try
        {
            foreach (var activity in _activeActivities.Values)
            {
                activity.AddEvent(new System.Diagnostics.ActivityEvent($"Health check unregistered: {name}"));
            }

            _logger.LogDebug("Unregistered OpenTelemetry health check: {Name}", name);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to unregister OpenTelemetry health check: {Name}", name);
        }
    }

    public async Task RecordHealthCheckResult(string name, HealthCheckResult result)
    {
        try
        {
            foreach (var activity in _activeActivities.Values)
            {
                activity.AddEvent(new System.Diagnostics.ActivityEvent($"Health check {name}: {result.Status}",
                    tags: new System.Collections.Generic.KeyValuePair<string, object?>[]
                    {
                        new("health_check_name", name),
                        new("health_check_status", result.Status.ToString()),
                        new("health_check_duration", result.Duration.TotalMilliseconds),
                        new("health_check_description", result.Description)
                    }));
            }

            _logger.LogDebug("Recorded OpenTelemetry health check result: {Name} - {Status}", name, result.Status);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to record OpenTelemetry health check result: {Name}", name);
        }
    }

    public async Task RecordSystemMetrics(Dictionary<string, object> metrics)
    {
        try
        {
            foreach (var activity in _activeActivities.Values)
            {
                foreach (var metric in metrics)
                {
                    activity.AddEvent(new System.Diagnostics.ActivityEvent($"System metric: {metric.Key}",
                        tags: new System.Collections.Generic.KeyValuePair<string, object?>[]
                        {
                            new("system_metric_name", metric.Key),
                            new("system_metric_value", metric.Value)
                        }));
                }
            }

            _logger.LogDebug("Recorded OpenTelemetry system metrics: {Count} metrics", metrics.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to record OpenTelemetry system metrics");
        }
    }

    public async Task RecordSnapshot(DiagnosticSnapshot snapshot)
    {
        try
        {
            foreach (var activity in _activeActivities.Values)
            {
                activity.AddEvent(new System.Diagnostics.ActivityEvent("Diagnostic snapshot captured",
                    tags: new System.Collections.Generic.KeyValuePair<string, object?>[]
                    {
                        new("snapshot_timestamp", snapshot.Timestamp),
                        new("snapshot_breadcrumbs", snapshot.Breadcrumbs.Count),
                        new("snapshot_spans", snapshot.ActiveSpans.Count),
                        new("snapshot_health_checks", snapshot.HealthChecks.Count)
                    }));
            }

            _logger.LogDebug("Recorded OpenTelemetry diagnostic snapshot");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to record OpenTelemetry diagnostic snapshot");
        }
    }

    public async Task SetupAlert(AlertRule rule)
    {
        try
        {
            foreach (var activity in _activeActivities.Values)
            {
                activity.AddEvent(new System.Diagnostics.ActivityEvent($"Alert rule setup: {rule.Name}",
                    tags: new System.Collections.Generic.KeyValuePair<string, object?>[]
                    {
                        new("alert_rule_name", rule.Name),
                        new("alert_rule_metric", rule.MetricName),
                        new("alert_rule_threshold", rule.Threshold),
                        new("alert_rule_severity", rule.Severity.ToString())
                    }));
            }

            _logger.LogDebug("Setup OpenTelemetry alert rule: {Name}", rule.Name);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to setup OpenTelemetry alert rule: {Name}", rule.Name);
        }
    }

    public async Task UpdateAlert(string ruleName, AlertRule rule)
    {
        await SetupAlert(rule); // Reuse the same logic
    }

    public async Task RemoveAlert(string ruleName)
    {
        try
        {
            foreach (var activity in _activeActivities.Values)
            {
                activity.AddEvent(new System.Diagnostics.ActivityEvent($"Alert rule removed: {ruleName}",
                    tags: new System.Collections.Generic.KeyValuePair<string, object?>[]
                    {
                        new("alert_rule_name", ruleName)
                    }));
            }

            _logger.LogDebug("Removed OpenTelemetry alert rule: {Name}", ruleName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to remove OpenTelemetry alert rule: {Name}", ruleName);
        }
    }

    public async Task AcknowledgeAlert(string alertId, string user, string? comment = null)
    {
        try
        {
            foreach (var activity in _activeActivities.Values)
            {
                activity.AddEvent(new System.Diagnostics.ActivityEvent($"Alert acknowledged: {alertId}",
                    tags: new System.Collections.Generic.KeyValuePair<string, object?>[]
                    {
                        new("alert_id", alertId),
                        new("alert_acknowledged_by", user),
                        new("alert_acknowledgement_comment", comment)
                    }));
            }

            _logger.LogDebug("Acknowledged OpenTelemetry alert: {AlertId} by {User}", alertId, user);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to acknowledge OpenTelemetry alert: {AlertId}", alertId);
        }
    }

    public async Task FlushAsync()
    {
        try
        {
            // OpenTelemetry activities are flushed automatically
            _logger.LogDebug("Flushed OpenTelemetry data");
            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to flush OpenTelemetry data");
        }
    }

    public async Task<byte[]> ExportDataAsync(string format, DateTimeOffset startTime, DateTimeOffset endTime)
    {
        try
        {
            var summary = new
            {
                Provider = "OpenTelemetry",
                TimeRange = $"{startTime} to {endTime}",
                Breadcrumbs = _breadcrumbs.Count,
                ActiveSpans = _activeActivities.Count,
                Note = "Data is exported via OpenTelemetry protocol to configured exporters"
            };

            return System.Text.Json.JsonSerializer.SerializeToUtf8Bytes(summary);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to export OpenTelemetry data");
            return System.Text.Json.JsonSerializer.SerializeToUtf8Bytes(new { Error = ex.Message });
        }
    }

    public async Task<long> ClearOldDataAsync(TimeSpan retentionPeriod)
    {
        try
        {
            // OpenTelemetry manages its own data retention
            _logger.LogInformation("OpenTelemetry data retention is managed by exporters");
            return 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to clear old OpenTelemetry data");
            return 0;
        }
    }
}

/// <summary>
/// Firebase Diagnostics backend implementation.
/// </summary>
public class FirebaseDiagnosticsBackend : IDiagnosticsBackend
{
    private readonly ILogger<FirebaseDiagnosticsBackend> _logger;
    private readonly FirebaseAdmin.FirebaseApp _firebaseApp;
    private readonly Google.Cloud.Firestore.FirestoreDb _firestore;
    private readonly Dictionary<string, TraceSpan> _activeSpans = new();
    private readonly List<DiagnosticBreadcrumb> _breadcrumbs = new();

    public FirebaseDiagnosticsBackend(ILogger<FirebaseDiagnosticsBackend> logger, string projectId, string? credentialsPath = null)
    {
        _logger = logger;

        try
        {
            var credential = credentialsPath != null
                ? Google.Apis.Auth.OAuth2.GoogleCredential.FromFile(credentialsPath)
                : Google.Apis.Auth.OAuth2.GoogleCredential.GetApplicationDefault();

            _firebaseApp = FirebaseAdmin.FirebaseApp.Create(new FirebaseAdmin.AppOptions
            {
                Credential = credential,
                ProjectId = projectId
            }, Guid.NewGuid().ToString());

            _firestore = Google.Cloud.Firestore.FirestoreDb.Create(projectId);

            _logger.LogInformation("Firebase Diagnostics backend initialized for project: {ProjectId}", projectId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize Firebase Diagnostics backend");
            throw;
        }
    }

    public async Task StartSpan(TraceSpan span)
    {
        try
        {
            _activeSpans[span.SpanId] = span;

            // Store span start in Firestore
            var spanData = new Dictionary<string, object>
            {
                ["span_id"] = span.SpanId,
                ["operation_name"] = span.OperationName,
                ["start_time"] = span.StartTime,
                ["parent_span_id"] = span.ParentSpanId,
                ["tags"] = span.Tags,
                ["status"] = "active"
            };

            var collection = _firestore.Collection("diagnostics_spans");
            await collection.AddAsync(spanData);

            _logger.LogDebug("Started Firebase Diagnostics span: {SpanId} - {OperationName}", span.SpanId, span.OperationName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to start Firebase Diagnostics span: {SpanId}", span.SpanId);
        }
    }

    public async Task SetSpanTags(string spanId, Dictionary<string, object> tags)
    {
        try
        {
            if (_activeSpans.TryGetValue(spanId, out var span))
            {
                foreach (var tag in tags)
                {
                    span.Tags[tag.Key] = tag.Value;
                }

                // Update in Firestore
                var query = _firestore.Collection("diagnostics_spans").WhereEqualTo("span_id", spanId);
                var snapshot = await query.GetSnapshotAsync();

                foreach (var document in snapshot.Documents)
                {
                    await document.Reference.UpdateAsync(new Dictionary<string, object>
                    {
                        ["tags"] = span.Tags
                    });
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to set Firebase Diagnostics span tags for span: {SpanId}", spanId);
        }
    }

    public async Task SetSpanError(string spanId, string error, Dictionary<string, object>? tags = null)
    {
        try
        {
            if (_activeSpans.TryGetValue(spanId, out var span))
            {
                span.IsError = true;
                span.ErrorMessage = error;

                if (tags != null)
                {
                    foreach (var tag in tags)
                    {
                        span.Tags[$"error_{tag.Key}"] = tag.Value;
                    }
                }

                // Update in Firestore
                var query = _firestore.Collection("diagnostics_spans").WhereEqualTo("span_id", spanId);
                var snapshot = await query.GetSnapshotAsync();

                foreach (var document in snapshot.Documents)
                {
                    await document.Reference.UpdateAsync(new Dictionary<string, object>
                    {
                        ["is_error"] = true,
                        ["error_message"] = error,
                        ["tags"] = span.Tags
                    });
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to set Firebase Diagnostics span error for span: {SpanId}", spanId);
        }
    }

    public async Task EndSpan(TraceSpan span)
    {
        try
        {
            _activeSpans.Remove(span.SpanId);

            // Update span end in Firestore
            var query = _firestore.Collection("diagnostics_spans").WhereEqualTo("span_id", span.SpanId);
            var snapshot = await query.GetSnapshotAsync();

            foreach (var document in snapshot.Documents)
            {
                await document.Reference.UpdateAsync(new Dictionary<string, object>
                {
                    ["end_time"] = span.EndTime,
                    ["duration_ms"] = span.Duration.TotalMilliseconds,
                    ["status"] = "completed"
                });
            }

            _logger.LogDebug("Ended Firebase Diagnostics span: {SpanId}", span.SpanId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to end Firebase Diagnostics span: {SpanId}", span.SpanId);
        }
    }

    public async Task AddBreadcrumb(DiagnosticBreadcrumb breadcrumb)
    {
        try
        {
            _breadcrumbs.Add(breadcrumb);

            // Store breadcrumb in Firestore
            var breadcrumbData = new Dictionary<string, object>
            {
                ["message"] = breadcrumb.Message,
                ["category"] = breadcrumb.Category,
                ["level"] = breadcrumb.Level.ToString(),
                ["timestamp"] = breadcrumb.Timestamp,
                ["data"] = breadcrumb.Data
            };

            var collection = _firestore.Collection("diagnostics_breadcrumbs");
            await collection.AddAsync(breadcrumbData);

            _logger.LogDebug("Added Firebase Diagnostics breadcrumb: {Message}", breadcrumb.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to add Firebase Diagnostics breadcrumb");
        }
    }

    public async Task ClearBreadcrumbs()
    {
        try
        {
            _breadcrumbs.Clear();
            // Note: Firestore breadcrumbs are kept for historical analysis
            _logger.LogDebug("Cleared local breadcrumbs");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to clear breadcrumbs");
        }
    }

    public async Task RecordException(Exception exception, Dictionary<string, object> context)
    {
        try
        {
            var exceptionData = new Dictionary<string, object>
            {
                ["type"] = exception.GetType().FullName,
                ["message"] = exception.Message,
                ["stack_trace"] = exception.StackTrace,
                ["timestamp"] = DateTimeOffset.UtcNow,
                ["context"] = context,
                ["inner_exception"] = exception.InnerException?.Message
            };

            var collection = _firestore.Collection("diagnostics_exceptions");
            await collection.AddAsync(exceptionData);

            _logger.LogDebug("Recorded Firebase Diagnostics exception: {ExceptionType}", exception.GetType().Name);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to record Firebase Diagnostics exception");
        }
    }

    public async Task RecordError(string message, Dictionary<string, object> context)
    {
        try
        {
            var exception = new Exception(message);
            await RecordException(exception, context);

            _logger.LogDebug("Recorded Firebase Diagnostics error: {Message}", message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to record Firebase Diagnostics error");
        }
    }

    public async Task RecordMetric(string name, double value, string unit, Dictionary<string, object> tags)
    {
        try
        {
            var metricData = new Dictionary<string, object>
            {
                ["name"] = name,
                ["value"] = value,
                ["unit"] = unit,
                ["timestamp"] = DateTimeOffset.UtcNow,
                ["tags"] = tags
            };

            var collection = _firestore.Collection("diagnostics_metrics");
            await collection.AddAsync(metricData);

            _logger.LogDebug("Recorded Firebase Diagnostics metric: {Name} = {Value} {Unit}", name, value, unit);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to record Firebase Diagnostics metric: {Name}", name);
        }
    }

    public async Task RecordCounter(string name, long increment, Dictionary<string, object> tags)
    {
        await RecordMetric(name, increment, "count", tags);
    }

    public async Task RecordGauge(string name, double value, Dictionary<string, object> tags)
    {
        await RecordMetric(name, value, "gauge", tags);
    }

    public async Task RecordHistogram(string name, double value, Dictionary<string, object> tags)
    {
        await RecordMetric(name, value, "histogram", tags);
    }

    public async Task StartProfiling(string operationName)
    {
        try
        {
            var profileData = new Dictionary<string, object>
            {
                ["operation_name"] = operationName,
                ["start_time"] = DateTimeOffset.UtcNow,
                ["status"] = "started"
            };

            var collection = _firestore.Collection("diagnostics_profiles");
            await collection.AddAsync(profileData);

            _logger.LogDebug("Started Firebase Diagnostics profiling for: {OperationName}", operationName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to start Firebase Diagnostics profiling");
        }
    }

    public async Task EndProfiling(string operationName, ProfilingSession session)
    {
        try
        {
            var profileData = new Dictionary<string, object>
            {
                ["operation_name"] = operationName,
                ["end_time"] = DateTimeOffset.UtcNow,
                ["duration_ms"] = session.Duration.TotalMilliseconds,
                ["status"] = "completed",
                ["samples"] = session.Samples?.Count ?? 0
            };

            var collection = _firestore.Collection("diagnostics_profiles");
            await collection.AddAsync(profileData);

            _logger.LogDebug("Ended Firebase Diagnostics profiling for: {OperationName}", operationName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to end Firebase Diagnostics profiling");
        }
    }

    public async Task RegisterHealthCheck(string name, HealthCheckRegistration registration)
    {
        try
        {
            var healthCheckData = new Dictionary<string, object>
            {
                ["name"] = name,
                ["type"] = "registration",
                ["timestamp"] = DateTimeOffset.UtcNow
            };

            var collection = _firestore.Collection("diagnostics_health_checks");
            await collection.AddAsync(healthCheckData);

            _logger.LogDebug("Registered Firebase Diagnostics health check: {Name}", name);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to register Firebase Diagnostics health check: {Name}", name);
        }
    }

    public async Task UnregisterHealthCheck(string name)
    {
        try
        {
            var healthCheckData = new Dictionary<string, object>
            {
                ["name"] = name,
                ["type"] = "unregistration",
                ["timestamp"] = DateTimeOffset.UtcNow
            };

            var collection = _firestore.Collection("diagnostics_health_checks");
            await collection.AddAsync(healthCheckData);

            _logger.LogDebug("Unregistered Firebase Diagnostics health check: {Name}", name);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to unregister Firebase Diagnostics health check: {Name}", name);
        }
    }

    public async Task RecordHealthCheckResult(string name, HealthCheckResult result)
    {
        try
        {
            var healthCheckData = new Dictionary<string, object>
            {
                ["name"] = name,
                ["status"] = result.Status.ToString(),
                ["description"] = result.Description,
                ["duration_ms"] = result.Duration.TotalMilliseconds,
                ["timestamp"] = result.Timestamp
            };

            var collection = _firestore.Collection("diagnostics_health_checks");
            await collection.AddAsync(healthCheckData);

            _logger.LogDebug("Recorded Firebase Diagnostics health check result: {Name} - {Status}", name, result.Status);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to record Firebase Diagnostics health check result: {Name}", name);
        }
    }

    public async Task RecordSystemMetrics(Dictionary<string, object> metrics)
    {
        try
        {
            var systemMetricsData = new Dictionary<string, object>
            {
                ["timestamp"] = DateTimeOffset.UtcNow,
                ["metrics"] = metrics
            };

            var collection = _firestore.Collection("diagnostics_system_metrics");
            await collection.AddAsync(systemMetricsData);

            _logger.LogDebug("Recorded Firebase Diagnostics system metrics: {Count} metrics", metrics.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to record Firebase Diagnostics system metrics");
        }
    }

    public async Task RecordSnapshot(DiagnosticSnapshot snapshot)
    {
        try
        {
            var snapshotData = new Dictionary<string, object>
            {
                ["timestamp"] = snapshot.Timestamp,
                ["breadcrumbs_count"] = snapshot.Breadcrumbs.Count,
                ["spans_count"] = snapshot.ActiveSpans.Count,
                ["health_checks_count"] = snapshot.HealthChecks.Count
            };

            var collection = _firestore.Collection("diagnostics_snapshots");
            await collection.AddAsync(snapshotData);

            _logger.LogDebug("Recorded Firebase Diagnostics snapshot");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to record Firebase Diagnostics snapshot");
        }
    }

    public async Task SetupAlert(AlertRule rule)
    {
        try
        {
            var alertData = new Dictionary<string, object>
            {
                ["name"] = rule.Name,
                ["metric_name"] = rule.MetricName,
                ["condition"] = rule.Condition.ToString(),
                ["threshold"] = rule.Threshold,
                ["severity"] = rule.Severity.ToString(),
                ["type"] = "setup",
                ["timestamp"] = DateTimeOffset.UtcNow
            };

            var collection = _firestore.Collection("diagnostics_alerts");
            await collection.AddAsync(alertData);

            _logger.LogDebug("Setup Firebase Diagnostics alert rule: {Name}", rule.Name);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to setup Firebase Diagnostics alert rule: {Name}", rule.Name);
        }
    }

    public async Task UpdateAlert(string ruleName, AlertRule rule)
    {
        await SetupAlert(rule); // Reuse the same logic
    }

    public async Task RemoveAlert(string ruleName)
    {
        try
        {
            var alertData = new Dictionary<string, object>
            {
                ["name"] = ruleName,
                ["type"] = "removal",
                ["timestamp"] = DateTimeOffset.UtcNow
            };

            var collection = _firestore.Collection("diagnostics_alerts");
            await collection.AddAsync(alertData);

            _logger.LogDebug("Removed Firebase Diagnostics alert rule: {Name}", ruleName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to remove Firebase Diagnostics alert rule: {Name}", ruleName);
        }
    }

    public async Task AcknowledgeAlert(string alertId, string user, string? comment = null)
    {
        try
        {
            var alertData = new Dictionary<string, object>
            {
                ["alert_id"] = alertId,
                ["acknowledged_by"] = user,
                ["comment"] = comment,
                ["timestamp"] = DateTimeOffset.UtcNow
            };

            var collection = _firestore.Collection("diagnostics_alerts");
            await collection.AddAsync(alertData);

            _logger.LogDebug("Acknowledged Firebase Diagnostics alert: {AlertId} by {User}", alertId, user);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to acknowledge Firebase Diagnostics alert: {AlertId}", alertId);
        }
    }

    public async Task FlushAsync()
    {
        try
        {
            // Firebase operations are synchronous
            _logger.LogDebug("Flushed Firebase Diagnostics data");
            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to flush Firebase Diagnostics data");
        }
    }

    public async Task<byte[]> ExportDataAsync(string format, DateTimeOffset startTime, DateTimeOffset endTime)
    {
        try
        {
            var summary = new
            {
                Provider = "Firebase",
                TimeRange = $"{startTime} to {endTime}",
                Breadcrumbs = _breadcrumbs.Count,
                ActiveSpans = _activeSpans.Count,
                Note = "Data is stored in Firebase Firestore, use Firebase console for detailed export"
            };

            return System.Text.Json.JsonSerializer.SerializeToUtf8Bytes(summary);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to export Firebase Diagnostics data");
            return System.Text.Json.JsonSerializer.SerializeToUtf8Bytes(new { Error = ex.Message });
        }
    }

    public async Task<long> ClearOldDataAsync(TimeSpan retentionPeriod)
    {
        try
        {
            var cutoff = DateTimeOffset.UtcNow - retentionPeriod;

            // Note: Firebase Firestore data retention would need to be configured at the project level
            // We can't delete data programmatically without specific queries
            _logger.LogInformation("Firebase Diagnostics data retention is managed by Firestore, not cleared programmatically");
            return 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to clear old Firebase Diagnostics data");
            return 0;
        }
    }
}
#endif // FALSE - Disabled broken backends

/// <summary>
/// In-memory diagnostics backend for testing and development.
/// </summary>
public class InMemoryDiagnosticsBackend : IDiagnosticsBackend
{
    private readonly List<TraceSpan> _completedSpans = new();
    private readonly Dictionary<string, TraceSpan> _activeSpans = new();
    private readonly List<DiagnosticBreadcrumb> _breadcrumbs = new();
    private readonly List<Exception> _exceptions = new();
    private readonly List<string> _errors = new();
    private readonly List<MetricRecord> _metrics = new();
    private readonly Dictionary<string, HealthCheckRegistration> _healthChecks = new();
    private readonly List<HealthCheckResult> _healthCheckResults = new();
    private readonly List<SystemMetrics> _systemMetrics = new();
    private readonly List<DiagnosticSnapshot> _snapshots = new();
    private readonly Dictionary<string, AlertRule> _alertRules = new();
    private readonly List<ProfilingSession> _profilingSessions = new();

    public Task StartSpan(TraceSpan span)
    {
        _activeSpans[span.SpanId] = span;
        return Task.CompletedTask;
    }

    public Task SetSpanTags(string spanId, Dictionary<string, object> tags)
    {
        if (_activeSpans.TryGetValue(spanId, out var span))
        {
            foreach (var kvp in tags)
            {
                span.Tags[kvp.Key] = kvp.Value;
            }
        }
        return Task.CompletedTask;
    }

    public Task SetSpanError(string spanId, string error, Dictionary<string, object>? tags = null)
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
        }
        return Task.CompletedTask;
    }

    public Task EndSpan(TraceSpan span)
    {
        _activeSpans.Remove(span.SpanId);
        _completedSpans.Add(span);
        return Task.CompletedTask;
    }

    public Task AddBreadcrumb(DiagnosticBreadcrumb breadcrumb)
    {
        _breadcrumbs.Add(breadcrumb);
        return Task.CompletedTask;
    }

    public Task ClearBreadcrumbs()
    {
        _breadcrumbs.Clear();
        return Task.CompletedTask;
    }

    public Task RecordException(Exception exception, Dictionary<string, object> context)
    {
        _exceptions.Add(exception);
        return Task.CompletedTask;
    }

    public Task RecordError(string message, Dictionary<string, object> context)
    {
        _errors.Add(message);
        return Task.CompletedTask;
    }

    public Task RecordMetric(string name, double value, string unit, Dictionary<string, object> tags)
    {
        _metrics.Add(new MetricRecord { Name = name, Value = value, Unit = unit, Tags = tags, Timestamp = DateTimeOffset.UtcNow });
        return Task.CompletedTask;
    }

    public Task RecordCounter(string name, long increment, Dictionary<string, object> tags)
    {
        _metrics.Add(new MetricRecord { Name = name, Value = increment, Unit = "count", Tags = tags, Timestamp = DateTimeOffset.UtcNow });
        return Task.CompletedTask;
    }

    public Task RecordGauge(string name, double value, Dictionary<string, object> tags)
    {
        _metrics.Add(new MetricRecord { Name = name, Value = value, Unit = "gauge", Tags = tags, Timestamp = DateTimeOffset.UtcNow });
        return Task.CompletedTask;
    }

    public Task RecordHistogram(string name, double value, Dictionary<string, object> tags)
    {
        _metrics.Add(new MetricRecord { Name = name, Value = value, Unit = "histogram", Tags = tags, Timestamp = DateTimeOffset.UtcNow });
        return Task.CompletedTask;
    }

    public Task StartProfiling(string operationName)
    {
        return Task.CompletedTask;
    }

    public Task EndProfiling(string operationName, ProfilingSession session)
    {
        _profilingSessions.Add(session);
        return Task.CompletedTask;
    }

    public Task RegisterHealthCheck(string name, HealthCheckRegistration registration)
    {
        _healthChecks[name] = registration;
        return Task.CompletedTask;
    }

    public Task UnregisterHealthCheck(string name)
    {
        _healthChecks.Remove(name);
        return Task.CompletedTask;
    }

    public Task RecordHealthCheckResult(string name, HealthCheckResult result)
    {
        _healthCheckResults.Add(result);
        return Task.CompletedTask;
    }

    public Task RecordSystemMetrics(Dictionary<string, object> metrics)
    {
        // Convert to SystemMetrics if possible, otherwise just store
        return Task.CompletedTask;
    }

    public Task RecordSnapshot(DiagnosticSnapshot snapshot)
    {
        _snapshots.Add(snapshot);
        return Task.CompletedTask;
    }

    public Task SetupAlert(AlertRule rule)
    {
        _alertRules[rule.Name] = rule;
        return Task.CompletedTask;
    }

    public Task UpdateAlert(string ruleName, AlertRule rule)
    {
        _alertRules[ruleName] = rule;
        return Task.CompletedTask;
    }

    public Task RemoveAlert(string ruleName)
    {
        _alertRules.Remove(ruleName);
        return Task.CompletedTask;
    }

    public Task AcknowledgeAlert(string alertId, string user, string? comment = null)
    {
        return Task.CompletedTask;
    }

    public Task FlushAsync()
    {
        return Task.CompletedTask;
    }

    public Task<byte[]> ExportDataAsync(string format, DateTimeOffset startTime, DateTimeOffset endTime)
    {
        // Simple JSON export for demo
        var data = new
        {
            Spans = _completedSpans.Where(s => s.StartTime >= startTime && s.StartTime <= endTime),
            Breadcrumbs = _breadcrumbs.Where(b => b.Timestamp >= startTime && b.Timestamp <= endTime),
            Metrics = _metrics.Where(m => m.Timestamp >= startTime && m.Timestamp <= endTime),
            Exceptions = _exceptions.Count,
            Errors = _errors.Count
        };

        return Task.FromResult(System.Text.Json.JsonSerializer.SerializeToUtf8Bytes(data));
    }

    public Task<long> ClearOldDataAsync(TimeSpan retentionPeriod)
    {
        var cutoff = DateTimeOffset.UtcNow - retentionPeriod;
        var removedSpans = _completedSpans.RemoveAll(s => s.StartTime < cutoff);
        var removedBreadcrumbs = _breadcrumbs.RemoveAll(b => b.Timestamp < cutoff);
        var removedMetrics = _metrics.RemoveAll(m => m.Timestamp < cutoff);

        return Task.FromResult((long)(removedSpans + removedBreadcrumbs + removedMetrics));
    }

    // Helper classes and accessors for testing
    public class MetricRecord
    {
        public string Name { get; set; } = string.Empty;
        public double Value { get; set; }
        public string Unit { get; set; } = string.Empty;
        public Dictionary<string, object> Tags { get; set; } = new();
        public DateTimeOffset Timestamp { get; set; }
    }

    public IReadOnlyList<TraceSpan> GetCompletedSpans() => _completedSpans.AsReadOnly();
    public IReadOnlyList<DiagnosticBreadcrumb> GetBreadcrumbs() => _breadcrumbs.AsReadOnly();
    public IReadOnlyList<Exception> GetExceptions() => _exceptions.AsReadOnly();
    public IReadOnlyList<string> GetErrors() => _errors.AsReadOnly();
    public IReadOnlyList<MetricRecord> GetMetrics() => _metrics.AsReadOnly();
    public IReadOnlyList<HealthCheckResult> GetHealthCheckResults() => _healthCheckResults.AsReadOnly();
    public IReadOnlyList<DiagnosticSnapshot> GetSnapshots() => _snapshots.AsReadOnly();
    public IReadOnlyDictionary<string, AlertRule> GetAlertRules() => _alertRules;
}

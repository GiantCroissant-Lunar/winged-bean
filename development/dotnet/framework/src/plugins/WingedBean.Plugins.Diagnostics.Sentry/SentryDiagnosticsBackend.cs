using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Plate.CrossMilo.Contracts.Diagnostics;
using Sentry;
using Sentry.Protocol;
using WingedBean.Plugins.Diagnostics;
using BreadcrumbLevel = Plate.CrossMilo.Contracts.Diagnostics.BreadcrumbLevel;

namespace WingedBean.Plugins.Diagnostics.Sentry;

/// <summary>
/// Sentry diagnostics backend implementation (Tier 4 Provider).
/// Provides error tracking, performance monitoring, and profiling via Sentry.io.
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

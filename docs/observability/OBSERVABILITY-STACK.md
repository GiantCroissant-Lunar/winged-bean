---
id: OBSERVABILITY-STACK
title: WingedBean Observability Stack Overview
status: Living Document
category: documentation
created: 2025-01-10
updated: 2025-01-11
---

# WingedBean Observability Stack

This document provides an overview of the WingedBean observability infrastructure, consisting of Analytics and Diagnostics services.

## Overview

The WingedBean framework implements a comprehensive observability stack with multiple layers:

### Contract Layer (High-Level Abstractions)
1. **Analytics** (RFC-0030) - Product analytics and user behavior tracking
2. **Diagnostics** (RFC-0031) - System health monitoring and troubleshooting
3. **Resilience** (Implemented) - Fault tolerance and reliability patterns

### Implementation Layer (Industry-Standard SDKs)
4. **Observability Strategy** (RFC-0033) - Unified strategy and architecture foundation
5. **Error Tracking** (RFC-0034) - Sentry integration for crash reports and exceptions
6. **Distributed Tracing** (RFC-0035) - OpenTelemetry integration for traces and metrics

Together, these services provide complete visibility into application behavior, system health, and user experience.

## Architecture Relationship

**Key Insight**: RFC-0030/0031 define **WingedBean-specific contracts** (IAnalyticsService, IDiagnosticsService), while RFC-0033/0034/0035 define the **implementation strategy** using industry-standard tools (Sentry, OpenTelemetry).

- `IDiagnosticsService` is a **thin wrapper** over Sentry SDK (errors) + OpenTelemetry SDK (traces)
- `IAnalyticsService` is an **independent service** for product/business metrics
- Both follow the observability strategy defined in RFC-0033

## Architecture

```
┌─────────────────────────────────────────────────────────────┐
│                    Application Code                          │
│                    (Game, Plugins, Services)                 │
└────────────────────┬────────────────┬────────────────────────┘
                     │                │
            ┌────────▼────────┐  ┌───▼──────────┐
            │  IAnalytics     │  │ IDiagnostics │  ← Contract Layer
            │  Service        │  │  Service     │    (RFC-0030/0031)
            │ (RFC-0030)      │  │ (RFC-0031)   │
            └────────┬────────┘  └───┬──────────┘
                     │                │
                     │           ┌────┴─────────┐
                     │           │              │
                     │           ↓              ↓
                     │    ┌──────────────┐ ┌───────────────┐
                     │    │  Sentry SDK  │ │ OTEL SDK      │ ← SDK Layer
                     │    │  (RFC-0034)  │ │ (RFC-0035)    │   (Industry Standard)
                     │    │  - Errors    │ │ - Traces      │
                     │    │  - Crashes   │ │ - Metrics     │
                     │    └──────┬───────┘ └───┬───────────┘
                     │           │             │
                     ↓           ↓             ↓
        ┌────────────────┐ ┌─────────┐ ┌──────────────┐
        │ Segment/       │ │ Sentry  │ │ OTEL Backend │  ← Backend Layer
        │ App Insights   │ │ Backend │ │ (Jaeger/     │    (Data Storage)
        │ (Analytics)    │ │ (Errors)│ │  Prometheus) │
        └────────────────┘ └─────────┘ └──────────────┘
                │              │              │
                └──────────────┴──────────────┘
                               │
                    ┌──────────▼──────────┐
                    │  RFC-0033: Strategy │     ← Strategy Layer
                    │  - PII Redaction    │       (Foundation)
                    │  - Configuration    │
                    │  - Multi-Language   │
                    └─────────────────────┘
```

## RFC Relationships

### Contract RFCs (Application-Facing APIs)
- **RFC-0030: Analytics Service** - Defines `IAnalyticsService` contract for product/business metrics
- **RFC-0031: Diagnostics Service** - Defines `IDiagnosticsService` contract for system observability

### Implementation RFCs (Infrastructure Strategy)
- **RFC-0033: Observability Strategy Foundation** - Overall architecture, multi-language support, configuration
- **RFC-0034: Sentry Integration** - Error tracking and crash reporting implementation
- **RFC-0035: OpenTelemetry Integration** - Distributed tracing and metrics implementation

### Implementation Strategy

**IDiagnosticsService** (RFC-0031) is implemented as a **thin wrapper** over:
- **Sentry SDK** (RFC-0034) for error tracking via `TrackErrorAsync()`
- **OpenTelemetry SDK** (RFC-0035) for distributed tracing via `StartTrace()`
- **Custom logic** for health checks (WingedBean-specific)

**IAnalyticsService** (RFC-0030) is implemented as:
- **Independent service** for product/business metrics
- Backend adapters for Segment or Application Insights
- Separate from system diagnostics (different retention, sampling)

**All services** follow:
- **RFC-0033** strategy for PII redaction, configuration, multi-language support
- **Unified correlation** via Trace IDs across all telemetry

## Service Comparison

| Feature | Analytics (RFC-0030) | Diagnostics (RFC-0031) | Resilience (Implemented) |
|---------|---------------------|------------------------|--------------------------|
| **Purpose** | Product metrics & user behavior | System health & troubleshooting | Fault tolerance & reliability |
| **Primary Use** | Business decisions, A/B testing | Debugging, performance tuning | Error handling, retries |
| **Data Type** | Events, funnels, cohorts | Traces, metrics, snapshots | Circuit state, retry stats |
| **Latency** | Async batched (seconds) | Real-time (milliseconds) | Sync/async (varies) |
| **Backends** | Segment, App Insights | Sentry, OpenTelemetry | Polly pipelines |
| **Retention** | 90 days (business data) | 7 days (diagnostic data) | In-memory (runtime) |

## Integration Points

### 1. Correlation via Trace IDs

All three services share correlation IDs for cross-referencing:

```csharp
// Diagnostics creates trace ID
var traceId = diagnostics.GetCurrentTraceId();

// Analytics uses same trace ID
await analytics.TrackEventAsync("level_complete", new Dictionary<string, object>
{
    ["trace_id"] = traceId,
    ["level"] = 5
});

// Resilience events include trace ID
resilience.ResilienceEventOccurred += (sender, e) =>
{
    diagnostics.AddBreadcrumb($"Resilience: {e.EventType}", context: new Dictionary<string, object>
    {
        ["trace_id"] = traceId
    });
};
```

### 2. Error Context Enrichment

Analytics and Diagnostics collaborate for rich error context:

```csharp
try
{
    // Analytics tracks user journey
    await analytics.TrackEventAsync("player_action", props);

    // Diagnostics adds breadcrumbs
    diagnostics.AddBreadcrumb("Player attacked enemy");

    await gameService.ProcessAttackAsync();
}
catch (Exception ex)
{
    // Diagnostics captures error with breadcrumbs
    await diagnostics.TrackErrorAsync(ex, ErrorSeverity.Error);

    // Analytics tracks error event
    await analytics.TrackErrorAsync(ex);

    // Resilience handles retry
    await resilience.ExecuteAsync("game-action", async ct =>
        await RecoverFromErrorAsync(ct));
}
```

### 3. Performance Monitoring

Analytics and Diagnostics both track performance from different angles:

```csharp
// Diagnostics: Low-level performance profiling
using (diagnostics.MeasurePerformance("render_frame"))
{
    // Analytics: High-level user experience metric
    var startTime = DateTimeOffset.UtcNow;

    await RenderFrameAsync();

    var duration = DateTimeOffset.UtcNow - startTime;
    await analytics.TrackPerformanceAsync("frame_time", duration.TotalMilliseconds);
}
```

### 4. Health-Driven Analytics

Health check results feed into analytics for proactive monitoring:

```csharp
var healthReport = await diagnostics.GetHealthAsync();

if (healthReport.Status == HealthStatus.Degraded)
{
    // Track degraded state as analytics event
    await analytics.TrackEventAsync("system_degraded", new Dictionary<string, object>
    {
        ["unhealthy_checks"] = healthReport.Checks
            .Where(c => c.Value.Status != HealthStatus.Healthy)
            .Select(c => c.Key)
            .ToArray()
    });
}
```

## Common Use Cases

### Use Case 1: Debugging Production Issues

**Scenario**: User reports game crash during level 5.

**Flow**:
1. **Analytics** shows user journey: `level_start` → `player_action` → `crash`
2. **Diagnostics** provides trace with error context and breadcrumbs
3. **Resilience** shows failed retry attempts and circuit breaker state
4. **Result**: Reproduce locally with diagnostic snapshot

```csharp
// Find user session in analytics
var session = await analytics.GetSessionAsync(userId);

// Get diagnostic trace for that session
var trace = await diagnostics.GetTraceAsync(session.TraceId);

// Capture diagnostic snapshot at time of crash
var snapshot = await diagnostics.CaptureSnapshotAsync(new SnapshotOptions
{
    IncludeHeapDump = true,
    IncludeThreadDump = true
});
```

### Use Case 2: Performance Optimization

**Scenario**: Users report slow load times.

**Flow**:
1. **Analytics** identifies which levels have high load times (p95)
2. **Diagnostics** profiles level loading operation
3. **Resilience** shows timeout rates for asset loading
4. **Result**: Optimize slow asset loading code

```csharp
// Analytics: Identify problem levels
var slowLevels = await analytics.GetEventsAsync("level_start")
    .Where(e => e.Properties["load_time_ms"] > 5000);

// Diagnostics: Profile level loading
using (diagnostics.MeasurePerformance("level_load", new Dictionary<string, object>
{
    ["level_id"] = levelId
}))
{
    await LoadLevelAsync(levelId);
}

// Get performance stats
var stats = diagnostics.GetPerformanceStats("level_load");
// Identify bottleneck: P95 = 6.2s, mostly in texture loading
```

### Use Case 3: A/B Testing with Health Monitoring

**Scenario**: Testing new combat system.

**Flow**:
1. **Analytics** tracks user engagement per variant
2. **Diagnostics** monitors error rates and performance
3. **Resilience** tracks failure rates per variant
4. **Result**: Choose variant with best UX and reliability

```csharp
// Analytics: Assign user to variant
var variant = await analytics.GetFeatureVariantAsync("new_combat_system");
await analytics.TrackEventAsync("combat_start", new Dictionary<string, object>
{
    ["variant"] = variant
});

// Diagnostics: Monitor variant health
diagnostics.RegisterHealthCheck($"combat_variant_{variant}", async ct =>
{
    var errorRate = await GetVariantErrorRateAsync(variant, ct);
    return new HealthCheckResult
    {
        Status = errorRate < 0.05 ? HealthStatus.Healthy : HealthStatus.Degraded,
        Data = { ["error_rate"] = errorRate }
    };
});
```

### Use Case 4: Funnel Analysis with Error Tracking

**Scenario**: Low onboarding completion rate.

**Flow**:
1. **Analytics** tracks funnel: `start` → `tutorial` → `complete`
2. **Diagnostics** shows errors during `tutorial` step
3. **Resilience** reveals service timeouts
4. **Result**: Fix tutorial service reliability

```csharp
// Analytics: Track funnel
await analytics.TrackFunnelStepAsync("onboarding", "start", 1);
await analytics.TrackFunnelStepAsync("onboarding", "tutorial", 2);

// If tutorial service fails...
try
{
    await tutorialService.LoadAsync();
}
catch (Exception ex)
{
    // Diagnostics: Track error
    await diagnostics.TrackErrorAsync(ex, ErrorSeverity.Error);

    // Resilience: Retry with circuit breaker
    await resilience.ExecuteAsync("tutorial-service", async ct =>
        await tutorialService.RetryLoadAsync(ct));
}

// Analytics: Track drop-off
await analytics.TrackFunnelStepAsync("onboarding", "dropped", 2);
```

## Data Flow

### Event Pipeline

```
User Action
    ↓
Application Code
    ↓
┌─────────────────────────────────────┐
│  1. Capture Context                 │
│     • Trace ID (Diagnostics)        │
│     • Session ID (Analytics)        │
│     • User ID (Analytics)           │
└─────────────────────────────────────┘
    ↓
┌─────────────────────────────────────┐
│  2. Enrich Event                    │
│     • Add global tags               │
│     • Add system context            │
│     • Scrub PII                     │
└─────────────────────────────────────┘
    ↓
┌─────────────────────────────────────┐
│  3. Route to Services               │
│     • Analytics: Business events    │
│     • Diagnostics: System events    │
│     • Resilience: Fault events      │
└─────────────────────────────────────┘
    ↓
┌─────────────────────────────────────┐
│  4. Batch & Send                    │
│     • Buffer events                 │
│     • Send batches                  │
│     • Handle failures (Resilience)  │
└─────────────────────────────────────┘
    ↓
Backend Systems
```

## Configuration

### Unified Configuration (appsettings.json)

```json
{
  "Observability": {
    "Analytics": {
      "Enabled": true,
      "Provider": "ApplicationInsights",
      "BatchSize": 20,
      "FlushInterval": "00:00:10",
      "SamplingRate": 1.0,
      "ScrubPII": true
    },
    "Diagnostics": {
      "Enabled": true,
      "HealthCheckInterval": "00:00:30",
      "EnableTracing": true,
      "EnableProfiling": true,
      "SnapshotRetention": 7
    },
    "Resilience": {
      "DefaultRetries": 3,
      "DefaultTimeout": "00:00:30",
      "CircuitBreakerThreshold": 5
    },
    "Correlation": {
      "TraceIdHeader": "X-Trace-ID",
      "SessionIdHeader": "X-Session-ID",
      "GlobalTags": {
        "environment": "production",
        "version": "1.0.0"
      }
    }
  }
}
```

### Code Configuration

```csharp
// Register all observability services
services.AddObservability(options =>
{
    options.Analytics.Provider = "ApplicationInsights";
    options.Diagnostics.EnableTracing = true;
    options.Resilience.DefaultRetries = 3;
});

// Or register individually
services.AddAnalytics(config.GetSection("Observability:Analytics"));
services.AddDiagnostics(config.GetSection("Observability:Diagnostics"));
services.AddResilience(config.GetSection("Observability:Resilience"));
```

## Best Practices

### 1. Use Correlation IDs Everywhere

```csharp
var traceId = diagnostics.GetCurrentTraceId();
var sessionId = analytics.GetCurrentSessionId();

// Include in all events
var commonContext = new Dictionary<string, object>
{
    ["trace_id"] = traceId,
    ["session_id"] = sessionId
};
```

### 2. Separate Business vs System Events

```csharp
// Analytics: Business event (what user did)
await analytics.TrackEventAsync("item_purchased", new Dictionary<string, object>
{
    ["item_id"] = "sword_of_doom",
    ["price"] = 100
});

// Diagnostics: System event (how system behaved)
diagnostics.RecordMetric("purchase_latency_ms", 245);
```

### 3. Use Health Checks Proactively

```csharp
// Register health checks for all critical services
diagnostics.RegisterHealthCheck("database", async ct =>
{
    var canConnect = await db.PingAsync(ct);
    return new HealthCheckResult
    {
        Status = canConnect ? HealthStatus.Healthy : HealthStatus.Unhealthy
    };
});

// Check health before operations
var health = await diagnostics.GetHealthStatusAsync();
if (health == HealthStatus.Unhealthy)
{
    // Degrade gracefully or retry
}
```

### 4. Leverage Resilience for Analytics/Diagnostics Reliability

```csharp
// Use resilience for analytics backend calls
await resilience.ExecuteAsync("analytics-backend", async ct =>
{
    await analytics.FlushAsync(ct);
});

// Use resilience for diagnostic exports
await resilience.ExecuteAsync("diagnostic-export", async ct =>
{
    await diagnostics.ExportDiagnosticsAsync(filePath, ct: ct);
});
```

## Monitoring the Monitors

Even observability services need monitoring:

```csharp
// Health check for analytics service
diagnostics.RegisterHealthCheck("analytics_service", async ct =>
{
    var queueSize = analytics.GetQueueSize();
    var lastFlush = analytics.GetLastFlushTime();

    var isHealthy = queueSize < 1000 &&
                   (DateTimeOffset.UtcNow - lastFlush) < TimeSpan.FromMinutes(5);

    return new HealthCheckResult
    {
        Status = isHealthy ? HealthStatus.Healthy : HealthStatus.Degraded,
        Data = { ["queue_size"] = queueSize, ["last_flush"] = lastFlush }
    };
});

// Alert on observability service failures
diagnostics.RegisterAlert("observability_failure", ctx =>
{
    return ctx.GetHealthStatus("analytics_service") == HealthStatus.Unhealthy;
}, alert =>
{
    logger.LogCritical("Observability failure: {Message}", alert.Message);
});
```

## Performance Overhead

Target overhead for all observability services:

| Service | CPU Overhead | Memory Overhead | Network Impact |
|---------|--------------|-----------------|----------------|
| Analytics | < 1% | < 10MB | Batched (low) |
| Diagnostics | < 2% | < 20MB | Real-time (medium) |
| Resilience | < 0.5% | < 5MB | None (local) |
| **Total** | **< 3.5%** | **< 35MB** | **Batched** |

## Roadmap

### Phase 1: Foundation (Weeks 1-2) ✅ COMPLETE
- ✅ RFC-0032: NuGet.Versioning migration (Implemented)
- ✅ RFC-0033: Observability Strategy (Documented)
- ✅ RFC-0034: Sentry Integration (Documented)
- ✅ RFC-0035: OpenTelemetry Integration (Documented)

### Phase 2: Diagnostics Core (Weeks 3-4) 🔄 IN PROGRESS
- 🔄 Implement `IDiagnosticsService` as thin wrapper over Sentry + OTEL
- 📝 Integrate Sentry SDK for error tracking (RFC-0034)
- 📝 Integrate OpenTelemetry SDK for distributed tracing (RFC-0035)
- 📝 Add health checks (WingedBean-specific)
- 📝 Python, Node.js, Unity instrumentation

### Phase 3: Analytics Core (Weeks 5-6)
- 📝 Implement `IAnalyticsService` 
- 📝 Integrate Segment or Application Insights
- 📝 Add session management and funnel tracking
- 📝 Multi-language support

### Phase 4: Integration & Tools (Weeks 7-8)
- 📝 Unified observability dashboard (Terminal.Gui)
- 📝 Cross-service correlation validation
- 📝 Documentation and developer guides
- 📝 Cost optimization and sampling strategies

## References

### RFC Documents
- [RFC-0030: Analytics Service](./0030-analytics-service.md) - Product metrics contract
- [RFC-0031: Diagnostics Service](./0031-diagnostics-service.md) - System observability contract
- [RFC-0032: NuGet.Versioning Migration](./0032-nuget-versioning-migration.md) - Version management
- [RFC-0033: Observability Strategy Foundation](./0033-observability-strategy-foundation.md) - Overall architecture
- [RFC-0034: Sentry Integration](./0034-sentry-integration-error-tracking.md) - Error tracking implementation
- [RFC-0035: OpenTelemetry Integration](./0035-opentelemetry-integration-distributed-tracing.md) - Tracing implementation
- [RFC Alignment Analysis](./RFC-ALIGNMENT-ANALYSIS.md) - Architecture reconciliation

### External Resources
- [OpenTelemetry](https://opentelemetry.io/)
- [Sentry Documentation](https://docs.sentry.io/)
- [The Three Pillars of Observability](https://www.oreilly.com/library/view/distributed-systems-observability/9781492033431/ch04.html)

## Contributors

- Claude (AI Assistant) - Initial RFC design
- WingedBean Team - Architecture review

---

**Status**: Draft
**Last Updated**: 2025-10-05

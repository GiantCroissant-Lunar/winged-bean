---
id: RFC-0030
title: Analytics Service for Game Telemetry and User Behavior Tracking
status: Draft
category: architecture
created: 2025-10-05
updated: 2025-10-05
related-rfcs: RFC-0033, RFC-0031
---

# RFC-0030: Analytics Service for Game Telemetry and User Behavior Tracking

> **⚠️ Scope Note**: This RFC focuses on **product/business analytics** (user behavior, engagement, funnels). For **system diagnostics** (errors, traces, health), see:
> - **RFC-0031**: Diagnostics Service (system observability contract)
> - **RFC-0033**: Observability Strategy (unified architecture)
> - **RFC-0034**: Sentry Integration (error tracking - used by Diagnostics, not Analytics)
> - **RFC-0035**: OpenTelemetry Integration (tracing - used by Diagnostics, not Analytics)

## Summary

Introduce a comprehensive Analytics service (`IAnalyticsService`) for tracking game telemetry, user behavior, and business metrics in the WingedBean framework. The service will provide event tracking, funnel analysis, session management, and real-time analytics with pluggable backends (Application Insights for Azure, Segment for multi-destination).

**Separation of Concerns**: Analytics focuses on **product data** (what users do), while Diagnostics (RFC-0031) focuses on **system data** (how the system behaves).

## Motivation

### Current State

The WingedBean framework currently lacks a unified analytics and telemetry system:
- ❌ No structured event tracking for game actions
- ❌ No user behavior analytics (sessions, funnels, retention)
- ❌ No business metrics (engagement, monetization)
- ❌ Limited observability for player experience
- ❌ No A/B testing or feature flag support
- ❌ Ad-hoc logging instead of structured telemetry

### Problems to Solve

1. **Visibility Gap**: Cannot track player behavior, progression, or engagement
2. **Performance Blindness**: No metrics for FPS, load times, or resource usage
3. **Business Metrics**: Cannot measure DAU/MAU, retention, or monetization
4. **Debugging**: Hard to reproduce issues without telemetry context
5. **Product Decisions**: No data for feature prioritization or A/B testing

### Goals

1. ✅ **Structured Event Tracking**: Track game events with rich context
2. ✅ **Session Management**: Automatic session tracking with user identification
3. ✅ **Funnel Analysis**: Track user journeys and conversion funnels
4. ✅ **Performance Metrics**: Track FPS, load times, memory usage
5. ✅ **Business Metrics**: DAU/MAU, retention cohorts, revenue tracking
6. ✅ **Pluggable Backends**: Support multiple analytics providers
7. ✅ **Privacy-First**: GDPR/CCPA compliant with consent management
8. ✅ **Real-time**: Low-latency event ingestion with batching

## Proposal

### Service Contract

```csharp
namespace WingedBean.Contracts.Analytics;

/// <summary>
/// Analytics service for tracking game telemetry and user behavior.
/// Supports event tracking, sessions, funnels, and business metrics.
/// </summary>
public interface IAnalyticsService
{
    // ===== Event Tracking =====

    /// <summary>
    /// Track a custom event with properties.
    /// </summary>
    Task TrackEventAsync(string eventName, IDictionary<string, object>? properties = null, CancellationToken ct = default);

    /// <summary>
    /// Track a page/screen view.
    /// </summary>
    Task TrackPageViewAsync(string pageName, IDictionary<string, object>? properties = null, CancellationToken ct = default);

    /// <summary>
    /// Track an error/exception.
    /// </summary>
    Task TrackErrorAsync(Exception exception, IDictionary<string, object>? properties = null, CancellationToken ct = default);

    // ===== User Identification =====

    /// <summary>
    /// Identify the current user.
    /// </summary>
    Task IdentifyAsync(string userId, IDictionary<string, object>? traits = null, CancellationToken ct = default);

    /// <summary>
    /// Track an anonymous user.
    /// </summary>
    Task TrackAnonymousAsync(string anonymousId, CancellationToken ct = default);

    /// <summary>
    /// Merge anonymous user with identified user.
    /// </summary>
    Task AliasAsync(string userId, string previousId, CancellationToken ct = default);

    // ===== Session Management =====

    /// <summary>
    /// Start a new analytics session.
    /// </summary>
    Task StartSessionAsync(CancellationToken ct = default);

    /// <summary>
    /// End the current session.
    /// </summary>
    Task EndSessionAsync(CancellationToken ct = default);

    /// <summary>
    /// Get the current session ID.
    /// </summary>
    string? GetCurrentSessionId();

    // ===== Game-Specific Tracking =====

    /// <summary>
    /// Track player progression (level up, achievement, etc.).
    /// </summary>
    Task TrackProgressionAsync(string milestone, int level, IDictionary<string, object>? properties = null, CancellationToken ct = default);

    /// <summary>
    /// Track in-game economy events (purchase, earn, spend).
    /// </summary>
    Task TrackEconomyAsync(EconomyEventType type, string itemId, decimal amount, string currency, CancellationToken ct = default);

    /// <summary>
    /// Track game performance metrics (FPS, load time, etc.).
    /// </summary>
    Task TrackPerformanceAsync(string metricName, double value, IDictionary<string, object>? properties = null, CancellationToken ct = default);

    // ===== Funnel Tracking =====

    /// <summary>
    /// Track a funnel step (e.g., onboarding flow).
    /// </summary>
    Task TrackFunnelStepAsync(string funnelName, string stepName, int stepIndex, CancellationToken ct = default);

    /// <summary>
    /// Track funnel completion.
    /// </summary>
    Task TrackFunnelCompletionAsync(string funnelName, TimeSpan duration, CancellationToken ct = default);

    // ===== Batch Operations =====

    /// <summary>
    /// Flush pending events immediately.
    /// </summary>
    Task FlushAsync(CancellationToken ct = default);

    /// <summary>
    /// Enable/disable analytics tracking (for privacy/consent).
    /// </summary>
    void SetTrackingEnabled(bool enabled);

    // ===== Configuration =====

    /// <summary>
    /// Set global context properties for all events.
    /// </summary>
    void SetGlobalContext(string key, object value);

    /// <summary>
    /// Remove a global context property.
    /// </summary>
    void RemoveGlobalContext(string key);
}
```

### Data Models

```csharp
/// <summary>
/// Economy event types for in-game transactions.
/// </summary>
public enum EconomyEventType
{
    Purchase,   // Player bought something
    Earn,       // Player earned currency/item
    Spend,      // Player spent currency/item
    Refund      // Transaction refunded
}

/// <summary>
/// Analytics event with rich context.
/// </summary>
public class AnalyticsEvent
{
    public string EventName { get; set; } = string.Empty;
    public string EventType { get; set; } = "track"; // track, page, identify, etc.
    public DateTimeOffset Timestamp { get; set; } = DateTimeOffset.UtcNow;
    public string? UserId { get; set; }
    public string? AnonymousId { get; set; }
    public string? SessionId { get; set; }
    public Dictionary<string, object> Properties { get; set; } = new();
    public Dictionary<string, object> Context { get; set; } = new();
}

/// <summary>
/// Session information.
/// </summary>
public class AnalyticsSession
{
    public string SessionId { get; set; } = Guid.NewGuid().ToString();
    public DateTimeOffset StartTime { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? EndTime { get; set; }
    public int EventCount { get; set; }
    public string? UserId { get; set; }
    public string Platform { get; set; } = "Console";
}

/// <summary>
/// Analytics configuration.
/// </summary>
public class AnalyticsOptions
{
    /// <summary>Batch size before auto-flush. Default: 20.</summary>
    public int BatchSize { get; set; } = 20;

    /// <summary>Flush interval. Default: 10 seconds.</summary>
    public TimeSpan FlushInterval { get; set; } = TimeSpan.FromSeconds(10);

    /// <summary>Enable offline queueing. Default: true.</summary>
    public bool EnableOfflineQueue { get; set; } = true;

    /// <summary>Maximum queue size. Default: 1000.</summary>
    public int MaxQueueSize { get; set; } = 1000;

    /// <summary>Enable sampling (0.0-1.0). Default: 1.0 (100%).</summary>
    public double SamplingRate { get; set; } = 1.0;

    /// <summary>Enable PII scrubbing. Default: true.</summary>
    public bool ScrubPII { get; set; } = true;

    /// <summary>Analytics backend provider.</summary>
    public string Provider { get; set; } = "ApplicationInsights"; // or "Segment", "Custom"
}
```

## Architecture

### Component Diagram

```
┌─────────────────────────────────────────────────────────────┐
│                       Game Code                              │
│  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐      │
│  │ DungeonGame  │  │ Scene System │  │  Input       │      │
│  │   Logic      │  │   Manager    │  │  Handler     │      │
│  └──────┬───────┘  └──────┬───────┘  └──────┬───────┘      │
│         │                 │                  │              │
│         └─────────────────┼──────────────────┘              │
│                           ↓                                 │
│               ┌──────────────────────┐                      │
│               │  IAnalyticsService   │ (Contract)           │
│               └──────────┬───────────┘                      │
└───────────────────────────┼──────────────────────────────────┘
                            ↓
                ┌───────────────────────┐
                │  AnalyticsService     │ (Implementation)
                │                       │
                │  • Event Queue        │
                │  • Batch Processor    │
                │  • Session Manager    │
                │  • Context Builder    │
                └───────────┬───────────┘
                            ↓
            ┌───────────────┴───────────────┐
            ↓                               ↓
    ┌──────────────────┐          ┌──────────────────┐
    │ Analytics Backend│          │  Local Storage   │
    │   Adapters       │          │    (Offline)     │
    │                  │          └──────────────────┘
    │ • App Insights   │
    │ • Segment        │
    │ • Mixpanel       │
    │ • Custom API     │
    └──────────────────┘
```

### Event Flow

1. **Event Creation**: Game code calls `TrackEventAsync("level_completed", props)`
2. **Enrichment**: Service adds session ID, user ID, global context, timestamp
3. **Validation**: Check sampling rate, PII scrubbing, consent
4. **Queueing**: Add to in-memory queue (or offline storage)
5. **Batching**: Group events until batch size or flush interval reached
6. **Transmission**: Send batch to backend (Application Insights, Segment, etc.)
7. **Retry**: Handle failures with exponential backoff (using IResilienceService)

## Implementation Plan

### Phase 1: Core Analytics Service (Week 1)

**Deliverables:**
- ✅ `WingedBean.Contracts.Analytics` - Interface contracts
- ✅ `WingedBean.Plugins.Analytics` - Base implementation with:
  - Event queue and batching
  - Session management
  - Context enrichment
  - PII scrubbing
- ✅ In-memory backend (for testing)

**Files:**
```
framework/src/WingedBean.Contracts.Analytics/
  ├── IAnalyticsService.cs
  ├── AnalyticsEvent.cs
  ├── AnalyticsSession.cs
  ├── AnalyticsOptions.cs
  └── EconomyEventType.cs

console/src/plugins/WingedBean.Plugins.Analytics/
  ├── AnalyticsService.cs
  ├── EventQueue.cs
  ├── SessionManager.cs
  ├── ContextBuilder.cs
  └── PIIScrubber.cs
```

### Phase 2: Backend Adapters (Week 2)

**Deliverables:**
- ✅ Application Insights adapter
- ✅ Segment adapter
- ✅ File-based backend (for local testing)
- ✅ Backend factory pattern

**Files:**
```
console/src/plugins/WingedBean.Plugins.Analytics/Backends/
  ├── IAnalyticsBackend.cs
  ├── ApplicationInsightsBackend.cs
  ├── SegmentBackend.cs
  └── FileBackend.cs
```

### Phase 3: Game Integration (Week 3)

**Deliverables:**
- ✅ DungeonGame analytics integration
- ✅ Standard game events (level_start, level_complete, player_death, etc.)
- ✅ Performance tracking (FPS, render time)
- ✅ Funnel tracking (onboarding, tutorial)

### Phase 4: Advanced Features (Week 4)

**Deliverables:**
- ✅ A/B testing support
- ✅ Feature flags integration
- ✅ Cohort analysis
- ✅ Revenue analytics (for future monetization)

## Standard Game Events

### Player Events
- `player_spawn` - Player spawned in level
- `player_death` - Player died
- `player_respawn` - Player respawned
- `player_level_up` - Player leveled up

### Progression Events
- `level_start` - Level started
- `level_complete` - Level completed
- `level_fail` - Level failed
- `achievement_unlocked` - Achievement earned
- `milestone_reached` - Progression milestone

### Combat Events
- `enemy_spawn` - Enemy spawned
- `enemy_killed` - Enemy defeated
- `damage_dealt` - Damage inflicted
- `damage_taken` - Damage received

### Economy Events
- `item_purchased` - Item bought
- `item_equipped` - Item equipped
- `currency_earned` - Currency gained
- `currency_spent` - Currency used

### Performance Events
- `fps_sample` - Frame rate measurement
- `load_time` - Scene/level load duration
- `memory_usage` - Memory snapshot

### Session Events
- `session_start` - Session began
- `session_end` - Session ended
- `app_foreground` - App activated
- `app_background` - App backgrounded

## Privacy & Compliance

### GDPR/CCPA Compliance

1. **Consent Management**:
   ```csharp
   analytics.SetTrackingEnabled(userConsented);
   ```

2. **PII Scrubbing**:
   - Remove email, IP, device IDs
   - Hash user identifiers
   - Configurable scrubbing rules

3. **Data Retention**:
   - Automatic deletion after 90 days (configurable)
   - User data export API
   - Right to be forgotten

4. **Opt-out**:
   ```csharp
   analytics.SetTrackingEnabled(false);
   await analytics.FlushAsync(); // Send pending, then stop
   ```

## Integration with Existing Services

### Resilience Service
Use `IResilienceService` for backend retries:
```csharp
await resilience.ExecuteAsync("analytics-backend", async ct =>
{
    await backend.SendBatchAsync(events, ct);
});
```

### Diagnostics Service (RFC-0031)
Send analytics events to diagnostics for debugging:
```csharp
diagnostics.LogEvent("analytics_event_tracked", eventName);
```

### Logging
Structured logging for analytics pipeline:
```csharp
logger.LogInformation("Analytics event tracked: {EventName} (Session: {SessionId})",
    eventName, sessionId);
```

## Testing Strategy

### Unit Tests
- Event queue behavior (batching, overflow)
- Session management (start, end, timeout)
- PII scrubbing rules
- Context enrichment

### Integration Tests
- Backend adapters (Application Insights, Segment)
- Offline queueing and replay
- Resilience integration (retries)

### E2E Tests
- Full game flow with analytics
- Session lifecycle
- Funnel completion tracking

## Metrics & Monitoring

### Service Health Metrics
- Events per second
- Queue size
- Batch success rate
- Backend latency

### Business Metrics
- DAU/MAU
- Session duration (avg, p50, p95)
- Retention (D1, D7, D30)
- Funnel conversion rates

## Alternatives Considered

### Alternative 1: Use ILogger for Analytics

**Rejected because:**
- ILogger is for diagnostics, not product analytics
- No session management or user identification
- No backend integrations (Application Insights, Segment)
- No batching or offline support

### Alternative 2: Direct Application Insights Integration

**Rejected because:**
- Tight coupling to Microsoft ecosystem
- Cannot switch backends (Segment, Mixpanel, etc.)
- Less control over event structure
- No unified contract for testing

### Alternative 3: Third-party SDK (Segment, Amplitude)

**Partially Adopted:**
- Use adapters to integrate third-party SDKs
- Keep WingedBean contract as abstraction
- Allows multi-backend support

## Dependencies

- **Polly** (via IResilienceService) - Retry policies for backend calls
- **Microsoft.ApplicationInsights** (optional) - Application Insights backend
- **Segment.Analytics.NET** (optional) - Segment backend
- **System.Text.Json** - Event serialization

## Security Considerations

1. **PII Protection**: Hash/scrub sensitive data before transmission
2. **API Keys**: Store backend API keys securely (Azure Key Vault, environment variables)
3. **HTTPS Only**: All backend communication over TLS
4. **Rate Limiting**: Prevent analytics abuse/DoS
5. **Input Validation**: Sanitize event properties to prevent injection

## Performance Considerations

1. **Async-First**: All operations are async to avoid blocking game loop
2. **Batching**: Reduce network overhead with batched sends
3. **Sampling**: Support sampling to reduce volume (e.g., 10% of users)
4. **Memory Limits**: Cap queue size to prevent memory bloat
5. **Background Processing**: Event sending on background thread

## Success Criteria

- ✅ Event latency < 100ms (p95)
- ✅ Event loss < 0.1% (with resilience)
- ✅ Memory overhead < 10MB
- ✅ Zero impact on game FPS
- ✅ Backend failover < 1 second
- ✅ GDPR/CCPA compliant

## Questions

1. **Which analytics backend should be default?**
   - **Recommendation**: Application Insights (Azure native, good free tier)
   - **Alternative**: File-based for local dev

2. **Should we support multiple backends simultaneously?**
   - **Recommendation**: Yes, for gradual migration and redundancy

3. **How to handle offline analytics?**
   - **Recommendation**: SQLite queue with background sync

4. **Should analytics be opt-in or opt-out?**
   - **Recommendation**: Opt-out with clear consent UI

## References

- [Application Insights](https://learn.microsoft.com/en-us/azure/azure-monitor/app/app-insights-overview)
- [Segment Spec](https://segment.com/docs/connections/spec/)
- [GDPR Analytics](https://gdpr.eu/cookies/)
- [Game Analytics Best Practices](https://www.gamedeveloper.com/business/game-analytics-best-practices)

## Approval

- [ ] Architecture approved
- [ ] Privacy/compliance reviewed
- [ ] Performance benchmarks met
- [ ] Integration plan validated

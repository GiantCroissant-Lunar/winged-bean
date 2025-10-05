---
id: RFC-ALIGNMENT-ANALYSIS
title: RFC Alignment Analysis - Observability Stack
status: Review Required
category: documentation
created: 2025-01-10
updated: 2025-01-11
---

# RFC Alignment Analysis: Observability Stack (RFC-0030 to RFC-0035)

**Date**: October 5, 2025  
**Author**: GitHub Copilot  
**Status**: Review Required

## Executive Summary

This document analyzes the alignment between the WingedBean observability RFCs created by two different agents:

- **Agent 1** (Oct 5, 2025): Created RFC-0030 (Analytics), RFC-0031 (Diagnostics), RFC-0032 (NuGet.Versioning)
- **Agent 2** (Jan 10, 2025): Created RFC-0033 (Observability Strategy), RFC-0034 (Sentry), RFC-0035 (OpenTelemetry)

**Key Finding**: There is **significant overlap and potential conflict** in architecture approaches that must be reconciled before implementation.

---

## Architecture Comparison

### Proposed Stacks

#### Agent 1 Approach (RFC-0030/0031)
```
Application Layer
    ↓
IAnalyticsService (Contract)  IDiagnosticsService (Contract)
    ↓                              ↓
AnalyticsService (Impl)       DiagnosticsService (Impl)
    ↓                              ↓
Backend Adapters               Backend Adapters
    ↓                              ↓
App Insights / Segment        Sentry / OpenTelemetry
```

**Philosophy**: WingedBean owns high-level abstractions (IAnalyticsService, IDiagnosticsService) with pluggable backend adapters.

#### Agent 2 Approach (RFC-0033/0034/0035)
```
Application Layer
    ↓
Sentry SDK (Errors)    OpenTelemetry SDK (Traces/Metrics)
    ↓                          ↓
Sentry Backend         OTEL Collector → Jaeger/Prometheus
```

**Philosophy**: Direct use of industry-standard SDKs (Sentry, OpenTelemetry) without abstraction layer.

---

## Key Conflicts

### 1. Error Tracking Duplication

**Conflict**: Three different error tracking mechanisms proposed:

| RFC | Service | Purpose | Backend |
|-----|---------|---------|---------|
| RFC-0030 | `IAnalyticsService.TrackErrorAsync()` | Business/product errors | App Insights / Segment |
| RFC-0031 | `IDiagnosticsService.TrackErrorAsync()` | System/technical errors | Sentry / OpenTelemetry |
| RFC-0034 | Sentry SDK direct | Crash reports, exceptions | Sentry |

**Recommendation**: 
- **Analytics**: Track business error events (e.g., "payment_failed", "level_incomplete")
- **Diagnostics**: Track system exceptions with breadcrumbs (wraps Sentry SDK)
- **Sentry**: Backend for all exception tracking (used by Diagnostics)

**Action**: Update RFC-0031 to specify `IDiagnosticsService.TrackErrorAsync()` delegates to Sentry SDK.

---

### 2. Distributed Tracing Overlap

**Conflict**: Two tracing approaches:

| RFC | Approach | API | Backend |
|-----|----------|-----|---------|
| RFC-0031 | `IDiagnosticsService.StartTrace()` | Custom abstraction | Sentry / OpenTelemetry |
| RFC-0035 | OpenTelemetry SDK | Standard OTEL API | OTEL Collector → Jaeger |

**Recommendation**: 
- **Use OpenTelemetry as the source of truth** for distributed tracing
- `IDiagnosticsService.StartTrace()` should be a **thin wrapper** around `Activity` API (.NET) or OTEL span creation
- Benefits: Standards compliance, ecosystem compatibility, no vendor lock-in

**Action**: Update RFC-0031 to specify it wraps `System.Diagnostics.Activity` and OTEL primitives.

---

### 3. Backend Confusion

**Conflict**: RFC-0030/0031 mention different backends than RFC-0034/0035:

| Service | RFC-0030/0031 Backends | RFC-0034/0035 Backends | Status |
|---------|------------------------|------------------------|---------|
| **Analytics** | App Insights, Segment | ❌ Not mentioned | Needs alignment |
| **Diagnostics** | Sentry, OpenTelemetry | ❌ Not mentioned | Needs alignment |
| **Error Tracking** | ❌ Not mentioned | Sentry | New (RFC-0034) |
| **Tracing** | ❌ Not mentioned | OpenTelemetry → Jaeger | New (RFC-0035) |

**Recommendation**: Unified backend strategy:
- **Sentry**: Error tracking (all exceptions, crashes)
- **OpenTelemetry**: Distributed tracing and metrics
  - Traces → Jaeger (self-hosted) or Honeycomb (SaaS)
  - Metrics → Prometheus (self-hosted)
- **Analytics**: Separate concern (product/business data)
  - Use Segment or App Insights (not for system observability)

**Action**: Update OBSERVABILITY-STACK.md to clarify backend separation.

---

### 4. Contract Interface Ambiguity

**Question**: Are `IAnalyticsService` and `IDiagnosticsService` supposed to be:
1. **High-level facades** over Sentry/OTEL SDKs? (Abstraction layer)
2. **Independent implementations** with adapters to Sentry/OTEL? (Plugin pattern)
3. **Direct OTEL wrappers** with WingedBean-specific helpers? (Thin wrapper)

**Current State**: RFC-0030/0031 suggest independent implementations, RFC-0034/0035 suggest direct SDK usage.

**Recommendation**: **Thin Wrapper Approach** (Option 3)

```csharp
namespace WingedBean.Contracts.Diagnostics;

/// <summary>
/// Diagnostics service wrapping OpenTelemetry and Sentry SDKs.
/// Provides WingedBean-specific conventions and helpers.
/// </summary>
public interface IDiagnosticsService
{
    // Wraps System.Diagnostics.Activity (OTEL-compatible)
    IDisposable StartTrace(string operationName, IDictionary<string, object>? tags = null);
    
    // Delegates to Sentry SDK
    Task TrackErrorAsync(Exception exception, ErrorSeverity severity, IDictionary<string, object>? context = null);
    
    // Custom health checks (not part of OTEL/Sentry)
    void RegisterHealthCheck(string name, Func<CancellationToken, Task<HealthCheckResult>> check);
}
```

**Benefits**:
- Leverages standard SDKs (battle-tested)
- No duplication of OTEL functionality
- WingedBean-specific features (health checks, conventions) added on top
- Easy to migrate to new OTEL versions

**Action**: Update RFC-0031 to specify "thin wrapper over OTEL + Sentry" approach.

---

## Proposed Unified Architecture

```
┌─────────────────────────────────────────────────────────────┐
│                    Application Layer                         │
│                (Game, Plugins, Host, Services)               │
└────────────┬──────────────────────────┬──────────────────────┘
             │                          │
             │                          │
    ┌────────▼────────┐        ┌───────▼─────────┐
    │ IAnalytics      │        │ IDiagnostics    │
    │ Service         │        │ Service         │
    │                 │        │                 │
    │ (Product data)  │        │ (System data)   │
    └────────┬────────┘        └───────┬─────────┘
             │                         │
             │                    ┌────┴──────┐
             │                    │           │
             ↓                    ↓           ↓
    ┌────────────────┐   ┌─────────────┐ ┌─────────────┐
    │ Segment/       │   │ Sentry SDK  │ │ OTEL SDK    │
    │ App Insights   │   │ (Errors)    │ │ (Traces)    │
    └────────────────┘   └──────┬──────┘ └──────┬──────┘
                                │               │
                                │               │
                                ↓               ↓
                         ┌───────────┐  ┌──────────────┐
                         │  Sentry   │  │ OTEL Collector│
                         │  Backend  │  │ → Jaeger      │
                         └───────────┘  │ → Prometheus  │
                                        └──────────────┘
```

### Layer Responsibilities

| Layer | Responsibility | Examples |
|-------|----------------|----------|
| **Application** | Business logic, user interactions | Game code, plugin code |
| **Contract** | WingedBean-specific abstractions | IAnalyticsService, IDiagnosticsService |
| **Implementation** | Thin wrappers + helpers | Delegates to SDKs, adds conventions |
| **SDK** | Industry-standard instrumentation | Sentry, OpenTelemetry |
| **Backend** | Data storage and querying | Sentry SaaS, Jaeger, Prometheus |

---

## Reconciliation Plan

### Immediate Actions

1. **Update RFC-0030 (Analytics)**
   - Remove overlap with system diagnostics
   - Focus on product/business metrics
   - Clarify backend: Segment or App Insights (not Sentry)
   - Reference RFC-0034/0035 for error tracking

2. **Update RFC-0031 (Diagnostics)**
   - Change status to "Superseded by RFC-0034/0035" OR
   - Redefine as "thin wrapper over OTEL + Sentry"
   - Remove custom tracing (use OTEL `Activity` API)
   - Keep health checks (WingedBean-specific feature)

3. **Update RFC-0033 (Observability Strategy)**
   - Add reference to RFC-0030/0031 for context
   - Clarify relationship: "RFC-0030/0031 define contracts, RFC-0034/0035 define implementation"

4. **Update RFC-0034 (Sentry)**
   - Add section: "Integration with IDiagnosticsService"
   - Show how `IDiagnosticsService.TrackErrorAsync()` delegates to Sentry

5. **Update RFC-0035 (OpenTelemetry)**
   - Add section: "Integration with IDiagnosticsService"
   - Show how `IDiagnosticsService.StartTrace()` wraps OTEL spans

6. **Update OBSERVABILITY-STACK.md**
   - Add RFC-0033, RFC-0034, RFC-0035 to overview
   - Update architecture diagram to show unified approach
   - Add "Implementation Strategy" section clarifying thin wrapper approach

---

## Implementation Sequence

### Phase 1: Foundation (Weeks 1-2)
- ✅ RFC-0032: NuGet.Versioning migration (Already complete)
- 🔄 Update RFCs per reconciliation plan
- 📝 Create unified configuration schema
- 🧪 Validate approach with prototype

### Phase 2: Diagnostics Core (Weeks 3-4)
- Implement `IDiagnosticsService` as thin wrapper
- Integrate Sentry SDK (RFC-0034)
- Integrate OpenTelemetry SDK (RFC-0035)
- Add health checks (WingedBean-specific)

### Phase 3: Analytics Core (Weeks 5-6)
- Implement `IAnalyticsService` 
- Integrate Segment or App Insights
- Add session management
- Add funnel tracking

### Phase 4: Multi-Language Support (Weeks 7-8)
- Python instrumentation
- Node.js instrumentation
- Unity instrumentation (carefully tested)

---

## Configuration Schema

### Unified Config Structure

```yaml
# config/observability/observability.yaml
observability:
  # Strategy-level config (RFC-0033)
  environment: production
  service_name: winged-bean
  service_version: 1.0.0
  
  # Analytics (RFC-0030)
  analytics:
    enabled: true
    provider: segment  # or app-insights
    batch_size: 20
    flush_interval: 10s
    sampling_rate: 1.0
    scrub_pii: true
  
  # Diagnostics (RFC-0031, implemented via RFC-0034/0035)
  diagnostics:
    enabled: true
    
    # Error tracking via Sentry (RFC-0034)
    error_tracking:
      provider: sentry
      sample_rate: 1.0  # Always capture errors
      traces_sample_rate: 0.1
      attach_stacktrace: true
    
    # Distributed tracing via OTEL (RFC-0035)
    tracing:
      provider: opentelemetry
      traces_sample_rate: 0.1
      exporter: otlp  # or jaeger, zipkin
      endpoint: http://localhost:4317
    
    # Health checks (WingedBean-specific)
    health_checks:
      enabled: true
      interval: 30s
  
  # Security (R-SEC-030)
  pii_redaction:
    enabled: true
    scrub_fields:
      - email
      - ip_address
      - username
      - device_id

# Secrets (SOPS encrypted)
# config/observability/secrets/production.enc.yaml
secrets:
  analytics:
    segment_write_key: <encrypted>
  diagnostics:
    sentry_dsn: <encrypted>
    otel_api_key: <encrypted>
```

---

## Questions for Resolution

### Critical Questions

1. **Architecture Decision**: 
   - Should `IDiagnosticsService` be a thin wrapper over OTEL/Sentry, or a full abstraction?
   - **Recommendation**: Thin wrapper (Option 3 above)

2. **Analytics Backend**: 
   - Segment (multi-destination) or App Insights (Azure-native)?
   - **Recommendation**: Start with App Insights (simpler), add Segment adapter later

3. **OTEL Backend**: 
   - Self-hosted Jaeger (free) or managed Honeycomb (paid)?
   - **Recommendation**: Start with Jaeger, evaluate Honeycomb after 3 months

4. **Contract Scope**:
   - Should RFC-0030/0031 contracts remain as high-level abstractions?
   - **Recommendation**: Yes, but clarify they are **facades**, not reimplementations

### Open Questions

5. **Unity OTEL Support**: 
   - Can OpenTelemetry SDK work with Unity IL2CPP?
   - **Action**: Prototype and test (RFC-0035 acknowledges this risk)

6. **PII Redaction Implementation**:
   - Should scrubbing be centralized or per-SDK?
   - **Recommendation**: Centralized utility with SDK-specific hooks

7. **Multi-Tenant Support**:
   - Do we need per-user or per-tenant observability isolation?
   - **Action**: Defer until requirements are clear

---

## Success Criteria

### Alignment Success
- [ ] All RFCs reference each other correctly
- [ ] No conflicting architecture approaches
- [ ] Clear implementation path from contracts to SDKs
- [ ] Unified configuration schema documented

### Technical Success
- [ ] `IDiagnosticsService` successfully wraps OTEL + Sentry
- [ ] `IAnalyticsService` works with Segment/App Insights
- [ ] PII redaction validated across all services
- [ ] Multi-language support (C#, Python, Node.js, Unity)

### Operational Success
- [ ] <5% performance overhead
- [ ] <$200/month observability costs (initial)
- [ ] Errors visible in Sentry within 5 seconds
- [ ] Traces visible in Jaeger within 10 seconds

---

## Conclusion

**Status**: RFC-0033/0034/0035 provide **excellent infrastructure choices** (Sentry + OpenTelemetry), but they need to be **aligned with** RFC-0030/0031's contract-based architecture.

**Recommendation**: 
1. **Keep both approaches**: RFC-0030/0031 define WingedBean contracts, RFC-0034/0035 define implementations
2. **Clarify relationship**: Contracts are thin wrappers/facades over industry-standard SDKs
3. **Update cross-references**: All RFCs should reference each other clearly
4. **Unified config**: Create single observability configuration covering all services

**Next Steps**: 
- Update all RFCs per reconciliation plan
- Create prototype of thin wrapper approach
- Validate Unity IL2CPP compatibility
- Begin Phase 2 implementation

---

**Approval Required From**:
- Architecture review team
- Security team (PII redaction validation)
- DevOps team (infrastructure/cost approval)

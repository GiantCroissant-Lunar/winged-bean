---
id: RFC-0033
title: Observability Strategy Foundation
status: Draft
category: infra
created: 2025-01-10
updated: 2025-10-05
related-rfcs: RFC-0030, RFC-0031, RFC-0034, RFC-0035
---

# RFC-0033: Observability Strategy Foundation

## Status

Draft

## Date

2025-01-10 (Updated 2025-10-05)

## Summary

Establish a comprehensive observability strategy for Winged Bean's multi-language architecture (C#/.NET/Unity, Python, Node.js) using Sentry for error tracking and OpenTelemetry for distributed tracing, metrics, and structured logging. This RFC defines the architectural foundation, principles, and phased rollout approach.

**Relationship to Other RFCs**: This RFC defines the **implementation strategy** for:
- **RFC-0030**: Analytics Service contract (product/business metrics)
- **RFC-0031**: Diagnostics Service contract (system observability)

The strategy is implemented via:
- **RFC-0034**: Sentry Integration (error tracking backend)
- **RFC-0035**: OpenTelemetry Integration (tracing/metrics backend)

## Motivation

### Problems to Solve

1. **Visibility Gap**: Currently no centralized error tracking or performance monitoring across services
2. **Multi-Language Challenges**: Need unified observability across .NET, Python, Node.js, and Unity
3. **Production Debugging**: Difficult to diagnose issues without structured telemetry
4. **Performance Optimization**: No visibility into distributed system performance bottlenecks
5. **User Impact Assessment**: Cannot correlate errors with user experience or specific deployments

### Goals

- **Unified Observability**: Single pane of glass for errors, traces, and metrics
- **Multi-Language Support**: Consistent instrumentation across all project languages
- **Security First**: Comply with R-SEC-030 (PII redaction) and R-SEC-010 (secret management)
- **Cost-Effective**: Start with free tiers, implement smart sampling strategies
- **Developer Experience**: Easy to instrument new code, low overhead

## Proposal

### Architecture Overview

```
┌─────────────────────────────────────────────────────────────┐
│                    Application Layer                         │
│  ┌──────────┐  ┌──────────┐  ┌──────────┐  ┌──────────┐   │
│  │   .NET   │  │  Unity   │  │  Python  │  │ Node.js  │   │
│  │ Console  │  │   Host   │  │ Services │  │   PTY    │   │
│  └────┬─────┘  └────┬─────┘  └────┬─────┘  └────┬─────┘   │
└───────┼─────────────┼─────────────┼─────────────┼──────────┘
        │             │             │             │
        │   ┌─────────┴─────────────┴─────────────┘
        │   │
        ▼   ▼
┌─────────────────────────────────────────────────────────────┐
│              Observability Instrumentation                   │
│  ┌──────────────────────┐  ┌──────────────────────┐        │
│  │   Sentry SDK         │  │  OpenTelemetry SDK   │        │
│  │  - Error tracking    │  │  - Distributed traces│        │
│  │  - Crash reports     │  │  - Metrics           │        │
│  │  - Release tracking  │  │  - Structured logs   │        │
│  └──────────┬───────────┘  └──────────┬───────────┘        │
└─────────────┼──────────────────────────┼────────────────────┘
              │                          │
              ▼                          ▼
        ┌──────────┐            ┌──────────────┐
        │  Sentry  │            │ OTEL Backend │
        │ Backend  │            │ (TBD: Self-  │
        │          │            │  hosted/SaaS)│
        └──────────┘            └──────────────┘
```

### Dual-Tool Strategy

**Sentry** (Error Tracking)
- Purpose: Error aggregation, crash reports, release tracking
- Strengths: Excellent UI, alerting, issue grouping, source maps
- Use Cases: Application errors, exceptions, user-facing issues

**OpenTelemetry** (Performance & Traces)
- Purpose: Distributed tracing, metrics, structured logging
- Strengths: Vendor-neutral, comprehensive instrumentation, correlation
- Use Cases: Performance monitoring, service dependencies, debugging

### Phased Rollout

#### Phase 1: Foundation (Weeks 1-2)
- **Infrastructure Setup**
  - Create Sentry organization and projects
  - Evaluate OTEL backend options (self-hosted Jaeger vs SaaS)
  - Set up SOPS-encrypted configuration per R-SEC-010
  - Define environment strategy (dev/staging/prod)

- **Documentation**
  - Create observability architecture guide
  - Document semantic conventions and naming standards
  - Define PII redaction policies per R-SEC-030

- **Configuration Management**
  - Add centralized config for DSNs and endpoints
  - Implement environment-based sampling rates
  - Set up secret rotation procedures

#### Phase 2: Non-Unity Services (Weeks 3-4)
- **Node.js PTY Service** (Easiest first)
  - Add Sentry JavaScript SDK
  - Integrate OpenTelemetry Node.js SDK
  - Implement trace context propagation

- **Python Scripts & Services**
  - Add Sentry Python SDK
  - Integrate OpenTelemetry Python SDK
  - Instrument key workflows

#### Phase 3: .NET Console Applications (Weeks 5-6)
- **.NET Host Applications**
  - Add Sentry .NET SDK
  - Integrate OpenTelemetry .NET SDK
  - Use Microsoft.Extensions.Logging integration
  - Test with plugin architecture (AssemblyLoadContext)

#### Phase 4: Unity Integration (Weeks 7-8)
- **Unity Host (Careful Testing)**
  - Add Sentry Unity SDK (official support)
  - Evaluate OpenTelemetry compatibility with IL2CPP
  - Test on target platforms (iOS, Android, WebGL, Desktop)
  - Consider lightweight manual instrumentation if needed

### Configuration Structure

```
config/
├── observability/
│   ├── common.yaml              # Shared config (service names, versions)
│   ├── sentry.yaml              # Sentry project mappings
│   ├── opentelemetry.yaml       # OTEL collector endpoints
│   └── secrets/
│       ├── dev.enc.yaml         # SOPS encrypted (age)
│       ├── staging.enc.yaml
│       └── production.enc.yaml
```

### Semantic Conventions

Following OpenTelemetry semantic conventions with project-specific extensions:

```yaml
# Standard Attributes (all telemetry)
service.name: "winged-bean-{component}"
service.version: "{semver}"
deployment.environment: "dev|staging|production"

# Custom Attributes
wingedbean.component: "dotnet|python|nodejs|unity"
wingedbean.plugin.name: "{plugin-name}"
wingedbean.plugin.version: "{plugin-version}"
```

### Security & Privacy

**PII Redaction (R-SEC-030)**
- Scrub usernames, email addresses, IP addresses
- Implement beforeSend hooks in Sentry
- Use OTEL processors for data sanitization

**Secret Management (R-SEC-010)**
- Store DSNs and API keys in SOPS-encrypted files
- Never commit secrets to version control
- Use environment variables at runtime
- Document in secrets rotation runbook

**Data Retention**
- Development: 7 days
- Staging: 30 days
- Production: 90 days (configurable per compliance needs)

### Sampling Strategy

| Environment | Sentry Traces | OTEL Traces | OTEL Metrics |
|-------------|---------------|-------------|--------------|
| Development | 100%          | 100%        | 100%         |
| Staging     | 100%          | 50%         | 100%         |
| Production  | 100% (errors) | 10%         | 100%         |

Adjust based on volume and cost after initial deployment.

## Detailed Design

### Language-Specific Considerations

#### .NET/C#
- **Target Framework**: netstandard2.1 (Unity compat) + net8.0 (console)
- **Integration Point**: Microsoft.Extensions.Logging
- **Packages**: Add to `Directory.Packages.props`
  - Sentry.AspNetCore (console host)
  - OpenTelemetry.Extensions.Hosting
  - OpenTelemetry.Instrumentation.*

#### Unity
- **Constraints**: IL2CPP compilation, .NET Standard 2.1 API surface
- **Sentry**: Use official Unity SDK (tested with IL2CPP)
- **OpenTelemetry**: May require manual instrumentation
- **Testing**: Must validate on all target platforms

#### Python
- **Integration Point**: Standard logging module
- **Packages**: Add to `pyproject.toml`
  - sentry-sdk
  - opentelemetry-api, opentelemetry-sdk
  - opentelemetry-instrumentation-*

#### Node.js
- **Integration Point**: Winston/Pino loggers (if used)
- **Packages**: Add to workspace `package.json`
  - @sentry/node
  - @opentelemetry/sdk-node
  - @opentelemetry/auto-instrumentations-node

### Backend Decision Matrix

| Option | Pros | Cons | Cost |
|--------|------|------|------|
| **Sentry SaaS** | Easy setup, great UI | Limited free tier | $26/mo (dev) |
| **Self-hosted Sentry** | Free, full control | Maintenance overhead | Infrastructure |
| **Jaeger (self-hosted)** | Free, OTEL native | Basic UI, no managed | Infrastructure |
| **Honeycomb** | Excellent OTEL support | Cost scales with data | $0-$200+/mo |
| **Datadog** | Full observability suite | Expensive | $15+/host/mo |

**Recommendation**: Start with Sentry SaaS (free tier) + self-hosted Jaeger, evaluate migration to managed OTEL backend based on needs.

## Implementation Plan

### Deliverables

1. **RFC-0034**: Sentry Integration for Error Tracking
2. **RFC-0035**: OpenTelemetry Integration for Distributed Tracing
3. Execution plans for each RFC (per R-DOC-040)
4. Language-specific instrumentation libraries
5. Configuration templates and documentation
6. Monitoring dashboards and alerting rules

### Success Criteria

- [ ] All services emit structured telemetry
- [ ] Errors automatically create Sentry issues
- [ ] Distributed traces visible across service boundaries
- [ ] No PII leakage in telemetry data
- [ ] <5% performance overhead in production
- [ ] Developer onboarding docs complete

### Timeline

- **Week 1-2**: Foundation + RFC reviews
- **Week 3-4**: Node.js + Python implementation
- **Week 5-6**: .NET console implementation
- **Week 7-8**: Unity integration and testing
- **Week 9**: Documentation, training, rollout

## Alternatives Considered

### Single Tool Approach

**Option**: Use only Sentry (with Performance Monitoring)
- **Pros**: Simpler setup, single vendor
- **Cons**: Vendor lock-in, limited OTEL ecosystem benefits
- **Verdict**: Rejected - loses flexibility and open standards

**Option**: Use only OpenTelemetry
- **Pros**: Vendor-neutral, comprehensive
- **Cons**: Sentry's error grouping and UI are superior
- **Verdict**: Rejected - loses best-in-class error tracking

### Alternative Tools

- **Rollbar**: Similar to Sentry, less Unity support
- **Bugsnag**: Good mobile support, smaller ecosystem
- **Application Insights**: Strong .NET support, Azure lock-in
- **Elastic APM**: Good observability, more complex setup

**Verdict**: Sentry + OpenTelemetry provides best balance of features, ecosystem, and flexibility.

## Open Questions

1. **OTEL Backend**: Self-host Jaeger or use managed service? (Lean toward Jaeger initially)
2. **Unity IL2CPP**: Will OpenTelemetry SDKs work with IL2CPP? (Needs testing)
3. **Budget**: What's the monthly observability budget? (Affects sampling/retention)
4. **Alert Routing**: Where should alerts go? (Slack, PagerDuty, email?)
5. **Compliance**: Any specific data residency requirements? (Affects backend choice)

## Related RFCs

### Contract RFCs (Application-Facing)
- **RFC-0030**: Analytics Service - Product/business metrics contract
- **RFC-0031**: Diagnostics Service - System observability contract

### Implementation RFCs (This Strategy)
- **RFC-0034**: Sentry Integration for Error Tracking (implements RFC-0031 errors)
- **RFC-0035**: OpenTelemetry Integration for Distributed Tracing (implements RFC-0031 tracing)

### Architecture RFCs
- RFC-0002: Service Platform Core (4-Tier Architecture)
- RFC-0004: Project Organization and Folder Structure
- RFC-0032: NuGet.Versioning Migration

## References

- [OpenTelemetry Specification](https://opentelemetry.io/docs/specs/)
- [Sentry Documentation](https://docs.sentry.io/)
- [Unity IL2CPP Limitations](https://docs.unity3d.com/Manual/IL2CPP.html)
- R-SEC-010: Secret Management
- R-SEC-030: PII Redaction
- R-SEC-040: External API Timeouts

## Appendix

### Example Configuration

```yaml
# config/observability/common.yaml
version: 1.0.0
environments:
  - dev
  - staging
  - production

services:
  - name: winged-bean-console
    component: dotnet
  - name: winged-bean-pty
    component: nodejs
  - name: winged-bean-unity
    component: unity

sampling:
  dev:
    traces: 1.0
    errors: 1.0
  staging:
    traces: 0.5
    errors: 1.0
  production:
    traces: 0.1
    errors: 1.0
```

### Cost Estimation

**Initial Setup (Free Tier)**
- Sentry: Free tier (5k errors/mo, 50k transactions)
- Jaeger: Self-hosted ($10-20/mo infrastructure)
- Total: ~$20/mo

**Growth (After 6 months)**
- Sentry: Developer tier $26/mo
- Managed OTEL (if needed): $50-200/mo
- Total: $75-225/mo

---

*This RFC establishes the foundation. See RFC-0034 and RFC-0035 for detailed implementation specifications.*

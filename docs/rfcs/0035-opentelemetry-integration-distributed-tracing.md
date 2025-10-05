---
id: RFC-0035
title: OpenTelemetry Integration for Distributed Tracing
status: Draft
category: infra
created: 2025-01-10
updated: 2025-01-10
---

# RFC-0035: OpenTelemetry Integration for Distributed Tracing

## Status

Draft

## Date

2025-01-10

## Summary

Integrate OpenTelemetry (OTEL) across all Winged Bean components (C#/.NET, Unity, Python, Node.js) for distributed tracing, metrics collection, and structured logging with vendor-neutral instrumentation and flexible backend options.

## Motivation

### Problems to Solve

1. **Performance Blindness**: No visibility into service latencies and bottlenecks
2. **Distributed System Debugging**: Cannot trace requests across service boundaries
3. **Metrics Gap**: No structured metrics for system health monitoring
4. **Vendor Lock-in Risk**: Proprietary instrumentation limits backend flexibility
5. **Context Propagation**: Missing correlation between logs, traces, and metrics

### Goals

- **Distributed Tracing**: End-to-end request visibility across services
- **Vendor Neutrality**: Use OpenTelemetry standard for flexibility
- **Performance Metrics**: Track latencies, throughput, resource usage
- **Structured Logging**: Correlate logs with traces and spans
- **Context Propagation**: W3C Trace Context across all components

## Proposal

### Architecture Overview

\`\`\`
┌─────────────────────────────────────────────────────────────┐
│                    Application Layer                         │
│  ┌──────────┐  ┌──────────┐  ┌──────────┐  ┌──────────┐   │
│  │   .NET   │  │  Unity   │  │  Python  │  │ Node.js  │   │
│  │ Console  │  │   Host   │  │ Services │  │   PTY    │   │
│  └────┬─────┘  └────┬─────┘  └────┬─────┘  └────┬─────┘   │
└───────┼─────────────┼─────────────┼─────────────┼──────────┘
        │             │             │             │
        ▼             ▼             ▼             ▼
┌─────────────────────────────────────────────────────────────┐
│              OpenTelemetry SDK (per language)                │
│  ┌────────────────┐  ┌────────────────┐  ┌────────────────┐│
│  │    Tracing     │  │    Metrics     │  │     Logs       ││
│  │  - Spans       │  │  - Counters    │  │  - Structured  ││
│  │  - Propagation │  │  - Histograms  │  │  - Correlated  ││
│  └────────┬───────┘  └────────┬───────┘  └────────┬───────┘│
└───────────┼──────────────────────┼──────────────────┼────────┘
            └──────────────────────┼──────────────────┘
                                   │
                                   ▼
                    ┌──────────────────────────┐
                    │   OTEL Collector         │
                    │  (Optional aggregation)  │
                    └──────────────┬───────────┘
                                   │
            ┌──────────────────────┼──────────────────────┐
            │                      │                      │
            ▼                      ▼                      ▼
    ┌──────────────┐      ┌──────────────┐      ┌──────────────┐
    │    Jaeger    │      │  Prometheus  │      │    Loki      │
    │   (Traces)   │      │  (Metrics)   │      │    (Logs)    │
    └──────────────┘      └──────────────┘      └──────────────┘
\`\`\`

### Backend Options

**Phase 1: Self-Hosted (Cost-Effective)**
- **Traces**: Jaeger (Docker Compose)
- **Metrics**: Prometheus (Docker Compose)
- **Logs**: Loki (Docker Compose) or just structured to stdout

**Phase 2: Hybrid (If budget allows)**
- **Traces**: Honeycomb or Lightstep (better querying)
- **Metrics**: Self-hosted Prometheus
- **Logs**: Self-hosted Loki

**Decision Point**: Start with Phase 1, evaluate Phase 2 after 3 months based on needs and budget.

## Detailed Design

See full RFC file for complete implementation details covering:

- .NET/C# implementation with code examples
- Unity implementation challenges and solutions
- Python implementation with OTLP exporters
- Node.js auto-instrumentation setup
- Context propagation using W3C Trace Context
- Backend setup with Docker Compose
- Sampling strategies per environment
- Security considerations (PII redaction per R-SEC-030)

## Success Criteria

- [ ] All services emit traces to Jaeger
- [ ] Distributed traces visible across service boundaries
- [ ] W3C trace context propagated correctly
- [ ] Metrics exposed on Prometheus endpoints
- [ ] <5% performance overhead
- [ ] PII redacted from spans

## Related RFCs

- RFC-0033: Observability Strategy Foundation
- RFC-0034: Sentry Integration for Error Tracking

---

*This RFC depends on RFC-0033 (Observability Strategy Foundation).*

# Observability Implementation Summary

**Created**: 2025-01-10  
**Status**: Planning Phase  
**Related RFCs**: RFC-0033, RFC-0034, RFC-0035

## Overview

Three RFCs have been created to introduce comprehensive observability to Winged Bean's multi-language architecture.

## RFCs Created

### RFC-0033: Observability Strategy Foundation
**Path**: `docs/rfcs/0033-observability-strategy-foundation.md`  
**Category**: Infrastructure  
**Status**: Draft

**Purpose**: Establishes the architectural foundation and phased rollout strategy for observability across all project components.

**Key Decisions**:
- Dual-tool approach: Sentry (errors) + OpenTelemetry (traces/metrics)
- Phased rollout: Non-Unity → .NET Console → Unity (4 phases over 8 weeks)
- Self-hosted backends initially (Jaeger + Prometheus)
- Environment-based sampling strategies
- SOPS-encrypted configuration per R-SEC-010

**Architecture**:
```
Applications (C#, Unity, Python, Node.js)
    ↓
Sentry SDK + OpenTelemetry SDK
    ↓
Sentry Backend + OTEL Backends (Jaeger/Prometheus)
```

### RFC-0034: Sentry Integration for Error Tracking
**Path**: `docs/rfcs/0034-sentry-integration-error-tracking.md`  
**Category**: Infrastructure  
**Status**: Draft

**Purpose**: Detailed implementation specification for Sentry error tracking across all languages.

**Key Features**:
- Separate Sentry projects per language stack
- PII redaction hooks per R-SEC-030
- Release tracking and source map upload
- Unity-specific considerations (IL2CPP compatibility)
- Smart alert routing (Slack, email, PagerDuty)

**Cost Estimate**: $0/month (free tier) → $104/month (4 projects at Developer tier)

**Code Examples Included**:
- ✅ .NET/C# console host integration
- ✅ Unity initialization with IL2CPP considerations
- ✅ Python SDK setup with PII scrubbing
- ✅ Node.js Express middleware integration
- ✅ Release tracking scripts for all languages

### RFC-0035: OpenTelemetry Integration for Distributed Tracing
**Path**: `docs/rfcs/0035-opentelemetry-integration-distributed-tracing.md`  
**Category**: Infrastructure  
**Status**: Draft

**Purpose**: Vendor-neutral distributed tracing, metrics, and structured logging implementation.

**Key Features**:
- W3C Trace Context propagation across services
- Auto-instrumentation where available (Node.js, Python)
- Custom instrumentation for .NET plugin architecture
- Manual instrumentation approach for Unity (IL2CPP compatibility)
- Self-hosted backends (Jaeger, Prometheus, Grafana)

**Cost Estimate**: $40-60/month (self-hosted infrastructure) → $0-$200/month (if migrating to managed services)

**Code Examples Included**:
- ✅ .NET/C# with Microsoft.Extensions integration
- ✅ Unity manual OTLP export approach
- ✅ Python auto-instrumentation
- ✅ Node.js SDK initialization
- ✅ Docker Compose for local backends

## Implementation Phases

### Phase 1: Foundation (Weeks 1-2)
**Deliverables**:
- [ ] RFC reviews and approval
- [ ] Sentry organization and projects setup
- [ ] Self-hosted Jaeger/Prometheus deployment
- [ ] SOPS-encrypted configuration files
- [ ] Documentation: semantic conventions, PII policies

**Owner**: Infrastructure team  
**Blocker**: None

### Phase 2: Non-Unity Services (Weeks 3-4)
**Deliverables**:
- [ ] Node.js PTY service integration (Sentry + OTEL)
- [ ] Python scripts instrumentation
- [ ] Context propagation testing
- [ ] Local testing with Docker backends

**Owner**: Backend team  
**Blocker**: Phase 1 completion

### Phase 3: .NET Console Applications (Weeks 5-6)
**Deliverables**:
- [ ] .NET console host integration
- [ ] Plugin architecture compatibility testing
- [ ] Central package management updates
- [ ] Microsoft.Extensions.Logging integration

**Owner**: .NET team  
**Blocker**: Phase 2 completion

### Phase 4: Unity Integration (Weeks 7-8)
**Deliverables**:
- [ ] Sentry Unity SDK integration
- [ ] OTEL manual instrumentation (if full SDK incompatible)
- [ ] IL2CPP build testing (iOS, Android, WebGL, Desktop)
- [ ] Performance impact assessment

**Owner**: Unity team  
**Blocker**: Phase 3 completion, Unity IL2CPP testing results

## Configuration Structure

\`\`\`
config/
└── observability/
    ├── common.yaml              # Service names, versions, environments
    ├── sentry.yaml              # Sentry project mappings
    ├── opentelemetry.yaml       # OTEL endpoints, sampling
    └── secrets/
        ├── dev.enc.yaml         # SOPS encrypted (age)
        ├── staging.enc.yaml
        └── production.enc.yaml
\`\`\`

## Package Updates Required

### .NET (`Directory.Packages.props`)
\`\`\`xml
<ItemGroup Label="Observability">
  <!-- Sentry -->
  <PackageVersion Include="Sentry.AspNetCore" Version="4.15.0" />
  <PackageVersion Include="Sentry.Extensions.Logging" Version="4.15.0" />
  
  <!-- OpenTelemetry -->
  <PackageVersion Include="OpenTelemetry.Extensions.Hosting" Version="1.10.0" />
  <PackageVersion Include="OpenTelemetry.Instrumentation.Http" Version="1.10.0" />
  <PackageVersion Include="OpenTelemetry.Exporter.Jaeger" Version="1.5.1" />
  <PackageVersion Include="OpenTelemetry.Exporter.Prometheus.AspNetCore" Version="1.10.0" />
</ItemGroup>
\`\`\`

### Python (`pyproject.toml`)
\`\`\`toml
dependencies = [
    "sentry-sdk>=2.18.0",
    "opentelemetry-api>=1.28.0",
    "opentelemetry-sdk>=1.28.0",
    "opentelemetry-exporter-otlp>=1.28.0",
]
\`\`\`

### Node.js (`package.json`)
\`\`\`json
{
  "dependencies": {
    "@sentry/node": "^8.40.0",
    "@opentelemetry/sdk-node": "^0.56.0",
    "@opentelemetry/auto-instrumentations-node": "^0.52.0"
  }
}
\`\`\`

### Unity
- Sentry: Unity Package Manager (`https://github.com/getsentry/unity.git#4.15.0`)
- OpenTelemetry: Manual instrumentation (SDK incompatible with IL2CPP)

## Security Compliance

### R-SEC-010: Secret Management
- ✅ All DSNs and API keys in SOPS-encrypted files
- ✅ Never commit secrets to version control
- ✅ Environment variables at runtime
- ✅ Secret rotation procedures documented

### R-SEC-030: PII Redaction
- ✅ `beforeSend` hooks in Sentry (all languages)
- ✅ OTEL span processors for data sanitization
- ✅ Scrub: emails, IPs, usernames, auth headers
- ✅ Test PII redaction in integration tests

### R-SEC-040: External API Timeouts
- ✅ 5-second timeout for Sentry/OTEL exports
- ✅ Async export (non-blocking)
- ✅ Graceful degradation if backends unavailable

## Cost Analysis

### Initial Setup (Months 1-3)
| Component | Cost | Notes |
|-----------|------|-------|
| Sentry | $0/mo | Free tier (5k errors, 50k transactions) |
| Jaeger (AWS t3.small) | $15-20/mo | Self-hosted |
| Prometheus (same instance) | $0 | Shared with Jaeger |
| Storage (S3/EBS) | $10-20/mo | 30-day retention |
| **Total** | **$25-40/mo** | |

### Growth Phase (Months 4-12)
| Component | Cost | Notes |
|-----------|------|-------|
| Sentry (4 projects) | $104/mo | Developer tier |
| Jaeger/Prometheus | $40-60/mo | Larger instances + storage |
| **Total** | **$144-164/mo** | |

**Budget Approval Required**: ~$150-200/month ongoing cost

## Open Questions & Decisions Needed

1. **Budget Approval** ⚠️  
   - Is $150-200/month acceptable for observability tooling?
   - Alternative: Stay on free tiers with aggressive sampling

2. **Unity IL2CPP Testing** 🔬  
   - Can OpenTelemetry SDK work with IL2CPP, or must we use manual instrumentation?
   - Requires dedicated testing sprint before Phase 4

3. **OTEL Backend Strategy** 🏗️  
   - Self-host Jaeger initially, or start with managed service (Honeycomb free tier)?
   - Recommendation: Self-host for 3 months, then evaluate

4. **Alert Routing** 📢  
   - Should critical production alerts go to PagerDuty?
   - Alternative: Slack-only for now (no additional cost)

5. **Data Retention** 📦  
   - 90 days sufficient, or compliance requirements for longer?
   - Affects storage costs

## Next Steps

1. **RFC Review** (Week 1)
   - [ ] Team review of all three RFCs
   - [ ] Address open questions
   - [ ] Approve budget allocation

2. **Infrastructure Setup** (Week 1-2)
   - [ ] Create Sentry organization and projects
   - [ ] Deploy Jaeger/Prometheus with Docker Compose
   - [ ] Set up SOPS encryption keys
   - [ ] Create configuration templates

3. **Pilot Implementation** (Week 3)
   - [ ] Start with Node.js PTY service (simplest)
   - [ ] Validate end-to-end flow
   - [ ] Document learnings

4. **Rollout** (Weeks 4-8)
   - [ ] Python → .NET Console → Unity
   - [ ] Weekly checkpoint reviews
   - [ ] Update documentation continuously

## References

- **RFCs**:
  - [RFC-0033: Observability Strategy Foundation](../rfcs/0033-observability-strategy-foundation.md)
  - [RFC-0034: Sentry Integration](../rfcs/0034-sentry-integration-error-tracking.md)
  - [RFC-0035: OpenTelemetry Integration](../rfcs/0035-opentelemetry-integration-distributed-tracing.md)

- **External Documentation**:
  - [Sentry Documentation](https://docs.sentry.io/)
  - [OpenTelemetry Documentation](https://opentelemetry.io/docs/)
  - [W3C Trace Context](https://www.w3.org/TR/trace-context/)

- **Project Rules**:
  - R-SEC-010: Secret Management
  - R-SEC-030: PII Redaction
  - R-SEC-040: External API Timeouts

---

**Status**: Awaiting RFC approval and budget sign-off  
**Contact**: Infrastructure team lead  
**Last Updated**: 2025-01-10

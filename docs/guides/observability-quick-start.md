# Observability Quick Start Guide

**Status**: Planning Phase  
**Last Updated**: 2025-01-10

## Overview

This guide provides a quick reference for implementing Sentry and OpenTelemetry observability across Winged Bean's multi-language architecture.

## 📚 Documentation Structure

### RFCs (Detailed Specifications)
1. **[RFC-0033: Observability Strategy Foundation](../rfcs/0033-observability-strategy-foundation.md)**
   - High-level architecture and phased rollout
   - Backend selection and configuration management
   - Security and compliance requirements

2. **[RFC-0034: Sentry Integration](../rfcs/0034-sentry-integration-error-tracking.md)**
   - Error tracking implementation for all languages
   - PII redaction and release tracking
   - Alert configuration and cost analysis

3. **[RFC-0035: OpenTelemetry Integration](../rfcs/0035-opentelemetry-integration-distributed-tracing.md)**
   - Distributed tracing and metrics collection
   - W3C Trace Context propagation
   - Self-hosted backend setup

### Implementation Resources
- **[Implementation Summary](../implementation/observability-implementation-summary.md)**: Phase timeline, package updates, cost analysis
- **Quick Start Guide** (this document): Fast reference and code snippets

## 🚀 Quick Start: Local Development

### 1. Start Observability Backends

```bash
# Create Docker Compose file
cd infra/docker
mkdir -p observability
cd observability

# Create docker-compose.yml (see RFC-0035 for full config)
docker-compose up -d

# Access UIs
open http://localhost:16686  # Jaeger
open http://localhost:9090   # Prometheus
open http://localhost:3001   # Grafana
```

### 2. Set Environment Variables

```bash
# Development environment
export ENVIRONMENT=dev
export SENTRY_DSN="https://your-dsn@sentry.io/project"
export OTEL_ENDPOINT="http://localhost:4317"
```

### 3. Run Your Service

Choose your language:

#### .NET Console
```bash
cd development/dotnet/console
dotnet run
```

#### Node.js PTY Service
```bash
cd development/nodejs/pty-service
npm start
```

#### Python Scripts
```bash
cd development/python
python -m scripts.example_script
```

### 4. Generate Test Traffic

```bash
# Trigger some errors and traces
curl http://localhost:3000/api/test

# View in Jaeger
open http://localhost:16686

# View in Sentry
open https://sentry.io/organizations/winged-bean/issues/
```

## 📦 Package Installation

### .NET
Add to `Directory.Packages.props`:
```xml
<ItemGroup Label="Observability">
  <PackageVersion Include="Sentry.Extensions.Logging" Version="4.15.0" />
  <PackageVersion Include="OpenTelemetry.Extensions.Hosting" Version="1.10.0" />
  <PackageVersion Include="OpenTelemetry.Exporter.Jaeger" Version="1.5.1" />
</ItemGroup>
```

### Python
Add to `pyproject.toml`:
```toml
dependencies = [
    "sentry-sdk>=2.18.0",
    "opentelemetry-api>=1.28.0",
    "opentelemetry-sdk>=1.28.0",
]
```

### Node.js
```bash
pnpm add @sentry/node @opentelemetry/sdk-node @opentelemetry/auto-instrumentations-node
```

### Unity
```
Unity Package Manager → Add from Git URL:
https://github.com/getsentry/unity.git#4.15.0
```

## 💻 Code Examples

### Sentry: Capture Exception

**C#**
```csharp
using Sentry;

try {
    await DoSomethingRisky();
} catch (Exception ex) {
    SentrySdk.CaptureException(ex);
    throw;
}
```

**Python**
```python
import sentry_sdk

try:
    do_something_risky()
except Exception as e:
    sentry_sdk.capture_exception(e)
    raise
```

**JavaScript**
```javascript
import * as Sentry from "@sentry/node";

try {
    await doSomethingRisky();
} catch (error) {
    Sentry.captureException(error);
    throw error;
}
```

### OpenTelemetry: Create Span

**C#**
```csharp
using System.Diagnostics;

private static readonly ActivitySource ActivitySource = new("winged-bean");

public async Task ProcessData()
{
    using var activity = ActivitySource.StartActivity("ProcessData");
    activity?.SetTag("data.size", dataSize);
    
    await DoProcessing();
    
    activity?.SetStatus(ActivityStatusCode.Ok);
}
```

**Python**
```python
from opentelemetry import trace

tracer = trace.get_tracer(__name__)

def process_data():
    with tracer.start_as_current_span("process_data") as span:
        span.set_attribute("data.size", data_size)
        do_processing()
```

**JavaScript**
```javascript
import { trace } from "@opentelemetry/api";

const tracer = trace.getTracer("winged-bean-pty");

async function processData() {
    const span = tracer.startSpan("process_data");
    span.setAttribute("data.size", dataSize);
    
    try {
        await doProcessing();
        span.end();
    } catch (error) {
        span.recordException(error);
        span.end();
        throw error;
    }
}
```

## 🔒 Security Checklist

Per project rules:

- [ ] **R-SEC-010**: DSNs stored in SOPS-encrypted files
- [ ] **R-SEC-030**: PII scrubbing implemented (`beforeSend` hooks)
- [ ] **R-SEC-040**: Export timeouts configured (5 seconds)
- [ ] No secrets committed to version control
- [ ] Test PII redaction in integration tests

## 🧪 Testing

### Test Sentry Integration
```bash
# Send test event
./scripts/test-sentry.sh

# Verify in Sentry UI
echo "Check: https://sentry.io/organizations/winged-bean/"
```

### Test OpenTelemetry
```bash
# Generate test span
./scripts/test-otel.sh

# View in Jaeger
open http://localhost:16686
```

### Verify PII Redaction
```bash
# Run integration tests
dotnet test --filter "Category=Security"
pytest -k "test_pii_redaction"
npm test -- --grep "PII"
```

## 📊 Dashboards

### Sentry
- **Issues**: https://sentry.io/organizations/winged-bean/issues/
- **Performance**: https://sentry.io/organizations/winged-bean/performance/
- **Releases**: https://sentry.io/organizations/winged-bean/releases/

### Jaeger
- **Local**: http://localhost:16686
- **Search**: Filter by service name `winged-bean-*`

### Grafana
- **Local**: http://localhost:3001 (admin/admin)
- **Dashboards**: Import from `infra/docker/observability/grafana/dashboards/`

## 🐛 Troubleshooting

### Sentry events not showing up
```bash
# Check DSN is set
echo $SENTRY_DSN

# Check network connectivity
curl -I https://sentry.io

# Enable debug logging
export SENTRY_DEBUG=true
```

### OTEL traces not appearing
```bash
# Check Jaeger is running
docker ps | grep jaeger

# Check endpoint
echo $OTEL_ENDPOINT

# Test OTLP endpoint
curl http://localhost:4317
```

### Performance overhead too high
```bash
# Reduce sampling rate (environment config)
# dev: 1.0 → 0.5
# staging: 0.5 → 0.1
# production: 0.1 → 0.05

# Disable console exporters in production
# Remove .AddConsoleExporter() calls
```

## 📈 Cost Monitoring

### Current Usage
```bash
# Check Sentry quota
# Dashboard → Settings → Subscription

# Check storage usage (self-hosted)
docker exec -it jaeger df -h
docker exec -it prometheus df -h
```

### Optimization Tips
- Use sampling for traces (not errors)
- Filter noisy errors (network timeouts)
- Set appropriate retention periods
- Archive old issues in Sentry

## 🔄 Next Steps

1. **Review RFCs** → Get team feedback and approval
2. **Budget Sign-off** → ~$150-200/month ongoing cost
3. **Phase 1: Setup** → Deploy backends, configure secrets
4. **Phase 2: Node.js** → Start with PTY service
5. **Phase 3: Python** → Instrument scripts
6. **Phase 4: .NET** → Console applications
7. **Phase 5: Unity** → IL2CPP testing and integration

## 📞 Support

- **Questions**: Post in `#observability` Slack channel
- **Issues**: Create GitHub issue with `observability` label
- **RFCs**: See links at top of document

---

**Related Documents**:
- [RFC-0033: Strategy](../rfcs/0033-observability-strategy-foundation.md)
- [RFC-0034: Sentry](../rfcs/0034-sentry-integration-error-tracking.md)
- [RFC-0035: OpenTelemetry](../rfcs/0035-opentelemetry-integration-distributed-tracing.md)
- [Implementation Summary](../implementation/observability-implementation-summary.md)

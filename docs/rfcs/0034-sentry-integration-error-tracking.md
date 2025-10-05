---
id: RFC-0034
title: Sentry Integration for Error Tracking
status: Draft
category: infra
created: 2025-01-10
updated: 2025-10-05
related-rfcs: RFC-0031, RFC-0033, RFC-0035
---

# RFC-0034: Sentry Integration for Error Tracking

## Status

Draft

## Date

2025-01-10 (Updated 2025-10-05)

## Summary

Integrate Sentry error tracking and crash reporting across all Winged Bean components (C#/.NET, Unity, Python, Node.js) with unified release tracking, environment segregation, and PII-compliant data handling.

**Implementation Note**: This RFC provides the **error tracking backend** for:
- **RFC-0031**: `IDiagnosticsService.TrackErrorAsync()` delegates to Sentry SDK
- **RFC-0033**: Follows the observability strategy architecture

Sentry handles **exceptions and crashes** (system errors), not product analytics (see RFC-0030).

## Motivation

### Problems to Solve

1. **Error Blindness**: No centralized view of application errors and crashes
2. **Release Tracking**: Cannot correlate errors with specific deployments
3. **User Impact**: Unknown scope and severity of production issues
4. **Debug Context**: Missing stack traces, breadcrumbs, and environment data
5. **Alert Fatigue**: Need smart grouping to avoid duplicate noise

### Goals

- **Centralized Error Tracking**: All exceptions flow to Sentry
- **Smart Grouping**: Sentry's fingerprinting for issue deduplication
- **Release Correlation**: Tag errors with version and deployment info
- **Source Maps**: Link errors to source code (where applicable)
- **PII Compliance**: Scrub sensitive data per R-SEC-030

## Proposal

### Sentry Project Structure

```
Sentry Organization: winged-bean
├── Project: winged-bean-dotnet-console
│   └── Environments: dev, staging, production
├── Project: winged-bean-unity
│   └── Environments: dev, staging, production
├── Project: winged-bean-python
│   └── Environments: dev, staging, production
└── Project: winged-bean-nodejs
    └── Environments: dev, staging, production
```

**Rationale**: Separate projects per language stack for:
- Independent quota management
- Language-specific settings (source maps, symbolication)
- Team permissions and alert routing

### Configuration Management

**Secrets (SOPS Encrypted)**
```yaml
# config/observability/secrets/production.enc.yaml
sentry:
  dsn:
    dotnet: "https://<key>@o<org>.ingest.sentry.io/<project>"
    unity: "https://<key>@o<org>.ingest.sentry.io/<project>"
    python: "https://<key>@o<org>.ingest.sentry.io/<project>"
    nodejs: "https://<key>@o<org>.ingest.sentry.io/<project>"
```

**Public Config**
```yaml
# config/observability/sentry.yaml
sentry:
  org: winged-bean
  projects:
    dotnet:
      name: winged-bean-dotnet-console
      platform: csharp
    unity:
      name: winged-bean-unity
      platform: unity
    python:
      name: winged-bean-python
      platform: python
    nodejs:
      name: winged-bean-nodejs
      platform: node

  # Per-environment settings
  environments:
    dev:
      sample_rate: 1.0
      traces_sample_rate: 1.0
      send_default_pii: false
    staging:
      sample_rate: 1.0
      traces_sample_rate: 0.5
      send_default_pii: false
    production:
      sample_rate: 1.0  # Always capture errors
      traces_sample_rate: 0.1
      send_default_pii: false
      attach_stacktrace: true
```

## Detailed Design

### .NET/C# Implementation

**Package Updates (`Directory.Packages.props`)**
```xml
<ItemGroup Label="Observability">
  <PackageVersion Include="Sentry.AspNetCore" Version="4.15.0" />
  <!-- For console apps without ASP.NET -->
  <PackageVersion Include="Sentry" Version="4.15.0" />
  <PackageVersion Include="Sentry.Extensions.Logging" Version="4.15.0" />
</ItemGroup>
```

**Console Host Integration**
```csharp
// Program.cs or Startup configuration
using Sentry;

public class Program
{
    public static async Task Main(string[] args)
    {
        var host = Host.CreateDefaultBuilder(args)
            .ConfigureServices((context, services) =>
            {
                // Sentry integration with Microsoft.Extensions.Logging
                services.AddSentry(options =>
                {
                    options.Dsn = context.Configuration["Sentry:Dsn"];
                    options.Environment = context.Configuration["Environment"];
                    options.Release = GetVersion(); // From assembly or git tag
                    options.TracesSampleRate = GetSampleRate(context.Configuration);
                    
                    // PII redaction (R-SEC-030)
                    options.BeforeSend = (sentryEvent) =>
                    {
                        // Remove PII from event data
                        ScrubSensitiveData(sentryEvent);
                        return sentryEvent;
                    };
                    
                    // Add custom tags
                    options.SetBeforeSendTransaction((transaction) =>
                    {
                        transaction.SetTag("component", "dotnet-console");
                        return transaction;
                    });
                });
            })
            .Build();

        try
        {
            await host.RunAsync();
        }
        catch (Exception ex)
        {
            SentrySdk.CaptureException(ex);
            await SentrySdk.FlushAsync(TimeSpan.FromSeconds(5));
            throw;
        }
    }

    private static void ScrubSensitiveData(SentryEvent sentryEvent)
    {
        // Redact PII per R-SEC-030
        if (sentryEvent.User != null)
        {
            sentryEvent.User.Email = null;
            sentryEvent.User.IpAddress = null;
            sentryEvent.User.Username = RedactUsername(sentryEvent.User.Username);
        }
        
        // Scrub sensitive context data
        // ... additional scrubbing logic
    }
}
```

**Plugin Context**
```csharp
// In plugin loading code
using (SentrySdk.PushScope())
{
    SentrySdk.ConfigureScope(scope =>
    {
        scope.SetTag("plugin.name", plugin.Name);
        scope.SetTag("plugin.version", plugin.Version);
    });
    
    try
    {
        // Load plugin
        await pluginLoader.LoadAsync(plugin);
    }
    catch (Exception ex)
    {
        SentrySdk.CaptureException(ex);
        throw;
    }
}
```

### Unity Implementation

**Package Installation**
- Import Sentry Unity SDK via Unity Package Manager
- URL: `https://github.com/getsentry/unity.git#4.15.0`

**Unity Configuration**
```csharp
// SentryInitialization.cs (attach to GameObject or RuntimeInitializeOnLoadMethod)
using Sentry.Unity;
using UnityEngine;

public class SentryInitialization : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Initialize()
    {
        var options = new SentryUnityOptions
        {
            Dsn = LoadDsnFromConfig(), // From StreamingAssets or PlayerPrefs
            Environment = GetEnvironment(),
            Release = Application.version,
            
            // Unity-specific settings
            AttachScreenshot = true,
            AttachViewHierarchy = false, // Privacy consideration
            
            // IL2CPP considerations
            Debug = false, // Disable in production builds
            DiagnosticLevel = SentryLevel.Warning,
            
            // PII redaction (R-SEC-030)
            BeforeSend = (sentryEvent) =>
            {
                ScrubUnityPII(sentryEvent);
                return sentryEvent;
            }
        };
        
        SentryUnity.Init(options);
    }
    
    private static string LoadDsnFromConfig()
    {
        // Load from secure storage or build-time injection
        // DO NOT hardcode per R-SEC-020
        return Environment.GetEnvironmentVariable("SENTRY_DSN");
    }
    
    private static void ScrubUnityPII(SentryEvent sentryEvent)
    {
        // Redact device identifiers, user IDs
        if (sentryEvent.Contexts?.Device != null)
        {
            sentryEvent.Contexts.Device.Name = "[REDACTED]";
        }
    }
}
```

**IL2CPP Testing**
- Must test on iOS (ARM64), Android (ARM64, ARMv7), WebGL
- Verify exception stack traces are symbolicated correctly
- Test with IL2CPP stripping settings (Low, Medium, High)

### Python Implementation

**Dependencies (`pyproject.toml`)**
```toml
[project]
dependencies = [
    # ... existing
    "sentry-sdk>=2.18.0",
]
```

**Application Integration**
```python
# src/winged_bean/observability/sentry_init.py
import os
import sentry_sdk
from sentry_sdk.integrations.logging import LoggingIntegration

def initialize_sentry(environment: str, release: str):
    """Initialize Sentry SDK with PII scrubbing."""
    
    sentry_sdk.init(
        dsn=os.environ.get("SENTRY_DSN"),
        environment=environment,
        release=release,
        
        # Integrations
        integrations=[
            LoggingIntegration(
                level=logging.INFO,
                event_level=logging.ERROR
            ),
        ],
        
        # Sampling
        traces_sample_rate=get_sample_rate(environment),
        
        # PII redaction (R-SEC-030)
        before_send=scrub_pii,
        
        # Performance
        send_default_pii=False,
        attach_stacktrace=True,
    )

def scrub_pii(event, hint):
    """Remove PII from Sentry events per R-SEC-030."""
    if event.get("user"):
        event["user"].pop("email", None)
        event["user"].pop("ip_address", None)
        if "username" in event["user"]:
            event["user"]["username"] = redact_username(event["user"]["username"])
    
    # Scrub request data
    if event.get("request"):
        # Remove sensitive headers
        if "headers" in event["request"]:
            sensitive_headers = ["authorization", "cookie", "x-api-key"]
            for header in sensitive_headers:
                event["request"]["headers"].pop(header, None)
    
    return event

def redact_username(username: str) -> str:
    """Partial redaction: show first 2 chars only."""
    if len(username) <= 2:
        return "**"
    return username[:2] + "*" * (len(username) - 2)
```

**Usage in Scripts**
```python
# scripts/example_script.py
from winged_bean.observability.sentry_init import initialize_sentry

def main():
    initialize_sentry(
        environment=os.environ.get("ENVIRONMENT", "dev"),
        release=os.environ.get("GIT_SHA", "dev")
    )
    
    try:
        # Your logic
        process_data()
    except Exception as e:
        # Automatically captured by Sentry
        logger.exception("Processing failed")
        raise

if __name__ == "__main__":
    main()
```

### Node.js Implementation

**Dependencies (`package.json`)**
```json
{
  "dependencies": {
    "@sentry/node": "^8.40.0",
    "@sentry/profiling-node": "^8.40.0"
  }
}
```

**Express/Fastify Integration**
```javascript
// pty-service/src/observability/sentry.js
import * as Sentry from "@sentry/node";
import { nodeProfilingIntegration } from "@sentry/profiling-node";

export function initializeSentry(app) {
  Sentry.init({
    dsn: process.env.SENTRY_DSN,
    environment: process.env.NODE_ENV || "development",
    release: process.env.GIT_SHA || "dev",
    
    integrations: [
      // Express integration (if using Express)
      new Sentry.Integrations.Http({ tracing: true }),
      new Sentry.Integrations.Express({ app }),
      // Profiling
      nodeProfilingIntegration(),
    ],
    
    // Performance monitoring
    tracesSampleRate: getSampleRate(process.env.NODE_ENV),
    profilesSampleRate: 0.1, // Profile 10% of transactions
    
    // PII redaction (R-SEC-030)
    beforeSend(event, hint) {
      return scrubPII(event);
    },
  });
  
  // Request handler middleware (must be first)
  app.use(Sentry.Handlers.requestHandler());
  app.use(Sentry.Handlers.tracingHandler());
}

export function setupSentryErrorHandler(app) {
  // Error handler middleware (must be last)
  app.use(Sentry.Handlers.errorHandler());
}

function scrubPII(event) {
  // Remove PII per R-SEC-030
  if (event.user) {
    delete event.user.email;
    delete event.user.ip_address;
    if (event.user.username) {
      event.user.username = redactUsername(event.user.username);
    }
  }
  
  // Scrub request cookies and auth headers
  if (event.request?.headers) {
    delete event.request.headers.cookie;
    delete event.request.headers.authorization;
  }
  
  return event;
}
```

**Usage Example**
```javascript
// pty-service/src/index.js
import express from "express";
import { initializeSentry, setupSentryErrorHandler } from "./observability/sentry.js";

const app = express();

// Initialize Sentry FIRST
initializeSentry(app);

// Your middleware and routes
app.use(express.json());
app.get("/health", (req, res) => res.json({ status: "ok" }));

// Error handler LAST
setupSentryErrorHandler(app);

app.listen(3000);
```

## Release Tracking

### .NET
```bash
# In build/deployment script
dotnet build -c Release
sentry-cli releases new "winged-bean-dotnet@1.2.3"
sentry-cli releases files "winged-bean-dotnet@1.2.3" upload-sourcemaps ./bin/Release
sentry-cli releases finalize "winged-bean-dotnet@1.2.3"
sentry-cli releases deploys "winged-bean-dotnet@1.2.3" new -e production
```

### Unity
```bash
# After Unity build
sentry-cli releases new "winged-bean-unity@$(cat version.txt)"
sentry-cli releases files upload-sourcemaps ./Build/Symbols
sentry-cli releases finalize "winged-bean-unity@$(cat version.txt)"
```

### Python
```bash
# In deployment pipeline
export SENTRY_RELEASE="winged-bean-python@$(git rev-parse --short HEAD)"
sentry-cli releases new "$SENTRY_RELEASE"
sentry-cli releases finalize "$SENTRY_RELEASE"
```

### Node.js
```bash
# In package.json build script
npm run build
sentry-cli releases new "winged-bean-nodejs@$(node get-version.js)"
sentry-cli releases files upload-sourcemaps ./dist
sentry-cli releases finalize "winged-bean-nodejs@$(node get-version.js)"
```

## Alert Configuration

### Alert Rules (Per Project)

**Critical Errors** (Immediate Slack notification)
- New issue first seen
- Issue frequency > 100/min
- Issue affects > 50 users
- Tags: `level:fatal`, `environment:production`

**High Priority** (Slack notification, daily digest)
- Issue regression (previously resolved)
- Error rate increase > 50%
- Tags: `level:error`, `environment:production`

**Monitoring** (Weekly digest)
- All other errors
- Performance regressions

### Notification Channels
- Slack: `#alerts-production` (critical), `#alerts-staging` (high)
- Email: Team leads (critical only)
- PagerDuty: On-call rotation (critical production only)

## Testing Strategy

### Development Testing
```bash
# Test Sentry integration locally
export SENTRY_DSN="https://...@sentry.io/..."
export ENVIRONMENT="dev"

# Trigger test error
./scripts/test-sentry-integration.sh

# Verify in Sentry UI
echo "Check https://sentry.io/organizations/winged-bean/issues/"
```

### Integration Tests
```csharp
// Test Sentry initialization without sending events
[Fact]
public void SentryInitializes_WithoutErrors()
{
    using var _ = SentrySdk.Init(o =>
    {
        o.Dsn = "https://fake@sentry.io/0"; // Disabled DSN
        o.Environment = "test";
    });
    
    Assert.NotNull(SentrySdk.LastEventId);
}

[Fact]
public void PII_IsScrubbedFromEvents()
{
    var @event = new SentryEvent
    {
        User = new User { Email = "test@example.com" }
    };
    
    var scrubbed = ScrubSensitiveData(@event);
    
    Assert.Null(scrubbed.User.Email);
}
```

## Security Considerations

### Secret Management (R-SEC-010)
- DSNs stored in SOPS-encrypted files
- Never commit DSNs to version control
- Rotate DSNs if accidentally exposed
- Use project-specific DSNs (not org keys)

### PII Redaction (R-SEC-030)
- `beforeSend` hooks in all languages
- Remove email, IP addresses, usernames
- Scrub request cookies and authorization headers
- Redact sensitive context data

### Data Retention
- Development: 7 days (reduce cost)
- Staging: 30 days
- Production: 90 days (configurable per compliance)

## Cost Estimation

### Free Tier (Initial)
- 5,000 errors/month
- 50,000 transactions/month
- **Cost**: $0

### Developer Tier (After growth)
- 50,000 errors/month
- 100,000 transactions/month
- **Cost**: $26/month per project
- **Total**: ~$104/month (4 projects)

### Optimization Strategies
- Use sampling for traces (not errors)
- Filter noisy errors (e.g., network timeouts)
- Aggressive grouping/fingerprinting
- Archive old issues

## Success Criteria

- [ ] All components report errors to Sentry
- [ ] PII scrubbing validated (no email/IP leaks)
- [ ] Release tracking configured for all projects
- [ ] Alerts routed to appropriate channels
- [ ] Source maps uploaded for Node.js
- [ ] IL2CPP symbolication works for Unity
- [ ] Dashboard shows error trends
- [ ] <5 seconds P95 event ingestion latency

## Open Questions

1. **Budget Approval**: Is $104/month acceptable for Developer tier? (If not, use free tier with sampling)
2. **Alert Routing**: Should we use PagerDuty for critical alerts? (Adds cost)
3. **Unity Platforms**: Which Unity platforms to prioritize for testing? (iOS/Android/Desktop/WebGL)
4. **Retention**: Any compliance requirements for longer retention? (Affects cost)

## Related RFCs

- RFC-0033: Observability Strategy Foundation
- RFC-0035: OpenTelemetry Integration for Distributed Tracing

## References

- [Sentry Documentation](https://docs.sentry.io/)
- [Sentry Unity SDK](https://docs.sentry.io/platforms/unity/)
- [Sentry .NET SDK](https://docs.sentry.io/platforms/dotnet/)
- [Sentry Python SDK](https://docs.sentry.io/platforms/python/)
- [Sentry Node.js SDK](https://docs.sentry.io/platforms/node/)
- R-SEC-010: Secret Management
- R-SEC-030: PII Redaction

---

*This RFC depends on RFC-0033 (Observability Strategy Foundation). See RFC-0035 for OpenTelemetry implementation.*

# RFC-0016: Cost-Effective Deployment Strategy with Vercel and Fly.io

**Status:** Proposed  
**Date:** 2025-10-02  
**Author:** Development Team  
**Category:** infra, deployment  
**Priority:** MEDIUM (P2)  
**Estimated Effort:** 6-8 hours  

---

## Summary

Establish a cost-effective deployment strategy for Winged Bean documentation site and PTY service using Vercel (free tier) for static/SSR content and Fly.io (pay-per-use) for the PTY + Terminal.Gui C# console application. Implement automated start/stop scheduling to minimize costs to $0.69-2/month for 4 hours/day uptime, integrated with RFC-0010 Task-based build orchestration.

---

## Motivation

### Current State

- **Local development only** - No remote testing environment
- **Manual deployment** - No CI/CD pipeline
- **No E2E testing against live services** - Tests run only against localhost
- **No cost optimization** - Would pay 24/7 for services used 4 hours/day

### Goals

1. **Minimize deployment costs** - Target <$5/month for development/testing
2. **Remote E2E testing** - Automated tests against live deployments
3. **Scheduled uptime** - Auto-start/stop for cost savings
4. **Integrate with RFC-0010** - Use Task build orchestration
5. **Support C# console app** - Terminal.Gui v2 requires .NET runtime + adequate resources

---

## Platform Evaluation

### Requirements

- **Docs Site**: Astro static/SSR, ~100MB bandwidth/month
- **PTY Service**: Node.js + node-pty + .NET 8 runtime + Terminal.Gui
  - Minimum: 512MB RAM, 0.5 vCPU
  - WebSocket support (long-lived connections)
  - 4 hours/day uptime (scheduled)

### Evaluated Platforms

| Platform | WebSocket | .NET Support | Free Tier | 4h/day Cost | Notes |
|----------|-----------|--------------|-----------|-------------|-------|
| **Vercel** | ❌ No | ❌ No | ✅ 100GB | $0 | Docs only, no PTY |
| **Fly.io** | ✅ Yes | ✅ Yes | ❌ $5 trial | **$0.69** | Best for PTY |
| **Koyeb** | ✅ Yes | ✅ Yes | ⚠️ 512MB | $5.18 | Free tier too small |
| **Render** | ✅ Yes | ✅ Yes | ⚠️ Spins down | $0-7 | Cold starts |
| **Railway** | ✅ Yes | ✅ Yes | ❌ None | $10-20 | No free tier |
| **GCP Cloud Run** | ⚠️ 60min limit | ✅ Yes | ✅ Generous | $5-16 | Timeout kills PTY |

**Decision: Vercel (docs) + Fly.io (PTY) = $0.69-2/month**

---

## Proposal

### 1. Architecture

```
┌─────────────────────────────────────────────────────────────┐
│                     GitHub Actions                          │
│  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐     │
│  │ Task Build   │  │ Deploy       │  │ Start/Stop   │     │
│  │ (RFC-0010)   │  │ Workflows    │  │ Scheduler    │     │
│  └──────┬───────┘  └──────┬───────┘  └──────┬───────┘     │
└─────────┼──────────────────┼──────────────────┼────────────┘
          │                  │                  │
          ▼                  ▼                  ▼
┌─────────────────┐  ┌─────────────────┐  ┌─────────────────┐
│ Task Build      │  │ Vercel          │  │ Fly.io          │
│ _artifacts/     │  │ (Docs Site)     │  │ (PTY Service)   │
│ v{GitVersion}/  │  │ FREE            │  │ $0.69-2/month   │
│ ├─ web/dist     │  │                 │  │                 │
│ ├─ pty/dist     │  │ - Astro SSR     │  │ - Node.js       │
│ └─ dotnet/bin   │  │ - Global CDN    │  │ - .NET 8        │
└─────────────────┘  │ - Auto preview  │  │ - Terminal.Gui  │
                     └─────────────────┘  │ - WebSocket     │
                                          └─────────────────┘
```

### 2. Deployment Strategy

#### Vercel (Documentation Site)

**Configuration:**
```json
{
  "buildCommand": "cd ../../../ && task nodejs:build-docs",
  "outputDirectory": "dist",
  "framework": "astro",
  "regions": ["iad1"]
}
```

**Features:**
- Zero-config Astro deployment
- Automatic preview deployments per PR
- Global CDN (fast worldwide)
- Free tier: 100GB bandwidth/month
- **Cost: $0/month**

#### Fly.io (PTY Service)

**Configuration:**
```toml
[vm]
  memory = '512mb'
  cpu_kind = 'shared'
  cpus = 1

[http_service]
  internal_port = 4041
  auto_stop_machines = false  # Controlled by GitHub Actions
  auto_start_machines = false
  min_machines_running = 0
```

**Features:**
- Full VM (Firecracker microVM)
- WebSocket support (no timeout)
- .NET 8 runtime + Terminal.Gui
- Pay-per-second billing
- **Cost: $0.0000016/second = $0.69/month (4h/day)**

### 3. Cost Optimization: Scheduled Start/Stop

**Problem:** Always-on VM costs $4.14/month, but only needed 4 hours/day.

**Solution:** GitHub Actions scheduled workflows

```yaml
# Start at 8 AM UTC
- cron: '0 8 * * *'
# Stop at 12 PM UTC (4 hours later)
- cron: '0 12 * * *'
```

**Cost Calculation:**
- 4 hours/day = 14,400 seconds/day
- 30 days = 432,000 seconds/month
- 432,000 × $0.0000016 = **$0.69/month**

**Savings:** 83% reduction ($4.14 → $0.69)

### 4. Integration with RFC-0010 Task Orchestration

**New Task commands** (`build/Taskfile.deploy.yml`):

```yaml
tasks:
  deploy-vercel-docs:
    desc: "Deploy docs site to Vercel"
    cmds:
      - vercel --prod

  deploy-flyio-pty:
    desc: "Deploy PTY service to Fly.io"
    deps: [build-pty]  # Uses RFC-0010 build task
    cmds:
      - flyctl deploy --app winged-bean-pty

  flyio-start:
    desc: "Start Fly.io PTY machine"
    cmds:
      - flyctl machine start <machine-id> --app winged-bean-pty

  flyio-stop:
    desc: "Stop Fly.io PTY machine"
    cmds:
      - flyctl machine stop <machine-id> --app winged-bean-pty
```

**GitHub Actions integration:**
```yaml
- name: Build PTY service
  run: task build:build-pty  # RFC-0010 Task command

- name: Deploy to Fly.io
  run: task build:deploy-flyio-pty
```

### 5. Dockerfile for PTY Service

**Multi-stage build** using Task artifacts:

```dockerfile
FROM node:20-slim

# Install .NET 8 runtime
RUN wget https://dot.net/v1/dotnet-install.sh -O dotnet-install.sh \
    && chmod +x dotnet-install.sh \
    && ./dotnet-install.sh --channel 8.0 --install-dir /usr/share/dotnet

# Copy Task-built artifacts
COPY . .
COPY --from=artifacts ./console-app/ ./console-app/

EXPOSE 4041
CMD ["npm", "start"]
```

---

## Implementation Plan

### Phase 1: Vercel Deployment (1-2 hours)

1. Create `vercel.json` config with Task build command
2. Connect GitHub repo to Vercel
3. Deploy docs site: `task build:deploy-vercel-docs`
4. Verify deployment and preview URLs

**Deliverables:**
- `development/nodejs/sites/docs/vercel.json`
- Live docs site at `https://winged-bean-docs.vercel.app`

### Phase 2: Fly.io Deployment (2-3 hours)

1. Create `fly.toml` config (512MB, 1 vCPU)
2. Create Dockerfile with .NET 8 runtime
3. Deploy PTY service: `task build:deploy-flyio-pty`
4. Test WebSocket connection to Terminal.Gui

**Deliverables:**
- `development/nodejs/pty-service/fly.toml`
- `development/nodejs/pty-service/Dockerfile`
- Live PTY service at `https://winged-bean-pty.fly.dev`

### Phase 3: Automated Start/Stop (1-2 hours)

1. Create GitHub Actions workflow for scheduled start/stop
2. Add `FLY_API_TOKEN` secret to GitHub
3. Test manual start/stop via workflow dispatch
4. Verify scheduled execution

**Deliverables:**
- `.github/workflows/deploy-flyio-scheduled.yml`
- Automated cost savings (83% reduction)

### Phase 4: E2E Testing (1-2 hours)

1. Create GitHub Actions workflow for E2E tests
2. Update tests to use deployed URLs
3. Run tests after machine starts (8:05 AM UTC)
4. Upload test results and screenshots

**Deliverables:**
- `.github/workflows/e2e-tests-task.yml`
- Automated E2E testing against live services

### Phase 5: Task Integration (1 hour)

1. Create `build/Taskfile.deploy.yml`
2. Add deployment tasks to root Taskfile
3. Document Task commands in deployment guide
4. Test full deployment workflow

**Deliverables:**
- `build/Taskfile.deploy.yml`
- Updated `Taskfile.yml` with includes
- `docs/deployment/DEPLOYMENT_GUIDE.md`

---

## Cost Projection

### Monthly Costs (4 hours/day uptime)

| Service | Instance | Monthly Cost | Notes |
|---------|----------|--------------|-------|
| **Vercel (Docs)** | Free tier | **$0** | 100GB bandwidth included |
| **Fly.io (PTY)** | 512MB, 1 vCPU | **$0.69** | 432,000 sec/month @ $0.0000016/sec |
| **Total** | - | **$0.69-2** | May vary with usage |

### Annual Cost Projection

- **Year 1:** $8.28-24 (vs $49.68 always-on)
- **Savings:** 83% reduction

### Cost Scaling

| Uptime | Monthly Cost | Use Case |
|--------|--------------|----------|
| 4h/day | $0.69 | Development/testing |
| 8h/day | $1.38 | Extended testing |
| 12h/day | $2.07 | Production demos |
| 24/7 | $4.14 | Always-on production |

---

## Alternatives Considered

### Alternative 1: Vercel + Render Free Tier

**Cost:** $0/month  
**Pros:** Completely free  
**Cons:** PTY service spins down after 15min (30-60s cold start)

**Decision:** Rejected - Cold starts break terminal UX

### Alternative 2: Fly.io Only (Both Services)

**Cost:** $4-5/month (always-on)  
**Pros:** Single platform, simple setup  
**Cons:** Paying for docs hosting when Vercel is free

**Decision:** Rejected - Unnecessary cost for static docs

### Alternative 3: GCP Cloud Run

**Cost:** $5-16/month  
**Pros:** Generous free tier, scales to zero  
**Cons:** 60min WebSocket timeout kills PTY sessions

**Decision:** Rejected - WebSocket timeout is a deal-breaker

### Alternative 4: Koyeb Free Tier

**Cost:** $0/month  
**Pros:** Free tier with no time limits  
**Cons:** 512MB RAM + 0.1 vCPU too small for .NET + Terminal.Gui

**Decision:** Rejected - Insufficient resources for C# console app

---

## Success Criteria

1. ✅ Docs site deployed to Vercel with automatic preview deployments
2. ✅ PTY service deployed to Fly.io with WebSocket support
3. ✅ Automated start/stop reduces costs by 80%+
4. ✅ E2E tests run against live deployments daily
5. ✅ All deployments use Task commands (RFC-0010 integration)
6. ✅ Monthly cost <$2 for 4 hours/day uptime
7. ✅ Terminal.Gui C# console app runs reliably on Fly.io
8. ✅ Documentation includes cost monitoring and troubleshooting

---

## Security Considerations

1. **Fly.io API Token**: Stored as GitHub secret, rotated periodically
2. **WebSocket**: Uses WSS (encrypted) in production
3. **Vercel**: Automatic HTTPS, no additional config needed
4. **GitHub Actions**: Secrets never logged or exposed
5. **PTY Service**: No public endpoints except WebSocket (port 4041)

---

## Monitoring and Maintenance

### Cost Monitoring

```bash
# Check Fly.io usage
fly billing show

# Check machine status
task build:flyio-status

# View machine metrics
fly machine status <machine-id> --app winged-bean-pty
```

### Health Checks

- **Docs site**: Vercel automatic monitoring
- **PTY service**: Health endpoint at `/health`
- **E2E tests**: Daily automated runs with alerts

### Maintenance Tasks

- **Weekly**: Review Fly.io billing dashboard
- **Monthly**: Check for platform updates (Vercel, Fly.io)
- **Quarterly**: Evaluate cost trends and optimize schedule

---

## Open Questions

1. **Timezone optimization**: Should start/stop times vary by developer timezone?
2. **Weekend uptime**: Should machine stay off on weekends to save costs?
3. **Burst testing**: How to handle occasional 24/7 testing needs? (Manual override?)
4. **Multi-region**: Should we deploy PTY service to multiple Fly.io regions for redundancy?
5. **Artifact retention**: How long to keep versioned artifacts in `build/_artifacts/`? (Relates to RFC-0010)

---

## References

- [RFC-0010: Multi-Language Build Orchestration with Task](./0010-multi-language-build-orchestration-with-task.md)
- [Platform Survey](../deployment/deployment-platform-survey.md) - Detailed comparison of 6 platforms
- [Deployment Guide](../deployment/DEPLOYMENT_GUIDE.md) - Step-by-step implementation
- [Vercel Documentation](https://vercel.com/docs)
- [Fly.io Documentation](https://fly.io/docs)
- [Task Documentation](https://taskfile.dev)

---

## Appendix: Platform Comparison Matrix

| Platform | WebSocket | PTY/Long-Running | Free Tier | 4h/day Cost | Setup Complexity | Best For |
|----------|-----------|------------------|-----------|-------------|------------------|----------|
| **Fly.io** | ✅ Native | ✅ Full VM | ⚠️ $5 trial | **$0.69** | Medium | Full-stack apps |
| **GCP Cloud Run** | ⚠️ 60min timeout | ⚠️ 60min timeout | ✅ Generous | $5-16 | Medium-High | Serverless APIs |
| **Vercel** | ❌ No | ❌ 10-60s timeout | ✅ Excellent | $0 (docs only) | Low | Static/SSR sites |
| **Railway** | ✅ Native | ✅ Full VM | ❌ None | $10-20 | Low | Rapid prototyping |
| **Render** | ✅ Native | ✅ Full VM | ⚠️ Spins down | $0-7 | Low | Simple deployments |
| **Koyeb** | ✅ Native | ✅ Full VM | ⚠️ 512MB/0.1vCPU | $5.18 | Low | Small apps |

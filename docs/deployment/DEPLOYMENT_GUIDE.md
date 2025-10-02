# Deployment Guide: Vercel + Fly.io with Task Orchestration

**Implements:** [RFC-0016: Cost-Effective Deployment Strategy](../rfcs/0016-cost-effective-deployment-strategy.md)  
**Builds with:** [RFC-0010: Multi-Language Build Orchestration](../rfcs/0010-multi-language-build-orchestration-with-task.md)  
**Cost:** $0.69-2/month for 4 hours/day uptime  
**Last Updated:** 2025-10-02

---

## Overview

This deployment strategy uses:
- **Task** for build orchestration (per RFC-0010)
- **Vercel** for Astro docs site (free tier)
- **Fly.io** for PTY + Terminal.Gui C# console service (pay-per-use)
- **GitHub Actions** for automated start/stop and E2E testing
- **GitVersion** for semantic versioning of artifacts

---

## Architecture

```
┌─────────────────────────────────────────────────────────────┐
│                     GitHub Actions                          │
│  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐     │
│  │ Task Build   │  │ Deploy       │  │ Start/Stop   │     │
│  │ Orchestrator │  │ Workflows    │  │ Scheduler    │     │
│  └──────┬───────┘  └──────┬───────┘  └──────┬───────┘     │
└─────────┼──────────────────┼──────────────────┼────────────┘
          │                  │                  │
          ▼                  ▼                  ▼
┌─────────────────┐  ┌─────────────────┐  ┌─────────────────┐
│ Task Build      │  │ Vercel          │  │ Fly.io          │
│ _artifacts/     │  │ (Docs Site)     │  │ (PTY Service)   │
│ v{GitVersion}/  │  │ FREE            │  │ $0.69-2/month   │
│ ├─ dotnet/bin   │  └─────────────────┘  └─────────────────┘
│ ├─ web/dist     │
│ └─ pty/dist     │
└─────────────────┘
```

---

## Prerequisites

### 1. Install Task (Build Orchestrator)

```bash
# macOS
brew install go-task/tap/go-task

# Windows
scoop install task

# Linux
sh -c "$(curl --location https://taskfile.dev/install.sh)" -- -d
```

### 2. Install Deployment CLIs

```bash
# Vercel CLI
npm install -g vercel

# Fly.io CLI
curl -L https://fly.io/install.sh | sh
```

### 3. Create Accounts

- **Vercel**: https://vercel.com (free)
- **Fly.io**: https://fly.io (requires credit card)

### 4. Authenticate

```bash
# Vercel
vercel login

# Fly.io
fly auth login
```

---

## Part 1: Build with Task

### Step 1: Verify Task Setup

```bash
# From project root
task --list

# Should show tasks from RFC-0010:
# - build:version
# - build:init-dirs
# - build:build-dotnet
# - build:build-web
# - build:build-pty
# - build:build-all
```

### Step 2: Build All Components

```bash
# Build everything (uses GitVersion for artifact paths)
task build:build-all

# Verify artifacts created
ls -la build/_artifacts/v*/
```

Expected structure:
```
build/_artifacts/v1.0.0-alpha.1/
├── dotnet/
│   ├── bin/              # Terminal.Gui console app
│   ├── recordings/       # Runtime: asciinema casts
│   └── logs/             # Runtime: console logs
├── web/
│   ├── dist/             # Astro docs site
│   ├── recordings/       # Runtime: browser recordings
│   └── logs/             # Runtime: web logs
├── pty/
│   ├── dist/             # PTY service
│   └── logs/             # Runtime: PTY logs
└── _logs/                # Build-time logs
```

---

## Part 2: Deploy Docs to Vercel

### Step 1: Configure Vercel Project

The `vercel.json` config is already created in `development/nodejs/sites/docs/`.

Key settings:
- **buildCommand**: `cd ../../../ && task nodejs:build-docs`
- **outputDirectory**: `dist`
- **framework**: `astro`

### Step 2: Deploy via Task

```bash
# Deploy docs to Vercel
task build:deploy-vercel-docs
```

Or manually:
```bash
cd development/nodejs/sites/docs
vercel --prod
```

### Step 3: Note Deployment URL

Example: `https://winged-bean-docs.vercel.app`

---

## Part 3: Deploy PTY Service to Fly.io

### Step 1: Create Fly.io App (First Time Only)

```bash
cd development/nodejs/pty-service

# Launch app (don't deploy yet)
fly launch --name winged-bean-pty --region iad --no-deploy
```

### Step 2: Build and Deploy via Task

```bash
# From project root
task build:deploy-flyio-pty
```

This will:
1. Build PTY service via `task build:build-pty`
2. Copy artifacts to deployment directory
3. Deploy to Fly.io with Docker

### Step 3: Verify Deployment

```bash
# Check status
task build:flyio-status

# Or manually
fly status --app winged-bean-pty
fly logs --app winged-bean-pty

# Test health endpoint
curl https://winged-bean-pty.fly.dev/health
```

---

## Part 4: Configure Auto-Start/Stop

### Step 1: Add GitHub Secret

1. Get Fly.io API token:
   ```bash
   fly auth token
   ```

2. Add to GitHub:
   - Go to: Settings → Secrets and variables → Actions
   - New secret: `FLY_API_TOKEN`
   - Value: Paste token from step 1

### Step 2: Customize Schedule

Edit `.github/workflows/deploy-flyio-scheduled.yml`:

```yaml
schedule:
  # Start at 8 AM UTC (adjust to your timezone)
  - cron: '0 8 * * *'
  # Stop at 12 PM UTC (4 hours later)
  - cron: '0 12 * * *'
```

**Timezone examples:**
- 8 AM PST = `0 16 * * *` (UTC)
- 8 AM EST = `0 13 * * *` (UTC)
- 8 AM CST (China) = `0 0 * * *` (UTC)

### Step 3: Test Manual Control

Via GitHub Actions:
```bash
# Start machine
gh workflow run deploy-flyio-scheduled.yml -f action=start

# Stop machine
gh workflow run deploy-flyio-scheduled.yml -f action=stop

# Check status
gh workflow run deploy-flyio-scheduled.yml -f action=status
```

Via Task:
```bash
# Start machine
task build:flyio-start

# Stop machine
task build:flyio-stop

# Check status
task build:flyio-status
```

---

## Part 5: Update Demo Page WebSocket URL

Edit `development/nodejs/sites/docs/src/pages/demo/index.astro`:

```astro
<XTerm 
  id="pty-terminal" 
  websocketUrl="wss://winged-bean-pty.fly.dev" 
  mode="pty" 
/>
```

Redeploy:
```bash
task build:deploy-vercel-docs
```

---

## Part 6: E2E Testing

### Automated Testing

Tests run automatically:
- **Daily at 8:05 AM UTC** (after machine starts)
- **On push to main** (if docs/pty files change)
- **Manual trigger**: `gh workflow run e2e-tests-task.yml`

### Manual Testing

```bash
# Run E2E tests locally
task nodejs:test-e2e

# Or with custom URLs
BASE_URL=https://winged-bean-docs.vercel.app \
PTY_WEBSOCKET_URL=wss://winged-bean-pty.fly.dev \
task nodejs:test-e2e
```

---

## Cost Monitoring

### Expected Costs (4 hours/day)

**Fly.io (512MB, 1 vCPU):**
- Rate: $0.0000016/second
- 4 hours/day = 14,400 seconds/day
- 30 days = 432,000 seconds/month
- **Cost: $0.69/month**

**Vercel:**
- Free tier (100GB bandwidth)
- **Cost: $0/month**

**Total: $0.69-2/month**

### Track Usage

```bash
# Fly.io billing
fly billing show

# Machine metrics
task build:flyio-status

# Or manually
fly machine status <machine-id> --app winged-bean-pty
```

---

## Task Commands Reference

### Build Tasks (from RFC-0010)

```bash
task build:version              # Show GitVersion
task build:init-dirs            # Create artifact directories
task build:build-dotnet         # Build .NET projects via Nuke
task build:build-web            # Build Astro docs site
task build:build-pty            # Build PTY service
task build:build-all            # Build everything
task build:clean                # Clean artifacts
```

### Deployment Tasks (new)

```bash
task build:deploy-vercel-docs   # Deploy docs to Vercel
task build:deploy-flyio-pty     # Deploy PTY to Fly.io
task build:deploy-all           # Deploy everything
task build:flyio-start          # Start Fly.io machine
task build:flyio-stop           # Stop Fly.io machine
task build:flyio-status         # Check machine status
```

### Node.js Tasks

```bash
task nodejs:install             # Install dependencies
task nodejs:install-playwright  # Install Playwright browsers
task nodejs:test                # Run unit tests
task nodejs:test-e2e            # Run E2E tests
task nodejs:build-docs          # Build docs site
```

---

## Troubleshooting

### Task Build Fails

```bash
# Check GitVersion
task build:version

# Clean and rebuild
task build:clean
task build:build-all

# Check build logs
cat build/_artifacts/v*/logs/*.log
```

### Fly.io Machine Won't Start

```bash
# Check status
task build:flyio-status

# View logs
fly logs --app winged-bean-pty

# Restart machine
fly machine restart <machine-id> --app winged-bean-pty
```

### E2E Tests Failing

1. Verify machine is running:
   ```bash
   task build:flyio-status
   ```

2. Test endpoints:
   ```bash
   curl https://winged-bean-docs.vercel.app
   curl https://winged-bean-pty.fly.dev/health
   ```

3. Check GitHub Actions logs

4. Run tests locally:
   ```bash
   task nodejs:test-e2e
   ```

---

## Integration with RFC-0010

This deployment strategy follows RFC-0010 principles:

1. ✅ **Task orchestration** - All builds use `task` commands
2. ✅ **GitVersion integration** - Artifacts versioned as `v{GitVersion}`
3. ✅ **Structured artifacts** - Follows `build/_artifacts/` structure
4. ✅ **Multi-language support** - .NET, Node.js, Docker all orchestrated
5. ✅ **Incremental builds** - Task checksum-based caching
6. ✅ **Cross-platform** - Same commands work on Windows/macOS/Linux
7. ✅ **CI/CD ready** - GitHub Actions use `task` commands

---

## Next Steps

1. ✅ Install Task and deployment CLIs
2. ✅ Build all components: `task build:build-all`
3. ✅ Deploy docs: `task build:deploy-vercel-docs`
4. ✅ Deploy PTY: `task build:deploy-flyio-pty`
5. ✅ Configure GitHub secrets
6. ✅ Test auto-start/stop
7. ✅ Run E2E tests
8. 📊 Monitor costs for first month

---

## References

- [RFC-0010: Multi-Language Build Orchestration](../rfcs/0010-multi-language-build-orchestration-with-task.md)
- [Task Documentation](https://taskfile.dev)
- [GitVersion Documentation](https://gitversion.net)
- [Vercel Documentation](https://vercel.com/docs)
- [Fly.io Documentation](https://fly.io/docs)

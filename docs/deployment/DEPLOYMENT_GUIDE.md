# Deployment Guide: Vercel + Fly.io with Auto-Stop

**Cost:** $0.69-2/month for 4 hours/day uptime  
**Last Updated:** 2025-10-02

## Overview

This guide covers deploying the Winged Bean documentation site and PTY service using:
- **Vercel** for the Astro docs site (free tier)
- **Fly.io** for the PTY + Terminal.Gui C# console service (pay-per-use)
- **GitHub Actions** for automated start/stop and E2E testing

---

## Prerequisites

1. **Vercel Account** (free)
   - Sign up at https://vercel.com
   - Install Vercel CLI: `npm i -g vercel`

2. **Fly.io Account** (requires credit card)
   - Sign up at https://fly.io
   - Install Fly CLI: `curl -L https://fly.io/install.sh | sh`
   - Login: `fly auth login`

3. **GitHub Secrets** (for automation)
   - `FLY_API_TOKEN`: Get from `fly auth token`
   - `VERCEL_TOKEN`: Get from Vercel dashboard

---

## Part 1: Deploy Docs Site to Vercel

### Step 1: Configure Vercel

The `vercel.json` config is already created in `development/nodejs/sites/docs/`.

### Step 2: Deploy via CLI

```bash
cd development/nodejs/sites/docs
vercel --prod
```

Or connect your GitHub repo in the Vercel dashboard for automatic deployments.

### Step 3: Note Your Deployment URL

Example: `https://winged-bean-docs.vercel.app`

---

## Part 2: Deploy PTY Service to Fly.io

### Step 1: Build and Deploy

```bash
cd development/nodejs/pty-service

# Create Fly.io app (first time only)
fly launch --name winged-bean-pty --region iad --no-deploy

# Deploy the app
fly deploy
```

### Step 2: Verify Deployment

```bash
# Check app status
fly status --app winged-bean-pty

# View logs
fly logs --app winged-bean-pty

# Test WebSocket endpoint
curl https://winged-bean-pty.fly.dev/health
```

### Step 3: Get Machine ID

```bash
# List machines
fly machines list --app winged-bean-pty

# Note the machine ID (e.g., 148ed79c241e89)
```

---

## Part 3: Configure Auto-Start/Stop

### Step 1: Add GitHub Secret

1. Go to your GitHub repo → Settings → Secrets and variables → Actions
2. Add new secret:
   - Name: `FLY_API_TOKEN`
   - Value: Run `fly auth token` and paste the output

### Step 2: Customize Schedule

Edit `.github/workflows/flyio-scheduled-start-stop.yml`:

```yaml
schedule:
  # Start at 8 AM UTC (adjust to your timezone)
  - cron: '0 8 * * *'
  # Stop at 12 PM UTC (4 hours later)
  - cron: '0 12 * * *'
```

**Timezone conversion examples:**
- 8 AM PST = `0 16 * * *` (UTC)
- 8 AM EST = `0 13 * * *` (UTC)
- 8 AM CST (China) = `0 0 * * *` (UTC)

### Step 3: Test Manual Start/Stop

```bash
# Manual start via GitHub Actions
gh workflow run flyio-scheduled-start-stop.yml -f action=start

# Manual stop
gh workflow run flyio-scheduled-start-stop.yml -f action=stop

# Check status
gh workflow run flyio-scheduled-start-stop.yml -f action=status
```

Or use Fly CLI directly:

```bash
# Start machine
fly machine start <machine-id> --app winged-bean-pty

# Stop machine
fly machine stop <machine-id> --app winged-bean-pty
```

---

## Part 4: Update Demo Page WebSocket URL

Edit `development/nodejs/sites/docs/src/pages/demo/index.astro`:

```astro
<XTerm 
  id="pty-terminal" 
  websocketUrl="wss://winged-bean-pty.fly.dev" 
  mode="pty" 
/>
```

Redeploy to Vercel:

```bash
cd development/nodejs/sites/docs
vercel --prod
```

---

## Part 5: Set Up E2E Testing

### Step 1: Update E2E Test Config

The workflow `.github/workflows/e2e-tests-deployed.yml` is already configured.

### Step 2: Run E2E Tests

Tests run automatically:
- After machine starts (8:05 AM UTC daily)
- On push to main (if docs/pty files change)
- Manually via: `gh workflow run e2e-tests-deployed.yml`

### Step 3: View Test Results

- Check GitHub Actions tab
- Download artifacts (screenshots, reports)
- View logs for failures

---

## Cost Monitoring

### Fly.io Usage Tracking

```bash
# View current month usage
fly billing show

# View machine metrics
fly machine status <machine-id> --app winged-bean-pty
```

### Expected Costs (4 hours/day)

**Fly.io (512MB, 1 vCPU):**
- Hourly rate: $0.0000016/second
- 4 hours/day = 14,400 seconds/day
- 30 days = 432,000 seconds/month
- **Cost: $0.69/month**

**Vercel:**
- Free tier (100GB bandwidth)
- **Cost: $0/month**

**Total: $0.69-2/month**

---

## Troubleshooting

### Machine Won't Start

```bash
# Check machine status
fly machine status <machine-id> --app winged-bean-pty

# View recent logs
fly logs --app winged-bean-pty

# Restart machine
fly machine restart <machine-id> --app winged-bean-pty
```

### WebSocket Connection Fails

1. Check machine is running: `fly status --app winged-bean-pty`
2. Test health endpoint: `curl https://winged-bean-pty.fly.dev/health`
3. Check browser console for WebSocket errors
4. Verify WebSocket URL in demo page

### E2E Tests Failing

1. Check if machine is running during test
2. Verify deployment URLs in workflow
3. Check Playwright test logs in GitHub Actions
4. Download test screenshots from artifacts

### High Costs

1. Check if machine is stopping properly:
   ```bash
   fly machines list --app winged-bean-pty
   ```
2. Verify scheduled stop workflow is running
3. Manually stop if needed:
   ```bash
   fly machine stop <machine-id> --app winged-bean-pty
   ```

---

## Maintenance

### Update PTY Service

```bash
cd development/nodejs/pty-service
fly deploy --app winged-bean-pty
```

### Update Docs Site

```bash
cd development/nodejs/sites/docs
vercel --prod
```

Or push to main for automatic deployment.

### Scale Resources

Edit `fly.toml`:

```toml
[[vm]]
  memory = '1gb'  # Increase if needed
  cpu_kind = 'shared'
  cpus = 1
```

Then redeploy:

```bash
fly deploy --app winged-bean-pty
```

---

## Security Notes

1. **Fly.io API Token**: Keep secret, rotate periodically
2. **WebSocket**: Uses WSS (encrypted) in production
3. **Vercel**: Automatic HTTPS, no config needed
4. **GitHub Actions**: Use secrets, never commit tokens

---

## Next Steps

1. ✅ Deploy docs to Vercel
2. ✅ Deploy PTY service to Fly.io
3. ✅ Configure auto-start/stop
4. ✅ Update demo page WebSocket URL
5. ✅ Test E2E workflow
6. 📊 Monitor costs for first month
7. 🔧 Adjust schedule/resources as needed

---

## Support

- **Vercel Docs**: https://vercel.com/docs
- **Fly.io Docs**: https://fly.io/docs
- **GitHub Actions**: https://docs.github.com/actions
- **Project Issues**: https://github.com/GiantCroissant-Lunar/winged-bean/issues

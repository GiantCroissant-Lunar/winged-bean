# Deployment Platform Survey for Winged Bean Docs + PTY Demo

**Date:** 2025-10-02  
**Purpose:** Evaluate cloud platforms for remote E2E testing of Astro docs site + node-pty PTY service

## Project Requirements

- **Docs Site:** Astro static site with SSR demo page
- **PTY Service:** Node.js + node-pty (Terminal.Gui v2 via WebSocket on port 4041)
- **E2E Tests:** Playwright tests against live deployment
- **Use Case:** Continuous deployment previews + automated E2E testing
- **Budget:** Minimize cost for development/testing workload

---

## Platform Comparison Matrix

| Platform | WebSocket Support | PTY/Long-Running | Free Tier | Est. Monthly Cost | Setup Complexity | Best For |
|----------|-------------------|------------------|-----------|-------------------|------------------|----------|
| **Fly.io** | ✅ Native | ✅ Full VM | ⚠️ $5 trial credit | $0-8 | Medium | Full-stack apps |
| **GCP Cloud Run** | ✅ Yes (with caveats) | ⚠️ 60min timeout | ✅ Generous | $0.50-5 | Medium-High | Serverless APIs |
| **Vercel** | ❌ No (Edge only) | ❌ 10-60s timeout | ✅ Excellent | $0 (docs only) | Low | Static/SSR sites |
| **Railway** | ✅ Native | ✅ Full VM | ❌ Removed | $5-20 | Low | Rapid prototyping |
| **Render** | ✅ Native | ✅ Full VM | ✅ 750hrs/month | $0-7 | Low | Simple deployments |

---

## Detailed Platform Analysis

### 1. Fly.io 🚀

#### Overview
Fly.io runs full VMs (Firecracker microVMs) globally distributed via Anycast. Best for apps needing persistent connections and long-running processes.

#### Pricing (2024)
- **Compute:**
  - Shared CPU 1x (256MB): $0.0000008/sec = ~$2.07/month (always-on)
  - Shared CPU 2x (512MB): $0.0000016/sec = ~$4.14/month (always-on)
- **Free Tier (Trial):**
  - $5 one-time credit for new accounts
  - No ongoing free tier (pay-as-you-go after credit)
- **Network:**
  - First 100GB egress/month free
  - $0.02/GB after
- **Storage:**
  - 3GB free persistent volume
  - $0.15/GB/month after

#### WebSocket & PTY Support
- ✅ **Full WebSocket support** (native TCP/HTTP)
- ✅ **Long-running processes** (no timeout)
- ✅ **PTY/terminal apps** work perfectly
- ✅ **PM2 compatible**

#### Deployment
```bash
# Install flyctl
curl -L https://fly.io/install.sh | sh

# Deploy docs-site
cd development/nodejs/sites/docs
fly launch --name winged-bean-docs

# Deploy pty-service
cd ../../pty-service
fly launch --name winged-bean-pty
```

#### Pros
- ✅ Zero cold starts (always-on VMs)
- ✅ Perfect for WebSocket/PTY workloads
- ✅ Multi-region deployment (edge network)
- ✅ Simple monorepo support (multiple apps)
- ✅ Built-in health checks and auto-restart
- ✅ SSH access to VMs for debugging

#### Cons
- ❌ No free tier (only $5 trial credit)
- ❌ Always-on = always billed (~$4-8/month minimum)
- ⚠️ Requires credit card immediately
- ⚠️ Network egress can add up with heavy usage

#### Best For
- Production-grade PTY/WebSocket apps
- Apps needing <50ms latency globally
- Teams willing to pay $5-10/month for reliability

#### Estimated Cost for Your Project
- 2 small VMs (docs + pty): **$4-8/month**
- Network (light usage): **$0-2/month**
- **Total: $4-10/month**

---

### 2. GCP Cloud Run ☁️

#### Overview
Serverless container platform that scales to zero. Billed per request and CPU time.

#### Pricing (2024)
- **Compute:**
  - CPU: $0.00002400/vCPU-second
  - Memory: $0.00000250/GiB-second
  - Requests: $0.40/million
- **Free Tier (Monthly):**
  - 2M requests
  - 360,000 vCPU-seconds
  - 180,000 GiB-seconds
  - 1GB egress to North America
- **Network:**
  - $0.12/GB egress (after free tier)

#### WebSocket & PTY Support
- ⚠️ **WebSocket support added in 2023** but with limitations:
  - Max connection duration: **60 minutes** (hard limit)
  - Billed for entire connection duration (even if idle)
  - Cold starts can disconnect clients
- ⚠️ **Not ideal for PTY terminals:**
  - Connections reset after 60min
  - Cold start latency (2-5s) breaks terminal UX
  - Expensive for long-lived WebSocket connections

#### Deployment
```bash
# Build and deploy docs-site
gcloud run deploy winged-bean-docs \
  --source development/nodejs/sites/docs \
  --region us-central1 \
  --allow-unauthenticated

# Deploy pty-service (separate service)
gcloud run deploy winged-bean-pty \
  --source development/nodejs/pty-service \
  --region us-central1 \
  --allow-unauthenticated
```

#### Pros
- ✅ Generous free tier (likely covers docs site)
- ✅ Scales to zero (no cost when idle)
- ✅ Fast deployment via Dockerfile
- ✅ Integrated with GCP ecosystem
- ✅ Good for bursty workloads

#### Cons
- ❌ **60min WebSocket timeout** (kills PTY sessions)
- ❌ Cold starts (2-5s) hurt terminal UX
- ❌ **Expensive for long-lived connections** (billed per second)
- ⚠️ Complex networking for multi-service apps
- ⚠️ Requires GCP account setup

#### Best For
- Stateless APIs and serverless functions
- Apps with short-lived requests (<60s)
- Teams already using GCP

#### Estimated Cost for Your Project
- Docs site (mostly static): **$0-1/month** (free tier)
- PTY service (WebSocket): **$5-15/month** (always connected = always billed)
- **Total: $5-16/month**
- **⚠️ Not recommended for PTY use case**

---

### 3. Vercel 🔺

#### Overview
Edge-first platform optimized for frontend frameworks. Excellent for Astro static/SSR sites.

#### Pricing (2024)
- **Hobby (Free):**
  - 100GB bandwidth/month
  - Unlimited requests
  - Serverless functions: 100GB-hours
  - 10s function timeout
- **Pro ($20/month):**
  - 1TB bandwidth
  - 1000GB-hours functions
  - 60s function timeout

#### WebSocket & PTY Support
- ❌ **No WebSocket support** in serverless functions
- ❌ **No long-running processes**
- ⚠️ Edge Functions have 25s timeout (Pro: 60s)
- ❌ **Cannot run node-pty PTY service**

#### Deployment
```bash
# Install Vercel CLI
npm i -g vercel

# Deploy docs-site (works great)
cd development/nodejs/sites/docs
vercel --prod

# PTY service: NOT SUPPORTED
```

#### Pros
- ✅ **Best-in-class Astro support** (zero-config)
- ✅ Generous free tier (100GB bandwidth)
- ✅ Global edge network (fast everywhere)
- ✅ Automatic preview deployments per PR
- ✅ Built-in analytics and monitoring
- ✅ Zero cold starts for static content

#### Cons
- ❌ **No WebSocket support** (deal-breaker for PTY)
- ❌ Cannot run node-pty service
- ⚠️ Would need separate platform for PTY
- ⚠️ Serverless functions timeout (10-60s)

#### Best For
- Astro docs site **only** (not PTY service)
- Teams wanting zero-config deployment
- Static sites with optional SSR

#### Estimated Cost for Your Project
- Docs site: **$0/month** (free tier sufficient)
- PTY service: **Not supported** (need separate platform)
- **Total: $0 + cost of PTY elsewhere**

#### Hybrid Approach
- **Vercel (docs)** + **Fly.io (PTY)** = $0 + $4-8/month = **$4-8/month total**

---

### 4. Railway 🚂

#### Overview
Developer-friendly platform with Git-based deployment. Simple pricing model.

#### Pricing (2024)
- **Free Tier:** ❌ **Removed August 2023**
- **Hobby Plan:**
  - $5/month base
  - Includes $5 usage credit
  - $0.000463/GB-second RAM
  - $0.000231/vCPU-second
- **Typical Cost:**
  - Small app (512MB, 0.5 vCPU): ~$5-10/month
  - Medium app (1GB, 1 vCPU): ~$10-20/month

#### WebSocket & PTY Support
- ✅ **Full WebSocket support**
- ✅ **Long-running processes**
- ✅ **PTY/terminal apps** work well
- ✅ **PM2 compatible**

#### Deployment
```bash
# Install Railway CLI
npm i -g @railway/cli

# Deploy via CLI or connect GitHub repo
railway login
railway init
railway up
```

#### Pros
- ✅ Simple Git-based deployment
- ✅ WebSocket and PTY support
- ✅ Monorepo support (multiple services)
- ✅ Built-in databases (Postgres, Redis, etc.)
- ✅ Easy environment variables management
- ✅ Good developer UX

#### Cons
- ❌ **No free tier** (removed in 2023)
- ⚠️ $5/month minimum (base fee)
- ⚠️ Usage costs add up quickly
- ⚠️ Less mature than GCP/AWS

#### Best For
- Rapid prototyping and MVPs
- Teams wanting simple deployment
- Projects with $10-20/month budget

#### Estimated Cost for Your Project
- Base fee: **$5/month**
- 2 services (docs + pty): **$5-15/month usage**
- **Total: $10-20/month**

---

### 5. Render 🎨

#### Overview
Modern cloud platform with focus on simplicity. Good Heroku alternative.

#### Pricing (2024)
- **Free Tier:**
  - 750 hours/month free (enough for 1 always-on service)
  - Spins down after 15min inactivity
  - 512MB RAM
- **Starter ($7/month per service):**
  - Always-on
  - 512MB RAM
  - No spin-down
- **Standard ($25/month per service):**
  - 2GB RAM
  - Auto-scaling

#### WebSocket & PTY Support
- ✅ **Full WebSocket support**
- ✅ **Long-running processes**
- ✅ **PTY/terminal apps** work well
- ⚠️ Free tier spins down (not ideal for PTY)

#### Deployment
```bash
# Connect GitHub repo via Render dashboard
# Or use render.yaml for infrastructure-as-code

# render.yaml example:
services:
  - type: web
    name: winged-bean-docs
    env: node
    buildCommand: npm run build
    startCommand: npm run preview
  - type: web
    name: winged-bean-pty
    env: node
    buildCommand: npm install
    startCommand: pm2-runtime start ecosystem.config.js
```

#### Pros
- ✅ **Free tier with 750 hours** (1 service always-on)
- ✅ WebSocket and PTY support
- ✅ Simple Git-based deployment
- ✅ Built-in SSL certificates
- ✅ Good documentation
- ✅ Infrastructure-as-code (render.yaml)

#### Cons
- ⚠️ Free tier spins down after 15min (cold starts)
- ⚠️ Need 2 paid services for docs + PTY ($14/month)
- ⚠️ Slower than edge platforms
- ⚠️ Limited regions (US, EU, Singapore)

#### Best For
- Simple full-stack apps
- Teams wanting Heroku-like experience
- Projects with $7-14/month budget

#### Estimated Cost for Your Project
- **Option A (Free tier):** 1 service free, 1 paid = **$7/month**
- **Option B (Both paid):** 2 services = **$14/month**
- **Recommended:** Docs on Vercel (free) + PTY on Render (free tier) = **$0/month**

---

## Recommendation Matrix

### Scenario 1: **Minimal Cost** ($0-5/month)
**Best Choice: Vercel (docs) + Render Free Tier (PTY)**
- Docs on Vercel: $0/month (free tier)
- PTY on Render: $0/month (free tier, spins down after 15min)
- **Total: $0/month**
- **Trade-off:** PTY service has cold starts (15min inactivity)

### Scenario 2: **Best Performance** ($4-10/month)
**Best Choice: Fly.io (both services)**
- Docs + PTY on Fly.io: $4-8/month (2 small VMs)
- **Total: $4-10/month**
- **Benefits:** Zero cold starts, perfect WebSocket support, global edge

### Scenario 3: **Hybrid Optimal** ($4-8/month)
**Best Choice: Vercel (docs) + Fly.io (PTY)**
- Docs on Vercel: $0/month (free tier, excellent Astro support)
- PTY on Fly.io: $4-8/month (always-on, zero cold starts)
- **Total: $4-8/month**
- **Benefits:** Best of both worlds - fast docs + reliable PTY

### Scenario 4: **Simplest Setup** ($7-14/month)
**Best Choice: Render (both services)**
- Docs on Render: $7/month (Starter tier)
- PTY on Render: $0-7/month (free tier or Starter)
- **Total: $7-14/month**
- **Benefits:** Single platform, simple deployment, good DX

---

## Final Recommendation

### 🏆 **Winner: Vercel (docs) + Fly.io (PTY)**

**Rationale:**
1. **Vercel for Docs:**
   - Zero-config Astro deployment
   - Free tier covers your traffic
   - Global edge network (fast)
   - Automatic preview deployments
   - No cold starts for static content

2. **Fly.io for PTY Service:**
   - Perfect WebSocket/PTY support
   - Zero cold starts (always-on VM)
   - Reliable terminal sessions
   - SSH access for debugging
   - Multi-region if needed

**Total Cost:** **$4-8/month**

**Setup Steps:**
1. Deploy docs to Vercel (5 minutes)
2. Deploy PTY service to Fly.io (10 minutes)
3. Update demo page WebSocket URL to Fly.io endpoint
4. Set up GitHub Actions for E2E tests

---

## Alternative: If Budget is $0

### **Render Free Tier (PTY only) + Vercel (docs)**

**Setup:**
- Docs on Vercel: Free
- PTY on Render Free Tier: Free (with 15min spin-down)

**Trade-offs:**
- PTY service spins down after 15min inactivity
- Cold start: 30-60s to wake up
- Not ideal for live demos, but fine for E2E testing

**Total Cost:** **$0/month**

---

## Implementation Priority

### Phase 1: Vercel Docs Deployment (Week 1)
- [ ] Add `vercel.json` config
- [ ] Connect GitHub repo to Vercel
- [ ] Set up preview deployments
- [ ] Update E2E tests to run against Vercel preview URLs

### Phase 2: Fly.io PTY Deployment (Week 2)
- [ ] Create `fly.toml` for pty-service
- [ ] Deploy to Fly.io
- [ ] Update demo page WebSocket URL
- [ ] Test E2E against live PTY service

### Phase 3: CI/CD Integration (Week 3)
- [ ] GitHub Actions workflow for E2E tests
- [ ] Run tests on PR preview deployments
- [ ] Set up deployment notifications

---

## Cost Projection (12 months)

| Platform Combo | Month 1-3 | Month 4-12 | Year 1 Total |
|----------------|-----------|------------|--------------|
| **Vercel + Fly.io** | $4-8 | $4-8 | **$48-96** |
| **Vercel + Render Free** | $0 | $0 | **$0** |
| **Fly.io Only** | $4-10 | $4-10 | **$48-120** |
| **Render Paid** | $7-14 | $7-14 | **$84-168** |
| **Railway** | $10-20 | $10-20 | **$120-240** |
| **GCP Cloud Run** | $5-16 | $5-16 | **$60-192** |

---

## Conclusion

For your Winged Bean project, I recommend:

1. **Start with:** Vercel (docs) + Render Free Tier (PTY) = **$0/month**
   - Test the workflow with zero cost
   - Accept cold starts for initial testing

2. **Upgrade to:** Vercel (docs) + Fly.io (PTY) = **$4-8/month**
   - When you need reliable live demos
   - Zero cold starts, perfect WebSocket support

3. **Avoid:** GCP Cloud Run for PTY (60min timeout is a deal-breaker)

**Next Steps:**
- Deploy docs to Vercel (I can create config files)
- Deploy PTY to Render free tier or Fly.io
- Set up GitHub Actions for automated E2E testing

Would you like me to create the deployment configs for Vercel + Fly.io or Vercel + Render?

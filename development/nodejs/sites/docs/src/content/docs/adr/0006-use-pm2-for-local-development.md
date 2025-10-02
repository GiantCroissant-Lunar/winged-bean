---
title: ADR-0006: Use PM2 for Local Development Workflow
---

# ADR-0006: Use PM2 for Local Development Workflow

**Status:** Accepted
**Date:** 2025-09-30
**Deciders:** Development Team
**Technical Story:** Multi-service development workflow for Terminal.Gui PTY integration

## Context and Problem Statement

The Terminal.Gui PTY web integration requires running multiple services simultaneously during development:

1. Node.js PTY WebSocket service (port 4041)
2. Astro development server (port 4321)
3. Optional: .NET Terminal.Gui application (spawned by PTY service)

Developers need a simple way to:

- Start all services with one command
- View aggregated logs from all services
- Auto-restart services on code changes
- Monitor service health and status
- Stop all services cleanly

We needed to decide between multiple approaches for managing these services during local development.

## Decision Drivers

- **Developer Experience**: Minimize setup complexity and cognitive load
- **Hot Reload**: Support fast iteration cycles with automatic restarts
- **Log Management**: Aggregate and format logs from multiple services
- **Cross-Platform**: Work on Windows, macOS, and Linux
- **Production Similarity**: Development setup should reflect production architecture
- **Debugging**: Easy to attach debuggers and inspect individual services
- **Maintainability**: Simple configuration, well-documented, community-supported

## Considered Options

### Option 1: Custom Shell Scripts

```bash
#!/bin/bash
trap 'kill 0' SIGINT
cd pty-service && npm run dev &
cd sites/docs && npm run dev &
wait
```

**Pros:**

- No dependencies
- Simple to understand
- Full control over process management

**Cons:**

- Platform-specific (bash vs PowerShell)
- Manual process tracking
- No log aggregation
- No automatic restart on crashes
- Complex signal handling
- No status monitoring

### Option 2: npm-run-all / concurrently

```json
{
  "scripts": {
    "dev": "concurrently \"npm:dev:*\"",
    "dev:pty": "cd pty-service && node server.js",
    "dev:astro": "cd sites/docs && npm run dev"
  }
}
```

**Pros:**

- npm-native solution
- Simple parallel execution
- Good for simple workflows

**Cons:**

- No process management (restart on crash)
- Logs mixed together without clear formatting
- No status monitoring
- No individual service control (must restart all)
- Limited watch mode capabilities

### Option 3: Docker Compose

```yaml
services:
  pty-service:
    build: ./pty-service
    volumes:
      - ./pty-service:/app
  docs-site:
    build: ./sites/docs
    volumes:
      - ./sites/docs:/app
```

**Pros:**

- Production-like environment
- Isolation and reproducibility
- Good for complex dependencies

**Cons:**

- Complex setup for simple use case
- Slower hot reload (container overhead)
- .NET app spawning complications (Docker-in-Docker or separate container)
- Heavier resource usage
- More complex debugging

### Option 4: PM2 (Process Manager 2) ✅ **SELECTED**

```javascript
// ecosystem.config.js
module.exports = {
  apps: [
    {
      name: 'pty-service',
      script: 'server.js',
      watch: ['server.js'],
      env: { NODE_ENV: 'development' }
    },
    {
      name: 'docs-site',
      cwd: './sites/docs',
      script: 'npm',
      args: 'run dev'
    }
  ]
};
```

**Pros:**

- ✅ Production-grade process manager
- ✅ Built-in file watching and auto-restart
- ✅ Excellent log aggregation (`pm2 logs`)
- ✅ Status monitoring (`pm2 status`, `pm2 monit`)
- ✅ Individual service control (`pm2 restart pty-service`)
- ✅ Cross-platform (Windows, macOS, Linux)
- ✅ Active community support
- ✅ Can also be used in production
- ✅ Native Node.js processes (no containerization overhead)

**Cons:**

- Additional dependency (but lightweight)
- Learning curve (minimal)
- Daemon process (background management)

## Decision Outcome

**Chosen option:** PM2 (Option 4)

PM2 provides the best balance of developer experience, features, and maintainability for our multi-service development workflow.

### Implementation Details

**1. Configuration: `ecosystem.config.js`**

```javascript
module.exports = {
  apps: [
    {
      name: 'pty-service',
      cwd: './pty-service',
      script: 'server.js',
      watch: ['server.js'],
      ignore_watch: ['node_modules', 'logs', '*.log'],
      env: {
        NODE_ENV: 'development',
        PORT: 4041
      },
      error_file: './logs/pty-service-error.log',
      out_file: './logs/pty-service-out.log',
      log_date_format: 'YYYY-MM-DD HH:mm:ss Z',
      merge_logs: true
    },
    {
      name: 'docs-site',
      cwd: './sites/docs',
      script: 'npm',
      args: 'run dev',
      watch: false, // Astro has its own watcher
      env: {
        NODE_ENV: 'development'
      },
      error_file: './logs/docs-site-error.log',
      out_file: './logs/docs-site-out.log',
      log_date_format: 'YYYY-MM-DD HH:mm:ss Z',
      merge_logs: true
    }
  ]
};
```

**2. Package Scripts:**

```json
{
  "scripts": {
    "dev": "pm2 start ecosystem.config.js",
    "dev:stop": "pm2 stop ecosystem.config.js",
    "dev:restart": "pm2 restart ecosystem.config.js",
    "dev:logs": "pm2 logs",
    "dev:status": "pm2 status",
    "dev:monit": "pm2 monit"
  }
}
```

**3. Development Workflow:**

```bash
# Start all services
pnpm run dev

# View logs
pnpm run dev:logs

# Check status
pnpm run dev:status

# Monitor in real-time
pnpm run dev:monit

# Restart specific service
pm2 restart pty-service

# Stop all
pnpm run dev:stop
```

### Positive Consequences

- **Single Command Startup:** `pnpm run dev` starts everything
- **Automatic Recovery:** Services restart on crashes
- **File Watching:** PTY service auto-restarts on code changes
- **Aggregated Logs:** All service logs in one view with timestamps
- **Status Dashboard:** Real-time monitoring of CPU, memory, uptime
- **Individual Control:** Restart/stop individual services without affecting others
- **Production Ready:** PM2 can also manage production deployments

### Negative Consequences

- **Additional Dependency:** Adds PM2 to devDependencies (~5MB)
- **PM2 Daemon:** Background process management (requires `pm2 kill` to fully stop)
- **Learning Curve:** Developers need to learn basic PM2 commands

### Mitigation Strategies

1. **Documentation:** Comprehensive README with common commands
2. **Package Scripts:** Wrap PM2 commands in npm scripts for convenience
3. **Graceful Fallback:** Manual start commands still work (`node server.js`)

## Testing Strategy

Comprehensive test suite validates the PM2 workflow:

**Unit Tests:**

- PTY process spawning
- WebSocket server functionality

**Integration Tests:**

- PM2 service lifecycle (start, status, logs, stop)
- File watching behavior (manual verification)
- End-to-end browser tests (Playwright)

**Test Commands:**

```bash
pnpm test              # All tests
pnpm test:unit         # Unit tests only
pnpm test:integration  # Integration tests
pnpm test:pm2          # PM2 workflow tests
pnpm test:e2e          # Browser automation tests
```

## Links

- **PM2 Documentation:** <https://pm2.keymetrics.io/docs/usage/quick-start/>
- **Ecosystem File:** <https://pm2.keymetrics.io/docs/usage/application-declaration/>
- **Implementation:** `projects/nodejs/ecosystem.config.js`
- **Test Suite:** `projects/nodejs/tests/integration/pm2-lifecycle.test.js`
- **Related ADR:** [ADR-0005: Use PTY for Terminal.Gui Web Integration](0005-use-pty-for-terminal-gui-web-integration.md)

## Future Considerations

### Docker Compose for Production

While PM2 is excellent for local development, Docker Compose may still be valuable for:

- Production deployments with orchestration
- Integration testing in CI/CD
- Multi-environment configuration management

PM2 and Docker are not mutually exclusive - PM2 can run inside Docker containers.

### When to Reconsider

Consider alternative solutions if:

- Services exceed 5-10 processes (complexity threshold)
- Need for complex service dependencies (startup ordering)
- Cross-language service management becomes cumbersome
- Production deployment diverges significantly from development setup

## Notes

- The `.NET Terminal.Gui application` is not managed by PM2 directly - it's spawned by the PTY service on WebSocket connections
- File watching for PTY service uses PM2's built-in `watch` feature
- Astro's Vite dev server has its own file watching, so PM2 watch is disabled for it
- Log files are written to `<service>/logs/` directories (automatically created)

---

**Last Updated:** 2025-09-30
**Next Review:** 2025-12-30 (or when adding new services)

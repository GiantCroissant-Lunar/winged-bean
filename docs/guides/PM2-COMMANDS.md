# PM2 Management Commands

## Quick Start
```bash
# Start all services
pm2 start ecosystem.config.js

# Stop all services
pm2 stop all

# Restart all services
pm2 restart all

# Delete all services
pm2 delete all
```

## Service Status
```bash
# View status of all services
pm2 status

# View detailed info
pm2 info <service-name>

# Monitor services in real-time
pm2 monit
```

## Logs
```bash
# View all logs (live stream)
pm2 logs

# View specific service logs
pm2 logs pty-service
pm2 logs docs-site
pm2 logs console-dungeon

# View last N lines without streaming
pm2 logs pty-service --lines 50 --nostream

# Clear logs
pm2 flush
```

## Individual Service Control
```bash
# Restart specific service
pm2 restart pty-service
pm2 restart docs-site
pm2 restart console-dungeon

# Stop specific service
pm2 stop pty-service

# Start specific service
pm2 start pty-service
```

## Service URLs
- **Web (Docs Site)**: http://localhost:4000
- **PTY Service WebSocket**: ws://localhost:4041
- **Console Dungeon WebSocket**: ws://localhost:4040

## Troubleshooting

### PTY Service Native Module Issues
If you see `NODE_MODULE_VERSION` errors:
```bash
cd build/_artifacts/latest/pty/dist
npm rebuild node-pty
pm2 restart pty-service
```

### Check Ports
```bash
lsof -i :4000 -i :4040 -i :4041 | grep LISTEN
```

### Rebuild and Sync Artifacts
```bash
cd build
task console:build
task artifacts:sync-from-source
pm2 restart all
```

## PM2 Persistence
```bash
# Save current process list
pm2 save

# Startup script for auto-restart on boot
pm2 startup

# Resurrect saved processes
pm2 resurrect
```

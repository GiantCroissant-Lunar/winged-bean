# Winged Bean - Quick Start Guide

## Start Services
```bash
cd build
task dev:start
```

Services will start:
- **Web**: http://localhost:4321/
- **PTY Demo**: http://localhost:4321/demo/
- **PTY WebSocket**: ws://localhost:4041

## Stop Services
```bash
cd build
task dev:stop
```

## Build Everything
```bash
cd build
task build-all
```

## Run Tests
```bash
# Quick check
cd build
task capture:quick

# Full E2E
cd build
task test-e2e

# Console standalone
cd build
task console:debug
```

## Check Status
```bash
cd build
task dev:status
pm2 logs
```

## Recent Fix (2025-10-09)
✅ PTY integration now works!
- Console app runs in browser terminal
- Interactive gameplay functional
- See PTY-FIX-HANDOVER.md for details

## Troubleshooting
If services won't start:
```bash
pm2 delete all
cd build
task build-all
task dev:start
```

If PTY not connecting, check logs:
```bash
cat build/_artifacts/latest/dotnet/bin/logs/diagnostic-startup-*.log
pm2 logs pty-service
```

## File Locations
- **Build artifacts**: `build/_artifacts/0.0.1-392/`
- **Logs**: `build/_artifacts/latest/dotnet/bin/logs/`
- **PTY service**: `development/nodejs/pty-service/`
- **Console app**: `development/dotnet/console/`

## Key Commands Summary
| Command | Purpose |
|---------|---------|
| `task build-all` | Build everything |
| `task dev:start` | Start PM2 services |
| `task dev:stop` | Stop PM2 services |
| `task dev:status` | Check service status |
| `task capture:quick` | Quick E2E test |
| `task test-e2e` | Full E2E tests |
| `task console:debug` | Run console standalone |
| `pm2 logs` | View all service logs |
| `pm2 delete all` | Clean reset PM2 |

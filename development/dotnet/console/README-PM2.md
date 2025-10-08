# PM2 Development Workflow

This document explains how to run ConsoleDungeon from versioned artifacts using PM2.

## Overview

The workflow ensures you always run from properly built versioned artifacts instead of directly from source:

1. **Build** creates versioned artifacts (e.g., `v0.0.1-373`)
2. **Build also copies to `latest`** directory automatically
3. **PM2 runs from `latest`** directory
4. **After any rebuild**, just restart PM2 to pick up the new version

## Workflow

### 1. Build Version Artifacts

```bash
cd /path/to/yokan-projects/winged-bean/build
task build-all
```

This will:
- Build .NET projects (host + plugins)
- Build web projects (docs site)
- Build PTY service
- Create `_artifacts/v{VERSION}/` directory
- **Copy to `_artifacts/latest/`** automatically

### 2. Start with PM2

```bash
cd /path/to/console
pm2 start ecosystem.config.js
```

PM2 will run from `_artifacts/latest/dotnet/bin/ConsoleDungeon.Host`

### 3. After Code Changes

```bash
# Rebuild (updates latest automatically)
cd /path/to/build
task build-all

# Restart PM2 to pick up new build
pm2 restart console-dungeon
```

## PM2 Commands

```bash
# View status
pm2 list
pm2 status console-dungeon

# View logs
pm2 logs console-dungeon
pm2 logs console-dungeon --lines 100

# Control
pm2 stop console-dungeon
pm2 restart console-dungeon
pm2 delete console-dungeon

# Info
pm2 info console-dungeon
pm2 show console-dungeon
```

## Directory Structure

```
yokan-projects/winged-bean/
├── build/
│   ├── _artifacts/
│   │   ├── latest/              ← PM2 runs from here
│   │   │   ├── dotnet/bin/
│   │   │   ├── web/dist/
│   │   │   └── pty/dist/
│   │   ├── v0.0.1-373/          ← Versioned builds
│   │   └── v0.0.1-374/
│   └── Taskfile.yml
└── development/dotnet/console/
    └── ecosystem.config.js      ← PM2 config
```

## Benefits

✅ **Always run from artifacts** - Not from Debug bin directories  
✅ **Version tracking** - Each build creates a versioned directory  
✅ **Simple updates** - Just rebuild and restart  
✅ **Consistent with CI** - Same artifacts locally and in CI  
✅ **No hardcoded versions** - ecosystem.config.js uses `latest`

## Ecosystem Config

The `ecosystem.config.js` is configured to run from the `latest` directory:

```javascript
{
  name: 'console-dungeon',
  cwd: '/path/to/build/_artifacts/latest/dotnet/bin',
  script: './ConsoleDungeon.Host',
  // ...
}
```

This means you never need to update the config when versions change!

## Troubleshooting

### PM2 says "App not found"
Make sure you've built at least once:
```bash
cd build && task build-all
```

### Changes not taking effect
Make sure to restart PM2 after rebuild:
```bash
pm2 restart console-dungeon
```

### Check what version is running
```bash
ls -la /path/to/build/_artifacts/latest/
cat /path/to/build/_artifacts/latest/dotnet/bin/ConsoleDungeon.Host.runtimeconfig.json
```

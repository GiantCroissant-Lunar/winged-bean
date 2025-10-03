# Build and Development Tasks

This directory contains the main build orchestration for the Winged Bean project using [Task](https://taskfile.dev/).

## Quick Start

### Development Services

Start all development services (PTY, Astro docs, Console app) with PM2:

```bash
task dev:start    # Start all services
task dev:status   # Check status
task dev:logs     # View logs
task dev:restart  # Restart all services
task dev:stop     # Stop all services
```

### Build Tasks

```bash
task setup        # Initial project setup
task build-all    # Build all components (dotnet, web, pty)
task ci           # Full CI pipeline (clean, build, test)
task version      # Show current GitVersion
```

### Component-Specific Tasks

Tasks are namespaced by component. Use `task --list` to see all available tasks.

#### .NET Console Tasks

```bash
task console:build   # Build ConsoleDungeon game
task console:test    # Run tests
task console:run     # Run the game directly
task console:clean   # Clean build artifacts
```

#### Node.js Tasks

```bash
task nodejs:install  # Install dependencies
task nodejs:build    # Build web projects
task nodejs:test     # Run unit tests
task nodejs:test-e2e # Run E2E tests with Playwright
task nodejs:lint     # Lint JavaScript/TypeScript
task nodejs:format   # Format code with Prettier
task nodejs:dev      # Start development server
task nodejs:clean    # Clean artifacts
```

## Project Structure

```
build/
├── Taskfile.yml           # Main build orchestration (includes sub-taskfiles)
├── Taskfile.deploy.yml    # Deployment tasks
├── get-version.sh         # Version management script
├── nuke/                  # Nuke build system for .NET
└── _artifacts/            # Build outputs (versioned)
    └── v{version}/
        ├── dotnet/        # .NET binaries, logs
        ├── web/           # Web dist, logs
        ├── pty/           # PTY service dist, logs
        └── _logs/         # Build logs

development/
├── dotnet/
│   └── console/
│       └── Taskfile.yml   # Console-specific tasks
└── nodejs/
    ├── Taskfile.yml       # Node.js-specific tasks
    └── ecosystem.config.js # PM2 configuration
```

## Task System Architecture

The project uses a hierarchical Task structure:

- **Main Taskfile** (`build/Taskfile.yml`) - Orchestrates all builds and includes sub-taskfiles
- **Sub-Taskfiles** - Located in component directories for local development convenience
  - `development/dotnet/console/Taskfile.yml`
  - `development/nodejs/Taskfile.yml`

Developers can run tasks from either location:
- From `build/`: `task console:build` (namespaced)
- From `development/dotnet/console/`: `task build` (direct)

## PM2 Process Manager

Development services are managed by [PM2](https://pm2.keymetrics.io/):

- **pty-service**: WebSocket server on port 4041
- **docs-site**: Astro documentation site on http://localhost:4321/
- **console-dungeon**: .NET console app (accessed via PTY service)

### Manual PM2 Commands

```bash
cd development/nodejs
pm2 list                    # Show all processes
pm2 logs [service-name]     # View logs
pm2 restart [service-name]  # Restart specific service
pm2 stop all                # Stop all services
pm2 delete all              # Remove all processes
```

## CI/CD

The `ci` task runs the complete CI pipeline:

```bash
task ci
```

This executes:
1. `clean` - Remove all artifacts
2. `build-all` - Build dotnet, web, and pty components
3. `nodejs:test` - Run Node.js unit tests
4. `nodejs:test-e2e` - Run Playwright E2E tests

## Artifacts

Build artifacts are versioned and stored in `_artifacts/v{version}/`:

- **dotnet/bin/** - Compiled .NET binaries
- **web/dist/** - Built Astro site
- **pty/dist/** - PTY service files
- **_logs/** - Build logs for troubleshooting

## Version Management

Project version is managed via GitVersion. View current version:

```bash
task version
```

## Related Files

- `.github/copilot-instructions.md` - Agent rules and conventions
- `development/dotnet/console/AGENTS.md` - .NET-specific agent instructions
- `development/nodejs/AGENTS.md` - Node.js-specific agent instructions

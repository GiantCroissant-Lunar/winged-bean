# Winged Bean Documentation Site

An Astro-based documentation site featuring terminal demos and live WebSocket-connected terminals.

## Features

- **Asciinema Player**: Interactive terminal session recordings with multiple demo selections
- **xterm.js Terminal**: Live WebSocket-connected terminal for real-time console application interaction
- **Terminal.Gui Integration**: Connect to .NET console applications running Terminal.Gui v2
- **SuperSocket WebSocket**: Real-time communication between web frontend and .NET backend

## Project Structure

```text
/
├── public/
│   ├── *.cast              # Asciinema recording files
│   └── asciinema-player.min.js
├── src/
│   ├── components/
│   │   ├── AsciinemaPlayer.astro    # Asciinema player component
│   │   └── XTerm.astro              # xterm.js WebSocket terminal
│   └── pages/
│       └── index.astro              # Main documentation page
└── package.json
```

## Getting Started

### Install Dependencies

```bash
npm install
```

### Start Development Server

```bash
npm run dev
```

Open [http://localhost:4321](http://localhost:4321) in your browser.

## Components

### AsciinemaPlayer

Displays interactive terminal session recordings. Features:

- Multiple cast file selection
- Preloaded playback
- Responsive design

### XTerm

Provides a live terminal interface connected via WebSocket. Features:

- Real-time connection to .NET console applications
- Automatic reconnection
- Dark theme with syntax highlighting
- Responsive terminal sizing

## Console Dungeon Integration

To use the live terminal feature:

1. **Start the .NET Console Application**:

   ```bash
   cd ../../../dotnet/console-dungeon
   dotnet run --project ConsoleDungeon/ConsoleDungeon.csproj
   ```

2. **Open the Web Interface**:
   - Navigate to the "Live Terminal" section
   - The xterm.js terminal will automatically connect to `ws://localhost:4040`
   - Type commands and see real-time responses from the Terminal.Gui application

## Demo Cast Files

The site includes several sample terminal recordings:

- `example.cast` - Basic hello world demo
- `npm-install.cast` - NPM package installation
- `git-commit.cast` - Git workflow demonstration
- `docker-build.cast` - Docker container building

## Dependencies

- `astro` - Static site generator
- `asciinema-player` - Terminal session player
- `xterm` - Terminal emulator for web
- `@xterm/addon-fit` - Auto-fitting for xterm

const pty = require("node-pty");
const WebSocket = require("ws");
const path = require("path");
const fs = require("fs");
const RecordingManager = require("./recording-manager");
const { getArtifactsPath } = require("./get-version");

// WebSocket server configuration
// Try to read the port from websocket-port.txt written by ConsoleDungeon.Host
// Otherwise use environment variable or default
function discoverWebSocketPort() {
  const portFilePath = path.join(getArtifactsPath("dotnet", "bin"), "websocket-port.txt");
  const logsPortFilePath = path.join(getArtifactsPath("dotnet", "bin"), "logs", "websocket-port.txt");

  // Try reading from port info file first
  for (const filePath of [portFilePath, logsPortFilePath]) {
    try {
      if (fs.existsSync(filePath)) {
        const portStr = fs.readFileSync(filePath, "utf8").trim();
        const port = parseInt(portStr, 10);
        if (!isNaN(port) && port > 0) {
          console.log(`✓ Discovered WebSocket port from ${filePath}: ${port}`);
          return port;
        }
      }
    } catch (err) {
      // Ignore and try next
    }
  }

  // Fallback to environment variable or default
  const envPort = process.env.PTY_WS_PORT || process.env.DUNGEON_WS_PORT;
  if (envPort) {
    const port = parseInt(envPort, 10);
    if (!isNaN(port)) {
      console.log(`✓ Using WebSocket port from environment: ${port}`);
      return port;
    }
  }

  console.log(`✓ Using default WebSocket port: 4041`);
  return 4041; // Default
}

const WS_PORT = discoverWebSocketPort();

// Terminal.Gui application configuration
// ALWAYS run from versioned artifacts so plugins resolve correctly (R-CODE-050)
const DOTNET_DLL = "ConsoleDungeon.Host.dll";
const BIN_DIR = getArtifactsPath("dotnet", "bin");

console.log("Terminal.Gui PTY Service starting...");

// Create recording manager with versioned artifacts path (per RFC-0010)
const recordingsDir = getArtifactsPath("pty", "recordings");
const recordingManager = new RecordingManager(recordingsDir);
console.log(`Recording manager initialized: ${recordingsDir}`);

// Create WebSocket server
const wss = new WebSocket.Server({ port: WS_PORT });

console.log(`WebSocket server listening on port ${WS_PORT}`);
console.log(`Ready to spawn from: ${BIN_DIR}/${DOTNET_DLL}`);

// Handle WebSocket connections
wss.on("connection", (ws) => {
  console.log("WebSocket client connected");

  // Spawn .NET application via shell with exec to ensure proper runtime initialization
  // Direct spawning of dotnet with DLL path fails in PTY without shell context
  const ptyProcess = pty.spawn("sh", ["-c", `cd "${BIN_DIR}" && exec dotnet ./${DOTNET_DLL}`, "sh"], {
    name: "xterm-256color",
    cols: 80,
    rows: 24,
    env: {
      ...process.env,
      // Try different TERM settings for Terminal.Gui compatibility
      TERM: "xterm-256color",
      COLORTERM: "truecolor",
      LANG: process.env.LANG || "en_US.UTF-8",
      LC_ALL: process.env.LC_ALL || "en_US.UTF-8",
    },
  });

  console.log(`PTY process spawned with PID: ${ptyProcess.pid}`);

  // Send a test message immediately to verify WebSocket connection
  const testMessage = '\r\n\x1b[32m✓ WebSocket connection successful!\x1b[0m\r\n\x1b[33mPTY process spawned, console app starting...\x1b[0m\r\n';
  console.log('Sending test message immediately');
  try {
    ws.send(Buffer.from(testMessage, 'utf8'));
    console.log('Test message sent successfully');
  } catch (e) {
    console.log('Error sending test message:', e.message);
  }

  // Send some test input to the console app after a delay
  setTimeout(() => {
    console.log('Sending test input to console app');
    if (!ptyProcess.killed) {
      ptyProcess.write('test input\r');
    }
  }, 2000);

  // Stream PTY output to WebSocket client (binary frames)
  ptyProcess.onData((data) => {
    const dataStr = data.toString();

    // Check for recording control sequences (OSC 1337)
    if (dataStr.includes('\x1b]1337;StartRecording\x07')) {
      const filename = recordingManager.startRecording();
      if (filename) {
        const msg = `\r\n🔴 Recording started: ${filename}\r\n`;
        if (ws.readyState === WebSocket.OPEN) {
          ws.send(Buffer.from(msg, "utf8"));
        }
      }
      // Filter out control sequence, don't send to browser
      return;
    }

    if (dataStr.includes('\x1b]1337;StopRecording\x07')) {
      recordingManager.stopRecording().then(info => {
        if (info) {
          const msg = `\r\n⏹️  Recording stopped: ${info.filename} (${info.frameCount} frames, ${info.duration}s)\r\n`;
          if (ws.readyState === WebSocket.OPEN) {
            ws.send(Buffer.from(msg, "utf8"));
          }
        }
      });
      // Filter out control sequence, don't send to browser
      return;
    }

    // Record data if recording is active
    if (recordingManager.isRecording()) {
      recordingManager.writeData(data);
    }

    // Send to WebSocket client
    if (ws.readyState === WebSocket.OPEN) {
      // Send as binary to preserve all bytes exactly as PTY outputs them
      ws.send(Buffer.from(data, "utf8"));
    }
  });

  // Handle PTY process exit
  ptyProcess.onExit(({ exitCode, signal }) => {
    console.log(`PTY process exited with code ${exitCode}, signal ${signal}`);

    // Send exit information to client
    const exitMessage = `\r\n\r\n🔴 Console app exited (code: ${exitCode}, signal: ${signal})\r\n`;
    if (ws.readyState === WebSocket.OPEN) {
      ws.send(Buffer.from(exitMessage, 'utf8'));
      ws.close(1000, `Process exited with code ${exitCode}`);
    }
  });

  // No need to send commands - .NET app is spawned directly in PTY

  // Keepalive: send ping frames periodically; browser replies with pong automatically
  let pingInterval = setInterval(() => {
    try {
      if (ws.readyState === WebSocket.OPEN) ws.ping();
    } catch {}
  }, 10000);

  // Handle WebSocket messages (input from xterm.js)
  ws.on("message", (message) => {
    try {
      // Handle JSON messages (resize, etc.)
      const data = JSON.parse(message.toString());

      if (data.type === "resize") {
        try {
          const cols = Math.max(20, Math.min(300, data.cols | 0));
          const rows = Math.max(5, Math.min(120, data.rows | 0));
          console.log(`Resize request: ${cols}x${rows}`);
          ptyProcess.resize(cols, rows);
        } catch (e) {
          console.warn("Resize failed:", e?.message || e);
        }
        return;
      }
      if (data.type === "heartbeat") {
        // Client heartbeat - acknowledge optionally
        // No-op keeps the connection active
        return;
      }
    } catch (e) {
      // Not JSON, treat as raw input data
      const input = message.toString();

      // Log ALL input for debugging
      console.log(`[INPUT] Length: ${input.length}, Bytes: ${JSON.stringify(input)}, Hex: ${Buffer.from(input).toString('hex')}`);

      // Temporary debug: classify common escape sequences for arrows
      const arrowMap = {
        "\u001b[A": "ArrowUp",
        "\u001b[B": "ArrowDown",
        "\u001b[C": "ArrowRight",
        "\u001b[D": "ArrowLeft",
      };
      const arrow = arrowMap[input];
      if (arrow) {
        console.log(`Detected Arrow: ${arrow} (${JSON.stringify(input)})`);
      }
      // Normalize SS3 (ESC O A/B/C/D) to CSI (ESC [ A/B/C/D) for broader compatibility
      // Some browsers/terminals send SS3 for arrows, which Terminal.Gui may not decode
      const normalized = input
        .replace(/\u001bOA/g, '\u001b[A')
        .replace(/\u001bOB/g, '\u001b[B')
        .replace(/\u001bOC/g, '\u001b[C')
        .replace(/\u001bOD/g, '\u001b[D');
      if (normalized !== input) {
        console.log(`[NORMALIZE] SS3->CSI: ${JSON.stringify(input)} -> ${JSON.stringify(normalized)}`);
      }
      if (!ptyProcess.killed) {
        ptyProcess.write(normalized);
      }
    }
  });

  // Handle WebSocket close
  ws.on("close", (code, reason) => {
    console.log(`WebSocket client disconnected: ${code} ${reason}`);
    clearInterval(pingInterval);

    // Stop recording if active
    if (recordingManager.isRecording()) {
      recordingManager.stopRecording().then(info => {
        if (info) {
          console.log(`Auto-stopped recording on disconnect: ${info.filename}`);
        }
      });
    }

    if (!ptyProcess.killed) {
      console.log("Killing PTY process...");
      ptyProcess.kill();
    }
  });

  // Handle WebSocket errors
  ws.on("error", (error) => {
    console.error("WebSocket error:", error);
    clearInterval(pingInterval);
    if (!ptyProcess.killed) {
      ptyProcess.kill();
    }
  });

  // Don't do automatic resize - let it use default PTY size
});

// Handle server errors
wss.on("error", (error) => {
  console.error("WebSocket server error:", error);
});

// Graceful shutdown
process.on("SIGINT", () => {
  console.log("\nShutting down PTY service...");
  wss.close(() => {
    console.log("WebSocket server closed");
    process.exit(0);
  });
});

process.on("SIGTERM", () => {
  console.log("\nReceived SIGTERM, shutting down gracefully...");
  wss.close(() => {
    process.exit(0);
  });
});

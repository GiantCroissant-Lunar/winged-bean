const pty = require("node-pty");
const WebSocket = require("ws");
const path = require("path");
const RecordingManager = require("./recording-manager");
const { getArtifactsPath } = require("../get-version");

// WebSocket server configuration
const WS_PORT = 4041;

// Terminal.Gui application configuration
// Prefer running from build output so that plugins.json and plugins/ resolve correctly
const PROJECT_DIR = path.resolve(
  __dirname,
  "../../dotnet/console/src/host/ConsoleDungeon.Host",
);
const BIN_DIR = path.resolve(
  PROJECT_DIR,
  "bin/Debug/net8.0"
);
const DOTNET_DLL = "ConsoleDungeon.Host.dll";

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

  // Spawn .NET application from build output so relative plugin paths resolve
  const cmd = `TERM=xterm-256color COLORTERM=truecolor dotnet ./${DOTNET_DLL}`;
  const ptyProcess = pty.spawn("/bin/bash", ["-lc", cmd], {
    name: "xterm-256color",
    cols: 80,
    rows: 24,
    cwd: BIN_DIR,
    env: {
      ...process.env,
      TERM: "xterm-256color",
      COLORTERM: "truecolor",
      LANG: process.env.LANG || "en_US.UTF-8",
      LC_ALL: process.env.LC_ALL || "en_US.UTF-8",
    },
  });

  console.log(`PTY process spawned with PID: ${ptyProcess.pid}`);

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
    if (ws.readyState === WebSocket.OPEN) {
      ws.close(1000, `Process exited`);
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
      console.log(`Sending input to PTY: ${JSON.stringify(input)}`);
      if (!ptyProcess.killed) {
        ptyProcess.write(input);
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

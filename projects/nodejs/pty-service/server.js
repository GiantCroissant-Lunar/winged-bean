const pty = require("node-pty");
const WebSocket = require("ws");
const path = require("path");

// WebSocket server configuration
const WS_PORT = 4041;

// Terminal.Gui application configuration
const DOTNET_PROJECT_PATH = path.resolve(
  __dirname,
  "../../../projects/dotnet/console/src/host/TerminalGui.PtyHost/TerminalGui.PtyHost.csproj",
);

console.log("Terminal.Gui PTY Service starting...");

// Create WebSocket server
const wss = new WebSocket.Server({ port: WS_PORT });

console.log(`WebSocket server listening on port ${WS_PORT}`);
console.log(`Ready to spawn Terminal.Gui from: ${DOTNET_PROJECT_PATH}`);

// Handle WebSocket connections
wss.on("connection", (ws) => {
  console.log("WebSocket client connected");

  // Spawn .NET application directly in PTY (via bash to ensure proper TTY environment)
  const cmd = `TERM=xterm-256color COLORTERM=truecolor dotnet run --project "${DOTNET_PROJECT_PATH}"`;
  const ptyProcess = pty.spawn("/bin/bash", ["-lc", cmd], {
    name: "xterm-256color",
    cols: 80,
    rows: 24,
    cwd: path.dirname(DOTNET_PROJECT_PATH),
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
    if (!ptyProcess.killed) {
      console.log("Killing PTY process...");
      ptyProcess.kill();
    }
  });

  // Handle WebSocket errors
  ws.on("error", (error) => {
    console.error("WebSocket error:", error);
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

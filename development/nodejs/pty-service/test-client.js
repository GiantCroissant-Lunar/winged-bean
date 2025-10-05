const WebSocket = require("ws");
const url = "ws://localhost:4041";
console.log("Connecting to", url);
const ws = new WebSocket(url);
ws.binaryType = "arraybuffer";

let received = 0;
ws.on("open", () => {
  console.log("Client connected. Sending resize...");
  ws.send(JSON.stringify({ type: "resize", cols: 80, rows: 24 }));
  // Send a newline after a short delay to wake apps that prompt
  setTimeout(() => {
    try {
      ws.send("\r");
    } catch (e) {}
  }, 300);
  // Close after a few seconds to avoid hanging
  setTimeout(() => {
    console.log("Closing test client...");
    ws.close(1000, "done");
  }, 10000);
});

ws.on("message", (data) => {
  try {
    console.log("Received message!");
    if (data instanceof Buffer) {
      received += data.length;
      const preview = data
        .toString("utf8")
        .replace(/\x1b\[[0-9;?]*[A-Za-z]/g, "")
        .slice(0, 200);
      console.log(
        `[binary] ${data.length} bytes. Preview:`,
        JSON.stringify(preview),
      );
    } else if (data instanceof ArrayBuffer) {
      const buf = Buffer.from(data);
      received += buf.length;
      const preview = buf
        .toString("utf8")
        .replace(/\x1b\[[0-9;?]*[A-Za-z]/g, "")
        .slice(0, 200);
      console.log(
        `[arraybuffer] ${buf.length} bytes. Preview:`,
        JSON.stringify(preview),
      );
    } else {
      console.log("[text]", String(data).slice(0, 200));
    }
  } catch (e) {
    console.log("error parsing message:", e.message);
  }
});

ws.on("close", (code, reason) => {
  console.log("Client closed:", code, String(reason));
  console.log("Total bytes received:", received);
  process.exit(0);
});

ws.on("error", (err) => {
  console.error("Client error:", err.message);
});

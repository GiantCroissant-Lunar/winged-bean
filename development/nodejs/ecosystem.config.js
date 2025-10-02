const { getArtifactsPath } = require("./get-version");
const path = require("path");

// Get versioned log paths
const ptyLogsDir = getArtifactsPath("pty", "logs");
const webLogsDir = getArtifactsPath("web", "logs");

// Ensure log directories exist
const fs = require("fs");
fs.mkdirSync(ptyLogsDir, { recursive: true });
fs.mkdirSync(webLogsDir, { recursive: true });

module.exports = {
  apps: [
    {
      name: "pty-service",
      cwd: "./pty-service",
      script: "server.js",
      watch: ["server.js"],
      ignore_watch: ["node_modules", "logs", "*.log"],
      env: {
        NODE_ENV: "development",
        PORT: 4041,
      },
      error_file: path.join(ptyLogsDir, "pty-service-error.log"),
      out_file: path.join(ptyLogsDir, "pty-service-out.log"),
      log_date_format: "YYYY-MM-DD HH:mm:ss Z",
      merge_logs: true,
    },
    {
      name: "docs-site",
      cwd: "./sites/docs",
      script: "npm",
      args: "run dev",
      watch: false, // Astro has its own file watcher via Vite
      node_args: "--no-experimental-strip-types", // Disable Node.js type stripping for Starlight compatibility
      env: {
        NODE_ENV: "development",
      },
      error_file: path.join(webLogsDir, "docs-site-error.log"),
      out_file: path.join(webLogsDir, "docs-site-out.log"),
      log_date_format: "YYYY-MM-DD HH:mm:ss Z",
      merge_logs: true,
    },
  ],
};

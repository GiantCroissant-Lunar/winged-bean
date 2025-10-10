const path = require("path");
const fs = require("fs");

// Base paths
const repoRoot = __dirname;
const buildRoot = path.join(repoRoot, "build");
const artifactsRoot = path.join(buildRoot, "_artifacts", "latest");

// Create log directories
const logsDir = path.join(artifactsRoot, "_logs");
fs.mkdirSync(logsDir, { recursive: true });

module.exports = {
  apps: [
    // 1. PTY Service
    {
      name: "pty-service",
      cwd: path.join(artifactsRoot, "pty", "dist"),
      script: "server.js",
      watch: false,
      env: {
        NODE_ENV: "development",
        PORT: 4041,
      },
      error_file: path.join(logsDir, "pty-service-error.log"),
      out_file: path.join(logsDir, "pty-service-out.log"),
      log_date_format: "YYYY-MM-DD HH:mm:ss Z",
      merge_logs: true,
    },
    // 2. Docs Site (Web) - Astro dev server
    {
      name: "docs-site",
      cwd: path.join(repoRoot, "development", "nodejs", "sites", "docs"),
      script: "npm",
      args: "run dev",
      interpreter: "none",
      watch: false,
      env: {
        NODE_ENV: "development",
      },
      error_file: path.join(logsDir, "docs-site-error.log"),
      out_file: path.join(logsDir, "docs-site-out.log"),
      log_date_format: "YYYY-MM-DD HH:mm:ss Z",
      merge_logs: true,
    },
    // 3. Console Dungeon App
    {
      name: "console-dungeon",
      cwd: path.join(artifactsRoot, "dotnet", "bin"),
      script: "./ConsoleDungeon.Host",
      interpreter: "none",
      autorestart: true,
      watch: false,
      max_memory_restart: "500M",
      env: {
        DOTNET_ENVIRONMENT: "Development",
        PTY_WS_PORT: "4041",
      },
      error_file: path.join(logsDir, "console-dungeon-error.log"),
      out_file: path.join(logsDir, "console-dungeon-out.log"),
      log_date_format: "YYYY-MM-DD HH:mm:ss Z",
      merge_logs: true,
    },
  ],
};

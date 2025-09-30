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
      error_file: "./logs/pty-service-error.log",
      out_file: "./logs/pty-service-out.log",
      log_date_format: "YYYY-MM-DD HH:mm:ss Z",
      merge_logs: true,
    },
    {
      name: "docs-site",
      cwd: "./sites/docs",
      script: "npm",
      args: "run dev",
      watch: false, // Astro has its own file watcher via Vite
      env: {
        NODE_ENV: "development",
      },
      error_file: "./logs/docs-site-error.log",
      out_file: "./logs/docs-site-out.log",
      log_date_format: "YYYY-MM-DD HH:mm:ss Z",
      merge_logs: true,
    },
  ],
};

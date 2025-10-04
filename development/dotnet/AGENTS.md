# Agents Run Policy (Dotnet console)

Scope: This file applies to the `development/dotnet/` subtree.

Build & Run rules (normative):

- R-BLD-010: Use Taskfile builds from `build/`.
  - Build via `build/task build-all` (not `dotnet build`).

- R-RUN-020: Run from versioned artifacts only.
  - Use binaries from `build/_artifacts/v<version>/dotnet/bin`.
  - Do not use `dotnet run` for ConsoleDungeon.Host in scripts/docs.

- R-RUN-021: Use run helpers.
  - Debug UI: `build/test-tools/run-debug-mode.sh` or `task console:debug`
  - Normal UI: `build/test-tools/run-normal-mode.sh` or `task console:normal`

- R-RUN-022: PTY validation via tasks.
  - `task verify:pty-keys` to start WS PTY, send keys, and parse logs.

- R-RUN-023: Keep plugin paths relative.
  - Artifact runs rely on relative paths with loader fallback; avoid absolute paths.

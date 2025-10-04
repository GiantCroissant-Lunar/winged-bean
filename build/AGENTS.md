# Agents Run Policy (Build subsystem)

Scope: This file applies to the entire `build/` subtree.

Normative rules for building and running console apps:

- Use Taskfile orchestrated builds exclusively (R-BLD-010).
  - Build everything via `task build-all`.

- Always run from versioned artifacts (R-RUN-020).
  - Binaries live under `build/_artifacts/v<version>/dotnet/bin`.
  - Do not use `dotnet run` for ConsoleDungeon.Host.

- Use provided scripts/tasks (R-RUN-021/R-RUN-022).
  - Debug (blank UI): `./test-tools/run-debug-mode.sh` or `task console:debug`
  - Normal (full UI): `./test-tools/run-normal-mode.sh` or `task console:normal`
  - PTY E2E: `task verify:pty-keys`

- Keep plugin paths relative (R-RUN-023).
  - The loader can resolve flattened artifact layouts; avoid absolute paths.


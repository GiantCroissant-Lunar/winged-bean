# Handover — Winged Bean Desktop/Console Run + Recording (Next Session)

Date: 2025-10-15
Status: Watchdog in place; desktop/console verify flows added; recorder snapshot wiring improved

## Summary
- Added a non-hanging execution flow using a watchdog wrapper around host runs.
- Introduced verify scripts + tasks to assert recording success for desktop and console.
- Improved recorder plugin so URF snapshots capture gameplay even if recorder loads before gameplay.
- Ensured gameplay plugin loads before recorder via manifest priority (source + artifacts), making `IWorldSnapshotProvider` reliably available.

## What Changed
- Watchdog safety (prevents hangs):
  - build/test-hybrid-recording.sh — now enforces a hard timeout (GNU `timeout` if available; portable FIFO+watchdog otherwise). Exits `124` on timeout.
- Desktop verification:
  - build/verify-flow.sh — runs desktop host from artifacts, then checks logs + fresh URF under `recordings/urf/`.
  - build/Taskfile.yml — new task `run:desktop:record:verify` that calls the verifier.
- Console verification (headless-friendly):
  - build/verify-flow-console.sh — runs console host headless with `ENABLE_BOT_MODE=1`, verifies fresh URF.
  - build/Taskfile.yml — new task `run:console:record:verify` that calls the console verifier.
- Recorder snapshot reliability:
  - development/dotnet/windows/src/plugins/WingedBean.Plugins.Recorder.Snapshots/URFRecorderService.cs — lazily resolves `IWorldSnapshotProvider` from the registry if not supplied at activation.
  - RecorderSnapshotsPlugin.cs — passes `IRegistry` to the recorder for lazy resolution.
  - Deployed the rebuilt recorder to current artifacts: `_artifacts/0.0.1-474/dotnet/desktop/bin/plugins/`.
- Gameplay load order:
  - Added manifest (artifacts): `_artifacts/0.0.1-474/dotnet/desktop/bin/plugins/WingedBean.Plugins.DungeonGame.plugin.json` with `priority: 120`.
  - Added manifest (source): `development/dotnet/framework/src/plugins/WingedBean.Plugins.DungeonGame/.plugin.json` so future builds include correct priority automatically.

## How To Run (No Hangs)
- Desktop — build, run, verify (10s):
  - `cd personal-work/yokan-projects/winged-bean/build`
  - `task run:desktop:record:verify`
- Console — build, run, verify (10s, headless):
  - `cd personal-work/yokan-projects/winged-bean/build`
  - `task run:console:record:verify`
- Custom duration/timeouts:
  - Desktop: `./verify-flow.sh 15 $(./get-version.sh) 75`
  - Console: `./verify-flow-console.sh 15 $(./get-version.sh) 75`

## Expected Signals
- Desktop host log: `_artifacts/<ver>/dotnet/desktop/bin/logs/winged-bean-*.log`
  - Plugin load order, `Starting recording with URFRecorderService` → `✓ Recording started` → `✓ Recording stopped`.
- Console host log: `_artifacts/<ver>/dotnet/console/bin/logs/`
  - Diagnostic startup + recording lifecycle (and optional WebSocket bootstrapper notes in headless mode).
- URF artifacts:
  - Desktop: `_artifacts/<ver>/dotnet/desktop/bin/recordings/urf/session-*.urf.jsonl` (+ optional `.cast`).
  - Console: `_artifacts/<ver>/dotnet/console/bin/recordings/urf/session-*.urf.jsonl` (+ optional `.cast`).
  - Content check (examples):
    - Desktop: should contain `"stream": "cells"` and `"stream": "gameplay"` with `entityCount`.
    - Console: should contain `"stream": "terminal"` frames.

## Notes
- In headless CI/sandbox, the desktop (MonoGame/SadConsole) window may not progress; the watchdog then stops the app at the configured timeout. On a normal desktop environment, both Desktop and Console flows should pass and produce fresh URF.
- The console flow is headless-friendly (`DUNGEON_HEADLESS=1`) and suitable for automated verification.

## Next Steps (Recommended)
1) Run both verify tasks locally to capture fresh URF and confirm frames.
2) Bake the DungeonGame manifest into the normal build copy rules if any custom packaging bypasses project output.
3) Optionally extend `verify-flow.sh` to assert presence of specific gameplay fields (e.g., `hud.hp > 0`, `entityCount > 0`).
4) Reduce plugin manifest noise via `plugins-overrides.json` if duplicate manifests cause warnings.
5) When ready, rebuild gameplay to surface the new lightweight `[Verify]` console messages added during this session.

## Quick Commands
```
cd personal-work/yokan-projects/winged-bean/build
# Desktop verify (10s)
task run:desktop:record:verify
# Console verify (10s)
task run:console:record:verify
# Direct scripts
./verify-flow.sh 10 $(./get-version.sh) 60
./verify-flow-console.sh 10 $(./get-version.sh) 60
```

## Risks / Constraints
- Desktop GUI may not run fully in some headless environments; use console verification there.
- If URF has metadata only, ensure gameplay manifest is present in `plugins/` and the updated recorder DLL is copied.

## Contact / Handoff
- Start next session from the Quick Commands above; prefer console verification when scripting.
- The current artifacts are at `_artifacts/0.0.1-474/` and include the updated recorder + manifest.

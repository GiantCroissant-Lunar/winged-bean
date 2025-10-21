# Handover — Winged Bean Desktop Run + Recording (Next Session)

Date: 2025-10-15
Status: Running from versioned artifacts; recording stable; dungeon init fixed via UI fallback

## Summary
- Desktop host (SadConsole) now runs reliably from versioned artifacts with URF recording enabled.
- Gameplay (DungeonGame) plugin is loaded and registers `IDungeonService`.
- Recording starts/stops and produces URF `.jsonl` files (with optional .cast export by the recorder).
- Key fix: create the game screen eagerly (pre-Started fallback) so `DungeonGame.Initialize()` runs even if the `Game.Started` event is missed on macOS.

## What Works
- Manifest-based plugin loading for: DungeonGame, SadConsole, Recorder.Snapshots
- `./test-hybrid-recording.sh <seconds> <version>` runs from `_artifacts/<version>/...`
- URF recording lifecycle: start → stop → file written
- Logging to `logs/winged-bean-YYYY-MM-DD.log`

## What Doesn’t (Non-blocking)
- Duplicate SadConsole manifests discovered (top-level + nested `bin/Debug/net8.0`) → harmless "already loaded" warning.
- Recorder manifest discovery tries a nested path first and logs a not-found, then loads from the flat path correctly. Harmless but noisy.
- Gameplay plugin was physically migrated under `framework/` but compile still needs refactoring; we intentionally do NOT rebuild it in the host. We use the last known-good DLL in artifacts.

## Key Changes in This Session
- Added pre-Started fallback to ensure dungeon init:
  - `development/dotnet/windows/src/plugins/WingedBean.Plugins.SadConsole/SadConsoleApp.cs` — eagerly creates `DungeonGameScreen` and assigns `GameHost.Instance.Screen` before the event fires.
- Hybrid test script exports duration as env var to RecordingControl:
  - `build/test-hybrid-recording.sh` — exports `DURATION` in addition to `--record` flags.
- Gameplay plugin location:
  - Created framework project at `development/dotnet/framework/src/plugins/WingedBean.Plugins.DungeonGame/` and began migration.
  - Compile refactors are incomplete; host build no longer references this project directly to avoid long rebuilds. We run with existing artifacts.
- Build pipeline:
  - `build/Taskfile.yml` updated to avoid hard failing if framework gameplay build is skipped and to require the existing DLL presence in desktop `plugins/`.

## How To Run (No Rebuild)
1) Preflight
   - `cd personal-work/yokan-projects/winged-bean/build`
   - `./get-version.sh` → confirm version (e.g., `0.0.1-474`)
   - Ensure plugin exists: `_artifacts/<ver>/dotnet/desktop/bin/plugins/WingedBean.Plugins.DungeonGame.dll`

2) Run 5s hybrid recording
   - `./test-hybrid-recording.sh 5 <ver>`

3) Check logs (tail last 120 lines)
   - `_artifacts/<ver>/dotnet/desktop/bin/test-hybrid-recording.log`
   - `_artifacts/<ver>/dotnet/desktop/bin/logs/winged-bean-YYYY-MM-DD.log`

Expected markers in logs:
- Plugin load order and ✓ load messages
- "Creating game screen eagerly (pre-Started fallback)..."
- "Calling _dungeon.Initialize()..." and "✓ Dungeon initialized successfully"
- "Starting recording with URFRecorderService" then "✓ Recording started" and "✓ Recording stopped"

Artifacts:
- URF: `_artifacts/<ver>/dotnet/desktop/bin/recordings/urf/session-*.urf.jsonl`
- (Optional) .cast export if enabled by recorder

## Known Log Noise (Safe to Ignore)
- Recorder manifest not found at nested `bin/Debug/...` path. Actual load uses flat `plugins/WingedBean.Plugins.Recorder.Snapshots.dll`.
- SadConsole "already loaded" due to duplicate manifest discovery (top-level + nested). Load proceeds with one instance.

## Next Steps (Recommended)
1) Keep running from versioned artifacts; skip gameplay rebuild for now.
2) Plugin loader hygiene (quick wins):
   - Add `plugins-overrides.json` to disable nested plugin manifests (e.g., `WingedBean.Plugins.SadConsole/bin/Debug/net8.0`).
   - Optionally, make loader filter duplicates by plugin id, keeping the first discovered path.
3) Complete gameplay migration under `framework/`:
   - Fix compile errors (types and namespaces): unify `MapMemoryComponent` (class vs struct), adjust ECS `IWorld` generic constraints (value types), and reconcile `SadRogue.Primitives.GridViews` usage.
   - Re-enable `RenderSystem`/`HUDRenderSystem` or keep rendering solely in UI plugins (SadConsole/TerminalUI).
4) Bot movement verification:
   - With `ENABLE_BOT_MODE=1`, confirm entities update: add a short console log from `AISystem` (e.g., first step tick) or subscribe to `IWorldSnapshotProvider` and log entity count changes.
5) Optional polish:
   - Reduce duplicate-manifest warnings (copy rules or loader filter).
   - Add a lightweight heartbeat log during long host builds/tests to make progress visible.

## Quick Commands
```
cd personal-work/yokan-projects/winged-bean/build
# Run from existing artifacts (no rebuild)
./test-hybrid-recording.sh 5 $(./get-version.sh)

# Tail logs
tail -n 120 _artifacts/$(./get-version.sh)/dotnet/desktop/bin/test-hybrid-recording.log
tail -n 120 _artifacts/$(./get-version.sh)/dotnet/desktop/bin/logs/winged-bean-$(date +%F).log
```

## Integration Notes (Plate Projects)
- Capability/GOAP (capability-orlia, goap-ckpor): artifact copies are best-effort in `build/Taskfile.yml`; safe to keep.
- ModernSatsuma, Camera2D, InventoryPoki: included as dependencies; Inventory systems are currently excluded from gameplay build pending refactor to new ECS world API.

## Risks / Constraints
- Rebuilding the framework gameplay project currently shows compile errors (namespace/type mismatches post-move). We intentionally avoid it in the desktop host build; rely on existing DLL.
- MonoGame/SadConsole on macOS require main-thread loop; the host calls `RunGame()` via reflection on the main thread. The new pre-Started fallback ensures dungeon init even if the Started event is flaky.

## Contact / Handoff
- This document closes the current session with a reproducible, versioned run flow and stable recording.
- Start next session from the Quick Commands and Next Steps sections above.

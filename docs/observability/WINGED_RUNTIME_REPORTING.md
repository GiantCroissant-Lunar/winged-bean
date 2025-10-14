# Winged Runtime Reporting (nfun-report contracts)

This document explains how Winged-Bean emits runtime reports as JSON artifacts and how to aggregate them using nfun-report.

## Overview

- Contract-first: Reports are JSON files written under `build/_artifacts/<version>/winged/` so CI can archive them.
- Producers (inside Winged):
  - Resource bundle builds emit `ResourceBundleReport.json`.
  - Asciinema recorder emits `AsciinemaReport.json`.
  - Runtime metrics (optional, future): `RuntimeMetrics.json`.
- Consumers (outside Winged):
  - Use `Plate.Reporting.Winged` (nfun-report adapter) to aggregate reports from the artifacts folder.

## Artifact Layout

build/_artifacts/<version>/winged/
- resource-bundles/<bundleId>/ResourceBundleReport.json
- asciinema/<sessionId>/AsciinemaReport.json
- runtime/<timestamp>/RuntimeMetrics.json (future)

Notes
- `<version>` is detected from git (git describe), then git SHA, else `0.0.0-local`.
- The folder structure is stable and CI-friendly.

## Emission Points (already wired)

- Resource Bundles
  - File: `development/dotnet/console/src/plugins/WingedBean.Plugins.Resource/ResourceBundleBuilder.cs`
  - When `BuildAsync(...)` completes, it writes `ResourceBundleReport.json`.

- Asciinema Sessions
  - File: `development/dotnet/console/src/plugins/WingedBean.Plugins.AsciinemaRecorder/URFRecorderService.cs`
  - When `StopRecordingAsync(...)` completes, it writes `AsciinemaReport.json` (in addition to `.cast`).

## Quick Start (demo)

1) Generate example reports

From repo root:
- `dotnet run --project personal-work/yokan-projects/winged-bean/development/dotnet/console/src/demos/WingedBean.Reporting.Demo -c Release`

This will:
- Build a tiny bundle and emit `ResourceBundleReport.json`.
- Create a short URF/asciinema session and emit `AsciinemaReport.json`.

2) Aggregate and print a summary

- `dotnet run --project personal-work/plate-projects/nfun-report/examples/WingedIngestionDemo -- build/_artifacts/<version>/winged`
- Writes `winged-aggregate.json` in the provided folder and prints a summary (bundles, sessions, metrics).

Tip: For a proper `<version>`, run commands inside the git repo (`cd personal-work/yokan-projects/winged-bean/development/dotnet`).

## Programmatic Ingestion

Add a reference to `Plate.Reporting.Winged` and call:

```
using Plate.Reporting.Winged;

var wingedDir = Path.Combine(repoRoot, "build", "_artifacts", version, "winged");
var bundles = WingedResourceBundleProvider.LoadFromArtifacts(wingedDir);
var asci = WingedAsciinemaProvider.LoadFromArtifacts(wingedDir);
var metrics = WingedRuntimeMetricsProvider.LoadFromArtifacts(wingedDir);
```

Use the aggregates to feed dashboards, export HTML/JSON, or persist to a datastore.

## Troubleshooting

- No reports written
  - Ensure the relevant code paths ran: `ResourceBundleBuilder.BuildAsync` / `URFRecorderService.StopRecordingAsync`.
  - Check process permissions to write under `build/_artifacts/...`.
  - If `<version>` is `0.0.0-local`, ensure you’re running under a git repo, or set `NUKE_ROOT`/working directory appropriately.

- Wrong artifact location
  - This integration uses the new layout: `build/_artifacts/<version>/winged/...`.
  - Avoid writing to legacy NUKE paths.

- CI
  - Archive `build/_artifacts/` as a job artifact; run the ingestion demo (or your own host) to produce an aggregate.

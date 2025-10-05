#!/usr/bin/env bash
# Run Console Dungeon in DEBUG_MINIMAL_UI mode to test input handling
# Usage: ./run-debug-mode.sh [artifact-path]
#
# This script runs the console app with minimal UI to isolate input handling
# and determine if UI widgets are interfering with key events (issue #214)

set -euo pipefail

ARTIFACT_PATH="${1:-}"

if [[ -z "$ARTIFACT_PATH" ]]; then
  # Find the latest artifact
  ARTIFACTS_DIR="/Users/apprenticegc/Work/lunar-horse/personal-work/yokan-projects/winged-bean/build/_artifacts"
  LATEST=$(ls -t "$ARTIFACTS_DIR" | head -1)
  if [[ -z "$LATEST" ]]; then
    echo "Error: No artifacts found in $ARTIFACTS_DIR"
    echo "Run 'task build-all' first"
    exit 1
  fi
  ARTIFACT_PATH="$ARTIFACTS_DIR/$LATEST/dotnet/bin"
fi

CONSOLE_APP="$ARTIFACT_PATH/ConsoleDungeon.Host"
LOG_DIR="$ARTIFACT_PATH/logs"
BG_DIR="$ARTIFACT_PATH/_bg_logs"

if [[ ! -x "$CONSOLE_APP" ]]; then
  echo "Error: Console app not found at $CONSOLE_APP"
  exit 1
fi

echo "=== Console Dungeon - DEBUG MODE ==="
echo "Artifact: $CONSOLE_APP"
echo "Logs: $LOG_DIR"
echo ""
echo "Running with DEBUG_MINIMAL_UI=1"
echo ""
echo "Instructions:"
echo "  1. Press arrow keys (Up, Down, Left, Right)"
echo "  2. Press M to test menu toggle"
echo "  3. Press Esc or Ctrl+C to quit"
echo ""
echo "All key events will be logged."
echo "After exiting, run parse-keylog.js on the log file."
echo ""

export DEBUG_MINIMAL_UI=1

if [[ "${RUN_BG:-0}" = "1" ]]; then
  mkdir -p "$BG_DIR"
  echo "Running in background (RUN_BG=1). PID/logs in: $BG_DIR"
  (
    cd "$ARTIFACT_PATH" && nohup ./ConsoleDungeon.Host > "$BG_DIR/console.out" 2>&1 < /dev/null & echo $! > "$BG_DIR/console.pid"
  )
  sleep 3
  if [[ -f "$BG_DIR/console.pid" ]] && kill -0 "$(cat "$BG_DIR/console.pid")" 2>/dev/null; then
    echo "Started. PID: $(cat "$BG_DIR/console.pid")"
  else
    echo "Process not running; check logs at $BG_DIR/console.out"
  fi
  echo "--- Background log tail ---"
  tail -n 120 "$BG_DIR/console.out" 2>/dev/null || true
  exit 0
else
  # Foreground run
  (
    cd "$ARTIFACT_PATH" && ./ConsoleDungeon.Host
  )
fi

# Find and parse the latest log
echo ""
echo "=== Finding latest log file ==="
LATEST_LOG=$(ls -t "$LOG_DIR"/console-dungeon-*.log 2>/dev/null | head -1 || echo "")

if [[ -n "$LATEST_LOG" ]]; then
  echo "Log file: $LATEST_LOG"
  echo ""
  echo "=== Auto-parsing log ==="
  node "$(dirname "$0")/parse-keylog.js" "$LATEST_LOG"
else
  echo "No log file found"
fi

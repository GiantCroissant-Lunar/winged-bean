#!/usr/bin/env bash
# Run Console Dungeon in NORMAL mode (with full UI) to compare behavior
# Usage: ./run-normal-mode.sh [artifact-path]
#
# This script runs the console app with full UI (game widgets)
# to compare against debug mode and identify widget interference (issue #214)

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

if [[ ! -x "$CONSOLE_APP" ]]; then
  echo "Error: Console app not found at $CONSOLE_APP"
  exit 1
fi

echo "=== Console Dungeon - NORMAL MODE ==="
echo "Artifact: $CONSOLE_APP"
echo "Logs: $LOG_DIR"
echo ""
echo "Running with DEBUG_MINIMAL_UI=0 (full game UI)"
echo ""
echo "Instructions:"
echo "  1. Press arrow keys (Up, Down, Left, Right)"
echo "  2. Press M to test menu toggle"
echo "  3. Press Esc or Q to quit"
echo ""
echo "All key events will be logged."
echo "After exiting, run parse-keylog.js on the log file."
echo ""

# Run with debug mode disabled (normal mode)
export DEBUG_MINIMAL_UI=0
(
  cd "$ARTIFACT_PATH" && ./ConsoleDungeon.Host
)

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

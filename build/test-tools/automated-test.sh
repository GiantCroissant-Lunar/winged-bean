#!/usr/bin/env bash
# Automated testing script that sends key sequences to the console app
# Usage: ./automated-test.sh <mode>
# mode: "debug" or "normal"

set -euo pipefail

MODE="${1:-debug}"
ARTIFACT_PATH="/Users/apprenticegc/Work/lunar-horse/personal-work/yokan-projects/winged-bean/build/_artifacts/v0.0.1-285/dotnet/bin"
CONSOLE_APP="$ARTIFACT_PATH/ConsoleDungeon.Host"
LOG_DIR="$ARTIFACT_PATH/logs"

if [[ ! -x "$CONSOLE_APP" ]]; then
  echo "Error: Console app not found at $CONSOLE_APP"
  exit 1
fi

# Set environment based on mode
if [[ "$MODE" == "debug" ]]; then
  export DEBUG_MINIMAL_UI=1
  echo "=== AUTOMATED TEST: DEBUG MODE ==="
else
  export DEBUG_MINIMAL_UI=0
  echo "=== AUTOMATED TEST: NORMAL MODE ==="
fi

# Clean old logs
rm -f "$LOG_DIR"/console-dungeon-*.log 2>/dev/null || true

echo "Starting console app in background..."
# Run the app in background, redirecting to a temp file
TEMP_OUT=$(mktemp)
"$CONSOLE_APP" > "$TEMP_OUT" 2>&1 &
APP_PID=$!
echo "App PID: $APP_PID"

# Wait for initialization
echo "Waiting for app to initialize..."
sleep 5

# Check if still running
if ! kill -0 $APP_PID 2>/dev/null; then
  echo "Error: App died during initialization"
  cat "$TEMP_OUT"
  exit 1
fi

echo "App is running. Note: Cannot inject keys without PTY control."
echo "This test will just let the app run for 10 seconds, then quit it."
echo ""
echo "For manual testing:"
echo "  1. Run: DEBUG_MINIMAL_UI=$DEBUG_MINIMAL_UI $CONSOLE_APP"
echo "  2. Manually press: Up, Down, Left, Right, M, Esc"
echo "  3. Check the log file"
echo ""

# Let it run for a bit
sleep 10

# Kill the app
echo "Stopping app..."
kill $APP_PID 2>/dev/null || true
wait $APP_PID 2>/dev/null || true

# Find the latest log
LATEST_LOG=$(ls -t "$LOG_DIR"/console-dungeon-*.log 2>/dev/null | head -1 || echo "")

if [[ -z "$LATEST_LOG" ]]; then
  echo "Error: No log file found"
  exit 1
fi

echo ""
echo "=== Log file: $LATEST_LOG ==="
echo ""

# Show app output
echo "=== App stdout/stderr ==="
cat "$TEMP_OUT"
rm -f "$TEMP_OUT"

echo ""
echo "=== Parsing log ==="
node "$(dirname "$0")/parse-keylog.js" "$LATEST_LOG" || true

echo ""
echo "Test completed. Log saved at: $LATEST_LOG"

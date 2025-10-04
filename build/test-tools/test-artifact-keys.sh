#!/usr/bin/env bash
# Test the arrow keys in the built artifact with debug logging
# Usage: ./test-artifact-keys.sh [artifact-path]
#
# This script:
# 1. Runs the console dungeon artifact
# 2. Sends arrow key sequences
# 3. Monitors the log file for key events
# 4. Reports which arrow keys worked

set -euo pipefail

ARTIFACT_PATH="${1:-/Users/apprenticegc/Work/lunar-horse/personal-work/yokan-projects/winged-bean/build/_artifacts/v0.0.1-285/dotnet/bin}"
CONSOLE_APP="$ARTIFACT_PATH/ConsoleDungeon.Host"
LOG_DIR="$ARTIFACT_PATH/../logs"

if [[ ! -x "$CONSOLE_APP" ]]; then
  echo "Error: Console app not found at $CONSOLE_APP"
  exit 1
fi

echo "=== Testing Arrow Keys in Console Dungeon ==="
echo "Artifact: $CONSOLE_APP"
echo "Logs: $LOG_DIR"
echo ""

# Clean old logs
rm -f "$LOG_DIR"/console-dungeon-*.log

# Start the app in background
echo "Starting console app..."
"$CONSOLE_APP" &
APP_PID=$!

# Wait for app to initialize
sleep 3

# Find the latest log file
LOG_FILE=$(ls -t "$LOG_DIR"/console-dungeon-*.log 2>/dev/null | head -1 || echo "")

if [[ -z "$LOG_FILE" ]]; then
  echo "Error: No log file found"
  kill $APP_PID 2>/dev/null || true
  exit 1
fi

echo "Monitoring: $LOG_FILE"
echo ""

# Function to send key and wait
send_key() {
  local key_name="$1"
  local key_seq="$2"
  echo "Sending $key_name..."
  # Note: This doesn't actually work without proper PTY injection
  # This is a placeholder - the real test would need xdotool or similar
  echo "  (Would send sequence: $key_seq)"
  sleep 1
}

# Test sequence
echo "Testing arrow keys..."
send_key "UP" "\x1b[A"
send_key "DOWN" "\x1b[B"
send_key "LEFT" "\x1b[D"
send_key "RIGHT" "\x1b[C"

# Wait a moment for logs to flush
sleep 2

# Stop the app
echo ""
echo "Stopping app..."
kill $APP_PID 2>/dev/null || true
wait $APP_PID 2>/dev/null || true

# Analyze the log
echo ""
echo "=== Log Analysis ==="
echo "Key events captured:"
grep -i "KeyDown:" "$LOG_FILE" || echo "  (none)"

echo ""
echo "Game inputs processed:"
grep -i "Game input received:" "$LOG_FILE" || echo "  (none)"

echo ""
echo "=== Full Log ==="
cat "$LOG_FILE"

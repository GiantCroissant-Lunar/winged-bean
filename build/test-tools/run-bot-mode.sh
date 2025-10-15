#!/usr/bin/env bash
# Run Console Dungeon with BOT MODE enabled for automated gameplay testing
# The player will be AI-controlled and wander around the dungeon
# Usage: ./run-bot-mode.sh [duration-seconds]

set -euo pipefail

DURATION="${1:-30}"  # Default 30 seconds
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
BUILD_DIR="$(cd "$SCRIPT_DIR/.." && pwd)"
ARTIFACT_DIR="$BUILD_DIR/_artifacts/latest/dotnet/console/bin"
LOG_DIR="$ARTIFACT_DIR/logs"
RECORDING_DIR="$ARTIFACT_DIR/recordings"

mkdir -p "$LOG_DIR" "$RECORDING_DIR"

echo "=== Console Dungeon - BOT MODE ==="
echo "Duration: ${DURATION} seconds"
echo "Artifact: $ARTIFACT_DIR"
echo "Logs: $LOG_DIR"
echo "Recordings: $RECORDING_DIR"
echo ""
echo "🤖 Player will be AI-controlled (patrol mode)"
echo "📹 Recording gameplay session..."
echo ""

cd "$ARTIFACT_DIR"

# Export bot mode environment variable
export ENABLE_BOT_MODE=1
export DEBUG_MINIMAL_UI=1

# Run with timeout
TIMESTAMP=$(date +%Y%m%d_%H%M%S)
LOG_FILE="$LOG_DIR/bot-gameplay-$TIMESTAMP.log"

echo "Starting bot gameplay for ${DURATION} seconds..."
echo "Log file: $LOG_FILE"

# Run in background with timeout (macOS compatible)
./Dungeon.Host.Console > "$LOG_FILE" 2>&1 &
PID=$!
sleep "$DURATION"
kill $PID 2>/dev/null || true
wait $PID 2>/dev/null || true

echo ""
echo "✅ Bot gameplay session complete"
echo ""
echo "=== Log Summary ==="
tail -n 20 "$LOG_FILE"
echo ""
echo "Full log: $LOG_FILE"

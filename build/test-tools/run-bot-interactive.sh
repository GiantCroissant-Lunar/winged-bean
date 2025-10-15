#!/usr/bin/env bash
# Run Console Dungeon with BOT MODE in an interactive terminal
# This ensures the game actually starts (not headless mode)
# Usage: ./run-bot-interactive.sh [duration-seconds]

set -euo pipefail

DURATION="${1:-60}"
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
BUILD_DIR="$(cd "$SCRIPT_DIR/.." && pwd)"
ARTIFACT_DIR="$BUILD_DIR/_artifacts/latest/dotnet/console/bin"

echo "=== Console Dungeon - BOT MODE (Interactive) ==="
echo "Duration: ${DURATION} seconds"
echo "Artifact: $ARTIFACT_DIR"
echo ""
echo "🤖 Player will be AI-controlled (patrol mode)"
echo "📺 Running in interactive terminal (not headless)"
echo ""
echo "Press Ctrl+C to stop early"
echo ""

cd "$ARTIFACT_DIR"

# Export bot mode environment variable
export ENABLE_BOT_MODE=1

# Run interactively (no redirection, so Terminal.Gui will start)
echo "Starting bot gameplay..."
echo ""

# Use script command to create a pseudo-terminal
# This prevents headless mode
./Dungeon.Host.Console &
PID=$!

echo "Game started (PID: $PID)"
echo "Will run for ${DURATION} seconds..."

sleep "$DURATION"

echo ""
echo "Stopping game..."
kill $PID 2>/dev/null || true
wait $PID 2>/dev/null || true

echo "✅ Bot gameplay session complete"

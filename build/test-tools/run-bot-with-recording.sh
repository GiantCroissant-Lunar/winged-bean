#!/usr/bin/env bash
# Run Console Dungeon with BOT MODE and asciinema recording
# Usage: ./run-bot-with-recording.sh [duration-seconds] [recording-name]

set -euo pipefail

DURATION="${1:-60}"  # Default 60 seconds
RECORDING_NAME="${2:-bot-gameplay-$(date +%Y%m%d_%H%M%S)}"
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
BUILD_DIR="$(cd "$SCRIPT_DIR/.." && pwd)"
ARTIFACT_DIR="$BUILD_DIR/_artifacts/latest/dotnet/console/bin"
LOG_DIR="$ARTIFACT_DIR/logs"
RECORDING_DIR="$ARTIFACT_DIR/recordings"

mkdir -p "$LOG_DIR" "$RECORDING_DIR"

echo "=== Console Dungeon - BOT MODE WITH RECORDING ==="
echo "Duration: ${DURATION} seconds"
echo "Recording: ${RECORDING_NAME}"
echo "Artifact: $ARTIFACT_DIR"
echo "Logs: $LOG_DIR"
echo "Recordings: $RECORDING_DIR"
echo ""

# Check if asciinema is available
if command -v asciinema &> /dev/null; then
    RECORDING_FILE="$RECORDING_DIR/${RECORDING_NAME}.cast"
    echo "📹 Recording with asciinema to: $RECORDING_FILE"
    echo "🤖 Player will be AI-controlled (patrol mode)"
    echo ""
    
    cd "$ARTIFACT_DIR"
    
    # Export bot mode environment variable
    export ENABLE_BOT_MODE=1
    export DEBUG_MINIMAL_UI=1
    
    # Record with asciinema and timeout
    (
        asciinema rec "$RECORDING_FILE" \
            --title "Bot Gameplay - ${RECORDING_NAME}" \
            --command "./Dungeon.Host.Console" \
            --overwrite &
        ASCIINEMA_PID=$!
        sleep "$DURATION"
        kill $ASCIINEMA_PID 2>/dev/null || true
        wait $ASCIINEMA_PID 2>/dev/null || true
    ) 2>&1 | tee "$LOG_DIR/${RECORDING_NAME}.log"
    
    echo ""
    echo "✅ Recording complete"
    echo "📹 Asciinema cast: $RECORDING_FILE"
    echo "📝 Log file: $LOG_DIR/${RECORDING_NAME}.log"
    echo ""
    echo "To replay:"
    echo "  asciinema play $RECORDING_FILE"
    echo ""
    echo "To upload (optional):"
    echo "  asciinema upload $RECORDING_FILE"
    
else
    echo "⚠️  asciinema not found - falling back to log-only mode"
    echo "   Install with: brew install asciinema"
    echo ""
    echo "🤖 Running bot mode without recording..."
    
    cd "$ARTIFACT_DIR"
    
    export ENABLE_BOT_MODE=1
    export DEBUG_MINIMAL_UI=1
    
    LOG_FILE="$LOG_DIR/${RECORDING_NAME}.log"
    
    ./Dungeon.Host.Console > "$LOG_FILE" 2>&1 &
    PID=$!
    sleep "$DURATION"
    kill $PID 2>/dev/null || true
    wait $PID 2>/dev/null || true
    
    echo ""
    echo "✅ Bot gameplay session complete"
    echo "📝 Log file: $LOG_FILE"
    echo ""
    echo "=== Log Summary ==="
    tail -n 20 "$LOG_FILE"
fi

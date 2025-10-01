#!/bin/bash
# Record Terminal.Gui session with asciinema
# Usage: ./scripts/record-terminal-gui-session.sh [session-name]

set -e

# Configuration
RECORDINGS_DIR="docs/recordings"
TIMESTAMP=$(date +%Y%m%d-%H%M%S)
SESSION_NAME="${1:-terminal-gui}"
OUTPUT_FILE="$RECORDINGS_DIR/${SESSION_NAME}-${TIMESTAMP}.cast"

# Ensure recordings directory exists
mkdir -p "$RECORDINGS_DIR"

# Terminal.Gui project path
DOTNET_PROJECT="development/dotnet/console/src/host/TerminalGui.PtyHost/TerminalGui.PtyHost.csproj"

echo "========================================="
echo "Terminal.Gui Session Recording"
echo "========================================="
echo ""
echo "Recording will be saved to: $OUTPUT_FILE"
echo "Press Ctrl+Q to quit Terminal.Gui and stop recording"
echo ""
echo "Starting in 3 seconds..."
sleep 3

# Record the session
asciinema rec "$OUTPUT_FILE" \
  --title "Terminal.Gui v2 - $SESSION_NAME - $TIMESTAMP" \
  --command "cd development/dotnet/console && dotnet run --project src/host/TerminalGui.PtyHost/TerminalGui.PtyHost.csproj"

echo ""
echo "========================================="
echo "Recording saved successfully!"
echo "========================================="
echo ""
echo "File: $OUTPUT_FILE"
echo "Size: $(du -h "$OUTPUT_FILE" | cut -f1)"
echo ""
echo "To play the recording:"
echo "  asciinema play $OUTPUT_FILE"
echo ""
echo "To upload to asciinema.org:"
echo "  asciinema upload $OUTPUT_FILE"
echo ""

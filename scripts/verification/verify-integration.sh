#!/bin/bash
# RFC-0005 Phase 5.3: xterm.js Integration Verification Script
#
# This script verifies that xterm.js integration works after refactoring:
# - Starts ConsoleDungeon.Host (WebSocket server on port 4040)
# - Starts Astro dev server (port 4321)
# - Waits for services to be ready
# - Provides manual verification instructions

set -e

# Determine repository root (script is in scripts/verification/)
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd "$SCRIPT_DIR/../.." && pwd)"
DOTNET_DIR="$REPO_ROOT/development/dotnet/console/src/host/ConsoleDungeon.Host"
ASTRO_DIR="$REPO_ROOT/development/nodejs/sites/docs"
LOG_DIR="/tmp/xterm-verification"

echo "========================================="
echo "RFC-0005 Phase 5.3: xterm.js Verification"
echo "========================================="
echo ""

# Create log directory
mkdir -p "$LOG_DIR"

# Function to check if port is in use
check_port() {
    local port=$1
    if lsof -Pi :$port -sTCP:LISTEN -t >/dev/null 2>&1 ; then
        return 0
    else
        return 1
    fi
}

# Function to cleanup background processes
cleanup() {
    echo ""
    echo "Cleaning up processes..."
    if [ ! -z "$DOTNET_PID" ]; then
        kill $DOTNET_PID 2>/dev/null || true
        echo "✓ Stopped ConsoleDungeon.Host (PID: $DOTNET_PID)"
    fi
    if [ ! -z "$ASTRO_PID" ]; then
        kill $ASTRO_PID 2>/dev/null || true
        echo "✓ Stopped Astro dev server (PID: $ASTRO_PID)"
    fi
    # Kill any remaining node processes on port 4321
    lsof -ti:4321 | xargs kill -9 2>/dev/null || true
    # Kill any remaining dotnet processes on port 4040
    lsof -ti:4040 | xargs kill -9 2>/dev/null || true
}

trap cleanup EXIT INT TERM

echo "[1/5] Starting ConsoleDungeon.Host (WebSocket on port 4040)..."
cd "$DOTNET_DIR"
dotnet run --no-build -c Release > "$LOG_DIR/consoledungeon.log" 2>&1 &
DOTNET_PID=$!
echo "  ✓ Started ConsoleDungeon.Host (PID: $DOTNET_PID)"
echo "  ✓ Logs: $LOG_DIR/consoledungeon.log"

# Wait for WebSocket server to be ready
echo "[2/5] Waiting for WebSocket server (port 4040)..."
for i in {1..30}; do
    if check_port 4040; then
        echo "  ✓ WebSocket server is ready on port 4040"
        break
    fi
    if [ $i -eq 30 ]; then
        echo "  ✗ WebSocket server failed to start"
        echo "  Last 20 lines of log:"
        tail -20 "$LOG_DIR/consoledungeon.log"
        exit 1
    fi
    sleep 1
done

echo "[3/5] Starting Astro dev server (port 4321)..."
cd "$ASTRO_DIR"
npm run dev > "$LOG_DIR/astro.log" 2>&1 &
ASTRO_PID=$!
echo "  ✓ Started Astro dev server (PID: $ASTRO_PID)"
echo "  ✓ Logs: $LOG_DIR/astro.log"

# Wait for Astro to be ready
echo "[4/5] Waiting for Astro dev server (port 4321)..."
for i in {1..60}; do
    if check_port 4321; then
        echo "  ✓ Astro dev server is ready on port 4321"
        break
    fi
    if [ $i -eq 60 ]; then
        echo "  ✗ Astro dev server failed to start"
        echo "  Last 20 lines of log:"
        tail -20 "$LOG_DIR/astro.log"
        exit 1
    fi
    sleep 1
done

echo ""
echo "========================================="
echo "✅ Both services are running!"
echo "========================================="
echo ""
echo "[5/5] Manual Verification Steps:"
echo ""
echo "1. Open browser to: http://localhost:4321"
echo "2. Verify xterm.js terminal loads"
echo "3. Verify WebSocket connection message appears"
echo "4. Verify Terminal.Gui interface renders"
echo "5. Try keyboard input and verify response"
echo ""
echo "Logs available at:"
echo "  - ConsoleDungeon.Host: $LOG_DIR/consoledungeon.log"
echo "  - Astro dev server:    $LOG_DIR/astro.log"
echo ""
echo "Press Ctrl+C to stop all services"
echo ""

# Keep script running until interrupted
wait

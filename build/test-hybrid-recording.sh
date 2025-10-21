#!/usr/bin/env bash
set -euo pipefail

# Test hybrid recording with bot mode
# Usage: ./test-hybrid-recording.sh [duration_seconds] [version] [hard_timeout_seconds]
# - duration_seconds: how long the in-app recorder should run
# - version: artifact version to run from (defaults to ./get-version.sh)
# - hard_timeout_seconds: absolute upper bound; kills the app if exceeded

DURATION="${1:-10}"
# Resolve version: prefer second arg, then get-version.sh, then 'latest' symlink
REQ_VERSION="${2:-}"
if [[ -z "$REQ_VERSION" ]]; then
  if [[ -x ./get-version.sh ]]; then
    REQ_VERSION="$(./get-version.sh)"
  elif [[ -L "_artifacts/latest" ]]; then
    REQ_VERSION="$(basename "$(readlink _artifacts/latest)")"
  else
    echo "ERROR: Could not resolve version. Pass as arg2 or ensure get-version.sh exists." >&2
    exit 1
  fi
fi
ARTIFACTS_DIR="$(pwd)/_artifacts/${REQ_VERSION}/dotnet/desktop/bin"

# Hard timeout provides a safety net to avoid getting stuck if the app fails to exit.
# Default: duration + 20s grace.
HARD_TIMEOUT="${3:-}"
if [[ -z "${HARD_TIMEOUT}" ]]; then
  # Add 20s grace to allow orderly shutdown and exports
  HARD_TIMEOUT=$(( DURATION + 20 ))
fi

cd "$ARTIFACTS_DIR"

echo "========================================="
echo "Testing Hybrid Recording"
echo "========================================="
echo "Duration: ${DURATION}s"
echo "Hard timeout: ${HARD_TIMEOUT}s (safety net)"
echo "Working directory: $(pwd)"
echo "Version: ${REQ_VERSION}"
echo ""

# Set environment variables (ensure RecordingControl picks up duration via env as fallback)
export ENABLE_BOT_MODE=1
export ENABLE_VIDEO=1
export DURATION="$DURATION"

# Create recordings directory
mkdir -p recordings

echo "Starting Dungeon.Host.SadConsole with hybrid recording..."
echo ""

# Run with logging and enable URF recording
# Prefer GNU timeout if available; otherwise use a portable watchdog.
if command -v timeout >/dev/null 2>&1; then
  echo "Using GNU timeout wrapper (kill-after 10s)."
  # Send TERM at HARD_TIMEOUT, then KILL 10s later if still alive.
  # timeout exits with 124 on timeout.
  set +e
  timeout --preserve-status -k 10 "${HARD_TIMEOUT}" \
    ./Dungeon.Host.SadConsole --record --record-duration="${DURATION}" 2>&1 | tee test-hybrid-recording.log
  app_rc=${PIPESTATUS[0]}
  set -e
else
  echo "Using portable watchdog (no GNU timeout found)."
  # Start the app in background, capture its PID robustly while still tee'ing logs.
  # Avoid process substitution (not allowed in some sandboxes); use a FIFO.
  set +e
  fifo=.hybrid-log-fifo
  rm -f "$fifo" 2>/dev/null || true
  mkfifo "$fifo"
  # Fan-out to console and file via tee
  tee test-hybrid-recording.log < "$fifo" & tee_pid=$!
  # Start app, sending its stdout/stderr into the FIFO
  ./Dungeon.Host.SadConsole --record --record-duration="${DURATION}" > "$fifo" 2>&1 &
  app_pid=$!
  timed_out=0

  # Watchdog: after HARD_TIMEOUT, kill the app if still running
  (
    sleep "${HARD_TIMEOUT}"
    if kill -0 "${app_pid}" 2>/dev/null; then
      echo "[watchdog] HARD TIMEOUT reached (${HARD_TIMEOUT}s). Sending SIGTERM to PID ${app_pid}..." | tee -a test-hybrid-recording.log
      kill "${app_pid}" 2>/dev/null || true
      # Give it a few seconds to shutdown gracefully, then SIGKILL if needed
      sleep 10
      if kill -0 "${app_pid}" 2>/dev/null; then
        echo "[watchdog] Process still alive. Sending SIGKILL to PID ${app_pid}." | tee -a test-hybrid-recording.log
        kill -9 "${app_pid}" 2>/dev/null || true
      fi
      # Mark timeout path so we can return 124
      echo timeout > .hybrid-timeout-hit 2>/dev/null || true
    fi
  ) & watcher_pid=$!

  # Ensure watcher is cleaned up when app exits normally
  wait "${app_pid}" 2>/dev/null
  app_rc=$?
  kill "${watcher_pid}" 2>/dev/null || true
  # Stop tee and remove FIFO
  kill "${tee_pid}" 2>/dev/null || true
  rm -f "$fifo" 2>/dev/null || true

  if [[ -f .hybrid-timeout-hit ]]; then
    rm -f .hybrid-timeout-hit
    app_rc=124
  fi
  set -e
fi

echo ""
echo "========================================="
echo "Test Complete"
echo "========================================="
echo ""
echo "Output files:"
ls -lh recordings/ 2>/dev/null || echo "No recordings directory found"
echo ""
echo "Log file: test-hybrid-recording.log"

# Propagate app exit code (124 indicates hard timeout)
exit ${app_rc:-0}

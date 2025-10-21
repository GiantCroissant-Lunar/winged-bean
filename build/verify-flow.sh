#!/usr/bin/env bash
set -euo pipefail

# Verify end-to-end flow: dungeon plugin loads, app runs, recording starts/stops, URF file exists.
# Usage: ./verify-flow.sh [duration_seconds] [version] [hard_timeout_seconds]

DURATION="${1:-5}"
REQ_VERSION="${2:-}"
HARD_TIMEOUT="${3:-}"

cd "$(dirname "$0")"

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

BIN_DIR="_artifacts/${REQ_VERSION}/dotnet/desktop/bin"
LOG_APP="${BIN_DIR}/test-hybrid-recording.log"
LOG_HOST_DIR="${BIN_DIR}/logs"

echo "== Running hybrid test (duration=${DURATION}s, version=${REQ_VERSION}) =="
if [[ -n "${HARD_TIMEOUT}" ]]; then
  ./test-hybrid-recording.sh "${DURATION}" "${REQ_VERSION}" "${HARD_TIMEOUT}" || true
else
  ./test-hybrid-recording.sh "${DURATION}" "${REQ_VERSION}" || true
fi

# Collect latest host log
LATEST_HOST_LOG=$(ls -t "${LOG_HOST_DIR}"/winged-bean-*.log 2>/dev/null | head -n1 || true)
START_TS=$(date +%s)

echo "== Analyzing logs =="
ok=1

check() {
  local name="$1"; shift
  local pattern="$1"; shift
  local file="$1"; shift
  if rg -n "${pattern}" -S "${file}" >/dev/null 2>&1; then
    echo "[✓] ${name}"
  else
    echo "[ ] ${name} (not found)"
    ok=0
  fi
}

if [[ -f "${LOG_APP}" ]]; then
  check "App started (diagnostic)" "Diagnostic Startup" "${LOG_APP}"
else
  echo "[ ] App log missing: ${LOG_APP}"; ok=0
fi

if [[ -n "${LATEST_HOST_LOG}" && -f "${LATEST_HOST_LOG}" ]]; then
  check "DungeonGame plugin loaded" "WingedBean.Plugins.DungeonGame" "${LATEST_HOST_LOG}"
  check "Recording started" "Recording started" "${LATEST_HOST_LOG}"
  check "Recording stopped" "Recording stopped" "${LATEST_HOST_LOG}"
else
  echo "[ ] Host log not found in ${LOG_HOST_DIR}"; ok=0
fi

# Verify URF output exists and is fresh
URF_DIR="${BIN_DIR}/recordings/urf"
LATEST_URF=$(ls -t "${URF_DIR}"/*.urf.jsonl 2>/dev/null | head -n1 || true)
if [[ -n "${LATEST_URF}" && -f "${LATEST_URF}" ]]; then
  # Freshness: file mtime within 2 minutes of script start
  mt=$(stat -f "%m" "${LATEST_URF}" 2>/dev/null || stat -c "%Y" "${LATEST_URF}" 2>/dev/null || echo 0)
  if [[ $(( START_TS - mt )) -le 120 ]]; then
    echo "[✓] URF present: $(basename "${LATEST_URF}") (fresh)"
  else
    echo "[ ] URF present but stale: $(basename "${LATEST_URF}")"; ok=0
  fi
else
  echo "[ ] URF output not found in ${URF_DIR}"; ok=0
fi

echo "== Summary =="
if [[ "$ok" -eq 1 ]]; then
  echo "Flow OK for version ${REQ_VERSION} (duration=${DURATION}s)"
  exit 0
else
  echo "Flow incomplete. See logs in ${BIN_DIR}"; exit 1
fi

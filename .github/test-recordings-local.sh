#!/bin/bash
# Quick script to test recording workflow locally with act
set -e

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT_ROOT="$(cd "$SCRIPT_DIR/.." && pwd)"

cd "$PROJECT_ROOT"

echo "🎬 Testing Recording Workflow with Act"
echo "======================================"
echo ""

# Check if act is installed
if ! command -v act &> /dev/null; then
    echo "❌ 'act' is not installed"
    echo "   Install: brew install act"
    exit 1
fi

# Check if Docker is running
if ! docker info &> /dev/null; then
    echo "❌ Docker is not running"
    echo "   Please start Docker Desktop"
    exit 1
fi

echo "✅ Prerequisites OK"
echo ""

# Default values
DURATION="${1:-5}"
JOB="${2:-}"

echo "📝 Configuration:"
echo "   Recording duration: ${DURATION}s"
if [ -n "$JOB" ]; then
    echo "   Job filter: $JOB"
else
    echo "   Jobs: All (console + desktop + summary)"
fi
echo ""

# Build act command
ACT_CMD="act workflow_dispatch -W .github/workflows/recording-tests.yml"
ACT_CMD="$ACT_CMD --input record_duration=$DURATION"

if [ -n "$JOB" ]; then
    ACT_CMD="$ACT_CMD -j $JOB"
fi

echo "🚀 Running: $ACT_CMD"
echo ""

# Run act
eval $ACT_CMD

echo ""
echo "✅ Test complete!"
echo ""
echo "📦 Artifacts location: /tmp/act-artifacts/"
echo ""
echo "To view artifacts:"
echo "   ls -la /tmp/act-artifacts/"
echo ""

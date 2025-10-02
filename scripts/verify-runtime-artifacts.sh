#!/bin/bash
# Verification script for runtime artifacts archiving (Issue #170)
# This script verifies that recordings and logs are properly archived in versioned folders

set -e

REPO_ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$REPO_ROOT"

echo "=================================="
echo "Runtime Artifacts Verification"
echo "=================================="
echo ""

# Get current version
VERSION=$(bash build/get-version.sh)
echo "✓ Current version: $VERSION"

# Define expected paths
ARTIFACTS_BASE="build/_artifacts/v${VERSION}"
DOTNET_RECORDINGS="${ARTIFACTS_BASE}/dotnet/recordings"
DOTNET_LOGS="${ARTIFACTS_BASE}/dotnet/logs"
PTY_LOGS="${ARTIFACTS_BASE}/pty/logs"
WEB_LOGS="${ARTIFACTS_BASE}/web/logs"

echo ""
echo "Expected artifact paths:"
echo "  - Dotnet recordings: ${DOTNET_RECORDINGS}"
echo "  - Dotnet logs:       ${DOTNET_LOGS}"
echo "  - PTY logs:          ${PTY_LOGS}"
echo "  - Web logs:          ${WEB_LOGS}"
echo ""

# Check if directories exist or create them
echo "Verifying directory structure..."
if [ -d "$ARTIFACTS_BASE" ]; then
    echo "✓ Artifacts directory exists: ${ARTIFACTS_BASE}"
else
    echo "✗ Artifacts directory does not exist yet: ${ARTIFACTS_BASE}"
    echo "  (Will be created when apps run)"
fi

# Build the demo app
echo ""
echo "Building demo application..."
cd development/dotnet/console
dotnet build src/demos/WingedBean.Demo/WingedBean.Demo.csproj --configuration Release --verbosity quiet

# Run the demo which will create recordings
echo ""
echo "Running demo to generate recording artifact..."
echo "(This will test the recording functionality and create a .cast file)"
echo ""

# Run in background with timeout
timeout 10s dotnet run --project src/demos/WingedBean.Demo/WingedBean.Demo.csproj --configuration Release --no-build 2>&1 || {
    exit_code=$?
    if [ $exit_code -eq 124 ]; then
        echo ""
        echo "✓ Demo timed out as expected (user input required)"
    else
        echo "✗ Demo exited with error code: $exit_code"
    fi
}

cd "$REPO_ROOT"

# Verify recording was created
echo ""
echo "Verifying recordings..."
if [ -d "$DOTNET_RECORDINGS" ]; then
    RECORDING_COUNT=$(find "$DOTNET_RECORDINGS" -name "*.cast" | wc -l)
    if [ "$RECORDING_COUNT" -gt 0 ]; then
        echo "✓ Found $RECORDING_COUNT recording(s) in: ${DOTNET_RECORDINGS}"
        echo ""
        echo "Recording files:"
        find "$DOTNET_RECORDINGS" -name "*.cast" -exec ls -lh {} \;
    else
        echo "✗ No .cast files found in: ${DOTNET_RECORDINGS}"
    fi
else
    echo "✗ Recordings directory does not exist: ${DOTNET_RECORDINGS}"
fi

# Show directory tree
echo ""
echo "Artifact directory structure:"
if [ -d "$ARTIFACTS_BASE" ]; then
    tree "$ARTIFACTS_BASE" -L 3 || ls -R "$ARTIFACTS_BASE"
else
    echo "  (No artifacts created yet)"
fi

echo ""
echo "=================================="
echo "Verification Complete"
echo "=================================="

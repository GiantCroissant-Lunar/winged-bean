#!/bin/bash
# End-to-end test runner for ConsoleDungeon.Host
# Tests console, PTY, and WebSocket modes

set -e

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT_ROOT="$SCRIPT_DIR/../.."
TEST_PROJECT="$SCRIPT_DIR/WingedBean.Tests.E2E.ConsoleDungeon"

echo "========================================="
echo "E2E Tests: ConsoleDungeon.Host"
echo "========================================="
echo ""

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

# Step 1: Build the host
echo "Step 1: Building ConsoleDungeon.Host..."
cd "$PROJECT_ROOT/src/host/ConsoleDungeon.Host"
if dotnet build --configuration Release; then
    echo -e "${GREEN}✓ Host build successful${NC}"
else
    echo -e "${RED}✗ Host build failed${NC}"
    exit 1
fi
echo ""

# Step 2: Build test project
echo "Step 2: Building E2E test project..."
cd "$TEST_PROJECT"
if dotnet build --configuration Release; then
    echo -e "${GREEN}✓ Test project build successful${NC}"
else
    echo -e "${RED}✗ Test project build failed${NC}"
    exit 1
fi
echo ""

# Step 3: Run E2E tests
echo "Step 3: Running E2E tests..."
echo "==========================================="
echo ""

# Run with detailed output
if dotnet test \
    --configuration Release \
    --no-build \
    --logger "console;verbosity=detailed" \
    --filter "Category=E2E" \
    -- \
    RunConfiguration.TestSessionTimeout=300000; then

    echo ""
    echo "==========================================="
    echo -e "${GREEN}✓ All E2E tests passed!${NC}"
    echo "==========================================="
    exit 0
else
    echo ""
    echo "==========================================="
    echo -e "${RED}✗ Some E2E tests failed${NC}"
    echo "==========================================="
    exit 1
fi

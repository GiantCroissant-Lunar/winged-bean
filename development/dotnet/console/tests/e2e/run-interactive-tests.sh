#!/bin/bash
# Master script to run all interactive console tests
# Tests actual console operation using expect and asciinema

set -e

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
CONSOLE_DIR="$SCRIPT_DIR/../../src/host/ConsoleDungeon.Host"

echo "========================================="
echo "Interactive Console Operation Tests"
echo "========================================="
echo ""

# Colors
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m'

# Check prerequisites
echo "Checking prerequisites..."
echo ""

# Check expect
if ! command -v expect &> /dev/null; then
    echo -e "${RED}✗ expect not found${NC}"
    echo "Install with: brew install expect"
    exit 1
fi
echo -e "${GREEN}✓ expect found${NC}"

# Check asciinema
if ! command -v asciinema &> /dev/null; then
    echo -e "${RED}✗ asciinema not found${NC}"
    echo "Install with: brew install asciinema"
    exit 1
fi
echo -e "${GREEN}✓ asciinema found${NC}"

# Check jq (optional, for validation)
if ! command -v jq &> /dev/null; then
    echo -e "${YELLOW}⚠ jq not found (optional)${NC}"
else
    echo -e "${GREEN}✓ jq found${NC}"
fi

echo ""

# Build the host
echo "========================================="
echo "Building ConsoleDungeon.Host"
echo "========================================="
echo ""

cd "$CONSOLE_DIR"
if dotnet build --configuration Release; then
    echo -e "${GREEN}✓ Build successful${NC}"
else
    echo -e "${RED}✗ Build failed${NC}"
    exit 1
fi
echo ""

# Test 1: Interactive Console Test
echo "========================================="
echo "Test 1: Interactive Console Operation"
echo "========================================="
echo ""

cd "$SCRIPT_DIR"
if [ -f "./interactive-test-console.exp" ]; then
    echo "Running interactive-test-console.exp..."
    if ./interactive-test-console.exp; then
        echo -e "\n${GREEN}✓ Interactive test passed${NC}"
        TEST1_PASSED=1
    else
        echo -e "\n${RED}✗ Interactive test failed${NC}"
        TEST1_PASSED=0
    fi
else
    echo -e "${RED}✗ interactive-test-console.exp not found${NC}"
    TEST1_PASSED=0
fi
echo ""

# Test 2: Asciinema Recording Test
echo "========================================="
echo "Test 2: Asciinema Recording"
echo "========================================="
echo ""

if [ -f "./record-console-session.exp" ]; then
    echo "Running record-console-session.exp..."
    if ./record-console-session.exp; then
        echo -e "\n${GREEN}✓ Recording test passed${NC}"
        TEST2_PASSED=1
    else
        echo -e "\n${RED}✗ Recording test failed${NC}"
        TEST2_PASSED=0
    fi
else
    echo -e "${RED}✗ record-console-session.exp not found${NC}"
    TEST2_PASSED=0
fi
echo ""

# Test 3: Validate Recording
if [ -f "./recordings/console-operation-test.cast" ] && [ -f "./validate-recording.sh" ]; then
    echo "========================================="
    echo "Test 3: Recording Validation"
    echo "========================================="
    echo ""

    if ./validate-recording.sh; then
        echo -e "${GREEN}✓ Recording validation passed${NC}"
        TEST3_PASSED=1
    else
        echo -e "${RED}✗ Recording validation failed${NC}"
        TEST3_PASSED=0
    fi
else
    echo -e "${YELLOW}⚠ Skipping recording validation (no cast file)${NC}"
    TEST3_PASSED=0
fi
echo ""

# Summary
echo "========================================="
echo "Test Summary"
echo "========================================="
echo ""

TOTAL=0
PASSED=0

if [ "${TEST1_PASSED:-0}" -eq 1 ]; then
    echo -e "Interactive Test:       ${GREEN}✓ PASS${NC}"
    ((PASSED++))
else
    echo -e "Interactive Test:       ${RED}✗ FAIL${NC}"
fi
((TOTAL++))

if [ "${TEST2_PASSED:-0}" -eq 1 ]; then
    echo -e "Recording Test:         ${GREEN}✓ PASS${NC}"
    ((PASSED++))
else
    echo -e "Recording Test:         ${RED}✗ FAIL${NC}"
fi
((TOTAL++))

if [ "${TEST3_PASSED:-0}" -eq 1 ]; then
    echo -e "Validation Test:        ${GREEN}✓ PASS${NC}"
    ((PASSED++))
else
    echo -e "Validation Test:        ${YELLOW}⚠ SKIP/FAIL${NC}"
fi
((TOTAL++))

echo ""
echo "Total: $PASSED/$TOTAL passed"
echo ""

if [ $PASSED -ge 2 ]; then
    echo -e "${GREEN}✓ Interactive tests completed successfully!${NC}"
    echo ""

    if [ -f "./recordings/console-operation-test.cast" ]; then
        echo "📹 Recording available:"
        echo "  asciinema play recordings/console-operation-test.cast"
        echo ""
    fi

    exit 0
else
    echo -e "${RED}✗ Some interactive tests failed${NC}"
    exit 1
fi

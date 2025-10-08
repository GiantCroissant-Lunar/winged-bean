#!/bin/bash
# Validate asciinema recording to ensure the console app operated correctly
# Checks for expected output patterns in the cast file

set -e

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
RECORDING_DIR="$SCRIPT_DIR/recordings"
CAST_FILE="$RECORDING_DIR/console-operation-test.cast"

echo "========================================="
echo "Recording Validation Script"
echo "========================================="
echo ""

# Colors
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m'

# Check if cast file exists
if [ ! -f "$CAST_FILE" ]; then
    echo -e "${RED}✗ Cast file not found: $CAST_FILE${NC}"
    echo "Run ./record-console-session.exp first"
    exit 1
fi

echo -e "${GREEN}✓ Found cast file: $CAST_FILE${NC}"
echo ""

# Extract text content from cast file
echo "Extracting content from recording..."
CONTENT=$(cat "$CAST_FILE" | jq -r '.[2]' 2>/dev/null || echo "")

if [ -z "$CONTENT" ]; then
    echo -e "${YELLOW}⚠ Could not extract content with jq, trying alternative method${NC}"
    CONTENT=$(cat "$CAST_FILE")
fi

echo "Content length: ${#CONTENT} characters"
echo ""

# Validation checks
echo "========================================="
echo "Running Validation Checks"
echo "========================================="
echo ""

PASSED=0
FAILED=0

# Check 1: Host started
echo -n "1. Checking if host started... "
if echo "$CONTENT" | grep -q "ConsoleDungeon.Host"; then
    echo -e "${GREEN}✓ PASS${NC}"
    ((PASSED++))
else
    echo -e "${RED}✗ FAIL${NC}"
    ((FAILED++))
fi

# Check 2: Foundation services
echo -n "2. Checking foundation services... "
if echo "$CONTENT" | grep -q "Foundation"; then
    echo -e "${GREEN}✓ PASS${NC}"
    ((PASSED++))
else
    echo -e "${RED}✗ FAIL${NC}"
    ((FAILED++))
fi

# Check 3: Plugins loaded
echo -n "3. Checking plugins loaded... "
if echo "$CONTENT" | grep -q "plugin\|Plugin\|Loaded"; then
    echo -e "${GREEN}✓ PASS${NC}"
    ((PASSED++))
else
    echo -e "${RED}✗ FAIL${NC}"
    ((FAILED++))
fi

# Check 4: No fatal errors
echo -n "4. Checking for fatal errors... "
if echo "$CONTENT" | grep -iq "fatal\|unhandled exception"; then
    echo -e "${RED}✗ FAIL (found errors)${NC}"
    ((FAILED++))
else
    echo -e "${GREEN}✓ PASS (no fatal errors)${NC}"
    ((PASSED++))
fi

# Check 5: Terminal UI active
echo -n "5. Checking terminal UI activity... "
if echo "$CONTENT" | grep -q "Terminal\|GUI\|Console"; then
    echo -e "${GREEN}✓ PASS${NC}"
    ((PASSED++))
else
    echo -e "${RED}✗ FAIL${NC}"
    ((FAILED++))
fi

# Check 6: Recording has reasonable length
echo -n "6. Checking recording length... "
FILE_SIZE=$(wc -c < "$CAST_FILE")
if [ "$FILE_SIZE" -gt 1000 ]; then
    echo -e "${GREEN}✓ PASS ($FILE_SIZE bytes)${NC}"
    ((PASSED++))
else
    echo -e "${RED}✗ FAIL (only $FILE_SIZE bytes)${NC}"
    ((FAILED++))
fi

echo ""
echo "========================================="
echo "Validation Summary"
echo "========================================="
echo -e "Passed: ${GREEN}$PASSED${NC}"
echo -e "Failed: ${RED}$FAILED${NC}"
echo ""

if [ $FAILED -eq 0 ]; then
    echo -e "${GREEN}✓ All validation checks passed!${NC}"
    echo ""
    echo "To view the recording:"
    echo "  asciinema play $CAST_FILE"
    echo ""
    exit 0
else
    echo -e "${RED}✗ Some validation checks failed${NC}"
    echo ""
    exit 1
fi

#!/bin/bash
# Test script to run ConsoleDungeon.Host and capture what's happening

echo "=== Environment Check ==="
echo "TERM: $TERM"
echo "DUNGEON_HEADLESS: ${DUNGEON_HEADLESS:-not set}"
echo "DUNGEON_EXIT_ON_UI: ${DUNGEON_EXIT_ON_UI:-not set}"
echo ""

echo "=== Starting ConsoleDungeon.Host ==="
echo "Working directory: $(pwd)"
echo "Executable: /Users/apprenticegc/Work/lunar-horse/personal-work/yokan-projects/winged-bean/build/_artifacts/0.0.1-422/dotnet/bin/ConsoleDungeon.Host"
echo ""
echo "Output will appear below:"
echo "========================================"
echo ""

cd /Users/apprenticegc/Work/lunar-horse/personal-work/yokan-projects/winged-bean

# Make sure we're not in headless mode
unset DUNGEON_HEADLESS

# Run with logging to see what's happening
export DOTNET_ENVIRONMENT=Development

/Users/apprenticegc/Work/lunar-horse/personal-work/yokan-projects/winged-bean/build/_artifacts/0.0.1-422/dotnet/bin/ConsoleDungeon.Host

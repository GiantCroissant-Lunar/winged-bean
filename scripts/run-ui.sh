#!/bin/bash
# Run ConsoleDungeon.Host with Terminal.Gui UI forced on

echo "=== ConsoleDungeon with Terminal.Gui UI ==="
echo ""
echo "Forcing UI mode with FORCE_UI=1"
echo "Press Ctrl+C to quit"
echo ""

cd /Users/apprenticegc/Work/lunar-horse/personal-work/yokan-projects/winged-bean

export FORCE_UI=1
export DOTNET_ENVIRONMENT=Development

exec ./build/_artifacts/latest/dotnet/bin/ConsoleDungeon.Host

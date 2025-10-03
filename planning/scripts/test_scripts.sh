#!/bin/bash
# Quick test script for planning tools

echo "🧪 Testing Planning Scripts"
echo "=========================="
echo ""

echo "1️⃣ Testing validate_and_render.py..."
python3 planning/scripts/validate_and_render.py
if [ $? -eq 0 ]; then
    echo "✅ Validation script OK"
else
    echo "❌ Validation script failed"
    exit 1
fi

echo ""
echo "2️⃣ Testing open_issues_from_plan.py (dry-run)..."
python3 planning/scripts/open_issues_from_plan.py --dry-run | head -50
if [ $? -eq 0 ]; then
    echo "✅ Issue creation script OK"
else
    echo "❌ Issue creation script failed"
    exit 1
fi

echo ""
echo "🎉 All planning scripts working!"

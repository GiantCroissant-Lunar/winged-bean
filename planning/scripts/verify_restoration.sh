#!/bin/bash
# Quick verification that all restored planning files are working

set -e  # Exit on any error

echo "🔍 Verifying Planning Infrastructure"
echo "====================================="
echo ""

# Check files exist
echo "1️⃣ Checking files exist..."
files=(
    "planning/plan.yaml"
    "planning/codex-config-template.toml"
    "planning/owl/planner_server.py"
    "planning/owl/mcp_servers_config.json"
    "planning/scripts/validate_and_render.py"
    "planning/scripts/open_issues_from_plan.py"
)

for file in "${files[@]}"; do
    if [ -f "$file" ]; then
        echo "   ✅ $file"
    else
        echo "   ❌ $file (MISSING)"
        exit 1
    fi
done

echo ""
echo "2️⃣ Checking Python syntax..."
python3 -m py_compile planning/owl/planner_server.py
python3 -m py_compile planning/scripts/validate_and_render.py
python3 -m py_compile planning/scripts/open_issues_from_plan.py
echo "   ✅ All Python scripts compile"

echo ""
echo "3️⃣ Checking plan.yaml is valid YAML..."
python3 -c "import yaml; yaml.safe_load(open('planning/plan.yaml'))"
echo "   ✅ plan.yaml is valid YAML"

echo ""
echo "4️⃣ Checking plan.yaml has correct structure..."
python3 << 'EOF'
import yaml
plan = yaml.safe_load(open('planning/plan.yaml'))
assert 'meta' in plan, "Missing 'meta' section"
assert 'tasks' in plan, "Missing 'tasks' section"
assert len(plan['tasks']) > 0, "No tasks found"
for task in plan['tasks']:
    assert 'id' in task, f"Task missing 'id': {task}"
    assert 'desc' in task, f"Task missing 'desc': {task}"
print(f"   ✅ plan.yaml has valid structure ({len(plan['tasks'])} tasks)")
EOF

echo ""
echo "5️⃣ Running validation script..."
python3 planning/scripts/validate_and_render.py
echo ""

echo "6️⃣ Testing issue creation (dry-run, first 3 tasks)..."
python3 planning/scripts/open_issues_from_plan.py --dry-run | head -100
echo "   ✅ Issue creation script works"

echo ""
echo "========================================"
echo "✅ All planning infrastructure verified!"
echo "========================================"
echo ""
echo "Next steps:"
echo "  • Test in Claude Code: Ask to call owl-planner.plan_dag"
echo "  • Customize plan.yaml with real tasks"
echo "  • Run: python3 planning/scripts/open_issues_from_plan.py --dry-run"

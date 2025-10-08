#!/usr/bin/env bash
# Validate all plugin manifests against the JSON schema

set -e

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
SCHEMA_FILE="$SCRIPT_DIR/plugin-manifest.schema.json"
PLUGINS_DIR="$SCRIPT_DIR/../src/plugins"

echo "=========================================="
echo "Plugin Manifest Validation"
echo "=========================================="
echo "Schema: $SCHEMA_FILE"
echo ""

# Check if ajv is installed
if ! command -v ajv &> /dev/null; then
    echo "❌ ajv-cli is not installed"
    echo "   Install with: npm install -g ajv-cli ajv-formats"
    exit 1
fi

# Find all plugin manifests (excluding build directories)
MANIFESTS=$(find "$PLUGINS_DIR" -name ".plugin.json" ! -path "*/bin/*" ! -path "*/obj/*" | sort)

if [ -z "$MANIFESTS" ]; then
    echo "⚠️  No plugin manifests found"
    exit 0
fi

MANIFEST_COUNT=$(echo "$MANIFESTS" | wc -l | tr -d ' ')
echo "Found $MANIFEST_COUNT plugin manifest(s) to validate"
echo ""

VALID_COUNT=0
INVALID_COUNT=0
ERRORS=""

# Validate each manifest
while IFS= read -r manifest; do
    PLUGIN_NAME=$(basename "$(dirname "$manifest")")

    # Use ajv to validate (draft-07 is the default)
    if ajv validate -s "$SCHEMA_FILE" -d "$manifest" 2>/dev/null; then
        echo "✅ $PLUGIN_NAME"
        ((VALID_COUNT++))
    else
        echo "❌ $PLUGIN_NAME"
        # Capture detailed error
        ERROR_DETAIL=$(ajv validate -s "$SCHEMA_FILE" -d "$manifest" 2>&1 || true)
        ERRORS="$ERRORS\n\n=== $PLUGIN_NAME ===\n$ERROR_DETAIL"
        ((INVALID_COUNT++))
    fi
done <<< "$MANIFESTS"

echo ""
echo "=========================================="
echo "Validation Summary"
echo "=========================================="
echo "✅ Valid:   $VALID_COUNT"
echo "❌ Invalid: $INVALID_COUNT"
echo "📊 Total:   $MANIFEST_COUNT"

if [ $INVALID_COUNT -gt 0 ]; then
    echo ""
    echo "=========================================="
    echo "Validation Errors"
    echo "=========================================="
    echo -e "$ERRORS"
    exit 1
fi

echo ""
echo "✅ All plugin manifests are valid!"
exit 0

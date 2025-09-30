#!/bin/bash

# Chat History Organization Script (Fixed for bash 3.2)
# This script organizes Claude Code chat history exports by date
# Groups multiple files per date into single consolidated files

set -e  # Exit on any error

# Function to display usage
usage() {
    echo "Usage: $0 [directory]"
    echo "  directory: Path to chat history directory (default: current directory)"
    echo ""
    echo "This script will:"
    echo "1. Find all chat history files with pattern: YYYY-MM-DD-caveat-the-messages-below-were-generated-by-the-u-*.txt"
    echo "2. Group files by date"
    echo "3. Sort files chronologically within each date group"
    echo "4. Concatenate all files for each date into YYYY-MM-DD-chat-history-consolidated.txt"
    echo "5. Remove the original individual files"
    echo ""
    echo "WARNING: This will permanently delete the original files after consolidation!"
}

# Check if help is requested
if [[ "$1" == "-h" || "$1" == "--help" ]]; then
    usage
    exit 0
fi

# Set target directory
TARGET_DIR="${1:-.}"
CHAT_HISTORY_DIR="$TARGET_DIR"

# Check if directory exists
if [[ ! -d "$CHAT_HISTORY_DIR" ]]; then
    echo "Error: Directory '$CHAT_HISTORY_DIR' does not exist"
    exit 1
fi

echo "Organizing chat history files in: $CHAT_HISTORY_DIR"
echo "WARNING: This will consolidate files and remove originals. Continue? (y/N)"
read -r confirm
if [[ ! "$confirm" =~ ^[Yy]$ ]]; then
    echo "Operation cancelled."
    exit 0
fi

# Find all chat history files and extract dates
echo "Finding chat history files..."
CHAT_FILES=$(find "$CHAT_HISTORY_DIR" -name "????-??-??-caveat-the-messages-below-were-generated-by-the-u-*.txt" -type f)

if [[ -z "$CHAT_FILES" ]]; then
    echo "No chat history files found with expected naming pattern."
    exit 1
fi

# Get unique dates (bash 3.2 compatible approach)
DATES=$(for file in $CHAT_FILES; do
    filename=$(basename "$file")
    echo "${filename:0:10}"
done | sort | uniq)

# Process each date group
for date in $DATES; do
    echo "Processing date: $date"

    # Get files for this date
    files=$(for file in $CHAT_FILES; do
        filename=$(basename "$file")
        file_date="${filename:0:10}"
        if [[ "$file_date" == "$date" ]]; then
            echo "$file"
        fi
    done)

    # Sort files by modification time (oldest first)
    files=$(echo "$files" | xargs -I {} ls -tr "{}")

    # Count files
    file_count=$(echo "$files" | wc -l | tr -d ' ')
    echo "  Found $file_count files"

    # Create consolidated filename
    consolidated_file="$CHAT_HISTORY_DIR/${date}-chat-history-consolidated.txt"

    # Check if consolidated file already exists
    if [[ -f "$consolidated_file" ]]; then
        echo "  Warning: Consolidated file already exists: $consolidated_file"
        echo "  Skipping this date group."
        continue
    fi

    # Concatenate files in chronological order (oldest first)
    echo "  Creating consolidated file: $(basename "$consolidated_file")"
    echo "$files" | xargs cat > "$consolidated_file"

    # Verify the consolidated file was created successfully
    if [[ ! -f "$consolidated_file" ]]; then
        echo "  Error: Failed to create consolidated file"
        continue
    fi

    # Get file sizes for verification
    original_total=0
    for file in $files; do
        size=$(stat -f%z "$file" 2>/dev/null || stat -c%s "$file" 2>/dev/null)
        original_total=$((original_total + size))
    done

    consolidated_size=$(stat -f%z "$consolidated_file" 2>/dev/null || stat -c%s "$consolidated_file" 2>/dev/null)

    echo "  Original files total: $original_total bytes"
    echo "  Consolidated file: $consolidated_size bytes"

    if [[ $original_total -eq $consolidated_size ]]; then
        echo "  ✓ File sizes match - consolidation successful"

        # Remove original files
        echo "  Removing $file_count original files..."
        echo "$files" | xargs rm -f
        echo "  ✓ Cleanup completed for $date"
    else
        echo "  ⚠ Warning: File sizes don't match! Original files not removed."
        echo "    This may indicate an issue with the consolidation."
    fi

    echo ""
done

echo "Organization complete!"
echo ""
echo "Summary of consolidated files:"
ls -la "$CHAT_HISTORY_DIR"/*-chat-history-consolidated.txt 2>/dev/null || echo "No consolidated files found"

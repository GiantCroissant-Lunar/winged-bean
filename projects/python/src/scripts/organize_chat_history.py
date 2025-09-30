#!/usr/bin/env python3
"""
Chat History Organization Script

This script organizes Claude Code chat history exports by date.
Groups multiple files per date into single consolidated files.

Usage:
    python organize_chat_history.py [directory]

Arguments:
    directory: Path to chat history directory (default: current directory)

The script will:
1. Find all chat history files with pattern: YYYY-MM-DD-caveat-the-messages-below-were-generated-by-the-u-*.txt
2. Group files by date
3. Sort files chronologically within each date group
4. Concatenate all files for each date into YYYY-MM-DD-chat-history-consolidated.txt
5. Remove the original individual files

WARNING: This will permanently delete the original files after consolidation!
"""

import argparse
import glob
import os
import sys
from collections import defaultdict
from pathlib import Path


def parse_arguments():
    """Parse command line arguments."""
    parser = argparse.ArgumentParser(
        description="Organize Claude Code chat history files by date",
        formatter_class=argparse.RawDescriptionHelpFormatter,
        epilog="""
Examples:
    python organize_chat_history.py                    # Organize current directory
    python organize_chat_history.py /path/to/chat-history  # Organize specific directory

WARNING: This will permanently delete the original files after consolidation!
        """,
    )
    parser.add_argument(
        "directory",
        nargs="?",
        default=".",
        help="Path to chat history directory (default: current directory)",
    )

    return parser.parse_args()


def find_chat_history_files(directory):
    """Find all chat history files with the expected naming pattern."""
    pattern = os.path.join(
        directory, "????-??-??-caveat-the-messages-below-were-generated-by-the-u-*.txt"
    )
    return glob.glob(pattern)


def extract_date_from_filename(filename):
    """Extract date from filename (first 10 characters: YYYY-MM-DD)."""
    basename = os.path.basename(filename)
    return basename[:10]


def group_files_by_date(files):
    """Group files by date extracted from filename."""
    date_groups = defaultdict(list)

    for file_path in files:
        date = extract_date_from_filename(file_path)
        date_groups[date].append(file_path)

    return date_groups


def sort_files_chronologically(file_paths):
    """Sort files by modification time (oldest first)."""
    # Get file stats and sort by modification time
    file_stats = []
    for file_path in file_paths:
        try:
            stat = os.stat(file_path)
            file_stats.append((stat.st_mtime, file_path))
        except OSError as e:
            print(f"Warning: Could not access file {file_path}: {e}")
            continue

    # Sort by modification time (oldest first)
    file_stats.sort(key=lambda x: x[0])
    return [file_path for _, file_path in file_stats]


def get_total_size(file_paths):
    """Get total size of all files in bytes."""
    total_size = 0
    for file_path in file_paths:
        try:
            total_size += os.path.getsize(file_path)
        except OSError as e:
            print(f"Warning: Could not get size of {file_path}: {e}")
    return total_size


def concatenate_files(input_files, output_file, append=False):
    """Concatenate multiple files into one output file."""
    try:
        with open(output_file, "ab" if append else "wb") as outfile:
            for input_file in input_files:
                with open(input_file, "rb") as infile:
                    # Copy file contents
                    while True:
                        chunk = infile.read(8192)  # Read in 8KB chunks
                        if not chunk:
                            break
                        outfile.write(chunk)
        return True
    except OSError as e:
        print(f"Error: Failed to concatenate files: {e}")
        return False


def confirm_action(message):
    """Get user confirmation for destructive actions."""
    try:
        response = input(f"{message} (y/N): ").strip().lower()
        return response in ("y", "yes")
    except (EOFError, KeyboardInterrupt):
        print("\nOperation cancelled.")
        return False


def main():
    """Main function."""
    args = parse_arguments()
    chat_history_dir = Path(args.directory).resolve()

    # Check if directory exists
    if not chat_history_dir.exists():
        print(f"Error: Directory '{chat_history_dir}' does not exist")
        sys.exit(1)

    if not chat_history_dir.is_dir():
        print(f"Error: '{chat_history_dir}' is not a directory")
        sys.exit(1)

    print(f"Organizing chat history files in: {chat_history_dir}")
    print("WARNING: This will consolidate files and remove originals. Continue? (y/N)")

    if not confirm_action(""):
        print("Operation cancelled.")
        sys.exit(0)

    # Find all chat history files
    print("Finding chat history files...")
    chat_files = find_chat_history_files(str(chat_history_dir))

    if not chat_files:
        print("No chat history files found with expected naming pattern.")
        sys.exit(1)

    # Group files by date
    date_groups = group_files_by_date(chat_files)

    # Process each date group
    for date, files in sorted(date_groups.items()):
        print(f"Processing date: {date}")

        # Sort files chronologically (oldest first)
        sorted_files = sort_files_chronologically(files)

        file_count = len(sorted_files)
        print(f"  Found {file_count} files")

        # Create consolidated filename
        consolidated_file = chat_history_dir / f"{date}-chat-history-consolidated.txt"

        # Check if consolidated file already exists
        # Check if consolidated file already exists
        append_mode = consolidated_file.exists()
        if append_mode:
            print(
                f"  Consolidated file exists, will append new files: {consolidated_file}"
            )
        else:
            print(f"  Creating new consolidated file: {consolidated_file.name}")
        # Get original total size for verification
        original_total = get_total_size(sorted_files)
        original_consolidated_size = (
            consolidated_file.stat().st_size if append_mode else 0
        )
        expected_total = original_total + original_consolidated_size

        # Concatenate files
        print(f"  Creating consolidated file: {consolidated_file.name}")
        if not concatenate_files(sorted_files, consolidated_file, append_mode):
            print("  Error: Failed to create consolidated file")
            continue

        # Verify consolidation
        try:
            consolidated_size = consolidated_file.stat().st_size
        except OSError as e:
            print(f"  Error: Could not verify consolidated file: {e}")
            continue

        print(f"  Original files total: {original_total} bytes")
        print(f"  Consolidated file: {consolidated_size} bytes")

        if expected_total == consolidated_size:
            print("  ✓ File sizes match - consolidation successful")

            # Remove original files
            print(f"  Removing {file_count} original files...")
            removed_count = 0
            for file_path in sorted_files:
                try:
                    os.remove(file_path)
                    removed_count += 1
                except OSError as e:
                    print(f"  Warning: Could not remove {file_path}: {e}")

            if removed_count == file_count:
                print("  ✓ Cleanup completed for {date}")
            else:
                print(f"  ⚠ Warning: Only removed {removed_count}/{file_count} files")
        else:
            print("  ⚠ Warning: File sizes don't match! Original files not removed.")
            print("    This may indicate an issue with the consolidation.")

        print()

    print("Organization complete!")
    print()
    print("Summary of consolidated files:")

    # List consolidated files
    consolidated_pattern = str(chat_history_dir / "*-chat-history-consolidated.txt")
    consolidated_files = glob.glob(consolidated_pattern)

    if consolidated_files:
        # Sort by modification time (newest first)
        consolidated_files.sort(key=lambda x: os.path.getmtime(x), reverse=True)

        for file_path in consolidated_files:
            try:
                size = os.path.getsize(file_path)
                print(f"  {file_path} ({size} bytes)")
            except OSError:
                print(f"  {file_path} (could not get size)")
    else:
        print("No consolidated files found")


if __name__ == "__main__":
    main()

#!/usr/bin/env python3
"""Detect orphaned (unreferenced) documentation files.

This script scans the docs/ directory to find markdown files that are not
referenced from any other document, helping maintain documentation discoverability.

Usage:
    python detect_orphans.py
    python detect_orphans.py --format=json
    python detect_orphans.py --format=markdown
    python detect_orphans.py --strict
"""

import argparse
import json
import re
import sys
from dataclasses import dataclass, field
from datetime import datetime, timedelta
from pathlib import Path
from typing import Dict, List, Set


@dataclass
class OrphanedFile:
    """Represents an orphaned documentation file."""
    path: Path
    last_modified: datetime
    age_days: int
    reason: str = "No references found"
    suggestion: str = ""

    def to_dict(self) -> Dict:
        """Convert to dictionary for JSON serialization."""
        return {
            'path': str(self.path),
            'last_modified': self.last_modified.isoformat(),
            'age_days': self.age_days,
            'reason': self.reason,
            'suggestion': self.suggestion
        }


class OrphanDetector:
    """Detects orphaned documentation files."""

    # Files/directories to exclude from orphan detection
    EXCLUDE_PATTERNS = {
        '*/README.md',
        '*/INDEX.md',
        'docs/INDEX.md',
        '.archive/**',
        '**/node_modules/**',
        '**/.git/**',
    }

    # Markdown link patterns
    MARKDOWN_LINK_PATTERN = re.compile(r'\[([^\]]+)\]\(([^)]+)\)')
    WIKI_LINK_PATTERN = re.compile(r'\[\[([^\]]+)\]\]')

    def __init__(self, docs_root: Path):
        self.docs_root = docs_root
        self.all_files: Set[Path] = set()
        self.references: Dict[Path, Set[Path]] = {}  # file -> set of files that reference it
        self.orphans: List[OrphanedFile] = []

    def should_exclude(self, file_path: Path) -> bool:
        """Check if file should be excluded from orphan detection."""
        relative_path = file_path.relative_to(self.docs_root.parent)
        path_str = str(relative_path)

        for pattern in self.EXCLUDE_PATTERNS:
            if self._match_pattern(path_str, pattern):
                return True

        return False

    def _match_pattern(self, path: str, pattern: str) -> bool:
        """Simple glob-like pattern matching."""
        import fnmatch
        return fnmatch.fnmatch(path, pattern)

    def scan_files(self) -> None:
        """Scan docs directory for all markdown files."""
        for md_file in self.docs_root.rglob('*.md'):
            if not self.should_exclude(md_file):
                self.all_files.add(md_file)
                if md_file not in self.references:
                    self.references[md_file] = set()

    def extract_references(self, file_path: Path) -> Set[Path]:
        """Extract all file references from a markdown file."""
        references = set()

        try:
            with open(file_path, 'r', encoding='utf-8') as f:
                content = f.read()

            # Extract markdown links [text](path)
            for match in self.MARKDOWN_LINK_PATTERN.finditer(content):
                link = match.group(2)
                if not link.startswith(('http://', 'https://', '#', 'mailto:')):
                    # Resolve relative path
                    referenced_file = self._resolve_reference(file_path, link)
                    if referenced_file:
                        references.add(referenced_file)

            # Extract wiki links [[path]]
            for match in self.WIKI_LINK_PATTERN.finditer(content):
                link = match.group(1)
                referenced_file = self._resolve_reference(file_path, link)
                if referenced_file:
                    references.add(referenced_file)

        except Exception as e:
            print(f"Warning: Failed to read {file_path}: {e}", file=sys.stderr)

        return references

    def _resolve_reference(self, source_file: Path, link: str) -> Path | None:
        """Resolve a reference link to an absolute path."""
        # Remove anchors and query strings
        link = link.split('#')[0].split('?')[0].strip()
        if not link:
            return None

        # Handle ./ and ../ relative paths
        if link.startswith('./') or link.startswith('../'):
            referenced_file = (source_file.parent / link).resolve()
        else:
            # Try as relative to source file
            referenced_file = (source_file.parent / link).resolve()

        # Check if file exists and is in our file set
        if referenced_file.exists() and referenced_file.suffix == '.md':
            return referenced_file

        return None

    def build_reference_graph(self) -> None:
        """Build reference graph by scanning all files."""
        for file_path in self.all_files:
            referenced_files = self.extract_references(file_path)

            for referenced_file in referenced_files:
                if referenced_file in self.references:
                    self.references[referenced_file].add(file_path)

    def detect_orphans(self) -> None:
        """Detect orphaned files (those with no incoming references)."""
        now = datetime.now()

        for file_path in self.all_files:
            # Skip if file has incoming references
            if self.references.get(file_path):
                continue

            # File is orphaned - gather info
            try:
                stat = file_path.stat()
                last_modified = datetime.fromtimestamp(stat.st_mtime)
                age_days = (now - last_modified).days

                orphan = OrphanedFile(
                    path=file_path.relative_to(self.docs_root.parent),
                    last_modified=last_modified,
                    age_days=age_days
                )

                # Add helpful suggestions
                if 'draft' in file_path.name.lower():
                    orphan.reason = "Draft document (no references expected)"
                    orphan.suggestion = "Complete or remove draft"
                elif age_days > 60:
                    orphan.reason = f"No references found (last modified {age_days} days ago)"
                    orphan.suggestion = f"Archive to .archive/{file_path.parent.name}/{last_modified.year}/{last_modified.month:02d}/"
                else:
                    orphan.reason = "No references found"
                    parent_readme = file_path.parent / 'README.md'
                    if parent_readme.exists():
                        orphan.suggestion = f"Link from {parent_readme.relative_to(self.docs_root.parent)}"
                    else:
                        orphan.suggestion = f"Add index file or link from related docs"

                self.orphans.append(orphan)

            except Exception as e:
                print(f"Warning: Failed to stat {file_path}: {e}", file=sys.stderr)

    def print_console_report(self) -> None:
        """Print human-readable report to console."""
        if not self.orphans:
            print("✅ No orphaned files detected")
            return

        print(f"⚠️  Orphaned Files Detected ({len(self.orphans)}):\n")

        for orphan in sorted(self.orphans, key=lambda o: o.age_days, reverse=True):
            print(f"  {orphan.path}")
            print(f"    → {orphan.reason}")
            if orphan.suggestion:
                print(f"    → Suggestion: {orphan.suggestion}")
            print()

        print(f"Summary: {len(self.orphans)} orphaned files")

    def print_json_report(self) -> None:
        """Print JSON report."""
        report = {
            'orphaned_files': [o.to_dict() for o in self.orphans],
            'total_orphans': len(self.orphans),
            'scan_date': datetime.now().isoformat()
        }
        print(json.dumps(report, indent=2))

    def print_markdown_report(self) -> None:
        """Print Markdown report for issues/PRs."""
        print("# Orphaned Files Report\n")

        if not self.orphans:
            print("✅ No orphaned files detected\n")
            return

        print(f"⚠️ Found {len(self.orphans)} orphaned files:\n")
        print("| File | Age (days) | Suggestion |")
        print("|------|------------|------------|")

        for orphan in sorted(self.orphans, key=lambda o: o.age_days, reverse=True):
            print(f"| `{orphan.path}` | {orphan.age_days} | {orphan.suggestion} |")

        print(f"\n**Total:** {len(self.orphans)} orphaned files")
        print(f"\n**Scan Date:** {datetime.now().strftime('%Y-%m-%d %H:%M:%S')}")


def main():
    parser = argparse.ArgumentParser(
        description="Detect orphaned (unreferenced) documentation files",
        formatter_class=argparse.RawDescriptionHelpFormatter,
        epilog="""
Examples:
  # Console report (default)
  %(prog)s

  # JSON format for CI/CD
  %(prog)s --format=json

  # Markdown format for GitHub issues
  %(prog)s --format=markdown > orphan-report.md

  # Strict mode (exit 1 if orphans found)
  %(prog)s --strict
        """
    )

    parser.add_argument(
        '--format',
        choices=['console', 'json', 'markdown'],
        default='console',
        help='Output format (default: console)'
    )

    parser.add_argument(
        '--strict',
        action='store_true',
        help='Exit with code 1 if orphans are found'
    )

    parser.add_argument(
        '--docs-root',
        type=Path,
        help='Path to docs directory (default: auto-detect)'
    )

    args = parser.parse_args()

    # Determine docs root
    if args.docs_root:
        docs_root = args.docs_root
    else:
        # Auto-detect from script location
        script_path = Path(__file__).resolve()
        repo_root = script_path.parents[5]  # Navigate to repo root
        docs_root = repo_root / 'docs'

    if not docs_root.exists():
        print(f"Error: docs directory not found at {docs_root}", file=sys.stderr)
        sys.exit(1)

    # Run detection
    detector = OrphanDetector(docs_root)
    detector.scan_files()
    detector.build_reference_graph()
    detector.detect_orphans()

    # Print report in requested format
    if args.format == 'json':
        detector.print_json_report()
    elif args.format == 'markdown':
        detector.print_markdown_report()
    else:
        detector.print_console_report()

    # Exit with appropriate code
    if args.strict and detector.orphans:
        sys.exit(1)
    else:
        sys.exit(0)


if __name__ == '__main__':
    main()

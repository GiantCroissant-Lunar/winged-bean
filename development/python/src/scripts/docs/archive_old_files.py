#!/usr/bin/env python3
"""Auto-archive old documentation files based on retention policies.

This script enforces retention policies by archiving old chat-history,
recordings, and test results to .archive/ directory.

Usage:
    python archive_old_files.py                    # Dry-run preview
    python archive_old_files.py --execute          # Execute with confirmation
    python archive_old_files.py --execute --yes    # Auto-confirm (CI/CD)
    python archive_old_files.py --category=recordings --execute
"""

import argparse
import json
import shutil
import sys
from dataclasses import dataclass
from datetime import datetime, timedelta
from pathlib import Path
from typing import Dict, List, Optional


@dataclass
class ArchivalRule:
    """Defines archival rule for a category of files."""
    pattern: str
    age_days: int
    archive_to: str
    exclude_patterns: List[str] = None

    def __post_init__(self):
        if self.exclude_patterns is None:
            self.exclude_patterns = []


@dataclass
class ArchivalCandidate:
    """Represents a file candidate for archival."""
    source: Path
    destination: Path
    size_bytes: int
    age_days: int
    excluded: bool = False
    exclusion_reason: str = ""


# Archival rules from RFC-0013
ARCHIVAL_RULES = {
    'chat-history': ArchivalRule(
        pattern='docs/chat-history/*.txt',
        age_days=30,
        archive_to='.archive/chat-history/{year}/{month:02d}/'
    ),
    'recordings-cast': ArchivalRule(
        pattern='docs/recordings/*.cast',
        age_days=30,
        archive_to='.archive/recordings/{year}/{month:02d}/',
        exclude_patterns=['baseline-*']
    ),
    'recordings-txt': ArchivalRule(
        pattern='docs/recordings/*.txt',
        age_days=30,
        archive_to='.archive/recordings/{year}/{month:02d}/'
    ),
    'test-results': ArchivalRule(
        pattern='docs/test-results/**/*.md',
        age_days=90,
        archive_to='.archive/test-results/{year}/{month:02d}/',
        exclude_patterns=['phase*', 'README.md']
    ),
}


class Archiver:
    """Handles archival of old documentation files."""

    def __init__(self, repo_root: Path, dry_run: bool = True):
        self.repo_root = repo_root
        self.dry_run = dry_run
        self.candidates: List[ArchivalCandidate] = []
        self.archived_count = 0
        self.skipped_count = 0
        self.total_size_saved = 0

    def _match_exclude_pattern(self, file_path: Path, patterns: List[str]) -> Optional[str]:
        """Check if file matches any exclusion pattern."""
        import fnmatch
        file_name = file_path.name

        for pattern in patterns:
            if fnmatch.fnmatch(file_name, pattern):
                return pattern

        return None

    def find_candidates(self, category: Optional[str] = None) -> None:
        """Find files matching archival rules."""
        rules_to_check = ARCHIVAL_RULES

        if category:
            if category not in ARCHIVAL_RULES:
                print(f"Error: Unknown category '{category}'", file=sys.stderr)
                print(f"Available categories: {', '.join(ARCHIVAL_RULES.keys())}", file=sys.stderr)
                sys.exit(1)
            rules_to_check = {category: ARCHIVAL_RULES[category]}

        now = datetime.now()
        cutoff_date = now

        for rule_name, rule in rules_to_check.items():
            cutoff_date = now - timedelta(days=rule.age_days)

            # Parse pattern to get base path and glob
            pattern_parts = rule.pattern.split('/')
            if pattern_parts[0] == 'docs':
                base_path = self.repo_root / 'docs'
                glob_pattern = '/'.join(pattern_parts[1:])
            else:
                base_path = self.repo_root
                glob_pattern = rule.pattern

            # Find matching files
            if '**' in glob_pattern:
                matches = base_path.glob(glob_pattern)
            else:
                # Handle single-level glob
                parent_dir = base_path / Path(glob_pattern).parent
                if parent_dir.exists():
                    pattern_name = Path(glob_pattern).name
                    matches = parent_dir.glob(pattern_name)
                else:
                    matches = []

            for file_path in matches:
                if not file_path.is_file():
                    continue

                # Check file age
                stat = file_path.stat()
                last_modified = datetime.fromtimestamp(stat.st_mtime)
                age = (now - last_modified).days

                if age < rule.age_days:
                    continue

                # Check exclusion patterns
                exclusion_pattern = self._match_exclude_pattern(file_path, rule.exclude_patterns)

                # Calculate destination
                archive_path_template = rule.archive_to
                archive_path = archive_path_template.format(
                    year=last_modified.year,
                    month=last_modified.month
                )
                destination = self.repo_root / archive_path / file_path.name

                candidate = ArchivalCandidate(
                    source=file_path,
                    destination=destination,
                    size_bytes=stat.st_size,
                    age_days=age,
                    excluded=exclusion_pattern is not None,
                    exclusion_reason=f"exclusion_pattern: {exclusion_pattern}" if exclusion_pattern else ""
                )

                self.candidates.append(candidate)

    def print_preview(self) -> None:
        """Print preview of archival actions."""
        print("Documentation Archival Preview")
        print("=" * 50)
        print()

        if not self.candidates:
            print("No files match archival criteria")
            return

        # Group by category
        categories: Dict[str, List[ArchivalCandidate]] = {}

        for candidate in self.candidates:
            # Determine category from path
            if 'chat-history' in str(candidate.source):
                category = 'Chat History (30-day retention)'
            elif 'recordings' in str(candidate.source):
                category = 'Recordings (30-day retention)'
            elif 'test-results' in str(candidate.source):
                category = 'Test Results (90-day retention)'
            else:
                category = 'Other'

            if category not in categories:
                categories[category] = []
            categories[category].append(candidate)

        # Print by category
        for category, candidates in sorted(categories.items()):
            print(f"{category}:")

            for candidate in candidates:
                if candidate.excluded:
                    print(f"  ⊘ {candidate.source.relative_to(self.repo_root)} ({candidate.exclusion_reason})")
                else:
                    print(f"  ✓ {candidate.source.relative_to(self.repo_root)}")
                    print(f"    → {candidate.destination.relative_to(self.repo_root)}")

            print()

        # Summary
        to_archive = [c for c in self.candidates if not c.excluded]
        skipped = [c for c in self.candidates if c.excluded]
        total_size = sum(c.size_bytes for c in to_archive)

        print("Summary:")
        print(f"  To archive: {len(to_archive)} files")
        print(f"  Skipped: {len(skipped)} files (exclusions)")
        print(f"  Space to save: {self._format_size(total_size)}")

        if self.dry_run:
            print()
            print("This is a DRY RUN. No files will be moved.")
            print("Use --execute to perform archival.")

    def execute_archival(self, auto_confirm: bool = False) -> None:
        """Execute archival of files."""
        to_archive = [c for c in self.candidates if not c.excluded]

        if not to_archive:
            print("No files to archive")
            return

        # Confirmation prompt
        if not auto_confirm:
            print(f"\nAbout to archive {len(to_archive)} files.")
            response = input("Continue? [y/N]: ")
            if response.lower() != 'y':
                print("Archival cancelled")
                return

        # Create archival log
        log_entries = []

        # Archive files
        for candidate in to_archive:
            try:
                # Create destination directory
                candidate.destination.parent.mkdir(parents=True, exist_ok=True)

                # Move file
                shutil.move(str(candidate.source), str(candidate.destination))

                self.archived_count += 1
                self.total_size_saved += candidate.size_bytes

                log_entries.append({
                    'source': str(candidate.source.relative_to(self.repo_root)),
                    'destination': str(candidate.destination.relative_to(self.repo_root)),
                    'size_bytes': candidate.size_bytes,
                    'age_days': candidate.age_days
                })

                print(f"✓ Archived: {candidate.source.relative_to(self.repo_root)}")

            except Exception as e:
                print(f"✗ Failed to archive {candidate.source}: {e}", file=sys.stderr)

        # Update archival log
        self._update_archival_log(log_entries)

        # Print summary
        print()
        print("Archival Complete:")
        print(f"  Archived: {self.archived_count} files")
        print(f"  Space saved: {self._format_size(self.total_size_saved)}")

    def _update_archival_log(self, entries: List[Dict]) -> None:
        """Update archival log with new entries."""
        log_file = self.repo_root / '.archive' / 'archival-log.json'

        # Load existing log
        if log_file.exists():
            with open(log_file, 'r') as f:
                log_data = json.load(f)
        else:
            log_data = {'archival_runs': []}

        # Add new run
        log_data['archival_runs'].append({
            'timestamp': datetime.now().isoformat(),
            'files_archived': entries,
            'summary': {
                'total_archived': len(entries),
                'space_saved_bytes': sum(e['size_bytes'] for e in entries)
            }
        })

        # Save log
        log_file.parent.mkdir(parents=True, exist_ok=True)
        with open(log_file, 'w') as f:
            json.dump(log_data, f, indent=2)

        print(f"Log updated: {log_file.relative_to(self.repo_root)}")

    def _format_size(self, size_bytes: int) -> str:
        """Format byte size as human-readable string."""
        for unit in ['B', 'KB', 'MB', 'GB']:
            if size_bytes < 1024.0:
                return f"{size_bytes:.1f} {unit}"
            size_bytes /= 1024.0
        return f"{size_bytes:.1f} TB"


def main():
    parser = argparse.ArgumentParser(
        description="Auto-archive old documentation files",
        formatter_class=argparse.RawDescriptionHelpFormatter,
        epilog="""
Examples:
  # Preview what would be archived (dry-run)
  %(prog)s

  # Execute archival with confirmation
  %(prog)s --execute

  # Auto-confirm (for CI/CD)
  %(prog)s --execute --yes

  # Archive specific category only
  %(prog)s --category=recordings --execute

Available categories:
  - chat-history: 30-day retention
  - recordings-cast: 30-day retention (exclude baseline-*)
  - recordings-txt: 30-day retention
  - test-results: 90-day retention (exclude phase*)
        """
    )

    parser.add_argument(
        '--execute',
        action='store_true',
        help='Execute archival (default is dry-run)'
    )

    parser.add_argument(
        '--yes',
        action='store_true',
        help='Auto-confirm without prompting'
    )

    parser.add_argument(
        '--category',
        choices=list(ARCHIVAL_RULES.keys()),
        help='Archive specific category only'
    )

    parser.add_argument(
        '--repo-root',
        type=Path,
        help='Path to repository root (default: auto-detect)'
    )

    args = parser.parse_args()

    # Determine repo root
    if args.repo_root:
        repo_root = args.repo_root
    else:
        # Auto-detect from script location
        script_path = Path(__file__).resolve()
        repo_root = script_path.parents[5]  # Navigate to repo root

    if not repo_root.exists():
        print(f"Error: Repository root not found at {repo_root}", file=sys.stderr)
        sys.exit(1)

    # Create archiver
    archiver = Archiver(repo_root, dry_run=not args.execute)

    # Find candidates
    archiver.find_candidates(category=args.category)

    # Print preview
    archiver.print_preview()

    # Execute if requested
    if args.execute:
        archiver.execute_archival(auto_confirm=args.yes)


if __name__ == '__main__':
    main()

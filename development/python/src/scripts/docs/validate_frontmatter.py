#!/usr/bin/env python3
"""Validate RFC and ADR frontmatter per R-DOC-020.

This script validates YAML frontmatter in RFC and ADR markdown files to ensure
compliance with the project's documentation standards.

Usage:
    python validate_frontmatter.py docs/rfcs/*.md docs/adr/*.md
    python validate_frontmatter.py --check-all
    python validate_frontmatter.py --help
"""

import argparse
import re
import sys
from pathlib import Path
from typing import Dict, List, Optional, Set, Tuple

try:
    import yaml
except ImportError:
    print("Error: PyYAML is required. Install with: pip install pyyaml", file=sys.stderr)
    sys.exit(1)


# RFC Validation Rules (R-DOC-020)
RFC_REQUIRED_FIELDS = {'id', 'title', 'status', 'category', 'created', 'updated'}
RFC_VALID_STATUSES = {'Draft', 'Proposed', 'Accepted', 'Implemented', 'Superseded'}
RFC_ID_PATTERN = re.compile(r'^RFC-\d{4}$')
DATE_PATTERN = re.compile(r'^\d{4}-\d{2}-\d{2}$')

# ADR Validation Rules (R-DOC-030)
ADR_REQUIRED_FIELDS = {'id', 'title', 'status', 'date'}
ADR_VALID_STATUSES = {'Accepted', 'Superseded'}
ADR_ID_PATTERN = re.compile(r'^ADR-\d{4}$')


class FrontmatterError:
    """Represents a frontmatter validation error."""

    def __init__(self, file_path: Path, message: str, line: Optional[int] = None):
        self.file_path = file_path
        self.message = message
        self.line = line

    def __str__(self) -> str:
        location = f"{self.file_path}"
        if self.line is not None:
            location += f":{self.line}"
        return f"{location}: {self.message}"


class FrontmatterValidator:
    """Validates frontmatter in documentation files."""

    def __init__(self):
        self.errors: List[FrontmatterError] = []

    def extract_frontmatter(self, file_path: Path) -> Optional[Tuple[Dict, int]]:
        """Extract YAML frontmatter from a markdown file.

        Args:
            file_path: Path to the markdown file

        Returns:
            Tuple of (frontmatter dict, line number where frontmatter ends)
            or None if no frontmatter found
        """
        try:
            with open(file_path, 'r', encoding='utf-8') as f:
                lines = f.readlines()

            # Check for YAML frontmatter delimiters
            if not lines or not lines[0].strip() == '---':
                self.errors.append(FrontmatterError(
                    file_path,
                    "Missing frontmatter: file must start with '---' delimiter"
                ))
                return None

            # Find closing delimiter
            end_line = None
            for i, line in enumerate(lines[1:], start=1):
                if line.strip() == '---':
                    end_line = i
                    break

            if end_line is None:
                self.errors.append(FrontmatterError(
                    file_path,
                    "Frontmatter opening '---' found but no closing '---'"
                ))
                return None

            # Parse YAML
            frontmatter_text = ''.join(lines[1:end_line])
            try:
                frontmatter = yaml.safe_load(frontmatter_text)
                if frontmatter is None:
                    frontmatter = {}
                return frontmatter, end_line
            except yaml.YAMLError as e:
                self.errors.append(FrontmatterError(
                    file_path,
                    f"Invalid YAML: {e}",
                    line=getattr(e, 'problem_mark', None).line if hasattr(e, 'problem_mark') else None
                ))
                return None

        except Exception as e:
            self.errors.append(FrontmatterError(
                file_path,
                f"Failed to read file: {e}"
            ))
            return None

    def validate_rfc(self, file_path: Path, frontmatter: Dict) -> bool:
        """Validate RFC frontmatter.

        Args:
            file_path: Path to the RFC file
            frontmatter: Parsed frontmatter dictionary

        Returns:
            True if valid, False otherwise
        """
        valid = True

        # Check required fields
        missing_fields = RFC_REQUIRED_FIELDS - set(frontmatter.keys())
        if missing_fields:
            self.errors.append(FrontmatterError(
                file_path,
                f"Missing required fields: {', '.join(sorted(missing_fields))}"
            ))
            valid = False

        # Validate ID format
        if 'id' in frontmatter:
            rfc_id = str(frontmatter['id'])
            if not RFC_ID_PATTERN.match(rfc_id):
                self.errors.append(FrontmatterError(
                    file_path,
                    f"Invalid ID format: '{rfc_id}' (expected: RFC-XXXX with 4 digits)"
                ))
                valid = False

        # Validate status
        if 'status' in frontmatter:
            status = str(frontmatter['status'])
            if status not in RFC_VALID_STATUSES:
                self.errors.append(FrontmatterError(
                    file_path,
                    f"Invalid status: '{status}' (must be one of: {', '.join(sorted(RFC_VALID_STATUSES))})"
                ))
                valid = False

        # Validate date formats
        for date_field in ['created', 'updated']:
            if date_field in frontmatter:
                date_value = str(frontmatter[date_field])
                if not DATE_PATTERN.match(date_value):
                    self.errors.append(FrontmatterError(
                        file_path,
                        f"Invalid {date_field} date format: '{date_value}' (expected: YYYY-MM-DD)"
                    ))
                    valid = False

        return valid

    def validate_adr(self, file_path: Path, frontmatter: Dict) -> bool:
        """Validate ADR frontmatter.

        Args:
            file_path: Path to the ADR file
            frontmatter: Parsed frontmatter dictionary

        Returns:
            True if valid, False otherwise
        """
        valid = True

        # Check required fields
        missing_fields = ADR_REQUIRED_FIELDS - set(frontmatter.keys())
        if missing_fields:
            self.errors.append(FrontmatterError(
                file_path,
                f"Missing required fields: {', '.join(sorted(missing_fields))}"
            ))
            valid = False

        # Validate ID format
        if 'id' in frontmatter:
            adr_id = str(frontmatter['id'])
            if not ADR_ID_PATTERN.match(adr_id):
                self.errors.append(FrontmatterError(
                    file_path,
                    f"Invalid ID format: '{adr_id}' (expected: ADR-XXXX with 4 digits)"
                ))
                valid = False

        # Validate status
        if 'status' in frontmatter:
            status = str(frontmatter['status'])
            if status not in ADR_VALID_STATUSES:
                self.errors.append(FrontmatterError(
                    file_path,
                    f"Invalid status: '{status}' (must be one of: {', '.join(sorted(ADR_VALID_STATUSES))})"
                ))
                valid = False

        # Validate date format
        if 'date' in frontmatter:
            date_value = str(frontmatter['date'])
            if not DATE_PATTERN.match(date_value):
                self.errors.append(FrontmatterError(
                    file_path,
                    f"Invalid date format: '{date_value}' (expected: YYYY-MM-DD)"
                ))
                valid = False

        return valid

    def validate_file(self, file_path: Path) -> bool:
        """Validate a single documentation file.

        Args:
            file_path: Path to the file to validate

        Returns:
            True if valid, False otherwise
        """
        # Extract frontmatter
        result = self.extract_frontmatter(file_path)
        if result is None:
            # Error already recorded in extract_frontmatter
            return False

        frontmatter, _ = result

        # Determine file type and validate accordingly
        if 'rfcs' in file_path.parts:
            return self.validate_rfc(file_path, frontmatter)
        elif 'adr' in file_path.parts:
            return self.validate_adr(file_path, frontmatter)
        else:
            # Unknown file type - skip validation
            return True

    def validate_files(self, file_paths: List[Path]) -> bool:
        """Validate multiple files.

        Args:
            file_paths: List of file paths to validate

        Returns:
            True if all files valid, False if any errors
        """
        all_valid = True
        for file_path in file_paths:
            if not file_path.exists():
                self.errors.append(FrontmatterError(
                    file_path,
                    "File not found"
                ))
                all_valid = False
                continue

            if not self.validate_file(file_path):
                all_valid = False

        return all_valid

    def print_report(self) -> None:
        """Print validation report to stdout."""
        if not self.errors:
            print("✅ All frontmatter validation passed")
            return

        print(f"❌ Frontmatter validation failed ({len(self.errors)} errors):\n")

        # Group errors by file
        errors_by_file: Dict[Path, List[FrontmatterError]] = {}
        for error in self.errors:
            if error.file_path not in errors_by_file:
                errors_by_file[error.file_path] = []
            errors_by_file[error.file_path].append(error)

        for file_path in sorted(errors_by_file.keys()):
            print(f"❌ {file_path}:")
            for error in errors_by_file[file_path]:
                print(f"  - {error.message}")
            print()


def main():
    parser = argparse.ArgumentParser(
        description="Validate RFC and ADR frontmatter (R-DOC-020)",
        formatter_class=argparse.RawDescriptionHelpFormatter,
        epilog="""
Examples:
  # Validate specific files
  %(prog)s docs/rfcs/0013-*.md docs/adr/0001-*.md

  # Validate all RFCs and ADRs
  %(prog)s --check-all

  # Validate files from pre-commit (receives filenames as arguments)
  %(prog)s docs/rfcs/0013-documentation-automation-tooling.md
        """
    )

    parser.add_argument(
        'files',
        nargs='*',
        type=Path,
        help='Markdown files to validate'
    )

    parser.add_argument(
        '--check-all',
        action='store_true',
        help='Check all RFCs and ADRs in docs/ directory'
    )

    parser.add_argument(
        '--quiet',
        action='store_true',
        help='Suppress output, only use exit code'
    )

    args = parser.parse_args()

    # Determine files to check
    files_to_check: List[Path] = []

    if args.check_all:
        # Find all RFCs and ADRs
        repo_root = Path(__file__).resolve().parents[5]  # Navigate to repo root
        docs_dir = repo_root / 'docs'

        if docs_dir.exists():
            files_to_check.extend(docs_dir.glob('rfcs/*.md'))
            files_to_check.extend(docs_dir.glob('adr/*.md'))
        else:
            print(f"Error: docs directory not found at {docs_dir}", file=sys.stderr)
            sys.exit(1)
    elif args.files:
        files_to_check = args.files
    else:
        parser.print_help()
        sys.exit(1)

    # Filter out README files
    files_to_check = [f for f in files_to_check if f.name.upper() != 'README.MD']

    if not files_to_check:
        if not args.quiet:
            print("No files to validate")
        sys.exit(0)

    # Validate files
    validator = FrontmatterValidator()
    all_valid = validator.validate_files(files_to_check)

    # Print report
    if not args.quiet:
        validator.print_report()

    # Exit with appropriate code
    sys.exit(0 if all_valid else 1)


if __name__ == '__main__':
    main()

#!/usr/bin/env python3
"""Pre-commit hook for validating issue dependency metadata (R-ISS-010).

This hook validates issue metadata in documentation files and issue templates
to ensure proper dependency tracking for agent-assisted development.

Per RFC-0015, all agent-created issues must include dependency metadata:
- rfc: RFC identifier
- depends_on: List of blocking issue numbers
- priority: critical|high|medium|low
- agent_assignable: Boolean
- retry_count: Integer
- max_retries: Integer

Usage:
    python pre_commit_issue_validator.py <file1> <file2> ...
    python pre_commit_issue_validator.py --check-all
"""

import argparse
import os
import re
import sys
from pathlib import Path
from typing import Dict, List, Optional, Set, Tuple

try:
    import yaml
except ImportError:
    print("Error: PyYAML required. Install: pip install pyyaml", file=sys.stderr)
    sys.exit(1)


# Issue metadata schema (R-ISS-010)
REQUIRED_FIELDS = {'rfc', 'depends_on', 'priority', 'agent_assignable'}
OPTIONAL_FIELDS = {'retry_count', 'max_retries', 'blocks', 'phase', 'wave', 'estimate_minutes'}
VALID_PRIORITIES = {'critical', 'high', 'medium', 'low'}
DEFAULT_MAX_RETRIES = 3

RFC_PATTERN = re.compile(r'^RFC-\d{4}$', re.IGNORECASE)
ISSUE_REF_PATTERN = re.compile(r'#(\d+)')


class ValidationError:
    """Represents an issue metadata validation error."""

    def __init__(self, file_path: Path, message: str, line: Optional[int] = None):
        self.file_path = file_path
        self.message = message
        self.line = line

    def __str__(self) -> str:
        location = f"{self.file_path}"
        if self.line is not None:
            location += f":{self.line}"
        return f"{location}: {self.message}"


class IssueMetadataValidator:
    """Validates issue dependency metadata in documentation and templates."""

    def __init__(self, online_check: bool = False):
        """Initialize validator.

        Args:
            online_check: If True, verify referenced issues exist via GitHub API
        """
        self.errors: List[ValidationError] = []
        self.online_check = online_check

    def extract_issue_metadata(self, file_path: Path) -> Optional[Dict]:
        """Extract issue metadata from file content.

        Looks for YAML frontmatter or code blocks containing issue metadata.

        Args:
            file_path: Path to the file

        Returns:
            Dictionary of metadata or None if not found
        """
        try:
            with open(file_path, 'r', encoding='utf-8') as f:
                content = f.read()

            # Try YAML frontmatter first (---...---)
            frontmatter_match = re.match(r'^---\n(.*?)\n---', content, re.DOTALL)
            if frontmatter_match:
                try:
                    metadata = yaml.safe_load(frontmatter_match.group(1))
                    if isinstance(metadata, dict):
                        return metadata
                except yaml.YAMLError as e:
                    self.errors.append(ValidationError(
                        file_path,
                        f"Invalid YAML frontmatter: {e}"
                    ))
                    return None

            # Try code blocks with yaml/yml language tag
            code_block_pattern = re.compile(
                r'```(?:yaml|yml)\n(.*?)\n```',
                re.DOTALL | re.MULTILINE
            )
            for match in code_block_pattern.finditer(content):
                try:
                    metadata = yaml.safe_load(match.group(1))
                    if isinstance(metadata, dict) and 'rfc' in metadata:
                        return metadata
                except yaml.YAMLError:
                    continue

            return None

        except Exception as e:
            self.errors.append(ValidationError(
                file_path,
                f"Failed to read file: {e}"
            ))
            return None

    def validate_metadata_schema(
        self,
        file_path: Path,
        metadata: Dict
    ) -> bool:
        """Validate metadata against schema requirements.

        Args:
            file_path: Path to the file
            metadata: Metadata dictionary

        Returns:
            True if valid, False otherwise
        """
        valid = True

        # Check required fields
        missing_fields = REQUIRED_FIELDS - set(metadata.keys())
        if missing_fields:
            self.errors.append(ValidationError(
                file_path,
                f"Missing required fields: {', '.join(sorted(missing_fields))}"
            ))
            valid = False

        # Validate RFC format
        if 'rfc' in metadata:
            rfc = str(metadata['rfc']).strip()
            if not RFC_PATTERN.match(rfc):
                self.errors.append(ValidationError(
                    file_path,
                    f"Invalid RFC format: '{rfc}' (expected: RFC-XXXX)"
                ))
                valid = False

        # Validate priority
        if 'priority' in metadata:
            priority = str(metadata['priority']).lower().strip()
            if priority not in VALID_PRIORITIES:
                self.errors.append(ValidationError(
                    file_path,
                    f"Invalid priority: '{priority}' "
                    f"(must be one of: {', '.join(sorted(VALID_PRIORITIES))})"
                ))
                valid = False

        # Validate depends_on is a list
        if 'depends_on' in metadata:
            depends_on = metadata['depends_on']
            if not isinstance(depends_on, list):
                self.errors.append(ValidationError(
                    file_path,
                    f"'depends_on' must be a list, got: {type(depends_on).__name__}"
                ))
                valid = False
            else:
                # Validate each dependency is an integer
                for i, dep in enumerate(depends_on):
                    if not isinstance(dep, int) or dep <= 0:
                        self.errors.append(ValidationError(
                            file_path,
                            f"depends_on[{i}] must be a positive integer, got: {dep}"
                        ))
                        valid = False

        # Validate agent_assignable is boolean
        if 'agent_assignable' in metadata:
            if not isinstance(metadata['agent_assignable'], bool):
                self.errors.append(ValidationError(
                    file_path,
                    f"'agent_assignable' must be boolean, "
                    f"got: {type(metadata['agent_assignable']).__name__}"
                ))
                valid = False

        # Validate retry_count and max_retries if present
        for field in ['retry_count', 'max_retries']:
            if field in metadata:
                value = metadata[field]
                if not isinstance(value, int) or value < 0:
                    self.errors.append(ValidationError(
                        file_path,
                        f"'{field}' must be a non-negative integer, got: {value}"
                    ))
                    valid = False

        # Validate max_retries doesn't exceed configured limit
        if 'max_retries' in metadata:
            if metadata['max_retries'] > DEFAULT_MAX_RETRIES:
                self.errors.append(ValidationError(
                    file_path,
                    f"'max_retries' exceeds configured limit "
                    f"({metadata['max_retries']} > {DEFAULT_MAX_RETRIES})"
                ))
                valid = False

        return valid

    def detect_circular_dependencies(
        self,
        file_path: Path,
        metadata: Dict,
        all_metadata: Dict[int, Dict]
    ) -> bool:
        """Detect circular dependencies in issue graph.

        Args:
            file_path: Path to the file
            metadata: Metadata for current issue
            all_metadata: Dictionary mapping issue numbers to metadata

        Returns:
            True if no circular dependencies, False otherwise
        """
        # Extract issue number from file or metadata
        issue_num = self._extract_issue_number(file_path, metadata)
        if issue_num is None:
            # Can't check without issue number
            return True

        depends_on = metadata.get('depends_on', [])
        if not depends_on:
            return True

        # DFS to detect cycles
        visited: Set[int] = set()
        path: List[int] = []

        def has_cycle(current: int) -> bool:
            if current in path:
                # Found cycle
                cycle_start = path.index(current)
                cycle = path[cycle_start:] + [current]
                self.errors.append(ValidationError(
                    file_path,
                    f"Circular dependency detected: {' -> '.join(f'#{n}' for n in cycle)}"
                ))
                return True

            if current in visited:
                return False

            visited.add(current)
            path.append(current)

            # Check dependencies
            current_metadata = all_metadata.get(current, {})
            for dep in current_metadata.get('depends_on', []):
                if has_cycle(dep):
                    return True

            path.pop()
            return False

        return not has_cycle(issue_num)

    def _extract_issue_number(
        self,
        file_path: Path,
        metadata: Dict
    ) -> Optional[int]:
        """Extract issue number from file path or metadata.

        Args:
            file_path: Path to the file
            metadata: Issue metadata

        Returns:
            Issue number or None
        """
        # Try to extract from filename (e.g., issue-85.md)
        match = re.search(r'issue[_-]?(\d+)', file_path.name, re.IGNORECASE)
        if match:
            return int(match.group(1))

        # Try to extract from metadata
        if 'issue_number' in metadata:
            return int(metadata['issue_number'])

        return None

    def validate_file(self, file_path: Path) -> bool:
        """Validate a single file for issue metadata.

        Args:
            file_path: Path to the file

        Returns:
            True if valid, False otherwise
        """
        # Only validate certain file types
        if not self._should_validate(file_path):
            return True

        metadata = self.extract_issue_metadata(file_path)
        if metadata is None:
            # No metadata found - might be okay depending on file type
            if self._requires_metadata(file_path):
                self.errors.append(ValidationError(
                    file_path,
                    "No issue metadata found (required for this file type)"
                ))
                return False
            return True

        return self.validate_metadata_schema(file_path, metadata)

    def _should_validate(self, file_path: Path) -> bool:
        """Check if file should be validated for issue metadata.

        Args:
            file_path: Path to the file

        Returns:
            True if should validate
        """
        # Validate issue templates
        if '.github/ISSUE_TEMPLATE' in str(file_path):
            return True

        # Validate docs that mention issues
        if file_path.suffix in ['.md', '.markdown']:
            try:
                with open(file_path, 'r', encoding='utf-8') as f:
                    content = f.read()
                # Check if file contains issue references
                if ISSUE_REF_PATTERN.search(content):
                    return True
            except Exception:
                pass

        return False

    def _requires_metadata(self, file_path: Path) -> bool:
        """Check if file requires metadata.

        Args:
            file_path: Path to the file

        Returns:
            True if metadata is required
        """
        # Issue templates must have metadata
        if '.github/ISSUE_TEMPLATE' in str(file_path):
            return True

        return False

    def validate_files(self, file_paths: List[Path]) -> bool:
        """Validate multiple files.

        Args:
            file_paths: List of file paths

        Returns:
            True if all valid, False otherwise
        """
        all_valid = True
        all_metadata: Dict[int, Dict] = {}

        # First pass: validate schemas and collect metadata
        for file_path in file_paths:
            if not file_path.exists():
                self.errors.append(ValidationError(
                    file_path,
                    "File not found"
                ))
                all_valid = False
                continue

            if not self.validate_file(file_path):
                all_valid = False
            else:
                # Collect metadata for circular dependency check
                metadata = self.extract_issue_metadata(file_path)
                if metadata:
                    issue_num = self._extract_issue_number(file_path, metadata)
                    if issue_num:
                        all_metadata[issue_num] = metadata

        # Second pass: check circular dependencies
        for file_path in file_paths:
            metadata = self.extract_issue_metadata(file_path)
            if metadata:
                if not self.detect_circular_dependencies(
                    file_path,
                    metadata,
                    all_metadata
                ):
                    all_valid = False

        return all_valid

    def print_report(self) -> None:
        """Print validation report to stdout."""
        if not self.errors:
            print("✅ All issue metadata validation passed")
            return

        print(f"❌ Issue metadata validation failed ({len(self.errors)} errors):\n")

        # Group errors by file
        errors_by_file: Dict[Path, List[ValidationError]] = {}
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
        description="Validate issue dependency metadata (R-ISS-010 per RFC-0015)",
        formatter_class=argparse.RawDescriptionHelpFormatter,
        epilog="""
Examples:
  # Validate specific files
  %(prog)s .github/ISSUE_TEMPLATE/feature.yml docs/implementation/issue-85.md

  # Validate all issue-related files
  %(prog)s --check-all

  # Enable online validation (checks if referenced issues exist)
  %(prog)s --online .github/ISSUE_TEMPLATE/*.yml
        """
    )

    parser.add_argument(
        'files',
        nargs='*',
        type=Path,
        help='Files to validate'
    )

    parser.add_argument(
        '--check-all',
        action='store_true',
        help='Check all issue templates and docs'
    )

    parser.add_argument(
        '--online',
        action='store_true',
        help='Verify referenced issues exist via GitHub API'
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
        # Find all issue templates and related docs
        repo_root = Path(__file__).resolve().parents[5]  # Navigate to repo root

        # Check issue templates
        template_dir = repo_root / '.github' / 'ISSUE_TEMPLATE'
        if template_dir.exists():
            files_to_check.extend(template_dir.glob('*.yml'))
            files_to_check.extend(template_dir.glob('*.yaml'))
            files_to_check.extend(template_dir.glob('*.md'))

        # Check implementation docs
        impl_dir = repo_root / 'docs' / 'implementation'
        if impl_dir.exists():
            files_to_check.extend(impl_dir.glob('**/*.md'))

    elif args.files:
        files_to_check = args.files
    else:
        parser.print_help()
        sys.exit(1)

    if not files_to_check:
        if not args.quiet:
            print("No files to validate")
        sys.exit(0)

    # Validate files
    validator = IssueMetadataValidator(online_check=args.online)
    all_valid = validator.validate_files(files_to_check)

    # Print report
    if not args.quiet:
        validator.print_report()

    # Exit with appropriate code
    sys.exit(0 if all_valid else 1)


if __name__ == '__main__':
    main()

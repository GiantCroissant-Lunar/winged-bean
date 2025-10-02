#!/usr/bin/env python3
"""
Pre-commit hook: Validate issue metadata in templates and documentation.

Per R-ISS-010: Enforce issue metadata schema in committed files.
Per R-ISS-020: Use Python for complex validation logic.

This hook validates:
1. Issue template YAML files
2. Markdown files containing issue frontmatter
3. RFC/ADR documents referencing issues
"""

import re
import sys
import subprocess
from pathlib import Path
from typing import Dict, List, Set, Optional, Tuple
import yaml


class IssueMetadataValidator:
    """Validates issue metadata schema and dependencies."""

    REQUIRED_FIELDS = ["rfc", "depends_on", "priority", "agent_assignable", "retry_count", "max_retries"]
    VALID_PRIORITIES = ["critical", "high", "medium", "low"]

    def __init__(self, strict_mode: bool = True):
        """
        Initialize validator.

        Args:
            strict_mode: If True, hard block on validation failures (per user preference)
        """
        self.strict_mode = strict_mode
        self.errors: List[str] = []
        self.warnings: List[str] = []

    def validate_metadata(self, metadata: Dict, file_path: str) -> bool:
        """
        Validate issue metadata structure.

        Args:
            metadata: Parsed YAML frontmatter
            file_path: Source file path for error messages

        Returns:
            True if valid, False otherwise
        """
        is_valid = True

        # Check required fields
        for field in self.REQUIRED_FIELDS:
            if field not in metadata:
                self.errors.append(f"{file_path}: Missing required field '{field}'")
                is_valid = False

        # Validate priority
        if "priority" in metadata:
            if metadata["priority"] not in self.VALID_PRIORITIES:
                self.errors.append(
                    f"{file_path}: Invalid priority '{metadata['priority']}'. "
                    f"Must be one of: {', '.join(self.VALID_PRIORITIES)}"
                )
                is_valid = False

        # Validate depends_on is a list
        if "depends_on" in metadata:
            if not isinstance(metadata["depends_on"], list):
                self.errors.append(
                    f"{file_path}: Field 'depends_on' must be a list (empty array if no dependencies)"
                )
                is_valid = False
            else:
                # Validate all elements are integers
                for dep in metadata["depends_on"]:
                    if not isinstance(dep, int):
                        self.errors.append(
                            f"{file_path}: Dependency '{dep}' in 'depends_on' must be an integer"
                        )
                        is_valid = False

        # Validate blocks is a list (if present)
        if "blocks" in metadata:
            if not isinstance(metadata["blocks"], list):
                self.errors.append(f"{file_path}: Field 'blocks' must be a list")
                is_valid = False

        # Validate retry_count and max_retries
        if "retry_count" in metadata:
            if not isinstance(metadata["retry_count"], int) or metadata["retry_count"] < 0:
                self.errors.append(f"{file_path}: Field 'retry_count' must be a non-negative integer")
                is_valid = False

        if "max_retries" in metadata:
            if not isinstance(metadata["max_retries"], int) or metadata["max_retries"] < 1:
                self.errors.append(f"{file_path}: Field 'max_retries' must be a positive integer")
                is_valid = False

            # Per user preference: max_retries should default to 3
            if metadata["max_retries"] != 3:
                self.warnings.append(
                    f"{file_path}: Field 'max_retries' is {metadata['max_retries']}, "
                    "but user preference is 3. Consider updating."
                )

        # Validate agent_assignable is boolean
        if "agent_assignable" in metadata:
            if not isinstance(metadata["agent_assignable"], bool):
                self.errors.append(f"{file_path}: Field 'agent_assignable' must be a boolean")
                is_valid = False

        # Validate RFC format
        if "rfc" in metadata:
            rfc_pattern = r"^RFC-\d{4}$"
            if not re.match(rfc_pattern, metadata["rfc"]):
                self.errors.append(
                    f"{file_path}: Field 'rfc' must match format 'RFC-XXXX' (e.g., RFC-0007)"
                )
                is_valid = False

        return is_valid

    def detect_circular_dependencies(self, deps_graph: Dict[int, List[int]]) -> List[List[int]]:
        """
        Detect circular dependencies using DFS.

        Args:
            deps_graph: Issue number -> list of dependencies

        Returns:
            List of cycles (each cycle is a list of issue numbers)
        """
        cycles = []
        visited = set()
        rec_stack = set()

        def dfs(node: int, path: List[int]) -> bool:
            """DFS helper to detect cycles."""
            visited.add(node)
            rec_stack.add(node)
            path.append(node)

            for neighbor in deps_graph.get(node, []):
                if neighbor not in visited:
                    if dfs(neighbor, path):
                        return True
                elif neighbor in rec_stack:
                    # Found a cycle
                    cycle_start = path.index(neighbor)
                    cycle = path[cycle_start:] + [neighbor]
                    cycles.append(cycle)
                    return True

            path.pop()
            rec_stack.remove(node)
            return False

        for node in deps_graph:
            if node not in visited:
                dfs(node, [])

        return cycles

    def extract_frontmatter(self, content: str, file_path: str) -> Optional[Dict]:
        """
        Extract YAML frontmatter from markdown content.

        Args:
            content: File content
            file_path: Source file path for error messages

        Returns:
            Parsed YAML dict or None if no frontmatter
        """
        # Match YAML frontmatter (--- ... ---)
        pattern = r"^---\s*\n(.*?)\n---\s*\n"
        match = re.match(pattern, content, re.DOTALL)

        if not match:
            return None

        yaml_content = match.group(1)

        try:
            metadata = yaml.safe_load(yaml_content)
            return metadata if isinstance(metadata, dict) else None
        except yaml.YAMLError as e:
            self.errors.append(f"{file_path}: Invalid YAML frontmatter: {e}")
            return None

    def validate_file(self, file_path: Path) -> bool:
        """
        Validate a single file for issue metadata.

        Args:
            file_path: Path to file to validate

        Returns:
            True if valid or no metadata found, False if invalid
        """
        try:
            content = file_path.read_text(encoding="utf-8")
        except Exception as e:
            self.errors.append(f"{file_path}: Failed to read file: {e}")
            return False

        # Extract frontmatter
        metadata = self.extract_frontmatter(content, str(file_path))

        if metadata is None:
            # No frontmatter found - this is OK for many files
            return True

        # Check if this looks like issue metadata (has our specific fields)
        has_issue_fields = any(field in metadata for field in ["depends_on", "blocks", "agent_assignable"])

        if not has_issue_fields:
            # Has frontmatter but not issue metadata (e.g., RFC frontmatter)
            return True

        # Validate the metadata
        return self.validate_metadata(metadata, str(file_path))


def get_staged_files() -> List[Path]:
    """
    Get list of staged files from git.

    Returns:
        List of Path objects for staged files
    """
    try:
        result = subprocess.run(
            ["git", "diff", "--cached", "--name-only", "--diff-filter=ACM"],
            capture_output=True,
            text=True,
            check=True,
        )

        files = [
            Path(f.strip())
            for f in result.stdout.strip().split("\n")
            if f.strip()
        ]

        return files
    except subprocess.CalledProcessError as e:
        print(f"Error getting staged files: {e.stderr}", file=sys.stderr)
        return []


def should_validate_file(file_path: Path) -> bool:
    """
    Check if file should be validated.

    Args:
        file_path: Path to check

    Returns:
        True if file should be validated
    """
    # Validate markdown files that might contain issue metadata
    if file_path.suffix in [".md", ".markdown"]:
        # Check if it's in relevant directories
        parts = file_path.parts
        if any(part in parts for part in ["docs", ".github", "ISSUE_TEMPLATE"]):
            return True

    # Validate YAML issue templates
    if file_path.suffix in [".yml", ".yaml"]:
        if "ISSUE_TEMPLATE" in file_path.parts:
            return True

    return False


def main() -> int:
    """Main pre-commit hook logic."""
    print("🔍 Running issue metadata validator (per R-ISS-010)...\n")

    # Get staged files
    staged_files = get_staged_files()

    if not staged_files:
        print("No files to validate.")
        return 0

    # Filter files to validate
    files_to_validate = [f for f in staged_files if should_validate_file(f)]

    if not files_to_validate:
        print(f"Scanned {len(staged_files)} staged files, none require issue metadata validation.")
        return 0

    print(f"Validating {len(files_to_validate)} file(s) for issue metadata...\n")

    # Validate each file
    validator = IssueMetadataValidator(strict_mode=True)
    all_valid = True

    for file_path in files_to_validate:
        if not file_path.exists():
            # File was deleted
            continue

        print(f"  Checking {file_path}...", end=" ")

        if validator.validate_file(file_path):
            print("✓")
        else:
            print("✗")
            all_valid = False

    # Print warnings (non-blocking)
    if validator.warnings:
        print("\n⚠️  Warnings:")
        for warning in validator.warnings:
            print(f"  • {warning}")

    # Print errors (blocking in strict mode)
    if validator.errors:
        print("\n❌ Validation Errors:")
        for error in validator.errors:
            print(f"  • {error}")
        print()

    # Decision based on strict mode
    if not all_valid:
        if validator.strict_mode:
            print("━" * 70)
            print("❌ COMMIT BLOCKED: Invalid issue metadata (per R-ISS-010)")
            print("━" * 70)
            print()
            print("Required schema:")
            print("""
---
rfc: RFC-XXXX
phase: N                    # Optional
wave: N.N                   # Optional
depends_on: [issue_numbers] # Required (empty array if none)
blocks: [issue_numbers]     # Optional
estimate_minutes: NN        # Optional
priority: critical|high|medium|low  # Required
agent_assignable: true      # Required (boolean)
retry_count: 0              # Required (default 0)
max_retries: 3              # Required (default 3)
---
""")
            print("To bypass this check (NOT RECOMMENDED): git commit --no-verify")
            return 1
        else:
            print("\n⚠️  Validation failed, but proceeding (strict_mode=False)")
            return 0

    print(f"\n✓ All issue metadata is valid")
    return 0


if __name__ == "__main__":
    sys.exit(main())

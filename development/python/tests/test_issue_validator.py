#!/usr/bin/env python3
"""
Unit tests for pre-commit issue metadata validator.

Per R-TST-010: Use xUnit conventions for testing.
"""

import pytest
from pathlib import Path
import tempfile
import sys

# Add src to path
sys.path.insert(0, str(Path(__file__).parent.parent / "src"))

from hooks.pre_commit_issue_validator import IssueMetadataValidator


class TestIssueMetadataValidator:
    """Test suite for IssueMetadataValidator."""

    @pytest.fixture
    def validator(self):
        """Create validator instance for testing."""
        return IssueMetadataValidator(strict_mode=True)

    def test_valid_metadata(self, validator):
        """Test validation of correct metadata."""
        valid_metadata = {
            "rfc": "RFC-0007",
            "phase": 1,
            "wave": 1.1,
            "depends_on": [48, 62],
            "blocks": [86, 87],
            "estimate_minutes": 30,
            "priority": "critical",
            "agent_assignable": True,
            "retry_count": 0,
            "max_retries": 3,
        }

        assert validator.validate_metadata(valid_metadata, "test.md") is True
        assert len(validator.errors) == 0

    def test_missing_required_fields(self, validator):
        """Test detection of missing required fields."""
        incomplete_metadata = {
            "rfc": "RFC-0007",
            "priority": "high",
            # Missing: depends_on, agent_assignable, retry_count, max_retries
        }

        assert validator.validate_metadata(incomplete_metadata, "test.md") is False
        assert len(validator.errors) >= 4
        assert any("depends_on" in err for err in validator.errors)
        assert any("agent_assignable" in err for err in validator.errors)

    def test_invalid_priority(self, validator):
        """Test detection of invalid priority values."""
        invalid_metadata = {
            "rfc": "RFC-0007",
            "depends_on": [],
            "priority": "super-urgent",  # Invalid
            "agent_assignable": True,
            "retry_count": 0,
            "max_retries": 3,
        }

        assert validator.validate_metadata(invalid_metadata, "test.md") is False
        assert any("Invalid priority" in err for err in validator.errors)

    def test_invalid_depends_on_type(self, validator):
        """Test detection of non-list depends_on field."""
        invalid_metadata = {
            "rfc": "RFC-0007",
            "depends_on": "48, 62",  # Should be list, not string
            "priority": "medium",
            "agent_assignable": True,
            "retry_count": 0,
            "max_retries": 3,
        }

        assert validator.validate_metadata(invalid_metadata, "test.md") is False
        assert any("must be a list" in err for err in validator.errors)

    def test_invalid_depends_on_elements(self, validator):
        """Test detection of non-integer elements in depends_on."""
        invalid_metadata = {
            "rfc": "RFC-0007",
            "depends_on": [48, "62", 70],  # String in list
            "priority": "medium",
            "agent_assignable": True,
            "retry_count": 0,
            "max_retries": 3,
        }

        assert validator.validate_metadata(invalid_metadata, "test.md") is False
        assert any("must be an integer" in err for err in validator.errors)

    def test_invalid_rfc_format(self, validator):
        """Test detection of invalid RFC format."""
        invalid_metadata = {
            "rfc": "RFC-07",  # Should be RFC-0007 (4 digits)
            "depends_on": [],
            "priority": "low",
            "agent_assignable": True,
            "retry_count": 0,
            "max_retries": 3,
        }

        assert validator.validate_metadata(invalid_metadata, "test.md") is False
        assert any("RFC-XXXX" in err for err in validator.errors)

    def test_negative_retry_count(self, validator):
        """Test detection of negative retry_count."""
        invalid_metadata = {
            "rfc": "RFC-0007",
            "depends_on": [],
            "priority": "low",
            "agent_assignable": True,
            "retry_count": -1,  # Invalid
            "max_retries": 3,
        }

        assert validator.validate_metadata(invalid_metadata, "test.md") is False
        assert any("non-negative integer" in err for err in validator.errors)

    def test_max_retries_warning(self, validator):
        """Test warning when max_retries != 3 (user preference)."""
        metadata_with_non_standard_retries = {
            "rfc": "RFC-0007",
            "depends_on": [],
            "priority": "low",
            "agent_assignable": True,
            "retry_count": 0,
            "max_retries": 5,  # Non-standard
        }

        # Should be valid but generate warning
        assert validator.validate_metadata(metadata_with_non_standard_retries, "test.md") is True
        assert len(validator.warnings) > 0
        assert any("user preference is 3" in warn for warn in validator.warnings)

    def test_extract_frontmatter_valid(self, validator):
        """Test extraction of valid YAML frontmatter."""
        content = """---
rfc: RFC-0007
depends_on: [48]
priority: high
agent_assignable: true
retry_count: 0
max_retries: 3
---

# Issue Title

Issue body content here.
"""

        metadata = validator.extract_frontmatter(content, "test.md")
        assert metadata is not None
        assert metadata["rfc"] == "RFC-0007"
        assert metadata["depends_on"] == [48]

    def test_extract_frontmatter_none_when_missing(self, validator):
        """Test that files without frontmatter return None."""
        content = """# Issue Title

No frontmatter here.
"""

        metadata = validator.extract_frontmatter(content, "test.md")
        assert metadata is None

    def test_extract_frontmatter_invalid_yaml(self, validator):
        """Test handling of invalid YAML frontmatter."""
        content = """---
rfc: RFC-0007
invalid yaml: {[}
---

# Issue
"""

        metadata = validator.extract_frontmatter(content, "test.md")
        assert metadata is None
        assert len(validator.errors) > 0
        assert any("Invalid YAML" in err for err in validator.errors)

    def test_validate_file_with_metadata(self, validator):
        """Test validation of a file with issue metadata."""
        with tempfile.NamedTemporaryFile(mode="w", suffix=".md", delete=False) as f:
            f.write("""---
rfc: RFC-0007
depends_on: []
priority: medium
agent_assignable: true
retry_count: 0
max_retries: 3
---

# Test Issue
""")
            temp_path = Path(f.name)

        try:
            assert validator.validate_file(temp_path) is True
            assert len(validator.errors) == 0
        finally:
            temp_path.unlink()

    def test_validate_file_without_metadata(self, validator):
        """Test validation of a file without issue metadata (should pass)."""
        with tempfile.NamedTemporaryFile(mode="w", suffix=".md", delete=False) as f:
            f.write("# Regular Document\n\nNo metadata here.")
            temp_path = Path(f.name)

        try:
            assert validator.validate_file(temp_path) is True
        finally:
            temp_path.unlink()

    def test_validate_file_with_rfc_frontmatter_only(self, validator):
        """Test that RFC frontmatter (without issue fields) is ignored."""
        with tempfile.NamedTemporaryFile(mode="w", suffix=".md", delete=False) as f:
            f.write("""---
id: RFC-0007
title: Example RFC
status: Draft
category: framework
---

# RFC Content
""")
            temp_path = Path(f.name)

        try:
            # Should pass because it doesn't have issue-specific fields
            assert validator.validate_file(temp_path) is True
        finally:
            temp_path.unlink()

    def test_circular_dependencies_detection(self, validator):
        """Test detection of circular dependencies."""
        # A -> B -> C -> A (cycle)
        deps_graph = {
            1: [2],
            2: [3],
            3: [1],
        }

        cycles = validator.detect_circular_dependencies(deps_graph)
        assert len(cycles) > 0

        # Check that cycle contains the expected nodes
        cycle = cycles[0]
        assert 1 in cycle
        assert 2 in cycle
        assert 3 in cycle

    def test_no_circular_dependencies(self, validator):
        """Test that acyclic graph returns no cycles."""
        # A -> B -> C (no cycle)
        deps_graph = {
            1: [2],
            2: [3],
            3: [],
        }

        cycles = validator.detect_circular_dependencies(deps_graph)
        assert len(cycles) == 0

    def test_empty_depends_on_is_valid(self, validator):
        """Test that empty depends_on array is valid."""
        valid_metadata = {
            "rfc": "RFC-0007",
            "depends_on": [],  # Empty is OK
            "priority": "low",
            "agent_assignable": True,
            "retry_count": 0,
            "max_retries": 3,
        }

        assert validator.validate_metadata(valid_metadata, "test.md") is True
        assert len(validator.errors) == 0


if __name__ == "__main__":
    pytest.main([__file__, "-v"])

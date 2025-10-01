#!/usr/bin/env python3
"""Test file with intentional issues for pre-commit testing."""


def test_function( ):
    """Test function with formatting issues."""
    print( "Hello world" )
    unused_var = "this should trigger ruff"
    return True

if __name__ == "__main__":
    test_function()

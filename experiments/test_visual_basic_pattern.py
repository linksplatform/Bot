#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""Test script to reproduce the Visual Basic pattern matching issue."""

import sys
import os

# Add the parent directory to sys.path to import modules
sys.path.insert(0, os.path.join(os.path.dirname(__file__), '..', 'python'))

from regex import compile as recompile, IGNORECASE
from config import DEFAULT_PROGRAMMING_LANGUAGES_PATTERN_STRING as DEFAULT_LANGUAGES

# Recreate the patterns from patterns.py
TOP = recompile(
    r'\A\s*(топ|верх|top)\s*(?P<maximum_users>\d+)?\s*\Z', IGNORECASE)

TOP_LANGUAGES = recompile(
    r'\A\s*(топ|верх|top)\s*(?P<count>\d+\s+)?\s*(?P<languages>(' + DEFAULT_LANGUAGES +
    r')(\s+(' + DEFAULT_LANGUAGES + r'))*)\s*\Z', IGNORECASE)

PEOPLE_LANGUAGES = recompile(
    r'\A\s*(люди|народ|people)\s*(?P<languages>(' + DEFAULT_LANGUAGES +
    r')(\s+(' + DEFAULT_LANGUAGES + r'))*)\s*\Z', IGNORECASE)

def test_patterns():
    """Test various Visual Basic related commands."""
    test_cases = [
        "top Visual Basic",
        "top visual basic",
        "TOP Visual Basic", 
        "топ Visual Basic",
        "верх Visual Basic",
        "top 5 Visual Basic",
        "top Visual Basic 5",
        "people Visual Basic",
        "люди Visual Basic",
        "народ Visual Basic",
    ]
    
    print("Testing TOP pattern:")
    for case in test_cases:
        match = TOP.match(case)
        print(f"  '{case}' -> {bool(match)}")
        if match:
            print(f"    Groups: {match.groupdict()}")
    
    print("\nTesting TOP_LANGUAGES pattern:")
    for case in test_cases:
        match = TOP_LANGUAGES.match(case)
        print(f"  '{case}' -> {bool(match)}")
        if match:
            print(f"    Groups: {match.groupdict()}")
    
    print("\nTesting PEOPLE_LANGUAGES pattern:")
    for case in test_cases:
        match = PEOPLE_LANGUAGES.match(case)
        print(f"  '{case}' -> {bool(match)}")
        if match:
            print(f"    Groups: {match.groupdict()}")

if __name__ == "__main__":
    test_patterns()
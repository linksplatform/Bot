#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""Test script to demonstrate the language parsing issue."""

import sys
import os
from regex import split

# Add the parent directory to sys.path to import modules
sys.path.insert(0, os.path.join(os.path.dirname(__file__), '..', 'python'))

from config import DEFAULT_PROGRAMMING_LANGUAGES

def test_current_behavior():
    """Test how the current split logic works with Visual Basic"""
    
    # Simulate what happens when someone types "top Visual Basic"
    matched_languages = "Visual Basic"
    
    print("Current behavior:")
    print(f"Input: '{matched_languages}'")
    
    # This is what the current code does
    languages = split(r"\s+", matched_languages)
    print(f"After split: {languages}")
    
    # Show what the actual language names are in the config
    print(f"\nActual Visual Basic in config: {[lang for lang in DEFAULT_PROGRAMMING_LANGUAGES if 'Visual' in lang]}")

def test_multiple_languages():
    """Test how it should work with multiple languages"""
    
    test_cases = [
        "Python Java",
        "Visual Basic C#",
        "JavaScript TypeScript Python"
    ]
    
    print("\nTesting multiple language parsing:")
    for case in test_cases:
        languages = split(r"\s+", case)
        print(f"'{case}' -> {languages}")

if __name__ == "__main__":
    test_current_behavior()
    test_multiple_languages()
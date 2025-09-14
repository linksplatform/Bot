#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""Test the improved language parsing logic v2."""

import sys
import os
import re
from typing import List

# Add the parent directory to sys.path to import modules
sys.path.insert(0, os.path.join(os.path.dirname(__file__), '..', 'python'))

from config import DEFAULT_PROGRAMMING_LANGUAGES

def parse_languages_from_text(text: str) -> List[str]:
    """
    Parse language names from text, handling multi-word languages like 'Visual Basic'.
    
    :param text: Input text containing language names
    :return: List of matched language names
    """
    # Convert regex patterns back to actual language names for matching
    language_names = []
    for lang_pattern in DEFAULT_PROGRAMMING_LANGUAGES:
        # Remove regex escaping to get actual language name
        lang_name = lang_pattern.replace('\\+', '+').replace('\\-', '-').replace('\\#', '#').replace('\\!', '!')
        language_names.append(lang_name)
    
    # Sort by length (longest first) to match multi-word languages first
    language_names.sort(key=len, reverse=True)
    
    matched_languages = []
    remaining_text = text
    
    # Case-insensitive matching
    for lang_name in language_names:
        # Use word boundaries and case-insensitive matching
        # For special characters, we need to be more careful with escaping
        escaped_name = re.escape(lang_name)
        pattern = r'\b' + escaped_name + r'\b'
        
        if re.search(pattern, remaining_text, re.IGNORECASE):
            matched_languages.append(lang_name)
            # Remove the matched language from remaining text to avoid duplicates
            remaining_text = re.sub(pattern, '', remaining_text, flags=re.IGNORECASE)
            remaining_text = re.sub(r'\s+', ' ', remaining_text).strip()  # Clean up extra spaces
    
    return matched_languages

def test_improved_parsing_v2():
    """Test the improved language parsing v2"""
    test_cases = [
        "Visual Basic",
        "visual basic",
        "Python Java",
        "Visual Basic C#",
        "JavaScript TypeScript Python", 
        "C++ C# Java",
        "Go Python",
        "Objective-C C++",
        "F# C#"
    ]
    
    print("Testing improved language parsing v2:")
    for case in test_cases:
        result = parse_languages_from_text(case)
        print(f"'{case}' -> {result}")

if __name__ == "__main__":
    test_improved_parsing_v2()
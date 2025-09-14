#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""Test the improved language parsing logic v3 using existing utility."""

import sys
import os
import re
from typing import List

# Add the parent directory to sys.path to import modules
sys.path.insert(0, os.path.join(os.path.dirname(__file__), '..', 'python'))

from config import DEFAULT_PROGRAMMING_LANGUAGES
from modules.utils import get_default_programming_language

def parse_languages_from_text(text: str) -> List[str]:
    """
    Parse language names from text, handling multi-word languages like 'Visual Basic'.
    Uses the existing get_default_programming_language utility for accurate matching.
    
    :param text: Input text containing language names
    :return: List of matched language names
    """
    # Get all possible language names (without regex escaping)
    language_names = []
    for lang_pattern in DEFAULT_PROGRAMMING_LANGUAGES:
        # Remove common regex escaping to get actual language name
        lang_name = lang_pattern.replace('\\+', '+').replace('\\-', '-').replace('\\#', '#').replace('\\!', '!').replace('\\', '')
        language_names.append(lang_name)
    
    # Sort by length (longest first) to match multi-word languages first
    language_names.sort(key=len, reverse=True)
    
    matched_languages = []
    remaining_text = text.lower()
    
    # Try to find each language in the text
    for lang_name in language_names:
        # Check if this language appears in the text (case-insensitive)
        if lang_name.lower() in remaining_text:
            # Use word boundaries to ensure we match complete words
            pattern = r'\b' + re.escape(lang_name.lower()) + r'\b'
            if re.search(pattern, remaining_text):
                # Use the utility function to get the correct case
                correct_name = get_default_programming_language(lang_name)
                if correct_name and correct_name not in [ml.replace('\\', '') for ml in matched_languages]:
                    matched_languages.append(correct_name.replace('\\', ''))
                    # Remove the matched language from remaining text
                    remaining_text = re.sub(pattern, '', remaining_text)
                    remaining_text = re.sub(r'\s+', ' ', remaining_text).strip()
    
    return matched_languages

def test_improved_parsing_v3():
    """Test the improved language parsing v3"""
    test_cases = [
        "Visual Basic",
        "visual basic",
        "Python Java",
        "Visual Basic C#",
        "JavaScript TypeScript Python", 
        "C++ C# Java",
        "Go Python",
        "Objective-C C++",
        "F# C#",
        "c++ java python",
        "VISUAL BASIC python"
    ]
    
    print("Testing improved language parsing v3:")
    for case in test_cases:
        result = parse_languages_from_text(case)
        print(f"'{case}' -> {result}")
        
    # Test the utility function directly
    print(f"\nTesting get_default_programming_language:")
    print(f"'visual basic' -> '{get_default_programming_language('visual basic')}'")
    print(f"'c++' -> '{get_default_programming_language('c++')}'") 
    print(f"'c#' -> '{get_default_programming_language('c#')}'")

if __name__ == "__main__":
    test_improved_parsing_v3()
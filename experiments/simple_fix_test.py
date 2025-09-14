#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""Simple test of the language parsing fix."""

import re
from typing import List

# Simplified list of languages for testing
DEFAULT_LANGUAGES = [
    r"Visual Basic",
    r"JavaScript", 
    r"TypeScript",
    r"Java",
    r"Python",
    r"C\+\+",
    r"C",  
    r"C#",
    r"Objective\-C",
    r"Go",
    r"F#"
]

def get_default_programming_language(language: str) -> str:
    """Returns default appearance of language"""
    language = language.lower()
    for lang in DEFAULT_LANGUAGES:
        if lang.replace('\\', '').lower() == language:
            return lang.replace('\\', '')
    return ""

def parse_languages_from_text(text: str) -> List[str]:
    """
    Parse language names from text, handling multi-word languages like 'Visual Basic'.
    
    :param text: Input text containing language names
    :return: List of matched language names
    """
    # Get all possible language names (without regex escaping)
    language_names = []
    for lang_pattern in DEFAULT_LANGUAGES:
        # Remove common regex escaping to get actual language name
        lang_name = lang_pattern.replace('\\+', '+').replace('\\-', '-').replace('\\#', '#')
        language_names.append(lang_name)
    
    # Sort by length (longest first) to match multi-word languages first
    language_names.sort(key=len, reverse=True)
    
    matched_languages = []
    remaining_text = text.lower()
    
    # Try to find each language in the text
    for lang_name in language_names:
        # Check if this language appears in the text (case-insensitive)
        lang_lower = lang_name.lower()
        if lang_lower in remaining_text:
            # Use word boundaries to ensure we match complete words
            pattern = r'\b' + re.escape(lang_lower) + r'\b'
            if re.search(pattern, remaining_text):
                # Get the correct case using the utility function
                correct_name = get_default_programming_language(lang_name)
                if correct_name and correct_name not in matched_languages:
                    matched_languages.append(correct_name)
                    # Remove the matched language from remaining text
                    remaining_text = re.sub(pattern, '', remaining_text)
                    remaining_text = re.sub(r'\s+', ' ', remaining_text).strip()
    
    return matched_languages

def test_parsing():
    """Test the language parsing"""
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
    
    print("Testing language parsing:")
    for case in test_cases:
        result = parse_languages_from_text(case)
        print(f"'{case}' -> {result}")

if __name__ == "__main__":
    test_parsing()
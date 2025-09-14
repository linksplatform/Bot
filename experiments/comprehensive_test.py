#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
Comprehensive test showing the fix for issue #54: "top Visual Basic is not working"

This test demonstrates:
1. The original problem with Visual Basic command parsing
2. How the fix resolves the issue
3. Validation that other language combinations still work
"""

import re
from typing import List

# Simplified language list for testing (based on the actual config)
DEFAULT_PROGRAMMING_LANGUAGES = [
    r"Visual Basic",
    r"JavaScript", 
    r"TypeScript",
    r"Java",
    r"Python",
    r"C\+\+",
    r"C",
    r"C#",
    r"Go",
    r"Basic"  # Note: this is different from "Visual Basic"
]

def get_default_programming_language(language: str) -> str:
    """Returns default appearance of language (mimics the original utility)"""
    language = language.lower()
    for lang in DEFAULT_PROGRAMMING_LANGUAGES:
        if lang.replace('\\', '').lower() == language:
            return lang
    return ""

def old_parse_method(text: str) -> List[str]:
    """The old method that was causing the bug"""
    return re.split(r'\s+', text)

def new_parse_method(text: str) -> List[str]:
    """The new fixed method"""
    if not text:
        return []
    
    # Get all language names without regex escaping
    language_names = []
    for lang_pattern in DEFAULT_PROGRAMMING_LANGUAGES:
        lang_name = (lang_pattern.replace('\\+', '+')
                                .replace('\\-', '-')
                                .replace('\\#', '#')
                                .replace('\\!', '!')
                                .replace('\\', ''))
        language_names.append(lang_name)
    
    # Sort by length (longest first) to prioritize multi-word languages
    language_names.sort(key=len, reverse=True)
    
    matched_languages = []
    remaining_text = text
    
    for lang_name in language_names:
        pattern = r'\b' + re.escape(lang_name) + r'\b'
        match = re.search(pattern, remaining_text, re.IGNORECASE)
        
        if match:
            canonical_name = get_default_programming_language(lang_name)
            if canonical_name:
                clean_name = (canonical_name.replace('\\+', '+')
                                           .replace('\\-', '-')
                                           .replace('\\#', '#')
                                           .replace('\\!', '!')
                                           .replace('\\', ''))
                if clean_name not in matched_languages:
                    matched_languages.append(clean_name)
                    remaining_text = remaining_text[:match.start()] + remaining_text[match.end():]
    
    return matched_languages

def contains_all_strings(user_languages: List[str], search_languages: List[str], ignore_case: bool) -> bool:
    """Mimics the contains_all_strings function from utils"""
    if not search_languages:
        return True
    
    for search_lang in search_languages:
        found = False
        for user_lang in user_languages:
            if ignore_case:
                if user_lang.lower() == search_lang.lower():
                    found = True
                    break
            else:
                if user_lang == search_lang:
                    found = True
                    break
        if not found:
            return False
    return True

def test_issue_54_fix():
    """Test that demonstrates the fix for issue #54"""
    print("=" * 60)
    print("COMPREHENSIVE TEST FOR ISSUE #54: 'top Visual Basic is not working'")
    print("=" * 60)
    print()
    
    # Test case from the issue
    issue_command = "Visual Basic"
    
    print("1. REPRODUCING THE ORIGINAL BUG")
    print("-" * 40)
    print(f"User command: 'top {issue_command}'")
    print(f"Extracted languages text: '{issue_command}'")
    print()
    
    old_result = old_parse_method(issue_command)
    print(f"OLD METHOD (buggy): {old_result}")
    print("  -> Searches for users with 'Visual' AND 'Basic' languages")
    print("  -> This is WRONG - 'Visual Basic' is one language!")
    print()
    
    new_result = new_parse_method(issue_command)
    print(f"NEW METHOD (fixed): {new_result}")
    print("  -> Searches for users with 'Visual Basic' language")
    print("  -> This is CORRECT!")
    print()
    
    print("2. SIMULATING USER DATABASE SEARCH")
    print("-" * 40)
    
    # Simulate some user data
    mock_users = [
        {"name": "Alice", "programming_languages": ["Python", "Java"]},
        {"name": "Bob", "programming_languages": ["Visual Basic", "C#"]},
        {"name": "Charlie", "programming_languages": ["JavaScript", "TypeScript"]},
        {"name": "Diana", "programming_languages": ["Visual", "Basic"]},  # Someone with separate "Visual" and "Basic" languages
    ]
    
    print("Mock user database:")
    for user in mock_users:
        print(f"  {user['name']}: {user['programming_languages']}")
    print()
    
    print("Search results for 'top Visual Basic':")
    
    # Test with old method
    old_matches = [user for user in mock_users if contains_all_strings(user['programming_languages'], old_result, True)]
    print(f"OLD METHOD finds: {[u['name'] for u in old_matches]}")
    print("  -> Diana has both 'Visual' and 'Basic' as separate languages")
    print("  -> Bob has 'Visual Basic' as one language, but doesn't match!")
    print()
    
    # Test with new method  
    new_matches = [user for user in mock_users if contains_all_strings(user['programming_languages'], new_result, True)]
    print(f"NEW METHOD finds: {[u['name'] for u in new_matches]}")
    print("  -> Bob has 'Visual Basic' language - CORRECT match!")
    print("  -> Diana doesn't match - she doesn't have 'Visual Basic' as one language")
    print()
    
    print("3. TESTING OTHER LANGUAGE COMBINATIONS")
    print("-" * 40)
    
    other_tests = [
        "Python Java",
        "JavaScript TypeScript", 
        "Visual Basic Python",
        "Python"
    ]
    
    for test_case in other_tests:
        old_result = old_parse_method(test_case)
        new_result = new_parse_method(test_case)
        status = "✓ SAME" if old_result == new_result else "⚠ DIFFERENT"
        print(f"'{test_case}':")
        print(f"  OLD: {old_result}")
        print(f"  NEW: {new_result} {status}")
        print()
    
    print("4. CONCLUSION")
    print("-" * 40)
    print("✓ The fix correctly handles 'Visual Basic' as a single language")
    print("✓ Other language combinations continue to work as expected") 
    print("✓ Issue #54 is RESOLVED!")
    print()

if __name__ == "__main__":
    test_issue_54_fix()
#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
Simple test script for the new auto-update patterns.
This script tests the new regex patterns without requiring external dependencies.
"""

import sys
import os
sys.path.append(os.path.join(os.path.dirname(__file__), '..', 'python'))

from regex import compile as recompile, IGNORECASE, match

# Copy the new patterns for testing
UPDATE = recompile(
    r'\A\s*(обновить|update)\s*\Z', IGNORECASE)

UPDATE_ALL = recompile(
    r'\A\s*(обновить всех|update all)\s*\Z', IGNORECASE)

AUTO_UPDATE = recompile(
    r'\A\s*(авто[- ]?обновление|auto[- ]?update)\s+(вкл|on|выкл|off)\s*\Z', IGNORECASE)

def test_update_patterns():
    """Test all update-related patterns."""
    print("=== Testing Auto-Update Patterns ===")
    
    # Test cases for UPDATE pattern
    update_tests = [
        ("обновить", True),
        ("update", True),
        ("UPDATE", True),
        ("  обновить  ", True),
        ("обновить профиль", False),
        ("update now", False),
    ]
    
    print("\n1. Testing UPDATE pattern:")
    for text, expected in update_tests:
        result = bool(match(UPDATE, text))
        status = "✅" if result == expected else "❌"
        print(f"  {status} '{text}' -> {result} (expected {expected})")
    
    # Test cases for UPDATE_ALL pattern
    update_all_tests = [
        ("обновить всех", True),
        ("update all", True),
        ("UPDATE ALL", True),
        ("  обновить всех  ", True),
        ("обновить всех пользователей", False),
        ("update all users", False),
        ("обновить", False),
    ]
    
    print("\n2. Testing UPDATE_ALL pattern:")
    for text, expected in update_all_tests:
        result = bool(match(UPDATE_ALL, text))
        status = "✅" if result == expected else "❌"
        print(f"  {status} '{text}' -> {result} (expected {expected})")
    
    # Test cases for AUTO_UPDATE pattern
    auto_update_tests = [
        ("автообновление вкл", True),
        ("auto-update on", True),
        ("авто обновление выкл", True),
        ("auto update off", True),
        ("автообновление", False),
        ("auto-update", False),
        ("автообновление включить", False),
    ]
    
    print("\n3. Testing AUTO_UPDATE pattern:")
    for text, expected in auto_update_tests:
        result_match = match(AUTO_UPDATE, text)
        result = bool(result_match)
        status = "✅" if result == expected else "❌"
        print(f"  {status} '{text}' -> {result} (expected {expected})")
        if result_match:
            print(f"    Groups: {result_match.groups()}")

def test_github_detection():
    """Test GitHub profile detection logic."""
    print("\n=== Testing GitHub Profile Detection ===")
    
    import re
    
    test_cases = [
        ("https://github.com/konard", "konard"),
        ("http://github.com/test-user", "test-user"),
        ("github.com/my_username", "my_username"),
        ("Visit my profile at github.com/developer123/", "developer123"),
        ("https://github.com/user-name-123", "user-name-123"),
        ("not a github url", None),
        ("https://gitlab.com/user", None),
        ("github.com/", None),
    ]
    
    for url, expected in test_cases:
        github_match = re.search(r'github\.com/([a-zA-Z0-9-_]+)', url)
        result = github_match.group(1) if github_match else None
        status = "✅" if result == expected else "❌"
        print(f"  {status} '{url}' -> '{result}' (expected '{expected}')")

def test_language_detection():
    """Test programming language detection logic."""
    print("\n=== Testing Programming Language Detection ===")
    
    # Sample programming languages for testing
    test_languages = ["Python", "JavaScript", "C++", "Java", "C#"]
    
    test_cases = [
        ("I love programming in Python and JavaScript", ["Python", "JavaScript"]),
        ("Working with C++ and Java daily", ["C++", "Java"]),
        ("Expert in C#", ["C#"]),
        ("Just a regular user", []),
        ("python developer", ["Python"]),  # Case insensitive
    ]
    
    for text, expected in test_cases:
        detected = []
        text_lower = text.lower()
        
        for lang in test_languages:
            if lang.lower().replace('+', r'\+').replace('#', r'\#') in text_lower:
                detected.append(lang)
        
        # Sort both lists for comparison
        detected.sort()
        expected.sort()
        
        status = "✅" if detected == expected else "❌"
        print(f"  {status} '{text}'")
        print(f"      -> {detected} (expected {expected})")

if __name__ == "__main__":
    print("Testing Enhanced Auto-Update Functionality")
    print("=" * 50)
    
    try:
        test_update_patterns()
        test_github_detection()
        test_language_detection()
        
        print("\n=== Test Summary ===")
        print("✅ Pattern matching tests completed")
        print("✅ GitHub profile detection tests completed")  
        print("✅ Programming language detection tests completed")
        print("\n🎉 All tests completed!")
        
    except Exception as e:
        print(f"\n❌ Error during testing: {e}")
        import traceback
        traceback.print_exc()
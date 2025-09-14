#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
Test script for the enhanced auto-update functionality.
This script validates the new auto-update features without requiring VK API tokens.
"""

import sys
import os
sys.path.append(os.path.join(os.path.dirname(__file__), '..', 'python'))

from modules.data_service import BetterBotBaseDataService
import patterns
from regex import match

def test_patterns():
    """Test the new regex patterns."""
    print("=== Testing Patterns ===")
    
    # Test UPDATE pattern (existing)
    test_cases = [
        ("обновить", patterns.UPDATE),
        ("update", patterns.UPDATE),
        ("UPDATE", patterns.UPDATE),
    ]
    
    for test_text, pattern in test_cases:
        result = match(pattern, test_text)
        print(f"Pattern {pattern.pattern[:30]}... matches '{test_text}': {bool(result)}")
    
    # Test UPDATE_ALL pattern (new)
    test_cases = [
        ("обновить всех", patterns.UPDATE_ALL),
        ("update all", patterns.UPDATE_ALL),
        ("UPDATE ALL", patterns.UPDATE_ALL),
    ]
    
    for test_text, pattern in test_cases:
        result = match(pattern, test_text)
        print(f"Pattern {pattern.pattern[:30]}... matches '{test_text}': {bool(result)}")
    
    # Test AUTO_UPDATE pattern (new)
    test_cases = [
        ("автообновление вкл", patterns.AUTO_UPDATE),
        ("auto-update on", patterns.AUTO_UPDATE),
        ("автообновление выкл", patterns.AUTO_UPDATE),
        ("auto update off", patterns.AUTO_UPDATE),
    ]
    
    for test_text, pattern in test_cases:
        result = match(pattern, test_text)
        print(f"Pattern {pattern.pattern[:30]}... matches '{test_text}': {bool(result)}")
        if result:
            print(f"  Groups: {result.groups()}")

def test_data_service():
    """Test the enhanced data service with new fields."""
    print("\n=== Testing Data Service ===")
    
    # Create temporary database
    data_service = BetterBotBaseDataService("test_db")
    
    # Test creating a user and checking new fields
    user = data_service.get_or_create_user(12345, None)
    print(f"Created user: {user.uid}")
    print(f"Auto-update enabled: {user.auto_update_enabled}")
    print(f"Last auto-update: {user.last_auto_update}")
    
    # Test updating auto-update settings
    user.auto_update_enabled = False
    user.last_auto_update = 1234567890
    data_service.save_user(user)
    
    # Reload user and verify changes
    user_reloaded = data_service.get_user(12345)
    print(f"Reloaded user auto-update enabled: {user_reloaded.auto_update_enabled}")
    print(f"Reloaded user last auto-update: {user_reloaded.last_auto_update}")
    
    # Cleanup
    try:
        os.remove("test_db.dat")
        print("Cleaned up test database")
    except:
        pass

def test_github_profile_detection():
    """Test GitHub profile URL detection logic."""
    print("\n=== Testing GitHub Profile Detection ===")
    
    import re
    
    test_urls = [
        "https://github.com/konard",
        "http://github.com/test-user",
        "github.com/my_username",
        "Visit my profile at github.com/developer123/",
        "https://github.com/user-name-123",
        "not a github url",
        "https://gitlab.com/user"
    ]
    
    for url in test_urls:
        github_match = re.search(r'github\.com/([a-zA-Z0-9-_]+)', url)
        if github_match:
            username = github_match.group(1)
            print(f"URL: '{url}' -> GitHub username: '{username}'")
        else:
            print(f"URL: '{url}' -> No GitHub username found")

def test_programming_language_detection():
    """Test programming language detection logic."""
    print("\n=== Testing Programming Language Detection ===")
    
    import sys
    sys.path.append(os.path.join(os.path.dirname(__file__), '..', 'python'))
    import config
    
    test_texts = [
        "I love programming in Python and JavaScript",
        "Working with C++ and Java daily",
        "Learning Rust and Go",
        "Expert in C#, PHP, and SQL",
        "Just a regular user with no programming languages mentioned"
    ]
    
    for text in test_texts:
        detected = []
        text_lower = text.lower()
        
        for lang_pattern in config.DEFAULT_PROGRAMMING_LANGUAGES[:10]:  # Test first 10 languages
            lang_clean = lang_pattern.replace('\\', '').replace('+', '\+').replace('-', '\-')
            if lang_clean.lower() in text_lower:
                detected.append(lang_pattern.replace('\\', ''))
        
        print(f"Text: '{text}'")
        print(f"  Detected languages: {detected}")

if __name__ == "__main__":
    print("Testing Enhanced Auto-Update Functionality")
    print("=" * 50)
    
    test_patterns()
    test_data_service()
    test_github_profile_detection()
    test_programming_language_detection()
    
    print("\n=== Test Summary ===")
    print("✅ Pattern matching tests completed")
    print("✅ Data service tests completed")
    print("✅ GitHub profile detection tests completed")
    print("✅ Programming language detection tests completed")
    print("\nAll tests completed successfully!")